using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace DueTime.Data
{
    public class SQLiteTimeEntryRepository : ITimeEntryRepository
    {
        public async Task AddTimeEntryAsync(TimeEntry entry)
        {
            // If entry.ProjectId is not set, try to auto-assign via rules
            int? projectId = entry.ProjectId;
            using (var conn = Database.GetConnection())
            {
                if (projectId == null)
                {
                    using var cmdRule = conn.CreateCommand();
                    cmdRule.CommandText = @"
                        SELECT ProjectId 
                        FROM Rules 
                        WHERE (instr(lower(@title), lower(Pattern)) > 0)
                           OR (instr(lower(@app), lower(Pattern)) > 0)
                        LIMIT 1;";
                    cmdRule.Parameters.AddWithValue("@title", entry.WindowTitle ?? "");
                    cmdRule.Parameters.AddWithValue("@app", entry.ApplicationName ?? "");
                    var result = await cmdRule.ExecuteScalarAsync();
                    if(result != null && result != DBNull.Value)
                    {
                        projectId = Convert.ToInt32(result);
                    }
                }
            }
            
            // Now insert the entry (outside previous using because we might need new command)
            using var conn2 = Database.GetConnection();
            using var cmd = conn2.CreateCommand();
            cmd.CommandText = @"INSERT INTO TimeEntries 
                                (StartTime, EndTime, WindowTitle, ApplicationName, ProjectId)
                                VALUES (@start, @end, @title, @app, @proj);
                                SELECT last_insert_rowid();";
            // Store timestamps as ISO 8601 strings for simplicity
            cmd.Parameters.AddWithValue("@start", entry.StartTime.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@end", entry.EndTime.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@title", entry.WindowTitle ?? "");
            cmd.Parameters.AddWithValue("@app", entry.ApplicationName ?? "");
            cmd.Parameters.AddWithValue("@proj", projectId.HasValue ? projectId.Value : (object)DBNull.Value);
            
            var newIdObj = await cmd.ExecuteScalarAsync();
            if(newIdObj != null) entry.Id = Convert.ToInt32(newIdObj);
            
            // Update entry's ProjectId with the auto-assigned value if applicable
            entry.ProjectId = projectId;
        }

        public async Task<List<TimeEntry>> GetEntriesByDateAsync(DateTime date)
        {
            // Load entries whose StartTime date = given date
            DateTime startOfDay = date.Date;
            DateTime endOfDay = startOfDay.AddDays(1);
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT t.Id, t.StartTime, t.EndTime, t.WindowTitle, t.ApplicationName, t.ProjectId, p.Name as ProjectName
                                FROM TimeEntries t
                                LEFT JOIN Projects p ON t.ProjectId = p.Id
                                WHERE t.StartTime >= @dayStart AND t.StartTime < @dayEnd
                                ORDER BY t.StartTime;";
            cmd.Parameters.AddWithValue("@dayStart", startOfDay.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@dayEnd", endOfDay.ToString("o", CultureInfo.InvariantCulture));
            var reader = await cmd.ExecuteReaderAsync();
            var entries = new List<TimeEntry>();
            while (await reader.ReadAsync())
            {
                // Parse stored ISO datetime strings
                DateTime start = DateTime.Parse(reader.GetString(1), null, DateTimeStyles.RoundtripKind);
                DateTime end = DateTime.Parse(reader.GetString(2), null, DateTimeStyles.RoundtripKind);
                entries.Add(new TimeEntry
                {
                    Id = reader.GetInt32(0),
                    StartTime = start,
                    EndTime = end,
                    WindowTitle = reader.GetString(3),
                    ApplicationName = reader.GetString(4),
                    ProjectId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                    ProjectName = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return entries;
        }

        public async Task<List<TimeEntry>> GetEntriesInRangeAsync(DateTime start, DateTime end)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT t.Id, t.StartTime, t.EndTime, t.WindowTitle, t.ApplicationName, t.ProjectId, p.Name as ProjectName
                                FROM TimeEntries t
                                LEFT JOIN Projects p ON t.ProjectId = p.Id
                                WHERE t.StartTime >= @start AND t.StartTime < @end
                                ORDER BY t.StartTime;";
            cmd.Parameters.AddWithValue("@start", start.ToString("o", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@end", end.ToString("o", CultureInfo.InvariantCulture));
            var reader = await cmd.ExecuteReaderAsync();
            var entries = new List<TimeEntry>();
            while (await reader.ReadAsync())
            {
                // Parse stored ISO datetime strings
                DateTime startTime = DateTime.Parse(reader.GetString(1), null, DateTimeStyles.RoundtripKind);
                DateTime endTime = DateTime.Parse(reader.GetString(2), null, DateTimeStyles.RoundtripKind);
                entries.Add(new TimeEntry
                {
                    Id = reader.GetInt32(0),
                    StartTime = startTime,
                    EndTime = endTime,
                    WindowTitle = reader.GetString(3),
                    ApplicationName = reader.GetString(4),
                    ProjectId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                    ProjectName = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return entries;
        }
        
        public async Task UpdateEntryProjectAsync(int entryId, int? projectId)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE TimeEntries SET ProjectId=@proj WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@proj", projectId.HasValue ? projectId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", entryId);
            await cmd.ExecuteNonQueryAsync();
        }
        
        public async Task DeleteAllEntriesAsync()
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM TimeEntries;";
            await cmd.ExecuteNonQueryAsync();
        }
    }
} 