namespace DueTime.TrackingEngine.Models
{
    /// <summary>Represents a project or client to which time can be assigned.</summary>
    public class Project
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
} 