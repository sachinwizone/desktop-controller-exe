using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EmployeeAttendance
{
    /// <summary>
    /// Collects installed applications from Windows registry and syncs to server
    /// </summary>
    public class InstalledAppsCollector
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverApiUrl;
        private readonly string _activationKey;
        private System.Threading.Timer? _syncTimer;
        private const int SYNC_INTERVAL_MINUTES = 10; // Sync every 10 minutes

        public InstalledAppsCollector(string serverUrl, string activationKey)
        {
            _httpClient = new HttpClient();
            _serverApiUrl = serverUrl.TrimEnd('/') + "/api";
            _activationKey = activationKey;
        }

        /// <summary>
        /// Start periodic installed apps collection and syncing
        /// </summary>
        public void Start()
        {
            // Initial sync immediately
            _ = SyncInstalledApps();

            // Then sync periodically
            _syncTimer = new System.Threading.Timer(async (state) =>
            {
                await SyncInstalledApps();
            }, null, TimeSpan.FromMinutes(SYNC_INTERVAL_MINUTES), TimeSpan.FromMinutes(SYNC_INTERVAL_MINUTES));
        }

        /// <summary>
        /// Stop the sync timer
        /// </summary>
        public void Stop()
        {
            _syncTimer?.Dispose();
        }

        /// <summary>
        /// Get all installed applications from Windows registry
        /// </summary>
        private List<InstalledApp> GetInstalledApps()
        {
            var apps = new List<InstalledApp>();

            try
            {
                // Paths in Windows registry where applications are listed
                var registryPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var path in registryPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null) continue;

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;

                                try
                                {
                                    var displayName = subKey.GetValue("DisplayName")?.ToString();
                                    if (string.IsNullOrWhiteSpace(displayName)) continue;

                                    var app = new InstalledApp
                                    {
                                        Name = displayName,
                                        Version = subKey.GetValue("DisplayVersion")?.ToString() ?? "",
                                        Publisher = subKey.GetValue("Publisher")?.ToString() ?? "",
                                        InstallDate = subKey.GetValue("InstallDate")?.ToString() ?? "",
                                        UninstallString = subKey.GetValue("UninstallString")?.ToString() ?? "",
                                        InstallLocation = subKey.GetValue("InstallLocation")?.ToString() ?? "",
                                        Size = GetAppSize(subKey),
                                        RegistryPath = subKeyName,
                                        Hive = path.Contains("WOW6432Node") ? "x86" : "x64"
                                    };

                                    // Only add if it looks like a real application
                                    if (!apps.Any(a => a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        apps.Add(app);
                                    }
                                }
                                catch
                                {
                                    // Skip entries with errors
                                }
                            }
                        }
                    }
                }

                // Also check Program Files directories for portable apps
                apps.AddRange(GetPortableApps());

                return apps.OrderBy(a => a.Name).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting installed apps: {ex.Message}");
                return new List<InstalledApp>();
            }
        }

        /// <summary>
        /// Detect portable applications
        /// </summary>
        private List<InstalledApp> GetPortableApps()
        {
            var portableApps = new List<InstalledApp>();

            try
            {
                var programFiles = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                };

                foreach (var programFile in programFiles)
                {
                    if (!System.IO.Directory.Exists(programFile)) continue;

                    try
                    {
                        var directories = System.IO.Directory.GetDirectories(programFile);
                        foreach (var dir in directories.Take(50)) // Limit to prevent long scanning
                        {
                            try
                            {
                                var dirInfo = new System.IO.DirectoryInfo(dir);
                                var exeFiles = System.IO.Directory.GetFiles(dir, "*.exe", System.IO.SearchOption.TopDirectoryOnly);

                                if (exeFiles.Length > 0)
                                {
                                    var app = new InstalledApp
                                    {
                                        Name = dirInfo.Name,
                                        Version = "Portable",
                                        InstallLocation = dir,
                                        Publisher = "Unknown",
                                        Hive = programFile.Contains("Program Files (x86)") ? "x86" : "x64"
                                    };

                                    if (!portableApps.Any(a => a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        portableApps.Add(app);
                                    }
                                }
                            }
                            catch
                            {
                                // Skip directories with access errors
                            }
                        }
                    }
                    catch
                    {
                        // Skip if directory can't be accessed
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return portableApps;
        }

        /// <summary>
        /// Get application size from registry
        /// </summary>
        private string GetAppSize(RegistryKey key)
        {
            try
            {
                var estimatedSize = key.GetValue("EstimatedSize");
                if (estimatedSize != null && long.TryParse(estimatedSize.ToString(), out long sizeKb))
                {
                    var sizeMb = sizeKb / 1024.0;
                    if (sizeMb > 1024)
                        return $"{(sizeMb / 1024.0):F2} GB";
                    return $"{sizeMb:F2} MB";
                }
            }
            catch
            {
                // Ignore errors
            }

            return "Unknown";
        }

        /// <summary>
        /// Sync installed apps to server
        /// </summary>
        private async Task SyncInstalledApps()
        {
            try
            {
                var apps = GetInstalledApps();
                var data = new
                {
                    ActivationKey = _activationKey,
                    ComputerName = Environment.MachineName,
                    UserName = Environment.UserName,
                    SyncedAt = DateTime.UtcNow,
                    TotalApps = apps.Count,
                    Applications = apps
                };

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_serverApiUrl}/installed-apps/sync", content);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to sync installed apps: {response.StatusCode}");
                }
                else
                {
                    Debug.WriteLine($"Successfully synced {apps.Count} installed apps");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing installed apps: {ex.Message}");
            }
        }

        /// <summary>
        /// Force refresh of installed apps list
        /// </summary>
        public async Task RefreshNow()
        {
            await SyncInstalledApps();
        }
    }

    // ==================== Data Models ====================

    public class InstalledApp
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string InstallDate { get; set; } = "";
        public string UninstallString { get; set; } = "";
        public string InstallLocation { get; set; } = "";
        public string Size { get; set; } = "";
        public string RegistryPath { get; set; } = "";
        public string Hive { get; set; } = "x64"; // x64 or x86
    }

    public class InstalledAppsData
    {
        public string ActivationKey { get; set; } = "";
        public string ComputerName { get; set; } = "";
        public string UserName { get; set; } = "";
        public DateTime SyncedAt { get; set; }
        public int TotalApps { get; set; }
        public List<InstalledApp> Applications { get; set; } = new();
    }
}
