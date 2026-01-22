# 🌐 Multi-Browser Web Tracking System - Complete Documentation

## Overview
This system tracks web browsing activity across different browsers, tabs, and windows with complete session tracking and detailed analytics.

---

## ✅ What's Tracked

### 1. **Browser Information**
- **Browser Type**: Chrome, Firefox, Safari, Edge, Opera, etc.
- **Browser Version**: Exact version number (e.g., Chrome 120.0.6099)
- **User Agent**: Full browser identification string

### 2. **Session & Tab Details**
- **Session ID**: Unique identifier for entire browser session (persists across all tabs in same browser)
- **Tab ID**: Unique identifier for each individual browser tab
- **Window ID**: Unique identifier for browser window instance
- **Device Fingerprint**: Identifies different device/browser installations

### 3. **Website Activity**
- **URL**: Full website address visited
- **Page Title**: HTML page title
- **Category**: Auto-categorized (Development, Email, Communication, etc.)
- **Referrer URL**: Source page that led to this site
- **Visit Duration**: Time spent on page (in seconds)

### 4. **Timestamp & Location**
- **Visit Time**: Exact timestamp (with timezone: Asia/Kolkata)
- **IP Address**: User's IP address
- **System Name**: Employee's system/computer name
- **Username**: Employee username

---

## 🗄️ Database Enhancement

### New Columns Added to `web_logs` Table

```sql
CREATE TABLE web_logs (
    -- Existing columns
    id SERIAL PRIMARY KEY,
    username VARCHAR(255),
    system_name VARCHAR(255),
    website_url VARCHAR(1024),
    page_title VARCHAR(1024),
    category VARCHAR(100),
    visit_time TIMESTAMP WITH TIME ZONE,
    ip_address VARCHAR(45),
    company_name VARCHAR(255),
    activation_key VARCHAR(255),
    
    -- NEW COLUMNS FOR MULTI-BROWSER TRACKING
    browser_type VARCHAR(50) DEFAULT 'Unknown',           -- Chrome, Firefox, Safari, Edge
    browser_version VARCHAR(50) DEFAULT 'Unknown',        -- Version number
    tab_id VARCHAR(100),                                  -- Unique per tab
    window_id VARCHAR(100),                               -- Unique per window
    session_id VARCHAR(100),                              -- Unique per browser session
    device_fingerprint VARCHAR(255),                      -- Identifies different devices
    user_agent TEXT,                                      -- Full user agent string
    referrer_url VARCHAR(1024),                           -- Referrer URL
    is_active BOOLEAN DEFAULT false,                      -- Flag for ongoing activities
    duration_seconds INTEGER DEFAULT 0                    -- Time spent on page
);

-- Performance indexes
CREATE INDEX idx_web_logs_session_id ON web_logs(session_id);
CREATE INDEX idx_web_logs_browser ON web_logs(browser_type);
CREATE INDEX idx_web_logs_tab ON web_logs(tab_id);
```

### Setup Instructions

1. **Run Database Migration**:
   ```bash
   node enhance_web_logs_schema.js
   ```

2. **What This Does**:
   - ✅ Adds 9 new columns to track browser/session details
   - ✅ Creates 3 performance indexes
   - ✅ Maintains backward compatibility with existing data
   - ✅ Supports multi-browser tracking for same user

---

## 📱 API Endpoints

### 1. **Log Web Activity** (New)
**Endpoint**: `POST /api.php?action=log_web_activity`

**Request Body**:
```json
{
    "activation_key": "your-activation-key",
    "username": "john.doe",
    "system_name": "DESKTOP-ABC123",
    "website_url": "https://github.com/example",
    "page_title": "GitHub Example Repository",
    "category": "Development",
    "browser_type": "Chrome",
    "browser_version": "120.0.6099",
    "tab_id": "tab_1704121234_abc123def",
    "window_id": "win_1704121234_xyz789",
    "session_id": "sess_1704121234_session123",
    "device_fingerprint": "fp_a1b2c3d4e5f6",
    "user_agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
    "referrer_url": "https://google.com",
    "duration_seconds": 120
}
```

**Response**:
```json
{
    "success": true,
    "message": "Web activity logged",
    "log_id": 12345,
    "company": "Your Company",
    "browser": "Chrome 120.0.6099",
    "session_id": "sess_1704121234_session123",
    "tab_id": "tab_1704121234_abc123def"
}
```

