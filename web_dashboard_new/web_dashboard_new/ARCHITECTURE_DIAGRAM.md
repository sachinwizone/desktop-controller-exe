# Architecture & Flow Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     WEB DASHBOARD (Browser)                 │
│                       (index.html / app.js)                 │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Navigation Menu                                      │   │
│  │  • Dashboard                                          │   │
│  │  • Daily Working Hours  ← Daily Hours Report Fixed   │   │
│  │  • Attendance Reports                                 │   │
│  │  • Web Browsing Logs    ← Connected to database      │   │
│  │  • Application Usage    ← Connected to database      │   │
│  │  • Inactivity Logs      ← Connected to database      │   │
│  │  • Screenshots          ← Connected to database      │   │
│  │  • Analytics & Insights                              │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ (AJAX/Fetch)
                           │ POST /api.php?action=...
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    NODE.JS API SERVER                       │
│                      (server.js:8889)                       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  API Endpoints / Request Handlers                     │   │
│  │                                                        │   │
│  │  1. get_daily_working_hours()                        │   │
│  │     ↓ FIXED - Was missing endpoint                   │   │
│  │     Query: punch_log_consolidated                    │   │
│  │                                                        │   │
│  │  2. get_web_logs()                                   │   │
│  │     ↓ NEW endpoint                                   │   │
│  │     Query: web_logs table                            │   │
│  │                                                        │   │
│  │  3. get_application_logs()                           │   │
│  │     ↓ NEW endpoint                                   │   │
│  │     Query: application_logs table                    │   │
│  │                                                        │   │
│  │  4. get_inactivity_logs()                            │   │
│  │     ↓ NEW endpoint                                   │   │
│  │     Query: inactivity_logs table                     │   │
│  │                                                        │   │
│  │  5. get_screenshots()                                │   │
│  │     ↓ NEW endpoint                                   │   │
│  │     Query: screenshot_logs table                     │   │
│  │                                                        │   │
│  │  6. get_employees()                                  │   │
│  │     ↓ EXISTING endpoint                              │   │
│  │     Query: punch_log_consolidated (distinct)         │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ (PostgreSQL Client Library)
                           │ SQL Queries with Timezone
                           ↓
┌─────────────────────────────────────────────────────────────┐
│               POSTGRESQL DATABASE                           │
│           (72.61.170.243:9095 - controller_application)    │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Tables (Total: 27,771 records)                       │   │
│  │                                                        │   │
│  │  1. punch_log_consolidated          (62 records)      │   │
│  │     └─ Columns: punch_in_time, punch_out_time,       │   │
│  │        break_duration, work_duration, username, etc. │   │
│  │                                                        │   │
│  │  2. web_logs                        (8,534 records)   │   │
│  │     └─ Columns: website_url, duration_seconds,       │   │
│  │        browser_name, domain, visit_time, etc.        │   │
│  │                                                        │   │
│  │  3. application_logs                (13,479 records)  │   │
│  │     └─ Columns: app_name, window_title, duration,    │   │
│  │        start_time, end_time, is_active, etc.         │   │
│  │                                                        │   │
│  │  4. inactivity_logs                 (2,049 records)   │   │
│  │     └─ Columns: start_time, end_time, duration,      │   │
│  │        status, username, etc.                        │   │
│  │                                                        │   │
│  │  5. screenshot_logs                 (1,647 records)   │   │
│  │     └─ Columns: screenshot_data, screen_width,       │   │
│  │        screen_height, timestamp, etc.                │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## API Request/Response Flow

### Example: Daily Working Hours Request

