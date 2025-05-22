using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            
            // Set AI checkbox enabled state based on trial and license
            EnableAICheckBox.IsEnabled = !AppState.TrialExpired || AppState.LicenseValid;
            
            // Update trial status text
            UpdateTrialStatusText();
        }
        
        /// <summary>
        /// Updates the trial status text based on current state
        /// </summary>
        private void UpdateTrialStatusText()
        {
            if (AppState.LicenseValid)
            {
                TrialStatusText.Text = "License activated - Premium features enabled";
                TrialStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (AppState.TrialExpired)
            {
                TrialStatusText.Text = "Trial period ended - Please enter a license key to continue using premium features";
                TrialStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                int daysRemaining = App.TRIAL_DAYS - (int)(DateTime.Today - AppState.InstallDate).TotalDays;
                TrialStatusText.Text = $"Trial period: {daysRemaining} days remaining for premium features";
                TrialStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private void LicenseKeyTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ActivateLicense_Click(sender, e);
                e.Handled = true;
            }
        }

        private void ActivateLicense_Click(object sender, RoutedEventArgs e)
        {
            string licenseKey = LicenseKeyTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(licenseKey))
            {
                System.Windows.MessageBox.Show("Please enter a valid license key", "License Activation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // For demo, we'll accept any non-empty key
            // In a production app, this would validate against a license server or check a signature
            
            // Save the license key
            SettingsManager.SaveSetting("LicenseKey", licenseKey);
            
            // Update app state
            AppState.LicenseValid = true;
            AppState.TrialExpired = false;
            
            // Update UI
            EnableAICheckBox.IsEnabled = true;
            UpdateTrialStatusText();
            
            System.Windows.MessageBox.Show("License activated successfully!", "License Activation", MessageBoxButton.OK, MessageBoxImage.Information);
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
            SaveApiKey();
        }
        
        private void ApiKeyPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveApiKey();
                e.Handled = true;
            }
        }
        
        private void SaveApiKey()
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
                // Check if trial has expired and no license
                if (AppState.TrialExpired && !AppState.LicenseValid)
                {
                    System.Windows.MessageBox.Show(
                        "Your trial period has expired. Please enter a license key to use premium features.", 
                        "Trial Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
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
                    
                    // Clear license key and restore default trial state
                    SettingsManager.DeleteSetting("LicenseKey");
                    AppState.LicenseValid = false;
                    
                    // Reset install date to current date
                    AppState.InstallDate = DateTime.Today;
                    SettingsManager.SaveSetting("InstallDate", AppState.InstallDate.ToString("yyyy-MM-dd"));
                    AppState.TrialExpired = false;
                    
                    // Update UI
                    UpdateTrialStatusText();
                    EnableAICheckBox.IsEnabled = true;
                    
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

#if DEBUG
        // For testing only - in debug mode
        private void SimulateTrialExpiration_Click(object sender, RoutedEventArgs e)
        {
            // Set install date to 31 days ago
            DateTime expiredInstallDate = DateTime.Today.AddDays(-31);
            SettingsManager.SaveSetting("InstallDate", expiredInstallDate.ToString("yyyy-MM-dd"));
            
            // Update app state
            AppState.InstallDate = expiredInstallDate;
            AppState.TrialExpired = true;
            AppState.LicenseValid = false;
            
            // Update UI
            UpdateTrialStatusText();
            EnableAICheckBox.IsEnabled = false;
            
            System.Windows.MessageBox.Show("Trial period has been manually expired for testing.", "Testing");
        }
#endif

        private void EnableAICheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // If enabling AI and trial expired with no license, prevent it
            if (EnableAICheckBox.IsChecked == true && AppState.TrialExpired && !AppState.LicenseValid)
            {
                System.Windows.MessageBox.Show(
                    "Your trial period has expired. Please enter a license key to use premium features.",
                    "Trial Expired", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                
                // Prevent checking the box
                EnableAICheckBox.IsChecked = false;
                return;
            }
            
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