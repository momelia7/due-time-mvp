using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DueTime.UI.Utilities;

namespace DueTime.UI.Views
{
    /// <summary>
    /// Interaction logic for ServiceManagementView.xaml
    /// </summary>
    public partial class ServiceManagementView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private bool _isServiceInstalled;
        private bool _isServiceRunning;
        private bool _autoStartEnabled;
        private bool _isOperationInProgress;
        private string _statusMessage = string.Empty;
        
        public ServiceManagementView()
        {
            InitializeComponent();
            DataContext = this;
            
            // Initial refresh of service status
            RefreshServiceStatus();
        }
        
        #region Properties
        
        public bool IsServiceInstalled
        {
            get => _isServiceInstalled;
            set
            {
                if (_isServiceInstalled != value)
                {
                    _isServiceInstalled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanInstall));
                    OnPropertyChanged(nameof(CanUninstall));
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(CanStop));
                    OnPropertyChanged(nameof(ServiceStatusText));
                    OnPropertyChanged(nameof(ServiceStatusColor));
                }
            }
        }
        
        public bool IsServiceRunning
        {
            get => _isServiceRunning;
            set
            {
                if (_isServiceRunning != value)
                {
                    _isServiceRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(CanStop));
                    OnPropertyChanged(nameof(ServiceStatusText));
                    OnPropertyChanged(nameof(ServiceStatusColor));
                }
            }
        }
        
        public bool AutoStartEnabled
        {
            get => _autoStartEnabled;
            set
            {
                if (_autoStartEnabled != value)
                {
                    _autoStartEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public bool IsOperationInProgress
        {
            get => _isOperationInProgress;
            set
            {
                if (_isOperationInProgress != value)
                {
                    _isOperationInProgress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanInstall));
                    OnPropertyChanged(nameof(CanUninstall));
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(CanStop));
                    OnPropertyChanged(nameof(ProgressVisibility));
                }
            }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusMessageVisibility));
                }
            }
        }
        
        public bool CanInstall => !IsOperationInProgress && !IsServiceInstalled;
        public bool CanUninstall => !IsOperationInProgress && IsServiceInstalled;
        public bool CanStart => !IsOperationInProgress && IsServiceInstalled && !IsServiceRunning;
        public bool CanStop => !IsOperationInProgress && IsServiceInstalled && IsServiceRunning;
        
        public string ServiceStatusText
        {
            get
            {
                if (!IsServiceInstalled)
                    return "Not Installed";
                if (IsServiceRunning)
                    return "Running";
                return "Stopped";
            }
        }
        
        public System.Windows.Media.Brush ServiceStatusColor
        {
            get
            {
                if (!IsServiceInstalled)
                    return System.Windows.Media.Brushes.Gray;
                if (IsServiceRunning)
                    return System.Windows.Media.Brushes.Green;
                return System.Windows.Media.Brushes.Red;
            }
        }
        
        public Visibility StatusMessageVisibility => string.IsNullOrEmpty(StatusMessage) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ProgressVisibility => IsOperationInProgress ? Visibility.Visible : Visibility.Collapsed;
        
        #endregion
        
        #region Event Handlers
        
        private async void InstallService_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteServiceOperation(async () =>
            {
                StatusMessage = "Installing service...";
                bool success = await ServiceCommunication.InstallServiceAsync();
                StatusMessage = success ? "Service installed successfully." : "Failed to install service.";
                return success;
            });
        }
        
        private async void UninstallService_Click(object sender, RoutedEventArgs e)
        {
            // Confirm with the user
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to uninstall the background service? This will stop all background tracking.",
                "Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result != MessageBoxResult.Yes)
                return;
                
            await ExecuteServiceOperation(async () =>
            {
                StatusMessage = "Uninstalling service...";
                bool success = await ServiceCommunication.UninstallServiceAsync();
                StatusMessage = success ? "Service uninstalled successfully." : "Failed to uninstall service.";
                return success;
            });
        }
        
        private async void StartService_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteServiceOperation(async () =>
            {
                StatusMessage = "Starting service...";
                bool success = await ServiceCommunication.StartServiceAsync();
                StatusMessage = success ? "Service started successfully." : "Failed to start service.";
                return success;
            });
        }
        
        private async void StopService_Click(object sender, RoutedEventArgs e)
        {
            // Confirm with the user
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to stop the background service? This will stop all background tracking.",
                "Confirm Stop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result != MessageBoxResult.Yes)
                return;
                
            await ExecuteServiceOperation(async () =>
            {
                StatusMessage = "Stopping service...";
                bool success = await ServiceCommunication.StopServiceAsync();
                StatusMessage = success ? "Service stopped successfully." : "Failed to stop service.";
                return success;
            });
        }
        
        private async void AutoStart_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((System.Windows.Controls.CheckBox)sender).IsChecked ?? false;
            
            await ExecuteServiceOperation(async () =>
            {
                StatusMessage = isChecked ? "Enabling auto-start..." : "Disabling auto-start...";
                bool success = await ServiceCommunication.SetupServiceAutoStartAsync(isChecked);
                StatusMessage = success 
                    ? (isChecked ? "Auto-start enabled successfully." : "Auto-start disabled successfully.") 
                    : (isChecked ? "Failed to enable auto-start." : "Failed to disable auto-start.");
                return success;
            });
        }
        
        #endregion
        
        #region Helper Methods
        
        private void RefreshServiceStatus()
        {
            try
            {
                IsServiceInstalled = ServiceCommunication.IsServiceInstalled();
                IsServiceRunning = ServiceCommunication.IsServiceRunning();
                
                // TODO: Check auto-start status
                // This requires reading from registry or service configuration
                AutoStartEnabled = IsServiceInstalled; // Simplification for now
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "RefreshServiceStatus");
                StatusMessage = "Error checking service status.";
            }
        }
        
        private async Task<bool> ExecuteServiceOperation(Func<Task<bool>> operation)
        {
            try
            {
                IsOperationInProgress = true;
                
                bool success = await operation();
                
                // Refresh service status after operation
                RefreshServiceStatus();
                
                return success;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "ExecuteServiceOperation");
                StatusMessage = $"Error: {ex.Message}";
                return false;
            }
            finally
            {
                IsOperationInProgress = false;
            }
        }
        
        #endregion
        
        #region INotifyPropertyChanged Implementation
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
} 