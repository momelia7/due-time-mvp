using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
namespace DueTime.TrackingEngine.Services
{
    /// <summary>Windows implementation of ISystemEvents using Win32 hooks for foreground changes and idle detection.</summary>
    public class WindowsSystemEvents : ISystemEvents
    {
        public event EventHandler<ForegroundChangedEventArgs>? ForegroundChanged;
        public event EventHandler? IdleStarted;
        public event EventHandler? IdleEnded;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
                                               int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        private IntPtr _winEventHook = IntPtr.Zero;
        private WinEventDelegate? _winEventProc;
        private System.Timers.Timer? _idleTimer;
        private bool _isIdle = false;
        private readonly TimeSpan _idleThreshold;
        public WindowsSystemEvents(TimeSpan? idleThreshold = null)
        {
            _idleThreshold = idleThreshold ?? TimeSpan.FromMinutes(5);
        }
        public void Start()
        {
            // Hook foreground window change events
            _winEventProc = new WinEventDelegate(WinEventCallback);
            _winEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                                            _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            if (_winEventHook == IntPtr.Zero)
                throw new InvalidOperationException("Failed to set WinEvent hook.");
            // Start idle detection timer
            _isIdle = false;
            _idleTimer = new System.Timers.Timer(1000);
            _idleTimer.Elapsed += (s, e) => CheckIdleStatus();
            _idleTimer.AutoReset = true;
            _idleTimer.Start();
        }
        public void Stop()
        {
            _idleTimer?.Stop(); _idleTimer?.Dispose(); _idleTimer = null;
            if (_winEventHook != IntPtr.Zero) { UnhookWinEvent(_winEventHook); _winEventHook = IntPtr.Zero; }
            _winEventProc = null;
        }
        private void CheckIdleStatus()
        {
            uint lastInputTick = GetLastInputTick();
            uint nowTick = (uint)Environment.TickCount;
            uint inactiveTicks = nowTick >= lastInputTick ? nowTick - lastInputTick
                                                          : uint.MaxValue - lastInputTick + nowTick;
            TimeSpan inactive = TimeSpan.FromMilliseconds(inactiveTicks);
            if (!_isIdle && inactive >= _idleThreshold)
            {
                _isIdle = true;
                IdleStarted?.Invoke(this, EventArgs.Empty);
            }
            else if (_isIdle && inactive < _idleThreshold)
            {
                _isIdle = false;
                IdleEnded?.Invoke(this, EventArgs.Empty);
            }
        }
        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
                                      int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType != EVENT_SYSTEM_FOREGROUND || hwnd == IntPtr.Zero) return;
            try
            {
                string title = GetWindowTitle(hwnd);
                string app = GetProcessName(hwnd);
                ForegroundChanged?.Invoke(this, new ForegroundChangedEventArgs(hwnd, title, app));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WinEventCallback error: " + ex.Message);
            }
        }
        // P/Invoke for Win32 APIs
        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [StructLayout(LayoutKind.Sequential)] private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
        private uint GetLastInputTick()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
            if (!GetLastInputInfo(ref lii)) throw new InvalidOperationException("GetLastInputInfo failed.");
            return lii.dwTime;
        }
        private string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            var sb = new System.Text.StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        private string GetProcessName(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            try { return Process.GetProcessById((int)pid).ProcessName; }
            catch { return string.Empty; }
        }
    }
} 