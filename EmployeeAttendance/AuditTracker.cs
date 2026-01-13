using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace EmployeeAttendance
{
    /// <summary>
    /// Comprehensive audit tracker for applications, web history, inactivity, screenshots, and live streaming
    /// </summary>
    public class AuditTracker : IDisposable
    {
        // Win32 APIs for idle time detection
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        
        // Activation details
        private string activationKey = "";
        private string companyName = "";
        private string username = "";
        private string displayName = "";  // User's proper display name
        
        // System ID for remote viewing
        private string systemId = "";
        
        // Timers
        private System.Windows.Forms.Timer appTrackingTimer = null!;      // Track foreground apps
        private System.Windows.Forms.Timer browserScanTimer = null!;      // Scan browser history
        private System.Windows.Forms.Timer inactivityTimer = null!;       // Check for idle time
        private System.Windows.Forms.Timer screenshotTimer = null!;       // Capture screenshots
        private System.Windows.Forms.Timer syncTimer = null!;             // Sync to database
        private System.Windows.Forms.Timer heartbeatTimer = null!;        // System heartbeat
        private System.Windows.Forms.Timer liveStreamTimer = null!;       // Live stream for viewers
        
        // Tracking state
        private bool isTracking = false;
        private DateTime lastActivityTime = DateTime.Now;
        private Point lastMousePosition = Point.Empty;
        private bool isCurrentlyInactive = false;
        private DateTime? inactivityStartTime = null;
        
        // Current app tracking
        private string lastTrackedApp = "";
        private string lastTrackedTitle = "";
        private DateTime lastAppStartTime = DateTime.Now;
        
        // Log storage (in-memory before sync)
        private List<AppUsageLog> appUsageLogs = new List<AppUsageLog>();
        private List<WebLogEntry> webLogs = new List<WebLogEntry>();
        private List<InactivityLog> inactivityLogs = new List<InactivityLog>();
        private HashSet<string> syncedUrls = new HashSet<string>();  // Prevent duplicate web logs
        
        // Settings
        private int screenshotIntervalSeconds = 600;   // 10 minutes (600 seconds)
        private int inactivityThresholdSeconds = 60;  // 1 minute of no input = inactive
        
        public bool IsTracking => isTracking;
        public string SystemId => systemId;
        
        public AuditTracker(string activationKey, string companyName, string username, string displayName)
        {
            this.activationKey = activationKey;
            this.companyName = companyName;
            this.username = username;
            this.displayName = displayName;
            
            // Ensure audit tables exist
            DatabaseHelper.EnsureAuditTablesExist();
            
            // Generate and register system ID
            systemId = DatabaseHelper.GetOrCreateSystemId();
            
            InitializeTimers();
        }
        
        private void InitializeTimers()
        {
            // App tracking timer - every 3 seconds
            appTrackingTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            appTrackingTimer.Tick += AppTrackingTimer_Tick;
            
            // Browser scan timer - every 30 seconds
            browserScanTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            browserScanTimer.Tick += BrowserScanTimer_Tick;
            
            // Inactivity timer - every 1 second
            inactivityTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            inactivityTimer.Tick += InactivityTimer_Tick;
            
            // Screenshot timer - configurable (default 30 seconds)
            screenshotTimer = new System.Windows.Forms.Timer { Interval = screenshotIntervalSeconds * 1000 };
            screenshotTimer.Tick += ScreenshotTimer_Tick;
            
            // Sync timer - every 10 seconds
            syncTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            syncTimer.Tick += SyncTimer_Tick;
            
            // Heartbeat timer - every 30 seconds
            heartbeatTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            heartbeatTimer.Tick += HeartbeatTimer_Tick;
            
            // Live stream timer - every 2 seconds
            liveStreamTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            liveStreamTimer.Tick += LiveStreamTimer_Tick;
        }
        
        /// <summary>
        /// Start all tracking
        /// </summary>
        public void StartTracking()
        {
            if (isTracking) return;
            isTracking = true;
            
            lastActivityTime = DateTime.Now;
            lastMousePosition = Cursor.Position;
            
            // Register system for remote viewing
            _ = Task.Run(() =>
            {
                try { DatabaseHelper.RegisterSystem(activationKey, companyName, username); }
                catch { }
            });
            
            // Start all timers
            appTrackingTimer.Start();
            browserScanTimer.Start();
            inactivityTimer.Start();
            screenshotTimer.Start();
            syncTimer.Start();
            heartbeatTimer.Start();
            liveStreamTimer.Start();
            
            LogToFile("Audit tracking started");
        }
        
        /// <summary>
        /// Stop all tracking
        /// </summary>
        public void StopTracking()
        {
            if (!isTracking) return;
            isTracking = false;
            
            // Stop all timers
            appTrackingTimer.Stop();
            browserScanTimer.Stop();
            inactivityTimer.Stop();
            screenshotTimer.Stop();
            syncTimer.Stop();
            heartbeatTimer.Stop();
            liveStreamTimer.Stop();
            
            // Final sync
            SyncAllData();
            
            LogToFile("Audit tracking stopped");
        }
        
        #region Timer Event Handlers
        
        private void AppTrackingTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                TrackForegroundApp();
            }
            catch { }
        }
        
        private void BrowserScanTimer_Tick(object? sender, EventArgs e)
        {
            _ = Task.Run(() =>
            {
                try { ScanBrowserHistory(); }
                catch { }
            });
        }
        
        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                CheckInactivity();
            }
            catch { }
        }
        
        private void ScreenshotTimer_Tick(object? sender, EventArgs e)
        {
            _ = Task.Run(() =>
            {
                try { CaptureScreenshot(); }
                catch { }
            });
        }
        
        private void SyncTimer_Tick(object? sender, EventArgs e)
        {
            _ = Task.Run(() =>
            {
                try { SyncAllData(); }
                catch { }
            });
        }
        
        private void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(systemId))
            {
                _ = Task.Run(() =>
                {
                    try { DatabaseHelper.UpdateSystemHeartbeat(systemId); }
                    catch { }
                });
            }
        }
        
        private void LiveStreamTimer_Tick(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(systemId))
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (DatabaseHelper.HasActiveViewingSession(systemId))
                        {
                            CaptureAndSendLiveFrame();
                        }
                    }
                    catch { }
                });
            }
        }
        
        #endregion
        
        #region Application Tracking
        
        private void TrackForegroundApp()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;
            
            // Get window title
            StringBuilder title = new StringBuilder(256);
            GetWindowText(hwnd, title, 256);
            string windowTitle = title.ToString();
            
            // Get process name
            GetWindowThreadProcessId(hwnd, out uint processId);
            string appName = "";
            try
            {
                var process = Process.GetProcessById((int)processId);
                appName = process.ProcessName;
            }
            catch { return; }
            
            // Check if app changed
            if (appName != lastTrackedApp || windowTitle != lastTrackedTitle)
            {
                // Log previous app if valid
                if (!string.IsNullOrEmpty(lastTrackedApp))
                {
                    TimeSpan duration = DateTime.Now - lastAppStartTime;
                    if (duration.TotalSeconds >= 5)  // Only log if used for 5+ seconds
                    {
                        lock (appUsageLogs)
                        {
                            appUsageLogs.Add(new AppUsageLog
                            {
                                AppName = lastTrackedApp,
                                WindowTitle = lastTrackedTitle,
                                StartTime = lastAppStartTime,
                                EndTime = DateTime.Now,
                                IsSynced = false
                            });
                        }
                    }
                }
                
                // Start tracking new app
                lastTrackedApp = appName;
                lastTrackedTitle = windowTitle;
                lastAppStartTime = DateTime.Now;
            }
            
            // Update activity time (user is active if using apps)
            lastActivityTime = DateTime.Now;
        }
        
        #endregion
        
        #region Browser History Scanning
        
        private void ScanBrowserHistory()
        {
            DateTime since = DateTime.Now.AddHours(-24);  // Last 24 hours
            
            try { ScanChromeHistory(since); } catch { }
            try { ScanEdgeHistory(since); } catch { }
            try { ScanFirefoxHistory(since); } catch { }
        }
        
        private void ScanChromeHistory(DateTime since)
        {
            string historyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data", "Default", "History");
            
            if (!File.Exists(historyPath)) return;
            
            // Copy database to temp (Chrome locks the file)
            string tempPath = Path.Combine(Path.GetTempPath(), $"chrome_history_{Guid.NewGuid()}.db");
            try
            {
                File.Copy(historyPath, tempPath, true);
                ReadSQLiteHistory(tempPath, "Chrome", since);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
        
        private void ScanEdgeHistory(DateTime since)
        {
            string historyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default", "History");
            
            if (!File.Exists(historyPath)) return;
            
            string tempPath = Path.Combine(Path.GetTempPath(), $"edge_history_{Guid.NewGuid()}.db");
            try
            {
                File.Copy(historyPath, tempPath, true);
                ReadSQLiteHistory(tempPath, "Edge", since);
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
        
        private void ScanFirefoxHistory(DateTime since)
        {
            string profilesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Mozilla", "Firefox", "Profiles");
            
            if (!Directory.Exists(profilesPath)) return;
            
            var profiles = Directory.GetDirectories(profilesPath, "*.default*");
            foreach (var profile in profiles)
            {
                string historyPath = Path.Combine(profile, "places.sqlite");
                if (!File.Exists(historyPath)) continue;
                
                string tempPath = Path.Combine(Path.GetTempPath(), $"firefox_history_{Guid.NewGuid()}.db");
                try
                {
                    File.Copy(historyPath, tempPath, true);
                    ReadFirefoxHistory(tempPath, since);
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
        
        private void ReadSQLiteHistory(string dbPath, string browserName, DateTime since)
        {
            try
            {
                string connStr = $"Data Source={dbPath};Version=3;Read Only=True;";
                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    
                    // Chrome/Edge epoch: January 1, 1601
                    long chromeEpoch = (since.ToUniversalTime() - new DateTime(1601, 1, 1)).Ticks / 10;
                    
                    string sql = @"SELECT url, title, last_visit_time FROM urls 
                                   WHERE last_visit_time > @since ORDER BY last_visit_time DESC LIMIT 100";
                    
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@since", chromeEpoch);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string url = reader.GetString(0);
                                string title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                long visitTime = reader.GetInt64(2);
                                
                                // Convert Chrome timestamp to DateTime
                                DateTime visitDateTime = new DateTime(1601, 1, 1).AddTicks(visitTime * 10);
                                
                                // Create unique key to prevent duplicates
                                string uniqueKey = $"{browserName}|{url}|{visitDateTime:yyyyMMddHHmm}";
                                
                                lock (syncedUrls)
                                {
                                    if (syncedUrls.Contains(uniqueKey)) continue;
                                    syncedUrls.Add(uniqueKey);
                                }
                                
                                lock (webLogs)
                                {
                                    webLogs.Add(new WebLogEntry
                                    {
                                        Browser = browserName,
                                        Url = url.Length > 2000 ? url.Substring(0, 2000) : url,
                                        Title = title.Length > 500 ? title.Substring(0, 500) : title,
                                        Category = CategorizeUrl(url),
                                        VisitTime = visitDateTime,
                                        IsSynced = false
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        private void ReadFirefoxHistory(string dbPath, DateTime since)
        {
            try
            {
                string connStr = $"Data Source={dbPath};Version=3;Read Only=True;";
                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    
                    // Firefox uses microseconds since Unix epoch
                    long firefoxEpoch = ((DateTimeOffset)since.ToUniversalTime()).ToUnixTimeSeconds() * 1000000;
                    
                    string sql = @"SELECT p.url, p.title, h.visit_date 
                                   FROM moz_places p 
                                   JOIN moz_historyvisits h ON p.id = h.place_id 
                                   WHERE h.visit_date > @since 
                                   ORDER BY h.visit_date DESC LIMIT 100";
                    
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@since", firefoxEpoch);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string url = reader.GetString(0);
                                string title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                long visitTime = reader.GetInt64(2);
                                
                                // Convert Firefox timestamp
                                DateTime visitDateTime = DateTimeOffset.FromUnixTimeSeconds(visitTime / 1000000).LocalDateTime;
                                
                                string uniqueKey = $"Firefox|{url}|{visitDateTime:yyyyMMddHHmm}";
                                
                                lock (syncedUrls)
                                {
                                    if (syncedUrls.Contains(uniqueKey)) continue;
                                    syncedUrls.Add(uniqueKey);
                                }
                                
                                lock (webLogs)
                                {
                                    webLogs.Add(new WebLogEntry
                                    {
                                        Browser = "Firefox",
                                        Url = url.Length > 2000 ? url.Substring(0, 2000) : url,
                                        Title = title.Length > 500 ? title.Substring(0, 500) : title,
                                        Category = CategorizeUrl(url),
                                        VisitTime = visitDateTime,
                                        IsSynced = false
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        private string CategorizeUrl(string url)
        {
            url = url.ToLower();
            
            if (url.Contains("youtube") || url.Contains("netflix") || url.Contains("spotify") || url.Contains("twitch"))
                return "Entertainment";
            if (url.Contains("facebook") || url.Contains("twitter") || url.Contains("instagram") || url.Contains("linkedin") || url.Contains("reddit"))
                return "Social Media";
            if (url.Contains("gmail") || url.Contains("outlook") || url.Contains("mail"))
                return "Email";
            if (url.Contains("google") || url.Contains("bing") || url.Contains("duckduckgo"))
                return "Search";
            if (url.Contains("github") || url.Contains("stackoverflow") || url.Contains("docs."))
                return "Development";
            if (url.Contains("amazon") || url.Contains("ebay") || url.Contains("shop") || url.Contains("store"))
                return "Shopping";
            if (url.Contains("news") || url.Contains("bbc") || url.Contains("cnn"))
                return "News";
            
            return "General";
        }
        
        #endregion
        
        #region Inactivity Detection
        
        private void CheckInactivity()
        {
            uint idleTime = GetIdleTime();
            
            if (idleTime >= inactivityThresholdSeconds * 1000)  // Convert to ms
            {
                // User is inactive
                if (!isCurrentlyInactive)
                {
                    isCurrentlyInactive = true;
                    inactivityStartTime = DateTime.Now.AddMilliseconds(-idleTime);
                    LogToFile($"User became inactive. Idle time: {idleTime / 1000}s");
                }
            }
            else
            {
                // User is active
                if (isCurrentlyInactive && inactivityStartTime.HasValue)
                {
                    // Log the inactivity period
                    TimeSpan duration = DateTime.Now - inactivityStartTime.Value;
                    if (duration.TotalSeconds >= inactivityThresholdSeconds)
                    {
                        LogToFile($"Logging inactivity period: {duration.TotalSeconds:F0}s ({inactivityStartTime.Value:HH:mm:ss} to {DateTime.Now:HH:mm:ss})");
                        lock (inactivityLogs)
                        {
                            inactivityLogs.Add(new InactivityLog
                            {
                                StartTime = inactivityStartTime.Value,
                                EndTime = DateTime.Now,
                                DurationSeconds = (int)duration.TotalSeconds,
                                IsSynced = false
                            });
                        }
                    }
                }
                
                isCurrentlyInactive = false;
                inactivityStartTime = null;
                lastActivityTime = DateTime.Now;
            }
        }
        
        private uint GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            
            if (!GetLastInputInfo(ref lastInputInfo))
                return 0;
            
            return (uint)Environment.TickCount - lastInputInfo.dwTime;
        }
        
        #endregion
        
        #region Screenshot Capture
        
        private void CaptureScreenshot()
        {
            try
            {
                LogToFile("Starting screenshot capture...");
                
                // Get combined screen bounds
                Rectangle bounds = Rectangle.Empty;
                foreach (var screen in Screen.AllScreens)
                {
                    bounds = Rectangle.Union(bounds, screen.Bounds);
                }
                
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    LogToFile("Screenshot failed: Invalid screen bounds");
                    return;
                }
                
                using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                    }
                    
                    // Compress to JPEG (30% quality to save space)
                    using (MemoryStream ms = new MemoryStream())
                    {
                        var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        
                        if (jpegEncoder != null)
                        {
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 30L);
                            screenshot.Save(ms, jpegEncoder, encoderParams);
                        }
                        else
                        {
                            screenshot.Save(ms, ImageFormat.Jpeg);
                        }
                        
                        string base64 = Convert.ToBase64String(ms.ToArray());
                        LogToFile($"Screenshot captured: {bounds.Width}x{bounds.Height}, size: {base64.Length / 1024}KB");
                        
                        // Insert to database
                        bool success = DatabaseHelper.InsertScreenshot(activationKey, companyName, username, displayName,
                            base64, bounds.Width, bounds.Height);
                        LogToFile($"Screenshot insert result: {(success ? "SUCCESS" : "FAILED")}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Screenshot error: {ex.Message}");
            }
        }
        
        private void CaptureAndSendLiveFrame()
        {
            try
            {
                Rectangle bounds = Rectangle.Empty;
                foreach (var screen in Screen.AllScreens)
                {
                    bounds = Rectangle.Union(bounds, screen.Bounds);
                }
                
                if (bounds.Width <= 0 || bounds.Height <= 0) return;
                
                using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                    }
                    
                    // Very low quality for live streaming (faster)
                    using (MemoryStream ms = new MemoryStream())
                    {
                        var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        
                        if (jpegEncoder != null)
                        {
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 20L);
                            screenshot.Save(ms, jpegEncoder, encoderParams);
                        }
                        else
                        {
                            screenshot.Save(ms, ImageFormat.Jpeg);
                        }
                        
                        string base64 = Convert.ToBase64String(ms.ToArray());
                        DatabaseHelper.InsertLiveStreamFrame(systemId, base64, bounds.Width, bounds.Height);
                    }
                }
            }
            catch { }
        }
        
        #endregion
        
        #region Data Sync
        
        private void SyncAllData()
        {
            // Sync app usage logs
            lock (appUsageLogs)
            {
                foreach (var log in appUsageLogs.Where(l => !l.IsSynced))
                {
                    try
                    {
                        if (DatabaseHelper.InsertAppLog(activationKey, companyName, username, displayName,
                            log.AppName, log.WindowTitle, log.StartTime, log.EndTime))
                        {
                            log.IsSynced = true;
                        }
                    }
                    catch { }
                }
                
                // Remove old synced logs
                appUsageLogs.RemoveAll(l => l.IsSynced && l.EndTime < DateTime.Now.AddHours(-1));
            }
            
            // Sync web logs
            lock (webLogs)
            {
                var toSync = webLogs.Where(l => !l.IsSynced).ToList();
                if (toSync.Count > 0)
                {
                    int synced = DatabaseHelper.InsertWebLogsBatch(activationKey, companyName, username, displayName, toSync);
                    if (synced > 0)
                    {
                        foreach (var log in toSync)
                        {
                            log.IsSynced = true;
                        }
                    }
                }
                
                // Remove old synced logs
                webLogs.RemoveAll(l => l.IsSynced && l.VisitTime < DateTime.Now.AddHours(-1));
            }
            
            // Sync inactivity logs
            lock (inactivityLogs)
            {
                var toSync = inactivityLogs.Where(l => !l.IsSynced).ToList();
                if (toSync.Count > 0)
                {
                    LogToFile($"Syncing {toSync.Count} inactivity logs...");
                }
                foreach (var log in toSync)
                {
                    try
                    {
                        if (DatabaseHelper.InsertInactivityLog(activationKey, companyName, username, displayName,
                            log.StartTime, log.EndTime, "INACTIVE"))
                        {
                            log.IsSynced = true;
                            LogToFile($"Inactivity log synced: {log.DurationSeconds}s");
                        }
                        else
                        {
                            LogToFile($"Inactivity log sync FAILED");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Inactivity sync error: {ex.Message}");
                    }
                }
                
                // Remove old synced logs
                inactivityLogs.RemoveAll(l => l.IsSynced && l.EndTime < DateTime.Now.AddHours(-1));
            }
        }
        
        #endregion
        
        #region Helpers
        
        private void LogToFile(string message)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EmployeeAttendance");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "audit.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            }
            catch { }
        }
        
        public void Dispose()
        {
            StopTracking();
            
            appTrackingTimer?.Dispose();
            browserScanTimer?.Dispose();
            inactivityTimer?.Dispose();
            screenshotTimer?.Dispose();
            syncTimer?.Dispose();
            heartbeatTimer?.Dispose();
            liveStreamTimer?.Dispose();
        }
        
        #endregion
    }
}
