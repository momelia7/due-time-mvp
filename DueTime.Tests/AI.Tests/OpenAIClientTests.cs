using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DueTime.Data;
using Xunit;

namespace DueTime.Tests.AI
{
    public class OpenAIClientIntegrationTests
    {
        // Custom HttpMessageHandler to fake HTTP responses
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage? _response;
            private readonly Exception? _exception;
            public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
            public FakeHttpMessageHandler(Exception exception) => _exception = exception;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_exception != null)
                {
                    throw _exception;
                }
                return Task.FromResult(_response!);
            }
        }

        [Fact]
        public async Task GetProjectSuggestionAsync_ReturnsExpectedProjectName_OnSuccess()
        {
            // Arrange: fake a successful API JSON response
            var json = @"{ ""choices"": [ { ""text"": ""ProjectX"" } ] }";
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            // Inject fake HttpClient into OpenAIClient via reflection
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClientField = typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            httpClientField!.SetValue(null, new HttpClient(fakeHandler));
            
            // Act
            string? result = await OpenAIClient.GetProjectSuggestionAsync("WindowTitle", "AppName", new string[] { "ProjectX" }, "fake-api-key");
            
            // Assert: the result should be "ProjectX" (trimmed)
            Assert.Equal("ProjectX", result);
        }

        [Fact]
        public async Task GetProjectSuggestionAsync_ReturnsNull_OnApiError()
        {
            // Arrange: fake a 401 Unauthorized response
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}") };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                                 .SetValue(null, new HttpClient(fakeHandler));
            
            // Act
            string? result = await OpenAIClient.GetProjectSuggestionAsync("Win", "App", new string[0], "fake-api-key");
            
            // Assert: should return null on non-success status code
            Assert.Null(result);
        }

        [Fact]
        public async Task GetWeeklySummaryAsync_ReturnsNull_OnException()
        {
            // Arrange: make the HttpClient throw an exception (simulating network failure)
            var fakeHandler = new FakeHttpMessageHandler(new HttpRequestException("Network error"));
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                                 .SetValue(null, new HttpClient(fakeHandler));
            
            // Act
            string? summary = await OpenAIClient.GetWeeklySummaryAsync(DateTime.Now, DateTime.Now.AddDays(7), "Prompt", "fake-key");
            
            // Assert: exception should be caught, resulting in null
            Assert.Null(summary);
        }

        [Fact]
        public async Task GetProjectSuggestionAsync_UsesCaching_ForSameInput()
        {
            // Clear the cache first
            OpenAIClient.ClearCache();
            
            // Arrange: first response
            var json1 = @"{ ""choices"": [ { ""text"": ""ProjectA"" } ] }";
            var fakeResponse1 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json1) };
            var fakeHandler1 = new FakeHttpMessageHandler(fakeResponse1);
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                               .SetValue(null, new HttpClient(fakeHandler1));
            
            // First call should hit the API
            string? result1 = await OpenAIClient.GetProjectSuggestionAsync("WindowTitle", "AppName", new string[] { "ProjectA", "ProjectB" }, "fake-api-key");
            
            // Arrange: second response (different from first)
            var json2 = @"{ ""choices"": [ { ""text"": ""ProjectB"" } ] }";
            var fakeResponse2 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json2) };
            var fakeHandler2 = new FakeHttpMessageHandler(fakeResponse2);
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                               .SetValue(null, new HttpClient(fakeHandler2));
            
            // Second call with same input should use cache and not hit the API
            string? result2 = await OpenAIClient.GetProjectSuggestionAsync("WindowTitle", "AppName", new string[] { "ProjectA", "ProjectB" }, "fake-api-key");
            
            // Assert: both results should be the same (from cache)
            Assert.Equal("ProjectA", result1);
            Assert.Equal("ProjectA", result2); // Should return cached result, not "ProjectB"
            
            // Different input should hit the API
            string? result3 = await OpenAIClient.GetProjectSuggestionAsync("DifferentWindow", "AppName", new string[] { "ProjectA", "ProjectB" }, "fake-api-key");
            
            // Assert: should get the new result
            Assert.Equal("ProjectB", result3);
        }
        
        [Fact]
        public async Task GetProjectSuggestionAsync_ParsesSuggestion_MatchingProjectName()
        {
            // Arrange: response with text that contains a project name but isn't exact
            var json = @"{ ""choices"": [ { ""text"": ""I think this belongs to ProjectX category"" } ] }";
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                               .SetValue(null, new HttpClient(fakeHandler));
            
            // Act: call with project names array
            string? result = await OpenAIClient.GetProjectSuggestionAsync(
                "WindowTitle", 
                "AppName", 
                new string[] { "ProjectX", "ProjectY", "ProjectZ" }, 
                "fake-api-key");
            
            // Assert: should extract the matching project name
            Assert.Equal("ProjectX", result);
        }
        
        [Fact]
        public async Task TestConnectionAsync_ReturnsTrue_OnSuccess()
        {
            // Arrange: fake a successful API response
            var json = @"{ ""choices"": [ { ""text"": ""Hello"" } ] }";
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                               .SetValue(null, new HttpClient(fakeHandler));
            
            // Act
            bool result = await OpenAIClient.TestConnectionAsync("fake-api-key");
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task TestConnectionAsync_ReturnsFalse_OnFailure()
        {
            // Arrange: fake an error response
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}") };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            typeof(OpenAIClient).GetField("httpClient", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                               .SetValue(null, new HttpClient(fakeHandler));
            
            // Act
            bool result = await OpenAIClient.TestConnectionAsync("fake-api-key");
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetProjectSuggestionAsync_WithValidInput_ReturnsSuggestion()
        {
            // This test requires a valid API key and internet connection
            // For CI/CD, we should mock the HTTP response instead
            
            // Skip this test if no API key is available
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            if (string.IsNullOrEmpty(apiKey))
            {
                // Skip test if no API key
                return;
            }
            
            // Arrange
            string windowTitle = "Microsoft Visual Studio - DueTime.sln";
            string applicationName = "devenv";
            string[] projectNames = new[] { "Development", "Meetings", "Research", "Documentation" };
            
            // Act
            string? suggestion = await OpenAIClient.GetProjectSuggestionAsync(
                windowTitle, 
                applicationName, 
                projectNames, 
                apiKey);
            
            // Assert
            Assert.NotNull(suggestion);
            // The suggestion should be one of our project names
            Assert.Contains(suggestion, projectNames);
        }
        
        [Fact]
        public async Task GetProjectSuggestionAsync_WithCachedResult_ReturnsCachedValue()
        {
            // Skip this test if no API key is available
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            if (string.IsNullOrEmpty(apiKey))
            {
                // Skip test if no API key
                return;
            }
            
            // Arrange
            string windowTitle = "Unique Window Title For Cache Test";
            string applicationName = "TestApp";
            string[] projectNames = new[] { "Development", "Meetings", "Research", "Documentation" };
            
            // Clear the cache first
            OpenAIClient.ClearCache();
            
            // Act - First call should hit the API
            string? firstSuggestion = await OpenAIClient.GetProjectSuggestionAsync(
                windowTitle, 
                applicationName, 
                projectNames, 
                apiKey);
                
            // Second call with same parameters should use cache
            string? secondSuggestion = await OpenAIClient.GetProjectSuggestionAsync(
                windowTitle, 
                applicationName, 
                projectNames, 
                apiKey);
            
            // Assert
            Assert.NotNull(firstSuggestion);
            Assert.NotNull(secondSuggestion);
            Assert.Equal(firstSuggestion, secondSuggestion);
        }
        
        [Fact]
        public async Task GetProjectSuggestionAsync_WithInvalidApiKey_ReturnsNull()
        {
            // Arrange
            string windowTitle = "Test Window";
            string applicationName = "TestApp";
            string[] projectNames = new[] { "Development", "Meetings" };
            string invalidApiKey = "invalid-key";
            
            // Act
            string? suggestion = await OpenAIClient.GetProjectSuggestionAsync(
                windowTitle, 
                applicationName, 
                projectNames, 
                invalidApiKey);
            
            // Assert
            Assert.Null(suggestion);
        }
    }
} 