using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopController
{
    /// <summary>
    /// Helper class for uploading screenshots directly to cloud storage (Google Drive / OneDrive)
    /// Supports direct API upload to user's specific folder link
    /// </summary>
    public static class CloudStorageHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        private static string? _cachedAccessToken = null;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        
        // Google OAuth2 credentials - using installed app flow
        // You can get these from Google Cloud Console
        private const string GoogleClientId = "1055712273793-qqv6r0q8k5r1q0k5r1q0k5r1q0k5r1q0.apps.googleusercontent.com";
        private const string GoogleClientSecret = "GOCSPX-placeholder";
        
        // Registry path for storing tokens
        private const string TokenRegistryPath = @"SOFTWARE\DesktopController\CloudTokens";
        
        /// <summary>
        /// Upload screenshot to configured cloud storage - DIRECT to user's folder link
        /// </summary>
        public static async Task<bool> UploadScreenshotAsync(byte[] imageBytes, string fileName, ScreenshotSettings settings)
        {
            if (string.IsNullOrEmpty(settings.CloudLink))
            {
                LogError("No cloud link configured");
                return false;
            }
            
            try
            {
                LogInfo($"Uploading {fileName} to {settings.StorageType}...");
                
                return settings.StorageType switch
                {
                    "googledrive" => await UploadToGoogleDriveDirectAsync(imageBytes, fileName, settings.CloudLink),
                    "onedrive" => await UploadToOneDriveAsync(imageBytes, fileName, settings.CloudLink),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                LogError($"Cloud upload failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Upload directly to Google Drive using the folder ID from user's link
        /// </summary>
        private static async Task<bool> UploadToGoogleDriveDirectAsync(byte[] imageBytes, string fileName, string folderLink)
        {
            try
            {
                // Extract folder ID from the link
                string? folderId = ExtractGoogleDriveFolderId(folderLink);
                
                if (string.IsNullOrEmpty(folderId))
                {
                    LogError($"Could not extract folder ID from: {folderLink}");
                    return await SaveToLocalFallback(imageBytes, fileName, "GoogleDrive");
                }
                
                LogInfo($"Target folder ID: {folderId}");
                
                // Get access token
                string? accessToken = await GetGoogleAccessTokenAsync();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    LogInfo("No Google authorization - please click 'Authorize Google Drive' in Screenshot Settings");
                    return await SaveToLocalFallback(imageBytes, fileName, "GoogleDrive");
                }
                
                // Upload using multipart upload API
                bool success = await UploadFileToGoogleDrive(imageBytes, fileName, folderId, accessToken);
                
                if (success)
                {
                    LogInfo($"✓ Screenshot uploaded directly to Google Drive folder: {fileName}");
                    return true;
                }
                else
                {
                    LogError("Upload failed - saving locally as fallback");
                    return await SaveToLocalFallback(imageBytes, fileName, "GoogleDrive");
                }
            }
            catch (Exception ex)
            {
                LogError($"Google Drive direct upload error: {ex.Message}");
                return await SaveToLocalFallback(imageBytes, fileName, "GoogleDrive");
            }
        }
        
        /// <summary>
        /// Upload file to Google Drive using REST API (multipart upload)
        /// </summary>
        private static async Task<bool> UploadFileToGoogleDrive(byte[] fileBytes, string fileName, string folderId, string accessToken)
        {
            try
            {
                // Create metadata JSON
                string metadataJson = JsonSerializer.Serialize(new 
                { 
                    name = fileName, 
                    parents = new[] { folderId } 
                });
                
                // Build multipart request manually
                string boundary = "===screenshot_boundary_" + Guid.NewGuid().ToString("N") + "===";
                
                using var content = new MemoryStream();
                using var writer = new StreamWriter(content, Encoding.UTF8, leaveOpen: true);
                
                // Metadata part
                await writer.WriteAsync($"--{boundary}\r\n");
                await writer.WriteAsync("Content-Type: application/json; charset=UTF-8\r\n\r\n");
                await writer.WriteAsync(metadataJson);
                await writer.WriteAsync("\r\n");
                
                // File part
                await writer.WriteAsync($"--{boundary}\r\n");
                await writer.WriteAsync("Content-Type: image/jpeg\r\n\r\n");
                await writer.FlushAsync();
                
                await content.WriteAsync(fileBytes, 0, fileBytes.Length);
                
                await writer.WriteAsync($"\r\n--{boundary}--\r\n");
                await writer.FlushAsync();
                
                content.Position = 0;
                
                // Create request
                using var request = new HttpRequestMessage(HttpMethod.Post, 
                    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StreamContent(content);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue($"multipart/related");
                request.Content.Headers.ContentType.Parameters.Add(
                    new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary));
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    LogInfo($"Upload successful!");
                    return true;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    LogError($"Upload failed ({response.StatusCode}): {error}");
                    
                    // If unauthorized, clear token
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        ClearStoredToken();
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Upload request error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get Google access token - from cache or refresh
        /// </summary>
        private static async Task<string?> GetGoogleAccessTokenAsync()
        {
            try
            {
                // Check cache first
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.Now < _tokenExpiry)
                {
                    return _cachedAccessToken;
                }
                
                // Try to load stored refresh token
                string? refreshToken = LoadStoredRefreshToken();
                
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    string? newToken = await RefreshAccessToken(refreshToken);
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        return newToken;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Token error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Refresh access token using stored refresh token
        /// </summary>
        private static async Task<string?> RefreshAccessToken(string refreshToken)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", GoogleClientId),
                    new KeyValuePair<string, string>("client_secret", GoogleClientSecret),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                });
                
                var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
                    {
                        _cachedAccessToken = tokenElement.GetString();
                        _tokenExpiry = DateTime.Now.AddMinutes(55);
                        return _cachedAccessToken;
                    }
                }
                else
                {
                    LogError($"Token refresh failed: {response.StatusCode}");
                    ClearStoredToken();
                }
            }
            catch (Exception ex)
            {
                LogError($"Refresh error: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Start OAuth2 authorization flow - opens browser
        /// Returns authorization code that user enters
        /// </summary>
        public static async Task<bool> AuthorizeGoogleDriveAsync(Action<string>? statusCallback = null)
        {
            try
            {
                statusCallback?.Invoke("Starting authorization...");
                
                // Generate code verifier and challenge for PKCE
                string codeVerifier = GenerateCodeVerifier();
                string codeChallenge = GenerateCodeChallenge(codeVerifier);
                
                // Build authorization URL
                string redirectUri = "urn:ietf:wg:oauth:2.0:oob"; // Manual copy/paste
                string scope = "https://www.googleapis.com/auth/drive.file";
                
                string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={Uri.EscapeDataString(GoogleClientId)}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                    $"response_type=code&" +
                    $"scope={Uri.EscapeDataString(scope)}&" +
                    $"code_challenge={codeChallenge}&" +
                    $"code_challenge_method=S256&" +
                    $"access_type=offline&" +
                    $"prompt=consent";
                
                // Open browser
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                
                statusCallback?.Invoke("Browser opened. Please sign in and paste the code.");
                
                // Store code verifier for exchange
                SaveCodeVerifier(codeVerifier);
                
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Authorization start error: {ex.Message}");
                statusCallback?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Complete authorization with code from user
        /// </summary>
        public static async Task<bool> CompleteGoogleAuthorizationAsync(string authCode, Action<string>? statusCallback = null)
        {
            try
            {
                statusCallback?.Invoke("Exchanging code for tokens...");
                
                string? codeVerifier = LoadCodeVerifier();
                if (string.IsNullOrEmpty(codeVerifier))
                {
                    statusCallback?.Invoke("Error: No pending authorization");
                    return false;
                }
                
                string redirectUri = "urn:ietf:wg:oauth:2.0:oob";
                
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", GoogleClientId),
                    new KeyValuePair<string, string>("client_secret", GoogleClientSecret),
                    new KeyValuePair<string, string>("code", authCode.Trim()),
                    new KeyValuePair<string, string>("code_verifier", codeVerifier),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });
                
                var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
                string json = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(json);
                    
                    string? accessToken = doc.RootElement.GetProperty("access_token").GetString();
                    string? refreshToken = null;
                    if (doc.RootElement.TryGetProperty("refresh_token", out var rtElement))
                    {
                        refreshToken = rtElement.GetString();
                    }
                    
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        SaveRefreshToken(refreshToken);
                        _cachedAccessToken = accessToken;
                        _tokenExpiry = DateTime.Now.AddMinutes(55);
                        
                        ClearCodeVerifier();
                        statusCallback?.Invoke("✓ Google Drive authorized successfully!");
                        LogInfo("Google Drive authorization successful");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke("Error: No refresh token received");
                        return false;
                    }
                }
                else
                {
                    LogError($"Token exchange failed: {json}");
                    statusCallback?.Invoke($"Error: Authorization failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Authorization complete error: {ex.Message}");
                statusCallback?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if Google Drive is authorized
        /// </summary>
        public static bool IsGoogleDriveAuthorized()
        {
            string? token = LoadStoredRefreshToken();
            return !string.IsNullOrEmpty(token);
        }
        
        #region Token Storage
        
        private static void SaveRefreshToken(string refreshToken)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(TokenRegistryPath);
                // Encrypt the token before storing
                byte[] encrypted = System.Security.Cryptography.ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(refreshToken),
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                key?.SetValue("GoogleRefreshToken", Convert.ToBase64String(encrypted));
            }
            catch (Exception ex)
            {
                LogError($"Failed to save token: {ex.Message}");
            }
        }
        
        private static string? LoadStoredRefreshToken()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(TokenRegistryPath);
                string? encrypted = key?.GetValue("GoogleRefreshToken")?.ToString();
                if (!string.IsNullOrEmpty(encrypted))
                {
                    byte[] decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                        Convert.FromBase64String(encrypted),
                        null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }
            }
            catch { }
            return null;
        }
        
        private static void ClearStoredToken()
        {
            try
            {
                _cachedAccessToken = null;
                _tokenExpiry = DateTime.MinValue;
                using var key = Registry.CurrentUser.OpenSubKey(TokenRegistryPath, true);
                key?.DeleteValue("GoogleRefreshToken", false);
            }
            catch { }
        }
        
        private static void SaveCodeVerifier(string verifier)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(TokenRegistryPath);
                key?.SetValue("CodeVerifier", verifier);
            }
            catch { }
        }
        
        private static string? LoadCodeVerifier()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(TokenRegistryPath);
                return key?.GetValue("CodeVerifier")?.ToString();
            }
            catch { return null; }
        }
        
        private static void ClearCodeVerifier()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(TokenRegistryPath, true);
                key?.DeleteValue("CodeVerifier", false);
            }
            catch { }
        }
        
        #endregion
        
        #region PKCE Helpers
        
        private static string GenerateCodeVerifier()
        {
            byte[] bytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        
        private static string GenerateCodeChallenge(string verifier)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        
        #endregion
        
        /// <summary>
        /// Upload to OneDrive - uses local sync folder
        /// </summary>
        private static async Task<bool> UploadToOneDriveAsync(byte[] imageBytes, string fileName, string folderLink)
        {
            try
            {
                string? oneDrivePath = GetOneDriveSyncPath();
                
                if (!string.IsNullOrEmpty(oneDrivePath))
                {
                    string screenshotFolder = Path.Combine(oneDrivePath, "DesktopController_Screenshots");
                    Directory.CreateDirectory(screenshotFolder);
                    
                    string filePath = Path.Combine(screenshotFolder, fileName);
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    
                    LogInfo($"✓ Screenshot saved to OneDrive: {filePath}");
                    return true;
                }
                else
                {
                    return await SaveToLocalFallback(imageBytes, fileName, "OneDrive");
                }
            }
            catch (Exception ex)
            {
                LogError($"OneDrive upload error: {ex.Message}");
                return await SaveToLocalFallback(imageBytes, fileName, "OneDrive");
            }
        }
        
        /// <summary>
        /// Save to local fallback folder
        /// </summary>
        private static async Task<bool> SaveToLocalFallback(byte[] imageBytes, string fileName, string cloudType)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string screenshotFolder = Path.Combine(desktopPath, $"DesktopController_Screenshots_{cloudType}");
                Directory.CreateDirectory(screenshotFolder);
                
                string filePath = Path.Combine(screenshotFolder, fileName);
                await File.WriteAllBytesAsync(filePath, imageBytes);
                
                LogInfo($"Screenshot saved locally (needs authorization): {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Local save error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Extract Google Drive folder ID from various link formats
        /// </summary>
        private static string? ExtractGoogleDriveFolderId(string link)
        {
            if (string.IsNullOrEmpty(link)) return null;
            
            // Pattern: https://drive.google.com/drive/folders/FOLDER_ID
            var match = Regex.Match(link, @"folders[/=]([a-zA-Z0-9_-]+)");
            if (match.Success)
                return match.Groups[1].Value;
            
            // Pattern: https://drive.google.com/drive/u/0/folders/FOLDER_ID
            match = Regex.Match(link, @"drive/[^/]+/folders/([a-zA-Z0-9_-]+)");
            if (match.Success)
                return match.Groups[1].Value;
            
            // Pattern with id parameter
            match = Regex.Match(link, @"[?&]id=([a-zA-Z0-9_-]+)");
            if (match.Success)
                return match.Groups[1].Value;
            
            return null;
        }
        
        /// <summary>
        /// Get OneDrive sync folder path
        /// </summary>
        private static string? GetOneDriveSyncPath()
        {
            try
            {
                string[] possiblePaths = new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive - Personal"),
                    Environment.GetEnvironmentVariable("OneDrive") ?? "",
                    Environment.GetEnvironmentVariable("OneDriveConsumer") ?? ""
                };
                
                foreach (string path in possiblePaths)
                {
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        /// <summary>
        /// Generate unique screenshot filename
        /// </summary>
        public static string GenerateScreenshotFileName(string? machineName = null, string? userName = null)
        {
            string machine = machineName ?? Environment.MachineName;
            string user = userName ?? Environment.UserName;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            
            return $"screenshot_{machine}_{user}_{timestamp}.jpg";
        }
        
        private static void LogInfo(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DesktopController",
                    "cloud_storage.log"
                );
                
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n");
            }
            catch { }
        }
        
        private static void LogError(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DesktopController",
                    "cloud_storage.log"
                );
                
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n");
            }
            catch { }
        }
    }
}
