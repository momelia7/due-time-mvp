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

        static Database()
        {
            string appDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dueTimeDir = Path.Combine(appDir, "DueTime");
            if (!Directory.Exists(dueTimeDir))
                Directory.CreateDirectory(dueTimeDir);
            DbPath = Path.Combine(dueTimeDir, "DueTime.db");
        }

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            return conn;
        }

        public static void InitializeSchema()
        {
            using var conn = GetConnection();
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
            ";
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
    }
} 