using System.Windows;
using System.Windows.Controls;
using DueTime.Data;

namespace DueTime.UI.Views
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
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
    }
} 