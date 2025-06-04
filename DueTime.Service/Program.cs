using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DueTime.Data;
using DueTime.Tracking;

namespace DueTime.Service
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // Setup logging
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DueTime",
                "Logs");
                
            // Ensure log directory exists
            Directory.CreateDirectory(logPath);
            
            // Configure full log path
            string logFile = Path.Combine(logPath, "duetime-service.log");
            
            // Log startup
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Service starting\n");
            
            try
            {
                // If running with --console argument, run as console app for debugging
                if (args.Length > 0 && args[0] == "--console")
                {
                    Console.WriteLine("Running in console mode");
                    TestConsole.RunTest();
                }
                else if (args.Length > 0 && args[0] == "--test")
                {
                    // Run test console
                    Console.WriteLine("Running test console");
                    TestConsole.RunTest();
                }
                else
                {
                    // Run as service
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new TrackerService(logFile)
                    };
#pragma warning disable CA1416 // Validate platform compatibility
                    ServiceBase.Run(ServicesToRun);
#pragma warning restore CA1416 // Validate platform compatibility
                }
            }
            catch (Exception ex)
            {
                // Log any startup exceptions
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n{ex.StackTrace}\n");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        
        private static void RunAsConsole(string logFile)
        {
            Console.WriteLine("DueTime Tracking Service running in console mode");
            Console.WriteLine($"Logs will be written to: {logFile}");
            Console.WriteLine("Press Ctrl+C to exit");
            
            // Create and start the tracker service
            var service = new TrackerService(logFile);
            service.StartTracker();
            
            // Wait for Ctrl+C
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) => 
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
            
            // Wait for exit signal
            exitEvent.WaitOne();
            
            // Stop the service
            service.StopTracker();
            Console.WriteLine("Service stopped");
        }
    }
}
