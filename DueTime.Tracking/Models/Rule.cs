namespace DueTime.TrackingEngine.Models
{
    /// <summary>Represents a mapping rule for auto-categorization (keyword -> project).</summary>
    public class Rule
    {
        public int Id { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
    }
} 