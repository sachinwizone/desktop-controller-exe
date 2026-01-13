using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DesktopController
{
    public class LoginForm : Form
    {
        // Activation step controls
        private Panel activationPanel = null!;
        private TextBox txtActivationKey = null!;
        private Button btnVerifyKey = null!;
        private Label lblActivationStatus = null!;
        
        // Login step controls
        private Panel loginPanel = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private CheckBox chkShowPassword = null!;
        private CheckBox chkRememberMe = null!;
        private Label lblLoginStatus = null!;
        private Label lblCompanyName = null!;
        
        private Panel mainPanel = null!;
        
        // Store logged in user info
        public UserInfo? LoggedInUser { get; private set; }
        
        // Store activation info
        private string _companyName = "";
        private bool _isActivated = false;
        
        // Modern Professional Color Palette (matching MainForm)
        private readonly Color primaryColor = Color.FromArgb(56, 139, 253);      // Blue accent
        private readonly Color secondaryColor = Color.FromArgb(31, 111, 235);    // Darker blue
        private readonly Color backgroundColor = Color.FromArgb(13, 17, 23);     // Deep dark background
        private readonly Color cardColor = Color.FromArgb(22, 27, 34);           // Card background
        private readonly Color textColor = Color.FromArgb(240, 246, 252);        // Light text
        private readonly Color placeholderColor = Color.FromArgb(139, 148, 158); // Muted text
        private readonly Color inputBgColor = Color.FromArgb(30, 35, 44);        // Input background
        private readonly Color successColor = Color.FromArgb(46, 160, 67);       // Green success
        private readonly Color errorColor = Color.FromArgb(248, 81, 73);         // Red danger
        private readonly Color borderColor = Color.FromArgb(48, 54, 61);         // Border color
        
        public LoginForm()
        {
            InitializeComponent();
            SetupUI();
            CheckExistingActivation();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Desktop Controller - Login";
            this.Size = new Size(480, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = backgroundColor;
            
            // Load custom icon from exe
            try
            {
                string exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "DesktopController.exe");
                if (System.IO.File.Exists(exePath))
                {
                    this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                }
                else
                {
                    // Try to get from app.ico
                    string icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "app.ico");
                    if (System.IO.File.Exists(icoPath))
                    {
                        this.Icon = new Icon(icoPath);
                    }
                    else
                    {
                        this.Icon = SystemIcons.Shield;
                    }
                }
            }
            catch
            {
                this.Icon = SystemIcons.Shield;
            }
            
            this.Font = new Font("Segoe UI", 10f);
        }
        
        private void SetupUI()
        {
            // Main container panel
            mainPanel = new Panel
            {
                Size = new Size(400, 580),
                Location = new Point(40, 40),
                BackColor = cardColor
            };
            mainPanel.Paint += (s, e) => 
            {
                using (GraphicsPath path = CreateRoundedRectangle(0, 0, mainPanel.Width, mainPanel.Height, 20))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (SolidBrush brush = new SolidBrush(cardColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            };
            this.Controls.Add(mainPanel);
            
            int yPos = 20;
            
            // Shield Icon / Logo
            Panel iconPanel = new Panel
            {
                Size = new Size(70, 70),
                Location = new Point((mainPanel.Width - 70) / 2, yPos),
                BackColor = Color.Transparent
            };
            iconPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, 70, 70), primaryColor, secondaryColor, 45f))
                {
                    e.Graphics.FillEllipse(gradientBrush, 0, 0, 68, 68);
                }
                using (Font iconFont = new Font("Segoe UI", 24f, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("🛡", iconFont, Brushes.White, new RectangleF(0, 0, 70, 70), sf);
                }
            };
            mainPanel.Controls.Add(iconPanel);
            yPos += 85;
            
            // Title
            Label lblTitle = new Label
            {
                Text = "Desktop Controller",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = false,
                Size = new Size(mainPanel.Width, 30),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(lblTitle);
            yPos += 40;
            
            // Company Name Label (shown after activation)
            lblCompanyName = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = successColor,
                AutoSize = false,
                Size = new Size(mainPanel.Width, 25),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Visible = false
            };
            mainPanel.Controls.Add(lblCompanyName);
            yPos += 35;
            
            // Create both panels
            CreateActivationPanel(yPos);
            CreateLoginPanel(yPos);
            
            // Footer
            Label lblFooter = new Label
            {
                Text = "© 2025 Desktop Controller. All rights reserved.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = placeholderColor,
                AutoSize = false,
                Size = new Size(mainPanel.Width, 20),
                Location = new Point(0, mainPanel.Height - 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(lblFooter);
        }
        
        private void CreateActivationPanel(int startY)
        {
            activationPanel = new Panel
            {
                Size = new Size(360, 280),
                Location = new Point(20, startY),
                BackColor = Color.Transparent
            };
            mainPanel.Controls.Add(activationPanel);
            
            int yPos = 0;
            
            // Subtitle
            Label lblSubtitle = new Label
            {
                Text = "Enter Activation Key",
                Font = new Font("Segoe UI", 11f),
                ForeColor = placeholderColor,
                AutoSize = false,
                Size = new Size(360, 25),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            activationPanel.Controls.Add(lblSubtitle);
            yPos += 40;
            
            // Activation Key Label
            Label lblKeyLabel = new Label
            {
                Text = "Activation Key",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = true,
                Location = new Point(15, yPos),
                BackColor = Color.Transparent
            };
            activationPanel.Controls.Add(lblKeyLabel);
            yPos += 28;
            
            // Activation Key TextBox
            Panel keyPanel = CreateInputPanel(15, yPos, 330, 48);
            activationPanel.Controls.Add(keyPanel);
            
            txtActivationKey = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                ForeColor = textColor,
                BackColor = inputBgColor,
                BorderStyle = BorderStyle.None,
                Size = new Size(290, 28),
                Location = new Point(15, 10),
                PlaceholderText = "Enter your activation key",
                CharacterCasing = CharacterCasing.Upper
            };
            keyPanel.Controls.Add(txtActivationKey);
            yPos += 65;
            
            // Verify Button
            btnVerifyKey = new Button
            {
                Text = "VERIFY KEY",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = primaryColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(330, 50),
                Location = new Point(15, yPos),
                Cursor = Cursors.Hand
            };
            btnVerifyKey.FlatAppearance.BorderSize = 0;
            btnVerifyKey.FlatAppearance.MouseOverBackColor = secondaryColor;
            btnVerifyKey.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 50, 180);
            btnVerifyKey.Click += BtnVerifyKey_Click;
            activationPanel.Controls.Add(btnVerifyKey);
            yPos += 70;
            
            // Status Label
            lblActivationStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10f),
                ForeColor = errorColor,
                AutoSize = false,
                Size = new Size(330, 40),
                Location = new Point(15, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            activationPanel.Controls.Add(lblActivationStatus);
            
            // Enter key to verify
            txtActivationKey.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    BtnVerifyKey_Click(btnVerifyKey, EventArgs.Empty);
                    e.Handled = true;
                }
            };
        }
        
        private void CreateLoginPanel(int startY)
        {
            loginPanel = new Panel
            {
                Size = new Size(360, 400),
                Location = new Point(20, startY),
                BackColor = Color.Transparent,
                Visible = false
            };
            mainPanel.Controls.Add(loginPanel);
            
            int yPos = 0;
            
            // Subtitle
            Label lblSubtitle = new Label
            {
                Text = "Sign in to continue",
                Font = new Font("Segoe UI", 11f),
                ForeColor = placeholderColor,
                AutoSize = false,
                Size = new Size(360, 25),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            loginPanel.Controls.Add(lblSubtitle);
            yPos += 40;
            
            // System User Display (shows Windows logged-in user)
            Panel systemUserPanel = new Panel
            {
                Size = new Size(330, 42),
                Location = new Point(15, yPos),
                BackColor = Color.FromArgb(40, 45, 54),
                Padding = new Padding(12, 8, 12, 8)
            };
            systemUserPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(0, 0, systemUserPanel.Width, systemUserPanel.Height, 8))
                {
                    using (Pen pen = new Pen(Color.FromArgb(72, 139, 253), 2))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            
            Label lblSystemUser = new Label
            {
                Text = $"💻 System User: {Environment.UserName}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(136, 192, 255),
                AutoSize = false,
                Size = new Size(310, 26),
                Location = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            systemUserPanel.Controls.Add(lblSystemUser);
            loginPanel.Controls.Add(systemUserPanel);
            yPos += 55;
            
            // Username Label
            Label lblUsername = new Label
            {
                Text = "Username",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = true,
                Location = new Point(15, yPos),
                BackColor = Color.Transparent
            };
            loginPanel.Controls.Add(lblUsername);
            yPos += 28;
            
            // Username TextBox
            Panel usernamePanel = CreateInputPanel(15, yPos, 330, 48);
            loginPanel.Controls.Add(usernamePanel);
            
            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                ForeColor = textColor,
                BackColor = inputBgColor,
                BorderStyle = BorderStyle.None,
                Size = new Size(290, 28),
                Location = new Point(15, 10),
                PlaceholderText = "Enter your username"
            };
            usernamePanel.Controls.Add(txtUsername);
            yPos += 65;
            
            // Password Label
            Label lblPassword = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = true,
                Location = new Point(15, yPos),
                BackColor = Color.Transparent
            };
            loginPanel.Controls.Add(lblPassword);
            yPos += 28;
            
            // Password TextBox
            Panel passwordPanel = CreateInputPanel(15, yPos, 330, 48);
            loginPanel.Controls.Add(passwordPanel);
            
            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                ForeColor = textColor,
                BackColor = inputBgColor,
                BorderStyle = BorderStyle.None,
                Size = new Size(290, 28),
                Location = new Point(15, 10),
                PlaceholderText = "Enter your password",
                UseSystemPasswordChar = true
            };
            passwordPanel.Controls.Add(txtPassword);
            yPos += 58;
            
            // Show Password Checkbox
            chkShowPassword = new CheckBox
            {
                Text = "Show password",
                Font = new Font("Segoe UI", 9f),
                ForeColor = placeholderColor,
                AutoSize = true,
                Location = new Point(15, yPos),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            };
            loginPanel.Controls.Add(chkShowPassword);
            
            // Remember Me Checkbox
            chkRememberMe = new CheckBox
            {
                Text = "Remember me",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = primaryColor,
                AutoSize = true,
                Location = new Point(200, yPos),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Checked = false
            };
            loginPanel.Controls.Add(chkRememberMe);
            yPos += 45;
            
            // Login Button
            btnLogin = new Button
            {
                Text = "SIGN IN",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = primaryColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(330, 50),
                Location = new Point(15, yPos),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = secondaryColor;
            btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 50, 180);
            btnLogin.Click += BtnLogin_Click;
            loginPanel.Controls.Add(btnLogin);
            yPos += 70;
            
            // Status Label
            lblLoginStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10f),
                ForeColor = errorColor,
                AutoSize = false,
                Size = new Size(330, 40),
                Location = new Point(15, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            loginPanel.Controls.Add(lblLoginStatus);
            yPos += 45;
            
            // Change Activation Key Link
            LinkLabel lnkChangeKey = new LinkLabel
            {
                Text = "🔑 Change Activation Key",
                Font = new Font("Segoe UI", 10f),
                LinkColor = primaryColor,
                ActiveLinkColor = successColor,
                VisitedLinkColor = primaryColor,
                AutoSize = false,
                Size = new Size(330, 25),
                Location = new Point(15, yPos),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            lnkChangeKey.LinkClicked += (s, e) => ShowActivationStep();
            loginPanel.Controls.Add(lnkChangeKey);
            
            // Enter key handlers
            txtPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    BtnLogin_Click(btnLogin, EventArgs.Empty);
                    e.Handled = true;
                }
            };
            
            txtUsername.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    txtPassword.Focus();
                    e.Handled = true;
                }
            };
        }
        
        private void CheckExistingActivation()
        {
            // Always require fresh activation key entry on launch
            // Do not auto-login with saved activation
            _isActivated = false;
            _companyName = "";
            
            // Show activation panel first (already visible by default)
            activationPanel.Visible = true;
            loginPanel.Visible = false;
            lblCompanyName.Visible = false;
            txtActivationKey.Focus();
        }
        
        private void ShowActivationStep()
        {
            // Reset activation state for new key entry
            _isActivated = false;
            _companyName = "";
            
            // Clear previous data
            txtActivationKey.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            lblActivationStatus.Text = "";
            lblLoginStatus.Text = "";
            
            // Show activation panel
            activationPanel.Visible = true;
            loginPanel.Visible = false;
            lblCompanyName.Visible = false;
            
            // Clear saved activation from registry
            DatabaseHelper.ClearActivation();
            
            txtActivationKey.Focus();
        }
        
        private void ShowLoginStep()
        {
            activationPanel.Visible = false;
            loginPanel.Visible = true;
            lblCompanyName.Text = $"✓ {_companyName}";
            lblCompanyName.Visible = true;
            
            // Load saved credentials if "Remember Me" was checked
            LoadSavedCredentials();
            
            txtUsername.Focus();
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
                using (GraphicsPath path = CreateRoundedRectangle(0, 0, panel.Width - 1, panel.Height - 1, 10))
                {
                    using (SolidBrush brush = new SolidBrush(inputBgColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(71, 85, 105), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
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
        
        private async void BtnVerifyKey_Click(object? sender, EventArgs e)
        {
            string activationKey = txtActivationKey.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(activationKey))
            {
                ShowActivationError("Please enter your activation key");
                txtActivationKey.Focus();
                return;
            }
            
            // Disable controls during verification
            SetActivationControlsEnabled(false);
            ShowActivationStatus("Verifying activation key...", placeholderColor);
            
            try
            {
                ActivationInfo? activationInfo = await System.Threading.Tasks.Task.Run(() =>
                {
                    return DatabaseHelper.ValidateActivationKey(activationKey);
                });
                
                if (activationInfo != null && activationInfo.IsValid)
                {
                    _companyName = activationInfo.CompanyName;
                    _isActivated = true;
                    
                    // Save activation to registry for this machine
                    DatabaseHelper.SaveActivation(activationKey, _companyName);
                    
                    ShowActivationStatus($"✓ Verified: {_companyName}", successColor);
                    await System.Threading.Tasks.Task.Delay(1000);
                    
                    ShowLoginStep();
                }
                else
                {
                    ShowActivationError("Invalid activation key");
                    txtActivationKey.SelectAll();
                    txtActivationKey.Focus();
                    SetActivationControlsEnabled(true);
                }
            }
            catch (Exception ex)
            {
                ShowActivationError($"Connection failed: {ex.Message}");
                SetActivationControlsEnabled(true);
            }
        }
        
        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            
            // Validation
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowLoginError("Please enter your username");
                txtUsername.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowLoginError("Please enter your password");
                txtPassword.Focus();
                return;
            }
            
            // Disable controls during login
            SetLoginControlsEnabled(false);
            ShowLoginStatus("Signing in...", placeholderColor);
            
            try
            {
                UserInfo? userInfo = await System.Threading.Tasks.Task.Run(() => 
                {
                    return DatabaseHelper.ValidateAndGetUser(username, password, _companyName);
                });
                
                if (userInfo != null)
                {
                    LoggedInUser = userInfo;
                    ShowLoginStatus("Login successful!", successColor);
                    
                    // Save credentials if "Remember Me" is checked
                    if (chkRememberMe.Checked)
                    {
                        SaveCredentials(username, password);
                    }
                    else
                    {
                        ClearSavedCredentials();
                    }
                    
                    // Always save auto-login credentials for auto-start feature
                    string activationKey = GetSavedActivationKey();
                    if (!string.IsNullOrEmpty(activationKey))
                    {
                        DatabaseHelper.SaveAutoLoginCredentials(activationKey, username, password);
                    }
                    
                    await System.Threading.Tasks.Task.Delay(500);
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowLoginError("Invalid username or password");
                    txtPassword.Clear();
                    txtPassword.Focus();
                    SetLoginControlsEnabled(true);
                }
            }
            catch (Exception ex)
            {
                ShowLoginError($"Connection failed: {ex.Message}");
                SetLoginControlsEnabled(true);
            }
        }
        
        private string GetSavedActivationKey()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        return key.GetValue("ActivationKey")?.ToString() ?? "";
                    }
                }
            }
            catch { }
            return "";
        }
        
        private void ShowActivationError(string message)
        {
            lblActivationStatus.ForeColor = errorColor;
            lblActivationStatus.Text = message;
        }
        
        private void ShowActivationStatus(string message, Color color)
        {
            lblActivationStatus.ForeColor = color;
            lblActivationStatus.Text = message;
        }
        
        private void SetActivationControlsEnabled(bool enabled)
        {
            txtActivationKey.Enabled = enabled;
            btnVerifyKey.Enabled = enabled;
            
            if (!enabled)
            {
                btnVerifyKey.Text = "VERIFYING...";
                btnVerifyKey.BackColor = Color.FromArgb(100, 116, 139);
            }
            else
            {
                btnVerifyKey.Text = "VERIFY KEY";
                btnVerifyKey.BackColor = primaryColor;
            }
        }
        
        private void ShowLoginError(string message)
        {
            lblLoginStatus.ForeColor = errorColor;
            lblLoginStatus.Text = message;
        }
        
        private void ShowLoginStatus(string message, Color color)
        {
            lblLoginStatus.ForeColor = color;
            lblLoginStatus.Text = message;
        }
        
        private void SetLoginControlsEnabled(bool enabled)
        {
            txtUsername.Enabled = enabled;
            txtPassword.Enabled = enabled;
            btnLogin.Enabled = enabled;
            chkShowPassword.Enabled = enabled;
            
            if (!enabled)
            {
                btnLogin.Text = "SIGNING IN...";
                btnLogin.BackColor = Color.FromArgb(100, 116, 139);
            }
            else
            {
                btnLogin.Text = "SIGN IN";
                btnLogin.BackColor = primaryColor;
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // If not logged in successfully, exit application
            if (this.DialogResult != DialogResult.OK)
            {
                // Allow closing
            }
            base.OnFormClosing(e);
        }
        
        // Save user credentials to registry
        private void SaveCredentials(string username, string password)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        // Simple encoding (not secure, but better than plain text)
                        string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}|{password}"));
                        key.SetValue("SavedCredentials", encoded);
                        key.SetValue("RememberMe", "1");
                    }
                }
            }
            catch { }
        }
        
        // Load saved credentials from registry
        private void LoadSavedCredentials()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        string? rememberMe = key.GetValue("RememberMe")?.ToString();
                        if (rememberMe == "1")
                        {
                            string? encoded = key.GetValue("SavedCredentials")?.ToString();
                            if (!string.IsNullOrEmpty(encoded))
                            {
                                string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                                string[] parts = decoded.Split('|');
                                if (parts.Length == 2)
                                {
                                    txtUsername.Text = parts[0];
                                    txtPassword.Text = parts[1];
                                    chkRememberMe.Checked = true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        // Clear saved credentials from registry
        private void ClearSavedCredentials()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("SavedCredentials", false);
                        key.DeleteValue("RememberMe", false);
                    }
                }
            }
            catch { }
        }
    }
}
