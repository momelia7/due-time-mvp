using System;
using System.Diagnostics;
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
        private string _lastWindowTitle = string.Empty;
        private string _lastAppName = string.Empty;
        
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
            
            Debug.WriteLine("Tracking service started");
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
                
                Debug.WriteLine("Final time entry saved on tracking stop");
            }
            
            Debug.WriteLine("Tracking service stopped");
        }

        private void OnWindowChanged(object? sender, WindowChangedEventArgs e)
        {
            if (_isIdle) return; // Don't create entries while idle
            
            var now = DateTime.Now;
            
            // Save the current window info for potential use when idle ends
            _lastWindowTitle = e.WindowTitle;
            _lastAppName = e.ApplicationName;
            
            // If we have an active entry, complete it
            if (_currentEntry != null)
            {
                _currentEntry.EndTime = now;
                _repository.AddTimeEntryAsync(_currentEntry).Wait();
                OnTimeEntryRecorded(_currentEntry);
                
                Debug.WriteLine($"Time entry ended due to window change: {_currentEntry.WindowTitle} ({_currentEntry.ApplicationName})");
            }
            
            // Start a new entry
            _currentEntry = new TimeEntry
            {
                StartTime = now,
                EndTime = now, // Will be updated when window changes again
                WindowTitle = e.WindowTitle,
                ApplicationName = e.ApplicationName
            };
            
            Debug.WriteLine($"New time entry started: {e.WindowTitle} ({e.ApplicationName})");
        }

        private void OnIdleStateChanged(object? sender, IdleStateChangedEventArgs e)
        {
            var now = DateTime.Now;
            
            if (e.IsIdle)
            {
                // Going idle
                _isIdle = true;
                
                if (_currentEntry != null)
                {
                    // Complete the current entry when going idle
                    _currentEntry.EndTime = now;
                    _repository.AddTimeEntryAsync(_currentEntry).Wait();
                    OnTimeEntryRecorded(_currentEntry);
                    _currentEntry = null;
                    
                    Debug.WriteLine("Time entry ended due to system idle");
                }
                
                Debug.WriteLine("System idle started");
            }
            else
            {
                // Coming back from idle
                _isIdle = false;
                Debug.WriteLine("System idle ended");
                
                // Get the current window info
                GetCurrentWindowAndCreateEntry();
            }
        }
        
        private void GetCurrentWindowAndCreateEntry()
        {
            try
            {
                // Get the current foreground window
                IntPtr hwnd = User32.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;
                
                // Get window title
                var titleBuilder = new System.Text.StringBuilder(256);
                User32.GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
                string title = titleBuilder.ToString();
                
                // Get process name
                User32.GetWindowThreadProcessId(hwnd, out uint processId);
                string appName = "Unknown";
                
                try
                {
                    using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                    appName = process.ProcessName;
                }
                catch
                {
                    // Process might have terminated
                }
                
                // Start a new entry with the current window info
                var now = DateTime.Now;
                _currentEntry = new TimeEntry
                {
                    StartTime = now,
                    EndTime = now,
                    WindowTitle = title,
                    ApplicationName = appName
                };
                
                Debug.WriteLine($"New time entry started after idle: {title} ({appName})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting current window: {ex.Message}");
                
                // Fallback: if we can't get the current window info, use the last known window
                if (!string.IsNullOrEmpty(_lastWindowTitle) && !string.IsNullOrEmpty(_lastAppName))
                {
                    var now = DateTime.Now;
                    _currentEntry = new TimeEntry
                    {
                        StartTime = now,
                        EndTime = now,
                        WindowTitle = _lastWindowTitle,
                        ApplicationName = _lastAppName
                    };
                    
                    Debug.WriteLine($"New time entry started after idle (using last known window): {_lastWindowTitle} ({_lastAppName})");
                }
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
                
                Debug.WriteLine("Time entry ended due to timeout (30+ minutes)");
            }
        }
        
        private void OnTimeEntryRecorded(TimeEntry entry)
        {
            TimeEntryRecorded?.Invoke(this, new TimeEntryRecordedEventArgs(entry));
        }
    }
    
    // Helper class for Win32 API calls
    internal static class User32
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
} 