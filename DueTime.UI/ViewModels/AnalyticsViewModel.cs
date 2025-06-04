using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DueTime.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Text;

namespace DueTime.UI.ViewModels
{
    public class AnalyticsViewModel : INotifyPropertyChanged
    {
        private readonly ITimeEntryRepository _timeEntryRepo;
        private readonly IProjectRepository _projectRepo;
        
        // Properties for charts
        private ISeries[] _projectSeries = Array.Empty<ISeries>();
        public ISeries[] ProjectSeries
        {
            get => _projectSeries;
            set
            {
                _projectSeries = value;
                OnPropertyChanged(nameof(ProjectSeries));
            }
        }
        
        private ISeries[] _dailyActivitySeries = Array.Empty<ISeries>();
        public ISeries[] DailyActivitySeries
        {
            get => _dailyActivitySeries;
            set
            {
                _dailyActivitySeries = value;
                OnPropertyChanged(nameof(DailyActivitySeries));
            }
        }
        
        private string[] _projectLabels = Array.Empty<string>();
        public string[] ProjectLabels
        {
            get => _projectLabels;
            set
            {
                _projectLabels = value;
                OnPropertyChanged(nameof(ProjectLabels));
            }
        }
        
        private string[] _weekdayLabels = Array.Empty<string>();
        public string[] WeekdayLabels
        {
            get => _weekdayLabels;
            set
            {
                _weekdayLabels = value;
                OnPropertyChanged(nameof(WeekdayLabels));
            }
        }
        
        // Statistics properties
        private string _totalTrackedTime = "0h 0m";
        public string TotalTrackedTime
        {
            get => _totalTrackedTime;
            set
            {
                _totalTrackedTime = value;
                OnPropertyChanged(nameof(TotalTrackedTime));
            }
        }
        
        private string _mostActiveProject = "None";
        public string MostActiveProject
        {
            get => _mostActiveProject;
            set
            {
                _mostActiveProject = value;
                OnPropertyChanged(nameof(MostActiveProject));
            }
        }
        
        private string _mostActiveDay = "None";
        public string MostActiveDay
        {
            get => _mostActiveDay;
            set
            {
                _mostActiveDay = value;
                OnPropertyChanged(nameof(MostActiveDay));
            }
        }
        
        private int _entryCount = 0;
        public int EntryCount
        {
            get => _entryCount;
            set
            {
                _entryCount = value;
                OnPropertyChanged(nameof(EntryCount));
            }
        }
        
        // AI Insights properties
        private string _aiInsights = "Enable AI features in Settings to view productivity insights.";
        public string AIInsights
        {
            get => _aiInsights;
            set
            {
                _aiInsights = value;
                OnPropertyChanged(nameof(AIInsights));
            }
        }
        
        private bool _isLoadingInsights = false;
        public bool IsLoadingInsights
        {
            get => _isLoadingInsights;
            set
            {
                _isLoadingInsights = value;
                OnPropertyChanged(nameof(IsLoadingInsights));
            }
        }
        
        private bool _showAIInsights = false;
        public bool ShowAIInsights
        {
            get => _showAIInsights;
            set
            {
                _showAIInsights = value;
                OnPropertyChanged(nameof(ShowAIInsights));
            }
        }
        
        public AnalyticsViewModel(ITimeEntryRepository timeEntryRepo, IProjectRepository projectRepo)
        {
            _timeEntryRepo = timeEntryRepo;
            _projectRepo = projectRepo;
            
            // Set default labels
            WeekdayLabels = new string[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            
            // Check if AI is enabled
            ShowAIInsights = AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext);
            
            // Load data for the past week
            _ = LoadWeeklyDataAsync();
        }
        
