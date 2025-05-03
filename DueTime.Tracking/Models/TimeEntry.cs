using System;
namespace DueTime.TrackingEngine.Models
{
    /// <summary>Represents a logged time interval of activity.</summary>
    public class TimeEntry
    {
        public int Id { get; set; }                   // Database ID
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int? ProjectId { get; set; }           // Assigned project (null if unassigned)
    }
} 