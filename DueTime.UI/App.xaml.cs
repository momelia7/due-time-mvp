using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DueTime.Data;
using DueTime.Tracking;
using DueTime.UI.Utilities;
using Microsoft.Win32;

namespace DueTime.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        // Trial period in days
        public const int TRIAL_DAYS = 30;
        
        // Flag to indicate we're intentionally shutting down
        public bool IsShuttingDown { get; set; } = false;
        
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Set up global exception handlers for UI and background threads
            SetupExceptionHandling();
            
            // Initialize database and load data
            Logger.LogInfo("Initializing database schema");
            Database.InitializeSchema();
            
            // Check for saved API key and set AI enabled flag accordingly
            AppState.ApiKeyPlaintext = SecureStorage.LoadApiKey();
            AppState.AIEnabled = (AppState.ApiKeyPlaintext != null);
            Logger.LogInfo($"AI features enabled: {AppState.AIEnabled}");
            
            // Check if app is set to run on startup
            try
            {
                var runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                AppState.RunOnStartup = runKey?.GetValue("DueTime") != null;
                Logger.LogInfo($"Run on startup: {AppState.RunOnStartup}");
            }
            catch (Exception ex)
            {
                // If we can't access registry, default to false
                AppState.RunOnStartup = false;
                Logger.LogException(ex, "Checking startup registry");
            }
            
            // Load dark mode setting
            string? darkModeSetting = SettingsManager.GetSetting("EnableDarkMode");
            AppState.EnableDarkMode = darkModeSetting == "True";
            
            // Apply theme based on setting
            if (AppState.EnableDarkMode)
            {
                ApplyDarkTheme();
                Logger.LogInfo("Applied dark theme");
            }
            else
            {
                ApplyLightTheme();
                Logger.LogInfo("Applied light theme");
            }
            
            try
            {
                // Load projects
                var projList = AppState.ProjectRepo.GetAllProjectsAsync().Result;
                AppState.Projects.Clear();
                foreach(var proj in projList)
                    AppState.Projects.Add(proj);
                Logger.LogInfo($"Loaded {AppState.Projects.Count} projects");
                
                // Load rules
                var ruleList = AppState.RuleRepo.GetAllRulesAsync().Result;
                AppState.Rules.Clear();
                foreach(var rule in ruleList)
                    AppState.Rules.Add(rule);
                Logger.LogInfo($"Loaded {AppState.Rules.Count} rules");
                
                // Load today's entries
                var entryList = AppState.EntryRepo.GetEntriesByDateAsync(DateTime.Today).Result;
                AppState.Entries.Clear();
                foreach(var entry in entryList)
                    AppState.Entries.Add(entry);
                Logger.LogInfo($"Loaded {AppState.Entries.Count} time entries for today");
                    
                // Detect first run
                AppState.IsFirstRun = (AppState.Projects.Count == 0 && AppState.Entries.Count == 0);
                if (AppState.IsFirstRun)
                {
                    Logger.LogInfo("First run detected - showing welcome message");
                    // Show first-run dialog after main window is loaded (dispatch to end of UI queue)
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(ShowFirstRunDialog));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Loading application data");
                MessageBox.Show(
                    "There was a problem loading application data. Please restart the application or contact support if the issue persists.",
                    "Data Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            
            // Handle trial period and license
            CheckTrialAndLicense();
            
            // Configure AI for TrackingService if it's available
            if (AppState.TrackingService != null && AppState.AIEnabled && !string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
            {
                AppState.TrackingService.ConfigureAI(true, AppState.ApiKeyPlaintext, AppState.ProjectRepo);
                Logger.LogInfo("Configured TrackingService with AI project categorization");
            }
        }
        
        private void SetupExceptionHandling()
        {
            // Handle exceptions from UI thread
            DispatcherUnhandledException += (sender, e) => 
            {
                Logger.LogException(e.Exception, "UI Thread (Unhandled)");
                MessageBox.Show(
                    "An unexpected error occurred and has been logged. Please restart the application if needed.",
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.Handled = true; // Prevent the app from crashing
            };
            
            // Handle exceptions from non-UI threads
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Logger.LogException(e.Exception, "Background Task (Unobserved)");
                e.SetObserved(); // Prevent the app from crashing
            };
            
            // Handle exceptions during app domain unload
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    Logger.LogException(exception, "AppDomain (Unhandled)");
                }
                else
                {
                    Logger.LogError($"Unhandled AppDomain exception: {e.ExceptionObject}");
                }
                
                // Can't prevent app from crashing for this type of exception if IsTerminating is true
                if (!e.IsTerminating)
                {
                    MessageBox.Show(
                        "A critical error occurred. The application will now exit.",
                        "Critical Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };
            
            Logger.LogInfo("Global exception handlers set up");
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.LogInfo("Application exiting");
            IsShuttingDown = true;
            base.OnExit(e);
        }
        
        private void ShowFirstRunDialog()
        {
            string welcomeMessage = 
                "Welcome to DueTime!\n\n" +
                "To get started:\n" +
                "1. Add a couple of Projects in the Projects tab.\n" +
                "2. Create rules by using 'Select Window' or 'Select Folder' to automatically categorize time entries.\n" +
                "3. Go to the Dashboard and start working; the app will automatically track your active window.\n\n" +
                "You can always edit or assign entries manually in the Dashboard.\nEnjoy automatic time tracking!";
                
            MessageBox.Show(
                welcomeMessage, 
                "Getting Started", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
                
            Logger.LogInfo("First-run welcome dialog shown");
        }
        
        /// <summary>
        /// Checks trial period status and license validity
        /// </summary>
        private void CheckTrialAndLicense()
        {
            try
            {
                // Check if license key exists
                string? licenseKey = SettingsManager.GetSetting("LicenseKey");
                AppState.LicenseValid = !string.IsNullOrEmpty(licenseKey);
                
                // If license is valid, trial status doesn't matter
                if (AppState.LicenseValid)
                {
                    AppState.TrialExpired = false;
                    Logger.LogInfo("Valid license detected");
                    return;
                }
                
                // Check install date
                string? installDateStr = SettingsManager.GetSetting("InstallDate");
                
                if (installDateStr == null && AppState.IsFirstRun)
                {
                    // First run - set install date to today
                    AppState.InstallDate = DateTime.Today;
                    SettingsManager.SaveSetting("InstallDate", AppState.InstallDate.ToString("yyyy-MM-dd"));
                    Logger.LogInfo($"First run - set install date to {AppState.InstallDate:yyyy-MM-dd}");
                }
                else if (installDateStr != null)
                {
                    // Parse stored install date
                    AppState.InstallDate = DateTime.Parse(installDateStr);
                    Logger.LogInfo($"Found install date: {AppState.InstallDate:yyyy-MM-dd}");
                }
                else
                {
                    // Fallback if something went wrong
                    AppState.InstallDate = DateTime.Today;
                    SettingsManager.SaveSetting("InstallDate", AppState.InstallDate.ToString("yyyy-MM-dd"));
                    Logger.LogWarning($"No install date found - using today: {AppState.InstallDate:yyyy-MM-dd}");
                }
                
                // Calculate trial status
                int daysUsed = (int)(DateTime.Today - AppState.InstallDate).TotalDays;
                AppState.TrialExpired = daysUsed >= TRIAL_DAYS;
                
                if (AppState.TrialExpired)
                {
                    Logger.LogInfo($"Trial expired. Used {daysUsed} days (limit: {TRIAL_DAYS})");
                }
                else
                {
                    Logger.LogInfo($"Trial active. Used {daysUsed} of {TRIAL_DAYS} days");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Checking trial/license");
                
                // Default to not expired to avoid blocking access due to an error
                AppState.TrialExpired = false;
                Logger.LogWarning("Error in license check - defaulting to trial active");
            }
        }
        
        /// <summary>
        /// Applies the light theme to the application
        /// </summary>
        public void ApplyLightTheme()
        {
            var resourceDictionaries = Resources.MergedDictionaries;
            resourceDictionaries.Clear();
            resourceDictionaries.Add(new ResourceDictionary { Source = new Uri("Themes/FluentLight.xaml", UriKind.Relative) });
            AppState.EnableDarkMode = false;
        }
        
        /// <summary>
        /// Applies the dark theme to the application
        /// </summary>
        public void ApplyDarkTheme()
        {
            var resourceDictionaries = Resources.MergedDictionaries;
            resourceDictionaries.Clear();
            resourceDictionaries.Add(new ResourceDictionary { Source = new Uri("Themes/FluentDark.xaml", UriKind.Relative) });
            AppState.EnableDarkMode = true;
        }
    }
}
