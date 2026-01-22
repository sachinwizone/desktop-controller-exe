# Daily Working Hours API - Technical Documentation

## Overview
The Daily Working Hours feature requires a new API endpoint `get_daily_working_hours` that aggregates punch in/out records for a specific employee on a specific date and calculates various metrics.

## API Endpoint

### Endpoint Name
`get_daily_working_hours`

### HTTP Method
GET or POST

### Request Format

```javascript
{
    "company_name": "WIZONE IT NETWORK INDIA PVT LTD",
    "employee": "john.doe",
    "date": "2024-01-20"
}
```

### Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `company_name` | string | Yes | Company name (from session) |
| `employee` | string | Yes | Employee username |
| `date` | string | Yes | Date in YYYY-MM-DD format |

---

## Response Format

### Success Response (Status 200)

```javascript
{
    "success": true,
    "data": {
        "total_work_hours": 8.25,
        "total_break_minutes": 75,
        "first_punch_in": "2024-01-20T09:00:00",
        "last_punch_out": "2024-01-20T18:30:00",
        "sessions": [
            {
                "punch_in_time": "2024-01-20T09:00:15",
                "punch_out_time": "2024-01-20T13:00:30",
                "work_hours": 4.00,
                "system_name": "System-001",
                "break_before_next": 30
            },
            {
                "punch_in_time": "2024-01-20T13:30:45",
                "punch_out_time": "2024-01-20T18:30:20",
                "work_hours": 4.99,
                "system_name": "System-002",
                "break_before_next": null
            }
        ]
    }
}
```

### Error Response (Status 400/500)

```javascript
{
    "success": false,
    "error": "Employee not found" 
    // or "No records found for this date"
    // or "Invalid date format"
    // or "Unauthorized access"
}
```

---

## Response Field Descriptions

### Top-Level Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the request was successful |
| `data` | object | Contains aggregated daily data (only if success=true) |
| `error` | string | Error message (only if success=false) |

### Data Object Fields

| Field | Type | Description | Calculation |
|-------|------|-------------|-------------|
| `total_work_hours` | decimal | Total hours worked today | Sum of all session work_hours |
| `total_break_minutes` | integer | Total break time in minutes | Sum of break_before_next for all but last session |
| `first_punch_in` | ISO 8601 datetime | First punch in time | MIN(punch_in_time) from all sessions |
| `last_punch_out` | ISO 8601 datetime | Last punch out time | MAX(punch_out_time) from all sessions |
| `sessions` | array | Array of work sessions | See Session Object below |

### Session Object Fields

| Field | Type | Description | Calculation |
|-------|------|-------------|-------------|
| `punch_in_time` | ISO 8601 datetime | Time of punch in | From attendance_log table |
| `punch_out_time` | ISO 8601 datetime | Time of punch out | From attendance_log table |
| `work_hours` | decimal | Hours worked in session | (punch_out_time - punch_in_time) / 3600 |
| `system_name` | string | Computer/system name | From attendance_log table |
| `break_before_next` | integer | Break time before next session (minutes) | (next_punch_in - punch_out_time) / 60 |

---

## Business Logic

### Calculations

#### Total Work Hours
```
Sum of all (punch_out_time - punch_in_time) for the day
Result in decimal hours (e.g., 8.25 = 8 hours 15 minutes)
```

#### Total Break Minutes
```
For each session except the last:
  break_minutes = (next_session.punch_in_time - current_session.punch_out_time) / 60
Sum all break_minutes
```

#### First Punch In
```
Earliest punch_in_time of the day for the employee
```

#### Last Punch Out
```
Latest punch_out_time of the day for the employee
If employee still has active punch (no punch_out), use system's current time or return null
```

#### Work Sessions Array
```
All punch in/out pairs for the employee on the selected date
Ordered by punch_in_time chronologically
Include system_name from each session
Include break time before next session (null for last session)
```

---

## Implementation Example (PHP)

