using System.Threading.Tasks;
using System.Collections.Generic;
using DueTime.TrackingEngine.Models;
namespace DueTime.TrackingEngine.Services
{
    /// <summary>Abstraction for storing and retrieving TimeEntry records.</summary>
    public interface ITimeEntryRepository
    {
        Task AddTimeEntryAsync(TimeEntry entry);
        Task<List<TimeEntry>> GetEntriesByDateAsync(System.DateTime date);
        Task<List<TimeEntry>> GetEntriesInRangeAsync(System.DateTime start, System.DateTime end);
        Task UpdateEntryProjectAsync(int entryId, int? projectId);
    }
} 