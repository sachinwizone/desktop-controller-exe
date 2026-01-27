using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Configuration;

namespace EmployeeAttendance
{
    /// <summary>
    /// Service to collect system information and send it to the web dashboard API
    /// Runs in background and syncs data periodically
    /// </summary>
    public class SystemDataCollectionService
    {
        private static SystemDataCollectionService? _instance;
        private static readonly object _lock = new object();
        
        private Thread? _collectionThread;
        private bool _isRunning = false;
        private string _deviceId = "";
        private string _apiBaseUrl = "";
        private string _sysInfoEndpoint = "";
        private string _appsEndpoint = "";
        private HttpClient? _httpClient;
        
        // Collection intervals in seconds
        private int _systemInfoInterval = 300;
        private int _appsSyncInterval = 3600;
        
        public static SystemDataCollectionService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SystemDataCollectionService();
                    }
                }
            }
            return _instance;
        }
        
        private SystemDataCollectionService()
        {
            _httpClient = new HttpClient();
            LoadConfiguration();
            _deviceId = GenerateDeviceId();
        }
        
        /// <summary>
        /// Load configuration from App.config
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                _apiBaseUrl = ConfigurationManager.AppSettings["API_BASE_URL"] ?? "http://localhost:8888";
                _sysInfoEndpoint = ConfigurationManager.AppSettings["API_ENDPOINT_SYSTEM_INFO"] ?? "/api/system-info/sync";
                _appsEndpoint = ConfigurationManager.AppSettings["API_ENDPOINT_APPS"] ?? "/api/installed-apps/sync";
                
                if (int.TryParse(ConfigurationManager.AppSettings["SYSTEM_INFO_INTERVAL"], out int sysInterval))
                    _systemInfoInterval = sysInterval;
                
                if (int.TryParse(ConfigurationManager.AppSettings["APPS_SYNC_INTERVAL"], out int appsInterval))
                    _appsSyncInterval = appsInterval;
                
                Debug.WriteLine($"[SystemDataCollectionService] Configuration loaded: API={_apiBaseUrl}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemDataCollectionService] Error loading configuration: {ex.Message}");
                // Use defaults
                _apiBaseUrl = "http://localhost:8888";
                _sysInfoEndpoint = "/api/system-info/sync";
                _appsEndpoint = "/api/installed-apps/sync";
            }
        }
        
        /// <summary>
        /// Start the background data collection service
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _collectionThread = new Thread(CollectionWorker)
            {
                Name = "SystemDataCollectionWorker",
                IsBackground = true
            };
            _collectionThread.Start();
            
            Debug.WriteLine("[SystemDataCollectionService] Started successfully");
        }
        
        /// <summary>
        /// Stop the background data collection service
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            if (_collectionThread != null)
            {
                _collectionThread.Join(5000);
            }
            Debug.WriteLine("[SystemDataCollectionService] Stopped");
        }
        
        /// <summary>
        /// Generate unique device ID based on hardware
        /// </summary>
        private string GenerateDeviceId()
        {
            // Try to get device name from environment
            string deviceName = Environment.MachineName;
            string deviceId = $"{deviceName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            Debug.WriteLine($"[SystemDataCollectionService] Device ID generated: {deviceId}");
            return deviceId;
        }
        
        /// <summary>
        /// Background worker thread that collects and sends data
        /// </summary>
        private void CollectionWorker()
        {
            int sysInfoCounter = 0;
            int appsSyncCounter = 0;
            const int checkInterval = 10; // Check every 10 seconds
            
            Debug.WriteLine("[SystemDataCollectionService] Collection worker started");
            
            try
            {
                while (_isRunning)
                {
                    try
                    {
                        // Collect system info
                        if (sysInfoCounter >= _systemInfoInterval)
                        {
                            CollectAndSendSystemInfo();
                            sysInfoCounter = 0;
                        }
                        
                        // Sync installed apps
                        if (appsSyncCounter >= _appsSyncInterval)
                        {
                            CollectAndSendInstalledApps();
                            appsSyncCounter = 0;
                        }
                        
                        sysInfoCounter += checkInterval;
                        appsSyncCounter += checkInterval;
                        
                        // Sleep before next check
                        Thread.Sleep(checkInterval * 1000);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SystemDataCollectionService] Error in collection worker: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemDataCollectionService] Fatal error: {ex.Message}");
            }
            
            Debug.WriteLine("[SystemDataCollectionService] Collection worker stopped");
        }
        
        /// <summary>
        /// Collect system information and send to API
        /// </summary>
        private void CollectAndSendSystemInfo()
        {
            try
            {
                Debug.WriteLine("[SystemDataCollectionService] Collecting system info...");
                
                var payload = new
                {
                    device_id = _deviceId,
                    device_name = Environment.MachineName,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    data_type = "system_info",
                    status = "online",
                    os = Environment.OSVersion.VersionString,
                    processor_count = Environment.ProcessorCount,
                    uptime = GetSystemUptime()
                };
                
                SendToApi(_sysInfoEndpoint, payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemDataCollectionService] Error collecting system info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Collect installed applications and send to API
        /// </summary>
        private void CollectAndSendInstalledApps()
        {
            try
            {
                Debug.WriteLine("[SystemDataCollectionService] Collecting installed apps...");
                
                var payload = new
                {
                    device_id = _deviceId,
                    device_name = Environment.MachineName,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    data_type = "installed_apps",
                    apps_count = 0,
                    status = "online"
                };
                
                SendToApi(_appsEndpoint, payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemDataCollectionService] Error collecting installed apps: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get system uptime in seconds
        /// </summary>
        private long GetSystemUptime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return (long)uptime.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Send data to the API endpoint
        /// </summary>
        private async void SendToApi(string endpoint, object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var url = $"{_apiBaseUrl.TrimEnd('/')}{endpoint}";
                
                if (ConfigurationManager.AppSettings["LOG_API_CALLS"] == "true")
                {
                    Debug.WriteLine($"[SystemDataCollectionService] Sending to: {url}");
                }
                
                var response = await _httpClient!.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[SystemDataCollectionService] ✓ Data sent successfully to {endpoint}");
                }
                else
                {
                    Debug.WriteLine($"[SystemDataCollectionService] ✗ Failed to send data. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemDataCollectionService] Error sending to API: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if service is running
        /// </summary>
        public bool IsRunning => _isRunning;
    }
}
