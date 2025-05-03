using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DueTime.Data
{
    /// <summary>Abstraction for storing and retrieving TimeEntry records.</summary>
    public interface ITimeEntryRepository
    {
        Task AddTimeEntryAsync(TimeEntry entry);
        Task<List<TimeEntry>> GetEntriesByDateAsync(DateTime date);
        Task<List<TimeEntry>> GetEntriesInRangeAsync(DateTime start, DateTime end);
        Task UpdateEntryProjectAsync(int entryId, int? projectId);
        Task DeleteAllEntriesAsync();
    }
} 