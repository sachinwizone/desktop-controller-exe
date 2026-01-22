using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;

namespace EmployeeAttendance
{
    // Custom rounded panel to wrap TextBox for rounded appearance
    public class RoundedTextBoxContainer : Panel
    {
        private TextBox innerTextBox;
        private int cornerRadius = 15;
        private Color borderColor = Color.FromArgb(55, 65, 81);
        
        public RoundedTextBoxContainer()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BorderStyle = BorderStyle.None;
            this.Padding = new Padding(10, 5, 10, 5);
            
            innerTextBox = new TextBox();
            innerTextBox.BorderStyle = BorderStyle.None;
            innerTextBox.Dock = DockStyle.Fill;
            this.Controls.Add(innerTextBox);
        }
        
        public TextBox TextBox => innerTextBox;
        
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; Invalidate(); }
        }
        
        public Color BorderColorCustom
        {
            get => borderColor;
            set { borderColor = value; Invalidate(); }
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (var path = GetRoundedRect(rect, cornerRadius))
            {
                using (var brush = new SolidBrush(this.BackColor))
                    e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(borderColor, 1))
                    e.Graphics.DrawPath(pen, path);
            }
        }
        
        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ActivationForm : Form
    {
        private RoundedTextBoxContainer activationKeyContainer = null!;
        private RoundedTextBoxContainer displayNameContainer = null!;
        private TextBox activationKeyTextBox = null!;
        private TextBox displayNameTextBox = null!;
        private TextBox usernameTextBox = null!;
        private ComboBox departmentComboBox = null!;
        private Button activateButton = null!;
        private Button validateButton = null!;
        private Label statusLabel = null!;
        private Label companyLabel = null!;
        private Label validationStatusLabel = null!;
        private PictureBox logoPictureBox = null!;
        
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
            
            // Logo from file
            logoPictureBox = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point((formWidth - 120) / 2, y),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            
            // Load logo from file next to EXE
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                string logoPath = Path.Combine(exeDir, "logo.png");
                if (File.Exists(logoPath))
                {
                    logoPictureBox.Image = Image.FromFile(logoPath);
                }
                else
                {
                    // Try alternate name
                    logoPath = Path.Combine(exeDir, "logo ai.png");
                    if (File.Exists(logoPath))
                    {
                        logoPictureBox.Image = Image.FromFile(logoPath);
                    }
                    else
                    {
                        throw new FileNotFoundException("Logo not found");
                    }
                }
            }
            catch
            {
                // Fallback: draw a simple logo if file not found
                var bmp = new Bitmap(120, 120);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new LinearGradientBrush(
                        new Rectangle(0, 0, 120, 120),
                        Color.FromArgb(6, 182, 212),
                        Color.FromArgb(59, 130, 246),
                        45F))
                    {
                        g.FillEllipse(brush, 10, 10, 100, 100);
                    }
                    using (var font = new Font("Segoe UI", 28, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        var size = g.MeasureString("AI", font);
                        g.DrawString("AI", font, textBrush, 
                            (120 - size.Width) / 2, (120 - size.Height) / 2);
                    }
                }
                logoPictureBox.Image = bmp;
            }
            this.Controls.Add(logoPictureBox);
            y += 130;
            
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
            
            // Activation Key Input - Rounded
            activationKeyContainer = new RoundedTextBoxContainer
            {
                Size = new Size(contentWidth, 40),
                Location = new Point(padding, y),
                BackColor = inputBgColor,
                BorderColorCustom = borderColor,
                CornerRadius = 20
            };
            activationKeyTextBox = activationKeyContainer.TextBox;
            activationKeyTextBox.Font = new Font("Segoe UI", 11);
            activationKeyTextBox.BackColor = inputBgColor;
            activationKeyTextBox.ForeColor = textColor;
            activationKeyTextBox.CharacterCasing = CharacterCasing.Upper;
            activationKeyTextBox.TextChanged += ActivationKeyTextBox_TextChanged;
            this.Controls.Add(activationKeyContainer);
            y += 50;
            
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
            
            // Display Name Input - Rounded
            displayNameContainer = new RoundedTextBoxContainer
            {
                Size = new Size(contentWidth - 100, 40),
                Location = new Point(padding, y),
                BackColor = inputBgColor,
                BorderColorCustom = borderColor,
                CornerRadius = 20
            };
            displayNameTextBox = displayNameContainer.TextBox;
            displayNameTextBox.Font = new Font("Segoe UI", 11);
            displayNameTextBox.BackColor = inputBgColor;
            displayNameTextBox.ForeColor = mutedColor;
            displayNameTextBox.Text = "Enter your full name";
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
            this.Controls.Add(displayNameContainer);
            
            // Validate Button - Rounded
            validateButton = new Button
            {
                Text = "✓ VALID",
                Size = new Size(90, 40),
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
            
            // Username Label (Hidden)
            var userLabel = new Label
            {
                Text = "Employee ID (Auto-filled)",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true,
                Visible = false
            };
            this.Controls.Add(userLabel);
            
            // Username Input (Hidden - auto-filled in background)
            usernameTextBox = new TextBox
            {
                Size = new Size(contentWidth, 32),
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 11),
                BackColor = inputBgColor,
                ForeColor = mutedColor,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = "",
                Visible = false
            };
            this.Controls.Add(usernameTextBox);
            
            // Department Label (Hidden)
            var deptLabel = new Label
            {
                Text = "Department (Auto-filled)",
                Font = new Font("Segoe UI", 10),
                ForeColor = mutedColor,
                Location = new Point(padding, y),
                AutoSize = true,
                Visible = false
            };
            this.Controls.Add(deptLabel);
            
            // Department ComboBox (Hidden - auto-filled in background)
            departmentComboBox = new ComboBox
            {
                Size = new Size(contentWidth, 32),
                Location = new Point(padding, y),
                Font = new Font("Segoe UI", 11),
                BackColor = inputBgColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Visible = false
            };
            departmentComboBox.Items.Add("Select Department");
            departmentComboBox.SelectedIndex = 0;
            this.Controls.Add(departmentComboBox);
            
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
