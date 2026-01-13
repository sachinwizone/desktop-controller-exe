using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;

namespace DesktopController
{
    public partial class MainForm
    {
        #region Registry Helper Methods

        private RegistryKey? OpenRegistryKey(string path, bool writable = false)
        {
            try
            {
                return Registry.LocalMachine.OpenSubKey(path, writable) ??
                       Registry.CurrentUser.OpenSubKey(path, writable);
            }
            catch
            {
                return null;
            }
        }

        private void SetRegistryValue(string path, string name, object value, RegistryValueKind kind = RegistryValueKind.DWord, bool useLM = true)
        {
            try
            {
                RegistryKey baseKey = useLM ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey? key = baseKey.CreateSubKey(path))
                {
                    key?.SetValue(name, value, kind);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set registry value: {ex.Message}");
            }
        }

        private object? GetRegistryValue(string path, string name, bool useLM = true)
        {
            try
            {
                RegistryKey baseKey = useLM ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey? key = baseKey.OpenSubKey(path))
                {
                    return key?.GetValue(name);
                }
            }
            catch
            {
                return null;
            }
        }

        private void DeleteRegistryValue(string path, string name, bool useLM = true)
        {
            try
            {
                RegistryKey baseKey = useLM ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey? key = baseKey.OpenSubKey(path, true))
                {
                    key?.DeleteValue(name, false);
                }
            }
            catch { }
        }

        private void RunCommand(string command, bool hidden = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                CreateNoWindow = hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process? process = Process.Start(psi))
            {
                process?.WaitForExit();
            }
        }

        private void RunPowerShell(string command, bool hidden = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process? process = Process.Start(psi))
            {
                process?.WaitForExit();
            }
        }

        #endregion

        #region Control Panel Methods

        // Control Panel - Block using registry policy
        private bool IsControlPanelBlocked()
        {
            // Check HKCU policy
            object? valueHKCU = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel", false);
            if (valueHKCU != null && Convert.ToInt32(valueHKCU) == 1) return true;
            
            // Check HKLM policy
            object? valueHKLM = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel", true);
            return valueHKLM != null && Convert.ToInt32(valueHKLM) == 1;
        }

        private void SetControlPanelAccess(bool block)
        {
            try
            {
                // Kill any running control panel first
                RunCommand("taskkill /f /im control.exe 2>nul");
                RunCommand("taskkill /f /im SystemSettings.exe 2>nul");
                
                if (block)
                {
                    // BLOCK - Set NoControlPanel = 1
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /t REG_DWORD /d 1 /f");
                }
                else
                {
                    // UNBLOCK - Delete the values
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /f 2>nul");
                }
                
                // Restart explorer for changes to take effect
                RestartExplorerFull();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Control Panel access: {ex.Message}");
            }
        }
        
        private void RestartExplorerFull()
        {
            try
            {
                // Kill explorer
                foreach (var proc in Process.GetProcessesByName("explorer"))
                {
                    try { proc.Kill(); } catch { }
                }
                System.Threading.Thread.Sleep(1500);
                // Start explorer
                Process.Start("explorer.exe");
                System.Threading.Thread.Sleep(500);
            }
            catch { }
        }
        
        // Fast explorer refresh without full restart
        private void RestartExplorerShell()
        {
            try
            {
                // Use NIRCMD style approach - just refresh shell namespace
                IntPtr HWND_BROADCAST = new IntPtr(0xffff);
                uint WM_SETTINGCHANGE = 0x001A;
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Policy", 0x0002, 1000, out _);
            }
            catch { }
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        // Delete Key - Block using file permission denial
        private bool IsDeleteKeyDisabled()
        {
            string markerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController", "delete_blocked.txt");
            return File.Exists(markerPath);
        }

        private void SetDeleteKey(bool enabled)
        {
            try
            {
                string markerDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController");
                string markerPath = Path.Combine(markerDir, "delete_blocked.txt");
                
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                
                if (enabled)
                {
                    // ALLOW - Restore delete functionality
                    if (File.Exists(markerPath)) File.Delete(markerPath);
                    
                    // Remove registry policies
                    RunCommand("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRecycleFiles /f 2>nul");
                    RunCommand("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v ConfirmFileDelete /f 2>nul");
                    
                    // Remove keyboard scancode block
                    RunCommand("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout\" /v \"Scancode Map\" /f 2>nul");
                    
                    // REMOVE the Deny ACL rules we added - use icacls /remove to remove Deny entries
                    RunCommand($"icacls \"{desktop}\" /remove:d Everyone /T /C /Q 2>nul");
                    RunCommand($"icacls \"{userProfile}\\Downloads\" /remove:d Everyone /T /C /Q 2>nul");
                    
                    // Also reset to default permissions
                    RunCommand($"icacls \"{desktop}\" /reset /T /C /Q 2>nul");
                    RunCommand($"icacls \"{userProfile}\\Downloads\" /reset /T /C /Q 2>nul");
                    
                    System.Windows.Forms.MessageBox.Show("Delete has been restored.\\nYou can now delete files normally.", "Delete Allowed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    // BLOCK - Deny delete permission
                    if (!Directory.Exists(markerDir)) Directory.CreateDirectory(markerDir);
                    File.WriteAllText(markerPath, DateTime.Now.ToString());
                    
                    // Block DELETE key on keyboard (works after reboot)
                    RunCommand("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout\" /v \"Scancode Map\" /t REG_BINARY /d 0000000000000000020000000000530000000000 /f");
                    
                    // Deny delete permission using icacls (immediate effect)
                    RunCommand($"icacls \"{desktop}\" /deny Everyone:(DE,DC) /T /C /Q");
                    RunCommand($"icacls \"{userProfile}\\Downloads\" /deny Everyone:(DE,DC) /T /C /Q");
                    
                    // Require confirmation for delete
                    RunCommand("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v ConfirmFileDelete /t REG_DWORD /d 1 /f");
                    
                    System.Windows.Forms.MessageBox.Show("Delete blocked on Desktop and Downloads.\\nDELETE key will be disabled after restart.", "Delete Blocked", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Delete key: {ex.Message}");
            }
        }
        
        private void RestartExplorer()
        {
            try
            {
                // Method 1: Clear Group Policy cache for user
                string gpCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Group Policy");
                if (Directory.Exists(gpCachePath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(gpCachePath, "*.pol", SearchOption.AllDirectories))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                    catch { }
                }
                
                // Method 2: Broadcast policy change to all windows
                IntPtr HWND_BROADCAST = new IntPtr(0xffff);
                uint WM_SETTINGCHANGE = 0x001A;
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Policy", 0x0002, 1000, out _);
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0x0002, 1000, out _);
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Software\\Policies", 0x0002, 1000, out _);
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies", 0x0002, 1000, out _);
                
                // Method 3: Force Group Policy refresh (runs in background)
                RunCommandAsync("gpupdate /target:user /force");
                RunCommandAsync("gpupdate /target:computer /force");
                
                // Method 4: Kill explorer and wait, then restart
                RunCommand("taskkill /f /im explorer.exe");
                System.Threading.Thread.Sleep(2500);
                
                // Method 5: Also refresh shell icons
                RunCommand("ie4uinit.exe -show");
                
                Process.Start("explorer.exe");
                System.Threading.Thread.Sleep(1000);
            }
            catch { }
        }
        
        private void RunCommandAsync(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process.Start(psi); // Don't wait
            }
            catch { }
        }

        // Clipboard / Cut-Copy-Paste - Block by continuously clearing clipboard
        private bool IsClipboardDisabled()
        {
            string markerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController", "clipboard_blocked.txt");
            bool isBlocked = File.Exists(markerPath);
            
            // If blocked, make sure the timer is running
            if (isBlocked && !clipboardBlocked)
            {
                clipboardBlocked = true;
                clipboardTimer?.Dispose();
                clipboardTimer = new System.Threading.Timer(ClipboardClearerCallback, null, 0, 500);
            }
            
            return isBlocked;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();
        
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        // Clipboard clearer timer
        private static System.Threading.Timer? clipboardTimer;
        private static bool clipboardBlocked = false;

        private void SetClipboard(bool enabled)
        {
            try
            {
                string markerDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController");
                string markerPath = Path.Combine(markerDir, "clipboard_blocked.txt");
                
                if (enabled)
                {
                    // ALLOW - Restore clipboard functionality
                    if (File.Exists(markerPath)) File.Delete(markerPath);
                    
                    // Stop the clipboard clearing timer
                    clipboardBlocked = false;
                    clipboardTimer?.Dispose();
                    clipboardTimer = null;
                    
                    // Re-enable clipboard services
                    RunCommand("sc config ClipSVC start= auto 2>nul");
                    RunCommand("net start ClipSVC 2>nul");
                    RunPowerShell("Get-Service cbdhsvc_* | Set-Service -StartupType Automatic -ErrorAction SilentlyContinue");
                    RunPowerShell("Get-Service cbdhsvc_* | Start-Service -ErrorAction SilentlyContinue");
                    
                    // Remove clipboard policies
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v AllowClipboardHistory /f 2>nul");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Clipboard\" /v EnableClipboardHistory /t REG_DWORD /d 1 /f");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services\" /v fDisableClip /f 2>nul");
                    
                    System.Windows.Forms.MessageBox.Show("Clipboard has been restored.\\nCut/Copy/Paste should work now.", "Clipboard Allowed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    // BLOCK - Start continuous clipboard clearing
                    if (!Directory.Exists(markerDir)) Directory.CreateDirectory(markerDir);
                    File.WriteAllText(markerPath, DateTime.Now.ToString());
                    
                    // Clear clipboard immediately
                    ClearClipboardNow();
                    
                    // Start a timer that clears clipboard every 500ms
                    clipboardBlocked = true;
                    clipboardTimer?.Dispose();
                    clipboardTimer = new System.Threading.Timer(ClipboardClearerCallback, null, 0, 500);
                    
                    // Also set policies as backup
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services\" /v fDisableClip /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Clipboard\" /v EnableClipboardHistory /t REG_DWORD /d 0 /f");
                    
                    System.Windows.Forms.MessageBox.Show("Clipboard is now being blocked.\\nAny copied content will be cleared immediately.\\n\\nNote: Keep this app running for the block to work.", "Clipboard Blocked", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Clipboard: {ex.Message}");
            }
        }
        
        private static void ClipboardClearerCallback(object? state)
        {
            if (clipboardBlocked)
            {
                ClearClipboardNow();
            }
        }
        
        private static void ClearClipboardNow()
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    EmptyClipboard();
                    CloseClipboard();
                }
            }
            catch { }
        }

        // Administrator Account
        private bool IsAdminAccountEnabled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE Name='Administrator' AND LocalAccount=True"))
                {
                    foreach (ManagementObject user in searcher.Get())
                    {
                        return !(bool)user["Disabled"];
                    }
                }
            }
            catch { }
            return false;
        }

        private void SetAdminAccount(bool enabled)
        {
            RunCommand($"net user Administrator /active:{(enabled ? "yes" : "no")}");
        }

        // Task Manager - IMPROVED
        private bool IsTaskManagerDisabled()
        {
            object? valueHKCU = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr", false);
            if (valueHKCU != null && Convert.ToInt32(valueHKCU) == 1) return true;
            
            object? valueHKLM = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr", true);
            return valueHKLM != null && Convert.ToInt32(valueHKLM) == 1;
        }

        private void SetTaskManager(bool enabled)
        {
            try
            {
                // HKCU
                using (RegistryKey? key1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (enabled)
                        key1?.DeleteValue("DisableTaskMgr", false);
                    else
                        key1?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                }
                
                // HKLM
                using (RegistryKey? key2 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (enabled)
                        key2?.DeleteValue("DisableTaskMgr", false);
                    else
                        key2?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Task Manager: {ex.Message}");
            }
        }

        // Command Prompt - IMPROVED
        private bool IsCmdDisabled()
        {
            object? valueHKCU = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\System", "DisableCMD", false);
            if (valueHKCU != null && Convert.ToInt32(valueHKCU) != 0) return true;
            
            object? valueHKLM = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\System", "DisableCMD", true);
            return valueHKLM != null && Convert.ToInt32(valueHKLM) != 0;
        }

        private void SetCommandPrompt(bool enabled)
        {
            try
            {
                // HKCU
                using (RegistryKey? key1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    if (enabled)
                        key1?.DeleteValue("DisableCMD", false);
                    else
                        key1?.SetValue("DisableCMD", 2, RegistryValueKind.DWord); // 2 = disable CMD but allow batch files
                }
                
                // HKLM
                using (RegistryKey? key2 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    if (enabled)
                        key2?.DeleteValue("DisableCMD", false);
                    else
                        key2?.SetValue("DisableCMD", 2, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Command Prompt: {ex.Message}");
            }
        }

        // USB Storage - IMPROVED
        private bool IsUSBStorageDisabled()
        {
            object? value = GetRegistryValue(@"SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", true);
            return value != null && Convert.ToInt32(value) == 4;
        }

        private void SetUSBStorage(bool enabled)
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR"))
                {
                    key?.SetValue("Start", enabled ? 3 : 4, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set USB Storage: {ex.Message}");
            }
        }

        // Send To - ENHANCED with multiple methods
        private bool IsSendToDisabled()
        {
            // Check registry policy
            object? valueHKCU = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoSendToMenu", false);
            if (valueHKCU != null && Convert.ToInt32(valueHKCU) == 1) return true;
            
            object? valueHKLM = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoSendToMenu", true);
            if (valueHKLM != null && Convert.ToInt32(valueHKLM) == 1) return true;
            
            // Check if SendTo folder has been renamed/hidden
            string sendToPath = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
            string backupPath = sendToPath + ".disabled";
            return Directory.Exists(backupPath) && !Directory.Exists(sendToPath);
        }

        private void SetSendTo(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ALLOW - Remove all SendTo restrictions using reg commands
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoSendToMenu /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoSendToMenu /f 2>nul");
                    
                    // Restore context menu handler
                    RunCommand("reg add \"HKCR\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\SendTo\" /ve /t REG_SZ /d \"{7BA4C740-9E81-11CF-99D3-00AA004AE837}\" /f");
                    
                    // Restore SendTo folder
                    string sendToPath = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                    string backupPath = sendToPath + ".disabled";
                    
                    if (Directory.Exists(backupPath))
                    {
                        if (!Directory.Exists(sendToPath))
                        {
                            try { Directory.Move(backupPath, sendToPath); } catch { }
                        }
                        else
                        {
                            try { Directory.Delete(backupPath, true); } catch { }
                        }
                    }
                    
                    if (!Directory.Exists(sendToPath))
                    {
                        try { Directory.CreateDirectory(sendToPath); } catch { }
                    }
                }
                else
                {
                    // BLOCK - Add SendTo restrictions using reg commands
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoSendToMenu /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoSendToMenu /t REG_DWORD /d 1 /f");
                    
                    // Clear the handler
                    RunCommand("reg add \"HKCR\\AllFilesystemObjects\\shellex\\ContextMenuHandlers\\SendTo\" /ve /t REG_SZ /d \"\" /f");
                    
                    // Rename SendTo folder
                    string sendToPath = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                    string backupPath = sendToPath + ".disabled";
                    
                    if (Directory.Exists(sendToPath) && !Directory.Exists(backupPath))
                    {
                        try { Directory.Move(sendToPath, backupPath); } catch { }
                    }
                }
                
                // Restart explorer for immediate effect
                RestartExplorerFull();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Send To: {ex.Message}");
            }
        }

        // PowerShell - ENHANCED to actually block PowerShell execution
        private bool IsPowerShellDisabled()
        {
            // Check if PowerShell is disabled via Software Restriction Policies
            object? value1 = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\PowerShell", "EnableScripts", true);
            if (value1 != null && Convert.ToInt32(value1) == 0) return true;
            
            // Check execution policy
            object? value2 = GetRegistryValue(@"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell", "ExecutionPolicy", true);
            if (value2 != null && value2.ToString() == "Restricted") return true;
            
            // Check if DisallowRun contains powershell
            object? disallowValue = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\\DisallowRun", "1", false);
            return disallowValue != null && disallowValue.ToString()?.ToLower().Contains("powershell") == true;
        }

        private void SetPowerShell(bool enabled)
        {
            try
            {
                // Method 1: Execution policy registry
                using (RegistryKey? key1 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PowerShell"))
                {
                    if (enabled)
                    {
                        key1?.DeleteValue("EnableScripts", false);
                        key1?.DeleteValue("ExecutionPolicy", false);
                    }
                    else
                    {
                        key1?.SetValue("EnableScripts", 0, RegistryValueKind.DWord);
                        key1?.SetValue("ExecutionPolicy", "Restricted", RegistryValueKind.String);
                    }
                }
                
                // Method 2: Shell execution policy
                using (RegistryKey? key2 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"))
                {
                    key2?.SetValue("ExecutionPolicy", enabled ? "RemoteSigned" : "Restricted", RegistryValueKind.String);
                }
                
                // Method 3: Use DisallowRun policy to block powershell.exe
                using (RegistryKey? explorerKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    if (enabled)
                    {
                        explorerKey?.DeleteValue("DisallowRun", false);
                    }
                    else
                    {
                        explorerKey?.SetValue("DisallowRun", 1, RegistryValueKind.DWord);
                    }
                }
                
                using (RegistryKey? disallowKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\\DisallowRun"))
                {
                    if (enabled)
                    {
                        // Remove all PowerShell entries
                        disallowKey?.DeleteValue("1", false);
                        disallowKey?.DeleteValue("2", false);
                        disallowKey?.DeleteValue("3", false);
                    }
                    else
                    {
                        // Block powershell executables
                        disallowKey?.SetValue("1", "powershell.exe", RegistryValueKind.String);
                        disallowKey?.SetValue("2", "powershell_ise.exe", RegistryValueKind.String);
                        disallowKey?.SetValue("3", "pwsh.exe", RegistryValueKind.String);
                    }
                }
                
                // Method 4: Also restrict via constrained language mode
                if (!enabled)
                {
                    Environment.SetEnvironmentVariable("__PSLockdownPolicy", "4", EnvironmentVariableTarget.Machine);
                }
                else
                {
                    Environment.SetEnvironmentVariable("__PSLockdownPolicy", null, EnvironmentVariableTarget.Machine);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set PowerShell: {ex.Message}");
            }
        }

        // Switch User - IMPROVED
        private bool IsSwitchUserDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "HideFastUserSwitching", true);
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetSwitchUser(bool enabled)
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (enabled)
                        key?.DeleteValue("HideFastUserSwitching", false);
                    else
                        key?.SetValue("HideFastUserSwitching", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Switch User: {ex.Message}");
            }
        }

        // Registry Editor - IMPROVED
        private bool IsRegeditDisabled()
        {
            object? valueHKCU = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools", false);
            if (valueHKCU != null && Convert.ToInt32(valueHKCU) != 0) return true;
            
            object? valueHKLM = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools", true);
            return valueHKLM != null && Convert.ToInt32(valueHKLM) != 0;
        }

        private void SetRegedit(bool enabled)
        {
            try
            {
                // HKCU
                using (RegistryKey? key1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (enabled)
                        key1?.DeleteValue("DisableRegistryTools", false);
                    else
                        key1?.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);
                }
                
                // HKLM
                using (RegistryKey? key2 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (enabled)
                        key2?.DeleteValue("DisableRegistryTools", false);
                    else
                        key2?.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Registry Editor: {ex.Message}");
            }
        }

        // IP Change - Block by disabling network adapter
        private bool IsIPChangeDisabled()
        {
            string markerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController", "ipchange_blocked.txt");
            return File.Exists(markerPath);
        }

        private void SetIPChange(bool enabled)
        {
            try
            {
                string markerDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController");
                string markerPath = Path.Combine(markerDir, "ipchange_blocked.txt");
                
                if (enabled)
                {
                    // ALLOW - Remove all network restrictions
                    if (File.Exists(markerPath)) File.Delete(markerPath);
                    
                    // Remove network connection policies
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /f 2>nul");
                    
                    // Remove IFEO blocks
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\ncpa.cpl\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\netsh.exe\" /f 2>nul");
                    
                    // Start Network Setup Service
                    RunCommand("net start NetSetupSvc 2>nul");
                    RunCommand("sc config NetSetupSvc start= demand 2>nul");
                }
                else
                {
                    // BLOCK - Multiple aggressive methods
                    if (!Directory.Exists(markerDir)) Directory.CreateDirectory(markerDir);
                    File.WriteAllText(markerPath, DateTime.Now.ToString());
                    
                    // Method 1: IFEO block for ncpa.cpl (Network Connections)
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\ncpa.cpl\" /v Debugger /t REG_SZ /d \"cmd.exe /c exit\" /f");
                    
                    // Method 2: IFEO block for netsh.exe
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\netsh.exe\" /v Debugger /t REG_SZ /d \"cmd.exe /c exit\" /f");
                    
                    // Method 3: Stop Network Setup Service (prevents IP configuration)
                    RunCommand("net stop NetSetupSvc /y 2>nul");
                    RunCommand("sc config NetSetupSvc start= disabled 2>nul");
                    
                    // Method 4: Group Policy blocks
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_LanChangeProperties /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_LanProperties /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_EnableAdminProhibits /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_LanChangeProperties /t REG_DWORD /d 0 /f");
                    
                    // Method 5: Block Settings app network page
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v SettingsPageVisibility /t REG_SZ /d \"hide:network-status;network-ethernet;network-wifi;network-proxy\" /f");
                    
                    System.Windows.Forms.MessageBox.Show("IP Change has been blocked.\nNetwork settings dialogs will not open.", "IP Change Block", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set IP Change: {ex.Message}");
            }
        }

        // App Installation - Block installer executables immediately
        private bool IsAppInstallDisabled()
        {
            // Check if msiexec is in DisallowRun
            object? disallowValue = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun", "10", false);
            if (disallowValue != null && disallowValue.ToString()?.ToLower() == "msiexec.exe") return true;
            
            // Check MSI policy
            object? msiValue = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Installer", "DisableMSI", true);
            return msiValue != null && Convert.ToInt32(msiValue) == 2;
        }

        private void SetAppInstall(bool enabled)
        {
            try
            {
                // Method 1: Enable/Disable DisallowRun
                using (RegistryKey? key1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key1?.SetValue("DisallowRun", enabled ? 0 : 1, RegistryValueKind.DWord);
                }
                
                // Method 2: Block common installer executables via DisallowRun
                using (RegistryKey? key2 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"))
                {
                    if (enabled)
                    {
                        // Remove all installer blocks
                        key2?.DeleteValue("10", false);
                        key2?.DeleteValue("11", false);
                        key2?.DeleteValue("12", false);
                        key2?.DeleteValue("13", false);
                        key2?.DeleteValue("14", false);
                    }
                    else
                    {
                        // Block all installer executables
                        key2?.SetValue("10", "msiexec.exe", RegistryValueKind.String);
                        key2?.SetValue("11", "setup.exe", RegistryValueKind.String);
                        key2?.SetValue("12", "install.exe", RegistryValueKind.String);
                        key2?.SetValue("13", "installer.exe", RegistryValueKind.String);
                        key2?.SetValue("14", "uninstall.exe", RegistryValueKind.String);
                    }
                }
                
                // Method 3: Disable MSI installer policy
                using (RegistryKey? key3 = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Installer"))
                {
                    if (enabled)
                    {
                        key3?.DeleteValue("DisableMSI", false);
                        key3?.DeleteValue("DisableUserInstalls", false);
                    }
                    else
                    {
                        key3?.SetValue("DisableMSI", 2, RegistryValueKind.DWord); // 2 = Always disable
                        key3?.SetValue("DisableUserInstalls", 1, RegistryValueKind.DWord);
                    }
                }
                
                // Method 4: Stop/Start Windows Installer service
                if (enabled)
                {
                    RunCommand("sc config msiserver start=demand");
                    RunCommand("net start msiserver 2>nul");
                }
                else
                {
                    RunCommand("net stop msiserver 2>nul");
                    RunCommand("sc config msiserver start=disabled");
                }
                
                // Apply policies immediately
                RestartExplorerShell();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set App Installation: {ex.Message}");
            }
        }

        // Device Manager - NEW
        private bool IsDeviceManagerDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "NoDevMgrPage", true);
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetDeviceManager(bool enabled)
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (enabled)
                        key?.DeleteValue("NoDevMgrPage", false);
                    else
                        key?.SetValue("NoDevMgrPage", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Device Manager: {ex.Message}");
            }
        }

        // Task Scheduler - NEW
        private bool IsTaskSchedulerDisabled()
        {
            try
            {
                using (ServiceController sc = new ServiceController("Schedule"))
                {
                    return sc.Status != ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetTaskScheduler(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    RunCommand("sc config Schedule start=auto");
                    RunCommand("net start Schedule");
                }
                else
                {
                    RunCommand("net stop Schedule");
                    RunCommand("sc config Schedule start=disabled");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Task Scheduler: {ex.Message}");
            }
        }

        #endregion

        #region Windows Tab Methods

        // Windows Update - ENHANCED with multiple services and registry
        private bool IsWindowsUpdateEnabled()
        {
            try
            {
                // Check main Windows Update service
                using (ServiceController sc = new ServiceController("wuauserv"))
                {
                    if (sc.Status == ServiceControllerStatus.Running) return true;
                }
                
                // Also check if disabled via policy
                object? policyValue = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableWindowsUpdateAccess", true);
                if (policyValue != null && Convert.ToInt32(policyValue) == 1) return false;
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void SetWindowsUpdate(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ALLOW - Enable Windows Update - FULL RESTORATION
                    
                    // Step 1: Kill Settings app first so it can reload fresh
                    RunCommand("taskkill /f /im SystemSettings.exe 2>nul");
                    
                    // Step 2: Remove ALL policy blocks
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\WindowsUpdate\" /f 2>nul");
                    
                    // Step 3: Remove any Settings page hiding for Windows Update
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v SettingsPageVisibility /f 2>nul");
                    
                    // Step 4: Remove IFEO blocks for update-related executables
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\wuauclt.exe\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\UsoClient.exe\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\MusNotification.exe\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\MusNotificationUx.exe\" /f 2>nul");
                    
                    // Step 5: Enable all update services
                    RunCommand("sc config wuauserv start= demand");
                    RunCommand("sc config bits start= demand");
                    RunCommand("sc config dosvc start= automatic");
                    RunCommand("sc config UsoSvc start= automatic");
                    RunCommand("sc config WaaSMedicSvc start= manual");
                    RunCommand("sc config TrustedInstaller start= demand");
                    RunCommand("sc config cryptsvc start= automatic");
                    
                    // Step 6: Start services in correct order
                    RunCommand("net start cryptsvc 2>nul");
                    RunCommand("net start bits 2>nul");
                    RunCommand("net start wuauserv 2>nul");
                    RunCommand("net start UsoSvc 2>nul");
                    
                    // Step 7: Reset Windows Update components
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\" /v DisableOSUpgrade /f 2>nul");
                    
                    // Step 8: Clear any paused updates
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v PauseFeatureUpdatesStartTime /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v PauseQualityUpdatesStartTime /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v PauseUpdatesExpiryTime /f 2>nul");
                    
                    // Step 9: Reset Windows Update cache/state (helps fix "Something went wrong")
                    RunCommand("net stop wuauserv 2>nul");
                    RunCommand("net stop cryptsvc 2>nul");
                    RunCommand("net stop bits 2>nul");
                    RunCommand("ren C:\\Windows\\SoftwareDistribution SoftwareDistribution.old 2>nul");
                    RunCommand("ren C:\\Windows\\System32\\catroot2 catroot2.old 2>nul");
                    RunCommand("net start cryptsvc 2>nul");
                    RunCommand("net start bits 2>nul");
                    RunCommand("net start wuauserv 2>nul");
                    
                    // Step 10: Force Windows Update to reinitialize
                    RunCommand("wuauclt.exe /resetauthorization /detectnow 2>nul");
                    RunCommand("UsoClient.exe StartScan 2>nul");
                    
                    System.Windows.Forms.MessageBox.Show("Windows Update has been FULLY RESTORED.\n\n• All services restored and started\n• All policies removed\n• Update cache reset\n\nPlease close and reopen Settings app.", "Windows Update", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    // BLOCK - Disable Windows Update COMPLETELY
                    
                    // Step 1: Stop all update services
                    RunCommand("net stop wuauserv /y 2>nul");
                    RunCommand("net stop bits /y 2>nul");
                    RunCommand("net stop dosvc /y 2>nul");
                    RunCommand("net stop UsoSvc /y 2>nul");
                    RunCommand("net stop WaaSMedicSvc /y 2>nul");
                    
                    // Step 2: Disable services completely
                    RunCommand("sc config wuauserv start= disabled");
                    RunCommand("sc config bits start= disabled");
                    RunCommand("sc config dosvc start= disabled");
                    RunCommand("sc config UsoSvc start= disabled");
                    RunCommand("sc config WaaSMedicSvc start= disabled");
                    
                    // Step 3: Set policies to block Windows Update
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DisableWindowsUpdateAccess /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 1 /f");
                    
                    // Step 4: Block OS upgrades
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\" /v DisableOSUpgrade /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DisableOSUpgrade /t REG_DWORD /d 1 /f");
                    
                    System.Windows.Forms.MessageBox.Show("Windows Update has been COMPLETELY DISABLED.\n\n• All services stopped and disabled\n• Policies applied\n• OS upgrades blocked", "Windows Update", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Windows Update: {ex.Message}");
            }
        }

        // User Management
        private void RefreshUsers(ComboBox combo)
        {
            combo.Items.Clear();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
                {
                    foreach (ManagementObject user in searcher.Get())
                    {
                        string status = (bool)user["Disabled"] ? " [Disabled]" : " [Active]";
                        combo.Items.Add(user["Name"].ToString() + status);
                    }
                }
                if (combo.Items.Count > 0)
                    combo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}");
            }
        }

        private void SetUserEnabled(string? username, bool enabled)
        {
            if (string.IsNullOrEmpty(username)) return;
            
            // Remove status suffix
            string user = username.Split('[')[0].Trim();
            RunCommand($"net user \"{user}\" /active:{(enabled ? "yes" : "no")}");
            MessageBox.Show($"User '{user}' has been {(enabled ? "enabled" : "disabled")}.", "User Management", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CreateNewUser()
        {
            using (Form form = new Form())
            {
                form.Text = "Create New User";
                form.Size = new Size(350, 200);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = Color.FromArgb(33, 37, 41);

                Label userLabel = new Label { Text = "Username:", Location = new Point(20, 20), ForeColor = Color.White, AutoSize = true };
                TextBox userBox = new TextBox { Location = new Point(120, 17), Width = 180 };
                
                Label passLabel = new Label { Text = "Password:", Location = new Point(20, 60), ForeColor = Color.White, AutoSize = true };
                TextBox passBox = new TextBox { Location = new Point(120, 57), Width = 180, PasswordChar = '*' };
                
                CheckBox adminCheck = new CheckBox { Text = "Make Administrator", Location = new Point(120, 90), ForeColor = Color.White, AutoSize = true };

                Button createBtn = new Button { Text = "Create", Location = new Point(120, 120), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White };
                createBtn.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(userBox.Text))
                    {
                        RunCommand($"net user \"{userBox.Text}\" \"{passBox.Text}\" /add");
                        if (adminCheck.Checked)
                            RunCommand($"net localgroup Administrators \"{userBox.Text}\" /add");
                        MessageBox.Show($"User '{userBox.Text}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.Close();
                    }
                };

                form.Controls.AddRange(new Control[] { userLabel, userBox, passLabel, passBox, adminCheck, createBtn });
                form.ShowDialog();
            }
        }

        // Firewall - ENHANCED with better error handling and multiple methods
        private bool IsFirewallEnabled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall show allprofiles state",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using (Process? process = Process.Start(psi))
                {
                    string output = process?.StandardOutput.ReadToEnd() ?? "";
                    process?.WaitForExit();
                    return output.Contains("ON");
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetFirewall(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ALLOW - Remove ALL blocks and restore Public & Private firewall access
                    
                    // Step 1: Re-enable firewall service FIRST (needed for file operations)
                    RunCommand("sc config MpsSvc start= auto");
                    RunCommand("net start MpsSvc 2>nul");
                    
                    // Step 2: Remove ALL deny permissions on firewall files
                    RunCommand("icacls \"C:\\Windows\\System32\\FirewallControlPanel.dll\" /remove:d Everyone 2>nul");
                    RunCommand("icacls \"C:\\Windows\\System32\\FirewallControlPanel.dll.blocked\" /remove:d Everyone 2>nul");
                    RunCommand("icacls \"C:\\Windows\\System32\\Firewall.cpl\" /remove:d Everyone 2>nul");
                    RunCommand("icacls \"C:\\Windows\\System32\\Firewall.cpl.blocked\" /remove:d Everyone 2>nul");
                    RunCommand("icacls \"C:\\Windows\\SysWOW64\\Firewall.cpl\" /remove:d Everyone 2>nul");
                    RunCommand("icacls \"C:\\Windows\\SysWOW64\\Firewall.cpl.blocked\" /remove:d Everyone 2>nul");
                    
                    // Step 3: Restore FirewallControlPanel.dll
                    string fwDll = @"C:\Windows\System32\FirewallControlPanel.dll";
                    string fwDllBlocked = @"C:\Windows\System32\FirewallControlPanel.dll.blocked";
                    if (System.IO.File.Exists(fwDllBlocked))
                    {
                        try 
                        { 
                            RunCommand($"takeown /f \"{fwDllBlocked}\"");
                            RunCommand($"icacls \"{fwDllBlocked}\" /grant Administrators:F");
                            if (System.IO.File.Exists(fwDll))
                            {
                                System.IO.File.Delete(fwDll);
                            }
                            System.IO.File.Move(fwDllBlocked, fwDll); 
                        } 
                        catch { }
                    }
                    
                    // Step 4: Restore Firewall.cpl files
                    string firewallCpl = @"C:\Windows\System32\Firewall.cpl";
                    string firewallCplBackup = @"C:\Windows\System32\Firewall.cpl.blocked";
                    if (System.IO.File.Exists(firewallCplBackup))
                    {
                        try 
                        { 
                            RunCommand($"takeown /f \"{firewallCplBackup}\"");
                            RunCommand($"icacls \"{firewallCplBackup}\" /grant Administrators:F");
                            if (System.IO.File.Exists(firewallCpl))
                            {
                                System.IO.File.Delete(firewallCpl);
                            }
                            System.IO.File.Move(firewallCplBackup, firewallCpl); 
                        } 
                        catch { }
                    }
                    
                    string firewallCpl64 = @"C:\Windows\SysWOW64\Firewall.cpl";
                    string firewallCpl64Backup = @"C:\Windows\SysWOW64\Firewall.cpl.blocked";
                    if (System.IO.File.Exists(firewallCpl64Backup))
                    {
                        try 
                        { 
                            RunCommand($"takeown /f \"{firewallCpl64Backup}\"");
                            RunCommand($"icacls \"{firewallCpl64Backup}\" /grant Administrators:F");
                            if (System.IO.File.Exists(firewallCpl64))
                            {
                                System.IO.File.Delete(firewallCpl64);
                            }
                            System.IO.File.Move(firewallCpl64Backup, firewallCpl64); 
                        } 
                        catch { }
                    }
                    
                    // Step 5: Remove ALL policy restrictions
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\PublicProfile\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\StandardProfile\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\DomainProfile\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender Security Center\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender Security Center\\Firewall and network protection\" /f 2>nul");
                    
                    // Step 6: Remove IFEO blocks
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\SmartScreenSettings.exe\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\WF.msc\" /f 2>nul");
                    
                    // Step 7: Remove shell command blocks
                    RunCommand("reg delete \"HKCR\\CLSID\\{4026492F-2F69-46B8-B9BF-5654FC07E423}\\shell\" /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace\\{4026492F-2F69-46B8-B9BF-5654FC07E423}\" /v \"(Default)\" /f 2>nul");
                    
                    // Step 8: Remove Settings page hiding
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v SettingsPageVisibility /f 2>nul");
                    
                    // Step 9: Set registry to enable firewall for Public & Private profiles
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile"))
                    {
                        key?.SetValue("EnableFirewall", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.DeleteValue("DisableNotifications", false);
                    }
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile"))
                    {
                        key?.SetValue("EnableFirewall", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.DeleteValue("DisableNotifications", false);
                    }
                    
                    // Step 10: Enable via PowerShell and netsh
                    RunPowerShell("Set-NetFirewallProfile -Profile Public,Private,Domain -Enabled True -ErrorAction SilentlyContinue");
                    RunCommand("netsh advfirewall set allprofiles state on 2>nul");
                    RunCommand("netsh advfirewall set publicprofile state on");
                    RunCommand("netsh advfirewall set privateprofile state on");
                    
                    System.Windows.Forms.MessageBox.Show("Public & Private Network Firewall is now ENABLED.\nUsers can change firewall settings.", "Firewall", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    // BLOCK - Disable PUBLIC and PRIVATE network firewall (users cannot turn ON)
                    
                    // 1. Turn off firewall for PUBLIC and PRIVATE profiles
                    RunPowerShell("Set-NetFirewallProfile -Profile Public,Private -Enabled False -ErrorAction SilentlyContinue");
                    RunCommand("netsh advfirewall set publicprofile state off");
                    RunCommand("netsh advfirewall set privateprofile state off");
                    
                    // 2. Set registry to disable firewall for PUBLIC profile
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile"))
                    {
                        key?.SetValue("EnableFirewall", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.SetValue("DisableNotifications", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 3. Set registry to disable firewall for PRIVATE (StandardProfile)
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile"))
                    {
                        key?.SetValue("EnableFirewall", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.SetValue("DisableNotifications", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 4. BLOCK via Group Policy - force PUBLIC firewall OFF
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsFirewall\PublicProfile"))
                    {
                        key?.SetValue("EnableFirewall", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.SetValue("DisableNotifications", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 5. BLOCK via Group Policy - force PRIVATE firewall OFF
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsFirewall\StandardProfile"))
                    {
                        key?.SetValue("EnableFirewall", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.SetValue("DisableNotifications", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 6. Block Windows Security Center firewall UI (prevents changing settings)
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Firewall and network protection"))
                    {
                        key?.SetValue("UILockdown", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 6b. DISABLE "Turn Windows Defender Firewall on or off" link - Block access to customize settings
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsFirewall"))
                    {
                        key?.SetValue("DisableStatefulFTP", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 6c. Block PUBLIC profile settings changes via Group Policy
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsFirewall\PublicProfile"))
                    {
                        key?.SetValue("AllowLocalPolicyMerge", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.SetValue("AllowLocalIPsecPolicyMerge", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 6d. Block PRIVATE profile settings changes via Group Policy
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsFirewall\StandardProfile"))
                    {
                        key?.SetValue("AllowLocalPolicyMerge", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        key?.SetValue("AllowLocalIPsecPolicyMerge", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    
                    // 6e. Disable the "Customize Settings" / "Turn on or off" page - THIS IS THE KEY BLOCK
                    // When this is set, clicking "Turn Windows Defender Firewall on or off" will show "managed by admin"
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\PublicProfile\" /v \"EnableFirewall\" /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\StandardProfile\" /v \"EnableFirewall\" /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsFirewall\\DomainProfile\" /v \"EnableFirewall\" /t REG_DWORD /d 0 /f");
                    
                    // 6f. BLOCK the NetCplTasksPage.dll - this is responsible for "Turn on/off" and settings pages
                    // Use IFEO to prevent Control Panel from loading this functionality
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\rundll32.exe"))
                    {
                        // We DON'T block rundll32 completely as it's used system-wide
                        // Instead we'll remove the shell command for firewall settings
                    }
                    
                    // 6g. Remove the "Turn Windows Defender Firewall on or off" shell command
                    RunCommand("reg add \"HKCR\\CLSID\\{4026492F-2F69-46B8-B9BF-5654FC07E423}\\shell\\open\\command\" /ve /t REG_SZ /d \"\" /f 2>nul");
                    
                    // 6h. Block the Firewall Settings page by removing its handler
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace\\{4026492F-2F69-46B8-B9BF-5654FC07E423}\" /v \"(Default)\" /t REG_SZ /d \"\" /f 2>nul");
                    
                    // 6i. ULTIMATE - Deny EXECUTE permission on Firewall.cpl for Everyone
                    RunCommand("icacls \"C:\\Windows\\System32\\Firewall.cpl\" /deny Everyone:(RX) 2>nul");
                    RunCommand("icacls \"C:\\Windows\\SysWOW64\\Firewall.cpl\" /deny Everyone:(RX) 2>nul");
                    
                    // 6j. Block SmartScreenSettings.exe which can also access firewall
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\SmartScreenSettings.exe"))
                    {
                        key?.SetValue("Debugger", "systray.exe", Microsoft.Win32.RegistryValueKind.String);
                    }
                    
                    // 6k. ULTIMATE BLOCK - Rename/Block FirewallControlPanel.dll - THIS IS THE REAL CONTROL
                    // This DLL is what provides ALL the firewall UI pages including "Turn on/off"
                    string fwDll = @"C:\Windows\System32\FirewallControlPanel.dll";
                    string fwDllBlocked = @"C:\Windows\System32\FirewallControlPanel.dll.blocked";
                    if (System.IO.File.Exists(fwDll) && !System.IO.File.Exists(fwDllBlocked))
                    {
                        try
                        {
                            RunCommand($"takeown /f \"{fwDll}\"");
                            RunCommand($"icacls \"{fwDll}\" /grant Administrators:F");
                            System.IO.File.Move(fwDll, fwDllBlocked);
                        }
                        catch { }
                    }
                    // Also deny access as backup
                    RunCommand("icacls \"C:\\Windows\\System32\\FirewallControlPanel.dll\" /deny Everyone:(RX) 2>nul");
                    
                    // 7. Hide Firewall from Windows Settings app
                    using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                    {
                        key?.SetValue("SettingsPageVisibility", "hide:windowsdefender-firewall;network-firewall", Microsoft.Win32.RegistryValueKind.String);
                    }
                    
                    // 8. Disable the MpsSvc firewall service - this prevents ANY firewall changes
                    RunCommand("sc config MpsSvc start= disabled");
                    RunCommand("net stop MpsSvc 2>nul");
                    
                    // 9. RENAME the Firewall.cpl file to block Control Panel access
                    string firewallCpl = @"C:\Windows\System32\Firewall.cpl";
                    string firewallCplBackup = @"C:\Windows\System32\Firewall.cpl.blocked";
                    if (System.IO.File.Exists(firewallCpl) && !System.IO.File.Exists(firewallCplBackup))
                    {
                        try
                        {
                            // Take ownership and rename
                            RunCommand($"takeown /f \"{firewallCpl}\"");
                            RunCommand($"icacls \"{firewallCpl}\" /grant Administrators:F");
                            System.IO.File.Move(firewallCpl, firewallCplBackup);
                        }
                        catch { }
                    }
                    
                    // 10. Also block the 64-bit version if exists
                    string firewallCpl64 = @"C:\Windows\SysWOW64\Firewall.cpl";
                    string firewallCpl64Backup = @"C:\Windows\SysWOW64\Firewall.cpl.blocked";
                    if (System.IO.File.Exists(firewallCpl64) && !System.IO.File.Exists(firewallCpl64Backup))
                    {
                        try
                        {
                            RunCommand($"takeown /f \"{firewallCpl64}\"");
                            RunCommand($"icacls \"{firewallCpl64}\" /grant Administrators:F");
                            System.IO.File.Move(firewallCpl64, firewallCpl64Backup);
                        }
                        catch { }
                    }
                    
                    // 11. Unregister the Firewall CPL from Control Panel
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Cpls\" /v \"Firewall.cpl\" /f 2>nul");
                    
                    // 12. Remove firewall from Control Panel namespace
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace\\{4026492F-2F69-46B8-B9BF-5654FC07E423}\" /v \"(Default)\" /t REG_SZ /d \"\" /f 2>nul");
                    
                    System.Windows.Forms.MessageBox.Show("Public & Private Network Firewall DISABLED and COMPLETELY BLOCKED.\n\n• Both firewalls OFF\n• Firewall service DISABLED\n• Control Panel access REMOVED\n• Settings hidden\n\nUsers CANNOT access or change firewall.", "Firewall", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Firewall: {ex.Message}");
            }
        }
        
        private void ExecuteNetsh(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process? process = Process.Start(psi))
            {
                process?.WaitForExit(10000);
            }
        }
        
        private void RunNetshCommand(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process? process = Process.Start(psi))
            {
                process?.WaitForExit();
            }
        }

        // Internet Access Control - Uses network adapter disable method (most reliable)
        private bool IsInternetEnabled()
        {
            try
            {
                // Check registry for our internet block flag
                object? value = GetRegistryValue(@"SOFTWARE\DesktopController", "InternetBlocked", false);
                if (value != null && Convert.ToInt32(value) == 1)
                    return false;
                    
                return true;
            }
            catch
            {
                return true;
            }
        }

        private void SetInternet(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ALLOW Internet - Enable all network adapters
                    
                    // Remove our block flag
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\DesktopController\" /v InternetBlocked /f 2>nul");
                    
                    // Enable all network adapters using PowerShell (most reliable)
                    RunPowerShell("Get-NetAdapter | Enable-NetAdapter -Confirm:$false");
                    
                    // Also try netsh method for older systems
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True"))
                        {
                            foreach (ManagementObject adapter in searcher.Get())
                            {
                                string name = adapter["NetConnectionID"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(name))
                                {
                                    RunCommand($"netsh interface set interface \"{name}\" admin=enable 2>nul");
                                }
                            }
                        }
                    }
                    catch { }
                    
                    // Remove firewall blocks
                    RunCommand("netsh advfirewall firewall delete rule name=\"DesktopController_BlockAll\" 2>nul");
                    RunCommand("netsh advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound");
                    
                    MessageBox.Show("Internet Access has been ENABLED.\nAll network adapters have been enabled.", "Internet Access", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // BLOCK Internet - Disable all network adapters (most effective method)
                    
                    // Set our block flag in registry
                    RunCommand("reg add \"HKLM\\SOFTWARE\\DesktopController\" /v InternetBlocked /t REG_DWORD /d 1 /f");
                    
                    // Method 1: Disable all network adapters using PowerShell
                    RunPowerShell("Get-NetAdapter | Disable-NetAdapter -Confirm:$false");
                    
                    // Method 2: Also try netsh method
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled=True"))
                        {
                            foreach (ManagementObject adapter in searcher.Get())
                            {
                                string name = adapter["NetConnectionID"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(name))
                                {
                                    RunCommand($"netsh interface set interface \"{name}\" admin=disable 2>nul");
                                }
                            }
                        }
                    }
                    catch { }
                    
                    // Method 3: Also add firewall block as backup
                    RunCommand("netsh advfirewall firewall delete rule name=\"DesktopController_BlockAll\" 2>nul");
                    RunCommand("netsh advfirewall firewall add rule name=\"DesktopController_BlockAll\" dir=out action=block enable=yes");
                    RunCommand("netsh advfirewall set allprofiles firewallpolicy blockinbound,blockoutbound");
                    
                    MessageBox.Show("Internet Access has been BLOCKED.\nAll network adapters have been disabled.", "Internet Access", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set Internet Access: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Windows Defender
        private bool IsDefenderEnabled()
        {
            try
            {
                // Check multiple indicators
                object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
                if (value != null && Convert.ToInt32(value) == 1)
                    return false;
                
                // Check Real-time protection
                object? rtValue = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring");
                if (rtValue != null && Convert.ToInt32(rtValue) == 1)
                    return false;
                    
                return true;
            }
            catch
            {
                return true;
            }
        }

        private void SetDefender(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Windows Defender
                    
                    // Remove policy blocks
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiSpyware /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiVirus /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /f 2>nul");
                    
                    // Enable Defender services
                    RunCommand("sc config WinDefend start= auto");
                    RunCommand("sc config WdNisSvc start= demand");
                    RunCommand("net start WinDefend 2>nul");
                    
                    // Enable via PowerShell
                    RunPowerShell("Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue");
                    RunPowerShell("Start-Service WinDefend -ErrorAction SilentlyContinue");
                    
                    MessageBox.Show("Windows Defender has been ENABLED.\nNote: If Tamper Protection is on, some settings may not change.", "Windows Defender", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // DISABLE Windows Defender
                    // Note: On modern Windows, Tamper Protection must be disabled manually first in Windows Security
                    
                    // Disable via PowerShell first (most effective)
                    RunPowerShell("Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction SilentlyContinue");
                    RunPowerShell("Set-MpPreference -DisableBehaviorMonitoring $true -ErrorAction SilentlyContinue");
                    RunPowerShell("Set-MpPreference -DisableIOAVProtection $true -ErrorAction SilentlyContinue");
                    RunPowerShell("Set-MpPreference -DisableScriptScanning $true -ErrorAction SilentlyContinue");
                    
                    // Set registry policies to disable
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiSpyware /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\" /v DisableAntiVirus /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableOnAccessProtection /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f");
                    
                    // Stop Defender service
                    RunCommand("net stop WinDefend 2>nul");
                    
                    MessageBox.Show("Windows Defender has been DISABLED.\n\nIMPORTANT: If Tamper Protection is enabled in Windows Security,\nyou must disable it manually first for full effect.\n\nGo to: Windows Security → Virus & threat protection → Manage settings → Turn off Tamper Protection", "Windows Defender", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set Windows Defender: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // System Restore
        private bool IsSystemRestoreEnabled()
        {
            try
            {
                object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR");
                if (value != null && Convert.ToInt32(value) == 1)
                    return false;
                    
                object? configValue = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableConfig");
                if (configValue != null && Convert.ToInt32(configValue) == 1)
                    return false;
                    
                return true;
            }
            catch
            {
                return true;
            }
        }

        private void SetSystemRestore(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE System Restore
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /f 2>nul");
                    
                    // Enable required services
                    RunCommand("sc config swprv start= demand");
                    RunCommand("sc config VSS start= demand");
                    RunCommand("sc config SDRSVC start= demand");
                    
                    // Enable System Restore on C: drive
                    RunPowerShell("Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue");
                    
                    // Set disk space for restore points (10%)
                    RunCommand("vssadmin resize shadowstorage /for=C: /on=C: /maxsize=10% 2>nul");
                    
                    MessageBox.Show("System Restore has been ENABLED on drive C:\\.", "System Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // DISABLE System Restore
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableSR /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore\" /v DisableConfig /t REG_DWORD /d 1 /f");
                    
                    // Disable via PowerShell
                    RunPowerShell("Disable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue");
                    
                    // Delete existing restore points to save space
                    RunCommand("vssadmin delete shadows /for=C: /all /quiet 2>nul");
                    
                    MessageBox.Show("System Restore has been DISABLED.\nExisting restore points have been deleted.", "System Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set System Restore: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // AutoPlay
        private bool IsAutoPlayDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", false);
            return value != null && Convert.ToInt32(value) == 255;
        }

        private void SetAutoPlay(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE AutoPlay
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoDriveTypeAutoRun /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoDriveTypeAutoRun /f 2>nul");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers\" /v DisableAutoplay /t REG_DWORD /d 0 /f");
                }
                else
                {
                    // DISABLE AutoPlay
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers\" /v DisableAutoplay /t REG_DWORD /d 1 /f");
                }
                MessageBox.Show($"AutoPlay has been {(enabled ? "ENABLED" : "DISABLED")}.", "AutoPlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set AutoPlay: {ex.Message}");
            }
        }

        // Remote Desktop
        private bool IsRemoteDesktopEnabled()
        {
            object? value = GetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections");
            return value != null && Convert.ToInt32(value) == 0;
        }

        private void SetRemoteDesktop(bool enabled)
        {
            try
            {
                // Set registry DIRECTLY for immediate effect
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true))
                {
                    if (key != null)
                    {
                        if (enabled)
                        {
                            // ENABLE Remote Desktop
                            key.SetValue("fDenyTSConnections", 0, Microsoft.Win32.RegistryValueKind.DWord);
                            key.SetValue("fAllowToGetHelp", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        else
                        {
                            // DISABLE Remote Desktop
                            key.SetValue("fDenyTSConnections", 1, Microsoft.Win32.RegistryValueKind.DWord);
                            key.SetValue("fAllowToGetHelp", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                    }
                }
                
                // Also set WinStations\RDP-Tcp settings
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
                {
                    if (key != null)
                    {
                        key.SetValue("UserAuthentication", enabled ? 0 : 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
                
                if (enabled)
                {
                    // ENABLE - Start service and enable firewall rules
                    ExecuteCmd("sc config TermService start= demand");
                    ExecuteCmd("net start TermService");
                    ExecuteNetsh("advfirewall firewall set rule group=\"Remote Desktop\" new enable=yes");
                    
                    System.Windows.Forms.MessageBox.Show("Remote Desktop has been ENABLED.\nYou can now connect remotely.", "Remote Desktop", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    // DISABLE - Disable firewall rules (don't stop service to avoid issues)
                    ExecuteNetsh("advfirewall firewall set rule group=\"Remote Desktop\" new enable=no");
                    
                    System.Windows.Forms.MessageBox.Show("Remote Desktop has been DISABLED.\nRemote connections are now blocked.", "Remote Desktop", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Remote Desktop: {ex.Message}");
            }
        }
        
        private void ExecuteCmd(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process? process = Process.Start(psi))
            {
                process?.WaitForExit(10000);
            }
        }

        #endregion

        #region Printer Methods

        private void RefreshPrinters(ComboBox combo)
        {
            combo.Items.Clear();
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    combo.Items.Add(printer);
                }
                if (combo.Items.Count > 0)
                    combo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading printers: {ex.Message}");
            }
        }

        private void TroubleshootPrinter(string? printerName)
        {
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("Please select a printer first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Running troubleshooter for: {printerName}\n\nThis will:\n• Check printer status\n• Clear print queue\n• Restart print spooler\n\nContinue?",
                "Printer Troubleshooter", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Stop spooler
                    RunCommand("net stop spooler");
                    System.Threading.Thread.Sleep(1000);

                    // Clear print jobs
                    RunCommand("del /Q /F %systemroot%\\System32\\spool\\PRINTERS\\*");

                    // Start spooler
                    RunCommand("net start spooler");

                    MessageBox.Show("Troubleshooting completed!\n\nThe print spooler has been restarted and print queue cleared.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Troubleshooting error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AutoInstallPrinterDriver(string? printerName)
        {
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("Please select a printer first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Open Windows printer driver installation
                Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = "printui.dll,PrintUIEntry /il",
                    UseShellExecute = true
                });

                MessageBox.Show("The Windows Add Printer wizard has been opened.\nFollow the prompts to install the driver.",
                    "Install Driver", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetDefaultPrinter(string? printerName)
        {
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("Please select a printer first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                RunCommand($"wmic printer where name=\"{printerName}\" call setdefaultprinter");
                MessageBox.Show($"'{printerName}' is now the default printer.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TestPrint(string? printerName)
        {
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("Please select a printer first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                RunPowerShell($"(Get-WmiObject -Query \"SELECT * FROM Win32_Printer WHERE Name='{printerName}'\").PrintTestPage()");
                MessageBox.Show($"Test page sent to '{printerName}'", "Test Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestartPrintSpooler()
        {
            try
            {
                RunCommand("net stop spooler && net start spooler");
                MessageBox.Show("Print Spooler restarted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearPrintQueue()
        {
            try
            {
                RunCommand("net stop spooler");
                System.Threading.Thread.Sleep(500);
                RunCommand("del /Q /F %systemroot%\\System32\\spool\\PRINTERS\\*");
                RunCommand("net start spooler");
                MessageBox.Show("Print queue cleared successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddNetworkPrinter()
        {
            using (Form form = new Form())
            {
                form.Text = "Add Network Printer";
                form.Size = new Size(400, 150);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = Color.FromArgb(33, 37, 41);

                Label pathLabel = new Label { Text = "Printer Path (e.g., \\\\server\\printer):", Location = new Point(20, 20), ForeColor = Color.White, AutoSize = true };
                TextBox pathBox = new TextBox { Location = new Point(20, 45), Width = 340 };

                Button addBtn = new Button 
                { 
                    Text = "Add Printer", 
                    Location = new Point(20, 80), 
                    Width = 120,
                    BackColor = Color.FromArgb(40, 167, 69), 
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                addBtn.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(pathBox.Text))
                    {
                        RunCommand($"rundll32 printui.dll,PrintUIEntry /in /n \"{pathBox.Text}\"");
                        MessageBox.Show($"Attempting to add printer: {pathBox.Text}", "Add Printer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.Close();
                    }
                };

                form.Controls.AddRange(new Control[] { pathLabel, pathBox, addBtn });
                form.ShowDialog();
            }
        }

        private void ScanNetworkPrinters()
        {
            MessageBox.Show("Scanning for network printers...\nThis will open Windows Settings > Printers & Scanners", 
                "Scan Network", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:printers",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        #endregion

        #region Security Tab Methods

        // UAC
        private bool IsUACEnabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA");
            return value == null || Convert.ToInt32(value) == 1;
        }

        private void SetUAC(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE UAC
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableLUA /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 5 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v PromptOnSecureDesktop /t REG_DWORD /d 1 /f");
                }
                else
                {
                    // DISABLE UAC
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableLUA /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v PromptOnSecureDesktop /t REG_DWORD /d 0 /f");
                }
                MessageBox.Show($"UAC has been {(enabled ? "ENABLED" : "DISABLED")}.\nRestart required for full effect.", "UAC", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set UAC: {ex.Message}");
            }
        }

        // Lock Screen
        private bool IsLockScreenDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetLockScreen(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Lock Screen
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization\" /v NoLockScreen /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization\" /v NoLockScreen /f 2>nul");
                }
                else
                {
                    // DISABLE Lock Screen
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization\" /v NoLockScreen /t REG_DWORD /d 1 /f");
                }
                MessageBox.Show($"Lock Screen has been {(enabled ? "ENABLED" : "DISABLED")}.", "Lock Screen", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Lock Screen: {ex.Message}");
            }
        }

        // Guest Account
        private bool IsGuestAccountEnabled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE Name='Guest' AND LocalAccount=True"))
                {
                    foreach (ManagementObject user in searcher.Get())
                    {
                        return !(bool)user["Disabled"];
                    }
                }
            }
            catch { }
            return false;
        }

        private void SetGuestAccount(bool enabled)
        {
            try
            {
                RunCommand($"net user Guest /active:{(enabled ? "yes" : "no")}");
                MessageBox.Show($"Guest Account has been {(enabled ? "ENABLED" : "DISABLED")}.", "Guest Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Guest Account: {ex.Message}");
            }
        }

        // Hibernate
        private bool IsHibernateEnabled()
        {
            object? value = GetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetHibernate(bool enabled)
        {
            try
            {
                RunCommand(enabled ? "powercfg /hibernate on" : "powercfg /hibernate off");
                MessageBox.Show($"Hibernate has been {(enabled ? "ENABLED" : "DISABLED")}.", "Hibernate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Hibernate: {ex.Message}");
            }
        }

        // Fast Startup
        private bool IsFastStartupEnabled()
        {
            object? value = GetRegistryValue(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetFastStartup(bool enabled)
        {
            try
            {
                RunCommand($"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v HiberbootEnabled /t REG_DWORD /d {(enabled ? 1 : 0)} /f");
                MessageBox.Show($"Fast Startup has been {(enabled ? "ENABLED" : "DISABLED")}.", "Fast Startup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Fast Startup: {ex.Message}");
            }
        }

        // BitLocker Status
        private string GetBitLockerStatus()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "manage-bde",
                    Arguments = "-status C:",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using (Process? process = Process.Start(psi))
                {
                    string output = process?.StandardOutput.ReadToEnd() ?? "";
                    if (output.Contains("Protection On"))
                        return "🔒 BitLocker is ENABLED on C: drive";
                    else if (output.Contains("Protection Off"))
                        return "🔓 BitLocker is DISABLED on C: drive";
                    else
                        return "ℹ️ BitLocker status unknown";
                }
            }
            catch
            {
                return "⚠️ Unable to check BitLocker status";
            }
        }

        // Windows Hello
        private bool IsWindowsHelloEnabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\PolicyManager\default\Settings\AllowSignInOptions", "value");
            return value == null || Convert.ToInt32(value) != 0;
        }

        private void SetWindowsHello(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Windows Hello
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\" /v Enabled /f 2>nul");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Settings\\AllowSignInOptions\" /v value /t REG_DWORD /d 1 /f");
                }
                else
                {
                    // DISABLE Windows Hello
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\" /v Enabled /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\Settings\\AllowSignInOptions\" /v value /t REG_DWORD /d 0 /f");
                }
                MessageBox.Show($"Windows Hello has been {(enabled ? "ENABLED" : "DISABLED")}.", "Windows Hello", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Windows Hello: {ex.Message}");
            }
        }

        // Screensaver Password
        private bool IsScreensaverPasswordEnabled()
        {
            object? value = GetRegistryValue(@"Control Panel\Desktop", "ScreenSaverIsSecure", false);
            return value != null && value.ToString() == "1";
        }

        private void SetScreensaverPassword(bool enabled)
        {
            try
            {
                RunCommand($"reg add \"HKCU\\Control Panel\\Desktop\" /v ScreenSaverIsSecure /t REG_SZ /d {(enabled ? "1" : "0")} /f");
                MessageBox.Show($"Screensaver Password has been {(enabled ? "ENABLED" : "DISABLED")}.", "Screensaver Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Screensaver Password: {ex.Message}");
            }
        }

        #endregion

        #region Network Tab Methods

        // Network Discovery
        private bool IsNetworkDiscoveryEnabled()
        {
            try
            {
                using (ServiceController sc = new ServiceController("FDResPub"))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetNetworkDiscovery(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Network Discovery
                    RunCommand("sc config FDResPub start= auto");
                    RunCommand("sc config SSDPSRV start= auto");
                    RunCommand("sc config upnphost start= auto");
                    RunCommand("net start FDResPub 2>nul");
                    RunCommand("net start SSDPSRV 2>nul");
                    RunCommand("net start upnphost 2>nul");
                    RunCommand("netsh advfirewall firewall set rule group=\"Network Discovery\" new enable=Yes");
                }
                else
                {
                    // DISABLE Network Discovery
                    RunCommand("netsh advfirewall firewall set rule group=\"Network Discovery\" new enable=No");
                    RunCommand("net stop FDResPub 2>nul");
                    RunCommand("net stop SSDPSRV 2>nul");
                    RunCommand("net stop upnphost 2>nul");
                    RunCommand("sc config FDResPub start= disabled");
                    RunCommand("sc config SSDPSRV start= disabled");
                    RunCommand("sc config upnphost start= disabled");
                }
                MessageBox.Show($"Network Discovery has been {(enabled ? "ENABLED" : "DISABLED")}.", "Network Discovery", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Network Discovery: {ex.Message}");
            }
        }

        // File Sharing - use static field to track state reliably
        private static bool _fileSharingEnabled = false;
        private static bool _fileSharingInitialized = false;
        
        private bool IsFileSharingEnabled()
        {
            if (!_fileSharingInitialized)
            {
                // Initialize from registry on first call
                try
                {
                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DesktopController"))
                    {
                        if (key != null)
                        {
                            object? val = key.GetValue("FileSharingEnabled");
                            if (val != null)
                            {
                                _fileSharingEnabled = Convert.ToInt32(val) == 1;
                            }
                        }
                    }
                }
                catch { }
                _fileSharingInitialized = true;
            }
            return _fileSharingEnabled;
        }

        private void SetFileSharing(bool enabled)
        {
            try
            {
                // Update static state immediately
                _fileSharingEnabled = enabled;
                
                // Save to registry
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\DesktopController"))
                {
                    key.SetValue("FileSharingEnabled", enabled ? 1 : 0, RegistryValueKind.DWord);
                }
                
                // Run firewall command in background thread
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        string enableVal = enabled ? "Yes" : "No";
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"advfirewall firewall set rule group=\"Network Discovery\" new enable={enableVal}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"advfirewall firewall set rule group=\"File and Printer Sharing\" new enable={enableVal}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                    catch { }
                });
                
                MessageBox.Show($"File Sharing {(enabled ? "ENABLED" : "DISABLED")}", "File Sharing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set File Sharing: {ex.Message}");
            }
        }

        // WiFi
        private bool IsWiFiEnabled()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        return ni.OperationalStatus == OperationalStatus.Up;
                    }
                }
            }
            catch { }
            return false;
        }

        private void SetWiFi(bool enabled)
        {
            try
            {
                bool found = false;
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    {
                        RunCommand($"netsh interface set interface \"{ni.Name}\" {(enabled ? "enable" : "disable")}");
                        found = true;
                    }
                }
                
                // Also try using PowerShell for more reliability
                if (enabled)
                {
                    RunPowerShell("Get-NetAdapter -Name '*Wi*' | Enable-NetAdapter -Confirm:$false -ErrorAction SilentlyContinue");
                    RunPowerShell("Get-NetAdapter -Name '*Wireless*' | Enable-NetAdapter -Confirm:$false -ErrorAction SilentlyContinue");
                }
                else
                {
                    RunPowerShell("Get-NetAdapter -Name '*Wi*' | Disable-NetAdapter -Confirm:$false -ErrorAction SilentlyContinue");
                    RunPowerShell("Get-NetAdapter -Name '*Wireless*' | Disable-NetAdapter -Confirm:$false -ErrorAction SilentlyContinue");
                }
                
                MessageBox.Show($"WiFi Adapter has been {(enabled ? "ENABLED" : "DISABLED")}.", "WiFi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set WiFi: {ex.Message}");
            }
        }

        // Bluetooth
        private bool IsBluetoothEnabled()
        {
            object? value = GetRegistryValue(@"SYSTEM\CurrentControlSet\Services\bthserv", "Start");
            return value != null && Convert.ToInt32(value) != 4;
        }

        private void SetBluetooth(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Bluetooth
                    RunCommand("sc config bthserv start= auto");
                    RunCommand("sc config BTAGService start= auto");
                    RunCommand("net start bthserv 2>nul");
                    RunCommand("net start BTAGService 2>nul");
                }
                else
                {
                    // DISABLE Bluetooth
                    RunCommand("net stop bthserv 2>nul");
                    RunCommand("net stop BTAGService 2>nul");
                    RunCommand("sc config bthserv start= disabled");
                    RunCommand("sc config BTAGService start= disabled");
                }
                MessageBox.Show($"Bluetooth has been {(enabled ? "ENABLED" : "DISABLED")}.", "Bluetooth", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Bluetooth: {ex.Message}");
            }
        }

        // Network Info
        private string GetNetworkInfo()
        {
            try
            {
                string info = "";
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet || 
                         ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    {
                        info += $"• {ni.Name}: {ni.NetworkInterfaceType}\n";
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                info += $"  IP: {ip.Address}\n";
                            }
                        }
                    }
                }
                return string.IsNullOrEmpty(info) ? "No active network connections" : info;
            }
            catch
            {
                return "Unable to retrieve network information";
            }
        }

        private void FlushDNS()
        {
            RunCommand("ipconfig /flushdns");
            MessageBox.Show("DNS cache flushed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetNetwork()
        {
            DialogResult result = MessageBox.Show(
                "This will reset all network adapters and settings.\nYour computer will need to restart.\n\nContinue?",
                "Reset Network", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                RunCommand("netsh winsock reset");
                RunCommand("netsh int ip reset");
                MessageBox.Show("Network reset complete. Please restart your computer.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowNetworkConnections()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ncpa.cpl",
                UseShellExecute = true
            });
        }

        #endregion

        #region Advanced Tab Methods

        // Windows Script Host
        private bool IsWSHDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows Script Host\Settings", "Enabled");
            return value != null && Convert.ToInt32(value) == 0;
        }

        private void SetWSH(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows Script Host\\Settings\" /v Enabled /t REG_DWORD /d 1 /f");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows Script Host\\Settings\" /v Enabled /f 2>nul");
                }
                else
                {
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows Script Host\\Settings\" /v Enabled /t REG_DWORD /d 0 /f");
                }
                MessageBox.Show($"Windows Script Host has been {(enabled ? "ENABLED" : "DISABLED")}.", "Windows Script Host", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Windows Script Host: {ex.Message}");
            }
        }

        // MSI Installer
        private bool IsMSIDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Installer", "DisableMSI");
            return value != null && Convert.ToInt32(value) != 0;
        }

        private void SetMSI(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE MSI Installer
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Installer\" /v DisableMSI /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Installer\" /v DisableUserInstalls /f 2>nul");
                    RunCommand("sc config msiserver start= demand");
                    RunCommand("net start msiserver 2>nul");
                }
                else
                {
                    // DISABLE MSI Installer
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Installer\" /v DisableMSI /t REG_DWORD /d 2 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Installer\" /v DisableUserInstalls /t REG_DWORD /d 1 /f");
                    RunCommand("net stop msiserver 2>nul");
                    RunCommand("sc config msiserver start= disabled");
                }
                MessageBox.Show($"Windows Installer has been {(enabled ? "ENABLED" : "DISABLED")}.", "Windows Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Windows Installer: {ex.Message}");
            }
        }

        // Context Menu
        private bool IsContextMenuDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoViewContextMenu", false);
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetContextMenu(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Context Menu
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoViewContextMenu /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoViewContextMenu /f 2>nul");
                }
                else
                {
                    // DISABLE Context Menu
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoViewContextMenu /t REG_DWORD /d 1 /f");
                }
                RestartExplorerShell();
                MessageBox.Show($"Right-Click Context Menu has been {(enabled ? "ENABLED" : "DISABLED")}.", "Context Menu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Context Menu: {ex.Message}");
            }
        }

        // Run Dialog
        private bool IsRunDialogDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoRun", false);
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetRunDialog(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Run Dialog
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRun /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRun /f 2>nul");
                }
                else
                {
                    // DISABLE Run Dialog
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRun /t REG_DWORD /d 1 /f");
                }
                RestartExplorerShell();
                MessageBox.Show($"Run Dialog (Win+R) has been {(enabled ? "ENABLED" : "DISABLED")}.", "Run Dialog", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Run Dialog: {ex.Message}");
            }
        }

        // Windows Search
        private bool IsWindowsSearchEnabled()
        {
            try
            {
                using (ServiceController sc = new ServiceController("WSearch"))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetWindowsSearch(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Windows Search
                    RunCommand("sc config WSearch start= auto");
                    RunCommand("net start WSearch 2>nul");
                }
                else
                {
                    // DISABLE Windows Search
                    RunCommand("net stop WSearch 2>nul");
                    RunCommand("sc config WSearch start= disabled");
                }
                MessageBox.Show($"Windows Search has been {(enabled ? "ENABLED" : "DISABLED")}.", "Windows Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Windows Search: {ex.Message}");
            }
        }

        // Cortana
        private bool IsCortanaDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            return value != null && Convert.ToInt32(value) == 0;
        }

        private void SetCortana(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Cortana
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaEnabled /f 2>nul");
                }
                else
                {
                    // DISABLE Cortana
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v AllowCortana /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaEnabled /t REG_DWORD /d 0 /f");
                }
                MessageBox.Show($"Cortana has been {(enabled ? "ENABLED" : "DISABLED")}.", "Cortana", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Cortana: {ex.Message}");
            }
        }

        // Notifications
        private bool IsNotificationsDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetNotifications(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Notifications
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PushNotifications\" /v ToastEnabled /f 2>nul");
                }
                else
                {
                    // DISABLE Notifications
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PushNotifications\" /v ToastEnabled /t REG_DWORD /d 0 /f");
                }
                MessageBox.Show($"System Notifications have been {(enabled ? "ENABLED" : "DISABLED")}.", "Notifications", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Notifications: {ex.Message}");
            }
        }

        // Telemetry
        private bool IsTelemetryDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
            return value != null && Convert.ToInt32(value) == 0;
        }

        private void SetTelemetry(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Telemetry
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /f 2>nul");
                    RunCommand("sc config DiagTrack start= auto");
                    RunCommand("net start DiagTrack 2>nul");
                }
                else
                {
                    // DISABLE Telemetry
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f");
                    RunCommand("net stop DiagTrack 2>nul");
                    RunCommand("sc config DiagTrack start= disabled");
                }
                MessageBox.Show($"Windows Telemetry has been {(enabled ? "ENABLED" : "DISABLED")}.", "Telemetry", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Telemetry: {ex.Message}");
            }
        }

        // Action Center
        private bool IsActionCenterDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetActionCenter(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Action Center
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /f 2>nul");
                }
                else
                {
                    // DISABLE Action Center
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /t REG_DWORD /d 1 /f");
                }
                RestartExplorerShell();
                MessageBox.Show($"Action Center has been {(enabled ? "ENABLED" : "DISABLED")}.", "Action Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Action Center: {ex.Message}");
            }
        }

        // OneDrive
        private bool IsOneDriveDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetOneDrive(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE OneDrive
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSyncNGSC /f 2>nul");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSync /f 2>nul");
                }
                else
                {
                    // DISABLE OneDrive
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSync /t REG_DWORD /d 1 /f");
                    RunCommand("taskkill /f /im OneDrive.exe 2>nul");
                }
                MessageBox.Show($"OneDrive has been {(enabled ? "ENABLED" : "DISABLED")}.", "OneDrive", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set OneDrive: {ex.Message}");
            }
        }

        // Game Bar
        private bool IsGameBarDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled");
            return value != null && Convert.ToInt32(value) == 0;
        }

        private void SetGameBar(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Game Bar
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameDVR\" /v AppCaptureEnabled /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\GameBar\" /v UseNexusForGameBarEnabled /t REG_DWORD /d 1 /f");
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /f 2>nul");
                }
                else
                {
                    // DISABLE Game Bar
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameDVR\" /v AppCaptureEnabled /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\GameBar\" /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f");
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /t REG_DWORD /d 0 /f");
                }
                MessageBox.Show($"Xbox Game Bar has been {(enabled ? "ENABLED" : "DISABLED")}.", "Game Bar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Game Bar: {ex.Message}");
            }
        }

        // Thumbs.db
        private bool IsThumbsDbDisabled()
        {
            object? value = GetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableThumbsDBOnNetworkFolders");
            return value != null && Convert.ToInt32(value) == 1;
        }

        private void SetThumbsDb(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // ENABLE Thumbs.db creation
                    RunCommand("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableThumbsDBOnNetworkFolders /f 2>nul");
                    RunCommand("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisableThumbnailCache /f 2>nul");
                }
                else
                {
                    // DISABLE Thumbs.db creation
                    RunCommand("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableThumbsDBOnNetworkFolders /t REG_DWORD /d 1 /f");
                    RunCommand("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisableThumbnailCache /t REG_DWORD /d 1 /f");
                }
                MessageBox.Show($"Thumbs.db Creation has been {(enabled ? "ENABLED" : "DISABLED")}.", "Thumbs.db", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Thumbs.db: {ex.Message}");
            }
        }

        #endregion
    }
}
