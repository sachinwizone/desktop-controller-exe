# Desktop Controller - Remote Live Viewing Feature

## ✅ What's New

### 1. **Single Instance Enforcement** ✅
- ✅ **FIXED**: Only ONE application instance can run at a time
- ✅ **FIXED**: Screenshot timing issue resolved (exactly 1 per 10 seconds)
- Uses Mutex-based locking to prevent multiple launches
- Shows warning message if another instance is already running

### 2. **Remote Live Viewing System** 🆕
- Each system gets a unique ID based on hardware
- Systems auto-register in database on login
- Live streaming capability for remote desktop viewing
- Heartbeat system tracks which systems are online

---

## 📋 Database Tables Created

### 1. `system_registry`
Stores information about all registered systems:
- `system_id`: Unique identifier (MachineName + MachineID)
- `machine_id`: Hardware-based unique ID
- `system_name`: Computer name
- `activation_key`: License key
- `company_name`: Company name
- `username`: Logged in user
- `system_username`: Windows username
- `ip_address`: Current IP
- `os_version`: Windows version
- `processor`: CPU info
- `ram_mb`: RAM in MB
- `is_online`: Currently online status
- `first_seen`: First registration time
- `last_seen`: Last heartbeat time

### 2. `live_sessions`
Tracks active remote viewing sessions:
- `session_id`: Unique session identifier
- `system_id`: Which system is being viewed
- `viewer_username`: Who is viewing
- `started_at`: Session start time
- `ended_at`: Session end time
- `is_active`: Session active status
- `last_frame_time`: Last frame timestamp

### 3. `live_stream_frames`
Stores live screenshot frames for viewing:
- `system_id`: System being captured
- `frame_data`: Base64 JPEG screenshot (low quality 20%)
- `capture_time`: When captured
- `screen_width`: Screen width
- `screen_height`: Screen height
- `expires_at`: Auto-delete after 5 minutes

---

## 🔧 How It Works

### System Registration (Automatic)
1. When application starts and user logs in
2. System auto-registers with unique ID
3. Hardware info (CPU, RAM, OS) stored in database
4. System marked as "online"

### Heartbeat (Every 30 seconds)
- Updates `last_seen` timestamp
- Updates IP address
- Keeps system marked as "online"
- Backend can mark offline if no heartbeat for >2 minutes

### Live Streaming (Every 2 seconds when viewed)
1. Application checks if someone is viewing (queries `live_sessions`)
2. If active viewer:
   - Captures current screen
   - Compresses to JPEG (20% quality for speed)
   - Converts to Base64
   - Sends to `live_stream_frames` table
3. Old frames auto-deleted (keeps only 3 most recent per system)

### Screenshot Archival (Every 10 seconds)
- Separate from live viewing
- High quality JPEG (70%)
- Stored permanently in `screenshots` table
- Used for historical review

---

## 🌐 Web Interface Integration

### To View Systems List
```sql
SELECT 
    system_id,
    system_name,
    username,
    company_name,
    ip_address,
    is_online,
    last_seen,
    processor,
    ram_mb,
    os_version
FROM system_registry
WHERE company_name = 'YOUR_COMPANY'
ORDER BY is_online DESC, last_seen DESC;
```

### To Start Viewing Session
```sql
-- Create viewing session
INSERT INTO live_sessions (session_id, system_id, viewer_username, is_active)
VALUES ('SESSION_' || gen_random_uuid(), 'SYSTEM_ID', 'admin_username', TRUE);

-- Application will detect this and start streaming frames
```

### To Get Latest Frame
```sql
SELECT frame_data, capture_time, screen_width, screen_height
FROM live_stream_frames
WHERE system_id = 'SYSTEM_ID'
ORDER BY capture_time DESC
LIMIT 1;
```

### To Stop Viewing
```sql
UPDATE live_sessions
SET is_active = FALSE, ended_at = CURRENT_TIMESTAMP
WHERE session_id = 'YOUR_SESSION_ID';
```

---

## 📊 System IDs Format

