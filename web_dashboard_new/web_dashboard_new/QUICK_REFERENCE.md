# Quick Reference - Web Dashboard API Endpoints

## Running the Server
```bash
cd "c:\Users\sachi\Desktop\EXE - DESKTOP CONTROLLER\web_dashboard_new"
node server.js
```
Server runs on: `http://localhost:8889`

## Available Endpoints

### 1. Daily Working Hours
**Endpoint**: `POST /api.php?action=get_daily_working_hours`

**Request**:
```json
{
  "employee": "ashutosh",
  "date": "2026-01-19",
  "company_name": "WIZONE IT NETWORK INDIA PVT LTD"
}
```

**Response Fields**:
- `sessions[]`: Array of work sessions with punch times
- `total_work_hours`: Sum of all work duration for the day
- `total_break_minutes`: Total break time
- `first_punch_in`: First check-in time
- `last_punch_out`: Last check-out time
- `session_count`: Number of work sessions

---

### 2. Web Browsing Logs
**Endpoint**: `POST /api.php?action=get_web_logs`

**Request**:
```json
{
  "company_name": "WIZONE IT NETWORK INDIA PVT LTD",
  "employee": "ashutosh",
  "start_date": "2026-01-01",
  "end_date": "2026-01-20",
  "page": 1,
  "limit": 50
}
```

**Response Fields**:
- `records[]`: Web browsing sessions
  - `website_url`: URL visited
  - `page_title`: Page title
  - `duration_seconds`: Time on page
  - `browser_name`: Browser type
  - `domain`: Domain name

---

### 3. Application Usage Logs
**Endpoint**: `POST /api.php?action=get_application_logs`

**Request**:
```json
{
  "company_name": "WIZONE IT NETWORK INDIA PVT LTD",
  "employee": "ashutosh",
  "start_date": "2026-01-01",
  "end_date": "2026-01-20",
  "page": 1,
  "limit": 50
}
```

**Response Fields**:
- `records[]`: Application usage records
  - `app_name`: Application name
  - `window_title`: Active window title
  - `duration_seconds`: Usage duration
  - `is_active`: Currently active flag
  - `start_time`: Start timestamp
  - `end_time`: End timestamp

---

### 4. Inactivity Logs
**Endpoint**: `POST /api.php?action=get_inactivity_logs`

**Request**:
```json
{
  "company_name": "WIZONE IT NETWORK INDIA PVT LTD",
  "employee": "ashutosh",
  "start_date": "2026-01-01",
  "end_date": "2026-01-20",
  "page": 1,
  "limit": 50
}
```

**Response Fields**:
- `records[]`: Inactivity periods
  - `start_time`: Inactivity start
  - `end_time`: Inactivity end
  - `duration_seconds`: How long inactive
  - `status`: Inactivity status
  - `duration_minutes`: Duration in minutes

---

### 5. Screenshots
**Endpoint**: `POST /api.php?action=get_screenshots`

**Request**:
```json
{
  "company_name": "WIZONE IT NETWORK INDIA PVT LTD",
  "employee": "ashutosh",
  "start_date": "2026-01-01",
  "end_date": "2026-01-20",
  "page": 1,
  "limit": 50
}
```

**Response Fields**:
- `records[]`: Screenshot metadata
  - `screen_width`: Screen resolution width
  - `screen_height`: Screen resolution height
  - `has_screenshot`: Boolean flag if data exists
  - `log_time_formatted`: Timestamp
  - Note: Screenshot binary data is excluded from response

---

## Database Tables

| Table | Records | Key Fields |
|-------|---------|-----------|
| web_logs | 8,534 | website_url, duration_seconds, browser_name |
| application_logs | 13,479 | app_name, window_title, duration_seconds |
| inactivity_logs | 2,049 | duration_seconds, status |
| screenshot_logs | 1,647 | screen_width, screen_height |
| punch_log_consolidated | 62 | punch_in_time, punch_out_time, total_work_duration_seconds |

---

## Testing API

### Option 1: Use Test Page
Open `http://localhost:8889/test.html` in browser and click test buttons for each endpoint.

### Option 2: Direct API Call
```javascript
const response = await fetch('/api.php?action=get_daily_working_hours', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    employee: 'ashutosh',
    date: '2026-01-19',
    company_name: 'WIZONE IT NETWORK INDIA PVT LTD'
  })
});
const data = await response.json();
console.log(data);
```

### Option 3: Check Tables Script
```bash
node check_tables.js
```
Shows database table structures and record counts.

---

## Common Issues & Solutions

### Issue: "API endpoint not found"
**Solution**: Make sure endpoint name in action parameter matches exactly (case-sensitive)

### Issue: No records returned
**Solution**: 
- Verify date range is correct
- Check if employee username exists in database
- Use correct company_name

### Issue: Timezone mismatch
**Solution**: All timestamps are returned in IST (Asia/Kolkata) timezone

### Issue: Server crashes
**Solution**: Check terminal for error messages and restart with `node server.js`

---

## Admin Credentials
- **Username**: wizone
- **Password**: Wiz450%cont&2026

---

## Notes
- All timestamps are in IST (UTC+5:30)
- Pagination default limit: 50 records per page
- Company name defaults to "WIZONE IT NETWORK INDIA PVT LTD"
- Employee filter is username field
- Date format must be YYYY-MM-DD
