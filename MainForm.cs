using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using System.Threading.Tasks;

namespace DesktopController
{
    public partial class MainForm : Form
    {
        private TabControl tabControl = null!;
        private Panel headerPanel = null!;
        private Label titleLabel = null!;
        private Label statusLabel = null!;
        private Label systemIdLabel = null!;  // Display System ID like AnyDesk
        private Button connectButton = null!;  // Connect to partner system
        private Button viewSystemsButton = null!;  // View all registered systems
        private Label userNameLabel = null!;
        private Label companyNameLabel = null!;
        private Label userDetailsLabel = null!;  // Shows System User, Department, Office Location
        private Button logoutButton = null!;
        private Button punchButton = null!;
        private Label punchStatusLabel = null!;
        
        // System Tray components
        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        private bool isExitAllowed = false;  // Password protection for exit
        private bool minimizeToTray = true;
        
        // Store logged in user
        private UserInfo? currentUser;
        
        // Store activation details for audit logging
        private string? storedActivationKey = null;
        private string? storedCompanyName = null;
        
        // System registration for remote viewing
        private string systemId = "";
        private System.Windows.Forms.Timer heartbeatTimer = null!; // System heartbeat for online status
        private System.Windows.Forms.Timer liveStreamTimer = null!; // Live stream frame sender
        
        // Punch In/Out tracking
        private bool isPunchedIn = false;
        private DateTime? punchInTime = null;
        
        // Break tracking
        private bool isOnBreak = false;
        private DateTime? breakStartTime = null;
        private Button? breakButton = null;
        private ToolStripMenuItem? trayBreakItem = null;
        private ToolStripMenuItem? trayPunchItem = null;
        
        // Hidden tabs - password protected (all except AUDIT and SYSTEM INFO)
        private TabPage? hiddenControlTab = null;
        private TabPage? hiddenWindowsTab = null;
        private TabPage? hiddenPrinterTab = null;
        private TabPage? hiddenSecurityTab = null;
        private TabPage? hiddenNetworkTab = null;
        private TabPage? hiddenAdvancedTab = null;
        private TabPage? hiddenLogsTab = null;
        private bool advancedFeaturesUnlocked = false;
        private const string ADVANCED_PASSWORD = "Master_advance@#$123";
        private System.Windows.Forms.Timer? autoLockTimer = null;  // 5-minute auto-lock timer
        private Button? unlockAdvancedButtonRef = null;  // Store reference to unlock button
        
        // Sync control - user must click "Start Syncing" to enable database sync
        private bool isSyncingEnabled = false;
        private Button? startSyncButton = null;
        private Label? syncStatusLabel = null;
        
        // Activity tracking
        private System.Windows.Forms.Timer activityTimer = null!;
        private System.Windows.Forms.Timer inactivityTimer = null!;
        private System.Windows.Forms.Timer syncTimer = null!;  // Auto-sync timer for database
        private System.Windows.Forms.Timer browserScanTimer = null!; // Timer for browser scan
        private System.Windows.Forms.Timer appUsageTimer = null!; // Timer for app usage refresh
        private System.Windows.Forms.Timer uiRefreshTimer = null!; // Timer for UI auto-refresh
        private System.Windows.Forms.Timer processScanner = null!; // Timer to scan all running processes
        private System.Windows.Forms.Timer screenshotTimer = null!; // Timer for auto screenshot capture
        private ScreenshotSettings screenshotSettings = null!; // Screenshot configuration
        private DateTime lastActivityTime = DateTime.Now;
        private Point lastMousePosition = Point.Empty;
        private List<ActivityLog> activityLogs = new List<ActivityLog>();
        private List<WebsiteLog> websiteLogs = new List<WebsiteLog>();
        private List<AppUsageLog> appUsageLogs = new List<AppUsageLog>();
        private List<InactivityLog> inactivityLogs = new List<InactivityLog>();
        private Dictionary<string, DateTime> activeApps = new Dictionary<string, DateTime>();
        
        // Activity tab ListViews for auto-refresh
        private ListView? websitesListView = null;
        private ListView? applicationsListView = null;
        private ListView? inactivityListView = null;
        private bool isCurrentlyInactive = false;
        private DateTime? inactivityStartTime = null;
        private static bool isCapturingScreenshot = false;  // Prevent duplicate screenshots
        
        // Overview tab labels for auto-refresh
        private Label? overviewPunchLabel = null;
        private Label? overviewActivityLabel = null;
        private Label? overviewInactivityLabel = null;
        
        // Tailwind-Inspired Professional Color Palette (Enhanced Contrast)
        private Color primaryColor = Color.FromArgb(30, 41, 59);      // slate-800 (header)
        private Color accentColor = Color.FromArgb(99, 102, 241);     // indigo-500 (interactive)
        private Color successColor = Color.FromArgb(34, 197, 94);     // green-500
        private Color dangerColor = Color.FromArgb(239, 68, 68);      // red-500
        private Color warningColor = Color.FromArgb(251, 146, 60);    // orange-400
        private Color bgColor = Color.FromArgb(15, 23, 42);           // slate-900 (main background)
        private Color cardColor = Color.FromArgb(51, 65, 85);         // slate-700 (content cards) - LIGHTER
        private Color cardHoverColor = Color.FromArgb(71, 85, 105);   // slate-600 (hover state)
        private Color contentBgColor = Color.FromArgb(30, 41, 59);    // slate-800 (tab pages)
        private Color textColor = Color.FromArgb(248, 250, 252);      // slate-50
        private Color secondaryTextColor = Color.FromArgb(148, 163, 184); // slate-400
        private Color borderColor = Color.FromArgb(71, 85, 105);      // slate-600 (visible borders)

        // Button colors - Vibrant & Modern
        private Color blockColor = Color.FromArgb(220, 38, 38);       // red-600
        private Color blockHoverColor = Color.FromArgb(239, 68, 68);  // red-500
        private Color allowColor = Color.FromArgb(22, 163, 74);       // green-600
        private Color allowHoverColor = Color.FromArgb(34, 197, 94);  // green-500
        private Color inactiveColor = Color.FromArgb(71, 85, 105);    // slate-600

        public MainForm(UserInfo? user = null)
        {
            currentUser = user;
            InitializeComponent();
            CheckAdminRights();
        }

        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to logout?\n\nThis will clear saved login credentials.", 
                "Confirm Logout", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                // Clear auto-login credentials so user must login manually next time
                DatabaseHelper.ClearAutoLoginCredentials();
                
                // Stop all tracking and timers
                StopActivityTracking();
                
                // Reset punch status
                if (isPunchedIn)
                {
                    isPunchedIn = false;
                    punchButton.Text = "⏺ PUNCH IN";
                    punchButton.BackColor = successColor;
                    punchStatusLabel.Text = "";
                }
                
                // Clear stored data
                storedActivationKey = null;
                storedCompanyName = null;
                
                // Clear activity data
                websiteLogs.Clear();
                appUsageLogs.Clear();
                inactivityLogs.Clear();
                activeApps.Clear();
                
                // Reset inactivity state
                isCurrentlyInactive = false;
                inactivityStartTime = null;
                lastActivityTime = DateTime.Now;
                
                LogToFile("Logout: Cleared all activity data");
                
                this.Hide();
                
