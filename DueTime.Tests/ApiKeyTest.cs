using System;
using DueTime.Data;
using Xunit;

namespace DueTime.Tests
{
    public class ApiKeyTest
    {
        [Fact]
        public void ApiKeyManager_ShouldReturnValidKey()
        {
            // Act
            var apiKey = ApiKeyManager.GetApiKey();
            
            // Assert
            Assert.NotNull(apiKey);
            Assert.NotEmpty(apiKey);
            Assert.StartsWith("sk-", apiKey);
            Assert.Contains("qqpaz9fuTwVzzQ4f_73KvleG1fJ-3abWv-Zt3CjnOQege7wEh8e9SkitgtHA9Gct5FFy6NI6NCT3BlbkFJUNXjIdccBcoU9PZBu5yX7VnrqYTII9zx8quD3nZhsjrR9tTe58qaHYt8LwwPaCmPPLzWr93NcA", apiKey);
        }
        
        [Fact]
        public void ApiKeyManager_HasApiKey_ShouldReturnTrue()
        {
            // Arrange - ensure API key is initialized
            ApiKeyManager.GetApiKey();
            
            // Act
            var hasKey = ApiKeyManager.HasApiKey();
            
            // Assert
            Assert.True(hasKey);
        }
    }
} 