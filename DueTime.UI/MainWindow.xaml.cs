using System;
using System.Windows;
using DueTime.Tracking;
using DueTime.Data;
using System.Threading.Tasks;
using System.Linq;

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
            
            // Create an adapter for the repository that converts between the two TimeEntry types
            var trackingRepo = new TrackingRepositoryAdapter(AppState.EntryRepo);
            
            _trackingService = new TrackingService(systemEvents, trackingRepo);
            _trackingService.TimeEntryRecorded += TrackingService_TimeEntryRecorded;
            _trackingService.Start();
        }

        private void TrackingService_TimeEntryRecorded(object? sender, TimeEntryRecordedEventArgs e)
        {
            // Convert from Tracking.TimeEntry to Data.TimeEntry
            var dataEntry = TimeEntryAdapter.ConvertToDataModel(e.Entry);
            
            // Add to UI collection
            Dispatcher.Invoke(() =>
            {
                AppState.Entries.Add(dataEntry);
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _trackingService?.Stop();
        }
    }

    /// <summary>
    /// Adapter class that implements Tracking.ITimeEntryRepository and delegates to Data.SQLiteTimeEntryRepository
    /// </summary>
    public class TrackingRepositoryAdapter : DueTime.Tracking.ITimeEntryRepository
    {
        private readonly DueTime.Data.SQLiteTimeEntryRepository _dataRepo;

        public TrackingRepositoryAdapter(DueTime.Data.SQLiteTimeEntryRepository dataRepo)
        {
            _dataRepo = dataRepo;
        }

        public async Task AddTimeEntryAsync(DueTime.Tracking.TimeEntry entry)
        {
            var dataEntry = TimeEntryAdapter.ConvertToDataModel(entry);
            await _dataRepo.AddTimeEntryAsync(dataEntry);
            // Copy ID back to original entry
            entry.Id = dataEntry.Id;
        }

        public async Task<List<DueTime.Tracking.TimeEntry>> GetEntriesByDateAsync(DateTime date)
        {
            var dataEntries = await _dataRepo.GetEntriesByDateAsync(date);
            return dataEntries.Select(TimeEntryAdapter.ConvertToTrackingModel).ToList();
        }

        public async Task<List<DueTime.Tracking.TimeEntry>> GetEntriesInRangeAsync(DateTime start, DateTime end)
        {
            var dataEntries = await _dataRepo.GetEntriesInRangeAsync(start, end);
            return dataEntries.Select(TimeEntryAdapter.ConvertToTrackingModel).ToList();
        }

        public Task UpdateEntryProjectAsync(int entryId, int? projectId)
        {
            return _dataRepo.UpdateEntryProjectAsync(entryId, projectId);
        }
    }
}