using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EmployeeAttendance
{
    /// <summary>
    /// Embedded call window with WebView2 for WebRTC calling
    /// </summary>
    public class CallWindow : Form
    {
        private WebView2 webView;
        private Panel controlPanel;
        private Label statusLabel;
        private Label timerLabel;
        private Button endCallButton;
        private Button muteButton;
        private Button videoToggleButton;
        private System.Windows.Forms.Timer callTimer;
        private DateTime callStartTime;
        private bool isMuted = false;
        private bool isVideoOff = false;

        private string _callUrl;
        private string _remotePerson;
        private string _callType;

        public CallWindow(string callUrl, string remotePerson, string callType)
        {
            _callUrl = callUrl;
            _remotePerson = remotePerson;
            _callType = callType;

            InitializeComponent();
            InitializeWebView();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = $"Call with {_remotePerson}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(640, 480);
            this.BackColor = Color.FromArgb(17, 17, 17);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.ShowIcon = true;
            this.TopMost = true;

            // Control Panel at bottom
            controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(controlPanel);

            // Status label
            statusLabel = new Label
            {
                Text = "Connecting...",
                ForeColor = Color.FromArgb(156, 163, 175),
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 15),
                AutoSize = true
            };
            controlPanel.Controls.Add(statusLabel);

            // Timer label
            timerLabel = new Label
            {
                Text = "00:00",
                ForeColor = Color.FromArgb(59, 130, 246),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 40),
                AutoSize = true
            };
            controlPanel.Controls.Add(timerLabel);

            // Calculate button positions (centered)
            int buttonY = 25;
            int buttonSpacing = 70;
            int totalWidth = buttonSpacing * 3;
            int startX = (this.ClientSize.Width - totalWidth) / 2;

            // Mute button
            muteButton = new Button
            {
                Text = "🔇",
                Font = new Font("Segoe UI", 16F),
                Size = new Size(60, 60),
                Location = new Point(startX, buttonY),
                BackColor = Color.FromArgb(55, 65, 81),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            muteButton.FlatAppearance.BorderSize = 0;
            muteButton.Click += MuteButton_Click;
            controlPanel.Controls.Add(muteButton);

            // End call button
            endCallButton = new Button
            {
                Text = "📞",
                Font = new Font("Segoe UI", 16F),
                Size = new Size(60, 60),
                Location = new Point(startX + buttonSpacing, buttonY),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            endCallButton.FlatAppearance.BorderSize = 0;
            endCallButton.Click += EndCallButton_Click;
            controlPanel.Controls.Add(endCallButton);

            // Video toggle button (only for video calls)
            if (_callType == "video")
            {
                videoToggleButton = new Button
                {
                    Text = "📹",
                    Font = new Font("Segoe UI", 16F),
                    Size = new Size(60, 60),
                    Location = new Point(startX + buttonSpacing * 2, buttonY),
                    BackColor = Color.FromArgb(55, 65, 81),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                videoToggleButton.FlatAppearance.BorderSize = 0;
                videoToggleButton.Click += VideoToggleButton_Click;
                controlPanel.Controls.Add(videoToggleButton);
            }

            // Handle window resize to re-center buttons
            this.Resize += (s, e) =>
            {
                int newStartX = (this.ClientSize.Width - totalWidth) / 2;
                muteButton.Location = new Point(newStartX, buttonY);
                endCallButton.Location = new Point(newStartX + buttonSpacing, buttonY);
                if (videoToggleButton != null)
                    videoToggleButton.Location = new Point(newStartX + buttonSpacing * 2, buttonY);
            };

            // Call timer
            callTimer = new System.Windows.Forms.Timer();
            callTimer.Interval = 1000;
            callTimer.Tick += CallTimer_Tick;

            // Handle form closing
            this.FormClosing += CallWindow_FormClosing;
        }

        private async void InitializeWebView()
        {
            try
            {
                webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };
                this.Controls.Add(webView);
                webView.BringToFront();

                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);

                // Load the call URL
                webView.CoreWebView2.Navigate(_callUrl);

                // Monitor page load
                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        statusLabel.Text = "Connected";
                        statusLabel.ForeColor = Color.FromArgb(34, 197, 94);
                        StartCallTimer();
                    }
                    else
                    {
                        statusLabel.Text = "Connection failed";
                        statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    }
                };

                // Listen for console messages (for debugging)
                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    Debug.WriteLine($"[CallWindow] WebMessage: {e.TryGetWebMessageAsString()}");
                };

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallWindow] WebView initialization error: {ex.Message}");
                MessageBox.Show(
                    "Unable to initialize call interface. Please ensure Microsoft Edge WebView2 Runtime is installed.\n\n" +
                    "Download from: https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                    "WebView2 Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                this.Close();
            }
        }

        private void StartCallTimer()
        {
            callStartTime = DateTime.Now;
            callTimer.Start();
        }

        private void CallTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - callStartTime;
            timerLabel.Text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        private async void MuteButton_Click(object sender, EventArgs e)
        {
            isMuted = !isMuted;
            muteButton.BackColor = isMuted ? Color.FromArgb(239, 68, 68) : Color.FromArgb(55, 65, 81);
            muteButton.Text = isMuted ? "🔊" : "🔇";

            // Execute JavaScript to mute/unmute
            try
            {
                string script = isMuted ? "window.toggleMute?.(true)" : "window.toggleMute?.(false)";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch { }
        }

        private async void VideoToggleButton_Click(object sender, EventArgs e)
        {
            isVideoOff = !isVideoOff;
            videoToggleButton.BackColor = isVideoOff ? Color.FromArgb(239, 68, 68) : Color.FromArgb(55, 65, 81);
            videoToggleButton.Text = isVideoOff ? "📹❌" : "📹";

            // Execute JavaScript to toggle video
            try
            {
                string script = isVideoOff ? "window.toggleVideo?.(false)" : "window.toggleVideo?.(true)";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch { }
        }

        private async void EndCallButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Execute JavaScript to end call
                await webView.CoreWebView2.ExecuteScriptAsync("window.endCall?.()");
            }
            catch { }

            this.Close();
        }

        private void CallWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            callTimer?.Stop();
            callTimer?.Dispose();

            try
            {
                // Ensure call is ended
                webView?.CoreWebView2?.ExecuteScriptAsync("window.endCall?.()");
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                callTimer?.Dispose();
                webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
