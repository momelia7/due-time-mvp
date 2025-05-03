using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DueTime.Tracking
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
    }

    public interface ITimeEntryRepository
    {
        Task AddTimeEntryAsync(TimeEntry entry);
        Task<List<TimeEntry>> GetEntriesByDateAsync(DateTime date);
        Task<List<TimeEntry>> GetEntriesInRangeAsync(DateTime start, DateTime end);
        Task UpdateEntryProjectAsync(int entryId, int? projectId);
    }
} 