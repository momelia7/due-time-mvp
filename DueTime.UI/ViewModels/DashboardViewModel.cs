using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DueTime.Data;

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

        // Commands
        public ICommand ChangeProjectCommand { get; }
        public ICommand GenerateWeeklySummaryCommand { get; }

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
                    await UpdateEntryProjectAsync(entry);
                }
            });

            GenerateWeeklySummaryCommand = new RelayCommand<object>(async _ => 
            {
                await GenerateWeeklySummaryAsync();
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
                // In a real app, log this error
                System.Diagnostics.Debug.WriteLine($"Error loading entries: {ex.Message}");
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
                // In a real app, log this error
                System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error updating entry project: {ex.Message}");
                // Could notify the UI of the error with a property
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

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 