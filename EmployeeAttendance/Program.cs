using System;
using System.Windows.Forms;

namespace EmployeeAttendance
{
    static class Program
    {
        private static Mutex? mutex;
        
        [STAThread]
        static void Main()
        {
            // Ensure only one instance runs
            const string mutexName = "EmployeeAttendance_SingleInstance";
            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show("Employee Attendance is already running!", "Already Running", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
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
    }
}