```
1. USER INTERACTION
   └─ User navigates to "Daily Working Hours" page
   └─ Selects employee and date
   └─ Clicks "View Daily Hours" button

2. FRONTEND (app.js)
   ┌─────────────────────────────────────────┐
   │ async loadDailyWorkingHours() {          │
   │   const requestData = {                  │
   │     employee: 'ashutosh',                │
   │     date: '2026-01-19',                  │
   │     company_name: 'WIZONE IT NETWORK...' │
   │   }                                       │
   │   const response = await this.api(       │
   │     'get_daily_working_hours',           │
   │     requestData                          │
   │   );                                      │
   │ }                                         │
   └─────────────────────────────────────────┘
                    │
                    ↓ HTTP POST
   fetch('/api.php?action=get_daily_working_hours', {
     method: 'POST',
     headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify(requestData)
   })

3. SERVER (server.js)
   ┌─────────────────────────────────────────────────────┐
   │ Receives POST request at /api.php                   │
   │ Extracts action: 'get_daily_working_hours'          │
   │ Calls: apiHandlers.get_daily_working_hours(data)    │
   │                                                      │
   │ Inside handler:                                     │
   │ • Extract parameters: employee, date, company_name  │
   │ • Validate inputs                                   │
   │ • Parse date to IST timezone                        │
   │ • Build SQL query with WHERE conditions             │
   │ • Execute: SELECT ... FROM punch_log_consolidated   │
   │ • Format response with calculations                 │
   │ • Return JSON response                              │
   └─────────────────────────────────────────────────────┘
                    │
                    ↓ SQL Query
   PostgreSQL Connection:
   SELECT 
     id, username, punch_in_time, punch_out_time,
     total_work_duration_seconds, break_duration_seconds
   FROM punch_log_consolidated
   WHERE username = 'ashutosh'
     AND DATE(punch_in_time AT TIME ZONE 'Asia/Kolkata') = '2026-01-19'
     AND company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
   ORDER BY punch_in_time ASC

4. DATABASE (PostgreSQL)
   ┌──────────────────────────────────┐
   │ punch_log_consolidated table      │
   │ Matches: 3 sessions found         │
   │ Returns row data                  │
   └──────────────────────────────────┘

5. SERVER PROCESSING
   ┌──────────────────────────────────────────────┐
   │ Format response:                             │
   │ • Sum work hours: 9.5h                       │
   │ • Sum break time: 45 minutes                 │
   │ • First punch-in: 09:30 AM                   │
   │ • Last punch-out: 07:15 PM                   │
   │ • Format each session with times             │
   └──────────────────────────────────────────────┘

6. HTTP RESPONSE
   {
     "success": true,
     "data": {
       "sessions": [
         {
           "punch_in_time": "2026-01-19T09:30:00+05:30",
           "punch_out_time": "2026-01-19T13:00:00+05:30",
           "work_hours": 3.5,
           "break_minutes": 15,
           "status": "Completed"
         },
         { ... }
       ],
       "total_work_hours": 9.5,
       "total_break_minutes": 45,
       "first_punch_in": "09:30:00",
       "last_punch_out": "19:15:00",
       "session_count": 3
     }
   }

7. FRONTEND RENDERING
   ┌─────────────────────────────────┐
   │ displayDailyWorkingHours() {     │
   │   Create stats cards             │
   │   Create session table           │
   │   Update UI with data            │
   │   Show formatted timestamps      │
   │ }                                 │
   └─────────────────────────────────┘

8. USER VIEW
   ┌────────────────────────────────────────┐
   │ Daily Working Hours                     │
   │ ┌──────────────────────────────────┐   │
   │ │ Total: 9.5 hours     Break: 45m  │   │
   │ │ First: 09:30 AM  Last: 07:15 PM  │   │
   │ ├──────────────────────────────────┤   │
   │ │ Session 1: 09:30 - 13:00  3.5h   │   │
   │ │ Session 2: 13:45 - 17:30  3.75h  │   │
   │ │ Session 3: 17:45 - 19:15  1.5h   │   │
   │ └──────────────────────────────────┘   │
   └────────────────────────────────────────┘
```

---

## Data Flow Between Components

