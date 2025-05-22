using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DueTime.Tracking;
using DueTime.Data;

namespace DueTime.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ITrackingService? _trackingService;
        private NotifyIcon? _notifyIcon;
        private bool _hasShownTrayNotification = false;
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;
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
            catch
            {
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
            
            // Show welcome notification on first run
            if (AppState.IsFirstRun)
            {
                _notifyIcon.BalloonTipTitle = "Welcome to DueTime";
                _notifyIcon.BalloonTipText = "Time tracking has started automatically. You can always find DueTime here in the system tray.";
                _notifyIcon.ShowBalloonTip(5000);
            }
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize tracking engine
            var systemEvents = new WindowsSystemEvents();
            
            _trackingService = new TrackingService(systemEvents, AppState.EntryRepo);
            _trackingService.TimeEntryRecorded += TrackingService_TimeEntryRecorded;
            _trackingService.Start();
            
            // Store reference for other components to use
            AppState.TrackingService = _trackingService;
        }

        private async void TrackingService_TimeEntryRecorded(object? sender, TimeEntryRecordedEventArgs e)
        {
            var entry = e.Entry;
            
            // Add to UI collection
            Dispatcher.Invoke(() =>
            {
                AppState.Entries.Add(entry);
            });
            
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
                }
            }
            catch (Exception ex)
            {
                // Just log error, don't show UI since this is background processing
                System.Diagnostics.Debug.WriteLine($"Error suggesting project: {ex.Message}");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_notifyIcon != null)
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
                }
            }
            else
            {
                // If we're really exiting, stop tracking
                _trackingService?.Stop();
            }
        }
        
        private void CleanupAndExit()
        {
            // Dispose of the NotifyIcon before exiting
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            
            // Stop tracking
            _trackingService?.Stop();
            
            // Close the window (this time it won't be canceled)
            this.Close();
        }
    }
} 