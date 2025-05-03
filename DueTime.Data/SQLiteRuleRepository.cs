using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    public class SQLiteRuleRepository : IRuleRepository
    {
        public async Task<int> AddRuleAsync(string pattern, int projectId)
        {
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Rules (Pattern, ProjectId)
                VALUES (@pattern, @projectId);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@pattern", pattern);
            command.Parameters.AddWithValue("@projectId", projectId);
            
            var result = await command.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out int id))
            {
                return id;
            }
            
            return -1;
        }

        public async Task<List<Rule>> GetAllRulesAsync()
        {
            var rules = new List<Rule>();
            
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT r.Id, r.Pattern, r.ProjectId, p.Name
                FROM Rules r
                JOIN Projects p ON r.ProjectId = p.ProjectId
                ORDER BY p.Name";
            
            using var reader = await command.ExecuteReaderAsync();
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