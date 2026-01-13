# Web Dashboard API - Remote Live Viewing

## 🎯 System ID Format (Like AnyDesk/UltraViewer)

Each system gets a **9-digit ID** like: `123-456-789`

- ✅ Random and unique
- ✅ Persists across restarts (stored in Windows Registry)
- ✅ Displayed in application header (orange text)
- ✅ Click to copy to clipboard

---

## 📊 API Endpoints for Web Dashboard

### 1. Get All Online Systems
**List all systems connected to your company**

```sql
SELECT 
    system_id,
    system_name,
    username,
    system_username,
    ip_address,
    processor,
    ram_mb,
    os_version,
    is_online,
    last_seen,
    CASE 
        WHEN last_seen > (CURRENT_TIMESTAMP - INTERVAL '2 minutes') THEN 'online'
        ELSE 'offline'
    END as status
FROM system_registry
WHERE company_name = 'YOUR_COMPANY_NAME'
ORDER BY is_online DESC, last_seen DESC;
```

**Response Example:**
```json
[
  {
    "system_id": "123-456-789",
    "system_name": "SACHIN-GARG",
    "username": "admin@company.com",
    "system_username": "wizone it",
    "ip_address": "192.168.1.100",
    "processor": "Intel Core i7-9700K",
    "ram_mb": 16384,
    "os_version": "Microsoft Windows NT 10.0.19045.0",
    "is_online": true,
    "last_seen": "2026-01-01T12:30:45Z",
    "status": "online"
  }
]
```

---

### 2. Start Remote Viewing Session
**Begin watching a specific system**

```sql
INSERT INTO live_sessions (session_id, system_id, viewer_username, is_active)
VALUES (
    'session-' || EXTRACT(EPOCH FROM NOW())::TEXT || '-' || FLOOR(RANDOM() * 1000)::TEXT,
    '123-456-789',  -- System ID to view
    'admin@company.com',  -- Who is viewing
    TRUE
)
RETURNING session_id;
```

**Response:**
```json
{
  "session_id": "session-1704110445-742"
}
```

---

### 3. Get Latest Live Frame
**Fetch current screenshot from system**

```sql
SELECT 
    frame_data,
    capture_time,
    screen_width,
    screen_height,
    EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - capture_time)) as age_seconds
FROM live_stream_frames
WHERE system_id = '123-456-789'
ORDER BY capture_time DESC
LIMIT 1;
```

**Response:**
```json
{
  "frame_data": "/9j/4AAQSkZJRgABAQEAYABgAAD...",  // Base64 JPEG
  "capture_time": "2026-01-01T12:31:02Z",
  "screen_width": 3264,
  "screen_height": 1080,
  "age_seconds": 0.5
}
```

**Display in HTML:**
```html
<img id="liveView" src="data:image/jpeg;base64,FRAME_DATA_HERE" />
```

---

### 4. Stop Remote Viewing Session
**End viewing session**

```sql
UPDATE live_sessions
SET is_active = FALSE, ended_at = CURRENT_TIMESTAMP
WHERE session_id = 'session-1704110445-742';
```

---

### 5. Check System Online Status
**Quick check if system is connected**

```sql
SELECT 
    system_id,
    is_online,
    last_seen,
    EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - last_seen)) as seconds_since_heartbeat
FROM system_registry
WHERE system_id = '123-456-789';
```

---

## 🌐 Complete Web Dashboard Flow

### Step 1: Systems List Page
```javascript
// Fetch all systems
fetch('/api/systems/list?company=YOUR_COMPANY')
  .then(res => res.json())
  .then(systems => {
    systems.forEach(system => {
      // Display each system with:
      // - System ID (123-456-789)
      // - Computer Name
      // - User
      // - IP Address
      // - Online/Offline badge
      // - "View Live" button
    });
  });
```

### Step 2: Start Viewing
```javascript
function startViewing(systemId) {
  // Create viewing session
  fetch('/api/sessions/start', {
    method: 'POST',
    body: JSON.stringify({ systemId, viewer: currentUser })
  })
  .then(res => res.json())
  .then(session => {
    sessionId = session.session_id;
    startLiveStream(systemId);
  });
}
```

### Step 3: Live Stream Loop
```javascript
let streamInterval;

function startLiveStream(systemId) {
  streamInterval = setInterval(() => {
    fetch(`/api/systems/${systemId}/frame`)
      .then(res => res.json())
      .then(frame => {
        // Update image
        document.getElementById('liveView').src = 
          `data:image/jpeg;base64,${frame.frame_data}`;
        
        // Update timestamp
        document.getElementById('timestamp').textContent = 
          new Date(frame.capture_time).toLocaleTimeString();
      });
  }, 2000); // Fetch every 2 seconds
}
```

### Step 4: Stop Viewing
```javascript
function stopViewing() {
  clearInterval(streamInterval);
  
  fetch(`/api/sessions/${sessionId}/stop`, {
    method: 'POST'
  });
}
```

---

## 🎨 Sample Web Dashboard UI

