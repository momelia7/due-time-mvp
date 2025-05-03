using System;
using DueTime.TrackingEngine.Models;
namespace DueTime.TrackingEngine.Services
{
    /// <summary>Core tracking engine that listens to system events and logs time entries.</summary>
    public class TrackingService : ITrackingService
    {
        public event EventHandler<TimeEntryRecordedEventArgs>? TimeEntryRecorded;
        private readonly ISystemEvents _systemEvents;
        private readonly ITimeEntryRepository _repository;
        private TimeEntry? _currentEntry;
        private bool _isIdle;
        public TrackingService(ISystemEvents systemEvents, ITimeEntryRepository repository)
        {
            _systemEvents = systemEvents;
            _repository = repository;
            _systemEvents.ForegroundChanged += OnForegroundChanged;
            _systemEvents.IdleStarted += OnIdleStarted;
            _systemEvents.IdleEnded += OnIdleEnded;
        }
        public void Start()
        {
            _currentEntry = null;
            _isIdle = false;
            _systemEvents.Start();
        }
        public void Stop()
        {
            // End ongoing entry upon stopping tracking
            if (_currentEntry != null && !_isIdle)
            {
                _currentEntry.EndTime = DateTime.Now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                TimeEntryRecorded?.Invoke(this, new TimeEntryRecordedEventArgs(_currentEntry));
                _currentEntry = null;
            }
            _systemEvents.Stop();
        }
        private void OnForegroundChanged(object? sender, ForegroundChangedEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (_isIdle)
            {
                // If idle, ignore foreground changes (resumption will be handled in OnIdleEnded).
                return;
            }
            // End previous entry
            if (_currentEntry != null)
            {
                _currentEntry.EndTime = now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                TimeEntryRecorded?.Invoke(this, new TimeEntryRecordedEventArgs(_currentEntry));
            }
            // Start a new entry for the new foreground window
            _currentEntry = new TimeEntry
            {
                StartTime = now,
                EndTime = now,
                WindowTitle = e.WindowTitle,
                ApplicationName = e.ApplicationName,
                ProjectId = null
            };
        }
        private void OnIdleStarted(object? sender, EventArgs e)
        {
            if (_isIdle) return;
            _isIdle = true;
            if (_currentEntry != null)
            {
                // End the active entry at idle start time
                DateTime now = DateTime.Now;
                _currentEntry.EndTime = now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                TimeEntryRecorded?.Invoke(this, new TimeEntryRecordedEventArgs(_currentEntry));
                _currentEntry = null;
            }
        }
        private void OnIdleEnded(object? sender, EventArgs e)
        {
            if (!_isIdle) return;
            _isIdle = false;
            // Idle ended: we will start a new entry on the next ForegroundChanged event (or if same window continues, OnForegroundChanged will fire with that window again).
        }
    }
} 