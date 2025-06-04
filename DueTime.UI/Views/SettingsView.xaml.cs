using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DueTime.Data;
using DueTime.UI.Utilities;
using DueTime.UI.ViewModels;
using Microsoft.Win32;

// Create a dummy implementation for ExplorerService
namespace DueTime.Tracking.Services
{
    public class ExplorerService : IExplorerService
    {
        public string? GetActiveExplorerPath()
        {
            // Dummy implementation
            return null;
        }
    }
}

namespace DueTime.UI.Views
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new DueTime.UI.ViewModels.SettingsViewModel(new DueTime.Tracking.Services.ExplorerService());
            
            // Initialize password box if API key exists
            if (SecureStorage.HasApiKey())
            {
                ApiKeyPasswordBox.Password = SecureStorage.LoadApiKey() ?? string.Empty;
            }
            
            // Initialize CheckBox states from AppState
            StartupCheckBox.IsChecked = AppState.RunOnStartup;
            DarkModeCheckBox.IsChecked = AppState.EnableDarkMode;
            EnableAICheckBox.IsChecked = AppState.AIEnabled;
            
            // Register for AI checkbox changes
            EnableAICheckBox.Checked += EnableAICheckBox_Changed;
            EnableAICheckBox.Unchecked += EnableAICheckBox_Changed;
            
            // Register for dark mode checkbox changes
            DarkModeCheckBox.Checked += DarkModeCheckBox_Changed;
            DarkModeCheckBox.Unchecked += DarkModeCheckBox_Changed;
            
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
                MessageBox.Show("Please enter a valid license key", "License Activation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            
            MessageBox.Show("License activated successfully!", "License Activation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"DueTimeBackup_{DateTime.Now:yyyy-MM-dd}.db",
                DefaultExt = ".db",
                Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
                Title = "Save DueTime Backup"
            };
            
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                Logger.LogInfo($"Initiating backup to: {path}");
                
                // Variables for button state
                var button = sender as Button;
                string buttonContent = button?.Content.ToString() ?? "Backup Data";
                
                try
                {
                    // Show a simple progress indicator
                    if (button != null) 
                    {
                        button.Content = "Backing up...";
                        button.IsEnabled = false;
                    }
                    
                    // Ensure the directory exists
                    string? directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Perform the backup
                    Database.BackupDatabase(path);
                    
                    // Reset the button
                    if (button != null)
                    {
                        button.Content = buttonContent;
                        button.IsEnabled = true;
                    }
                    
                    Logger.LogInfo($"Backup successfully created at: {path}");
                    MessageBox.Show(
                        $"Backup successfully saved to:\n{path}", 
                        "Backup Complete", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.LogError($"Backup failed - Access denied to: {path}");
                    MessageBox.Show(
                        "Unable to create backup. You don't have permission to write to the selected location. Please try a different location.",
                        "Backup Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (DirectoryNotFoundException)
                {
                    Logger.LogError($"Backup failed - Directory not found: {Path.GetDirectoryName(path)}");
                    MessageBox.Show(
                        "The selected location does not exist. Please select a valid directory for the backup file.",
                        "Backup Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (IOException ex)
                {
                    Logger.LogException(ex, "Backup - IO Error");
                    MessageBox.Show(
                        "Could not create the backup file. The file may be in use by another process or the disk might be full.",
                        "Backup Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Backup");
                    MessageBox.Show(
                        "An error occurred while creating the backup. Please try again later.",
                        "Backup Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    // Ensure button is reset
                    if (button != null)
                    {
                        button.Content = buttonContent;
                        button.IsEnabled = true;
                    }
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
                Logger.LogInfo($"Selected restore file: {path}");
                
                // Variables for button state
                var button = sender as Button;
                string buttonContent = button?.Content.ToString() ?? "Restore Data";
                
                try
                {
                    // Verify the file exists and is accessible
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException("The selected backup file was not found.");
                    }
                    
                    // Confirm the restore operation
                    var result = MessageBox.Show(
                        "This will replace all current data with the backup. This cannot be undone.\n\nDo you want to continue?", 
                        "Confirm Restore", 
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        // Show a simple progress indicator
                        if (button != null)
                        {
                            button.Content = "Restoring...";
                            button.IsEnabled = false;
                        }
                        
                        Logger.LogInfo("User confirmed restore operation");
                        
                        // Ideally, stop tracking before restore
                        if (AppState.TrackingService != null)
                        {
                            Logger.LogInfo("Stopping tracking service for restore");
                            AppState.TrackingService.Stop();
                        }
                        
                        // Perform the actual restore
                        Database.RestoreDatabase(path);
                        
                        Logger.LogInfo("Database restored successfully");
                        MessageBox.Show(
                            "Data has been restored from the backup. The application will now restart to load the restored data.", 
                            "Restore Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                            
                        // Restart the application
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        Logger.LogInfo("User cancelled restore operation");
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Logger.LogException(ex, "Restore");
                    MessageBox.Show(
                        "The backup file could not be found. Please verify the file exists and try again.",
                        "Restore Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (IOException ex)
                {
                    Logger.LogException(ex, "Restore - IO Error");
                    MessageBox.Show(
                        "Could not access the backup file. It may be in use by another application or corrupted.",
                        "Restore Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Restore");
                    MessageBox.Show(
                        "An error occurred during the restore operation. The backup file may be corrupted or incompatible.",
                        "Restore Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    
                    // Restart tracking if it was previously running
                    if (AppState.TrackingService != null)
                    {
                        try
                        {
                            Logger.LogInfo("Restarting tracking service after failed restore");
                            AppState.TrackingService.Start();
                        }
                        catch (Exception startEx)
                        {
                            Logger.LogException(startEx, "Restarting tracking after restore failure");
                        }
                    }
                }
                finally
                {
                    // Ensure button is reset if we're still here (operation failed)
                    if (button != null && Application.Current != null)
                    {
                        button.Content = buttonContent;
                        button.IsEnabled = true;
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
                MessageBox.Show("API key has been removed.", "API Key Removed");
            }
            else
            {
                // Check if trial has expired and no license
                if (AppState.TrialExpired && !AppState.LicenseValid)
                {
                    MessageBox.Show(
                        "Your trial period has expired. Please enter a license key to use premium features.", 
                        "Trial Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Save the API key securely
                SecureStorage.SaveApiKey(apiKey);
                AppState.ApiKeyPlaintext = apiKey;
                MessageBox.Show("API key has been saved securely.", "API Key Saved");
            }
        }

        private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Update AppState to match checkbox state
            AppState.RunOnStartup = StartupCheckBox.IsChecked ?? false;
            
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null)
                {
                    MessageBox.Show("Unable to access registry. Run-on-startup setting could not be changed.", "Registry Error");
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
                MessageBox.Show($"Error changing startup setting: {ex.Message}", "Error");
                // Revert the checkbox state since operation failed
                AppState.RunOnStartup = !AppState.RunOnStartup;
                StartupCheckBox.IsChecked = AppState.RunOnStartup;
            }
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
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
                    
                    MessageBox.Show("All data has been successfully deleted.", "Data Cleared");
                    
                    // Restart tracking
                    if (AppState.TrackingService != null)
                    {
                        AppState.TrackingService.Start();
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error clearing data: {ex.Message}", "Error");
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
            
            MessageBox.Show("Trial period has been manually expired for testing.", "Testing");
        }
#endif

        private void EnableAICheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // If enabling AI and trial expired with no license, prevent it
            if (EnableAICheckBox.IsChecked == true && AppState.TrialExpired && !AppState.LicenseValid)
            {
                MessageBox.Show(
                    "Your trial period has expired. Please enter a license key to use premium features.",
                    "Trial Expired", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                
                // Prevent checking the box
                EnableAICheckBox.IsChecked = false;
                return;
            }
            
            // Update AppState to match checkbox
            AppState.AIEnabled = EnableAICheckBox.IsChecked ?? false;
            
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
                    MessageBox.Show(
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

        // Add handler for dark mode checkbox
        private void DarkModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isDarkModeEnabled = DarkModeCheckBox.IsChecked ?? false;
            AppState.EnableDarkMode = isDarkModeEnabled;
            
            // Save the setting
            SettingsManager.SaveSetting("EnableDarkMode", isDarkModeEnabled.ToString());
            
            // Apply the theme
            var app = Application.Current as App;
            if (app != null)
            {
                if (isDarkModeEnabled)
                {
                    app.ApplyDarkTheme();
                    Logger.LogInfo("Dark mode enabled by user");
                }
                else
                {
                    app.ApplyLightTheme();
                    Logger.LogInfo("Light mode enabled by user");
                }
            }
        }

        /// <summary>
        /// Tests the OpenAI API key
        /// </summary>
        private async void TestApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyPasswordBox.Password.Trim();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                ApiKeyTestResult.Text = "Please enter an API key first";
                ApiKeyTestResult.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            
            // Variables for button state
            var button = sender as Button;
            string buttonContent = button?.Content.ToString() ?? "Test API Key";
            
            try
            {
                // Show a simple progress indicator
                if (button != null) 
                {
                    button.Content = "Testing...";
                    button.IsEnabled = false;
                }
                
                ApiKeyTestResult.Text = "Testing API key...";
                ApiKeyTestResult.Foreground = System.Windows.Media.Brushes.Gray;
                
                // Test the API key
                bool isValid = await OpenAIClient.TestConnectionAsync(apiKey);
                
                if (isValid)
                {
                    ApiKeyTestResult.Text = "API key is valid!";
                    ApiKeyTestResult.Foreground = System.Windows.Media.Brushes.Green;
                    Logger.LogInfo("OpenAI API key test successful");
                }
                else
                {
                    ApiKeyTestResult.Text = "API key is invalid";
                    ApiKeyTestResult.Foreground = System.Windows.Media.Brushes.Red;
                    Logger.LogWarning("OpenAI API key test failed - invalid key");
                }
            }
            catch (Exception ex)
            {
                ApiKeyTestResult.Text = "Error testing API key";
                ApiKeyTestResult.Foreground = System.Windows.Media.Brushes.Red;
                Logger.LogException(ex, "TestApiKey");
            }
            finally
            {
                // Ensure button is reset
                if (button != null)
                {
                    button.Content = buttonContent;
                    button.IsEnabled = true;
                }
            }
        }
    }
} 