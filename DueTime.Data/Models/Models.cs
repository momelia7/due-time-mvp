using System;
using System.Collections.Generic;
using System.Linq;

namespace DueTime.Data
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }

        
        // Computed property for duration
        public string Duration => (EndTime - StartTime).ToString(@"hh\:mm\:ss");
    }

    public class Project
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Rule
    {
        public int Id { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
    }
} 