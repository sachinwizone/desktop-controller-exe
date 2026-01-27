using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace EmployeeAttendance
{
    public partial class ChatForm : Form
    {
        private string _currentUser;
        private string _selectedRecipient;
        private string _companyName;
        private int _lastMessageCount = 0;
        private System.Windows.Forms.Timer _refreshTimer;
        private const string ConnStr = "Host=72.61.170.243;Port=9095;Database=controller_application;Username=appuser;Password=jksdj$&^&*YUG*^%&THJHIO4546GHG&j;Timeout=30;CommandTimeout=60;";

        public ChatForm(string currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            
            var activation = DatabaseHelper.GetStoredActivation();
            if (activation.HasValue)
                _companyName = activation.Value.companyName;

            System.Diagnostics.Debug.WriteLine($"[Chat] Initialized - User: {_currentUser}, Company: {_companyName}");
            
            this.Text = $"Chat - {_currentUser}";
            LoadCompanyEmployees();
            SetupMessageRefresh();
        }

        private void LoadCompanyEmployees()
        {
            try
            {
                var employees = DatabaseHelper.GetCompanyEmployees(_companyName);
                recipientCombo.Items.Clear();
                
                foreach (var (fullName, employeeId, department) in employees.Where(e => e.employeeId != _currentUser))
                {
                    recipientCombo.Items.Add($"{fullName} ({employeeId})");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees: {ex.Message}");
            }
        }

        private void RecipientCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (recipientCombo.SelectedItem != null)
            {
                string fullEntry = recipientCombo.SelectedItem.ToString();
                _selectedRecipient = fullEntry.Substring(fullEntry.LastIndexOf("(") + 1).TrimEnd(')');
                _lastMessageCount = 0;  // Reset to force reload
                LoadMessages();
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedRecipient))
            {
                MessageBox.Show("Please select a recipient");
                return;
            }

            string message = messageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            System.Diagnostics.Debug.WriteLine($"[Chat] Sending message from {_currentUser} to {_selectedRecipient}: {message}");
            
            try
            {
                bool saved = SaveMessageToDb(message);
                System.Diagnostics.Debug.WriteLine($"[Chat] Message saved: {saved}");
                messageTextBox.Clear();
                System.Threading.Thread.Sleep(500);  // Wait for DB to commit
                LoadMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Chat Error] {ex}");
            }
        }

        private bool SaveMessageToDb(string message)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine($"[Chat] DB Connected for save");

                    // Ensure table exists
                    string createTableSql = @"CREATE TABLE IF NOT EXISTS chat_messages (
                        id SERIAL PRIMARY KEY,
                        sender VARCHAR(255) NOT NULL,
                        recipient VARCHAR(255) NOT NULL,
                        message TEXT NOT NULL,
                        company_name VARCHAR(255),
                        created_at TIMESTAMP DEFAULT NOW()
                    );";
                    
                    using (var cmd = new NpgsqlCommand(createTableSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert the message
                    string insertSql = @"INSERT INTO chat_messages (sender, recipient, message, company_name) 
                        VALUES (@sender, @recipient, @message, @company)";
                    
                    using (var cmd = new NpgsqlCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@sender", _currentUser ?? "");
                        cmd.Parameters.AddWithValue("@recipient", _selectedRecipient ?? "");
                        cmd.Parameters.AddWithValue("@message", message ?? "");
                        cmd.Parameters.AddWithValue("@company", (object)_companyName ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    System.Diagnostics.Debug.WriteLine($"[Chat] Message inserted: sender={_currentUser}, recipient={_selectedRecipient}, company={_companyName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat DB Error] SaveMessageToDb: {ex.Message}\n{ex}");
                return false;
            }
        }

        private void LoadMessages()
        {
            if (string.IsNullOrEmpty(_selectedRecipient))
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] LoadMessages skipped - no recipient selected");
                return;
            }
                
            try
            {
                var messages = new List<(string sender, string message, DateTime timestamp)>();
                
                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine($"[Chat] DB Connected for load");

                    // First try with company filter, then without if empty
                    string selectSql;
                    if (!string.IsNullOrEmpty(_companyName))
                    {
                        selectSql = @"SELECT sender, message, created_at FROM chat_messages 
                            WHERE company_name = @company
                            AND ((sender = @user1 AND recipient = @user2) OR (sender = @user2 AND recipient = @user1))
                            ORDER BY created_at ASC";
                    }
                    else
                    {
                        // Fallback without company filter if company is null
                        selectSql = @"SELECT sender, message, created_at FROM chat_messages 
                            WHERE (sender = @user1 AND recipient = @user2) OR (sender = @user2 AND recipient = @user1)
                            ORDER BY created_at ASC";
                    }
                    
                    using (var cmd = new NpgsqlCommand(selectSql, connection))
                    {
                        if (!string.IsNullOrEmpty(_companyName))
                            cmd.Parameters.AddWithValue("@company", _companyName);
                        cmd.Parameters.AddWithValue("@user1", _currentUser ?? "");
                        cmd.Parameters.AddWithValue("@user2", _selectedRecipient ?? "");
                        
                        System.Diagnostics.Debug.WriteLine($"[Chat] Query: company={_companyName}, user1={_currentUser}, user2={_selectedRecipient}");
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string sender = reader[0].ToString();
                                string msg = reader[1].ToString();
                                DateTime timestamp = (DateTime)reader[2];
                                messages.Add((sender, msg, timestamp));
                                System.Diagnostics.Debug.WriteLine($"[Chat] DB Row: {sender} -> {msg}");
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Chat] Loaded {messages.Count} messages (was {_lastMessageCount})");

                // Only refresh UI if message count changed
                if (messages.Count != _lastMessageCount)
                {
                    _lastMessageCount = messages.Count;
                    
                    if (conversationPanel.InvokeRequired)
                    {
                        conversationPanel.Invoke(new Action(() => UpdateUI(messages)));
                    }
                    else
                    {
                        UpdateUI(messages);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat DB Error] LoadMessages: {ex.Message}\n{ex}");
            }
        }

        private void AddMessageToUI(string sender, string message, DateTime timestamp)
        {
            Panel messagePanel = new Panel
            {
                Width = conversationPanel.Width - 20,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            bool isOwnMessage = sender == _currentUser;
            messagePanel.BackColor = isOwnMessage ? Color.LightBlue : Color.LightGray;

            Label senderLabel = new Label
            {
                Text = isOwnMessage ? "You" : sender,
                Font = new Font("Arial", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(5, 5),
                Margin = new Padding(0)
            };

            Label timeLabel = new Label
            {
                Text = timestamp.ToString("HH:mm:ss"),
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(messagePanel.Width - 75, 5),
                Margin = new Padding(0)
            };

            Label messageLabel = new Label
            {
                Text = message,
                AutoSize = true,
                MaximumSize = new Size(messagePanel.Width - 10, 0),
                Location = new Point(5, 25),
                Margin = new Padding(0, 5, 0, 0)
            };

            messagePanel.Controls.Add(senderLabel);
            messagePanel.Controls.Add(timeLabel);
            messagePanel.Controls.Add(messageLabel);
            messagePanel.Height = messageLabel.Height + 40;
            conversationPanel.Controls.Add(messagePanel);
        }

        private void UpdateUI(List<(string sender, string message, DateTime timestamp)> messages)
        {
            conversationPanel.Controls.Clear();
            
            foreach (var (sender, msg, timestamp) in messages)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] Displaying: {sender}: {msg}");
                AddMessageToUI(sender, msg, timestamp);
            }
            
            conversationPanel.Refresh();
            
            // Auto-scroll to bottom
            if (conversationPanel.Controls.Count > 0)
            {
                conversationPanel.ScrollControlIntoView(conversationPanel.Controls[conversationPanel.Controls.Count - 1]);
            }
        }

        private void SetupMessageRefresh()
        {
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 500;  // Refresh every 500ms for faster real-time updates
            _refreshTimer.Tick += (s, e) => RefreshMessages();
            _refreshTimer.Start();
        }

        private void RefreshMessages()
        {
            if (string.IsNullOrEmpty(_selectedRecipient))
                return;

            try
            {
                LoadMessages();
                
                // Force UI refresh
                if (conversationPanel.InvokeRequired)
                {
                    conversationPanel.Invoke(new Action(() => 
                    {
                        conversationPanel.Refresh();
                    }));
                }
                else
                {
                    conversationPanel.Refresh();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] RefreshMessages error: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10)
            };

            // Recipient selector
            Label recipientLabel = new Label { Text = "Chat with:", AutoSize = true };
            mainPanel.Controls.Add(recipientLabel, 0, 0);

            recipientCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            recipientCombo.SelectedIndexChanged += RecipientCombo_SelectedIndexChanged;
            mainPanel.Controls.Add(recipientCombo, 0, 1);

            // Conversation panel
            conversationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(conversationPanel, 0, 2);

            // Message input
            messageTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            mainPanel.Controls.Add(messageTextBox, 0, 3);

            // Send button
            sendButton = new Button
            {
                Text = "Send",
                Height = 40,
                Dock = DockStyle.Top
            };
            sendButton.Click += SendButton_Click;
            mainPanel.Controls.Add(sendButton, 0, 4);

            this.Controls.Add(mainPanel);
        }

        private ComboBox recipientCombo;
        private Panel conversationPanel;
        private TextBox messageTextBox;
        private Button sendButton;
    }
}

