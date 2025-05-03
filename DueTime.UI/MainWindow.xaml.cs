using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
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
            if (entry.ProjectId == null && AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
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
                if (!AppState.AIEnabled || entry.ProjectId != null || string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
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
            base.OnClosing(e);
            _trackingService?.Stop();
        }
    }
} 