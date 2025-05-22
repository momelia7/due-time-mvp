using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DueTime.Tracking;
using DueTime.Data;
using DueTime.UI.Utilities;

namespace DueTime.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ITrackingService? _trackingService;
        private NotifyIcon? _notifyIcon;
        private bool _hasShownTrayNotification = false;
        private bool _isTrackingPaused = false;
        private ToolStripMenuItem? _pauseResumeMenuItem;
        
        // Status message property for binding
        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Set DataContext for binding
            this.DataContext = this;
            
            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;
            
            // DEBUG: Make window visible immediately
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        
        /// <summary>
        /// Shows a status message for a specified duration
        /// </summary>
        public async Task ShowStatusMessageAsync(string message, int durationMs = 5000)
        {
            StatusMessage = message;
            
            // Show progress indicator if needed
            if (message.Contains("..."))
            {
                Dispatcher.Invoke(() => StatusProgress.Visibility = Visibility.Visible);
            }
            
            if (durationMs > 0)
            {
                await Task.Delay(durationMs);
                
                // Only clear if it's still the same message
                if (StatusMessage == message)
                {
                    StatusMessage = string.Empty;
                }
                
                // Hide progress indicator
                Dispatcher.Invoke(() => StatusProgress.Visibility = Visibility.Collapsed);
            }
        }
        
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            SetupTrayIcon();
        }
        
        private void SetupTrayIcon()
        {
            // Create the NotifyIcon
            _notifyIcon = new NotifyIcon();
            
            // Try to extract icon from the executable
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule?.FileName ?? "");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Icon extraction for tray");
                // If icon extraction fails, the NotifyIcon will just use a default icon
            }
            
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "DueTime (tracking active)";
            
            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            var openItem = contextMenu.Items.Add("Open Dashboard");
            openItem.Click += (s, e) => 
            { 
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                });
            };
            
            // Add pause/resume menu item
            _pauseResumeMenuItem = new ToolStripMenuItem("Pause Tracking");
            _pauseResumeMenuItem.Click += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ToggleTracking();
                });
            };
            contextMenu.Items.Add(_pauseResumeMenuItem);
            
            contextMenu.Items.Add("-"); // Separator
            
            var exitItem = contextMenu.Items.Add("Exit");
            exitItem.Click += (s, e) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    // True exit - cleanup and close
                    CleanupAndExit();
                });
            };
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Double-click to open main window
            _notifyIcon.DoubleClick += (s, e) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                });
            };
            
            // Always show notification to help find the app
            _notifyIcon.BalloonTipTitle = "DueTime is running";
            _notifyIcon.BalloonTipText = "Time tracking has started. You can find DueTime here in the system tray.";
            _notifyIcon.ShowBalloonTip(5000);
            
            Logger.LogInfo("System tray icon initialized");
        }
        
        /// <summary>
        /// Toggles tracking between paused and active states
        /// </summary>
        private void ToggleTracking()
        {
            if (_trackingService == null || _notifyIcon == null || _pauseResumeMenuItem == null)
                return;
            
            try
            {
                _isTrackingPaused = !_isTrackingPaused;
                
                if (_isTrackingPaused)
                {
                    // Pause tracking
                    _trackingService.Stop();
                    _notifyIcon.Text = "DueTime (paused)";
                    _pauseResumeMenuItem.Text = "Resume Tracking";
                    
                    // Show notification about paused state
                    _notifyIcon.BalloonTipTitle = "Tracking Paused";
                    _notifyIcon.BalloonTipText = "Time tracking has been paused. Your activities are not being recorded.";
                    _notifyIcon.ShowBalloonTip(3000);
                    
                    Logger.LogInfo("Tracking paused by user");
                }
                else
                {
                    // Resume tracking
                    _trackingService.Start();
                    _notifyIcon.Text = "DueTime (tracking active)";
                    _pauseResumeMenuItem.Text = "Pause Tracking";
                    
                    // Show notification about resumed state
                    _notifyIcon.BalloonTipTitle = "Tracking Resumed";
                    _notifyIcon.BalloonTipText = "Time tracking has been resumed. Your activities are now being recorded.";
                    _notifyIcon.ShowBalloonTip(3000);
                    
                    Logger.LogInfo("Tracking resumed by user");
                }
                
                // Update status in AppState (if needed by other components)
                AppState.IsTrackingPaused = _isTrackingPaused;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "ToggleTracking");
                MessageBox.Show(
                    "An error occurred while trying to pause/resume tracking. Please try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize tracking engine
                var systemEvents = new WindowsSystemEvents();
                
                _trackingService = new TrackingService(systemEvents, AppState.EntryRepo);
                _trackingService.TimeEntryRecorded += TrackingService_TimeEntryRecorded;
                _trackingService.Start();
                
                // Store reference for other components to use
                AppState.TrackingService = _trackingService;
                
                Logger.LogInfo("Tracking service initialized and started");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "MainWindow_Loaded");
                MessageBox.Show(
                    "There was a problem initializing the tracking service. Some features may not work correctly.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void TrackingService_TimeEntryRecorded(object? sender, TimeEntryRecordedEventArgs e)
        {
            var entry = e.Entry;
            
            // Add to UI collection
            Dispatcher.Invoke(() =>
            {
                AppState.Entries.Add(entry);
            });
            
            Logger.LogInfo($"Time entry recorded: {entry.WindowTitle} ({entry.ApplicationName})");
            
            // If entry has no project and AI is enabled, suggest a project
            // Also check for trial period and license validity
            if (entry.ProjectId == null && 
                AppState.AIEnabled && 
                !string.IsNullOrEmpty(AppState.ApiKeyPlaintext) &&
                (!AppState.TrialExpired || AppState.LicenseValid))
            {
                // Launch suggestion in background to avoid blocking UI
                await SuggestProjectForEntryAsync(entry);
            }
        }
        
        private async Task SuggestProjectForEntryAsync(TimeEntry entry)
        {
            try
            {
                // Only proceed if we still have AI enabled and entry still doesn't have project
                // Also check for trial period and license validity
                if (!AppState.AIEnabled || 
                    entry.ProjectId != null || 
                    string.IsNullOrEmpty(AppState.ApiKeyPlaintext) ||
                    (AppState.TrialExpired && !AppState.LicenseValid))
                {
                    return;
                }
                
                // Get all project names for the suggestion
                string[] projectNames = AppState.Projects.Select(p => p.Name).ToArray();
                
                // Don't bother calling API if we have no projects
                if (projectNames.Length == 0)
                {
                    return;
                }
                
                // Show status message
                await ShowStatusMessageAsync("Getting AI suggestion...", 0);
                
                Logger.LogInfo($"Requesting AI project suggestion for: {entry.WindowTitle}");
                
                // Get suggestion from OpenAI
                string? suggestion = await OpenAIClient.GetProjectSuggestionAsync(
                    entry.WindowTitle ?? string.Empty,
                    entry.ApplicationName ?? string.Empty,
                    projectNames,
                    AppState.ApiKeyPlaintext);
                
                // Apply suggestion if valid
                if (!string.IsNullOrEmpty(suggestion) && suggestion.ToLower() != "none")
                {
                    var project = AppState.Projects.FirstOrDefault(
                        p => p.Name.Equals(suggestion, StringComparison.OrdinalIgnoreCase));
                    
                    if (project != null)
                    {
                        // Update entry in database
                        await AppState.EntryRepo.UpdateEntryProjectAsync(entry.Id, project.ProjectId);
                        
                        Logger.LogInfo($"AI suggested project '{project.Name}' for entry: {entry.WindowTitle}");
                        
                        // Update status message
                        await ShowStatusMessageAsync($"AI suggested project: {project.Name}", 3000);
                        
                        // Update UI
                        Dispatcher.Invoke(() =>
                        {
                            // Update the entry's ProjectId
                            entry.ProjectId = project.ProjectId;
                            
                            // Force refresh of the DataGrid item
                            int index = AppState.Entries.IndexOf(entry);
                            if (index >= 0)
                            {
                                AppState.Entries.RemoveAt(index);
                                AppState.Entries.Insert(index, entry);
                            }
                        });
                    }
                    else
                    {
                        await ShowStatusMessageAsync("AI suggestion could not be applied", 3000);
                    }
                }
                else
                {
                    Logger.LogInfo($"AI did not suggest a project for: {entry.WindowTitle}");
                    await ShowStatusMessageAsync("No AI suggestion available", 3000);
                }
            }
            catch (Exception ex)
            {
                // Just log error, don't show UI since this is background processing
                Logger.LogException(ex, "SuggestProjectForEntryAsync");
                await ShowStatusMessageAsync("AI suggestion error", 3000);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Check if we're really exiting or just hiding to tray
            if (_notifyIcon != null && !((App)Application.Current).IsShuttingDown)
            {
                // Cancel the close operation and hide the window instead
                e.Cancel = true;
                this.Hide();
                
                // Show notification that app is still running (only once per session)
                if (!_hasShownTrayNotification)
                {
                    _notifyIcon.BalloonTipTitle = "DueTime is still running";
                    _notifyIcon.BalloonTipText = "DueTime will continue tracking in the background. You can find it in the system tray.";
                    _notifyIcon.ShowBalloonTip(3000);
                    _hasShownTrayNotification = true;
                    
                    Logger.LogInfo("Window hidden to system tray");
                }
            }
            else
            {
                // If we're really exiting, stop tracking
                _trackingService?.Stop();
                Logger.LogInfo("Window closing, tracking stopped");
            }
        }
        
        private void CleanupAndExit()
        {
            try
            {
                // Set the shutdown flag in App
                ((App)Application.Current).IsShuttingDown = true;
                
                // Dispose of the NotifyIcon before exiting
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
                
                // Stop tracking
                _trackingService?.Stop();
                
                Logger.LogInfo("Application exit requested by user from tray menu");
                
                // Close the window (this time it won't be canceled)
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "CleanupAndExit");
                Application.Current.Shutdown(); // Force shutdown even if there was an error
            }
        }
    }
} 