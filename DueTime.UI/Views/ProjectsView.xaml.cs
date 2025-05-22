using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DueTime.Data;
using DueTime.UI.Utilities;
using DueTime.UI.ViewModels;
using Microsoft.Win32;

namespace DueTime.UI.Views
{
    public partial class ProjectsView : System.Windows.Controls.UserControl
    {
        private ProjectsViewModel? _viewModel;
        
        public ProjectsView()
        {
            InitializeComponent();
            
            // We'll set this in the Loaded event when we know repositories are available
            Loaded += ProjectsView_Loaded;
        }
        
        private void ProjectsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the ViewModel with repositories from AppState
            // Eventually these would be injected through DI
            _viewModel = new ProjectsViewModel(AppState.ProjectRepo, AppState.RuleRepo);
            DataContext = _viewModel;
            
            // Wire up key events to execute commands
            ProjectNameTextBox.KeyDown += ProjectNameTextBox_KeyDown;
            RulePatternTextBox.KeyDown += RulePatternTextBox_KeyDown;
        }
        
        private void ProjectNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null && _viewModel.AddProjectCommand.CanExecute(null))
            {
                _viewModel.AddProjectCommand.Execute(null);
                e.Handled = true;
            }
        }
        
        private void RulePatternTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel != null && _viewModel.AddRuleCommand.CanExecute(null))
            {
                _viewModel.AddRuleCommand.Execute(null);
                e.Handled = true;
            }
        }
        
        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.AddProjectCommand.CanExecute(null))
            {
                _viewModel.AddProjectCommand.Execute(null);
            }
        }
        
        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.AddRuleCommand.CanExecute(null))
            {
                _viewModel.AddRuleCommand.Execute(null);
            }
        }
        
        /// <summary>
        /// Handles the "Select Window" button click
        /// </summary>
        private void OnSelectWindowClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show the window picker dialog
                var picker = new WindowPicker();
                var mainWindow = Window.GetWindow(this);
                picker.Owner = mainWindow;
                bool? result = picker.ShowDialog();
                
                // If user selected a window, update the rule pattern field
                if (result == true && picker.SelectedWindow != null)
                {
                    Logger.LogInfo($"Setting rule pattern to window: {picker.SelectedWindow.Title}");
                    
                    if (_viewModel != null)
                    {
                        _viewModel.NewRulePattern = picker.SelectedWindow.Title;
                        
                        // Try to auto-select a project based on the application name if possible
                        if (!string.IsNullOrEmpty(picker.SelectedWindow.AppName))
                        {
                            string appName = picker.SelectedWindow.AppName;
                            
                            // Try to find a project with a name similar to the app
                            foreach (var project in _viewModel.Projects)
                            {
                                if (project.Name.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    appName.IndexOf(project.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    _viewModel.SelectedProject = project;
                                    Logger.LogInfo($"Auto-selected project '{project.Name}' based on application name '{appName}'");
                                    break;
                                }
                            }
                        }
                        
                        // Focus the Add Rule button so the user can quickly add the rule
                        FocusAddRuleButton();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "OnSelectWindowClick");
                MessageBox.Show(
                    "An error occurred while selecting a window. Please try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Handles the "Select Folder" button click
        /// </summary>
        private void OnSelectFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create folder browser dialog
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select a folder to create a rule for:",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };
                
                // Show the dialog
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                
                // If user selected a folder, update the rule pattern field with the folder name
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    string folderPath = dialog.SelectedPath;
                    string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar));
                    
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        Logger.LogInfo($"Setting rule pattern to folder: {folderName} (from {folderPath})");
                        
                        if (_viewModel != null)
                        {
                            _viewModel.NewRulePattern = folderName;
                            
                            // Try to auto-select a project based on the folder name if possible
                            foreach (var project in _viewModel.Projects)
                            {
                                if (project.Name.IndexOf(folderName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    folderName.IndexOf(project.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    _viewModel.SelectedProject = project;
                                    Logger.LogInfo($"Auto-selected project '{project.Name}' based on folder name '{folderName}'");
                                    break;
                                }
                            }
                            
                            // Focus the Add Rule button so the user can quickly add the rule
                            FocusAddRuleButton();
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Empty folder name from path: {folderPath}");
                        MessageBox.Show(
                            "Could not determine the folder name. Please select a different folder or enter a pattern manually.",
                            "Invalid Folder",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "OnSelectFolderClick");
                MessageBox.Show(
                    "An error occurred while selecting a folder. Please try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Helper method to focus the Add Rule button
        /// </summary>
        private void FocusAddRuleButton()
        {
            // Find button by name and focus it
            if (this.FindName("AddRuleButton") is Button addRuleButton)
            {
                addRuleButton.Focus();
            }
        }
    }
} 