using System;
using System.Threading;
using DueTime.Data;

namespace DueTime.Tracking
{
    /// <summary>Core service that records time entries based on active window and idle state.</summary>
    public class TrackingService : ITrackingService
    {
        private readonly ISystemEvents _systemEvents;
        private readonly ITimeEntryRepository _repository;
        private TimeEntry? _currentEntry;
        private bool _isIdle;
        private Timer? _idleTimer;
        
        public event EventHandler<TimeEntryRecordedEventArgs>? TimeEntryRecorded;

        public TrackingService(ISystemEvents systemEvents, ITimeEntryRepository repository)
        {
            _systemEvents = systemEvents;
            _repository = repository;
            _isIdle = false;
        }

        public void Start()
        {
            _systemEvents.WindowChanged += OnWindowChanged;
            _systemEvents.IdleStateChanged += OnIdleStateChanged;
            _systemEvents.Start();
            
            // Create an idle check timer (every minute)
            _idleTimer = new Timer(CheckIdleTimeout, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public void Stop()
        {
            _systemEvents.WindowChanged -= OnWindowChanged;
            _systemEvents.IdleStateChanged -= OnIdleStateChanged;
            _systemEvents.Stop();
            
            _idleTimer?.Dispose();
            _idleTimer = null;
            
            // Close any open time entry
            if (_currentEntry != null)
            {
                _currentEntry.EndTime = DateTime.Now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                OnTimeEntryRecorded(_currentEntry);
                _currentEntry = null;
            }
        }

        private void OnWindowChanged(object? sender, WindowChangedEventArgs e)
        {
            if (_isIdle) return; // Don't create entries while idle
            
            var now = DateTime.Now;
            
            // If we have an active entry, complete it
            if (_currentEntry != null)
            {
                _currentEntry.EndTime = now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                OnTimeEntryRecorded(_currentEntry);
            }
            
            // Start a new entry
            _currentEntry = new TimeEntry
            {
                StartTime = now,
                EndTime = now, // Will be updated when window changes again
                WindowTitle = e.WindowTitle,
                ApplicationName = e.ApplicationName
            };
        }

        private void OnIdleStateChanged(object? sender, IdleStateChangedEventArgs e)
        {
            _isIdle = e.IsIdle;
            
            if (_isIdle && _currentEntry != null)
            {
                // Complete the current entry when going idle
                _currentEntry.EndTime = DateTime.Now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                OnTimeEntryRecorded(_currentEntry);
                _currentEntry = null;
            }
        }
        
        private void CheckIdleTimeout(object? state)
        {
            // This is a safety check in case the idle events aren't detected
            if (_currentEntry != null && (DateTime.Now - _currentEntry.StartTime).TotalMinutes > 30)
            {
                // If an entry has been active for too long, close it
                _currentEntry.EndTime = DateTime.Now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                OnTimeEntryRecorded(_currentEntry);
                _currentEntry = null;
            }
        }
        
        private void OnTimeEntryRecorded(TimeEntry entry)
        {
            TimeEntryRecorded?.Invoke(this, new TimeEntryRecordedEventArgs(entry));
        }
    }
} 