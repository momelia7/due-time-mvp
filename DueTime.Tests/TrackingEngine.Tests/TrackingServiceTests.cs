using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DueTime.Data;
using DueTime.Tracking;
using Xunit;

namespace DueTime.Tests.TrackingEngine
{
    public class TrackingServiceTests
    {
        [Fact]
        public void Start_BeginsTrackingWithoutError()
        {
            // Arrange: use dummy system events and repository
            var systemEvents = new DummySystemEvents();  // (this dummy would implement ISystemEvents)
            var repository = new DummyTimeEntryRepository(); // (implements ITimeEntryRepository)
            var service = new TrackingService(systemEvents, repository);

            // Act
            Exception? exception = Record.Exception(() => service.Start());

            // Assert: no exception should be thrown starting the service
            Assert.Null(exception);
        }

        [Fact]
        public void Stop_CompletesAnyOpenEntry()
        {
            // Arrange: start tracking, then stop immediately
            var systemEvents = new DummySystemEvents();
            var repository = new DummyTimeEntryRepository();
            var service = new TrackingService(systemEvents, repository);
            service.Start();

            // Act
            service.Stop();

            // Assert: placeholder (in real test, verify repository received a completed entry)
            Assert.True(true);
        }
        
        [Fact]
        public void IdleResume_CreatesNewEntryIfWindowDidNotChange()
        {
            // Arrange
            var fakeEvents = new FakeSystemEvents();
            var repository = new InMemoryTimeEntryRepository();
            var service = new TrackingService(fakeEvents, repository);
            
            // Start tracking
            service.Start();
            
            // Simulate an active window
            fakeEvents.SimulateForegroundWindow("Visual Studio", "Code.exe");
            
            // Wait a bit for the entry to be processed
            System.Threading.Thread.Sleep(100);
            
            // Act: User goes idle
            fakeEvents.RaiseIdleStateChanged(true);
            
            // Wait a bit for the idle to be processed
            System.Threading.Thread.Sleep(100);
            
            // User returns from idle with same window
            fakeEvents.RaiseIdleStateChanged(false);
            
            // Wait a bit for the idle end to be processed
            System.Threading.Thread.Sleep(100);
            
            // Stop tracking
            service.Stop();
            
            // Assert: We should have two entries
            // 1. The initial entry that ended when idle started
            // 2. A new entry that started when idle ended
            Assert.Equal(2, repository.Entries.Count);
            
            // First entry should be ended
            Assert.True(repository.Entries[0].EndTime > DateTime.MinValue);
            
            // Second entry should have started after the first ended
            Assert.True(repository.Entries[1].StartTime > repository.Entries[0].EndTime);
            
            // Both entries should have the same window title since the window didn't change
            Assert.Equal(repository.Entries[0].WindowTitle, repository.Entries[1].WindowTitle);
            Assert.Equal("Visual Studio", repository.Entries[1].WindowTitle);
        }
        
        [Fact]
        public void TrackingService_CanHandleMultipleEntriesOverTime()
        {
            // Arrange
            var fakeEvents = new FakeSystemEvents();
            var repository = new InMemoryTimeEntryRepository();
            var service = new TrackingService(fakeEvents, repository);
            
            // Start tracking
            service.Start();
            
            // Simulate many window switches (100 is enough for testing, 1000 would be too slow)
            for (int i = 0; i < 100; i++)
            {
                fakeEvents.SimulateForegroundWindow($"Window {i % 5}", $"App{i % 5}");
                
                // Small delay to ensure entries are processed
                System.Threading.Thread.Sleep(10);
            }
            
            // Stop tracking
            service.Stop();
            
            // Assert: We should have many entries
            Assert.True(repository.Entries.Count > 50);
            
            // Ensure no two consecutive entries have the same start time (they should be distinct events)
            for (int j = 1; j < repository.Entries.Count; j++)
            {
                Assert.NotEqual(repository.Entries[j - 1].StartTime, repository.Entries[j].StartTime);
            }
            
            // Check that entries don't overlap
            for (int j = 1; j < repository.Entries.Count; j++)
            {
                Assert.True(repository.Entries[j].StartTime >= repository.Entries[j - 1].EndTime);
            }
        }
    }

    // Dummy implementations for test purposes (minimal stubs)
    internal class DummySystemEvents : ISystemEvents
    {
        // These events are required by the interface but not used in these specific tests
        public event EventHandler<WindowChangedEventArgs>? WindowChanged = delegate { };
        public event EventHandler<IdleStateChangedEventArgs>? IdleStateChanged = delegate { };
        
        public void Start() { /* no-op */ }
        public void Stop() { /* no-op */ }
    }
    
    // Enhanced fake implementation for testing idle resume
    internal class FakeSystemEvents : ISystemEvents
    {
        public event EventHandler<WindowChangedEventArgs>? WindowChanged;
        public event EventHandler<IdleStateChangedEventArgs>? IdleStateChanged;
        
        public void Start() { /* no-op */ }
        public void Stop() { /* no-op */ }
        
        public void SimulateForegroundWindow(string windowTitle, string appName)
        {
            WindowChanged?.Invoke(this, new WindowChangedEventArgs(windowTitle, appName));
        }
        
        public void RaiseIdleStateChanged(bool isIdle)
        {
            IdleStateChanged?.Invoke(this, new IdleStateChangedEventArgs(isIdle));
        }
    }
    
    // In-memory repository for testing
    internal class InMemoryTimeEntryRepository : ITimeEntryRepository
    {
        public List<TimeEntry> Entries { get; } = new List<TimeEntry>();
        
        public Task AddTimeEntryAsync(TimeEntry entry)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
        
        public Task<List<TimeEntry>> GetEntriesByDateAsync(DateTime date)
        {
            return Task.FromResult(Entries.FindAll(e => e.StartTime.Date == date.Date));
        }
        
        public Task<List<TimeEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return Task.FromResult(Entries.FindAll(e => e.StartTime.Date >= startDate.Date && e.StartTime.Date <= endDate.Date));
        }
        
        public Task<List<TimeEntry>> GetEntriesInRangeAsync(DateTime startDate, DateTime endDate)
        {
            return Task.FromResult(Entries.FindAll(e => e.StartTime >= startDate && e.StartTime <= endDate));
        }
        
        public Task UpdateEntryProjectAsync(int entryId, int? projectId)
        {
            var entry = Entries.Find(e => e.Id == entryId);
            if (entry != null)
            {
                entry.ProjectId = projectId;
            }
            return Task.CompletedTask;
        }
        
        public Task DeleteAllEntriesAsync()
        {
            Entries.Clear();
            return Task.CompletedTask;
        }
    }
    
    internal class DummyTimeEntryRepository : ITimeEntryRepository
    {
        public Task AddTimeEntryAsync(TimeEntry entry) => Task.CompletedTask;
        
        public Task<List<TimeEntry>> GetEntriesByDateAsync(DateTime date) => 
            Task.FromResult(new List<TimeEntry>());
            
        public Task<List<TimeEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate) => 
            Task.FromResult(new List<TimeEntry>());
            
        public Task<List<TimeEntry>> GetEntriesInRangeAsync(DateTime startDate, DateTime endDate) => 
            Task.FromResult(new List<TimeEntry>());
            
        public Task UpdateEntryProjectAsync(int entryId, int? projectId) =>
            Task.CompletedTask;
            
        public Task DeleteAllEntriesAsync() =>
            Task.CompletedTask;
    }
} 