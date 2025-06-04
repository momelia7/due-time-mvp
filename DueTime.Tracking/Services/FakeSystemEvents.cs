using System;
using System.Threading;
using System.Threading.Tasks;

namespace DueTime.Tracking.Services
{
    /// <summary>
    /// A fake implementation of ISystemEvents for testing purposes
    /// </summary>
    public class FakeSystemEvents : ISystemEvents
    {
        public event EventHandler<WindowChangedEventArgs>? WindowChanged;
        public event EventHandler<IdleStateChangedEventArgs>? IdleStateChanged;
        
        private bool _isRunning = false;
        private CancellationTokenSource? _cts;
        private Task? _simulationTask;
        
        /// <summary>
        /// Starts the fake system events
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Stops the fake system events
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            _simulationTask?.Wait();
            _simulationTask = null;
        }
        
        /// <summary>
        /// Simulates a window change event
        /// </summary>
        public void SimulateWindowChange(string windowTitle, string applicationName)
        {
            if (!_isRunning) return;
            
            WindowChanged?.Invoke(this, new WindowChangedEventArgs(windowTitle, applicationName));
        }
        
        /// <summary>
        /// Simulates an idle state change event
        /// </summary>
        public void SimulateIdleStateChange(bool isIdle)
        {
            if (!_isRunning) return;
            
            IdleStateChanged?.Invoke(this, new IdleStateChangedEventArgs(isIdle));
        }
        
        /// <summary>
        /// Simulates a sequence of window changes and idle states
        /// </summary>
        public async Task SimulateActivitySequenceAsync(TimeSpan totalDuration, TimeSpan intervalBetweenEvents)
        {
            if (!_isRunning) return;
            
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            
            _simulationTask = Task.Run(async () =>
            {
                DateTime startTime = DateTime.Now;
                DateTime endTime = startTime + totalDuration;
                
                // Sample window titles and applications
                string[] windowTitles = new[]
                {
                    "Document1 - Microsoft Word",
                    "Inbox - Outlook",
                    "DueTime Project - Visual Studio",
                    "Budget.xlsx - Excel",
                    "Meeting Notes - OneNote",
                    "Google - Chrome"
                };
                
                string[] applications = new[]
                {
                    "WINWORD",
                    "OUTLOOK",
                    "devenv",
                    "EXCEL",
                    "ONENOTE",
                    "chrome"
                };
                
                int index = 0;
                bool isIdle = false;
                
                while (DateTime.Now < endTime && !token.IsCancellationRequested)
                {
                    // Simulate window change
                    if (!isIdle)
                    {
                        SimulateWindowChange(windowTitles[index % windowTitles.Length], 
                                           applications[index % applications.Length]);
                        index++;
                    }
                    
                    // Occasionally simulate idle
                    if (new Random().Next(10) == 0)
                    {
                        isIdle = !isIdle;
                        SimulateIdleStateChange(isIdle);
                        
                        // If coming back from idle, wait a bit before simulating window change
                        if (!isIdle)
                        {
                            await Task.Delay(1000, token);
                        }
                    }
                    
                    // Wait between events
                    await Task.Delay(intervalBetweenEvents, token);
                }
            }, token);
            
            await _simulationTask;
        }
    }
} 