### 2. **Get Detailed Web Logs** (Enhanced)
**Endpoint**: `GET /api.php?action=get_web_logs_detailed&activation_key=...`

**Parameters**:
- `activation_key`: Company activation key (required)
- `username`: Filter by employee (optional)
- `date_from`: Start date YYYY-MM-DD (default: today)
- `date_to`: End date YYYY-MM-DD (default: today)
- `browser`: Filter by browser type (optional: Chrome, Firefox, Safari, etc.)
- `session_id`: Filter by specific session (optional)

**Response**:
```json
{
    "success": true,
    "company": "Your Company",
    "count": 150,
    "logs": [
        {
            "id": 12345,
            "username": "john.doe",
            "system_name": "DESKTOP-ABC123",
            "website": "https://github.com/example",
            "title": "GitHub Example",
            "category": "Development",
            "browser": "Chrome",
            "browser_type": "Chrome",
            "browser_version": "120.0.6099",
            "tab_id": "tab_1704121234_abc",
            "window_id": "win_1704121234_xyz",
            "session_id": "sess_1704121234_001",
            "device_fingerprint": "fp_a1b2c3d4e5f6",
            "referrer": "https://google.com",
            "ip_address": "192.168.1.100",
            "duration": 120,
            "timestamp": "2024-01-01T10:30:00+05:30",
            "is_active": false
        }
    ],
    "session_summary": [
        {
            "session_id": "sess_1704121234_001",
            "browser": "Chrome",
            "browser_version": "120.0.6099",
            "device_fingerprint": "fp_a1b2c3d4e5f6",
            "tab_count": 5,
            "total_duration": 3600,
            "first_visit": "2024-01-01T09:00:00",
            "last_visit": "2024-01-01T10:30:00",
            "websites_visited": ["github.com", "stackoverflow.com", "gmail.com"]
        }
    ]
}
```

---

## 🔧 Client-Side Tracking Script

### Installation

1. **Add Script to Web Dashboard**:
   ```html
   <script src="/multi_tab_tracker.js"></script>
   ```

2. **Features**:
   - ✅ Detects browser type and version automatically
   - ✅ Generates unique session/tab/window IDs
   - ✅ Tracks page visits in real-time
   - ✅ Monitors tab visibility (active/background)
   - ✅ Auto-categorizes websites
   - ✅ Sends heartbeat every 30 seconds
   - ✅ Sends data on page unload

### How It Works

```javascript
// Initialize tracker
window.webTracker = new MultiTabWebTracker();

// Get session info
window.webTracker.getSessionInfo();
// Returns: { sessionId, tabId, windowId, browser, isActive, duration }

// Get analytics
window.webTracker.getSessionAnalytics();
// Returns: Complete session and device info
```

### Browser Detection

```
Chrome   → 🌐 Chrome 120.0.6099
Firefox  → 🦊 Firefox 121.0
Safari   → 🍎 Safari 17.2
Edge     → ⌃ Edge 121.0
Opera    → 🎭 Opera 108.0
```

---

## 📊 Dashboard Features

### Web Logs View with Enhanced Details

**Columns Displayed**:
1. **User** - Employee name
2. **System** - Computer/system name
3. **Website** - Domain visited
4. **Page Title** - HTML page title
5. **Browser** - Browser type with color coding
6. **Category** - Auto-categorized activity type
7. **Tab ID** - Short tab identifier
8. **Session** - Short session identifier
9. **Duration** - Time spent (HH:MM:SS)
10. **Timestamp** - Visit time with timezone

### Filters Available

- **Date Range**: Filter by date (from/to)
- **Search**: Search URLs, domains, page titles
- **Browser Type**: Filter by Chrome, Firefox, Safari, Edge, Opera
- **Employee**: Filter by username

### Sample Dashboard Display

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ User    │ System      │ Website        │ Page Title    │ Browser │ Category  │
├─────────────────────────────────────────────────────────────────────────────┤
│ john    │ DESK-01     │ github.com     │ GitHub        │ Chrome  │ Dev      │
│ mary    │ DESK-02     │ gmail.com      │ Gmail          │ Firefox │ Email    │
│ peter   │ DESK-03     │ youtube.com    │ YouTube        │ Safari  │ Entertain│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Use Cases

