using System;
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
    /// Web log entry for batch insert
    /// </summary>
    public class WebLogEntry
    {
        public string Browser { get; set; } = "";
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
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
        private const string Host = "72.61.170.243";
        private const int Port = 9095;
        private const string Database = "controller_application";
        private const string Username = "appuser";
        private const string Password = "jksdj$&^&*YUG*^%&THJHIO4546GHG&j";
        
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
                
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        UPDATE connected_systems 
                        SET last_heartbeat = NOW(), 
                            is_online = true, 
                            status = @status,
                            ip_address = @ip
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
                        cmd.Parameters.AddWithValue("@punch_in_time", DateTime.Now);
                        
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
                        cmd.Parameters.AddWithValue("@punch_out_time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        
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
                        cmd.Parameters.AddWithValue("@break_start_time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        
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
                        cmd.Parameters.AddWithValue("@break_end_time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        
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
        
        public static List<ActivityItem> GetTodayActivities(string username)
        {
            var activities = new List<ActivityItem>();
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Get today's session
                    string sql = @"SELECT punch_in_time, punch_out_time, break_start_time, break_end_time, 
                                   break_duration_seconds, total_work_duration_seconds
                                   FROM punch_log_consolidated 
                                   WHERE username = @username 
                                   AND DATE(punch_in_time) = CURRENT_DATE
                                   ORDER BY id DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(0),
                                        Type = "Punch In",
                                        Color = Color.FromArgb(16, 185, 129) // Green
                                    });
                                }
                                
                                if (!reader.IsDBNull(2))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(2),
                                        Type = "Break Start",
                                        Color = Color.FromArgb(251, 191, 36) // Yellow
                                    });
                                }
                                
                                if (!reader.IsDBNull(3))
                                {
                                    activities.Add(new ActivityItem
                                    {
                                        Time = reader.GetDateTime(3),
                                        Type = "Break Stop",
                                        Color = Color.FromArgb(139, 92, 246) // Purple
                                    });
                                }
                                
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
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {method}: {error}\n");
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
                                        machine_id, browser_name, website_url, page_title, category, visit_time)
                                    VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @display_user_name,
                                        @machine_id, @browser_name, @website_url, @page_title, @category, @visit_time)";
                                
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
    }
}
