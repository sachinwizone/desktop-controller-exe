# Desktop Controller - Database Schema & Features Documentation
## WIZONE IT NETWORK INDIA PVT LTD

---

## 📊 DATABASE SCHEMA

### 1. **users** Table
Stores employee information and login credentials

```sql
CREATE TABLE users (
    user_id VARCHAR(50) PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    department_id INT,
    employee_code VARCHAR(20) UNIQUE,
    phone VARCHAR(15),
    designation VARCHAR(50),
    date_of_joining DATE,
    manager_id VARCHAR(50),
    is_active BOOLEAN DEFAULT TRUE,
    profile_image TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (department_id) REFERENCES departments(department_id),
    FOREIGN KEY (manager_id) REFERENCES users(user_id)
);
```

### 2. **departments** Table
Manages organizational departments

```sql
CREATE TABLE departments (
    department_id INT PRIMARY KEY AUTO_INCREMENT,
    department_name VARCHAR(100) NOT NULL,
    department_code VARCHAR(20),
    head_of_department VARCHAR(50),
    location VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (head_of_department) REFERENCES users(user_id)
);
```

### 3. **attendance_logs** Table
Primary table for punch in/out records

```sql
CREATE TABLE attendance_logs (
    log_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id VARCHAR(50) NOT NULL,
    punch_in_time TIMESTAMP NOT NULL,
    punch_out_time TIMESTAMP NULL,
    punch_in_location VARCHAR(200),
    punch_out_location VARCHAR(200),
    device_info TEXT,
    ip_address VARCHAR(45),
    work_status ENUM('active', 'break', 'idle', 'offline') DEFAULT 'active',
    total_work_hours DECIMAL(5,2),
    total_break_time INT DEFAULT 0, -- in minutes
    productive_hours DECIMAL(5,2),
    idle_time INT DEFAULT 0, -- in minutes
    notes TEXT,
    is_manual_entry BOOLEAN DEFAULT FALSE,
    approved_by VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (approved_by) REFERENCES users(user_id)
);
```

### 4. **break_logs** Table
Detailed tracking of breaks during work hours

```sql
CREATE TABLE break_logs (
    break_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    log_id BIGINT NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    break_start TIMESTAMP NOT NULL,
    break_end TIMESTAMP NULL,
    break_type ENUM('tea', 'lunch', 'personal', 'meeting', 'other') DEFAULT 'personal',
    break_duration INT, -- in minutes
    notes VARCHAR(200),
    FOREIGN KEY (log_id) REFERENCES attendance_logs(log_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
```

### 5. **activity_tracking** Table
Monitors computer activity and screen time

```sql
CREATE TABLE activity_tracking (
    activity_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    log_id BIGINT NOT NULL,
    user_id VARCHAR(50) NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    activity_type ENUM('active', 'idle', 'locked', 'away') NOT NULL,
    application_name VARCHAR(100),
    window_title VARCHAR(200),
    duration_seconds INT,
    mouse_clicks INT DEFAULT 0,
    keyboard_strokes INT DEFAULT 0,
    FOREIGN KEY (log_id) REFERENCES attendance_logs(log_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
```

### 6. **productivity_metrics** Table
Daily productivity calculations and scores

```sql
CREATE TABLE productivity_metrics (
    metric_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id VARCHAR(50) NOT NULL,
    date DATE NOT NULL,
    total_active_time INT, -- in minutes
    total_idle_time INT, -- in minutes
    productivity_score DECIMAL(5,2), -- 0-100
    focus_time INT, -- uninterrupted work time in minutes
    applications_used INT,
    tasks_completed INT DEFAULT 0,
    meetings_attended INT DEFAULT 0,
    calculated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    UNIQUE KEY unique_user_date (user_id, date)
);
```

### 7. **leave_requests** Table
Leave management system

```sql
CREATE TABLE leave_requests (
    request_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id VARCHAR(50) NOT NULL,
    leave_type ENUM('casual', 'sick', 'earned', 'comp_off', 'unpaid') NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    total_days DECIMAL(3,1),
    reason TEXT,
    status ENUM('pending', 'approved', 'rejected', 'cancelled') DEFAULT 'pending',
    applied_on TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reviewed_by VARCHAR(50),
    reviewed_on TIMESTAMP NULL,
    reviewer_comments TEXT,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (reviewed_by) REFERENCES users(user_id)
);
```

