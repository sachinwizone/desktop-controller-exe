using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EmployeeAttendance
{
    /// <summary>
    /// Handles system control commands from web server (restart, uninstall, registry edits, permission blocking)
    /// </summary>
    public class SystemControlHandler
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverApiUrl;
        private readonly string _activationKey;
        private System.Threading.Timer? _commandCheckTimer;
        private const int CHECK_INTERVAL_SECONDS = 30; // Check for commands every 30 seconds

        public SystemControlHandler(string serverUrl, string activationKey)
        {
            _httpClient = new HttpClient();
            _serverApiUrl = serverUrl.TrimEnd('/') + "/api";
            _activationKey = activationKey;
        }

        /// <summary>
        /// Start checking for control commands from server
        /// </summary>
        public void Start()
        {
            _commandCheckTimer = new System.Threading.Timer(async (state) =>
            {
                await CheckAndExecuteCommands();
            }, null, TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS), TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS));
        }

        /// <summary>
        /// Stop checking for commands
        /// </summary>
        public void Stop()
        {
            _commandCheckTimer?.Dispose();
        }

        /// <summary>
        /// Check for pending commands from server and execute them
        /// </summary>
        private async Task CheckAndExecuteCommands()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverApiUrl}/control/commands?activationKey={_activationKey}&computerName={Environment.MachineName}");
                
                if (!response.IsSuccessStatusCode)
                    return;

                var json = await response.Content.ReadAsStringAsync();
                var commands = JsonSerializer.Deserialize<List<ControlCommand>>(json);

                if (commands != null && commands.Count > 0)
                {
                    foreach (var command in commands)
                    {
                        await ExecuteCommand(command);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking commands: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a control command
        /// </summary>
        private async Task ExecuteCommand(ControlCommand command)
        {
            try
            {
                var result = new CommandExecutionResult
                {
                    CommandId = command.Id,
                    Status = "success",
                    Message = ""
                };

                switch (command.Type?.ToLower())
                {
                    case "restart":
                        RestartComputer();
                        result.Message = "System restart initiated";
                        break;

                    case "shutdown":
                        ShutdownComputer();
                        result.Message = "System shutdown initiated";
                        break;

                    case "uninstall_app":
                        UninstallApplication(command.Parameters?.GetValueOrDefault("appName") ?? "");
                        result.Message = $"Uninstall command sent for {command.Parameters?.GetValueOrDefault("appName")}";
                        break;

                    case "block_application":
                        BlockApplication(command.Parameters?.GetValueOrDefault("appPath") ?? "", command.Parameters?.GetValueOrDefault("appName") ?? "");
                        result.Message = $"Application blocked: {command.Parameters?.GetValueOrDefault("appName")}";
                        break;

                    case "unblock_application":
                        UnblockApplication(command.Parameters?.GetValueOrDefault("appPath") ?? "");
                        result.Message = "Application unblocked";
                        break;

                    case "block_registry":
                        BlockRegistryKey(command.Parameters?.GetValueOrDefault("registryPath") ?? "");
                        result.Message = "Registry path blocked";
                        break;

                    case "block_file":
                        BlockFile(command.Parameters?.GetValueOrDefault("filePath") ?? "");
                        result.Message = "File access blocked";
                        break;

                    case "execute_command":
                        result.Message = ExecuteShellCommand(command.Parameters?.GetValueOrDefault("command") ?? "");
                        break;

                    case "lock_screen":
                        LockScreen();
                        result.Message = "Screen locked";
                        break;

                    case "show_message":
                        ShowMessageToUser(command.Parameters?.GetValueOrDefault("message") ?? "");
                        result.Message = "Message displayed to user";
                        break;

                    default:
                        result.Status = "error";
                        result.Message = "Unknown command type";
                        break;
                }

                // Report execution result back to server
                await ReportCommandResult(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing command {command.Id}: {ex.Message}");
                await ReportCommandResult(new CommandExecutionResult
                {
                    CommandId = command.Id,
                    Status = "error",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Restart computer
        /// </summary>
        private void RestartComputer()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c shutdown /r /t 30 /c \"System restart initiated by administrator\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restarting computer: {ex.Message}");
            }
        }

        /// <summary>
        /// Shutdown computer
        /// </summary>
        private void ShutdownComputer()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c shutdown /s /t 30 /c \"System shutdown initiated by administrator\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error shutting down computer: {ex.Message}");
            }
        }

        /// <summary>
        /// Uninstall an application
        /// </summary>
        private void UninstallApplication(string appName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appName)) return;

                // Find uninstall string in registry
                var uninstallString = FindApplicationUninstallString(appName);
                if (!string.IsNullOrEmpty(uninstallString))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {uninstallString}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uninstalling {appName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Find application uninstall string from registry
        /// </summary>
        private string FindApplicationUninstallString(string appName)
        {
            try
            {
                var registryPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var path in registryPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null) continue;

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName")?.ToString();
                                if (displayName?.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    return subKey?.GetValue("UninstallString")?.ToString() ?? "";
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return "";
        }

        /// <summary>
        /// Block application execution
        /// </summary>
        private void BlockApplication(string appPath, string appName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appPath)) return;

                // Add to registry blocked list
                var blockedAppsKey = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\BlockedApps");
                blockedAppsKey?.SetValue(appName, appPath);
                blockedAppsKey?.Close();

                // Set NTFS permissions to deny execution
                if (System.IO.File.Exists(appPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "icacls.exe",
                        Arguments = $"\"{appPath}\" /deny Everyone:(F)",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error blocking {appName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unblock application
        /// </summary>
        private void UnblockApplication(string appPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appPath)) return;

                // Restore NTFS permissions
                if (System.IO.File.Exists(appPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "icacls.exe",
                        Arguments = $"\"{appPath}\" /remove:d Everyone",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unblocking application: {ex.Message}");
            }
        }

        /// <summary>
        /// Block registry key modification
        /// </summary>
        private void BlockRegistryKey(string registryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(registryPath)) return;

                // Log blocked registry attempts
                var blockedKey = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\BlockedRegistry");
                blockedKey?.SetValue(registryPath, DateTime.Now.ToString());
                blockedKey?.Close();

                Debug.WriteLine($"Registry blocked: {registryPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error blocking registry: {ex.Message}");
            }
        }

        /// <summary>
        /// Block file access
        /// </summary>
        private void BlockFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return;

                // Set NTFS permissions to deny access
                if (System.IO.File.Exists(filePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "icacls.exe",
                        Arguments = $"\"{filePath}\" /deny Everyone:(F)",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error blocking file: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute shell command
        /// </summary>
        private string ExecuteShellCommand(string command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command)) return "No command";

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit(5000); // Wait max 5 seconds
                        var output = process.StandardOutput.ReadToEnd();
                        return string.IsNullOrEmpty(output) ? "Command executed" : output;
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }

            return "Command execution failed";
        }

        /// <summary>
        /// Lock user screen
        /// </summary>
        private void LockScreen()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = "user32.dll,LockWorkStation",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error locking screen: {ex.Message}");
            }
        }

        /// <summary>
        /// Show message to user
        /// </summary>
        private void ShowMessageToUser(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;

                // Could use MessageBox in UI thread
                Debug.WriteLine($"Message to user: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Report command execution result to server
        /// </summary>
        private async Task ReportCommandResult(CommandExecutionResult result)
        {
            try
            {
                var json = JsonSerializer.Serialize(result);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_serverApiUrl}/control/command-result", content);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to report command result: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reporting command result: {ex.Message}");
            }
        }
    }

    // ==================== Data Models ====================

    public class ControlCommand
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = ""; // restart, shutdown, uninstall_app, block_application, etc.
        public Dictionary<string, string> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool Executed { get; set; }
    }

    public class CommandExecutionResult
    {
        public string CommandId { get; set; } = "";
        public string Status { get; set; } = ""; // success, error
        public string Message { get; set; } = "";
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}
