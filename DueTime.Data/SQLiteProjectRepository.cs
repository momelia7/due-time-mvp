using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    public class SQLiteProjectRepository : IProjectRepository
    {
        public async Task<int> AddProjectAsync(string name)
        {
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Projects (Name)
                VALUES (@name);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@name", name);
            
            var result = await command.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out int id))
            {
                return id;
            }
            
            return -1;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            var projects = new List<Project>();
            
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT ProjectId, Name FROM Projects ORDER BY Name";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(new Project
                {
                    ProjectId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return projects;
        }
    }
} 