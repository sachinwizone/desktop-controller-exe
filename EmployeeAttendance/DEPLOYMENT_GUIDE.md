# Chat System - Deployment & Setup Guide

## Build Information

**Build Date**: January 23, 2026  
**EXE File**: `EmployeeAttendance.exe`  
**Location**: `bin/x64/Release/net6.0-windows/win-x64/EmployeeAttendance.exe`  
**File Size**: 0.18 MB  
**Build Status**: ✅ SUCCESS (0 Errors, 10 Warnings)  

---

## What's New

### ✅ Desktop EXE Enhancement
- Added `ChatService.cs` - Service for sending/receiving chat messages
- Added `ChatForm.cs` - Windows Forms UI for chat messaging
- Full integration with web dashboard via REST API

### ✅ Web Server Enhancement
- Added `backend_chatController.js` - Complete chat API endpoints
- 6 API endpoints for message management
- MongoDB storage for message persistence

### ✅ Web Dashboard Enhancement
- Added `chat-module.js` - Floating chat widget for web interface
- Real-time message display with 3-second auto-refresh
- Collapsible UI with message history

### ✅ Existing Features Preserved
All existing controllers, services, and functionality remain intact:
- Employee management
- Activity history tracking
- System information collection
- Device control
- Database operations
- All other features

---

## Step-by-Step Deployment

### Phase 1: Deploy Desktop EXE (5 minutes)

**Step 1.1**: Copy EXE to Target Machines
```bash
Source: bin/x64/Release/net6.0-windows/win-x64/EmployeeAttendance.exe
Target: C:\Program Files\EmployeeAttendance\EmployeeAttendance.exe
        (or your preferred installation directory)
```

**Step 1.2**: Copy Configuration File
```bash
Source: App.config (if modified)
Target: C:\Program Files\EmployeeAttendance\EmployeeAttendance.dll.config
```

**Step 1.3**: Verify Configuration
- Check `EmployeeAttendance.dll.config` contains:
  ```xml
  <add key="API_BASE_URL" value="http://localhost:8888"/>
  ```
- Update URL if web server is on different machine/port

**Step 1.4**: Test Execution
```bash
# Run executable to verify no errors
C:\Program Files\EmployeeAttendance\EmployeeAttendance.exe

# Should start without errors and show main dashboard
```

### Phase 2: Deploy Web Server (10 minutes)

**Step 2.1**: Copy Chat Controller
```bash
Source: backend_chatController.js
Target: /path/to/web/server/controllers/backend_chatController.js
```

**Step 2.2**: Update Server.js
Add these lines to your main server.js file:

```javascript
// Near the top with other imports
const chatRouter = require('./controllers/backend_chatController');

// In your Express app setup
app.use('/api/chat', chatRouter);

// Make sure MongoDB connection is available
// chatController will use: mongodb://72.61.170.243:9095/controller_application
```

**Step 2.3**: Verify MongoDB Connection
```bash
# Test MongoDB connectivity
mongosh mongodb://72.61.170.243:9095/controller_application

# You should see:
# > use controller_application
# switched to db controller_application
```

**Step 2.4**: Restart Web Server
```bash
# Stop current server
npm stop
# or Ctrl+C

# Start server
npm start
# or node server.js
```

**Step 2.5**: Test Chat Endpoint
```bash
# Open browser or use curl to test
curl http://localhost:8888/api/chat/stats

# Should return:
# {"success":true,"message":"Chat statistics retrieved successfully","data":{...}}
```

### Phase 3: Deploy Web Dashboard (5 minutes)

**Step 3.1**: Copy Chat Module
```bash
Source: chat-module.js
Target: /path/to/web/dashboard/chat-module.js
```

**Step 3.2**: Add to HTML Template
Find your main dashboard HTML file and add before closing `</body>` tag:

```html
<!-- Chat Widget Module -->
<script src="chat-module.js"></script>

<!-- Initialize Chat -->
<script>
    document.addEventListener('DOMContentLoaded', function() {
        // Initialize chat with optional device ID
        ChatModule.initialize();
        console.log('[Dashboard] Chat module initialized');
    });
</script>
```

