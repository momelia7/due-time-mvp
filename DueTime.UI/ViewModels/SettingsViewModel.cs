using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DueTime.Tracking.Services;

namespace DueTime.UI.ViewModels
{
    public class SettingsViewModel
    {
        private readonly IExplorerService _explorerService;
        public ICommand AssignFolderCommand { get; }

        public SettingsViewModel(IExplorerService explorerService)
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
                    MessageBox.Show("No active File Explorer window found. Please open a folder in Explorer and try again.",
                                    "Folder Assignment", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Persist the folder path (save to a file in AppData\DueTime directory)
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DueTime");
                if (!Directory.Exists(appDataDir))
                    Directory.CreateDirectory(appDataDir);
                string filePath = Path.Combine(appDataDir, "AssignedFolder.txt");
                File.WriteAllText(filePath, folderPath);

                MessageBox.Show($"Folder successfully assigned:\n{folderPath}", 
                                "Folder Assignment", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to assign folder: {ex.Message}", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// A simple ICommand implementation for relay commands.
    /// </summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public event EventHandler? CanExecuteChanged;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
} 