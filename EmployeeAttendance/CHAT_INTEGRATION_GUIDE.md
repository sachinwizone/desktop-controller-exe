# Chat System Integration Guide

## Quick Start

### For Desktop EXE (C#)

The chat functionality is **already compiled** into the new EmployeeAttendance.exe.

To use it in MainDashboard.cs:

```csharp
// Add this button click handler in MainDashboard
private void OpenChatButton_Click(object sender, EventArgs e)
{
    var chatForm = new ChatForm("EmployeeName"); // Pass current user name
    chatForm.Show();
}
```

### For Web Dashboard (JavaScript)

Add these lines to your HTML file:

```html
<!-- At the end of your HTML body -->
<script src="chat-module.js"></script>
<script>
    // Initialize chat module on page load
    document.addEventListener('DOMContentLoaded', function() {
        ChatModule.initialize(); // Automatically generates device ID
        console.log('Chat module initialized');
    });
</script>
```

### For Web Server (Node.js/Express)

Add the chat controller to your main server file:

```javascript
const express = require('express');
const app = express();

// ... other middleware ...

// Import chat routes
const chatRouter = require('./backend_chatController');

// Add chat routes
app.use('/api/chat', chatRouter);

// Start server
app.listen(8888, () => {
    console.log('Server running on port 8888');
    console.log('Chat API available at /api/chat');
});
```

---

## Testing the Chat System

### Test 1: Desktop to Web Messaging

**Steps:**
1. Launch EmployeeAttendance.exe
2. Open Chat Form (from MainDashboard)
3. Type a message: "Hello from Desktop"
4. Click Send
5. Open web dashboard in browser
6. Message should appear in chat widget

**Expected Result:** Message appears in web dashboard with "Desktop App" as sender

### Test 2: Web to Desktop Messaging

**Steps:**
1. Open web dashboard with chat widget visible
2. Type a message: "Hello from Web"
3. Click Send
4. Check Desktop chat form

**Expected Result:** Message appears in desktop chat with "Web Dashboard" as sender

### Test 3: Message Persistence

**Steps:**
1. Send a message from desktop
2. Close desktop app
3. Relaunch desktop app and open chat
4. Previous message should still appear

**Expected Result:** All historical messages are displayed

### Test 4: Multiple Devices

**Steps:**
1. Open dashboard on 2 different computers
2. Send message from computer A
3. Check if visible on computer B

**Expected Result:** Messages sync across all devices for same employee

---

## API Endpoint Reference

### Send Message
```bash
POST /api/chat/send
Content-Type: application/json

{
  "device_id": "DESKTOP-USER",
  "sender": "User Name",
  "message": "Hello world",
  "timestamp": "2026-01-23T10:30:00Z",
  "is_from_desktop": true
}

Response: {
  "success": true,
  "message": "Message sent successfully",
  "data": {
    "id": "507f1f77bcf86cd799439011",
    "device_id": "DESKTOP-USER",
    "sender": "User Name",
    "message": "Hello world",
    "timestamp": "2026-01-23T10:30:00Z",
    "is_from_desktop": true
  }
}
```

### Get Messages
```bash
GET /api/chat/get?device_id=DESKTOP-USER&limit=50&offset=0

Response: {
  "success": true,
  "message": "Messages retrieved successfully",
  "data": [
    {
      "id": "507f1f77bcf86cd799439011",
      "device_id": "DESKTOP-USER",
      "sender": "Web Dashboard",
      "message": "Hey, how are you?",
      "timestamp": "2026-01-23T10:25:00Z",
      "is_from_desktop": false
    },
    ...
  ],
  "count": 25
}
```

### Get Conversations
```bash
GET /api/chat/conversations?device_id=DESKTOP-USER

Response: {
  "success": true,
  "message": "Conversations retrieved successfully",
  "data": [
    {
      "_id": "DESKTOP-USER",
      "last_message": "See you later!",
      "last_sender": "Web Dashboard",
      "last_timestamp": "2026-01-23T10:35:00Z",
      "message_count": 15,
      "unread_count": 2
    }
  ]
}
```

