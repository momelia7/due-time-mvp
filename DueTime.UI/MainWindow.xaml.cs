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
            var repo = new SQLiteTimeEntryRepository();
            _trackingService = new TrackingService(systemEvents, repo);
            _trackingService.Start();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _trackingService?.Stop();
        }
    }
}