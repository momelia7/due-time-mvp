using System.Threading.Tasks;
using System.Collections.Generic;
using DueTime.TrackingEngine.Models;
namespace DueTime.TrackingEngine.Services
{
    public interface IRuleRepository
    {
        Task<int> AddRuleAsync(string pattern, int projectId);
        Task<List<Rule>> GetAllRulesAsync();
    }
} 