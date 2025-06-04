using System;
using System.IO;
using System.ServiceProcess;
using DueTime.Data;
using DueTime.Tracking;

namespace DueTime.Service
{
    public class TrackerService : ServiceBase
    {
        private readonly string _logFile;
        private ITrackingService? _trackingService;
        private ISystemEvents? _systemEvents;
        
        public TrackerService(string logFile)
        {
            _logFile = logFile;
            
            // Configure service properties
#pragma warning disable CA1416 // Validate platform compatibility
            ServiceName = "DueTimeTracker";
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
#pragma warning restore CA1416 // Validate platform compatibility
        }
        
        protected override void OnStart(string[] args)
        {
            try
            {
                StartTracker();
                LogMessage("Service started successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error starting service: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        protected override void OnStop()
        {
            try
            {
                StopTracker();
                LogMessage("Service stopped successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping service: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        protected override void OnPause()
        {
            try
            {
                _trackingService?.Stop();
                LogMessage("Service paused");
            }
            catch (Exception ex)
            {
                LogMessage($"Error pausing service: {ex.Message}");
                throw;
            }
        }
        
        protected override void OnContinue()
        {
            try
            {
                _trackingService?.Start();
                LogMessage("Service continued");
            }
            catch (Exception ex)
            {
                LogMessage($"Error continuing service: {ex.Message}");
                throw;
            }
        }
        
        public void StartTracker()
        {
            // Initialize database
            Database.InitializeSchema();
            
            // Create repository
            var entryRepo = new SQLiteTimeEntryRepository();
            
            // Create system events
            _systemEvents = new WindowsSystemEvents();
            
            // Create tracking service
            _trackingService = new TrackingService(_systemEvents, entryRepo);
            
            // Subscribe to events
            _trackingService.TimeEntryRecorded += TrackingService_TimeEntryRecorded;
            
            // Start tracking
            _trackingService.Start();
            
            LogMessage("Tracking started");
        }
        
        public void StopTracker()
        {
            if (_trackingService != null)
            {
                // Unsubscribe from events
                _trackingService.TimeEntryRecorded -= TrackingService_TimeEntryRecorded;
                
                // Stop tracking
                _trackingService.Stop();
                
                LogMessage("Tracking stopped");
            }
        }
        
        private void TrackingService_TimeEntryRecorded(object? sender, TimeEntryRecordedEventArgs e)
        {
            try
            {
                var entry = e.Entry;
                LogMessage($"Entry recorded: {entry.ApplicationName} - {entry.WindowTitle} ({entry.StartTime:HH:mm:ss} to {entry.EndTime:HH:mm:ss})");
                
                // Here we could implement AI categorization if needed
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing time entry: {ex.Message}");
            }
        }
        
        private void LogMessage(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                File.AppendAllText(_logFile, logEntry);
            }
            catch
            {
                // Ignore logging errors to prevent cascading failures
            }
        }
    }
} 