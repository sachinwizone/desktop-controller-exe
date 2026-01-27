using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EmployeeAttendance
{
    /// <summary>
    /// Chat service for communicating with web dashboard
    /// Enables real-time messaging between desktop app and web portal
    /// </summary>
    public class ChatService
    {
        private static ChatService _instance;
        private readonly string _apiBaseUrl;
        private readonly string _deviceId;
        private readonly HttpClient _httpClient;
        private List<ChatMessage> _localMessages;

        private ChatService()
        {
            _apiBaseUrl = LoadApiBaseUrl();
            _deviceId = Environment.MachineName;
            _httpClient = new HttpClient();
            _localMessages = new List<ChatMessage>();
        }

        public static ChatService GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ChatService();
            }
            return _instance;
        }

        private string LoadApiBaseUrl()
        {
            try
            {
                var config = System.Configuration.ConfigurationManager.AppSettings["API_BASE_URL"];
                return config ?? "http://localhost:8888";
            }
            catch
            {
                return "http://localhost:8888";
            }
        }

        /// <summary>
        /// Send a chat message to the web dashboard
        /// </summary>
        public async Task<bool> SendMessage(string sender, string message)
        {
            try
            {
                var chatMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = _deviceId,
                    Sender = sender,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    IsFromDesktop = true
                };

                _localMessages.Add(chatMessage);
                Debug.WriteLine($"[Chat] Sending message from {sender}: {message}");

                // Send to web API
                var payload = new
                {
                    device_id = _deviceId,
                    sender = sender,
                    message = message,
                    timestamp = DateTime.UtcNow,
                    is_from_desktop = true
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/chat/send", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[Chat] Message sent successfully: {chatMessage.Id}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[Chat] Failed to send message: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Chat] Error sending message: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieve messages from web dashboard
        /// </summary>
        public async Task<List<ChatMessage>> GetMessages(string conversationId = null)
        {
            try
            {
                var url = $"{_apiBaseUrl}/api/chat/get?device_id={_deviceId}";
                if (!string.IsNullOrEmpty(conversationId))
                {
                    url += $"&conversation_id={conversationId}";
                }

                Debug.WriteLine($"[Chat] Fetching messages from: {url}");
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var messages = new List<ChatMessage>();

                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        foreach (var msg in dataElement.EnumerateArray())
                        {
                            messages.Add(new ChatMessage
                            {
                                Id = msg.GetProperty("id").GetString() ?? "",
                                DeviceId = msg.GetProperty("device_id").GetString() ?? "",
                                Sender = msg.GetProperty("sender").GetString() ?? "",
                                Message = msg.GetProperty("message").GetString() ?? "",
                                Timestamp = msg.GetProperty("timestamp").GetDateTime(),
                                IsFromDesktop = msg.GetProperty("is_from_desktop").GetBoolean()
                            });
                        }
                    }

                    Debug.WriteLine($"[Chat] Retrieved {messages.Count} messages");
                    return messages;
                }

                return new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Chat] Error retrieving messages: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        /// <summary>
        /// Get all messages for this device
        /// </summary>
        public List<ChatMessage> GetLocalMessages()
        {
            return _localMessages.OrderBy(m => m.Timestamp).ToList();
        }

        /// <summary>
        /// Clear local message cache
        /// </summary>
        public void ClearLocalMessages()
        {
            _localMessages.Clear();
            Debug.WriteLine("[Chat] Local messages cleared");
        }
    }

    /// <summary>
    /// Represents a chat message
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsFromDesktop { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Sender}: {Message}";
        }
    }
}
