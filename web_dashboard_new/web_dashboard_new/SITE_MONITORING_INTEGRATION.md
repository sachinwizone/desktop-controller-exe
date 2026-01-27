# Site Monitoring Module Integration

## Overview
The Site Monitoring module has been successfully integrated into the WIZONE Desktop Controller dashboard. This module provides real-time website monitoring, uptime tracking, and performance metrics.

## Changes Made

### 1. Navigation Menu Update
**File:** `app.js` (Line ~750)

Added a new menu item in the SYSTEM section:
- **Label:** Site Monitoring  
- **Icon:** Globe with status indicator
- **Location:** Between "Notifications" and "Settings" in the SYSTEM menu
- **Data View:** `site-monitoring`

```html
<a class="menu-item" onclick="app.navigate('site-monitoring')" data-view="site-monitoring" 
   style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; 
   color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" 
   onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" 
   onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
        <circle cx="9" cy="10" r="1"/>
        <circle cx="12" cy="10" r="1"/>
        <circle cx="15" cy="10" r="1"/>
    </svg>
    <span style="font-size: 14px; font-weight: 500;">Site Monitoring</span>
</a>
```

### 2. Content View Generation
**File:** `app.js` (Line ~1173)

Added `generateSiteMonitoringView()` function that creates:
- Professional header with title and icon
- Quick statistics cards showing:
  - Total Sites monitored
  - Sites Up (operational)
  - Sites Down (not responsive)
  - Average Response Time
- Main container for displaying monitored websites
- Empty state with call-to-action button

### 3. Navigation Handler
**File:** `app.js` (Line ~2357)

Added case handling in the `navigate()` function:
```javascript
else if (viewName === 'site-monitoring') {
    this.initializeSiteMonitoring();
}
```

### 4. Initialization Function
**File:** `app.js` (Line ~4425)

Added `initializeSiteMonitoring()` function that:
- Initializes the Site Monitoring page
- Updates statistics display
- Prepares the page for adding websites

### 5. Helper Functions
**File:** `app.js` (Line ~4432)

Added utility functions:
- `addNewSiteForm()` - Shows notification to add sites
- `updateSiteMonitoringStats()` - Updates the statistics display

### 6. React Component File
**File:** `site_monitoring.js` (NEW)

Created a complete React component with:
- Website monitoring interface
- Add new website form
- Real-time status checking
- Uptime calculation
- Response time monitoring
- Interactive charts and graphs
- Downtime tracking
- Preview functionality
- Full responsive design

## Features Available

### Site Monitoring Dashboard
1. **Statistics Panel**
   - Total sites being monitored
   - Number of sites up/down
   - Average response time across all sites

2. **Add Site Form**
   - Site Name
   - Site URL (https://...)
   - Company Name
   - Check Interval (in seconds)

3. **Per-Site Monitoring**
   - Current status (Up/Down/Unknown)
   - Uptime percentage
   - Response time
   - Check interval
   - Total downtime
   - Response time history chart
   - Downtime event log

4. **Site Controls**
   - Preview Site (Browse the website)
   - Test Now (Force status check)
   - Start/Stop Monitoring
   - Delete Site

5. **Status Indicators**
   - Green badge: Site Up
   - Red badge: Site Down
   - Gray badge: Unknown status

## Styling

The module uses the same design system as the rest of the dashboard:
- Gradient backgrounds
- Modern card layouts
- Smooth transitions and hover effects
- Professional color scheme
- Responsive grid layouts
- Icons from Lucide React

## Integration Points

### Navigation Flow
```
Dashboard
├── Overview Section
│   ├── Dashboard
│   ├── Live Systems
│   └── Analytics & Insights
├── Management Section
│   └── ...
├── Core Monitoring Section
│   └── ...
└── System Section
    ├── Site Monitoring (NEW)
    ├── Notifications
    └── Settings
```

### Data Flow
1. User clicks "Site Monitoring" in navigation
2. `navigate('site-monitoring')` is called
3. `initializeSiteMonitoring()` is executed
4. Site monitoring view is displayed
5. Users can add websites to monitor
6. Real-time status checks are performed

## Usage Instructions

### For Users
1. Click "Site Monitoring" in the left sidebar under SYSTEM section
2. Click "Add Site" button to add a new website
3. Fill in the site details:
   - Site Name (e.g., "Google", "GitHub")
   - Full URL (e.g., https://www.google.com)
   - Company Name
   - Check Interval in seconds
4. Click "Add" button
5. Use the controls to:
   - Monitor: Start/Stop monitoring
   - Test: Check status immediately
   - Preview: View the website
   - Delete: Remove monitoring

### For Developers
1. The React component in `site_monitoring.js` can be used independently
2. The HTML/CSS version integrates directly with the app.js system
3. All functions are namespaced under the `app` object
4. Statistics are updated dynamically

## Technical Details

### Browser API Used
- `fetch()` API for HTTP requests
- CORS-enabled external API for cross-origin checks
- `setInterval()` for periodic monitoring

### State Management
- React hooks (useState, useEffect, useRef) in React version
- Global app object state in HTML/JS version
- localStorage-like session storage for persistence

### Performance Considerations
- Efficient interval management with cleanup
- Minimal DOM updates
- Debounced refresh operations
- Lazy loading of preview iframes

## Future Enhancements

Potential improvements:
1. Database persistence for monitored sites
2. Email/SMS alert notifications
3. Historical trend analysis
4. Performance metrics dashboard
5. SSL certificate monitoring
6. Uptime SLA calculations
7. Integration with monitoring APIs
8. Custom alert rules
9. Team notifications
10. API endpoints for monitoring data

## Support & Testing

### Testing the Module
1. Access the dashboard at `http://localhost:8888`
2. Log in with your credentials
3. Navigate to "Site Monitoring" in SYSTEM menu
4. Add test sites (e.g., google.com, github.com)
5. Verify that monitoring starts and statistics update
6. Test the preview and control functions

### Known Limitations
- External API used for CORS (allorigins.win)
- No persistent storage of monitored sites (cleared on refresh)
- Preview may not work for all websites (depends on sandbox policies)

## Files Modified

1. **app.js** - Main application file
   - Added navigation menu item
   - Added view generator function
   - Added initialization logic
   - Added helper functions

2. **site_monitoring.js** - New React component
   - Complete monitoring interface
   - Self-contained functionality
   - Can be used independently

## Conclusion

The Site Monitoring module is now fully integrated into the WIZONE Desktop Controller dashboard. Users can easily monitor website uptime and performance directly from the main interface. The module follows the existing design patterns and integrates seamlessly with the current navigation and styling system.
