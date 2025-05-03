using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DueTime.Data
{
    public static class OpenAIClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

        public static async Task<string?> GetProjectSuggestionAsync(
            string windowTitle, 
            string applicationName, 
            string[] projectNames,
            string apiKey)
        {
            if (projectNames.Length == 0)
                return null;

            try
            {
                // Add available projects to the prompt
                var projectsList = string.Join(", ", projectNames);
                var promptText = $"Based on this window title '{windowTitle}' from application '{applicationName}', " +
                                $"which of these projects does it most likely belong to? Available projects: {projectsList}. " +
                                $"Reply with just the project name, or 'None' if it doesn't match any project.";

                // Setup request
                var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Create request body
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant that categorizes work based on window titles." },
                        new { role = "user", content = promptText }
                    },
                    max_tokens = 50,
                    temperature = 0.3
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                // Send request
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                // Parse response
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                // Clean up the response to just get the project name
                var projectSuggestion = content?.Trim();
                if (string.IsNullOrEmpty(projectSuggestion))
                    return null;

                // Verify it's a valid project
                return projectNames.FirstOrDefault(p => 
                    p.Equals(projectSuggestion, StringComparison.OrdinalIgnoreCase)) ?? "None";
            }
            catch
            {
                return null;
            }
        }
    }
} 