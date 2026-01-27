# System Management Control Panel - Integration Guide

## 🎯 Overview
Complete system management and control panel module for **web_dashboard_new ONLY**. This module provides:
- Real-time system monitoring (hardware, OS, storage)
- Active user tracking
- Installed applications management
- Remote system control (restart, shutdown, uninstall, block apps)
- Live dashboard with statistics

---

## 📋 Features

### 1. **Dashboard Tab (Statistics)**
- Total devices count
- Active devices (last 30 minutes)
- Active users (last 5 minutes)
- Total applications across all devices
- Most common applications report

### 2. **Devices Tab**
- List all registered devices
- Show system specifications:
  - Operating System
  - CPU information
  - RAM details
  - Storage capacity & free space
- Control actions:
  - View detailed device info
  - Send restart command
  - Send shutdown command

### 3. **Active Users Tab**
- Real-time active users list
- Last seen timestamp
- Computer association
- Actions:
  - Lock screen
  - Send message to user

### 4. **Applications Tab**
- Browse all installed applications
- Search applications
- View installation details
- Management actions:
  - Uninstall application
  - Block application execution

---

## 🔧 Desktop Client Integration (C# .NET)

### Step 1: Add System Collectors to Main Program
In `Program.cs`, add initialization:

```csharp
// In the Main() method
private static SystemInfoCollector? systemInfoCollector;
private static InstalledAppsCollector? appsCollector;
private static SystemControlHandler? controlHandler;

static void Main(string[] args)
{
    // ... existing code ...

    // Initialize system monitoring (after getting company info)
    string serverUrl = "https://your-server.com"; // Your backend URL
    string activationKey = "your-activation-key";
    string companyName = "Your Company";
    string userName = Environment.UserName;

    systemInfoCollector = new SystemInfoCollector(
        serverUrl, activationKey, companyName, userName
    );
    systemInfoCollector.Start();

    appsCollector = new InstalledAppsCollector(
        serverUrl, activationKey
    );
    appsCollector.Start();

    controlHandler = new SystemControlHandler(
        serverUrl, activationKey
    );
    controlHandler.Start();

    // ... rest of application ...
}
```

### Step 2: Cleanup on Exit
```csharp
private static void CleanupBeforeExit()
{
    systemInfoCollector?.Stop();
    appsCollector?.Stop();
    controlHandler?.Stop();
}
```

---

## 🌐 Backend API Integration (Node.js/Express)

### Step 1: Setup Database Models
Copy files from `database_schema.js` to `backend/models/`:
- `SystemInfo.js`
- `InstalledApp.js`
- `ControlCommand.js`
- `CommandResult.js`
- `ActiveUser.js`

### Step 2: Add API Routes
In your Express app (`server.js` or `app.js`):

```javascript
const systemManagementRouter = require('./controllers/systemManagementController');

// Add authentication middleware
const authenticateApiKey = (req, res, next) => {
    const apiKey = req.headers['x-api-key'];
    if (!apiKey || apiKey !== process.env.API_KEY) {
        return res.status(401).json({ error: 'Unauthorized' });
    }
    next();
};

// Register routes
app.use('/api', authenticateApiKey, systemManagementRouter);
```

### Step 3: Environment Variables
Add to `.env`:
```
API_KEY=your-secure-api-key-here
MONGODB_URI=mongodb://your-mongodb-connection
SYNC_INTERVAL=30000
```

---

## 🎨 Web Dashboard Integration (NEW DASHBOARD ONLY)

### Step 1: Include Control Panel in New Dashboard
In `web_dashboard_new/index.html`, add:

```html
<!-- Before closing </head> -->
<link rel="stylesheet" href="system-control-panel.css">

<!-- Before closing </body> -->
<script src="web_control_panel_new_dashboard_only.js"></script>
```

### Step 2: Initialize Control Panel in App
In your main app JavaScript:

```javascript
// Initialize System Management Control Panel
const systemControl = new SystemManagementControlPanel(
    'https://your-api-server.com/api',
    'your-api-key'
);

// Set current company
localStorage.setItem('currentCompany', 'Company Name');
localStorage.setItem('activationKey', 'user-activation-key');
```

### Step 3: Add Control Panel Container
Add to your dashboard HTML where you want the panel:

```html
<div id="controlPanelContainer"></div>
```

### Step 4: Render Control Panel
```javascript
const panelHTML = `<!-- paste content from web_control_panel_html_new_dashboard_only.html -->`;
document.getElementById('controlPanelContainer').innerHTML = panelHTML;

// Re-initialize event listeners after injection
systemControl.init();
```

---

## 📡 API Endpoints Reference

### System Information
- **POST** `/api/system-info/sync` - Submit system info from client
- **GET** `/api/system-info/:activationKey/:computerName` - Get device info
- **GET** `/api/system-info/company/:companyName` - Get all devices in company

### Installed Applications
- **POST** `/api/installed-apps/sync` - Submit installed apps list
- **GET** `/api/installed-apps/:activationKey/:computerName` - Get device apps
- **GET** `/api/installed-apps/search?appName=X` - Search applications

### Control Commands
- **POST** `/api/control/commands` - Create new command
- **GET** `/api/control/commands` - Get pending commands
- **POST** `/api/control/command-result` - Report command execution
- **DELETE** `/api/control/commands/:commandId` - Cancel command

### Active Users
- **POST** `/api/active-users/ping` - Heartbeat from client
- **GET** `/api/active-users/:companyName` - Get active users

