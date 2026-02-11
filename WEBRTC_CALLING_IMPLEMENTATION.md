# WebRTC Voice/Video Calling - Implementation Guide

## Overview
Complete WebRTC-based calling system between web dashboard and desktop EXE application.

## What Was Fixed

### 1. **EXE Side - Incoming Call Form**
**Problem:**
- Buttons not showing properly in the incoming call notification
- Call accept action not establishing connection

**Solution:**
- Redesigned the incoming call form with proper layout and sizing
- Added better icon display with emoji support
- Fixed button placement and styling
- Implemented proper UI thread handling to avoid rendering issues
- Added call status updates (answered, rejected, missed)

**File Modified:** `EmployeeAttendance\TrayChatSystem.cs`

**Key Changes:**
- Improved `ShowIncomingCallForm()` method to use main UI thread
- New `ShowIncomingCallFormUI()` method with better layout:
  - Form size: 400x280px
  - Circular icon panel with emoji support
  - Centered caller name and call type
  - Large, visible Accept (green) and Reject (red) buttons
  - Auto-close timer (30 seconds) with missed call status

### 2. **WebRTC Call Interface**
**Problem:**
- No actual call interface after accepting the call
- Missing audio/video communication functionality

**Solution:**
- Created dedicated call interface page (`call.html`)
- Implemented full WebRTC peer-to-peer connection
- Added video display with local preview
- Added audio-only mode with visual indicator
- Implemented call controls (mute, video toggle, end call)

**New File:** `web_dashboard_new\web_dashboard_new\call.html`

**Features:**
- **Audio Calls:** Microphone audio with animated indicator
- **Video Calls:** Full video with local preview in corner
- **Call Controls:**
  - Mute/Unmute microphone
  - Video on/off toggle
  - End call button
- **Call Timer:** Shows duration of call
- **Status Updates:** Real-time connection status
- **Error Handling:** Displays permission errors and connection issues

### 3. **Call Flow Integration**

#### When Web User Calls EXE User:
1. Web user clicks call button in chat
2. Server creates call record in `call_signaling` table
3. EXE polls database (every 5 seconds) via `CheckForNewMessages()`
4. EXE detects incoming call
5. EXE shows notification balloon
6. EXE displays incoming call form with Accept/Reject buttons
7. User clicks Accept:
   - Updates database status to 'answered'
   - Opens browser window with `call.html`
   - WebRTC connection established

#### When EXE User Calls Web User:
1. EXE user would initiate call (to be implemented in future)
2. Same flow but reversed roles

## Database Schema

```sql
CREATE TABLE call_signaling (
    id SERIAL PRIMARY KEY,
    caller VARCHAR(255) NOT NULL,
    callee VARCHAR(255) NOT NULL,
    company_name VARCHAR(255),
    call_type VARCHAR(20) DEFAULT 'audio',  -- 'audio' or 'video'
    status VARCHAR(20) DEFAULT 'ringing',   -- 'ringing', 'answered', 'rejected', 'ended', 'missed'
    signal_data TEXT,                        -- WebRTC SDP offer/answer
    ice_candidates TEXT DEFAULT '[]',        -- ICE candidates for NAT traversal
    started_at TIMESTAMP DEFAULT NOW(),
    answered_at TIMESTAMP,
    ended_at TIMESTAMP
);
```

## API Endpoints Used

### 1. `initiate_call`
- **Purpose:** Start a new call
- **Parameters:** `caller`, `callee`, `company_name`, `call_type`
- **Returns:** `call_id`

### 2. `update_call_signal`
- **Purpose:** Update call with WebRTC signals and status
- **Parameters:** `call_id`, `signal_data`, `ice_candidate`, `status`

### 3. `get_call_signal`
- **Purpose:** Get call information and signals
- **Parameters:** `call_id`
- **Returns:** Call data with signals

### 4. `check_incoming_call`
- **Purpose:** Check for incoming calls (used by EXE)
- **Parameters:** `callee`, `company_name`
- **Returns:** Pending call information

## WebRTC Connection Flow

### Caller Side (Web):
1. Request user media (camera/microphone)
2. Create RTCPeerConnection
3. Add local tracks to connection
4. Create SDP offer
5. Send offer to server
6. Wait for answer from callee
7. Set remote description
8. Add ICE candidates
9. Connection established

### Callee Side (EXE):
1. Accept call notification
2. Open call.html in browser
3. Request user media
4. Create RTCPeerConnection
5. Get offer from server
6. Set remote description (offer)
7. Create SDP answer
8. Send answer to server
9. Add ICE candidates
10. Connection established

## Testing Guide

### Prerequisites:
1. Web dashboard running on port 3000
2. PostgreSQL database accessible
3. EXE application running on target machine
4. Both devices on same network or have STUN servers configured

### Test Steps:

#### 1. Test Incoming Call Notification:
```bash
# From web dashboard, select an employee and click call button
# Expected: EXE shows notification balloon and incoming call form
```

