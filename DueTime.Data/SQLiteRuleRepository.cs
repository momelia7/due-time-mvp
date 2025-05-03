using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    public class SQLiteRuleRepository : IRuleRepository
    {
        public async Task<int> AddRuleAsync(string pattern, int projectId)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Rules (Pattern, ProjectId) VALUES (@pat, @pid); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@pat", pattern);
            cmd.Parameters.AddWithValue("@pid", projectId);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? System.Convert.ToInt32(result) : -1;
        }

        public async Task<List<Rule>> GetAllRulesAsync()
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT R.Id, R.Pattern, R.ProjectId, P.Name FROM Rules R JOIN Projects P ON R.ProjectId = P.Id;";
            var reader = await cmd.ExecuteReaderAsync();
            var rules = new List<Rule>();
            while (await reader.ReadAsync())
            {
                rules.Add(new Rule
                {
                    Id = reader.GetInt32(0),
                    Pattern = reader.GetString(1),
                    ProjectId = reader.GetInt32(2),
                    ProjectName = reader.GetString(3)
                });
            }
            return rules;
        }
    }
} 