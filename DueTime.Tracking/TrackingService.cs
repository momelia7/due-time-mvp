using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private bool _isTracking;
        
        // AI-related fields
        private bool _useAI = false;
        private string? _apiKey = null;
        private List<string> _projectNames = new List<string>();
        private IProjectRepository? _projectRepository;
        
        public event EventHandler<TimeEntryRecordedEventArgs>? TimeEntryRecorded;
        public event EventHandler<TrackingStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// Gets whether the tracking service is currently active
        /// </summary>
        public bool IsTracking => _isTracking;
        
        /// <summary>
        /// Gets the current time entry being tracked, if any
        /// </summary>
        public TimeEntry? CurrentEntry => _currentEntry;

        public TrackingService(ISystemEvents systemEvents, ITimeEntryRepository repository)
        {
            _systemEvents = systemEvents;
            _repository = repository;
            _isIdle = false;
            _isTracking = false;
        }
        
        /// <summary>
        /// Configures AI-powered project categorization
        /// </summary>
        public void ConfigureAI(bool useAI, string? apiKey, IProjectRepository projectRepository)
        {
            _useAI = useAI;
            _apiKey = apiKey;
            _projectRepository = projectRepository;
            
            // Load project names for AI categorization
            if (_useAI && _projectRepository != null)
            {
                LoadProjectNames();
            }
        }
        
        private async void LoadProjectNames()
        {
            try
            {
                if (_projectRepository != null)
                {
                    var projects = await _projectRepository.GetAllProjectsAsync();
                    _projectNames = projects.Select(p => p.Name).ToList();
                    Debug.WriteLine($"Loaded {_projectNames.Count} project names for AI categorization");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading project names: {ex.Message}");
            }
        }

        public void Start()
        {
            if (_isTracking) return; // Already tracking
            
            _systemEvents.WindowChanged += OnWindowChanged;
            _systemEvents.IdleStateChanged += OnIdleStateChanged;
            _systemEvents.Start();
            
            // Create an idle check timer (every minute)
            _idleTimer = new Timer(CheckIdleTimeout, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            
            _isTracking = true;
            OnStatusChanged(true, "Tracking started");
            Debug.WriteLine("Tracking service started");
        }

        public void Stop()
        {
            if (!_isTracking) return; // Not tracking
            
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
            
            _isTracking = false;
            OnStatusChanged(false, "Tracking stopped");
            Debug.WriteLine("Tracking service stopped");
        }

        private async void OnWindowChanged(object? sender, WindowChangedEventArgs e)
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
                
                // Try AI categorization if enabled
                if (_useAI && !string.IsNullOrEmpty(_apiKey) && _projectNames.Count > 0 && _currentEntry.ProjectId == null)
                {
                    await TryAICategorization(_currentEntry);
                }
                
                await _repository.AddTimeEntryAsync(_currentEntry);
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
        
        private async Task TryAICategorization(TimeEntry entry)
        {
            try
            {
                if (!_useAI || string.IsNullOrEmpty(_apiKey) || _projectNames.Count == 0 || _projectRepository == null)
                    return;
                
                // Get project suggestion from OpenAI
                string? projectSuggestion = await OpenAIClient.GetProjectSuggestionAsync(
                    entry.WindowTitle,
                    entry.ApplicationName,
                    _projectNames.ToArray(),
                    _apiKey);
                
                if (!string.IsNullOrEmpty(projectSuggestion) && projectSuggestion != "None")
                {
                    // Find the project ID for the suggested project name
                    var projects = await _projectRepository.GetAllProjectsAsync();
                    var matchingProject = projects.FirstOrDefault(p => 
                        string.Equals(p.Name, projectSuggestion, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingProject != null)
                    {
                        entry.ProjectId = matchingProject.ProjectId;
                        Debug.WriteLine($"AI categorized entry to project: {matchingProject.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AI categorization: {ex.Message}");
                // Continue without AI categorization
            }
        }

        private async void OnIdleStateChanged(object? sender, IdleStateChangedEventArgs e)
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
                    
                    // Try AI categorization if enabled
                    if (_useAI && !string.IsNullOrEmpty(_apiKey) && _projectNames.Count > 0 && _currentEntry.ProjectId == null)
                    {
                        await TryAICategorization(_currentEntry);
                    }
                    
                    await _repository.AddTimeEntryAsync(_currentEntry);
                    OnTimeEntryRecorded(_currentEntry);
                    _currentEntry = null;
                    
                    Debug.WriteLine("Time entry ended due to system idle");
                }
                
                OnStatusChanged(true, "System idle started");
                Debug.WriteLine("System idle started");
            }
            else
            {
                // Coming back from idle
                _isIdle = false;
                OnStatusChanged(true, "System idle ended");
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
        
        private async void CheckIdleTimeout(object? state)
        {
            // This is called periodically to check if we should close entries after long idle periods
            if (!_isTracking || _isIdle || _currentEntry == null) return;
            
            // Check if the current entry has been open for too long (e.g., 30 minutes)
            // This can happen if the idle detection fails for some reason
            var now = DateTime.Now;
            var duration = now - _currentEntry.StartTime;
            
            if (duration.TotalMinutes > 30)
            {
                Debug.WriteLine("Entry duration exceeds 30 minutes, closing as a precaution");
                
                _currentEntry.EndTime = now;
                await _repository.AddTimeEntryAsync(_currentEntry);
                OnTimeEntryRecorded(_currentEntry);
                _currentEntry = null;
                
                // Get the current window to start a new entry
                GetCurrentWindowAndCreateEntry();
            }
        }
        
        private void OnTimeEntryRecorded(TimeEntry entry)
        {
            TimeEntryRecorded?.Invoke(this, new TimeEntryRecordedEventArgs(entry));
        }
        
        private void OnStatusChanged(bool isTracking, string reason)
        {
            StatusChanged?.Invoke(this, new TrackingStatusChangedEventArgs(isTracking, reason));
        }
        
        internal static class User32
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();
            
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            internal static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
            
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        }
    }
} 