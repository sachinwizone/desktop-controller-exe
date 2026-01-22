# Daily Working Hours Feature - User Guide

## Quick Start

### Step 1: Access the Feature
1. Log in to the WIZONE Desktop Controller admin dashboard
2. Look for **"Daily Working Hours"** in the left sidebar under the MANAGEMENT section
3. Click on it to open the Daily Working Hours page

### Step 2: Select Date and Employee
1. **Select Date**: Click the date picker and choose the date you want to view
   - Default is today's date
   - Can select any past or future date

2. **Select Engineer**: Click the employee dropdown and select the engineer whose working hours you want to view
   - Shows display name and username
   - Example: "John Doe (john.doe)"

### Step 3: View Results
Click the **"View Daily Hours"** button to fetch and display:
- 6 informative statistics cards
- Detailed work sessions table
- Break time breakdown

---

## Understanding the Cards

### 📊 Card 1: Total Working Hours
**What it shows:** Total cumulative hours worked in a single day

**Example:**
```
8.25h
Cumulative work time today
```

**Calculation:** Sum of all work session durations

---

### 🔽 Card 2: First Check In
**What it shows:** Time when employee first logged in/checked in

**Example:**
```
09:00
First entry time
```

**Format:** HH:MM in 24-hour Indian time format

---

### 🔼 Card 3: Last Check Out
**What it shows:** Time when employee last logged out/checked out

**Example:**
```
18:30
Last exit time
```

**Shows:** "Still Working" if employee hasn't checked out yet

---

### ☕ Card 4: Total Break Time
**What it shows:** Total time spent on breaks during the day

**Example:**
```
1.25h
75 minutes break
```

**Calculation:** Time between check-out and next check-in

---

### 👥 Card 5: Work Sessions
**What it shows:** Number of separate work sessions (check in/out pairs)

**Example:**
```
3
Check in/out sessions
```

**Note:** Employee may have multiple sessions if they took breaks

---

### 📈 Card 6: Productivity
**What it shows:** Percentage of standard 8-hour workday completed

**Example:**
```
103%
Of 8-hour target
[████████████ 103%]
```

**Calculation:** (Total Work Hours / 8) × 100
- 100% = Full 8-hour day
- 50% = 4 hours worked
- 150% = 12 hours worked (overtime)

---

## Work Sessions Table

Shows detailed information for each work session:

| Column | Description |
|--------|-------------|
| **Session #** | Sequential number (#1, #2, #3, etc.) |
| **Check In** | Time employee started work (HH:MM:SS) |
| **Check Out** | Time employee stopped work (HH:MM:SS) or "Still working" |
| **Work Duration** | Hours worked in this specific session |
| **System** | Computer/System name used |

### Example Table:
```
Session #  Check In      Check Out     Work Duration  System
#1         09:00:15      13:00:30      4.00h         System-001
#2         14:00:45      18:30:20      4.50h         System-002
```

---

## Features & Buttons

### 🔄 Refresh Button
- Updates the current view with latest data
- Click after an employee has worked more hours
- Preserves your selected date and employee

### 🔄 Reset Button
- Clears all selections and filters
- Resets to today's date
- Clears employee selection
- Useful to start fresh

---

## Use Cases

### 1. **Daily Performance Check**
- Manager wants to verify if John completed his 8-hour workday
- Select today's date
- Select John from employee dropdown
- View "Productivity" card to see percentage completed

### 2. **Overtime Tracking**
- HR wants to identify which employees worked overtime
- Select the date
- Check each employee's productivity card
- Those over 100% worked overtime

### 3. **Break Time Verification**
- Verify if employee took appropriate breaks
- Check the "Total Break Time" card
- Standard is usually 1-2 hours per 8-hour day

### 4. **System/Computer Tracking**
- Check which system an employee used
- Look at the "System" column in the sessions table
- Helps identify if policy compliant systems are used

### 5. **Unusual Patterns**
- Check if employee has many small sessions
- Look at the "Work Sessions" card
- Many sessions might indicate frequent breaks or system issues

---

## Tips & Tricks

### Tip 1: Select Past Dates
- Can view historical data for any previous date
- Useful for generating reports or audits
- Data goes back as far as the system has records

### Tip 2: Export for Reports
- Use browser's print feature (Ctrl+P) to save as PDF
- Can create attendance reports for HR

### Tip 3: Cross-Reference Systems
- Compare which systems employees use across days
- Identify if they're using approved devices

### Tip 4: Check Multiple Employees
- Compare productivity between team members
- Identify high/low performers

### Tip 5: Weekly Summary
- Check daily hours for each employee throughout the week
- Calculate weekly totals manually if needed

---

## Troubleshooting

### Issue: "No work sessions found for this date"
**Solution:** Employee didn't work on that date, OR they haven't checked in yet
- Try selecting yesterday's date to verify the system works
- Check if the employee actually punched in/out

### Issue: "Select an employee" error
**Solution:** Forgot to select engineer
- Click the "Select Engineer" dropdown
- Choose an employee from the list
- Try again

### Issue: Employees list is empty
**Solution:** System may be loading or there's a connection issue
- Refresh the page
- Try navigating away and back
- Check internet connection

### Issue: Decimal hours look strange (e.g., 8.75h)
**Solution:** This is correct - 0.75 hours = 45 minutes
- 0.25h = 15 minutes
- 0.50h = 30 minutes
- 0.75h = 45 minutes

---

## Data Privacy Notes

- All data is company-level (WIZONE IT NETWORK INDIA PVT LTD)
- Admin dashboard only - requires login
- Shows working hours, breaks, and systems used
- Does NOT show private browsing or email data

---

## Color Codes

- 🟣 **Purple**: Overall working hours and productivity
- 🟢 **Green**: Check-in times
- 🟠 **Orange**: Check-out times
- 🟡 **Cyan**: Session counts
- 🔴 **Pink**: Productivity percentage

---

## Questions?

If you encounter issues or need more features:
1. Check the troubleshooting section above
2. Verify your internet connection
3. Try refreshing the page
4. Contact your IT administrator

