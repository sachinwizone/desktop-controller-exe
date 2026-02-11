using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using Npgsql;

namespace EmployeeAttendance
{
    /// <summary>
    /// Tray Chat System - In-app chat for peer-to-peer messaging between users.
    /// Polls for new admin messages and shows tray notifications.
    /// </summary>
    public class TrayChatSystem
    {
        private ChatForm _chatForm;
        private string _currentUser;
        private string _companyName;
        private string _apiBaseUrl;
        private NotifyIcon _trayIcon;
        private System.Windows.Forms.Timer _pollTimer;
        private int _lastKnownMessageId = 0;
        private int _lastIncomingCallId = 0; // Track last incoming call to avoid duplicate notifications
        private string _lastSender = null; // Track the last sender for auto-select
        private const int POLL_INTERVAL_MS = 5000; // 5 seconds
        private const string ConnStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=10;CommandTimeout=15;";

        public TrayChatSystem(string apiBaseUrl, string companyName, string currentUser)
        {
            _apiBaseUrl = apiBaseUrl;
            _companyName = companyName;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Set the tray icon reference for balloon notifications
        /// </summary>
        public void SetTrayIcon(NotifyIcon trayIcon)
        {
            _trayIcon = trayIcon;

            // When user clicks the balloon notification, open chat with the sender
            if (_trayIcon != null)
            {
                _trayIcon.BalloonTipClicked += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(_lastSender))
                    {
                        ShowChatWindowWithSender(_lastSender);
                    }
                };
            }
        }

