using System;
using System.Windows;
using System.Windows.Controls;
using DueTime.UI.ViewModels;

namespace DueTime.UI.Views
{
    public partial class AnalyticsView : System.Windows.Controls.UserControl
    {
        private AnalyticsViewModel? _viewModel;
        
        public AnalyticsView()
        {
            InitializeComponent();
            
            // We'll set the ViewModel in the Loaded event when repositories are available
            Loaded += AnalyticsView_Loaded;
        }
        
        private void AnalyticsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize ViewModel with repositories from AppState
            _viewModel = new AnalyticsViewModel(AppState.EntryRepo, AppState.ProjectRepo);
            DataContext = _viewModel;
        }
        
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Get the main window for status updates
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    await mainWindow.ShowStatusMessageAsync("Refreshing analytics data...", 0);
                }
                
                // Refresh data
                await _viewModel.LoadWeeklyDataAsync();
                
                // Clear status message
                if (mainWindow != null)
                {
                    await mainWindow.ShowStatusMessageAsync("Analytics data refreshed", 3000);
                }
            }
        }
    }
} 