```php
<?php
// In your api.php file, add this function

function get_daily_working_hours($data) {
    global $mysqli;
    
    // Validate input
    if (!isset($data['employee']) || !isset($data['date'])) {
        return ['success' => false, 'error' => 'Missing required parameters'];
    }
    
    $employee = $mysqli->real_escape_string($data['employee']);
    $date = $data['date']; // Format: YYYY-MM-DD
    
    // Validate date format
    if (!preg_match('/^\d{4}-\d{2}-\d{2}$/', $date)) {
        return ['success' => false, 'error' => 'Invalid date format'];
    }
    
    // Parse date range for the selected date
    $start_datetime = $date . ' 00:00:00';
    $end_datetime = $date . ' 23:59:59';
    
    // Query attendance records for the date
    $query = "SELECT 
                punch_in_time,
                punch_out_time,
                system_name,
                ip_address
              FROM attendance_log
              WHERE username = '$employee'
              AND DATE(punch_in_time) = '$date'
              ORDER BY punch_in_time ASC";
    
    $result = $mysqli->query($query);
    
    if (!$result) {
        return ['success' => false, 'error' => 'Database error'];
    }
    
    if ($result->num_rows === 0) {
        return ['success' => false, 'error' => 'No records found for this date'];
    }
    
    $sessions = [];
    $total_work_hours = 0;
    $total_break_minutes = 0;
    $first_punch_in = null;
    $last_punch_out = null;
    
    // Fetch all records
    while ($row = $result->fetch_assoc()) {
        $punch_in = strtotime($row['punch_in_time']);
        $punch_out = $row['punch_out_time'] ? strtotime($row['punch_out_time']) : time();
        
        $work_hours = ($punch_out - $punch_in) / 3600;
        $total_work_hours += $work_hours;
        
        if (!$first_punch_in) {
            $first_punch_in = $row['punch_in_time'];
        }
        $last_punch_out = $row['punch_out_time'];
        
        $sessions[] = [
            'punch_in_time' => $row['punch_in_time'],
            'punch_out_time' => $row['punch_out_time'],
            'work_hours' => round($work_hours, 2),
            'system_name' => $row['system_name']
        ];
    }
    
    // Calculate break times between sessions
    for ($i = 0; $i < count($sessions) - 1; $i++) {
        $current_out = strtotime($sessions[$i]['punch_out_time']);
        $next_in = strtotime($sessions[$i + 1]['punch_in_time']);
        $break_minutes = ($next_in - $current_out) / 60;
        $sessions[$i]['break_before_next'] = round($break_minutes);
        $total_break_minutes += $break_minutes;
    }
    
    return [
        'success' => true,
        'data' => [
            'total_work_hours' => round($total_work_hours, 2),
            'total_break_minutes' => round($total_break_minutes),
            'first_punch_in' => $first_punch_in,
            'last_punch_out' => $last_punch_out,
            'sessions' => $sessions
        ]
    ];
}

// Add to your API router
if ($action === 'get_daily_working_hours') {
    echo json_encode(get_daily_working_hours($_REQUEST));
}
?>
```

---

## Database Queries Required

### Query 1: Get All Sessions for Employee on Date
```sql
SELECT 
    punch_in_time,
    punch_out_time,
    system_name,
    ip_address
FROM attendance_log
WHERE username = 'john.doe'
AND DATE(punch_in_time) = '2024-01-20'
ORDER BY punch_in_time ASC
```

### Query 2: Verify Employee Exists (optional)
```sql
SELECT username, display_name
FROM users
WHERE username = 'john.doe'
AND company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
```

---

## Edge Cases to Handle

### 1. Employee Still Working (No Punch Out)
**Scenario:** punch_out_time is NULL

**Solution:** 
- Use current system time as punch_out_time for calculation
- Or return null and display "Still Working" in UI
- Recommended: Use current time

### 2. No Records for Date
**Scenario:** Employee didn't work on selected date

**Solution:**
- Return success=false with error message
- OR return empty sessions array with zeros
- Recommended: Return error message

### 3. Multiple Punch Cycles
**Scenario:** Employee has multiple punch in/out pairs

**Solution:**
- Include all sessions in the response
- Calculate breaks between each session
- Sum all work hours

### 4. Invalid Date
**Scenario:** User submits invalid date format

**Solution:**
- Validate date format YYYY-MM-DD
- Return error if invalid
- Don't query database

### 5. Unauthorized Access
**Scenario:** Admin tries to view another company's employee

**Solution:**
- Verify company_name matches session
- Return error if mismatch
- Don't return any data

---

## Error Codes

| Code | Message | HTTP Status | Action |
|------|---------|-------------|--------|
| 1 | Employee not found | 400 | Invalid employee selection |
| 2 | No records found for this date | 200 | Empty/valid response |
| 3 | Invalid date format | 400 | Invalid date input |
| 4 | Invalid company | 403 | Unauthorized access |
| 5 | Database error | 500 | Server error |

---

## Performance Considerations

1. **Index Required**: Index on `(username, DATE(punch_in_time))` for fast queries
2. **Date Range**: Query uses single date, not range - very efficient
3. **Data Size**: Limited to one employee, one day - small result set
4. **Caching**: Can cache results for past dates (not real-time)

### Recommended Index
```sql
CREATE INDEX idx_attendance_user_date 
ON attendance_log(username, punch_in_time);
```

---

## Testing

### Test Case 1: Normal Day
**Input:** john.doe, 2024-01-20
**Expected:** 8.25 hours, 2 sessions, 75 min break

### Test Case 2: Still Working
**Input:** jane.smith, 2024-01-20
**Expected:** Current session with no punch_out

### Test Case 3: Off Day
**Input:** mike.jones, 2024-01-20
**Expected:** Error: No records found

### Test Case 4: Invalid Date
**Input:** john.doe, 2024-13-45
**Expected:** Error: Invalid date format

---

## Frontend Integration

Frontend expects this response format and displays:
1. **Total Work Hours**: From `total_work_hours`
2. **First Check In**: From `first_punch_in` (formatted as HH:MM)
3. **Last Check Out**: From `last_punch_out` (formatted as HH:MM)
4. **Total Break**: From `total_break_minutes` (converted to hours)
5. **Session Count**: From length of `sessions` array
6. **Productivity**: `(total_work_hours / 8) * 100`
7. **Sessions Table**: From `sessions` array

---

## Security Notes

1. **Authentication**: Require valid session/login
2. **Authorization**: Only admins can access
3. **Input Validation**: Validate all parameters
4. **SQL Injection**: Use prepared statements
5. **Rate Limiting**: Consider limiting API calls
6. **Data Privacy**: Don't log sensitive data

---

## Version History

- **v1.0** (2024-01-20): Initial implementation
  - Single employee daily view
  - Work hours aggregation
  - Session breakdown
  - Break time calculation

