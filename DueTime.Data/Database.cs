using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    public static class Database
    {
        public static string DbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DueTime", "duetime.db");

        public static void InitializeSchema()
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath) ?? string.Empty);

            using var connection = GetConnection();
            using var command = connection.CreateCommand();

            // Create Settings table
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";
            command.ExecuteNonQuery();

            // Create Projects table
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Projects (
                    ProjectId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );";
            command.ExecuteNonQuery();

            // Create Rules table
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Rules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Pattern TEXT NOT NULL,
                    ProjectId INTEGER NOT NULL,
                    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
                );";
            command.ExecuteNonQuery();

            // Create TimeEntries table
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TimeEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL,
                    WindowTitle TEXT NOT NULL,
                    ApplicationName TEXT NOT NULL,
                    ProjectId INTEGER NULL,
                    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
                );";
            command.ExecuteNonQuery();
        }

        public static SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            return connection;
        }

        public static void BackupDatabase(string targetPath)
        {
            File.Copy(DbPath, targetPath, overwrite: true);
        }

        public static void RestoreDatabase(string sourcePath)
        {
            // Close connection to current database
            using (var connection = GetConnection())
            {
                connection.Close();
            }

            // Replace the file
            File.Copy(sourcePath, DbPath, overwrite: true);
        }
    }
} 