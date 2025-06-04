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
using DueTime.UI.Views;

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
        private ToolStripMenuItem? _serviceMenuItem;
        
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
            
            // Add service management menu item
            _serviceMenuItem = new ToolStripMenuItem("Service Management");
            _serviceMenuItem.Click += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowServiceManagement();
                });
            };
            contextMenu.Items.Add(_serviceMenuItem);
            
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
            
            // Initialize the NotificationManager with our NotifyIcon
            NotificationManager.Initialize(_notifyIcon);
            
            // Always show notification to help find the app
            _notifyIcon.BalloonTipTitle = "DueTime is running";
            _notifyIcon.BalloonTipText = "Time tracking has started. You can find DueTime here in the system tray.";
            _notifyIcon.ShowBalloonTip(5000);
            
            Logger.LogInfo("System tray icon initialized");
        }
        
        /// <summary>
        /// Shows the service management view
        /// </summary>
        private void ShowServiceManagement()
        {
            // Create a new window to host the service management view
            var window = new Window
            {
                Title = "DueTime Service Management",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Content = new ServiceManagementView()
            };
            
            window.ShowDialog();
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
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if the background service is running
                bool isServiceRunning = ServiceCommunication.IsServiceRunning();
                
                if (isServiceRunning)
                {
                    // If the background service is running, we don't need to start our own tracking
                    Logger.LogInfo("Background service is running, skipping local tracking initialization");
                    await ShowStatusMessageAsync("Background service is running. Time tracking is active.");
                    
                    // Update menu items
                    if (_pauseResumeMenuItem != null)
                    {
                        _pauseResumeMenuItem.Text = "Service is tracking (managed via Service Management)";
                        _pauseResumeMenuItem.Enabled = false;
                    }
                }
                else
                {
                    // Initialize tracking engine locally
                    var systemEvents = new WindowsSystemEvents();
                    
                    _trackingService = new TrackingService(systemEvents, AppState.EntryRepo);
                    _trackingService.TimeEntryRecorded += TrackingService_TimeEntryRecorded;
                    _trackingService.Start();
                    
                    // Store reference for other components to use
                    AppState.TrackingService = _trackingService;
                    
                    Logger.LogInfo("Local tracking service initialized and started");
                }
                
                // Initialize database
                Database.InitializeSchema();
                
                // Initialize app state
                await InitializeAppStateAsync();
                
                // Configure AI if enabled
                if (AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext) && _trackingService != null)
                {
                    _trackingService.ConfigureAI(true, AppState.ApiKeyPlaintext, AppState.ProjectRepo);
                    Logger.LogInfo("AI categorization enabled");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "MainWindow_Loaded");
                MessageBox.Show(
                    $"An error occurred during startup: {ex.Message}\n\nThe application may not function correctly.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private async void TrackingService_TimeEntryRecorded(object? sender, TimeEntryRecordedEventArgs e)
        {
            try
            {
                // Update UI with new entry if needed
                await Dispatcher.InvokeAsync(() =>
                {
                    // If we're on the Dashboard view, refresh the entries
                    // This is handled by the DashboardViewModel
                });
                
                // If entry has no project assigned, try to suggest one using AI
                if (e.Entry.ProjectId == null && AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
                {
                    await SuggestProjectForEntryAsync(e.Entry);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "TimeEntryRecorded");
            }
        }
        
        private async Task SuggestProjectForEntryAsync(TimeEntry entry)
        {
            try
            {
                // Skip very short entries (< 30 seconds)
                TimeSpan duration = entry.EndTime - entry.StartTime;
                if (duration.TotalSeconds < 30)
                    return;
                
                // Get all projects for context
                var projects = await AppState.ProjectRepo.GetAllProjectsAsync();
                if (projects.Count == 0)
                    return; // No projects to suggest
                    
                // Get project names for AI
                string[] projectNames = projects.Select(p => p.Name).ToArray();
                
                // Get suggestion from OpenAI
                string? suggestion = await OpenAIClient.GetProjectSuggestionAsync(
                    entry.WindowTitle,
                    entry.ApplicationName,
                    projectNames,
                    AppState.ApiKeyPlaintext!);
                
                if (string.IsNullOrEmpty(suggestion) || suggestion == "None")
                    return;
                
                // Find matching project
                var matchingProject = projects.FirstOrDefault(p => 
                    string.Equals(p.Name, suggestion, StringComparison.OrdinalIgnoreCase));
                
                if (matchingProject != null)
                {
                    // Update the entry with the suggested project
                    await AppState.EntryRepo.UpdateEntryProjectAsync(entry.Id, matchingProject.ProjectId);
                    
                    // Log the suggestion
                    Logger.LogInfo($"AI suggested project '{matchingProject.Name}' for entry: {entry.WindowTitle}");
                    
                    // Show notification if enabled
                    Dispatcher.Invoke(() =>
                    {
                        NotificationManager.ShowInfo(
                            $"Entry automatically categorized as '{matchingProject.Name}'",
                            "AI Suggestion");
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "SuggestProjectForEntry");
            }
        }
        
        private async Task InitializeAppStateAsync()
        {
            try
            {
                // Load settings from database
                // TODO: Implement settings storage and retrieval
                
                // Load projects
                var projects = await AppState.ProjectRepo.GetAllProjectsAsync();
                AppState.Projects.Clear();
                foreach (var project in projects)
                {
                    AppState.Projects.Add(project);
                }
                
                // Load today's entries
                var entries = await AppState.EntryRepo.GetEntriesByDateAsync(DateTime.Today);
                AppState.Entries.Clear();
                foreach (var entry in entries)
                {
                    AppState.Entries.Add(entry);
                }
                
                Logger.LogInfo($"App state initialized with {AppState.Projects.Count} projects and {AppState.Entries.Count} entries");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "InitializeAppState");
                MessageBox.Show(
                    $"An error occurred while loading data: {ex.Message}",
                    "Data Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Check if this is a true exit or just minimizing to tray
            if (!_isExiting)
            {
                e.Cancel = true;
                this.Hide();
                
                // Show tray notification the first time
                if (!_hasShownTrayNotification)
                {
                    _notifyIcon?.ShowBalloonTip(
                        5000,
                        "DueTime is still running",
                        "The application will continue tracking in the background. Click the tray icon to reopen.",
                        ToolTipIcon.Info);
                    
                    _hasShownTrayNotification = true;
                }
                
                return;
            }
            
            base.OnClosing(e);
        }
        
        private bool _isExiting = false;
        
        private void CleanupAndExit()
        {
            try
            {
                // Stop tracking service if it's running locally
                if (_trackingService != null && !ServiceCommunication.IsServiceRunning())
                {
                    _trackingService.Stop();
                    Logger.LogInfo("Tracking service stopped");
                }
                
                // Dispose tray icon
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                
                // Mark as exiting and close
                _isExiting = true;
                this.Close();
                
                // Exit application
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "CleanupAndExit");
                
                // Force exit in case of error
                Environment.Exit(1);
            }
        }
    }
} 