### Mark Messages as Read
```bash
PUT /api/chat/mark-read
Content-Type: application/json

{
  "message_ids": ["507f1f77bcf86cd799439011", "507f1f77bcf86cd799439012"],
  "device_id": "DESKTOP-USER"
}

Response: {
  "success": true,
  "message": "Messages marked as read",
  "modified_count": 2
}
```

### Delete Message
```bash
DELETE /api/chat/delete/507f1f77bcf86cd799439011

Response: {
  "success": true,
  "message": "Message deleted successfully"
}
```

### Get Statistics
```bash
GET /api/chat/stats

Response: {
  "success": true,
  "message": "Chat statistics retrieved successfully",
  "data": {
    "total_messages": 152,
    "unread_messages": 8,
    "desktop_messages": 45,
    "web_messages": 107,
    "unique_device_count": 3
  }
}
```

---

## Database Schema

### chat_messages Collection

```javascript
{
  _id: ObjectId("507f1f77bcf86cd799439011"),
  device_id: "DESKTOP-USER",              // Machine name or device identifier
  sender: "John Doe",                     // Display name of sender
  message: "Hello world",                 // Message content
  timestamp: ISODate("2026-01-23T10:30:00Z"),  // When message was created
  is_from_desktop: true,                  // true if from EXE, false if from web
  recipient_id: null,                     // Optional specific recipient
  conversation_id: "DESKTOP-USER",        // Conversation grouping
  read: false,                            // Read status
  created_at: ISODate("2026-01-23T10:30:00Z"),
  updated_at: ISODate("2026-01-23T10:30:00Z")
}
```

### Indexes

```javascript
// Index 1: Efficient device message queries
db.chat_messages.createIndex({ device_id: 1, timestamp: -1 })

// Index 2: Time-based queries
db.chat_messages.createIndex({ timestamp: -1 })
```

---

## Configuration Files

### App.config (Desktop EXE)

Ensure this setting exists in `EmployeeAttendance.dll.config`:

```xml
<configuration>
  <appSettings>
    <add key="API_BASE_URL" value="http://localhost:8888"/>
    <add key="API_ENDPOINT_CHAT_SEND" value="/api/chat/send"/>
    <add key="API_ENDPOINT_CHAT_GET" value="/api/chat/get"/>
    <!-- ... other settings ... -->
  </appSettings>
</configuration>
```

### Environment Variables (Node.js Server)

Optional - for production deployment:

```bash
export MONGODB_URL="mongodb://user:pass@host:port/controller_application"
export API_KEY="your-secret-api-key"
export PORT=8888
```

---

## Troubleshooting Guide

### Issue: "API_BASE_URL not found"

**Cause:** App.config missing or setting not present  
**Solution:** Add the setting to App.config:
```xml
<add key="API_BASE_URL" value="http://localhost:8888"/>
```

### Issue: "Cannot connect to server"

**Cause:** Web server not running on port 8888  
**Solution:**
```bash
# Check if server is running
netstat -ano | findstr :8888

# Start server
node server.js
```

### Issue: "No messages appearing"

**Cause:** MongoDB not connected  
**Solution:**
```bash
# Check MongoDB connection
mongosh mongodb://localhost:27017/controller_application

# Verify collection exists
db.chat_messages.find()
```

### Issue: "Chat widget not visible"

**Cause:** CSS z-index or chat-module.js not loaded  
**Solution:**
1. Check browser console for errors
2. Verify chat-module.js is in correct path
3. Check that `ChatModule.initialize()` is called
4. Clear browser cache and reload

### Issue: "Messages disappearing after refresh"

**Cause:** Messages not persisting to MongoDB  
**Solution:**
1. Verify MongoDB is running
2. Check API response for errors
3. Monitor server logs for insertion failures