#### 2. Test Accept Call:
- Accept button should be visible and clickable
- Browser window should open with call interface
- Local video/audio should display
- Timer should start

#### 3. Test Reject Call:
- Reject button should be visible and clickable
- Call status should update to 'rejected'
- Form should close

#### 4. Test Auto-Reject (Missed Call):
- Don't click any button for 30 seconds
- Call status should update to 'missed'
- Form should close automatically

#### 5. Test Call Controls:
- **Mute:** Should disable audio
- **Video Toggle:** Should enable/disable video
- **End Call:** Should close connection and window

### Debugging:

#### Check EXE Logs:
```
Debug.WriteLine messages in TrayChatSystem.cs:
- "[TrayChatSystem] Incoming {callType} call from {caller}"
- "[TrayChatSystem] Call {callId} accepted from {caller}"
- "[TrayChatSystem] Call {callId} rejected from {caller}"
```

#### Check Browser Console:
```javascript
// In call.html, check for:
- 'Received remote track'
- 'Sending ICE candidate'
- 'Connection state: connected'
```

#### Check Database:
```sql
-- Check call records
SELECT * FROM call_signaling ORDER BY started_at DESC LIMIT 10;

-- Check for ringing calls
SELECT * FROM call_signaling WHERE status = 'ringing' AND started_at > NOW() - interval '1 minute';
```

## Known Limitations & Future Enhancements

### Current Limitations:
1. **No Call from EXE:** EXE users can only receive calls, not initiate them
2. **Browser Dependency:** EXE users need a browser for the call interface
3. **STUN Only:** No TURN server configured for strict NAT environments
4. **No Call History UI:** Call history exists in database but no UI to view it

### Future Enhancements:
1. **Native Call Interface:** Embed WebRTC in WinForms (using CefSharp or WebView2)
2. **Call Initiation from EXE:** Add call button in ChatForm
3. **Call History:** Show past calls with duration and status
4. **Group Calls:** Support multiple participants
5. **Screen Sharing:** Add screen share option
6. **Call Recording:** Record calls to server
7. **TURN Server:** Add TURN server for better NAT traversal
8. **Mobile Support:** Add mobile app with call support

## File Locations

```
EXE - DESKTOP CONTROLLER/
├── EmployeeAttendance/
│   └── TrayChatSystem.cs          # Incoming call handling (MODIFIED)
└── web_dashboard_new/web_dashboard_new/
    ├── app.js                      # Web calling interface (EXISTING)
    ├── server.js                   # Call signaling APIs (EXISTING)
    └── call.html                   # WebRTC call interface (NEW)
```

## Configuration

### API Base URL
**EXE Side:**
```csharp
private const string API_BASE = "http://72.61.235.203:3000";
```

**Web Side:**
```javascript
const API_BASE = 'http://72.61.235.203:3000/api';
```

### STUN Servers
```javascript
iceServers: [
    { urls: 'stun:stun.l.google.com:19302' },
    { urls: 'stun:stun1.l.google.com:19302' }
]
```

### Polling Interval (EXE)
```csharp
private const int POLL_INTERVAL_MS = 5000; // 5 seconds
```

## Troubleshooting

### Problem: Call notification not showing on EXE
**Solution:**
- Check if TrayChatSystem is initialized with correct user and company
- Verify database connectivity
- Check poll timer is running
- Verify call record exists in database with status 'ringing'

### Problem: Accept button not visible
**Solution:**
- Form now uses proper ClientSize and DPI scaling
- Buttons are positioned at fixed locations with adequate spacing
- Ensure the form is shown on the main UI thread

### Problem: Browser window doesn't open after accepting
**Solution:**
- Check API base URL is correct in TrayChatSystem.cs
- Verify call.html file exists in web_dashboard_new folder
- Check browser default app settings

### Problem: No audio/video in call
**Solution:**
- Grant camera/microphone permissions in browser
- Check WebRTC connection state in browser console
- Verify both users are using supported browsers (Chrome, Edge, Firefox)
- Check firewall settings for WebRTC traffic

### Problem: Connection stuck on "Connecting..."
**Solution:**
- Verify STUN servers are reachable
- Check signaling data is being sent/received
- Verify both peers are sending ICE candidates
- May need TURN server for strict NAT environments

## Summary

The calling feature is now fully functional with:
- ✅ Proper incoming call notifications with clear Accept/Reject buttons
- ✅ WebRTC-based voice and video calling
- ✅ Real-time peer-to-peer communication
- ✅ Call controls (mute, video toggle, end)
- ✅ Call status tracking (ringing, answered, rejected, missed, ended)
- ✅ Auto-timeout for missed calls
- ✅ Browser-based call interface with professional UI

Users can now make voice/video calls from the web dashboard to EXE users, who will receive a notification and can accept/reject the call with a fully functional WebRTC interface.
