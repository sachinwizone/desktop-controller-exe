using System.Diagnostics;

namespace DesktopController;

/// <summary>
/// Stores user details collected after login
/// </summary>
public class UserSessionDetails
{
    public string SystemUserName { get; set; } = "";
    public string Department { get; set; } = "";
    public string OfficeLocation { get; set; } = "";
    
    private static UserSessionDetails? _instance;
    public static UserSessionDetails Instance => _instance ??= new UserSessionDetails();
    
    public static void Set(string systemUserName, string department, string officeLocation)
    {
        Instance.SystemUserName = systemUserName;
        Instance.Department = department;
        Instance.OfficeLocation = officeLocation;
    }
}

static class Program
{
    private static System.Threading.Mutex? mutex = null;
    
    [STAThread]
    static void Main(string[] args)
    {
        // Create a named mutex to ensure only one instance runs
        const string mutexName = "DesktopControllerProSingleInstance";
        bool createdNew;
        
        mutex = new System.Threading.Mutex(true, mutexName, out createdNew);
        
        if (!createdNew)
        {
            // Another instance is already running - just exit silently for auto-start
            if (args.Any(arg => arg.Equals("--autostart", StringComparison.OrdinalIgnoreCase)))
            {
                return; // Silent exit for auto-start when already running
            }
            
            MessageBox.Show(
                "Desktop Controller is already running!\n\nCheck the system tray icon.",
                "Already Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }
        
        try
        {
            ApplicationConfiguration.Initialize();
            
            // Check if this is auto-start mode
            bool isAutoStart = args.Any(arg => arg.Equals("--autostart", StringComparison.OrdinalIgnoreCase));
            
            if (isAutoStart)
            {
                // Try auto-login with saved credentials
                var credentials = DatabaseHelper.GetAutoLoginCredentials();
                if (credentials.HasValue)
                {
                    try
                    {
                        // Verify activation key first
                        var activationInfo = DatabaseHelper.ValidateActivationKey(credentials.Value.activationKey);
                        if (activationInfo != null && activationInfo.IsValid)
                        {
                            // Try to login with saved credentials
                            UserInfo? userInfo = DatabaseHelper.ValidateAndGetUser(
                                credentials.Value.username, 
                                credentials.Value.password, 
                                activationInfo.CompanyName);
                            
                            if (userInfo != null)
                            {
                                // Load saved user details for auto-start
                                LoadSavedUserDetails();
                                
                                // Auto-login successful, start in background
                                var mainForm = new MainForm(userInfo);
                                mainForm.WindowState = FormWindowState.Minimized;
                                mainForm.ShowInTaskbar = false;
                                Application.Run(mainForm);
                                return;
                            }
                        }
                    }
                    catch { }
                }
                
                // Auto-login failed, show login form
                ShowLoginAndRun();
            }
            else
            {
                // Normal startup - show login form
                ShowLoginAndRun();
            }
        }
        finally
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }
    }
    
    private static void LoadSavedUserDetails()
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
            {
                if (key != null)
                {
                    string systemUserName = key.GetValue("UserDetailsName")?.ToString() ?? Environment.UserName;
                    string department = key.GetValue("UserDetailsDepartment")?.ToString() ?? "";
                    string officeLocation = key.GetValue("UserDetailsLocation")?.ToString() ?? "";
                    
                    UserSessionDetails.Set(systemUserName, department, officeLocation);
                }
            }
        }
        catch { }
    }
    
    private static void ShowLoginAndRun()
    {
        using (LoginForm loginForm = new LoginForm())
        {
            if (loginForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Get activation key for user details form
                string activationKey = "";
                string companyName = loginForm.LoggedInUser?.CompanyName ?? "";
                
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                    {
                        activationKey = key?.GetValue("ActivationKey")?.ToString() ?? "";
                    }
                }
                catch { }
                
                // Show user details form after login
                using (UserDetailsForm detailsForm = new UserDetailsForm(loginForm.LoggedInUser, companyName, activationKey))
                {
                    if (detailsForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Save user details to session
                        UserSessionDetails.Set(
                            detailsForm.SystemUserName, 
                            detailsForm.Department, 
                            detailsForm.OfficeLocation);
                        
                        // Start main form with user info
                        Application.Run(new MainForm(loginForm.LoggedInUser));
                    }
                    else
                    {
                        // User cancelled, exit
                        Application.Exit();
                    }
                }
            }
            else
            {
                // Login cancelled or failed, exit application
                Application.Exit();
            }
        }
    }
}
