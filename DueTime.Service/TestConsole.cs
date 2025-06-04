using System;
using System.Threading;
using DueTime.Tracking;
using DueTime.Data;

namespace DueTime.Service
{
    public class TestConsole
    {
        public static void RunTest()
        {
            Console.WriteLine("DueTime Test Console");
            Console.WriteLine("===================");
            Console.WriteLine("Starting tracking engine...");
            
            try
            {
                // Initialize database
                Database.InitializeSchema();
                Console.WriteLine("Database initialized.");
                
                // Create repository
                var entryRepo = new SQLiteTimeEntryRepository();
                Console.WriteLine("Repository created.");
                
                // Create system events
                var systemEvents = new WindowsSystemEvents();
                Console.WriteLine("System events initialized.");
                
                // Create tracking service
                var trackingService = new TrackingService(systemEvents, entryRepo);
                
                // Subscribe to events
                trackingService.TimeEntryRecorded += (sender, e) =>
                {
                    var entry = e.Entry;
                    Console.WriteLine($"Entry recorded: {entry.ApplicationName} - {entry.WindowTitle}");
                };
                
                Console.WriteLine("Starting tracking...");
                
                // Start tracking
                trackingService.Start();
                
                Console.WriteLine("Tracking started. Press Enter to stop.");
                Console.ReadLine();
                
                // Stop tracking
                trackingService.Stop();
                Console.WriteLine("Tracking stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
} 