        /// <summary>
        /// Start polling for new chat messages from admin
        /// </summary>
        public void StartNotificationPolling()
        {
            try
            {
                // Get the last known message ID first
                _lastKnownMessageId = GetLastMessageId();
                Debug.WriteLine($"[TrayChatSystem] Starting poll. Last known message ID: {_lastKnownMessageId}");

                _pollTimer = new System.Windows.Forms.Timer();
                _pollTimer.Interval = POLL_INTERVAL_MS;
                _pollTimer.Tick += (s, e) => CheckForNewMessages();
                _pollTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayChatSystem] StartNotificationPolling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop polling
        /// </summary>
        public void StopNotificationPolling()
        {
            _pollTimer?.Stop();
            _pollTimer?.Dispose();
        }

        /// <summary>
        /// Check for new unread messages and incoming calls
        /// </summary>
        private void CheckForNewMessages()
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();

                    // ===== CHECK FOR INCOMING CALLS =====
                    try
                    {
                        string callSql = @"SELECT id, caller, call_type FROM call_signaling
                                           WHERE callee = @callee AND company_name = @company
                                           AND status = 'ringing' AND started_at > NOW() - interval '30 seconds'
                                           ORDER BY started_at DESC LIMIT 1";
                        using (var callCmd = new NpgsqlCommand(callSql, connection))
                        {
                            callCmd.Parameters.AddWithValue("@callee", _currentUser ?? "");
                            callCmd.Parameters.AddWithValue("@company", _companyName ?? "");
                            using (var callReader = callCmd.ExecuteReader())
                            {
                                if (callReader.Read())
                                {
                                    int callId = callReader.GetInt32(0);
                                    string caller = callReader.GetString(1);
                                    string callType = callReader.IsDBNull(2) ? "audio" : callReader.GetString(2);

                                    if (callId != _lastIncomingCallId)
                                    {
                                        _lastIncomingCallId = callId;
                                        ShowNotification($"Incoming {callType} call", $"{caller} is calling you...");
                                        ShowIncomingCallForm(callId, caller, callType);
                                        Debug.WriteLine($"[TrayChatSystem] Incoming {callType} call from {caller} (callId: {callId})");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine($"[TrayChatSystem] Call check error: {ex.Message}"); }

                    // ===== CHECK FOR NEW MESSAGES =====
                    string sql = @"SELECT id, sender, message, message_type, file_name, created_at
                                   FROM chat_messages
                                   WHERE recipient = @recipient
                                   AND id > @lastId
                                   AND (is_read = FALSE OR is_read IS NULL)
                                   ORDER BY id ASC LIMIT 5";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@recipient", _currentUser ?? "");
                        cmd.Parameters.AddWithValue("@lastId", _lastKnownMessageId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            var newMessages = new List<(int id, string sender, string message, string messageType, string fileName)>();
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string sender = reader.GetString(1);
                                string message = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                string msgType = reader.IsDBNull(3) ? "text" : reader.GetString(3);
                                string fileName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                newMessages.Add((id, sender, message, msgType, fileName));
                            }

                            if (newMessages.Count > 0)
                            {
                                // Update last known ID
                                _lastKnownMessageId = newMessages.Max(m => m.id);

                                // Show notification for the latest message
                                var latest = newMessages.Last();
                                _lastSender = latest.sender; // Store for balloon click handler

                                string notifMessage;
                                if (latest.messageType == "file")
                                {
                                    notifMessage = $"Sent a file: {latest.fileName}";
                                }
                                else
                                {
                                    notifMessage = latest.message.Length > 80
                                        ? latest.message.Substring(0, 80) + "..."
                                        : latest.message;
                                }

                                string title = $"New message from {latest.sender}";
                                if (newMessages.Count > 1)
                                {
                                    title = $"{newMessages.Count} new messages";
                                }

                                // Show balloon tip
                                ShowNotification(title, notifMessage);

                                // Auto-open chat window with the sender auto-selected
                                ShowChatWindowWithSender(latest.sender);

                                Debug.WriteLine($"[TrayChatSystem] {newMessages.Count} new message(s) from {latest.sender}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayChatSystem] CheckForNewMessages error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the last message ID for this user to avoid duplicate notifications
        /// </summary>
        private int GetLastMessageId()
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnStr))
                {
                    connection.Open();
                    string sql = @"SELECT COALESCE(MAX(id), 0) FROM chat_messages
                                   WHERE recipient = @recipient";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@recipient", _currentUser ?? "");
                        var result = cmd.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayChatSystem] GetLastMessageId error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Show balloon notification via tray icon
        /// </summary>
        private void ShowNotification(string title, string message)
        {
            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayChatSystem] ShowNotification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Show chat window with a specific sender auto-selected
        /// </summary>
        public void ShowChatWindowWithSender(string sender)
        {
            try
            {
                if (_chatForm == null || _chatForm.IsDisposed)
                {
                    // Create new form with auto-select sender
                    _chatForm = new ChatForm(_currentUser, sender);
                }
                else
                {
                    // Form already exists - tell it to switch to this sender
                    if (_chatForm.InvokeRequired)
                    {
                        _chatForm.Invoke(new Action(() =>
                        {
                            _chatForm.SetRecipientAndLoad(sender);
                        }));
                    }
                    else
                    {
                        _chatForm.SetRecipientAndLoad(sender);
                    }
                }

                // Show and bring to front
                if (_chatForm.InvokeRequired)
                {
                    _chatForm.Invoke(new Action(() =>
                    {
                        _chatForm.Show();
                        _chatForm.WindowState = FormWindowState.Normal;
                        _chatForm.BringToFront();
                        _chatForm.Activate();
                    }));
                }
                else
                {
                    _chatForm.Show();
                    _chatForm.WindowState = FormWindowState.Normal;
                    _chatForm.BringToFront();
                    _chatForm.Activate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing chat with sender: {ex.Message}");
            }
        }

        /// <summary>
        /// Show chat window (no sender pre-selected)
        /// </summary>
        public void ShowChatWindow()
        {
            try
            {
                if (_chatForm == null || _chatForm.IsDisposed)
                {
                    _chatForm = new ChatForm(_currentUser);
                }

                if (_chatForm.InvokeRequired)
                {
                    _chatForm.Invoke(new Action(() =>
                    {
                        _chatForm.Show();
                        _chatForm.WindowState = FormWindowState.Normal;
                        _chatForm.BringToFront();
                        _chatForm.Activate();
                    }));
                }
                else
                {
                    _chatForm.Show();
                    _chatForm.WindowState = FormWindowState.Normal;
                    _chatForm.BringToFront();
                    _chatForm.Activate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing chat: {ex.Message}");
            }
        }

        /// <summary>
        /// Show incoming call form with Accept/Reject buttons
        /// </summary>
        private void ShowIncomingCallForm(int callId, string caller, string callType)
        {
            try
            {
                // Run on UI thread
                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    var mainForm = System.Windows.Forms.Application.OpenForms[0];
                    mainForm.Invoke(new Action(() => ShowIncomingCallFormUI(callId, caller, callType)));
                }
                else
                {
                    var thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            ShowIncomingCallFormUI(callId, caller, callType);
                        }
                        catch (Exception ex) { Debug.WriteLine($"[TrayChatSystem] ShowIncomingCallForm thread error: {ex.Message}"); }
                    });
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[TrayChatSystem] ShowIncomingCallForm outer error: {ex.Message}"); }
        }

        private void ShowIncomingCallFormUI(int callId, string caller, string callType)
        {
            try
            {
                string icon = callType == "video" ? "📹" : "📞";
                var form = new Form
                {
                    Text = $"Incoming {callType} Call",
                    ClientSize = new System.Drawing.Size(400, 280),
                    StartPosition = FormStartPosition.CenterScreen,
                    TopMost = true,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
                    ShowInTaskbar = true,
                    AutoScaleMode = AutoScaleMode.Dpi
                };

                // Icon Panel
                var iconPanel = new Panel
                {
                    Location = new System.Drawing.Point(150, 20),
                    Size = new System.Drawing.Size(100, 100),
                    BackColor = System.Drawing.Color.FromArgb(59, 130, 246)
                };
                var iconLabel = new Label
                {
                    Text = icon,
                    Font = new System.Drawing.Font("Segoe UI Emoji", 40, System.Drawing.FontStyle.Regular),
                    ForeColor = System.Drawing.Color.White,
                    AutoSize = true,
                    Location = new System.Drawing.Point(25, 20)
                };
                iconPanel.Controls.Add(iconLabel);
                form.Controls.Add(iconPanel);

                // Caller Name
                var callerLabel = new Label
                {
                    Text = caller,
                    Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.White,
                    Location = new System.Drawing.Point(20, 135),
                    Size = new System.Drawing.Size(360, 30),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                form.Controls.Add(callerLabel);

                // Call Type Label
                var typeLabel = new Label
                {
                    Text = $"Incoming {callType} call...",
                    Font = new System.Drawing.Font("Segoe UI", 11),
                    ForeColor = System.Drawing.Color.FromArgb(156, 163, 175),
                    Location = new System.Drawing.Point(20, 170),
                    Size = new System.Drawing.Size(360, 25),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                form.Controls.Add(typeLabel);

                // Accept Button
                var acceptBtn = new Button
                {
                    Text = "✓ Accept",
                    Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                    BackColor = System.Drawing.Color.FromArgb(34, 197, 94),
                    ForeColor = System.Drawing.Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new System.Drawing.Point(30, 210),
                    Size = new System.Drawing.Size(165, 50),
                    Cursor = Cursors.Hand
                };
                acceptBtn.FlatAppearance.BorderSize = 0;
                acceptBtn.Click += async (s, e) =>
                {
                    try
                    {
                        // Update call status to 'answered' with signal data
                        using (var conn = new NpgsqlConnection(ConnStr))
                        {
                            conn.Open();

                            // First get the offer signal data
                            string signalData = null;
                            using (var getCmd = new NpgsqlCommand("SELECT signal_data FROM call_signaling WHERE id = @id", conn))
                            {
                                getCmd.Parameters.AddWithValue("@id", callId);
                                var result = getCmd.ExecuteScalar();
                                signalData = result?.ToString();
                            }

                            // Update status to answered
                            using (var cmd = new NpgsqlCommand("UPDATE call_signaling SET status = 'answered', answered_at = NOW() WHERE id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", callId);
                                cmd.ExecuteNonQuery();
                            }

                            // Create answer signal (for now just acknowledge - full WebRTC will be added)
                            if (!string.IsNullOrEmpty(signalData))
                            {
                                // Store answer signal - this will be a placeholder until WebRTC is fully implemented
                                string answerSignal = "{\"type\":\"answer\",\"sdp\":\"answered\"}";
                                using (var answerCmd = new NpgsqlCommand("UPDATE call_signaling SET signal_data = @signal WHERE id = @id", conn))
                                {
                                    answerCmd.Parameters.AddWithValue("@signal", answerSignal);
                                    answerCmd.Parameters.AddWithValue("@id", callId);
                                    answerCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        Debug.WriteLine($"[TrayChatSystem] Call {callId} accepted from {caller}");

                        // Show call interface (opens browser-based WebRTC interface)
                        OpenWebRTCCallInterface(callId, caller, callType, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[TrayChatSystem] Accept call error: {ex.Message}");
                        MessageBox.Show($"Error accepting call: {ex.Message}", "Call Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    form.Close();
                };
                form.Controls.Add(acceptBtn);

                // Reject Button
                var rejectBtn = new Button
                {
                    Text = "✕ Reject",
                    Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                    BackColor = System.Drawing.Color.FromArgb(239, 68, 68),
                    ForeColor = System.Drawing.Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new System.Drawing.Point(205, 210),
                    Size = new System.Drawing.Size(165, 50),
                    Cursor = Cursors.Hand
                };
                rejectBtn.FlatAppearance.BorderSize = 0;
                rejectBtn.Click += (s, e) =>
                {
                    try
                    {
                        // Update call status to 'rejected'
                        using (var conn = new NpgsqlConnection(ConnStr))
                        {
                            conn.Open();
                            using (var cmd = new NpgsqlCommand("UPDATE call_signaling SET status = 'rejected', ended_at = NOW() WHERE id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", callId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        Debug.WriteLine($"[TrayChatSystem] Call {callId} rejected from {caller}");
                    }
                    catch (Exception ex) { Debug.WriteLine($"[TrayChatSystem] Reject call error: {ex.Message}"); }
                    form.Close();
                };
                form.Controls.Add(rejectBtn);

                // Auto-close after 30 seconds if not answered
                var autoCloseTimer = new System.Windows.Forms.Timer { Interval = 30000 };
                autoCloseTimer.Tick += (s, e) =>
                {
                    autoCloseTimer.Stop();
                    autoCloseTimer.Dispose();
                    if (!form.IsDisposed)
                    {
                        try
                        {
                            // Mark call as missed
                            using (var conn = new NpgsqlConnection(ConnStr))
                            {
                                conn.Open();
                                using (var cmd = new NpgsqlCommand("UPDATE call_signaling SET status = 'missed', ended_at = NOW() WHERE id = @id", conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", callId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        catch { }
                        form.Close();
                    }
                };
                autoCloseTimer.Start();

                form.ShowDialog();
            }
            catch (Exception ex) { Debug.WriteLine($"[TrayChatSystem] ShowIncomingCallFormUI error: {ex.Message}"); }
        }

        /// <summary>
        /// Open WebRTC call interface in embedded window
        /// </summary>
        private void OpenWebRTCCallInterface(int callId, string remotePerson, string callType, bool isOutgoing)
        {
            try
            {
                // Construct the call URL
                string url = $"{_apiBaseUrl.Replace("/api", "")}/call.html?callId={callId}&user={_currentUser}&remote={remotePerson}&type={callType}&role={(isOutgoing ? "caller" : "callee")}";

                Debug.WriteLine($"[TrayChatSystem] Opening embedded call interface: {url}");

                // Open embedded call window (within EXE)
                var callWindow = new CallWindow(url, remotePerson, callType);
                callWindow.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayChatSystem] Error opening call interface: {ex.Message}");

                // Fallback to browser if embedded window fails
                try
                {
                    string url = $"{_apiBaseUrl.Replace("/api", "")}/call.html?callId={callId}&user={_currentUser}&remote={remotePerson}&type={callType}&role={(isOutgoing ? "caller" : "callee")}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    Debug.WriteLine($"[TrayChatSystem] Fallback: Opened call in browser");
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine($"[TrayChatSystem] Browser fallback also failed: {ex2.Message}");
                }
            }
        }

        /// <summary>
        /// Cleanup resources on exit
        /// </summary>
        public void Exit()
        {
            try
            {
                StopNotificationPolling();
                if (_chatForm != null && !_chatForm.IsDisposed)
                {
                    _chatForm.Close();
                    _chatForm.Dispose();
                }
            }
            catch { }
        }
    }
}
