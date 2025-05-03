using System.Windows;
using System.Windows.Controls;
using DueTime.Data;
using Microsoft.Win32;

namespace DueTime.UI.Views
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            
            // Initialize password box if API key exists
            if (SecureStorage.HasApiKey())
            {
                ApiKeyPasswordBox.Password = SecureStorage.LoadApiKey() ?? string.Empty;
            }
            
            // Register for AI checkbox changes
            EnableAICheckBox.Checked += EnableAICheckBox_Changed;
            EnableAICheckBox.Unchecked += EnableAICheckBox_Changed;
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "DueTimeBackup.db",
                Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*"
            };
            
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                try
                {
                    Database.BackupDatabase(path);
                    System.Windows.MessageBox.Show("Backup saved to: " + path, "Backup Successful");
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show("Backup failed: " + ex.Message, "Error");
                }
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
                Title = "Select a DueTime backup file to restore"
            };
            
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                var result = System.Windows.MessageBox.Show(
                    "This will overwrite current data with the backup. Continue?", 
                    "Confirm Restore", 
                    MessageBoxButton.YesNo);
                    
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Ideally, stop tracking before restore
                        if (AppState.TrackingService != null)
                        {
                            AppState.TrackingService.Stop();
                        }
                        
                        Database.RestoreDatabase(path);
                        System.Windows.MessageBox.Show(
                            "Data restored from backup. The application will now close. Please restart to load the restored data.", 
                            "Restore Complete");
                            
                        System.Windows.Application.Current.Shutdown();
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show("Restore failed: " + ex.Message, "Error");
                        
                        // Restart tracking if it was previously running
                        if (AppState.TrackingService != null)
                        {
                            AppState.TrackingService.Start();
                        }
                    }
                }
            }
        }

        private void SaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyPasswordBox.Password;
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Clear the API key if empty
                SecureStorage.DeleteApiKey();
                AppState.ApiKeyPlaintext = null;
                AppState.AIEnabled = false;
                System.Windows.MessageBox.Show("API key has been removed.", "API Key Removed");
            }
            else
            {
                // Save the API key securely
                SecureStorage.SaveApiKey(apiKey);
                AppState.ApiKeyPlaintext = apiKey;
                System.Windows.MessageBox.Show("API key has been saved securely.", "API Key Saved");
            }
        }

        private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null)
                {
                    System.Windows.MessageBox.Show("Unable to access registry. Run-on-startup setting could not be changed.", "Registry Error");
                    return;
                }

                if (AppState.RunOnStartup)
                {
                    // Add app to startup
                    string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                    key.SetValue("DueTime", appPath);
                }
                else
                {
                    // Remove from startup
                    key.DeleteValue("DueTime", false);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error changing startup setting: {ex.Message}", "Error");
                // Revert the checkbox state since operation failed
                AppState.RunOnStartup = !AppState.RunOnStartup;
            }
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "This will delete ALL your time entries, projects, and rules. This action cannot be undone.\n\nAre you sure you want to continue?",
                "Confirm Data Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Stop tracking if active
                    if (AppState.TrackingService != null)
                    {
                        AppState.TrackingService.Stop();
                    }
                    
                    // Clear all data from repositories
                    AppState.EntryRepo.DeleteAllEntriesAsync().Wait();
                    AppState.ProjectRepo.DeleteAllProjectsAsync().Wait();
                    AppState.RuleRepo.DeleteAllRulesAsync().Wait();
                    
                    // Clear observable collections
                    AppState.Entries.Clear();
                    AppState.Projects.Clear();
                    AppState.Rules.Clear();
                    
                    // Clear API key as well
                    SecureStorage.DeleteApiKey();
                    AppState.ApiKeyPlaintext = null;
                    AppState.AIEnabled = false;
                    ApiKeyPasswordBox.Password = string.Empty;
                    
                    System.Windows.MessageBox.Show("All data has been successfully deleted.", "Data Cleared");
                    
                    // Restart tracking
                    if (AppState.TrackingService != null)
                    {
                        AppState.TrackingService.Start();
                    }
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error clearing data: {ex.Message}", "Error");
                }
            }
        }

        private void EnableAICheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (EnableAICheckBox.IsChecked == true)
            {
                // If API key exists but not loaded, load it now
                if (SecureStorage.HasApiKey() && string.IsNullOrEmpty(AppState.ApiKeyPlaintext))
                {
                    AppState.ApiKeyPlaintext = SecureStorage.LoadApiKey();
                }
                
                // Show a message if no API key is available
                if (string.IsNullOrEmpty(ApiKeyPasswordBox.Password))
                {
                    System.Windows.MessageBox.Show(
                        "Please enter an OpenAI API key to use AI features.", 
                        "API Key Required", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            else
            {
                // Keep the key stored, but clear it from memory if AI is disabled
                // This allows re-enabling without re-entering the key
                AppState.ApiKeyPlaintext = null;
            }
        }
    }
} 