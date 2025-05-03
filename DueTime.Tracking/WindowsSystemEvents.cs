using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DueTime.Tracking
{
    /// <summary>Implementation of system events using Windows API hooks.</summary>
    public class WindowsSystemEvents : ISystemEvents, IDisposable
    {
        public event EventHandler<WindowChangedEventArgs>? WindowChanged;
        public event EventHandler<IdleStateChangedEventArgs>? IdleStateChanged;
        
        private CancellationTokenSource? _cts;
        private Task? _monitorTask;
        private string _lastWindowTitle = string.Empty;
        private string _lastAppName = string.Empty;
        private bool _isIdle = false;
        private const int IdleThresholdMs = 300000; // 5 minutes
        
        // P/Invoke declarations for Windows API
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        
        public void Start()
        {
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(MonitorWindowChanges, _cts.Token);
        }
        
        public void Stop()
        {
            _cts?.Cancel();
            _monitorTask?.Wait();
            _monitorTask = null;
        }
        
        private async Task MonitorWindowChanges()
        {
            while (!_cts!.IsCancellationRequested)
            {
                CheckIdleState();
                
                if (!_isIdle)
                {
                    CheckActiveWindow();
                }
                
                await Task.Delay(1000); // Check every second
            }
        }
        
        private void CheckActiveWindow()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;
                
                // Get window title
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(hwnd, sb, sb.Capacity);
                string title = sb.ToString();
                
                // Get process name
                GetWindowThreadProcessId(hwnd, out uint processId);
                string appName = "Unknown";
                
                try
                {
                    using Process process = Process.GetProcessById((int)processId);
                    appName = process.ProcessName;
                }
                catch
                {
                    // Process might have terminated
                }
                
                // Raise event only if window changed
                if (title != _lastWindowTitle || appName != _lastAppName)
                {
                    _lastWindowTitle = title;
                    _lastAppName = appName;
                    WindowChanged?.Invoke(this, new WindowChangedEventArgs(title, appName));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking active window: {ex.Message}");
            }
        }
        
        private void CheckIdleState()
        {
            try
            {
                LASTINPUTINFO lii = new LASTINPUTINFO();
                lii.cbSize = (uint)Marshal.SizeOf(lii);
                
                if (GetLastInputInfo(ref lii))
                {
                    uint idleTime = ((uint)Environment.TickCount - lii.dwTime);
                    bool isCurrentlyIdle = idleTime >= IdleThresholdMs;
                    
                    if (isCurrentlyIdle != _isIdle)
                    {
                        _isIdle = isCurrentlyIdle;
                        IdleStateChanged?.Invoke(this, new IdleStateChangedEventArgs(_isIdle));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking idle state: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
} 