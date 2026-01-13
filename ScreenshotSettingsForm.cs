using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace DesktopController
{
    /// <summary>
    /// Screenshot storage configuration
    /// </summary>
    public class ScreenshotSettings
    {
        public string StorageType { get; set; } = "none"; // "none", "googledrive", "onedrive"
        public string CloudLink { get; set; } = "";
        public int IntervalValue { get; set; } = 10;
        public string IntervalUnit { get; set; } = "seconds"; // "seconds", "minutes", "hours"
        public bool IsEnabled { get; set; } = false;
        public bool IsConfigured { get; set; } = false;
        
        // Registry path for storing settings
        private const string RegistryPath = @"SOFTWARE\DesktopController\ScreenshotSettings";
        
        /// <summary>
        /// Get interval in milliseconds
        /// </summary>
        public int GetIntervalMilliseconds()
        {
            int multiplier = IntervalUnit switch
            {
                "seconds" => 1000,
                "minutes" => 60000,
                "hours" => 3600000,
                _ => 1000
            };
            return IntervalValue * multiplier;
        }
        
        /// <summary>
        /// Save settings to registry
        /// </summary>
        public void Save()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        key.SetValue("StorageType", StorageType);
                        key.SetValue("CloudLink", CloudLink);
                        key.SetValue("IntervalValue", IntervalValue);
                        key.SetValue("IntervalUnit", IntervalUnit);
                        key.SetValue("IsEnabled", IsEnabled ? 1 : 0);
                        key.SetValue("IsConfigured", IsConfigured ? 1 : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving screenshot settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load settings from registry
        /// </summary>
        public static ScreenshotSettings Load()
        {
            var settings = new ScreenshotSettings();
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        settings.StorageType = key.GetValue("StorageType")?.ToString() ?? "none";
                        settings.CloudLink = key.GetValue("CloudLink")?.ToString() ?? "";
                        settings.IntervalValue = Convert.ToInt32(key.GetValue("IntervalValue") ?? 10);
                        settings.IntervalUnit = key.GetValue("IntervalUnit")?.ToString() ?? "seconds";
                        settings.IsEnabled = Convert.ToInt32(key.GetValue("IsEnabled") ?? 0) == 1;
                        settings.IsConfigured = Convert.ToInt32(key.GetValue("IsConfigured") ?? 0) == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading screenshot settings: {ex.Message}");
            }
            return settings;
        }
    }
    
    public class ScreenshotSettingsForm : Form
    {
        private ComboBox storageTypeCombo = null!;
        private TextBox cloudLinkTextBox = null!;
        private Button testConnectionButton = null!;
        private Label connectionStatusLabel = null!;
        private NumericUpDown intervalValueNumeric = null!;
        private ComboBox intervalUnitCombo = null!;
        private CheckBox enableScreenshotsCheckBox = null!;
        private Button saveButton = null!;
        private Button cancelButton = null!;
        private Panel cloudSettingsPanel = null!;
        private Button authorizeGoogleButton = null!;
        private TextBox authCodeTextBox = null!;
        private Button submitAuthCodeButton = null!;
        private Label authStatusLabel = null!;
        
        private ScreenshotSettings settings;
        public bool SettingsSaved { get; private set; } = false;
        
        // Colors
        private Color primaryColor = Color.FromArgb(30, 41, 59);
        private Color bgColor = Color.FromArgb(15, 23, 42);
        private Color cardColor = Color.FromArgb(51, 65, 85);
        private Color textColor = Color.FromArgb(248, 250, 252);
        private Color secondaryTextColor = Color.FromArgb(148, 163, 184);
        private Color accentColor = Color.FromArgb(99, 102, 241);
        private Color successColor = Color.FromArgb(34, 197, 94);
        private Color dangerColor = Color.FromArgb(239, 68, 68);
        private Color borderColor = Color.FromArgb(71, 85, 105);
        
        public ScreenshotSettingsForm()
        {
            settings = ScreenshotSettings.Load();
            InitializeComponents();
            LoadCurrentSettings();
        }
        
        private void InitializeComponents()
        {
            this.Text = "Screenshot Settings";
            this.Size = new Size(550, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = bgColor;
            this.ForeColor = textColor;
            this.AutoScroll = true;
            
            int yPos = 20;
            
            // Title
            Label titleLabel = new Label
            {
                Text = "📷 Screenshot Configuration",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
            yPos += 50;
            
            // Enable Screenshots Checkbox
            enableScreenshotsCheckBox = new CheckBox
            {
                Text = "Enable Automatic Screenshots",
                Font = new Font("Segoe UI", 11),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                AutoSize = true,
                Checked = settings.IsEnabled
            };
            enableScreenshotsCheckBox.CheckedChanged += EnableScreenshots_CheckedChanged;
            this.Controls.Add(enableScreenshotsCheckBox);
            yPos += 40;
            
            // Interval Settings Group
            GroupBox intervalGroup = new GroupBox
            {
                Text = "Capture Interval",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                Size = new Size(490, 80),
                BackColor = cardColor
            };
            
            Label intervalLabel = new Label
            {
                Text = "Take screenshot every:",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(15, 30),
                AutoSize = true
            };
            intervalGroup.Controls.Add(intervalLabel);
            
            intervalValueNumeric = new NumericUpDown
            {
                Location = new Point(180, 27),
                Size = new Size(80, 30),
                Minimum = 1,
                Maximum = 999,
                Value = settings.IntervalValue,
                Font = new Font("Segoe UI", 10),
                BackColor = primaryColor,
                ForeColor = textColor
            };
            intervalGroup.Controls.Add(intervalValueNumeric);
            
            intervalUnitCombo = new ComboBox
            {
                Location = new Point(270, 27),
                Size = new Size(120, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = primaryColor,
                ForeColor = textColor
            };
            intervalUnitCombo.Items.AddRange(new object[] { "seconds", "minutes", "hours" });
            intervalUnitCombo.SelectedItem = settings.IntervalUnit;
            intervalGroup.Controls.Add(intervalUnitCombo);
            
            this.Controls.Add(intervalGroup);
            yPos += 100;
            
            // Storage Type Group
            GroupBox storageGroup = new GroupBox
            {
                Text = "Storage Location",
                Font = new Font("Segoe UI", 10),
                ForeColor = textColor,
                Location = new Point(20, yPos),
                Size = new Size(490, 80),
                BackColor = cardColor
            };
            
            Label storageLabel = new Label
            {
                Text = "Save screenshots to:",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(15, 35),
                AutoSize = true
            };
            storageGroup.Controls.Add(storageLabel);
            
            storageTypeCombo = new ComboBox
            {
                Location = new Point(180, 32),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = primaryColor,
                ForeColor = textColor
            };
            storageTypeCombo.Items.AddRange(new object[] { 
                "None (Disabled)", 
                "Google Drive", 
                "OneDrive"
            });
            storageTypeCombo.SelectedIndexChanged += StorageType_Changed;
            storageGroup.Controls.Add(storageTypeCombo);
            
            this.Controls.Add(storageGroup);
            yPos += 100;
            
            // Cloud Settings Panel
            cloudSettingsPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(490, 250),
                BackColor = cardColor,
                Visible = false
            };
            
            Label cloudLinkLabel = new Label
            {
                Text = "Cloud Storage Shared Folder Link:",
                Font = new Font("Segoe UI", 10),
                ForeColor = secondaryTextColor,
                Location = new Point(15, 15),
                AutoSize = true
            };
            cloudSettingsPanel.Controls.Add(cloudLinkLabel);
            
            cloudLinkTextBox = new TextBox
            {
                Location = new Point(15, 40),
                Size = new Size(455, 30),
                Font = new Font("Segoe UI", 10),
                BackColor = primaryColor,
                ForeColor = textColor,
                PlaceholderText = "Paste your shared folder link here..."
            };
            cloudSettingsPanel.Controls.Add(cloudLinkTextBox);
            
            testConnectionButton = new Button
            {
                Text = "🔗 Test Connection",
                Location = new Point(15, 80),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            testConnectionButton.FlatAppearance.BorderSize = 0;
            testConnectionButton.Click += TestConnection_Click;
            cloudSettingsPanel.Controls.Add(testConnectionButton);
            
            connectionStatusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                Location = new Point(175, 85),
                Size = new Size(300, 25),
                ForeColor = secondaryTextColor
            };
            cloudSettingsPanel.Controls.Add(connectionStatusLabel);
            
            // Google Drive Authorization Section
            Label authLabel = new Label
            {
                Text = "📌 Google Drive Authorization (Required for direct upload):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(15, 125),
                AutoSize = true
            };
            cloudSettingsPanel.Controls.Add(authLabel);
            
            authorizeGoogleButton = new Button
            {
                Text = CloudStorageHelper.IsGoogleDriveAuthorized() ? "✓ Authorized" : "🔐 Authorize Google Drive",
                Location = new Point(15, 150),
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = CloudStorageHelper.IsGoogleDriveAuthorized() ? successColor : Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            authorizeGoogleButton.FlatAppearance.BorderSize = 0;
            authorizeGoogleButton.Click += AuthorizeGoogle_Click;
            cloudSettingsPanel.Controls.Add(authorizeGoogleButton);
            
            authCodeTextBox = new TextBox
            {
                Location = new Point(205, 150),
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = primaryColor,
                ForeColor = textColor,
                PlaceholderText = "Paste auth code here..."
            };
            cloudSettingsPanel.Controls.Add(authCodeTextBox);
            
            submitAuthCodeButton = new Button
            {
                Text = "Submit",
                Location = new Point(395, 150),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            submitAuthCodeButton.FlatAppearance.BorderSize = 0;
            submitAuthCodeButton.Click += SubmitAuthCode_Click;
            cloudSettingsPanel.Controls.Add(submitAuthCodeButton);
            
            authStatusLabel = new Label
            {
                Text = CloudStorageHelper.IsGoogleDriveAuthorized() ? "✓ Google Drive is authorized" : "Not authorized - click Authorize button",
                Font = new Font("Segoe UI", 9),
                Location = new Point(15, 195),
                Size = new Size(460, 40),
                ForeColor = CloudStorageHelper.IsGoogleDriveAuthorized() ? successColor : secondaryTextColor
            };
            cloudSettingsPanel.Controls.Add(authStatusLabel);
            
            this.Controls.Add(cloudSettingsPanel);
            
            // Buttons Panel - Fixed at bottom
            Panel buttonPanel = new Panel
            {
                Location = new Point(20, 600),
                Size = new Size(490, 60),
                BackColor = bgColor
            };
            
            saveButton = new Button
            {
                Text = "💾 Save Settings",
                Location = new Point(200, 10),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = successColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;
            buttonPanel.Controls.Add(saveButton);
            
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(360, 10),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = cardColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderColor = borderColor;
            cancelButton.Click += (s, e) => this.Close();
            buttonPanel.Controls.Add(cancelButton);
            
            this.Controls.Add(buttonPanel);
            
            // Set initial storage type
            storageTypeCombo.SelectedIndex = settings.StorageType switch
            {
                "googledrive" => 1,
                "onedrive" => 2,
                _ => 0
            };
        }
        
        private void LoadCurrentSettings()
        {
            enableScreenshotsCheckBox.Checked = settings.IsEnabled;
            intervalValueNumeric.Value = settings.IntervalValue;
            intervalUnitCombo.SelectedItem = settings.IntervalUnit;
            cloudLinkTextBox.Text = settings.CloudLink;
            
            UpdateUIState();
        }
        
        private void EnableScreenshots_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateUIState();
        }
        
        private void StorageType_Changed(object? sender, EventArgs e)
        {
            UpdateUIState();
        }
        
        private async void AuthorizeGoogle_Click(object? sender, EventArgs e)
        {
            if (CloudStorageHelper.IsGoogleDriveAuthorized())
            {
                var result = MessageBox.Show(
                    "Google Drive is already authorized. Do you want to re-authorize?",
                    "Already Authorized",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                    
                if (result != DialogResult.Yes) return;
            }
            
            authStatusLabel.Text = "Opening browser for authorization...";
            authStatusLabel.ForeColor = secondaryTextColor;
            authorizeGoogleButton.Enabled = false;
            
            try
            {
                await CloudStorageHelper.AuthorizeGoogleDriveAsync(status => 
                {
                    this.Invoke(() => authStatusLabel.Text = status);
                });
                
                authStatusLabel.Text = "Browser opened! Sign in with Google, then paste the authorization code above and click Submit.";
                authStatusLabel.ForeColor = Color.FromArgb(251, 191, 36);
            }
            catch (Exception ex)
            {
                authStatusLabel.Text = $"Error: {ex.Message}";
                authStatusLabel.ForeColor = dangerColor;
            }
            finally
            {
                authorizeGoogleButton.Enabled = true;
            }
        }
        
        private async void SubmitAuthCode_Click(object? sender, EventArgs e)
        {
            string authCode = authCodeTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(authCode))
            {
                authStatusLabel.Text = "Please enter the authorization code from the browser.";
                authStatusLabel.ForeColor = dangerColor;
                return;
            }
            
            authStatusLabel.Text = "Verifying authorization code...";
            authStatusLabel.ForeColor = secondaryTextColor;
            submitAuthCodeButton.Enabled = false;
            
            try
            {
                bool success = await CloudStorageHelper.CompleteGoogleAuthorizationAsync(authCode, status =>
                {
                    this.Invoke(() => authStatusLabel.Text = status);
                });
                
                if (success)
                {
                    authStatusLabel.Text = "✓ Google Drive authorized! Screenshots will upload directly.";
                    authStatusLabel.ForeColor = successColor;
                    authorizeGoogleButton.Text = "✓ Authorized";
                    authorizeGoogleButton.BackColor = successColor;
                    authCodeTextBox.Clear();
                }
                else
                {
                    authStatusLabel.Text = "Authorization failed. Please try again.";
                    authStatusLabel.ForeColor = dangerColor;
                }
            }
            catch (Exception ex)
            {
                authStatusLabel.Text = $"Error: {ex.Message}";
                authStatusLabel.ForeColor = dangerColor;
            }
            finally
            {
                submitAuthCodeButton.Enabled = true;
            }
        }
        
        private void UpdateUIState()
        {
            bool isEnabled = enableScreenshotsCheckBox.Checked;
            int storageIndex = storageTypeCombo.SelectedIndex;
            
            // Show cloud settings panel when cloud storage is selected (Google Drive or OneDrive)
            cloudSettingsPanel.Visible = (storageIndex == 1 || storageIndex == 2);
            
            // Auto-enable screenshots when cloud storage is selected
            if (storageIndex == 1 || storageIndex == 2)
            {
                enableScreenshotsCheckBox.Checked = true;
            }
            
            // Form height stays fixed at 600 to ensure buttons are visible
        }
        
        private async void TestConnection_Click(object? sender, EventArgs e)
        {
            string link = cloudLinkTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(link))
            {
                connectionStatusLabel.Text = "❌ Please enter a link";
                connectionStatusLabel.ForeColor = dangerColor;
                return;
            }
            
            connectionStatusLabel.Text = "⏳ Testing connection...";
            connectionStatusLabel.ForeColor = secondaryTextColor;
            testConnectionButton.Enabled = false;
            
            try
            {
                bool isValid = await TestCloudLinkAsync(link);
                
                if (isValid)
                {
                    connectionStatusLabel.Text = "✅ Connection successful!";
                    connectionStatusLabel.ForeColor = successColor;
                }
                else
                {
                    connectionStatusLabel.Text = "❌ Invalid or inaccessible link";
                    connectionStatusLabel.ForeColor = dangerColor;
                }
            }
            catch (Exception ex)
            {
                connectionStatusLabel.Text = $"❌ Error: {ex.Message}";
                connectionStatusLabel.ForeColor = dangerColor;
            }
            finally
            {
                testConnectionButton.Enabled = true;
            }
        }
        
        private async Task<bool> TestCloudLinkAsync(string link)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // Check if it's a Google Drive or OneDrive link
                    bool isGoogleDrive = link.Contains("drive.google.com") || link.Contains("docs.google.com");
                    bool isOneDrive = link.Contains("onedrive.live.com") || link.Contains("1drv.ms") || link.Contains("sharepoint.com");
                    
                    if (!isGoogleDrive && !isOneDrive)
                    {
                        return false;
                    }
                    
                    // Try to access the link (HEAD request to verify it exists)
                    var request = new HttpRequestMessage(HttpMethod.Head, link);
                    var response = await client.SendAsync(request);
                    
                    // For shared folders, we expect a redirect or success
                    return response.IsSuccessStatusCode || 
                           response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                           response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                           response.StatusCode == System.Net.HttpStatusCode.Found;
                }
            }
            catch
            {
                return false;
            }
        }
        
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // Validate settings
            int storageIndex = storageTypeCombo.SelectedIndex;
            bool requiresCloudLink = storageIndex == 1 || storageIndex == 2;
            
            if (enableScreenshotsCheckBox.Checked && requiresCloudLink)
            {
                string link = cloudLinkTextBox.Text.Trim();
                if (string.IsNullOrEmpty(link))
                {
                    MessageBox.Show("Please enter a cloud storage link.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Validate link format
                bool isGoogleDrive = link.Contains("drive.google.com") || link.Contains("docs.google.com");
                bool isOneDrive = link.Contains("onedrive.live.com") || link.Contains("1drv.ms") || link.Contains("sharepoint.com");
                
                if (!isGoogleDrive && !isOneDrive)
                {
                    MessageBox.Show("Please enter a valid Google Drive or OneDrive shared folder link.", 
                        "Invalid Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            
            // Save settings
            settings.IsEnabled = enableScreenshotsCheckBox.Checked;
            settings.IntervalValue = (int)intervalValueNumeric.Value;
            settings.IntervalUnit = intervalUnitCombo.SelectedItem?.ToString() ?? "seconds";
            settings.StorageType = storageIndex switch
            {
                1 => "googledrive",
                2 => "onedrive",
                _ => "none"
            };
            settings.CloudLink = cloudLinkTextBox.Text.Trim();
            settings.IsConfigured = true;
            
            settings.Save();
            SettingsSaved = true;
            
            MessageBox.Show("Screenshot settings saved successfully!\n\n" +
                $"Capture Interval: Every {settings.IntervalValue} {settings.IntervalUnit}\n" +
                $"Storage: {(settings.StorageType == "none" ? "Disabled" : settings.StorageType.ToUpper())}", 
                "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        /// <summary>
        /// Get current screenshot settings
        /// </summary>
        public ScreenshotSettings GetSettings()
        {
            return settings;
        }
    }
}
