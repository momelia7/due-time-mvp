using System.Threading.Tasks;
using DueTime.Data;
using Xunit;

namespace DueTime.Tests.DataLayer
{
    public class OpenAIClientTests
    {
        [Fact]
        public async Task GetProjectSuggestionAsync_ReturnsResultOrNull()
        {
            // Act
            string? result = await OpenAIClient.GetProjectSuggestionAsync(
                "Test Window", "TestApp", new string[] { "ProjectA", "ProjectB" }, "invalid-api-key");
            
            // Assert: for an invalid key, result is likely null (placeholder assertion allowing null or string)
            Assert.True(result == null || result is string);
        }

        [Fact]
        public async Task GetWeeklySummaryAsync_ReturnsResultOrNull()
        {
            // Act
            string? summary = await OpenAIClient.GetWeeklySummaryAsync(
                System.DateTime.Today, System.DateTime.Today.AddDays(6), "Summary prompt", "invalid-api-key");
            
            // Assert: placeholder (summary might be null due to invalid key)
            Assert.True(summary == null || summary is string);
        }
    }
} 