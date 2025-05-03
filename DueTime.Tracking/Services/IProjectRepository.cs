using System.Threading.Tasks;
using System.Collections.Generic;
using DueTime.TrackingEngine.Models;
namespace DueTime.TrackingEngine.Services
{
    public interface IProjectRepository
    {
        Task<int> AddProjectAsync(string name);
        Task<List<Project>> GetAllProjectsAsync();
    }
} 