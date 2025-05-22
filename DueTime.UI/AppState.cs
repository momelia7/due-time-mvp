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
        // Tracking state
        public static bool IsTrackingPaused { get; set; } = false;
        // AI features (disabled by default for privacy)
        public static bool AIEnabled { get; set; } = false;
        public static string? ApiKeyPlaintext { get; set; } = null;
        // License/trial status (for future use)
        public static bool LicenseValid { get; set; } = false;
        public static bool TrialExpired { get; set; } = false;
        public static DateTime InstallDate { get; set; }
        // Active tracking service reference - allows stopping from any component
        public static ITrackingService? TrackingService;
        // First run detection
        public static bool IsFirstRun { get; set; } = false;
    }
} 