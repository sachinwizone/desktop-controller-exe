using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EmployeeAttendance
{
    public class MainDashboard : Form
    {
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
        
        // Colors matching the design
        private readonly Color bgColor = Color.FromArgb(17, 17, 17);
        private readonly Color cardColor = Color.FromArgb(30, 30, 30);
        private readonly Color textColor = Color.FromArgb(255, 255, 255);
        private readonly Color mutedColor = Color.FromArgb(156, 163, 175);
        private readonly Color greenColor = Color.FromArgb(16, 185, 129);
        private readonly Color redColor = Color.FromArgb(239, 68, 68);
        private readonly Color yellowColor = Color.FromArgb(251, 191, 36);
        private readonly Color purpleColor = Color.FromArgb(139, 92, 246);
        private readonly Color cyanColor = Color.FromArgb(6, 182, 212);
        
        public MainDashboard()
        {
            LoadActivationData();
            InitializeComponent();
            InitializeTrayIcon();
            InitializeAuditTracker();
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
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.Text = $"{companyName} - Attendance";
            this.Size = new Size(500, 750);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgColor;
            this.Font = new Font("Segoe UI", 10F);
            
            // Main panel
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                BackColor = bgColor
            };
            
            int y = 20;
            
            // Company Header
            var companyHeader = new Label
            {
                Text = companyName.ToUpper(),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(companyHeader);
            y += 35;
            
            var subtitleLabel = new Label
            {
                Text = "Attendance System",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(subtitleLabel);
            
            // System ID (like AnyDesk) - right aligned
            systemIdLabel = new Label
            {
                Text = "🔢 ID: ---",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = cyanColor,
                Location = new Point(300, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(systemIdLabel);
            y += 40;
            
            // Status Card
            var statusCard = CreateCard(0, y, 440, 80);
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
                ForeColor = cyanColor,
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
                ForeColor = redColor,
                Location = new Point(320, 28),
                AutoSize = true
            };
            statusCard.Controls.Add(breakTimeLabel);
            
            y += 100;
            
            // Punch Buttons Row
            punchInButton = CreateActionButton("▶ PUNCH IN", greenColor, 0, y, 210, 50);
            punchInButton.Click += PunchInButton_Click;
            mainPanel.Controls.Add(punchInButton);
            
            punchOutButton = CreateActionButton("⏹ PUNCH OUT", redColor, 230, y, 210, 50);
            punchOutButton.Click += PunchOutButton_Click;
            mainPanel.Controls.Add(punchOutButton);
            y += 65;
            
            // Break Buttons Row
            breakStartButton = CreateActionButton("☕ BREAK START", yellowColor, 0, y, 210, 50, true);
            breakStartButton.Click += BreakStartButton_Click;
            mainPanel.Controls.Add(breakStartButton);
            
            breakStopButton = CreateActionButton("⏸ BREAK STOP", purpleColor, 230, y, 210, 50);
            breakStopButton.Click += BreakStopButton_Click;
            mainPanel.Controls.Add(breakStopButton);
            y += 75;
            
            // Today's Activity Header
            var activityHeader = new Label
            {
                Text = "📋 TODAY'S ACTIVITY",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = mutedColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            mainPanel.Controls.Add(activityHeader);
            y += 30;
            
            // Activity Panel (scrollable)
            activityPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(440, 280),
                AutoScroll = true,
                BackColor = bgColor
            };
            mainPanel.Controls.Add(activityPanel);
            y += 295;
            
            // Web Dashboard Button
            webDashboardButton = new Button
            {
                Text = "🌐 OPEN WEB DASHBOARD",
                Size = new Size(440, 55),
                Location = new Point(0, y),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = purpleColor,
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
                using (var pen = new Pen(Color.FromArgb(55, 65, 81), 1))
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
                button.BackColor = Color.Transparent;
                button.ForeColor = color;
                button.FlatAppearance.BorderColor = color;
                button.FlatAppearance.BorderSize = 2;
            }
            else
            {
                button.BackColor = color;
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
            
            // Exit
            var exitItem = new ToolStripMenuItem("🚪 Exit (Admin Only)");
            exitItem.ForeColor = mutedColor;
            exitItem.Click += (s, e) =>
            {
                string password = ShowPasswordDialog("Enter admin password to exit:", "Admin Exit");
                if (password == "admin123")
                {
                    trayIcon.Visible = false;
                    Application.Exit();
                }
                else if (!string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Invalid admin password", "Access Denied", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, "Employee Attendance", 
                    "Application minimized to tray. Double-click to open.", ToolTipIcon.Info);
            }
            else
            {
                // App is truly closing - mark system as offline
                DatabaseHelper.SetSystemOffline(companyName, username);
            }
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
            // Send heartbeat directly to database
            string status = isPunchedIn ? (isOnBreak ? "on-break" : "working") : "idle";
            DatabaseHelper.SendHeartbeatToDatabase(companyName, username, DatabaseHelper.GetOrCreateSystemId(), status);
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
            
            // Visual feedback
            punchInButton.BackColor = punchInButton.Enabled ? greenColor : Color.FromArgb(60, 60, 60);
            punchOutButton.BackColor = punchOutButton.Enabled ? redColor : Color.FromArgb(60, 60, 60);
            breakStartButton.ForeColor = breakStartButton.Enabled ? yellowColor : Color.FromArgb(80, 80, 80);
            breakStartButton.FlatAppearance.BorderColor = breakStartButton.Enabled ? yellowColor : Color.FromArgb(80, 80, 80);
            breakStopButton.BackColor = breakStopButton.Enabled ? purpleColor : Color.FromArgb(60, 60, 60);
            
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
                    ForeColor = mutedColor,
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
                BackColor = cardColor
            };
            
            var timeLabel = new Label
            {
                Text = time.ToString("hh:mm tt"),
                Font = new Font("Segoe UI", 11),
                ForeColor = textColor,
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
                
                // Start audit tracking (apps, web, inactivity, screenshots, live stream)
                auditTracker?.StartTracking();
                
                trayIcon.ShowBalloonTip(2000, "Punch In", 
                    $"You have punched in at {DateTime.Now:hh:mm tt}\nMonitoring started.", ToolTipIcon.Info);
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
                
                // Stop audit tracking
                auditTracker?.StopTracking();
                
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
                form.Size = new Size(350, 180);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = cardColor;
                
                var label = new Label
                {
                    Text = prompt,
                    Location = new Point(20, 20),
                    Size = new Size(300, 25),
                    ForeColor = textColor
                };
                form.Controls.Add(label);
                
                var textBox = new TextBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(290, 30),
                    PasswordChar = '*',
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = textColor,
                    BorderStyle = BorderStyle.FixedSingle
                };
                form.Controls.Add(textBox);
                
                var okButton = new Button
                {
                    Text = "OK",
                    Location = new Point(130, 95),
                    Size = new Size(80, 30),
                    BackColor = greenColor,
                    ForeColor = textColor,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                okButton.FlatAppearance.BorderSize = 0;
                form.Controls.Add(okButton);
                
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Location = new Point(220, 95),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = textColor,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                cancelButton.FlatAppearance.BorderSize = 0;
                form.Controls.Add(cancelButton);
                
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                
                return form.ShowDialog() == DialogResult.OK ? textBox.Text : "";
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
                trayIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