---

## Performance Tuning

### Reduce Message Refresh Rate (Slower Updates)

**Desktop:**
```csharp
// In ChatForm.cs, change:
_messageRefreshTimer.Interval = 5000; // 5 seconds instead of 3
```

**Web:**
```javascript
// In chat-module.js, change:
messageRefreshInterval = setInterval(() => {
    loadMessages();
}, 5000); // 5 seconds instead of 3
```

### Increase Message Limit (More Messages Per Load)

**Desktop & Web:**
```javascript
// In ChatForm.cs or chat-module.js, change:
const response = await fetch(
    `${API_BASE}/get?device_id=${currentDeviceId}&limit=100&offset=0`
);
```

### Implement Pagination

```javascript
// Load messages in batches
let offset = 0;
const pageSize = 50;

async function loadMoreMessages() {
    const response = await fetch(
        `${API_BASE}/get?device_id=${currentDeviceId}&limit=${pageSize}&offset=${offset}`
    );
    offset += pageSize;
    // Display messages...
}
```

---

## Security Best Practices

1. **Validate Input:**
   - Sanitize message content before display
   - Limit message length (e.g., 5000 characters)
   - Check device_id format

2. **Rate Limiting:**
   - Add rate limiting to API endpoints
   - Limit messages per user per minute
   - Example: 100 messages per user per 5 minutes

3. **Authentication:**
   - Add user authentication
   - Link messages to authenticated users
   - Prevent message spoofing

4. **Data Encryption:**
   - Encrypt sensitive messages
   - Use HTTPS in production
   - Encrypt database at rest

5. **Access Control:**
   - Restrict message access by device/user
   - Implement role-based permissions
   - Log all message access

---

## Monitoring & Logging

### Enable Logging (Desktop)

```csharp
// ChatService already logs to Debug console
// To file, add:
System.Diagnostics.Debug.WriteLine("[Chat] Message sent");
```

### Enable Logging (Web Server)

```javascript
// Already included in backend_chatController.js
console.log('[Chat] Message saved: ' + messageId);
console.log('[Chat] Retrieved ' + messages.length + ' messages');
```

### Monitor Database

```javascript
// Check message count
db.chat_messages.countDocuments()

// Check unread messages
db.chat_messages.countDocuments({ read: false })

// Check messages by device
db.chat_messages.countDocuments({ device_id: "DESKTOP-USER" })

// Check recent messages
db.chat_messages.find().sort({ timestamp: -1 }).limit(10)
```

---

## Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| 404 Not Found on `/api/chat/send` | Route not registered | Add router to server.js |
| CORS errors | Cross-origin request | Add CORS middleware to Express |
| "undefined" messages | JSON parsing issue | Check API response format |
| Slow message loading | No database indexes | Create indexes on device_id |
| Duplicate messages | Duplicate API calls | Add debouncing to send function |
| Lost messages after browser refresh | Not persisting to DB | Check MongoDB connection |
| Chat widget overlapping | Z-index conflict | Increase z-index value |
| High memory usage | Too many messages in memory | Implement pagination/limits |

---

## Additional Resources

- **Chat Module API**: See `ChatModule` object properties in chat-module.js
- **Chat Service API**: See `ChatService` class methods in ChatService.cs
- **API Documentation**: See backend_chatController.js comments
- **Database Queries**: Use MongoDB CLI or MongoDB Compass for inspection

---

## Support & Maintenance

For issues or questions:

1. **Check Logs:**
   - Desktop: Debug console output
   - Web: Browser console + server logs
   - Database: MongoDB logs

2. **Verify Configuration:**
   - App.config API_BASE_URL
   - MongoDB connection string
   - Express server routing

3. **Test Endpoints:**
   - Use Postman or curl to test API
   - Verify database connectivity
   - Check network connectivity

---

**Integration Complete! System Ready for Use** ✅
