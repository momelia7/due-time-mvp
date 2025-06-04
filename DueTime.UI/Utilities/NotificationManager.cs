using System;
using System.Windows;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace DueTime.UI.Utilities
{
    /// <summary>
    /// Manages application notifications using toast-style messages and tray balloon tips
    /// </summary>
    public static class NotificationManager
    {
        private static NotifyIcon? _notifyIcon;
        
        /// <summary>
        /// Initializes the notification manager with a NotifyIcon
        /// </summary>
        public static void Initialize(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
        }
        
        /// <summary>
        /// Shows an information notification
        /// </summary>
        public static void ShowInfo(string message, string? title = null)
        {
            ShowNotification(message, title ?? "Information", ToolTipIcon.Info);
            Logger.LogInfo($"Notification: {message}");
        }
        
        /// <summary>
        /// Shows a warning notification
        /// </summary>
        public static void ShowWarning(string message, string? title = null)
        {
            ShowNotification(message, title ?? "Warning", ToolTipIcon.Warning);
            Logger.LogWarning($"Warning notification: {message}");
        }
        
        /// <summary>
        /// Shows an error notification
        /// </summary>
        public static void ShowError(string message, string? title = null)
        {
            ShowNotification(message, title ?? "Error", ToolTipIcon.Error);
            Logger.LogError($"Error notification: {message}");
        }
        
        /// <summary>
        /// Shows a notification about AI suggestions
        /// </summary>
        public static void ShowSuggestion(string message, string? title = null)
        {
            ShowNotification(message, title ?? "AI Suggestion", ToolTipIcon.Info);
            Logger.LogInfo($"AI Suggestion: {message}");
        }
        
        /// <summary>
        /// Shows a notification about repeated AI suggestion overrides
        /// </summary>
        public static void ShowSuggestionPattern(string suggestedProject, string chosenProject)
        {
            string message = $"You've repeatedly changed AI suggestions from '{suggestedProject}' to '{chosenProject}'. Consider creating an auto-assignment rule.";
            ShowNotification(message, "AI Learning", ToolTipIcon.Info);
            Logger.LogInfo($"AI Pattern detected: {suggestedProject} -> {chosenProject}");
        }
        
        /// <summary>
        /// Shows a notification using the system tray icon
        /// </summary>
        private static void ShowNotification(string message, string title, ToolTipIcon icon)
        {
            if (_notifyIcon == null)
            {
                // Fall back to message box if notify icon not available
                System.Windows.MessageBox.Show(message, title);
                return;
            }
            
            try
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.BalloonTipIcon = icon;
                _notifyIcon.ShowBalloonTip(5000); // Show for 5 seconds
            }
            catch (Exception ex)
            {
                // Log the error but don't crash
                Logger.LogException(ex, "ShowNotification");
                
                // Fall back to message box
                System.Windows.MessageBox.Show(message, title);
            }
        }
        
        /// <summary>
        /// Shows a notification in the application's status bar
        /// </summary>
        public static async Task ShowStatusAsync(string message, int durationMs = 3000)
        {
            // Find the main window
            var mainWindow = System.Windows.Application.Current?.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                await mainWindow.ShowStatusMessageAsync(message, durationMs);
            }
        }
    }
} 