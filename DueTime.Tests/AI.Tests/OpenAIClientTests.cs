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
    }
} 