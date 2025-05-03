using System;

namespace DueTime.Tracking
{
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
    
    public class WindowChangedEventArgs : EventArgs
    {
        public string WindowTitle { get; }
        public string ApplicationName { get; }
        public WindowChangedEventArgs(string title, string app) => (WindowTitle, ApplicationName) = (title, app);
    }
    
    public class IdleStateChangedEventArgs : EventArgs
    {
        public bool IsIdle { get; }
        public IdleStateChangedEventArgs(bool isIdle) => IsIdle = isIdle;
    }
} 