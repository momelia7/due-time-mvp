using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    public class SQLiteProjectRepository : IProjectRepository
    {
        public async Task<int> AddProjectAsync(string name)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Projects (Name) VALUES (@name); SELECT Id FROM Projects WHERE Name=@name;";
            cmd.Parameters.AddWithValue("@name", name);
            // We use a SELECT to get the Id whether inserted now or already existed.
            var result = await cmd.ExecuteScalarAsync();
            if(result != null)
            {
                return System.Convert.ToInt32(result);
            }
            // If result null, something wrong (should not happen unless maybe concurrency).
            return -1;
        }



        public async Task<List<Project>> GetAllProjectsAsync()
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Projects;";
            var reader = await cmd.ExecuteReaderAsync();
            var projects = new List<Project>();
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

        public async Task DeleteAllProjectsAsync()
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Projects;";
            await cmd.ExecuteNonQueryAsync();
        }
    }
} 