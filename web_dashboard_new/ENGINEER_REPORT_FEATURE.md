# Engineer Activity Report Feature

## Overview
A comprehensive engineer activity reporting system has been added to the WIZONE Desktop Controller dashboard. This feature allows administrators to generate detailed activity reports for individual engineers on specific dates.

## Features Implemented

### 1. Navigation Menu Item
- **Location**: MANAGEMENT section of the sidebar
- **Label**: "Engineer Report" 
- **Icon**: Chart icon (bar chart SVG)
- **Route**: `engineer-report`

### 2. Report Generation Page Structure

#### Filter Section
- **Engineer Dropdown**: Lists all registered engineers from the company
- **Date Picker**: Select specific date for the report
- **Generate Button**: Triggers report generation with all data
- **Reset Button**: Clears all filters and resets the page

#### Report Output

**Statistics Cards (4 cards)**
1. Total Working Hours - Total hours worked by the engineer
2. Idle Time - Total idle/inactive time in minutes
3. Actual Working Time - Working hours minus idle time
4. Productivity - Percentage of actual working time vs total hours

**Tables**
1. **Punch In/Out & Working Hours**
   - Punch In Time
   - Punch Out Time
   - Total Working Hours

2. **Idle Time Details**
   - Start Time
   - End Time
   - Duration (in minutes, highlighted red if >= 10 minutes)

3. **Top Domains**
   - Rank (#)
   - Domain Name
   - Visit Count

4. **Top Applications**
   - Rank (#)
   - Application Name
   - Usage Time (in minutes)

**Charts** (Using Chart.js)
1. **Time Distribution (Doughnut Chart)**
   - Shows working time vs idle time
   - Colors: Green for working, Orange for idle
   - Responsive design

2. **Application Usage (Horizontal Bar Chart)**
   - Top 5 applications by usage time
   - Shows usage duration in minutes
   - Sorted by usage time

### 3. Backend Integration

The report system fetches data from multiple API endpoints:

```javascript
// API Calls Made:
- get_employees              // Fetch engineer list
- get_punch_logs             // Punch in/out data
- get_inactivity_data        // Idle time records
- get_browser_logs           // Domain/website data
- get_app_logs               // Application usage data
```

### 4. Data Processing

**Metrics Calculated:**
- `totalWorkHours`: From punch_log total_work_duration_seconds field
- `totalIdleMinutes`: Sum of all idle period durations
- `actualWorkingHours`: Total work hours - idle hours
- `productivity`: (Actual working hours / Total hours) * 100
- `topDomain`: Domain with highest visit count
- `topApplication`: Application with highest usage time

### 5. Export Functionality

- Export button generates CSV file with:
  - Report header with date
  - All table data formatted as text
  - Filename: `engineer-report-{engineerId}-{date}.csv`

### 6. Functions Added

#### Initialization
- `initializeEngineerReport()` - Setup page on navigation
- `loadEngineersList()` - Populate engineer dropdown

#### Report Generation
- `onEngineerSelected()` - Handle engineer selection
- `generateEngineerActivityReport()` - Main report generation function
- `displayEngineerReport()` - Orchestrate all data display

#### Data Calculations
- `calculateReportMetrics()` - Process all data and calculate metrics

#### Display Functions
- `displayEngineerReportStats()` - Show statistics cards
- `displayPunchInOutTable()` - Show punch data
- `displayIdleTimeTable()` - Show idle time details
- `displayTopDomainsTable()` - Show top domains
- `displayTopApplicationsTable()` - Show top applications

#### Chart Functions
- `renderEngineerReportCharts()` - Load Chart.js if needed
- `createEngineerReportCharts()` - Setup both charts
- `createWorkingTimeChart()` - Doughnut chart
- `createApplicationUsageChart()` - Bar chart

#### Utility Functions
- `clearEngineerReportFilters()` - Reset all fields and tables
- `exportEngineerReport()` - Export report as CSV

### 7. Styling

- Professional dark sidebar with light text
- White content cards with subtle shadows
- Color-coded metrics cards:
  - Blue border: Total Working Hours
  - Orange border: Idle Time
  - Green border: Actual Working Time
  - Blue border: Productivity
- Responsive grid layout
- Hover effects on buttons
- Table styling with alternating row backgrounds

### 8. Error Handling

- Toast notifications for user feedback
- Try-catch blocks on all async operations
- Validation for:
  - Engineer selection
  - Date selection
  - API response success
- Graceful degradation if Chart.js not available

## Usage Flow

1. User navigates to "Engineer Report" from sidebar
2. System loads list of engineers for selected company
3. User selects an engineer from dropdown
4. User selects a date using date picker
5. User clicks "Generate Report" button
6. System:
   - Fetches data from multiple APIs in parallel
   - Calculates all metrics
   - Displays statistics cards
   - Renders all four tables
   - Creates two interactive charts
7. User can export report as CSV or reset filters

## Data Dependencies

The report requires the following database tables/views:
- `punch_log_consolidated` - For punch in/out times and working hours
- `inactivity_logs` - For idle time periods
- `web_logs` / `browsing_logs` - For domain/website data
- `application_logs` - For application usage data
- `company_employees` - For engineer list

## Performance Notes

- Multiple API calls are made in parallel using Promise.all()
- Chart.js is lazy-loaded from CDN only when reports are generated
- Tables are rendered efficiently using string templates
- Chart objects are properly destroyed before re-rendering to prevent memory leaks

## Browser Compatibility

- Modern browsers with:
  - ES6 support (async/await, Promise)
  - Canvas support (for charts)
  - Fetch API support

## Future Enhancements

1. Date range reports (multi-day analysis)
2. Comparison reports (multiple engineers)
3. Weekly/Monthly summary reports
4. Email report delivery
5. Custom metric definitions
6. Performance benchmarking
7. Attendance predictions
8. Automated alerts for low productivity
