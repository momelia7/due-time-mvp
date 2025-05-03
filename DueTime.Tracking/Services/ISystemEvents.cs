using System;
namespace DueTime.TrackingEngine.Services
{
    /// <summary>Abstracts system events for foreground window changes and idle state.</summary>
    public interface ISystemEvents
    {
        event EventHandler<ForegroundChangedEventArgs> ForegroundChanged;
        event EventHandler IdleStarted;
        event EventHandler IdleEnded;
        void Start();
        void Stop();
    }
    public class ForegroundChangedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; }
        public string WindowTitle { get; }
        public string ApplicationName { get; }
        public ForegroundChangedEventArgs(IntPtr hwnd, string title, string appName)
        {
            WindowHandle = hwnd;
            WindowTitle = title;
            ApplicationName = appName;
        }
    }
} 