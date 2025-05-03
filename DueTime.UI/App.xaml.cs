using System;
using DueTime.Data;
using Microsoft.Win32;

namespace DueTime.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize database and load data
            Database.InitializeSchema();
            
            // Check for saved API key and set AI enabled flag accordingly
            AppState.ApiKeyPlaintext = SecureStorage.LoadApiKey();
            AppState.AIEnabled = (AppState.ApiKeyPlaintext != null);
            
            // Check if app is set to run on startup
            try
            {
                var runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                AppState.RunOnStartup = runKey?.GetValue("DueTime") != null;
            }
            catch
            {
                // If we can't access registry, default to false
                AppState.RunOnStartup = false;
            }
            
            // Load projects
            var projList = AppState.ProjectRepo.GetAllProjectsAsync().Result;
            AppState.Projects.Clear();
            foreach(var proj in projList)
                AppState.Projects.Add(proj);
            
            // Load rules
            var ruleList = AppState.RuleRepo.GetAllRulesAsync().Result;
            AppState.Rules.Clear();
            foreach(var rule in ruleList)
                AppState.Rules.Add(rule);
            
            // Load today's entries
            var entryList = AppState.EntryRepo.GetEntriesByDateAsync(DateTime.Today).Result;
            AppState.Entries.Clear();
            foreach(var entry in entryList)
                AppState.Entries.Add(entry);
                
            // Detect first run
            AppState.IsFirstRun = (AppState.Projects.Count == 0 && AppState.Entries.Count == 0);
        }
    }
}
