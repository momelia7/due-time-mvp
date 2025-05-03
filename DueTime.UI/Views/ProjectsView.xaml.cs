using System;
using System.Windows;
using System.Windows.Controls;
using DueTime.Data;

namespace DueTime.UI.Views
{
    public partial class ProjectsView : System.Windows.Controls.UserControl
    {
        public ProjectsView()
        {
            InitializeComponent();
        }
        
        private void AddProject_Click(object sender, RoutedEventArgs e)
        {
            AddProject();
        }
        
        private void ProjectNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddProject();
                e.Handled = true;
            }
        }
        
        private void AddProject()
        {
            string name = ProjectNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                try
                {
                    // Add to database
                    int id = AppState.ProjectRepo.AddProjectAsync(name).Result;
                    if (id > 0)
                    {
                        // Add to observable collection
                        var newProj = new Project { ProjectId = id, Name = name };
                        AppState.Projects.Add(newProj);
                        
                        // Clear input field
                        ProjectNameTextBox.Clear();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Could not add project. It may already exist.", "Error");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error adding project: {ex.Message}", "Error");
                }
            }
        }
        
        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            AddRule();
        }
        
        private void RulePatternTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddRule();
                e.Handled = true;
            }
        }
        
        private void AddRule()
        {
            string pattern = RulePatternTextBox.Text.Trim();
            Project? selectedProject = ProjectsComboBox.SelectedItem as Project;
            
            if (string.IsNullOrEmpty(pattern))
            {
                System.Windows.MessageBox.Show("Please enter a pattern for the rule.", "Validation Error");
                return;
            }
            
            if (selectedProject == null)
            {
                System.Windows.MessageBox.Show("Please select a project for the rule.", "Validation Error");
                return;
            }
            
            try
            {
                // Add to database
                int ruleId = AppState.RuleRepo.AddRuleAsync(pattern, selectedProject.ProjectId).Result;
                if (ruleId > 0)
                {
                    // Add to observable collection
                    var newRule = new Rule 
                    { 
                        Id = ruleId, 
                        Pattern = pattern, 
                        ProjectId = selectedProject.ProjectId, 
                        ProjectName = selectedProject.Name 
                    };
                    AppState.Rules.Add(newRule);
                    
                    // Clear input field
                    RulePatternTextBox.Clear();
                }
                else
                {
                    System.Windows.MessageBox.Show("Could not add rule.", "Error");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error adding rule: {ex.Message}", "Error");
            }
        }
    }
} 