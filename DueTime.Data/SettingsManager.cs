using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DueTime.Data
{
    /// <summary>
    /// Manages application settings stored in the database
    /// </summary>
    public static class SettingsManager
    {
        /// <summary>
        /// Gets a setting value by key
        /// </summary>
        public static string? GetSetting(string key)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @key;";
            cmd.Parameters.AddWithValue("@key", key);
            return cmd.ExecuteScalar() as string;
        }

        /// <summary>
        /// Saves a setting value
        /// </summary>
        public static void SaveSetting(string key, string value)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Settings(Key, Value) VALUES(@key, @value);";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a setting
        /// </summary>
        public static void DeleteSetting(string key)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Settings WHERE Key = @key;";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Checks if a setting exists
        /// </summary>
        public static bool HasSetting(string key)
        {
            using var conn = Database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Settings WHERE Key = @key;";
            cmd.Parameters.AddWithValue("@key", key);
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt64(result) > 0;
        }
    }
} 