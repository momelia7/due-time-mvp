using System;
using System.Collections.ObjectModel;
using DueTime.Data;
using DueTime.Tracking;

namespace DueTime.UI
{
    /// <summary>Global application state and data collections for binding.</summary>
    public static class AppState
    {
        public static ObservableCollection<DueTime.Data.TimeEntry> Entries { get; } = new ObservableCollection<DueTime.Data.TimeEntry>();
        public static ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public static ObservableCollection<Rule> Rules { get; } = new ObservableCollection<Rule>();
        public static SQLiteProjectRepository ProjectRepo { get; } = new SQLiteProjectRepository();
        public static SQLiteTimeEntryRepository EntryRepo { get; } = new SQLiteTimeEntryRepository();
        public static SQLiteRuleRepository RuleRepo { get; } = new SQLiteRuleRepository();
        // Settings and state flags
        public static bool RunOnStartup { get; set; } = false;
        public static bool EnableDarkMode { get; set; } = false;
        public static bool AIEnabled { get; set; } = false;
        public static string? ApiKeyPlaintext { get; set; } = null;
        public static bool LicenseValid { get; set; } = false;
        public static bool TrialExpired { get; set; } = false;
        public static DateTime InstallDate { get; set; }
        public static ITrackingService? TrackingService;  // active tracking service reference
        public static bool IsFirstRun { get; set; } = false;
    }
} 