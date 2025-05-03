using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Versioning;

namespace DueTime.Data
{
    [SupportedOSPlatform("windows")]
    public static class SecureStorage
    {
        private static string KeyFilePath 
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DueTime", "openai.key.enc");

        public static void SaveApiKey(string apiKey)
        {
            byte[] plaintext = Encoding.UTF8.GetBytes(apiKey);
            byte[] encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(KeyFilePath) ?? throw new InvalidOperationException("Unable to determine directory for key file");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            File.WriteAllBytes(KeyFilePath, encrypted);
        }

        public static string? LoadApiKey()
        {
            if (!File.Exists(KeyFilePath)) return null;
            
            try
            {
                byte[] encrypted = File.ReadAllBytes(KeyFilePath);
                byte[] plaintext = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plaintext);
            }
            catch
            {
                // If decryption fails or file is corrupted, return null
                return null;
            }
        }

        public static void DeleteApiKey()
        {
            if (File.Exists(KeyFilePath))
            {
                try { File.Delete(KeyFilePath); } catch { }
            }
        }

        public static bool HasApiKey()
        {
            return File.Exists(KeyFilePath);
        }
    }
} 