                // Show login form again
                using (LoginForm loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        // Update current user
                        currentUser = loginForm.LoggedInUser;
                        userNameLabel.Text = currentUser != null ? $"👤 {currentUser.Name}" : "👤 User";
                        companyNameLabel.Text = currentUser != null && !string.IsNullOrEmpty(currentUser.CompanyName) 
                            ? $"🏢 {currentUser.CompanyName}" : "";
                        
                        // IMPORTANT: Reload activation details for new company
                        LoadActivationDetails();
                        
                        // IMPORTANT: Clear password cache for new company and check password setup
                        PasswordManager.ClearCache();
                        
                        // Check if new company has password set, if not prompt setup
                        CheckCompanyPasswordSetup();
                        
                        // Re-register system with new company
                        Task.Run(() => {
                            try {
                                string newSystemId = RemoteViewingHelper.RegisterSystem(storedActivationKey, storedCompanyName, currentUser?.Name);
                                LogToFile($"Logout->Login: Re-registered system with new company: {storedCompanyName}, SystemID: {newSystemId}");
                                
                                // Update System ID label on UI thread
                                this.Invoke(new Action(() => {
                                    if (systemIdLabel != null) {
                                        systemIdLabel.Text = $"🆔 ID: {newSystemId}";
                                    }
                                }));
                            } catch (Exception ex) {
                                LogToFile($"Re-register error: {ex.Message}");
                            }
                        });
                        
                        // Refresh UI
                        RefreshAllData();
                        
                        this.Show();
                        LogToFile($"User re-logged in: {currentUser?.Name}, Company: {storedCompanyName}");
                    }
                    else
                    {
                        // User cancelled login, close application
                        Application.Exit();
                    }
                }
            }
        }

        private void RefreshAllData()
        {
            try
            {
                // Clear all list views
                if (websitesListView != null && !websitesListView.IsDisposed)
                    websitesListView.Items.Clear();
                if (applicationsListView != null && !applicationsListView.IsDisposed)
                    applicationsListView.Items.Clear();
                if (inactivityListView != null && !inactivityListView.IsDisposed)
                    inactivityListView.Items.Clear();
                
                LogToFile("All data refreshed after re-login");
            }
            catch (Exception ex)
            {
                LogToFile($"RefreshAllData error: {ex.Message}");
            }
        }

        private void CheckAdminRights()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            
            if (!isAdmin)
            {
                MessageBox.Show("This application requires Administrator privileges!\nPlease run as Administrator.", 
                    "Admin Rights Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                statusLabel.Text = "✓ Running as Administrator";
                statusLabel.ForeColor = successColor;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Load activation details for audit logging
            LoadActivationDetails();
            
            // Ensure audit_logs table exists in database
            Task.Run(() => DatabaseHelper.EnsureAuditLogsTableExists());
            
            // Form settings
            this.Text = "Desktop Controller Pro - System Management Tool";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.Font = new Font("Segoe UI", 10);
            
            // Header Panel with modern gradient effect - Tailwind Inspired
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = primaryColor,  // slate-800
                Padding = new Padding(30, 18, 30, 18)
            };
            
            // Add modern bottom border with subtle shadow
            headerPanel.Paint += (s, e) =>
            {
                // Bottom border
                using (var pen = new Pen(borderColor, 2))
                {
                    e.Graphics.DrawLine(pen, 0, headerPanel.Height - 2, headerPanel.Width, headerPanel.Height - 2);
                }
                // Top accent gradient
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, headerPanel.Width, 4),
                    accentColor,  // indigo-500
                    Color.FromArgb(139, 92, 246),  // violet-500
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, headerPanel.Width, 4);
                }
            };

            titleLabel = new Label
            {
                Text = "⬢ DESKTOP CONTROLLER PRO",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = textColor,  // slate-50
                AutoSize = true,
                Location = new Point(30, 12),
                BackColor = Color.Transparent
            };

            statusLabel = new Label
            {
                Text = "⏳ Checking admin rights...",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = warningColor,  // orange-400
                AutoSize = true,
                Location = new Point(30, 42),
                BackColor = Color.Transparent
            };

            // User info and logout button on the right side - Modern Style
            logoutButton = new Button
            {
                Text = "⏻ LOGOUT",
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = dangerColor,  // red-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(115, 42),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);  // red-600
            logoutButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(185, 28, 28);  // red-700
            logoutButton.Click += LogoutButton_Click;
            
            // Company name label - Modern Style
            companyNameLabel = new Label
            {
                Text = currentUser != null && !string.IsNullOrEmpty(currentUser.CompanyName) 
                    ? $"🏢 {currentUser.CompanyName}" : "",
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(74, 222, 128),  // green-400
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            
            userNameLabel = new Label
            {
                Text = currentUser != null ? $"👤 {currentUser.Name}" : "👤 User",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(129, 140, 248),  // indigo-400
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            
            // User Details Label - Shows System User, Department, Office Location
            var sessionDetails = UserSessionDetails.Instance;
            string detailsText = "";
            if (!string.IsNullOrEmpty(sessionDetails.SystemUserName) || 
                !string.IsNullOrEmpty(sessionDetails.Department) || 
                !string.IsNullOrEmpty(sessionDetails.OfficeLocation))
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(sessionDetails.SystemUserName))
                    parts.Add($"👤 {sessionDetails.SystemUserName}");
                if (!string.IsNullOrEmpty(sessionDetails.Department))
                    parts.Add($"🏛️ {sessionDetails.Department}");
                if (!string.IsNullOrEmpty(sessionDetails.OfficeLocation))
                    parts.Add($"📍 {sessionDetails.OfficeLocation}");
                detailsText = string.Join("  |  ", parts);
            }
            
            userDetailsLabel = new Label
            {
                Text = detailsText,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),  // slate-400
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(30, 60),
                BackColor = Color.Transparent,
                Visible = !string.IsNullOrEmpty(detailsText)
            };
            
            // System ID Label (like AnyDesk/UltraViewer ID) - Modern with Badge Style
            systemIdLabel = new Label
            {
                Text = "🔢 ID: Loading...",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(251, 191, 36),  // amber-400 for high visibility
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 4, 8, 4)
            };
            systemIdLabel.Click += (s, e) => {
                // Copy ID to clipboard on click
                if (!string.IsNullOrEmpty(systemId) && !systemId.Contains("Loading"))
                {
                    try
                    {
                        Clipboard.SetText(systemId);
                        MessageBox.Show($"System ID copied: {systemId}", "ID Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            };
            
            // Connect to Partner Button (like AnyDesk connection) - Modern Tailwind Style
            connectButton = new Button
            {
                Text = "🔗 CONNECT",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentColor,  // indigo-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 36),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            connectButton.FlatAppearance.BorderSize = 0;
            connectButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(79, 70, 229);  // indigo-600
            connectButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(67, 56, 202);  // indigo-700
            connectButton.Click += ConnectButton_Click;
            
            // View All Systems Button - Modern Tailwind Style
            viewSystemsButton = new Button
            {
                Text = "📋 SYSTEMS",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(59, 130, 246),  // blue-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 36),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            viewSystemsButton.FlatAppearance.BorderSize = 0;
            viewSystemsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 99, 235);  // blue-600
            viewSystemsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);  // blue-700
            viewSystemsButton.Click += ViewSystemsButton_Click;
            
            // Punch In/Out Button - Modern Tailwind Style
            punchButton = new Button
            {
                Text = "⏱ PUNCH IN",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = successColor,  // green-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 42),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            punchButton.FlatAppearance.BorderSize = 0;
            punchButton.FlatAppearance.MouseOverBackColor = allowHoverColor;  // green-500
            punchButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 163, 74);  // green-600
            punchButton.Click += PunchButton_Click;
            
            // Punch Status Label
            punchStatusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = secondaryTextColor,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            
            // Start Syncing Button - User must click to enable database sync
            startSyncButton = new Button
            {
                Text = "▶ START SYNC",
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(249, 115, 22),  // orange-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 28),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            startSyncButton.FlatAppearance.BorderSize = 0;
            startSyncButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(234, 88, 12);  // orange-600
            startSyncButton.Click += StartSyncButton_Click;
            startSyncButton.Location = new Point(30, 82);
            
            // Sync Status Label
            syncStatusLabel = new Label
            {
                Text = "⏸ Sync Paused",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(251, 146, 60),  // orange-400
                AutoSize = true,
                Location = new Point(145, 87),
                BackColor = Color.Transparent
            };
            
            // Take Break Button - Only visible after Punch In
            breakButton = new Button
            {
                Text = "☕ TAKE BREAK",
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(234, 179, 8),  // yellow-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 36),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false,  // Hidden until Punch In
                Enabled = true,
                Location = new Point(300, 18)  // Default position
            };
            breakButton.FlatAppearance.BorderSize = 0;
            breakButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(202, 138, 4);  // yellow-600
            breakButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(161, 98, 7);  // yellow-700
            breakButton.Click += BreakButton_Click;
            
            // Unlock Advanced Features Button
            Button unlockAdvancedButton = new Button
            {
                Text = "🔓 UNLOCK TABS",
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(139, 92, 246),  // violet-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 28),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(260, 82)
            };
            unlockAdvancedButton.FlatAppearance.BorderSize = 0;
            unlockAdvancedButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(124, 58, 237);  // violet-600
            unlockAdvancedButton.Click += UnlockAdvancedButton_Click;
            unlockAdvancedButtonRef = unlockAdvancedButton;  // Store reference for auto-lock

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(statusLabel);
            headerPanel.Controls.Add(userDetailsLabel);
            headerPanel.Controls.Add(systemIdLabel);
            headerPanel.Controls.Add(connectButton);
            headerPanel.Controls.Add(viewSystemsButton);
            headerPanel.Controls.Add(companyNameLabel);
            headerPanel.Controls.Add(userNameLabel);
            headerPanel.Controls.Add(punchButton);
            headerPanel.Controls.Add(punchStatusLabel);
            headerPanel.Controls.Add(breakButton);
            headerPanel.Controls.Add(startSyncButton);
            headerPanel.Controls.Add(syncStatusLabel);
            headerPanel.Controls.Add(unlockAdvancedButton);
            headerPanel.Controls.Add(logoutButton);
            
            // Position user controls on the right - will be adjusted on resize
            headerPanel.Resize += (s, e) => {
                // Top row: Company, User, Break, Punch, Logout
                logoutButton.Location = new Point(headerPanel.Width - logoutButton.Width - 15, 15);
                punchButton.Location = new Point(headerPanel.Width - logoutButton.Width - punchButton.Width - 25, 15);
                if (breakButton != null)
                {
                    breakButton.Location = new Point(headerPanel.Width - logoutButton.Width - punchButton.Width - breakButton.Width - 35, 18);
                }
                punchStatusLabel.Location = new Point(headerPanel.Width - logoutButton.Width - punchButton.Width - 25, 58);
                userNameLabel.Location = new Point(headerPanel.Width - logoutButton.Width - punchButton.Width - (breakButton?.Visible == true ? breakButton.Width + 10 : 0) - userNameLabel.Width - 45, 25);
                companyNameLabel.Location = new Point(headerPanel.Width - logoutButton.Width - punchButton.Width - (breakButton?.Visible == true ? breakButton.Width + 10 : 0) - companyNameLabel.Width - 45, 8);
                
                // Bottom row: System ID, Connect, Systems
                viewSystemsButton.Location = new Point(headerPanel.Width - viewSystemsButton.Width - 15, 80);
                connectButton.Location = new Point(headerPanel.Width - viewSystemsButton.Width - connectButton.Width - 25, 80);
                systemIdLabel.Location = new Point(headerPanel.Width - viewSystemsButton.Width - connectButton.Width - systemIdLabel.Width - 35, 84);
            };

            // Tab Control - Modern styled with better contrast
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10),
                Padding = new Point(25, 8),
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(130, 36),
                Appearance = TabAppearance.Normal
            };
            
            // Add margin padding for visual breathing room
            tabControl.Margin = new Padding(10);
            
            // Custom draw tabs for modern look
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += (s, e) =>
            {
                bool isSelected = e.Index == tabControl.SelectedIndex;
                Color tabBgColor = isSelected ? accentColor : primaryColor;
                Color tabTextColor = isSelected ? Color.White : secondaryTextColor;
                
                using (var brush = new SolidBrush(tabBgColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
                
                // Draw bottom line for selected tab
                if (isSelected)
                {
                    using (var pen = new Pen(accentColor, 3))
                    {
                        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 2, e.Bounds.Right, e.Bounds.Bottom - 2);
                    }
                }
                
                // Draw text
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                using (var brush = new SolidBrush(tabTextColor))
                {
                    e.Graphics.DrawString(tabControl.TabPages[e.Index].Text, tabControl.Font, brush, e.Bounds, sf);
                }
            };

            // Create tabs
            TabPage controlTab = CreateControlTab();
            TabPage windowsTab = CreateWindowsTab();
            TabPage printerTab = CreatePrinterTab();
            TabPage securityTab = CreateSecurityTab();
            TabPage networkTab = CreateNetworkTab();
            TabPage advancedTab = CreateAdvancedTab();
            
            TabPage logsTab = null!;
            TabPage systemInfoTab = null!;
            
            try
            {
                logsTab = CreateLogsTab();
            }
            catch (Exception ex)
            {
                logsTab = new TabPage("LOGS (Error)") { BackColor = bgColor };
                Label errLabel = new Label { Text = $"Error: {ex.Message}", Dock = DockStyle.Fill, ForeColor = Color.Red };
                logsTab.Controls.Add(errLabel);
            }
            
            // Create System Info tab with lazy loading to prevent startup freeze
            try
            {
                systemInfoTab = CreateSystemInfoTabLazy();
            }
            catch (Exception ex)
            {
                systemInfoTab = new TabPage("SYSTEM INFO (Error)") { BackColor = bgColor };
                Label errLabel = new Label { Text = $"Error: {ex.Message}", Dock = DockStyle.Fill, ForeColor = Color.Red };
                systemInfoTab.Controls.Add(errLabel);
            }
            
            // Create Audit Logs Tab
            TabPage auditTab = null!;
            try
            {
                auditTab = CreateAuditLogsTab();
            }
            catch (Exception ex)
            {
                auditTab = new TabPage("📊 AUDIT (Error)") { BackColor = bgColor };
                Label errLabel = new Label { Text = $"Error: {ex.Message}", Dock = DockStyle.Fill, ForeColor = Color.Red };
                auditTab.Controls.Add(errLabel);
            }

            // Store all tabs that need to be hidden (password protected)
            hiddenControlTab = controlTab;
            hiddenWindowsTab = windowsTab;
            hiddenPrinterTab = printerTab;
            hiddenSecurityTab = securityTab;
            hiddenNetworkTab = networkTab;
            hiddenAdvancedTab = advancedTab;
            hiddenLogsTab = logsTab;
            
            // Only show AUDIT and SYSTEM INFO by default
            tabControl.TabPages.Add(auditTab);
            tabControl.TabPages.Add(systemInfoTab);

            this.Controls.Add(tabControl);
            this.Controls.Add(headerPanel);
            
            // Initialize activity tracking
            InitializeActivityTracking();
            
            // Generate and display System ID immediately
            GenerateAndDisplaySystemId();
            
            // Ensure all audit tables exist in database (run in background)
            _ = Task.Run(() => {
                try {
                    DatabaseHelper.EnsureAllAuditTablesExist();
                    // Ensure user details columns exist in log tables
                    DatabaseHelper.EnsureUserDetailsColumnsExist();
                } catch { }
            });
            
            // Auto-start activity tracking immediately (for auto-logging)
            StartActivityTracking();
            
            // Check and setup company password protection
            CheckCompanyPasswordSetup();
            
            // Initialize System Tray Icon
            InitializeSystemTray();
            
            // Setup auto-start on Windows startup
            SetupAutoStart();
            
            // Handle form closing (minimize to tray instead of close)
            this.FormClosing += MainForm_FormClosing;
            
            // Handle minimize to tray
            this.Resize += MainForm_Resize;

            this.ResumeLayout(false);
        }
        
        // Modern Button Styling Helper - Tailwind-Inspired
        private void StyleModernButton(Button btn, Color bgColor, Color hoverColor, int cornerRadius = 8)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bgColor;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.Cursor = Cursors.Hand;
            
            // Add subtle shadow effect via padding simulation
            btn.Padding = new Padding(12, 6, 12, 6);
            
            // Smooth hover transition feel
            btn.MouseEnter += (s, e) => {
                btn.BackColor = hoverColor;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                    Math.Max(0, hoverColor.R - 20),
                    Math.Max(0, hoverColor.G - 20),
                    Math.Max(0, hoverColor.B - 20)
                );
            };
            btn.MouseLeave += (s, e) => {
                btn.BackColor = bgColor;
            };
        }
        
        // Modern Card Panel Styling - Tailwind-Inspired
        private Panel CreateModernCard(int width, int height)
        {
            var panel = new Panel
            {
                Size = new Size(width, height),
                BackColor = cardColor,
                Padding = new Padding(20)
            };
            
            // Add subtle border
            panel.Paint += (s, e) => {
                using (var pen = new Pen(borderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
            
            return panel;
        }
        
        /// <summary>
        /// Check if company password is set, if not show setup dialog (first time)
        /// </summary>
        private void CheckCompanyPasswordSetup()
        {
            try
            {
                if (string.IsNullOrEmpty(storedCompanyName))
                {
                    LogToFile("No company name found - skipping password check");
                    return;
                }
                
                // Check if password is already set for this company
                if (!PasswordManager.IsPasswordSet(storedCompanyName))
                {
                    LogToFile($"First time setup - prompting password for company: {storedCompanyName}");
                    
                    // Show password setup dialog - user must set a password
                    if (!PasswordManager.ShowPasswordSetup(storedCompanyName))
                    {
                        // User must set password - show error and exit
                        MessageBox.Show(
                            "A company password must be set to use this application.\n" +
                            "Please restart and set a password.",
                            "Password Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    LogToFile($"Company password already set for: {storedCompanyName}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error checking company password: {ex.Message}");
            }
        }
        
        private void LoadActivationDetails()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        storedActivationKey = key.GetValue("ActivationKey")?.ToString();
                        storedCompanyName = key.GetValue("CompanyName")?.ToString();
                    }
                }
            }
            catch { }
        }
        
        private void StartSyncButton_Click(object? sender, EventArgs e)
        {
            // Require password to control sync - ALWAYS ask, never use cache
            if (!PasswordManager.PromptForPasswordAlways(storedCompanyName, isSyncingEnabled ? "stop syncing" : "start syncing"))
            {
                return; // Password not verified
            }
            
            if (!isSyncingEnabled)
            {
                // Start Syncing - All data + Screenshots
                isSyncingEnabled = true;
                startSyncButton!.Text = "⏹ STOP SYNC";
                startSyncButton.BackColor = Color.FromArgb(34, 197, 94);  // green-500
                startSyncButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 163, 74);  // green-600
                syncStatusLabel!.Text = "🔄 Syncing Active";
                syncStatusLabel.ForeColor = Color.FromArgb(34, 197, 94);  // green-500
                
                // Start sync timer for database
                syncTimer.Start();
                browserScanTimer.Start();
                
                // Start screenshot timer - reload settings and start
                screenshotSettings = ScreenshotSettings.Load();
                if (screenshotSettings.StorageType != "none")
                {
                    screenshotTimer.Interval = screenshotSettings.GetIntervalMilliseconds();
                    screenshotTimer.Start();
                    LogToFile($"Screenshot timer started - Interval: {screenshotSettings.IntervalValue} {screenshotSettings.IntervalUnit}, Storage: {screenshotSettings.StorageType}");
                }
                
                // Do immediate sync
                _ = Task.Run(() => {
                    try {
                        SyncAllDataToDatabase();
                    } catch (Exception ex) {
                        LogToFile($"Sync error: {ex.Message}");
                    }
                });
                
                LogToFile("=== FULL SYNC ENABLED by user (DB + Screenshots) ===");
                statusLabel.Text = "✓ Syncing started (DB + Screenshots)";
                statusLabel.ForeColor = successColor;
            }
            else
            {
                // Stop Syncing - All data + Screenshots
                isSyncingEnabled = false;
                startSyncButton!.Text = "▶ START SYNC";
                startSyncButton.BackColor = Color.FromArgb(249, 115, 22);  // orange-500
                startSyncButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(234, 88, 12);  // orange-600
                syncStatusLabel!.Text = "⏸ Sync Paused";
                syncStatusLabel.ForeColor = Color.FromArgb(251, 146, 60);  // orange-400
                
                // Stop all sync timers
                syncTimer.Stop();
                browserScanTimer.Stop();
                screenshotTimer.Stop();
                
                LogToFile("=== FULL SYNC PAUSED by user (DB + Screenshots) ===");
                statusLabel.Text = "Sync paused";
                statusLabel.ForeColor = warningColor;
            }
        }
        
        /// <summary>
        /// Sync all activity data to database
        /// </summary>
        private void SyncAllDataToDatabase()
        {
            if (!isSyncingEnabled) return;
            
            LogToFile("Starting full database sync...");
            
            try
            {
                // Sync browser history/websites
                SyncBrowserHistoryToDatabase();
                
                // Sync GPS location
                AutoCaptureGPSLocation();
                
                // Sync app usage
                SyncAppUsageToDatabase();
                
                // Sync inactivity logs
                SyncInactivityToDatabase();
                
                LogToFile("Full database sync completed");
            }
            catch (Exception ex)
            {
                LogToFile($"Database sync error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sync app usage logs to database
        /// </summary>
        private void SyncAppUsageToDatabase()
        {
            if (!isSyncingEnabled) return;
            
            try
            {
                foreach (var app in appUsageLogs.ToList())
                {
                    if (!app.IsSynced)
                    {
                        bool success = DatabaseHelper.InsertAppLog(
                            storedActivationKey, storedCompanyName, currentUser?.Name,
                            app.AppName, app.WindowTitle, app.StartTime, app.EndTime);
                        
                        if (success)
                        {
                            app.IsSynced = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"App usage sync error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sync inactivity logs to database
        /// </summary>
        private void SyncInactivityToDatabase()
        {
            if (!isSyncingEnabled) return;
            
            try
            {
                foreach (var log in inactivityLogs.ToList())
                {
                    if (!log.IsSynced)
                    {
                        bool success = DatabaseHelper.InsertInactivityLog(
                            storedActivationKey, storedCompanyName, currentUser?.Name,
                            log.StartTime, log.EndTime, "INACTIVE");
                        
                        if (success)
                        {
                            log.IsSynced = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Inactivity sync error: {ex.Message}");
            }
        }
        
        private void PunchButton_Click(object? sender, EventArgs e)
        {
            if (!isPunchedIn)
            {
                // Punch In
                isPunchedIn = true;
                punchInTime = DateTime.Now;
                punchButton.Text = "⏹ PUNCH OUT";
                punchButton.BackColor = dangerColor;
                punchButton.FlatAppearance.MouseOverBackColor = blockHoverColor;
                punchStatusLabel.Text = $"In: {punchInTime:HH:mm}";
                punchStatusLabel.ForeColor = successColor;
                
                // Show break button
                if (breakButton != null)
                {
                    breakButton.Visible = true;
                    breakButton.Enabled = true;
                    breakButton.BringToFront();
                }
                // Update tray menu
                UpdateTrayPunchBreakItems();
                
                // Start activity tracking
                StartActivityTracking();
                
                // Log punch in
                activityLogs.Add(new ActivityLog
                {
                    Timestamp = DateTime.Now,
                    Type = "PUNCH IN",
                    Description = $"User punched in at {DateTime.Now:HH:mm:ss}",
                    Duration = ""
                });
                
                // Log to unified punch_log_consolidated table with error handling
                Task.Run(() => {
                    try
                    {
                        bool success = DatabaseHelper.StartPunchSession(storedActivationKey, storedCompanyName, 
                            currentUser?.Name);
                        if (!success)
                            LogToFile("Failed to start punch session in database");
                        else
                            LogToFile("Punch session started in database successfully");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error starting punch session: {ex.Message}");
                    }
                });
                
                statusLabel.Text = $"✓ Punched In at {punchInTime:HH:mm:ss}";
                statusLabel.ForeColor = successColor;
            }
            else
            {
                // End any active break first
                if (isOnBreak)
                {
                    EndBreak(false); // End break without showing message
                }
                
                // Punch Out
                isPunchedIn = false;
                DateTime punchOutTime = DateTime.Now;
                TimeSpan duration = punchOutTime - (punchInTime ?? punchOutTime);
                
                punchButton.Text = "⏱ PUNCH IN";
                punchButton.BackColor = successColor;
                punchButton.FlatAppearance.MouseOverBackColor = allowHoverColor;
                punchStatusLabel.Text = $"Last: {duration:hh\\:mm\\:ss}";
                punchStatusLabel.ForeColor = accentColor;
                
                // Hide break button
                if (breakButton != null)
                {
                    breakButton.Visible = false;
                }
                // Update tray menu
                UpdateTrayPunchBreakItems();
                
                // Stop activity tracking
                StopActivityTracking();
                
                // Log punch out
                activityLogs.Add(new ActivityLog
                {
                    Timestamp = DateTime.Now,
                    Type = "PUNCH OUT",
                    Description = $"User punched out at {punchOutTime:HH:mm:ss}",
                    Duration = duration.ToString(@"hh\:mm\:ss")
                });
                
                // Log to unified punch_log_consolidated table with error handling
                Task.Run(() => {
                    try
                    {
                        bool success = DatabaseHelper.EndPunchSession(currentUser?.Name);
                        if (!success)
                            LogToFile("Failed to end punch session in database");
                        else
                            LogToFile("Punch session ended in database successfully");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error ending punch session: {ex.Message}");
                    }
                });
                
                statusLabel.Text = $"✓ Punched Out - Duration: {duration:hh\\:mm\\:ss}";
                statusLabel.ForeColor = accentColor;
                
                punchInTime = null;
            }
        }
        
        private void BreakButton_Click(object? sender, EventArgs e)
        {
            if (!isOnBreak)
            {
                // Start Break
                isOnBreak = true;
                breakStartTime = DateTime.Now;
                if (breakButton != null)
                {
                    breakButton.Text = "▶ RESUME WORK";
                    breakButton.BackColor = Color.FromArgb(34, 197, 94);  // green-500
                    breakButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 163, 74);  // green-600
                }
                
                // Log break start with detailed date/time
                activityLogs.Add(new ActivityLog
                {
                    Timestamp = DateTime.Now,
                    Type = "BREAK START",
                    Description = $"User started break at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Duration = ""
                });
                
                // Log to unified punch_log_consolidated table with error handling
                Task.Run(() => {
                    try
                    {
                        bool success = DatabaseHelper.StartBreak(currentUser?.Name);
                        if (!success)
                            LogToFile("Failed to start break in database");
                        else
                            LogToFile("Break started in database successfully");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error starting break: {ex.Message}");
                    }
                });
                
                statusLabel.Text = $"☕ On Break since {breakStartTime:yyyy-MM-dd HH:mm:ss}";
                statusLabel.ForeColor = Color.FromArgb(234, 179, 8);  // yellow-500
                
                // Update tray menu
                UpdateTrayPunchBreakItems();
            }
            else
            {
                EndBreak(true);
            }
        }
        
        private void EndBreak(bool showMessage)
        {
            if (!isOnBreak) return;
            
            // End Break / Resume Work
            isOnBreak = false;
            DateTime breakEndTime = DateTime.Now;
            TimeSpan breakDuration = breakEndTime - (breakStartTime ?? breakEndTime);
            
            if (breakButton != null)
            {
                breakButton.Text = "☕ TAKE BREAK";
                breakButton.BackColor = Color.FromArgb(234, 179, 8);  // yellow-500
                breakButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(202, 138, 4);  // yellow-600
            }
            
            // Log break end with detailed information (date, time, duration)
            activityLogs.Add(new ActivityLog
            {
                Timestamp = DateTime.Now,
                Type = "BREAK END",
                Description = $"User resumed work at {breakEndTime:yyyy-MM-dd HH:mm:ss}. Break duration: {breakDuration:hh\\:mm\\:ss}",
                Duration = breakDuration.ToString(@"hh\:mm\:ss")
            });
            
            // Log to punch_logs table with full details (break start time, end time, and duration in seconds) with error handling
            if (breakStartTime.HasValue)
            {
                Task.Run(() => {
                    try
                    {
                        bool success = DatabaseHelper.EndBreak(currentUser?.Name);
                        if (!success)
                            LogToFile("Failed to end break in database");
                        else
                            LogToFile("Break ended in database successfully");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error ending break: {ex.Message}");
                    }
                });
            }
            
            if (showMessage)
            {
                statusLabel.Text = $"✓ Break ended - Duration: {breakDuration:hh\\:mm\\:ss}. Resumed work at {breakEndTime:HH:mm:ss}";
                statusLabel.ForeColor = successColor;
            }
            
            breakStartTime = null;
            
            // Update tray menu
            UpdateTrayPunchBreakItems();
        }
        
        private void UpdateTrayPunchBreakItems()
        {
            if (trayPunchItem != null)
            {
                trayPunchItem.Text = isPunchedIn ? "⏹ Punch Out" : "⏺ Punch In";
            }
            if (trayBreakItem != null)
            {
                trayBreakItem.Visible = isPunchedIn;  // Show break item only when punched in
                if (isPunchedIn)
                {
                    trayBreakItem.Text = isOnBreak ? "▶ RESUME WORK" : "☕ TAKE BREAK";
                }
            }
            
            // Refresh the context menu to show/hide items
            if (trayMenu != null)
            {
                trayMenu.Refresh();
            }
        }
        
        private void UnlockAdvancedButton_Click(object? sender, EventArgs e)
        {
            if (advancedFeaturesUnlocked)
            {
                MessageBox.Show("Tabs are already unlocked. They will auto-lock in a few minutes.", "Already Unlocked", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Create password dialog
            Form passwordDialog = new Form
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "🔓 Unlock Hidden Tabs",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = cardColor,
                ForeColor = textColor,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label promptLabel = new Label
            {
                Text = "Enter password to unlock all tabs (5 min access):",
                Left = 20,
                Top = 20,
                Width = 340,
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor
            };
            
            TextBox passwordBox = new TextBox
            {
                Left = 20,
                Top = 50,
                Width = 340,
                Font = new Font("Segoe UI", 11),
                BackColor = bgColor,
                ForeColor = textColor,
                PasswordChar = '●',
                UseSystemPasswordChar = true
            };
            
            Button unlockBtn = new Button
            {
                Text = "🔓 UNLOCK",
                Left = 140,
                Top = 90,
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(139, 92, 246),  // violet-500
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            unlockBtn.FlatAppearance.BorderSize = 0;
            
            Button cancelBtn = new Button
            {
                Text = "CANCEL",
                Left = 250,
                Top = 90,
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(55, 62, 71),
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            
            passwordDialog.Controls.Add(promptLabel);
            passwordDialog.Controls.Add(passwordBox);
            passwordDialog.Controls.Add(unlockBtn);
            passwordDialog.Controls.Add(cancelBtn);
            passwordDialog.AcceptButton = unlockBtn;
            
            if (passwordDialog.ShowDialog() == DialogResult.OK)
            {
                if (passwordBox.Text == ADVANCED_PASSWORD)
                {
                    // Unlock successful
                    advancedFeaturesUnlocked = true;
                    
                    // Add all hidden tabs in order
                    int insertIndex = 0;
                    if (hiddenControlTab != null && !tabControl.TabPages.Contains(hiddenControlTab))
                    {
                        tabControl.TabPages.Insert(insertIndex++, hiddenControlTab);
                    }
                    if (hiddenWindowsTab != null && !tabControl.TabPages.Contains(hiddenWindowsTab))
                    {
                        tabControl.TabPages.Insert(insertIndex++, hiddenWindowsTab);
                    }
                    if (hiddenPrinterTab != null && !tabControl.TabPages.Contains(hiddenPrinterTab))
                    {
                        tabControl.TabPages.Insert(insertIndex++, hiddenPrinterTab);
                    }
                    if (hiddenSecurityTab != null && !tabControl.TabPages.Contains(hiddenSecurityTab))
                    {
                        tabControl.TabPages.Insert(insertIndex++, hiddenSecurityTab);
                    }
                    if (hiddenNetworkTab != null && !tabControl.TabPages.Contains(hiddenNetworkTab))
                    {
                        tabControl.TabPages.Insert(insertIndex++, hiddenNetworkTab);
                    }
                    if (hiddenAdvancedTab != null && !tabControl.TabPages.Contains(hiddenAdvancedTab))
                    {
                        tabControl.TabPages.Insert(insertIndex++, hiddenAdvancedTab);
                    }
                    // AUDIT is already at position after ADVANCED
                    // LOGS tab after AUDIT
                    if (hiddenLogsTab != null && !tabControl.TabPages.Contains(hiddenLogsTab))
                    {
                        // Insert before SYSTEM INFO (which is last)
                        int auditIndex = tabControl.TabPages.IndexOf(tabControl.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text.Contains("AUDIT")) ?? tabControl.TabPages[0]);
                        tabControl.TabPages.Insert(auditIndex + 1, hiddenLogsTab);
                    }
                    
                    // Update unlock button
                    if (sender is Button btn)
                    {
                        btn.Text = "⏱ 5:00";
                        btn.BackColor = Color.FromArgb(34, 197, 94);  // green-500
                    }
                    
                    // Start 5-minute auto-lock timer
                    StartAutoLockTimer();
                    
                    MessageBox.Show("All tabs unlocked! Access will auto-lock in 5 minutes.", 
                        "Unlocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Incorrect password. Access denied.", "Access Denied", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void StartAutoLockTimer()
        {
            // Stop existing timer if any
            if (autoLockTimer != null)
            {
                autoLockTimer.Stop();
                autoLockTimer.Dispose();
            }
            
            int remainingSeconds = 300; // 5 minutes = 300 seconds
            
            autoLockTimer = new System.Windows.Forms.Timer();
            autoLockTimer.Interval = 1000; // 1 second
            autoLockTimer.Tick += (s, e) =>
            {
                remainingSeconds--;
                
                // Update button text with countdown
                if (unlockAdvancedButtonRef != null)
                {
                    int mins = remainingSeconds / 60;
                    int secs = remainingSeconds % 60;
                    unlockAdvancedButtonRef.Text = $"⏱ {mins}:{secs:D2}";
                }
                
                if (remainingSeconds <= 0)
                {
                    // Time's up - lock the tabs
                    AutoLockTabs();
                }
            };
            autoLockTimer.Start();
        }
        
        private void AutoLockTabs()
        {
            // Stop timer
            if (autoLockTimer != null)
            {
                autoLockTimer.Stop();
                autoLockTimer.Dispose();
                autoLockTimer = null;
            }
            
            // Remove all hidden tabs from view
            if (hiddenControlTab != null && tabControl.TabPages.Contains(hiddenControlTab))
                tabControl.TabPages.Remove(hiddenControlTab);
            if (hiddenWindowsTab != null && tabControl.TabPages.Contains(hiddenWindowsTab))
                tabControl.TabPages.Remove(hiddenWindowsTab);
            if (hiddenPrinterTab != null && tabControl.TabPages.Contains(hiddenPrinterTab))
                tabControl.TabPages.Remove(hiddenPrinterTab);
            if (hiddenSecurityTab != null && tabControl.TabPages.Contains(hiddenSecurityTab))
                tabControl.TabPages.Remove(hiddenSecurityTab);
            if (hiddenNetworkTab != null && tabControl.TabPages.Contains(hiddenNetworkTab))
                tabControl.TabPages.Remove(hiddenNetworkTab);
            if (hiddenAdvancedTab != null && tabControl.TabPages.Contains(hiddenAdvancedTab))
                tabControl.TabPages.Remove(hiddenAdvancedTab);
            if (hiddenLogsTab != null && tabControl.TabPages.Contains(hiddenLogsTab))
                tabControl.TabPages.Remove(hiddenLogsTab);
            
            // Reset unlock state
            advancedFeaturesUnlocked = false;
            
            // Reset button
            if (unlockAdvancedButtonRef != null)
            {
                unlockAdvancedButtonRef.Text = "🔓 UNLOCK TABS";
                unlockAdvancedButtonRef.BackColor = Color.FromArgb(139, 92, 246);  // violet-500
                unlockAdvancedButtonRef.Enabled = true;
            }
            
            // Switch to AUDIT tab (first visible tab)
            if (tabControl.TabPages.Count > 0)
            {
                tabControl.SelectedIndex = 0;
            }
            
            // Notify user
            if (trayIcon != null)
            {
                trayIcon.ShowBalloonTip(3000, "Desktop Controller Pro", 
                    "Tabs have been auto-locked. Enter password to unlock again.", 
                    ToolTipIcon.Info);
            }
        }
        
        /// <summary>
        /// Connect to Partner button - allows viewing remote system by entering System ID
        /// </summary>
        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            // Require password to connect to other systems - ALWAYS ask, never use cache
            if (!PasswordManager.PromptForPasswordAlways(storedCompanyName, "connect to remote system"))
            {
                return;
            }
            
            // Create input dialog
            Form inputDialog = new Form
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Connect to Partner System",
                StartPosition = FormStartPosition.CenterParent,
                BackColor = cardColor,
                ForeColor = textColor,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label promptLabel = new Label
            {
                Text = "Enter Partner System ID:",
                Left = 20,
                Top = 20,
                Width = 340,
                Font = new Font("Segoe UI", 11),
                ForeColor = textColor
            };
            
            TextBox inputBox = new TextBox
            {
                Left = 20,
                Top = 55,
                Width = 340,
                Font = new Font("Segoe UI", 12),
                BackColor = bgColor,
                ForeColor = textColor,
                Text = ""
            };
            
            Button connectBtn = new Button
            {
                Text = "🔗 CONNECT",
                Left = 150,
                Top = 105,
                Width = 100,
                Height = 35,
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            connectBtn.FlatAppearance.BorderSize = 0;
            
            Button cancelBtn = new Button
            {
                Text = "CANCEL",
                Left = 260,
                Top = 105,
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(55, 62, 71),
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            
            inputDialog.Controls.Add(promptLabel);
            inputDialog.Controls.Add(inputBox);
            inputDialog.Controls.Add(connectBtn);
            inputDialog.Controls.Add(cancelBtn);
            inputDialog.AcceptButton = connectBtn;
            
            if (inputDialog.ShowDialog() == DialogResult.OK)
            {
                string partnerSystemId = inputBox.Text.Trim();
                
                if (string.IsNullOrEmpty(partnerSystemId))
                {
                    MessageBox.Show("Please enter a System ID.", "Invalid ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Open viewer
                OpenPartnerViewer(partnerSystemId);
            }
        }
        
        /// <summary>
        /// Opens viewer for partner system
        /// </summary>
        private void OpenPartnerViewer(string partnerSystemId)
        {
            string sessionId = $"session-{DateTime.Now.Ticks}";
            
            LogToFile($"=== OPENING VIEWER FOR: {partnerSystemId} ===");
            
            // Create viewing session
            Task.Run(() =>
            {
                try
                {
                    using (var connection = new Npgsql.NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                    {
                        connection.Open();
                        string sql = @"INSERT INTO live_sessions (session_id, system_id, viewer_username, is_active)
                                     VALUES (@session_id, @system_id, @viewer, TRUE)";
                        using (var cmd = new Npgsql.NpgsqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@session_id", sessionId);
                            cmd.Parameters.AddWithValue("@system_id", partnerSystemId);
                            cmd.Parameters.AddWithValue("@viewer", currentUser?.Name ?? "Unknown");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LogToFile($"Session created in database: {sessionId}");
                }
                catch (Exception ex)
                {
                    LogToFile($"Failed to create session: {ex.Message}");
                }
            });
            
            // Create viewer window
            Form viewer = new Form
            {
                Text = $"🔗 Viewing: {partnerSystemId}",
                Width = 1200,
                Height = 700,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = bgColor
            };
            
            PictureBox picBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
            Label status = new Label { 
                Dock = DockStyle.Bottom, 
                Height = 40, 
                TextAlign = ContentAlignment.MiddleCenter, 
                Font = new Font("Segoe UI", 11), 
                ForeColor = accentColor, 
                BackColor = cardColor, 
                Text = $"📡 Connecting to {partnerSystemId}..."
            };
            
            viewer.Controls.Add(picBox);
            viewer.Controls.Add(status);
            
            int frameCount = 0;
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, ev) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        LogToFile($"Fetching frame for: {partnerSystemId}");
                        var frame = RemoteViewingHelper.GetLatestLiveFrame(partnerSystemId);
                        
                        if (frame.HasValue)
                        {
                            frameCount++;
                            LogToFile($"Frame received: {frameCount}, Size: {frame.Value.frameData.Length} bytes");
                            
                            byte[] bytes = Convert.FromBase64String(frame.Value.frameData);
                            using (var ms = new MemoryStream(bytes))
                            {
                                Image img = Image.FromStream(ms);
                                
                                if (picBox.InvokeRequired)
                                {
                                    picBox.Invoke(new Action(() =>
                                    {
                                        try
                                        {
                                            picBox.Image?.Dispose();
                                            picBox.Image = img;
                                            status.Text = $"🟢 Live • {frame.Value.width}x{frame.Value.height} • Frame #{frameCount} • {frame.Value.captureTime:HH:mm:ss}";
                                            status.ForeColor = successColor;
                                        }
                                        catch (Exception ex)
                                        {
                                            LogToFile($"UI update error: {ex.Message}");
                                        }
                                    }));
                                }
                                else
                                {
                                    picBox.Image?.Dispose();
                                    picBox.Image = img;
                                    status.Text = $"🟢 Live • {frame.Value.width}x{frame.Value.height} • Frame #{frameCount} • {frame.Value.captureTime:HH:mm:ss}";
                                    status.ForeColor = successColor;
                                }
                            }
                        }
                        else
                        {
                            LogToFile($"No frame available for: {partnerSystemId}");
                            
                            if (status.InvokeRequired)
                            {
                                status.Invoke(new Action(() =>
                                {
                                    status.Text = $"⚠️ Waiting for frames from {partnerSystemId}... (Check if system is online and punched in)";
                                    status.ForeColor = warningColor;
                                }));
                            }
                            else
                            {
                                status.Text = $"⚠️ Waiting for frames from {partnerSystemId}... (Check if system is online and punched in)";
                                status.ForeColor = warningColor;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Frame fetch error: {ex.Message}");
                        
                        if (status.InvokeRequired)
                        {
                            status.Invoke(new Action(() =>
                            {
                                status.Text = $"❌ Error: {ex.Message}";
                                status.ForeColor = dangerColor;
                            }));
                        }
                    }
                });
            };
            
            viewer.Load += (s, ev) => 
            {
                timer.Start();
                LogToFile($"Viewer loaded, timer started for: {partnerSystemId}");
            };
            
            viewer.FormClosing += (s, ev) =>
            {
                timer.Stop();
                LogToFile($"Viewer closing for: {partnerSystemId}");
                
                Task.Run(() =>
                {
                    try
                    {
                        using (var connection = new Npgsql.NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                        {
                            connection.Open();
                            using (var cmd = new Npgsql.NpgsqlCommand("UPDATE live_sessions SET is_active=FALSE, ended_at=CURRENT_TIMESTAMP WHERE session_id=@id", connection))
                            {
                                cmd.Parameters.AddWithValue("@id", sessionId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LogToFile($"Session ended: {sessionId}");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Failed to end session: {ex.Message}");
                    }
                });
            };
            
            viewer.Show();
        }
        
        /// <summary>
        /// View All Systems button - shows all registered systems with their details (PASSWORD PROTECTED)
        /// Uses unified company password from PasswordManager.
        /// </summary>
        private async void ViewSystemsButton_Click(object? sender, EventArgs e)
        {
            try
            {
                LogToFile("Opening View All Systems window - checking company password");
                
                // Use unified PasswordManager for company password - ALWAYS ask, never use cache
                if (!PasswordManager.PromptForPasswordAlways(storedCompanyName, "view registered systems"))
                {
                    return; // Password not verified or cancelled
                }
                
                LogToFile("Company password verified - opening systems list");
                
                // Fetch all registered systems from database (async to prevent UI freeze)
                var allSystems = await Task.Run(() => RemoteViewingHelper.GetAllRegisteredSystems());
                
                // Debug: Log all companies in database
                LogToFile($"Current logged in company: '{storedCompanyName}'");
                foreach (var sys in allSystems) {
                    LogToFile($"  DB System: {sys.systemId} | Company: '{sys.companyName}' | Match: {sys.companyName?.Trim().ToLower() == storedCompanyName?.Trim().ToLower()}");
                }
                
                // Filter to show only systems from the SAME COMPANY
                var systems = allSystems.Where(s => 
                    !string.IsNullOrEmpty(storedCompanyName) && 
                    !string.IsNullOrEmpty(s.companyName) &&
                    s.companyName.Trim().Equals(storedCompanyName.Trim(), StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                LogToFile($"Filtered systems: {systems.Count} of {allSystems.Count} (Company: '{storedCompanyName}')");
                
                if (systems.Count == 0)
                {
                    MessageBox.Show($"No systems registered for company: {storedCompanyName}", "Systems List", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Create viewer form - Modern Tailwind Style
                Form viewer = new Form
                {
                    Text = $"📋 {storedCompanyName} - Registered Systems ({systems.Count})",
                    Size = new Size(1250, 720),
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = bgColor,  // slate-900
                    Icon = this.Icon
                };
                
                // Create DataGridView to display systems - Modern Tailwind Style
                DataGridView grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = bgColor,  // slate-900
                    ForeColor = textColor,  // slate-50
                    GridColor = borderColor,  // slate-700
                    BorderStyle = BorderStyle.None,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ColumnHeadersHeight = 45,
                    RowTemplate = { Height = 40 },
                    Margin = new Padding(20)
                };
                
                // Style grid - Modern Tailwind Colors
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = primaryColor;  // slate-800
                grid.ColumnHeadersDefaultCellStyle.ForeColor = textColor;  // slate-50
                grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
                grid.DefaultCellStyle.BackColor = cardColor;  // slate-800
                grid.DefaultCellStyle.ForeColor = textColor;  // slate-50
                grid.DefaultCellStyle.SelectionBackColor = accentColor;  // indigo-500
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
                grid.DefaultCellStyle.Padding = new Padding(15, 8, 15, 8);
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(41, 53, 73);  // slightly lighter slate
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                
                // Add columns
                grid.Columns.Add("SystemId", "🔢 System ID");
                grid.Columns.Add("SystemName", "💻 Computer Name");
                grid.Columns.Add("IpAddress", "🌐 IP Address");
                grid.Columns.Add("MachineId", "🔑 Machine ID");
                grid.Columns.Add("Username", "👤 User");
                grid.Columns.Add("CompanyName", "🏢 Company");
                grid.Columns.Add("Status", "📡 Status");
                grid.Columns.Add("LastSeen", "🕒 Last Seen");
                
                // Set column widths
                grid.Columns["SystemId"].Width = 120;
                grid.Columns["SystemName"].Width = 150;
                grid.Columns["IpAddress"].Width = 130;
                grid.Columns["MachineId"].Width = 200;
                grid.Columns["Username"].Width = 120;
                grid.Columns["CompanyName"].Width = 150;
                grid.Columns["Status"].Width = 100;
                grid.Columns["LastSeen"].Width = 180;
                
                // Add rows
                foreach (var system in systems)
                {
                    string status = system.isOnline ? "🟢 Online" : "⚫ Offline";
                    string lastSeen = system.lastSeen == DateTime.MinValue ? "Never" : system.lastSeen.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    grid.Rows.Add(
                        system.systemId,
                        system.systemName,
                        system.ipAddress,
                        system.machineId.Substring(0, Math.Min(24, system.machineId.Length)) + "...",
                        system.username,
                        system.companyName,
                        status,
                        lastSeen
                    );
                }
                
                // Add connect button column
                DataGridViewButtonColumn connectCol = new DataGridViewButtonColumn
                {
                    Name = "ConnectButton",
                    HeaderText = "Action",
                    Text = "🔗 Connect",
                    UseColumnTextForButtonValue = true,
                    Width = 100
                };
                grid.Columns.Add(connectCol);
                
                // Handle connect button click
                grid.CellContentClick += (s, ev) =>
                {
                    if (ev.ColumnIndex == grid.Columns["ConnectButton"].Index && ev.RowIndex >= 0)
                    {
                        string systemId = grid.Rows[ev.RowIndex].Cells["SystemId"].Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(systemId))
                        {
                            LogToFile($"Connecting to system from grid: {systemId}");
                            viewer.Close();
                            OpenPartnerViewer(systemId);
                        }
                    }
                };
                
                // Add refresh button at bottom - Modern Tailwind Style
                Panel bottomPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 70,
                    BackColor = primaryColor,  // slate-800
                    Padding = new Padding(20, 15, 20, 15)
                };
                
                // Add top border to bottom panel
                bottomPanel.Paint += (s, e) =>
                {
                    using (var pen = new Pen(borderColor, 2))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
                    }
                };
                
                Button refreshButton = new Button
                {
                    Text = "🔄 REFRESH",
                    Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = accentColor,  // indigo-500
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(130, 40),
                    Location = new Point(20, 15),
                    Cursor = Cursors.Hand
                };
                refreshButton.FlatAppearance.BorderSize = 0;
                refreshButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(79, 70, 229);  // indigo-600
                refreshButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(67, 56, 202);  // indigo-700
                refreshButton.Click += (s, ev) =>
                {
                    viewer.Close();
                    ViewSystemsButton_Click(null, EventArgs.Empty);
                };
                
                Label countLabel = new Label
                {
                    Text = $"Total Systems: {systems.Count}",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.White,
                    AutoSize = true,
                    Location = new Point(150, 20)
                };
                
                bottomPanel.Controls.Add(refreshButton);
                bottomPanel.Controls.Add(countLabel);
                
                viewer.Controls.Add(grid);
                viewer.Controls.Add(bottomPanel);
                viewer.Show();
                
                LogToFile($"Displayed {systems.Count} registered systems");
            }
            catch (Exception ex)
            {
                LogToFile($"Error opening systems viewer: {ex.Message}");
                MessageBox.Show($"Error loading systems: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Shows a dialog to set password for SYSTEMS (first time)
        /// </summary>
        private bool ShowSetSystemsPasswordDialog()
        {
            // Pause UI refresh timer to prevent interference with dialog
            uiRefreshTimer?.Stop();
            
            Form dialog = new Form
            {
                Text = "🔐 Set Systems Password",
                Size = new Size(450, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = cardColor,
                ShowInTaskbar = false
            };
            
            Label titleLabel = new Label
            {
                Text = "First Time Setup - Set Password for Systems Access",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = successColor,
                Location = new Point(25, 20),
                AutoSize = true
            };
            
            Label newPwdLabel = new Label
            {
                Text = "New Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(25, 60),
                AutoSize = true
            };
            
            TextBox newPwdBox = new TextBox
            {
                Location = new Point(25, 85),
                Size = new Size(390, 32),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Label confirmPwdLabel = new Label
            {
                Text = "Confirm Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(25, 125),
                AutoSize = true
            };
            
            TextBox confirmPwdBox = new TextBox
            {
                Location = new Point(25, 150),
                Size = new Size(390, 32),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Button saveBtn = new Button
            {
                Text = "✓ SET PASSWORD",
                Location = new Point(190, 195),
                Size = new Size(130, 38),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = successColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            
            Button cancelBtn = new Button
            {
                Text = "✕ CANCEL",
                Location = new Point(325, 195),
                Size = new Size(90, 38),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = dangerColor,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            
            bool success = false;
            
            saveBtn.Click += async (s, ev) =>
            {
                if (string.IsNullOrEmpty(newPwdBox.Text) || newPwdBox.Text.Length < 4)
                {
                    MessageBox.Show("Password must be at least 4 characters!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (newPwdBox.Text != confirmPwdBox.Text)
                {
                    MessageBox.Show("Passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Save password for this company (async to prevent freeze)
                string password = newPwdBox.Text;
                string activationKey = storedActivationKey ?? "";
                await Task.Run(() => RemoteViewingHelper.SaveStreamPassword(activationKey, password));
                LogToFile("Systems password set successfully for company");
                MessageBox.Show("✅ Systems password set successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                success = true;
                dialog.Close();
            };
            
            dialog.Controls.AddRange(new Control[] { titleLabel, newPwdLabel, newPwdBox, confirmPwdLabel, confirmPwdBox, saveBtn, cancelBtn });
            dialog.AcceptButton = saveBtn;
            dialog.CancelButton = cancelBtn;
            newPwdBox.Focus();
            
            dialog.ShowDialog(this);
            
            // Resume UI refresh timer
            uiRefreshTimer?.Start();
            
            return success;
        }
        
        /// <summary>
        /// Shows a dialog to verify password for SYSTEMS access
        /// </summary>
        private bool ShowVerifySystemsPasswordDialog()
        {
            // Pause UI refresh timer to prevent interference with dialog
            uiRefreshTimer?.Stop();
            
            Form dialog = new Form
            {
                Text = "🔒 Password Required",
                Size = new Size(450, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = cardColor,
                ShowInTaskbar = false
            };
            
            Label promptLabel = new Label
            {
                Text = "Enter password to view all systems:",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(25, 25),
                AutoSize = true
            };
            
            TextBox passwordBox = new TextBox
            {
                Location = new Point(25, 60),
                Size = new Size(390, 35),
                Font = new Font("Segoe UI", 12),
                PasswordChar = '●',
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Button okButton = new Button
            {
                Text = "✓ VERIFY",
                Location = new Point(200, 115),
                Size = new Size(100, 38),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            okButton.FlatAppearance.BorderSize = 0;
            
            Button cancelButton = new Button
            {
                Text = "✕ CANCEL",
                Location = new Point(310, 115),
                Size = new Size(100, 38),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = dangerColor,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            
            // Change password button
            Button changePasswordBtn = new Button
            {
                Text = "🔑 Change Password",
                Location = new Point(25, 115),
                Size = new Size(160, 38),
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = warningColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            changePasswordBtn.FlatAppearance.BorderSize = 0;
            
            bool success = false;
            
            okButton.Click += async (s, ev) =>
            {
                string password = passwordBox.Text;
                string activationKey = storedActivationKey ?? "";
                bool verified = await Task.Run(() => RemoteViewingHelper.VerifyStreamPassword(activationKey, password));
                
                if (verified)
                {
                    success = true;
                    dialog.Close();
                }
                else
                {
                    LogToFile("Incorrect password entered for systems view");
                    MessageBox.Show("❌ Incorrect password!", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    passwordBox.Text = "";
                    passwordBox.Focus();
                }
            };
            
            changePasswordBtn.Click += (s, ev) =>
            {
                dialog.Close();
                ShowChangeSystemsPasswordDialog();
            };
            
            dialog.Controls.AddRange(new Control[] { promptLabel, passwordBox, okButton, cancelButton, changePasswordBtn });
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;
            passwordBox.Focus();
            
            dialog.ShowDialog(this);
            
            // Resume UI refresh timer
            uiRefreshTimer?.Start();
            
            return success;
        }
        
        /// <summary>
        /// Shows a dialog to change the SYSTEMS password (Previous + New + Confirm)
        /// </summary>
        private void ShowChangeSystemsPasswordDialog()
        {
            // Pause UI refresh timer to prevent interference with dialog
            uiRefreshTimer?.Stop();
            
            Form dialog = new Form
            {
                Text = "🔑 Change Systems Password",
                Size = new Size(450, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = cardColor,
                ShowInTaskbar = false
            };
            
            Label titleLabel = new Label
            {
                Text = "Change Systems Password",
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = warningColor,
                Location = new Point(25, 20),
                AutoSize = true
            };
            
            Label prevPwdLabel = new Label
            {
                Text = "Previous Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(25, 55),
                AutoSize = true
            };
            
            TextBox prevPwdBox = new TextBox
            {
                Location = new Point(25, 78),
                Size = new Size(390, 32),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Label newPwdLabel = new Label
            {
                Text = "New Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(25, 118),
                AutoSize = true
            };
            
            TextBox newPwdBox = new TextBox
            {
                Location = new Point(25, 141),
                Size = new Size(390, 32),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Label confirmPwdLabel = new Label
            {
                Text = "Confirm New Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(25, 181),
                AutoSize = true
            };
            
            TextBox confirmPwdBox = new TextBox
            {
                Location = new Point(25, 204),
                Size = new Size(390, 32),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            Button saveBtn = new Button
            {
                Text = "✓ CHANGE PASSWORD",
                Location = new Point(190, 255),
                Size = new Size(150, 38),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = successColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            
            Button cancelBtn = new Button
            {
                Text = "✕ CANCEL",
                Location = new Point(345, 255),
                Size = new Size(80, 38),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = dangerColor,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            
            saveBtn.Click += async (s, ev) =>
            {
                // Verify previous password (async)
                string prevPassword = prevPwdBox.Text;
                string activationKey = storedActivationKey ?? "";
                bool prevValid = await Task.Run(() => RemoteViewingHelper.VerifyStreamPassword(activationKey, prevPassword));
                
                if (!prevValid)
                {
                    MessageBox.Show("❌ Previous password is incorrect!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (string.IsNullOrEmpty(newPwdBox.Text) || newPwdBox.Text.Length < 4)
                {
                    MessageBox.Show("New password must be at least 4 characters!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (newPwdBox.Text != confirmPwdBox.Text)
                {
                    MessageBox.Show("New passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Save new password (async)
                string newPassword = newPwdBox.Text;
                await Task.Run(() => RemoteViewingHelper.SaveStreamPassword(activationKey, newPassword));
                LogToFile("Systems password changed successfully");
                MessageBox.Show("✅ Password changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dialog.Close();
            };
            
            dialog.Controls.AddRange(new Control[] { titleLabel, prevPwdLabel, prevPwdBox, newPwdLabel, newPwdBox, confirmPwdLabel, confirmPwdBox, saveBtn, cancelBtn });
            dialog.AcceptButton = saveBtn;
            dialog.CancelButton = cancelBtn;
            prevPwdBox.Focus();
            
            dialog.ShowDialog(this);
            
            // Resume UI refresh timer
            uiRefreshTimer?.Start();
        }
        
        private void InitializeActivityTracking()
        {
            // Activity timer - checks every 3 seconds for app usage and browser activity
            activityTimer = new System.Windows.Forms.Timer();
            activityTimer.Interval = 3000; // 3 seconds for faster capture
            activityTimer.Tick += ActivityTimer_Tick;

            // App usage timer - refreshes app usage every 5 seconds
            appUsageTimer = new System.Windows.Forms.Timer();
            appUsageTimer.Interval = 5000; // 5 seconds
            appUsageTimer.Tick += AppUsageTimer_Tick;

            // Inactivity timer - tracks idle time
            inactivityTimer = new System.Windows.Forms.Timer();
            inactivityTimer.Interval = 1000; // 1 second
            inactivityTimer.Tick += InactivityTimer_Tick;

            // Browser scan timer - scans browser history every 10 seconds
            browserScanTimer = new System.Windows.Forms.Timer();
            browserScanTimer.Interval = 10000; // 10 seconds for auto browser history scan
            browserScanTimer.Tick += BrowserScanTimer_Tick;

            // Sync timer - auto-sync all data every 8 seconds
            syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = 8000; // 8 seconds
            syncTimer.Tick += SyncTimer_Tick;

            // UI refresh timer - refreshes website/app/inactivity tabs every 5 seconds
            uiRefreshTimer = new System.Windows.Forms.Timer();
            uiRefreshTimer.Interval = 5000; // 5 seconds
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            
            // Process scanner - scans ALL running processes every 15 seconds
            processScanner = new System.Windows.Forms.Timer();
            processScanner.Interval = 15000; // 15 seconds
            processScanner.Tick += ProcessScanner_Tick;
            
            // Load screenshot settings from registry
            screenshotSettings = ScreenshotSettings.Load();
            
            // Screenshot timer - uses user-configured interval (default 10 seconds)
            screenshotTimer = new System.Windows.Forms.Timer();
            screenshotTimer.Interval = screenshotSettings.IsConfigured ? screenshotSettings.GetIntervalMilliseconds() : 10000;
            screenshotTimer.Tick += ScreenshotTimer_Tick;
            
            // Heartbeat timer - updates system online status every 30 seconds
            heartbeatTimer = new System.Windows.Forms.Timer();
            heartbeatTimer.Interval = 30000; // 30 seconds
            heartbeatTimer.Tick += HeartbeatTimer_Tick;
            
            // Live stream timer - sends frames when someone is viewing (every 2 seconds)
            liveStreamTimer = new System.Windows.Forms.Timer();
            liveStreamTimer.Interval = 2000; // 2 seconds
            liveStreamTimer.Tick += LiveStreamTimer_Tick;
        }
        
        private void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            // Update system heartbeat in background
            if (!string.IsNullOrEmpty(systemId))
            {
                _ = Task.Run(() => {
                    try {
                        RemoteViewingHelper.UpdateSystemHeartbeat(systemId);
                    } catch { }
                });
            }
        }
        
        private void LiveStreamTimer_Tick(object? sender, EventArgs e)
        {
            // Check if someone is viewing this system
            if (!string.IsNullOrEmpty(systemId))
            {
                _ = Task.Run(() => {
                    try {
                        bool hasViewer = RemoteViewingHelper.HasActiveViewingSession(systemId);
                        if (hasViewer)
                        {
                            // Someone is viewing, capture and send frame
                            CaptureAndSendLiveFrame();
                        }
                    } catch { }
                });
            }
        }
        
        private void CaptureAndSendLiveFrame()
        {
            try
            {
                // Get combined screen bounds
                Rectangle bounds = Rectangle.Empty;
                foreach (var screen in Screen.AllScreens)
                {
                    bounds = Rectangle.Union(bounds, screen.Bounds);
                }
                
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    // Capture screenshot
                    using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                    {
                        using (Graphics g = Graphics.FromImage(screenshot))
                        {
                            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                        }
                        
                        // Convert to JPEG for smaller size (20% quality for live streaming)
                        using (MemoryStream ms = new MemoryStream())
                        {
                            var jpegEncoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                                .FirstOrDefault(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                            
                            if (jpegEncoder != null)
                            {
                                var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                                encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                                    System.Drawing.Imaging.Encoder.Quality, 20L); // Very low quality for live streaming
                                
                                screenshot.Save(ms, jpegEncoder, encoderParams);
                            }
                            else
                            {
                                screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            
                            string base64 = Convert.ToBase64String(ms.ToArray());
                            
                            // Send to database
                            RemoteViewingHelper.InsertLiveStreamFrame(systemId, base64, bounds.Width, bounds.Height);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Live frame capture error: {ex.Message}");
            }
        }
        
        private int gpsSyncCounter = 0;
        
        private void SyncTimer_Tick(object? sender, EventArgs e)
        {
            // Only sync if user has enabled syncing
            if (!isSyncingEnabled) return;
            
            // Auto-scan and sync browser history to database
            _ = Task.Run(() => {
                try {
                    // First scan browser history
                    ScanAllBrowsersForHistory();
                    // Then sync to database
                    SyncBrowserHistoryToDatabase();
                } catch { }
            });
            
            // Force sync current active app to database
            _ = Task.Run(() => {
                try {
                    ForceLogCurrentApp();
                } catch { }
            });
            
            // NOTE: Inactivity is only logged ONCE when activity resumes (in InactivityTimer_Tick)
            // This prevents duplicate database entries
            
            // Auto-capture GPS every 5 minutes (10 ticks * 8 seconds = 80 seconds, ~1.3 minutes)
            gpsSyncCounter++;
            if (gpsSyncCounter >= 10)
            {
                gpsSyncCounter = 0;
                _ = Task.Run(() => {
                    try {
                        AutoCaptureGPSLocation();
                    } catch { }
                });
            }
        }
        
        private void ForceLogCurrentApp()
        {
            try
            {
                if (lastTrackedAppKey != null && activeApps.ContainsKey(lastTrackedAppKey))
                {
                    DateTime startTime = activeApps[lastTrackedAppKey];
                    TimeSpan duration = DateTime.Now - startTime;
                    
                    // Log if used for more than 10 seconds
                    if (duration.TotalSeconds >= 10)
                    {
                        var parts = lastTrackedAppKey.Split('|');
                        string appName = parts.Length > 0 ? parts[0] : "Unknown";
                        string windowTitle = parts.Length > 1 ? parts[1] : "";
                        
                        // Log to application_logs table
                        DatabaseHelper.InsertAppLog(storedActivationKey, storedCompanyName, 
                            currentUser?.Name, appName, windowTitle, startTime, DateTime.Now);
                        
                        // Reset the start time for next interval
                        activeApps[lastTrackedAppKey] = DateTime.Now;
                    }
                }
            }
            catch { }
        }
        
        // ForceLogInactivity removed - inactivity is now only logged ONCE when activity resumes
        // This prevents duplicate database entries for the same inactivity period
        
        private void ScanAllBrowsersForHistory()
        {
            try
            {
                LogToFile("Scanning browser history...");
                // Scan last 24 hours of history (changed from 10 minutes to get all recent history)
                DateTime since = DateTime.Now.AddHours(-24);
                
                // Scan Chrome
                try { ScanChromeHistorySince(since); LogToFile("Chrome scanned"); } catch (Exception ex) { LogToFile($"Chrome scan error: {ex.Message}"); }
                
                // Scan Edge
                try { ScanEdgeHistorySince(since); LogToFile("Edge scanned"); } catch (Exception ex) { LogToFile($"Edge scan error: {ex.Message}"); }
                
                // Scan Firefox
                try { ScanFirefoxHistorySince(since); LogToFile("Firefox scanned"); } catch (Exception ex) { LogToFile($"Firefox scan error: {ex.Message}"); }
                try { ScanFirefoxHistorySince(since); } catch { }
            }
            catch { }
        }
        
        private void GenerateAndDisplaySystemId()
        {
            try
            {
                // Generate system ID immediately (this is fast, stored in registry)
                systemId = DatabaseHelper.GetOrCreateSystemId();
                
                // Update label on UI thread
                if (systemIdLabel != null && !string.IsNullOrEmpty(systemId))
                {
                    systemIdLabel.Text = $"🔢 ID: {systemId}";
                    systemIdLabel.ForeColor = accentColor;  // Blue initially
                }
                
                LogToFile($"=== SYSTEM ID GENERATED: {systemId} ===");
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to generate System ID: {ex.Message}");
                systemId = "ERROR";
                if (systemIdLabel != null)
                {
                    systemIdLabel.Text = "🔢 ID: Error";
                    systemIdLabel.ForeColor = dangerColor;
                }
            }
        }

        private void StartActivityTracking()
        {
            lastActivityTime = DateTime.Now;
            lastMousePosition = Cursor.Position;
            
            // Register system for remote viewing in background (to prevent UI freeze)
            _ = Task.Run(() => {
                try
                {
                    string registeredId = RemoteViewingHelper.RegisterSystem(storedActivationKey, storedCompanyName, currentUser?.Name);
                    
                    if (!string.IsNullOrEmpty(registeredId))
                    {
                        LogToFile($"=== SYSTEM REGISTERED IN DATABASE: {registeredId} ===");
                        
                        // Update label to show registration successful (turn green)
                        if (systemIdLabel != null)
                        {
                            if (systemIdLabel.InvokeRequired)
                            {
                                systemIdLabel.Invoke(new Action(() =>
                                {
                                    systemIdLabel.ForeColor = successColor;  // Green when registered in DB
                                }));
                            }
                            else
                            {
                                systemIdLabel.BeginInvoke(new Action(() =>
                                {
                                    systemIdLabel.ForeColor = successColor;
                                }));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Failed to register in database: {ex.Message}");
                }
            });
            
            // Start all timers - but NOT sync timers (user must click "Start Sync" button)
            activityTimer.Start();
            inactivityTimer.Start();
            // syncTimer and browserScanTimer NOT started - user must click "Start Sync" button
            // syncTimer.Start();
            // browserScanTimer.Start();
            appUsageTimer.Start();
            uiRefreshTimer.Start();
            processScanner.Start();
            // Screenshot timer NOT auto-started - user must click "Start Sync" button
            // This ensures syncing only happens when user explicitly enables it
            LogToFile("Screenshot timer NOT started - waiting for Start Sync button");
            heartbeatTimer.Start();       // Start heartbeat for online status
            liveStreamTimer.Start();      // Start checking for live viewers
            LogToFile("=== TIMERS STARTED - Activity tracking is now running (DB Sync paused - click Start Sync) ===");

            // NOTE: Removed auto-sync on startup - user must click "Start Sync" button
            // to enable database syncing for websites, apps, GPS, inactivity
        }
                        // Timer for UI auto-refresh every 5 seconds
                        private void UiRefreshTimer_Tick(object? sender, EventArgs e)
                        {
                            try {
                                LogToFile($"UI REFRESH: Websites={websiteLogs.Count}, Apps={appUsageLogs.Count}");
                                RefreshWebsitesTab();
                                RefreshApplicationsTab();
                                RefreshInactivityTab();
                                RefreshOverviewTab();
                            } catch (Exception ex) {
                                LogToFile($"UI REFRESH ERROR: {ex.Message}");
                            }
                        }
                        
                        // Refresh the Overview tab labels
                        private void RefreshOverviewTab()
                        {
                            try {
                                // Refresh Punch Status
                                if (overviewPunchLabel != null && !overviewPunchLabel.IsDisposed)
                                {
                                    if (overviewPunchLabel.InvokeRequired)
                                    {
                                        overviewPunchLabel.Invoke((MethodInvoker)delegate {
                                            overviewPunchLabel.Text = isPunchedIn ? $"✅ Punched In since {punchInTime:HH:mm:ss}" : "⏸️ Not Punched In";
                                            overviewPunchLabel.ForeColor = isPunchedIn ? successColor : secondaryTextColor;
                                        });
                                    }
                                    else
                                    {
                                        overviewPunchLabel.Text = isPunchedIn ? $"✅ Punched In since {punchInTime:HH:mm:ss}" : "⏸️ Not Punched In";
                                        overviewPunchLabel.ForeColor = isPunchedIn ? successColor : secondaryTextColor;
                                    }
                                }
                                
                                // Refresh Activity Summary
                                if (overviewActivityLabel != null && !overviewActivityLabel.IsDisposed)
                                {
                                    string activityText = $"Total Activities: {activityLogs.Count}\nApps Tracked: {appUsageLogs.Count}\nWebsites Visited: {websiteLogs.Count}";
                                    if (overviewActivityLabel.InvokeRequired)
                                    {
                                        overviewActivityLabel.Invoke((MethodInvoker)delegate {
                                            overviewActivityLabel.Text = activityText;
                                        });
                                    }
                                    else
                                    {
                                        overviewActivityLabel.Text = activityText;
                                    }
                                }
                                
                                // Refresh Inactivity
                                if (overviewInactivityLabel != null && !overviewInactivityLabel.IsDisposed)
                                {
                                    TimeSpan idleTime = DateTime.Now - lastActivityTime;
                                    string inactivityText = isPunchedIn ? $"Current Idle: {idleTime:mm\\:ss}\nLast Activity: {lastActivityTime:HH:mm:ss}" : "Tracking paused";
                                    Color inactivityColor = idleTime.TotalMinutes > 5 ? warningColor : textColor;
                                    
                                    if (overviewInactivityLabel.InvokeRequired)
                                    {
                                        overviewInactivityLabel.Invoke((MethodInvoker)delegate {
                                            overviewInactivityLabel.Text = inactivityText;
                                            overviewInactivityLabel.ForeColor = inactivityColor;
                                        });
                                    }
                                    else
                                    {
                                        overviewInactivityLabel.Text = inactivityText;
                                        overviewInactivityLabel.ForeColor = inactivityColor;
                                    }
                                }
                            } catch (Exception ex) {
                                LogToFile($"Overview refresh error: {ex.Message}");
                            }
                        }

                        // Refresh the Websites tab from websiteLogs
                        private void RefreshWebsitesTab()
                        {
                            try {
                                if (websitesListView == null || websitesListView.IsDisposed) return;
                                
                                if (websitesListView.InvokeRequired)
                                {
                                    websitesListView.Invoke((MethodInvoker)delegate {
                                        RefreshWebsitesTabInternal();
                                    });
                                }
                                else
                                {
                                    RefreshWebsitesTabInternal();
                                }
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"RefreshWebsitesTab Error: {ex.Message}");
                            }
                        }
                        
                        private void RefreshWebsitesTabInternal()
                        {
                            websitesListView.Items.Clear();
                            lock (websiteLogs)
                            {
                                System.Diagnostics.Debug.WriteLine($"[REFRESH WEBSITES] Total logs: {websiteLogs.Count}");
                                foreach (var log in websiteLogs.OrderByDescending(w => w.Timestamp).Take(100))
                                {
                                    var item = new ListViewItem(new[] {
                                        log.Timestamp.ToString("HH:mm:ss"),
                                        log.Browser,
                                        log.Url,
                                        log.Title,
                                        CategorizeUrl(log.Url)
                                    });
                                    websitesListView.Items.Add(item);
                                }
                                System.Diagnostics.Debug.WriteLine($"[REFRESH WEBSITES] UI updated with {websitesListView.Items.Count} items");
                            }
                        }

                        // Refresh the Applications tab from appUsageLogs
                        private void RefreshApplicationsTab()
                        {
                            if (applicationsListView == null || applicationsListView.IsDisposed) return;
                            applicationsListView.Invoke((MethodInvoker)delegate {
                                applicationsListView.Items.Clear();
                                System.Diagnostics.Debug.WriteLine($"[REFRESH APPS] Total logs: {appUsageLogs.Count}");
                                foreach (var log in appUsageLogs.OrderByDescending(a => a.EndTime).Take(100))
                                {
                                    var item = new ListViewItem(new[] {
                                        log.StartTime.ToString("HH:mm:ss"),
                                        log.EndTime.ToString("HH:mm:ss"),
                                        log.AppName,
                                        log.WindowTitle,
                                        log.Duration.ToString(@"hh\:mm\:ss")
                                    });
                                    applicationsListView.Items.Add(item);
                                }
                                System.Diagnostics.Debug.WriteLine($"[REFRESH APPS] UI updated with {applicationsListView.Items.Count} items");
                            });
                        }

                        // Refresh the Inactivity tab from inactivityLogs
                        private void RefreshInactivityTab()
                        {
                            if (inactivityListView == null || inactivityListView.IsDisposed) return;
                            inactivityListView.Invoke((MethodInvoker)delegate {
                                inactivityListView.Items.Clear();
                                foreach (var log in inactivityLogs.OrderByDescending(l => l.StartTime).Take(100))
                                {
                                    var item = new ListViewItem(new[] {
                                        log.StartTime.ToString("HH:mm:ss"),
                                        log.EndTime.ToString("HH:mm:ss"),
                                        log.Duration.ToString(@"hh\:mm\:ss"),
                                        log.Duration.TotalMinutes >= 10 ? "⚠️ Long" : "⏸️ Short"
                                    });
                                    inactivityListView.Items.Add(item);
                                }
                            });
                        }
                        
                // Timer for browser scan every 3 seconds
                private void BrowserScanTimer_Tick(object? sender, EventArgs e)
                {
                    try {
                        LogToFile($"=== BROWSER SCAN START ===");
                        // Scan browser databases
                        ScanAllBrowsersForHistory();
                        LogToFile($"Browser scan complete. Total website logs: {websiteLogs.Count}");
                        // Sync to database
                        SyncBrowserHistoryToDatabase();
                        LogToFile($"Database sync complete");
                        // Force UI refresh
                        RefreshWebsitesTab();
                        RefreshApplicationsTab();
                        RefreshInactivityTab();
                        LogToFile($"UI refresh complete");
                    } catch (Exception ex) {
                        LogToFile($"BrowserScanTimer Error: {ex.Message}");
                    }
                }

                // Timer for app usage refresh every 5 seconds
                private void AppUsageTimer_Tick(object? sender, EventArgs e)
                {
                    try {
                        // This will force refresh of app usage and push to DB
                        TrackActiveApplication();
                    } catch { }
                }
                
                // Process scanner - captures ALL running applications every 15 seconds
                private void ProcessScanner_Tick(object? sender, EventArgs e)
                {
                    try {
                        LogToFile("=== SCANNING ALL RUNNING PROCESSES ===");
                        var allProcesses = Process.GetProcesses();
                        int addedCount = 0;
                        
                        foreach (var proc in allProcesses)
                        {
                            try {
                                // Skip system processes and processes without main window
                                if (string.IsNullOrEmpty(proc.MainWindowTitle)) continue;
                                
                                string appName = proc.ProcessName;
                                string lowerApp = appName.ToLower();
                                
                                // Skip browsers - they go to web logs
                                bool isBrowser = lowerApp.Contains("chrome") || lowerApp.Contains("msedge") || 
                                               lowerApp.Contains("firefox") || lowerApp.Contains("opera") || 
                                               lowerApp.Contains("brave") || lowerApp.Contains("browser");
                                if (isBrowser) continue;
                                
                                // Skip system apps
                                if (lowerApp == "explorer" || lowerApp == "taskmgr" || lowerApp == "searchhost" ||
                                    lowerApp == "textinputhost" || lowerApp == "applicationframehost") continue;
                                
                                string windowTitle = proc.MainWindowTitle;
                                
                                // Check if already logged in last 30 seconds
                                bool recentlyLogged = appUsageLogs.Any(a => 
                                    a.AppName == appName && 
                                    (DateTime.Now - a.EndTime).TotalSeconds < 30);
                                
                                if (!recentlyLogged)
                                {
                                    appUsageLogs.Add(new AppUsageLog
                                    {
                                        StartTime = DateTime.Now,
                                        EndTime = DateTime.Now,
                                        AppName = appName,
                                        WindowTitle = windowTitle
                                    });
                                    
                                    // Insert to database
                                    DatabaseHelper.InsertAppLog(storedActivationKey, storedCompanyName, 
                                        currentUser?.Name, appName, windowTitle, DateTime.Now, DateTime.Now);
                                    addedCount++;
                                }
                            } catch { }
                        }
                        
                        LogToFile($"Process scan complete: Added {addedCount} applications");
                    } catch (Exception ex) {
                        LogToFile($"Process scanner error: {ex.Message}");
                    }
                }
                
                // Screenshot timer - captures screen based on user settings (with lock to prevent duplicates)
                private void ScreenshotTimer_Tick(object? sender, EventArgs e)
                {
                    // Check if syncing is enabled (user clicked Start Sync button)
                    if (!isSyncingEnabled)
                    {
                        return;
                    }
                    
                    // Check if cloud storage is configured
                    if (screenshotSettings.StorageType == "none")
                    {
                        return;
                    }
                    
                    // Prevent multiple simultaneous screenshots
                    if (isCapturingScreenshot) {
                        LogToFile("⚠ Skipping screenshot - previous capture still in progress");
                        return;
                    }
                    
                    // Run in background to not block timer
                    _ = Task.Run(async () => {
                        try {
                            isCapturingScreenshot = true;
                            LogToFile($"📸 CAPTURING SCREENSHOT at {DateTime.Now:HH:mm:ss}");
                            await CaptureAndSaveScreenshotAsync();
                        } catch (Exception ex) {
                            LogToFile($"✗ Screenshot capture error: {ex.Message}");
                        } finally {
                            isCapturingScreenshot = false;
                        }
                    });
                }
                
                private async Task CaptureAndSaveScreenshotAsync()
                {
                    try {
                        // Get all screens (multi-monitor support)
                        Rectangle bounds = Rectangle.Empty;
                        foreach (Screen screen in Screen.AllScreens)
                        {
                            bounds = Rectangle.Union(bounds, screen.Bounds);
                        }
                        
                        // Capture screenshot
                        using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                        {
                            using (Graphics g = Graphics.FromImage(screenshot))
                            {
                                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                            }
                            
                            // Convert to JPEG bytes
                            using (MemoryStream ms = new MemoryStream())
                            {
                                // Use lower quality JPEG to reduce file size
                                var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                                    .FirstOrDefault(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                                var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                                encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                                    System.Drawing.Imaging.Encoder.Quality, 75L); // 75% quality
                                
                                if (encoder != null)
                                {
                                    screenshot.Save(ms, encoder, encoderParams);
                                }
                                else
                                {
                                    screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                }
                                
                                byte[] imageBytes = ms.ToArray();
                                string fileName = CloudStorageHelper.GenerateScreenshotFileName(
                                    Environment.MachineName, 
                                    currentUser?.Name ?? Environment.UserName);
                                
                                // Upload to cloud storage
                                bool success = await CloudStorageHelper.UploadScreenshotAsync(
                                    imageBytes, 
                                    fileName, 
                                    screenshotSettings);
                                
                                if (success)
                                {
                                    LogToFile($"Screenshot saved to {screenshotSettings.StorageType}: {fileName} ({imageBytes.Length} bytes)");
                                }
                                else
                                {
                                    LogToFile($"Screenshot save to {screenshotSettings.StorageType} failed");
                                }
                            }
                        }
                    } catch (Exception ex) {
                        LogToFile($"Screenshot error: {ex.Message}");
                    }
                }
                
                // Legacy method for compatibility
                private void CaptureAndSaveScreenshot()
                {
                    _ = CaptureAndSaveScreenshotAsync();
                }
        
        private void AutoCaptureGPSLocation()
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = client.GetStringAsync("http://ip-api.com/json/").Result;
                    
                    if (response.Contains("\"lat\"") && response.Contains("\"lon\""))
                    {
                        var latMatch = System.Text.RegularExpressions.Regex.Match(response, "\"lat\":([\\d.-]+)");
                        var lonMatch = System.Text.RegularExpressions.Regex.Match(response, "\"lon\":([\\d.-]+)");
                        var cityMatch = System.Text.RegularExpressions.Regex.Match(response, "\"city\":\"([^\"]+)\"");
                        var countryMatch = System.Text.RegularExpressions.Regex.Match(response, "\"country\":\"([^\"]+)\"");
                        var ispMatch = System.Text.RegularExpressions.Regex.Match(response, "\"isp\":\"([^\"]+)\"");
                        
                        if (latMatch.Success && lonMatch.Success)
                        {
                            double lat = double.Parse(latMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                            double lon = double.Parse(lonMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                            string city = cityMatch.Success ? cityMatch.Groups[1].Value : "Unknown";
                            string country = countryMatch.Success ? countryMatch.Groups[1].Value : "Unknown";
                            string isp = ispMatch.Success ? ispMatch.Groups[1].Value : "Unknown";
                            
                            // Save GPS log to database
                            DatabaseHelper.InsertGpsLog(storedActivationKey, storedCompanyName, 
                                currentUser?.Name, lat, lon, city, country, isp);
                        }
                    }
                }
            }
            catch { }
        }
        
        private void StopActivityTracking()
        {
            activityTimer.Stop();
            inactivityTimer.Stop();
            syncTimer.Stop();
            
            // Save any active app usage
            foreach (var app in activeApps.ToList())
            {
                appUsageLogs.Add(new AppUsageLog
                {
                    StartTime = app.Value,
                    EndTime = DateTime.Now,
                    AppName = app.Key,
                    WindowTitle = app.Key
                });
            }
            activeApps.Clear();
        }
        
        private string lastLoggedApp = "";
        private string lastLoggedUrl = "";
        private DateTime lastAppLogTime = DateTime.MinValue;
        private DateTime lastWebLogTime = DateTime.MinValue;
        
        private void LogToFile(string message)
        {
            try {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DesktopController_Debug.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            } catch { }
        }
        
        private void ActivityTimer_Tick(object? sender, EventArgs e)
        {
            // Always track activity - capture and log immediately
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;
                
                var sb = new System.Text.StringBuilder(512);
                GetWindowText(hwnd, sb, 512);
                string windowTitle = sb.ToString();
                if (string.IsNullOrEmpty(windowTitle)) return;
                
                GetWindowThreadProcessId(hwnd, out uint processId);
                string appName = "";
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    appName = process.ProcessName;
                }
                catch { appName = "Unknown"; }
                
                LogToFile($"CAPTURED: App={appName}, Title={windowTitle}");
                
                // Check if it's a browser FIRST
                string lowerApp = appName.ToLower();
                bool isBrowser = lowerApp.Contains("chrome") || lowerApp.Contains("msedge") || lowerApp.Contains("firefox") || 
                                 lowerApp.Contains("opera") || lowerApp.Contains("brave") || lowerApp.Contains("browser");
                
                // Log to APPLICATION logs ONLY if it's NOT a browser
                if (!isBrowser)
                {
                    appUsageLogs.Add(new AppUsageLog
                    {
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now,
                        AppName = appName,
                        WindowTitle = windowTitle
                    });
                    LogToFile($"APPLOG ADDED (non-browser): Count={appUsageLogs.Count}, App={appName}");
                    
                    // IMMEDIATE database insert for non-browser apps only
                    try {
                        DatabaseHelper.InsertAppLog(storedActivationKey, storedCompanyName, 
                            currentUser?.Name, appName, windowTitle, DateTime.Now, DateTime.Now);
                        LogToFile($"DB INSERT SUCCESS: App={appName}");
                    } catch (Exception ex) {
                        LogToFile($"DB INSERT ERROR: {ex.Message}");
                    }
                }
                else
                {
                    LogToFile($"SKIPPED APP LOG: {appName} is a browser");
                }
                
                // If it's a browser, log to WEBSITE logs instead
                if (isBrowser)
                {
                    string browserName = lowerApp.Contains("chrome") && !lowerApp.Contains("msedge") ? "Chrome" : 
                                        lowerApp.Contains("msedge") || lowerApp.Contains("edge") ? "Edge" : 
                                        lowerApp.Contains("firefox") ? "Firefox" :
                                        lowerApp.Contains("opera") ? "Opera" :
                                        lowerApp.Contains("brave") ? "Brave" : "Browser";
                    
                    LogToFile($"BROWSER DETECTED: {browserName}, Title={windowTitle}");
                    
                    string url = ExtractUrlFromTitle(windowTitle);
                    LogToFile($"URL EXTRACTED: {url}");
                    
                    string category = CategorizeUrl(url);
                    DateTime capturedWebTime = DateTime.Now;
                    
                    // Add to local list
                    lock (websiteLogs)
                    {
                        websiteLogs.Add(new WebsiteLog
                        {
                            Timestamp = capturedWebTime,
                            Browser = browserName,
                            Url = url,
                            Title = windowTitle,
                            Duration = TimeSpan.Zero
                        });
                        LogToFile($"WEBSITELOG ADDED: Count={websiteLogs.Count}, Browser={browserName}, URL={url}");
                    }
                    
                    // IMMEDIATE database insert
                    try {
                        DatabaseHelper.InsertWebLog(storedActivationKey, storedCompanyName, 
                            currentUser?.Name, browserName, url, windowTitle, category, capturedWebTime);
                        LogToFile($"WEB DB INSERT SUCCESS: URL={url}");
                    } catch (Exception ex) {
                        LogToFile($"WEB DB INSERT ERROR: {ex.Message}");
                    }
                }
            }
            catch { }
        }
        
        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Always track inactivity (removed isPunchedIn check for auto-logging)
                Point currentMousePosition = Cursor.Position;
                bool hasActivity = currentMousePosition != lastMousePosition;
                
                if (hasActivity)
                {
                    lastMousePosition = currentMousePosition;
                    
                    // If we were inactive, log the period
                    if (isCurrentlyInactive && inactivityStartTime.HasValue)
                    {
                        TimeSpan inactiveDuration = DateTime.Now - inactivityStartTime.Value;
                        
                        LogToFile($"🔍 Activity resumed - Inactivity duration: {inactiveDuration.TotalSeconds:F0}s");
                        
                        // Log if inactive for more than 30 seconds (changed from 1 minute)
                        if (inactiveDuration.TotalSeconds >= 30)
                        {
                            var log = new InactivityLog
                            {
                                StartTime = inactivityStartTime.Value,
                                EndTime = DateTime.Now
                            };
                            inactivityLogs.Add(log);
                            
                            // Auto-sync to database immediately with error logging
                            string status = inactiveDuration.TotalMinutes >= 10 ? "LONG" : "SHORT";
                            
                            LogToFile($"⏸ Inactivity period: {inactiveDuration.TotalSeconds:F0}s ({inactiveDuration.TotalMinutes:F1} mins) - Status: {status}");
                            LogToFile($"📝 Attempting DB insert: ActivationKey={storedActivationKey}, Company={storedCompanyName}, User={currentUser?.Name}");
                            
                            // Sync to database synchronously to ensure it completes
                            try {
                                bool success = DatabaseHelper.InsertInactivityLog(
                                    storedActivationKey, 
                                    storedCompanyName,
                                    currentUser?.Name, 
                                    log.StartTime, 
                                    log.EndTime, 
                                    status
                                );
                                if (success) {
                                    LogToFile($"✅ SUCCESS - Inactivity logged to DB: {inactiveDuration.TotalMinutes:F1} mins ({status})");
                                } else {
                                    LogToFile($"❌ FAILED - Database returned false for inactivity insert");
                                }
                            } catch (Exception ex) {
                                LogToFile($"❌ EXCEPTION - Inactivity DB error: {ex.Message}");
                                LogToFile($"   Stack: {ex.StackTrace}");
                            }
                            
                            // Update ListView if available
                            if (inactivityListView != null && !inactivityListView.IsDisposed)
                            {
                                try
                                {
                                    if (inactivityListView.InvokeRequired)
                                    {
                                        inactivityListView.Invoke(new Action(() => AddInactivityToList(log)));
                                    }
                                    else
                                    {
                                        AddInactivityToList(log);
                                    }
                                }
                                catch (Exception ex2) {
                                    LogToFile($"ListView update error: {ex2.Message}");
                                }
                            }
                        }
                        else
                        {
                            LogToFile($"⏩ Inactivity too short ({inactiveDuration.TotalSeconds:F0}s) - Not logged");
                        }
                    }
                    
                    isCurrentlyInactive = false;
                    inactivityStartTime = null;
                    lastActivityTime = DateTime.Now;
                }
                else
                {
                    // No activity detected
                    TimeSpan idleTime = DateTime.Now - lastActivityTime;
                    
                    // Start tracking inactivity if idle for more than 30 seconds
                    if (idleTime.TotalSeconds >= 30 && !isCurrentlyInactive)
                    {
                        isCurrentlyInactive = true;
                        inactivityStartTime = lastActivityTime;
                        LogToFile($"⏸️ INACTIVITY STARTED at {lastActivityTime:HH:mm:ss} (idle for {idleTime.TotalSeconds:F0}s)");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"❌ InactivityTimer_Tick ERROR: {ex.Message}");
            }
        }
        
        private void AddInactivityToList(InactivityLog log)
        {
            if (inactivityListView == null) return;
            
            var item = new ListViewItem(new[] {
                log.StartTime.ToString("HH:mm:ss"),
                log.EndTime.ToString("HH:mm:ss"),
                log.Duration.ToString(@"hh\:mm\:ss"),
                log.Duration.TotalMinutes >= 10 ? "⚠️ Long" : "⏸️ Short"
            });
            item.ForeColor = log.Duration.TotalMinutes >= 10 ? dangerColor : warningColor;
            inactivityListView.Items.Insert(0, item); // Add to top
        }
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        private string? lastTrackedAppKey = null;
        
        private void TrackActiveApplication()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;
                
                var sb = new System.Text.StringBuilder(512);
                GetWindowText(hwnd, sb, 512);
                string windowTitle = sb.ToString();
                
                GetWindowThreadProcessId(hwnd, out uint processId);
                string appName = "";
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    appName = process.ProcessName;
                }
                catch { appName = "Unknown"; }
                
                string key = $"{appName}|{windowTitle}";
                
                // Detect browser activity and log as website visit
                if (IsBrowserProcess(appName) && !string.IsNullOrEmpty(windowTitle))
                {
                    TrackBrowserActivity(appName, windowTitle);
                }
                
                // If switched to a different app, log the previous one
                if (lastTrackedAppKey != null && lastTrackedAppKey != key && activeApps.ContainsKey(lastTrackedAppKey))
                {
                    DateTime startTime = activeApps[lastTrackedAppKey];
                    TimeSpan duration = DateTime.Now - startTime;
                    
                    // Only log if used for more than 5 seconds (reduced from 30)
                    if (duration.TotalSeconds >= 5)
                    {
                        var parts = lastTrackedAppKey.Split('|');
                        string prevAppName = parts.Length > 0 ? parts[0] : "Unknown";
                        string prevWindowTitle = parts.Length > 1 ? parts[1] : "";
                        
                        // Add to local log
                        appUsageLogs.Add(new AppUsageLog
                        {
                            StartTime = startTime,
                            EndTime = DateTime.Now,
                            AppName = prevAppName,
                            WindowTitle = prevWindowTitle
                        });
                        
                        // Log to application_logs table immediately
                        Task.Run(() => DatabaseHelper.InsertAppLog(storedActivationKey, storedCompanyName, 
                            currentUser?.Name, prevAppName, prevWindowTitle, startTime, DateTime.Now));
                    }
                    
                    activeApps.Remove(lastTrackedAppKey);
                }
                
                if (!activeApps.ContainsKey(key))
                {
                    // New app/window - log immediately
                    activeApps[key] = DateTime.Now;
                    
                    // Immediately log app start
                    Task.Run(() => DatabaseHelper.InsertAppLog(storedActivationKey, storedCompanyName, 
                        currentUser?.Name, appName, windowTitle, DateTime.Now, DateTime.Now));
                }
                
                lastTrackedAppKey = key;
            }
            catch { }
        }
        
        private bool IsBrowserProcess(string processName)
        {
            string lower = processName.ToLower();
            return lower.Contains("chrome") || lower.Contains("msedge") || lower.Contains("firefox") || 
                   lower.Contains("opera") || lower.Contains("brave") || lower.Contains("iexplore");
        }
        
        private string? lastTrackedUrl = null;
        
        private void TrackBrowserActivity(string browserName, string windowTitle)
        {
            try
            {
                // Extract URL/site info from window title
                // Browser titles usually end with " - Browser Name" or "Site Name - Page Title"
                string title = windowTitle;
                string url = "";
                string siteName = "";
                
                // Common browser title patterns
                if (title.Contains(" - YouTube"))
                {
                    siteName = "YouTube";
                    url = "https://www.youtube.com";
                }
                else if (title.Contains(" - Google Search") || title.EndsWith(" - Google"))
                {
                    siteName = "Google";
                    url = "https://www.google.com";
                }
                else if (title.Contains("Facebook"))
                {
                    siteName = "Facebook";
                    url = "https://www.facebook.com";
                }
                else if (title.Contains("Instagram"))
                {
                    siteName = "Instagram";
                    url = "https://www.instagram.com";
                }
                else if (title.Contains("Twitter") || title.Contains(" / X"))
                {
                    siteName = "Twitter/X";
                    url = "https://www.x.com";
                }
                else if (title.Contains("LinkedIn"))
                {
                    siteName = "LinkedIn";
                    url = "https://www.linkedin.com";
                }
                else if (title.Contains("WhatsApp"))
                {
                    siteName = "WhatsApp Web";
                    url = "https://web.whatsapp.com";
                }
                else if (title.Contains("Gmail") || title.Contains("Inbox"))
                {
                    siteName = "Gmail";
                    url = "https://mail.google.com";
                }
                else if (title.Contains("ChatGPT"))
                {
                    siteName = "ChatGPT";
                    url = "https://chat.openai.com";
                }
                else
                {
                    // Generic - extract from title
                    var parts = title.Split(new[] { " - ", " | ", " — " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        siteName = parts[parts.Length > 1 ? parts.Length - 2 : 0];
                        url = $"https://{siteName.ToLower().Replace(" ", "")}";
                    }
                }
                
                if (string.IsNullOrEmpty(siteName)) return;
                
                string trackKey = $"{browserName}|{siteName}|{title}";
                
                // Avoid duplicate logging within 30 seconds
                if (lastTrackedUrl == trackKey) return;
                lastTrackedUrl = trackKey;
                
                // Map browser process name to friendly name
                string friendlyBrowserName = browserName.ToLower() switch
                {
                    "chrome" => "Chrome",
                    "msedge" => "Edge",
                    "firefox" => "Firefox",
                    "opera" => "Opera",
                    "brave" => "Brave",
                    _ => browserName
                };
                
                // Add to website logs
                var webLog = new WebsiteLog
                {
                    Timestamp = DateTime.Now,
                    Browser = friendlyBrowserName,
                    Url = url,
                    Title = title,
                    Duration = TimeSpan.Zero
                };
                
                lock (websiteLogs)
                {
                    websiteLogs.Add(webLog);
                }
                
                // Immediately sync to database
                Task.Run(() => {
                    try
                    {
                        DatabaseHelper.InsertWebLog(storedActivationKey, storedCompanyName, currentUser?.Name,
                            friendlyBrowserName, url, title, CategorizeUrl(url), DateTime.Now);
                    }
                    catch { }
                });
            }
            catch { }
        }

        #region Tab Creation Methods

        private TabPage CreateControlTab()
        {
            TabPage tab = new TabPage("⚙️ CONTROL");
            tab.BackColor = contentBgColor;  // slate-800 for content separation
            
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor  // slate-800 for content separation
            };

            // Control Panel - Note: IsControlPanelBlocked returns true if blocked, so we negate for "allowed" state
            flowPanel.Controls.Add(CreateToggleCard("🎛️ Control Panel", "Block/Unblock Control Panel access",
                () => !IsControlPanelBlocked(), (enabled) => SetControlPanelAccess(!enabled)));

            // Delete Button
            flowPanel.Controls.Add(CreateToggleCard("🗑️ Delete Button", "Enable/Disable Delete key functionality",
                () => !IsDeleteKeyDisabled(), (enabled) => SetDeleteKey(enabled)));

            // Cut-Copy-Paste
            flowPanel.Controls.Add(CreateToggleCard("📋 Cut-Copy-Paste", "Enable/Disable clipboard operations",
                () => !IsClipboardDisabled(), (enabled) => SetClipboard(enabled)));

            // Administrator Account
            flowPanel.Controls.Add(CreateToggleCard("👤 Administrator Account", "Enable/Disable built-in Admin account",
                () => IsAdminAccountEnabled(), (enabled) => SetAdminAccount(enabled)));

            // Task Manager
            flowPanel.Controls.Add(CreateToggleCard("📊 Task Manager", "Enable/Disable Task Manager access",
                () => !IsTaskManagerDisabled(), (enabled) => SetTaskManager(enabled)));

            // Command Prompt
            flowPanel.Controls.Add(CreateToggleCard("💻 Command Prompt", "Enable/Disable CMD access",
                () => !IsCmdDisabled(), (enabled) => SetCommandPrompt(enabled)));

            // Removable Storage
            flowPanel.Controls.Add(CreateToggleCard("💾 Removable Storage", "Enable/Disable USB storage devices",
                () => !IsUSBStorageDisabled(), (enabled) => SetUSBStorage(enabled)));

            // Send To Menu
            flowPanel.Controls.Add(CreateToggleCard("📤 Send To Menu", "Enable/Disable Send To context menu",
                () => !IsSendToDisabled(), (enabled) => SetSendTo(enabled)));

            // PowerShell
            flowPanel.Controls.Add(CreateToggleCard("⚡ PowerShell", "Enable/Disable PowerShell access",
                () => !IsPowerShellDisabled(), (enabled) => SetPowerShell(enabled)));

            // Switch User
            flowPanel.Controls.Add(CreateToggleCard("🔄 Switch User", "Enable/Disable Switch User option",
                () => !IsSwitchUserDisabled(), (enabled) => SetSwitchUser(enabled)));

            // Registry Editor
            flowPanel.Controls.Add(CreateToggleCard("📝 Registry Editor", "Enable/Disable Registry Editor access",
                () => !IsRegeditDisabled(), (enabled) => SetRegedit(enabled)));

            // IP Changing
            flowPanel.Controls.Add(CreateToggleCard("🌐 IP Changing", "Allow/Block IP address changes",
                () => !IsIPChangeDisabled(), (enabled) => SetIPChange(enabled)));

            // New App Installation
            flowPanel.Controls.Add(CreateToggleCard("📦 App Installation", "Enable/Disable new application installation",
                () => !IsAppInstallDisabled(), (enabled) => SetAppInstall(enabled)));
            
            // Screenshot Settings Card
            flowPanel.Controls.Add(CreateScreenshotSettingsCard());

            tab.Controls.Add(flowPanel);
            return tab;
        }
        
        private Panel CreateScreenshotSettingsCard()
        {
            Panel card = new Panel
            {
                Size = new Size(340, 180),
                BackColor = cardColor,
                Margin = new Padding(10),
                Padding = new Padding(15)
            };
            
            // Title
            Label titleLabel = new Label
            {
                Text = "📷 Screenshot Settings",
                Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(15, 12),
                AutoSize = true
            };
            card.Controls.Add(titleLabel);
            
            // Description
            Label descLabel = new Label
            {
                Text = "Configure automatic screenshot capture settings",
                Font = new Font("Segoe UI", 9),
                ForeColor = secondaryTextColor,
                Location = new Point(15, 38),
                AutoSize = true
            };
            card.Controls.Add(descLabel);
            
            // Current Settings Label
            Label currentSettingsLabel = new Label
            {
                Name = "screenshotStatusLabel",
                Text = GetScreenshotStatusText(),
                Font = new Font("Segoe UI", 9),
                ForeColor = accentColor,
                Location = new Point(15, 60),
                Size = new Size(310, 40)
            };
            card.Controls.Add(currentSettingsLabel);
            
            // Configure Button
            Button configureBtn = new Button
            {
                Text = "⚙️ CONFIGURE",
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 38),
                Location = new Point(15, 110),
                Cursor = Cursors.Hand
            };
            configureBtn.FlatAppearance.BorderSize = 0;
            configureBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);  // blue-500
            configureBtn.Click += (s, e) => {
                OpenScreenshotSettings();
                // Update the status label after settings change
                currentSettingsLabel.Text = GetScreenshotStatusText();
            };
            card.Controls.Add(configureBtn);
            
            // Test Screenshot Button
            Button testBtn = new Button
            {
                Text = "📸 TEST",
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 197, 94),  // green-500
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(165, 110),
                Cursor = Cursors.Hand
            };
            testBtn.FlatAppearance.BorderSize = 0;
            testBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 163, 74);  // green-600
            testBtn.Click += async (s, e) => {
                testBtn.Enabled = false;
                testBtn.Text = "...";
                try
                {
                    await CaptureAndSaveScreenshotAsync();
                    MessageBox.Show("Screenshot captured and saved successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to capture screenshot: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    testBtn.Enabled = true;
                    testBtn.Text = "📸 TEST";
                }
            };
            card.Controls.Add(testBtn);
            
            return card;
        }
        
        private string GetScreenshotStatusText()
        {
            try
            {
                var settings = ScreenshotSettings.Load();
                if (!settings.IsEnabled || settings.StorageType == "none")
                {
                    return "Status: ❌ Disabled";
                }
                return $"Status: ✅ Enabled\nInterval: {settings.IntervalValue} {settings.IntervalUnit}, Storage: {settings.StorageType}";
            }
            catch
            {
                return "Status: Not configured";
            }
        }

        private TabPage CreateWindowsTab()
        {
            TabPage tab = new TabPage("🪟 WINDOWS");
            tab.BackColor = contentBgColor;

            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };

            // Windows Update
            flowPanel.Controls.Add(CreateToggleCard("🔄 Windows Update", "Enable/Disable Windows Update service",
                () => IsWindowsUpdateEnabled(), (enabled) => SetWindowsUpdate(enabled)));

            // User Management Card
            flowPanel.Controls.Add(CreateUserManagementCard());

            // Firewall
            flowPanel.Controls.Add(CreateToggleCard("🛡️ Windows Firewall", "Enable/Disable Windows Firewall",
                () => IsFirewallEnabled(), (enabled) => SetFirewall(enabled)));

            // Internet
            flowPanel.Controls.Add(CreateToggleCard("🌍 Internet Access", "Enable/Disable Internet connectivity",
                () => IsInternetEnabled(), (enabled) => SetInternet(enabled)));

            // Windows Defender
            flowPanel.Controls.Add(CreateToggleCard("🦠 Windows Defender", "Enable/Disable Windows Defender",
                () => IsDefenderEnabled(), (enabled) => SetDefender(enabled)));

            // System Restore
            flowPanel.Controls.Add(CreateToggleCard("⏪ System Restore", "Enable/Disable System Restore",
                () => IsSystemRestoreEnabled(), (enabled) => SetSystemRestore(enabled)));

            // AutoPlay
            flowPanel.Controls.Add(CreateToggleCard("▶️ AutoPlay", "Enable/Disable AutoPlay for all drives",
                () => !IsAutoPlayDisabled(), (enabled) => SetAutoPlay(enabled)));

            // Remote Desktop
            flowPanel.Controls.Add(CreateToggleCard("🖥️ Remote Desktop", "Enable/Disable Remote Desktop",
                () => IsRemoteDesktopEnabled(), (enabled) => SetRemoteDesktop(enabled)));

            tab.Controls.Add(flowPanel);
            return tab;
        }

        private TabPage CreatePrinterTab()
        {
            TabPage tab = new TabPage("🖨️ PRINTER");
            tab.BackColor = contentBgColor;

            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // Printer List Panel
            Panel printerListPanel = CreateCard("🖨️ Installed Printers", 0);
            
            ComboBox printerCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11),
                Height = 35
            };
            printerCombo.Name = "printerCombo";

            Button refreshBtn = CreateStyledButton("🔄 Refresh Printers", accentColor);
            refreshBtn.Click += (s, e) => RefreshPrinters(printerCombo);

            Button troubleshootBtn = CreateStyledButton("🔧 Troubleshoot Selected", warningColor);
            troubleshootBtn.Click += (s, e) => TroubleshootPrinter(printerCombo.SelectedItem?.ToString());

            Button installDriverBtn = CreateStyledButton("📥 Auto Install Driver", successColor);
            installDriverBtn.Click += (s, e) => AutoInstallPrinterDriver(printerCombo.SelectedItem?.ToString());

            Button setDefaultBtn = CreateStyledButton("⭐ Set as Default", accentColor);
            setDefaultBtn.Click += (s, e) => SetDefaultPrinter(printerCombo.SelectedItem?.ToString());

            Button testPrintBtn = CreateStyledButton("🖨️ Test Print", successColor);
            testPrintBtn.Click += (s, e) => TestPrint(printerCombo.SelectedItem?.ToString());

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };
            buttonPanel.Controls.AddRange(new Control[] { refreshBtn, troubleshootBtn, installDriverBtn, setDefaultBtn, testPrintBtn });

            printerListPanel.Controls.Add(printerCombo);
            printerListPanel.Controls.Add(buttonPanel);

            // Printer Services Panel
            Panel servicesPanel = CreateCard("🔧 Printer Services", 0);
            
            FlowLayoutPanel servicesFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            Button restartSpooler = CreateStyledButton("🔄 Restart Print Spooler", warningColor);
            restartSpooler.Width = 250;
            restartSpooler.Click += (s, e) => RestartPrintSpooler();

            Button clearQueue = CreateStyledButton("🗑️ Clear Print Queue", dangerColor);
            clearQueue.Width = 250;
            clearQueue.Click += (s, e) => ClearPrintQueue();

            Button addNetworkPrinter = CreateStyledButton("➕ Add Network Printer", accentColor);
            addNetworkPrinter.Width = 250;
            addNetworkPrinter.Click += (s, e) => AddNetworkPrinter();

            Button scanForPrinters = CreateStyledButton("🔍 Scan Network Printers", successColor);
            scanForPrinters.Width = 250;
            scanForPrinters.Click += (s, e) => ScanNetworkPrinters();

            servicesFlow.Controls.AddRange(new Control[] { restartSpooler, clearQueue, addNetworkPrinter, scanForPrinters });
            servicesPanel.Controls.Add(servicesFlow);

            mainPanel.Controls.Add(printerListPanel, 0, 0);
            mainPanel.Controls.Add(servicesPanel, 0, 1);

            tab.Controls.Add(mainPanel);

            // Initial load
            RefreshPrinters(printerCombo);

            return tab;
        }

        private TabPage CreateSecurityTab()
        {
            TabPage tab = new TabPage("🔒 SECURITY");
            tab.BackColor = contentBgColor;

            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };

            // Website Control Card - NEW FEATURE
            flowPanel.Controls.Add(CreateWebsiteControlCard());
            
            // Stream Password Card - User can set password for live stream protection
            flowPanel.Controls.Add(CreateStreamPasswordCard());

            // UAC
            flowPanel.Controls.Add(CreateToggleCard("🛡️ User Account Control (UAC)", "Enable/Disable UAC prompts",
                () => IsUACEnabled(), (enabled) => SetUAC(enabled)));

            // Lock Screen
            flowPanel.Controls.Add(CreateToggleCard("🔐 Lock Screen", "Enable/Disable Lock Screen",
                () => !IsLockScreenDisabled(), (enabled) => SetLockScreen(enabled)));

            // Guest Account
            flowPanel.Controls.Add(CreateToggleCard("👥 Guest Account", "Enable/Disable Guest Account",
                () => IsGuestAccountEnabled(), (enabled) => SetGuestAccount(enabled)));

            // Hibernate
            flowPanel.Controls.Add(CreateToggleCard("💤 Hibernate", "Enable/Disable Hibernate option",
                () => IsHibernateEnabled(), (enabled) => SetHibernate(enabled)));

            // Fast Startup
            flowPanel.Controls.Add(CreateToggleCard("⚡ Fast Startup", "Enable/Disable Fast Startup",
                () => IsFastStartupEnabled(), (enabled) => SetFastStartup(enabled)));

            // BitLocker (info only)
            flowPanel.Controls.Add(CreateInfoCard("🔐 BitLocker Status", GetBitLockerStatus()));

            // Windows Hello
            flowPanel.Controls.Add(CreateToggleCard("👋 Windows Hello", "Enable/Disable Windows Hello",
                () => IsWindowsHelloEnabled(), (enabled) => SetWindowsHello(enabled)));

            // Screen Saver Password
            flowPanel.Controls.Add(CreateToggleCard("🖼️ Screensaver Password", "Require password on screensaver resume",
                () => IsScreensaverPasswordEnabled(), (enabled) => SetScreensaverPassword(enabled)));

            tab.Controls.Add(flowPanel);
            return tab;
        }

        private TabPage CreateNetworkTab()
        {
            TabPage tab = new TabPage("🌐 NETWORK");
            tab.BackColor = contentBgColor;

            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };

            // Network Discovery
            flowPanel.Controls.Add(CreateToggleCard("🔍 Network Discovery", "Enable/Disable Network Discovery",
                () => IsNetworkDiscoveryEnabled(), (enabled) => SetNetworkDiscovery(enabled)));

            // File Sharing
            flowPanel.Controls.Add(CreateToggleCard("📁 File Sharing", "Enable/Disable File and Printer Sharing",
                () => IsFileSharingEnabled(), (enabled) => SetFileSharing(enabled)));

            // WiFi
            flowPanel.Controls.Add(CreateToggleCard("📶 WiFi Adapter", "Enable/Disable WiFi adapter",
                () => IsWiFiEnabled(), (enabled) => SetWiFi(enabled)));

            // Bluetooth
            flowPanel.Controls.Add(CreateToggleCard("🔵 Bluetooth", "Enable/Disable Bluetooth adapter",
                () => IsBluetoothEnabled(), (enabled) => SetBluetooth(enabled)));

            // Network Info Card
            flowPanel.Controls.Add(CreateInfoCard("📊 Network Information", GetNetworkInfo()));

            // Action Buttons Panel
            Panel actionPanel = CreateCard("🔧 Network Actions", 120);
            FlowLayoutPanel actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            Button flushDns = CreateStyledButton("🔄 Flush DNS", accentColor);
            flushDns.Click += (s, e) => FlushDNS();

            Button resetNetwork = CreateStyledButton("⚡ Reset Network", warningColor);
            resetNetwork.Click += (s, e) => ResetNetwork();

            Button showConnections = CreateStyledButton("📊 Show Connections", successColor);
            showConnections.Click += (s, e) => ShowNetworkConnections();

            actionFlow.Controls.AddRange(new Control[] { flushDns, resetNetwork, showConnections });
            actionPanel.Controls.Add(actionFlow);
            flowPanel.Controls.Add(actionPanel);

            tab.Controls.Add(flowPanel);
            return tab;
        }

        private TabPage CreateAdvancedTab()
        {
            TabPage tab = new TabPage("⚙️ ADVANCED");
            tab.BackColor = contentBgColor;

            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };

            // Windows Script Host
            flowPanel.Controls.Add(CreateToggleCard("📜 Windows Script Host", "Enable/Disable Windows Script Host",
                () => !IsWSHDisabled(), (enabled) => SetWSH(enabled)));

            // MSI Installer
            flowPanel.Controls.Add(CreateToggleCard("📦 Windows Installer", "Enable/Disable Windows Installer",
                () => !IsMSIDisabled(), (enabled) => SetMSI(enabled)));

            // Context Menu
            flowPanel.Controls.Add(CreateToggleCard("📋 Right-Click Context Menu", "Enable/Disable Explorer context menu",
                () => !IsContextMenuDisabled(), (enabled) => SetContextMenu(enabled)));

            // Run Dialog
            flowPanel.Controls.Add(CreateToggleCard("▶️ Run Dialog (Win+R)", "Enable/Disable Run dialog",
                () => !IsRunDialogDisabled(), (enabled) => SetRunDialog(enabled)));

            // Search
            flowPanel.Controls.Add(CreateToggleCard("🔍 Windows Search", "Enable/Disable Windows Search",
                () => IsWindowsSearchEnabled(), (enabled) => SetWindowsSearch(enabled)));

            // Cortana
            flowPanel.Controls.Add(CreateToggleCard("🎤 Cortana", "Enable/Disable Cortana",
                () => !IsCortanaDisabled(), (enabled) => SetCortana(enabled)));

            // Notifications
            flowPanel.Controls.Add(CreateToggleCard("🔔 Notifications", "Enable/Disable system notifications",
                () => !IsNotificationsDisabled(), (enabled) => SetNotifications(enabled)));

            // Telemetry
            flowPanel.Controls.Add(CreateToggleCard("📊 Telemetry", "Enable/Disable Windows Telemetry",
                () => !IsTelemetryDisabled(), (enabled) => SetTelemetry(enabled)));

            // Action Center
            flowPanel.Controls.Add(CreateToggleCard("📱 Action Center", "Enable/Disable Action Center",
                () => !IsActionCenterDisabled(), (enabled) => SetActionCenter(enabled)));

            // OneDrive
            flowPanel.Controls.Add(CreateToggleCard("☁️ OneDrive", "Enable/Disable OneDrive integration",
                () => !IsOneDriveDisabled(), (enabled) => SetOneDrive(enabled)));

            // Game Bar
            flowPanel.Controls.Add(CreateToggleCard("🎮 Game Bar", "Enable/Disable Xbox Game Bar",
                () => !IsGameBarDisabled(), (enabled) => SetGameBar(enabled)));

            // Thumb.db
            flowPanel.Controls.Add(CreateToggleCard("🖼️ Thumbs.db Creation", "Enable/Disable thumbnail cache files",
                () => !IsThumbsDbDisabled(), (enabled) => SetThumbsDb(enabled)));

            tab.Controls.Add(flowPanel);
            return tab;
        }

        #endregion

        #region UI Helper Methods

        private Panel CreateCard(string title, int height)
        {
            Panel card = new Panel
            {
                Width = 350,
                Height = height > 0 ? height : 200,
                Margin = new Padding(10),
                BackColor = cardColor,
                Padding = new Padding(15)
            };

            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textColor,
                Dock = DockStyle.Top,
                Height = 30
            };

            card.Controls.Add(titleLabel);
            return card;
        }
        
        /// <summary>
        /// Creates a card for setting Stream Password (protection for live view access)
        /// Company-specific password with change password flow
        /// </summary>
        private Panel CreateStreamPasswordCard()
        {
            Panel card = new Panel
            {
                Width = 740,
                Height = 280,
                Margin = new Padding(12),
                BackColor = cardColor,
                Padding = new Padding(18)
            };
            
            // Round corners
            card.Paint += (s, e) => {
                using (var path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(cardColor))
                        e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(Color.FromArgb(55, 65, 81), 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            
            // Title
            Label titleLabel = new Label
            {
                Text = "🔐 Stream Password Protection",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            card.Controls.Add(titleLabel);
            
            // Company name indicator
            Label lblCompany = new Label
            {
                Text = $"Company: {storedCompanyName ?? "Unknown"}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(129, 140, 248),  // indigo-400
                Location = new Point(350, 18),
                AutoSize = true
            };
            card.Controls.Add(lblCompany);
            
            // Description
            Label descLabel = new Label
            {
                Text = "Set a password for your company to protect live stream access. This password is required when viewing any system in your company.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = secondaryTextColor,
                Location = new Point(20, 42),
                Size = new Size(680, 20),
                AutoSize = false
            };
            card.Controls.Add(descLabel);
            
            // Don't check database during initialization - assume no password initially
            // The card will check when user clicks the button
            bool hasExistingPassword = false;
            
            int yPos = 70;
            
            // Previous Password (only shown if password already exists)
            Label lblPrevPwd = new Label
            {
                Text = "Previous Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                AutoSize = true,
                Visible = hasExistingPassword
            };
            card.Controls.Add(lblPrevPwd);
            
            TextBox txtPrevPassword = new TextBox
            {
                Location = new Point(180, yPos - 3),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 35, 44),
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
                Visible = hasExistingPassword
            };
            card.Controls.Add(txtPrevPassword);
            
            if (hasExistingPassword) yPos += 35;
            
            // New Password
            Label lblNewPwd = new Label
            {
                Text = hasExistingPassword ? "New Password:" : "Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                AutoSize = true
            };
            card.Controls.Add(lblNewPwd);
            
            TextBox txtNewPassword = new TextBox
            {
                Location = new Point(180, yPos - 3),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 35, 44),
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };
            card.Controls.Add(txtNewPassword);
            
            yPos += 35;
            
            // Confirm Password
            Label lblConfirmPwd = new Label
            {
                Text = "Confirm Password:",
                Font = new Font("Segoe UI", 10f),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                AutoSize = true
            };
            card.Controls.Add(lblConfirmPwd);
            
            TextBox txtConfirmPassword = new TextBox
            {
                Location = new Point(180, yPos - 3),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(30, 35, 44),
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };
            card.Controls.Add(txtConfirmPassword);
            
            // Show/Hide password checkbox
            CheckBox chkShowPwd = new CheckBox
            {
                Text = "Show passwords",
                Font = new Font("Segoe UI", 9f),
                ForeColor = secondaryTextColor,
                Location = new Point(420, yPos),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            chkShowPwd.CheckedChanged += (s, e) => {
                txtPrevPassword.UseSystemPasswordChar = !chkShowPwd.Checked;
                txtNewPassword.UseSystemPasswordChar = !chkShowPwd.Checked;
                txtConfirmPassword.UseSystemPasswordChar = !chkShowPwd.Checked;
            };
            card.Controls.Add(chkShowPwd);
            
            yPos += 35;
            
            // Status label
            Label lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = successColor,
                Location = new Point(20, yPos),
                Size = new Size(500, 20),
                AutoSize = false
            };
            card.Controls.Add(lblStatus);
            
            // Check current status
            lblStatus.Text = hasExistingPassword 
                ? "✅ Stream password is SET for this company" 
                : "⚠️ First time setup - Set a password for your company";
            lblStatus.ForeColor = hasExistingPassword ? successColor : warningColor;
            
            yPos += 30;
            
            // Save button
            Button btnSave = new Button
            {
                Text = hasExistingPassword ? "🔄 Change Password" : "💾 Set Password",
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 38),
                Location = new Point(20, yPos),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                // Validate inputs
                string newPassword = txtNewPassword.Text.Trim();
                string confirmPassword = txtConfirmPassword.Text.Trim();
                
                if (string.IsNullOrEmpty(newPassword))
                {
                    MessageBox.Show("Please enter a password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (newPassword.Length < 4)
                {
                    MessageBox.Show("Password must be at least 4 characters!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("Passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }
                
                // If password already exists, verify previous password
                if (hasExistingPassword)
                {
                    string prevPassword = txtPrevPassword.Text.Trim();
                    if (string.IsNullOrEmpty(prevPassword))
                    {
                        MessageBox.Show("Please enter your previous password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPrevPassword.Focus();
                        return;
                    }
                    
                    // Verify previous password
                    if (!RemoteViewingHelper.VerifyStreamPassword(storedActivationKey ?? "", prevPassword))
                    {
                        MessageBox.Show("Previous password is incorrect!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtPrevPassword.Focus();
                        txtPrevPassword.SelectAll();
                        return;
                    }
                }
                
                // Save new password (using activation key for company-specific)
                if (RemoteViewingHelper.SaveStreamPassword(storedActivationKey ?? systemId, newPassword))
                {
                    lblStatus.Text = "✅ Stream password saved successfully!";
                    lblStatus.ForeColor = successColor;
                    
                    // Clear fields
                    txtPrevPassword.Text = "";
                    txtNewPassword.Text = "";
                    txtConfirmPassword.Text = "";
                    
                    // Update UI for password change mode
                    if (!hasExistingPassword)
                    {
                        MessageBox.Show("Stream password has been set for your company!\n\nAnyone trying to view systems in your company will need to enter this password.", 
                            "Password Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Stream password has been changed successfully!", 
                            "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    lblStatus.Text = "❌ Failed to save password";
                    lblStatus.ForeColor = dangerColor;
                }
            };
            card.Controls.Add(btnSave);
            
            return card;
        }

        private Panel CreateWebsiteControlCard()
        {
            Panel card = new Panel
            {
                Width = 740,
                Height = 450,
                Margin = new Padding(12),
                BackColor = cardColor,
                Padding = new Padding(18)
            };
            
            // Add rounded corners
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12))
                {
                    using (var brush = new SolidBrush(card.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new Pen(borderColor, 1.5f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "🌐 Website Control (Whitelist / Blocklist)",
                Font = new Font("Segoe UI Semibold", 14, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(titleLabel);

            // Mode selection
            Label modeLabel = new Label
            {
                Text = "Mode:",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(20, 50),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(modeLabel);

            RadioButton whitelistRadio = new RadioButton
            {
                Text = "Whitelist (Allow only listed sites)",
                Font = new Font("Segoe UI", 10),
                ForeColor = successColor,
                Location = new Point(80, 48),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(whitelistRadio);

            RadioButton blocklistRadio = new RadioButton
            {
                Text = "Blocklist (Block only listed sites)",
                Font = new Font("Segoe UI", 10),
                ForeColor = dangerColor,
                Location = new Point(320, 48),
                AutoSize = true,
                BackColor = Color.Transparent,
                Checked = true
            };
            card.Controls.Add(blocklistRadio);

            RadioButton disabledRadio = new RadioButton
            {
                Text = "Disabled",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(560, 48),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(disabledRadio);

            // URL input
            Label urlLabel = new Label
            {
                Text = "Enter URL to add:",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(20, 85),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(urlLabel);

            TextBox urlTextBox = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                Location = new Point(20, 110),
                Width = 500,
                Height = 35,
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            urlTextBox.Text = "example.com";
            urlTextBox.GotFocus += (s, e) => { if (urlTextBox.Text == "example.com") urlTextBox.Text = ""; };
            urlTextBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(urlTextBox.Text)) urlTextBox.Text = "example.com"; };
            card.Controls.Add(urlTextBox);

            // Add button
            Button addButton = new Button
            {
                Text = "➕ ADD",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(530, 108),
                Width = 90,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = successColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            addButton.FlatAppearance.BorderSize = 0;
            card.Controls.Add(addButton);

            // Remove button
            Button removeButton = new Button
            {
                Text = "➖ REMOVE",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(630, 108),
                Width = 90,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = dangerColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            removeButton.FlatAppearance.BorderSize = 0;
            card.Controls.Add(removeButton);

            // Website list
            ListView websiteListView = new ListView
            {
                Location = new Point(20, 155),
                Width = 700,
                Height = 230,
                View = View.Details,
                FullRowSelect = true,
                BackColor = bgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            websiteListView.Columns.Add("URL", 400);
            websiteListView.Columns.Add("Type", 140);
            websiteListView.Columns.Add("Added", 140);
            card.Controls.Add(websiteListView);

            // Apply button
            Button applyButton = new Button
            {
                Text = "✅ APPLY RULES",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 400),
                Width = 180,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            applyButton.FlatAppearance.BorderSize = 0;
            card.Controls.Add(applyButton);

            // Clear all button
            Button clearButton = new Button
            {
                Text = "🗑️ CLEAR ALL",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(210, 400),
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = warningColor,
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            clearButton.FlatAppearance.BorderSize = 0;
            card.Controls.Add(clearButton);

            // Status label
            Label statusLabel = new Label
            {
                Text = "Status: Not applied",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(380, 410),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(statusLabel);

            // Load existing rules
            LoadWebsiteRules(websiteListView, whitelistRadio, blocklistRadio, disabledRadio, statusLabel);

            // Add button click
            addButton.Click += (s, e) =>
            {
                string url = urlTextBox.Text.Trim().ToLower();
                if (string.IsNullOrEmpty(url) || url == "example.com")
                {
                    MessageBox.Show("Please enter a valid URL", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Remove http:// or https:// if present
                url = url.Replace("http://", "").Replace("https://", "").TrimEnd('/');
                
                // Check if already exists
                foreach (ListViewItem item in websiteListView.Items)
                {
                    if (item.Text.ToLower() == url)
                    {
                        MessageBox.Show("URL already in the list", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                string type = whitelistRadio.Checked ? "Whitelist" : "Blocklist";
                ListViewItem newItem = new ListViewItem(new[] { url, type, DateTime.Now.ToString("yyyy-MM-dd HH:mm") });
                newItem.ForeColor = whitelistRadio.Checked ? successColor : dangerColor;
                websiteListView.Items.Add(newItem);
                urlTextBox.Text = "example.com";
                statusLabel.Text = "Status: Changes pending - Click APPLY";
                statusLabel.ForeColor = warningColor;
            };

            // Remove button click
            removeButton.Click += (s, e) =>
            {
                if (websiteListView.SelectedItems.Count > 0)
                {
                    websiteListView.Items.Remove(websiteListView.SelectedItems[0]);
                    statusLabel.Text = "Status: Changes pending - Click APPLY";
                    statusLabel.ForeColor = warningColor;
                }
                else
                {
                    MessageBox.Show("Please select a URL to remove", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // Clear all button click
            clearButton.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to clear all website rules?", "Confirm Clear", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    websiteListView.Items.Clear();
                    ClearWebsiteBlocking();
                    statusLabel.Text = "Status: All rules cleared";
                    statusLabel.ForeColor = successColor;
                }
            };

            // Apply button click
            applyButton.Click += (s, e) =>
            {
                try
                {
                    // Collect URLs by type
                    List<string> whitelistUrls = new List<string>();
                    List<string> blocklistUrls = new List<string>();
                    
                    foreach (ListViewItem item in websiteListView.Items)
                    {
                        string type = item.SubItems[1].Text;
                        if (type == "Whitelist")
                            whitelistUrls.Add(item.Text);
                        else if (type == "Blocklist")
                            blocklistUrls.Add(item.Text);
                    }

                    if (disabledRadio.Checked)
                    {
                        ClearWebsiteBlocking();
                        statusLabel.Text = "Status: Website control disabled";
                        statusLabel.ForeColor = secondaryTextColor;
                    }
                    else if (whitelistRadio.Checked)
                    {
                        // In whitelist mode: Allow only whitelisted sites, block everything else
                        if (whitelistUrls.Count == 0)
                        {
                            MessageBox.Show("Please add at least one URL to whitelist.\n\nIn Whitelist mode, only whitelisted URLs will work.", 
                                "No Whitelist URLs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        ApplyWhitelistMode(whitelistUrls);
                        SaveWebsiteRules(websiteListView, "whitelist");
                        statusLabel.Text = $"Status: Whitelist active ({whitelistUrls.Count} sites allowed, all others blocked)";
                        statusLabel.ForeColor = successColor;
                    }
                    else
                    {
                        // In blocklist mode: Block only blocklisted sites
                        if (blocklistUrls.Count == 0)
                        {
                            MessageBox.Show("Please add at least one URL to blocklist.\n\nIn Blocklist mode, only blocklisted URLs will be blocked.", 
                                "No Blocklist URLs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        ApplyBlocklistMode(blocklistUrls);
                        SaveWebsiteRules(websiteListView, "blocklist");
                        statusLabel.Text = $"Status: Blocklist active ({blocklistUrls.Count} sites blocked)";
                        statusLabel.ForeColor = dangerColor;
                    }

                    MessageBox.Show("Website rules applied successfully!\n\nNote: Changes may require browser restart to take effect.", 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error applying rules: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogToFile($"Website blocking error: {ex.Message}");
                }
            };

            // Mode change handlers - DO NOT change existing items, just update status
            whitelistRadio.CheckedChanged += (s, e) =>
            {
                if (whitelistRadio.Checked)
                {
                    // Don't change existing items - they keep their own type
                    statusLabel.Text = "Status: Whitelist mode - Only whitelisted URLs will work";
                    statusLabel.ForeColor = successColor;
                }
            };

            blocklistRadio.CheckedChanged += (s, e) =>
            {
                if (blocklistRadio.Checked)
                {
                    // Don't change existing items - they keep their own type
                    statusLabel.Text = "Status: Blocklist mode - Blocklisted URLs will be blocked";
                    statusLabel.ForeColor = dangerColor;
                }
            };
            
            disabledRadio.CheckedChanged += (s, e) =>
            {
                if (disabledRadio.Checked)
                {
                    statusLabel.Text = "Status: Website control will be disabled - Click APPLY";
                    statusLabel.ForeColor = warningColor;
                }
            };

            return card;
        }

        private void LoadWebsiteRules(ListView listView, RadioButton whitelist, RadioButton blocklist, RadioButton disabled, Label status)
        {
            try
            {
                string rulesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "DesktopController", "website_rules.txt");
                
                if (File.Exists(rulesPath))
                {
                    string[] lines = File.ReadAllLines(rulesPath);
                    if (lines.Length > 0)
                    {
                        string mode = lines[0].Trim().ToLower();
                        if (mode == "whitelist") whitelist.Checked = true;
                        else if (mode == "blocklist") blocklist.Checked = true;
                        else disabled.Checked = true;

                        int whitelistCount = 0;
                        int blocklistCount = 0;

                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(lines[i]))
                            {
                                string[] parts = lines[i].Split('|');
                                string url = parts[0];
                                // Load saved type, or default based on mode
                                string type = parts.Length > 2 ? parts[2] : (mode == "whitelist" ? "Whitelist" : "Blocklist");
                                string date = parts.Length > 1 ? parts[1] : DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                                
                                ListViewItem item = new ListViewItem(new[] { url, type, date });
                                item.ForeColor = type == "Whitelist" ? successColor : dangerColor;
                                listView.Items.Add(item);
                                
                                if (type == "Whitelist") whitelistCount++;
                                else blocklistCount++;
                            }
                        }
                        
                        status.Text = $"Status: {whitelistCount} whitelisted, {blocklistCount} blocklisted";
                        status.ForeColor = mode == "whitelist" ? successColor : dangerColor;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading website rules: {ex.Message}");
            }
        }

        private void SaveWebsiteRules(ListView listView, string mode)
        {
            try
            {
                string rulesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController");
                Directory.CreateDirectory(rulesDir);
                string rulesPath = Path.Combine(rulesDir, "website_rules.txt");

                List<string> lines = new List<string> { mode };
                foreach (ListViewItem item in listView.Items)
                {
                    // Save: URL|Date|Type
                    lines.Add($"{item.Text}|{item.SubItems[2].Text}|{item.SubItems[1].Text}");
                }
                File.WriteAllLines(rulesPath, lines);
                LogToFile($"Saved {listView.Items.Count} website rules in {mode} mode");
            }
            catch (Exception ex)
            {
                LogToFile($"Error saving website rules: {ex.Message}");
            }
        }

        private void ApplyBlocklistMode(List<string> urls)
        {
            // Block sites by adding them to Windows hosts file
            string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
            
            try
            {
                // Read existing hosts file
                List<string> existingLines = new List<string>();
                if (File.Exists(hostsPath))
                {
                    existingLines = File.ReadAllLines(hostsPath).ToList();
                }

                // Remove any previous DesktopController entries
                existingLines.RemoveAll(line => line.Contains("# DesktopController"));

                // Add new blocked sites
                existingLines.Add("");
                existingLines.Add("# DesktopController Blocked Sites - DO NOT EDIT MANUALLY");
                foreach (string url in urls)
                {
                    existingLines.Add($"127.0.0.1 {url} # DesktopController");
                    existingLines.Add($"127.0.0.1 www.{url} # DesktopController");
                }

                File.WriteAllLines(hostsPath, existingLines);
                
                // Flush DNS cache
                FlushDNSCache();
                
                LogToFile($"Applied blocklist: {urls.Count} sites blocked");
            }
            catch (Exception ex)
            {
                LogToFile($"Error applying blocklist: {ex.Message}");
                throw;
            }
        }

        private void ApplyWhitelistMode(List<string> allowedUrls)
        {
            // TRUE WHITELIST: Block all browser traffic, then ALLOW only whitelisted IPs
            // Uses priority-based rules: BLOCK all (low priority), ALLOW whitelist (high priority)
            
            try
            {
                // Step 1: Remove any existing rules
                RemoveFirewallRules();
                ClearHostsFile();
                
                // Step 2: Resolve all whitelisted domains to IPs
                HashSet<string> allowedIPs = new HashSet<string>();
                
                // Always allow localhost and local network
                allowedIPs.Add("127.0.0.1");
                allowedIPs.Add("192.168.0.0/16");
                allowedIPs.Add("10.0.0.0/8");
                allowedIPs.Add("172.16.0.0/12");
                
                // Allow DNS servers for resolution
                allowedIPs.Add("8.8.8.8");
                allowedIPs.Add("8.8.4.4");
                allowedIPs.Add("1.1.1.1");
                allowedIPs.Add("1.0.0.1");
                
                foreach (string url in allowedUrls)
                {
                    string domain = url.Trim().ToLower().Replace("http://", "").Replace("https://", "").TrimEnd('/');
                    string baseDomain = GetBaseDomain(domain);
                    
                    // Resolve main domain and many variants
                    string[] variants = new string[] { 
                        domain, "www." + domain, "m." + domain, "mail." + domain, 
                        "accounts." + domain, "login." + domain, "api." + domain,
                        "static." + domain, "cdn." + domain, "s." + domain
                    };
                    foreach (string variant in variants)
                    {
                        ResolveAndAddIPs(variant, allowedIPs);
                    }
                    
                    // Add CDNs for this site
                    if (siteCDNMapping.ContainsKey(baseDomain))
                    {
                        foreach (string cdn in siteCDNMapping[baseDomain])
                        {
                            ResolveAndAddIPs(cdn, allowedIPs);
                            ResolveAndAddIPs("www." + cdn, allowedIPs);
                            ResolveAndAddIPs("s." + cdn, allowedIPs);
                            ResolveAndAddIPs("static." + cdn, allowedIPs);
                        }
                    }
                }
                
                // Step 3: All browser executables (including helper processes)
                List<string> allBrowserPaths = new List<string>();
                
                // Chrome paths
                allBrowserPaths.Add(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
                allBrowserPaths.Add(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe");
                allBrowserPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe"));
                
                // Edge paths (including ALL edge executables)
                allBrowserPaths.Add(@"C:\Program Files\Microsoft\Edge\Application\msedge.exe");
                allBrowserPaths.Add(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");
                allBrowserPaths.Add(@"C:\Program Files\Microsoft\Edge\WebView2\msedgewebview2.exe");
                allBrowserPaths.Add(@"C:\Program Files (x86)\Microsoft\Edge\WebView2\msedgewebview2.exe");
                
                // Firefox paths
                allBrowserPaths.Add(@"C:\Program Files\Mozilla Firefox\firefox.exe");
                allBrowserPaths.Add(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
                
                // Brave paths
                allBrowserPaths.Add(@"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe");
                allBrowserPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BraveSoftware\Brave-Browser\Application\brave.exe"));
                
                // Opera paths
                allBrowserPaths.Add(@"C:\Program Files\Opera\opera.exe");
                allBrowserPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Opera\opera.exe"));
                
                string ipList = string.Join(",", allowedIPs);
                int browsersBlocked = 0;
                
                // Step 4: For each browser, create BLOCK rule (priority 1) then ALLOW rule for whitelist IPs (priority 0 - higher)
                // In Windows Firewall, LOWER number = HIGHER priority. But BLOCK still wins.
                // So we use: BLOCK ALL first, then use remoteip filter in ALLOW rule
                
                // Actually, Windows Firewall doesn't have true priority for conflicting rules.
                // Solution: Don't change default policy. Instead:
                // 1. Create BLOCK rule for browser to ANY IP
                // 2. Create ALLOW rule for browser to specific IPs BEFORE the block
                // Windows Firewall processes ALLOW rules that match before BLOCK rules when they have the same specificity
                // BUT when we specify remoteip in ALLOW, it becomes more specific than a general BLOCK
                
                foreach (string browserPath in allBrowserPaths)
                {
                    if (File.Exists(browserPath))
                    {
                        string browserName = Path.GetFileNameWithoutExtension(browserPath);
                        string safeName = browserName.Replace(" ", "_");
                        
                        // First: Create ALLOW rule for whitelisted IPs (more specific)
                        RunNetshDirect($"advfirewall firewall add rule name=\"DC_WhiteAllow_{safeName}\" dir=out action=allow program=\"{browserPath}\" remoteip={ipList} enable=yes");
                        
                        // Second: Create BLOCK rule for ALL other IPs (less specific because it has no remoteip filter... wait that's wrong)
                        // Actually we need to block all IPs EXCEPT the whitelist
                        // Solution: Use remoteip=any and rely on ALLOW rule being processed first
                        // This doesn't work in Windows Firewall - BLOCK wins
                        
                        // NEW APPROACH: Block all, but the Allow rule with specific IPs should work
                        // Let's try setting ALLOW first with specific IPs
                        // Then BLOCK all - but this will still block everything
                        
                        // REAL SOLUTION: Use Windows Filtering Platform (WFP) or just hosts file
                        // For now, let's use a PROXY approach via hosts file
                        
                        browsersBlocked++;
                        LogToFile($"Created rules for {browserName}");
                    }
                }
                
                // Step 5: Since Windows Firewall BLOCK overrides ALLOW, we need different approach
                // Use HOSTS FILE to block all sites, then remove whitelisted sites from hosts
                // This is the OPPOSITE of blocklist mode
                
                // But we can't block "all" sites in hosts file - too many
                // 
                // ALTERNATIVE: Use Windows Firewall to block browsers completely,
                // Then use a LOCAL PROXY for whitelisted sites only
                //
                // SIMPLEST WORKING APPROACH:
                // 1. Set firewall default to ALLOW (normal)
                // 2. Create BLOCK rules for browsers to port 80,443
                // 3. Create ALLOW rules for browsers to whitelisted IPs on port 80,443 (these run BEFORE due to specificity)
                
                // Clear any previous policy changes
                RunNetshDirect("advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound");
                
                // CRITICAL: Create ALLOW rules BEFORE changing policy to block
                // This ensures app and database work even when outbound is blocked
                
                // Allow PostgreSQL database connection FIRST (before policy change)
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_PostgreSQL\" dir=out action=allow protocol=TCP remoteport=9095 remoteip=72.61.170.243 enable=yes");
                
                // Allow our app FIRST (before policy change) - use process path for single-file apps
                string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                {
                    RunNetshDirect($"advfirewall firewall add rule name=\"DC_Sys_App\" dir=out action=allow program=\"{appPath}\" enable=yes");
                }
                
                // Allow ALL system processes and services BEFORE policy change
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_DNS\" dir=out action=allow protocol=UDP remoteport=53 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_DNS_TCP\" dir=out action=allow protocol=TCP remoteport=53 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_DHCP\" dir=out action=allow protocol=UDP localport=68 remoteport=67 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_NTP\" dir=out action=allow protocol=UDP remoteport=123 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Local\" dir=out action=allow remoteip=LocalSubnet enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Loopback\" dir=out action=allow remoteip=127.0.0.1 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Private1\" dir=out action=allow remoteip=192.168.0.0/16 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Private2\" dir=out action=allow remoteip=10.0.0.0/8 enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Private3\" dir=out action=allow remoteip=172.16.0.0/12 enable=yes");
                
                // Allow Windows services
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Svchost\" dir=out action=allow program=\"%SystemRoot%\\System32\\svchost.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Lsass\" dir=out action=allow program=\"%SystemRoot%\\System32\\lsass.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Services\" dir=out action=allow program=\"%SystemRoot%\\System32\\services.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Wuauclt\" dir=out action=allow program=\"%SystemRoot%\\System32\\wuauclt.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Spoolsv\" dir=out action=allow program=\"%SystemRoot%\\System32\\spoolsv.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_SearchIndexer\" dir=out action=allow program=\"%SystemRoot%\\System32\\SearchIndexer.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Taskhostw\" dir=out action=allow program=\"%SystemRoot%\\System32\\taskhostw.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Dllhost\" dir=out action=allow program=\"%SystemRoot%\\System32\\dllhost.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Conhost\" dir=out action=allow program=\"%SystemRoot%\\System32\\conhost.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_RuntimeBroker\" dir=out action=allow program=\"%SystemRoot%\\System32\\RuntimeBroker.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Sihost\" dir=out action=allow program=\"%SystemRoot%\\System32\\sihost.exe\" enable=yes");
                RunNetshDirect("advfirewall firewall add rule name=\"DC_Sys_Dotnet\" dir=out action=allow program=\"%ProgramFiles%\\dotnet\\dotnet.exe\" enable=yes");
                
                // Create ALLOW rules for browsers ONLY to whitelisted IPs (before policy change)
                browsersBlocked = 0;
                foreach (string browserPath in allBrowserPaths)
                {
                    if (File.Exists(browserPath))
                    {
                        string browserName = Path.GetFileNameWithoutExtension(browserPath);
                        string safeName = browserName.Replace(" ", "_");
                        
                        // Allow browser ONLY to whitelisted IPs
                        RunNetshDirect($"advfirewall firewall add rule name=\"DC_Browser_{safeName}\" dir=out action=allow program=\"{browserPath}\" remoteip={ipList} enable=yes");
                        
                        browsersBlocked++;
                        LogToFile($"Browser {browserName} allowed to {allowedIPs.Count} IPs only");
                    }
                }
                
                // NOW change policy to BLOCK outbound (all ALLOW rules already in place)
                RunNetshDirect("advfirewall set allprofiles firewallpolicy blockinbound,blockoutbound");
                
                // Step 6: Flush DNS
                FlushDNSCache();
                
                // Save config
                string rulesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DesktopController");
                Directory.CreateDirectory(rulesDir);
                File.WriteAllLines(Path.Combine(rulesDir, "whitelist_active.txt"), allowedUrls);
                File.WriteAllLines(Path.Combine(rulesDir, "whitelist_ips.txt"), allowedIPs.ToList());
                File.WriteAllText(Path.Combine(rulesDir, "firewall_blocked.txt"), "true");
                
                LogToFile($"TRUE WHITELIST: {allowedUrls.Count} domains -> {allowedIPs.Count} IPs. {browsersBlocked} browsers restricted.");
                
                MessageBox.Show($"TRUE Whitelist Applied!\n\n✓ {allowedUrls.Count} sites whitelisted\n✓ {allowedIPs.Count} IPs allowed\n✓ {browsersBlocked} browsers restricted\n✓ Default outbound: BLOCKED\n✓ System services: ALLOWED\n\nONLY whitelisted sites will work in browsers.\nRestart your browser for changes.", 
                    "TRUE Whitelist Active", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Restore firewall on error
                RunNetshDirect("advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound");
                
                MessageBox.Show($"Error: {ex.Message}\n\nFirewall restored to default.\nMake sure app is running as Administrator.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogToFile($"Whitelist error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        private void RunNetshDirect(string arguments)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process?.WaitForExit(15000);
                    string output = process?.StandardOutput.ReadToEnd() ?? "";
                    string error = process?.StandardError.ReadToEnd() ?? "";
                    
                    if (!string.IsNullOrEmpty(error))
                        LogToFile($"netsh error: {error}");
                    
                    LogToFile($"netsh {arguments} -> {(process?.ExitCode == 0 ? "OK" : "FAILED")}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"RunNetshDirect failed: {ex.Message}");
            }
        }
        
        // CDN mapping for popular sites
        private Dictionary<string, string[]> siteCDNMapping = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "google.com", new[] { "googleapis.com", "gstatic.com", "googleusercontent.com", "google.co.in", "ggpht.com", "googlevideo.com", "googletagmanager.com", "google-analytics.com", "googlesyndication.com", "googleadservices.com", "gvt1.com", "gvt2.com" } },
            { "gmail.com", new[] { 
                // Core Google domains
                "google.com", "googleapis.com", "gstatic.com", "googleusercontent.com", "googlemail.com",
                // Gmail specific
                "mail.google.com", "inbox.google.com", "chat.google.com",
                // Auth domains
                "accounts.google.com", "ssl.gstatic.com", "clients1.google.com", "clients2.google.com", 
                "clients3.google.com", "clients4.google.com", "clients5.google.com", "clients6.google.com",
                // APIs and services
                "www.googleapis.com", "content.googleapis.com", "fonts.googleapis.com", "fonts.gstatic.com",
                "lh3.googleusercontent.com", "lh4.googleusercontent.com", "lh5.googleusercontent.com",
                // Play services for attachments
                "play.google.com", "play.googleapis.com",
                // Other Google services
                "ogs.google.com", "apis.google.com", "www.google.com", "encrypted.google.com",
                "plus.google.com", "drive.google.com", "docs.google.com",
                // CDN and static
                "ggpht.com", "gvt1.com", "gvt2.com", "gvt3.com", "googlehosted.com",
                // Recaptcha (for login)
                "recaptcha.net", "www.recaptcha.net", "google.com/recaptcha"
            } },
            { "yahoo.com", new[] { "yimg.com", "yahooapis.com", "yahoo.net", "s.yimg.com", "l.yimg.com", "mail.yahoo.com", "login.yahoo.com" } },
            { "facebook.com", new[] { "fbcdn.net", "facebook.net", "fb.com", "fbsbx.com", "staticxx.facebook.com", "connect.facebook.net" } },
            { "youtube.com", new[] { "googleapis.com", "gstatic.com", "googlevideo.com", "ytimg.com", "ggpht.com", "youtube-nocookie.com", "youtu.be", "i.ytimg.com", "s.ytimg.com" } },
            { "microsoft.com", new[] { "msn.com", "live.com", "bing.com", "msftconnecttest.com", "microsoftonline.com", "azure.com", "office.com", "windows.net" } },
            { "outlook.com", new[] { "live.com", "microsoft.com", "office365.com", "microsoftonline.com", "msn.com", "passport.net", "hotmail.com", "outlook.live.com", "office.com" } },
            { "amazon.in", new[] { "amazon.com", "amazonaws.com", "ssl-images-amazon.com", "media-amazon.com", "images-amazon.com", "amazonpay.in" } },
            { "amazon.com", new[] { "amazon.in", "amazonaws.com", "ssl-images-amazon.com", "media-amazon.com", "images-amazon.com" } },
            { "flipkart.com", new[] { "flixcart.com", "akamaized.net", "cloudfront.net" } },
            { "twitter.com", new[] { "twimg.com", "t.co", "x.com", "abs.twimg.com", "pbs.twimg.com" } },
            { "instagram.com", new[] { "cdninstagram.com", "fbcdn.net", "facebook.com", "instagram.com" } },
            { "linkedin.com", new[] { "licdn.com", "linkedin.cn", "static.licdn.com" } },
            { "github.com", new[] { "githubusercontent.com", "githubassets.com", "github.io", "raw.githubusercontent.com" } },
            { "whatsapp.com", new[] { "whatsapp.net", "wa.me", "web.whatsapp.com" } }
        };
        
        private string GetBaseDomain(string domain)
        {
            // Extract base domain from subdomain (e.g., mail.google.com -> google.com)
            var parts = domain.Split('.');
            if (parts.Length >= 2)
                return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
            return domain;
        }
        
        private void ResolveAndAddIPs(string domain, HashSet<string> ips)
        {
            try
            {
                var hostEntry = System.Net.Dns.GetHostEntry(domain);
                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ips.Add(ip.ToString());
                    }
                }
            }
            catch { }
        }
        
        private void AddCDNDomainsForSite(string baseDomain, HashSet<string> ips)
        {
            // Add known CDN/API domains for popular sites
            Dictionary<string, string[]> siteCDNs = new Dictionary<string, string[]>
            {
                { "google.com", new[] { "googleapis.com", "gstatic.com", "googleusercontent.com", "google.co.in", "googlevideo.com", "ggpht.com", "googletagmanager.com" } },
                { "gmail.com", new[] { "googleapis.com", "gstatic.com", "googleusercontent.com", "google.com", "googlemail.com" } },
                { "yahoo.com", new[] { "yimg.com", "yahooapis.com", "yahoo.net", "yahoofs.com", "oath.com", "aol.com", "s.yimg.com", "l.yimg.com" } },
                { "facebook.com", new[] { "fbcdn.net", "facebook.net", "fb.com", "fbsbx.com", "instagram.com" } },
                { "youtube.com", new[] { "googleapis.com", "gstatic.com", "googlevideo.com", "ytimg.com", "ggpht.com", "youtube-nocookie.com" } },
                { "microsoft.com", new[] { "msn.com", "live.com", "bing.com", "msftconnecttest.com", "msauth.net", "microsoftonline.com", "office.com", "azure.com" } },
                { "outlook.com", new[] { "live.com", "microsoft.com", "office365.com", "microsoftonline.com", "msn.com", "passport.net" } },
                { "amazon.com", new[] { "amazon.in", "amazonaws.com", "ssl-images-amazon.com", "media-amazon.com" } },
                { "amazon.in", new[] { "amazon.com", "amazonaws.com", "ssl-images-amazon.com", "media-amazon.com" } },
                { "flipkart.com", new[] { "flixcart.com", "akamaized.net" } },
                { "twitter.com", new[] { "twimg.com", "t.co", "twitter.net", "x.com" } },
                { "instagram.com", new[] { "cdninstagram.com", "fbcdn.net", "facebook.com" } },
                { "linkedin.com", new[] { "licdn.com", "linkedin.cn" } },
                { "github.com", new[] { "githubusercontent.com", "githubassets.com", "github.io" } }
            };
            
            if (siteCDNs.ContainsKey(baseDomain))
            {
                foreach (string cdn in siteCDNs[baseDomain])
                {
                    string[] variants = new[] { cdn, "www." + cdn, "s." + cdn, "static." + cdn };
                    foreach (string variant in variants)
                    {
                        ResolveAndAddIPs(variant, ips);
                    }
                }
            }
        }
        
        private void ExecuteNetshCommand(string fullCommand)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {fullCommand}",
                    UseShellExecute = true,
                    Verb = "runas", // Run as admin
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                
                var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit(10000);
                
                LogToFile($"Executed: {fullCommand}");
            }
            catch (Exception ex)
            {
                LogToFile($"Command failed: {fullCommand} - {ex.Message}");
            }
        }
        
        private void AddCommonSubdomains(string domain, List<string> hostsEntries)
        {
            // Add common subdomains for whitelisted domains
            string[] commonSubdomains = new string[] { "mail", "www", "m", "mobile", "api", "cdn", "static", "accounts", "login", "auth" };
            
            foreach (string sub in commonSubdomains)
            {
                string subDomain = $"{sub}.{domain}";
                try
                {
                    var entry = System.Net.Dns.GetHostEntry(subDomain);
                    foreach (var ip in entry.AddressList)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            hostsEntries.Add($"{ip} {subDomain} # DC-WHITELIST");
                            break;
                        }
                    }
                }
                catch { } // Subdomain doesn't exist, skip
            }
        }
        
        private void ClearHostsFile()
        {
            string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
            try
            {
                if (File.Exists(hostsPath))
                {
                    var lines = File.ReadAllLines(hostsPath).ToList();
                    lines.RemoveAll(line => line.Contains("# DC-WHITELIST") || line.Contains("# DC-BLOCKLIST"));
                    File.WriteAllLines(hostsPath, lines);
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error clearing hosts file: {ex.Message}");
            }
        }
        
        private List<string> GetComprehensiveSiteList()
        {
            // Comprehensive list of 500+ popular websites to block
            return new List<string>
            {
                // Social Media
                "facebook.com", "www.facebook.com", "m.facebook.com", "web.facebook.com", "fb.com",
                "twitter.com", "www.twitter.com", "mobile.twitter.com", "x.com", "www.x.com",
                "instagram.com", "www.instagram.com",
                "linkedin.com", "www.linkedin.com", "in.linkedin.com",
                "pinterest.com", "www.pinterest.com",
                "snapchat.com", "www.snapchat.com",
                "tiktok.com", "www.tiktok.com", "vm.tiktok.com",
                "reddit.com", "www.reddit.com", "old.reddit.com", "i.reddit.com",
                "tumblr.com", "www.tumblr.com",
                "whatsapp.com", "www.whatsapp.com", "web.whatsapp.com",
                "telegram.org", "www.telegram.org", "web.telegram.org", "t.me",
                "discord.com", "www.discord.com", "discordapp.com",
                "slack.com", "www.slack.com",
                "quora.com", "www.quora.com",
                "medium.com", "www.medium.com",
                
                // Video & Streaming
                "youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "music.youtube.com", "studio.youtube.com",
                "netflix.com", "www.netflix.com",
                "primevideo.com", "www.primevideo.com",
                "hotstar.com", "www.hotstar.com", "disney.hotstar.com", "disneyplus.com",
                "hulu.com", "www.hulu.com",
                "twitch.tv", "www.twitch.tv",
                "vimeo.com", "www.vimeo.com",
                "dailymotion.com", "www.dailymotion.com",
                "zee5.com", "www.zee5.com",
                "sonyliv.com", "www.sonyliv.com",
                "jiocinema.com", "www.jiocinema.com",
                "mxplayer.in", "www.mxplayer.in",
                "altbalaji.com", "www.altbalaji.com",
                "aha.video", "www.aha.video",
                "erosnow.com", "www.erosnow.com",
                "voot.com", "www.voot.com",
                "hungama.com", "www.hungama.com",
                
                // Music
                "spotify.com", "www.spotify.com", "open.spotify.com",
                "soundcloud.com", "www.soundcloud.com",
                "gaana.com", "www.gaana.com",
                "wynk.in", "www.wynk.in",
                "jiosaavn.com", "www.jiosaavn.com",
                "pandora.com", "www.pandora.com",
                "deezer.com", "www.deezer.com",
                
                // Shopping - India
                "amazon.in", "www.amazon.in", "amazon.com", "www.amazon.com",
                "flipkart.com", "www.flipkart.com",
                "myntra.com", "www.myntra.com",
                "snapdeal.com", "www.snapdeal.com",
                "ajio.com", "www.ajio.com",
                "nykaa.com", "www.nykaa.com",
                "meesho.com", "www.meesho.com",
                "shopclues.com", "www.shopclues.com",
                "paytmmall.com", "www.paytmmall.com",
                "tatacliq.com", "www.tatacliq.com",
                "reliancedigital.in", "www.reliancedigital.in",
                "croma.com", "www.croma.com",
                "bigbasket.com", "www.bigbasket.com",
                "grofers.com", "www.grofers.com", "blinkit.com", "www.blinkit.com",
                "swiggy.com", "www.swiggy.com",
                "zomato.com", "www.zomato.com",
                
                // Shopping - International
                "ebay.com", "www.ebay.com",
                "aliexpress.com", "www.aliexpress.com",
                "alibaba.com", "www.alibaba.com",
                "wish.com", "www.wish.com",
                "etsy.com", "www.etsy.com",
                "walmart.com", "www.walmart.com",
                "target.com", "www.target.com",
                "bestbuy.com", "www.bestbuy.com",
                
                // News - India
                "ndtv.com", "www.ndtv.com",
                "aajtak.in", "www.aajtak.in",
                "indiatoday.in", "www.indiatoday.in",
                "timesofindia.indiatimes.com", "timesofindia.com",
                "hindustantimes.com", "www.hindustantimes.com",
                "thehindu.com", "www.thehindu.com",
                "indianexpress.com", "www.indianexpress.com",
                "news18.com", "www.news18.com",
                "firstpost.com", "www.firstpost.com",
                "scroll.in", "www.scroll.in",
                "thewire.in", "www.thewire.in",
                "livemint.com", "www.livemint.com",
                "economictimes.indiatimes.com", "economictimes.com",
                "moneycontrol.com", "www.moneycontrol.com",
                "zeenews.india.com", "zeenews.com",
                
                // News - International
                "bbc.com", "www.bbc.com", "bbc.co.uk",
                "cnn.com", "www.cnn.com",
                "foxnews.com", "www.foxnews.com",
                "nytimes.com", "www.nytimes.com",
                "theguardian.com", "www.theguardian.com",
                "washingtonpost.com", "www.washingtonpost.com",
                "reuters.com", "www.reuters.com",
                "aljazeera.com", "www.aljazeera.com",
                "buzzfeed.com", "www.buzzfeed.com",
                "huffpost.com", "www.huffpost.com",
                
                // Entertainment
                "9gag.com", "www.9gag.com",
                "imgur.com", "www.imgur.com", "i.imgur.com",
                "giphy.com", "www.giphy.com",
                "boredpanda.com", "www.boredpanda.com",
                "imdb.com", "www.imdb.com",
                "rottentomatoes.com", "www.rottentomatoes.com",
                
                // Gaming
                "steam.com", "www.steam.com", "store.steampowered.com", "steampowered.com", "steamcommunity.com",
                "epicgames.com", "www.epicgames.com",
                "origin.com", "www.origin.com", "ea.com",
                "ubisoft.com", "www.ubisoft.com",
                "roblox.com", "www.roblox.com",
                "minecraft.net", "www.minecraft.net",
                "battle.net", "www.battle.net",
                "gog.com", "www.gog.com",
                "itch.io", "www.itch.io",
                "twitch.tv", "www.twitch.tv",
                
                // Dating
                "tinder.com", "www.tinder.com",
                "bumble.com", "www.bumble.com",
                "hinge.co", "www.hinge.co",
                "okcupid.com", "www.okcupid.com",
                "match.com", "www.match.com",
                "shaadi.com", "www.shaadi.com",
                "bharatmatrimony.com", "www.bharatmatrimony.com",
                "jeevansathi.com", "www.jeevansathi.com",
                
                // Search & Email
                "bing.com", "www.bing.com",
                "duckduckgo.com", "www.duckduckgo.com",
                "yahoo.com", "www.yahoo.com", "mail.yahoo.com", "in.yahoo.com",
                "outlook.com", "www.outlook.com", "outlook.live.com",
                "live.com", "www.live.com", "login.live.com",
                "msn.com", "www.msn.com",
                "aol.com", "www.aol.com", "mail.aol.com",
                "protonmail.com", "www.protonmail.com",
                "zoho.com", "www.zoho.com", "mail.zoho.com",
                
                // Google Services
                "google.com", "www.google.com", "google.co.in", "www.google.co.in",
                "gmail.com", "www.gmail.com", "mail.google.com",
                "drive.google.com", "docs.google.com", "sheets.google.com", "slides.google.com",
                "maps.google.com", "www.google.com/maps",
                "translate.google.com", "translate.google.co.in",
                "photos.google.com",
                "play.google.com",
                "news.google.com",
                "calendar.google.com",
                "meet.google.com",
                "chat.google.com",
                "classroom.google.com",
                "accounts.google.com",
                
                // Cloud & Storage
                "dropbox.com", "www.dropbox.com",
                "onedrive.com", "www.onedrive.com", "onedrive.live.com",
                "icloud.com", "www.icloud.com",
                "box.com", "www.box.com",
                "mega.nz", "www.mega.nz",
                "mediafire.com", "www.mediafire.com",
                "wetransfer.com", "www.wetransfer.com",
                
                // Tech & Developer
                "github.com", "www.github.com", "gist.github.com",
                "gitlab.com", "www.gitlab.com",
                "stackoverflow.com", "www.stackoverflow.com", "stackexchange.com",
                "w3schools.com", "www.w3schools.com",
                "geeksforgeeks.org", "www.geeksforgeeks.org",
                "hackerrank.com", "www.hackerrank.com",
                "leetcode.com", "www.leetcode.com",
                "codepen.io", "www.codepen.io",
                "jsfiddle.net", "www.jsfiddle.net",
                "replit.com", "www.replit.com",
                
                // Tools & Utilities
                "canva.com", "www.canva.com",
                "figma.com", "www.figma.com",
                "trello.com", "www.trello.com",
                "notion.so", "www.notion.so",
                "evernote.com", "www.evernote.com",
                "grammarly.com", "www.grammarly.com",
                
                // Video Calling
                "zoom.us", "www.zoom.us",
                "teams.microsoft.com",
                "skype.com", "www.skype.com", "web.skype.com",
                "webex.com", "www.webex.com",
                "gotomeeting.com", "www.gotomeeting.com",
                
                // Education
                "wikipedia.org", "www.wikipedia.org", "en.wikipedia.org",
                "coursera.org", "www.coursera.org",
                "udemy.com", "www.udemy.com",
                "edx.org", "www.edx.org",
                "khanacademy.org", "www.khanacademy.org",
                "unacademy.com", "www.unacademy.com",
                "byjus.com", "www.byjus.com",
                "vedantu.com", "www.vedantu.com",
                "toppr.com", "www.toppr.com",
                
                // Finance & Banking - India
                "paytm.com", "www.paytm.com",
                "phonepe.com", "www.phonepe.com",
                "gpay.co.in",
                "freecharge.in", "www.freecharge.in",
                "mobikwik.com", "www.mobikwik.com",
                "cred.club", "www.cred.club",
                "groww.in", "www.groww.in",
                "zerodha.com", "www.zerodha.com", "kite.zerodha.com",
                "upstox.com", "www.upstox.com",
                "angelone.in", "www.angelone.in",
                "policybazaar.com", "www.policybazaar.com",
                "bankbazaar.com", "www.bankbazaar.com",
                "paisabazaar.com", "www.paisabazaar.com",
                
                // Insurance
                "reliancegeneral.co.in", "www.reliancegeneral.co.in",
                "icicilombard.com", "www.icicilombard.com",
                "hdfcergo.com", "www.hdfcergo.com",
                "bajajallianz.com", "www.bajajallianz.com",
                "tataaig.com", "www.tataaig.com",
                "acko.com", "www.acko.com",
                "digit.in", "www.digit.in",
                "indusindinsurance.com",
                
                // Travel
                "makemytrip.com", "www.makemytrip.com",
                "goibibo.com", "www.goibibo.com",
                "yatra.com", "www.yatra.com",
                "cleartrip.com", "www.cleartrip.com",
                "irctc.co.in", "www.irctc.co.in",
                "redbus.in", "www.redbus.in",
                "ixigo.com", "www.ixigo.com",
                "booking.com", "www.booking.com",
                "airbnb.com", "www.airbnb.com",
                "tripadvisor.com", "www.tripadvisor.com",
                "expedia.com", "www.expedia.com",
                "agoda.com", "www.agoda.com",
                "oyo.com", "www.oyorooms.com",
                
                // Job Portals
                "naukri.com", "www.naukri.com",
                "indeed.com", "www.indeed.com", "in.indeed.com",
                "linkedin.com", "www.linkedin.com",
                "glassdoor.com", "www.glassdoor.co.in",
                "monster.com", "www.monsterindia.com",
                "shine.com", "www.shine.com",
                "timesjobs.com", "www.timesjobs.com",
                "foundit.in", "www.foundit.in",
                "internshala.com", "www.internshala.com",
                
                // Real Estate
                "99acres.com", "www.99acres.com",
                "magicbricks.com", "www.magicbricks.com",
                "housing.com", "www.housing.com",
                "nobroker.in", "www.nobroker.in",
                "makaan.com", "www.makaan.com",
                "commonfloor.com", "www.commonfloor.com",
                
                // Automotive
                "cardekho.com", "www.cardekho.com",
                "carwale.com", "www.carwale.com",
                "bikewale.com", "www.bikewale.com",
                "olx.in", "www.olx.in",
                "cars24.com", "www.cars24.com",
                "spinny.com", "www.spinny.com",
                "droom.in", "www.droom.in",
                
                // Miscellaneous Popular
                "indiamart.com", "www.indiamart.com",
                "justdial.com", "www.justdial.com",
                "sulekha.com", "www.sulekha.com",
                "urbanclap.com", "www.urbancompany.com",
                "practo.com", "www.practo.com",
                "1mg.com", "www.1mg.com",
                "pharmeasy.in", "www.pharmeasy.in",
                "netmeds.com", "www.netmeds.com",
                "apollopharmacy.in", "www.apollopharmacy.in",
                "lenskart.com", "www.lenskart.com",
                "pepperfry.com", "www.pepperfry.com",
                "urbanladder.com", "www.urbanladder.com",
                "fabindia.com", "www.fabindia.com",
                "decathlon.in", "www.decathlon.in",
                
                // Tech Companies
                "apple.com", "www.apple.com",
                "microsoft.com", "www.microsoft.com",
                "adobe.com", "www.adobe.com",
                "oracle.com", "www.oracle.com",
                "ibm.com", "www.ibm.com",
                "intel.com", "www.intel.com",
                "nvidia.com", "www.nvidia.com",
                "amd.com", "www.amd.com",
                "samsung.com", "www.samsung.com",
                "oneplus.in", "www.oneplus.in",
                "mi.com", "www.mi.com",
                "realme.com", "www.realme.com",
                "oppo.com", "www.oppo.com",
                "vivo.com", "www.vivo.com",
                
                // CDNs and common domains (block to prevent loading)
                "cloudflare.com", "cdnjs.cloudflare.com",
                "jsdelivr.net", "cdn.jsdelivr.net",
                "bootstrapcdn.com", "maxcdn.bootstrapcdn.com",
                "fontawesome.com",
                "fonts.googleapis.com", "fonts.gstatic.com",
                "ajax.googleapis.com",
                "cdnjs.com",
                "unpkg.com",
                
                // Analytics (block tracking)
                "googletagmanager.com", "www.googletagmanager.com",
                "google-analytics.com", "www.google-analytics.com",
                "analytics.google.com",
                "doubleclick.net", "www.doubleclick.net",
                "googlesyndication.com",
                "googleadservices.com",
                "facebook.net",
                "fbcdn.net",
                "connect.facebook.net"
            };
        }
        
        private void RemoveFirewallRules()
        {
            // Remove all DC_ firewall rules
            string[] browsers = new string[] { "chrome", "msedge", "msedgewebview2", "firefox", "brave", "opera" };
            
            foreach (string browser in browsers)
            {
                // Delete all DC rules for browsers
                RunNetshDirect($"advfirewall firewall delete rule name=\"DC_Allow_{browser}\"");
                RunNetshDirect($"advfirewall firewall delete rule name=\"DC_Block_{browser}\"");
                RunNetshDirect($"advfirewall firewall delete rule name=\"DC_Browser_{browser}\"");
                RunNetshDirect($"advfirewall firewall delete rule name=\"DC_WhiteAllow_{browser}\"");
                RunNetshDirect($"advfirewall firewall delete rule name=\"DC_WhiteBlock_{browser}\"");
            }
            
            // Delete essential/system rules
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Essential_DNS\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Essential_DNS_TCP\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Essential_DHCP\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Essential_LocalNet\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Essential_Loopback\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Essential_App\"");
            
            // Delete system rules (new naming)
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_DNS\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_DNS_TCP\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_DHCP\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_NTP\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Local\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Loopback\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Private1\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Private2\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Private3\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Svchost\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Lsass\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Services\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Wuauclt\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Spoolsv\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_SearchIndexer\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Taskhostw\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Dllhost\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Conhost\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_RuntimeBroker\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Sihost\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_App\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_Dotnet\"");
            RunNetshDirect("advfirewall firewall delete rule name=\"DC_Sys_PostgreSQL\"");
            
            // Also try to delete any other DC_ rules by scanning
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall firewall show rule name=all",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(10000);
                
                // Find and delete all DC_ and DesktopController_ rules
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("DC_") || line.Contains("DesktopController_"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1)
                        {
                            string ruleName = parts[1].Trim();
                            ExecuteCommand($"netsh advfirewall firewall delete rule name=\"{ruleName}\"");
                        }
                    }
                }
            }
            catch { }
        }
        
        private void ExecuteCommand(string command)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, 2);
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = parts[0],
                        Arguments = parts.Length > 1 ? parts[1] : "",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                LogToFile($"Command error ({command}): {ex.Message}");
            }
        }
        private void ClearWebsiteBlocking()
        {
            try
            {
                // IMPORTANT: Restore firewall default outbound to ALLOW
                RunNetshDirect("advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound");
                
                // Remove firewall rules
                RemoveFirewallRules();
                
                // Also clean hosts file
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                if (File.Exists(hostsPath))
                {
                    List<string> lines = File.ReadAllLines(hostsPath).ToList();
                    lines.RemoveAll(line => line.Contains("# DesktopController") || line.Contains("# DC-"));
                    File.WriteAllLines(hostsPath, lines);
                }

                // Remove whitelist file
                string whitelistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "DesktopController", "whitelist_active.txt");
                if (File.Exists(whitelistPath))
                {
                    File.Delete(whitelistPath);
                }

                // Remove rules file
                string rulesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "DesktopController", "website_rules.txt");
                if (File.Exists(rulesPath))
                {
                    File.Delete(rulesPath);
                }
                
                // Remove firewall blocked flag
                string blockedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "DesktopController", "firewall_blocked.txt");
                if (File.Exists(blockedPath))
                {
                    File.Delete(blockedPath);
                }

                FlushDNSCache();
                LogToFile("Cleared all website blocking rules and restored firewall to default");
                
                MessageBox.Show("Website blocking cleared!\n\nFirewall restored to normal.\nAll websites should work now.", 
                    "Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogToFile($"Error clearing website rules: {ex.Message}");
            }
        }

        private void FlushDNSCache()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                process.WaitForExit(5000);
                LogToFile("DNS cache flushed");
            }
            catch (Exception ex)
            {
                LogToFile($"Error flushing DNS: {ex.Message}");
            }
        }

        private Panel CreateToggleCard(string title, string description, Func<bool> getState, Action<bool> setState)
        {
            Panel card = new Panel
            {
                Width = 350,
                Height = 115,
                Margin = new Padding(12),
                BackColor = cardColor,  // slate-700 (lighter than content bg)
                Padding = new Padding(18)
            };
            
            // Add modern rounded corners with subtle shadow
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw subtle shadow
                using (var shadowPath = CreateRoundedRectPath(2, 3, card.Width - 1, card.Height - 1, 12))
                {
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
                
                // Draw card with border
                using (var path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12))
                {
                    using (var brush = new SolidBrush(card.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new Pen(borderColor, 1.5f))  // Slightly thicker border
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            
            // Mouse hover effect
            card.MouseEnter += (s, e) => { card.BackColor = cardHoverColor; card.Invalidate(); };
            card.MouseLeave += (s, e) => { card.BackColor = cardColor; card.Invalidate(); };

            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(18, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Status indicator dot with glow effect
            Panel statusDot = new Panel
            {
                Size = new Size(14, 14),
                Location = new Point(305, 14),
                BackColor = Color.Transparent
            };
            statusDot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color dotColor = statusDot.Tag as Color? ?? allowColor;
                // Glow effect
                using (var glowBrush = new SolidBrush(Color.FromArgb(60, dotColor)))
                {
                    e.Graphics.FillEllipse(glowBrush, -2, -2, 18, 18);
                }
                using (var brush = new SolidBrush(dotColor))
                {
                    e.Graphics.FillEllipse(brush, 2, 2, 10, 10);
                }
            };

            Label descLabel = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9),
                ForeColor = secondaryTextColor,
                Location = new Point(18, 38),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // BLOCK Button - Modern style
            Button blockBtn = new Button
            {
                Text = "BLOCK",
                Width = 90,
                Height = 34,
                Location = new Point(90, 68),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.White
            };
            blockBtn.FlatAppearance.BorderSize = 0;
            blockBtn.FlatAppearance.MouseOverBackColor = blockHoverColor;

            // ALLOW Button - Modern style
            Button allowBtn = new Button
            {
                Text = "ALLOW",
                Width = 90,
                Height = 34,
                Location = new Point(190, 68),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.White
            };
            allowBtn.FlatAppearance.BorderSize = 0;
            allowBtn.FlatAppearance.MouseOverBackColor = allowHoverColor;

            // Update UI based on state
            Action updateUI = () =>
            {
                try
                {
                    bool currentState = getState();
                    if (currentState) // Allowed
                    {
                        statusDot.Tag = successColor;
                        statusDot.Invalidate();
                        blockBtn.BackColor = inactiveColor;
                        blockBtn.ForeColor = secondaryTextColor;
                        allowBtn.BackColor = allowColor;
                        allowBtn.ForeColor = Color.White;
                    }
                    else // Blocked
                    {
                        statusDot.Tag = dangerColor;
                        statusDot.Invalidate();
                        blockBtn.BackColor = blockColor;
                        blockBtn.ForeColor = Color.White;
                        allowBtn.BackColor = inactiveColor;
                        allowBtn.ForeColor = secondaryTextColor;
                    }
                }
                catch
                {
                    statusDot.Tag = warningColor;
                    statusDot.Invalidate();
                    blockBtn.BackColor = inactiveColor;
                    allowBtn.BackColor = inactiveColor;
                }
            };

            // Set initial state
            updateUI();

            blockBtn.Click += (s, e) =>
            {
                try
                {
                    setState(false);
                    updateUI();
                    ShowNotification(title, "Blocked");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            allowBtn.Click += (s, e) =>
            {
                try
                {
                    setState(true);
                    updateUI();
                    ShowNotification(title, "Allowed");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            card.Controls.AddRange(new Control[] { titleLabel, statusDot, descLabel, blockBtn, allowBtn });
            return card;
        }

        private Panel CreateUserManagementCard()
        {
            Panel card = new Panel
            {
                Width = 350,
                Height = 180,
                Margin = new Padding(10),
                BackColor = cardColor,
                Padding = new Padding(15)
            };

            Label titleLabel = new Label
            {
                Text = "👥 User Account Management",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(15, 15),
                AutoSize = true
            };

            ComboBox userCombo = new ComboBox
            {
                Location = new Point(15, 50),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            RefreshUsers(userCombo);

            Button refreshBtn = CreateStyledButton("🔄", accentColor);
            refreshBtn.Width = 40;
            refreshBtn.Location = new Point(220, 48);
            refreshBtn.Click += (s, e) => RefreshUsers(userCombo);

            Button enableBtn = CreateStyledButton("Enable", successColor);
            enableBtn.Location = new Point(15, 90);
            enableBtn.Width = 100;
            enableBtn.Click += (s, e) => SetUserEnabled(userCombo.SelectedItem?.ToString(), true);

            Button disableBtn = CreateStyledButton("Disable", dangerColor);
            disableBtn.Location = new Point(125, 90);
            disableBtn.Width = 100;
            disableBtn.Click += (s, e) => SetUserEnabled(userCombo.SelectedItem?.ToString(), false);

            Button createBtn = CreateStyledButton("➕ New User", accentColor);
            createBtn.Location = new Point(15, 130);
            createBtn.Width = 150;
            createBtn.Click += (s, e) => CreateNewUser();

            card.Controls.AddRange(new Control[] { titleLabel, userCombo, refreshBtn, enableBtn, disableBtn, createBtn });
            return card;
        }

        private Panel CreateInfoCard(string title, string info)
        {
            Panel card = new Panel
            {
                Width = 350,
                Height = 150,
                Margin = new Padding(10),
                BackColor = cardColor,
                Padding = new Padding(15)
            };

            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(15, 15),
                AutoSize = true
            };

            Label infoLabel = new Label
            {
                Text = info,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(15, 45),
                Size = new Size(320, 90),
                AutoEllipsis = true
            };

            card.Controls.AddRange(new Control[] { titleLabel, infoLabel });
            return card;
        }

        private Button CreateStyledButton(string text, Color bgColor)
        {
            Button btn = new Button
            {
                Text = text,
                Width = 140,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void ShowNotification(string title, string status)
        {
            statusLabel.Text = $"✓ {title}: {status}";
            statusLabel.ForeColor = status == "Allowed" ? successColor : dangerColor;
        }
        
        // Helper method for creating rounded rectangle paths
        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        #region Audit Logs Tab
        
        private TabPage CreateAuditLogsTab()
        {
            TabPage tab = new TabPage("📊 AUDIT");
            tab.BackColor = bgColor;
            
            // Main container with tabs for different audit sections
            TabControl auditTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                Padding = new Point(15, 5),
                ItemSize = new Size(120, 28)
            };
            
            // System Info Panel (Always visible at top)
            Panel systemInfoPanel = CreateSystemInfoPanel();
            systemInfoPanel.Dock = DockStyle.Top;
            systemInfoPanel.Height = 80;
            
            // Create sub-tabs
            TabPage overviewTab = CreateAuditOverviewTab();
            TabPage websitesTab = CreateWebsitesAuditTab();
            TabPage appsTab = CreateAppsAuditTab();
            TabPage inactivityTab = CreateInactivityAuditTab();
            TabPage locationTab = CreateLocationAuditTab();
            
            auditTabs.TabPages.Add(overviewTab);
            auditTabs.TabPages.Add(websitesTab);
            auditTabs.TabPages.Add(appsTab);
            auditTabs.TabPages.Add(inactivityTab);
            auditTabs.TabPages.Add(locationTab);
            
            tab.Controls.Add(auditTabs);
            tab.Controls.Add(systemInfoPanel);
            
            return tab;
        }
        
        private Panel CreateSystemInfoPanel()
        {
            Panel panel = new Panel
            {
                BackColor = cardColor,
                Padding = new Padding(15)
            };
            
            // Get system info
            string machineName = Environment.MachineName;
            string ipAddress = GetLocalIPAddress();
            string userName = Environment.UserName;
            
            Label systemLabel = new Label
            {
                Text = $"🖥️ System: {machineName}  |  🌐 IP: {ipAddress}  |  👤 User: {userName}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = true,
                Location = new Point(20, 15),
                BackColor = Color.Transparent
            };
            
            Label timeLabel = new Label
            {
                Text = $"📅 Date: {DateTime.Now:yyyy-MM-dd}  |  ⏰ Time: {DateTime.Now:HH:mm:ss}",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                AutoSize = true,
                Location = new Point(20, 45),
                BackColor = Color.Transparent
            };
            
            // Update time every second
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) => {
                timeLabel.Text = $"📅 Date: {DateTime.Now:yyyy-MM-dd}  |  ⏰ Time: {DateTime.Now:HH:mm:ss}";
            };
            timer.Start();
            
            // Refresh button
            Button refreshBtn = new Button
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 30),
                Location = new Point(panel.Width - 120, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            refreshBtn.FlatAppearance.BorderSize = 0;
            refreshBtn.Click += (s, e) => RefreshAuditData();
            
            panel.Controls.Add(systemLabel);
            panel.Controls.Add(timeLabel);
            panel.Controls.Add(refreshBtn);
            
            return panel;
        }
        
        private TabPage CreateAuditOverviewTab()
        {
            TabPage tab = new TabPage("📋 Overview");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(15);
            
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = bgColor
            };
            
            // Punch Stats Card
            Panel punchCard = CreateAuditCard("⏱️ Punch Status", 350, 120);
            overviewPunchLabel = new Label
            {
                Text = isPunchedIn ? $"✅ Punched In since {punchInTime:HH:mm:ss}" : "⏸️ Not Punched In",
                Font = new Font("Segoe UI", 11),
                ForeColor = isPunchedIn ? successColor : secondaryTextColor,
                Location = new Point(15, 45),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            punchCard.Controls.Add(overviewPunchLabel);
            
            // Activity Summary Card
            Panel activityCard = CreateAuditCard("📊 Activity Summary", 350, 120);
            overviewActivityLabel = new Label
            {
                Text = $"Total Activities: {activityLogs.Count}\nApps Tracked: {appUsageLogs.Count}\nWebsites Visited: {websiteLogs.Count}",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(15, 40),
                Size = new Size(320, 70),
                BackColor = Color.Transparent
            };
            activityCard.Controls.Add(overviewActivityLabel);
            
            // Inactivity Card
            Panel inactivityCard = CreateAuditCard("💤 Inactivity", 350, 120);
            TimeSpan idleTime = DateTime.Now - lastActivityTime;
            overviewInactivityLabel = new Label
            {
                Text = isPunchedIn ? $"Current Idle: {idleTime:mm\\:ss}\nLast Activity: {lastActivityTime:HH:mm:ss}" : "Tracking paused",
                Font = new Font("Segoe UI", 10),
                ForeColor = idleTime.TotalMinutes > 5 ? warningColor : textColor,
                Location = new Point(15, 40),
                Size = new Size(320, 70),
                BackColor = Color.Transparent
            };
            inactivityCard.Controls.Add(overviewInactivityLabel);
            
            flowPanel.Controls.Add(punchCard);
            flowPanel.Controls.Add(activityCard);
            flowPanel.Controls.Add(inactivityCard);
            
            tab.Controls.Add(flowPanel);
            return tab;
        }
        
        private TabPage CreateWebsitesAuditTab()
        {
            TabPage tab = new TabPage("🌐 Websites");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(15);
            
            // ListView for websites
            ListView listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = cardColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None
            };
            
            listView.Columns.Add("Time", 80);
            listView.Columns.Add("Browser", 80);
            listView.Columns.Add("Website URL", 350);
            listView.Columns.Add("Title", 200);
            listView.Columns.Add("Category", 130);
            
            // Store reference for auto-refresh
            websitesListView = listView;
            
            // Button panel with scan and sync buttons
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = primaryColor,
                Padding = new Padding(10)
            };
            
            Button scanBtn = new Button
            {
                Text = "🔍 Scan Browser History",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 30),
                Location = new Point(10, 10),
                Cursor = Cursors.Hand
            };
            scanBtn.FlatAppearance.BorderSize = 0;
            scanBtn.Click += (s, e) => ScanBrowserHistory(listView);
            
            Button syncBtn = new Button
            {
                Text = "📤 Sync to Database",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = successColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 30),
                Location = new Point(220, 10),
                Cursor = Cursors.Hand
            };
            syncBtn.FlatAppearance.BorderSize = 0;
            syncBtn.Click += (s, e) => SyncBrowserHistoryToDatabase();
            
            Label infoLabel = new Label
            {
                Text = "📊 Shows today's browsing history from Chrome, Edge, Firefox",
                Font = new Font("Segoe UI", 9),
                ForeColor = secondaryTextColor,
                AutoSize = true,
                Location = new Point(420, 15)
            };
            
            buttonPanel.Controls.Add(scanBtn);
            buttonPanel.Controls.Add(syncBtn);
            buttonPanel.Controls.Add(infoLabel);
            
            tab.Controls.Add(listView);
            tab.Controls.Add(buttonPanel);
            
            return tab;
        }
        
        private void SyncBrowserHistoryToDatabase()
        {
            SyncBrowserHistoryToDatabaseSilent();
        }
        
        private void SyncBrowserHistoryToDatabaseSilent()
        {
            if (websiteLogs.Count == 0)
            {
                LogToFile("No website logs to sync");
                return;
            }
            
            try
            {
                // Prepare web log entries - create a copy WITHOUT clearing the original list
                List<WebLogEntry> webEntries;
                lock (websiteLogs)
                {
                    webEntries = websiteLogs.Select(w => new WebLogEntry
                    {
                        Browser = w.Browser,
                        Url = w.Url,
                        Title = w.Title,
                        Category = CategorizeUrl(w.Url),
                        VisitTime = w.Timestamp
                    }).ToList();
                    
                    // DO NOT CLEAR - Keep logs for UI display
                    LogToFile($"Syncing {webEntries.Count} website logs to database");
                }
                
                // Batch insert to web_logs table
                int synced = DatabaseHelper.InsertWebLogsBatch(storedActivationKey, storedCompanyName, 
                    currentUser?.Name, webEntries);
                LogToFile($"Synced {synced} website logs to database");
            }
            catch (Exception ex) {
                LogToFile($"Sync error: {ex.Message}");
            }
        }
        
        private TabPage CreateAppsAuditTab()
        {
            TabPage tab = new TabPage("📱 Applications");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(15);
            
            // ListView for apps
            ListView listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = cardColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None
            };
            
            listView.Columns.Add("Start Time", 120);
            listView.Columns.Add("End Time", 120);
            listView.Columns.Add("Application", 150);
            listView.Columns.Add("Window Title", 300);
            listView.Columns.Add("Duration", 100);
            
            // Store reference for auto-refresh
            applicationsListView = listView;
            
            // Button to refresh
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = primaryColor,
                Padding = new Padding(10)
            };
            
            Button refreshBtn = new Button
            {
                Text = "🔄 Refresh App Usage",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 30),
                Cursor = Cursors.Hand
            };
            refreshBtn.FlatAppearance.BorderSize = 0;
            refreshBtn.Click += (s, e) => RefreshAppUsage(listView);
            
            buttonPanel.Controls.Add(refreshBtn);
            
            tab.Controls.Add(listView);
            tab.Controls.Add(buttonPanel);
            
            return tab;
        }
        
        private TabPage CreateInactivityAuditTab()
        {
            TabPage tab = new TabPage("💤 Inactivity");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(15);
            
            // Current inactivity status
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = cardColor,
                Padding = new Padding(20)
            };
            
            Label statusTitle = new Label
            {
                Text = "Current Inactivity Monitor",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(20, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            Label currentIdleLabel = new Label
            {
                Name = "idleLabel",
                Text = "Idle Time: 00:00",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = successColor,
                Location = new Point(20, 45),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            // Update idle time
            var idleTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            idleTimer.Tick += (s, e) => {
                if (isPunchedIn)
                {
                    TimeSpan idle = DateTime.Now - lastActivityTime;
                    currentIdleLabel.Text = $"Idle Time: {idle:mm\\:ss}";
                    currentIdleLabel.ForeColor = idle.TotalMinutes > 5 ? dangerColor : 
                                                  idle.TotalMinutes > 2 ? warningColor : successColor;
                }
                else
                {
                    currentIdleLabel.Text = "Tracking paused (not punched in)";
                    currentIdleLabel.ForeColor = secondaryTextColor;
                }
            };
            idleTimer.Start();
            
            statusPanel.Controls.Add(statusTitle);
            statusPanel.Controls.Add(currentIdleLabel);
            
            // ListView for inactivity logs
            ListView listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = cardColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None
            };
            
            listView.Columns.Add("Start Time", 150);
            listView.Columns.Add("End Time", 150);
            listView.Columns.Add("Duration", 120);
            listView.Columns.Add("Status", 100);
            
            // Store reference for updating from timer
            inactivityListView = listView;
            
            // Populate with existing logs
            foreach (var log in inactivityLogs.OrderByDescending(l => l.StartTime))
            {
                AddInactivityToList(log);
            }
            
            Label listLabel = new Label
            {
                Text = "Inactivity periods (≥ 1 minute) will be logged here when detected",
                Font = new Font("Segoe UI", 9),
                ForeColor = secondaryTextColor,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = primaryColor
            };
            
            tab.Controls.Add(listView);
            tab.Controls.Add(listLabel);
            tab.Controls.Add(statusPanel);
            
            return tab;
        }
        
        private TabPage CreateLocationAuditTab()
        {
            TabPage tab = new TabPage("📍 GPS Location");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(20);
            
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardColor,
                Padding = new Padding(30)
            };
            
            Label titleLabel = new Label
            {
                Text = "📍 GPS Location Tracking",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(30, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            Label latLabel = new Label
            {
                Name = "latLabel",
                Text = "Latitude: Loading...",
                Font = new Font("Segoe UI", 14),
                ForeColor = accentColor,
                Location = new Point(30, 70),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            Label lonLabel = new Label
            {
                Name = "lonLabel",
                Text = "Longitude: Loading...",
                Font = new Font("Segoe UI", 14),
                ForeColor = accentColor,
                Location = new Point(30, 110),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            Label accuracyLabel = new Label
            {
                Name = "accuracyLabel",
                Text = "Accuracy: --",
                Font = new Font("Segoe UI", 11),
                ForeColor = secondaryTextColor,
                Location = new Point(30, 150),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            Label timestampLabel = new Label
            {
                Name = "timestampLabel",
                Text = "Last Updated: --",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(30, 185),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            Button getLocationBtn = new Button
            {
                Text = "📍 Get Current Location",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 45),
                Location = new Point(30, 230),
                Cursor = Cursors.Hand
            };
            getLocationBtn.FlatAppearance.BorderSize = 0;
            getLocationBtn.Click += async (s, e) => await GetGPSLocation(latLabel, lonLabel, accuracyLabel, timestampLabel, getLocationBtn);
            
            // Open in Maps button
            Button openMapsBtn = new Button
            {
                Text = "🗺️ Open in Maps",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = successColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 40),
                Location = new Point(250, 232),
                Cursor = Cursors.Hand
            };
            openMapsBtn.FlatAppearance.BorderSize = 0;
            openMapsBtn.Click += (s, e) => {
                if (latLabel.Tag != null && lonLabel.Tag != null)
                {
                    string url = $"https://www.google.com/maps?q={latLabel.Tag},{lonLabel.Tag}";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Please get location first.", "Location", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(latLabel);
            mainPanel.Controls.Add(lonLabel);
            mainPanel.Controls.Add(accuracyLabel);
            mainPanel.Controls.Add(timestampLabel);
            mainPanel.Controls.Add(getLocationBtn);
            mainPanel.Controls.Add(openMapsBtn);
            
            tab.Controls.Add(mainPanel);
            
            // Auto-get location on load
            tab.VisibleChanged += async (s, e) => {
                if (tab.Visible && latLabel.Text.Contains("Loading"))
                {
                    await GetGPSLocation(latLabel, lonLabel, accuracyLabel, timestampLabel, getLocationBtn);
                }
            };
            
            return tab;
        }
        
        private Panel CreateAuditCard(string title, int width, int height)
        {
            Panel card = new Panel
            {
                Width = width,
                Height = height,
                Margin = new Padding(10),
                BackColor = cardColor,
                Padding = new Padding(15)
            };
            
            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(15, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            card.Controls.Add(titleLabel);
            return card;
        }
        
        private string GetLocalIPAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }
        
        private void RefreshAuditData()
        {
            statusLabel.Text = "✓ Audit data refreshed";
            statusLabel.ForeColor = successColor;
        }
        
        private void ScanBrowserHistory(ListView listView)
        {
            listView.Items.Clear();
            int totalFound = 0;
            
            try
            {
                // Scan Chrome history
                totalFound += ScanChromeHistory(listView);
                
                // Scan Edge history
                totalFound += ScanEdgeHistory(listView);
                
                // Scan Firefox history
                totalFound += ScanFirefoxHistory(listView);
                
                // Add tracked websites from real-time monitoring
                foreach (var web in websiteLogs.OrderByDescending(w => w.Timestamp))
                {
                    var item = new ListViewItem(new[] { 
                        web.Timestamp.ToString("HH:mm:ss"),
                        web.Browser,
                        web.Url,
                        web.Title,
                        web.Duration.ToString(@"mm\:ss")
                    });
                    item.ForeColor = accentColor;
                    listView.Items.Add(item);
                }
                
                if (listView.Items.Count == 0)
                {
                    var item = new ListViewItem(new[] { "-", "-", "No browser history found", "-", "-" });
                    listView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                var item = new ListViewItem(new[] { "-", "-", $"Error: {ex.Message}", "-", "-" });
                listView.Items.Add(item);
            }
            
            statusLabel.Text = $"✓ Scanned browser history - Found {listView.Items.Count} entries";
            statusLabel.ForeColor = successColor;
        }
        
        private int ScanChromeHistory(ListView listView)
        {
            int count = 0;
            try
            {
                string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\User Data\Default\History");
                
                if (File.Exists(chromePath))
                {
                    count = ReadBrowserHistoryDb(listView, chromePath, "Chrome");
                }
            }
            catch (Exception ex)
            {
                var item = new ListViewItem(new[] { "-", "Chrome", $"Error: {ex.Message}", "-", "-" });
                item.ForeColor = dangerColor;
                listView.Items.Add(item);
            }
            return count;
        }
        
        private int ScanEdgeHistory(ListView listView)
        {
            int count = 0;
            try
            {
                string edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Edge\User Data\Default\History");
                
                if (File.Exists(edgePath))
                {
                    count = ReadBrowserHistoryDb(listView, edgePath, "Edge");
                }
            }
            catch (Exception ex)
            {
                var item = new ListViewItem(new[] { "-", "Edge", $"Error: {ex.Message}", "-", "-" });
                item.ForeColor = dangerColor;
                listView.Items.Add(item);
            }
            return count;
        }
        
        private int ScanFirefoxHistory(ListView listView)
        {
            int count = 0;
            try
            {
                string firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Mozilla\Firefox\Profiles");
                
                if (Directory.Exists(firefoxPath))
                {
                    // Find default profile
                    foreach (var profileDir in Directory.GetDirectories(firefoxPath))
                    {
                        string placesDb = Path.Combine(profileDir, "places.sqlite");
                        if (File.Exists(placesDb))
                        {
                            count += ReadFirefoxHistoryDb(listView, placesDb);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var item = new ListViewItem(new[] { "-", "Firefox", $"Error: {ex.Message}", "-", "-" });
                item.ForeColor = dangerColor;
                listView.Items.Add(item);
            }
            return count;
        }
        
        private int ReadBrowserHistoryDb(ListView listView, string dbPath, string browserName)
        {
            int count = 0;
            string tempPath = Path.GetTempFileName();
            
            try
            {
                // Copy the database file because browsers lock it
                File.Copy(dbPath, tempPath, true);
                
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempPath};Mode=ReadOnly"))
                {
                    connection.Open();
                    
                    // Query last 100 history entries from today
                    string query = @"
                        SELECT url, title, datetime(last_visit_time/1000000-11644473600, 'unixepoch', 'localtime') as visit_time
                        FROM urls 
                        WHERE date(datetime(last_visit_time/1000000-11644473600, 'unixepoch', 'localtime')) = date('now', 'localtime')
                        ORDER BY last_visit_time DESC 
                        LIMIT 100";
                    
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string url = reader.GetString(0);
                                string title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string visitTime = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                
                                // Extract just the time portion
                                string timeDisplay = "";
                                if (DateTime.TryParse(visitTime, out DateTime dt))
                                {
                                    timeDisplay = dt.ToString("HH:mm:ss");
                                }
                                
                                // Categorize the URL
                                string category = CategorizeUrl(url);
                                
                                var item = new ListViewItem(new[] { 
                                    timeDisplay,
                                    browserName,
                                    url.Length > 80 ? url.Substring(0, 80) + "..." : url,
                                    title.Length > 50 ? title.Substring(0, 50) + "..." : title,
                                    category
                                });
                                
                                // Color code by category
                                if (category.Contains("YouTube") || category.Contains("Video"))
                                    item.ForeColor = dangerColor;
                                else if (category.Contains("Social"))
                                    item.ForeColor = warningColor;
                                else if (category.Contains("Search"))
                                    item.ForeColor = accentColor;
                                else
                                    item.ForeColor = textColor;
                                
                                listView.Items.Add(item);
                                count++;
                                
                                // Add to websiteLogs for database sync
                                websiteLogs.Add(new WebsiteLog
                                {
                                    Timestamp = dt,
                                    Browser = browserName,
                                    Url = url,
                                    Title = title,
                                    Duration = TimeSpan.Zero
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var item = new ListViewItem(new[] { "-", browserName, $"Read error: {ex.Message}", "-", "-" });
                item.ForeColor = warningColor;
                listView.Items.Add(item);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
            
            return count;
        }
        
        private int ReadFirefoxHistoryDb(ListView listView, string dbPath)
        {
            int count = 0;
            string tempPath = Path.GetTempFileName();
            
            try
            {
                File.Copy(dbPath, tempPath, true);
                
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempPath};Mode=ReadOnly"))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT p.url, p.title, datetime(h.visit_date/1000000, 'unixepoch', 'localtime') as visit_time
                        FROM moz_places p
                        JOIN moz_historyvisits h ON p.id = h.place_id
                        WHERE date(datetime(h.visit_date/1000000, 'unixepoch', 'localtime')) = date('now', 'localtime')
                        ORDER BY h.visit_date DESC
                        LIMIT 100";
                    
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string url = reader.GetString(0);
                                string title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string visitTime = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                
                                string timeDisplay = "";
                                if (DateTime.TryParse(visitTime, out DateTime dt))
                                {
                                    timeDisplay = dt.ToString("HH:mm:ss");
                                }
                                
                                string category = CategorizeUrl(url);
                                
                                var item = new ListViewItem(new[] { 
                                    timeDisplay,
                                    "Firefox",
                                    url.Length > 80 ? url.Substring(0, 80) + "..." : url,
                                    title.Length > 50 ? title.Substring(0, 50) + "..." : title,
                                    category
                                });
                                
                                if (category.Contains("YouTube") || category.Contains("Video"))
                                    item.ForeColor = dangerColor;
                                else if (category.Contains("Social"))
                                    item.ForeColor = warningColor;
                                else if (category.Contains("Search"))
                                    item.ForeColor = accentColor;
                                else
                                    item.ForeColor = textColor;
                                
                                listView.Items.Add(item);
                                count++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var item = new ListViewItem(new[] { "-", "Firefox", $"Read error: {ex.Message}", "-", "-" });
                item.ForeColor = warningColor;
                listView.Items.Add(item);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
            
            return count;
        }
        
        // Silent browser history scanning methods for auto-sync (no ListView required)
        private void ScanChromeHistorySince(DateTime since)
        {
            try
            {
                string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\User Data\Default\History");
                if (File.Exists(chromePath))
                    ReadBrowserHistoryDbSince(chromePath, "Chrome", since);
            }
            catch { }
        }
        
        private void ScanEdgeHistorySince(DateTime since)
        {
            try
            {
                string edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Edge\User Data\Default\History");
                if (File.Exists(edgePath))
                    ReadBrowserHistoryDbSince(edgePath, "Edge", since);
            }
            catch { }
        }
        
        private void ScanFirefoxHistorySince(DateTime since)
        {
            try
            {
                string firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(firefoxPath))
                {
                    foreach (var profileDir in Directory.GetDirectories(firefoxPath))
                    {
                        string placesDb = Path.Combine(profileDir, "places.sqlite");
                        if (File.Exists(placesDb))
                        {
                            ReadFirefoxHistoryDbSince(placesDb, since);
                            break;
                        }
                    }
                }
            }
            catch { }
        }
        
        private void ReadBrowserHistoryDbSince(string dbPath, string browserName, DateTime since)
        {
            string tempPath = Path.GetTempFileName();
            try
            {
                LogToFile($"Reading {browserName} history from: {dbPath}");
                // Try to copy the database file (it might be locked)
                try
                {
                    File.Copy(dbPath, tempPath, true);
                    LogToFile($"{browserName} DB copied successfully");
                }
                catch (Exception ex)
                {
                    // If file is locked, skip this scan
                    LogToFile($"{browserName} DB locked or error: {ex.Message}");
                    return;
                }
                
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempPath};Mode=ReadOnly"))
                {
                    connection.Open();
                    LogToFile($"{browserName} DB opened, querying since {since}");
                    
                    string query = @"
                        SELECT url, title, datetime(last_visit_time/1000000-11644473600, 'unixepoch', 'localtime') as visit_time
                        FROM urls 
                        WHERE last_visit_time/1000000-11644473600 > @sinceUnix
                        ORDER BY last_visit_time DESC 
                        LIMIT 100";
                    
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(query, connection))
                    {
                        long sinceUnix = ((DateTimeOffset)since).ToUnixTimeSeconds();
                        cmd.Parameters.AddWithValue("@sinceUnix", sinceUnix);
                        LogToFile($"{browserName} Query executing with sinceUnix={sinceUnix}");
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            int count = 0;
                            int totalRows = 0;
                            lock (websiteLogs)
                            {
                                while (reader.Read())
                                {
                                    totalRows++;
                                    string url = reader.GetString(0);
                                    string title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    string visitTime = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                    DateTime.TryParse(visitTime, out DateTime dt);
                                    
                                    if (totalRows <= 3) {
                                        LogToFile($"  Row {totalRows}: {url} at {visitTime}");
                                    }
                                    
                                    // Check if already in websiteLogs to avoid duplicates
                                    bool exists = websiteLogs.Any(w => w.Url == url && Math.Abs((w.Timestamp - dt).TotalSeconds) < 60);
                                    if (!exists)
                                    {
                                        websiteLogs.Add(new WebsiteLog
                                        {
                                            Timestamp = dt,
                                            Browser = browserName,
                                            Url = url,
                                            Title = title,
                                            Duration = TimeSpan.Zero
                                        });
                                        count++;
                                    }
                                }
                            }
                            LogToFile($"{browserName}: Found {totalRows} rows, Added {count} new history entries");
                        }
                    }
                }
            }
            catch { }
            finally { try { File.Delete(tempPath); } catch { } }
        }
        
        private void ReadFirefoxHistoryDbSince(string dbPath, DateTime since)
        {
            string tempPath = Path.GetTempFileName();
            try
            {
                File.Copy(dbPath, tempPath, true);
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempPath};Mode=ReadOnly"))
                {
                    connection.Open();
                    string query = @"
                        SELECT p.url, p.title, datetime(h.visit_date/1000000, 'unixepoch', 'localtime') as visit_time
                        FROM moz_places p
                        JOIN moz_historyvisits h ON p.id = h.place_id
                        WHERE h.visit_date/1000000 > @sinceUnix
                        ORDER BY h.visit_date DESC
                        LIMIT 50";
                    
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(query, connection))
                    {
                        long sinceUnix = ((DateTimeOffset)since).ToUnixTimeSeconds();
                        cmd.Parameters.AddWithValue("@sinceUnix", sinceUnix);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string url = reader.GetString(0);
                                string title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string visitTime = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                DateTime.TryParse(visitTime, out DateTime dt);
                                
                                bool exists = websiteLogs.Any(w => w.Url == url && Math.Abs((w.Timestamp - dt).TotalSeconds) < 60);
                                if (!exists)
                                {
                                    websiteLogs.Add(new WebsiteLog
                                    {
                                        Timestamp = dt,
                                        Browser = "Firefox",
                                        Url = url,
                                        Title = title,
                                        Duration = TimeSpan.Zero
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            finally { try { File.Delete(tempPath); } catch { } }
        }
        
        private string ExtractUrlFromTitle(string windowTitle)
        {
            // Extract website URL from browser window title
            string title = windowTitle.ToLower();
            
            // Known websites detection from title
            if (title.Contains("youtube"))
                return "https://www.youtube.com";
            if (title.Contains("google") && (title.Contains("search") || title.Contains(" - google")))
                return "https://www.google.com";
            if (title.Contains("facebook"))
                return "https://www.facebook.com";
            if (title.Contains("instagram"))
                return "https://www.instagram.com";
            if (title.Contains("twitter") || title.Contains(" / x") || title.Contains("x.com"))
                return "https://www.x.com";
            if (title.Contains("linkedin"))
                return "https://www.linkedin.com";
            if (title.Contains("whatsapp"))
                return "https://web.whatsapp.com";
            if (title.Contains("gmail") || (title.Contains("inbox") && title.Contains("mail")))
                return "https://mail.google.com";
            if (title.Contains("chatgpt") || title.Contains("openai"))
                return "https://chat.openai.com";
            if (title.Contains("github"))
                return "https://github.com";
            if (title.Contains("stackoverflow"))
                return "https://stackoverflow.com";
            if (title.Contains("reddit"))
                return "https://www.reddit.com";
            if (title.Contains("amazon"))
                return "https://www.amazon.com";
            if (title.Contains("netflix"))
                return "https://www.netflix.com";
            if (title.Contains("hotstar") || title.Contains("disney"))
                return "https://www.hotstar.com";
            if (title.Contains("flipkart"))
                return "https://www.flipkart.com";
            if (title.Contains("spotify"))
                return "https://www.spotify.com";
            if (title.Contains("microsoft") || title.Contains("outlook") || title.Contains("office"))
                return "https://www.microsoft.com";
            if (title.Contains("zoom"))
                return "https://zoom.us";
            if (title.Contains("teams"))
                return "https://teams.microsoft.com";
            if (title.Contains("slack"))
                return "https://slack.com";
            
            // Try to extract domain from title
            // Common format: "Page Title - Site Name - Browser"
            var parts = windowTitle.Split(new[] { " - ", " | ", " — " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                // Usually the site name is second-to-last or last before browser name
                string sitePart = parts.Length >= 3 ? parts[parts.Length - 2] : parts[parts.Length - 1];
                if (!string.IsNullOrWhiteSpace(sitePart) && 
                    !sitePart.ToLower().Contains("chrome") && 
                    !sitePart.ToLower().Contains("edge") && 
                    !sitePart.ToLower().Contains("firefox"))
                {
                    return $"https://{sitePart.ToLower().Replace(" ", "")}";
                }
            }
            
            return windowTitle;
        }
        
        private string CategorizeUrl(string url)
        {
            url = url.ToLower();
            
            // Video streaming
            if (url.Contains("youtube.com") || url.Contains("youtu.be"))
                return "🎬 YouTube";
            if (url.Contains("netflix.com"))
                return "🎬 Netflix";
            if (url.Contains("primevideo") || url.Contains("amazon.com/gp/video"))
                return "🎬 Prime Video";
            if (url.Contains("hotstar") || url.Contains("disney"))
                return "🎬 Disney+";
            if (url.Contains("twitch.tv"))
                return "🎬 Twitch";
            if (url.Contains("vimeo.com"))
                return "🎬 Vimeo";
            
            // Social Media
            if (url.Contains("facebook.com") || url.Contains("fb.com"))
                return "📱 Social (Facebook)";
            if (url.Contains("instagram.com"))
                return "📱 Social (Instagram)";
            if (url.Contains("twitter.com") || url.Contains("x.com"))
                return "📱 Social (X/Twitter)";
            if (url.Contains("linkedin.com"))
                return "💼 LinkedIn";
            if (url.Contains("reddit.com"))
                return "📱 Social (Reddit)";
            if (url.Contains("whatsapp"))
                return "📱 WhatsApp";
            if (url.Contains("telegram"))
                return "📱 Telegram";
            
            // Search Engines
            if (url.Contains("google.com/search") || url.Contains("google.co"))
                return "🔍 Google Search";
            if (url.Contains("bing.com/search"))
                return "🔍 Bing Search";
            if (url.Contains("duckduckgo.com"))
                return "🔍 DuckDuckGo";
            
            // Shopping
            if (url.Contains("amazon.") && !url.Contains("video"))
                return "🛒 Amazon";
            if (url.Contains("flipkart.com"))
                return "🛒 Flipkart";
            if (url.Contains("myntra.com"))
                return "🛒 Myntra";
            
            // Email
            if (url.Contains("mail.google") || url.Contains("gmail"))
                return "📧 Gmail";
            if (url.Contains("outlook") || url.Contains("hotmail"))
                return "📧 Outlook";
            
            // Work/Productivity
            if (url.Contains("github.com"))
                return "💻 GitHub";
            if (url.Contains("stackoverflow.com"))
                return "💻 StackOverflow";
            if (url.Contains("docs.google") || url.Contains("drive.google"))
                return "📄 Google Docs";
            if (url.Contains("office.com") || url.Contains("microsoft365"))
                return "📄 Microsoft 365";
            
            // News
            if (url.Contains("news") || url.Contains("bbc.") || url.Contains("cnn."))
                return "📰 News";
            
            return "🌐 Website";
        }
        
        private void RefreshAppUsage(ListView listView)
        {
            listView.Items.Clear();
            
            // Add currently active apps
            foreach (var app in activeApps)
            {
                var parts = app.Key.Split('|');
                string appName = parts.Length > 0 ? parts[0] : "Unknown";
                string title = parts.Length > 1 ? parts[1] : "";
                TimeSpan duration = DateTime.Now - app.Value;
                
                var item = new ListViewItem(new[] { 
                    app.Value.ToString("HH:mm:ss"),
                    "Running",
                    appName,
                    title,
                    duration.ToString(@"hh\:mm\:ss")
                });
                item.ForeColor = successColor;
                listView.Items.Add(item);
            }
            
            // Add completed app sessions
            foreach (var app in appUsageLogs.OrderByDescending(a => a.StartTime))
            {
                var item = new ListViewItem(new[] { 
                    app.StartTime.ToString("HH:mm:ss"),
                    app.EndTime.ToString("HH:mm:ss"),
                    app.AppName,
                    app.WindowTitle,
                    app.Duration.ToString(@"hh\:mm\:ss")
                });
                listView.Items.Add(item);
            }
            
            if (listView.Items.Count == 0)
            {
                var item = new ListViewItem(new[] { "-", "-", "No app usage tracked yet", "-", "-" });
                item.ForeColor = secondaryTextColor;
                listView.Items.Add(item);
            }
            
            statusLabel.Text = $"✓ App usage refreshed - {activeApps.Count} active, {appUsageLogs.Count} completed";
            statusLabel.ForeColor = accentColor;
        }
        
        private async Task GetGPSLocation(Label latLabel, Label lonLabel, Label accuracyLabel, 
            Label timestampLabel, Button btn)
        {
            btn.Enabled = false;
            btn.Text = "⏳ Getting Location...";
            
            try
            {
                // Use IP-based geolocation (works in .NET 6 without UWP)
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = await client.GetStringAsync("http://ip-api.com/json/");
                    
                    // Parse JSON manually (basic)
                    if (response.Contains("\"lat\"") && response.Contains("\"lon\""))
                    {
                        var latMatch = System.Text.RegularExpressions.Regex.Match(response, "\"lat\":([\\d.-]+)");
                        var lonMatch = System.Text.RegularExpressions.Regex.Match(response, "\"lon\":([\\d.-]+)");
                        var cityMatch = System.Text.RegularExpressions.Regex.Match(response, "\"city\":\"([^\"]+)\"");
                        var countryMatch = System.Text.RegularExpressions.Regex.Match(response, "\"country\":\"([^\"]+)\"");
                        var ispMatch = System.Text.RegularExpressions.Regex.Match(response, "\"isp\":\"([^\"]+)\"");
                        
                        if (latMatch.Success && lonMatch.Success)
                        {
                            string lat = latMatch.Groups[1].Value;
                            string lon = lonMatch.Groups[1].Value;
                            string city = cityMatch.Success ? cityMatch.Groups[1].Value : "Unknown";
                            string country = countryMatch.Success ? countryMatch.Groups[1].Value : "Unknown";
                            string isp = ispMatch.Success ? ispMatch.Groups[1].Value : "Unknown";
                            
                            latLabel.Text = $"Latitude: {lat}";
                            lonLabel.Text = $"Longitude: {lon}";
                            accuracyLabel.Text = $"Location: {city}, {country}  |  ISP: {isp}";
                            timestampLabel.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
                            
                            // Store for map link
                            latLabel.Tag = lat;
                            lonLabel.Tag = lon;
                            
                            statusLabel.Text = $"✓ Location obtained: {city}, {country} ({lat}, {lon})";
                            statusLabel.ForeColor = successColor;
                            
                            // Save GPS log to database
                            double latVal = double.Parse(lat, System.Globalization.CultureInfo.InvariantCulture);
                            double lonVal = double.Parse(lon, System.Globalization.CultureInfo.InvariantCulture);
                            _ = Task.Run(() => {
                                try {
                                    DatabaseHelper.InsertGpsLog(
                                        storedActivationKey, storedCompanyName, currentUser?.Name,
                                        latVal, lonVal, city, country, isp
                                    );
                                } catch { }
                            });
                        }
                        else
                        {
                            throw new Exception("Could not parse location data");
                        }
                    }
                    else
                    {
                        throw new Exception("Invalid response from location service");
                    }
                }
            }
            catch (Exception ex)
            {
                latLabel.Text = "Latitude: Unavailable";
                lonLabel.Text = "Longitude: Unavailable";
                accuracyLabel.Text = $"Error: {ex.Message}";
                timestampLabel.Text = $"Failed at: {DateTime.Now:HH:mm:ss}";
                
                statusLabel.Text = $"✗ Could not get location: {ex.Message}";
                statusLabel.ForeColor = dangerColor;
            }
            finally
            {
                btn.Enabled = true;
                btn.Text = "📍 Get Current Location";
            }
        }
        
        #endregion

        #region Logs Tab
        
        private TabPage CreateLogsTab()
        {
            TabPage tab = new TabPage("LOGS");
            tab.BackColor = contentBgColor;
            
            // Main split container
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 50,
                IsSplitterFixed = true,
                BackColor = contentBgColor,
                Panel1MinSize = 50,
                Panel2MinSize = 100
            };
            
            // Top panel with filter buttons
            Panel filterPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardColor,  // Use card color for filter panel
                Padding = new Padding(15)
            };
            
            FlowLayoutPanel buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            
            // Log category buttons
            string[] categories = { "🔄 All Logs", "🔐 Security", "📱 Application", "⚙️ System", "🔍 Search History", "💻 User Activity", "🔌 Startup/Shutdown" };
            Button[] categoryButtons = new Button[categories.Length];
            
            for (int i = 0; i < categories.Length; i++)
            {
                Button btn = new Button
                {
                    Text = categories[i],
                    Height = 32,
                    AutoSize = true,
                    Padding = new Padding(10, 0, 10, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = i == 0 ? accentColor : cardColor,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(5, 5, 5, 5),
                    Tag = i
                };
                btn.FlatAppearance.BorderSize = 0;
                categoryButtons[i] = btn;
                buttonFlow.Controls.Add(btn);
            }
            
            // Refresh button
            Button refreshBtn = new Button
            {
                Text = "🔄 Refresh",
                Height = 32,
                AutoSize = true,
                Padding = new Padding(10, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = successColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(20, 5, 5, 5)
            };
            refreshBtn.FlatAppearance.BorderSize = 0;
            buttonFlow.Controls.Add(refreshBtn);
            
            // Export button
            Button exportBtn = new Button
            {
                Text = "💾 Export",
                Height = 32,
                AutoSize = true,
                Padding = new Padding(10, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = warningColor,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 5, 5, 5)
            };
            exportBtn.FlatAppearance.BorderSize = 0;
            buttonFlow.Controls.Add(exportBtn);
            
            filterPanel.Controls.Add(buttonFlow);
            splitContainer.Panel1.Controls.Add(filterPanel);
            
            // Bottom panel with DataGridView for logs
            DataGridView logsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgColor,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(60, 60, 60),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9)
            };
            // Assign ListViews for auto-refresh (if present in this tab)
            // If you have ListViews for websites/applications/inactivity in other tabs, assign them there as well
            // Example (replace with actual ListView creation if needed):
            // websitesListView = ...;
            // applicationsListView = ...;
            // inactivityListView = ...;
            
            // Style the grid
            logsGrid.ColumnHeadersDefaultCellStyle.BackColor = primaryColor;
            logsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            logsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            logsGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);
            logsGrid.ColumnHeadersHeight = 40;
            
            logsGrid.DefaultCellStyle.BackColor = cardColor;
            logsGrid.DefaultCellStyle.ForeColor = Color.White;
            logsGrid.DefaultCellStyle.SelectionBackColor = accentColor;
            logsGrid.DefaultCellStyle.SelectionForeColor = Color.White;
            logsGrid.DefaultCellStyle.Padding = new Padding(5);
            logsGrid.RowTemplate.Height = 30;
            
            logsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(40, 45, 50);
            
            // Add columns
            logsGrid.Columns.Add("DateTime", "Date/Time");
            logsGrid.Columns.Add("Category", "Category");
            logsGrid.Columns.Add("Source", "Source");
            logsGrid.Columns.Add("EventID", "Event ID");
            logsGrid.Columns.Add("User", "User");
            logsGrid.Columns.Add("Computer", "Computer");
            logsGrid.Columns.Add("Description", "Description");
            
            logsGrid.Columns["DateTime"].Width = 150;
            logsGrid.Columns["Category"].Width = 100;
            logsGrid.Columns["Source"].Width = 150;
            logsGrid.Columns["EventID"].Width = 80;
            logsGrid.Columns["User"].Width = 120;
            logsGrid.Columns["Computer"].Width = 120;
            logsGrid.Columns["Description"].Width = 400;
            
            splitContainer.Panel2.Controls.Add(logsGrid);
            
            // Button click handlers
            int currentCategory = 0;
            
            foreach (Button btn in categoryButtons)
            {
                btn.Click += (s, e) =>
                {
                    int index = (int)((Button)s!).Tag;
                    currentCategory = index;
                    
                    // Update button colors
                    foreach (Button b in categoryButtons)
                    {
                        b.BackColor = (int)b.Tag == index ? accentColor : cardColor;
                    }
                    
                    // Load logs for selected category
                    LoadLogs(logsGrid, index);
                };
            }
            
            refreshBtn.Click += (s, e) => LoadLogs(logsGrid, currentCategory);
            
            exportBtn.Click += (s, e) =>
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV File|*.csv|Text File|*.txt",
                    Title = "Export Logs",
                    FileName = $"SystemLogs_{DateTime.Now:yyyyMMdd_HHmmss}"
                };
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ExportLogsToFile(logsGrid, saveDialog.FileName);
                }
            };
            
            tab.Controls.Add(splitContainer);
            
            // Load all logs initially
            tab.Enter += (s, e) => LoadLogs(logsGrid, 0);
            
            return tab;
        }
        
        private void LoadLogs(DataGridView grid, int category)
        {
            grid.Rows.Clear();
            Cursor.Current = Cursors.WaitCursor;
            
            try
            {
                string computerName = Environment.MachineName;
                
                switch (category)
                {
                    case 0: // All Logs
                        LoadWindowsEventLogs(grid, "Security", 100);
                        LoadWindowsEventLogs(grid, "Application", 100);
                        LoadWindowsEventLogs(grid, "System", 100);
                        LoadUserActivityLogs(grid);
                        LoadStartupShutdownLogs(grid);
                        break;
                    case 1: // Security
                        LoadWindowsEventLogs(grid, "Security", 500);
                        break;
                    case 2: // Application
                        LoadWindowsEventLogs(grid, "Application", 500);
                        break;
                    case 3: // System
                        LoadWindowsEventLogs(grid, "System", 500);
                        break;
                    case 4: // Search History
                        LoadSearchHistory(grid);
                        break;
                    case 5: // User Activity
                        LoadUserActivityLogs(grid);
                        LoadRecentFiles(grid);
                        LoadBrowserHistory(grid);
                        break;
                    case 6: // Startup/Shutdown
                        LoadStartupShutdownLogs(grid);
                        break;
                }
                
                // Sort by date descending
                if (grid.Rows.Count > 0)
                {
                    grid.Sort(grid.Columns["DateTime"], System.ComponentModel.ListSortDirection.Descending);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        
        private void LoadWindowsEventLogs(DataGridView grid, string logName, int maxEntries)
        {
            try
            {
                System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog(logName);
                int count = 0;
                
                for (int i = eventLog.Entries.Count - 1; i >= 0 && count < maxEntries; i--)
                {
                    try
                    {
                        var entry = eventLog.Entries[i];
                        string user = !string.IsNullOrEmpty(entry.UserName) ? entry.UserName : "SYSTEM";
                        
                        grid.Rows.Add(
                            entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss"),
                            logName,
                            entry.Source,
                            entry.InstanceId.ToString(),
                            user,
                            entry.MachineName,
                            TruncateText(entry.Message, 200)
                        );
                        count++;
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private void LoadUserActivityLogs(DataGridView grid)
        {
            try
            {
                string computerName = Environment.MachineName;
                string userName = Environment.UserName;
                
                // Get logon/logoff events from Security log
                System.Diagnostics.EventLog secLog = new System.Diagnostics.EventLog("Security");
                int count = 0;
                
                for (int i = secLog.Entries.Count - 1; i >= 0 && count < 200; i--)
                {
                    try
                    {
                        var entry = secLog.Entries[i];
                        // Logon events: 4624, Logoff: 4634, Failed logon: 4625
                        if (entry.InstanceId == 4624 || entry.InstanceId == 4634 || entry.InstanceId == 4625 ||
                            entry.InstanceId == 4648 || entry.InstanceId == 4672)
                        {
                            string eventType = entry.InstanceId switch
                            {
                                4624 => "User Logon",
                                4634 => "User Logoff",
                                4625 => "Failed Logon",
                                4648 => "Explicit Credential Logon",
                                4672 => "Special Privileges Assigned",
                                _ => "User Activity"
                            };
                            
                            grid.Rows.Add(
                                entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss"),
                                "User Activity",
                                eventType,
                                entry.InstanceId.ToString(),
                                entry.UserName ?? userName,
                                computerName,
                                TruncateText(entry.Message, 200)
                            );
                            count++;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private void LoadStartupShutdownLogs(DataGridView grid)
        {
            try
            {
                string computerName = Environment.MachineName;
                System.Diagnostics.EventLog sysLog = new System.Diagnostics.EventLog("System");
                int count = 0;
                
                for (int i = sysLog.Entries.Count - 1; i >= 0 && count < 200; i--)
                {
                    try
                    {
                        var entry = sysLog.Entries[i];
                        // Event IDs: 6005=Start, 6006=Shutdown, 6008=Unexpected shutdown, 6009=Windows version, 1074=Restart
                        if (entry.InstanceId == 6005 || entry.InstanceId == 6006 || entry.InstanceId == 6008 ||
                            entry.InstanceId == 6009 || entry.InstanceId == 1074 || entry.InstanceId == 41)
                        {
                            string eventType = entry.InstanceId switch
                            {
                                6005 => "System Startup",
                                6006 => "System Shutdown",
                                6008 => "Unexpected Shutdown",
                                6009 => "Windows Started",
                                1074 => "System Restart",
                                41 => "Kernel Power (Crash/Force Off)",
                                _ => "Power Event"
                            };
                            
                            grid.Rows.Add(
                                entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss"),
                                "Startup/Shutdown",
                                eventType,
                                entry.InstanceId.ToString(),
                                "SYSTEM",
                                computerName,
                                TruncateText(entry.Message, 200)
                            );
                            count++;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private void LoadSearchHistory(DataGridView grid)
        {
            try
            {
                string computerName = Environment.MachineName;
                string userName = Environment.UserName;
                
                // Windows Search history from registry
                string[] searchPaths = new string[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"
                };
                
                foreach (string path in searchPaths)
                {
                    try
                    {
                        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                foreach (string valueName in key.GetValueNames())
                                {
                                    if (valueName != "MRUListEx" && valueName != "MRUList")
                                    {
                                        object? value = key.GetValue(valueName);
                                        string searchText = "";
                                        
                                        if (value is byte[] bytes)
                                        {
                                            searchText = System.Text.Encoding.Unicode.GetString(bytes).TrimEnd('\0');
                                        }
                                        else if (value != null)
                                        {
                                            searchText = value.ToString() ?? "";
                                        }
                                        
                                        if (!string.IsNullOrWhiteSpace(searchText))
                                        {
                                            string category = path.Contains("WordWheel") ? "Windows Search" :
                                                            path.Contains("TypedPaths") ? "Explorer Path" : "Run Command";
                                            
                                            grid.Rows.Add(
                                                DateTime.Now.ToString("yyyy-MM-dd") + " --:--:--",
                                                "Search History",
                                                category,
                                                "-",
                                                userName,
                                                computerName,
                                                searchText
                                            );
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private void LoadRecentFiles(DataGridView grid)
        {
            try
            {
                string computerName = Environment.MachineName;
                string userName = Environment.UserName;
                string recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                
                if (System.IO.Directory.Exists(recentPath))
                {
                    var files = System.IO.Directory.GetFiles(recentPath, "*.lnk")
                        .Select(f => new System.IO.FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .Take(100);
                    
                    foreach (var file in files)
                    {
                        grid.Rows.Add(
                            file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            "Recent Files",
                            "File Access",
                            "-",
                            userName,
                            computerName,
                            System.IO.Path.GetFileNameWithoutExtension(file.Name)
                        );
                    }
                }
            }
            catch { }
        }
        
        private void LoadBrowserHistory(DataGridView grid)
        {
            try
            {
                string computerName = Environment.MachineName;
                string userName = Environment.UserName;
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // Check for Edge/Chrome history databases
                string[] browserPaths = new string[]
                {
                    System.IO.Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\History"),
                    System.IO.Path.Combine(localAppData, @"Google\Chrome\User Data\Default\History")
                };
                
                foreach (string browserPath in browserPaths)
                {
                    if (System.IO.File.Exists(browserPath))
                    {
                        string browserName = browserPath.Contains("Edge") ? "Microsoft Edge" : "Google Chrome";
                        grid.Rows.Add(
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            "Browser History",
                            browserName,
                            "-",
                            userName,
                            computerName,
                            $"Browser history available at: {browserPath}"
                        );
                    }
                }
            }
            catch { }
        }
        
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\r", " ").Replace("\n", " ");
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
        
        private void ExportLogsToFile(DataGridView grid, string filePath)
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
                {
                    // Write header
                    string[] headers = new string[grid.Columns.Count];
                    for (int i = 0; i < grid.Columns.Count; i++)
                    {
                        headers[i] = grid.Columns[i].HeaderText;
                    }
                    writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h}\"")));
                    
                    // Write data
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        string[] cells = new string[grid.Columns.Count];
                        for (int i = 0; i < grid.Columns.Count; i++)
                        {
                            cells[i] = $"\"{row.Cells[i].Value?.ToString()?.Replace("\"", "\"\"") ?? ""}\"";
                        }
                        writer.WriteLine(string.Join(",", cells));
                    }
                }
                
                MessageBox.Show($"Logs exported successfully to:\n{filePath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting logs: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #endregion
        
        #region System Info Tab
        
        /// <summary>
        /// Creates a lazy-loaded System Info tab that doesn't freeze the UI on startup
        /// </summary>
        private TabPage CreateSystemInfoTabLazy()
        {
            TabPage tab = new TabPage("SYSTEM INFO");
            tab.BackColor = contentBgColor;
            
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };
            
            // Loading indicator
            Label loadingLabel = new Label
            {
                Text = "⏳ Loading system information...",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize = true,
                Location = new Point(50, 50)
            };
            mainPanel.Controls.Add(loadingLabel);
            
            tab.Controls.Add(mainPanel);
            
            // Load data async when tab is first shown
            bool loaded = false;
            tab.VisibleChanged += async (s, e) =>
            {
                if (tab.Visible && !loaded)
                {
                    loaded = true;
                    await Task.Run(() => { /* Give UI time to render */ System.Threading.Thread.Sleep(100); });
                    
                    // Load system info on background thread
                    var generalInfo = await Task.Run(() => GetGeneralInfo());
                    var osInfo = await Task.Run(() => GetOSInfo());
                    var cpuInfo = await Task.Run(() => GetCPUInfo());
                    var memoryInfo = await Task.Run(() => GetMemoryInfo());
                    var storageInfo = await Task.Run(() => GetStorageInfo());
                    var networkInfo = await Task.Run(() => GetNetworkAdaptersInfo());
                    var usersInfo = await Task.Run(() => GetUserAccountsInfo());
                    var graphicsInfo = await Task.Run(() => GetGraphicsInfo());
                    
                    // Update UI on main thread
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => PopulateSystemInfoTab(mainPanel, generalInfo, osInfo, cpuInfo, memoryInfo, storageInfo, networkInfo, usersInfo, graphicsInfo)));
                    }
                    else
                    {
                        PopulateSystemInfoTab(mainPanel, generalInfo, osInfo, cpuInfo, memoryInfo, storageInfo, networkInfo, usersInfo, graphicsInfo);
                    }
                }
            };
            
            return tab;
        }
        
        private void PopulateSystemInfoTab(Panel mainPanel, string generalInfo, string osInfo, string cpuInfo, string memoryInfo, string storageInfo, string networkInfo, string usersInfo, string graphicsInfo)
        {
            mainPanel.Controls.Clear();
            
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            
            // Refresh button at top
            Button refreshBtn = new Button
            {
                Text = "🔄 Refresh System Information",
                Height = 40,
                Width = 250,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 20)
            };
            refreshBtn.FlatAppearance.BorderSize = 0;
            flowPanel.Controls.Add(refreshBtn);
            
            flowPanel.Controls.Add(CreateSystemInfoSection("🖥️ General Information", generalInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("💿 Operating System", osInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("⚡ Processor", cpuInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("🧠 Memory (RAM)", memoryInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("💾 Storage Drives", storageInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("🌐 Network Adapters", networkInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("👥 User Accounts", usersInfo));
            flowPanel.Controls.Add(CreateSystemInfoSection("🎮 Graphics", graphicsInfo));
            
            mainPanel.Controls.Add(flowPanel);
            
            refreshBtn.Click += async (s, e) =>
            {
                refreshBtn.Enabled = false;
                refreshBtn.Text = "⏳ Loading...";
                
                var g = await Task.Run(() => GetGeneralInfo());
                var o = await Task.Run(() => GetOSInfo());
                var c = await Task.Run(() => GetCPUInfo());
                var m = await Task.Run(() => GetMemoryInfo());
                var st = await Task.Run(() => GetStorageInfo());
                var n = await Task.Run(() => GetNetworkAdaptersInfo());
                var u = await Task.Run(() => GetUserAccountsInfo());
                var gr = await Task.Run(() => GetGraphicsInfo());
                
                PopulateSystemInfoTab(mainPanel, g, o, c, m, st, n, u, gr);
            };
        }
        
        private TabPage CreateSystemInfoTab()
        {
            TabPage tab = new TabPage("SYSTEM INFO");
            tab.BackColor = contentBgColor;
            
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25),
                BackColor = contentBgColor
            };
            
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            
            // Refresh button at top
            Button refreshBtn = new Button
            {
                Text = "🔄 Refresh System Information",
                Height = 40,
                Width = 250,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 20)
            };
            refreshBtn.FlatAppearance.BorderSize = 0;
            flowPanel.Controls.Add(refreshBtn);
            
            // System info sections
            Panel generalPanel = CreateSystemInfoSection("🖥️ General Information", GetGeneralInfo());
            Panel osPanel = CreateSystemInfoSection("💿 Operating System", GetOSInfo());
            Panel cpuPanel = CreateSystemInfoSection("⚡ Processor", GetCPUInfo());
            Panel memoryPanel = CreateSystemInfoSection("🧠 Memory (RAM)", GetMemoryInfo());
            Panel storagePanel = CreateSystemInfoSection("💾 Storage Drives", GetStorageInfo());
            Panel networkPanel = CreateSystemInfoSection("🌐 Network Adapters", GetNetworkAdaptersInfo());
            Panel usersPanel = CreateSystemInfoSection("👥 User Accounts", GetUserAccountsInfo());
            Panel graphicsPanel = CreateSystemInfoSection("🎮 Graphics", GetGraphicsInfo());
            
            flowPanel.Controls.Add(generalPanel);
            flowPanel.Controls.Add(osPanel);
            flowPanel.Controls.Add(cpuPanel);
            flowPanel.Controls.Add(memoryPanel);
            flowPanel.Controls.Add(storagePanel);
            flowPanel.Controls.Add(networkPanel);
            flowPanel.Controls.Add(usersPanel);
            flowPanel.Controls.Add(graphicsPanel);
            
            mainPanel.Controls.Add(flowPanel);
            
            refreshBtn.Click += (s, e) =>
            {
                flowPanel.Controls.Clear();
                flowPanel.Controls.Add(refreshBtn);
                flowPanel.Controls.Add(CreateSystemInfoSection("🖥️ General Information", GetGeneralInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("💿 Operating System", GetOSInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("⚡ Processor", GetCPUInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("🧠 Memory (RAM)", GetMemoryInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("💾 Storage Drives", GetStorageInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("🌐 Network Adapters", GetNetworkAdaptersInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("👥 User Accounts", GetUserAccountsInfo()));
                flowPanel.Controls.Add(CreateSystemInfoSection("🎮 Graphics", GetGraphicsInfo()));
            };
            
            tab.Controls.Add(mainPanel);
            return tab;
        }
        
        private Panel CreateSystemInfoSection(string title, string content)
        {
            Panel section = new Panel
            {
                Width = 1100,
                AutoSize = true,
                MinimumSize = new Size(1100, 100),
                Margin = new Padding(0, 0, 0, 15),
                BackColor = cardColor,
                Padding = new Padding(20)
            };
            
            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            
            Label contentLabel = new Label
            {
                Text = content,
                Font = new Font("Consolas", 10),
                ForeColor = Color.LightGray,
                AutoSize = true,
                MaximumSize = new Size(1050, 0),
                Location = new Point(20, 50)
            };
            
            section.Controls.Add(titleLabel);
            section.Controls.Add(contentLabel);
            
            // Adjust height based on content
            section.Height = contentLabel.Bottom + 20;
            
            return section;
        }
        
        private string GetGeneralInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                sb.AppendLine($"Computer Name:     {Environment.MachineName}");
                sb.AppendLine($"User Name:         {Environment.UserName}");
                sb.AppendLine($"User Domain:       {Environment.UserDomainName}");
                sb.AppendLine($"System Directory:  {Environment.SystemDirectory}");
                sb.AppendLine($"Machine Type:      {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
                sb.AppendLine($"Processor Count:   {Environment.ProcessorCount} cores");
                sb.AppendLine($"System Uptime:     {GetSystemUptime()}");
                sb.AppendLine($"Last Boot Time:    {GetLastBootTime()}");
                
                // Get manufacturer info
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        sb.AppendLine($"Manufacturer:      {obj["Manufacturer"]}");
                        sb.AppendLine($"Model:             {obj["Model"]}");
                        sb.AppendLine($"System Type:       {obj["SystemType"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error retrieving info: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetOSInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                sb.AppendLine($"OS Version:        {Environment.OSVersion}");
                sb.AppendLine($".NET Version:      {Environment.Version}");
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        sb.AppendLine($"OS Name:           {obj["Caption"]}");
                        sb.AppendLine($"Version:           {obj["Version"]}");
                        sb.AppendLine($"Build Number:      {obj["BuildNumber"]}");
                        sb.AppendLine($"Architecture:      {obj["OSArchitecture"]}");
                        sb.AppendLine($"Serial Number:     {obj["SerialNumber"]}");
                        sb.AppendLine($"Install Date:      {ManagementDateTimeConverter.ToDateTime(obj["InstallDate"]?.ToString() ?? "")}");
                        sb.AppendLine($"Registered User:   {obj["RegisteredUser"]}");
                        sb.AppendLine($"Organization:      {obj["Organization"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetCPUInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    int cpuIndex = 0;
                    foreach (var obj in searcher.Get())
                    {
                        if (cpuIndex > 0) sb.AppendLine();
                        sb.AppendLine($"CPU #{cpuIndex + 1}");
                        sb.AppendLine($"  Name:            {obj["Name"]}");
                        sb.AppendLine($"  Manufacturer:    {obj["Manufacturer"]}");
                        sb.AppendLine($"  Cores:           {obj["NumberOfCores"]}");
                        sb.AppendLine($"  Logical CPUs:    {obj["NumberOfLogicalProcessors"]}");
                        sb.AppendLine($"  Max Clock:       {obj["MaxClockSpeed"]} MHz");
                        sb.AppendLine($"  Current Clock:   {obj["CurrentClockSpeed"]} MHz");
                        sb.AppendLine($"  L2 Cache:        {obj["L2CacheSize"]} KB");
                        sb.AppendLine($"  L3 Cache:        {obj["L3CacheSize"]} KB");
                        sb.AppendLine($"  Architecture:    {GetCPUArchitecture(Convert.ToInt32(obj["Architecture"]))}");
                        cpuIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetMemoryInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                ulong totalMemory = 0;
                int slotIndex = 0;
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        ulong capacity = Convert.ToUInt64(obj["Capacity"]);
                        totalMemory += capacity;
                        
                        sb.AppendLine($"Slot #{slotIndex + 1}:");
                        sb.AppendLine($"  Capacity:        {FormatBytes(capacity)}");
                        sb.AppendLine($"  Speed:           {obj["Speed"]} MHz");
                        sb.AppendLine($"  Manufacturer:    {obj["Manufacturer"]}");
                        sb.AppendLine($"  Part Number:     {obj["PartNumber"]?.ToString()?.Trim()}");
                        sb.AppendLine($"  Form Factor:     {GetMemoryFormFactor(Convert.ToInt32(obj["FormFactor"]))}");
                        sb.AppendLine($"  Memory Type:     {GetMemoryType(Convert.ToInt32(obj["MemoryType"]))}");
                        sb.AppendLine();
                        slotIndex++;
                    }
                }
                
                sb.AppendLine($"═══════════════════════════════════════");
                sb.AppendLine($"Total Installed:   {FormatBytes(totalMemory)}");
                
                // Get available memory
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        ulong freePhysical = Convert.ToUInt64(obj["FreePhysicalMemory"]) * 1024;
                        ulong totalVisible = Convert.ToUInt64(obj["TotalVisibleMemorySize"]) * 1024;
                        sb.AppendLine($"Available:         {FormatBytes(freePhysical)}");
                        sb.AppendLine($"Used:              {FormatBytes(totalVisible - freePhysical)}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetStorageInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                // Physical drives
                sb.AppendLine("══ Physical Drives ══");
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        sb.AppendLine($"  Model:           {obj["Model"]}");
                        sb.AppendLine($"  Serial:          {obj["SerialNumber"]?.ToString()?.Trim()}");
                        sb.AppendLine($"  Size:            {FormatBytes(Convert.ToUInt64(obj["Size"] ?? 0))}");
                        sb.AppendLine($"  Interface:       {obj["InterfaceType"]}");
                        sb.AppendLine($"  Media Type:      {obj["MediaType"]}");
                        sb.AppendLine($"  Partitions:      {obj["Partitions"]}");
                        sb.AppendLine();
                    }
                }
                
                sb.AppendLine("══ Logical Drives ══");
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.IsReady)
                        {
                            sb.AppendLine($"  Drive {drive.Name}");
                            sb.AppendLine($"    Label:         {drive.VolumeLabel}");
                            sb.AppendLine($"    Type:          {drive.DriveType}");
                            sb.AppendLine($"    Format:        {drive.DriveFormat}");
                            sb.AppendLine($"    Total Size:    {FormatBytes((ulong)drive.TotalSize)}");
                            sb.AppendLine($"    Free Space:    {FormatBytes((ulong)drive.TotalFreeSpace)}");
                            sb.AppendLine($"    Used Space:    {FormatBytes((ulong)(drive.TotalSize - drive.TotalFreeSpace))}");
                            double usedPercent = ((double)(drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize) * 100;
                            sb.AppendLine($"    Used:          {usedPercent:F1}%");
                            sb.AppendLine();
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetNetworkAdaptersInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up || n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                
                foreach (var adapter in adapters)
                {
                    sb.AppendLine($"Adapter: {adapter.Name}");
                    sb.AppendLine($"  Description:     {adapter.Description}");
                    sb.AppendLine($"  Type:            {adapter.NetworkInterfaceType}");
                    sb.AppendLine($"  Status:          {adapter.OperationalStatus}");
                    sb.AppendLine($"  Speed:           {adapter.Speed / 1000000} Mbps");
                    sb.AppendLine($"  MAC Address:     {FormatMacAddress(adapter.GetPhysicalAddress().ToString())}");
                    
                    var ipProps = adapter.GetIPProperties();
                    foreach (var ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            sb.AppendLine($"  IPv4 Address:    {ip.Address}");
                            sb.AppendLine($"  Subnet Mask:     {ip.IPv4Mask}");
                        }
                        else if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            sb.AppendLine($"  IPv6 Address:    {ip.Address}");
                        }
                    }
                    
                    foreach (var gateway in ipProps.GatewayAddresses)
                    {
                        sb.AppendLine($"  Gateway:         {gateway.Address}");
                    }
                    
                    foreach (var dns in ipProps.DnsAddresses)
                    {
                        sb.AppendLine($"  DNS:             {dns}");
                    }
                    
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetUserAccountsInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                int totalUsers = 0;
                int activeUsers = 0;
                int disabledUsers = 0;
                
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount WHERE LocalAccount=True"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        totalUsers++;
                        bool disabled = Convert.ToBoolean(obj["Disabled"]);
                        if (disabled) disabledUsers++; else activeUsers++;
                        
                        string status = disabled ? "❌ Disabled" : "✅ Active";
                        sb.AppendLine($"User: {obj["Name"]}");
                        sb.AppendLine($"  Full Name:       {obj["FullName"]}");
                        sb.AppendLine($"  Status:          {status}");
                        sb.AppendLine($"  Description:     {obj["Description"]}");
                        sb.AppendLine($"  SID:             {obj["SID"]}");
                        sb.AppendLine();
                    }
                }
                
                sb.AppendLine($"═══════════════════════════════════════");
                sb.AppendLine($"Total Users:       {totalUsers}");
                sb.AppendLine($"Active Users:      {activeUsers}");
                sb.AppendLine($"Disabled Users:    {disabledUsers}");
                
                // Current logged-in users
                sb.AppendLine();
                sb.AppendLine("══ Currently Logged In ══");
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogonSession WHERE LogonType = 2 OR LogonType = 10"))
                {
                    foreach (var session in searcher.Get())
                    {
                        string logonId = session["LogonId"]?.ToString() ?? "";
                        using (var userSearcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_LogonSession.LogonId='{logonId}'}} WHERE AssocClass=Win32_LoggedOnUser"))
                        {
                            foreach (var user in userSearcher.Get())
                            {
                                sb.AppendLine($"  {user["Name"]} (Domain: {user["Domain"]})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        private string GetGraphicsInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    int gpuIndex = 0;
                    foreach (var obj in searcher.Get())
                    {
                        if (gpuIndex > 0) sb.AppendLine();
                        sb.AppendLine($"GPU #{gpuIndex + 1}");
                        sb.AppendLine($"  Name:            {obj["Name"]}");
                        sb.AppendLine($"  Adapter RAM:     {FormatBytes(Convert.ToUInt64(obj["AdapterRAM"] ?? 0))}");
                        sb.AppendLine($"  Driver Version:  {obj["DriverVersion"]}");
                        sb.AppendLine($"  Driver Date:     {obj["DriverDate"]}");
                        sb.AppendLine($"  Resolution:      {obj["CurrentHorizontalResolution"]}x{obj["CurrentVerticalResolution"]}");
                        sb.AppendLine($"  Refresh Rate:    {obj["CurrentRefreshRate"]} Hz");
                        sb.AppendLine($"  Bits Per Pixel:  {obj["CurrentBitsPerPixel"]}");
                        sb.AppendLine($"  Video Mode:      {obj["VideoModeDescription"]}");
                        gpuIndex++;
                    }
                }
                
                // Monitor info
                sb.AppendLine();
                sb.AppendLine("══ Monitors ══");
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        sb.AppendLine($"  Name:            {obj["Name"]}");
                        sb.AppendLine($"  Screen Width:    {obj["ScreenWidth"]}");
                        sb.AppendLine($"  Screen Height:   {obj["ScreenHeight"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        // Helper methods for System Info
        private string GetSystemUptime()
        {
            try
            {
                TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes";
            }
            catch
            {
                return "Unknown";
            }
        }
        
        private string GetLastBootTime()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"]?.ToString() ?? "").ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            }
            catch { }
            return "Unknown";
        }
        
        private string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        
        private string FormatMacAddress(string mac)
        {
            if (string.IsNullOrEmpty(mac) || mac.Length < 12) return mac;
            return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
        }
        
        private string GetCPUArchitecture(int arch)
        {
            return arch switch
            {
                0 => "x86",
                5 => "ARM",
                6 => "IA64",
                9 => "x64",
                12 => "ARM64",
                _ => "Unknown"
            };
        }
        
        private string GetMemoryFormFactor(int formFactor)
        {
            return formFactor switch
            {
                8 => "DIMM",
                12 => "SODIMM",
                _ => formFactor.ToString()
            };
        }
        
        private string GetMemoryType(int memType)
        {
            return memType switch
            {
                20 => "DDR",
                21 => "DDR2",
                22 => "DDR2 FB-DIMM",
                24 => "DDR3",
                26 => "DDR4",
                34 => "DDR5",
                _ => memType == 0 ? "Unknown" : $"Type {memType}"
            };
        }
        
        #endregion

        #region System Tray and Background Operation
        
        private void InitializeSystemTray()
        {
            try
            {
                // Create context menu for tray icon
                trayMenu = new ContextMenuStrip();
                trayMenu.BackColor = cardColor;
                trayMenu.ForeColor = textColor;
                trayMenu.Font = new Font("Segoe UI", 10f);
                trayMenu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
                
                var openItem = new ToolStripMenuItem("🖥 Open Desktop Controller");
                openItem.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                openItem.Click += (s, e) => ShowFromTray();
                trayMenu.Items.Add(openItem);
                
                trayMenu.Items.Add(new ToolStripSeparator());
                
                var statusItem = new ToolStripMenuItem($"👤 {currentUser?.Name ?? "User"}");
                statusItem.Enabled = false;
                trayMenu.Items.Add(statusItem);
                
                var companyItem = new ToolStripMenuItem($"🏢 {storedCompanyName ?? "Company"}");
                companyItem.Enabled = false;
                trayMenu.Items.Add(companyItem);
                
                trayMenu.Items.Add(new ToolStripSeparator());
                
                // Punch In/Out item - store reference for updates
                trayPunchItem = new ToolStripMenuItem(isPunchedIn ? "⏹ Punch Out" : "⏺ Punch In");
                trayPunchItem.Click += (s, e) => {
                    // Toggle punch from tray
                    if (punchButton != null)
                    {
                        punchButton.PerformClick();
                    }
                };
                trayMenu.Items.Add(trayPunchItem);
                
                // Break item - store reference for updates, hidden until punched in
                trayBreakItem = new ToolStripMenuItem("☕ Take Break");
                trayBreakItem.Visible = isPunchedIn;  // Only visible when punched in
                trayBreakItem.Click += (s, e) => {
                    // Toggle break from tray
                    if (breakButton != null)
                    {
                        breakButton.PerformClick();
                    }
                };
                trayMenu.Items.Add(trayBreakItem);
                
                trayMenu.Items.Add(new ToolStripSeparator());
                
                var changePasswordItem = new ToolStripMenuItem("🔑 Change Company Password");
                changePasswordItem.Click += (s, e) => ChangeCompanyPassword();
                trayMenu.Items.Add(changePasswordItem);
                
                trayMenu.Items.Add(new ToolStripSeparator());
                
                var exitItem = new ToolStripMenuItem("⏻ Exit (Password Required)");
                exitItem.Click += (s, e) => RequestPasswordExit();
                trayMenu.Items.Add(exitItem);
                
                // Create tray icon
                trayIcon = new NotifyIcon();
                trayIcon.Text = "Desktop Controller Pro - Running";
                trayIcon.Visible = true;
                trayIcon.ContextMenuStrip = trayMenu;
                
                // Set tray icon
                try
                {
                    string exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "DesktopController.exe");
                    if (File.Exists(exePath))
                    {
                        trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    }
                    else
                    {
                        trayIcon.Icon = SystemIcons.Shield;
                    }
                }
                catch
                {
                    trayIcon.Icon = SystemIcons.Shield;
                }
                
                // Double-click to show window
                trayIcon.DoubleClick += (s, e) => ShowFromTray();
                
                // Show balloon tip on startup
                trayIcon.BalloonTipTitle = "Desktop Controller Pro";
                trayIcon.BalloonTipText = "Application is running in background. Double-click to open.";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                
                LogToFile("System tray initialized");
            }
            catch (Exception ex)
            {
                LogToFile($"Error initializing system tray: {ex.Message}");
            }
        }
        
        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();
        }
        
        private void OpenScreenshotSettings()
        {
            try
            {
                using (var settingsForm = new ScreenshotSettingsForm())
                {
                    if (settingsForm.ShowDialog(this) == DialogResult.OK)
                    {
                        // Reload settings
                        screenshotSettings = ScreenshotSettings.Load();
                        
                        // Update timer interval
                        screenshotTimer.Stop();
                        screenshotTimer.Interval = screenshotSettings.GetIntervalMilliseconds();
                        
                        // Start or stop timer based on settings
                        if (screenshotSettings.IsEnabled && screenshotSettings.StorageType != "none")
                        {
                            screenshotTimer.Start();
                            LogToFile($"Screenshot timer restarted - Interval: {screenshotSettings.IntervalValue} {screenshotSettings.IntervalUnit}");
                        }
                        else
                        {
                            LogToFile("Screenshot timer stopped - Screenshots disabled");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error opening screenshot settings: {ex.Message}");
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (minimizeToTray && this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(2000, "Desktop Controller Pro", 
                        "Application minimized to system tray. Double-click icon to restore.", 
                        ToolTipIcon.Info);
                }
            }
        }
        
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // If exit is allowed (password entered), proceed with close
            if (isExitAllowed)
            {
                // Cleanup
                StopActivityTracking();
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
                return;
            }
            
            // If user clicked X button or Alt+F4, minimize to tray instead of closing
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(2000, "Desktop Controller Pro", 
                        "Application is still running in background.\nRight-click tray icon for options.", 
                        ToolTipIcon.Info);
                }
            }
        }
        
        private void RequestPasswordExit()
        {
            // Use the new company password system - ALWAYS ask, never use cache
            if (PasswordManager.PromptForPasswordAlways(storedCompanyName, "exit the application"))
            {
                isExitAllowed = true;
                LogToFile("Exit authorized with company password");
                Application.Exit();
            }
            else
            {
                LogToFile("Exit attempt cancelled or wrong password");
            }
        }
        
        /// <summary>
        /// Change/Reset company password - requires current password first
        /// </summary>
        private void ChangeCompanyPassword()
        {
            if (string.IsNullOrEmpty(storedCompanyName))
            {
                MessageBox.Show("No company found. Please login first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // First verify current password
            if (!PasswordManager.PromptForPasswordAlways(storedCompanyName, "change password"))
            {
                return; // Wrong password or cancelled
            }
            
            // Show change password dialog
            if (PasswordManager.ShowChangePassword(storedCompanyName))
            {
                MessageBox.Show("Password changed successfully!\n\nThis new password will be used for all users in this company.", 
                    "Password Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LogToFile($"Company password changed for: {storedCompanyName}");
            }
        }
        
        private bool VerifyExitPassword(string password)
        {
            // Use company password verification
            return PasswordManager.VerifyPassword(storedCompanyName, password);
        }
        
        private void SetupAutoStart()
        {
            try
            {
                string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(appPath)) return;
                
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.SetValue("DesktopControllerPro", $"\"{appPath}\" --autostart");
                        LogToFile("Auto-start registry entry created");
                    }
                }
                
                // Also save auto-login flag
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        key.SetValue("AutoLogin", "1");
                        key.SetValue("LastActivationKey", storedActivationKey ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error setting up auto-start: {ex.Message}");
            }
        }
        
        public static bool IsAutoStartMode()
        {
            string[] args = Environment.GetCommandLineArgs();
            return args.Any(arg => arg.Equals("--autostart", StringComparison.OrdinalIgnoreCase));
        }
        
        #endregion

        #endregion
    }
    
    // Password Dialog for protected exit
    public class PasswordDialog : Form
    {
        private TextBox txtPassword = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        public string EnteredPassword => txtPassword.Text;
        
        public PasswordDialog(string title)
        {
            this.Text = title;
            this.Size = new Size(350, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 41, 59);
            this.ForeColor = Color.White;
            
            var lblPassword = new Label
            {
                Text = "Enter Password:",
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f)
            };
            
            txtPassword = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(290, 30),
                PasswordChar = '●',
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f)
            };
            
            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(120, 95),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnOK.FlatAppearance.BorderSize = 0;
            
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(220, 95),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            
            txtPassword.Focus();
        }
    }
    
    // Dark color table for tray menu
    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => Color.FromArgb(71, 85, 105);
        public override Color MenuItemBorder => Color.FromArgb(99, 102, 241);
        public override Color MenuItemSelected => Color.FromArgb(51, 65, 85);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(51, 65, 85);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(51, 65, 85);
        public override Color MenuStripGradientBegin => Color.FromArgb(30, 41, 59);
        public override Color MenuStripGradientEnd => Color.FromArgb(30, 41, 59);
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 41, 59);
        public override Color ImageMarginGradientBegin => Color.FromArgb(30, 41, 59);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 41, 59);
        public override Color ImageMarginGradientEnd => Color.FromArgb(30, 41, 59);
        public override Color SeparatorDark => Color.FromArgb(71, 85, 105);
        public override Color SeparatorLight => Color.FromArgb(71, 85, 105);
    }
    
    // Data classes for audit logging
    public class ActivityLog
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public string Duration { get; set; } = "";
    }
    
    public class WebsiteLog
    {
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string Browser { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public bool IsSynced { get; set; } = false;
    }
    
    public class AppUsageLog
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string AppName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public TimeSpan Duration => EndTime - StartTime;
        public bool IsSynced { get; set; } = false;
    }
    
    public class InactivityLog
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool IsSynced { get; set; } = false;
    }
}