### 1. **Track Across Multiple Browsers**
```
Same User: john.doe
Browser 1 (Chrome):  Session A, Tabs 1-3, Different Device Fingerprint
Browser 2 (Firefox): Session B, Tabs 1-2, Same Device Fingerprint
```

### 2. **Multi-Tab Monitoring**
```
Chrome Session:
  └─ Tab 1: github.com (10 min)
  └─ Tab 2: gmail.com (5 min)
  └─ Tab 3: stackoverflow.com (15 min)
  └─ Tab 4: google.com (2 min)
```

### 3. **Device Fingerprinting**
```
john.doe on Laptop:
  Device FP: fp_a1b2c3d4e5f6 (Windows 10, Chrome)
  
john.doe on Desktop:
  Device FP: fp_z9y8x7w6v5u4 (Windows 11, Firefox)
```

### 4. **Session Analysis**
```
Session ID: sess_1704121234_001
├─ Browser: Chrome 120
├─ Duration: 1 hour 30 min
├─ Tabs: 5
├─ Sites: github.com, gmail.com, slack.com
└─ Device: Desktop (Windows)
```

---

## 🔒 Security & Privacy

### Data Protection
- ✅ Activation key required for logging
- ✅ Only company-authorized access to logs
- ✅ IP address logged for audit trail
- ✅ User agent stored for forensic analysis

### Compliance
- ✅ GDPR compliant (audit logging)
- ✅ PII minimized (only username, not email)
- ✅ Device fingerprint doesn't store hardware ID
- ✅ Session IDs are temporary (session-based)

---

## 📈 Query Examples

### Find All Tabs Used by User in Date Range
```sql
SELECT DISTINCT tab_id, browser_type, session_id, COUNT(*) as visits
FROM web_logs
WHERE username = 'john.doe' 
  AND visit_time::date BETWEEN '2024-01-01' AND '2024-01-31'
GROUP BY tab_id, browser_type, session_id
ORDER BY visits DESC;
```

### Find Multi-Browser Activity
```sql
SELECT DISTINCT 
    username, 
    browser_type, 
    device_fingerprint, 
    COUNT(DISTINCT session_id) as sessions
FROM web_logs
WHERE visit_time::date = CURRENT_DATE
GROUP BY username, browser_type, device_fingerprint
HAVING COUNT(DISTINCT browser_type) > 1
ORDER BY username;
```

### Find Active Sessions
```sql
SELECT session_id, browser_type, COUNT(*) as page_views, 
       MAX(visit_time) as last_activity
FROM web_logs
WHERE is_active = true
  AND visit_time > NOW() - INTERVAL '30 minutes'
GROUP BY session_id, browser_type
ORDER BY last_activity DESC;
```

---

## 📝 Implementation Checklist

- [x] Database schema enhanced with 9 new columns
- [x] API endpoints created (log_web_activity, get_web_logs_detailed)
- [x] Client-side tracking script (multi_tab_tracker.js)
- [x] Dashboard updated with enhanced web logs view
- [x] Browser detection and categorization
- [x] Session/tab/window ID generation
- [x] Device fingerprinting
- [x] Performance indexes created
- [x] Filter options added to dashboard

---

## 🚀 Getting Started

1. **Run Database Migration**:
   ```bash
   node web_dashboard/enhance_web_logs_schema.js
   ```

2. **Add Tracking Script to Dashboard**:
   ```html
   <script src="/web_dashboard/multi_tab_tracker.js"></script>
   ```

3. **Access Enhanced Web Logs**:
   - Go to Admin Dashboard → Web Browsing Logs
   - See all columns: User, System, Website, Title, Browser, Category, Tab ID, Session, Duration, Timestamp

4. **Monitor Multi-Browser Activity**:
   - Filter by browser type (Chrome, Firefox, Safari, etc.)
   - View session summary with tab count and duration
   - Track same user across different browsers

---

## 📞 Support

For issues or questions:
- Check browser console for tracking logs (window.webTracker)
- Review database logs in web_logs table
- Monitor session_summary output from API endpoint
- Contact: support@company.com

---

**Version**: 1.0.0  
**Last Updated**: January 2024  
**Status**: ✅ Production Ready
