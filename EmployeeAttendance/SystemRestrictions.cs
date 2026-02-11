using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace EmployeeAttendance
{
    /// <summary>
    /// Static class providing system restriction methods for Danger Zone features.
    /// All methods are silent (no MessageBox popups) and designed to be called from SystemControlHandler.
    /// Uses multi-layer blocking for maximum effectiveness.
    /// </summary>
    public static class SystemRestrictions
    {
        private static readonly string MarkerDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController");

        private static System.Threading.Timer? _clipboardTimer;
        private static bool _clipboardBlocked = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        // ==================== HELPER METHODS ====================

        private static void RunCmd(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit(15000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] RunCmd error: {ex.Message}");
            }
        }

        private static void EnsureMarkerDir()
        {
            if (!Directory.Exists(MarkerDir))
                Directory.CreateDirectory(MarkerDir);
        }

        private static void WriteMarker(string name)
        {
            EnsureMarkerDir();
            File.WriteAllText(Path.Combine(MarkerDir, name), DateTime.Now.ToString());
        }

        private static void DeleteMarker(string name)
        {
            var path = Path.Combine(MarkerDir, name);
            if (File.Exists(path)) File.Delete(path);
        }

        private static bool MarkerExists(string name)
        {
            return File.Exists(Path.Combine(MarkerDir, name));
        }

        // ==================== 1. CMD BLOCK/UNBLOCK ====================

        public static void BlockCMD()
        {
            try
            {
                // Layer 1: HKCU policy - DisableCMD=2 (disable CMD but allow batch files)
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\System"))
                {
                    key?.SetValue("DisableCMD", 2, RegistryValueKind.DWord);
                }

                // Layer 2: HKLM policy
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    key?.SetValue("DisableCMD", 2, RegistryValueKind.DWord);
                }

                // Layer 3: IFEO redirect - cmd.exe debugger trick
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\cmd.exe"))
                {
                    key?.SetValue("Debugger", "cmd.exe /c exit", RegistryValueKind.String);
                }

                WriteMarker("cmd_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] CMD blocked (3 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockCMD error: {ex.Message}");
            }
        }

        public static void UnblockCMD()
        {
            try
            {
                // Remove HKCU policy
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\System"))
                {
                    key?.DeleteValue("DisableCMD", false);
                }

                // Remove HKLM policy
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    key?.DeleteValue("DisableCMD", false);
                }

                // Remove IFEO redirect
                try
                {
                    Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\cmd.exe", false);
                }
                catch { }

                DeleteMarker("cmd_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] CMD unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockCMD error: {ex.Message}");
            }
        }

        public static bool IsCMDBlocked()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\System"))
                {
                    var val = key?.GetValue("DisableCMD");
                    if (val != null && Convert.ToInt32(val) != 0) return true;
                }
            }
            catch { }
            return MarkerExists("cmd_blocked.txt");
        }

        // ==================== 2. POWERSHELL BLOCK/UNBLOCK ====================

        public static void BlockPowerShell()
        {
            try
            {
                // Layer 1: Execution policy via registry
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PowerShell"))
                {
                    key?.SetValue("EnableScripts", 0, RegistryValueKind.DWord);
                    key?.SetValue("ExecutionPolicy", "Restricted", RegistryValueKind.String);
                }

                // Layer 2: Shell execution policy
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"))
                {
                    key?.SetValue("ExecutionPolicy", "Restricted", RegistryValueKind.String);
                }

                // Layer 3: DisallowRun policy
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key?.SetValue("DisallowRun", 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"))
                {
                    key?.SetValue("1", "powershell.exe", RegistryValueKind.String);
                    key?.SetValue("2", "powershell_ise.exe", RegistryValueKind.String);
                    key?.SetValue("3", "pwsh.exe", RegistryValueKind.String);
                }

                // Layer 4: Constrained language mode
                try
                {
                    Environment.SetEnvironmentVariable("__PSLockdownPolicy", "4", EnvironmentVariableTarget.Machine);
                }
                catch { }

                // Layer 5: icacls deny execute on PowerShell executables
                string[] psExes = new[]
                {
                    @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                    @"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe",
                    @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell_ise.exe"
                };
                foreach (var path in psExes)
                {
                    if (File.Exists(path))
                        RunCmd($"icacls \"{path}\" /deny Everyone:(X)");
                }

                WriteMarker("powershell_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] PowerShell blocked (5 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockPowerShell error: {ex.Message}");
            }
        }

        public static void UnblockPowerShell()
        {
            try
            {
                // Remove execution policy
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PowerShell"))
                {
                    key?.DeleteValue("EnableScripts", false);
                    key?.DeleteValue("ExecutionPolicy", false);
                }

                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"))
                {
                    key?.SetValue("ExecutionPolicy", "RemoteSigned", RegistryValueKind.String);
                }

                // Remove DisallowRun
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key?.DeleteValue("DisallowRun", false);
                }
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun", false);
                }
                catch { }

                // Remove constrained language mode
                try
                {
                    Environment.SetEnvironmentVariable("__PSLockdownPolicy", null, EnvironmentVariableTarget.Machine);
                }
                catch { }

                // Remove icacls deny
                string[] psExes = new[]
                {
                    @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                    @"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe",
                    @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell_ise.exe"
                };
                foreach (var path in psExes)
                {
                    if (File.Exists(path))
                        RunCmd($"icacls \"{path}\" /remove:d Everyone");
                }

                DeleteMarker("powershell_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] PowerShell unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockPowerShell error: {ex.Message}");
            }
        }

        public static bool IsPowerShellBlocked()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PowerShell"))
                {
                    var val = key?.GetValue("EnableScripts");
                    if (val != null && Convert.ToInt32(val) == 0) return true;
                }
            }
            catch { }
            return MarkerExists("powershell_blocked.txt");
        }

        // ==================== 3. REGISTRY EDITOR BLOCK/UNBLOCK ====================

        public static void BlockRegedit()
        {
            try
            {
                // Layer 1: HKCU
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);
                }

                // Layer 2: HKLM
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);
                }

                WriteMarker("regedit_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] Registry Editor blocked (2 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockRegedit error: {ex.Message}");
            }
        }

        public static void UnblockRegedit()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.DeleteValue("DisableRegistryTools", false);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.DeleteValue("DisableRegistryTools", false);
                }

                DeleteMarker("regedit_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] Registry Editor unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockRegedit error: {ex.Message}");
            }
        }

        public static bool IsRegeditBlocked()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    var val = key?.GetValue("DisableRegistryTools");
                    if (val != null && Convert.ToInt32(val) != 0) return true;
                }
            }
            catch { }
            return MarkerExists("regedit_blocked.txt");
        }

        // ==================== 4. COPY/PASTE (CLIPBOARD) BLOCK/UNBLOCK ====================

        public static void BlockCopyPaste()
        {
            try
            {
                // Layer 1: Marker file for persistence
                WriteMarker("clipboard_blocked.txt");

                // Layer 2: Start clipboard clearer timer (clears every 500ms)
                _clipboardBlocked = true;
                _clipboardTimer?.Dispose();
                _clipboardTimer = new System.Threading.Timer(ClipboardClearerCallback, null, 0, 500);

                // Layer 3: Disable clipboard history policy
                RunCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v AllowClipboardHistory /t REG_DWORD /d 0 /f");
                RunCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Clipboard\" /v EnableClipboardHistory /t REG_DWORD /d 0 /f");

                // Layer 4: Terminal Services clipboard disable
                RunCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services\" /v fDisableClip /t REG_DWORD /d 1 /f");

                // Layer 5: Store in our app's restrictions registry
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("BlockCopyPaste", 1, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] Copy/Paste blocked (5 layers + timer)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockCopyPaste error: {ex.Message}");
            }
        }

        public static void UnblockCopyPaste()
        {
            try
            {
                // Stop timer
                _clipboardBlocked = false;
                _clipboardTimer?.Dispose();
                _clipboardTimer = null;

                // Remove marker
                DeleteMarker("clipboard_blocked.txt");

                // Re-enable clipboard services
                RunCmd("sc config ClipSVC start= auto 2>nul");
                RunCmd("net start ClipSVC 2>nul");

                // Remove policies
                RunCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v AllowClipboardHistory /f 2>nul");
                RunCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Clipboard\" /v EnableClipboardHistory /t REG_DWORD /d 1 /f");
                RunCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services\" /v fDisableClip /f 2>nul");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("BlockCopyPaste", 0, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] Copy/Paste unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockCopyPaste error: {ex.Message}");
            }
        }

        public static bool IsCopyPasteBlocked()
        {
            return _clipboardBlocked || MarkerExists("clipboard_blocked.txt");
        }

        /// <summary>
        /// Called on startup to re-activate clipboard blocking if marker exists
        /// </summary>
        public static void ResumeClipboardBlockingIfNeeded()
        {
            if (MarkerExists("clipboard_blocked.txt") && !_clipboardBlocked)
            {
                _clipboardBlocked = true;
                _clipboardTimer?.Dispose();
                _clipboardTimer = new System.Threading.Timer(ClipboardClearerCallback, null, 0, 500);
                Debug.WriteLine("[SystemRestrictions] Clipboard blocking resumed from marker");
            }
        }

        private static void ClipboardClearerCallback(object? state)
        {
            if (_clipboardBlocked)
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
        }

        // ==================== 5. SOFTWARE INSTALL BLOCK/UNBLOCK ====================

        public static void BlockSoftwareInstall()
        {
            try
            {
                // Layer 1: Disable Windows Installer (MSI)
                using (var key = Registry.LocalMachine.CreateSubKey(@"Software\Policies\Microsoft\Windows\Installer"))
                {
                    key?.SetValue("DisableMSI", 2, RegistryValueKind.DWord);
                    key?.SetValue("DisableUserInstalls", 1, RegistryValueKind.DWord);
                }

                // Layer 2: DisallowRun for installer executables
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key?.SetValue("DisallowRun", 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"))
                {
                    key?.SetValue("10", "msiexec.exe", RegistryValueKind.String);
                    key?.SetValue("11", "setup.exe", RegistryValueKind.String);
                    key?.SetValue("12", "install.exe", RegistryValueKind.String);
                    key?.SetValue("13", "installer.exe", RegistryValueKind.String);
                }

                // Layer 3: Stop MSI service
                RunCmd("sc stop msiserver 2>nul");
                RunCmd("sc config msiserver start= disabled 2>nul");

                WriteMarker("softwareinstall_blocked.txt");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("BlockSoftwareInstall", 1, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] Software installation blocked (3 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockSoftwareInstall error: {ex.Message}");
            }
        }

        public static void UnblockSoftwareInstall()
        {
            try
            {
                // Remove MSI policies
                using (var key = Registry.LocalMachine.CreateSubKey(@"Software\Policies\Microsoft\Windows\Installer"))
                {
                    key?.DeleteValue("DisableMSI", false);
                    key?.DeleteValue("DisableUserInstalls", false);
                }

                // Remove DisallowRun entries for installers
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"))
                {
                    key?.DeleteValue("10", false);
                    key?.DeleteValue("11", false);
                    key?.DeleteValue("12", false);
                    key?.DeleteValue("13", false);
                }

                // Re-enable MSI service
                RunCmd("sc config msiserver start= demand 2>nul");
                RunCmd("sc start msiserver 2>nul");

                DeleteMarker("softwareinstall_blocked.txt");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("BlockSoftwareInstall", 0, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] Software installation unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockSoftwareInstall error: {ex.Message}");
            }
        }

        public static bool IsSoftwareInstallBlocked()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\Installer"))
                {
                    var val = key?.GetValue("DisableMSI");
                    if (val != null && Convert.ToInt32(val) >= 1) return true;
                }
            }
            catch { }
            return MarkerExists("softwareinstall_blocked.txt");
        }

        // ==================== 6. DELETE FUNCTION BLOCK/UNBLOCK ====================

        public static void BlockDelete()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Layer 1: Marker
                WriteMarker("delete_blocked.txt");

                // Layer 2: Block DELETE key via scancode remap (effective after reboot)
                RunCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout\" /v \"Scancode Map\" /t REG_BINARY /d 0000000000000000020000000000530000000000 /f");

                // Layer 3: Deny delete permission on Desktop and Downloads via icacls
                RunCmd($"icacls \"{desktop}\" /deny Everyone:(DE,DC) /T /C /Q");
                RunCmd($"icacls \"{userProfile}\\Downloads\" /deny Everyone:(DE,DC) /T /C /Q");

                // Layer 4: Require confirmation for delete + no recycle
                RunCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v ConfirmFileDelete /t REG_DWORD /d 1 /f");
                RunCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRecycleFiles /t REG_DWORD /d 1 /f");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("BlockDelete", 1, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] Delete function blocked (4 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockDelete error: {ex.Message}");
            }
        }

        public static void UnblockDelete()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                DeleteMarker("delete_blocked.txt");

                // Remove scancode remap
                RunCmd("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout\" /v \"Scancode Map\" /f 2>nul");

                // Remove icacls deny
                RunCmd($"icacls \"{desktop}\" /remove:d Everyone /T /C /Q 2>nul");
                RunCmd($"icacls \"{userProfile}\\Downloads\" /remove:d Everyone /T /C /Q 2>nul");

                // Reset permissions
                RunCmd($"icacls \"{desktop}\" /reset /T /C /Q 2>nul");
                RunCmd($"icacls \"{userProfile}\\Downloads\" /reset /T /C /Q 2>nul");

                // Remove policies
                RunCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v ConfirmFileDelete /f 2>nul");
                RunCmd("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRecycleFiles /f 2>nul");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("BlockDelete", 0, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] Delete function unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockDelete error: {ex.Message}");
            }
        }

        public static bool IsDeleteBlocked()
        {
            return MarkerExists("delete_blocked.txt");
        }

        // ==================== 7. IP CHANGE BLOCK/UNBLOCK ====================

        public static void BlockIPChange()
        {
            try
            {
                // Layer 1: Marker
                WriteMarker("ipchange_blocked.txt");

                // Layer 2: Network connection policies
                RunCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_LanChangeProperties /t REG_DWORD /d 0 /f");
                RunCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_EnableAdminProhibits /t REG_DWORD /d 1 /f");
                RunCmd("reg add \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_LanChangeProperties /t REG_DWORD /d 0 /f");
                RunCmd("reg add \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /v NC_EnableAdminProhibits /t REG_DWORD /d 1 /f");

                // Layer 3: IFEO redirect for ncpa.cpl and netsh.exe
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ncpa.cpl"))
                {
                    key?.SetValue("Debugger", "cmd.exe /c exit", RegistryValueKind.String);
                }

                // Layer 4: Disable network setup service
                RunCmd("sc stop NetSetupSvc 2>nul");
                RunCmd("sc config NetSetupSvc start= disabled 2>nul");

                // Layer 5: Hide network settings pages
                RunCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v SettingsPageVisibility /t REG_SZ /d \"hide:network-proxy;network-ethernet;network-wifi\" /f");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("LockIP", 1, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] IP change blocked (5 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockIPChange error: {ex.Message}");
            }
        }

        public static void UnblockIPChange()
        {
            try
            {
                DeleteMarker("ipchange_blocked.txt");

                // Remove network policies
                RunCmd("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /f 2>nul");
                RunCmd("reg delete \"HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Network Connections\" /f 2>nul");

                // Remove IFEO redirect
                try
                {
                    Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ncpa.cpl", false);
                }
                catch { }

                // Re-enable service
                RunCmd("sc config NetSetupSvc start= demand 2>nul");
                RunCmd("sc start NetSetupSvc 2>nul");

                // Remove settings page hiding
                RunCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v SettingsPageVisibility /f 2>nul");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\Restrictions"))
                {
                    key?.SetValue("LockIP", 0, RegistryValueKind.DWord);
                }

                Debug.WriteLine("[SystemRestrictions] IP change unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockIPChange error: {ex.Message}");
            }
        }

        public static bool IsIPChangeBlocked()
        {
            return MarkerExists("ipchange_blocked.txt");
        }

        // ==================== 8. TASK MANAGER BLOCK/UNBLOCK ====================

        public static void BlockTaskManager()
        {
            try
            {
                // Layer 1: HKCU
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                }

                // Layer 2: HKLM
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                }

                WriteMarker("taskmgr_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] Task Manager blocked (2 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockTaskManager error: {ex.Message}");
            }
        }

        public static void UnblockTaskManager()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.DeleteValue("DisableTaskMgr", false);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.DeleteValue("DisableTaskMgr", false);
                }

                DeleteMarker("taskmgr_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] Task Manager unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockTaskManager error: {ex.Message}");
            }
        }

        public static bool IsTaskManagerBlocked()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    var val = key?.GetValue("DisableTaskMgr");
                    if (val != null && Convert.ToInt32(val) == 1) return true;
                }
            }
            catch { }
            return MarkerExists("taskmgr_blocked.txt");
        }

        // ==================== 9. CONTROL PANEL BLOCK/UNBLOCK ====================

        public static void BlockControlPanel()
        {
            try
            {
                // Layer 1: HKCU
                RunCmd("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /t REG_DWORD /d 1 /f");

                // Layer 2: HKLM
                RunCmd("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /t REG_DWORD /d 1 /f");

                // Layer 3: Also block Settings app folder options
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key?.SetValue("NoSetFolderOptions", 1, RegistryValueKind.DWord);
                }

                // Kill any running control panel
                RunCmd("taskkill /f /im control.exe 2>nul");
                RunCmd("taskkill /f /im SystemSettings.exe 2>nul");

                WriteMarker("controlpanel_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] Control Panel blocked (3 layers)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockControlPanel error: {ex.Message}");
            }
        }

        public static void UnblockControlPanel()
        {
            try
            {
                RunCmd("reg delete \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /f 2>nul");
                RunCmd("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoControlPanel /f 2>nul");

                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key?.DeleteValue("NoSetFolderOptions", false);
                }

                DeleteMarker("controlpanel_blocked.txt");
                Debug.WriteLine("[SystemRestrictions] Control Panel unblocked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockControlPanel error: {ex.Message}");
            }
        }

        public static bool IsControlPanelBlocked()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    var val = key?.GetValue("NoControlPanel");
                    if (val != null && Convert.ToInt32(val) == 1) return true;
                }
            }
            catch { }
            return MarkerExists("controlpanel_blocked.txt");
        }

        // ==================== ENHANCED APP BLOCKING (4-Layer) ====================

        /// <summary>
        /// Enhanced 4-layer application blocking that is difficult for users to reverse
        /// </summary>
        public static void BlockApplicationEnhanced(string appName, string appPath, string exeName)
        {
            try
            {
                // Determine the exe name
                if (string.IsNullOrWhiteSpace(exeName))
                {
                    exeName = Path.GetFileName(appPath);
                    if (string.IsNullOrWhiteSpace(exeName))
                        exeName = appName + ".exe";
                }

                // Layer 1: Kill the process
                string processName = exeName.Replace(".exe", "").Trim();
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (proc.ProcessName.IndexOf(processName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            proc.Kill();
                            proc.WaitForExit(3000);
                        }
                    }
                    catch { }
                }

                Thread.Sleep(1000);

                // Layer 2: IFEO redirect (prevents exe from launching)
                using (var key = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exeName}"))
                {
                    key?.SetValue("Debugger", "cmd.exe /c exit", RegistryValueKind.String);
                }

                // Layer 3: DisallowRun policy
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    key?.SetValue("DisallowRun", 1, RegistryValueKind.DWord);
                }
                // Find next available slot
                int slot = 100;
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"))
                {
                    // Find a free slot
                    for (int i = 100; i < 200; i++)
                    {
                        var existing = key?.GetValue(i.ToString());
                        if (existing == null || existing.ToString() == exeName)
                        {
                            slot = i;
                            break;
                        }
                    }
                    key?.SetValue(slot.ToString(), exeName, RegistryValueKind.String);
                }

                // Layer 4: icacls deny execute on the path
                if (!string.IsNullOrWhiteSpace(appPath) && File.Exists(appPath))
                {
                    RunCmd($"icacls \"{appPath}\" /deny Everyone:(RX)");
                }

                // Store in our blocked apps registry
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\EmployeeAttendance\BlockedApps"))
                {
                    key?.SetValue(appName, $"{appPath}|{exeName}|{slot}", RegistryValueKind.String);
                }

                Debug.WriteLine($"[SystemRestrictions] App blocked (4 layers): {appName} ({exeName})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] BlockApplicationEnhanced error: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced unblock application - removes all 4 layers
        /// </summary>
        public static void UnblockApplicationEnhanced(string appName, string appPath, string exeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(exeName))
                {
                    exeName = Path.GetFileName(appPath);
                    if (string.IsNullOrWhiteSpace(exeName))
                        exeName = appName + ".exe";
                }

                // Remove IFEO redirect
                try
                {
                    Registry.LocalMachine.DeleteSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exeName}", false);
                }
                catch { }

                // Remove from DisallowRun
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun", true))
                    {
                        if (key != null)
                        {
                            foreach (var name in key.GetValueNames())
                            {
                                var val = key.GetValue(name)?.ToString();
                                if (val != null && val.Equals(exeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    key.DeleteValue(name, false);
                                }
                            }
                        }
                    }
                }
                catch { }

                // Remove icacls deny
                if (!string.IsNullOrWhiteSpace(appPath) && File.Exists(appPath))
                {
                    RunCmd($"icacls \"{appPath}\" /remove:d Everyone");
                }

                // Remove from our blocked apps registry
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\EmployeeAttendance\BlockedApps", true))
                    {
                        key?.DeleteValue(appName, false);
                    }
                }
                catch { }

                Debug.WriteLine($"[SystemRestrictions] App unblocked: {appName} ({exeName})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] UnblockApplicationEnhanced error: {ex.Message}");
            }
        }

        // ==================== FULL SYSTEM RESTRICTION ====================

        public static void ApplyFullRestriction()
        {
            BlockCMD();
            BlockPowerShell();
            BlockRegedit();
            BlockCopyPaste();
            BlockSoftwareInstall();
            BlockDelete();
            BlockIPChange();
            BlockTaskManager();
            BlockControlPanel();
            WriteMarker("full_restriction.txt");
            Debug.WriteLine("[SystemRestrictions] Full system restriction applied (ALL)");
        }

        public static void RemoveFullRestriction()
        {
            UnblockCMD();
            UnblockPowerShell();
            UnblockRegedit();
            UnblockCopyPaste();
            UnblockSoftwareInstall();
            UnblockDelete();
            UnblockIPChange();
            UnblockTaskManager();
            UnblockControlPanel();
            DeleteMarker("full_restriction.txt");
            Debug.WriteLine("[SystemRestrictions] Full system restriction removed (ALL)");
        }

        public static bool IsFullRestrictionActive()
        {
            return MarkerExists("full_restriction.txt");
        }

        // ==================== STARTUP RE-APPLY ====================

        /// <summary>
        /// Called on application startup to re-apply active restrictions from database.
        /// Also checks marker files for locally-stored restrictions.
        /// </summary>
        public static void ReapplyActiveRestrictions(string companyName, string systemName)
        {
            try
            {
                Debug.WriteLine("[SystemRestrictions] Checking for active restrictions to re-apply...");

                // Re-apply from marker files (local state)
                if (MarkerExists("cmd_blocked.txt")) BlockCMD();
                if (MarkerExists("powershell_blocked.txt")) BlockPowerShell();
                if (MarkerExists("regedit_blocked.txt")) BlockRegedit();
                if (MarkerExists("clipboard_blocked.txt")) ResumeClipboardBlockingIfNeeded();
                if (MarkerExists("softwareinstall_blocked.txt")) BlockSoftwareInstall();
                if (MarkerExists("delete_blocked.txt")) BlockDelete();
                if (MarkerExists("ipchange_blocked.txt")) BlockIPChange();
                if (MarkerExists("taskmgr_blocked.txt")) BlockTaskManager();
                if (MarkerExists("controlpanel_blocked.txt")) BlockControlPanel();

                // Re-apply blocked applications from registry
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\EmployeeAttendance\BlockedApps"))
                    {
                        if (key != null)
                        {
                            foreach (var name in key.GetValueNames())
                            {
                                var val = key.GetValue(name)?.ToString();
                                if (!string.IsNullOrEmpty(val))
                                {
                                    var parts = val.Split('|');
                                    string appPath = parts.Length > 0 ? parts[0] : "";
                                    string exeName = parts.Length > 1 ? parts[1] : "";
                                    if (!string.IsNullOrEmpty(exeName))
                                    {
                                        // Re-apply IFEO
                                        using (var ifeoKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exeName}"))
                                        {
                                            ifeoKey?.SetValue("Debugger", "cmd.exe /c exit", RegistryValueKind.String);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                Debug.WriteLine("[SystemRestrictions] Active restrictions re-applied");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemRestrictions] ReapplyActiveRestrictions error: {ex.Message}");
            }
        }
    }
}
