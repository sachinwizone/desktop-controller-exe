using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EmployeeAttendance
{
    static class Program
    {
        private static Mutex? mutex;
        private const string APP_NAME = "EmployeeAttendance";
        
        [STAThread]
        static void Main(string[] args)
        {
            // Ensure only one instance runs
            const string mutexName = "EmployeeAttendance_SingleInstance";
            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);
            
            if (!createdNew)
            {
                // Already running - don't show message if started automatically
                if (args.Length == 0 || args[0] != "--silent")
                {
                    MessageBox.Show("Employee Attendance is already running!", "Already Running", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            
            // Enable auto-start on Windows login
            EnableAutoStart();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            // Check if already activated
            if (DatabaseHelper.IsActivated())
            {
                Application.Run(new MainDashboard());
            }
            else
            {
                Application.Run(new ActivationForm());
            }
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
    }
}