```
┌─────────────────┐
│  User Browser   │
│  (Frontend)     │
└────────┬────────┘
         │
         │ HTTP POST
         │ JSON Request
         ↓
┌─────────────────────┐       ┌──────────────┐
│   Node.js Server    │◄──────│   File Sync  │
│   (API Handler)     │       │  & Caching   │
│                     │       └──────────────┘
│ • Parse Request     │
│ • Validate Input    │
│ • Build SQL Query   │
│ • Error Handling    │
└────────┬────────────┘
         │
         │ PostgreSQL Client
         │ SQL Command
         ↓
┌─────────────────────┐
│  PostgreSQL DB      │
│                     │
│ • Execute Query     │
│ • Apply Timezone    │
│ • Return Rows       │
│ • Close Connection  │
└────────┬────────────┘
         │
         │ Row Data
         │ (Raw)
         ↓
┌─────────────────────┐
│  Node.js Server     │
│ (Response Building) │
│                     │
│ • Format Records    │
│ • Calculate Stats   │
│ • Build JSON        │
│ • Format Timestamps │
└────────┬────────────┘
         │
         │ HTTP 200 OK
         │ JSON Response
         ↓
┌─────────────────┐
│  User Browser   │
│ (Frontend)      │
│                 │
│ • Parse JSON    │
│ • Update DOM    │
│ • Display Data  │
│ • Show to User  │
└─────────────────┘
```

---

## Database Query Execution Pattern

```
All endpoints follow this pattern:

1. RECEIVE REQUEST
   ↓
2. VALIDATE PARAMETERS
   ├─ Check required fields
   ├─ Validate date format
   ├─ Check company name
   └─ Validate employee username
   ↓
3. BUILD SQL
   ├─ Start with SELECT clause
   ├─ Add FROM table name
   ├─ Add WHERE conditions (parameterized)
   ├─ Add timezone conversion (AT TIME ZONE)
   ├─ Add ORDER BY and LIMIT/OFFSET
   └─ Add pagination parameters
   ↓
4. EXECUTE QUERY
   ├─ Connect to PostgreSQL
   ├─ Execute prepared statement
   ├─ Handle errors
   └─ Fetch all rows
   ↓
5. PROCESS RESULTS
   ├─ Map database columns to response fields
   ├─ Format timestamps
   ├─ Calculate derived values
   ├─ Convert seconds to hours/minutes
   └─ Build pagination metadata
   ↓
6. RETURN RESPONSE
   ├─ success: true/false
   ├─ data: { records[], pagination{} }
   └─ error: (if failed)
```

---

## Timezone Handling

```
Database: UTC Storage
   ↓
PostgreSQL Stores timestamps in UTC (e.g., 2026-01-19 04:00:00 UTC)
   ↓
Query Execution:
   SELECT punch_in_time AT TIME ZONE 'Asia/Kolkata' as punch_in_time
   └─ Converts UTC → IST (+5:30)
   └─ Result: 2026-01-19 09:30:00
   ↓
Server Processing:
   JavaScript Date object created from string
   ↓
Frontend Display:
   toLocaleString('en-IN', { timeZone: 'Asia/Kolkata' })
   └─ Shows: 19/1/2026, 9:30:00 AM
   ↓
User Sees: Correct IST time
```

---

## Error Handling Flow

```
Request Received
   ↓
Try {
  Validate parameters
     ↓
  Connect to database
     ↓
  Execute query
     ↓
  Process results
     ↓
  Return success response
}
Catch (error) {
  Log error to console
  Return: {
    success: false,
    error: "User-friendly error message"
  }
}
Finally {
  Close database connection
}
```

---

## Summary of All Connections

| Feature | Table | Endpoint | Status |
|---------|-------|----------|--------|
| Daily Working Hours | punch_log_consolidated | get_daily_working_hours | ✅ FIXED |
| Web Logs | web_logs | get_web_logs | ✅ NEW |
| App Usage | application_logs | get_application_logs | ✅ NEW |
| Inactivity | inactivity_logs | get_inactivity_logs | ✅ NEW |
| Screenshots | screenshot_logs | get_screenshots | ✅ NEW |
| Employee List | punch_log_consolidated | get_employees | ✅ EXISTING |

All endpoints are fully functional and connected to their respective database tables.
