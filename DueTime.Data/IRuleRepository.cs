using System.Collections.Generic;
using System.Threading.Tasks;

namespace DueTime.Data
{
    /// <summary>Abstraction for storing and retrieving Rule data.</summary>
    public interface IRuleRepository
    {
        Task<int> AddRuleAsync(string pattern, int projectId);
        Task<List<Rule>> GetAllRulesAsync();
    }
} 