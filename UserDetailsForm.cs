using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DesktopController
{
    public class UserDetailsForm : Form
    {
        private TextBox txtSystemUserName = null!;
        private TextBox txtDepartment = null!;
        private ComboBox cmbOfficeLocation = null!;
        private TextBox txtCustomLocation = null!;
        private Panel customLocationPanel = null!;
        private Button btnSubmit = null!;
        private Button btnCancel = null!;
        private Label lblStatus = null!;
        
        // Store user details
        public string SystemUserName { get; private set; } = "";
        public string Department { get; private set; } = "";
        public string OfficeLocation { get; private set; } = "";
        
        // User info passed from login
        private UserInfo? _loggedInUser;
        private string _companyName;
        private string _activationKey;
        
        // Modern Professional Color Palette
        private readonly Color primaryColor = Color.FromArgb(56, 139, 253);
        private readonly Color secondaryColor = Color.FromArgb(31, 111, 235);
        private readonly Color backgroundColor = Color.FromArgb(13, 17, 23);
        private readonly Color cardColor = Color.FromArgb(22, 27, 34);
        private readonly Color textColor = Color.FromArgb(240, 246, 252);
        private readonly Color placeholderColor = Color.FromArgb(139, 148, 158);
        private readonly Color inputBgColor = Color.FromArgb(30, 35, 44);
        private readonly Color successColor = Color.FromArgb(46, 160, 67);
        private readonly Color errorColor = Color.FromArgb(248, 81, 73);
        private readonly Color borderColor = Color.FromArgb(48, 54, 61);
        
        public UserDetailsForm(UserInfo? user, string companyName, string activationKey)
        {
            _loggedInUser = user;
            _companyName = companyName;
            _activationKey = activationKey;
            
            InitializeComponent();
            LoadSavedDetails();
            
            // Load office locations async to prevent UI hang
            this.Load += async (s, e) => 
            {
                await Task.Run(() => LoadOfficeLocations());
            };
        }
        
        private void InitializeComponent()
        {
            this.Text = "User Details - Desktop Controller";
            this.Size = new Size(500, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = backgroundColor;
            this.Font = new Font("Segoe UI", 10f);
            
            // Load icon
            try
            {
                string exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "DesktopController.exe");
                if (System.IO.File.Exists(exePath))
                    this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                else
                    this.Icon = SystemIcons.Shield;
            }
            catch { this.Icon = SystemIcons.Shield; }
            
            // Main panel
            Panel mainPanel = new Panel
            {
                Size = new Size(440, 540),
                Location = new Point(30, 30),
                BackColor = cardColor
            };
            mainPanel.Paint += (s, e) =>
            {
                using (GraphicsPath path = CreateRoundedRectangle(0, 0, mainPanel.Width, mainPanel.Height, 15))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (SolidBrush brush = new SolidBrush(cardColor))
                        e.Graphics.FillPath(brush, path);
                    using (Pen pen = new Pen(borderColor, 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            this.Controls.Add(mainPanel);
            
            int yPos = 25;
            
            // Title with user icon
            Panel iconPanel = new Panel
            {
                Size = new Size(60, 60),
                Location = new Point((mainPanel.Width - 60) / 2, yPos),
                BackColor = Color.Transparent
            };
            iconPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, 60, 60), primaryColor, secondaryColor, 45f))
                {
                    e.Graphics.FillEllipse(gradientBrush, 0, 0, 58, 58);
                }
                using (Font iconFont = new Font("Segoe UI", 22f, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("👤", iconFont, Brushes.White, new RectangleF(0, 0, 60, 60), sf);
                }
            };
            mainPanel.Controls.Add(iconPanel);
            yPos += 75;
            
            // Title
            Label lblTitle = new Label
            {
                Text = "Complete Your Profile",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = false,
                Size = new Size(mainPanel.Width, 30),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblTitle);
            yPos += 35;
            
            // Subtitle with user name
            Label lblSubtitle = new Label
            {
                Text = $"Welcome, {_loggedInUser?.Name ?? "User"}! Please enter additional details.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = placeholderColor,
                AutoSize = false,
                Size = new Size(mainPanel.Width - 40, 20),
                Location = new Point(20, yPos),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblSubtitle);
            yPos += 35;
            
            // System User Name
            Label lblUserName = new Label
            {
                Text = "System User Name *",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(30, yPos),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblUserName);
            yPos += 25;
            
            Panel userNamePanel = CreateInputPanel(30, yPos, 380, 42);
            txtSystemUserName = new TextBox
            {
                Location = new Point(12, 10),
                Size = new Size(356, 22),
                BorderStyle = BorderStyle.None,
                BackColor = inputBgColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 11f),
                Text = Environment.UserName // Default to Windows username
            };
            userNamePanel.Controls.Add(txtSystemUserName);
            mainPanel.Controls.Add(userNamePanel);
            yPos += 55;
            
            // Department
            Label lblDepartment = new Label
            {
                Text = "Department *",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(30, yPos),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblDepartment);
            yPos += 25;
            
            Panel deptPanel = CreateInputPanel(30, yPos, 380, 42);
            txtDepartment = new TextBox
            {
                Location = new Point(12, 10),
                Size = new Size(356, 22),
                BorderStyle = BorderStyle.None,
                BackColor = inputBgColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 11f)
            };
            txtDepartment.GotFocus += (s, e) => { if (txtDepartment.Text == "Enter your department") txtDepartment.Text = ""; txtDepartment.ForeColor = textColor; };
            txtDepartment.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtDepartment.Text)) { txtDepartment.Text = "Enter your department"; txtDepartment.ForeColor = placeholderColor; } };
            deptPanel.Controls.Add(txtDepartment);
            mainPanel.Controls.Add(deptPanel);
            yPos += 55;
            
            // Office Location (Dropdown)
            Label lblLocation = new Label
            {
                Text = "Office Location *",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(30, yPos),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblLocation);
            yPos += 25;
            
            Panel locationPanel = CreateInputPanel(30, yPos, 380, 42);
            cmbOfficeLocation = new ComboBox
            {
                Location = new Point(10, 8),
                Size = new Size(360, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputBgColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 11f),
                FlatStyle = FlatStyle.Flat
            };
            cmbOfficeLocation.SelectedIndexChanged += CmbOfficeLocation_SelectedIndexChanged;
            locationPanel.Controls.Add(cmbOfficeLocation);
            mainPanel.Controls.Add(locationPanel);
            yPos += 50;
            
            // Custom Location TextBox (hidden by default)
            customLocationPanel = CreateInputPanel(30, yPos, 380, 42);
            customLocationPanel.Visible = false;
            txtCustomLocation = new TextBox
            {
                Location = new Point(12, 10),
                Size = new Size(356, 22),
                BorderStyle = BorderStyle.None,
                BackColor = inputBgColor,
                ForeColor = textColor,
                Font = new Font("Segoe UI", 11f)
            };
            txtCustomLocation.GotFocus += (s, e) => { if (txtCustomLocation.Text == "Enter custom location") txtCustomLocation.Text = ""; txtCustomLocation.ForeColor = textColor; };
            txtCustomLocation.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtCustomLocation.Text)) { txtCustomLocation.Text = "Enter custom location"; txtCustomLocation.ForeColor = placeholderColor; } };
            txtCustomLocation.Text = "Enter custom location";
            txtCustomLocation.ForeColor = placeholderColor;
            customLocationPanel.Controls.Add(txtCustomLocation);
            mainPanel.Controls.Add(customLocationPanel);
            yPos += 60;
            
            // Status label
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = errorColor,
                Location = new Point(30, yPos),
                Size = new Size(380, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblStatus);
            yPos += 25;
            
            // Submit Button
            btnSubmit = new Button
            {
                Text = "💾 SAVE",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = successColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 45),
                Location = new Point(30, yPos),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 150, 60);
            btnSubmit.Click += BtnSubmit_Click;
            mainPanel.Controls.Add(btnSubmit);
            
            // Cancel Button
            btnCancel = new Button
            {
                Text = "CANCEL",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 45),
                Location = new Point(230, yPos),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            mainPanel.Controls.Add(btnCancel);
            
            this.AcceptButton = btnSubmit;
        }
        
        private void LoadOfficeLocations()
        {
            try
            {
                // Try to load from database (this runs on background thread)
                List<string>? locations = null;
                try
                {
                    locations = DatabaseHelper.GetOfficeLocations(_activationKey);
                }
                catch { }
                
                // Update UI on main thread
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => PopulateOfficeLocations(locations)));
                }
                else
                {
                    PopulateOfficeLocations(locations);
                }
            }
            catch
            {
                // Default locations on error
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => PopulateOfficeLocations(null)));
                }
                else
                {
                    PopulateOfficeLocations(null);
                }
            }
        }
        
        private void PopulateOfficeLocations(List<string>? locations)
        {
            try
            {
                string? savedLocation = null;
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                    {
                        savedLocation = key?.GetValue("UserDetailsLocation")?.ToString();
                    }
                }
                catch { }
                
                cmbOfficeLocation.Items.Clear();
                cmbOfficeLocation.Items.Add("-- Select Office Location --");
                
                if (locations != null && locations.Count > 0)
                {
                    foreach (var loc in locations)
                    {
                        cmbOfficeLocation.Items.Add(loc);
                    }
                }
                else
                {
                    // Default locations
                    cmbOfficeLocation.Items.Add("Main Office");
                    cmbOfficeLocation.Items.Add("Branch Office");
                    cmbOfficeLocation.Items.Add("Remote / Work From Home");
                    cmbOfficeLocation.Items.Add("Field Office");
                    cmbOfficeLocation.Items.Add("Regional Office");
                }
                
                // Add Custom option at the end
                cmbOfficeLocation.Items.Add("📝 Custom (Enter your own)");
                
                // Set saved location if exists
                if (!string.IsNullOrEmpty(savedLocation))
                {
                    bool found = false;
                    for (int i = 0; i < cmbOfficeLocation.Items.Count; i++)
                    {
                        if (cmbOfficeLocation.Items[i]?.ToString() == savedLocation)
                        {
                            cmbOfficeLocation.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }
                    
                    // If not found in list, it might be a custom location
                    if (!found && savedLocation != "-- Select Office Location --")
                    {
                        // Select Custom option and fill in the custom textbox
                        cmbOfficeLocation.SelectedIndex = cmbOfficeLocation.Items.Count - 1; // Custom option
                        txtCustomLocation.Text = savedLocation;
                        txtCustomLocation.ForeColor = textColor;
                        customLocationPanel.Visible = true;
                    }
                }
                else
                {
                    cmbOfficeLocation.SelectedIndex = 0;
                }
            }
            catch { cmbOfficeLocation.SelectedIndex = 0; }
        }
        
        private void CmbOfficeLocation_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Show custom location textbox if "Custom" is selected
            string? selected = cmbOfficeLocation.SelectedItem?.ToString();
            bool isCustom = selected != null && selected.Contains("Custom");
            
            customLocationPanel.Visible = isCustom;
            
            if (isCustom)
            {
                txtCustomLocation.Focus();
            }
        }
        
        private void LoadSavedDetails()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        string? savedUserName = key.GetValue("UserDetailsName")?.ToString();
                        string? savedDept = key.GetValue("UserDetailsDepartment")?.ToString();
                        
                        if (!string.IsNullOrEmpty(savedUserName))
                            txtSystemUserName.Text = savedUserName;
                        if (!string.IsNullOrEmpty(savedDept))
                            txtDepartment.Text = savedDept;
                        
                        // Note: Location is handled in PopulateOfficeLocations() after async load
                    }
                }
            }
            catch { }
        }
        
        private void SaveDetails()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        key.SetValue("UserDetailsName", SystemUserName);
                        key.SetValue("UserDetailsDepartment", Department);
                        key.SetValue("UserDetailsLocation", OfficeLocation);
                    }
                }
            }
            catch { }
        }
        
        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtSystemUserName.Text))
            {
                ShowError("Please enter your system user name");
                txtSystemUserName.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtDepartment.Text) || txtDepartment.Text == "Enter your department")
            {
                ShowError("Please enter your department");
                txtDepartment.Focus();
                return;
            }
            
            if (cmbOfficeLocation.SelectedIndex <= 0)
            {
                ShowError("Please select your office location");
                cmbOfficeLocation.Focus();
                return;
            }
            
            // Check if custom location is selected
            string? selectedLocation = cmbOfficeLocation.SelectedItem?.ToString();
            bool isCustom = selectedLocation != null && selectedLocation.Contains("Custom");
            
            if (isCustom)
            {
                // Validate custom location
                if (string.IsNullOrWhiteSpace(txtCustomLocation.Text) || txtCustomLocation.Text == "Enter custom location")
                {
                    ShowError("Please enter your custom office location");
                    txtCustomLocation.Focus();
                    return;
                }
                OfficeLocation = txtCustomLocation.Text.Trim();
            }
            else
            {
                OfficeLocation = selectedLocation ?? "";
            }
            
            // Save values
            SystemUserName = txtSystemUserName.Text.Trim();
            Department = txtDepartment.Text.Trim();
            
            // Save to registry for persistence
            SaveDetails();
            
            // Save to database
            Task.Run(() => DatabaseHelper.SaveUserDetails(
                _activationKey, 
                _loggedInUser?.Name ?? "", 
                SystemUserName, 
                Department, 
                OfficeLocation));
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        private void ShowError(string message)
        {
            lblStatus.ForeColor = errorColor;
            lblStatus.Text = message;
        }
        
        private Panel CreateInputPanel(int x, int y, int width, int height)
        {
            Panel panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = inputBgColor
            };
            
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(0, 0, panel.Width - 1, panel.Height - 1, 8))
                {
                    using (SolidBrush brush = new SolidBrush(inputBgColor))
                        e.Graphics.FillPath(brush, path);
                    using (Pen pen = new Pen(Color.FromArgb(71, 85, 105), 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            
            return panel;
        }
        
        private GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
