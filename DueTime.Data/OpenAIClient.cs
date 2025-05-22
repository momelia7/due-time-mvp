using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, string> _suggestionCache = new Dictionary<string, string>();
        private static readonly object _cacheLock = new object();

        public static async Task<string?> GetProjectSuggestionAsync(string windowTitle, string applicationName, string[] projectNames, string apiKey)
        {
            // Create a unique key for the cache based on the input parameters
            string contextKey = $"{applicationName}|{windowTitle}|{string.Join(",", projectNames)}";
            
            // Check if we have a cached suggestion for this context
            lock (_cacheLock)
            {
                if (_suggestionCache.TryGetValue(contextKey, out var cachedSuggestion))
                {
                    return cachedSuggestion;
                }
            }
            
            // If no cached suggestion, make the API call
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var requestBody = new
            {
                model = "text-davinci-003",
                prompt = $"Projects: [{string.Join(", ", projectNames)}]. Activity: Window title '{windowTitle}', Application '{applicationName}'. Which project does this most likely belong to?",
                max_tokens = 50,
                temperature = 0.3
            };
            
            try
            {
                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/completions", requestBody);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonDocument.Parse(responseBody);
                    var choicesElement = jsonResponse.RootElement.GetProperty("choices");
                    
                    if (choicesElement.GetArrayLength() > 0)
                    {
                        var suggestion = choicesElement[0].GetProperty("text").GetString()?.Trim();
                        
                        // Try to match the suggestion with one of the project names
                        if (suggestion != null && projectNames.Length > 0)
                        {
                            foreach (var project in projectNames)
                            {
                                if (suggestion.Contains(project, StringComparison.OrdinalIgnoreCase))
                                {
                                    suggestion = project;
                                    break;
                                }
                            }
                        }
                        
                        // Cache the suggestion
                        if (!string.IsNullOrEmpty(suggestion))
                        {
                            lock (_cacheLock)
                            {
                                _suggestionCache[contextKey] = suggestion;
                            }
                        }
                        
                        return suggestion;
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception if needed
                // Just return null on failure
            }
            
            return null;
        }
        
        public static async Task<string?> GetWeeklySummaryAsync(DateTime startDate, DateTime endDate, string prompt, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var requestBody = new
            {
                model = "text-davinci-003",
                prompt = prompt,
                max_tokens = 200,
                temperature = 0.7
            };
            
            try
            {
                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/completions", requestBody);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonDocument.Parse(responseBody);
                    var choicesElement = jsonResponse.RootElement.GetProperty("choices");
                    
                    if (choicesElement.GetArrayLength() > 0)
                    {
                        return choicesElement[0].GetProperty("text").GetString()?.Trim();
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception if needed
            }
            
            return null;
        }
        
        /// <summary>
        /// Tests the connection to OpenAI API with the provided API key
        /// </summary>
        /// <param name="apiKey">The API key to test</param>
        /// <returns>True if the connection is successful, false otherwise</returns>
        public static async Task<bool> TestConnectionAsync(string apiKey)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var requestBody = new
            {
                model = "text-davinci-003",
                prompt = "Say hello",
                max_tokens = 5,
                temperature = 0.3
            };
            
            try
            {
                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/completions", requestBody);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Clears the suggestion cache
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _suggestionCache.Clear();
            }
        }
    }
} 