        public async Task LoadWeeklyDataAsync()
        {
            try
            {
                // Get data for the past week
                DateTime today = DateTime.Today;
                DateTime weekStart = today.AddDays(-6); // Last 7 days including today
                
                var entries = await _timeEntryRepo.GetEntriesInRangeAsync(weekStart, today.AddDays(1));
                var projects = await _projectRepo.GetAllProjectsAsync();
                
                // Filter out entries without end time
                var validEntries = entries.Where(e => e.EndTime != DateTime.MinValue).ToList();
                
                if (validEntries.Count > 0)
                {
                    UpdateProjectTimeChart(validEntries, projects);
                    UpdateDailyActivityChart(validEntries, weekStart, today);
                    UpdateStatistics(validEntries, projects);
                    
                    // Get AI insights if enabled
                    if (AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
                    {
                        ShowAIInsights = true;
                        await GenerateAIInsightsAsync(validEntries, projects, weekStart, today);
                    }
                    else
                    {
                        ShowAIInsights = false;
                    }
                }
                else
                {
                    // No data, set default empty charts
                    ProjectSeries = new ISeries[] { new PieSeries<double> { Values = new double[] { 1 }, InnerRadius = 50, Fill = new SolidColorPaint(SKColors.Gray) } };
                    ProjectLabels = new[] { "No Data" };
                    
                    DailyActivitySeries = new ISeries[] { new ColumnSeries<double> { Values = new double[7] } };
                    
                    TotalTrackedTime = "0h 0m";
                    MostActiveProject = "None";
                    MostActiveDay = "None";
                    EntryCount = 0;
                    
                    AIInsights = "No time tracking data available for insights.";
                }
            }
            catch (Exception ex)
            {
                // In a real app, log this error
                System.Diagnostics.Debug.WriteLine($"Error loading analytics data: {ex.Message}");
            }
        }
        
        private async Task GenerateAIInsightsAsync(List<TimeEntry> entries, List<Project> projects, DateTime startDate, DateTime endDate)
        {
            if (!AppState.AIEnabled || string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
            {
                AIInsights = "Enable AI features in Settings to view productivity insights.";
                return;
            }
            
            try
            {
                IsLoadingInsights = true;
                AIInsights = "Analyzing your productivity patterns...";
                
                // Create a data summary for the AI
                StringBuilder dataBuilder = new StringBuilder();
                dataBuilder.AppendLine($"Time period: {startDate.ToShortDateString()} to {endDate.ToShortDateString()}");
                dataBuilder.AppendLine();
                
                // Project breakdown
                dataBuilder.AppendLine("Project time breakdown:");
                var projectGroups = entries
                    .Where(e => e.EndTime != DateTime.MinValue)
                    .GroupBy(e => e.ProjectId ?? -1)
                    .Select(g => new
                    {
                        ProjectId = g.Key,
                        TotalHours = g.Sum(e => (e.EndTime - e.StartTime).TotalHours)
                    })
                    .OrderByDescending(p => p.TotalHours)
                    .ToList();
                
                foreach (var proj in projectGroups)
                {
                    string projectName;
                    if (proj.ProjectId == -1)
                    {
                        projectName = "(No Project)";
                    }
                    else
                    {
                        var project = projects.FirstOrDefault(p => p.ProjectId == proj.ProjectId);
                        projectName = project?.Name ?? "Unknown Project";
                    }
                    dataBuilder.AppendLine($"- {projectName}: {proj.TotalHours:F1} hours");
                }
                dataBuilder.AppendLine();
                
                // Daily breakdown
                dataBuilder.AppendLine("Daily time breakdown:");
                var dailyGroups = entries
                    .Where(e => e.EndTime != DateTime.MinValue)
                    .GroupBy(e => e.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        DayOfWeek = g.Key.DayOfWeek,
                        TotalHours = g.Sum(e => (e.EndTime - e.StartTime).TotalHours)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();
                
                foreach (var day in dailyGroups)
                {
                    dataBuilder.AppendLine($"- {day.Date.ToShortDateString()} ({day.DayOfWeek}): {day.TotalHours:F1} hours");
                }
                dataBuilder.AppendLine();
                
                // Application usage
                dataBuilder.AppendLine("Top applications:");
                var appGroups = entries
                    .Where(e => e.EndTime != DateTime.MinValue)
                    .GroupBy(e => e.ApplicationName)
                    .Select(g => new
                    {
                        Application = g.Key,
                        TotalHours = g.Sum(e => (e.EndTime - e.StartTime).TotalHours)
                    })
                    .OrderByDescending(a => a.TotalHours)
                    .Take(5)
                    .ToList();
                
                foreach (var app in appGroups)
                {
                    dataBuilder.AppendLine($"- {app.Application}: {app.TotalHours:F1} hours");
                }
                
                // Get insights from OpenAI
                string? insights = await OpenAIClient.GetTimeInsightsAsync(
                    dataBuilder.ToString(), 
                    AppState.ApiKeyPlaintext);
                
                if (!string.IsNullOrEmpty(insights))
                {
                    AIInsights = insights;
                }
                else
                {
                    AIInsights = "Unable to generate insights at this time. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                AIInsights = "Error generating insights. Please try again later.";
                System.Diagnostics.Debug.WriteLine($"Error generating AI insights: {ex.Message}");
            }
            finally
            {
                IsLoadingInsights = false;
            }
        }
        
        private void UpdateProjectTimeChart(List<TimeEntry> entries, List<Project> projects)
        {
            // Group entries by project and calculate total minutes
            var projectTotals = entries
                .GroupBy(e => e.ProjectId ?? -1) // -1 for no project
                .Select(g => new 
                {
                    ProjectId = g.Key,
                    TotalMinutes = g.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes)
                })
                .OrderByDescending(p => p.TotalMinutes)
                .ToList();
            
            // Prepare data for pie chart
            var projectValues = new List<double>();
            var projectNames = new List<string>();
            var projectColors = new List<SKColor>
            {
                SKColors.DodgerBlue,
                SKColors.Orange,
                SKColors.MediumSeaGreen,
                SKColors.Crimson,
                SKColors.BlueViolet,
                SKColors.Gold,
                SKColors.Teal
            };
            
            // Create color dictionary for projects
            Dictionary<int, SKColor> projectColorMap = new Dictionary<int, SKColor>();
            for (int i = 0; i < projects.Count && i < projectColors.Count; i++)
            {
                projectColorMap[projects[i].ProjectId] = projectColors[i % projectColors.Count];
            }
            
            // Ensure "No Project" has a color
            projectColorMap[-1] = SKColors.Gray;
            
            foreach (var projectTotal in projectTotals)
            {
                string projectName;
                if (projectTotal.ProjectId == -1)
                {
                    projectName = "(No Project)";
                }
                else
                {
                    var project = projects.FirstOrDefault(p => p.ProjectId == projectTotal.ProjectId);
                    projectName = project?.Name ?? "Unknown Project";
                }
                
                projectValues.Add(projectTotal.TotalMinutes);
                projectNames.Add(projectName);
            }
            
            // Create pie series
            var pieSeries = new List<ISeries>();
            for (int i = 0; i < projectValues.Count; i++)
            {
                var projectId = projectTotals[i].ProjectId;
                var color = projectColorMap.ContainsKey(projectId) 
                    ? projectColorMap[projectId] 
                    : projectColors[i % projectColors.Count];
                
                pieSeries.Add(new PieSeries<double>
                {
                    Values = new double[] { projectValues[i] },
                    Name = projectNames[i],
                    Fill = new SolidColorPaint(color),
                    InnerRadius = 50
                });
            }
            
            ProjectSeries = pieSeries.ToArray();
            ProjectLabels = projectNames.ToArray();
        }
        
        private void UpdateDailyActivityChart(List<TimeEntry> entries, DateTime weekStart, DateTime today)
        {
            // Group entries by day and calculate total hours
            var dailyTotals = new double[7];
            
            // Fill with 0 to ensure all days have a value
            for (int i = 0; i < 7; i++)
            {
                dailyTotals[i] = 0;
            }
            
            foreach (var entry in entries)
            {
                int dayIndex = (int)(entry.StartTime.Date - weekStart.Date).TotalDays;
                if (dayIndex >= 0 && dayIndex < 7)
                {
                    dailyTotals[dayIndex] += (entry.EndTime - entry.StartTime).TotalHours;
                }
            }
            
            // Create day labels
            var dayLabels = new string[7];
            for (int i = 0; i < 7; i++)
            {
                DateTime date = weekStart.AddDays(i);
                dayLabels[i] = date.ToString("ddd");
            }
            
            // Create column series
            var columnSeries = new ColumnSeries<double>
            {
                Values = dailyTotals,
                Fill = new SolidColorPaint(SKColors.DodgerBlue)
            };
            
            DailyActivitySeries = new ISeries[] { columnSeries };
            WeekdayLabels = dayLabels;
        }
        
        private void UpdateStatistics(List<TimeEntry> entries, List<Project> projects)
        {
            // Calculate total tracked time
            var totalMinutes = entries.Sum(e => (e.EndTime - e.StartTime).TotalMinutes);
            int hours = (int)(totalMinutes / 60);
            int minutes = (int)(totalMinutes % 60);
            TotalTrackedTime = $"{hours}h {minutes}m";
            
            // Find most active project
            if (entries.Count > 0)
            {
                // Group by project and find the one with most time
                var mostActiveProjectGroup = entries
                    .GroupBy(e => e.ProjectId)
                    .Select(g => new
                    {
                        ProjectId = g.Key,
                        TotalMinutes = g.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes)
                    })
                    .OrderByDescending(p => p.TotalMinutes)
                    .FirstOrDefault();
                
                if (mostActiveProjectGroup != null)
                {
                    if (mostActiveProjectGroup.ProjectId == null)
                    {
                        MostActiveProject = "(No Project)";
                    }
                    else
                    {
                        var project = projects.FirstOrDefault(p => p.ProjectId == mostActiveProjectGroup.ProjectId);
                        MostActiveProject = project?.Name ?? "Unknown Project";
                    }
                }
                
                // Find most active day
                var mostActiveDay = entries
                    .GroupBy(e => e.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalMinutes = g.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes)
                    })
                    .OrderByDescending(p => p.TotalMinutes)
                    .FirstOrDefault();
                
                if (mostActiveDay != null)
                {
                    MostActiveDay = mostActiveDay.Date.ToString("dddd");
                }
                
                EntryCount = entries.Count;
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