**Format**: `{MachineName}-{HardwareHash}`

Example: `SACHIN-GARG-A3F2B1C4D5E6F7A8B9C0D1E2F3A4B5C6`

- First part: Computer name (e.g., "SACHIN-GARG")
- Second part: 32-character hash from CPU ID + BIOS Serial + Disk Serial
- Unique per physical machine
- Persists across reboots

---

## 🔐 Security Features

1. **Authentication Required**: Only logged-in users can register
2. **Company Filtering**: Systems tied to company via activation key
3. **Session Tracking**: All viewing sessions logged
4. **Frame Expiration**: Live frames auto-delete after 5 minutes
5. **Bandwidth Optimization**: Low quality (20%) for live viewing

---

## 🚀 Testing the Feature

### Step 1: Start Application
```
cd publish_remote_view
.\DesktopController.exe
```

### Step 2: Check System Registration
After login, check your debug log:
```
Desktop\DesktopController_Debug.txt
```

Look for: `=== SYSTEM REGISTERED: SACHIN-GARG-xxxxx ===`

### Step 3: Verify in Database
```sql
SELECT * FROM system_registry ORDER BY last_seen DESC LIMIT 5;
```

### Step 4: Simulate Remote Viewing
```sql
-- Insert a fake viewing session
INSERT INTO live_sessions (session_id, system_id, viewer_username, is_active)
VALUES ('test-session-' || extract(epoch from now()), 'YOUR_SYSTEM_ID', 'test_admin', TRUE);
```

### Step 5: Check for Live Frames
After 2-3 seconds:
```sql
SELECT 
    system_id,
    capture_time,
    screen_width,
    screen_height,
    length(frame_data) as frame_size_bytes
FROM live_stream_frames
WHERE system_id = 'YOUR_SYSTEM_ID'
ORDER BY capture_time DESC
LIMIT 3;
```

---

## 📁 File Locations

- **Application**: `publish_remote_view\DesktopController.exe`
- **Debug Log**: `%USERPROFILE%\Desktop\DesktopController_Debug.txt`
- **Database**: `72.61.170.243:9095/controller_application`

---

## ⚡ Performance

- **Heartbeat**: Every 30 seconds (~10 KB/minute)
- **Live Frames**: Every 2 seconds when viewing (~500 KB/second)
- **Screenshots**: Every 10 seconds (~200 KB/screenshot)
- **Frame Retention**: Only 3 most recent live frames kept
- **Auto-Cleanup**: Frames older than 5 minutes deleted

---

## 🛠️ Next Steps for Web Interface

### 1. Systems Dashboard Page
Show all registered systems with:
- System name
- Username
- Online/Offline status
- Last seen time
- "View Live" button

### 2. Live Viewer Page
- Fetch latest frame every 2 seconds
- Display in `<img src="data:image/jpeg;base64,{frame_data}">`
- Show system info (resolution, capture time)
- Stop button to end session

### 3. API Endpoints Needed
- `GET /api/systems/list` - List all systems for company
- `POST /api/systems/{systemId}/view/start` - Start viewing session
- `GET /api/systems/{systemId}/view/frame` - Get latest frame
- `POST /api/systems/{systemId}/view/stop` - Stop viewing session

---

## ✅ Fixed Issues

1. ✅ Multiple screenshot captures (3-4 per 10 seconds)
   - **Root Cause**: 3 instances running simultaneously
   - **Fix**: Mutex-based single instance enforcement

2. ✅ System identification
   - **Solution**: Unique hardware-based system ID

3. ✅ Remote viewing infrastructure
   - **Solution**: Complete database schema + live streaming

---

## 📞 Support

For issues or questions:
1. Check `DesktopController_Debug.txt` for errors
2. Verify system registration in `system_registry` table
3. Check heartbeat timestamps in `last_seen` column
4. Confirm live_stream_frames table has data when viewing

---

**Version**: 2.0.0 (Remote Viewing + Single Instance Fix)
**Date**: January 1, 2026
**Build**: publish_remote_view