### Statistics
- **GET** `/api/statistics/company/:companyName` - Get company statistics

---

## 🔐 Security Considerations

### 1. API Authentication
All endpoints require `x-api-key` header:
```javascript
headers: {
    'x-api-key': process.env.API_KEY
}
```

### 2. Encryption
For sensitive data, implement TLS/SSL:
```
https://your-server.com/api/...
```

### 3. Command Authorization
Only authorized admins can send control commands. Implement role-based access:
```javascript
const authorizeAdmin = (req, res, next) => {
    if (req.user.role !== 'admin') {
        return res.status(403).json({ error: 'Forbidden' });
    }
    next();
};
```

### 4. Activity Logging
Log all commands and their execution:
```javascript
const commandLog = new CommandLog({
    commandId,
    issuedBy: req.user.email,
    issuedAt: new Date(),
    computerName,
    commandType
});
await commandLog.save();
```

---

## 🚀 Deployment Checklist

- [ ] Desktop client (.NET) compiles without errors
- [ ] All collectors (SystemInfo, InstalledApps, Control) initialized
- [ ] Backend API endpoints implemented
- [ ] MongoDB collections created
- [ ] API authentication configured
- [ ] SSL/TLS certificates installed
- [ ] Environment variables set in production
- [ ] Control panel HTML/CSS/JS added to new dashboard ONLY
- [ ] No modifications to old web_dashboard folder
- [ ] Database backups configured
- [ ] Logging enabled for audit trail
- [ ] Rate limiting configured for API
- [ ] CORS configured if needed

---

## 📊 Data Sync Flow

```
┌─────────────────────┐
│  Desktop Client     │
│  (.NET WinForms)    │
└──────────┬──────────┘
           │
           │ 1. Collect system info (every 5 min)
           │ 2. Collect installed apps (every 10 min)
           │ 3. Check for control commands (every 30 sec)
           │
           ▼
┌─────────────────────┐
│  Backend API        │
│  (Node.js/Express)  │
└──────────┬──────────┘
           │
           │ Store/Retrieve data
           │
           ▼
┌─────────────────────┐
│  MongoDB Database   │
└──────────┬──────────┘
           │
           │
           ▼
┌─────────────────────┐
│  Web Dashboard NEW  │
│  Control Panel      │
└─────────────────────┘
```

---

## 🔄 Command Execution Flow

```
Admin issues command in web UI
           │
           ▼
API creates ControlCommand in DB
           │
           ▼
Desktop client checks for commands (every 30 sec)
           │
           ▼
Client executes command locally
           │
           ▼
Client reports result back to API
           │
           ▼
Web dashboard shows execution status
```

---

## 📝 Command Types

| Command | Parameters | Effect |
|---------|------------|--------|
| `restart` | - | Restart computer |
| `shutdown` | - | Shutdown computer |
| `uninstall_app` | `appName` | Uninstall application |
| `block_application` | `appPath`, `appName` | Block app execution |
| `unblock_application` | `appPath` | Remove block from app |
| `lock_screen` | - | Lock user screen |
| `show_message` | `message` | Display message to user |
| `execute_command` | `command` | Run shell command |

---

## 🧪 Testing

### Test System Info Collection
```csharp
var collector = new SystemInfoCollector("https://localhost:5000", "test-key", "TestCo", "admin");
var info = collector.GatherSystemInfo();
Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
```

### Test API Endpoint
```bash
curl -X GET \
  'http://localhost:3000/api/system-info/test-key/COMPUTER-01' \
  -H 'x-api-key: your-api-key'
```

### Test Control Panel UI
Open `web_dashboard_new/index.html` and navigate to System Management tab.

---

## 📞 Troubleshooting

| Issue | Solution |
|-------|----------|
| Commands not executing | Check firewall, network connectivity |
| Apps not syncing | Verify API key, check internet connection |
| Dashboard not updating | Check auto-refresh toggle, browser console |
| Database errors | Verify MongoDB connection string |
| Slow performance | Implement caching, pagination on large datasets |

---

## 📦 Files Included

1. **Desktop Client (.NET)**
   - `SystemInfoCollector.cs` - Hardware/OS information
   - `InstalledAppsCollector.cs` - Application inventory
   - `SystemControlHandler.cs` - Command execution

2. **Backend (Node.js)**
   - `backend_systemManagementController.js` - API endpoints
   - `database_schema.js` - MongoDB schemas

3. **Web Dashboard (NEW ONLY)**
   - `web_control_panel_new_dashboard_only.js` - Control panel logic
   - `web_control_panel_html_new_dashboard_only.html` - HTML markup with CSS

---

## ⚡ Performance Notes

- System info syncs every 5 minutes
- Apps inventory syncs every 10 minutes
- Control commands checked every 30 seconds
- Dashboard auto-refresh interval: 30 seconds (configurable)
- Database indexes on frequently queried fields
- Implement pagination for large datasets (100+ devices)

---

## 🎓 Next Steps

1. Integrate desktop client modules into your WinForms app
2. Deploy backend API with MongoDB
3. Add control panel to NEW dashboard only
4. Configure API authentication & SSL
5. Test system-wide functionality
6. Monitor logs and optimize as needed
7. Train admins on control panel usage

---

**Version:** 1.0.0  
**Last Updated:** 2026-01-22  
**Status:** Production Ready  

For support, check logs in `/logs/system-management.log`
