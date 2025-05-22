using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DueTime.Data;
using DueTime.UI.ViewModels;

namespace DueTime.UI.Views
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        private DashboardViewModel? _viewModel;
        
        public DashboardView()
        {
            InitializeComponent();
            
            // We'll set this in the Loaded event when we know repositories are available
            Loaded += DashboardView_Loaded;
        }
        
        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the ViewModel with repositories from AppState
            // Eventually these would be injected through DI
            _viewModel = new DashboardViewModel(AppState.EntryRepo, AppState.ProjectRepo);
            DataContext = _viewModel;
            
            // No need to register for CellEditEnding - the ViewModel handles this via binding
        }
        
        private async void WeeklySummary_Click(object sender, RoutedEventArgs e)
        {
            // Check for trial expiration if not licensed
            if (AppState.TrialExpired && !AppState.LicenseValid)
            {
                System.Windows.MessageBox.Show(
                    "Your trial period has expired. Please enter a license key to use premium features.", 
                    "Trial Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // Get the main window for status updates
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    await mainWindow.ShowStatusMessageAsync("Generating weekly summary...", 0);
                }
                
                // Get entries for the past week
                DateTime today = DateTime.Today;
                DateTime weekStart = today.AddDays(-6); // Last 7 days including today
                
                var entries = await AppState.EntryRepo.GetEntriesInRangeAsync(weekStart, today.AddDays(1));
                
                if (entries.Count == 0)
                {
                    if (mainWindow != null)
                    {
                        await mainWindow.ShowStatusMessageAsync("No time entries found for the past week", 3000);
                    }
                    
                    MessageBox.Show("No time entries found for the past week.", "Weekly Summary", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Group entries by project and calculate total time
                var projectTotals = entries
                    .Where(e => e.EndTime != DateTime.MinValue) // Filter out entries without end time
                    .GroupBy(e => GetProjectName(e))
                    .Select(g => new 
                    {
                        Project = g.Key,
                        TotalMinutes = g.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes)
                    })
                    .OrderByDescending(p => p.TotalMinutes)
                    .ToList();
                
                // Create basic summary text
                StringBuilder summaryText = new StringBuilder();
                summaryText.AppendLine($"Weekly Summary ({weekStart.ToShortDateString()} - {today.ToShortDateString()})");
                summaryText.AppendLine();
                
                foreach (var proj in projectTotals)
                {
                    double hours = proj.TotalMinutes / 60.0;
                    summaryText.AppendLine($"• {proj.Project}: {hours:F1} hours");
                }
                
                double totalHours = projectTotals.Sum(p => p.TotalMinutes) / 60.0;
                summaryText.AppendLine();
                summaryText.AppendLine($"Total Tracked Time: {totalHours:F1} hours");
                
                string finalSummary = summaryText.ToString();
                
                // If AI is enabled, try to get a narrative summary
                if (AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext) &&
                    (!AppState.TrialExpired || AppState.LicenseValid))
                {
                    try
                    {
                        if (mainWindow != null)
                        {
                            await mainWindow.ShowStatusMessageAsync("Generating AI narrative...", 0);
                        }
                        
                        string aiPrompt = summaryText.ToString() + "\n\nBased on the data above, provide a brief summary of this week's work.";
                        string? aiSummary = await OpenAIClient.GetWeeklySummaryAsync(weekStart, today, aiPrompt, AppState.ApiKeyPlaintext);
                        
                        if (!string.IsNullOrEmpty(aiSummary))
                        {
                            // Add the AI summary to the basic summary
                            finalSummary = summaryText.ToString() + "\n\nAI Summary:\n" + aiSummary;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If AI summary fails, we'll just use the basic summary
                        Utilities.Logger.LogException(ex, "WeeklySummary_AI");
                    }
                }
                
                // Clear status message
                if (mainWindow != null)
                {
                    await mainWindow.ShowStatusMessageAsync("Weekly summary generated", 3000);
                }
                
                // Show the summary in a dialog
                ShowSummaryDialog(finalSummary);
            }
            catch (Exception ex)
            {
                Utilities.Logger.LogException(ex, "WeeklySummary_Click");
                MessageBox.Show("An error occurred while generating the weekly summary.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private string GetProjectName(TimeEntry entry)
        {
            if (entry.ProjectId == null)
                return "(No Project)";
                
            var project = AppState.Projects.FirstOrDefault(p => p.ProjectId == entry.ProjectId);
            return project?.Name ?? "(Unknown Project)";
        }
        
        private void ShowSummaryDialog(string summaryText)
        {
            // Create a simple dialog to show the summary
            var dialog = new Window
            {
                Title = "Weekly Summary",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            
            // Create the content
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var textBox = new System.Windows.Controls.TextBox
            {
                Text = summaryText,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10),
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New, monospace"),
                AcceptsReturn = true
            };
            Grid.SetRow(textBox, 0);
            
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            Grid.SetRow(buttonPanel, 1);
            
            var copyButton = new Button
            {
                Content = "Copy to Clipboard",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            copyButton.Click += (s, e) => 
            {
                System.Windows.Clipboard.SetText(summaryText);
                MessageBox.Show("Summary copied to clipboard.", "Copy Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            
            var closeButton = new Button
            {
                Content = "Close",
                Padding = new Thickness(10, 5, 10, 5),
                IsDefault = true
            };
            closeButton.Click += (s, e) => dialog.Close();
            
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);
            
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            
            dialog.Content = grid;
            dialog.ShowDialog();
        }
    }
} 