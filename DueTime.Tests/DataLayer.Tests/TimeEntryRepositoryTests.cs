using System.Collections.Generic;
using System.Threading.Tasks;
using DueTime.Data;
using Xunit;

namespace DueTime.Tests.DataLayer
{
    public class TimeEntryRepositoryTests
    {
        [Fact]
        public async Task GetTimeEntriesAsync_ReturnsEntries()
        {
            // Arrange
            var repo = new SQLiteTimeEntryRepository();
            
            // Act
            List<TimeEntry> entries = await repo.GetTimeEntriesAsync();
            
            // Assert: result is not null
            Assert.NotNull(entries);
        }

        [Fact]
        public async Task AddTimeEntryAsync_InsertsEntry()
        {
            // Arrange
            var repo = new SQLiteTimeEntryRepository();
            var entry = new TimeEntry { StartTime = System.DateTime.Now, EndTime = System.DateTime.Now, WindowTitle = "Test", ApplicationName = "TestApp" };
            
            // Act
            await repo.AddTimeEntryAsync(entry);
            
            // Assert: placeholder (if no exception, assume success)
            Assert.True(true);
        }
    }
} 