### 8. **shift_schedules** Table
Employee shift management

```sql
CREATE TABLE shift_schedules (
    schedule_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id VARCHAR(50) NOT NULL,
    shift_date DATE NOT NULL,
    shift_start TIME NOT NULL,
    shift_end TIME NOT NULL,
    shift_type VARCHAR(20), -- e.g., 'morning', 'evening', 'night'
    is_working_day BOOLEAN DEFAULT TRUE,
    created_by VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    UNIQUE KEY unique_user_shift (user_id, shift_date)
);
```

### 9. **notifications** Table
In-app notification system

```sql
CREATE TABLE notifications (
    notification_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT,
    notification_type ENUM('attendance', 'leave', 'reminder', 'alert', 'system'),
    is_read BOOLEAN DEFAULT FALSE,
    action_url VARCHAR(200),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    read_at TIMESTAMP NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
```

### 10. **system_settings** Table
Application configuration

```sql
CREATE TABLE system_settings (
    setting_id INT PRIMARY KEY AUTO_INCREMENT,
    setting_key VARCHAR(50) UNIQUE NOT NULL,
    setting_value TEXT,
    setting_type VARCHAR(20), -- 'string', 'number', 'boolean', 'json'
    description TEXT,
    updated_by VARCHAR(50),
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

---

## 🎯 ESSENTIAL FEATURES FOR USER PANEL

### **Core Attendance Features**

1. **Punch In/Out Controls**
   - One-click punch in/out
   - Automatic location capture (if enabled)
   - Device information logging
   - Confirmation dialogs
   - Visual status indicators

2. **Break Management**
   - Start/end break tracking
   - Multiple break types (tea, lunch, personal)
   - Break time accumulation
   - Break history view
   - Break time limits alerts

3. **Real-Time Dashboard**
   - Current status (active/idle/break)
   - Today's working hours (live counter)
   - Punch in/out times
   - Total break time
   - Expected vs actual hours

### **Monitoring & Analytics**

4. **Activity Tracking**
   - Screen activity monitoring
   - Active vs idle time tracking
   - Application usage tracking
   - Mouse/keyboard activity
   - Automatic idle detection

5. **Productivity Metrics**
   - Daily productivity score (0-100)
   - Focus time calculation
   - Task completion tracking
   - Weekly/monthly trends
   - Comparative analytics

6. **Attendance History**
   - Daily attendance logs (last 30/60/90 days)
   - Filterable date range
   - Detailed breakdown per day
   - Late arrival indicators
   - Early departure tracking
   - Export to PDF/Excel

### **Reports & Summaries**

7. **Daily Summary**
   - Total work hours
   - Break duration
   - Productivity score
   - Activities performed
   - Notes/comments section

8. **Monthly Report**
   - Working days count
   - Total hours worked
   - Average hours per day
   - Late days count
   - Leave days taken
   - Overtime hours
   - Attendance percentage

9. **Visual Analytics**
   - Work hours bar chart
   - Productivity trend line
   - Activity pie chart
   - Weekly comparison graph
   - Heat map calendar

### **Leave Management**

10. **Leave Requests**
    - Apply for leave
    - Leave type selection
    - Multi-day leave support
    - Attach medical documents
    - Track approval status
    - Leave balance view

11. **Leave Dashboard**
    - Available leave balance
    - Pending requests
    - Approved leaves calendar
    - Leave history
    - Team leave calendar

### **Communication Features**

12. **Notifications System**
    - Real-time alerts
    - Attendance reminders
    - Leave status updates
    - System announcements
    - Policy updates
    - Birthday/anniversary wishes

13. **Feedback & Support**
    - Report issues
    - Attendance corrections
    - Manager communication
    - Help documentation
    - FAQ section

### **Advanced Features**

14. **Geofencing**
    - Office location verification
    - Restrict punch from outside
    - Location-based alerts
    - Multiple office locations

15. **Offline Mode**
    - Work in offline mode
    - Sync when online
    - Local data storage
    - Conflict resolution

16. **Biometric Integration**
    - Fingerprint verification
    - Face recognition
    - Multi-factor authentication

17. **Work From Home (WFH)**
    - WFH mode toggle
    - Different tracking rules
    - Screenshot capture (if policy allows)
    - Task-based tracking

18. **Time Tracking Projects**
    - Track time per project
    - Multiple project support
    - Project-wise reports
    - Billing integration

19. **Goals & Tasks**
    - Daily task list
    - Goal setting
    - Task completion tracking
    - Performance reviews

20. **Team Collaboration**
    - View team attendance
    - Team calendar
    - Shift swap requests
    - Coverage planning

### **Compliance & Security**

21. **Data Privacy**
    - Privacy settings control
    - Data export option
    - Consent management
    - GDPR compliance

22. **Audit Trail**
    - All changes logged
    - Manual entry tracking
    - Admin modifications
    - Compliance reports

23. **Screenshot Monitoring** (Optional)
    - Periodic screenshots
    - Blur sensitive data
    - User-controlled settings
    - Storage management

### **Integration Capabilities**

24. **Calendar Integration**
    - Google Calendar sync
    - Outlook integration
    - Meeting attendance tracking
    - Auto-break for meetings

25. **Payroll Integration**
    - Export attendance for payroll
    - Overtime calculation
    - Deduction tracking
    - Salary slip generation

26. **HR System Integration**
    - Employee database sync
    - Performance management
    - Training attendance
    - Exit management

### **Mobile Features**

27. **Mobile App Sync**
    - Cross-device sync
    - Mobile punch in/out
    - Push notifications
    - Offline capability

### **Customization**

28. **User Preferences**
    - Theme selection (dark/light)
    - Notification settings
    - Display preferences
    - Language selection

29. **Dashboard Widgets**
    - Customizable layout
    - Add/remove widgets
    - Widget settings
    - Personal dashboard

---

## 🔐 SECURITY FEATURES

1. **Session Management**
   - Auto-logout on inactivity
   - Device fingerprinting
   - Session hijacking prevention
   - Multi-device login control

2. **Data Encryption**
   - End-to-end encryption
   - Secure data transmission
   - Encrypted local storage
   - Secure backup

3. **Access Control**
   - Role-based permissions
   - Feature-level access
   - Department restrictions
   - Admin approval workflows

---

## 📱 SYSTEM REQUIREMENTS

### **Desktop Application**
- Windows 10/11 (64-bit)
- 4GB RAM minimum
- 500MB disk space
- Internet connection for sync
- .NET Framework 4.8+

### **Network Requirements**
- HTTPS communication
- WebSocket for real-time updates
- API endpoints secured with JWT
- Rate limiting implementation

---

## 🚀 IMPLEMENTATION PRIORITY

### **Phase 1 (MVP - Month 1)**
- User login & authentication
- Punch in/out functionality
- Basic attendance logs
- Daily summary dashboard

### **Phase 2 (Month 2)**
- Break management
- Activity tracking
- Monthly reports
- Leave requests

### **Phase 3 (Month 3)**
- Productivity metrics
- Advanced analytics
- Notifications system
- Mobile app

### **Phase 4 (Month 4+)**
- Geofencing
- Biometric integration
- Screenshot monitoring
- Third-party integrations

---

## 📊 SAMPLE DATABASE INDEXES

```sql
-- Performance optimization indexes
CREATE INDEX idx_attendance_user_date ON attendance_logs(user_id, punch_in_time);
CREATE INDEX idx_break_log_date ON break_logs(user_id, break_start);
CREATE INDEX idx_activity_timestamp ON activity_tracking(user_id, timestamp);
CREATE INDEX idx_productivity_date ON productivity_metrics(user_id, date);
CREATE INDEX idx_leave_status ON leave_requests(user_id, status);
```

---

## 🔄 DATA RETENTION POLICY

- **Attendance Logs**: 3 years
- **Activity Tracking**: 6 months
- **Screenshots**: 30 days
- **Notifications**: 90 days
- **Audit Logs**: 7 years

---

## 📞 SUPPORT & CONTACT

**WIZONE IT NETWORK INDIA PVT LTD**
Haridwar, Uttarakhand
Technical Support: support@wizone.in

---

**Document Version**: 1.0
**Last Updated**: January 07, 2026
**Prepared By**: Software Architecture Team
