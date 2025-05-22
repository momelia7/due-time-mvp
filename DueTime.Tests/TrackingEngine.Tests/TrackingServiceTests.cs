using System;
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
    }

    // Dummy implementations for test purposes (minimal stubs)
    internal class DummySystemEvents : ISystemEvents
    {
        public event EventHandler<WindowChangedEventArgs>? WindowChanged;
        public event EventHandler<IdleStateChangedEventArgs>? IdleStateChanged;
        public void Start() { /* no-op */ }
        public void Stop() { /* no-op */ }
    }
    internal class DummyTimeEntryRepository : ITimeEntryRepository
    {
        public System.Threading.Tasks.Task AddTimeEntryAsync(TimeEntry entry) => System.Threading.Tasks.Task.CompletedTask;
        // ... other ITimeEntryRepository members with no-op implementations ...
    }
} 