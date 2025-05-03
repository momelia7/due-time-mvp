using System;
using DueTime.Data;

namespace DueTime.Tracking
{
    /// <summary>Interface for the background tracking service.</summary>
    public interface ITrackingService
    {
        event EventHandler<TimeEntryRecordedEventArgs> TimeEntryRecorded;
        void Start();
        void Stop();
    }

    public class TimeEntryRecordedEventArgs : EventArgs
    {
        public TimeEntry Entry { get; }
        public TimeEntryRecordedEventArgs(TimeEntry entry) => Entry = entry;
    }
} 