using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DueTime.Data;
using DueTime.UI.Utilities;

namespace DueTime.UI.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // Collections for data binding
        public ObservableCollection<TimeEntry> TimeEntries { get; private set; }
        public ObservableCollection<Project> Projects { get; private set; }

        // Currently selected time entry
        private TimeEntry? _selectedEntry;
        public TimeEntry? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry != value)
                {
                    _selectedEntry = value;
                    OnPropertyChanged(nameof(SelectedEntry));
                }
            }
        }

        // Dictionary to track AI suggestion overrides
        private Dictionary<string, int> _suggestionOverrides = new Dictionary<string, int>();
        
        // Currently suggested project for an entry
        private Project? _suggestedProject;
        private TimeEntry? _entryWithSuggestion;

        // Commands
        public ICommand ChangeProjectCommand { get; }
        public ICommand GenerateWeeklySummaryCommand { get; }
        public ICommand SuggestProjectCommand { get; }

        // Repository dependencies
        private readonly ITimeEntryRepository _timeEntryRepo;
        private readonly IProjectRepository _projectRepo;

        public DashboardViewModel(ITimeEntryRepository timeEntryRepo, IProjectRepository projectRepo)
        {
            _timeEntryRepo = timeEntryRepo;
            _projectRepo = projectRepo;

            TimeEntries = new ObservableCollection<TimeEntry>();
            Projects = new ObservableCollection<Project>();

            // Initialize commands
            ChangeProjectCommand = new RelayCommand<TimeEntry>(async entry => 
            {
                if (entry != null)
                {
                    // Check if this entry had a suggestion that's being overridden
                    await UpdateEntryProjectAsync(entry);
                    
                    // Track if user overrode an AI suggestion
                    if (_entryWithSuggestion == entry && _suggestedProject != null && 
                        entry.ProjectId != _suggestedProject.ProjectId)
                    {
                        TrackSuggestionOverride(_suggestedProject.Name, GetProjectNameById(entry.ProjectId));
                    }
                    
                    // Clear the suggestion state
                    _entryWithSuggestion = null;
                    _suggestedProject = null;
                }
            });

            GenerateWeeklySummaryCommand = new RelayCommand<object>(async _ => 
            {
                await GenerateWeeklySummaryAsync();
            });
            
            SuggestProjectCommand = new RelayCommand<TimeEntry>(async entry =>
            {
                if (entry != null)
                {
                    await SuggestProjectForEntryAsync(entry);
                }
            });

            // Load today's data
            LoadTodayEntries();
            LoadProjects();
        }

        private async void LoadTodayEntries()
        {
            try
            {
                var entries = await _timeEntryRepo.GetEntriesByDateAsync(DateTime.Today);
                
                TimeEntries.Clear();
                foreach (var entry in entries)
                {
                    TimeEntries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "LoadTodayEntries");
                NotificationManager.ShowError("Failed to load today's time entries.");
            }
        }

        private async void LoadProjects()
        {
            try
            {
                var projects = await _projectRepo.GetAllProjectsAsync();
                
                Projects.Clear();
                foreach (var project in projects)
                {
                    Projects.Add(project);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "LoadProjects");
                NotificationManager.ShowError("Failed to load projects.");
            }
        }

        private async Task UpdateEntryProjectAsync(TimeEntry entry)
        {
            try
            {
                // Update the entry in the database
                await _timeEntryRepo.UpdateEntryProjectAsync(entry.Id, entry.ProjectId);
                
                // Refresh the entry in the UI collection (if needed)
                int index = TimeEntries.IndexOf(entry);
                if (index >= 0)
                {
                    TimeEntries.RemoveAt(index);
                    TimeEntries.Insert(index, entry);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UpdateEntryProject");
                NotificationManager.ShowError("Failed to update project for this entry.");
            }
        }
        
        private async Task SuggestProjectForEntryAsync(TimeEntry entry)
        {
            if (entry == null)
            {
                return;
            }
            
            if (!AppState.AIEnabled || string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
            {
                NotificationManager.ShowWarning("AI suggestions are not enabled. Please enable AI in Settings and add an API key.");
                return;
            }
            
            try
            {
                // Show status message
                await NotificationManager.ShowStatusAsync("Getting AI suggestion...", 0);
                
                // Get all project names for the suggestion
                string[] projectNames = Projects.Select(p => p.Name).ToArray();
                
                // Call the OpenAI API to get a suggestion
                string? suggestedProjectName = await OpenAIClient.GetProjectSuggestionAsync(
                    entry.WindowTitle,
                    entry.ApplicationName,
                    projectNames,
                    AppState.ApiKeyPlaintext);
                
                if (!string.IsNullOrEmpty(suggestedProjectName))
                {
                    // Find the project by name
                    var suggestedProject = Projects.FirstOrDefault(p => 
                        p.Name.Equals(suggestedProjectName, StringComparison.OrdinalIgnoreCase));
                    
                    if (suggestedProject != null)
                    {
                        // Store the suggestion for tracking overrides
                        _entryWithSuggestion = entry;
                        _suggestedProject = suggestedProject;
                        
                        // Update the entry with the suggested project
                        entry.ProjectId = suggestedProject.ProjectId;
                        
                        // Update in the database
                        await UpdateEntryProjectAsync(entry);
                        
                        // Log and notify about the suggestion
                        Logger.LogInfo($"AI suggested project '{suggestedProjectName}' for entry '{entry.WindowTitle}'");
                        NotificationManager.ShowSuggestion($"Assigned to '{suggestedProjectName}'", "Project Suggestion");
                        
                        // Clear status message
                        await NotificationManager.ShowStatusAsync("Suggestion applied", 2000);
                    }
                    else
                    {
                        // The suggested project name doesn't match any existing project
                        NotificationManager.ShowWarning($"AI suggested '{suggestedProjectName}' but no matching project was found.");
                        await NotificationManager.ShowStatusAsync("No matching project found", 2000);
                    }
                }
                else
                {
                    // No suggestion was returned
                    NotificationManager.ShowWarning("AI couldn't suggest a project for this entry.");
                    await NotificationManager.ShowStatusAsync("No suggestion available", 2000);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "SuggestProjectForEntry");
                NotificationManager.ShowError("Failed to get AI suggestion. Check your API key and internet connection.");
                await NotificationManager.ShowStatusAsync("Suggestion failed", 2000);
            }
        }

        private async Task GenerateWeeklySummaryAsync()
        {
            // This would be implemented in the view or using a service
            // Since it involves UI dialogs and interactions, we'll leave the implementation
            // details to the view code-behind but could expand this in future iterations
            
            // The basic logic would be:
            // 1. Check if AI is enabled
            // 2. Get date range for past week
            // 3. Fetch entries for the week
            // 4. Calculate time per project
            // 5. Generate prompt
            // 6. Call API
            // 7. Display results

            // For now this serves as a placeholder for the command
            await Task.CompletedTask;
        }
        
        private void TrackSuggestionOverride(string suggestedProject, string chosenProject)
        {
            if (string.IsNullOrEmpty(suggestedProject) || string.IsNullOrEmpty(chosenProject))
                return;
                
            // Create a unique key for this override pattern
            string overrideKey = $"{suggestedProject}|{chosenProject}";
            
            // Increment the count for this override pattern
            if (_suggestionOverrides.ContainsKey(overrideKey))
            {
                _suggestionOverrides[overrideKey]++;
            }
            else
            {
                _suggestionOverrides[overrideKey] = 1;
            }
            
            // Log the override
            Logger.LogInfo($"AI suggestion override: Suggested '{suggestedProject}' but user chose '{chosenProject}'");
            
            // Check if this override has happened multiple times
            if (_suggestionOverrides[overrideKey] >= 3)
            {
                // Notify the user about creating a rule
                NotificationManager.ShowSuggestionPattern(suggestedProject, chosenProject);
                
                // Reset the counter after notification
                _suggestionOverrides[overrideKey] = 0;
            }
        }
        
        private string GetProjectNameById(int? projectId)
        {
            if (projectId == null)
                return "(No Project)";
                
            var project = Projects.FirstOrDefault(p => p.ProjectId == projectId);
            return project?.Name ?? "(Unknown Project)";
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 