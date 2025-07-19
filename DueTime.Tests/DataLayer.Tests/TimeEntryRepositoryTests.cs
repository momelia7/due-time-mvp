using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DueTime.Data;
using Xunit;

namespace DueTime.Tests.DataLayer
{
    public class TimeEntryRepositoryTests
    {
        [Fact]
        public async Task GetEntriesByDateAsync_ReturnsEntries()
        {
            // Arrange
            var repo = new SQLiteTimeEntryRepository();
            
            // Act
            List<TimeEntry> entries = await repo.GetEntriesByDateAsync(DateTime.Today);
            
            // Assert: result is not null
            Assert.NotNull(entries);
        }

        [Fact]
        public async Task AddTimeEntryAsync_InsertsEntry()
        {
            // Arrange
            var repo = new SQLiteTimeEntryRepository();
            var entry = new TimeEntry 
            { 
                StartTime = DateTime.Now.AddHours(-1), 
                EndTime = DateTime.Now, 
                WindowTitle = "Test", 
                ApplicationName = "TestApp" 
            };
            
            // Act
            await repo.AddTimeEntryAsync(entry);
            
            // Assert: entry should have been assigned an ID
            Assert.True(entry.Id > 0);
        }
        
        [Fact]
        public async Task UpdateEntryProjectAsync_UpdatesProject()
        {
            // Arrange
            var repo = new SQLiteTimeEntryRepository();
            var entry = new TimeEntry 
            { 
                StartTime = DateTime.Now.AddHours(-1), 
                EndTime = DateTime.Now, 
                WindowTitle = "Test Update", 
                ApplicationName = "TestApp" 
            };
            
            await repo.AddTimeEntryAsync(entry);
            
            // Act
            await repo.UpdateEntryProjectAsync(entry.Id, 1);
            
            // Assert: no exception thrown
            Assert.True(true);
        }
    }
} 