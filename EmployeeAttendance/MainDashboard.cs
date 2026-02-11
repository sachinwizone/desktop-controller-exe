using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;

namespace EmployeeAttendance
{
    public class MainDashboard : Form
    {
        // P/Invoke for window management
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // P/Invoke for idle detection
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        
        // UI Components
        private Label statusLabel = null!;
        private Label workHoursLabel = null!;
        private Label breakTimeLabel = null!;
        private Label systemIdLabel = null!;  // Display System ID for remote viewing
        private Button punchInButton = null!;
        private Button punchOutButton = null!;
        private Button breakStartButton = null!;
        private Button breakStopButton = null!;
        private Panel activityPanel = null!;
        private Button webDashboardButton = null!;
        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        
        // Audit Tracker - handles app tracking, web logs, inactivity, screenshots, live stream
        private AuditTracker? auditTracker = null;
        
        // System Info Collector - sends system details to web
        private SystemInfoCollector? systemInfoCollector = null;
        
        // System Data Sender - collects and sends installed software, hardware info
        private SystemDataSender? systemDataSender = null;
        
        // Tray Chat System - peer-to-peer chat in system tray
        private TrayChatSystem? trayChatSystem = null;

        // File Activity Tracker - tracks file renames and email activity
        private FileActivityTracker? fileActivityTracker = null;

        // System Control Handler - handles remote control commands from web dashboard
        private SystemControlHandler? systemControlHandler = null;
        private UsbFileMonitor? usbFileMonitor = null;

        // Installed Apps Collector - syncs installed software list to database
        private InstalledAppsCollector? installedAppsCollector = null;
        
        // State
        private string activationKey = "";
        private string username = "";
        private string department = "";
        private string companyName = "";
        private string displayName = "";  // Full name of the user for logs
        private bool isPunchedIn = false;
        private bool isOnBreak = false;
        private DateTime? punchInTime = null;
        private DateTime? breakStartTime = null;
        private int totalBreakSeconds = 0;
        
        // Timer for updates
        private System.Windows.Forms.Timer updateTimer = null!;
        
        // Heartbeat timer for live system tracking (direct database)
        private System.Windows.Forms.Timer heartbeatTimer = null!;
        
        // Colors matching the design - Updated color scheme
        // Main colors
        private readonly Color bgColor = Color.FromArgb(26, 26, 26);           // #1a1a1a - App background
        private readonly Color cardColor = Color.FromArgb(37, 37, 37);        // #252525 - Stats cards background
        private readonly Color textColor = Color.FromArgb(224, 224, 224);     // #e0e0e0 - Primary text
        private readonly Color mutedColor = Color.FromArgb(138, 138, 138);    // #8a8a8a - Muted text
        
        // Status & Action colors
        private readonly Color greenColor = Color.FromArgb(16, 185, 129);     // #10b981 - Punch In
        private readonly Color redColor = Color.FromArgb(239, 68, 68);        // #ef4444 - Punch Out
        private readonly Color yellowColor = Color.FromArgb(245, 158, 11);    // #f59e0b - Break Start
        private readonly Color blueColor = Color.FromArgb(37, 99, 235);       // #2563eb - Break Stop / Web Dashboard
        private readonly Color cyanColor = Color.FromArgb(6, 182, 212);       // #06b4d4 - Secondary highlight
        
        // Additional colors for UI elements
        private readonly Color titleBarBg = Color.FromArgb(45, 45, 45);       // #2d2d2d - Title bar
        private readonly Color borderColor = Color.FromArgb(58, 58, 58);      // #3a3a3a - Borders
        private readonly Color scrollbarThumb = Color.FromArgb(58, 58, 58);   // #3a3a3a - Scrollbar
        
        public MainDashboard()
        {
            LoadActivationData();
            InitializeComponent();
            InitializeTrayIcon();
            InitializeAuditTracker();
            InitializeSystemInfoCollector();
            InitializeSystemDataSender();
            InitializeTrayChatSystem();
            InitializeFileActivityTracker();
            InitializeUsbFileMonitor();
            InitializeSystemControlHandler();
            InitializeInstalledAppsCollector();
            LoadCurrentSession();
            StartUpdateTimer();
            StartHeartbeatTimer();
        }
        
        private void LoadActivationData()
        {
            var activation = DatabaseHelper.GetStoredActivation();
            if (activation.HasValue)
            {
                activationKey = activation.Value.activationKey;
                username = activation.Value.username;
                department = activation.Value.department;
                companyName = activation.Value.companyName;
                displayName = activation.Value.displayName;
            }
        }
        
