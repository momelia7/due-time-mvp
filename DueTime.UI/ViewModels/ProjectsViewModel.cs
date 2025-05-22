using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DueTime.Data;

namespace DueTime.UI.ViewModels
{
    public class ProjectsViewModel : INotifyPropertyChanged
    {
        // Collections for data binding
        public ObservableCollection<Project> Projects { get; private set; }
        public ObservableCollection<Rule> Rules { get; private set; }

        // Properties for input fields
        private string _newProjectName = string.Empty;
        public string NewProjectName
        {
            get => _newProjectName;
            set
            {
                if (_newProjectName != value)
                {
                    _newProjectName = value;
                    OnPropertyChanged(nameof(NewProjectName));
                    // Command will check CanExecute
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _newRulePattern = string.Empty;
        public string NewRulePattern
        {
            get => _newRulePattern;
            set
            {
                if (_newRulePattern != value)
                {
                    _newRulePattern = value;
                    OnPropertyChanged(nameof(NewRulePattern));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private Project? _selectedProject;
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (_selectedProject != value)
                {
                    _selectedProject = value;
                    OnPropertyChanged(nameof(SelectedProject));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private Rule? _selectedRule;
        public Rule? SelectedRule
        {
            get => _selectedRule;
            set
            {
                if (_selectedRule != value)
                {
                    _selectedRule = value;
                    OnPropertyChanged(nameof(SelectedRule));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Commands
        public ICommand AddProjectCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }

        // Repository dependencies
        private readonly IProjectRepository _projectRepo;
        private readonly IRuleRepository _ruleRepo;

        public ProjectsViewModel(IProjectRepository projectRepo, IRuleRepository ruleRepo)
        {
            _projectRepo = projectRepo;
            _ruleRepo = ruleRepo;

            Projects = new ObservableCollection<Project>();
            Rules = new ObservableCollection<Rule>();

            // Initialize commands
            AddProjectCommand = new RelayCommand<object>(
                async _ => await AddProjectAsync(), 
                _ => CanAddProject()
            );

            AddRuleCommand = new RelayCommand<object>(
                async _ => await AddRuleAsync(),
                _ => CanAddRule()
            );

            DeleteRuleCommand = new RelayCommand<Rule>(
                async rule => await DeleteRuleAsync(rule),
                rule => rule != null
            );

            // Load data
            LoadProjectsAndRules();
        }

        private async void LoadProjectsAndRules()
        {
            try
            {
                // Load projects
                var projects = await _projectRepo.GetAllProjectsAsync();
                Projects.Clear();
                foreach (var project in projects)
                {
                    Projects.Add(project);
                }

                // Load rules
                var rules = await _ruleRepo.GetAllRulesAsync();
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading projects or rules: {ex.Message}");
            }
        }

        private bool CanAddProject()
        {
            return !string.IsNullOrWhiteSpace(NewProjectName);
        }

        private async Task AddProjectAsync()
        {
            if (!CanAddProject()) return;

            try
            {
                string projectName = NewProjectName.Trim();
                
                // Add to database
                int projectId = await _projectRepo.AddProjectAsync(projectName);
                
                if (projectId > 0)
                {
                    // Add to observable collection
                    var newProject = new Project { ProjectId = projectId, Name = projectName };
                    Projects.Add(newProject);
                    
                    // Clear input
                    NewProjectName = string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding project: {ex.Message}");
            }
        }

        private bool CanAddRule()
        {
            return !string.IsNullOrWhiteSpace(NewRulePattern) && SelectedProject != null;
        }

        private async Task AddRuleAsync()
        {
            if (!CanAddRule()) return;

            try
            {
                string pattern = NewRulePattern.Trim();
                int projectId = SelectedProject!.ProjectId;
                
                // Add to database
                int ruleId = await _ruleRepo.AddRuleAsync(pattern, projectId);
                
                if (ruleId > 0)
                {
                    // Add to observable collection
                    var newRule = new Rule 
                    { 
                        Id = ruleId,
                        Pattern = pattern,
                        ProjectId = projectId,
                        ProjectName = SelectedProject.Name
                    };
                    Rules.Add(newRule);
                    
                    // Clear input
                    NewRulePattern = string.Empty;
                    // Don't reset SelectedProject to make adding multiple rules easier
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding rule: {ex.Message}");
            }
        }

        private async Task DeleteRuleAsync(Rule? rule)
        {
            if (rule == null) return;

            try
            {
                // Currently there's no method to delete a specific rule in the interface
                // This is a gap that should be added to the interface
                // For now we'll simulate this with what's available
                await Task.Run(() => {
                    // Here we'd call _ruleRepo.DeleteRuleAsync(rule.Id) if it existed
                    System.Diagnostics.Debug.WriteLine($"Would delete rule: {rule.Id}");
                });
                
                // Remove from observable collection
                Rules.Remove(rule);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting rule: {ex.Message}");
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 