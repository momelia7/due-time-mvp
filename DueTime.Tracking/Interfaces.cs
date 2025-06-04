using System;
using DueTime.Data;

namespace DueTime.Tracking
{
    #region Event Arguments
    
    /// <summary>
    /// Event arguments for when a time entry is recorded
    /// </summary>
    public class TimeEntryRecordedEventArgs : EventArgs
    {
        /// <summary>
        /// The time entry that was recorded
        /// </summary>
        public TimeEntry Entry { get; }
        
        public TimeEntryRecordedEventArgs(TimeEntry entry)
        {
            Entry = entry;
        }
    }
    
    /// <summary>
    /// Event arguments for when tracking status changes
    /// </summary>
    public class TrackingStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether tracking is now active
        /// </summary>
        public bool IsTracking { get; }
        
        /// <summary>
        /// The reason for the status change
        /// </summary>
        public string Reason { get; }
        
        public TrackingStatusChangedEventArgs(bool isTracking, string reason)
        {
            IsTracking = isTracking;
            Reason = reason;
        }
    }
    
    /// <summary>
    /// Event arguments for when the active window changes
    /// </summary>
    public class WindowChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The title of the new active window
        /// </summary>
        public string WindowTitle { get; }
        
        /// <summary>
        /// The name of the application that owns the window
        /// </summary>
        public string ApplicationName { get; }
        
        public WindowChangedEventArgs(string title, string app)
        {
            WindowTitle = title;
            ApplicationName = app;
        }
    }
    
    /// <summary>
    /// Event arguments for when the system idle state changes
    /// </summary>
    public class IdleStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the system is now idle
        /// </summary>
        public bool IsIdle { get; }
        
        public IdleStateChangedEventArgs(bool isIdle)
        {
            IsIdle = isIdle;
        }
    }
    
    #endregion
    
    #region Interfaces
    
    /// <summary>Interface for system-level events like window switching and idle detection.</summary>
    public interface ISystemEvents
    {
        /// <summary>Raised when the active foreground window changes.</summary>
        event EventHandler<WindowChangedEventArgs> WindowChanged;
        
        /// <summary>Raised when the system transitions to or from an idle state.</summary>
        event EventHandler<IdleStateChangedEventArgs> IdleStateChanged;
        
        void Start();
        void Stop();
    }
    
    /// <summary>
    /// Interface for tracking services that monitor user activity and record time entries
    /// </summary>
    public interface ITrackingService
    {
        /// <summary>
        /// Event raised when a new time entry is recorded
        /// </summary>
        event EventHandler<TimeEntryRecordedEventArgs> TimeEntryRecorded;
        
        /// <summary>
        /// Event raised when the tracking service status changes
        /// </summary>
        event EventHandler<TrackingStatusChangedEventArgs> StatusChanged;
        
        /// <summary>
        /// Gets whether the tracking service is currently active
        /// </summary>
        bool IsTracking { get; }
        
        /// <summary>
        /// Starts tracking user activity
        /// </summary>
        void Start();
        
        /// <summary>
        /// Stops tracking user activity
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Gets the current time entry being tracked, if any
        /// </summary>
        TimeEntry? CurrentEntry { get; }
        
        /// <summary>
        /// Configures AI-powered project categorization
        /// </summary>
        /// <param name="useAI">Whether to use AI for project categorization</param>
        /// <param name="apiKey">The OpenAI API key</param>
        /// <param name="projectRepository">Repository for accessing projects</param>
        void ConfigureAI(bool useAI, string? apiKey, IProjectRepository projectRepository);
    }
    
    #endregion
} 