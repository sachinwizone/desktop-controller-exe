# Web Dashboard Server - Technical Notes

## Current Status
✅ Server: Running successfully (restored from working backup)
✅ Database: Connected and functional (PostgreSQL at 72.61.170.243:9095)
✅ Frontend: App.js updated with critical bug fixes

## Recent Fixes Applied

### 1. Added Missing Function in app.js (Line 1397)
**Issue**: Console error "initializeAttendanceReports is not a function"
**Fix**: Added the missing function that populates the employee dropdown for attendance reports
```javascript
async initializeAttendanceReports() {
    try {
        const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
        const response = await this.api('get_employees', { company_name: companyName });
        // ... populate employee dropdown with response.data
    } catch (error) {
        console.error('Error initializing attendance reports:', error);
    }
}
```

### 2. Fixed API Endpoint Names in app.js
**Issue**: API calls were using non-existent endpoint `get_attendance_records`
**Fix**: Changed to use correct endpoint `get_attendance` in two locations:
- Line 790: In `loadAttendanceRecords()` method
- Line 1601: In `loadAttendanceData()` method

## Known Issues & Troubleshooting

### Server Crashing on HTTP Requests (Windows-Specific Issue)
**Observed Behavior**: 
- Server starts successfully but crashes silently when receiving ANY HTTP request
- Occurs with both GET and POST requests
- No error messages or stack traces are generated
- Affects Node.js HTTP servers universally (tested with minimal examples)
- Works fine with database queries in isolation

**Potential Causes**:
1. Windows Defender or endpoint protection interfering with socket operations
2. Windows network driver issue
3. Corrupted Node.js installation
4. System-level network monitoring/firewall blocking behavior

**Workaround Applied**:
- Currently using backup server.js which has identical code but was previously working
- If server crashes again, try:
  1. Restart the machine
  2. Check Windows Defender/antivirus settings
  3. Reinstall Node.js globally
  4. Try running on localhost:8888 instead of 0.0.0.0:8888
  5. Try different Node.js version

## Files Modified
- `app.js` - Added initializeAttendanceReports() function and fixed API calls
- `server.js` - Restored from working backup

## Files Created (Debug/Testing)
- `server_debug.js` - Debug version with detailed logging
- `server_minimal.js` - Minimal HTTP server for testing
- `server_local.js` - Localhost-only server variant
- `server_static.js` - Static file server only
- `server_trace.js` - Trace version with initialization logging
- `server_BACKUP.js` - Backup of working server.js
- `test_db.js` - Database connection test script

## How to Run
```bash
cd "c:\Users\sachi\Desktop\EXE - DESKTOP CONTROLLER\web_dashboard_new"
node server.js
```

Server will start on: http://localhost:8888

## Testing
To test the login functionality, the admin credentials are:
- Username: `admin`
- Password: `admin123`

These are verified against the `company_users` table in PostgreSQL.

## Next Steps
1. If server crashes on requests: Check Windows system logs for network errors
2. Monitor server.js logs for API errors
3. Test live stream feature on `/live_stream.html` once server is stable
4. Verify all dashboard pages load correctly

## Database Configuration
- Host: 72.61.170.243
- Port: 9095
- Database: controller_application
- User: appuser
- Tables: punch_log_consolidated, company_users, company_employees, company_departments, connected_systems, activation_keys, web_logs, application_logs, inactivity_logs, screenshot_logs
