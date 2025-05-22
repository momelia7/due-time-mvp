using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace DueTime.Tracking.Services
{
    /// <summary>
    /// Windows-specific implementation of IExplorerService using UI Automation to get the active Explorer folder path.
    /// </summary>
    public class ExplorerService : IExplorerService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Gets the path of the folder open in the currently active Windows Explorer window.
        /// </summary>
        public string? GetActiveExplorerPath()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                // Get AutomationElement for the foreground window
                AutomationElement root = AutomationElement.FromHandle(hwnd);
                if (root == null) return null;

                // Ensure it's an Explorer window by class name
                if (!root.Current.ClassName.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase))
                    return null;

                // Find the address bar element by name (UI Automation Name property for Explorer address bar)
                var condition = new PropertyCondition(AutomationElement.NameProperty, "Address and search bar");
                AutomationElement addressBar = root.FindFirst(TreeScope.Descendants, condition);
                if (addressBar == null)
                {
                    return null;
                }

                // Get the ValuePattern from the address bar to retrieve text
                if (addressBar.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern valuePattern)
                {
                    string path = valuePattern.Current.Value;
                    return string.IsNullOrEmpty(path) ? null : path;
                }

                return null;
            }
            catch (Exception)
            {
                // In case of any unexpected errors (e.g. UIAutomation failure), return null
                return null;
            }
        }
    }
} 