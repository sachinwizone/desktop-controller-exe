using System;
using System.Drawing;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text;

namespace DesktopController
{
    /// <summary>
    /// Manages company-wide password protection for the application
    /// </summary>
    public static class PasswordManager
    {
        private static string? _cachedPasswordHash = null;
        private static bool _isUnlocked = false;
        private static DateTime _unlockExpiry = DateTime.MinValue;
        
        // How long the password remains valid after entering (in minutes)
        private const int UnlockDurationMinutes = 30;
        
        /// <summary>
        /// Check if password is set for a company
        /// </summary>
        public static bool IsPasswordSet(string? companyName)
        {
            if (string.IsNullOrEmpty(companyName)) return false;
            
            try
            {
                string? hash = DatabaseHelper.GetCompanyPassword(companyName);
                return !string.IsNullOrEmpty(hash);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Set password for a company (first time setup)
        /// </summary>
        public static bool SetPassword(string? companyName, string password)
        {
            if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(password))
                return false;
            
            try
            {
                string hash = HashPassword(password);
                bool success = DatabaseHelper.SetCompanyPassword(companyName, hash);
                
                if (success)
                {
                    _cachedPasswordHash = hash;
                    _isUnlocked = true;
                    _unlockExpiry = DateTime.Now.AddMinutes(UnlockDurationMinutes);
                }
                
                return success;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Verify password for a company
        /// </summary>
        public static bool VerifyPassword(string? companyName, string password)
        {
            if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(password))
                return false;
            
            try
            {
                // Always get fresh password hash from database to ensure accuracy
                string? storedHash = DatabaseHelper.GetCompanyPassword(companyName);
                
                if (string.IsNullOrEmpty(storedHash))
                    return false;
                
                // Update cache
                _cachedPasswordHash = storedHash;
                
                string inputHash = HashPassword(password);
                bool isValid = inputHash == storedHash;
                
                if (isValid)
                {
                    _isUnlocked = true;
                    _unlockExpiry = DateTime.Now.AddMinutes(UnlockDurationMinutes);
                }
                
                return isValid;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Check if currently unlocked (password recently verified)
        /// </summary>
        public static bool IsUnlocked()
        {
            if (!_isUnlocked) return false;
            if (DateTime.Now > _unlockExpiry)
            {
                _isUnlocked = false;
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Lock the application (require password again)
        /// </summary>
        public static void Lock()
        {
            _isUnlocked = false;
            _unlockExpiry = DateTime.MinValue;
        }
        
        /// <summary>
        /// Clear cached password hash (call when switching companies)
        /// </summary>
        public static void ClearCache()
        {
            _cachedPasswordHash = null;
            _isUnlocked = false;
            _unlockExpiry = DateTime.MinValue;
        }
        
        /// <summary>
        /// Extend unlock duration
        /// </summary>
        public static void ExtendUnlock()
        {
            if (_isUnlocked)
            {
                _unlockExpiry = DateTime.Now.AddMinutes(UnlockDurationMinutes);
            }
        }
        
        /// <summary>
        /// Hash password using SHA256
        /// </summary>
        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "DesktopController_Salt_2025"));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        /// <summary>
        /// Show password prompt dialog
        /// </summary>
        public static bool PromptForPassword(string? companyName, string action = "continue")
        {
            if (string.IsNullOrEmpty(companyName)) return false;
            
            // If already unlocked, allow
            if (IsUnlocked()) return true;
            
            using (var dialog = new PasswordPromptForm(companyName, action))
            {
                return dialog.ShowDialog() == DialogResult.OK;
            }
        }
        
        /// <summary>
        /// Always show password prompt dialog (never use cache) - for critical actions
        /// </summary>
        public static bool PromptForPasswordAlways(string? companyName, string action = "continue")
        {
            if (string.IsNullOrEmpty(companyName)) return false;
            
            // Always show prompt, never use cache
            using (var dialog = new PasswordPromptForm(companyName, action))
            {
                return dialog.ShowDialog() == DialogResult.OK;
            }
        }
        
        /// <summary>
        /// Show password setup dialog (first time)
        /// </summary>
        public static bool ShowPasswordSetup(string? companyName)
        {
            if (string.IsNullOrEmpty(companyName)) return false;
            
            using (var dialog = new PasswordSetupForm(companyName))
            {
                return dialog.ShowDialog() == DialogResult.OK;
            }
        }
        
        /// <summary>
        /// Show change password dialog
        /// </summary>
        public static bool ShowChangePassword(string? companyName)
        {
            if (string.IsNullOrEmpty(companyName)) return false;
            
            using (var dialog = new PasswordChangeForm(companyName))
            {
                return dialog.ShowDialog() == DialogResult.OK;
            }
        }
    }
    
    /// <summary>
    /// Password prompt dialog
    /// </summary>
    public class PasswordPromptForm : Form
    {
        private TextBox passwordTextBox = null!;
        private Label statusLabel = null!;
        private string companyName;
        private string actionName;
        
        private Color bgColor = Color.FromArgb(15, 23, 42);
        private Color cardColor = Color.FromArgb(51, 65, 85);
        private Color textColor = Color.FromArgb(248, 250, 252);
        private Color accentColor = Color.FromArgb(99, 102, 241);
        private Color dangerColor = Color.FromArgb(239, 68, 68);
        
        public PasswordPromptForm(string company, string action)
        {
            companyName = company;
            actionName = action;
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            this.Text = "🔒 Password Required";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.TopMost = true;
            
            // Icon
            Label iconLabel = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI Emoji", 32),
                Location = new Point(170, 15),
                AutoSize = true
            };
            this.Controls.Add(iconLabel);
            
            // Message
            Label messageLabel = new Label
            {
                Text = $"Enter company password to {actionName}",
                Font = new Font("Segoe UI", 11),
                ForeColor = textColor,
                Location = new Point(50, 70),
                Size = new Size(300, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(messageLabel);
            
            // Password TextBox
            passwordTextBox = new TextBox
            {
                Location = new Point(75, 100),
                Size = new Size(250, 35),
                Font = new Font("Segoe UI", 12),
                BackColor = cardColor,
                ForeColor = textColor,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            passwordTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) VerifyButton_Click(s, e); };
            this.Controls.Add(passwordTextBox);
            
            // Status Label
            statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = dangerColor,
                Location = new Point(75, 140),
                Size = new Size(250, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(statusLabel);
            
            // Verify Button
            Button verifyButton = new Button
            {
                Text = "✓ Verify",
                Location = new Point(120, 165),
                Size = new Size(160, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            verifyButton.FlatAppearance.BorderSize = 0;
            verifyButton.Click += VerifyButton_Click;
            this.Controls.Add(verifyButton);
            
            this.ActiveControl = passwordTextBox;
        }
        
        private void VerifyButton_Click(object? sender, EventArgs e)
        {
            string password = passwordTextBox.Text;
            
            if (string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Please enter password";
                return;
            }
            
            if (PasswordManager.VerifyPassword(companyName, password))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                statusLabel.Text = "❌ Incorrect password";
                passwordTextBox.Clear();
                passwordTextBox.Focus();
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Allow close but don't set OK result
            base.OnFormClosing(e);
        }
    }
    
    /// <summary>
    /// Password setup dialog (first time)
    /// </summary>
    public class PasswordSetupForm : Form
    {
        private TextBox passwordTextBox = null!;
        private TextBox confirmTextBox = null!;
        private Label statusLabel = null!;
        private string companyName;
        
        private Color bgColor = Color.FromArgb(15, 23, 42);
        private Color cardColor = Color.FromArgb(51, 65, 85);
        private Color textColor = Color.FromArgb(248, 250, 252);
        private Color secondaryColor = Color.FromArgb(148, 163, 184);
        private Color accentColor = Color.FromArgb(99, 102, 241);
        private Color successColor = Color.FromArgb(34, 197, 94);
        private Color dangerColor = Color.FromArgb(239, 68, 68);
        
        public PasswordSetupForm(string company)
        {
            companyName = company;
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            this.Text = "🔒 Set Company Password";
            this.Size = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.TopMost = true;
            this.ControlBox = false; // Cannot close without setting password
            
            // Icon
            Label iconLabel = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI Emoji", 36),
                Location = new Point(190, 10),
                AutoSize = true
            };
            this.Controls.Add(iconLabel);
            
            // Title
            Label titleLabel = new Label
            {
                Text = "First Time Setup - Set Company Password",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(60, 65),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
            
            // Description
            Label descLabel = new Label
            {
                Text = $"Set a password for company: {companyName}\nThis password will be required to control the application.",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryColor,
                Location = new Point(50, 95),
                Size = new Size(350, 45)
            };
            this.Controls.Add(descLabel);
            
            // Password Label
            Label passLabel = new Label
            {
                Text = "Password:",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(50, 150),
                AutoSize = true
            };
            this.Controls.Add(passLabel);
            
            // Password TextBox
            passwordTextBox = new TextBox
            {
                Location = new Point(150, 147),
                Size = new Size(230, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = cardColor,
                ForeColor = textColor,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(passwordTextBox);
            
            // Confirm Label
            Label confirmLabel = new Label
            {
                Text = "Confirm:",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(50, 190),
                AutoSize = true
            };
            this.Controls.Add(confirmLabel);
            
            // Confirm TextBox
            confirmTextBox = new TextBox
            {
                Location = new Point(150, 187),
                Size = new Size(230, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = cardColor,
                ForeColor = textColor,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            confirmTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) SetButton_Click(s, e); };
            this.Controls.Add(confirmTextBox);
            
            // Status Label
            statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = dangerColor,
                Location = new Point(50, 225),
                Size = new Size(330, 20)
            };
            this.Controls.Add(statusLabel);
            
            // Set Password Button
            Button setButton = new Button
            {
                Text = "🔒 Set Password",
                Location = new Point(140, 255),
                Size = new Size(170, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = successColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            setButton.FlatAppearance.BorderSize = 0;
            setButton.Click += SetButton_Click;
            this.Controls.Add(setButton);
            
            this.ActiveControl = passwordTextBox;
        }
        
        private void SetButton_Click(object? sender, EventArgs e)
        {
            string password = passwordTextBox.Text;
            string confirm = confirmTextBox.Text;
            
            if (string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "❌ Please enter a password";
                statusLabel.ForeColor = dangerColor;
                return;
            }
            
            if (password.Length < 4)
            {
                statusLabel.Text = "❌ Password must be at least 4 characters";
                statusLabel.ForeColor = dangerColor;
                return;
            }
            
            if (password != confirm)
            {
                statusLabel.Text = "❌ Passwords do not match";
                statusLabel.ForeColor = dangerColor;
                confirmTextBox.Clear();
                confirmTextBox.Focus();
                return;
            }
            
            // Set the password
            if (PasswordManager.SetPassword(companyName, password))
            {
                statusLabel.Text = "✓ Password set successfully!";
                statusLabel.ForeColor = successColor;
                
                MessageBox.Show(
                    "Company password has been set successfully!\n\n" +
                    "This password will be required for:\n" +
                    "• Starting/Stopping sync\n" +
                    "• Exiting the application\n" +
                    "• Accessing protected features\n\n" +
                    "Keep this password safe!",
                    "Password Set",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                statusLabel.Text = "❌ Failed to save password. Please try again.";
                statusLabel.ForeColor = dangerColor;
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prevent closing without setting password
            if (this.DialogResult != DialogResult.OK)
            {
                e.Cancel = true;
                MessageBox.Show("You must set a password to continue.", "Password Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            base.OnFormClosing(e);
        }
    }
    
    /// <summary>
    /// Password change dialog
    /// </summary>
    public class PasswordChangeForm : Form
    {
        private TextBox newPasswordTextBox = null!;
        private TextBox confirmPasswordTextBox = null!;
        private Label statusLabel = null!;
        private string companyName;
        
        private Color bgColor = Color.FromArgb(15, 23, 42);
        private Color cardColor = Color.FromArgb(51, 65, 85);
        private Color textColor = Color.FromArgb(248, 250, 252);
        private Color accentColor = Color.FromArgb(99, 102, 241);
        private Color successColor = Color.FromArgb(34, 197, 94);
        private Color dangerColor = Color.FromArgb(239, 68, 68);
        
        public PasswordChangeForm(string company)
        {
            companyName = company;
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            this.Text = "🔑 Change Company Password";
            this.Size = new Size(420, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.TopMost = true;
            
            // Icon
            Label iconLabel = new Label
            {
                Text = "🔑",
                Font = new Font("Segoe UI Emoji", 32),
                Location = new Point(175, 15),
                AutoSize = true
            };
            this.Controls.Add(iconLabel);
            
            // Title
            Label titleLabel = new Label
            {
                Text = "Change Password",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(120, 65),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
            
            // Company info
            Label companyLabel = new Label
            {
                Text = $"Company: {companyName}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(50, 95),
                Size = new Size(320, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(companyLabel);
            
            // New Password Label
            Label newPassLabel = new Label
            {
                Text = "New Password:",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(50, 125),
                AutoSize = true
            };
            this.Controls.Add(newPassLabel);
            
            // New Password TextBox
            newPasswordTextBox = new TextBox
            {
                Location = new Point(50, 148),
                Size = new Size(320, 35),
                Font = new Font("Segoe UI", 12),
                BackColor = cardColor,
                ForeColor = textColor,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(newPasswordTextBox);
            
            // Confirm Password Label
            Label confirmLabel = new Label
            {
                Text = "Confirm New Password:",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(50, 185),
                AutoSize = true
            };
            this.Controls.Add(confirmLabel);
            
            // Confirm Password TextBox
            confirmPasswordTextBox = new TextBox
            {
                Location = new Point(50, 208),
                Size = new Size(320, 35),
                Font = new Font("Segoe UI", 12),
                BackColor = cardColor,
                ForeColor = textColor,
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            confirmPasswordTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ChangeButton_Click(s, e); };
            this.Controls.Add(confirmPasswordTextBox);
            
            // Status Label
            statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = dangerColor,
                Location = new Point(50, 245),
                Size = new Size(320, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(statusLabel);
            
            // Change Button
            Button changeButton = new Button
            {
                Text = "✓ Change Password",
                Location = new Point(100, 270),
                Size = new Size(220, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = successColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            changeButton.FlatAppearance.BorderSize = 0;
            changeButton.Click += ChangeButton_Click;
            this.Controls.Add(changeButton);
            
            this.ActiveControl = newPasswordTextBox;
        }
        
        private void ChangeButton_Click(object? sender, EventArgs e)
        {
            string newPassword = newPasswordTextBox.Text;
            string confirmPassword = confirmPasswordTextBox.Text;
            
            if (string.IsNullOrEmpty(newPassword))
            {
                statusLabel.Text = "Please enter new password";
                statusLabel.ForeColor = dangerColor;
                return;
            }
            
            if (newPassword.Length < 4)
            {
                statusLabel.Text = "Password must be at least 4 characters";
                statusLabel.ForeColor = dangerColor;
                return;
            }
            
            if (newPassword != confirmPassword)
            {
                statusLabel.Text = "Passwords do not match";
                statusLabel.ForeColor = dangerColor;
                confirmPasswordTextBox.Clear();
                confirmPasswordTextBox.Focus();
                return;
            }
            
            // Save new password
            if (PasswordManager.SetPassword(companyName, newPassword))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                statusLabel.Text = "❌ Failed to change password";
                statusLabel.ForeColor = dangerColor;
            }
        }
    }
}
