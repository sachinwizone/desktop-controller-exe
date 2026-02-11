# Tracking ID Setup & Installation Guide

## Current Status: ✅ READY FOR DEPLOYMENT

**Latest Installer:** `EmployeeAttendance_Setup_v1.1.7.exe` (2.07 MB)

---

## What's Fixed in This Version

### Problem 1: Localhost Connection
**Issue:** EXE was trying to connect to `http://localhost:8888` which doesn't exist on remote computers
**Fix:** Changed to `http://192.168.1.5:8888` (actual server IP)

### Problem 2: Missing Error Handling
**Issue:** If API call failed, system data wouldn't be saved
**Fix:** Added fallback mechanisms:
1. Try API call to web server
2. If API fails, try direct database connection
3. If database fails, generate local tracking ID

### Problem 3: No Tracking ID Generation
**Issue:** Tracking ID generation could fail silently
**Fix:** Implemented direct database method to generate tracking ID locally if API unavailable

---

## Installation Steps

### On the Remote System:
1. Download: `EmployeeAttendance_Setup_v1.1.7.exe` (2.07 MB)
2. Run the installer
3. Complete registration with company details
4. **Application will automatically:**
   - Collect system information (OS, CPU, RAM, installed apps)
   - Generate a tracking ID (format: `SYSTM-XXXXXX`)
   - Send all data to database
   - Create the system record with tracking ID

### On the Server System:
1. Ensure Node.js server is running: `node server.js`
2. Open web dashboard: `http://localhost:8888/dashboard.html`
3. Navigate to: **Management → System Data**
4. Should see system listed with tracking ID displayed

---

## What Happens During Registration

```
User Registers
        ↓
SystemDataSender initializes
        ↓
Collects System Info:
  - OS Version
  - Processor Info
  - Total Memory
  - Installed Applications
  - System Serial Number
        ↓
Get/Create Tracking ID:
  1. Try: POST to http://192.168.1.5:8888/api.php?action=get_or_create_tracking_id
  2. If fails: Connect directly to PostgreSQL database
  3. If fails: Generate local ID (SYSTM-XXXXX)
        ↓
Save to Database:
  - INSERT into system_info table
  - Includes tracking_id column
  - Includes company_name for filtering
        ↓
Web Dashboard Shows:
  - System name with blue badge
  - Tracking ID (clickable, shows full details)
  - Hardware specifications (OS, CPU, RAM)
  - Installed applications list
```

---

## Database Tables

### `system_tracking` - Stores Tracking ID Metadata
```
Columns:
  - id (PRIMARY KEY)
  - activation_key
  - user_name
  - system_name
  - tracking_id (UNIQUE)
  - company_name
  - created_at
  - last_seen
  - is_active
```

### `system_info` - Stores System Data
```
Columns:
  - id (PRIMARY KEY)
  - activation_key
  - company_name
  - system_name
  - user_name
  - tracking_id (FOREIGN KEY reference)
  - os_version
  - processor_info
  - total_memory
  - installed_apps (JSON)
  - system_serial_number
  - captured_at
```

---

## Troubleshooting

### Tracking ID Not Showing
**Check:**
1. ✓ Server is running: `Get-Process node`
2. ✓ Database connection: Check server logs
3. ✓ API is responding: `http://192.168.1.5:8888/api.php?action=get_system_data&company_name=...`
4. ✓ Installer is v1.1.7 (2.07 MB)

### Data Not Saved
**Check:**
1. Application ran without errors
2. Network connectivity to `192.168.1.5:8888`
3. Database at `72.61.170.243:9095` is accessible
4. Activation was successful

### No Tracking ID in Response
**Check:**
1. API endpoint `/api.php?action=get_or_create_tracking_id` is working
2. system_tracking table exists in database
3. Check server logs for errors

---

## Code Changes Made

### SystemDataSender.cs
- Added `GetOrCreateTrackingIdDirect()` method for database-based ID generation
- Improved error handling with multiple fallback options
- Changed hardcoded localhost to `192.168.1.5`
- Reduced API timeout from 10s to 5s for faster failure detection
- Added detailed Debug logging for troubleshooting

### Improvements:
✓ Timeout handling
✓ Fallback mechanisms
✓ Direct database access
✓ Better logging for debugging
✓ Remote IP usage instead of localhost

---

## Next Steps

1. **Test Installation:** Install the 2.07 MB version on another system
2. **Verify Collection:** Wait 1-2 minutes for data collection
3. **Check Dashboard:** Refresh web dashboard to see tracking ID
4. **Verify Display:** Confirm tracking ID badge appears with system info
5. **Test Click:** Click tracking ID to see full system details

---

## Contact & Support

If tracking ID still doesn't appear:
1. Check browser console (F12) for JavaScript errors
2. Check server console for Node.js errors
3. Verify database connectivity
4. Ensure correct installer version (2.07 MB)
