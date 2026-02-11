using System;
using System.Collections.Generic;
using System.Data;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Npgsql;

namespace EmployeeAttendance
{
    /// <summary>
    /// IST Timezone Helper
    /// </summary>
    public static class IstHelper
    {
        private static readonly TimeZoneInfo IstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        
        /// <summary>
        /// Get current time in IST (India Standard Time)
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstTimeZone);
    }

    /// <summary>
    /// Web log entry for batch insert
    /// </summary>
    public class WebLogEntry
    {
        public string Browser { get; set; } = "";
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string SearchQuery { get; set; } = "";  // Search query if applicable
        public string Domain { get; set; } = "";       // Domain extracted from URL
        public DateTime VisitTime { get; set; }
        public bool IsSynced { get; set; } = false;
    }
    
    /// <summary>
    /// App usage log entry
    /// </summary>
    public class AppUsageLog
    {
        public string AppName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSynced { get; set; } = false;
    }
    
    /// <summary>
    /// Inactivity log entry
    /// </summary>
    public class InactivityLog
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsSynced { get; set; } = false;
    }

    public static class DatabaseHelper
    {
        // Database connection settings
        private const string Host = "72.61.235.203";
        private const int Port = 9095;
        private const string Database = "controller";
        private const string Username = "controller_dbuser";
        private const string Password = "hwrw*&^hdg2gsGDGJHAU&838373h";
        
        // Local storage for activation
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EmployeeAttendance");
        private static readonly string ActivationFile = Path.Combine(AppDataPath, "activation.dat");
        
        private static string GetConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Timeout=30;CommandTimeout=60;";
        }
        
        public static bool TestConnection()
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
        public static bool IsActivated()
        {
            try
            {
                if (!File.Exists(ActivationFile)) return false;
                
                string content = File.ReadAllText(ActivationFile);
                var parts = content.Split('|');
                return parts.Length >= 3;
            }
            catch
            {
                return false;
            }
        }
        
        public static (string activationKey, string username, string department, string companyName, string displayName)? GetStoredActivation()
        {
            try
            {
                if (!File.Exists(ActivationFile)) return null;
                
                string content = File.ReadAllText(ActivationFile);
                var parts = content.Split('|');
                if (parts.Length >= 5)
                {
                    return (parts[0], parts[1], parts[2], parts[3], parts[4]);
                }
                else if (parts.Length >= 4)
                {
                    // Legacy support - no display name
                    return (parts[0], parts[1], parts[2], parts[3], parts[1]);
                }
            }
            catch { }
            return null;
        }
        
        public static bool SaveActivation(string activationKey, string username, string department, string companyName, string displayName)
        {
            try
            {
                // Generate unique System ID for this machine
                string systemId = GetOrCreateSystemId();
                
                // Save locally
                Directory.CreateDirectory(AppDataPath);
                File.WriteAllText(ActivationFile, $"{activationKey}|{username}|{department}|{companyName}|{displayName}");
                
                // Register system in database directly
                RegisterSystemInDatabase(companyName, username, displayName, department, systemId);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Register or update system in connected_systems table (direct database)
        /// </summary>
        public static void RegisterSystemInDatabase(string companyName, string employeeId, string displayName, string department, string systemId)
        {
            try
            {
                string machineId = GetMachineId();
                string machineName = Environment.MachineName;
                string ipAddress = GetLocalIPAddress();
                string osVersion = Environment.OSVersion.ToString();
                
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        INSERT INTO connected_systems 
                            (company_name, employee_id, display_name, department, machine_id, machine_name, 
                             ip_address, os_version, app_version, system_id, first_connected, last_heartbeat, is_online, status)
                        VALUES 
                            (@company, @empId, @name, @dept, @machineId, @machineName, 
                             @ip, @os, '1.0', @systemId, NOW(), NOW(), true, 'working')
                        ON CONFLICT (company_name, employee_id, machine_id) 
                        DO UPDATE SET 
                            display_name = @name,
                            department = @dept,
                            ip_address = @ip,
                            os_version = @os,
                            system_id = @systemId,
                            last_heartbeat = NOW(),
                            is_online = true,
                            status = 'working'";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        cmd.Parameters.AddWithValue("@empId", employeeId);
                        cmd.Parameters.AddWithValue("@name", displayName);
                        cmd.Parameters.AddWithValue("@dept", department);
                        cmd.Parameters.AddWithValue("@machineId", machineId);
                        cmd.Parameters.AddWithValue("@machineName", machineName);
                        cmd.Parameters.AddWithValue("@ip", ipAddress);
                        cmd.Parameters.AddWithValue("@os", osVersion);
                        cmd.Parameters.AddWithValue("@systemId", systemId);
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail activation
                System.Diagnostics.Debug.WriteLine("Register system error: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Send heartbeat directly to database (no API needed)
        /// </summary>
        public static void SendHeartbeatToDatabase(string companyName, string employeeId, string systemId, string status = "working")
        {
            try
            {
                string machineId = GetMachineId();
                string ipAddress = GetLocalIPAddress();

                // Get live CPU and RAM usage
                float cpuUsage = 0;
                float ramUsage = 0;
                try
                {
                    var cpuInfo = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuInfo.NextValue(); // First call always returns 0
                    System.Threading.Thread.Sleep(200);
                    cpuUsage = cpuInfo.NextValue();
                    cpuInfo.Dispose();
                }
                catch { }
                try
                {
                    var memInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                    ulong totalMem = memInfo.TotalPhysicalMemory;
                    ulong availMem = memInfo.AvailablePhysicalMemory;
                    if (totalMem > 0) ramUsage = ((float)(totalMem - availMem) / totalMem) * 100f;
                }
                catch { }

                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();

                    // Ensure columns exist
                    try { using (var c = new NpgsqlCommand("ALTER TABLE connected_systems ADD COLUMN IF NOT EXISTS cpu_usage REAL DEFAULT 0", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE connected_systems ADD COLUMN IF NOT EXISTS ram_usage REAL DEFAULT 0", connection)) c.ExecuteNonQuery(); } catch {}

                    string sql = @"
                        UPDATE connected_systems
                        SET last_heartbeat = NOW(),
                            is_online = true,
                            status = @status,
                            ip_address = @ip,
                            cpu_usage = @cpu,
                            ram_usage = @ram
                        WHERE company_name = @company
                          AND employee_id = @empId
                          AND machine_id = @machineId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        cmd.Parameters.AddWithValue("@empId", employeeId);
                        cmd.Parameters.AddWithValue("@machineId", machineId);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@ip", ipAddress);
                        cmd.Parameters.AddWithValue("@cpu", cpuUsage);
                        cmd.Parameters.AddWithValue("@ram", ramUsage);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Mark system as offline in database
        /// </summary>
        public static void SetSystemOffline(string companyName, string employeeId)
        {
            try
            {
                string machineId = GetMachineId();
                
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        UPDATE connected_systems 
                        SET is_online = false, last_heartbeat = NOW()
                        WHERE company_name = @company 
                          AND employee_id = @empId 
                          AND machine_id = @machineId";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        cmd.Parameters.AddWithValue("@empId", employeeId);
                        cmd.Parameters.AddWithValue("@machineId", machineId);
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Verify company password for logout functionality
        /// </summary>
        public static bool VerifyCompanyPassword(string companyName, string password)
        {
            try
            {
                // Hash the password using SHA256 (same as stored)
                string passwordHash = ComputeSha256Hash(password);
                
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"SELECT password_hash FROM company_passwords WHERE LOWER(company_name) = LOWER(@company)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        
                        object? result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            string storedHash = result.ToString() ?? "";
                            return storedHash.Equals(passwordHash, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get the company password from database (for verification)
        /// </summary>
        public static string GetCompanyPassword(string companyName)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"SELECT password FROM company_passwords WHERE LOWER(company_name) = LOWER(@company) LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        
                        object? result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            return result.ToString() ?? "";
                        }
                    }
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>
        /// Compute SHA256 hash of a string
        /// </summary>
        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        /// <summary>
        /// Delete activation data for logout
        /// </summary>
        public static bool DeleteActivation()
        {
            try
            {
                if (File.Exists(ActivationFile))
                {
                    File.Delete(ActivationFile);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public static (bool success, string companyName, string error) VerifyActivationKey(string activationKey)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"SELECT company_name, is_active FROM activation_keys WHERE activation_key = @key";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@key", activationKey);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string companyName = reader.GetString(0);
                                bool isActive = reader.GetBoolean(1);
                                
                                if (!isActive)
                                {
                                    return (false, "", "Activation key is deactivated");
                                }
                                
                                return (true, companyName, "");
                            }
                        }
                    }
                }
                return (false, "", "Invalid activation key");
            }
            catch (Exception ex)
            {
                return (false, "", $"Connection error: {ex.Message}");
            }
        }
        
        public static List<string> GetDepartments(string activationKey)
        {
            var departments = new List<string>();
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // First get company name from activation key
                    string companyName = "";
                    string getCompanySql = @"SELECT company_name FROM activation_keys WHERE activation_key = @key";
                    using (var cmd = new NpgsqlCommand(getCompanySql, connection))
                    {
                        cmd.Parameters.AddWithValue("@key", activationKey);
                        companyName = cmd.ExecuteScalar()?.ToString() ?? "";
                    }
                    
                    // Get departments from company_departments table
                    if (!string.IsNullOrEmpty(companyName))
                    {
                        string sql = @"SELECT department_name FROM company_departments 
                                      WHERE LOWER(company_name) = LOWER(@company) AND is_active = true 
                                      ORDER BY department_name";
                        
                        using (var cmd = new NpgsqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@company", companyName);
                            
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        departments.Add(reader.GetString(0));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            
            // Default departments if none found
            if (departments.Count == 0)
            {
                departments.AddRange(new[] { "IT", "HR", "Finance", "Operations", "Sales", "Marketing", "Support" });
            }
            
            return departments;
        }
        
        /// <summary>
        /// Validate employee by name from company_employees table
        /// Returns employee details if found
        /// </summary>
        public static (bool found, string employeeId, string department, string designation, string fullName) ValidateEmployee(string companyName, string employeeName)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Search for employee by name (case-insensitive partial match)
                    string sql = @"SELECT employee_id, department, designation, full_name 
                                   FROM company_employees 
                                   WHERE LOWER(company_name) = LOWER(@company) 
                                   AND LOWER(full_name) LIKE LOWER(@name)
                                   AND is_active = true
                                   LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        cmd.Parameters.AddWithValue("@name", "%" + employeeName.Trim() + "%");
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string empId = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                string dept = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string desig = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                string fullName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                
                                return (true, empId, dept, desig, fullName);
                            }
                        }
                    }
                }
            }
            catch { }
            
            return (false, "", "", "", "");
        }
        
        /// <summary>
        /// Auto-register a new employee when they login for the first time
        /// Generates employee ID and adds to company_employees
        /// </summary>
        public static (bool success, string employeeId, string error) AutoRegisterEmployee(string companyName, string fullName, string department)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Generate unique employee ID (emp + random 4 digits)
                    string employeeId = "emp" + new Random().Next(1000, 9999).ToString();
                    
                    // Check if ID already exists and regenerate if needed
                    for (int i = 0; i < 10; i++)
                    {
                        string checkSql = "SELECT COUNT(*) FROM company_employees WHERE employee_id = @empId AND company_name = @company";
                        using (var checkCmd = new NpgsqlCommand(checkSql, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@empId", employeeId);
                            checkCmd.Parameters.AddWithValue("@company", companyName);
                            long count = (long)(checkCmd.ExecuteScalar() ?? 0);
                            if (count == 0) break;
                            employeeId = "emp" + new Random().Next(1000, 9999).ToString();
                        }
                    }
                    
                    // Insert new employee
                    string sql = @"INSERT INTO company_employees 
                                   (company_name, employee_id, full_name, department, designation, is_active, created_at, updated_at)
                                   VALUES (@company, @empId, @name, @dept, 'Employee', true, NOW(), NOW())
                                   ON CONFLICT (company_name, employee_id) DO NOTHING";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        cmd.Parameters.AddWithValue("@empId", employeeId);
                        cmd.Parameters.AddWithValue("@name", fullName);
                        cmd.Parameters.AddWithValue("@dept", string.IsNullOrEmpty(department) ? "General" : department);
                        
                        cmd.ExecuteNonQuery();
                    }
                    
                    return (true, employeeId, "");
                }
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }
        
        /// <summary>
        /// Get all employees for a company
        /// </summary>
        public static List<(string fullName, string employeeId, string department)> GetCompanyEmployees(string companyName)
        {
            var employees = new List<(string, string, string)>();
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"SELECT full_name, employee_id, department 
                                   FROM company_employees 
                                   WHERE LOWER(company_name) = LOWER(@company) 
                                   AND is_active = true
                                   ORDER BY full_name";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                string empId = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string dept = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                employees.Add((name, empId, dept));
                            }
                        }
                    }
                }
            }
            catch { }
            
            return employees;
        }
        
        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
        
        public static string GetMachineId()
        {
            try
            {
                string machineInfo = Environment.MachineName + Environment.UserName + Environment.ProcessorCount;
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(machineInfo));
                    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
                }
            }
            catch
            {
                return "UNKNOWN";
            }
        }
        
        // ============= PUNCH LOG METHODS =============
        
        public static int? GetActivePunchSessionId(string username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"SELECT id FROM punch_log_consolidated 
                                   WHERE username = @username AND punch_out_time IS NULL 
                                   ORDER BY id DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        
        public static bool StartPunchSession(string activationKey, string companyName, string username, string department, string displayName = "")
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Check if already punched in
                    if (GetActivePunchSessionId(username) != null)
                    {
                        return false;
                    }
                    
                    // Use displayName if provided, otherwise fall back to username
                    string nameToStore = !string.IsNullOrEmpty(displayName) ? displayName : username;
                    
                    string sql = @"INSERT INTO punch_log_consolidated 
                        (activation_key, company_name, username, display_name, system_name, ip_address, machine_id, punch_in_time)
                        VALUES (@activation_key, @company_name, @username, @display_name, @system_name, @ip_address, @machine_id, @punch_in_time)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@display_name", nameToStore);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@punch_in_time", IstHelper.Now);
                        
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("StartPunchSession", ex.Message);
                return false;
            }
        }
        
        public static bool EndPunchSession(string username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    var sessionId = GetActivePunchSessionId(username);
                    if (sessionId == null) return false;
                    
                    string sql = @"UPDATE punch_log_consolidated 
                        SET punch_out_time = @punch_out_time,
                            total_work_duration_seconds = EXTRACT(EPOCH FROM (@punch_out_time - punch_in_time))::INTEGER,
                            updated_at = @updated_at
                        WHERE id = @id";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", sessionId.Value);
                        cmd.Parameters.AddWithValue("@punch_out_time", IstHelper.Now);
                        cmd.Parameters.AddWithValue("@updated_at", IstHelper.Now);
                        
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("EndPunchSession", ex.Message);
                return false;
            }
        }
        
        public static bool StartBreak(string username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    var sessionId = GetActivePunchSessionId(username);
                    if (sessionId == null) return false;
                    
                    string sql = @"UPDATE punch_log_consolidated 
                        SET break_start_time = @break_start_time,
                            updated_at = @updated_at
                        WHERE id = @id";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", sessionId.Value);
                        cmd.Parameters.AddWithValue("@break_start_time", IstHelper.Now);
                        cmd.Parameters.AddWithValue("@updated_at", IstHelper.Now);
                        
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("StartBreak", ex.Message);
                return false;
            }
        }
        
        public static bool EndBreak(string username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    var sessionId = GetActivePunchSessionId(username);
                    if (sessionId == null) return false;
                    
                    string sql = @"UPDATE punch_log_consolidated 
                        SET break_end_time = @break_end_time,
                            break_duration_seconds = COALESCE(break_duration_seconds, 0) + EXTRACT(EPOCH FROM (@break_end_time - break_start_time))::INTEGER,
                            break_start_time = NULL,
                            updated_at = @updated_at
                        WHERE id = @id";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", sessionId.Value);
                        cmd.Parameters.AddWithValue("@break_end_time", IstHelper.Now);
                        cmd.Parameters.AddWithValue("@updated_at", IstHelper.Now);
                        
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("EndBreak", ex.Message);
                return false;
            }
        }
        
        public static (DateTime? punchIn, DateTime? breakStart, int totalBreakSeconds, int workSeconds) GetCurrentSessionInfo(string username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"SELECT punch_in_time, break_start_time, 
                                   COALESCE(break_duration_seconds, 0) as break_secs,
                                   COALESCE(total_work_duration_seconds, 0) as work_secs
                                   FROM punch_log_consolidated 
                                   WHERE username = @username AND punch_out_time IS NULL 
                                   ORDER BY id DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                DateTime? punchIn = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                                DateTime? breakStart = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                                int breakSecs = reader.GetInt32(2);
                                int workSecs = reader.GetInt32(3);
                                
                                return (punchIn, breakStart, breakSecs, workSecs);
                            }
                        }
                    }
                }
            }
            catch { }
            return (null, null, 0, 0);
        }
        
        /// <summary>
        /// Get activity details for a specific date
        /// </summary>
        public class ActivityDetail
        {
            public DateTime? PunchInTime { get; set; }
            public DateTime? PunchOutTime { get; set; }
            public DateTime? BreakStartTime { get; set; }
            public DateTime? BreakEndTime { get; set; }
            public int BreakDurationSeconds { get; set; }
            public int TotalWorkDurationSeconds { get; set; }
            
            public string FormattedPunchIn => PunchInTime?.ToString("hh:mm tt") ?? "N/A";
            public string FormattedPunchOut => PunchOutTime?.ToString("hh:mm tt") ?? "N/A";
            public string FormattedBreakStart => BreakStartTime?.ToString("hh:mm tt") ?? "N/A";
            public string FormattedBreakEnd => BreakEndTime?.ToString("hh:mm tt") ?? "N/A";
            
            public string FormattedBreakDuration
            {
                get
                {
                    if (BreakDurationSeconds <= 0) return "0h 0m";
                    int hours = BreakDurationSeconds / 3600;
                    int minutes = (BreakDurationSeconds % 3600) / 60;
                    return $"{hours}h {minutes}m";
                }
            }
            
            public string FormattedWorkDuration
            {
                get
                {
                    if (TotalWorkDurationSeconds <= 0) return "0h 0m";
                    int hours = TotalWorkDurationSeconds / 3600;
                    int minutes = (TotalWorkDurationSeconds % 3600) / 60;
                    return $"{hours}h {minutes}m";
                }
            }
            
            /// <summary>
            /// Calculate actual duration from punch in to punch out
            /// </summary>
            public int GetTotalDurationSeconds()
            {
                if (!PunchInTime.HasValue || !PunchOutTime.HasValue)
                    return 0;
                
                var duration = PunchOutTime.Value - PunchInTime.Value;
                return (int)duration.TotalSeconds;
            }
            
            /// <summary>
            /// Calculate net work hours (total duration - break)
            /// </summary>
            public int GetNetWorkSeconds()
            {
                int totalDuration = GetTotalDurationSeconds();
                int netWork = totalDuration - BreakDurationSeconds;
                return netWork > 0 ? netWork : 0;
            }
            
            /// <summary>
            /// Calculate inactivity based on standard 8-hour workday
            /// </summary>
            public int GetInactivitySeconds()
            {
                const int standardWorkSeconds = 28800; // 8 hours
                int netWork = GetNetWorkSeconds();
                int inactivity = standardWorkSeconds - netWork;
                return inactivity > 0 ? inactivity : 0;
            }
            
            public string FormattedTotalDuration
            {
                get
                {
                    int seconds = GetTotalDurationSeconds();
                    if (seconds <= 0) return "0h 0m";
                    int hours = seconds / 3600;
                    int minutes = (seconds % 3600) / 60;
                    return $"{hours}h {minutes}m";
                }
            }
            
            public string FormattedNetWork
            {
                get
                {
                    int seconds = GetNetWorkSeconds();
                    if (seconds <= 0) return "0h 0m";
                    int hours = seconds / 3600;
                    int minutes = (seconds % 3600) / 60;
                    return $"{hours}h {minutes}m";
                }
            }
            
            public string FormattedInactivity
            {
                get
                {
                    int seconds = GetInactivitySeconds();
                    if (seconds <= 0) return "0h 0m";
                    int hours = seconds / 3600;
                    int minutes = (seconds % 3600) / 60;
                    return $"{hours}h {minutes}m";
                }
            }
            
            public bool HasActivity => PunchInTime.HasValue;
            
            private static string FormatDuration(int seconds)
            {
                if (seconds <= 0) return "0h 0m";
                int hours = seconds / 3600;
                int minutes = (seconds % 3600) / 60;
                return $"{hours}h {minutes}m";
            }
        }
        
        public static ActivityDetail GetActivityForDate(string username, DateTime date)
        {
            var detail = new ActivityDetail();
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Get ALL activity for the specified date (sum multiple punch sessions)
                    string sql = @"SELECT 
                                   MIN(punch_in_time) as first_punch_in,
                                   MAX(punch_out_time) as last_punch_out,
                                   MIN(break_start_time) as first_break_start,
                                   MAX(break_end_time) as last_break_end,
                                   COALESCE(SUM(break_duration_seconds), 0) as total_break_seconds,
                                   COALESCE(SUM(total_work_duration_seconds), 0) as total_work_seconds
                                   FROM punch_log_consolidated 
                                   WHERE username = @username 
                                   AND DATE(punch_in_time) = @date";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                detail.PunchInTime = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                                detail.PunchOutTime = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                                detail.BreakStartTime = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                                detail.BreakEndTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                                detail.BreakDurationSeconds = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                                detail.TotalWorkDurationSeconds = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                            }
                        }
                    }
                }
            }
            catch { }
            
            return detail;
        }
        
        public static List<ActivityItem> GetTodayActivities(string username)
        {
            var activities = new List<ActivityItem>();
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Get ALL today's sessions (multiple punch in/out pairs)
                    string sql = @"SELECT punch_in_time, punch_out_time, break_start_time, break_end_time
                                   FROM punch_log_consolidated 
                                   WHERE username = @username 
                                   AND DATE(punch_in_time) = CURRENT_DATE
                                   ORDER BY id ASC";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Add punch in time
                                if (!reader.IsDBNull(0))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(0),
                                        Type = "Punch In",
                                        Color = Color.FromArgb(16, 185, 129) // Green
                                    });
                                }
                                
                                // Add break start time
                                if (!reader.IsDBNull(2))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(2),
                                        Type = "Break Start",
                                        Color = Color.FromArgb(245, 158, 11) // Amber
                                    });
                                }
                                
                                // Add break end time
                                if (!reader.IsDBNull(3))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(3),
                                        Type = "Break Stop",
                                        Color = Color.FromArgb(37, 99, 235) // Blue
                                    });
                                }
                                
                                // Add punch out time
                                if (!reader.IsDBNull(1))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(1),
                                        Type = "Punch Out",
                                        Color = Color.FromArgb(239, 68, 68) // Red
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            
            return activities.OrderBy(a => a.Time).ToList();
        }
        
        private static void LogError(string method, string error)
        {
            try
            {
                Directory.CreateDirectory(AppDataPath);
                string logPath = Path.Combine(AppDataPath, "error.log");
                File.AppendAllText(logPath, $"{IstHelper.Now:yyyy-MM-dd HH:mm:ss} - {method}: {error}\n");
            }
            catch { }
        }
        
        // ============= AUDIT LOG METHODS =============
        
        /// <summary>
        /// Ensure all audit tables exist
        /// </summary>
        public static bool EnsureAuditTablesExist()
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Create screenshot_logs table
                    string createScreenshotLogsSql = @"
                        CREATE TABLE IF NOT EXISTS screenshot_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            machine_id VARCHAR(100),
                            screenshot_data TEXT,
                            screen_width INTEGER,
                            screen_height INTEGER,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_screenshot_logs_timestamp ON screenshot_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_screenshot_logs_activation_key ON screenshot_logs(activation_key);
                    ";
                    using (var cmd = new NpgsqlCommand(createScreenshotLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError("EnsureAuditTablesExist", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Insert web browsing log
        /// </summary>
        public static bool InsertWebLog(string activationKey, string companyName, string username, string displayName,
            string browserName, string url, string title, string category, DateTime visitTime)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        INSERT INTO web_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                            machine_id, browser_name, website_url, page_title, category, visit_time)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                            @machine_id, @browser_name, @website_url, @page_title, @category, @visit_time)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@display_user_name", displayName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@browser_name", browserName);
                        cmd.Parameters.AddWithValue("@website_url", url);
                        cmd.Parameters.AddWithValue("@page_title", title);
                        cmd.Parameters.AddWithValue("@category", category);
                        cmd.Parameters.AddWithValue("@visit_time", visitTime);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertWebLog", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Batch insert web logs
        /// </summary>
        public static int InsertWebLogsBatch(string activationKey, string companyName, string username, string displayName,
            List<WebLogEntry> entries)
        {
            if (entries == null || entries.Count == 0) return 0;
            int inserted = 0;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var entry in entries)
                        {
                            try
                            {
                                string sql = @"
                                    INSERT INTO web_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                                        machine_id, browser_name, website_url, page_title, category, search_query, domain, visit_time)
                                    VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                                        @machine_id, @browser_name, @website_url, @page_title, @category, @search_query, @domain, @visit_time)";
                                
                                using (var cmd = new NpgsqlCommand(sql, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@activation_key", activationKey);
                                    cmd.Parameters.AddWithValue("@company_name", companyName);
                                    cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                                    cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                                    cmd.Parameters.AddWithValue("@username", username);
                                    cmd.Parameters.AddWithValue("@display_user_name", displayName);
                                    cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                                    cmd.Parameters.AddWithValue("@browser_name", entry.Browser);
                                    cmd.Parameters.AddWithValue("@website_url", entry.Url);
                                    cmd.Parameters.AddWithValue("@page_title", entry.Title);
                                    cmd.Parameters.AddWithValue("@category", entry.Category);
                                    cmd.Parameters.AddWithValue("@search_query", entry.SearchQuery ?? "");
                                    cmd.Parameters.AddWithValue("@domain", entry.Domain ?? "");
                                    cmd.Parameters.AddWithValue("@visit_time", entry.VisitTime);
                                    cmd.ExecuteNonQuery();
                                    inserted++;
                                }
                            }
                            catch { }
                        }
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertWebLogsBatch", ex.Message);
            }
            return inserted;
        }
        
        /// <summary>
        /// Insert application usage log
        /// </summary>
        public static bool InsertAppLog(string activationKey, string companyName, string username, string displayName,
            string appName, string windowTitle, DateTime startTime, DateTime endTime)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    int duration = (int)(endTime - startTime).TotalSeconds;
                    string sql = @"
                        INSERT INTO application_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                            machine_id, app_name, window_title, start_time, end_time, duration_seconds)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                            @machine_id, @app_name, @window_title, @start_time, @end_time, @duration_seconds)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@display_user_name", displayName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@app_name", appName);
                        cmd.Parameters.AddWithValue("@window_title", windowTitle);
                        cmd.Parameters.AddWithValue("@start_time", startTime);
                        cmd.Parameters.AddWithValue("@end_time", endTime);
                        cmd.Parameters.AddWithValue("@duration_seconds", duration);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertAppLog", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Insert USB file transfer log
        /// </summary>
        public static bool InsertUSBFileLog(string activationKey, string companyName, string username, string displayName,
            string fileName, string filePath, long fileSize, string fileExtension, string transferType,
            string sourcePath, string destinationPath, string driveLetter, string driveLabel)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        INSERT INTO usb_file_transfer_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                            machine_id, file_name, file_path, file_size, file_extension, transfer_type,
                            source_path, destination_path, drive_letter, drive_label, transfer_time)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                            @machine_id, @file_name, @file_path, @file_size, @file_extension, @transfer_type,
                            @source_path, @destination_path, @drive_letter, @drive_label, @transfer_time)";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey ?? "");
                        cmd.Parameters.AddWithValue("@company_name", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", username ?? "");
                        cmd.Parameters.AddWithValue("@display_user_name", displayName ?? "");
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@file_name", fileName ?? "");
                        cmd.Parameters.AddWithValue("@file_path", filePath ?? "");
                        cmd.Parameters.AddWithValue("@file_size", fileSize);
                        cmd.Parameters.AddWithValue("@file_extension", fileExtension ?? "");
                        cmd.Parameters.AddWithValue("@transfer_type", transferType ?? "COPY");
                        cmd.Parameters.AddWithValue("@source_path", sourcePath ?? "");
                        cmd.Parameters.AddWithValue("@destination_path", destinationPath ?? "");
                        cmd.Parameters.AddWithValue("@drive_letter", driveLetter ?? "");
                        cmd.Parameters.AddWithValue("@drive_label", driveLabel ?? "");
                        // Use IST timezone
                        var istOffset = TimeSpan.FromHours(5.5);
                        var istNow = DateTimeOffset.UtcNow.ToOffset(istOffset).DateTime;
                        cmd.Parameters.AddWithValue("@transfer_time", istNow);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertUSBFileLog", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Insert inactivity log
        /// </summary>
        public static bool InsertInactivityLog(string activationKey, string companyName, string username, string displayName,
            DateTime startTime, DateTime endTime, string status)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    int duration = (int)(endTime - startTime).TotalSeconds;
                    string sql = @"
                        INSERT INTO inactivity_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                            machine_id, start_time, end_time, duration_seconds, status)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                            @machine_id, @start_time, @end_time, @duration_seconds, @status)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@display_user_name", displayName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@start_time", startTime);
                        cmd.Parameters.AddWithValue("@end_time", endTime);
                        cmd.Parameters.AddWithValue("@duration_seconds", duration);
                        cmd.Parameters.AddWithValue("@status", status);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertInactivityLog", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Insert screenshot log
        /// </summary>
        public static bool InsertScreenshot(string activationKey, string companyName, string username, string displayName,
            string base64Data, int width, int height)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        INSERT INTO screenshot_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                            machine_id, screenshot_data, screen_width, screen_height)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                            @machine_id, @screenshot_data, @screen_width, @screen_height)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@display_user_name", displayName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@screenshot_data", base64Data);
                        cmd.Parameters.AddWithValue("@screen_width", width);
                        cmd.Parameters.AddWithValue("@screen_height", height);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertScreenshot", ex.Message);
                return false;
            }
        }
        
        // ============= REMOTE VIEWING / LIVE STREAM METHODS =============
        
        /// <summary>
        /// Get or create a unique System ID for remote viewing (like AnyDesk ID format: XXX-XXX-XXX)
        /// </summary>
        public static string GetOrCreateSystemId()
        {
            const string RegistryPath = @"SOFTWARE\EmployeeAttendance";
            const string SystemIdKeyName = "SystemId";
            
            try
            {
                // Try to read existing ID
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        string? existingId = key.GetValue(SystemIdKeyName)?.ToString();
                        if (!string.IsNullOrEmpty(existingId)) return existingId;
                    }
                }
                
                // Generate new ID based on machine hardware
                string machineId = GetMachineId();
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId + Environment.MachineName));
                    int id1 = (Math.Abs(BitConverter.ToInt32(hashBytes, 0)) % 900) + 100;
                    int id2 = (Math.Abs(BitConverter.ToInt32(hashBytes, 4)) % 900) + 100;
                    int id3 = (Math.Abs(BitConverter.ToInt32(hashBytes, 8)) % 900) + 100;
                    
                    string systemId = $"{id1}-{id2}-{id3}";
                    
                    // Save to registry
                    using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                    {
                        key?.SetValue(SystemIdKeyName, systemId);
                    }
                    
                    return systemId;
                }
            }
            catch
            {
                return "000-000-000";
            }
        }
        
        /// <summary>
        /// Register system for remote viewing
        /// </summary>
        public static string RegisterSystem(string activationKey, string companyName, string username)
        {
            try
            {
                string systemId = GetOrCreateSystemId();
                string machineId = GetMachineId();
                
                // Get system info
                string osVersion = Environment.OSVersion.ToString();
                string processor = "";
                int ramMb = 0;
                
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            processor = obj["Name"]?.ToString() ?? "";
                            break;
                        }
                    }
                    
                    using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            long totalMem = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                            ramMb = (int)(totalMem / 1024 / 1024);
                            break;
                        }
                    }
                }
                catch { }
                
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        INSERT INTO system_registry (system_id, machine_id, system_name, activation_key, 
                            company_name, username, system_username, ip_address, os_version, processor, ram_mb, is_online)
                        VALUES (@system_id, @machine_id, @system_name, @activation_key, 
                            @company_name, @username, @system_username, @ip_address, @os_version, @processor, @ram_mb, TRUE)
                        ON CONFLICT (system_id) DO UPDATE SET
                            last_seen = CURRENT_TIMESTAMP,
                            is_online = TRUE,
                            ip_address = @ip_address,
                            username = @username";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        cmd.Parameters.AddWithValue("@machine_id", machineId);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@os_version", osVersion);
                        cmd.Parameters.AddWithValue("@processor", processor);
                        cmd.Parameters.AddWithValue("@ram_mb", ramMb);
                        cmd.ExecuteNonQuery();
                    }
                }
                
                return systemId;
            }
            catch (Exception ex)
            {
                LogError("RegisterSystem", ex.Message);
                return "";
            }
        }
        
        /// <summary>
        /// Update system heartbeat (keep-alive)
        /// </summary>
        public static void UpdateSystemHeartbeat(string systemId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        UPDATE system_registry 
                        SET last_seen = CURRENT_TIMESTAMP, is_online = TRUE, ip_address = @ip_address
                        WHERE system_id = @system_id";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Insert live stream frame for remote viewing
        /// </summary>
        public static bool InsertLiveStreamFrame(string systemId, string frameDataBase64, int screenWidth, int screenHeight)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Delete old frames
                    string deleteSql = @"DELETE FROM live_stream_frames WHERE system_id = @system_id 
                        AND id NOT IN (SELECT id FROM live_stream_frames WHERE system_id = @system_id ORDER BY capture_time DESC LIMIT 3)";
                    using (var delCmd = new NpgsqlCommand(deleteSql, connection))
                    {
                        delCmd.Parameters.AddWithValue("@system_id", systemId);
                        delCmd.ExecuteNonQuery();
                    }
                    
                    // Insert new frame
                    string sql = @"
                        INSERT INTO live_stream_frames (system_id, frame_data, screen_width, screen_height)
                        VALUES (@system_id, @frame_data, @screen_width, @screen_height)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        cmd.Parameters.AddWithValue("@frame_data", frameDataBase64);
                        cmd.Parameters.AddWithValue("@screen_width", screenWidth);
                        cmd.Parameters.AddWithValue("@screen_height", screenHeight);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("InsertLiveStreamFrame", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Check if someone is viewing this system
        /// </summary>
        public static bool HasActiveViewingSession(string systemId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        SELECT COUNT(*) FROM live_sessions 
                        WHERE system_id = @system_id AND is_active = TRUE 
                        AND started_at > (CURRENT_TIMESTAMP - INTERVAL '10 minutes')";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        long count = (long)(cmd.ExecuteScalar() ?? 0L);
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Store system information for a user
        /// </summary>
        public static bool SaveSystemInfo(string activationKey, string companyName, string systemName, string userName, 
            string osVersion, string processorInfo, string totalMemory, string installedApps, string systemSerialNumber, string trackingId = "")
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                
                string sql = @"
                    CREATE TABLE IF NOT EXISTS system_info (
                        id SERIAL PRIMARY KEY,
                        activation_key VARCHAR(255),
                        company_name VARCHAR(255),
                        system_name VARCHAR(255),
                        user_name VARCHAR(255),
                        tracking_id VARCHAR(50),
                        os_version VARCHAR(255),
                        processor_info VARCHAR(255),
                        total_memory VARCHAR(100),
                        installed_apps TEXT,
                        system_serial_number VARCHAR(255),
                        captured_at TIMESTAMP DEFAULT NOW(),
                        UNIQUE(activation_key, system_name, user_name, DATE(captured_at))
                    );
                    
                    INSERT INTO system_info (activation_key, company_name, system_name, user_name, tracking_id, os_version, 
                        processor_info, total_memory, installed_apps, system_serial_number)
                    VALUES (@activation_key, @company_name, @system_name, @user_name, @tracking_id, @os_version, 
                        @processor_info, @total_memory, @installed_apps, @system_serial_number)
                    ON CONFLICT (activation_key, system_name, user_name, DATE(captured_at))
                    DO UPDATE SET 
                        tracking_id = EXCLUDED.tracking_id,
                        os_version = EXCLUDED.os_version,
                        installed_apps = EXCLUDED.installed_apps,
                        captured_at = NOW()";

                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey ?? "");
                        cmd.Parameters.AddWithValue("@company_name", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system_name", systemName ?? "");
                        cmd.Parameters.AddWithValue("@user_name", userName ?? "");
                        cmd.Parameters.AddWithValue("@tracking_id", trackingId ?? "");
                        cmd.Parameters.AddWithValue("@os_version", osVersion ?? "");
                        cmd.Parameters.AddWithValue("@processor_info", processorInfo ?? "");
                        cmd.Parameters.AddWithValue("@total_memory", totalMemory ?? "");
                        cmd.Parameters.AddWithValue("@installed_apps", installedApps ?? "");
                        cmd.Parameters.AddWithValue("@system_serial_number", systemSerialNumber ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[System Info Error] SaveSystemInfo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get system information for all users in a company
        /// </summary>
        public static List<(string userName, string systemName, string osVersion, string processorInfo, string totalMemory, 
            string installedApps, DateTime capturedAt)> GetCompanySystemInfo(string companyName)
        {
            var results = new List<(string, string, string, string, string, string, DateTime)>();
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                
                string sql = @"
                    SELECT user_name, system_name, os_version, processor_info, total_memory, installed_apps, captured_at
                    FROM system_info
                    WHERE company_name = @company_name
                    ORDER BY captured_at DESC
                    LIMIT 1000";

                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company_name", companyName ?? "");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add((
                                    reader[0]?.ToString() ?? "",
                                    reader[1]?.ToString() ?? "",
                                    reader[2]?.ToString() ?? "",
                                    reader[3]?.ToString() ?? "",
                                    reader[4]?.ToString() ?? "",
                                    reader[5]?.ToString() ?? "",
                                    (DateTime)reader[6]
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[System Info Error] GetCompanySystemInfo: {ex.Message}");
            }
            return results;
        }

        /// <summary>
        /// Get system info for a specific user
        /// </summary>
        public static (string osVersion, string processorInfo, string totalMemory, string installedApps, DateTime capturedAt)? 
            GetUserSystemInfo(string companyName, string userName)
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                
                string sql = @"
                    SELECT os_version, processor_info, total_memory, installed_apps, captured_at
                    FROM system_info
                    WHERE company_name = @company_name AND user_name = @user_name
                    ORDER BY captured_at DESC
                    LIMIT 1";

                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company_name", companyName ?? "");
                        cmd.Parameters.AddWithValue("@user_name", userName ?? "");
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return (
                                    reader[0]?.ToString() ?? "",
                                    reader[1]?.ToString() ?? "",
                                    reader[2]?.ToString() ?? "",
                                    reader[3]?.ToString() ?? "",
                                    (DateTime)reader[4]
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[System Info Error] GetUserSystemInfo: {ex.Message}");
            }
            return null;
        }

        // ==================== SYSTEM CONTROL COMMANDS ====================
        public static List<(int id, string commandType, string parametersJson)> GetPendingCommands(string systemName, string companyName)
        {
            var commands = new List<(int id, string commandType, string parametersJson)>();
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=10;CommandTimeout=15;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    string createSql = @"CREATE TABLE IF NOT EXISTS system_control_commands (
                        id SERIAL PRIMARY KEY, company_name VARCHAR(255), system_name VARCHAR(255),
                        user_name VARCHAR(255), command_type VARCHAR(100), parameters JSONB DEFAULT '{}',
                        status VARCHAR(50) DEFAULT 'pending', result TEXT, created_by VARCHAR(255),
                        created_at TIMESTAMP DEFAULT NOW(), executed_at TIMESTAMP)";
                    using (var cmd = new NpgsqlCommand(createSql, connection)) cmd.ExecuteNonQuery();
                    string sql = @"SELECT id, command_type, COALESCE(parameters::text, '{}') FROM system_control_commands
                                   WHERE system_name = @system AND company_name = @company AND status = 'pending' ORDER BY created_at ASC";
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system", systemName ?? "");
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        using (var reader = cmd.ExecuteReader())
                        { while (reader.Read()) { commands.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2))); } }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SystemControl DB] GetPendingCommands error: {ex.Message}"); }
            return commands;
        }

        public static void UpdateCommandStatus(int commandId, string status, string result)
        {
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=10;CommandTimeout=15;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE system_control_commands SET status = @status, result = @result, executed_at = NOW() WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@status", status ?? "completed");
                        cmd.Parameters.AddWithValue("@result", result ?? "");
                        cmd.Parameters.AddWithValue("@id", commandId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SystemControl DB] UpdateCommandStatus error: {ex.Message}"); }
        }

        public static bool SaveSystemInfoDetailed(string companyName, string userName, string systemName, Dictionary<string, object> systemData)
        {
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=15;CommandTimeout=30;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    string createSql = @"CREATE TABLE IF NOT EXISTS system_info_detailed (id SERIAL PRIMARY KEY, company_name VARCHAR(255), user_name VARCHAR(255), system_name VARCHAR(255), machine_id VARCHAR(255),
                        os_name TEXT, os_version TEXT, os_build TEXT, os_install_date TEXT, os_serial TEXT, last_boot_time TEXT,
                        processor_name TEXT, processor_cores TEXT, processor_logical TEXT, processor_speed TEXT, processor_id TEXT,
                        total_ram TEXT, available_ram TEXT, memory_details TEXT, storage_info TEXT, network_info TEXT,
                        motherboard_manufacturer TEXT, motherboard_product TEXT, motherboard_serial TEXT,
                        bios_manufacturer TEXT, bios_version TEXT, bios_date TEXT, gpu_info TEXT, system_architecture TEXT, timezone TEXT,
                        raw_data JSONB, last_updated TIMESTAMP DEFAULT NOW(), UNIQUE(company_name, user_name, system_name))";
                    using (var cmd = new NpgsqlCommand(createSql, connection)) cmd.ExecuteNonQuery();
                    string machineId = DatabaseHelper.GetMachineId();
                    string jsonData = System.Text.Json.JsonSerializer.Serialize(systemData);
                    string GetVal(string key) { if (systemData.ContainsKey(key)) { var val = systemData[key]; if (val is System.Text.Json.JsonElement je) return je.ToString(); return val?.ToString() ?? ""; } return ""; }
                    string upsertSql = @"INSERT INTO system_info_detailed (company_name, user_name, system_name, machine_id, os_name, os_version, os_build, os_install_date, os_serial, last_boot_time, processor_name, processor_cores, processor_logical, processor_speed, processor_id, total_ram, available_ram, memory_details, storage_info, network_info, motherboard_manufacturer, motherboard_product, motherboard_serial, bios_manufacturer, bios_version, bios_date, gpu_info, system_architecture, timezone, raw_data, last_updated)
                        VALUES (@company, @user, @system, @machineId, @osName, @osVer, @osBuild, @osInstall, @osSerial, @lastBoot, @procName, @procCores, @procLogical, @procSpeed, @procId, @totalRam, @availRam, @memDetails, @storageInfo, @networkInfo, @mbMfr, @mbProduct, @mbSerial, @biosMfr, @biosVer, @biosDate, @gpuInfo, @arch, @tz, @rawData::jsonb, NOW())
                        ON CONFLICT (company_name, user_name, system_name) DO UPDATE SET machine_id=@machineId, os_name=@osName, os_version=@osVer, os_build=@osBuild, os_install_date=@osInstall, os_serial=@osSerial, last_boot_time=@lastBoot, processor_name=@procName, processor_cores=@procCores, processor_logical=@procLogical, processor_speed=@procSpeed, processor_id=@procId, total_ram=@totalRam, available_ram=@availRam, memory_details=@memDetails, storage_info=@storageInfo, network_info=@networkInfo, motherboard_manufacturer=@mbMfr, motherboard_product=@mbProduct, motherboard_serial=@mbSerial, bios_manufacturer=@biosMfr, bios_version=@biosVer, bios_date=@biosDate, gpu_info=@gpuInfo, system_architecture=@arch, timezone=@tz, raw_data=@rawData::jsonb, last_updated=NOW()";
                    using (var cmd = new NpgsqlCommand(upsertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? ""); cmd.Parameters.AddWithValue("@user", userName ?? ""); cmd.Parameters.AddWithValue("@system", systemName ?? ""); cmd.Parameters.AddWithValue("@machineId", machineId ?? "");
                        cmd.Parameters.AddWithValue("@osName", GetVal("os_name")); cmd.Parameters.AddWithValue("@osVer", GetVal("os_version")); cmd.Parameters.AddWithValue("@osBuild", GetVal("os_build")); cmd.Parameters.AddWithValue("@osInstall", GetVal("os_install_date")); cmd.Parameters.AddWithValue("@osSerial", GetVal("os_serial_number")); cmd.Parameters.AddWithValue("@lastBoot", GetVal("last_boot_time"));
                        cmd.Parameters.AddWithValue("@procName", GetVal("processor_name")); cmd.Parameters.AddWithValue("@procCores", GetVal("processor_cores")); cmd.Parameters.AddWithValue("@procLogical", GetVal("processor_logical")); cmd.Parameters.AddWithValue("@procSpeed", GetVal("processor_speed")); cmd.Parameters.AddWithValue("@procId", GetVal("processor_id"));
                        cmd.Parameters.AddWithValue("@totalRam", GetVal("total_ram")); cmd.Parameters.AddWithValue("@availRam", GetVal("available_ram")); cmd.Parameters.AddWithValue("@memDetails", GetVal("memory_details")); cmd.Parameters.AddWithValue("@storageInfo", GetVal("storage_info")); cmd.Parameters.AddWithValue("@networkInfo", GetVal("network_info"));
                        cmd.Parameters.AddWithValue("@mbMfr", GetVal("motherboard_manufacturer")); cmd.Parameters.AddWithValue("@mbProduct", GetVal("motherboard_product")); cmd.Parameters.AddWithValue("@mbSerial", GetVal("motherboard_serial"));
                        cmd.Parameters.AddWithValue("@biosMfr", GetVal("bios_manufacturer")); cmd.Parameters.AddWithValue("@biosVer", GetVal("bios_version")); cmd.Parameters.AddWithValue("@biosDate", GetVal("bios_release_date"));
                        cmd.Parameters.AddWithValue("@gpuInfo", GetVal("gpu_info")); cmd.Parameters.AddWithValue("@arch", GetVal("system_architecture")); cmd.Parameters.AddWithValue("@tz", GetVal("timezone")); cmd.Parameters.AddWithValue("@rawData", jsonData);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SystemInfo] SaveSystemInfoDetailed error: {ex.Message}"); return false; }
        }

        public static bool SyncInstalledAppsDetailed(string companyName, string userName, string systemName, string machineId, List<InstalledApp> apps)
        {
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=15;CommandTimeout=30;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS installed_apps_detailed (id SERIAL PRIMARY KEY, company_name VARCHAR(255), user_name VARCHAR(255), system_name VARCHAR(255), machine_id VARCHAR(255), app_name VARCHAR(500), app_version VARCHAR(255), app_publisher VARCHAR(255), install_date VARCHAR(100), install_location TEXT, uninstall_string TEXT, app_size VARCHAR(100), last_synced TIMESTAMP DEFAULT NOW(), UNIQUE(company_name, system_name, app_name))", connection)) cmd.ExecuteNonQuery();
                    using (var cmd = new NpgsqlCommand("DELETE FROM installed_apps_detailed WHERE company_name = @company AND system_name = @system", connection)) { cmd.Parameters.AddWithValue("@company", companyName ?? ""); cmd.Parameters.AddWithValue("@system", systemName ?? ""); cmd.ExecuteNonQuery(); }
                    foreach (var app in apps)
                    {
                        using (var cmd = new NpgsqlCommand(@"INSERT INTO installed_apps_detailed (company_name, user_name, system_name, machine_id, app_name, app_version, app_publisher, install_date, install_location, uninstall_string, app_size) VALUES (@company, @user, @system, @machineId, @name, @version, @publisher, @installDate, @installLoc, @uninstall, @size) ON CONFLICT (company_name, system_name, app_name) DO UPDATE SET app_version=@version, app_publisher=@publisher, install_date=@installDate, install_location=@installLoc, uninstall_string=@uninstall, app_size=@size, last_synced=NOW()", connection))
                        {
                            cmd.Parameters.AddWithValue("@company", companyName ?? ""); cmd.Parameters.AddWithValue("@user", userName ?? ""); cmd.Parameters.AddWithValue("@system", systemName ?? ""); cmd.Parameters.AddWithValue("@machineId", machineId ?? "");
                            cmd.Parameters.AddWithValue("@name", app.Name ?? ""); cmd.Parameters.AddWithValue("@version", app.Version ?? ""); cmd.Parameters.AddWithValue("@publisher", app.Publisher ?? ""); cmd.Parameters.AddWithValue("@installDate", app.InstallDate ?? ""); cmd.Parameters.AddWithValue("@installLoc", app.InstallLocation ?? ""); cmd.Parameters.AddWithValue("@uninstall", app.UninstallString ?? ""); cmd.Parameters.AddWithValue("@size", app.Size ?? "");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[InstalledApps] SyncInstalledAppsDetailed error: {ex.Message}"); return false; }
        }

        public static bool SaveFileActivityLog(string companyName, string userName, string systemName, string machineId, string activityType, string fileName, string oldFileName, string newFileName, string filePath, string emailDomain, string emailRecipient, string details, string emailSender = "", string emailSubject = "", long fileSize = 0)
        {
            try
            {
                string connStr = "Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=10;CommandTimeout=15;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS file_activity_logs (id SERIAL PRIMARY KEY, company_name VARCHAR(255), user_name VARCHAR(255), system_name VARCHAR(255), machine_id VARCHAR(255), activity_type VARCHAR(100), file_name VARCHAR(1000), old_file_name VARCHAR(1000), new_file_name VARCHAR(1000), file_path TEXT, email_domain VARCHAR(500), email_sender VARCHAR(500), email_recipient VARCHAR(500), email_subject VARCHAR(1000), details TEXT, file_size BIGINT DEFAULT 0, log_timestamp TIMESTAMP DEFAULT NOW(), created_at TIMESTAMP DEFAULT NOW())", connection)) cmd.ExecuteNonQuery();
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS machine_id VARCHAR(255)", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS new_file_name VARCHAR(1000)", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_domain VARCHAR(500)", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_sender VARCHAR(500)", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_subject VARCHAR(1000)", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS file_size BIGINT DEFAULT 0", connection)) c.ExecuteNonQuery(); } catch {}
                    try { using (var c = new NpgsqlCommand("ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS details TEXT", connection)) c.ExecuteNonQuery(); } catch {}
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO file_activity_logs (company_name, user_name, system_name, machine_id, activity_type, file_name, old_file_name, new_file_name, file_path, email_domain, email_sender, email_recipient, email_subject, details, file_size) VALUES (@company, @user, @system, @machineId, @actType, @fileName, @oldName, @newName, @filePath, @emailDomain, @emailSender, @emailRecipient, @emailSubject, @details, @fileSize)", connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? ""); cmd.Parameters.AddWithValue("@user", userName ?? ""); cmd.Parameters.AddWithValue("@system", systemName ?? ""); cmd.Parameters.AddWithValue("@machineId", machineId ?? "");
                        cmd.Parameters.AddWithValue("@actType", activityType ?? ""); cmd.Parameters.AddWithValue("@fileName", fileName ?? ""); cmd.Parameters.AddWithValue("@oldName", oldFileName ?? ""); cmd.Parameters.AddWithValue("@newName", newFileName ?? "");
                        cmd.Parameters.AddWithValue("@filePath", filePath ?? ""); cmd.Parameters.AddWithValue("@emailDomain", emailDomain ?? ""); cmd.Parameters.AddWithValue("@emailSender", emailSender ?? ""); cmd.Parameters.AddWithValue("@emailRecipient", emailRecipient ?? "");
                        cmd.Parameters.AddWithValue("@emailSubject", emailSubject ?? ""); cmd.Parameters.AddWithValue("@details", details ?? ""); cmd.Parameters.AddWithValue("@fileSize", fileSize);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FileActivity] SaveFileActivityLog error: {ex.Message}"); return false; }
        }
    }

    public class ActivityItem
    {
        public DateTime Time { get; set; }
        public string Type { get; set; } = "";
        public Color Color { get; set; }
    }
    
    // ============= LIVE SYSTEM TRACKING (HEARTBEAT) =============
    
    /// <summary>
    /// Data class for system heartbeat
    /// </summary>
    public class SystemHeartbeatData
    {
        public string CompanyName { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Department { get; set; } = "";
        public string MachineId { get; set; } = "";
        public string SystemId { get; set; } = "";  // The readable system ID like "203-760-824"
        public string MachineName { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string OsVersion { get; set; } = "";
        public string AppVersion { get; set; } = "1.0";
        public string Status { get; set; } = "active";
    }
    
    /// <summary>
    /// Helper class for live system tracking via web API
    /// </summary>
    public static class LiveSystemTracker
    {
        private const string ApiBaseUrl = "https://788225b3ea9f.ngrok-free.app/api.php";
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
        
        /// <summary>
        /// Send heartbeat to server to indicate this system is online
        /// </summary>
        public static async Task<bool> SendHeartbeatAsync(SystemHeartbeatData data)
        {
            try
            {
                var payload = new
                {
                    action = "system_heartbeat",
                    company_name = data.CompanyName,
                    employee_id = data.EmployeeId,
                    display_name = data.DisplayName,
                    department = data.Department,
                    machine_id = data.MachineId,
                    system_id = data.SystemId,
                    machine_name = data.MachineName,
                    ip_address = data.IpAddress,
                    os_version = data.OsVersion,
                    app_version = data.AppVersion,
                    status = data.Status
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(ApiBaseUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Heartbeat error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Send disconnect notification when app closes
        /// </summary>
        public static async Task<bool> SendDisconnectAsync(string companyName, string employeeId, string machineId)
        {
            try
            {
                var payload = new
                {
                    action = "system_disconnect",
                    company_name = companyName,
                    employee_id = employeeId,
                    machine_id = machineId
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(ApiBaseUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Build heartbeat data from current system info
        /// </summary>
        public static SystemHeartbeatData BuildHeartbeatData(string companyName, string employeeId, string displayName, string department, string systemId = "")
        {
            return new SystemHeartbeatData
            {
                CompanyName = companyName,
                EmployeeId = employeeId,
                DisplayName = displayName,
                Department = department,
                MachineId = DatabaseHelper.GetMachineId(),
                SystemId = string.IsNullOrEmpty(systemId) ? DatabaseHelper.GetOrCreateSystemId() : systemId,
                MachineName = Environment.MachineName,
                IpAddress = DatabaseHelper.GetLocalIPAddress(),
                OsVersion = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}",
                AppVersion = "1.0",
                Status = "active"
            };
        }

        /// <summary>
        /// Save a chat message to the database for sync between users
        /// </summary>
        public static bool SaveChatMessage(string sender, string recipient, string message, string companyName)
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    // Ensure table exists
                    string createTableSql = @"CREATE TABLE IF NOT EXISTS chat_messages (
                        id SERIAL PRIMARY KEY,
                        sender VARCHAR(255) NOT NULL,
                        recipient VARCHAR(255) NOT NULL,
                        message TEXT NOT NULL,
                        company_name VARCHAR(255) NOT NULL,
                        created_at TIMESTAMP DEFAULT NOW()
                    );";
                    
                    using (var cmd = new NpgsqlCommand(createTableSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert the message
                    string insertSql = @"INSERT INTO chat_messages (sender, recipient, message, company_name) 
                        VALUES (@sender, @recipient, @message, @company)";
                    
                    using (var cmd = new NpgsqlCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@sender", sender ?? "");
                        cmd.Parameters.AddWithValue("@recipient", recipient ?? "");
                        cmd.Parameters.AddWithValue("@message", message ?? "");
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat DB Error] SaveChatMessage: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get chat messages between two users from the database
        /// </summary>
        public static List<(string sender, string message, DateTime timestamp)> GetChatMessages(string user1, string user2, string companyName, int limit = 100)
        {
            var messages = new List<(string, string, DateTime)>();
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    string sql = @"SELECT sender, message, created_at 
                        FROM chat_messages 
                        WHERE company_name = @company
                        AND ((sender = @user1 AND recipient = @user2) 
                             OR (sender = @user2 AND recipient = @user1))
                        ORDER BY created_at ASC
                        LIMIT @limit";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@user1", user1 ?? "");
                        cmd.Parameters.AddWithValue("@user2", user2 ?? "");
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.Parameters.AddWithValue("@limit", limit);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string sender = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                string message = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                DateTime timestamp = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2);
                                messages.Add((sender, message, timestamp));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat DB Error] GetChatMessages: {ex.Message}");
            }
            return messages;
        }

        /// <summary>
        /// Check if there are new messages from a specific sender
        /// </summary>
        public static int GetNewMessageCount(string recipient, string sender, string companyName)
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    // Count messages from this sender that might not have been seen
                    string sql = @"SELECT COUNT(*) FROM chat_messages
                        WHERE recipient = @recipient
                        AND sender = @sender
                        AND company_name = @company";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@recipient", recipient ?? "");
                        cmd.Parameters.AddWithValue("@sender", sender ?? "");
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");

                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat DB Error] GetNewMessageCount: {ex.Message}");
                return 0;
            }
        }

        // ==================== BLOCKED APPLICATIONS DB METHODS ====================

        /// <summary>
        /// Save a blocked application to database for tracking
        /// </summary>
        public static void SaveBlockedApp(string companyName, string systemName, string appName, string appPath, string exeName)
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    // Ensure table exists
                    string createTable = @"CREATE TABLE IF NOT EXISTS blocked_applications (
                        id SERIAL PRIMARY KEY,
                        company_name VARCHAR(255),
                        system_name VARCHAR(255),
                        app_name VARCHAR(255),
                        app_path TEXT,
                        exe_name VARCHAR(255),
                        blocked_by VARCHAR(255) DEFAULT 'admin',
                        blocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        is_blocked BOOLEAN DEFAULT TRUE,
                        UNIQUE(company_name, system_name, app_name)
                    )";
                    using (var cmd = new NpgsqlCommand(createTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    string sql = @"INSERT INTO blocked_applications (company_name, system_name, app_name, app_path, exe_name, blocked_at, is_blocked)
                        VALUES (@company, @system, @app, @path, @exe, @time, TRUE)
                        ON CONFLICT (company_name, system_name, app_name)
                        DO UPDATE SET app_path = @path, exe_name = @exe, blocked_at = @time, is_blocked = TRUE";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system", systemName ?? "");
                        cmd.Parameters.AddWithValue("@app", appName ?? "");
                        cmd.Parameters.AddWithValue("@path", appPath ?? "");
                        cmd.Parameters.AddWithValue("@exe", exeName ?? "");
                        cmd.Parameters.AddWithValue("@time", IstHelper.Now);
                        cmd.ExecuteNonQuery();
                    }

                    System.Diagnostics.Debug.WriteLine($"[DB] Blocked app saved: {appName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB Error] SaveBlockedApp: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a blocked application from database
        /// </summary>
        public static void RemoveBlockedApp(string companyName, string systemName, string appName)
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    string sql = @"UPDATE blocked_applications SET is_blocked = FALSE
                        WHERE company_name = @company AND system_name = @system AND app_name = @app";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system", systemName ?? "");
                        cmd.Parameters.AddWithValue("@app", appName ?? "");
                        cmd.ExecuteNonQuery();
                    }

                    System.Diagnostics.Debug.WriteLine($"[DB] Blocked app removed: {appName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB Error] RemoveBlockedApp: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all blocked apps for a system
        /// </summary>
        public static List<(string appName, string appPath, string exeName)> GetBlockedApps(string companyName, string systemName)
        {
            var result = new List<(string, string, string)>();
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    string sql = @"SELECT app_name, app_path, exe_name FROM blocked_applications
                        WHERE company_name = @company AND system_name = @system AND is_blocked = TRUE";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system", systemName ?? "");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add((
                                    reader.GetString(0),
                                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    reader.IsDBNull(2) ? "" : reader.GetString(2)
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB Error] GetBlockedApps: {ex.Message}");
            }
            return result;
        }

        // ==================== SYSTEM RESTRICTIONS DB METHODS ====================

        /// <summary>
        /// Save restriction state to database
        /// </summary>
        public static void SaveRestrictionState(string companyName, string systemName, string restrictionType, bool isActive)
        {
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    // Ensure table exists
                    string createTable = @"CREATE TABLE IF NOT EXISTS system_restrictions (
                        id SERIAL PRIMARY KEY,
                        company_name VARCHAR(255),
                        system_name VARCHAR(255),
                        restriction_type VARCHAR(100),
                        is_active BOOLEAN DEFAULT FALSE,
                        changed_by VARCHAR(255) DEFAULT 'admin',
                        changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(company_name, system_name, restriction_type)
                    )";
                    using (var cmd = new NpgsqlCommand(createTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    string sql = @"INSERT INTO system_restrictions (company_name, system_name, restriction_type, is_active, changed_at)
                        VALUES (@company, @system, @type, @active, @time)
                        ON CONFLICT (company_name, system_name, restriction_type)
                        DO UPDATE SET is_active = @active, changed_at = @time";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system", systemName ?? "");
                        cmd.Parameters.AddWithValue("@type", restrictionType ?? "");
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@time", IstHelper.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB Error] SaveRestrictionState: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all active restrictions for a system
        /// </summary>
        public static Dictionary<string, bool> GetActiveRestrictions(string companyName, string systemName)
        {
            var result = new Dictionary<string, bool>();
            try
            {
                string connStr = $"Host=72.61.235.203;Port=9095;Database=controller;Username=controller_dbuser;Password=hwrw*&^hdg2gsGDGJHAU&838373h;Timeout=30;CommandTimeout=60;";
                using (var connection = new NpgsqlConnection(connStr))
                {
                    connection.Open();

                    string sql = @"SELECT restriction_type, is_active FROM system_restrictions
                        WHERE company_name = @company AND system_name = @system";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company", companyName ?? "");
                        cmd.Parameters.AddWithValue("@system", systemName ?? "");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result[reader.GetString(0)] = reader.GetBoolean(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB Error] GetActiveRestrictions: {ex.Message}");
            }
            return result;
        }
    }
}

