using System;
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
        }

        private void TrackingService_TimeEntryRecorded(object? sender, TimeEntryRecordedEventArgs e)
        {
            // Add to UI collection
            Dispatcher.Invoke(() =>
            {
                AppState.Entries.Add(e.Entry);
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _trackingService?.Stop();
        }
    }
} 