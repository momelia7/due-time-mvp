using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DueTime.Data
{
    public static class Database
    {
        public static string DbPath { get; private set; }
        private static string _connectionString;
        
        // Current schema version - Updated for Tasks table
        private const int CurrentSchemaVersion = 2;

        static Database()
        {
            string appDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dueTimeDir = Path.Combine(appDir, "DueTime");
            if (!Directory.Exists(dueTimeDir))
                Directory.CreateDirectory(dueTimeDir);
            DbPath = Path.Combine(dueTimeDir, "DueTime.db");
            _connectionString = $"Data Source={DbPath};Cache=Shared";
        }
        
        /// <summary>
        /// Initializes the database with a custom connection string
        /// </summary>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            
            // Extract the DbPath from the connection string if possible
            if (connectionString.Contains("Data Source="))
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    {
                        DbPath = part.Substring("Data Source=".Length);
                        break;
                    }
                }
            }
            
            // Initialize the schema
            InitializeSchema();
        }

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public static void InitializeSchema()
        {
            using var conn = GetConnection();
            
            // Check current schema version
            int currentVersion = GetSchemaVersion(conn);
            
            // If this is a new database (version 0), create the schema
            if (currentVersion == 0)
            {
                CreateInitialSchema(conn);
                SetSchemaVersion(conn, 1);
                currentVersion = 1;
            }
            
            // Apply any migrations needed
            if (currentVersion < 2)
            {
                ApplyMigrationToV2(conn);
                SetSchemaVersion(conn, 2);
                currentVersion = 2;
            }
            

        }
        
        private static int GetSchemaVersion(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA user_version;";
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }
        
        private static void SetSchemaVersion(SqliteConnection conn, int version)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA user_version = {version};";
            cmd.ExecuteNonQuery();
        }
        
        private static void CreateInitialSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            PRAGMA foreign_keys = ON;
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Projects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS TimeEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                WindowTitle TEXT,
                ApplicationName TEXT,
                ProjectId INTEGER NULL,
                FOREIGN KEY(ProjectId) REFERENCES Projects(Id)
            );
            CREATE TABLE IF NOT EXISTS Rules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Pattern TEXT NOT NULL,
                ProjectId INTEGER NOT NULL,
                FOREIGN KEY(ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );";
            cmd.ExecuteNonQuery();
        }
        
        private static void ApplyMigrationToV2(SqliteConnection conn)
        {
            // Migration for version 2: Add indexes for performance
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            -- Create indexes to improve query performance
            CREATE INDEX IF NOT EXISTS IX_TimeEntries_StartTime ON TimeEntries(StartTime);
            CREATE INDEX IF NOT EXISTS IX_TimeEntries_ProjectId ON TimeEntries(ProjectId);";
            cmd.ExecuteNonQuery();
        }



        public static void BackupDatabase(string backupPath)
        {
            // Ensure any open connections are disposed before copying (in our usage pattern, they should be by now).
            // Copy the DB file to backupPath.
            File.Copy(DbPath, backupPath, overwrite: true);
        }

        public static void RestoreDatabase(string sourcePath)
        {
            // Overwrite current DB with the sourcePath file.
            // It's recommended to do this when the app is not actively tracking (to avoid partial writes).
            File.Copy(sourcePath, DbPath, overwrite: true);
        }

        public static void EncryptFile(string inputPath, string outputPath, string password)
        {
            // Derive a 256-bit key from the password
            using var aes = Aes.Create();
            aes.KeySize = 256;
            // Use a fixed salt for PBKDF2 (for simplicity, could also store salt)
            byte[] salt = Encoding.UTF8.GetBytes("DueTimeSalt");
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(32);
            aes.GenerateIV();
            byte[] iv = aes.IV;
            using FileStream inStream = File.OpenRead(inputPath);
            using FileStream outStream = File.Create(outputPath);
            // Write IV at beginning of output file
            outStream.Write(iv, 0, iv.Length);
            using var cryptoStream = new CryptoStream(outStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            inStream.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
        }

        public static void DecryptFile(string inputPath, string outputPath, string password)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            byte[] salt = Encoding.UTF8.GetBytes("DueTimeSalt");
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(32);
            using FileStream inStream = File.OpenRead(inputPath);
            // Read IV from input file
            byte[] iv = new byte[16];
            if (inStream.Read(iv, 0, iv.Length) != iv.Length)
                throw new InvalidOperationException("Could not read initialization vector from encrypted file");
            aes.IV = iv;
            using FileStream outStream = File.Create(outputPath);
            using var cryptoStream = new CryptoStream(inStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cryptoStream.CopyTo(outStream);
        }

        public static void ForceInitializeForTesting(string connectionString)
        {
            _connectionString = connectionString;
            
            using var conn = GetConnection();
            
            // For testing, create the complete schema from scratch
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            PRAGMA foreign_keys = ON;
            
            -- Create Projects table
            CREATE TABLE IF NOT EXISTS Projects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );
            
            -- Create TimeEntries table
            CREATE TABLE IF NOT EXISTS TimeEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                WindowTitle TEXT,
                ApplicationName TEXT,
                ProjectId INTEGER NULL,
                FOREIGN KEY(ProjectId) REFERENCES Projects(Id)
            );
            
            -- Create Rules table
            CREATE TABLE IF NOT EXISTS Rules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Pattern TEXT NOT NULL,
                ProjectId INTEGER NOT NULL,
                FOREIGN KEY(ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );
            
            -- Create Settings table
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );
            
            -- Create all indexes
            CREATE INDEX IF NOT EXISTS IX_TimeEntries_StartTime ON TimeEntries(StartTime);
            CREATE INDEX IF NOT EXISTS IX_TimeEntries_ProjectId ON TimeEntries(ProjectId);";
            cmd.ExecuteNonQuery();
            
            SetSchemaVersion(conn, CurrentSchemaVersion);
        }
    }
} 