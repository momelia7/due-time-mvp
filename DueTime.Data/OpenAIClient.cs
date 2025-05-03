using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DueTime.Data
{
    public static class OpenAIClient
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string?> GetProjectSuggestionAsync(string windowTitle, string applicationName, string[] projectNames, string apiKey)
        {
            // Prepare prompt for completion
            string projectsList = projectNames.Length > 0 ? string.Join(", ", projectNames) : "none";
            string prompt = $"Projects: {projectsList}.\n";
            prompt += $"Activity: Window title \"{windowTitle}\", Application \"{applicationName}\".\n";
            prompt += "Which project does this activity most likely belong to? Respond with the project name or 'None'.";
            
            var requestObj = new
            {
                model = "text-davinci-003",
                prompt = prompt,
                max_tokens = 10,
                temperature = 0.0
            };
            
            var requestJson = JsonSerializer.Serialize(requestObj);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/completions");
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await httpClient.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    // API error (invalid key or quota etc.)
                    return null;
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                var choice = root.GetProperty("choices")[0];
                string text = choice.GetProperty("text").GetString() ?? "";
                text = text.Trim().Trim('"', '\n');
                
                return text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string?> GetWeeklySummaryAsync(DateTime weekStart, DateTime weekEnd, string summaryPrompt, string apiKey)
        {
            // We assume summaryPrompt already contains the structured info for the week
            string prompt = summaryPrompt + "\nProvide a brief summary of the week's work.";
            
            var requestObj = new
            {
                model = "text-davinci-003",
                prompt = prompt,
                max_tokens = 150,
                temperature = 0.5
            };
            
            var requestJson = JsonSerializer.Serialize(requestObj);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/completions");
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await httpClient.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var choice = doc.RootElement.GetProperty("choices")[0];
                string text = choice.GetProperty("text").GetString() ?? "";
                
                return text.Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
} 