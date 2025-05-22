using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using DueTime.UI.Utilities;

namespace DueTime.UI.Views
{
    /// <summary>
    /// Interaction logic for WindowPicker.xaml
    /// </summary>
    public partial class WindowPicker : Window
    {
        /// <summary>
        /// Represents information about a window for display and selection
        /// </summary>
        public class WindowInfo
        {
            public string Title { get; set; } = string.Empty;
            public string AppName { get; set; } = string.Empty;
            public IntPtr Handle { get; set; } = IntPtr.Zero;
            
            public override string ToString() => $"{Title} - {AppName}";
        }

        /// <summary>
        /// Gets the window selected by the user
        /// </summary>
        public WindowInfo? SelectedWindow { get; private set; }

        public WindowPicker()
        {
            InitializeComponent();
            
            try
            {
                // Load the list of windows
                List<WindowInfo> windows = GetOpenWindows();
                
                // Sort windows by application name then title
                windows.Sort((a, b) => {
                    int appCompare = string.Compare(a.AppName, b.AppName, StringComparison.OrdinalIgnoreCase);
                    return appCompare != 0 ? appCompare : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
                });
                
                // Set the list source
                WindowList.ItemsSource = windows;
                
                if (windows.Count > 0)
                {
                    WindowList.SelectedIndex = 0;
                }
                
                Logger.LogInfo($"Window picker loaded with {windows.Count} windows");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "WindowPicker initialization");
                System.Windows.MessageBox.Show(
                    "An error occurred while retrieving the list of open windows. Please try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets a list of all visible windows with titles
        /// </summary>
        private List<WindowInfo> GetOpenWindows()
        {
            var result = new List<WindowInfo>();
            
            try
            {
                // Use Win32 API EnumWindows to get all windows
                User32.EnumWindows((hWnd, lParam) =>
                {
                    // Only include visible windows with titles
                    if (User32.IsWindowVisible(hWnd))
                    {
                        int length = User32.GetWindowTextLength(hWnd);
                        if (length > 0)
                        {
                            StringBuilder builder = new StringBuilder(length + 1);
                            User32.GetWindowText(hWnd, builder, builder.Capacity);
                            string title = builder.ToString().Trim();
                            
                            // Ensure we have a title
                            if (!string.IsNullOrEmpty(title))
                            {
                                string appName = GetApplicationName(hWnd);
                                
                                // Don't include the WindowPicker itself
                                if (title != this.Title)
                                {
                                    result.Add(new WindowInfo { 
                                        Title = title, 
                                        AppName = appName,
                                        Handle = hWnd
                                    });
                                }
                            }
                        }
                    }
                    
                    // Continue enumeration
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "GetOpenWindows");
            }
            
            return result;
        }

        /// <summary>
        /// Gets the application name for the specified window handle
        /// </summary>
        private string GetApplicationName(IntPtr hWnd)
        {
            try
            {
                uint processId;
                User32.GetWindowThreadProcessId(hWnd, out processId);
                
                using var process = Process.GetProcessById((int)processId);
                try
                {
                    // Try to get the product name first
                    var fileVersionInfo = process.MainModule?.FileVersionInfo;
                    if (!string.IsNullOrEmpty(fileVersionInfo?.ProductName))
                    {
                        return fileVersionInfo.ProductName;
                    }
                    
                    // Fall back to executable name without extension
                    if (process.MainModule?.FileName != null)
                    {
                        return Path.GetFileNameWithoutExtension(process.MainModule.FileName);
                    }
                }
                catch
                {
                    // Fallback if we can't access process details (this can happen for system processes)
                    return process.ProcessName;
                }
                
                // Default
                return process.ProcessName;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to get application name for window: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Handles the OK button click
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedWindow = WindowList.SelectedItem as WindowInfo;
            
            if (SelectedWindow != null)
            {
                Logger.LogInfo($"Window selected: {SelectedWindow.Title} - {SelectedWindow.AppName}");
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Please select a window from the list.", 
                    "No Selection", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// Handles the Cancel button click
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogInfo("Window selection cancelled");
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// P/Invoke declarations for Win32 API functions 
        /// </summary>
        private static class User32
        {
            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
            
            [DllImport("user32.dll")]
            public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
            
            [DllImport("user32.dll")]
            public static extern bool IsWindowVisible(IntPtr hWnd);
            
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
            
            [DllImport("user32.dll")]
            public static extern int GetWindowTextLength(IntPtr hWnd);
            
            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        }
    }
} 