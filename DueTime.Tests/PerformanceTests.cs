using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DueTime.Data;
using Xunit;
using Xunit.Abstractions;

namespace DueTime.Tests
{
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Database_CanHandleLargeNumberOfEntries()
        {
            // Arrange - Create a test database
            string originalDbPath = Database.DbPath;
            
            try
            {
                // Set the database path to a test path
                var testDbPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), 
                    $"DueTimeTest_{Guid.NewGuid()}.db");
                
                // Set the DbPath property directly since it's public
                typeof(Database)
                    .GetProperty("DbPath")
                    ?.SetValue(null, testDbPath);
                
                // Initialize the schema
                Database.InitializeSchema();
                
                // Create repositories
                var timeEntryRepo = new SQLiteTimeEntryRepository();
                var projectRepo = new SQLiteProjectRepository();
                
                // Add a test project
                int projectId = await projectRepo.AddProjectAsync("Test Project");
                
                // Create 1000 sample time entries spanning several days
                var entries = new List<TimeEntry>();
                var baseTime = DateTime.Now.AddDays(-30);
                
                for (int i = 0; i < 1000; i++)
                {
                    var entry = new TimeEntry
                    {
                        StartTime = baseTime.AddMinutes(i * 15),
                        EndTime = baseTime.AddMinutes((i * 15) + 10),
                        WindowTitle = $"Test Window {i}",
                        ApplicationName = $"Test App {i % 10}",
                        ProjectId = i % 3 == 0 ? projectId : null
                    };
                    entries.Add(entry);
                }
                
                // Act - Insert all entries and measure time
                var stopwatch = Stopwatch.StartNew();
                
                foreach (var entry in entries)
                {
                    await timeEntryRepo.AddTimeEntryAsync(entry);
                }
                
                stopwatch.Stop();
                _output.WriteLine($"Time to insert 1000 entries: {stopwatch.ElapsedMilliseconds}ms");
                
                // Ensure insertion time is reasonable (should be much less than 10 seconds)
                Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
                    $"Time to insert 1000 entries: {stopwatch.ElapsedMilliseconds}ms - should be less than 10000ms");
                
                // Act - Measure query time for a day's entries
                stopwatch.Restart();
                
                var queryDate = baseTime.AddDays(15).Date;
                var result = await timeEntryRepo.GetEntriesByDateAsync(queryDate);
                
                stopwatch.Stop();
                _output.WriteLine($"Time to query entries for a day: {stopwatch.ElapsedMilliseconds}ms");
                _output.WriteLine($"Number of entries found: {result.Count}");
                
                // Ensure query time is reasonable (should be much less than 1 second)
                Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                    $"Time to query entries for a day: {stopwatch.ElapsedMilliseconds}ms - should be less than 1000ms");
                
                // Act - Measure query time for a range of entries
                stopwatch.Restart();
                
                var rangeStart = baseTime.AddDays(10);
                var rangeEnd = baseTime.AddDays(20);
                var rangeResult = await timeEntryRepo.GetEntriesInRangeAsync(rangeStart, rangeEnd);
                
                stopwatch.Stop();
                _output.WriteLine($"Time to query entries for a 10-day range: {stopwatch.ElapsedMilliseconds}ms");
                _output.WriteLine($"Number of entries found: {rangeResult.Count}");
                
                // Ensure query time is reasonable (should be much less than 1 second)
                Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                    $"Time to query entries for a 10-day range: {stopwatch.ElapsedMilliseconds}ms - should be less than 1000ms");
                
                // Validate memory consumption
                GC.Collect();
                var currentProcess = Process.GetCurrentProcess();
                var memoryMB = currentProcess.WorkingSet64 / (1024 * 1024);
                _output.WriteLine($"Memory usage: {memoryMB}MB");
                
                // Memory should be reasonable (less than 100MB for this test)
                Assert.True(memoryMB < 100, $"Memory usage: {memoryMB}MB - should be less than 100MB");
            }
            finally
            {
                // Restore the original database path
                typeof(Database)
                    .GetProperty("DbPath")
                    ?.SetValue(null, originalDbPath);
                
                // Delete the test database file if it exists
                try
                {
                    if (System.IO.File.Exists(Database.DbPath))
                        System.IO.File.Delete(Database.DbPath);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
} 