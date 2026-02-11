using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace EmployeeAttendance
{
    /// <summary>
    /// Collects and sends system data (installed software, system info, etc.)
    /// </summary>
    public class SystemDataSender
    {
        private readonly string _activationKey;
        private readonly string _companyName;
        private readonly string _systemName;
        private readonly string _userName;
        private System.Threading.Timer _collectionTimer;

        public SystemDataSender(string activationKey, string companyName, string systemName, string userName)
        {
            _activationKey = activationKey;
            _companyName = companyName;
            _systemName = systemName;
            _userName = userName;
        }

        /// <summary>
        /// Start periodic system data collection
        /// </summary>
        public void Start()
        {
            // Collect immediately
            CollectAndSendSystemData();
            
            // Then collect every 24 hours
            _collectionTimer = new System.Threading.Timer(_ =>
            {
                CollectAndSendSystemData();
            }, null, TimeSpan.FromHours(24), TimeSpan.FromHours(24));
        }

        /// <summary>
        /// Stop system data collection
        /// </summary>
        public void Stop()
        {
            _collectionTimer?.Dispose();
        }

        /// <summary>
        /// Collect and send system information
        /// </summary>
        private void CollectAndSendSystemData()
        {
            try
            {
                string osVersion = GetOSVersion();
                string processor = GetProcessorInfo();
                string totalMemory = GetTotalMemory();
                string installedApps = GetInstalledApplications();
                string serialNumber = GetSystemSerialNumber();

                Debug.WriteLine($"[System Data] Collected - OS: {osVersion}, Apps: {installedApps.Length} chars");

                // First, get or create tracking ID from server
                string trackingId = GetOrCreateTrackingId();
                
                // Save to database
                bool saved = DatabaseHelper.SaveSystemInfo(
                    _activationKey,
                    _companyName,
                    _systemName,
                    _userName,
                    osVersion,
                    processor,
                    totalMemory,
                    installedApps,
                    serialNumber,
                    trackingId
                );

                Debug.WriteLine($"[System Data] Saved to DB: {saved}, TrackingID: {trackingId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[System Data Error] {ex.Message}");
            }
        }

        /// <summary>
        /// Get or create tracking ID from web server or database directly
        /// </summary>
        private string GetOrCreateTrackingId()
        {
            // Try API call first
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);  // Reduced timeout
                    
                    // Call web server API to get tracking ID
                    string apiUrl = "http://192.168.1.5:8888/api.php?action=get_or_create_tracking_id";
                    
                    var request = new System.Net.Http.HttpRequestMessage(
                        System.Net.Http.HttpMethod.Post,
                        apiUrl
                    );
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        activation_key = _activationKey,
                        user_name = _userName,
                        system_name = _systemName,
                        company_name = _companyName
                    });
                    
                    request.Content = new System.Net.Http.StringContent(
                        json,
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                    
                    var response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                        
                        if (jsonDoc.RootElement.TryGetProperty("tracking_id", out var trackingIdElement))
                        {
                            string trackingId = trackingIdElement.GetString() ?? "";
                            if (!string.IsNullOrEmpty(trackingId))
                            {
                                Debug.WriteLine($"[Tracking ID] Got from API: {trackingId}");
                                return trackingId;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tracking ID] API call failed: {ex.Message}");
            }
            
            // If API fails, try direct database connection
            try
            {
                string trackingId = GetOrCreateTrackingIdDirect();
                if (!string.IsNullOrEmpty(trackingId))
                {
                    Debug.WriteLine($"[Tracking ID] Got from DB: {trackingId}");
                    return trackingId;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tracking ID] Direct DB failed: {ex.Message}");
            }
            
            // Generate local fallback ID
            string fallbackId = $"SYSTM-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";
            Debug.WriteLine($"[Tracking ID] Using fallback: {fallbackId}");
            return fallbackId;
        }

        /// <summary>
        /// Get or create tracking ID directly from database
        /// </summary>
        private string GetOrCreateTrackingIdDirect()
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=10;CommandTimeout=20;";
                
                using (var connection = new Npgsql.NpgsqlConnection(connStr))
                {
                    connection.Open();
                    
                    // Check if tracking_id already exists
                    string selectSql = @"SELECT tracking_id FROM system_tracking 
                                        WHERE activation_key = @key AND user_name = @user AND system_name = @system
                                        LIMIT 1";
                    
                    using (var cmd = new Npgsql.NpgsqlCommand(selectSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@key", _activationKey ?? "");
                        cmd.Parameters.AddWithValue("@user", _userName ?? "");
                        cmd.Parameters.AddWithValue("@system", _systemName ?? "");
                        
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            return result.ToString() ?? "";
                        }
                    }
                    
                    // Generate new tracking ID
                    string newTrackingId = $"SYSTM-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";
                    
                    // Insert into system_tracking table
                    string insertSql = @"INSERT INTO system_tracking 
                        (activation_key, user_name, system_name, company_name, tracking_id) 
                        VALUES (@key, @user, @system, @company, @tracking_id)
                        ON CONFLICT (activation_key, user_name, system_name) 
                        DO UPDATE SET tracking_id = @tracking_id, last_seen = NOW()";
                    
                    using (var cmd = new Npgsql.NpgsqlCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@key", _activationKey ?? "");
                        cmd.Parameters.AddWithValue("@user", _userName ?? "");
                        cmd.Parameters.AddWithValue("@system", _systemName ?? "");
                        cmd.Parameters.AddWithValue("@company", _companyName ?? "");
                        cmd.Parameters.AddWithValue("@tracking_id", newTrackingId);
                        
                        cmd.ExecuteNonQuery();
                    }
                    
                    return newTrackingId;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tracking ID Direct] Error: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Get OS version
        /// </summary>
        private string GetOSVersion()
        {
            try
            {
                return Environment.OSVersion.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Get processor information
        /// </summary>
        private string GetProcessorInfo()
        {
            try
            {
                var cpuCount = Environment.ProcessorCount;
                string cpuName = "Unknown";

                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            cpuName = obj["Name"]?.ToString() ?? "Unknown";
                            break;
                        }
                    }
                }
                catch { }

                return $"{cpuName} ({cpuCount} cores)";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Get total RAM
        /// </summary>
        private string GetTotalMemory()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        long totalMemoryKB = Convert.ToInt64(obj["TotalVisibleMemorySize"]);
                        long totalMemoryGB = totalMemoryKB / (1024 * 1024);
                        return $"{totalMemoryGB} GB";
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Get list of installed applications
        /// </summary>
        private string GetInstalledApplications()
        {
            try
            {
                var apps = new List<string>();
                
                // Get from registry - 64-bit
                var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (regKey != null)
                    {
                        foreach (var subKeyName in regKey.GetSubKeyNames())
                        {
                            using (var subKey = regKey.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName")?.ToString();
                                var displayVersion = subKey?.GetValue("DisplayVersion")?.ToString();
                                
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    string version = string.IsNullOrEmpty(displayVersion) ? "" : $" ({displayVersion})";
                                    apps.Add($"{displayName}{version}");
                                }
                            }
                        }
                    }
                }

                // Get from registry - 32-bit
                registryPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (regKey != null)
                    {
                        foreach (var subKeyName in regKey.GetSubKeyNames())
                        {
                            using (var subKey = regKey.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName")?.ToString();
                                var displayVersion = subKey?.GetValue("DisplayVersion")?.ToString();
                                
                                if (!string.IsNullOrEmpty(displayName) && !apps.Contains(displayName))
                                {
                                    string version = string.IsNullOrEmpty(displayVersion) ? "" : $" ({displayVersion})";
                                    apps.Add($"{displayName}{version}");
                                }
                            }
                        }
                    }
                }

                // Sort and limit to prevent massive strings
                apps.Sort();
                if (apps.Count > 200)
                    apps = apps.GetRange(0, 200);

                return string.Join("|", apps);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[System Data] GetInstalledApplications Error: {ex.Message}");
                return "Error retrieving applications";
            }
        }

        /// <summary>
        /// Get system serial number
        /// </summary>
        private string GetSystemSerialNumber()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? "Unknown";
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
