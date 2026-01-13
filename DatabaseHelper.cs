using System;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Microsoft.Win32;

namespace DesktopController
{
    /// <summary>
    /// Stores logged-in user information
    /// </summary>
    public class UserInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
        public string CompanyName { get; set; } = "";
    }
    
    /// <summary>
    /// Stores activation key information
    /// </summary>
    public class ActivationInfo
    {
        public string ActivationKey { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public bool IsValid { get; set; } = false;
    }
    
    public class DatabaseHelper
    {
        private const string Host = "72.61.170.243";
        private const int Port = 9095;
        private const string Database = "controller_application";
        private const string Username = "appuser";
        private const string Password = "jksdj$&^&*YUG*^%&THJHIO4546GHG&j";
        
        // Registry key for storing activation
        private const string RegistryPath = @"SOFTWARE\DesktopController";
        private const string ActivationKeyName = "ActivationKey";
        private const string MachineIdKeyName = "MachineId";
        private const string CompanyNameKeyName = "CompanyName";
        private const string SystemIdKeyName = "SystemId";
        
        /// <summary>
        /// Gets or generates a unique System ID based on machine hardware (like AnyDesk/UltraViewer ID)
        /// Format: 9-digit number (e.g., 123-456-789)
        /// GUARANTEED UNIQUE per physical machine using MAC address + CPU ID hash
        /// Stored in registry so it persists across restarts
        /// </summary>
        public static string GetOrCreateSystemId()
        {
            try
            {
                // Try to read existing ID from registry
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        string? existingId = key.GetValue(SystemIdKeyName)?.ToString();
                        if (!string.IsNullOrEmpty(existingId))
                        {
                            return existingId;
                        }
                    }
                }
                
                // Generate hardware-based unique ID (SAME HARDWARE = SAME ID)
                string machineId = GetMachineId(); // Hardware hash
                
                // Convert hardware hash to 9-digit ID format (XXX-XXX-XXX)
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
                    
                    // Convert first 12 bytes to integers for 9 digits
                    int id1 = (Math.Abs(BitConverter.ToInt32(hashBytes, 0)) % 900) + 100; // 100-999
                    int id2 = (Math.Abs(BitConverter.ToInt32(hashBytes, 4)) % 900) + 100; // 100-999
                    int id3 = (Math.Abs(BitConverter.ToInt32(hashBytes, 8)) % 900) + 100; // 100-999
                    
                    string systemId = $"{id1}-{id2}-{id3}";
                    
                    // Save to registry for faster lookup
                    using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                    {
                        if (key != null)
                        {
                            key.SetValue(SystemIdKeyName, systemId);
                        }
                    }
                    
                    return systemId;
                }
            }
            catch
            {
                // Fallback: generate from machine name hash
                string fallbackId = Environment.MachineName + Environment.UserName;
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackId));
                    int id1 = (Math.Abs(BitConverter.ToInt32(hashBytes, 0)) % 900) + 100;
                    int id2 = (Math.Abs(BitConverter.ToInt32(hashBytes, 4)) % 900) + 100;
                    int id3 = (Math.Abs(BitConverter.ToInt32(hashBytes, 8)) % 900) + 100;
                    return $"{id1}-{id2}-{id3}";
                }
            }
        }
        
        /// <summary>
        /// Gets a unique machine identifier based on hardware
        /// Uses MAC address + CPU ID for consistent unique identification
        /// </summary>
        public static string GetMachineId()
        {
            try
            {
                // Combine multiple hardware identifiers for uniqueness
                string macAddress = "";
                string cpuId = "";
                string biosId = "";
                string diskId = "";
                
                // Get MAC Address (most reliable)
                try
                {
                    foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                            nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        {
                            macAddress = nic.GetPhysicalAddress().ToString();
                            if (!string.IsNullOrEmpty(macAddress))
                                break;
                        }
                    }
                }
                catch { }
                
                // Get CPU ID
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        cpuId = obj["ProcessorId"]?.ToString() ?? "";
                        break;
                    }
                }
                
                // Get BIOS Serial
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        biosId = obj["SerialNumber"]?.ToString() ?? "";
                        break;
                    }
                }
                
                // Get Disk Serial
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index = 0"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        diskId = obj["SerialNumber"]?.ToString() ?? "";
                        break;
                    }
                }
                
                // Combine and hash (MAC + CPU is most reliable)
                string combined = $"{macAddress}-{cpuId}-{biosId}-{diskId}";
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32);
                }
            }
            catch
            {
                // Fallback: Use machine name + user name
                return ComputeSHA256Hash($"{Environment.MachineName}-{Environment.UserName}").Substring(0, 32);
            }
        }
        
        /// <summary>
        /// Checks if this system is already activated
        /// </summary>
        public static bool IsSystemActivated(out string companyName)
        {
            companyName = "";
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        string? storedKey = key.GetValue(ActivationKeyName)?.ToString();
                        string? storedMachineId = key.GetValue(MachineIdKeyName)?.ToString();
                        string? storedCompany = key.GetValue(CompanyNameKeyName)?.ToString();
                        
                        if (!string.IsNullOrEmpty(storedKey) && 
                            !string.IsNullOrEmpty(storedMachineId) &&
                            storedMachineId == GetMachineId())
                        {
                            companyName = storedCompany ?? "";
                            return true;
                        }
                    }
                }
            }
            catch { }
            
            return false;
        }
        
        /// <summary>
        /// Saves activation info to registry
        /// </summary>
        public static void SaveActivation(string activationKey, string companyName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPath))
                {
                    key.SetValue(ActivationKeyName, activationKey, RegistryValueKind.String);
                    key.SetValue(MachineIdKeyName, GetMachineId(), RegistryValueKind.String);
                    key.SetValue(CompanyNameKeyName, companyName, RegistryValueKind.String);
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Clears saved activation from registry (for re-registration)
        /// </summary>
        public static void ClearActivation()
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(ActivationKeyName, false);
                        key.DeleteValue(MachineIdKeyName, false);
                        key.DeleteValue(CompanyNameKeyName, false);
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Validates activation key against the database (activation_keys table)
        /// </summary>
        public static ActivationInfo? ValidateActivationKey(string activationKey)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Try different query variations for activation_keys table
                    string[] queries = new string[]
                    {
                        "SELECT activation_key, company_name FROM activation_keys WHERE activation_key = @key LIMIT 1",
                        "SELECT activation_key, company_name FROM \"activation_keys\" WHERE activation_key = @key LIMIT 1",
                        "SELECT key, company_name FROM activation_keys WHERE key = @key LIMIT 1",
                        "SELECT activation_key, company FROM activation_keys WHERE activation_key = @key LIMIT 1"
                    };
                    
                    foreach (string query in queries)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@key", activationKey.Trim());
                                
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        return new ActivationInfo
                                        {
                                            ActivationKey = activationKey.Trim(),
                                            CompanyName = reader.GetString(1),
                                            IsValid = true
                                        };
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database connection error: {ex.Message}");
            }
            
            return null;
        }
        
        public static string GetConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Timeout=5;CommandTimeout=10;";
        }
        
        /// <summary>
        /// Validates user credentials against the database (company_users table)
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Plain text password</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        public static bool ValidateUser(string username, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Try different table/column name variations for company_users table
                    string[] queries = new string[]
                    {
                        "SELECT password FROM company_users WHERE username = @username LIMIT 1",
                        "SELECT password FROM company_users WHERE LOWER(username) = LOWER(@username) LIMIT 1",
                        "SELECT password FROM \"company_users\" WHERE username = @username LIMIT 1",
                        "SELECT password FROM company_users WHERE user_name = @username LIMIT 1",
                        "SELECT password FROM company_users WHERE email = @username LIMIT 1",
                        "SELECT pass FROM company_users WHERE username = @username LIMIT 1"
                    };
                    
                    foreach (string query in queries)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@username", username.Trim());
                                
                                var result = cmd.ExecuteScalar();
                                
                                if (result != null && result != DBNull.Value)
                                {
                                    string storedHash = result.ToString() ?? "";
                                    
                                    // Try different hash verification methods
                                    if (VerifyPassword(password, storedHash))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Try next query variation
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database connection error: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Validates user and returns user info if successful (company_users table)
        /// </summary>
        public static UserInfo? ValidateAndGetUser(string username, string password, string companyName = "")
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Query to get user with password from company_users table
                    string[] queries = new string[]
                    {
                        "SELECT id, name, username, password, is_admin FROM company_users WHERE LOWER(username) = LOWER(@username) LIMIT 1",
                        "SELECT id, name, user_name, password, is_admin FROM company_users WHERE LOWER(user_name) = LOWER(@username) LIMIT 1",
                        "SELECT id, name, username, password, is_admin FROM \"company_users\" WHERE LOWER(username) = LOWER(@username) LIMIT 1",
                        "SELECT id, username, username as name, password, false as is_admin FROM company_users WHERE LOWER(username) = LOWER(@username) LIMIT 1"
                    };
                    
                    foreach (string query in queries)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@username", username.Trim());
                                
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        string storedHash = reader["password"]?.ToString() ?? "";
                                        
                                        if (VerifyPassword(password, storedHash))
                                        {
                                            return new UserInfo
                                            {
                                                Id = reader["id"]?.ToString() ?? "",
                                                Name = reader["name"]?.ToString() ?? username,
                                                Email = username,
                                                IsAdmin = reader["is_admin"] != DBNull.Value && Convert.ToBoolean(reader["is_admin"]),
                                                CompanyName = companyName
                                            };
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database connection error: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Verifies password against stored hash
        /// Supports: Plain text, MD5, SHA256, SHA512, BCrypt-style
        /// </summary>
        private static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;
            
            // Check BCrypt hash FIRST (starts with $2a$, $2b$, $2y$)
            if (storedHash.StartsWith("$2"))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
                }
                catch { }
            }
            
            // Check for plain text match (not recommended but checking)
            if (password == storedHash)
                return true;
            
            // Check MD5 hash
            string md5Hash = ComputeMD5Hash(password);
            if (string.Equals(md5Hash, storedHash, StringComparison.OrdinalIgnoreCase))
                return true;
            
            // Check SHA256 hash
            string sha256Hash = ComputeSHA256Hash(password);
            if (string.Equals(sha256Hash, storedHash, StringComparison.OrdinalIgnoreCase))
                return true;
            
            // Check SHA512 hash
            string sha512Hash = ComputeSHA512Hash(password);
            if (string.Equals(sha512Hash, storedHash, StringComparison.OrdinalIgnoreCase))
                return true;
            
            // Check Base64 encoded hashes
            try
            {
                // MD5 as Base64
                byte[] md5Bytes = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
                if (Convert.ToBase64String(md5Bytes) == storedHash)
                    return true;
                
                // SHA256 as Base64
                byte[] sha256Bytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
                if (Convert.ToBase64String(sha256Bytes) == storedHash)
                    return true;
            }
            catch { }
            
            // Check PBKDF2-style (if it contains separator for salt)
            if (storedHash.Contains(":"))
            {
                try
                {
                    return VerifyPBKDF2(password, storedHash);
                }
                catch { }
            }
            
            return false;
        }
        
        private static string ComputeMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        
        private static string ComputeSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        
        private static string ComputeSHA512Hash(string input)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha512.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        
        /// <summary>
        /// PBKDF2 verification for salted hashes
        /// </summary>
        private static bool VerifyPBKDF2(string password, string storedHash)
        {
            // Common format: iterations:salt:hash or salt:hash
            string[] parts = storedHash.Split(':');
            
            if (parts.Length == 2)
            {
                // salt:hash format
                string salt = parts[0];
                string hash = parts[1];
                
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] hashBytes = Convert.FromBase64String(hash);
                
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(hashBytes.Length);
                    return CryptographicEquals(computedHash, hashBytes);
                }
            }
            else if (parts.Length == 3)
            {
                // iterations:salt:hash format
                int iterations = int.Parse(parts[0]);
                string salt = parts[1];
                string hash = parts[2];
                
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] hashBytes = Convert.FromBase64String(hash);
                
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(hashBytes.Length);
                    return CryptographicEquals(computedHash, hashBytes);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Constant-time comparison to prevent timing attacks
        /// </summary>
        private static bool CryptographicEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            
            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }
        
        /// <summary>
        /// Tests the database connection
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = "";
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
        
        /// <summary>
        /// Debug method to get detailed login info
        /// </summary>
        public static string GetLoginDebugInfo(string email)
        {
            StringBuilder sb = new StringBuilder();
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    sb.AppendLine("✓ Database connected");
                    
                    // Get table structure info
                    string tableQuery = @"
                        SELECT column_name, data_type 
                        FROM information_schema.columns 
                        WHERE LOWER(table_name) = 'users'
                        ORDER BY ordinal_position";
                    
                    using (var cmd = new NpgsqlCommand(tableQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            sb.AppendLine("\nTable columns:");
                            while (reader.Read())
                            {
                                sb.AppendLine($"  - {reader["column_name"]}: {reader["data_type"]}");
                            }
                        }
                    }
                    
                    // Try to find the user
                    string[] userQueries = new string[]
                    {
                        "SELECT * FROM \"USERS\" WHERE email = @email LIMIT 1",
                        "SELECT * FROM \"USERS\" WHERE LOWER(email) = LOWER(@email) LIMIT 1",
                        "SELECT * FROM users WHERE email = @email LIMIT 1",
                        "SELECT * FROM users WHERE LOWER(email) = LOWER(@email) LIMIT 1"
                    };
                    
                    foreach (string query in userQueries)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@email", email.Trim());
                                
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        sb.AppendLine($"\n✓ User found with query: {query}");
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            string colName = reader.GetName(i);
                                            string value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "";
                                            
                                            // Mask password for display
                                            if (colName.ToLower().Contains("pass") || colName.ToLower().Contains("pwd"))
                                            {
                                                sb.AppendLine($"  - {colName}: [{value.Length} chars] = \"{value}\"");
                                            }
                                            else
                                            {
                                                sb.AppendLine($"  - {colName}: {value}");
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"\n✗ Query failed: {query}");
                            sb.AppendLine($"  Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"✗ Connection failed: {ex.Message}");
            }
            
            return sb.ToString();
        }
        
        #region Audit Logs Database Operations
        
        /// <summary>
        /// Creates all audit tables if they don't exist
        /// </summary>
        public static bool EnsureAllAuditTablesExist()
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Create web_logs table
                    string createWebLogsSql = @"
                        CREATE TABLE IF NOT EXISTS web_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            system_username VARCHAR(255),
                            machine_id VARCHAR(100),
                            browser_name VARCHAR(100),
                            website_url VARCHAR(2000),
                            page_title VARCHAR(500),
                            category VARCHAR(100),
                            visit_time TIMESTAMP WITH TIME ZONE,
                            duration_seconds INTEGER DEFAULT 0,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_web_logs_timestamp ON web_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_web_logs_activation_key ON web_logs(activation_key);
                        CREATE INDEX IF NOT EXISTS idx_web_logs_company ON web_logs(company_name);
                        CREATE INDEX IF NOT EXISTS idx_web_logs_system ON web_logs(system_name);
                    ";
                    
                    // Add system_username column if it doesn't exist
                    string alterWebLogsSql = @"
                        DO $$ 
                        BEGIN 
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                         WHERE table_name='web_logs' AND column_name='system_username') THEN
                                ALTER TABLE web_logs ADD COLUMN system_username VARCHAR(255);
                            END IF;
                        END $$;
                    ";
                    
                    // Create application_logs table
                    string createAppLogsSql = @"
                        CREATE TABLE IF NOT EXISTS application_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            system_username VARCHAR(255),
                            machine_id VARCHAR(100),
                            app_name VARCHAR(255),
                            window_title VARCHAR(500),
                            start_time TIMESTAMP WITH TIME ZONE,
                            end_time TIMESTAMP WITH TIME ZONE,
                            duration_seconds INTEGER DEFAULT 0,
                            is_active BOOLEAN DEFAULT FALSE,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_app_logs_timestamp ON application_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_app_logs_activation_key ON application_logs(activation_key);
                        CREATE INDEX IF NOT EXISTS idx_app_logs_company ON application_logs(company_name);
                        CREATE INDEX IF NOT EXISTS idx_app_logs_system ON application_logs(system_name);
                    ";
                    
                    // Create inactivity_logs table
                    string createInactivityLogsSql = @"
                        CREATE TABLE IF NOT EXISTS inactivity_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            system_username VARCHAR(255),
                            machine_id VARCHAR(100),
                            start_time TIMESTAMP WITH TIME ZONE,
                            end_time TIMESTAMP WITH TIME ZONE,
                            duration_seconds INTEGER DEFAULT 0,
                            status VARCHAR(50),
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_inactivity_logs_timestamp ON inactivity_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_inactivity_logs_activation_key ON inactivity_logs(activation_key);
                        CREATE INDEX IF NOT EXISTS idx_inactivity_logs_company ON inactivity_logs(company_name);
                        CREATE INDEX IF NOT EXISTS idx_inactivity_logs_system ON inactivity_logs(system_name);
                    ";
                    
                    // Create gps_logs table
                    string createGpsLogsSql = @"
                        CREATE TABLE IF NOT EXISTS gps_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            system_username VARCHAR(255),
                            machine_id VARCHAR(100),
                            latitude DECIMAL(10, 6),
                            longitude DECIMAL(10, 6),
                            city VARCHAR(100),
                            country VARCHAR(100),
                            isp VARCHAR(255),
                            accuracy VARCHAR(50),
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_gps_logs_timestamp ON gps_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_gps_logs_activation_key ON gps_logs(activation_key);
                        CREATE INDEX IF NOT EXISTS idx_gps_logs_company ON gps_logs(company_name);
                        CREATE INDEX IF NOT EXISTS idx_gps_logs_system ON gps_logs(system_name);
                    ";
                    
                    // Create punch_logs table for punch in/out tracking
                    string createPunchLogsSql = @"
                        CREATE TABLE IF NOT EXISTS punch_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            system_username VARCHAR(255),
                            machine_id VARCHAR(100),
                            punch_type VARCHAR(20),
                            punch_time TIMESTAMP WITH TIME ZONE,
                            punch_out_time TIMESTAMP WITH TIME ZONE,
                            total_duration_seconds INTEGER DEFAULT 0,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_punch_logs_timestamp ON punch_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_punch_logs_activation_key ON punch_logs(activation_key);
                        CREATE INDEX IF NOT EXISTS idx_punch_logs_company ON punch_logs(company_name);
                        CREATE INDEX IF NOT EXISTS idx_punch_logs_system ON punch_logs(system_name);
                    ";
                    
                    // Execute all table creation commands
                    using (var cmd = new NpgsqlCommand(createWebLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    using (var cmd = new NpgsqlCommand(alterWebLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    // Add system_username column to application_logs if it doesn't exist
                    string alterAppLogsSql = @"
                        DO $$ 
                        BEGIN 
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                         WHERE table_name='application_logs' AND column_name='system_username') THEN
                                ALTER TABLE application_logs ADD COLUMN system_username VARCHAR(255);
                            END IF;
                        END $$;
                    ";
                    using (var cmd = new NpgsqlCommand(createAppLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    using (var cmd = new NpgsqlCommand(alterAppLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    using (var cmd = new NpgsqlCommand(createInactivityLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    // Add system_username column to inactivity_logs if it doesn't exist
                    string alterInactivityLogsSql = @"
                        DO $$ 
                        BEGIN 
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                         WHERE table_name='inactivity_logs' AND column_name='system_username') THEN
                                ALTER TABLE inactivity_logs ADD COLUMN system_username VARCHAR(255);
                            END IF;
                        END $$;
                    ";
                    using (var cmd = new NpgsqlCommand(alterInactivityLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    using (var cmd = new NpgsqlCommand(createGpsLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    using (var cmd = new NpgsqlCommand(createPunchLogsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    // Screenshots table removed - screenshots now stored in cloud storage (Google Drive / OneDrive)
                    // User configures cloud storage in Screenshot Settings
                    
                    // Create system_registry table for tracking all registered systems
                    string createSystemRegistrySql = @"
                        CREATE TABLE IF NOT EXISTS system_registry (
                            id SERIAL PRIMARY KEY,
                            system_id VARCHAR(100) UNIQUE NOT NULL,
                            machine_id VARCHAR(100) UNIQUE NOT NULL,
                            system_name VARCHAR(255),
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            username VARCHAR(255),
                            system_username VARCHAR(255),
                            ip_address VARCHAR(50),
                            os_version VARCHAR(255),
                            processor VARCHAR(255),
                            ram_mb INTEGER,
                            first_seen TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            last_seen TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            is_online BOOLEAN DEFAULT TRUE,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_system_registry_system_id ON system_registry(system_id);
                        CREATE INDEX IF NOT EXISTS idx_system_registry_machine_id ON system_registry(machine_id);
                        CREATE INDEX IF NOT EXISTS idx_system_registry_company ON system_registry(company_name);
                        CREATE INDEX IF NOT EXISTS idx_system_registry_online ON system_registry(is_online);
                    ";
                    using (var cmd = new NpgsqlCommand(createSystemRegistrySql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    // Create live_sessions table for tracking active remote viewing sessions
                    string createLiveSessionsSql = @"
                        CREATE TABLE IF NOT EXISTS live_sessions (
                            id SERIAL PRIMARY KEY,
                            session_id VARCHAR(100) UNIQUE NOT NULL,
                            system_id VARCHAR(100) NOT NULL,
                            viewer_username VARCHAR(255),
                            started_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            ended_at TIMESTAMP WITH TIME ZONE,
                            is_active BOOLEAN DEFAULT TRUE,
                            last_frame_time TIMESTAMP WITH TIME ZONE,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (system_id) REFERENCES system_registry(system_id) ON DELETE CASCADE
                        );
                        CREATE INDEX IF NOT EXISTS idx_live_sessions_system_id ON live_sessions(system_id);
                        CREATE INDEX IF NOT EXISTS idx_live_sessions_active ON live_sessions(is_active);
                    ";
                    using (var cmd = new NpgsqlCommand(createLiveSessionsSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    // Create live_stream_frames table for storing screenshot frames for live viewing
                    string createLiveStreamFramesSql = @"
                        CREATE TABLE IF NOT EXISTS live_stream_frames (
                            id SERIAL PRIMARY KEY,
                            system_id VARCHAR(100) NOT NULL,
                            frame_data TEXT NOT NULL,
                            capture_time TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            screen_width INTEGER,
                            screen_height INTEGER,
                            expires_at TIMESTAMP WITH TIME ZONE DEFAULT (CURRENT_TIMESTAMP + INTERVAL '5 minutes'),
                            FOREIGN KEY (system_id) REFERENCES system_registry(system_id) ON DELETE CASCADE
                        );
                        CREATE INDEX IF NOT EXISTS idx_live_stream_frames_system_id ON live_stream_frames(system_id);
                        CREATE INDEX IF NOT EXISTS idx_live_stream_frames_capture_time ON live_stream_frames(capture_time DESC);
                        CREATE INDEX IF NOT EXISTS idx_live_stream_frames_expires_at ON live_stream_frames(expires_at);
                    ";
                    using (var cmd = new NpgsqlCommand(createLiveStreamFramesSql, connection)) { cmd.ExecuteNonQuery(); }
                    
                    // Add user details columns to log tables if they don't exist
                    AddUserDetailsColumnsToLogTables(connection);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create audit tables: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Add display_user_name, department, office_location columns to all log tables
        /// Call this to ensure columns exist before inserting
        /// </summary>
        public static void EnsureUserDetailsColumnsExist()
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    AddUserDetailsColumnsToLogTables(connection);
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Add display_user_name, department, office_location columns to all log tables
        /// </summary>
        private static void AddUserDetailsColumnsToLogTables(NpgsqlConnection connection)
        {
            string[] tables = { "web_logs", "application_logs", "inactivity_logs", "gps_logs" };
            string[] columns = { "display_user_name", "department", "office_location" };
            
            foreach (var table in tables)
            {
                foreach (var column in columns)
                {
                    try
                    {
                        string alterSql = $@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                    WHERE table_name = '{table}' AND column_name = '{column}') 
                                THEN 
                                    ALTER TABLE {table} ADD COLUMN {column} VARCHAR(200); 
                                END IF; 
                            END $$;";
                        using (var cmd = new NpgsqlCommand(alterSql, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column might already exist */ }
                }
            }
        }
        
        /// <summary>
        /// Get current user session details from UserSessionDetails class
        /// </summary>
        private static (string displayName, string department, string officeLocation) GetUserSessionDetails()
        {
            try
            {
                var session = UserSessionDetails.Instance;
                return (
                    session.SystemUserName ?? "",
                    session.Department ?? "",
                    session.OfficeLocation ?? ""
                );
            }
            catch
            {
                return ("", "", "");
            }
        }
        
        /// <summary>
        /// Insert web browsing log
        /// </summary>
        public static bool InsertWebLog(string? activationKey, string? companyName, string? username,
            string browserName, string url, string title, string category, DateTime visitTime)
        {
            try
            {
                var userDetails = GetUserSessionDetails();
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        INSERT INTO web_logs (activation_key, company_name, system_name, ip_address, username, system_username,
                            machine_id, browser_name, website_url, page_title, category, visit_time,
                            display_user_name, department, office_location)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @system_username,
                            @machine_id, @browser_name, @website_url, @page_title, @category, @visit_time,
                            @display_user_name, @department, @office_location)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@browser_name", browserName);
                        cmd.Parameters.AddWithValue("@website_url", url);
                        cmd.Parameters.AddWithValue("@page_title", title);
                        cmd.Parameters.AddWithValue("@category", category);
                        cmd.Parameters.AddWithValue("@visit_time", visitTime);
                        cmd.Parameters.AddWithValue("@display_user_name", string.IsNullOrEmpty(userDetails.displayName) ? DBNull.Value : userDetails.displayName);
                        cmd.Parameters.AddWithValue("@department", string.IsNullOrEmpty(userDetails.department) ? DBNull.Value : userDetails.department);
                        cmd.Parameters.AddWithValue("@office_location", string.IsNullOrEmpty(userDetails.officeLocation) ? DBNull.Value : userDetails.officeLocation);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch { return false; }
        }
        
        /// <summary>
        /// Batch insert web logs
        /// </summary>
        public static int InsertWebLogsBatch(string? activationKey, string? companyName, string? username,
            List<WebLogEntry> entries)
        {
            if (entries == null || entries.Count == 0) return 0;
            int inserted = 0;
            
            try
            {
                var userDetails = GetUserSessionDetails();
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
                                    INSERT INTO web_logs (activation_key, company_name, system_name, ip_address, username, system_username,
                                        machine_id, browser_name, website_url, page_title, category, visit_time,
                                        display_user_name, department, office_location)
                                    VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @system_username,
                                        @machine_id, @browser_name, @website_url, @page_title, @category, @visit_time,
                                        @display_user_name, @department, @office_location)";
                                
                                using (var cmd = new NpgsqlCommand(sql, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                                    cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                                    cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                                    cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                                    cmd.Parameters.AddWithValue("@browser_name", entry.Browser);
                                    cmd.Parameters.AddWithValue("@website_url", entry.Url);
                                    cmd.Parameters.AddWithValue("@page_title", entry.Title);
                                    cmd.Parameters.AddWithValue("@category", entry.Category);
                                    cmd.Parameters.AddWithValue("@visit_time", entry.VisitTime);
                                    cmd.Parameters.AddWithValue("@display_user_name", string.IsNullOrEmpty(userDetails.displayName) ? DBNull.Value : userDetails.displayName);
                                    cmd.Parameters.AddWithValue("@department", string.IsNullOrEmpty(userDetails.department) ? DBNull.Value : userDetails.department);
                                    cmd.Parameters.AddWithValue("@office_location", string.IsNullOrEmpty(userDetails.officeLocation) ? DBNull.Value : userDetails.officeLocation);
                                    cmd.ExecuteNonQuery();
                                    inserted++;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Web batch insert error for URL {entry.Url}: {ex.Message}");
                                // Log to file
                                try {
                                    string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DesktopController_Debug.txt");
                                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] WEB BATCH ERROR: {ex.Message}\\n");
                                } catch { }
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Web batch transaction error: {ex.Message}");
                try {
                    string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DesktopController_Debug.txt");
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] WEB BATCH TRANSACTION ERROR: {ex.Message}\\n");
                } catch { }
            }
            return inserted;
        }
        
        /// <summary>
        /// Insert application usage log
        /// </summary>
        public static bool InsertAppLog(string? activationKey, string? companyName, string? username,
            string appName, string windowTitle, DateTime startTime, DateTime endTime)
        {
            try
            {
                var userDetails = GetUserSessionDetails();
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    int duration = (int)(endTime - startTime).TotalSeconds;
                    string sql = @"
                        INSERT INTO application_logs (activation_key, company_name, system_name, ip_address, username, system_username,
                            machine_id, app_name, window_title, start_time, end_time, duration_seconds,
                            display_user_name, department, office_location)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @system_username,
                            @machine_id, @app_name, @window_title, @start_time, @end_time, @duration_seconds,
                            @display_user_name, @department, @office_location)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@app_name", appName);
                        cmd.Parameters.AddWithValue("@window_title", windowTitle);
                        cmd.Parameters.AddWithValue("@start_time", startTime);
                        cmd.Parameters.AddWithValue("@end_time", endTime);
                        cmd.Parameters.AddWithValue("@duration_seconds", duration);
                        cmd.Parameters.AddWithValue("@display_user_name", string.IsNullOrEmpty(userDetails.displayName) ? DBNull.Value : userDetails.displayName);
                        cmd.Parameters.AddWithValue("@department", string.IsNullOrEmpty(userDetails.department) ? DBNull.Value : userDetails.department);
                        cmd.Parameters.AddWithValue("@office_location", string.IsNullOrEmpty(userDetails.officeLocation) ? DBNull.Value : userDetails.officeLocation);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App log insert error: {ex.Message}");
                try {
                    string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DesktopController_Debug.txt");
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] APP INSERT ERROR: {ex.Message}\n");
                } catch { }
                return false;
            }
        }
        
        /// <summary>
        /// Insert inactivity log
        /// </summary>
        public static bool InsertInactivityLog(string? activationKey, string? companyName, string? username,
            DateTime startTime, DateTime endTime, string status)
        {
            try
            {
                var userDetails = GetUserSessionDetails();
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    int duration = (int)(endTime - startTime).TotalSeconds;
                    string sql = @"
                        INSERT INTO inactivity_logs (activation_key, company_name, system_name, ip_address, username, system_username,
                            machine_id, start_time, end_time, duration_seconds, status,
                            display_user_name, department, office_location)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @system_username,
                            @machine_id, @start_time, @end_time, @duration_seconds, @status,
                            @display_user_name, @department, @office_location)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@start_time", startTime);
                        cmd.Parameters.AddWithValue("@end_time", endTime);
                        cmd.Parameters.AddWithValue("@duration_seconds", duration);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@display_user_name", string.IsNullOrEmpty(userDetails.displayName) ? DBNull.Value : userDetails.displayName);
                        cmd.Parameters.AddWithValue("@department", string.IsNullOrEmpty(userDetails.department) ? DBNull.Value : userDetails.department);
                        cmd.Parameters.AddWithValue("@office_location", string.IsNullOrEmpty(userDetails.officeLocation) ? DBNull.Value : userDetails.officeLocation);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        // Log detailed info
                        System.Diagnostics.Debug.WriteLine($"Inactivity insert: {rowsAffected} rows affected");
                        
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the actual error to file so we can see it
                try {
                    string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DesktopController_Debug.txt");
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ❌ InsertInactivityLog ERROR: {ex.Message}\n");
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}]    Stack: {ex.StackTrace}\n");
                    if (ex.InnerException != null) {
                        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}]    Inner: {ex.InnerException.Message}\n");
                    }
                } catch { }
                throw; // Re-throw so caller can see the actual error
            }
        }
        
        /// <summary>
        /// Insert GPS location log
        /// </summary>
        public static bool InsertGpsLog(string? activationKey, string? companyName, string? username,
            double latitude, double longitude, string? city, string? country, string? isp)
        {
            try
            {
                var userDetails = GetUserSessionDetails();
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        INSERT INTO gps_logs (activation_key, company_name, system_name, ip_address, username, system_username,
                            machine_id, latitude, longitude, city, country, isp,
                            display_user_name, department, office_location)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username, @system_username,
                            @machine_id, @latitude, @longitude, @city, @country, @isp,
                            @display_user_name, @department, @office_location)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@latitude", (decimal)latitude);
                        cmd.Parameters.AddWithValue("@longitude", (decimal)longitude);
                        cmd.Parameters.AddWithValue("@city", (object?)city ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@country", (object?)country ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@isp", (object?)isp ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@display_user_name", string.IsNullOrEmpty(userDetails.displayName) ? DBNull.Value : userDetails.displayName);
                        cmd.Parameters.AddWithValue("@department", string.IsNullOrEmpty(userDetails.department) ? DBNull.Value : userDetails.department);
                        cmd.Parameters.AddWithValue("@office_location", string.IsNullOrEmpty(userDetails.officeLocation) ? DBNull.Value : userDetails.officeLocation);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch { return false; }
        }
        
        // InsertScreenshot method removed - screenshots now stored in cloud storage (Google Drive / OneDrive)
        // See CloudStorageHelper.cs for cloud upload functionality
        
        /// <summary>
        /// Insert punch in/out log
        /// </summary>
        public static bool InsertPunchLog(string? activationKey, string? companyName, string? username,
            string punchType, DateTime punchTime, DateTime? punchOutTime = null, int totalDuration = 0)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        INSERT INTO punch_logs (activation_key, company_name, system_name, ip_address, username,
                            machine_id, punch_type, punch_time, punch_out_time, total_duration_seconds)
                        VALUES (@activation_key, @company_name, @system_name, @ip_address, @username,
                            @machine_id, @punch_type, @punch_time, @punch_out_time, @total_duration_seconds)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@punch_type", punchType);
                        cmd.Parameters.AddWithValue("@punch_time", punchTime);
                        cmd.Parameters.AddWithValue("@punch_out_time", punchOutTime.HasValue ? punchOutTime.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@total_duration_seconds", totalDuration);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error to help debug
                try
                {
                    string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopController");
                    System.IO.Directory.CreateDirectory(logDir);
                    string logPath = System.IO.Path.Combine(logDir, "DatabaseError.log");
                    System.IO.File.AppendAllText(logPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - InsertPunchLog Error\n" +
                        $"  activation_key={activationKey ?? "NULL"}\n" +
                        $"  company_name={companyName ?? "NULL"}\n" +
                        $"  punch_type={punchType}\n" +
                        $"  Error: {ex.Message}\n" +
                        $"  Stack: {ex.StackTrace}\n");
                }
                catch { /* Ignore logging errors */ }
                return false;
            }
        }
        
        /// <summary>
        /// Insert or update punch in time for current session
        /// </summary>
        public static bool StartPunchSession(string? activationKey, string? companyName, string? username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Check if there's an open punch session
                    string checkSql = @"
                        SELECT id FROM punch_log_consolidated 
                        WHERE username = @username AND punch_out_time IS NULL 
                        ORDER BY id DESC LIMIT 1";
                    
                    using (var checkCmd = new NpgsqlCommand(checkSql, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        var result = checkCmd.ExecuteScalar();
                        
                        // If there's already an open session, don't create a new one
                        if (result != null) return false;
                    }
                    
                    // Insert new punch session
                    string sql = @"
                        INSERT INTO punch_log_consolidated 
                        (activation_key, company_name, username, system_name, ip_address, machine_id, punch_in_time)
                        VALUES (@activation_key, @company_name, @username, @system_name, @ip_address, @machine_id, @punch_in_time)";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", GetLocalIPAddress());
                        cmd.Parameters.AddWithValue("@machine_id", GetMachineId());
                        cmd.Parameters.AddWithValue("@punch_in_time", DateTime.Now);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDatabaseError("StartPunchSession", ex, $"username={username}");
                return false;
            }
        }
        
        /// <summary>
        /// Update punch out time and calculate work duration
        /// </summary>
        public static bool EndPunchSession(string? username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        UPDATE punch_log_consolidated 
                        SET punch_out_time = @punch_out_time,
                            total_work_duration_seconds = EXTRACT(EPOCH FROM (@punch_out_time - punch_in_time))::INTEGER,
                            updated_at = @updated_at
                        WHERE username = @username AND punch_out_time IS NULL
                        ORDER BY id DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@punch_out_time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDatabaseError("EndPunchSession", ex, $"username={username}");
                return false;
            }
        }
        
        /// <summary>
        /// Record break start time
        /// </summary>
        public static bool StartBreak(string? username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        UPDATE punch_log_consolidated 
                        SET break_start_time = @break_start_time,
                            updated_at = @updated_at
                        WHERE username = @username AND punch_out_time IS NULL
                        ORDER BY id DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@break_start_time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDatabaseError("StartBreak", ex, $"username={username}");
                return false;
            }
        }
        
        /// <summary>
        /// Record break end time and calculate break duration
        /// </summary>
        public static bool EndBreak(string? username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        UPDATE punch_log_consolidated 
                        SET break_end_time = @break_end_time,
                            break_duration_seconds = EXTRACT(EPOCH FROM (@break_end_time - break_start_time))::INTEGER,
                            updated_at = @updated_at
                        WHERE username = @username AND punch_out_time IS NULL
                        ORDER BY id DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@break_end_time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDatabaseError("EndBreak", ex, $"username={username}");
                return false;
            }
        }
        
        /// <summary>
        /// Helper method to log database errors
        /// </summary>
        private static void LogDatabaseError(string methodName, Exception ex, string details = "")
        {
            try
            {
                string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopController");
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = System.IO.Path.Combine(logDir, "DatabaseError.log");
                System.IO.File.AppendAllText(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {methodName} Error: {ex.Message}\n" +
                    $"  Details: {details}\n" +
                    $"  Stack: {ex.StackTrace}\n");
            }
            catch { /* Ignore logging errors */ }
        }
        
        /// <summary>
        /// Creates the audit_logs table if it doesn't exist
        /// </summary>
        public static bool EnsureAuditLogsTableExists()
        {
            // First ensure all specific tables exist
            EnsureAllAuditTablesExist();
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string createTableSql = @"
                        CREATE TABLE IF NOT EXISTS audit_logs (
                            id SERIAL PRIMARY KEY,
                            log_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            activation_key VARCHAR(255),
                            company_name VARCHAR(255),
                            system_name VARCHAR(255),
                            ip_address VARCHAR(50),
                            username VARCHAR(255),
                            machine_id VARCHAR(100),
                            log_type VARCHAR(50) NOT NULL,
                            log_category VARCHAR(50),
                            details TEXT,
                            start_time TIMESTAMP WITH TIME ZONE,
                            end_time TIMESTAMP WITH TIME ZONE,
                            duration_seconds INTEGER,
                            app_name VARCHAR(255),
                            window_title VARCHAR(500),
                            website_url VARCHAR(1000),
                            browser_name VARCHAR(100),
                            latitude DECIMAL(10, 6),
                            longitude DECIMAL(10, 6),
                            location_city VARCHAR(100),
                            location_country VARCHAR(100),
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        
                        -- Create indexes for better query performance
                        CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON audit_logs(log_timestamp);
                        CREATE INDEX IF NOT EXISTS idx_audit_logs_activation_key ON audit_logs(activation_key);
                        CREATE INDEX IF NOT EXISTS idx_audit_logs_company ON audit_logs(company_name);
                        CREATE INDEX IF NOT EXISTS idx_audit_logs_system ON audit_logs(system_name);
                        CREATE INDEX IF NOT EXISTS idx_audit_logs_type ON audit_logs(log_type);
                        CREATE INDEX IF NOT EXISTS idx_audit_logs_category ON audit_logs(log_category);
                    ";
                    
                    using (var cmd = new NpgsqlCommand(createTableSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create audit_logs table: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Inserts a general audit log entry
        /// </summary>
        public static bool InsertAuditLog(AuditLogEntry entry)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string insertSql = @"
                        INSERT INTO audit_logs (
                            log_timestamp, activation_key, company_name, system_name, ip_address,
                            username, machine_id, log_type, log_category, details,
                            start_time, end_time, duration_seconds, app_name, window_title,
                            website_url, browser_name, latitude, longitude, location_city, location_country
                        ) VALUES (
                            @timestamp, @activation_key, @company_name, @system_name, @ip_address,
                            @username, @machine_id, @log_type, @log_category, @details,
                            @start_time, @end_time, @duration_seconds, @app_name, @window_title,
                            @website_url, @browser_name, @latitude, @longitude, @location_city, @location_country
                        )";
                    
                    using (var cmd = new NpgsqlCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp);
                        cmd.Parameters.AddWithValue("@activation_key", (object?)entry.ActivationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)entry.CompanyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_name", (object?)entry.SystemName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ip_address", (object?)entry.IpAddress ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@username", (object?)entry.Username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@machine_id", (object?)entry.MachineId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@log_type", entry.LogType);
                        cmd.Parameters.AddWithValue("@log_category", (object?)entry.LogCategory ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@details", (object?)entry.Details ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@start_time", entry.StartTime.HasValue ? entry.StartTime.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@end_time", entry.EndTime.HasValue ? entry.EndTime.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@duration_seconds", entry.DurationSeconds.HasValue ? entry.DurationSeconds.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@app_name", (object?)entry.AppName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@window_title", (object?)entry.WindowTitle ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@website_url", (object?)entry.WebsiteUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@browser_name", (object?)entry.BrowserName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@latitude", entry.Latitude.HasValue ? entry.Latitude.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@longitude", entry.Longitude.HasValue ? entry.Longitude.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@location_city", (object?)entry.LocationCity ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@location_country", (object?)entry.LocationCountry ?? DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to insert audit log: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Batch insert multiple audit log entries
        /// </summary>
        public static bool InsertAuditLogsBatch(List<AuditLogEntry> entries)
        {
            if (entries == null || entries.Count == 0) return true;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var entry in entries)
                            {
                                string insertSql = @"
                                    INSERT INTO audit_logs (
                                        log_timestamp, activation_key, company_name, system_name, ip_address,
                                        username, machine_id, log_type, log_category, details,
                                        start_time, end_time, duration_seconds, app_name, window_title,
                                        website_url, browser_name, latitude, longitude, location_city, location_country
                                    ) VALUES (
                                        @timestamp, @activation_key, @company_name, @system_name, @ip_address,
                                        @username, @machine_id, @log_type, @log_category, @details,
                                        @start_time, @end_time, @duration_seconds, @app_name, @window_title,
                                        @website_url, @browser_name, @latitude, @longitude, @location_city, @location_country
                                    )";
                                
                                using (var cmd = new NpgsqlCommand(insertSql, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp);
                                    cmd.Parameters.AddWithValue("@activation_key", (object?)entry.ActivationKey ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@company_name", (object?)entry.CompanyName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@system_name", (object?)entry.SystemName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ip_address", (object?)entry.IpAddress ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@username", (object?)entry.Username ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@machine_id", (object?)entry.MachineId ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@log_type", entry.LogType);
                                    cmd.Parameters.AddWithValue("@log_category", (object?)entry.LogCategory ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@details", (object?)entry.Details ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@start_time", entry.StartTime.HasValue ? entry.StartTime.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@end_time", entry.EndTime.HasValue ? entry.EndTime.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@duration_seconds", entry.DurationSeconds.HasValue ? entry.DurationSeconds.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@app_name", (object?)entry.AppName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@window_title", (object?)entry.WindowTitle ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@website_url", (object?)entry.WebsiteUrl ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@browser_name", (object?)entry.BrowserName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@latitude", entry.Latitude.HasValue ? entry.Latitude.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@longitude", entry.Longitude.HasValue ? entry.Longitude.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@location_city", (object?)entry.LocationCity ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@location_country", (object?)entry.LocationCountry ?? DBNull.Value);
                                    
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            
                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to batch insert audit logs: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Helper method to log Punch In event
        /// </summary>
        public static void LogPunchIn(string? activationKey, string? companyName, string? username)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = "PUNCH_IN",
                LogCategory = "TIME_TRACKING",
                Details = $"User punched in at {DateTime.Now:HH:mm:ss}"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        /// <summary>
        /// Helper method to log Punch Out event
        /// </summary>
        public static void LogPunchOut(string? activationKey, string? companyName, string? username, DateTime punchInTime)
        {
            var duration = DateTime.Now - punchInTime;
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = "PUNCH_OUT",
                LogCategory = "TIME_TRACKING",
                StartTime = punchInTime,
                EndTime = DateTime.Now,
                DurationSeconds = (int)duration.TotalSeconds,
                Details = $"User punched out. Total work time: {duration:hh\\:mm\\:ss}"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        /// <summary>
        /// Helper method to log inactivity period
        /// </summary>
        public static void LogInactivity(string? activationKey, string? companyName, string? username, 
            DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = "INACTIVITY",
                LogCategory = "ACTIVITY_TRACKING",
                StartTime = startTime,
                EndTime = endTime,
                DurationSeconds = (int)duration.TotalSeconds,
                Details = $"Inactivity period detected: {duration:hh\\:mm\\:ss}"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        /// <summary>
        /// Helper method to log application usage
        /// </summary>
        public static void LogAppUsage(string? activationKey, string? companyName, string? username,
            string appName, string windowTitle, DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = "APP_USAGE",
                LogCategory = "ACTIVITY_TRACKING",
                AppName = appName,
                WindowTitle = windowTitle,
                StartTime = startTime,
                EndTime = endTime,
                DurationSeconds = (int)duration.TotalSeconds,
                Details = $"Used {appName} for {duration:hh\\:mm\\:ss}"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        /// <summary>
        /// Helper method to log login event
        /// </summary>
        public static void LogLogin(string? activationKey, string? companyName, string? username, bool success)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
                LogCategory = "AUTHENTICATION",
                Details = success ? $"User {username} logged in successfully" : $"Failed login attempt for user {username}"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        /// <summary>
        /// Helper method to log system control action
        /// </summary>
        public static void LogSystemControl(string? activationKey, string? companyName, string? username,
            string controlName, string action)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = "SYSTEM_CONTROL",
                LogCategory = "CONTROL_ACTION",
                Details = $"{controlName}: {action}"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        /// <summary>
        /// Helper method to log GPS location
        /// </summary>
        public static void LogLocation(string? activationKey, string? companyName, string? username,
            double latitude, double longitude, string? city, string? country)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                ActivationKey = activationKey,
                CompanyName = companyName,
                SystemName = Environment.MachineName,
                IpAddress = GetLocalIPAddress(),
                Username = username,
                MachineId = GetMachineId(),
                LogType = "GPS_LOCATION",
                LogCategory = "LOCATION_TRACKING",
                Latitude = (decimal)latitude,
                Longitude = (decimal)longitude,
                LocationCity = city,
                LocationCountry = country,
                Details = $"Location: {city}, {country} ({latitude:F6}, {longitude:F6})"
            };
            
            Task.Run(() => InsertAuditLog(entry));
        }
        
        public static string GetLocalIPAddress()
        {
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                        ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }
        
        #region User Details and Office Locations
        
        /// <summary>
        /// Get office locations for a company from database
        /// </summary>
        public static List<string> GetOfficeLocations(string activationKey)
        {
            var locations = new List<string>();
            
            if (string.IsNullOrEmpty(activationKey)) return locations;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // First get company ID
                    string sqlCompany = @"SELECT id FROM companies WHERE activation_key = @activation_key AND is_active = TRUE LIMIT 1";
                    int? companyId = null;
                    
                    using (var cmd = new NpgsqlCommand(sqlCompany, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            companyId = Convert.ToInt32(result);
                    }
                    
                    if (companyId.HasValue)
                    {
                        // Try to get office locations from office_locations table
                        string sql = @"
                            SELECT location_name 
                            FROM office_locations 
                            WHERE company_id = @company_id 
                            AND is_active = TRUE 
                            ORDER BY location_name";
                        
                        using (var cmd = new NpgsqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@company_id", companyId.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string locName = reader["location_name"]?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(locName))
                                        locations.Add(locName);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            
            return locations;
        }
        
        /// <summary>
        /// Save user details to database
        /// </summary>
        public static void SaveUserDetails(string activationKey, string loginUsername, string systemUserName, string department, string officeLocation)
        {
            if (string.IsNullOrEmpty(activationKey)) return;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // First get company ID
                    string sqlCompany = @"SELECT id FROM companies WHERE activation_key = @activation_key AND is_active = TRUE LIMIT 1";
                    int? companyId = null;
                    
                    using (var cmd = new NpgsqlCommand(sqlCompany, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            companyId = Convert.ToInt32(result);
                    }
                    
                    if (companyId.HasValue)
                    {
                        // Ensure user_details table exists
                        string createTableSql = @"
                            CREATE TABLE IF NOT EXISTS user_details (
                                id SERIAL PRIMARY KEY,
                                company_id INTEGER REFERENCES companies(id),
                                login_username VARCHAR(255),
                                system_user_name VARCHAR(255),
                                department VARCHAR(255),
                                office_location VARCHAR(255),
                                machine_id VARCHAR(255),
                                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                UNIQUE(company_id, login_username, machine_id)
                            )";
                        using (var cmd = new NpgsqlCommand(createTableSql, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Upsert user details
                        string machineId = GetMachineId();
                        string sql = @"
                            INSERT INTO user_details (company_id, login_username, system_user_name, department, office_location, machine_id, updated_at)
                            VALUES (@company_id, @login_username, @system_user_name, @department, @office_location, @machine_id, CURRENT_TIMESTAMP)
                            ON CONFLICT (company_id, login_username, machine_id) 
                            DO UPDATE SET 
                                system_user_name = @system_user_name,
                                department = @department,
                                office_location = @office_location,
                                updated_at = CURRENT_TIMESTAMP";
                        
                        using (var cmd = new NpgsqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@company_id", companyId.Value);
                            cmd.Parameters.AddWithValue("@login_username", loginUsername);
                            cmd.Parameters.AddWithValue("@system_user_name", systemUserName);
                            cmd.Parameters.AddWithValue("@department", department);
                            cmd.Parameters.AddWithValue("@office_location", officeLocation);
                            cmd.Parameters.AddWithValue("@machine_id", machineId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Get user details from database
        /// </summary>
        public static (string systemUserName, string department, string officeLocation)? GetUserDetails(string activationKey, string loginUsername)
        {
            if (string.IsNullOrEmpty(activationKey)) return null;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    string sqlCompany = @"SELECT id FROM companies WHERE activation_key = @activation_key AND is_active = TRUE LIMIT 1";
                    int? companyId = null;
                    
                    using (var cmd = new NpgsqlCommand(sqlCompany, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            companyId = Convert.ToInt32(result);
                    }
                    
                    if (companyId.HasValue)
                    {
                        string machineId = GetMachineId();
                        string sql = @"
                            SELECT system_user_name, department, office_location 
                            FROM user_details 
                            WHERE company_id = @company_id 
                            AND login_username = @login_username
                            AND machine_id = @machine_id
                            LIMIT 1";
                        
                        using (var cmd = new NpgsqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@company_id", companyId.Value);
                            cmd.Parameters.AddWithValue("@login_username", loginUsername);
                            cmd.Parameters.AddWithValue("@machine_id", machineId);
                            
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return (
                                        reader["system_user_name"]?.ToString() ?? "",
                                        reader["department"]?.ToString() ?? "",
                                        reader["office_location"]?.ToString() ?? ""
                                    );
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        #endregion
        
        #region Auto-Login and Password Verification
        
        /// <summary>
        /// Get master password for company (from activation settings)
        /// </summary>
        public static string? GetMasterPassword(string activationKey)
        {
            if (string.IsNullOrEmpty(activationKey)) return null;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Try to get master password from companies table
                    string sql = @"
                        SELECT master_password 
                        FROM companies 
                        WHERE activation_key = @activation_key 
                        AND is_active = TRUE
                        LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return result.ToString();
                        }
                    }
                }
            }
            catch { }
            
            // Default master password if not set in database
            return "Admin@123";
        }
        
        /// <summary>
        /// Verify user password against database
        /// </summary>
        public static bool VerifyUserPassword(string activationKey, string username, string password)
        {
            if (string.IsNullOrEmpty(activationKey) || string.IsNullOrEmpty(username)) return false;
            
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Get company_id from activation key
                    string sqlCompany = @"
                        SELECT id FROM companies 
                        WHERE activation_key = @activation_key 
                        AND is_active = TRUE 
                        LIMIT 1";
                    
                    int? companyId = null;
                    using (var cmd = new NpgsqlCommand(sqlCompany, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            companyId = Convert.ToInt32(result);
                        }
                    }
                    
                    if (companyId.HasValue)
                    {
                        // Check password hash
                        string sql = @"
                            SELECT password_hash 
                            FROM users 
                            WHERE company_id = @company_id 
                            AND name = @username 
                            AND is_active = TRUE 
                            LIMIT 1";
                        
                        using (var cmd = new NpgsqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@company_id", companyId.Value);
                            cmd.Parameters.AddWithValue("@username", username);
                            var result = cmd.ExecuteScalar();
                            
                            if (result != null && result != DBNull.Value)
                            {
                                string storedHash = result.ToString() ?? "";
                                
                                // Verify password (check plain text or hash)
                                if (storedHash == password)
                                    return true;
                                    
                                // Check against hash
                                string inputHash = HashPasswordInternal(password);
                                return storedHash == inputHash;
                            }
                        }
                    }
                }
            }
            catch { }
            
            return false;
        }
        
        /// <summary>
        /// Hash password using SHA256
        /// </summary>
        private static string HashPasswordInternal(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        /// <summary>
        /// Save auto-login credentials
        /// </summary>
        public static void SaveAutoLoginCredentials(string activationKey, string username, string password)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{activationKey}|{username}|{password}"));
                        key.SetValue("AutoLoginCredentials", encoded);
                        key.SetValue("AutoLogin", "1");
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Get auto-login credentials
        /// </summary>
        public static (string activationKey, string username, string password)? GetAutoLoginCredentials()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        string? autoLogin = key.GetValue("AutoLogin")?.ToString();
                        if (autoLogin == "1")
                        {
                            string? encoded = key.GetValue("AutoLoginCredentials")?.ToString();
                            if (!string.IsNullOrEmpty(encoded))
                            {
                                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                                string[] parts = decoded.Split('|');
                                if (parts.Length == 3)
                                {
                                    return (parts[0], parts[1], parts[2]);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }
        
        /// <summary>
        /// Clear auto-login credentials
        /// </summary>
        public static void ClearAutoLoginCredentials()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("AutoLoginCredentials", false);
                        key.DeleteValue("AutoLogin", false);
                    }
                }
            }
            catch { }
        }
        
        #endregion
        
        #region Company Password Management
        
        /// <summary>
        /// Get company password hash from database
        /// </summary>
        public static string? GetCompanyPassword(string companyName)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Ensure table exists
                    string createTable = @"
                        CREATE TABLE IF NOT EXISTS company_passwords (
                            id SERIAL PRIMARY KEY,
                            company_name VARCHAR(255) UNIQUE NOT NULL,
                            password_hash VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
                    using (var cmd = new NpgsqlCommand(createTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    
                    // Get password hash (case-insensitive match)
                    string sql = "SELECT password_hash FROM company_passwords WHERE LOWER(company_name) = LOWER(@company_name)";
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company_name", companyName.Trim());
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Set company password in database
        /// </summary>
        public static bool SetCompanyPassword(string companyName, string passwordHash)
        {
            try
            {
                using (var connection = new NpgsqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    
                    // Ensure table exists
                    string createTable = @"
                        CREATE TABLE IF NOT EXISTS company_passwords (
                            id SERIAL PRIMARY KEY,
                            company_name VARCHAR(255) UNIQUE NOT NULL,
                            password_hash VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
                    using (var cmd = new NpgsqlCommand(createTable, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    
                    // First try to find existing entry (case-insensitive)
                    string findSql = "SELECT company_name FROM company_passwords WHERE LOWER(company_name) = LOWER(@company_name)";
                    string? existingName = null;
                    using (var cmd = new NpgsqlCommand(findSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@company_name", companyName.Trim());
                        existingName = cmd.ExecuteScalar()?.ToString();
                    }
                    
                    if (existingName != null)
                    {
                        // Update existing record
                        string updateSql = "UPDATE company_passwords SET password_hash = @password_hash, updated_at = CURRENT_TIMESTAMP WHERE company_name = @existing_name";
                        using (var cmd = new NpgsqlCommand(updateSql, connection))
                        {
                            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                            cmd.Parameters.AddWithValue("@existing_name", existingName);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insert new record
                        string insertSql = "INSERT INTO company_passwords (company_name, password_hash, updated_at) VALUES (@company_name, @password_hash, CURRENT_TIMESTAMP)";
                        using (var cmd = new NpgsqlCommand(insertSql, connection))
                        {
                            cmd.Parameters.AddWithValue("@company_name", companyName.Trim());
                            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        #endregion
    }
    
    /// <summary>
    /// Represents an audit log entry for database storage
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? ActivationKey { get; set; }
        public string? CompanyName { get; set; }
        public string? SystemName { get; set; }
        public string? IpAddress { get; set; }
        public string? Username { get; set; }
        public string? MachineId { get; set; }
        public string LogType { get; set; } = "";  // PUNCH_IN, PUNCH_OUT, INACTIVITY, APP_USAGE, LOGIN, SYSTEM_CONTROL, GPS_LOCATION
        public string? LogCategory { get; set; }   // TIME_TRACKING, ACTIVITY_TRACKING, AUTHENTICATION, CONTROL_ACTION, LOCATION_TRACKING
        public string? Details { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationSeconds { get; set; }
        public string? AppName { get; set; }
        public string? WindowTitle { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? BrowserName { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? LocationCity { get; set; }
        public string? LocationCountry { get; set; }
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
        public DateTime VisitTime { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Extension methods for remote viewing support
    /// </summary>
    public static class RemoteViewingHelper
    {
        /// <summary>
        /// Register or update system in the system_registry table
        /// </summary>
        /// <summary>
        /// Gets all registered systems from database
        /// Returns list of systems with System ID, name, IP, machine ID, online status, last seen
        /// </summary>
        public static List<(string systemId, string systemName, string ipAddress, string machineId, bool isOnline, DateTime lastSeen, string username, string companyName)> GetAllRegisteredSystems()
        {
            var systems = new List<(string, string, string, string, bool, DateTime, string, string)>();
            
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    string sql = @"
                        SELECT system_id, system_name, ip_address, machine_id, is_online, 
                               last_seen, username, company_name
                        FROM system_registry
                        ORDER BY last_seen DESC";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            systems.Add((
                                reader.GetString(0), // system_id
                                reader.IsDBNull(1) ? "" : reader.GetString(1), // system_name
                                reader.IsDBNull(2) ? "" : reader.GetString(2), // ip_address
                                reader.IsDBNull(3) ? "" : reader.GetString(3), // machine_id
                                reader.IsDBNull(4) ? false : reader.GetBoolean(4), // is_online
                                reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5), // last_seen
                                reader.IsDBNull(6) ? "" : reader.GetString(6), // username
                                reader.IsDBNull(7) ? "" : reader.GetString(7)  // company_name
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Error fetching systems - log to console
                System.Diagnostics.Debug.WriteLine($"Error fetching registered systems: {ex.Message}");
            }
            
            return systems;
        }
        
        public static string RegisterSystem(string? activationKey, string? companyName, string? username)
        {
            try
            {
                // Get or create persistent hardware-based system ID (like AnyDesk ID: 123-456-789)
                // GUARANTEED UNIQUE per physical machine
                string systemId = DatabaseHelper.GetOrCreateSystemId();
                string machineId = DatabaseHelper.GetMachineId();
                
                // Get system info
                string osVersion = Environment.OSVersion.ToString();
                string processor = "";
                int ramMb = 0;
                
                try
                {
                    using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            processor = obj["Name"]?.ToString() ?? "";
                            break;
                        }
                    }
                    
                    using (var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
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
                
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    // Insert or update system registry
                    string sql = @"
                        INSERT INTO system_registry (system_id, machine_id, system_name, activation_key, 
                            company_name, username, system_username, ip_address, os_version, processor, ram_mb, is_online)
                        VALUES (@system_id, @machine_id, @system_name, @activation_key, 
                            @company_name, @username, @system_username, @ip_address, @os_version, @processor, @ram_mb, TRUE)
                        ON CONFLICT (system_id) DO UPDATE SET
                            last_seen = CURRENT_TIMESTAMP,
                            is_online = TRUE,
                            ip_address = @ip_address,
                            username = @username,
                            activation_key = @activation_key,
                            company_name = @company_name";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        cmd.Parameters.AddWithValue("@machine_id", machineId);
                        cmd.Parameters.AddWithValue("@system_name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@activation_key", (object?)activationKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@company_name", (object?)companyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@system_username", Environment.UserName);
                        cmd.Parameters.AddWithValue("@ip_address", DatabaseHelper.GetLocalIPAddress());
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
                System.Diagnostics.Debug.WriteLine($"Failed to register system: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// Update system heartbeat (mark as online)
        /// </summary>
        public static void UpdateSystemHeartbeat(string systemId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        UPDATE system_registry 
                        SET last_seen = CURRENT_TIMESTAMP, 
                            is_online = TRUE,
                            ip_address = @ip_address
                        WHERE system_id = @system_id";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        cmd.Parameters.AddWithValue("@ip_address", DatabaseHelper.GetLocalIPAddress());
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
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    // First, delete old frames (older than 5 minutes) to save space
                    string deleteSql = "DELETE FROM live_stream_frames WHERE expires_at < CURRENT_TIMESTAMP";
                    using (var delCmd = new NpgsqlCommand(deleteSql, connection))
                    {
                        delCmd.ExecuteNonQuery();
                    }
                    
                    // Keep only latest 3 frames per system for smooth streaming
                    string deleteOldSql = @"
                        DELETE FROM live_stream_frames 
                        WHERE system_id = @system_id 
                        AND id NOT IN (
                            SELECT id FROM live_stream_frames 
                            WHERE system_id = @system_id 
                            ORDER BY capture_time DESC 
                            LIMIT 3
                        )";
                    using (var delCmd = new NpgsqlCommand(deleteOldSql, connection))
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
                        cmd.ExecuteNonQuery();
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to insert live stream frame: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get latest live stream frame for a system
        /// </summary>
        public static (string frameData, DateTime captureTime, int width, int height)? GetLatestLiveFrame(string systemId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        SELECT frame_data, capture_time, screen_width, screen_height
                        FROM live_stream_frames
                        WHERE system_id = @system_id
                        ORDER BY capture_time DESC
                        LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return (
                                    reader["frame_data"].ToString() ?? "",
                                    Convert.ToDateTime(reader["capture_time"]),
                                    Convert.ToInt32(reader["screen_width"]),
                                    Convert.ToInt32(reader["screen_height"])
                                );
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }
        
        /// <summary>
        /// Check if there's an active viewing session for this system
        /// </summary>
        public static bool HasActiveViewingSession(string systemId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        SELECT COUNT(*) 
                        FROM live_sessions 
                        WHERE system_id = @system_id 
                        AND is_active = TRUE 
                        AND started_at > (CURRENT_TIMESTAMP - INTERVAL '10 minutes')";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@system_id", systemId);
                        long count = (long)(cmd.ExecuteScalar() ?? 0L);
                        return count > 0;
                    }
                }
            }
            catch { }
            return false;
        }
        
        /// <summary>
        /// Ensure company_stream_passwords table exists for company-specific stream passwords
        /// </summary>
        private static void EnsureCompanyStreamPasswordTableExists()
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string createTableSql = @"
                        CREATE TABLE IF NOT EXISTS company_stream_passwords (
                            id SERIAL PRIMARY KEY,
                            activation_key VARCHAR(255) UNIQUE NOT NULL,
                            company_name VARCHAR(255),
                            stream_password VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                        CREATE INDEX IF NOT EXISTS idx_company_stream_pwd_activation ON company_stream_passwords(activation_key);
                    ";
                    using (var cmd = new NpgsqlCommand(createTableSql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Save stream password for a company (activation key specific)
        /// </summary>
        public static bool SaveStreamPassword(string activationKey, string password)
        {
            try
            {
                // First ensure the table exists
                EnsureCompanyStreamPasswordTableExists();
                
                // Hash the password before storing
                string hashedPassword = HashStreamPassword(password);
                
                // Get company name from registry
                string companyName = "";
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                    {
                        companyName = key?.GetValue("CompanyName")?.ToString() ?? "";
                    }
                }
                catch { }
                
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    // Upsert - insert or update if exists
                    string sql = @"
                        INSERT INTO company_stream_passwords (activation_key, company_name, stream_password, updated_at)
                        VALUES (@activation_key, @company_name, @stream_password, CURRENT_TIMESTAMP)
                        ON CONFLICT (activation_key) 
                        DO UPDATE SET stream_password = @stream_password, updated_at = CURRENT_TIMESTAMP";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.Parameters.AddWithValue("@company_name", companyName);
                        cmd.Parameters.AddWithValue("@stream_password", hashedPassword);
                        cmd.ExecuteNonQuery();
                        
                        // Also save to registry for local access (don't save plain password, just hash)
                        SaveStreamPasswordHashToRegistry(hashedPassword);
                        
                        return true;
                    }
                }
            }
            catch { return false; }
        }
        
        /// <summary>
        /// Verify stream password for a company (activation key specific)
        /// </summary>
        public static bool VerifyStreamPassword(string activationKey, string password)
        {
            try
            {
                // First check local registry (faster)
                string localHash = GetStreamPasswordHashFromRegistry();
                if (!string.IsNullOrEmpty(localHash))
                {
                    string inputHash = HashStreamPassword(password);
                    if (localHash == inputHash)
                        return true;
                }
                
                // Fallback to database
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        SELECT stream_password 
                        FROM company_stream_passwords 
                        WHERE activation_key = @activation_key";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        
                        if (result == null || result == DBNull.Value)
                            return false; // No password set
                        
                        string storedHash = result.ToString() ?? "";
                        if (string.IsNullOrEmpty(storedHash))
                            return false; // No password set
                        
                        // Verify password
                        string inputHash = HashStreamPassword(password);
                        return storedHash == inputHash;
                    }
                }
            }
            catch { return false; }
        }
        
        /// <summary>
        /// Check if a company has stream password set (by activation key)
        /// </summary>
        public static bool HasStreamPassword(string activationKey)
        {
            try
            {
                // First check local registry (faster)
                string localHash = GetStreamPasswordHashFromRegistry();
                if (!string.IsNullOrEmpty(localHash))
                    return true;
                
                // Fallback to database
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        SELECT stream_password 
                        FROM company_stream_passwords 
                        WHERE activation_key = @activation_key";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        var result = cmd.ExecuteScalar();
                        
                        if (result == null || result == DBNull.Value)
                            return false;
                        
                        string storedHash = result.ToString() ?? "";
                        return !string.IsNullOrEmpty(storedHash);
                    }
                }
            }
            catch { return false; }
        }
        
        /// <summary>
        /// Get stream password hash from local registry
        /// </summary>
        private static string GetStreamPasswordHashFromRegistry()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        return key.GetValue("StreamPasswordHash")?.ToString() ?? "";
                    }
                }
            }
            catch { }
            return "";
        }
        
        /// <summary>
        /// Get stream password from local registry (for display - returns empty since we don't store plain text)
        /// </summary>
        public static string GetStreamPasswordFromRegistry()
        {
            // We don't store plain text password, so always return empty
            // The UI should check HasStreamPassword instead
            return "";
        }
        
        /// <summary>
        /// Save stream password hash to local registry
        /// </summary>
        private static void SaveStreamPasswordHashToRegistry(string hash)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DesktopController"))
                {
                    if (key != null)
                    {
                        if (string.IsNullOrEmpty(hash))
                        {
                            key.DeleteValue("StreamPasswordHash", false);
                        }
                        else
                        {
                            key.SetValue("StreamPasswordHash", hash);
                        }
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Clear stream password for a company
        /// </summary>
        public static bool ClearStreamPassword(string activationKey)
        {
            try
            {
                using (var connection = new NpgsqlConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string sql = @"
                        DELETE FROM company_stream_passwords 
                        WHERE activation_key = @activation_key";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@activation_key", activationKey);
                        cmd.ExecuteNonQuery();
                        
                        // Also clear from registry
                        SaveStreamPasswordHashToRegistry("");
                        
                        return true;
                    }
                }
            }
            catch { return false; }
        }
        
        /// <summary>
        /// Hash stream password using SHA256
        /// </summary>
        private static string HashStreamPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "StreamSalt_DC2025"));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}