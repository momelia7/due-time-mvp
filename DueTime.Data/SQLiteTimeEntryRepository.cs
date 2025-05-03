using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    public class SQLiteTimeEntryRepository : ITimeEntryRepository
    {
        public async Task AddTimeEntryAsync(TimeEntry entry)
        {
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO TimeEntries (StartTime, EndTime, WindowTitle, ApplicationName, ProjectId)
                VALUES (@startTime, @endTime, @windowTitle, @appName, @projectId);
                SELECT last_insert_rowid();";
            
            command.Parameters.AddWithValue("@startTime", entry.StartTime.ToString("O"));
            command.Parameters.AddWithValue("@endTime", entry.EndTime.ToString("O"));
            command.Parameters.AddWithValue("@windowTitle", entry.WindowTitle);
            command.Parameters.AddWithValue("@appName", entry.ApplicationName);
            command.Parameters.AddWithValue("@projectId", entry.ProjectId as object ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out int id))
            {
                entry.Id = id;
            }
        }

        public async Task<List<TimeEntry>> GetEntriesByDateAsync(DateTime date)
        {
            var entries = new List<TimeEntry>();
            
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            var startDate = date.Date.ToString("O");
            var endDate = date.Date.AddDays(1).ToString("O");

            command.CommandText = @"
                SELECT Id, StartTime, EndTime, WindowTitle, ApplicationName, ProjectId
                FROM TimeEntries
                WHERE StartTime >= @startDate AND StartTime < @endDate
                ORDER BY StartTime";
            
            command.Parameters.AddWithValue("@startDate", startDate);
            command.Parameters.AddWithValue("@endDate", endDate);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(new TimeEntry
                {
                    Id = reader.GetInt32(0),
                    StartTime = DateTime.Parse(reader.GetString(1)),
                    EndTime = DateTime.Parse(reader.GetString(2)),
                    WindowTitle = reader.GetString(3),
                    ApplicationName = reader.GetString(4),
                    ProjectId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5)
                });
            }

            return entries;
        }

        public async Task<List<TimeEntry>> GetEntriesInRangeAsync(DateTime start, DateTime end)
        {
            var entries = new List<TimeEntry>();
            
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, StartTime, EndTime, WindowTitle, ApplicationName, ProjectId
                FROM TimeEntries
                WHERE StartTime >= @startDate AND StartTime < @endDate
                ORDER BY StartTime";
            
            command.Parameters.AddWithValue("@startDate", start.ToString("O"));
            command.Parameters.AddWithValue("@endDate", end.ToString("O"));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(new TimeEntry
                {
                    Id = reader.GetInt32(0),
                    StartTime = DateTime.Parse(reader.GetString(1)),
                    EndTime = DateTime.Parse(reader.GetString(2)),
                    WindowTitle = reader.GetString(3),
                    ApplicationName = reader.GetString(4),
                    ProjectId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5)
                });
            }

            return entries;
        }

        public async Task UpdateEntryProjectAsync(int entryId, int? projectId)
        {
            using var connection = Database.GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "UPDATE TimeEntries SET ProjectId = @projectId WHERE Id = @entryId";
            command.Parameters.AddWithValue("@entryId", entryId);
            command.Parameters.AddWithValue("@projectId", projectId as object ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
    }
} 