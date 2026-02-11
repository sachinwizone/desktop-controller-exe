using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace EmployeeAttendance
{
    /// <summary>
    /// Tracks file renames and email activity (attachments, sent emails with domain tracking).
    /// Uses FileSystemWatcher for file renames and Outlook COM interop for email tracking.
    /// </summary>
    public class FileActivityTracker : IDisposable
    {
        private readonly string _companyName;
        private readonly string _userName;
        private readonly string _systemName;
        private readonly string _machineId;

        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private System.Threading.Timer? _emailCheckTimer;
        private DateTime _lastEmailCheckTime;
        private bool _isRunning = false;

        // Buffer for batching DB writes
        private readonly object _logLock = new object();
        private List<FileActivityEntry> _pendingLogs = new List<FileActivityEntry>();
        private System.Threading.Timer? _flushTimer;

        public FileActivityTracker(string companyName, string userName, string systemName, string machineId)
        {
            _companyName = companyName;
            _userName = userName;
            _systemName = systemName;
            _machineId = machineId;
            _lastEmailCheckTime = DateTime.Now.AddMinutes(-5); // Check last 5 mins on start
        }

        /// <summary>
        /// Start tracking file renames and email activity
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            Debug.WriteLine("[FileActivityTracker] Starting...");

            // Watch common user directories for file renames
            StartFileWatchers();

            // Periodic email check
            _emailCheckTimer = new System.Threading.Timer(_ => CheckOutlookSentItems(), null,
                TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));

            // Flush buffer every 30 seconds
            _flushTimer = new System.Threading.Timer(_ => FlushPendingLogs(), null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            Debug.WriteLine("[FileActivityTracker] Started successfully");
        }

        /// <summary>
        /// Stop all tracking
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
            _emailCheckTimer?.Dispose();
            _flushTimer?.Dispose();
            FlushPendingLogs();
            Debug.WriteLine("[FileActivityTracker] Stopped");
        }

        /// <summary>
        /// Set up FileSystemWatchers on key directories
        /// </summary>
        private void StartFileWatchers()
        {
            var directories = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir)) continue;

                try
                {
                    var watcher = new FileSystemWatcher(dir)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true
                    };

                    watcher.Renamed += OnFileRenamed;
                    _watchers.Add(watcher);
                    Debug.WriteLine($"[FileActivityTracker] Watching: {dir}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileActivityTracker] Error watching {dir}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle file rename event
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                var entry = new FileActivityEntry
                {
                    ActivityType = "file_rename",
                    OldFileName = Path.GetFileName(e.OldFullPath),
                    NewFileName = Path.GetFileName(e.FullPath),
                    FilePath = Path.GetDirectoryName(e.FullPath) ?? "",
                    FileName = Path.GetFileName(e.FullPath),
                    Details = $"Renamed from '{Path.GetFileName(e.OldFullPath)}' to '{Path.GetFileName(e.FullPath)}'"
                };

                lock (_logLock)
                {
                    _pendingLogs.Add(entry);
                }

                Debug.WriteLine($"[FileActivityTracker] File renamed: {e.OldName} -> {e.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileActivityTracker] Rename event error: {ex.Message}");
            }
        }

        // P/Invoke for GetActiveObject (not available in .NET 6 Marshal)
        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject([MarshalAs(UnmanagedType.LPStruct)] Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        // Outlook.Application CLSID
        private static readonly Guid OutlookClsid = new Guid("0006F03A-0000-0000-C000-000000000046");

        /// <summary>
        /// Try to get running Outlook COM object
        /// </summary>
        private static object? GetOutlookInstance()
        {
            try
            {
                GetActiveObject(OutlookClsid, IntPtr.Zero, out object outlookObj);
                return outlookObj;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check Outlook sent items for email activity
        /// Uses COM interop to access Outlook if running
        /// </summary>
        private void CheckOutlookSentItems()
        {
            if (!_isRunning) return;

            try
            {
                dynamic? outlookApp = GetOutlookInstance();
                if (outlookApp == null) return;

                try
                {
                    dynamic nameSpace = outlookApp.GetNamespace("MAPI");
                    // olFolderSentMail = 5
                    dynamic sentFolder = nameSpace.GetDefaultFolder(5);
                    dynamic items = sentFolder.Items;

                    // Sort by sent time descending
                    items.Sort("[SentOn]", true);

                    int checked_count = 0;
                    foreach (dynamic item in items)
                    {
                        try
                        {
                            DateTime sentOn = item.SentOn;
                            if (sentOn <= _lastEmailCheckTime) break;
                            if (checked_count++ > 50) break;

                            string recipients = "";
                            string domains = "";
                            try
                            {
                                dynamic recipientsObj = item.Recipients;
                                var recipientList = new List<string>();
                                var domainList = new List<string>();

                                for (int i = 1; i <= recipientsObj.Count; i++)
                                {
                                    string address = "";
                                    try
                                    {
                                        address = recipientsObj[i].Address ?? recipientsObj[i].Name ?? "";
                                    }
                                    catch
                                    {
                                        address = recipientsObj[i].Name ?? "";
                                    }

                                    recipientList.Add(address);
                                    if (address.Contains("@"))
                                    {
                                        domainList.Add(address.Split('@').Last());
                                    }
                                }

                                recipients = string.Join("; ", recipientList);
                                domains = string.Join("; ", domainList.Distinct());
                            }
                            catch { }

                            string emailSubject = "";
                            try { emailSubject = item.Subject ?? ""; } catch { }

                            string emailSenderAddr = "";
                            try { emailSenderAddr = item.SenderEmailAddress ?? _userName; } catch { emailSenderAddr = _userName; }

                            lock (_logLock)
                            {
                                _pendingLogs.Add(new FileActivityEntry
                                {
                                    ActivityType = "email_sent",
                                    EmailSender = emailSenderAddr,
                                    EmailRecipient = recipients,
                                    EmailSubject = emailSubject,
                                    EmailDomain = domains,
                                    FileName = emailSubject,
                                    Details = $"Email sent to: {recipients}"
                                });
                            }

                            // Check for attachments
                            try
                            {
                                dynamic attachments = item.Attachments;
                                if (attachments.Count > 0)
                                {
                                    for (int a = 1; a <= attachments.Count; a++)
                                    {
                                        string attachName = attachments[a].FileName ?? "Unknown";
                                        long attachSize = 0;
                                        try { attachSize = attachments[a].Size; } catch { }

                                        lock (_logLock)
                                        {
                                            _pendingLogs.Add(new FileActivityEntry
                                            {
                                                ActivityType = "email_attachment",
                                                FileName = attachName,
                                                FileSize = attachSize,
                                                EmailSender = emailSenderAddr,
                                                EmailDomain = domains,
                                                EmailRecipient = recipients,
                                                EmailSubject = emailSubject,
                                                Details = $"Attachment '{attachName}' ({attachSize / 1024}KB) sent to: {recipients}"
                                            });
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        catch { }
                        finally
                        {
                            try { Marshal.ReleaseComObject(item); } catch { }
                        }
                    }

                    _lastEmailCheckTime = DateTime.Now;

                    try { Marshal.ReleaseComObject(items); } catch { }
                    try { Marshal.ReleaseComObject(sentFolder); } catch { }
                    try { Marshal.ReleaseComObject(nameSpace); } catch { }
                }
                finally
                {
                    // Don't release outlookApp - it's a running instance we connected to
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileActivityTracker] Email check error: {ex.Message}");
            }
        }

        /// <summary>
        /// Flush pending log entries to database
        /// </summary>
        private void FlushPendingLogs()
        {
            List<FileActivityEntry> toFlush;
            lock (_logLock)
            {
                if (_pendingLogs.Count == 0) return;
                toFlush = new List<FileActivityEntry>(_pendingLogs);
                _pendingLogs.Clear();
            }

            foreach (var entry in toFlush)
            {
                try
                {
                    DatabaseHelper.SaveFileActivityLog(
                        _companyName, _userName, _systemName, _machineId,
                        entry.ActivityType, entry.FileName, entry.OldFileName,
                        entry.NewFileName, entry.FilePath, entry.EmailDomain,
                        entry.EmailRecipient, entry.Details,
                        entry.EmailSender, entry.EmailSubject, entry.FileSize
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileActivityTracker] Flush error: {ex.Message}");
                }
            }

            if (toFlush.Count > 0)
                Debug.WriteLine($"[FileActivityTracker] Flushed {toFlush.Count} entries to database");
        }

        public void Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    /// Data class for file activity log entry
    /// </summary>
    public class FileActivityEntry
    {
        public string ActivityType { get; set; } = "";
        public string FileName { get; set; } = "";
        public string OldFileName { get; set; } = "";
        public string NewFileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string EmailDomain { get; set; } = "";
        public string EmailSender { get; set; } = "";
        public string EmailRecipient { get; set; } = "";
        public string EmailSubject { get; set; } = "";
        public long FileSize { get; set; } = 0;
        public string Details { get; set; } = "";
    }
}
