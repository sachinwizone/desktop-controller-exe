using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EmployeeAttendance
{
    /// <summary>
    /// Collects comprehensive system information including hardware details, OS, storage, etc.
    /// </summary>
    public class SystemInfoCollector
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverApiUrl;
        private readonly string _activationKey;
        private readonly string _companyName;
        private readonly string _userName;
        private System.Threading.Timer? _syncTimer;
        private const int SYNC_INTERVAL_MINUTES = 5; // Sync every 5 minutes

        public SystemInfoCollector(string serverUrl, string activationKey, string companyName, string userName)
        {
            _httpClient = new HttpClient();
            _serverApiUrl = serverUrl.TrimEnd('/') + "/api";
            _activationKey = activationKey;
            _companyName = companyName;
            _userName = userName;
        }

        /// <summary>
        /// Start periodic system information collection and syncing
        /// </summary>
        public void Start()
        {
            // Initial sync immediately
            _ = SyncSystemInfo();

            // Then sync periodically
            _syncTimer = new System.Threading.Timer(async (state) =>
            {
                await SyncSystemInfo();
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
        /// Get comprehensive system information
        /// </summary>
        private SystemInfoData GatherSystemInfo()
        {
            var info = new SystemInfoData
            {
                CollectedAt = DateTime.UtcNow,
                ActivationKey = _activationKey,
                CompanyName = _companyName,
                UserName = _userName,
                ComputerName = Environment.MachineName,
                OperatingSystem = GetOSInfo(),
                ProcessorInfo = GetProcessorInfo(),
                MemoryInfo = GetMemoryInfo(),
                StorageInfo = GetStorageInfo(),
                NetworkInfo = GetNetworkInfo(),
                MotherboardInfo = GetMotherboardInfo(),
                BiosInfo = GetBiosInfo(),
                DisplayInfo = GetDisplayInfo(),
                TimeZone = TimeZoneInfo.Local.StandardName,
                SystemArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86"
            };

            return info;
        }

        /// <summary>
        /// Get OS information
        /// </summary>
        private OSInfo GetOSInfo()
        {
            try
            {
                var osInfo = new OSInfo();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        osInfo.Name = obj["Caption"]?.ToString() ?? "Unknown";
                        osInfo.Version = obj["Version"]?.ToString() ?? "Unknown";
                        osInfo.BuildNumber = obj["BuildNumber"]?.ToString() ?? "Unknown";
                        osInfo.InstallDate = obj["InstallDate"]?.ToString() ?? "Unknown";
                        osInfo.SystemDrive = obj["SystemDrive"]?.ToString() ?? "Unknown";
                        osInfo.WindowsDirectory = obj["WindowsDirectory"]?.ToString() ?? "Unknown";
                        osInfo.LastBootUpTime = obj["LastBootUpTime"]?.ToString() ?? "Unknown";
                        osInfo.SerialNumber = obj["SerialNumber"]?.ToString() ?? "Unknown";
                    }
                }

                return osInfo;
            }
            catch
            {
                return new OSInfo { Name = "Unknown", Version = "Unknown" };
            }
        }

        /// <summary>
        /// Get processor information
        /// </summary>
        private ProcessorInfo GetProcessorInfo()
        {
            try
            {
                var info = new ProcessorInfo();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        info.Name = obj["Name"]?.ToString() ?? "Unknown";
                        info.Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                        info.Cores = obj["NumberOfCores"]?.ToString() ?? "Unknown";
                        info.LogicalProcessors = obj["NumberOfLogicalProcessors"]?.ToString() ?? "Unknown";
                        info.MaxClockSpeed = obj["MaxClockSpeed"]?.ToString() ?? "Unknown";
                        info.Processor = obj["Processor"]?.ToString() ?? "Unknown";
                        info.ProcessorId = obj["ProcessorId"]?.ToString() ?? "Unknown";
                        break; // Only get first processor
                    }
                }

                return info;
            }
            catch
            {
                return new ProcessorInfo { Name = "Unknown" };
            }
        }

        /// <summary>
        /// Get memory information
        /// </summary>
        private MemoryInfo GetMemoryInfo()
        {
            try
            {
                var info = new MemoryInfo();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var capacity = obj["Capacity"]?.ToString();
                        if (long.TryParse(capacity, out long bytes))
                        {
                            info.TotalMemoryGB += bytes / (1024 * 1024 * 1024);
                        }
                        info.MemoryDevices.Add(new MemoryDevice
                        {
                            Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown",
                            PartNumber = obj["PartNumber"]?.ToString() ?? "Unknown",
                            SerialNumber = obj["SerialNumber"]?.ToString() ?? "Unknown",
                            CapacityGB = capacity != null ? (long.Parse(capacity) / (1024 * 1024 * 1024)).ToString() : "Unknown",
                            Speed = obj["Speed"]?.ToString() ?? "Unknown"
                        });
                    }
                }

                // Get available memory
                using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        if (long.TryParse(obj["FreePhysicalMemory"]?.ToString(), out long freeKb))
                        {
                            info.AvailableMemoryGB = (freeKb / (1024 * 1024)).ToString("F2");
                        }
                    }
                }

                return info;
            }
            catch
            {
                return new MemoryInfo { TotalMemoryGB = 0 };
            }
        }

        /// <summary>
        /// Get storage information
        /// </summary>
        private List<StorageInfo> GetStorageInfo()
        {
            var storageList = new List<StorageInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var storage = new StorageInfo
                        {
                            DriveLetter = obj["Name"]?.ToString() ?? "Unknown",
                            DriveType = obj["DriveType"]?.ToString() ?? "Unknown",
                            FileSystem = obj["FileSystem"]?.ToString() ?? "Unknown",
                            VolumeName = obj["VolumeName"]?.ToString() ?? "Unknown"
                        };

                        if (long.TryParse(obj["Size"]?.ToString(), out long size))
                        {
                            storage.TotalSizeGB = (size / (1024 * 1024 * 1024)).ToString();
                        }

                        if (long.TryParse(obj["FreeSpace"]?.ToString(), out long free))
                        {
                            storage.FreeSpaceGB = (free / (1024 * 1024 * 1024)).ToString("F2");
                        }

                        storageList.Add(storage);
                    }
                }

                // Get physical drive info
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var drive = new StorageInfo
                        {
                            Model = obj["Model"]?.ToString() ?? "Unknown",
                            SerialNumber = obj["SerialNumber"]?.ToString() ?? "Unknown",
                            Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown",
                            InterfaceType = obj["InterfaceType"]?.ToString() ?? "Unknown"
                        };

                        if (long.TryParse(obj["Size"]?.ToString(), out long size))
                        {
                            drive.TotalSizeGB = (size / (1024 * 1024 * 1024)).ToString();
                        }

                        storageList.Add(drive);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return storageList;
        }

        /// <summary>
        /// Get network information
        /// </summary>
        private List<NetworkAdapter> GetNetworkInfo()
        {
            var adapters = new List<NetworkAdapter>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionStatus=2"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        adapters.Add(new NetworkAdapter
                        {
                            Name = obj["Name"]?.ToString() ?? "Unknown",
                            MACAddress = obj["MACAddress"]?.ToString() ?? "Unknown",
                            Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown",
                            NetConnectionID = obj["NetConnectionID"]?.ToString() ?? "Unknown",
                            Speed = obj["Speed"]?.ToString() ?? "Unknown"
                        });
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return adapters;
        }

        /// <summary>
        /// Get motherboard information
        /// </summary>
        private MotherboardInfo GetMotherboardInfo()
        {
            try
            {
                var info = new MotherboardInfo();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        info.Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                        info.Product = obj["Product"]?.ToString() ?? "Unknown";
                        info.SerialNumber = obj["SerialNumber"]?.ToString() ?? "Unknown";
                        info.Version = obj["Version"]?.ToString() ?? "Unknown";
                        break;
                    }
                }

                return info;
            }
            catch
            {
                return new MotherboardInfo { Manufacturer = "Unknown" };
            }
        }

        /// <summary>
        /// Get BIOS information
        /// </summary>
        private BiosInfo GetBiosInfo()
        {
            try
            {
                var info = new BiosInfo();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        info.Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                        info.Version = obj["Version"]?.ToString() ?? "Unknown";
                        info.ReleaseDate = obj["ReleaseDate"]?.ToString() ?? "Unknown";
                        info.SerialNumber = obj["SerialNumber"]?.ToString() ?? "Unknown";
                        break;
                    }
                }

                return info;
            }
            catch
            {
                return new BiosInfo { Manufacturer = "Unknown" };
            }
        }

        /// <summary>
        /// Get display/GPU information
        /// </summary>
        private List<DisplayAdapter> GetDisplayInfo()
        {
            var displays = new List<DisplayAdapter>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        displays.Add(new DisplayAdapter
                        {
                            Name = obj["Name"]?.ToString() ?? "Unknown",
                            Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown",
                            DriverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown",
                            VideoMemory = obj["AdapterRAM"]?.ToString() ?? "Unknown"
                        });
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return displays;
        }

        /// <summary>
        /// Sync system information to server
        /// </summary>
        private async Task SyncSystemInfo()
        {
            try
            {
                var systemInfo = GatherSystemInfo();
                var json = JsonSerializer.Serialize(systemInfo);

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_serverApiUrl}/system-info/sync", content);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to sync system info: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing system info: {ex.Message}");
            }
        }
    }

    // ==================== Data Models ====================

    public class SystemInfoData
    {
        public DateTime CollectedAt { get; set; }
        public string ActivationKey { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ComputerName { get; set; } = "";
        public OSInfo OperatingSystem { get; set; } = new();
        public ProcessorInfo ProcessorInfo { get; set; } = new();
        public MemoryInfo MemoryInfo { get; set; } = new();
        public List<StorageInfo> StorageInfo { get; set; } = new();
        public List<NetworkAdapter> NetworkInfo { get; set; } = new();
        public MotherboardInfo MotherboardInfo { get; set; } = new();
        public BiosInfo BiosInfo { get; set; } = new();
        public List<DisplayAdapter> DisplayInfo { get; set; } = new();
        public string TimeZone { get; set; } = "";
        public string SystemArchitecture { get; set; } = "";
    }

    public class OSInfo
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string BuildNumber { get; set; } = "";
        public string InstallDate { get; set; } = "";
        public string SystemDrive { get; set; } = "";
        public string WindowsDirectory { get; set; } = "";
        public string LastBootUpTime { get; set; } = "";
        public string SerialNumber { get; set; } = "";
    }

    public class ProcessorInfo
    {
        public string Name { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Cores { get; set; } = "";
        public string LogicalProcessors { get; set; } = "";
        public string MaxClockSpeed { get; set; } = "";
        public string Processor { get; set; } = "";
        public string ProcessorId { get; set; } = "";
    }

    public class MemoryInfo
    {
        public long TotalMemoryGB { get; set; }
        public string AvailableMemoryGB { get; set; } = "";
        public List<MemoryDevice> MemoryDevices { get; set; } = new();
    }

    public class MemoryDevice
    {
        public string Manufacturer { get; set; } = "";
        public string PartNumber { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string CapacityGB { get; set; } = "";
        public string Speed { get; set; } = "";
    }

    public class StorageInfo
    {
        public string DriveLetter { get; set; } = "";
        public string DriveType { get; set; } = "";
        public string FileSystem { get; set; } = "";
        public string VolumeName { get; set; } = "";
        public string TotalSizeGB { get; set; } = "";
        public string FreeSpaceGB { get; set; } = "";
        public string Model { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string InterfaceType { get; set; } = "";
    }

    public class NetworkAdapter
    {
        public string Name { get; set; } = "";
        public string MACAddress { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string NetConnectionID { get; set; } = "";
        public string Speed { get; set; } = "";
    }

    public class MotherboardInfo
    {
        public string Manufacturer { get; set; } = "";
        public string Product { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Version { get; set; } = "";
    }

    public class BiosInfo
    {
        public string Manufacturer { get; set; } = "";
        public string Version { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string SerialNumber { get; set; } = "";
    }

    public class DisplayAdapter
    {
        public string Name { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string DriverVersion { get; set; } = "";
        public string VideoMemory { get; set; } = "";
    }
}
