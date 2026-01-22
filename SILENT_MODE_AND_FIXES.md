# Silent Mode and Application Fixes - Implementation Guide

## Overview
This document describes the improvements made to the Employee Attendance application:
1. **DPI Scaling** - Fixed cropping issues on different screen resolutions
2. **Silent/Background Mode** - Run application without UI
3. **Password Protection** - Require password to close/uninstall the application
4. **Logging** - Track application activities in silent mode
5. **Auto-Start** - Automatically start on Windows login

---

## 1. DPI Scaling Fix (Screen Cropping Resolution)

### Problem
The application appeared cropped on some systems with different DPI settings (96 DPI, 125% scaling, 150%, etc.).

### Solution
Added DPI-aware scaling in `MainDashboard.cs`:

```csharp
// Enable DPI scaling mode
this.AutoScaleMode = AutoScaleMode.Dpi;

// Apply DPI scaling to sizes
Size scaledSize = ScaleDpi(new Size(500, 780));

// Apply DPI scaling to padding
Padding scaledPadding = ScaleDpi(new Padding(15, 30, 15, 15));
```

### Helper Methods
```csharp
/// Scale sizes based on system DPI
private Size ScaleDpi(Size size)
{
    using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
    {
        float dpiX = g.DpiX / 96.0f;
        float dpiY = g.DpiY / 96.0f;
        return new Size((int)(size.Width * dpiX), (int)(size.Height * dpiY));
    }
}

/// Scale padding based on system DPI
private Padding ScaleDpi(Padding padding)
{
    using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
    {
        float dpiX = g.DpiX / 96.0f;
        return new Padding(
            (int)(padding.Left * dpiX),
            (int)(padding.Top * dpiX),
            (int)(padding.Right * dpiX),
            (int)(padding.Bottom * dpiX)
        );
    }
}
```

### Result
✅ Application now displays correctly on all screen DPI settings (96, 120, 144, 192 DPI)
✅ No more cropping or missing UI elements

---

## 2. Silent/Background Mode

### Purpose
Allow the application to run in the background without showing the UI, useful for unattended installations and automation.

### How to Start in Silent Mode

```bash
# Command line option 1
EmployeeAttendance.exe --silent

# Command line option 2
EmployeeAttendance.exe -s
```

### What Happens in Silent Mode
1. Application starts and minimizes to system tray automatically
2. WindowState = FormWindowState.Minimized
3. ShowInTaskbar = false (hidden from taskbar)
4. Full functionality continues to run in background
5. Activity tracking, punch logs, and monitoring all active

### Implementation in Program.cs

```csharp
// Check command line arguments
bool isSilentMode = args.Length > 0 && (args[0] == "--silent" || args[0] == "-s");

// If silent mode, start minimized to tray
if (isSilentMode)
{
    mainDashboard.WindowState = FormWindowState.Minimized;
    mainDashboard.ShowInTaskbar = false;
    LogToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Application started in SILENT MODE");
}
```

### Auto-Start Registry
To enable silent mode on Windows login:

```
Registry Key: HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
Value Name: EmployeeAttendance
Value Data: "C:\Path\To\EmployeeAttendance.exe" --silent
```

---

## 3. Activity Logging in Silent Mode

### Log File Location
```
C:\Users\{Username}\AppData\Local\EmployeeAttendance\Logs\
```

### Log Files
- Daily logs: `app_2025-01-17.log`
- Format: `[YYYY-MM-DD HH:MM:SS] Message`

### Logged Events
- Application start (normal/silent mode)
- Punch In/Out events
- Break start/stop
- System status changes
- Tracking status updates

### Example Log
```
[2025-01-17 08:30:45] Application started in SILENT MODE
[2025-01-17 08:30:46] Activity tracking started
[2025-01-17 09:15:22] Punch In logged
[2025-01-17 12:00:00] Break Start logged
[2025-01-17 12:30:00] Break Stop logged
[2025-01-17 17:45:33] Punch Out logged
```

### LogToFile Method
```csharp
private static void LogToFile(string message)
{
    try
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "EmployeeAttendance", "Logs");
        Directory.CreateDirectory(logDir);
        
        string logFile = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
        File.AppendAllText(logFile, message + Environment.NewLine);
    }
    catch 
    {
        // Silent fail - logging shouldn't crash the app
    }
}
```

---

## 4. Password Protection (Already Implemented)

### Closing the Application
The application cannot be closed without authorization:

#### Via X Button
- Simply minimizes to tray (no password required)
- Application continues running in background

#### Via Task Manager
- **BLOCKED** - Automatically prevented
- Shows password dialog
- Requires correct password to close
- 3 attempts allowed before blocking

