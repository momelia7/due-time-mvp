using System;
using DueTime.Data;

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
        }
    }
}
