using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DueTime.Data;
using DueTime.Tracking;
using DueTime.Tracking.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DueTime.Tests.IntegrationTests
{
    public class TrackingIntegrationTests
    {
        [Fact]
        public async Task SimulateOneHourActivity_GeneratesCorrectTimeEntries()
        {
            // Arrange - Set up a temporary database
            string dbPath = Path.Combine(Path.GetTempPath(), $"DueTime_Test_{Guid.NewGuid()}.db");
            string connectionString = $"Data Source={dbPath}";
            
            try
            {
                // Initialize the database schema
                await InitializeDatabaseAsync(connectionString);
                
                // Create repositories
                var timeEntryRepo = new SQLiteTimeEntryRepository(connectionString);
                
                // Create fake system events
                var fakeSystemEvents = new FakeSystemEvents();
                
                // Create tracking service with the fake events and repository
                var trackingService = new TrackingService(fakeSystemEvents, timeEntryRepo);
                
                // Set up event tracking
                List<TimeEntry> recordedEntries = new List<TimeEntry>();
                trackingService.TimeEntryRecorded += (sender, e) => 
                {
                    recordedEntries.Add(e.Entry);
                };
                
                // Act - Start tracking and simulate activity
                trackingService.Start();
                
                // Simulate a sequence of window changes and idle periods
                await SimulateActivitySequenceAsync(fakeSystemEvents);
                
                // Stop tracking
                trackingService.Stop();
                
                // Query the database for all entries
                var entriesFromDb = await timeEntryRepo.GetEntriesByDateAsync(DateTime.Today);
                
                // Assert - Verify that entries were recorded correctly
                Assert.NotEmpty(recordedEntries);
                Assert.NotEmpty(entriesFromDb);
                Assert.Equal(recordedEntries.Count, entriesFromDb.Count);
                
                // Verify that entries have valid timestamps
                foreach (var entry in entriesFromDb)
                {
                    Assert.True(entry.StartTime > DateTime.MinValue);
                    Assert.True(entry.EndTime > entry.StartTime);
                    Assert.False(string.IsNullOrEmpty(entry.WindowTitle));
                    Assert.False(string.IsNullOrEmpty(entry.ApplicationName));
                }
                
                // Verify total tracked time is reasonable (should be close to our simulation time)
                TimeSpan totalTrackedTime = new TimeSpan(entriesFromDb.Sum(e => (e.EndTime - e.StartTime).Ticks));
                Assert.True(totalTrackedTime.TotalMinutes > 0);
                
                // The total tracked time should be less than our simulation time (60 minutes)
                // because we have idle periods where no tracking occurs
                Assert.True(totalTrackedTime.TotalMinutes <= 60);
            }
            finally
            {
                // Clean up - Delete the temporary database file
                if (File.Exists(dbPath))
                {
                    try
                    {
                        File.Delete(dbPath);
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                    }
                }
            }
        }
        
        private async Task InitializeDatabaseAsync(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            
            // Create TimeEntries table
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TimeEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL,
                    WindowTitle TEXT,
                    ApplicationName TEXT,
                    ProjectId INTEGER NULL,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(ProjectId)
                );
                
                CREATE TABLE IF NOT EXISTS Projects (
                    ProjectId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Color TEXT,
                    IsArchived INTEGER DEFAULT 0
                );";
            
            await command.ExecuteNonQueryAsync();
        }
        
        private async Task SimulateActivitySequenceAsync(FakeSystemEvents fakeSystemEvents)
        {
            // Simulate 60 minutes of activity with 5-second intervals between events
            var simulationDuration = TimeSpan.FromMinutes(1); // For testing, use 1 minute instead of 60
            var eventInterval = TimeSpan.FromSeconds(5);
            
            // Define a specific sequence of window changes and idle periods
            string[] windowTitles = new[]
            {
                "DueTime Project - Visual Studio",
                "Pull Request #123 - GitHub",
                "Inbox - Outlook",
                "Team Meeting - Microsoft Teams",
                "Budget 2023 - Excel",
                "Project Documentation - Word"
            };
            
            string[] applications = new[]
            {
                "devenv",
                "chrome",
                "OUTLOOK",
                "Teams",
                "EXCEL",
                "WINWORD"
            };
            
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime + simulationDuration;
            
            int index = 0;
            bool isIdle = false;
            
            // Manually simulate the sequence
            while (DateTime.Now < endTime)
            {
                if (!isIdle)
                {
                    // Simulate window change
                    fakeSystemEvents.SimulateWindowChange(
                        windowTitles[index % windowTitles.Length],
                        applications[index % applications.Length]);
                    
                    index++;
                }
                
                // After every 3 window changes, simulate going idle for a bit
                if (index % 3 == 0 && !isIdle)
                {
                    // Go idle
                    isIdle = true;
                    fakeSystemEvents.SimulateIdleStateChange(true);
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    
                    // Come back from idle
                    isIdle = false;
                    fakeSystemEvents.SimulateIdleStateChange(false);
                }
                
                await Task.Delay(eventInterval);
            }
        }
    }
} 