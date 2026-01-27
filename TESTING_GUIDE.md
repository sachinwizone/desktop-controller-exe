# Test Instructions - System Control Integration

## ✅ What Was Fixed

### Web Dashboard (`system-control-integration.js`)
1. **Navigation Integration**
   - Fixed the initialization timing to wait for app to be ready
   - Added proper event handler override for navigation
   - Navigation now properly routes to system control panels

2. **Display Issues**
   - Fixed panel rendering to clear old content
   - Updated selectors to use class-based queries instead of IDs
   - Tab switching now properly displays content
   - All tabs have proper content loading

3. **JavaScript Updates**
   - `showSystemControlPanel()` - Now properly creates and displays panel
   - `setupSystemControlTabs()` - Fixed tab switching logic
   - `loadDevicesList()` - Uses correct selectors
   - `loadSystemInformation()` - Uses correct selectors
   - `loadInstalledApplications()` - Uses correct selectors
   - `sendSystemCommand()` - Fixed response display

### Desktop EXE (`SystemDataCollectionService.cs`)
1. **Configuration Support**
   - Now reads from App.config file
   - Configurable API endpoint (http://localhost:8888)
   - Configurable collection intervals

2. **Real Data Collection**
   - Sends device name and ID
   - Includes OS version
   - Sends processor count
   - Sends system uptime
   - Includes timestamp

3. **Logging**
   - Debug output for all major operations
   - API call logging enabled
   - Error tracking

### Configuration File (`App.config`)
```
API_BASE_URL = http://localhost:8888
API_ENDPOINT_SYSTEM_INFO = /api/system-info/sync
API_ENDPOINT_APPS = /api/installed-apps/sync
SYSTEM_INFO_INTERVAL = 300 seconds (5 minutes)
APPS_SYNC_INTERVAL = 3600 seconds (1 hour)
```

---

## 🧪 Testing Steps

### Step 1: Start Web Server
```powershell
cd "c:\Users\sachi\Desktop\EXE - DESKTOP CONTROLLER\web_dashboard_new"
npm start
```
✅ Server should run on http://localhost:8888

### Step 2: Open Web Dashboard
- Open browser and go to http://localhost:8888
- Login with your credentials

### Step 3: Click on Device Control Navigation
In the left sidebar, you should see:
- ✅ **DEVICE CONTROL** section
- ✅ **🖥️ Device Management**
- ✅ **💾 System Information**
- ✅ **📦 Installed Applications**

Click on each one and verify:
- Content displays properly
- Tabs switch between Devices, System Info, Applications, Commands
- Mock data shows (devices, system specs, apps list, command buttons)
- Command buttons are clickable (show success message)

### Step 4: Run Desktop EXE
```powershell
& "c:\Users\sachi\Desktop\EXE - DESKTOP CONTROLLER\EmployeeAttendance\bin\x64\Release\net6.0-windows\win-x64\EmployeeAttendance.exe"
```

✅ Should:
- Start without errors
- Automatically start SystemDataCollectionService
- Output "System data collection service started" in debug output
- Begin sending data to web API every 5 minutes

### Step 5: Monitor API Calls
- Open browser DevTools (F12)
- Go to Network tab
- Look for POST requests to http://localhost:8888/api/system-info/sync
- Should see requests every 5 minutes

---

## 🔧 Configuration Changes

To modify behavior, edit `App.config` in the build output:
```xml
<add key="API_BASE_URL" value="http://YOUR_SERVER:8888"/>
<add key="SYSTEM_INFO_INTERVAL" value="300"/> <!-- Change interval in seconds -->
<add key="LOG_API_CALLS" value="true"/> <!-- Enable/disable logging -->
```

---

## 📝 Files Modified

1. **system-control-integration.js**
   - Fixed initialization and navigation handling
   - Improved DOM manipulation and selector usage
   - Better error handling and logging

2. **SystemDataCollectionService.cs**
   - Added configuration loading
   - Added real system data collection
   - Improved logging and error handling

3. **App.config** (NEW)
   - Configuration file for the EXE
   - Easy to modify without recompiling

4. **MainDashboard.cs**
   - Already calls SystemDataCollectionService.Start()
   - Calls Service.Stop() on shutdown

---

## ✅ Expected Behavior

### Web Dashboard
- Navigation shows Device Control section
- Clicking navigation items displays the correct panel
- Tabs switch properly
- Mock data displays
- Command buttons are interactive

### Desktop EXE
- Service starts automatically
- Collects system information
- Sends data to web API every 5 minutes
- Gracefully stops on shutdown
- All operations logged for debugging

### Data Flow
```
Desktop EXE
    ↓ (every 5 min)
Collects: Device Name, OS, CPU Count, Uptime
    ↓
Sends to: http://localhost:8888/api/system-info/sync
    ↓
Web API receives and processes data
    ↓
(Future) Web Dashboard displays received data
```

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| Navigation shows but no content | Refresh browser, check console for errors |
| Device Control section missing | Restart web server with `npm start` |
| EXE crashes on start | Check EmployeeAttendance.dll.config exists in bin folder |
| API calls failing | Verify web server is running on port 8888 |
| No debug output | Ensure Debug logging is enabled in Visual Studio |

---

## Next Steps

After testing, you can:
1. **Replace mock data** with actual API responses
2. **Store device data** in MongoDB database
3. **Add real command execution** (restart, shutdown, etc.)
4. **Implement authentication** between desktop and web
5. **Add device status tracking** and historical data

Enjoy your system! 🚀
