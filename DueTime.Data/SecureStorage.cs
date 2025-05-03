using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DueTime.Data
{
    public static class SecureStorage
    {
        private static readonly string KeyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DueTime", "openai.key.enc");

        public static void SaveApiKey(string apiKey)
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(KeyFilePath) ?? string.Empty);

            // Encrypt using Windows Data Protection API
            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(apiKey),
                null,  // entropy
                DataProtectionScope.CurrentUser);

            // Save to file
            File.WriteAllBytes(KeyFilePath, encryptedData);
        }

        public static string? LoadApiKey()
        {
            if (!File.Exists(KeyFilePath))
                return null;

            try
            {
                // Read encrypted data
                byte[] encryptedData = File.ReadAllBytes(KeyFilePath);

                // Decrypt using Windows Data Protection API
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    null,  // entropy
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                // If any error occurs, return null
                return null;
            }
        }

        public static void DeleteApiKey()
        {
            if (File.Exists(KeyFilePath))
            {
                try { File.Delete(KeyFilePath); }
                catch { /* Ignore errors on delete */ }
            }
        }
    }
} 