**Step 3.3**: Test Chat Widget
1. Open dashboard in browser
2. Chat widget should appear in bottom-right corner
3. Try sending a message
4. Check desktop app to see if message appears

---

## Pre-Deployment Checklist

- [ ] **Desktop EXE**
  - [ ] Build succeeded with 0 errors
  - [ ] File size is ~0.18 MB
  - [ ] App.config exists with correct API_BASE_URL
  - [ ] Tested launch on target machine
  
- [ ] **Web Server**
  - [ ] Node.js running on correct port (8888)
  - [ ] MongoDB connection verified
  - [ ] backend_chatController.js in correct location
  - [ ] Chat router registered in server.js
  - [ ] npm dependencies installed
  
- [ ] **Web Dashboard**
  - [ ] chat-module.js copied to web folder
  - [ ] Script tag added to HTML
  - [ ] ChatModule.initialize() called
  - [ ] CSS not conflicting with chat widget
  
- [ ] **Network**
  - [ ] Port 8888 accessible from all machines
  - [ ] MongoDB port 9095 accessible from web server
  - [ ] Firewall rules allow traffic
  
- [ ] **Database**
  - [ ] MongoDB running and accessible
  - [ ] controller_application database exists
  - [ ] Indexes will be created automatically on first use

---

## Testing After Deployment

### Test 1: Basic Connectivity
```bash
# Test from desktop machine
curl -X POST http://localhost:8888/api/chat/send \
  -H "Content-Type: application/json" \
  -d '{
    "device_id": "TEST-PC",
    "sender": "TestUser",
    "message": "Test message",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
    "is_from_desktop": true
  }'

# Should return success response
```

### Test 2: End-to-End Message Flow
1. **Desktop App**:
   - Open EmployeeAttendance.exe
   - Open Chat Form (from main menu)
   - Type: "Hello from Desktop"
   - Click Send

2. **Web Dashboard**:
   - Open dashboard in browser
   - Message should appear in chat widget
   - Type: "Hello from Web"
   - Click Send

3. **Desktop App**:
   - Close and reopen Chat Form
   - Both messages should appear

### Test 3: Message Persistence
```bash
# Check MongoDB for stored messages
mongosh mongodb://72.61.170.243:9095/controller_application

# In MongoDB shell:
> use controller_application
> db.chat_messages.find()
> db.chat_messages.countDocuments()

# Should show messages with correct sender/content
```

### Test 4: Performance Baseline
- Send 10 messages from desktop
- Measure response time (should be < 500ms)
- Check MongoDB query performance
- Monitor CPU/memory usage on web server

---

## Troubleshooting Deployment

### Issue: "Cannot find EmployeeAttendance.exe"
**Solution**: Verify file path and copy command executed correctly

### Issue: "API_BASE_URL not configured"
**Solution**: Add to App.config:
```xml
<add key="API_BASE_URL" value="http://localhost:8888"/>
```

### Issue: "MongoDB connection failed"
**Solution**: 
```bash
# Verify MongoDB is running
mongosh mongodb://72.61.170.243:9095/

# Check network connectivity to MongoDB host
ping 72.61.170.243

# Check firewall allows port 9095
telnet 72.61.170.243 9095
```

### Issue: "404 on /api/chat/send"
**Solution**: Ensure router is registered in server.js:
```javascript
const chatRouter = require('./controllers/backend_chatController');
app.use('/api/chat', chatRouter);
```

### Issue: "Chat widget not appearing"
**Solution**:
1. Clear browser cache: `Ctrl+Shift+Del`
2. Verify chat-module.js is in correct path
3. Check browser console for errors
4. Verify ChatModule.initialize() is called

### Issue: "Messages not saving"
**Solution**:
1. Check MongoDB is running: `mongosh`
2. Verify database exists: `show dbs`
3. Check collection permissions
4. Review server logs for errors

---

## Post-Deployment Verification

### Performance Metrics to Check

1. **Message Send Time**: < 500ms
2. **Message Retrieval Time**: < 200ms  
3. **Memory Usage**: < 100MB (desktop), < 500MB (server)
4. **Database Query Time**: < 100ms
5. **Chat Widget Load Time**: < 1 second