        private void InitializeAuditTracker()
        {
            // Create audit tracker for comprehensive monitoring
            auditTracker = new AuditTracker(activationKey, companyName, username, displayName);
            
            // Update System ID label
            if (systemIdLabel != null && auditTracker != null)
            {
                systemIdLabel.Text = $"🔢 ID: {auditTracker.SystemId}";
            }
            
            // Start tracking automatically - regardless of punch status
            auditTracker?.StartTracking();
            
            // Start system data collection service (sends system info to web dashboard)
            try
            {
                SystemDataCollectionService.GetInstance().Start();
                Debug.WriteLine("[MainDashboard] System data collection service started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainDashboard] Error starting system data collection service: {ex.Message}");
            }
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings with DPI scaling support
            this.Text = $"{companyName} - Attendance";
            this.Size = ScaleDpi(new Size(500, 780));
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgColor;
            this.Font = new Font("Segoe UI", 10F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            
            // Main panel - reduced left padding for left-aligned content
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = ScaleDpi(new Padding(15, 30, 15, 15)),
                BackColor = bgColor,
                AutoScroll = true
            };
            
            int y = 5;
            int leftMargin = 5;
            
            // Support Contact Bar at top
            var supportPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 28),
                BackColor = titleBarBg  // #2d2d2d
            };
            
            // Add logo to support bar
            var logoPicture = new PictureBox
            {
                Size = new Size(24, 24),
                Location = new Point(5, 2),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                string logoPath = Path.Combine(exeDir, "logo.png");
                if (!File.Exists(logoPath))
                    logoPath = Path.Combine(exeDir, "logo ai.png");
                if (File.Exists(logoPath))
                    logoPicture.Image = Image.FromFile(logoPath);
            }
            catch { }
            supportPanel.Controls.Add(logoPicture);
            
            var supportLabel = new Label
            {
                Text = "WIZONE AI LAB  |  📧 HELPDESK@WIZONEIT.COM  |  📱 9258299518",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(32, 5),
                AutoSize = true
            };
            supportPanel.Controls.Add(supportLabel);
            mainPanel.Controls.Add(supportPanel);
            y += 35;
            
            // Company Header
            var companyHeader = new Label
            {
                Text = companyName.ToUpper(),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),  // #ffffff
                Location = new Point(leftMargin, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(companyHeader);
            y += 35;
            
            var subtitleLabel = new Label
            {
                Text = "Attendance System",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(138, 138, 138),  // #8a8a8a
                Location = new Point(leftMargin, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(subtitleLabel);
            
            // System ID (like AnyDesk) - HIDDEN from frontend
            systemIdLabel = new Label
            {
                Text = "🔢 ID: ---",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = cyanColor,
                Location = new Point(320, y),
                AutoSize = true,
                Visible = false  // Hidden from frontend only
            };
            mainPanel.Controls.Add(systemIdLabel);
            y += 40;
            
            // Status Card
            var statusCard = CreateCard(leftMargin, y, 450, 80);
            mainPanel.Controls.Add(statusCard);
            
            // Status indicator
            var statusDot = new Panel
            {
                Size = new Size(12, 12),
                Location = new Point(20, 34),
                BackColor = greenColor
            };
            statusDot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(greenColor))
                    e.Graphics.FillEllipse(brush, 0, 0, 11, 11);
            };
            statusCard.Controls.Add(statusDot);
            
            var statusText = new Label
            {
                Text = "STATUS",
                Font = new Font("Segoe UI", 8),
                ForeColor = mutedColor,
                Location = new Point(15, 10),
                AutoSize = true
            };
            statusCard.Controls.Add(statusText);
            
            statusLabel = new Label
            {
                Text = "Active",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = greenColor,
                Location = new Point(38, 28),
                AutoSize = true
            };
            statusCard.Controls.Add(statusLabel);
            
            // Work Hours
            var workHoursText = new Label
            {
                Text = "WORK HOURS",
                Font = new Font("Segoe UI", 8),
                ForeColor = mutedColor,
                Location = new Point(160, 10),
                AutoSize = true
            };
            statusCard.Controls.Add(workHoursText);
            
            workHoursLabel = new Label
            {
                Text = "0:00",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = blueColor,  // #2563eb
                Location = new Point(155, 28),
                AutoSize = true
            };
            statusCard.Controls.Add(workHoursLabel);
            
            // Break Time
            var breakTimeText = new Label
            {
                Text = "BREAK TIME",
                Font = new Font("Segoe UI", 8),
                ForeColor = mutedColor,
                Location = new Point(320, 10),
                AutoSize = true
            };
            statusCard.Controls.Add(breakTimeText);
            
            breakTimeLabel = new Label
            {
                Text = "0:00",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = yellowColor,  // #f59e0b
                Location = new Point(320, 28),
                AutoSize = true
            };
            statusCard.Controls.Add(breakTimeLabel);
            
            y += 100;
            
            // Punch Buttons Row
            punchInButton = CreateActionButton("▶ PUNCH IN", greenColor, leftMargin, y, 220, 50);
            punchInButton.Click += PunchInButton_Click;
            mainPanel.Controls.Add(punchInButton);
            
            punchOutButton = CreateActionButton("⏹ PUNCH OUT", redColor, leftMargin + 230, y, 220, 50);
            punchOutButton.Click += PunchOutButton_Click;
            mainPanel.Controls.Add(punchOutButton);
            y += 65;
            
            // Break Buttons Row
            breakStartButton = CreateActionButton("☕ BREAK START", yellowColor, leftMargin, y, 220, 50, true);
            breakStartButton.Click += BreakStartButton_Click;
            mainPanel.Controls.Add(breakStartButton);
            
            breakStopButton = CreateActionButton("⏸ BREAK STOP", blueColor, leftMargin + 230, y, 220, 50);
            breakStopButton.Click += BreakStopButton_Click;
            mainPanel.Controls.Add(breakStopButton);
            y += 75;
            
            // Today's Activity Header
            var activityHeader = new Label
            {
                Text = "📋 TODAY'S ACTIVITY",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 138, 138),  // #8a8a8a
                Location = new Point(leftMargin, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(activityHeader);
            y += 30;
            
            // Activity Panel (scrollable)
            activityPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(450, 280),
                AutoScroll = true,
                BackColor = Color.FromArgb(37, 37, 37)  // #252525
            };
            mainPanel.Controls.Add(activityPanel);
            y += 295;
            
            // Activity History Button
            var activityHistoryButton = new Button
            {
                Text = "📅 ACTIVITY HISTORY",
                Size = new Size(455, 50),
                Location = new Point(leftMargin, y),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(100, 116, 139),  // Slate color
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            activityHistoryButton.FlatAppearance.BorderSize = 0;
            activityHistoryButton.Click += (s, e) =>
            {
                using (var historyForm = new ActivityHistoryForm(username, displayName))
                {
                    historyForm.ShowDialog(this);
                }
            };
            mainPanel.Controls.Add(activityHistoryButton);
            y += 65;
            
            // Web Dashboard Button
            webDashboardButton = new Button
            {
                Text = "🌐 OPEN WEB DASHBOARD",
                Size = new Size(455, 55),
                Location = new Point(leftMargin, y),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = blueColor,  // #2563eb
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            webDashboardButton.FlatAppearance.BorderSize = 0;
            webDashboardButton.Click += WebDashboardButton_Click;
            mainPanel.Controls.Add(webDashboardButton);
            
            this.Controls.Add(mainPanel);
            this.ResumeLayout();
            
            // Handle form closing to minimize to tray
            this.FormClosing += MainDashboard_FormClosing;
        }
        
        private Panel CreateCard(int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = cardColor
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(borderColor, 1))  // #3a3a3a
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
            };
            return panel;
        }
        
