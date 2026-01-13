using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EmployeeAttendance
{
    public class ActivationForm : Form
    {
        private TextBox activationKeyTextBox = null!;
        private TextBox displayNameTextBox = null!;  // User's proper display name
        private TextBox usernameTextBox = null!;
        private ComboBox departmentComboBox = null!;
        private Button activateButton = null!;
        private Button validateButton = null!;  // Validate employee button
        private Label statusLabel = null!;
        private Label companyLabel = null!;
        private Label validationStatusLabel = null!;  // Shows validation result
        
        // Colors matching the design
        private readonly Color bgColor = Color.FromArgb(17, 17, 17);
        private readonly Color cardColor = Color.FromArgb(30, 30, 30);
        private readonly Color inputBgColor = Color.FromArgb(45, 45, 45);
        private readonly Color textColor = Color.FromArgb(255, 255, 255);
        private readonly Color mutedColor = Color.FromArgb(156, 163, 175);
        private readonly Color accentColor = Color.FromArgb(250, 204, 21); // Yellow
        private readonly Color successColor = Color.FromArgb(16, 185, 129); // Green
        private readonly Color borderColor = Color.FromArgb(55, 65, 81);
        
        private string verifiedCompanyName = "";
        private bool isEmployeeValidated = false;
        
        public ActivationForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.Text = "Employee Attendance - Activation";
            this.ClientSize = new Size(450, 700);  // Increased height for new field
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgColor;
            this.Font = new Font("Segoe UI", 10F);
            
            int formWidth = this.ClientSize.Width;
            int padding = 25;
            int contentWidth = formWidth - (padding * 2);
            int y = 25;
            
            // Logo placeholder (circle with icon)
            var logoPanel = new Panel
            {
                Size = new Size(100, 100),
                Location = new Point((formWidth - 100) / 2, y),
                BackColor = Color.Transparent
            };
            logoPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, 100, 100),
                    Color.FromArgb(6, 182, 212),
                    Color.FromArgb(59, 130, 246),
                    45F))
                {
                    e.Graphics.FillEllipse(brush, 10, 10, 80, 80);
                }
                
                // Draw "AI" text
                using (var font = new Font("Segoe UI", 24, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var size = e.Graphics.MeasureString("AI", font);
                    e.Graphics.DrawString("AI", font, brush, 
                        (100 - size.Width) / 2, (100 - size.Height) / 2);
                }
            };
            this.Controls.Add(logoPanel);
            y += 110;
            
            // Company name title
            companyLabel = new Label
            {
                Text = "WIZONE AI LABS",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = textColor,
                AutoSize = false,
                Size = new Size(formWidth, 32),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(companyLabel);
            y += 32;
            
            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Attendance System",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                AutoSize = false,
                Size = new Size(formWidth, 22),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(subtitleLabel);
            y += 35;
            
            // One-time activation header
            var headerLabel = new Label
            {
                Text = "ONE-TIME ACTIVATION",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true
            };
            this.Controls.Add(headerLabel);
            y += 30;
            
            // Activation Key Label
            var keyLabel = new Label
            {
                Text = "Activation Key *",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true
            };
            this.Controls.Add(keyLabel);
            y += 22;
            
            // Activation Key Input
            activationKeyTextBox = new TextBox
            {
                Size = new Size(contentWidth, 32),
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 11),
                BackColor = inputBgColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            activationKeyTextBox.CharacterCasing = CharacterCasing.Upper;
            activationKeyTextBox.TextChanged += ActivationKeyTextBox_TextChanged;
            this.Controls.Add(activationKeyTextBox);
            y += 45;
            
            // Display Name Label (proper name)
            var nameLabel = new Label
            {
                Text = "Your Full Name *",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true
            };
            this.Controls.Add(nameLabel);
            y += 22;
            
            // Display Name Input with Validate Button
            displayNameTextBox = new TextBox
            {
                Size = new Size(contentWidth - 100, 32),  // Leave space for button
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 11),
                BackColor = inputBgColor,
                ForeColor = mutedColor,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Enter your full name"
            };
            displayNameTextBox.GotFocus += (s, e) =>
            {
                if (displayNameTextBox.Text == "Enter your full name")
                {
                    displayNameTextBox.Text = "";
                    displayNameTextBox.ForeColor = textColor;
                }
            };
            displayNameTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(displayNameTextBox.Text))
                {
                    displayNameTextBox.Text = "Enter your full name";
                    displayNameTextBox.ForeColor = mutedColor;
                }
            };
            displayNameTextBox.TextChanged += (s, e) =>
            {
                // Reset validation when name changes
                isEmployeeValidated = false;
                validationStatusLabel.Text = "";
                usernameTextBox.Text = "";
                usernameTextBox.BackColor = inputBgColor;
                departmentComboBox.SelectedIndex = 0;
            };
            this.Controls.Add(displayNameTextBox);
            
            // Validate Button
            validateButton = new Button
            {
                Text = "VALIDATE",
                Size = new Size(90, 32),
                Location = new Point(padding + contentWidth - 90, y),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),  // Blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            validateButton.FlatAppearance.BorderSize = 0;
            validateButton.Click += ValidateButton_Click;
            this.Controls.Add(validateButton);
            y += 38;
            
            // Validation Status Label
            validationStatusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = successColor,
                Location = new Point(padding, y),
                Size = new Size(contentWidth, 20),
                AutoSize = false
            };
            this.Controls.Add(validationStatusLabel);
            y += 25;
            
            // Username Label
            var userLabel = new Label
            {
                Text = "Employee ID (Auto-filled)",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true
            };
            this.Controls.Add(userLabel);
            y += 22;
            
            // Username Input (Read-only, auto-filled)
            usernameTextBox = new TextBox
            {
                Size = new Size(contentWidth, 32),
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 11),
                BackColor = inputBgColor,
                ForeColor = mutedColor,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = ""
            };
            this.Controls.Add(usernameTextBox);
            y += 45;
            
            // Department Label
            var deptLabel = new Label
            {
                Text = "Department (Auto-filled)",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true
            };
            this.Controls.Add(deptLabel);
            y += 22;
            
            // Department ComboBox
            departmentComboBox = new ComboBox
            {
                Size = new Size(contentWidth, 32),
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 11),
                BackColor = inputBgColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false  // Disabled, auto-filled
            };
            departmentComboBox.Items.Add("Select Department");
            departmentComboBox.SelectedIndex = 0;
            this.Controls.Add(departmentComboBox);
            y += 50;
            
            // Status Label
            statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(239, 68, 68),
                Location = new Point(padding, y),
                Size = new Size(contentWidth, 22),
                AutoSize = false
            };
            this.Controls.Add(statusLabel);
            y += 28;
            
            // Activate Button
            activateButton = new Button
            {
                Text = "ACTIVATE",
                Size = new Size(contentWidth, 50),
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            activateButton.FlatAppearance.BorderSize = 0;
            activateButton.Click += ActivateButton_Click;
            this.Controls.Add(activateButton);
            
            this.ResumeLayout();
        }
        
        private void ActivationKeyTextBox_TextChanged(object? sender, EventArgs e)
        {
            string key = activationKeyTextBox.Text.Trim();
            if (key.Length >= 10)
            {
                var result = DatabaseHelper.VerifyActivationKey(key);
                if (result.success)
                {
                    verifiedCompanyName = result.companyName;
                    companyLabel.Text = result.companyName;
                    statusLabel.Text = "✓ Valid activation key - Enter your name and click VALIDATE";
                    statusLabel.ForeColor = Color.FromArgb(16, 185, 129);
                    
                    // Load departments for dropdown
                    departmentComboBox.Items.Clear();
                    departmentComboBox.Items.Add("Select Department");
                    foreach (var dept in DatabaseHelper.GetDepartments(key))
                    {
                        departmentComboBox.Items.Add(dept);
                    }
                    departmentComboBox.SelectedIndex = 0;
                    
                    // Reset employee validation
                    isEmployeeValidated = false;
                    validationStatusLabel.Text = "";
                    usernameTextBox.Text = "";
                }
                else
                {
                    statusLabel.Text = result.error;
                    statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    verifiedCompanyName = "";
                }
            }
            else
            {
                verifiedCompanyName = "";
            }
        }
        
        private void ValidateButton_Click(object? sender, EventArgs e)
        {
            // Check if activation key is verified first
            if (string.IsNullOrEmpty(verifiedCompanyName))
            {
                validationStatusLabel.Text = "⚠ Please enter a valid activation key first";
                validationStatusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            string employeeName = displayNameTextBox.Text.Trim();
            if (employeeName == "Enter your full name" || string.IsNullOrEmpty(employeeName))
            {
                validationStatusLabel.Text = "⚠ Please enter your name to validate";
                validationStatusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            // Validate employee from database
            validationStatusLabel.Text = "Validating...";
            validationStatusLabel.ForeColor = mutedColor;
            Application.DoEvents();
            
            var result = DatabaseHelper.ValidateEmployee(verifiedCompanyName, employeeName);
            
            if (result.found)
            {
                // Auto-fill fields
                displayNameTextBox.Text = result.fullName;  // Use exact name from DB
                displayNameTextBox.ForeColor = textColor;
                
                usernameTextBox.Text = result.employeeId;
                usernameTextBox.ForeColor = successColor;
                usernameTextBox.BackColor = Color.FromArgb(30, 60, 45);  // Greenish background
                
                // Set department
                if (!string.IsNullOrEmpty(result.department))
                {
                    int deptIndex = departmentComboBox.FindStringExact(result.department);
                    if (deptIndex >= 0)
                    {
                        departmentComboBox.SelectedIndex = deptIndex;
                    }
                    else
                    {
                        // Add department if not in list
                        departmentComboBox.Items.Add(result.department);
                        departmentComboBox.SelectedItem = result.department;
                    }
                }
                
                isEmployeeValidated = true;
                validationStatusLabel.Text = $"✓ Employee verified: {result.designation}";
                validationStatusLabel.ForeColor = successColor;
                validateButton.BackColor = successColor;
                validateButton.Text = "✓ VALID";
            }
            else
            {
                // AUTO-REGISTER NEW EMPLOYEE
                string department = departmentComboBox.SelectedItem?.ToString() ?? "General";
                if (department == "Select Department") department = "General";
                
                var registerResult = DatabaseHelper.AutoRegisterEmployee(verifiedCompanyName, employeeName, department);
                
                if (registerResult.success)
                {
                    // Auto-fill with new employee data
                    displayNameTextBox.Text = employeeName;
                    displayNameTextBox.ForeColor = textColor;
                    
                    usernameTextBox.Text = registerResult.employeeId;
                    usernameTextBox.ForeColor = successColor;
                    usernameTextBox.BackColor = Color.FromArgb(30, 60, 45);
                    
                    // Set department
                    int deptIndex = departmentComboBox.FindStringExact(department);
                    if (deptIndex >= 0)
                    {
                        departmentComboBox.SelectedIndex = deptIndex;
                    }
                    else
                    {
                        departmentComboBox.Items.Add(department);
                        departmentComboBox.SelectedItem = department;
                    }
                    
                    isEmployeeValidated = true;
                    validationStatusLabel.Text = $"✓ New employee registered: {registerResult.employeeId}";
                    validationStatusLabel.ForeColor = successColor;
                    validateButton.BackColor = successColor;
                    validateButton.Text = "✓ REGISTERED";
                }
                else
                {
                    isEmployeeValidated = false;
                    usernameTextBox.Text = "";
                    usernameTextBox.BackColor = inputBgColor;
                    validationStatusLabel.Text = "✗ Failed to register: " + registerResult.error;
                    validationStatusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    validateButton.BackColor = Color.FromArgb(59, 130, 246);
                    validateButton.Text = "VALIDATE";
                }
            }
        }
        
        private void ActivateButton_Click(object? sender, EventArgs e)
        {
            string key = activationKeyTextBox.Text.Trim();
            string displayName = displayNameTextBox.Text.Trim();
            string username = usernameTextBox.Text.Trim();
            string department = departmentComboBox.SelectedItem?.ToString() ?? "";
            
            // Validation
            if (key == "XXXX-XXXX-XXXX-XXXX" || string.IsNullOrEmpty(key))
            {
                statusLabel.Text = "Please enter activation key";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            if (displayName == "Enter your full name" || string.IsNullOrEmpty(displayName))
            {
                statusLabel.Text = "Please enter your full name";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            // Check if employee is validated
            if (!isEmployeeValidated)
            {
                statusLabel.Text = "Please validate your name first";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            if (string.IsNullOrEmpty(username))
            {
                statusLabel.Text = "Employee ID not found - please validate again";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            if (department == "Select Department" || string.IsNullOrEmpty(department))
            {
                statusLabel.Text = "Department not found - please validate again";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            // Verify key
            var result = DatabaseHelper.VerifyActivationKey(key);
            if (!result.success)
            {
                statusLabel.Text = result.error;
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            
            // Save activation with display name
            if (DatabaseHelper.SaveActivation(key, username, department, result.companyName, displayName))
            {
                statusLabel.Text = "Activation successful!";
                statusLabel.ForeColor = Color.FromArgb(16, 185, 129);
                
                // Open main dashboard
                this.Hide();
                var dashboard = new MainDashboard();
                dashboard.FormClosed += (s, args) => Application.Exit();
                dashboard.Show();
            }
            else
            {
                statusLabel.Text = "Failed to save activation";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
            }
        }
    }
}
