using System;
using DueTime.TrackingEngine.Models;
namespace DueTime.TrackingEngine.Services
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