### Health Check Commands

```bash
# Check EXE is running
tasklist | findstr EmployeeAttendance

# Check web server
curl http://localhost:8888/api/chat/stats

# Check database
mongosh mongodb://72.61.170.243:9095/controller_application -c "db.chat_messages.countDocuments()"

# Check ports
netstat -ano | findstr :8888
netstat -ano | findstr :9095
```

---

## Rollback Procedure

If issues occur, rollback is simple:

1. **Desktop EXE Rollback**:
   ```bash
   # Replace with previous version
   copy EmployeeAttendance.exe.backup EmployeeAttendance.exe
   ```

2. **Web Server Rollback**:
   ```bash
   # Remove chat router from server.js
   # Delete backend_chatController.js
   # Restart server
   ```

3. **Web Dashboard Rollback**:
   ```bash
   # Remove chat-module.js
   # Remove chat initialization script
   # Clear browser cache
   ```

---

## Monitoring After Deployment

### Set Up Logging

**Desktop (Debug Console)**:
- Already logged via Debug.WriteLine
- Review output in Visual Studio

**Web Server (Console)**:
```javascript
// Already logs to console
console.log('[Chat] Message saved');
console.log('[Chat] Retrieved messages');
```

**Database (MongoDB Logs)**:
- Check MongoDB logs at: `/var/log/mongodb/mongod.log` (Linux)
- Or check Event Viewer (Windows)

### Performance Monitoring

```javascript
// Add to backend_chatController.js for timing
const startTime = Date.now();
// ... API processing ...
const duration = Date.now() - startTime;
console.log(`[Chat] API took ${duration}ms`);
```

---

## Maintenance Tasks

### Daily
- Monitor error logs
- Check database size growth
- Verify all endpoints responding

### Weekly
- Review message volume trends
- Check database indexes are being used
- Monitor server resource usage

### Monthly
- Clean up old messages (optional)
- Analyze chat statistics
- Plan capacity upgrades

---

## Support Contact Information

For issues during deployment:

1. **Check Logs First**:
   - Desktop: Debug console
   - Web: Server logs + browser console
   - Database: MongoDB logs

2. **Verify Configuration**:
   - Check all settings match deployment guide
   - Verify network connectivity
   - Confirm file locations

3. **Test Endpoints**:
   - Use curl/Postman to test API
   - Verify MongoDB connectivity
   - Check server routing

---

## File Checklist

### Desktop EXE Package
- ✅ EmployeeAttendance.exe (0.18 MB)
- ✅ EmployeeAttendance.dll.config
- ✅ ChatService.cs (compiled into EXE)
- ✅ ChatForm.cs (compiled into EXE)

### Web Server Package
- ✅ backend_chatController.js
- ✅ Updated server.js (with chat router)

### Web Dashboard Package
- ✅ chat-module.js
- ✅ Updated HTML (with chat initialization)

### Documentation
- ✅ CHAT_IMPLEMENTATION_REPORT.md
- ✅ CHAT_INTEGRATION_GUIDE.md
- ✅ DEPLOYMENT_GUIDE.md (this file)

---

## Success Criteria

✅ Deployment is successful when:

1. Desktop EXE launches without errors
2. Chat Form opens and displays
3. Messages send to web API successfully
4. Messages appear in web dashboard
5. Web messages appear in desktop app
6. Messages persist in MongoDB
7. All 6 API endpoints respond correctly
8. No error messages in console/logs
9. Response times are acceptable (< 500ms)
10. All existing features still work

---

**Deployment Package Ready for Release** ✅

---

## Quick Reference Commands

```bash
# Start web server
cd /path/to/web/server
npm start

# Test MongoDB
mongosh mongodb://72.61.170.243:9095/controller_application

# Check service status
systemctl status nodejs  # Linux
Get-Service | where {$_.Name -like "*node*"}  # Windows

# View logs
tail -f /var/log/nodejs.log  # Linux
Get-Content server.log -Tail 50  # Windows

# Test API endpoint
curl http://localhost:8888/api/chat/stats
```

---

**All systems ready for production deployment** ✅
