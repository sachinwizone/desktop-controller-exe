using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace EmployeeAttendance
{
    static class Program
    {
        private static Mutex? mutex;
        private const string APP_NAME = "EmployeeAttendance";
        private static string? applicationExePath = null;
        
        [STAThread]
        static void Main(string[] args)
        {
            // Capture the executable path early
            applicationExePath = Application.ExecutablePath;
            if (string.IsNullOrEmpty(applicationExePath))
            {
                applicationExePath = System.AppContext.BaseDirectory.TrimEnd('\\') + "\\EmployeeAttendance.exe";
            }
            
            // Check for command line arguments
            bool isSilentMode = args.Length > 0 && (args[0] == "--silent" || args[0] == "-s");
            
            // Ensure only one instance runs
            const string mutexName = "EmployeeAttendance_SingleInstance";
            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);
            
            if (!createdNew)
            {
                // Already running - don't show message if started in silent mode
                if (!isSilentMode)
                {
                    MessageBox.Show("Employee Attendance is already running!", "Already Running", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            
            // Enable auto-start on Windows login (with silent mode)
            EnableAutoStart();
            
            // Start watchdog to prevent Task Manager kill
            StartProcessWatchdog();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            // Check if already activated
            if (DatabaseHelper.IsActivated())
            {
                var mainDashboard = new MainDashboard();
                
                // If silent mode, start minimized to tray
                if (isSilentMode)
                {
                    mainDashboard.WindowState = FormWindowState.Minimized;
                    mainDashboard.ShowInTaskbar = false;
                    // Log that app started in silent mode
                    LogToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application started in SILENT MODE");
                }
                
                Application.Run(mainDashboard);
            }
            else
            {
                Application.Run(new ActivationForm());
            }
        }
        
        /// <summary>
        /// Watchdog to protect application from Task Manager termination and auto-restart
        /// </summary>
        private static void StartProcessWatchdog()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                Process? currentProcess = null;
                int originalPID = 0;
                int restartAttempts = 0;
                const int MAX_RESTARTS = 5;
                
                try
                {
                    currentProcess = Process.GetCurrentProcess();
                    originalPID = currentProcess.Id;
                    
                    System.Diagnostics.Debug.WriteLine($"[Watchdog] Started - PID: {originalPID}, Exe: {applicationExePath}");
                    
                    while (restartAttempts < MAX_RESTARTS)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(100); // Check frequently
                            
                            // Check if main process still exists
                            bool processExists = false;
                            try
                            {
                                var proc = Process.GetProcessById(originalPID);
                                processExists = (proc != null && !proc.HasExited);
                            }
                            catch (ArgumentException)
                            {
                                // Process was killed
                                processExists = false;
                            }
                            
                            if (!processExists)
                            {
                                // Main process was terminated
                                System.Diagnostics.Debug.WriteLine($"[Watchdog] Main process terminated! Attempting restart #{restartAttempts + 1}...");
                                
                                if (!string.IsNullOrEmpty(applicationExePath) && File.Exists(applicationExePath))
                                {
                                    System.Threading.Thread.Sleep(2000); // Wait before restart
                                    
                                    try
                                    {
                                        var psi = new ProcessStartInfo
                                        {
                                            FileName = applicationExePath,
                                            UseShellExecute = true,
                                            CreateNoWindow = true
                                        };
                                        var newProc = System.Diagnostics.Process.Start(psi);
                                        
                                        if (newProc != null)
                                        {
                                            originalPID = newProc.Id;
                                            restartAttempts++;
                                            System.Diagnostics.Debug.WriteLine($"[Watchdog] Restarted successfully with PID: {originalPID}");
                                            
                                            // Continue monitoring the new process
                                            System.Threading.Thread.Sleep(2000);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[Watchdog] Restart failed: {ex.Message}");
                                        restartAttempts++;
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Watchdog] Cannot restart - exe not found");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Watchdog] Loop error: {ex.Message}");
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[Watchdog] Exiting - max restart attempts reached");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Watchdog] Fatal error: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Add application to Windows startup registry for current user
        /// </summary>
        public static void EnableAutoStart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        string exePath = Application.ExecutablePath;
                        key.SetValue(APP_NAME, $"\"{exePath}\" --silent");
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail - user may not have permissions
                System.Diagnostics.Debug.WriteLine($"Failed to set auto-start: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Remove application from Windows startup registry
        /// </summary>
        public static void DisableAutoStart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null && key.GetValue(APP_NAME) != null)
                    {
                        key.DeleteValue(APP_NAME, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to remove auto-start: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log messages to file for silent mode tracking
        /// </summary>
        private static void LogToFile(string message)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "EmployeeAttendance", "Logs");
                Directory.CreateDirectory(logDir);
                
                string logFile = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(logFile, message + Environment.NewLine);
            }
            catch 
            {
                // Silent fail - logging shouldn't crash the app
            }
        }
    }
}
