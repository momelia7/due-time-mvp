using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace DueTime.UI.Utilities
{
    /// <summary>
    /// Handles communication with the DueTime background tracking service
    /// </summary>
    public static class ServiceCommunication
    {
        private const string ServiceName = "DueTimeTracker";
        private const string ServiceDisplayName = "DueTime Time Tracking Service";
        private const string ServiceDescription = "Records time entries based on active windows and user activity";
        
        /// <summary>
        /// Checks if the background service is installed
        /// </summary>
        public static bool IsServiceInstalled()
        {
            try
            {
                var services = ServiceController.GetServices();
                foreach (var service in services)
                {
                    if (service.ServiceName == ServiceName)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "IsServiceInstalled");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the background service is running
        /// </summary>
        public static bool IsServiceRunning()
        {
            try
            {
                var services = ServiceController.GetServices();
                foreach (var service in services)
                {
                    if (service.ServiceName == ServiceName)
                    {
                        return service.Status == ServiceControllerStatus.Running;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "IsServiceRunning");
                return false;
            }
        }
        
        /// <summary>
        /// Installs the background service
        /// </summary>
        public static async Task<bool> InstallServiceAsync()
        {
            try
            {
                // Check if the service is already installed
                if (IsServiceInstalled())
                {
                    Logger.LogInfo("Service is already installed");
                    return true;
                }
                
                // Get the path to the service executable
                string servicePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "DueTime.Service.exe");
                
                // Make sure the service executable exists
                if (!File.Exists(servicePath))
                {
                    Logger.LogError($"Service executable not found at {servicePath}");
                    return false;
                }
                
                // Use sc.exe to install the service (requires admin privileges)
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create {ServiceName} binPath= \"{servicePath}\" start= auto DisplayName= \"{ServiceDisplayName}\"",
                    Verb = "runas", // Run as administrator
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    bool success = process.ExitCode == 0;
                    
                    if (success)
                    {
                        // Set service description
                        var descProcess = new ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = $"description {ServiceName} \"{ServiceDescription}\"",
                            Verb = "runas", // Run as administrator
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };
                        
                        var descProcessInstance = Process.Start(descProcess);
                        if (descProcessInstance != null)
                        {
                            await descProcessInstance.WaitForExitAsync();
                        }
                        
                        Logger.LogInfo("Service installed successfully");
                        
                        // Start the service
                        return await StartServiceAsync();
                    }
                    else
                    {
                        Logger.LogError($"Failed to install service, exit code: {process.ExitCode}");
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "InstallService");
                return false;
            }
        }
        
        /// <summary>
        /// Uninstalls the background service
        /// </summary>
        public static async Task<bool> UninstallServiceAsync()
        {
            try
            {
                // Check if the service is installed
                if (!IsServiceInstalled())
                {
                    Logger.LogInfo("Service is not installed");
                    return true;
                }
                
                // Stop the service first
                await StopServiceAsync();
                
                // Use sc.exe to uninstall the service (requires admin privileges)
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete {ServiceName}",
                    Verb = "runas", // Run as administrator
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    bool success = process.ExitCode == 0;
                    
                    if (success)
                    {
                        Logger.LogInfo("Service uninstalled successfully");
                    }
                    else
                    {
                        Logger.LogError($"Failed to uninstall service, exit code: {process.ExitCode}");
                    }
                    
                    return success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UninstallService");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the background service
        /// </summary>
        public static async Task<bool> StartServiceAsync()
        {
            try
            {
                // Check if the service is installed
                if (!IsServiceInstalled())
                {
                    Logger.LogError("Cannot start service: service is not installed");
                    return false;
                }
                
                // Check if the service is already running
                if (IsServiceRunning())
                {
                    Logger.LogInfo("Service is already running");
                    return true;
                }
                
                // Use sc.exe to start the service (requires admin privileges)
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"start {ServiceName}",
                    Verb = "runas", // Run as administrator
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    bool success = process.ExitCode == 0;
                    
                    if (success)
                    {
                        Logger.LogInfo("Service started successfully");
                    }
                    else
                    {
                        Logger.LogError($"Failed to start service, exit code: {process.ExitCode}");
                    }
                    
                    return success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "StartService");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the background service
        /// </summary>
        public static async Task<bool> StopServiceAsync()
        {
            try
            {
                // Check if the service is installed
                if (!IsServiceInstalled())
                {
                    Logger.LogError("Cannot stop service: service is not installed");
                    return false;
                }
                
                // Check if the service is already stopped
                if (!IsServiceRunning())
                {
                    Logger.LogInfo("Service is already stopped");
                    return true;
                }
                
                // Use sc.exe to stop the service (requires admin privileges)
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"stop {ServiceName}",
                    Verb = "runas", // Run as administrator
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    bool success = process.ExitCode == 0;
                    
                    if (success)
                    {
                        Logger.LogInfo("Service stopped successfully");
                    }
                    else
                    {
                        Logger.LogError($"Failed to stop service, exit code: {process.ExitCode}");
                    }
                    
                    return success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "StopService");
                return false;
            }
        }
        
        /// <summary>
        /// Sets up the service to run on system startup
        /// </summary>
        public static async Task<bool> SetupServiceAutoStartAsync(bool enableAutoStart)
        {
            try
            {
                if (enableAutoStart)
                {
                    // Make sure the service is installed
                    if (!IsServiceInstalled())
                    {
                        bool installed = await InstallServiceAsync();
                        if (!installed)
                        {
                            return false;
                        }
                    }
                    
                    // Set service startup type to automatic
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"config {ServiceName} start= auto",
                        Verb = "runas", // Run as administrator
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    
                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        bool success = process.ExitCode == 0;
                        
                        if (success)
                        {
                            Logger.LogInfo("Service auto-start enabled");
                        }
                        else
                        {
                            Logger.LogError($"Failed to enable service auto-start, exit code: {process.ExitCode}");
                        }
                        
                        return success;
                    }
                }
                else
                {
                    // Set service startup type to manual
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"config {ServiceName} start= demand",
                        Verb = "runas", // Run as administrator
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    
                    var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        bool success = process.ExitCode == 0;
                        
                        if (success)
                        {
                            Logger.LogInfo("Service auto-start disabled");
                        }
                        else
                        {
                            Logger.LogError($"Failed to disable service auto-start, exit code: {process.ExitCode}");
                        }
                        
                        return success;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "SetupServiceAutoStart");
                return false;
            }
        }
    }
} 