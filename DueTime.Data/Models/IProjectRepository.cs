using System.Collections.Generic;
using System.Threading.Tasks;

namespace DueTime.Data
{
    /// <summary>Abstraction for storing and retrieving Project data.</summary>
    public interface IProjectRepository
    {
        Task<int> AddProjectAsync(string name);
        Task<List<Project>> GetAllProjectsAsync();
        Task DeleteAllProjectsAsync();
    }
} 