# 🚀 System Management - Quick Start Guide

## For Developers - Copy & Paste Ready Code

---

## Desktop Client Integration

### Add to Program.cs Main()

```csharp
using EmployeeAttendance;
using System;

static class Program
{
    private static SystemInfoCollector? systemInfoCollector;
    private static InstalledAppsCollector? appsCollector;
    private static SystemControlHandler? controlHandler;

    [STAThread]
    static void Main()
    {
        // Your existing code...
        
        // Initialize system monitoring
        InitializeSystemMonitoring();

        Application.EnableVisualStyles();
        Application.Run(new MainDashboard());
    }

    private static void InitializeSystemMonitoring()
    {
        try
        {
            // Configuration
            string serverUrl = "https://your-api-server.com"; // Change this
            string activationKey = "your-activation-key-here"; // Change this
            string companyName = GetCompanyFromSettings(); // Get from your settings
            string userName = Environment.UserName;

            // Initialize collectors
            systemInfoCollector = new SystemInfoCollector(
                serverUrl, activationKey, companyName, userName
            );
            systemInfoCollector.Start();
            System.Diagnostics.Debug.WriteLine("✓ System Info Collector started");

            appsCollector = new InstalledAppsCollector(
                serverUrl, activationKey
            );
            appsCollector.Start();
            System.Diagnostics.Debug.WriteLine("✓ Installed Apps Collector started");

            controlHandler = new SystemControlHandler(
                serverUrl, activationKey
            );
            controlHandler.Start();
            System.Diagnostics.Debug.WriteLine("✓ System Control Handler started");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing system monitoring: {ex.Message}");
        }
    }

    private static void OnApplicationExit(object? sender, EventArgs e)
    {
        systemInfoCollector?.Stop();
        appsCollector?.Stop();
        controlHandler?.Stop();
    }

    private static string GetCompanyFromSettings()
    {
        // Get from your existing settings/database
        return "Your Company Name";
    }
}
```

---

## Backend Setup

### 1. Install Dependencies

```bash
npm install express mongoose dotenv cors uuid
```

### 2. Update server.js / app.js

```javascript
const express = require('express');
const mongoose = require('mongoose');
require('dotenv').config();

const app = express();

// Middleware
app.use(express.json());
app.use(cors());

// MongoDB Connection
mongoose.connect(process.env.MONGODB_URI, {
    useNewUrlParser: true,
    useUnifiedTopology: true
}).then(() => console.log('MongoDB connected'))
  .catch(err => console.log('MongoDB error:', err));

// API Authentication
const authenticateApiKey = (req, res, next) => {
    const apiKey = req.headers['x-api-key'];
    if (!apiKey || apiKey !== process.env.API_KEY) {
        return res.status(401).json({ error: 'Unauthorized' });
    }
    next();
};

// Register System Management Routes
const systemManagementRouter = require('./controllers/systemManagementController');
app.use('/api', authenticateApiKey, systemManagementRouter);

// Start Server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});
```

### 3. Create .env file

```
API_KEY=your-super-secret-api-key-12345
MONGODB_URI=mongodb://localhost:27017/employee-attendance
PORT=3000
NODE_ENV=production
```

---

## Web Dashboard (NEW ONLY)

### Add to web_dashboard_new/index.html

In the `<head>`:
```html
<!-- System Control Panel CSS -->
<style>
/* Paste CSS from web_control_panel_html_new_dashboard_only.html */
</style>
```

Before `</body>`:
```html
<!-- System Control Panel JavaScript -->
<script src="web_control_panel_new_dashboard_only.js"></script>
<script>
    // Initialize Control Panel after page load
    document.addEventListener('DOMContentLoaded', function() {
        window.systemControl = new SystemManagementControlPanel(
            'https://your-api-server.com/api',  // Change this
            'your-api-key-here'  // Change this
        );
        
        // Set company
        localStorage.setItem('currentCompany', 'Your Company Name');
        localStorage.setItem('activationKey', 'user-activation-key');
    });
</script>
```

### Add Control Panel Container

Where you want the panel to appear:
```html
<!-- System Management Control Panel -->
<div id="systemControlPanel" class="system-control-panel">
    <!-- Paste HTML from web_control_panel_html_new_dashboard_only.html -->
</div>
```

---

## API Testing

### Test System Info Sync
```bash
curl -X POST http://localhost:3000/api/system-info/sync \
  -H "Content-Type: application/json" \
  -H "x-api-key: your-api-key" \
  -d '{
    "activationKey": "test-key",
    "companyName": "Test Corp",
    "computerName": "DESKTOP-01",
    "userName": "admin",
    "operatingSystem": {
      "name": "Windows 10",
      "version": "21H2"
    }
  }'
```

