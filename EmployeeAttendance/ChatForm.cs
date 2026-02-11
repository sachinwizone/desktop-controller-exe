using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace EmployeeAttendance
{
    public partial class ChatForm : Form
    {
        private string _currentUser;
        private string _selectedRecipient;
        private string _selectedRecipientName;
        private string _companyName;
        private string _autoSelectSender;
        private int _lastMessageCount = 0;
        private System.Windows.Forms.Timer _refreshTimer;
        private const string ConnStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";

        // UI Controls
        private Panel contactsPanel;
        private Panel chatPanel;
        private FlowLayoutPanel contactsListPanel;
        private Panel messagesArea;
        private TextBox messageTextBox;
        private Button sendButton;
        private Label chatHeaderLabel;
        private Label chatStatusLabel;
        private TextBox searchBox;
        private List<ContactInfo> _allContacts = new List<ContactInfo>();

        public ChatForm(string currentUser, string autoSelectSender = null)
        {
            _currentUser = currentUser;
            _autoSelectSender = autoSelectSender;

            var activation = DatabaseHelper.GetStoredActivation();
            if (activation.HasValue)
                _companyName = activation.Value.companyName;

            System.Diagnostics.Debug.WriteLine($"[Chat] Initialized - User: {_currentUser}, Company: {_companyName}, AutoSelect: {_autoSelectSender}");

            InitializeComponent();
            this.Text = $"Chat - {_currentUser}";
            LoadContacts();
            SetupMessageRefresh();

            if (!string.IsNullOrEmpty(_autoSelectSender))
            {
                AutoSelectRecipient(_autoSelectSender);
            }
        }

        public void SetRecipientAndLoad(string sender)
        {
            if (string.IsNullOrEmpty(sender)) return;
            try
            {
                if (_selectedRecipient == sender)
                {
                    _lastMessageCount = 0;
                    LoadMessages();
                    return;
                }
                AutoSelectRecipient(sender);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] SetRecipientAndLoad error: {ex.Message}");
            }
        }

        private void AutoSelectRecipient(string sender)
        {
            try
            {
                var contact = _allContacts.FirstOrDefault(c =>
                    c.EmployeeId.Equals(sender, StringComparison.OrdinalIgnoreCase) ||
                    c.DisplayName.Equals(sender, StringComparison.OrdinalIgnoreCase));

                if (contact != null)
                {
                    SelectContact(contact.EmployeeId, contact.DisplayName, contact.IsOnline);
                }
                else
                {
                    SelectContact(sender, sender, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] AutoSelectRecipient error: {ex.Message}");
            }
        }

        private void LoadContacts()
        {
            try
            {
                _allContacts.Clear();

                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();

                    // Get all employees from connected_systems with unread counts
                    string sql = @"SELECT DISTINCT ON (cs.employee_id)
                                    cs.employee_id,
                                    COALESCE(cs.display_name, cs.employee_id) as display_name,
                                    cs.machine_name,
                                    COALESCE(cs.is_online, false) as is_online,
                                    COALESCE(unread.cnt, 0) as unread_count,
                                    last_msg.message as last_message,
                                    last_msg.created_at as last_message_time
                                FROM connected_systems cs
                                LEFT JOIN LATERAL (
                                    SELECT COUNT(*) as cnt FROM chat_messages
                                    WHERE sender = cs.employee_id AND recipient = @current_user AND is_read = FALSE AND company_name = @company
                                ) unread ON TRUE
                                LEFT JOIN LATERAL (
                                    SELECT message, created_at FROM chat_messages
                                    WHERE company_name = @company AND ((sender = cs.employee_id AND recipient = @current_user) OR (sender = @current_user AND recipient = cs.employee_id))
                                    ORDER BY created_at DESC LIMIT 1
                                ) last_msg ON TRUE
                                WHERE cs.company_name = @company
                                ORDER BY cs.employee_id, cs.last_heartbeat DESC NULLS LAST";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", _companyName ?? "");
                        cmd.Parameters.AddWithValue("@current_user", _currentUser ?? "");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var empId = reader.GetString(0);
                                if (empId == _currentUser) continue; // Skip self

                                _allContacts.Add(new ContactInfo
                                {
                                    EmployeeId = empId,
                                    DisplayName = reader.IsDBNull(1) ? empId : reader.GetString(1),
                                    MachineName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    IsOnline = !reader.IsDBNull(3) && reader.GetBoolean(3),
                                    UnreadCount = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetInt64(4)),
                                    LastMessage = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    LastMessageTime = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                                });
                            }
                        }
                    }

                    // Add admin if not in list
                    if (!_allContacts.Any(c => c.EmployeeId.Equals("admin", StringComparison.OrdinalIgnoreCase))
                        && !_currentUser.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check admin unread
                        int adminUnread = 0;
                        try
                        {
                            string unreadSql = "SELECT COUNT(*) FROM chat_messages WHERE sender = 'admin' AND recipient = @user AND is_read = FALSE AND company_name = @company";
                            using (var cmd2 = new NpgsqlCommand(unreadSql, connection))
                            {
                                cmd2.Parameters.AddWithValue("@user", _currentUser ?? "");
                                cmd2.Parameters.AddWithValue("@company", _companyName ?? "");
                                adminUnread = Convert.ToInt32(cmd2.ExecuteScalar());
                            }
                        }
                        catch { }

                        _allContacts.Insert(0, new ContactInfo
                        {
                            EmployeeId = "admin",
                            DisplayName = "Admin",
                            MachineName = "",
                            IsOnline = true,
                            UnreadCount = adminUnread,
                            LastMessage = "",
                            LastMessageTime = null
                        });
                    }
                }

                // Sort: unread first, then by last message time
                _allContacts = _allContacts
                    .OrderByDescending(c => c.UnreadCount)
                    .ThenByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                    .ThenBy(c => c.DisplayName)
                    .ToList();

                RenderContacts(_allContacts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] LoadContacts error: {ex.Message}");
            }
        }

        private void RenderContacts(List<ContactInfo> contacts)
        {
            if (contactsListPanel.InvokeRequired)
            {
                contactsListPanel.Invoke(new Action(() => RenderContacts(contacts)));
                return;
            }

            contactsListPanel.Controls.Clear();

            foreach (var contact in contacts)
            {
                var contactPanel = CreateContactPanel(contact);
                contactsListPanel.Controls.Add(contactPanel);
            }
        }

        private Panel CreateContactPanel(ContactInfo contact)
        {
            bool isSelected = _selectedRecipient == contact.EmployeeId;

            var panel = new Panel
            {
                Width = contactsListPanel.Width - 10,
                Height = 60,
                Margin = new Padding(2),
                BackColor = isSelected ? Color.FromArgb(219, 234, 254) : Color.White,
                Cursor = Cursors.Hand,
                Tag = contact
            };

            // Avatar circle
            var avatar = new Label
            {
                Text = (contact.DisplayName ?? "?").Substring(0, 1).ToUpper(),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = isSelected ? Color.FromArgb(59, 130, 246) : Color.FromArgb(148, 163, 184),
                Size = new Size(40, 40),
                Location = new Point(8, 10),
                TextAlign = ContentAlignment.MiddleCenter
            };
            // Make it round-ish
            panel.Controls.Add(avatar);

            // Online indicator
            var statusDot = new Label
            {
                Size = new Size(12, 12),
                Location = new Point(38, 40),
                BackColor = contact.IsOnline ? Color.FromArgb(34, 197, 94) : Color.FromArgb(148, 163, 184)
            };
            panel.Controls.Add(statusDot);

            // Name
            var nameLabel = new Label
            {
                Text = contact.DisplayName ?? contact.EmployeeId,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(56, 6),
                Size = new Size(panel.Width - 100, 22),
                AutoEllipsis = true
            };
            panel.Controls.Add(nameLabel);

            // Last message preview
            var lastMsgLabel = new Label
            {
                Text = string.IsNullOrEmpty(contact.LastMessage) ? "No messages yet" :
                    (contact.LastMessage.Length > 35 ? contact.LastMessage.Substring(0, 35) + "..." : contact.LastMessage),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(56, 30),
                Size = new Size(panel.Width - 100, 20),
                AutoEllipsis = true
            };
            panel.Controls.Add(lastMsgLabel);

            // Unread badge
            if (contact.UnreadCount > 0)
            {
                var badge = new Label
                {
                    Text = contact.UnreadCount.ToString(),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(239, 68, 68),
                    Size = new Size(22, 22),
                    Location = new Point(panel.Width - 35, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panel.Controls.Add(badge);
            }

            // Click handler for all controls
            EventHandler clickHandler = (s, e) =>
            {
                SelectContact(contact.EmployeeId, contact.DisplayName, contact.IsOnline);
            };

            panel.Click += clickHandler;
            foreach (Control ctrl in panel.Controls)
            {
                ctrl.Click += clickHandler;
            }

            return panel;
        }

        private void SelectContact(string employeeId, string displayName, bool isOnline)
        {
            _selectedRecipient = employeeId;
            _selectedRecipientName = displayName;
            _lastMessageCount = -1; // Force re-render by using -1

            // Update header
            chatHeaderLabel.Text = displayName ?? employeeId;
            chatStatusLabel.Text = isOnline ? "● Online" : "● Offline";
            chatStatusLabel.ForeColor = isOnline ? Color.FromArgb(34, 197, 94) : Color.Gray;

            // Enable input
            messageTextBox.Enabled = true;
            sendButton.Enabled = true;

            // Clear messages area immediately to prevent showing old contact's messages
            if (messagesArea.InvokeRequired)
                messagesArea.Invoke(new Action(() => messagesArea.Controls.Clear()));
            else
                messagesArea.Controls.Clear();

            // Refresh contact list to show selection
            RenderContacts(_allContacts);

            // Mark messages as read
            try
            {
                using (var conn = new NpgsqlConnection(ConnStr))
                {
                    conn.Open();
                    string sql = "UPDATE chat_messages SET is_read = TRUE WHERE sender = @sender AND recipient = @recipient AND company_name = @company AND is_read = FALSE";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sender", employeeId ?? "");
                        cmd.Parameters.AddWithValue("@recipient", _currentUser ?? "");
                        cmd.Parameters.AddWithValue("@company", _companyName ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }

            LoadMessages();
        }

        private void LoadMessages()
        {
            if (string.IsNullOrEmpty(_selectedRecipient)) return;

            try
            {
                var messages = new List<(string sender, string message, DateTime timestamp, string messageType, string fileName, int messageId, long fileSize)>();

                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();

                    string selectSql = @"SELECT sender, message, created_at,
                                     COALESCE(message_type, 'text') as message_type,
                                     COALESCE(file_name, '') as file_name,
                                     id, COALESCE(file_size, 0) as file_size
                                     FROM chat_messages
                                     WHERE company_name = @company
                                     AND ((sender = @user1 AND recipient = @user2) OR (sender = @user2 AND recipient = @user1))
                                     ORDER BY created_at ASC";

                    using (var cmd = new NpgsqlCommand(selectSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", _companyName ?? "");
                        cmd.Parameters.AddWithValue("@user1", _currentUser ?? "");
                        cmd.Parameters.AddWithValue("@user2", _selectedRecipient ?? "");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                messages.Add((
                                    reader.GetString(0),
                                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    reader.GetDateTime(2),
                                    reader.GetString(3),
                                    reader.GetString(4),
                                    reader.GetInt32(5),
                                    reader.GetInt64(6)
                                ));
                            }
                        }
                    }

                    // Mark as read
                    try
                    {
                        string markReadSql = @"UPDATE chat_messages SET is_read = TRUE
                                              WHERE sender = @sender AND recipient = @recipient AND is_read = FALSE AND company_name = @company";
                        using (var cmd = new NpgsqlCommand(markReadSql, connection))
                        {
                            cmd.Parameters.AddWithValue("@sender", _selectedRecipient ?? "");
                            cmd.Parameters.AddWithValue("@recipient", _currentUser ?? "");
                            cmd.Parameters.AddWithValue("@company", _companyName ?? "");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }

                // Always render if forced (-1) or if message count changed
                if (_lastMessageCount == -1 || messages.Count != _lastMessageCount)
                {
                    _lastMessageCount = messages.Count;
                    if (messagesArea.InvokeRequired)
                        messagesArea.Invoke(new Action(() => RenderMessages(messages)));
                    else
                        RenderMessages(messages);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat] LoadMessages error: {ex.Message}");
            }
        }

        private void RenderMessages(List<(string sender, string message, DateTime timestamp, string messageType, string fileName, int messageId, long fileSize)> messages)
        {
            messagesArea.Controls.Clear();

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10)
            };

            // Calculate available width (use messagesArea width as fallback since flow may not be laid out yet)
            int availableWidth = messagesArea.Width > 50 ? messagesArea.Width - 50 : 350;

            foreach (var (sender, msg, timestamp, msgType, fileName, msgId, fileSize) in messages)
            {
                bool isOwn = sender == _currentUser;
                var msgPanel = new Panel
                {
                    Width = availableWidth,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(isOwn ? 80 : 5, 3, isOwn ? 5 : 80, 3),
                    BackColor = isOwn ? Color.FromArgb(59, 130, 246) : Color.White,
                    Padding = new Padding(12, 8, 12, 8)
                };

                int yPos = 8;

                // File message
                if (msgType == "file" && !string.IsNullOrEmpty(fileName))
                {
                    var fileLabel = new Label
                    {
                        Text = $"📎 {fileName}",
                        Font = new Font("Segoe UI", 9, FontStyle.Bold),
                        ForeColor = isOwn ? Color.White : Color.FromArgb(30, 64, 175),
                        Location = new Point(12, yPos),
                        AutoSize = true,
                        MaximumSize = new Size(msgPanel.Width - 30, 0)
                    };
                    msgPanel.Controls.Add(fileLabel);
                    yPos += fileLabel.Height + 5;

                    var dlBtn = new Button
                    {
                        Text = "Download",
                        Location = new Point(12, yPos),
                        Size = new Size(100, 28),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = isOwn ? Color.FromArgb(37, 99, 235) : Color.FromArgb(59, 130, 246),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    int captId = msgId;
                    string captName = fileName;
                    dlBtn.Click += (s, e) => DownloadChatFile(captId, captName);
                    msgPanel.Controls.Add(dlBtn);
                    yPos += 33;
                }

                // Text content
                if (!string.IsNullOrEmpty(msg))
                {
                    var msgLabel = new Label
                    {
                        Text = msg,
                        Font = new Font("Segoe UI", 10),
                        ForeColor = isOwn ? Color.White : Color.FromArgb(15, 23, 42),
                        Location = new Point(12, yPos),
                        AutoSize = true,
                        MaximumSize = new Size(300, 0)
                    };
                    msgPanel.Controls.Add(msgLabel);
                    yPos += msgLabel.Height + 5;
                }

                // Time
                var timeLabel = new Label
                {
                    Text = timestamp.ToString("hh:mm tt"),
                    Font = new Font("Segoe UI", 7),
                    ForeColor = isOwn ? Color.FromArgb(191, 219, 254) : Color.Gray,
                    Location = new Point(12, yPos),
                    AutoSize = true
                };
                msgPanel.Controls.Add(timeLabel);

                flow.Controls.Add(msgPanel);
            }

            messagesArea.Controls.Add(flow);

            // Scroll to bottom
            flow.PerformLayout();
            if (flow.Controls.Count > 0)
            {
                flow.ScrollControlIntoView(flow.Controls[flow.Controls.Count - 1]);
            }
        }

        private void DownloadChatFile(int messageId, string fileName)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();
                    string sql = "SELECT file_data, file_name FROM chat_messages WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", messageId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read() && !reader.IsDBNull(0))
                            {
                                byte[] fileData = (byte[])reader[0];
                                string dbFileName = reader.IsDBNull(1) ? fileName : reader.GetString(1);
                                using (var saveDialog = new SaveFileDialog())
                                {
                                    saveDialog.FileName = dbFileName;
                                    saveDialog.Title = "Save File";
                                    if (saveDialog.ShowDialog() == DialogResult.OK)
                                    {
                                        File.WriteAllBytes(saveDialog.FileName, fileData);
                                        MessageBox.Show($"File saved to:\n{saveDialog.FileName}", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("File data not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedRecipient))
            {
                MessageBox.Show("Please select a contact first");
                return;
            }

            string message = messageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();
                    string insertSql = @"INSERT INTO chat_messages (sender, recipient, message, company_name, is_read, message_type)
                        VALUES (@sender, @recipient, @message, @company, FALSE, 'text')";
                    using (var cmd = new NpgsqlCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@sender", _currentUser ?? "");
                        cmd.Parameters.AddWithValue("@recipient", _selectedRecipient ?? "");
                        cmd.Parameters.AddWithValue("@message", message);
                        cmd.Parameters.AddWithValue("@company", (object)_companyName ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                messageTextBox.Clear();
                _lastMessageCount = 0;
                LoadMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private int _contactRefreshCounter = 0;
        private void SetupMessageRefresh()
        {
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 1500; // Faster polling for responsive chat
            _refreshTimer.Tick += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_selectedRecipient))
                    LoadMessages();

                // Refresh contacts every ~5 ticks (7.5 seconds)
                _contactRefreshCounter++;
                if (_contactRefreshCounter >= 5)
                {
                    _contactRefreshCounter = 0;
                    LoadContacts();
                }
            };
            _refreshTimer.Start();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            string search = searchBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(search))
            {
                RenderContacts(_allContacts);
            }
            else
            {
                var filtered = _allContacts.Where(c =>
                    (c.DisplayName ?? "").ToLower().Contains(search) ||
                    (c.EmployeeId ?? "").ToLower().Contains(search)
                ).ToList();
                RenderContacts(filtered);
            }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 450);
            this.BackColor = Color.FromArgb(241, 245, 249);

            // Main split - contacts left, chat right
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 240,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };

            // ===== LEFT PANEL - Contacts =====
            contactsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // Header
            var contactsHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 5)
            };
            var chatTitle = new Label
            {
                Text = "💬 Chat",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            contactsHeader.Controls.Add(chatTitle);
            contactsPanel.Controls.Add(contactsHeader);

            // Search
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = Color.White
            };
            searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Search contacts...",
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.TextChanged += SearchBox_TextChanged;
            searchPanel.Controls.Add(searchBox);
            contactsPanel.Controls.Add(searchPanel);

            // Contacts list
            contactsListPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(4)
            };
            contactsPanel.Controls.Add(contactsListPanel);

            splitContainer.Panel1.Controls.Add(contactsPanel);

            // ===== RIGHT PANEL - Chat Area =====
            chatPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 245, 249)
            };

            // Chat header
            var chatHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 8)
            };

            chatHeaderLabel = new Label
            {
                Text = "Select a contact to start chatting",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 8),
                AutoSize = true
            };
            chatHeaderPanel.Controls.Add(chatHeaderLabel);

            chatStatusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(16, 32),
                AutoSize = true
            };
            chatHeaderPanel.Controls.Add(chatStatusLabel);
            chatPanel.Controls.Add(chatHeaderPanel);

            // Messages area
            messagesArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 245, 249),
                AutoScroll = true
            };
            chatPanel.Controls.Add(messagesArea);

            // Input area
            var inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            messageTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false,
                PlaceholderText = "Type a message..."
            };
            messageTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    SendButton_Click(s, e);
                }
            };

            sendButton = new Button
            {
                Text = "Send",
                Dock = DockStyle.Right,
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            sendButton.Click += SendButton_Click;

            inputPanel.Controls.Add(messageTextBox);
            inputPanel.Controls.Add(sendButton);
            chatPanel.Controls.Add(inputPanel);

            splitContainer.Panel2.Controls.Add(chatPanel);

            this.Controls.Add(splitContainer);
        }
    }

    public class ContactInfo
    {
        public string EmployeeId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string MachineName { get; set; } = "";
        public bool IsOnline { get; set; }
        public int UnreadCount { get; set; }
        public string LastMessage { get; set; } = "";
        public DateTime? LastMessageTime { get; set; }
    }
}