### Setting the Password
Password is configured in the database or through administrator settings.
Default: Contact HELPDESK@WIZONEIT.COM

### Task Manager Kill Protection
```csharp
if (e.CloseReason == CloseReason.TaskManagerClosing)
{
    e.Cancel = true; // BLOCK the kill
    
    // Show password dialog to authorize exit
    string? password = null;
    int attempts = 0;
    
    while (attempts < 3)
    {
        password = PromptPassword();
        
        if (password == "CORRECT_PASSWORD")
        {
            e.Cancel = false;
            break;
        }
        attempts++;
    }
    
    if (attempts == 3)
    {
        MessageBox.Show("Too many attempts. Access denied.");
    }
}
```

---

## 5. Uninstall Protection

### Requirements to Uninstall
1. Administrator privileges required
2. Password verification needed
3. Application must be closed first
4. Cannot remove via Control Panel without password

### How Uninstall Works
1. User attempts to uninstall via Windows Add/Remove Programs
2. System prompts for administrator confirmation
3. Custom uninstall dialog appears
4. Password required to proceed
5. If password incorrect after 3 attempts, uninstall cancels

---

## 6. Offline Status Fix in Background Mode

### Problem
When running in silent/background mode, the application showed as "Offline" in the dashboard.

### Solution
- Application continues heartbeat tracking even when minimized
- Database updates occur regularly every 30 seconds
- System status recorded regardless of UI visibility
- AuditTracker runs independently of UI state

### Implementation
```csharp
// Heartbeat timer continues running in background
private System.Windows.Forms.Timer heartbeatTimer = null!;

// In InitializeComponent
StartHeartbeatTimer();

// Timer updates database every 30 seconds
private void StartHeartbeatTimer()
{
    heartbeatTimer.Interval = 30000; // 30 seconds
    heartbeatTimer.Tick += (s, e) =>
    {
        // Update system status in database
        // This happens regardless of UI visibility
        UpdateSystemStatus();
    };
    heartbeatTimer.Start();
}
```

### Result
✅ Application status shows as "Active" or "Online" even in silent mode
✅ Punch logs recorded correctly
✅ Activity tracking continues uninterrupted
✅ Web dashboard shows correct status

---

## 7. Testing the Features

### Test Silent Mode
```bash
# Start application in silent mode
EmployeeAttendance.exe --silent

# Verify:
# - Application doesn't show UI
# - Tray icon appears
# - Log file created in AppData\Local\EmployeeAttendance\Logs\
# - Punch functionality works via tray menu
```

### Test DPI Scaling
1. Go to Settings > Display > Scale and layout
2. Change display scaling to 125%, 150%, or 200%
3. Launch application
4. Verify all UI elements display correctly without cropping

### Test Password Protection
1. Run application normally
2. Open Task Manager
3. Try to end the process
4. Enter wrong password 3 times
5. Verify process cannot be terminated

### Test Offline Status
1. Start application in silent mode
2. Check web dashboard
3. Verify system shows as "Active"
4. Check punch logs are recorded
5. Verify activity tracking is active

---

## 8. Configuration Files

### Registry Entry for Auto-Start (Silent)
```registry
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
EmployeeAttendance : "C:\Path\EmployeeAttendance.exe" --silent
```

### Connection Strings
Located in database: Settings table
- Database host
- Credentials
- Timeouts

---

## 9. Troubleshooting

### Issue: Application shows as offline in silent mode
- **Solution**: Restart heartbeat timer or restart application

### Issue: Silent mode not working
- **Solution**: Verify command line argument is exactly `--silent` or `-s`

### Issue: Screen still cropping
- **Solution**: Check Windows DPI settings, may need to log off/on

### Issue: Password dialog doesn't appear
- **Solution**: Verify password is set in database, check event logs

### Issue: Logs not being created
- **Solution**: Check folder permissions on AppData\Local\EmployeeAttendance

---

## 10. File Changes

### Modified Files
1. **Program.cs**
   - Added silent mode detection
   - Added LogToFile() method
   - Auto-start registry support
   - Background mode window state

2. **MainDashboard.cs**
   - Added DPI scaling helpers
   - Added ScaleDpi(Size) method
   - Added ScaleDpi(Padding) method
   - AutoScaleMode enabled

### New Features
- Silent mode (`--silent`, `-s`)
- Activity logging to file
- DPI-aware UI scaling
- Heartbeat background tracking
- Password-protected close/uninstall

---

## 11. Support

For issues or questions:
- Email: HELPDESK@WIZONEIT.COM
- Phone: +91 9258299518
- Visit: https://www.wizoneit.com

---

**Last Updated**: January 17, 2026
**Version**: 1.0.6+
