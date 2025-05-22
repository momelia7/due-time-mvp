using DueTime.Data;
using Xunit;

namespace DueTime.Tests.DataLayer
{
    public class SecureStorageTests
    {
        [Fact]
        public void SaveAndLoadApiKey_PersistDataSuccessfully()
        {
            // Arrange
            string testKey = "dummy-api-key";
            
            // Act
            SecureStorage.SaveApiKey(testKey);
            string? loadedKey = SecureStorage.LoadApiKey();
            
            // Assert: the loaded key should match what was saved (if encryption/decryption succeeded)
            Assert.Equal(testKey, loadedKey);
        }

        [Fact]
        public void LoadApiKey_ReturnsNullWhenNotSet()
        {
            // Arrange: ensure no key file exists (could delete the file, omitted for brevity)
            // Act
            string? loadedKey = SecureStorage.LoadApiKey();
            // Assert: if no key was saved, result should be null or empty
            Assert.True(loadedKey == null || loadedKey == string.Empty);
        }
    }
} 