### Test Get Devices
```bash
curl -X GET http://localhost:3000/api/system-info/company/Test%20Corp \
  -H "x-api-key: your-api-key"
```

### Test Send Command
```bash
curl -X POST http://localhost:3000/api/control/commands \
  -H "Content-Type: application/json" \
  -H "x-api-key: your-api-key" \
  -d '{
    "activationKey": "test-key",
    "computerName": "DESKTOP-01",
    "type": "restart",
    "parameters": {}
  }'
```

---

## Troubleshooting

### Desktop Client Not Syncing

**Check:**
1. Server URL is correct and accessible
2. API key matches backend configuration
3. Internet connection is working
4. Check debug output in Visual Studio

**Fix:**
```csharp
// Enable debug logging
System.Diagnostics.Debug.WriteLine("Server URL: " + serverUrl);
System.Diagnostics.Debug.WriteLine("API Key: " + activationKey);
```

### Backend Not Receiving Data

**Check:**
1. MongoDB is running
2. API key in request header is correct
3. Port is not blocked by firewall

**Fix:**
```bash
# Check MongoDB
mongo
> use employee-attendance
> db.systeminfos.find()

# Check logs
tail -f server.log
```

### Dashboard Not Showing Data

**Check:**
1. API key is correct in JavaScript
2. Server URL is correct
3. Browser console for errors (F12)
4. Network tab to verify API calls

**Fix:**
```javascript
// Check in browser console
console.log('API URL:', systemControl.apiBaseUrl);
console.log('API Key:', systemControl.apiKey);

// Check network requests in F12
```

---

## File Locations Checklist

```
EmployeeAttendance/
├── SystemInfoCollector.cs              ✅ Desktop client
├── InstalledAppsCollector.cs           ✅ Desktop client
├── SystemControlHandler.cs             ✅ Desktop client
├── Program.cs                          ✅ Update Main()
│
backend/
├── controllers/
│   └── systemManagementController.js   ✅ API endpoints
├── models/
│   ├── SystemInfo.js                   ✅ MongoDB schema
│   ├── InstalledApp.js                 ✅ MongoDB schema
│   ├── ControlCommand.js               ✅ MongoDB schema
│   ├── CommandResult.js                ✅ MongoDB schema
│   └── ActiveUser.js                   ✅ MongoDB schema
├── app.js (or server.js)               ✅ Update routes
└── .env                                ✅ Configuration
│
web_dashboard_new/                      ⚠️  ONLY HERE!
├── index.html                          ✅ Add control panel
├── web_control_panel_new_dashboard_only.js
└── (CSS styles in HTML or separate)
│
web_dashboard/                          ❌ DO NOT MODIFY!
└── (Keep old dashboard untouched)
```

---

## Timeline

| Step | Time | Action |
|------|------|--------|
| 1 | Day 1 | Copy C# files, compile desktop client |
| 2 | Day 1 | Copy models to backend, test MongoDB |
| 3 | Day 2 | Copy API controller, test endpoints |
| 4 | Day 2 | Add control panel to NEW dashboard only |
| 5 | Day 3 | End-to-end testing |
| 6 | Day 3 | Deploy to production |
| 7 | Day 4 | Monitor logs and optimize |

---

## Success Indicators

✅ Desktop client compiles without errors  
✅ Collectors start without exceptions  
✅ MongoDB receives system info within 5 minutes  
✅ API endpoints respond with correct data  
✅ Web dashboard loads without console errors  
✅ All 4 tabs show data (Stats, Devices, Users, Apps)  
✅ Control commands execute on desktop client  
✅ Old dashboard (web_dashboard) is NOT modified  

---

## Emergency Reset

If something goes wrong, here's how to reset:

```bash
# MongoDB Reset
mongo
> use employee-attendance
> db.dropDatabase()

# API Reset
npm restart

# Client Reset
# Just close and restart EmployeeAttendance.exe
```

---

## Need Help?

Check these files in order:
1. `SYSTEM_MANAGEMENT_INTEGRATION_GUIDE.md` - Full documentation
2. `SYSTEM_MANAGEMENT_COMPLETE_SUMMARY.md` - Overview
3. Browser console (F12) - JavaScript errors
4. Server logs - Backend errors
5. MongoDB logs - Database errors

---

**Pro Tips:**
- Use Postman to test APIs
- Use MongoDB Compass to view database
- Use browser DevTools for frontend debugging
- Keep API keys in environment variables, never hardcode
- Test on development server before production

---

🎉 **You're ready to go!** Start with Desktop Client integration.

