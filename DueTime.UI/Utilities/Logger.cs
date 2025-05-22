using System;
using System.IO;
using System.Threading;

namespace DueTime.UI.Utilities
{
    /// <summary>
    /// Simple logging utility to write errors and information to a file
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = InitializeLogFilePath();
        private static readonly object _lockObj = new object();
        
        private static string InitializeLogFilePath()
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
                string logFilePath = Path.Combine(dir, $"DueTime_{date}.log");
                
                // Log startup information
                File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Application started{Environment.NewLine}");
                
                return logFilePath;
            }
            catch (Exception ex)
            {
                // If we can't initialize the logger, there's not much we can do
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
                // Return a fallback path
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DueTime", 
                    "Logs", 
                    "fallback.log");
            }
        }

        /// <summary>
        /// Logs a message to the log file with timestamp
        /// </summary>
        /// <param name="message">The message to log</param>
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
            catch (Exception)
            {
                // Fail silently on logging errors to avoid recursive issues
                // But output to debug console if available
                System.Diagnostics.Debug.WriteLine($"Failed to write to log: {message}");
            }
        }

        /// <summary>
        /// Logs an exception with detailed information
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="context">Optional context information</param>
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