using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace EmployeeAttendance
{
    /// <summary>
    /// Monitors USB drive connections and file transfers (copy to/from USB, delete on USB)
    /// </summary>
    public class UsbFileMonitor : IDisposable
    {
        private readonly string _activationKey;
        private readonly string _companyName;
        private readonly string _username;
        private readonly string _displayName;

        private ManagementEventWatcher? _insertWatcher;
        private ManagementEventWatcher? _removeWatcher;
        private readonly Dictionary<string, FileSystemWatcher> _driveWatchers = new();
        private readonly Dictionary<string, DriveInfo> _usbDrives = new();
        private readonly HashSet<string> _recentlyLogged = new();
        private readonly object _lock = new object();
        private System.Threading.Timer? _cleanupTimer;
        private bool _isRunning = false;
        private bool _disposed = false;

        // Track file snapshots per drive for detecting copies TO usb
        private readonly Dictionary<string, Dictionary<string, long>> _driveFileSnapshots = new();

        public event Action<string>? OnLog;

        public UsbFileMonitor(string activationKey, string companyName, string username, string displayName)
        {
            _activationKey = activationKey;
            _companyName = companyName;
            _username = username;
            _displayName = displayName;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            Log("USB File Monitor starting...");

            // Scan existing USB drives
            ScanExistingUsbDrives();

            // Watch for new USB drive insertions via WMI
            try
            {
                _insertWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"));
                _insertWatcher.EventArrived += OnDriveInserted;
                _insertWatcher.Start();

                _removeWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3"));
                _removeWatcher.EventArrived += OnDriveRemoved;
                _removeWatcher.Start();

                Log("WMI drive watchers started");
            }
            catch (Exception ex)
            {
                Log($"WMI watcher error: {ex.Message}");
                // Fallback: poll for drives every 5 seconds
                _cleanupTimer = new System.Threading.Timer(_ => ScanExistingUsbDrives(), null, 5000, 5000);
            }

            // Cleanup old entries from dedup set every 30 seconds
            if (_cleanupTimer == null)
            {
                _cleanupTimer = new System.Threading.Timer(_ =>
                {
                    lock (_lock)
                    {
                        if (_recentlyLogged.Count > 1000)
                            _recentlyLogged.Clear();
                    }
                    // Also re-check for new USB drives
                    ScanExistingUsbDrives();
                }, null, 30000, 30000);
            }

            Log("USB File Monitor started successfully");
        }

        public void Stop()
        {
            _isRunning = false;

            try { _insertWatcher?.Stop(); } catch { }
            try { _removeWatcher?.Stop(); } catch { }
            try { _cleanupTimer?.Dispose(); } catch { }

            lock (_lock)
            {
                foreach (var watcher in _driveWatchers.Values)
                {
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                    }
                    catch { }
                }
                _driveWatchers.Clear();
                _usbDrives.Clear();
                _driveFileSnapshots.Clear();
            }

            Log("USB File Monitor stopped");
        }

        private void ScanExistingUsbDrives()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                    .ToList();

                foreach (var drive in drives)
                {
                    string driveLetter = drive.Name.TrimEnd('\\');
                    lock (_lock)
                    {
                        if (!_driveWatchers.ContainsKey(driveLetter))
                        {
                            StartWatchingDrive(drive);
                        }
                    }
                }

                // Remove watchers for drives that are no longer present
                lock (_lock)
                {
                    var currentDriveLetters = drives.Select(d => d.Name.TrimEnd('\\')).ToHashSet();
                    var removedDrives = _driveWatchers.Keys.Where(k => !currentDriveLetters.Contains(k)).ToList();
                    foreach (var removed in removedDrives)
                    {
                        StopWatchingDrive(removed);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Drive scan error: {ex.Message}");
            }
        }

        private void OnDriveInserted(object sender, EventArrivedEventArgs e)
        {
            // Wait a moment for the drive to be ready
            Thread.Sleep(2000);
            ScanExistingUsbDrives();
        }

        private void OnDriveRemoved(object sender, EventArrivedEventArgs e)
        {
            ScanExistingUsbDrives();
        }

        private void StartWatchingDrive(DriveInfo drive)
        {
            try
            {
                string driveLetter = drive.Name.TrimEnd('\\');
                string driveLabel = "";
                try { driveLabel = drive.VolumeLabel; } catch { driveLabel = "USB Drive"; }

                Log($"USB Drive detected: {driveLetter} ({driveLabel})");

                _usbDrives[driveLetter] = drive;

                // Take initial snapshot of files on the USB
                TakeFileSnapshot(drive);

                // Create FileSystemWatcher for this USB drive
                var watcher = new FileSystemWatcher(drive.Name)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                   NotifyFilters.Size | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                watcher.Created += (s, e) => OnFileCreatedOnUsb(e, driveLetter, driveLabel);
                watcher.Deleted += (s, e) => OnFileDeletedOnUsb(e, driveLetter, driveLabel);
                watcher.Renamed += (s, e) => OnFileRenamedOnUsb(e, driveLetter, driveLabel);
                watcher.Error += (s, e) => Log($"Watcher error on {driveLetter}: {e.GetException().Message}");

                _driveWatchers[driveLetter] = watcher;

                Log($"Now monitoring USB drive: {driveLetter} ({driveLabel})");
            }
            catch (Exception ex)
            {
                Log($"Error watching drive: {ex.Message}");
            }
        }

        private void StopWatchingDrive(string driveLetter)
        {
            try
            {
                if (_driveWatchers.TryGetValue(driveLetter, out var watcher))
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    _driveWatchers.Remove(driveLetter);
                }
                _usbDrives.Remove(driveLetter);
                _driveFileSnapshots.Remove(driveLetter);
                Log($"Stopped monitoring drive: {driveLetter}");
            }
            catch (Exception ex)
            {
                Log($"Error stopping drive watcher: {ex.Message}");
            }
        }

        private void TakeFileSnapshot(DriveInfo drive)
        {
            try
            {
                string driveLetter = drive.Name.TrimEnd('\\');
                var snapshot = new Dictionary<string, long>();

                var files = Directory.GetFiles(drive.Name, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        snapshot[file] = fi.Length;
                    }
                    catch { }
                }

                _driveFileSnapshots[driveLetter] = snapshot;
                Log($"Snapshot taken for {driveLetter}: {snapshot.Count} files");
            }
            catch (Exception ex)
            {
                Log($"Snapshot error: {ex.Message}");
            }
        }

        private void OnFileCreatedOnUsb(FileSystemEventArgs e, string driveLetter, string driveLabel)
        {
            if (!_isRunning) return;
            if (IsSystemFile(e.FullPath)) return;

            // Deduplicate: avoid logging same file multiple times
            string dedupKey = $"TO_USB|{e.FullPath}|{DateTime.Now:yyyyMMddHHmmss}";
            lock (_lock)
            {
                if (_recentlyLogged.Contains(dedupKey)) return;
                _recentlyLogged.Add(dedupKey);
            }

            Task.Run(() =>
            {
                try
                {
                    // Wait briefly for file to finish writing
                    Thread.Sleep(500);

                    long fileSize = 0;
                    try
                    {
                        if (File.Exists(e.FullPath))
                        {
                            var fi = new FileInfo(e.FullPath);
                            fileSize = fi.Length;
                        }
                    }
                    catch { }

                    string fileName = Path.GetFileName(e.FullPath);
                    string fileExt = Path.GetExtension(e.FullPath);

                    Log($"[TO_USB] {fileName} ({FormatSize(fileSize)}) -> {driveLetter} ({driveLabel})");

                    DatabaseHelper.InsertUSBFileLog(
                        _activationKey, _companyName, _username, _displayName,
                        fileName, e.FullPath, fileSize, fileExt, "TO_USB",
                        "", e.FullPath, driveLetter, driveLabel);
                }
                catch (Exception ex)
                {
                    Log($"Error logging TO_USB: {ex.Message}");
                }
            });
        }

        private void OnFileDeletedOnUsb(FileSystemEventArgs e, string driveLetter, string driveLabel)
        {
            if (!_isRunning) return;
            if (IsSystemFile(e.FullPath)) return;

            string dedupKey = $"DELETE_USB|{e.FullPath}|{DateTime.Now:yyyyMMddHHmmss}";
            lock (_lock)
            {
                if (_recentlyLogged.Contains(dedupKey)) return;
                _recentlyLogged.Add(dedupKey);
            }

            Task.Run(() =>
            {
                try
                {
                    string fileName = Path.GetFileName(e.FullPath);
                    string fileExt = Path.GetExtension(e.FullPath);

                    // Check if this was a MOVE (copy FROM USB then delete)
                    // If file was recently created elsewhere, it's a FROM_USB transfer
                    long fileSize = 0;
                    lock (_lock)
                    {
                        if (_driveFileSnapshots.TryGetValue(driveLetter, out var snapshot))
                        {
                            if (snapshot.TryGetValue(e.FullPath, out var size))
                            {
                                fileSize = size;
                                snapshot.Remove(e.FullPath);
                            }
                        }
                    }

                    Log($"[DELETE_USB] {fileName} from {driveLetter} ({driveLabel})");

                    DatabaseHelper.InsertUSBFileLog(
                        _activationKey, _companyName, _username, _displayName,
                        fileName, e.FullPath, fileSize, fileExt, "DELETE_USB",
                        e.FullPath, "", driveLetter, driveLabel);
                }
                catch (Exception ex)
                {
                    Log($"Error logging DELETE_USB: {ex.Message}");
                }
            });
        }

        private void OnFileRenamedOnUsb(RenamedEventArgs e, string driveLetter, string driveLabel)
        {
            if (!_isRunning) return;
            if (IsSystemFile(e.FullPath)) return;

            Task.Run(() =>
            {
                try
                {
                    long fileSize = 0;
                    try
                    {
                        if (File.Exists(e.FullPath))
                        {
                            var fi = new FileInfo(e.FullPath);
                            fileSize = fi.Length;
                        }
                    }
                    catch { }

                    string fileName = Path.GetFileName(e.FullPath);
                    string fileExt = Path.GetExtension(e.FullPath);

                    Log($"[RENAME_USB] {Path.GetFileName(e.OldFullPath)} -> {fileName} on {driveLetter}");

                    DatabaseHelper.InsertUSBFileLog(
                        _activationKey, _companyName, _username, _displayName,
                        fileName, e.FullPath, fileSize, fileExt, "TO_USB",
                        e.OldFullPath, e.FullPath, driveLetter, driveLabel);
                }
                catch (Exception ex)
                {
                    Log($"Error logging RENAME_USB: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Call this from a timer to detect files copied FROM USB to local disk.
        /// Monitors common user folders for new files that match files on USB drives.
        /// </summary>
        public void CheckForCopiesFromUsb()
        {
            if (!_isRunning) return;

            try
            {
                // Monitor common user folders
                string[] watchFolders = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                };

                foreach (var folder in watchFolders)
                {
                    if (!Directory.Exists(folder)) continue;

                    try
                    {
                        // Get files modified in the last 30 seconds
                        var recentFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                            .Select(f => new FileInfo(f))
                            .Where(fi => fi.LastWriteTime > DateTime.Now.AddSeconds(-30))
                            .ToList();

                        foreach (var file in recentFiles)
                        {
                            // Check if this file matches a file on any USB drive
                            lock (_lock)
                            {
                                foreach (var kvp in _driveFileSnapshots)
                                {
                                    var matchingFile = kvp.Value.FirstOrDefault(f =>
                                        Path.GetFileName(f.Key) == file.Name && f.Value == file.Length);

                                    if (matchingFile.Key != null)
                                    {
                                        string dedupKey = $"FROM_USB|{file.FullName}|{file.LastWriteTime:yyyyMMddHHmmss}";
                                        if (!_recentlyLogged.Contains(dedupKey))
                                        {
                                            _recentlyLogged.Add(dedupKey);

                                            string driveLetter = kvp.Key;
                                            string driveLabel = "";
                                            if (_usbDrives.TryGetValue(driveLetter, out var drive))
                                            {
                                                try { driveLabel = drive.VolumeLabel; } catch { driveLabel = "USB Drive"; }
                                            }

                                            Log($"[FROM_USB] {file.Name} ({FormatSize(file.Length)}) from {driveLetter} -> {folder}");

                                            Task.Run(() =>
                                            {
                                                DatabaseHelper.InsertUSBFileLog(
                                                    _activationKey, _companyName, _username, _displayName,
                                                    file.Name, file.FullName, file.Length, file.Extension, "FROM_USB",
                                                    matchingFile.Key, file.FullName, driveLetter, driveLabel);
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"CheckForCopiesFromUsb error: {ex.Message}");
            }
        }

        private bool IsSystemFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            string name = Path.GetFileName(path).ToLower();
            // Skip system/temp files
            return name.StartsWith("~$") || name == "thumbs.db" || name == "desktop.ini" ||
                   name == ".ds_store" || name.EndsWith(".tmp") || name.StartsWith(".");
        }

        private string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            if (i >= sizes.Length) i = sizes.Length - 1;
            return $"{bytes / Math.Pow(1024, i):F1} {sizes[i]}";
        }

        private void Log(string message)
        {
            string logMsg = $"[UsbMonitor] {message}";
            System.Diagnostics.Debug.WriteLine(logMsg);
            OnLog?.Invoke(logMsg);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _insertWatcher?.Dispose();
            _removeWatcher?.Dispose();
        }
    }
}