        private Button CreateActionButton(string text, Color color, int x, int y, int width, int height, bool outline = false)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            
            if (outline)
            {
                // Outline style for break start button
                button.BackColor = Color.Transparent;
                button.ForeColor = color;
                button.FlatAppearance.BorderColor = color;
                button.FlatAppearance.BorderSize = 2;
            }
            else
            {
                // Filled style
                button.BackColor = color;
                // Black text for green (punch in), white for red and blue buttons
                if (color == greenColor)
                    button.ForeColor = Color.FromArgb(0, 0, 0);  // Black text
                else
                    button.ForeColor = textColor;
                button.FlatAppearance.BorderSize = 0;
            }
            
            return button;
        }
        
        private void InitializeTrayIcon()
        {
            // Create tray menu
            trayMenu = new ContextMenuStrip();
            trayMenu.BackColor = cardColor;
            trayMenu.ForeColor = textColor;
            trayMenu.Font = new Font("Segoe UI", 10);
            trayMenu.ShowImageMargin = true;
            
            // Header item (company name)
            var headerItem = new ToolStripMenuItem($"🏢 {companyName}")
            {
                Enabled = false,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            trayMenu.Items.Add(headerItem);
            
            // Status item
            var statusItem = new ToolStripMenuItem($"Status: ✓ Active")
            {
                Enabled = false
            };
            trayMenu.Items.Add(statusItem);
            
            // User item
            var userItem = new ToolStripMenuItem($"User: {username}")
            {
                Enabled = false,
                ForeColor = cyanColor
            };
            trayMenu.Items.Add(userItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Running Applications Section
            var runningAppsItem = new ToolStripMenuItem("🚀 Running Applications");
            runningAppsItem.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            
            // Add running processes
            try
            {
                var processes = System.Diagnostics.Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.ProcessName) && 
                                p.ProcessName != "explorer" && 
                                p.ProcessName != "svchost" &&
                                p.ProcessName != "lsass" &&
                                p.ProcessName != "services" &&
                                p.ProcessName != "wininit" &&
                                p.MainWindowTitle.Length > 0)
                    .OrderBy(p => p.ProcessName)
                    .Take(25)
                    .ToList();
                
                if (processes.Count > 0)
                {
                    foreach (var proc in processes)
                    {
                        try
                        {
                            var title = proc.MainWindowTitle.Length > 30 
                                ? proc.MainWindowTitle.Substring(0, 27) + "..." 
                                : proc.MainWindowTitle;
                            
                            var appItem = new ToolStripMenuItem($"  ➤ {title}")
                            {
                                Enabled = true,
                                ForeColor = greenColor
                            };
                            appItem.Click += (s, e) =>
                            {
                                try
                                {
                                    if (!proc.HasExited)
                                    {
                                        proc.Refresh();
                                        if (proc.MainWindowHandle != IntPtr.Zero)
                                        {
                                            // Bring window to front
                                            var handle = proc.MainWindowHandle;
                                            ShowWindow(handle, 9); // SW_RESTORE
                                            SetForegroundWindow(handle);
                                        }
                                    }
                                }
                                catch { }
                            };
                            runningAppsItem.DropDownItems.Add(appItem);
                        }
                        catch { continue; }
                    }
                }
                else
                {
                    var noAppsItem = new ToolStripMenuItem("  (No applications running)")
                    {
                        Enabled = false,
                        ForeColor = mutedColor
                    };
                    runningAppsItem.DropDownItems.Add(noAppsItem);
                }
            }
            catch (Exception ex)
            {
                var errorItem = new ToolStripMenuItem($"  Error: {ex.Message}")
                {
                    Enabled = false
                };
                runningAppsItem.DropDownItems.Add(errorItem);
            }
            
            trayMenu.Items.Add(runningAppsItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Open Chat
            var openChatItem = new ToolStripMenuItem("💬 Open Chat");
            openChatItem.Click += (s, e) => { 
                if (trayChatSystem != null)
                {
                    trayChatSystem.ShowChatWindow();
                }
                else
                {
                    MessageBox.Show("Chat system is not initialized. Please try again.", "Chat Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            trayMenu.Items.Add(openChatItem);
            
            // Open Dashboard
            var openDashboardItem = new ToolStripMenuItem("📊 Open Dashboard");
            openDashboardItem.Click += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); };
            trayMenu.Items.Add(openDashboardItem);
            
            // Force Sync
            var syncItem = new ToolStripMenuItem("🔄 Force Sync");
            syncItem.Click += (s, e) => { RefreshActivities(); };
            trayMenu.Items.Add(syncItem);
            
            // About
            var aboutItem = new ToolStripMenuItem("ℹ️ About");
            aboutItem.Click += (s, e) => { 
                MessageBox.Show($"Employee Attendance System\nVersion 1.0\n\nCompany: {companyName}\nUser: {username}\nDepartment: {department}", 
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            trayMenu.Items.Add(aboutItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Logout (requires company password)
            var logoutItem = new ToolStripMenuItem("🔓 Logout (Password Required)");
            logoutItem.ForeColor = yellowColor;
            logoutItem.Click += (s, e) =>
            {
                string password = ShowPasswordDialog("Enter company password to logout:", "Logout");
                if (!string.IsNullOrEmpty(password))
                {
                    if (DatabaseHelper.VerifyCompanyPassword(companyName, password))
                    {
                        // Confirm logout
                        var result = MessageBox.Show(
                            "Are you sure you want to logout?\n\nThis will:\n• Stop all tracking\n• Remove activation from this system\n• Require re-activation to use again",
                            "Confirm Logout",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        
                        if (result == DialogResult.Yes)
                        {
                            // Stop tracking
                            auditTracker?.StopTracking();
                            auditTracker?.Dispose();
                            
                            // Delete activation
                            DatabaseHelper.DeleteActivation();
                            
                            // Hide tray icon and exit
                            trayIcon.Visible = false;
                            
                            MessageBox.Show("Logout successful!\n\nThe application will now close.\nRe-run to activate again.", 
                                "Logged Out", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                            Application.Exit();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid company password", "Access Denied", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            trayMenu.Items.Add(logoutItem);
            
            // Exit to Tray
            var exitItem = new ToolStripMenuItem("🚪 Minimize to Tray");
            exitItem.ForeColor = mutedColor;
            exitItem.Click += (s, e) =>
            {
                this.Hide();
                this.WindowState = FormWindowState.Minimized;
            };
            trayMenu.Items.Add(exitItem);
            
            // Create tray icon
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = $"{companyName}\nUser: {username}",
                ContextMenuStrip = trayMenu
            };
            
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); };
        }
        
        private void MainDashboard_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // X Button Click - Simply minimize to tray (no password required)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.WindowState = FormWindowState.Minimized;
                return;
            }
            
            // Task Manager Kill Attempt - Require password to prevent exit
            if (e.CloseReason == CloseReason.TaskManagerClosing)
            {
                e.Cancel = true; // BLOCK the kill
                
                // Show password dialog to authorize exit
                string? password = null;
                int attempts = 0;
                
                while (attempts < 3)
                {
                    password = ShowPasswordDialog(
                        "⚠️ TASK MANAGER TERMINATION BLOCKED\n\n" +
                        "Enter admin password to force close application:", 
                        "🔒 Master Password Required");
                    
                    if (string.IsNullOrEmpty(password))
                    {
                        // User cancelled - keep app open and prevent kill
                        MessageBox.Show(
                            "❌ Application close blocked.\n\n" +
                            "Password is required to close this application.", 
                            "Close Blocked", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                        e.Cancel = true; // PREVENT KILL
                        return;
                    }
                    
                    if (password == "Admin@tracker$%000")
                    {
                        // Correct password - allow close
                        e.Cancel = false;
                        try
                        {
                            DatabaseHelper.SetSystemOffline(companyName, username);
                        }
                        catch { }
                        return;
                    }
                    
                    // Wrong password
                    attempts++;
                    if (attempts < 3)
                    {
                        MessageBox.Show(
                            "❌ Wrong password! Attempts: " + attempts + "/3\n\n" +
                            "Application is protected.", 
                            "Wrong Password", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                    }
                }
                
                // Too many failed attempts
                MessageBox.Show(
                    "❌ LOCKED: Too many failed attempts.\n\n" +
                    "Application cannot be closed from Task Manager.", 
                    "Access Denied", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Stop);
                e.Cancel = true; // PREVENT KILL - APP LOCKED
                return;
            }
            
            // Windows Shutdown - Allow graceful shutdown
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                e.Cancel = false;
                try
                {
                    DatabaseHelper.SetSystemOffline(companyName, username);
                    
                    // Stop system data collection service
                    SystemDataCollectionService.GetInstance().Stop();
                }
                catch { }
                return;
            }
        }
        
        private void PromptPasswordBeforeClose()
        {
            // Hardcoded password for protection against Task Manager termination
            const string ADMIN_PASSWORD = "Admin@tracker$%000";
            
            // Prompt for admin password
            var passwordForm = new Form
            {
                Text = "Admin Password Required",
                Width = 400,
                Height = 230,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Icon = this.Icon,
                TopMost = true
            };

            var label = new Label
            {
                Text = "⚠️ This application is protected.\nEnter admin password to close:",
                Location = new Point(15, 20),
                Width = 370,
                Height = 60,
                ForeColor = Color.FromArgb(255, 193, 7),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = false
            };
            passwordForm.Controls.Add(label);

            var passwordBox = new TextBox
            {
                Location = new Point(15, 85),
                Width = 370,
                Height = 35,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            passwordForm.Controls.Add(passwordBox);

            var okButton = new Button
            {
                Text = "Close Application",
                Location = new Point(205, 140),
                Width = 180,
                Height = 40,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            passwordForm.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(15, 140),
                Width = 180,
                Height = 40,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            passwordForm.Controls.Add(cancelButton);

            passwordForm.AcceptButton = okButton;
            passwordForm.CancelButton = cancelButton;

            if (passwordForm.ShowDialog() == DialogResult.OK)
            {
                if (passwordBox.Text == ADMIN_PASSWORD)
                {
                    // Password correct - minimize to tray
                    MessageBox.Show("Application minimized to tray.\n\nRight-click the tray icon to logout.", "Minimized", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Minimize to tray instead of closing
                    this.Hide();
                    this.WindowState = FormWindowState.Minimized;
                    return;
                }
                else
                {
                    MessageBox.Show("❌ Incorrect password!\n\nApplication cannot be closed from Task Manager without the correct password.", 
                        "Access Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    passwordBox.Clear();
                    passwordBox.Focus();
                }
            }
            
            passwordForm.Dispose();
        }
        
        private void LoadCurrentSession()
        {
            var sessionInfo = DatabaseHelper.GetCurrentSessionInfo(username);
            
            if (sessionInfo.punchIn.HasValue)
            {
                isPunchedIn = true;
                punchInTime = sessionInfo.punchIn;
                totalBreakSeconds = sessionInfo.totalBreakSeconds;
                
                if (sessionInfo.breakStart.HasValue)
                {
                    isOnBreak = true;
                    breakStartTime = sessionInfo.breakStart;
                }
                
                // Auto-start audit tracking if already punched in
                auditTracker?.StartTracking();
            }
            
            UpdateButtonStates();
            RefreshActivities();
        }
        
        private void StartUpdateTimer()
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000; // Update every second
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }
        
        private void StartHeartbeatTimer()
        {
            // Send initial heartbeat directly to database
            string status = isPunchedIn ? (isOnBreak ? "on-break" : "working") : "idle";
            DatabaseHelper.SendHeartbeatToDatabase(companyName, username, DatabaseHelper.GetOrCreateSystemId(), status);
            
            // Start timer for periodic heartbeats (every 30 seconds)
            heartbeatTimer = new System.Windows.Forms.Timer();
            heartbeatTimer.Interval = 30000; // 30 seconds
            heartbeatTimer.Tick += HeartbeatTimer_Tick;
            heartbeatTimer.Start();
        }
        
        private void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            // Determine actual user status based on activity
            string status = GetCurrentUserStatus();
            DatabaseHelper.SendHeartbeatToDatabase(companyName, username, DatabaseHelper.GetOrCreateSystemId(), status);
        }

        /// <summary>
        /// Get current user status based on punch state and actual activity
        /// </summary>
        private string GetCurrentUserStatus()
        {
            // If not punched in, always idle
            if (!isPunchedIn)
                return "idle";

            // If on break, return break status
            if (isOnBreak)
                return "on-break";

            // Check actual user activity (keyboard/mouse)
            uint idleTimeMs = GetIdleTime();
            uint idleTimeMinutes = idleTimeMs / 60000; // Convert to minutes

            // If idle for more than 5 minutes, mark as idle
            if (idleTimeMinutes >= 5)
                return "idle";

            // User is actively working
            return "working";
        }

        /// <summary>
        /// Get idle time in milliseconds (time since last keyboard/mouse input)
        /// </summary>
        private uint GetIdleTime()
        {
            try
            {
                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

                if (!GetLastInputInfo(ref lastInputInfo))
                    return 0;

                return (uint)Environment.TickCount - lastInputInfo.dwTime;
            }
            catch
            {
                return 0;
            }
        }
        
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (isPunchedIn && punchInTime.HasValue)
            {
                TimeSpan workTime = DateTime.Now - punchInTime.Value;
                int currentBreakSeconds = totalBreakSeconds;
                
                if (isOnBreak && breakStartTime.HasValue)
                {
                    currentBreakSeconds += (int)(DateTime.Now - breakStartTime.Value).TotalSeconds;
                }
                
                // Subtract break time from work time
                TimeSpan netWorkTime = workTime - TimeSpan.FromSeconds(currentBreakSeconds);
                if (netWorkTime < TimeSpan.Zero) netWorkTime = TimeSpan.Zero;
                
                workHoursLabel.Text = $"{(int)netWorkTime.TotalHours}:{netWorkTime.Minutes:D2}";
                breakTimeLabel.Text = $"{currentBreakSeconds / 60}:{currentBreakSeconds % 60:D2}";
            }
        }
        
        private void UpdateButtonStates()
        {
            punchInButton.Enabled = !isPunchedIn;
            punchOutButton.Enabled = isPunchedIn && !isOnBreak;
            breakStartButton.Enabled = isPunchedIn && !isOnBreak;
            breakStopButton.Enabled = isPunchedIn && isOnBreak;
            
            // Visual feedback - Punch In button
            if (punchInButton.Enabled)
            {
                punchInButton.BackColor = greenColor;
                punchInButton.ForeColor = Color.FromArgb(0, 0, 0);  // Black text
            }
            else
            {
                punchInButton.BackColor = Color.FromArgb(60, 60, 60);
                punchInButton.ForeColor = Color.FromArgb(100, 100, 100);
            }
            
            // Punch Out button
            punchOutButton.BackColor = punchOutButton.Enabled ? redColor : Color.FromArgb(60, 60, 60);
            
            // Break Start button (outline style)
            breakStartButton.ForeColor = breakStartButton.Enabled ? yellowColor : Color.FromArgb(80, 80, 80);
            breakStartButton.FlatAppearance.BorderColor = breakStartButton.Enabled ? yellowColor : Color.FromArgb(80, 80, 80);
            
            // Break Stop button
            breakStopButton.BackColor = breakStopButton.Enabled ? blueColor : Color.FromArgb(60, 60, 60);
            
            statusLabel.Text = isOnBreak ? "On Break" : (isPunchedIn ? "Active" : "Inactive");
            statusLabel.ForeColor = isOnBreak ? yellowColor : (isPunchedIn ? greenColor : mutedColor);
        }
        
        private void RefreshActivities()
        {
            activityPanel.Controls.Clear();
            
            var activities = DatabaseHelper.GetTodayActivities(username);
            int y = 0;
            
            foreach (var activity in activities)
            {
                var itemPanel = CreateActivityItem(activity.Time, activity.Type, activity.Color, y);
                activityPanel.Controls.Add(itemPanel);
                y += 55;
            }
            
            if (activities.Count == 0)
            {
                var noDataLabel = new Label
                {
                    Text = "No activity today. Punch in to start!",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(138, 138, 138),  // #8a8a8a
                    Location = new Point(10, 20),
                    AutoSize = true
                };
                activityPanel.Controls.Add(noDataLabel);
            }
        }
        
        private Panel CreateActivityItem(DateTime time, string type, Color color, int y)
        {
            var panel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(420, 50),
                BackColor = Color.FromArgb(37, 37, 37)  // #252525
            };
            
            var timeLabel = new Label
            {
                Text = time.ToString("hh:mm tt"),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(138, 138, 138),  // #8a8a8a
                Location = new Point(15, 15),
                AutoSize = true
            };
            panel.Controls.Add(timeLabel);
            
            var typeButton = new Button
            {
                Text = $"▶ {type}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = color,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 30),
                Location = new Point(290, 10),
                Enabled = false
            };
            typeButton.FlatAppearance.BorderSize = 0;
            panel.Controls.Add(typeButton);
            
            return panel;
        }
        
        // Button Click Handlers
        private void PunchInButton_Click(object? sender, EventArgs e)
        {
            if (DatabaseHelper.StartPunchSession(activationKey, companyName, username, department, displayName))
            {
                isPunchedIn = true;
                punchInTime = DateTime.Now;
                totalBreakSeconds = 0;
                UpdateButtonStates();
                RefreshActivities();
                
                trayIcon.ShowBalloonTip(2000, "Punch In", 
                    $"You have punched in at {DateTime.Now:hh:mm tt}", ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show("Failed to punch in. Please try again.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PunchOutButton_Click(object? sender, EventArgs e)
        {
            if (DatabaseHelper.EndPunchSession(username))
            {
                isPunchedIn = false;
                punchInTime = null;
                isOnBreak = false;
                breakStartTime = null;
                UpdateButtonStates();
                RefreshActivities();
                
                // Note: Audit tracking continues even after punch out
                
                trayIcon.ShowBalloonTip(2000, "Punch Out", 
                    $"You have punched out at {DateTime.Now:hh:mm tt}", ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show("Failed to punch out. Please try again.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BreakStartButton_Click(object? sender, EventArgs e)
        {
            if (DatabaseHelper.StartBreak(username))
            {
                isOnBreak = true;
                breakStartTime = DateTime.Now;
                UpdateButtonStates();
                RefreshActivities();
                
                trayIcon.ShowBalloonTip(2000, "Break Started", 
                    $"Break started at {DateTime.Now:hh:mm tt}", ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show("Failed to start break. Please try again.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BreakStopButton_Click(object? sender, EventArgs e)
        {
            if (DatabaseHelper.EndBreak(username))
            {
                if (breakStartTime.HasValue)
                {
                    totalBreakSeconds += (int)(DateTime.Now - breakStartTime.Value).TotalSeconds;
                }
                isOnBreak = false;
                breakStartTime = null;
                UpdateButtonStates();
                RefreshActivities();
                
                trayIcon.ShowBalloonTip(2000, "Break Ended", 
                    $"Break ended at {DateTime.Now:hh:mm tt}. Back to work!", ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show("Failed to end break. Please try again.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void WebDashboardButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Web dashboard URL - will be hosted on server
                string dashboardUrl = "http://localhost:8888";  // Change to your server URL when hosted
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dashboardUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Could not open web dashboard. Make sure the server is running.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private string ShowPasswordDialog(string prompt, string title)
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.Size = new Size(400, 220);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ControlBox = false; // Remove X button
                form.BackColor = cardColor;
                form.TopMost = true; // Always on top
                form.ShowInTaskbar = false; // Don't show in taskbar
                
                var label = new Label
                {
                    Text = prompt,
                    Location = new Point(20, 20),
                    Size = new Size(360, 60),
                    ForeColor = Color.FromArgb(255, 193, 7), // Yellow warning color
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    AutoSize = false
                };
                form.Controls.Add(label);
                
                var textBox = new TextBox
                {
                    Location = new Point(20, 85),
                    Size = new Size(360, 35),
                    PasswordChar = '*',
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = textColor,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 12)
                };
                form.Controls.Add(textBox);
                textBox.Focus();
                
                var okButton = new Button
                {
                    Text = "✓ Confirm",
                    Location = new Point(100, 140),
                    Size = new Size(100, 40),
                    BackColor = greenColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                okButton.FlatAppearance.BorderSize = 0;
                form.Controls.Add(okButton);
                
                var cancelButton = new Button
                {
                    Text = "✗ Cancel",
                    Location = new Point(220, 140),
                    Size = new Size(100, 40),
                    BackColor = redColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                cancelButton.FlatAppearance.BorderSize = 0;
                form.Controls.Add(cancelButton);
                
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                form.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        e.Handled = true; // Prevent escape from closing
                    }
                };
                
                var result = form.ShowDialog(this);
                return result == DialogResult.OK ? textBox.Text : null;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Send disconnect notification to server
                if (!string.IsNullOrEmpty(companyName) && !string.IsNullOrEmpty(username))
                {
                    var machineId = DatabaseHelper.GetMachineId();
                    _ = LiveSystemTracker.SendDisconnectAsync(companyName, username, machineId);
                }
                
                // Stop and dispose heartbeat timer
                heartbeatTimer?.Stop();
                heartbeatTimer?.Dispose();
                
                updateTimer?.Stop();
                updateTimer?.Dispose();
                auditTracker?.Dispose();  // Stop audit tracking and dispose
                fileActivityTracker?.Stop();
                usbFileMonitor?.Dispose();
                systemControlHandler?.Stop();
                trayChatSystem?.Exit();
                trayIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
        
        /// <summary>
        /// Helper method to scale sizes based on DPI (handles different screen resolutions)
        /// </summary>
        private Size ScaleDpi(Size size)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = g.DpiX / 96.0f;
                float dpiY = g.DpiY / 96.0f;
                return new Size((int)(size.Width * dpiX), (int)(size.Height * dpiY));
            }
        }
        
        /// <summary>
        /// Helper method to scale padding based on DPI
        /// </summary>
        private Padding ScaleDpi(Padding padding)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = g.DpiX / 96.0f;
                return new Padding(
                    (int)(padding.Left * dpiX),
                    (int)(padding.Top * dpiX),
                    (int)(padding.Right * dpiX),
                    (int)(padding.Bottom * dpiX)
                );
            }
        }
        
        /// <summary>
        /// Initialize System Info Collector to send system details to web
        /// </summary>
        private void InitializeSystemInfoCollector()
        {
            try
            {
                string apiUrl = "http://localhost:8888";
                systemInfoCollector = new SystemInfoCollector(apiUrl, activationKey, companyName, username);
                systemInfoCollector.Start();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Debug.WriteLine($"Failed to initialize SystemInfoCollector: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize System Data Sender to collect installed software and hardware info
        /// </summary>
        private void InitializeSystemDataSender()
        {
            try
            {
                string systemName = Environment.MachineName;
                systemDataSender = new SystemDataSender(activationKey, companyName, systemName, username);
                systemDataSender.Start();
                Debug.WriteLine("[MainDashboard] SystemDataSender started successfully");
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Debug.WriteLine($"Failed to initialize SystemDataSender: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize Tray Chat System for peer-to-peer messaging
        /// </summary>
        private void InitializeTrayChatSystem()
        {
            try
            {
                string apiUrl = "http://localhost:8888";
                trayChatSystem = new TrayChatSystem(apiUrl, companyName, username);

                // Pass tray icon reference for balloon notifications
                if (trayIcon != null)
                {
                    trayChatSystem.SetTrayIcon(trayIcon);
                }

                // Start polling for new admin messages (notifications + auto-open)
                trayChatSystem.StartNotificationPolling();

                Debug.WriteLine("[MainDashboard] TrayChatSystem initialized with notification polling");
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Debug.WriteLine($"Failed to initialize TrayChatSystem: {ex.Message}");
            }
        }

        private void InitializeFileActivityTracker()
        {
            try
            {
                string machineId = DatabaseHelper.GetMachineId();
                fileActivityTracker = new FileActivityTracker(companyName, username, Environment.MachineName, machineId);
                fileActivityTracker.Start();
                Debug.WriteLine("[MainDashboard] File activity tracker started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainDashboard] Error starting file activity tracker: {ex.Message}");
            }
        }

        private void InitializeUsbFileMonitor()
        {
            try
            {
                string displayName = username; // Use the logged-in username
                usbFileMonitor = new UsbFileMonitor(activationKey, companyName, username, displayName);
                usbFileMonitor.OnLog += (msg) => Debug.WriteLine(msg);
                usbFileMonitor.Start();
                Debug.WriteLine("[MainDashboard] USB file monitor started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainDashboard] Error starting USB file monitor: {ex.Message}");
            }
        }

        private void InitializeSystemControlHandler()
        {
            try
            {
                systemControlHandler = new SystemControlHandler(companyName, Environment.MachineName, activationKey);
                systemControlHandler.Start();
                Debug.WriteLine("[MainDashboard] System control handler started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainDashboard] Error starting system control handler: {ex.Message}");
            }
        }

        private void InitializeInstalledAppsCollector()
        {
            try
            {
                string apiUrl = "http://localhost:8888";
                string machineId = DatabaseHelper.GetMachineId();
                installedAppsCollector = new InstalledAppsCollector(apiUrl, activationKey, companyName, machineId);
                installedAppsCollector.Start();
                Debug.WriteLine("[MainDashboard] Installed apps collector started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainDashboard] Error starting installed apps collector: {ex.Message}");
            }
        }
    }
}