### Systems List (Bootstrap/Tailwind)
```html
<div class="container">
  <h1>🖥️ Online Systems</h1>
  
  <div class="systems-grid">
    <!-- For each system -->
    <div class="system-card">
      <div class="system-header">
        <span class="system-id">🔢 123-456-789</span>
        <span class="status-badge online">● Online</span>
      </div>
      
      <div class="system-info">
        <p><strong>Computer:</strong> SACHIN-GARG</p>
        <p><strong>User:</strong> admin@company.com</p>
        <p><strong>IP:</strong> 192.168.1.100</p>
        <p><strong>Last Seen:</strong> 2 seconds ago</p>
      </div>
      
      <div class="system-actions">
        <button onclick="startViewing('123-456-789')" class="btn-view">
          📺 View Live
        </button>
        <button onclick="viewHistory('123-456-789')" class="btn-history">
          📊 View History
        </button>
      </div>
    </div>
  </div>
</div>
```

### Live Viewer Page
```html
<div class="live-viewer">
  <div class="viewer-header">
    <h2>🔢 Viewing System: 123-456-789</h2>
    <span id="timestamp">Last update: 0s ago</span>
    <button onclick="stopViewing()" class="btn-stop">❌ Stop</button>
  </div>
  
  <div class="viewer-screen">
    <img id="liveView" alt="Live Screen" />
  </div>
  
  <div class="viewer-info">
    <p>Resolution: <span id="resolution">3264x1080</span></p>
    <p>Status: <span id="status" class="online">● Streaming</span></p>
  </div>
</div>
```

---

## 🔐 Security Considerations

### 1. Authentication
```javascript
// All API calls should include auth token
fetch('/api/systems/list', {
  headers: {
    'Authorization': `Bearer ${userToken}`,
    'Content-Type': 'application/json'
  }
});
```

### 2. Company Filtering
```sql
-- Backend should ALWAYS filter by company
WHERE company_name = @logged_in_user_company
```

### 3. Session Management
```sql
-- Auto-expire old sessions
DELETE FROM live_sessions
WHERE started_at < (CURRENT_TIMESTAMP - INTERVAL '10 minutes')
AND is_active = TRUE;
```

---

## 📱 Mobile-Friendly Features

```css
/* Responsive design */
@media (max-width: 768px) {
  .systems-grid {
    grid-template-columns: 1fr;
  }
  
  .viewer-screen img {
    max-width: 100%;
    height: auto;
  }
}
```

---

## ⚡ Performance Tips

### 1. Frame Compression
- Clients send 20% JPEG quality for speed
- ~50-100 KB per frame
- 2-second interval = ~25-50 KB/s bandwidth

### 2. Frame Caching
```javascript
let lastFrameTime = 0;

function fetchFrame(systemId) {
  fetch(`/api/systems/${systemId}/frame?since=${lastFrameTime}`)
    .then(res => res.json())
    .then(frame => {
      if (frame.capture_time > lastFrameTime) {
        updateDisplay(frame);
        lastFrameTime = frame.capture_time;
      }
    });
}
```

### 3. Auto-Cleanup
```sql
-- Run every minute
DELETE FROM live_stream_frames
WHERE capture_time < (CURRENT_TIMESTAMP - INTERVAL '5 minutes');
```

---

## 🧪 Testing

### 1. Register System
```bash
# Start application
publish_final\DesktopController.exe

# Check registry
reg query HKCU\SOFTWARE\DesktopController /v SystemId
```

### 2. Verify Registration
```sql
SELECT * FROM system_registry 
ORDER BY first_seen DESC 
LIMIT 5;
```

### 3. Test Live Viewing
```sql
-- Manually create session
INSERT INTO live_sessions (session_id, system_id, viewer_username, is_active)
VALUES ('test-session', '123-456-789', 'test', TRUE);

-- Wait 2-3 seconds, check for frames
SELECT * FROM live_stream_frames 
WHERE system_id = '123-456-789'
ORDER BY capture_time DESC 
LIMIT 3;
```

---

## 📋 Database Indexes (Already Created)

```sql
-- For fast lookups
CREATE INDEX idx_system_registry_system_id ON system_registry(system_id);
CREATE INDEX idx_system_registry_online ON system_registry(is_online);
CREATE INDEX idx_live_sessions_system_id ON live_sessions(system_id);
CREATE INDEX idx_live_stream_frames_system_id ON live_stream_frames(system_id);
CREATE INDEX idx_live_stream_frames_capture_time ON live_stream_frames(capture_time DESC);
```

---

## 🎯 Key Differences from AnyDesk/UltraViewer

| Feature | AnyDesk/UltraViewer | Desktop Controller |
|---------|---------------------|-------------------|
| **ID Format** | 9-10 digits | 9 digits (123-456-789) |
| **Connection** | P2P direct | Database-based |
| **Frame Rate** | 30+ FPS | 0.5 FPS (2 sec interval) |
| **Quality** | Adaptive | Fixed 20% JPEG |
| **Latency** | <100ms | 2-3 seconds |
| **Use Case** | Real-time control | Monitoring/audit |

---

## 📞 Next Steps

1. ✅ Install application on all systems
2. ✅ Each gets unique 9-digit ID
3. ✅ Build web dashboard with above APIs
4. ✅ Test live viewing
5. ✅ Add filters (company, online status)
6. ✅ Add history viewer for screenshots table

---

**Application Path:** `publish_final\DesktopController.exe`
**Database:** `72.61.170.243:9095/controller_application`
