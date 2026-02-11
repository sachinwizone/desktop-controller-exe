using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EmployeeAttendance
{
    /// <summary>
    /// Handles system control commands from web dashboard via direct database polling.
    /// Commands: restart, shutdown, lock_screen, show_message, uninstall_app,
    /// block_application, unblock_application, change_password, execute_command
    /// </summary>
    public class SystemControlHandler
    {
        private readonly string _companyName;
        private readonly string _systemName;
        private readonly string _activationKey;
        private System.Threading.Timer? _commandCheckTimer;
        private const int CHECK_INTERVAL_SECONDS = 10; // Poll every 10 seconds for faster response
        private volatile bool _isShowingMessage = false; // Prevent multiple message popups
        private readonly HashSet<int> _processedCommandIds = new HashSet<int>(); // Track processed commands

        public SystemControlHandler(string companyName, string systemName, string activationKey)
        {
            _companyName = companyName;
            _systemName = systemName;
            _activationKey = activationKey;
        }

        /// <summary>
        /// Start polling for control commands from database
        /// </summary>
        public void Start()
        {
            // Re-apply active restrictions on startup
            try
            {
                SystemRestrictions.ReapplyActiveRestrictions(_companyName, _systemName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemControl] Error re-applying restrictions: {ex.Message}");
            }

            _commandCheckTimer = new System.Threading.Timer(_ =>
            {
                CheckAndExecuteCommands();
            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS)); // First check after 2s, then every 10s

            Debug.WriteLine("[SystemControl] Started command polling");
        }

        /// <summary>
        /// Stop polling for commands
        /// </summary>
        public void Stop()
        {
            _commandCheckTimer?.Dispose();
            Debug.WriteLine("[SystemControl] Stopped command polling");
        }

        /// <summary>
        /// Check for pending commands in database and execute them
        /// </summary>
        private void CheckAndExecuteCommands()
        {
            try
            {
                var commands = DatabaseHelper.GetPendingCommands(_systemName, _companyName);

                if (commands.Count > 0)
                {
                    Debug.WriteLine($"[SystemControl] Found {commands.Count} pending commands");
                }

                foreach (var (id, commandType, parametersJson) in commands)
                {
                    // Skip already processed commands to prevent duplicates
                    if (_processedCommandIds.Contains(id)) continue;
                    _processedCommandIds.Add(id);

                    ExecuteCommand(id, commandType, parametersJson);
                }

                // Clean up old processed IDs (keep only last 100)
                if (_processedCommandIds.Count > 200)
                {
                    var sorted = new List<int>(_processedCommandIds);
                    sorted.Sort();
                    for (int i = 0; i < sorted.Count - 100; i++)
                        _processedCommandIds.Remove(sorted[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemControl] Error checking commands: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a control command and report result
        /// </summary>
        private void ExecuteCommand(int commandId, string commandType, string parametersJson)
        {
            string status = "completed";
            string resultMessage = "";

            try
            {
                // Mark as executing
                DatabaseHelper.UpdateCommandStatus(commandId, "executing", "Executing...");

                var parameters = new Dictionary<string, string>();
                try
                {
                    if (!string.IsNullOrWhiteSpace(parametersJson) && parametersJson != "{}")
                    {
                        var jsonDoc = JsonDocument.Parse(parametersJson);
                        foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                        {
                            parameters[prop.Name] = prop.Value.GetString() ?? prop.Value.ToString();
                        }
                    }
                }
                catch { }

                switch (commandType?.ToLower())
                {
                    case "restart":
                        RestartComputer();
                        resultMessage = "System restart initiated (30 second delay)";
                        break;

                    case "shutdown":
                        ShutdownComputer();
                        resultMessage = "System shutdown initiated (30 second delay)";
                        break;

                    case "lock_screen":
                        LockScreen();
                        resultMessage = "Screen locked";
                        break;

                    case "show_message":
                        ShowMessageToUser(parameters.GetValueOrDefault("message") ?? "");
                        resultMessage = "Message displayed to user";
                        break;

                    case "uninstall_app":
                        var appName = parameters.GetValueOrDefault("appName") ?? "";
                        UninstallApplication(appName);
                        resultMessage = $"Uninstall initiated for '{appName}'";
                        break;

                    case "block_application":
                        BlockApplication(
                            parameters.GetValueOrDefault("appPath") ?? "",
                            parameters.GetValueOrDefault("appName") ?? "");
                        resultMessage = $"Application blocked: {parameters.GetValueOrDefault("appName")}";
                        break;

                    case "unblock_application":
                        UnblockApplication(parameters.GetValueOrDefault("appPath") ?? "");
                        resultMessage = "Application unblocked";
                        break;

                    case "change_password":
                        var user = parameters.GetValueOrDefault("username") ?? "";
                        var newPwd = parameters.GetValueOrDefault("newPassword") ?? "";
                        resultMessage = ChangeUserPassword(user, newPwd);
                        break;

                    case "execute_command":
                        resultMessage = ExecuteShellCommand(parameters.GetValueOrDefault("command") ?? "");
                        break;

                    case "block_registry":
                        BlockRegistryKey(parameters.GetValueOrDefault("registryPath") ?? "");
                        resultMessage = "Registry path blocked";
                        break;

                    case "block_file":
                        BlockFile(parameters.GetValueOrDefault("filePath") ?? "");
                        resultMessage = "File access blocked";
                        break;

                    // ==================== DANGER ZONE COMMANDS ====================
                    case "block_cmd":
                        BlockCMD(true);
                        resultMessage = "CMD has been blocked";
                        break;

                    case "unblock_cmd":
                        BlockCMD(false);
                        resultMessage = "CMD has been unblocked";
                        break;

                    case "block_powershell":
                        BlockPowerShell(true);
                        resultMessage = "PowerShell has been blocked";
                        break;

                    case "unblock_powershell":
                        BlockPowerShell(false);
                        resultMessage = "PowerShell has been unblocked";
                        break;

                    case "block_regedit":
                        BlockRegedit(true);
                        resultMessage = "Registry Editor has been blocked";
                        break;

                    case "unblock_regedit":
                        BlockRegedit(false);
                        resultMessage = "Registry Editor has been unblocked";
                        break;

                    case "block_copy_paste":
                        BlockCopyPaste(true);
                        resultMessage = "Copy/Paste has been blocked";
                        break;

                    case "unblock_copy_paste":
                        BlockCopyPaste(false);
                        resultMessage = "Copy/Paste has been unblocked";
                        break;

                    case "block_software_install":
                        BlockSoftwareInstall(true);
                        resultMessage = "Software installation has been blocked";
                        break;

                    case "unblock_software_install":
                        BlockSoftwareInstall(false);
                        resultMessage = "Software installation has been unblocked";
                        break;

                    case "block_delete":
                        BlockDeleteFunction(true);
                        resultMessage = "Delete function has been disabled";
                        break;

                    case "unblock_delete":
                        BlockDeleteFunction(false);
                        resultMessage = "Delete function has been enabled";
                        break;

                    case "full_system_restriction":
                        ApplyFullSystemRestriction(true);
                        resultMessage = "Full system restriction applied";
                        break;

                    case "remove_full_restriction":
                        ApplyFullSystemRestriction(false);
                        resultMessage = "Full system restriction removed";
                        break;

                    case "lock_ip":
                        LockIPAddress(true);
                        resultMessage = "IP address has been locked";
                        break;

                    case "unlock_ip":
                        LockIPAddress(false);
                        resultMessage = "IP address has been unlocked";
                        break;

                    case "block_task_manager":
                        BlockTaskManager(true);
                        resultMessage = "Task Manager has been blocked";
                        break;

                    case "unblock_task_manager":
                        BlockTaskManager(false);
                        resultMessage = "Task Manager has been unblocked";
                        break;

                    case "block_control_panel":
                        BlockControlPanel(true);
                        resultMessage = "Control Panel has been blocked";
                        break;

                    case "unblock_control_panel":
                        BlockControlPanel(false);
                        resultMessage = "Control Panel has been unblocked";
                        break;

                    default:
                        status = "failed";
                        resultMessage = $"Unknown command type: {commandType}";
                        break;
                }
            }
            catch (Exception ex)
            {
                status = "failed";
                resultMessage = $"Error: {ex.Message}";
                Debug.WriteLine($"[SystemControl] Command {commandId} failed: {ex.Message}");
            }

            // Report result
            DatabaseHelper.UpdateCommandStatus(commandId, status, resultMessage);
            Debug.WriteLine($"[SystemControl] Command {commandId} ({commandType}): {status} - {resultMessage}");
        }

        // ==================== COMMAND IMPLEMENTATIONS ====================

        private void RestartComputer()
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

        private void ShutdownComputer()
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

        private void LockScreen()
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

        private void ShowMessageToUser(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // Prevent multiple message popups
            if (_isShowingMessage)
            {
                Debug.WriteLine("[SystemControl] Already showing a message, skipping duplicate");
                return;
            }

            _isShowingMessage = true;
            // Show single message notification on a new STA thread
            ShowAdminNotification("Administrator Message", message, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Show a topmost notification form that appears above all windows
        /// </summary>
        private void ShowAdminNotification(string title, string message, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            try
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        var form = new Form
                        {
                            Text = title,
                            Size = new System.Drawing.Size(450, 250),
                            StartPosition = FormStartPosition.CenterScreen,
                            TopMost = true,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            MaximizeBox = false,
                            MinimizeBox = false,
                            BackColor = System.Drawing.Color.White,
                            ShowInTaskbar = true
                        };

                        var iconLabel = new Label
                        {
                            Text = icon == MessageBoxIcon.Warning ? "\u26A0" : "\uD83D\uDCE2",
                            Font = new System.Drawing.Font("Segoe UI", 28),
                            Location = new System.Drawing.Point(20, 20),
                            AutoSize = true
                        };
                        form.Controls.Add(iconLabel);

                        var titleLabel = new Label
                        {
                            Text = title,
                            Font = new System.Drawing.Font("Segoe UI", 13, System.Drawing.FontStyle.Bold),
                            Location = new System.Drawing.Point(80, 20),
                            Size = new System.Drawing.Size(340, 30),
                            ForeColor = System.Drawing.Color.FromArgb(30, 41, 59)
                        };
                        form.Controls.Add(titleLabel);

                        var companyLabel = new Label
                        {
                            Text = $"Company: {_companyName}",
                            Font = new System.Drawing.Font("Segoe UI", 9),
                            Location = new System.Drawing.Point(80, 50),
                            Size = new System.Drawing.Size(340, 20),
                            ForeColor = System.Drawing.Color.Gray
                        };
                        form.Controls.Add(companyLabel);

                        var msgLabel = new Label
                        {
                            Text = message,
                            Font = new System.Drawing.Font("Segoe UI", 11),
                            Location = new System.Drawing.Point(20, 85),
                            Size = new System.Drawing.Size(400, 80),
                            ForeColor = System.Drawing.Color.FromArgb(51, 65, 85)
                        };
                        form.Controls.Add(msgLabel);

                        var okBtn = new Button
                        {
                            Text = "OK",
                            Size = new System.Drawing.Size(100, 35),
                            Location = new System.Drawing.Point(170, 170),
                            BackColor = System.Drawing.Color.FromArgb(59, 130, 246),
                            ForeColor = System.Drawing.Color.White,
                            FlatStyle = FlatStyle.Flat,
                            Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                            Cursor = Cursors.Hand
                        };
                        okBtn.Click += (s, e) => form.Close();
                        form.Controls.Add(okBtn);

                        form.FormClosed += (s, e) => { _isShowingMessage = false; };

                        Application.Run(form);
                    }
                    catch
                    {
                        _isShowingMessage = false;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                _isShowingMessage = false;
                Debug.WriteLine($"[SystemControl] ShowAdminNotification error: {ex.Message}");
            }
        }

        private void UninstallApplication(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) return;

            // Show admin notification to user (non-blocking)
            ShowAdminNotification(
                "Application Removal Notice",
                $"The application \"{appName}\" on your system is being removed by your administrator ({_companyName}) because it is against company policy.",
                MessageBoxIcon.Warning);

            // First kill the running application processes
            try
            {
                string processName = appName.Replace(".exe", "").Trim();
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (proc.ProcessName.IndexOf(processName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            proc.Kill();
                            proc.WaitForExit(5000);
                            Debug.WriteLine($"[SystemControl] Killed process before uninstall: {proc.ProcessName}");
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Wait a moment for processes to fully exit
            Thread.Sleep(2000);

            var (uninstallString, quietUninstallString) = FindApplicationUninstallStrings(appName);

            // Prefer QuietUninstallString if available
            var cmdToRun = !string.IsNullOrEmpty(quietUninstallString)
                ? quietUninstallString
                : MakeSilentUninstallCommand(uninstallString);

            if (!string.IsNullOrEmpty(cmdToRun))
            {
                // Parse the command to separate exe from arguments
                string exePath = "";
                string arguments = "";
                ParseCommand(cmdToRun, out exePath, out arguments);

                if (!string.IsNullOrEmpty(exePath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = arguments,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = true,
                            Verb = "runas" // Run elevated to avoid UAC prompt for the user
                        };
                        var proc = Process.Start(psi);
                        // Wait for the uninstall to complete (up to 120 seconds)
                        proc?.WaitForExit(120000);
                        Debug.WriteLine($"[SystemControl] Silent uninstall (elevated) completed for: {appName} | CMD: {exePath} {arguments}");
                    }
                    catch (Exception ex)
                    {
                        // Fallback: try via cmd.exe without elevation
                        Debug.WriteLine($"[SystemControl] Elevated uninstall failed: {ex.Message}, trying fallback...");
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c start /wait {cmdToRun}",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
                            var proc = Process.Start(psi);
                            proc?.WaitForExit(120000);
                        }
                        catch { }
                    }
                }
                else
                {
                    // Can't parse, use cmd.exe
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start /wait {cmdToRun}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var proc = Process.Start(psi);
                    proc?.WaitForExit(120000);
                }
                Debug.WriteLine($"[SystemControl] Silent uninstall completed for: {appName}");
            }
            else
            {
                // Fallback: Try WMIC for MSI-installed products
                Debug.WriteLine($"[SystemControl] No uninstall string found for: {appName}, trying WMIC fallback...");
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c wmic product where \"name like '%{appName.Replace("'", "''")}%'\" call uninstall /nointeractive",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    var proc = Process.Start(psi);
                    proc?.WaitForExit(120000);
                    var output = proc?.StandardOutput.ReadToEnd() ?? "";
                    Debug.WriteLine($"[SystemControl] WMIC uninstall result for {appName}: {output.Trim()}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SystemControl] WMIC uninstall failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Parse a command string into executable path and arguments
        /// </summary>
        private void ParseCommand(string command, out string exePath, out string arguments)
        {
            exePath = "";
            arguments = "";
            if (string.IsNullOrEmpty(command)) return;

            command = command.Trim();

            if (command.StartsWith("\""))
            {
                // Quoted path: "C:\path\to\exe.exe" /args
                int endQuote = command.IndexOf('"', 1);
                if (endQuote > 0)
                {
                    exePath = command.Substring(1, endQuote - 1);
                    arguments = command.Substring(endQuote + 1).Trim();
                }
            }
            else
            {
                // Check if it starts with MsiExec
                if (command.StartsWith("MsiExec", StringComparison.OrdinalIgnoreCase) ||
                    command.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
                {
                    exePath = "msiexec.exe";
                    int spaceIdx = command.IndexOf(' ');
                    arguments = spaceIdx > 0 ? command.Substring(spaceIdx + 1) : "";
                }
                else
                {
                    // Find the .exe part
                    int exeIdx = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                    if (exeIdx > 0)
                    {
                        exePath = command.Substring(0, exeIdx + 4);
                        arguments = command.Substring(exeIdx + 4).Trim();
                    }
                    else
                    {
                        // Just use the whole thing
                        int spaceIdx = command.IndexOf(' ');
                        if (spaceIdx > 0)
                        {
                            exePath = command.Substring(0, spaceIdx);
                            arguments = command.Substring(spaceIdx + 1);
                        }
                        else
                        {
                            exePath = command;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert uninstall string to silent mode by adding appropriate flags
        /// </summary>
        private string MakeSilentUninstallCommand(string uninstallString)
        {
            if (string.IsNullOrEmpty(uninstallString)) return "";

            var cmd = uninstallString.Trim();
            var cmdLower = cmd.ToLower();

            // Google Chrome - special handling
            if (cmdLower.Contains("chrome") && cmdLower.Contains("setup.exe"))
            {
                // Chrome uses --uninstall --force-uninstall --system-level
                if (!cmd.Contains("--force-uninstall"))
                {
                    cmd += " --force-uninstall";
                }
                return cmd;
            }

            // MSI Installer (MsiExec.exe)
            if (cmdLower.Contains("msiexec"))
            {
                // Replace /I (install/modify) with /X (uninstall) and add quiet flags
                cmd = System.Text.RegularExpressions.Regex.Replace(cmd, @"/I", "/X",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Remove any existing UI flags
                cmd = System.Text.RegularExpressions.Regex.Replace(cmd, @"\s*/q[nbrfu]?\s*", " ",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Add silent flags if not present
                if (!cmdLower.Contains("/quiet") && !cmdLower.Contains("/qn"))
                {
                    cmd += " /quiet /norestart";
                }
                return cmd;
            }

            // NSIS Installer (common for many apps like Chrome, Firefox, etc.)
            if (cmdLower.Contains("uninstall.exe") || cmdLower.Contains("uninst.exe"))
            {
                if (!cmd.Contains("/S") && !cmd.Contains("/s"))
                {
                    cmd += " /S";
                }
                return cmd;
            }

            // Inno Setup Installer
            if (cmdLower.Contains("unins") && cmdLower.Contains(".exe"))
            {
                if (!cmdLower.Contains("/verysilent") && !cmdLower.Contains("/silent"))
                {
                    cmd += " /VERYSILENT /NORESTART /SUPPRESSMSGBOXES";
                }
                return cmd;
            }

            // InstallShield
            if (cmdLower.Contains("installshield") || cmdLower.Contains("setup.exe"))
            {
                if (!cmd.Contains("-s") && !cmd.Contains("/s"))
                {
                    cmd += " /s /f1\"uninstall.iss\"";
                }
                return cmd;
            }

            // Generic - try common silent flags
            // Try /S first (NSIS style), then /SILENT, then /QUIET
            if (!cmd.Contains("/S") && !cmdLower.Contains("/silent") && !cmdLower.Contains("/quiet"))
            {
                cmd += " /S /SILENT /VERYSILENT /NORESTART";
            }

            return cmd;
        }

        private (string uninstallString, string quietUninstallString) FindApplicationUninstallStrings(string appName)
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
                                    var uninstall = subKey?.GetValue("UninstallString")?.ToString() ?? "";
                                    var quietUninstall = subKey?.GetValue("QuietUninstallString")?.ToString() ?? "";
                                    return (uninstall, quietUninstall);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return ("", "");
        }

        private void BlockApplication(string appPath, string appName)
        {
            if (string.IsNullOrWhiteSpace(appName) && string.IsNullOrWhiteSpace(appPath)) return;

            string targetName = appName;
            if (string.IsNullOrWhiteSpace(targetName)) targetName = appPath;

            // Show admin notification
            ShowAdminNotification(
                "Application Blocked",
                $"The application \"{targetName}\" on your system has been blocked by your administrator ({_companyName}) because it is against company policy.\n\nIf you have any issues, please contact your company administrator.",
                MessageBoxIcon.Warning);

            // Determine exe name from various sources
            string exeName = "";
            string exeFullPath = appPath;

            // Try to find exe from running process
            try
            {
                string processName = targetName.Replace(".exe", "").Trim();
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (proc.ProcessName.IndexOf(processName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            try { exeFullPath = proc.MainModule?.FileName ?? exeFullPath; } catch { }
                            exeName = proc.ProcessName + ".exe";
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Try to find from registry if we don't have it yet
            if (string.IsNullOrWhiteSpace(exeFullPath) || string.IsNullOrWhiteSpace(exeName))
            {
                try
                {
                    var (installLocation, foundExeName) = FindApplicationInstallPath(targetName);
                    if (!string.IsNullOrEmpty(installLocation))
                    {
                        if (string.IsNullOrEmpty(exeFullPath))
                        {
                            var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
                            if (exeFiles.Length > 0) exeFullPath = exeFiles[0];
                        }
                    }
                    if (!string.IsNullOrEmpty(foundExeName) && string.IsNullOrEmpty(exeName))
                        exeName = foundExeName;
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(exeName) && !string.IsNullOrWhiteSpace(exeFullPath))
                exeName = Path.GetFileName(exeFullPath);

            // Use enhanced 4-layer blocking
            SystemRestrictions.BlockApplicationEnhanced(targetName, exeFullPath ?? "", exeName ?? "");

            // Also try to block all exe files in install directory
            try
            {
                var (installLocation, _) = FindApplicationInstallPath(targetName);
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    string[] exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.AllDirectories);
                    foreach (var exeFile in exeFiles)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "icacls.exe",
                            Arguments = $"\"{exeFile}\" /deny Everyone:(RX)",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(psi);
                    }
                    Debug.WriteLine($"[SystemControl] Additionally blocked {exeFiles.Length} exe files in: {installLocation}");
                }
            }
            catch { }

            // Save block status to database (inline to avoid compilation issues)
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var conn = new Npgsql.NpgsqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new Npgsql.NpgsqlCommand(@"INSERT INTO blocked_applications (company_name, system_name, app_name, app_path, exe_name, blocked_at, is_blocked)
                        VALUES (@c, @s, @a, @p, @e, CURRENT_TIMESTAMP, TRUE)
                        ON CONFLICT (company_name, system_name, app_name) DO UPDATE SET app_path = @p, exe_name = @e, blocked_at = CURRENT_TIMESTAMP, is_blocked = TRUE", conn))
                    {
                        cmd.Parameters.AddWithValue("@c", _companyName ?? "");
                        cmd.Parameters.AddWithValue("@s", _systemName ?? "");
                        cmd.Parameters.AddWithValue("@a", targetName ?? "");
                        cmd.Parameters.AddWithValue("@p", exeFullPath ?? "");
                        cmd.Parameters.AddWithValue("@e", exeName ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                Debug.WriteLine($"[SystemControl] Block saved to DB: {targetName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemControl] Error saving blocked app to DB: {ex.Message}");
            }
        }

        /// <summary>
        /// Find application install path from registry
        /// </summary>
        private (string installLocation, string exeName) FindApplicationInstallPath(string appName)
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
                                    var installLoc = subKey?.GetValue("InstallLocation")?.ToString() ?? "";
                                    var displayIcon = subKey?.GetValue("DisplayIcon")?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(installLoc) && Directory.Exists(installLoc))
                                        return (installLoc, "");
                                    if (!string.IsNullOrEmpty(displayIcon))
                                    {
                                        var iconPath = displayIcon.Split(',')[0].Trim('"');
                                        if (File.Exists(iconPath))
                                            return (Path.GetDirectoryName(iconPath) ?? "", Path.GetFileName(iconPath));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return ("", "");
        }

        private void UnblockApplication(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath)) return;

            // Get the app name and exe name from parameters or path
            string appName = Path.GetFileNameWithoutExtension(appPath);
            string exeName = Path.GetFileName(appPath);

            // Use enhanced unblock (removes all 4 layers)
            SystemRestrictions.UnblockApplicationEnhanced(appName, appPath, exeName);

            // Also remove from install directory
            try
            {
                var (installLocation, _) = FindApplicationInstallPath(appName);
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    string[] exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.AllDirectories);
                    foreach (var exeFile in exeFiles)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "icacls.exe",
                            Arguments = $"\"{exeFile}\" /remove:d Everyone",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(psi);
                    }
                }
            }
            catch { }

            // Remove from database (inline to avoid compilation issues)
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var conn = new Npgsql.NpgsqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new Npgsql.NpgsqlCommand("UPDATE blocked_applications SET is_blocked = FALSE WHERE company_name = @c AND system_name = @s AND app_name = @a", conn))
                    {
                        cmd.Parameters.AddWithValue("@c", _companyName ?? "");
                        cmd.Parameters.AddWithValue("@s", _systemName ?? "");
                        cmd.Parameters.AddWithValue("@a", appName ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                Debug.WriteLine($"[SystemControl] Block removed from DB: {appName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemControl] Error removing blocked app from DB: {ex.Message}");
            }
        }

        private string ChangeUserPassword(string username, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
                return "Username and password are required";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = $"user \"{username}\" \"{newPassword}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit(10000);
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        if (process.ExitCode == 0)
                            return $"Password changed for '{username}'";
                        else
                            return $"Failed: {error}".Trim();
                    }
                }
                return "Process failed to start";
            }
            catch (Exception ex)
            {
                return $"Error changing password: {ex.Message}";
            }
        }

        private string ExecuteShellCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return "No command provided";

            try
            {
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
                        process.WaitForExit(15000);
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(error) && string.IsNullOrWhiteSpace(output))
                            return $"Error: {error}".Trim();
                        return string.IsNullOrEmpty(output) ? "Command executed" : output.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }

            return "Command execution failed";
        }

        private void BlockRegistryKey(string registryPath)
        {
            if (string.IsNullOrWhiteSpace(registryPath)) return;

            var blockedKey = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\BlockedRegistry");
            blockedKey?.SetValue(registryPath, DateTime.Now.ToString());
            blockedKey?.Close();
        }

        private void BlockFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (File.Exists(filePath))
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

        // ==================== DANGER ZONE IMPLEMENTATIONS ====================
        // All danger zone commands now delegate to the enhanced SystemRestrictions static class
        // which uses multi-layer blocking for maximum effectiveness.

        private void BlockCMD(bool block)
        {
            if (block) SystemRestrictions.BlockCMD();
            else SystemRestrictions.UnblockCMD();
        }

        private void BlockPowerShell(bool block)
        {
            if (block) SystemRestrictions.BlockPowerShell();
            else SystemRestrictions.UnblockPowerShell();
        }

        private void BlockRegedit(bool block)
        {
            if (block) SystemRestrictions.BlockRegedit();
            else SystemRestrictions.UnblockRegedit();
        }

        private void BlockCopyPaste(bool block)
        {
            if (block) SystemRestrictions.BlockCopyPaste();
            else SystemRestrictions.UnblockCopyPaste();
        }

        private void BlockSoftwareInstall(bool block)
        {
            if (block) SystemRestrictions.BlockSoftwareInstall();
            else SystemRestrictions.UnblockSoftwareInstall();
        }

        private void BlockDeleteFunction(bool block)
        {
            if (block) SystemRestrictions.BlockDelete();
            else SystemRestrictions.UnblockDelete();
        }

        private void ApplyFullSystemRestriction(bool apply)
        {
            if (apply) SystemRestrictions.ApplyFullRestriction();
            else SystemRestrictions.RemoveFullRestriction();
        }

        private void LockIPAddress(bool lockIP)
        {
            if (lockIP) SystemRestrictions.BlockIPChange();
            else SystemRestrictions.UnblockIPChange();
        }

        private void BlockTaskManager(bool block)
        {
            if (block) SystemRestrictions.BlockTaskManager();
            else SystemRestrictions.UnblockTaskManager();
        }

        private void BlockControlPanel(bool block)
        {
            if (block) SystemRestrictions.BlockControlPanel();
            else SystemRestrictions.UnblockControlPanel();
        }
    }

    // ==================== Data Models (kept for backward compatibility) ====================

    public class ControlCommand
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public Dictionary<string, string> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool Executed { get; set; }
    }

    public class CommandExecutionResult
    {
        public string CommandId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}
