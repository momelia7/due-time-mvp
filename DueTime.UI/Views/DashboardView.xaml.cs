using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DueTime.Data;

namespace DueTime.UI.Views
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        public DateTime Date { get; set; } = DateTime.Today;
        
        public DashboardView()
        {
            InitializeComponent();
            DataContext = this;
            
            // Register event handlers
            EntriesDataGrid.CellEditEnding += EntriesDataGrid_CellEditEnding;
        }
        
        private void EntriesDataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && e.Column == ProjectColumn)
            {
                if (e.Row.Item is TimeEntry entry)
                {
                    // Update the database with the new project assignment
                    AppState.EntryRepo.UpdateEntryProjectAsync(entry.Id, entry.ProjectId).Wait();
                }
            }
        }
        
        private async void WeeklySummary_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.AIEnabled || string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
            {
                System.Windows.MessageBox.Show("AI features are not enabled. Please enable AI and add your OpenAI API key in Settings.", 
                               "AI Not Enabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                // Show loading indicator
                var button = (System.Windows.Controls.Button)sender;
                var originalContent = button.Content;
                button.Content = "Generating...";
                button.IsEnabled = false;
                
                // Get date range for the past week
                DateTime today = DateTime.Today;
                DateTime weekStart = today.AddDays(-6); // Last 7 days including today
                
                // Fetch entries for the past week
                var entries = await AppState.EntryRepo.GetEntriesInRangeAsync(weekStart, today.AddDays(1));
                
                if (entries.Count == 0)
                {
                    System.Windows.MessageBox.Show("No time entries found for the past week.", 
                                   "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    button.Content = originalContent;
                    button.IsEnabled = true;
                    return;
                }
                
                // Calculate total time per project
                var projectTotals = new Dictionary<string, TimeSpan>();
                
                foreach (var entry in entries)
                {
                    string projectName = "Unassigned";
                    
                    if (entry.ProjectId.HasValue)
                    {
                        var project = AppState.Projects.FirstOrDefault(p => p.ProjectId == entry.ProjectId.Value);
                        if (project != null)
                        {
                            projectName = project.Name;
                        }
                    }
                    
                    TimeSpan duration = entry.EndTime - entry.StartTime;
                    
                    if (projectTotals.ContainsKey(projectName))
                    {
                        projectTotals[projectName] += duration;
                    }
                    else
                    {
                        projectTotals[projectName] = duration;
                    }
                }
                
                // Build the prompt for the summary
                var summaryPrompt = new StringBuilder();
                summaryPrompt.AppendLine($"Week from {weekStart:MMMM d, yyyy} to {today:MMMM d, yyyy}:");
                
                foreach (var project in projectTotals.OrderByDescending(p => p.Value))
                {
                    double hours = Math.Round(project.Value.TotalHours, 1);
                    summaryPrompt.AppendLine($"{project.Key}: {hours} hours");
                }
                
                // Call OpenAI to generate summary
                string? summary = await OpenAIClient.GetWeeklySummaryAsync(
                    weekStart, today, summaryPrompt.ToString(), AppState.ApiKeyPlaintext);
                
                // Reset button state
                button.Content = originalContent;
                button.IsEnabled = true;
                
                if (summary != null)
                {
                    // Display the summary
                    var fullSummary = $"{summaryPrompt}\n\n{summary}";
                    var summaryWindow = new SummaryWindow(fullSummary);
                    summaryWindow.Owner = Window.GetWindow(this);
                    summaryWindow.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to generate summary. Please check your API key and internet connection.", 
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Reset button state on error
                if (sender is System.Windows.Controls.Button button)
                {
                    button.Content = "Generate Weekly Summary";
                    button.IsEnabled = true;
                }
            }
        }
    }
} 