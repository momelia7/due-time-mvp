using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace DueTime.Tracking.Services
{
    // Create a dummy interface to allow compilation until we can properly reference the real interface
    public interface IExplorerService
    {
        string? GetActiveExplorerPath();
    }
}

namespace DueTime.UI.ViewModels
{
    public class SettingsViewModel
    {
        private readonly DueTime.Tracking.Services.IExplorerService _explorerService;
        public ICommand AssignFolderCommand { get; }

        public SettingsViewModel(DueTime.Tracking.Services.IExplorerService explorerService)
        {
            _explorerService = explorerService;
            // Initialize the command with an execute action and no specific canExecute logic
            AssignFolderCommand = new RelayCommand(_ => ExecuteAssignFolder());
        }

        private void ExecuteAssignFolder()
        {
            try
            {
                string? folderPath = _explorerService.GetActiveExplorerPath();
                if (folderPath == null)
                {
                    System.Windows.MessageBox.Show("No active File Explorer window found. Please open a folder in Explorer and try again.",
                                    "Folder Assignment", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Persist the folder path (save to a file in AppData\DueTime directory)
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DueTime");
                if (!Directory.Exists(appDataDir))
                    Directory.CreateDirectory(appDataDir);
                string filePath = Path.Combine(appDataDir, "AssignedFolder.txt");
                File.WriteAllText(filePath, folderPath);

                System.Windows.MessageBox.Show($"Folder successfully assigned:\n{folderPath}", 
                                "Folder Assignment", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to assign folder: {ex.Message}", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 