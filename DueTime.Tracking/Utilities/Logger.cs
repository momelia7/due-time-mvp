using System;
using System.Diagnostics;
using System.IO;

namespace DueTime.Tracking.Utilities
{
    /// <summary>
    /// Simple logging utility for the tracking engine
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath;
        private static readonly object _lockObj = new object();
        
        static Logger()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(appData, "DueTime", "Logs");
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                // Set the log file path with date-based filename
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                LogFilePath = Path.Combine(dir, $"DueTime_{date}.log");
                
                // Log startup information
                Log("Tracking engine initialized");
            }
            catch (Exception ex)
            {
                // If we can't initialize the logger, output to debug console
                Debug.WriteLine($"Failed to initialize tracking logger: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Logs a message to the log file with timestamp
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                lock (_lockObj) // Thread-safe logging
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, logEntry);
                }
            }
            catch (Exception ex)
            {
                // Fail silently on logging errors, but output to debug console
                Debug.WriteLine($"Failed to write to tracking log: {message} - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Logs an exception with detailed information
        /// </summary>
        public static void LogException(Exception ex, string context = "")
        {
            string message = string.IsNullOrEmpty(context) 
                ? $"Exception: {ex.GetType().Name}"
                : $"Exception in {context}: {ex.GetType().Name}";
                
            message += $"\nMessage: {ex.Message}";
            message += $"\nStackTrace: {ex.StackTrace}";
            
            // Include inner exception info if available
            if (ex.InnerException != null)
            {
                message += $"\nInner Exception: {ex.InnerException.GetType().Name}";
                message += $"\nInner Message: {ex.InnerException.Message}";
            }
            
            Log(message);
        }
        
        /// <summary>
        /// Logs information message
        /// </summary>
        public static void LogInfo(string message)
        {
            Log($"INFO: {message}");
        }
        
        /// <summary>
        /// Logs warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            Log($"WARNING: {message}");
        }
        
        /// <summary>
        /// Logs error message
        /// </summary>
        public static void LogError(string message)
        {
            Log($"ERROR: {message}");
        }
    }
} 