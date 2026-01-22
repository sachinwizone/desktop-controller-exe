# Employee Attendance System

A modern, clean employee attendance tracking application for Windows.

## Features

### 1. One-Time Activation (First Launch)
- Enter company Activation Key
- Enter Username
- Select Department
- Validates against database

### 2. Main Dashboard
- Real-time Work Hours counter
- Real-time Break Time counter
- **PUNCH IN** - Start work session
- **PUNCH OUT** - End work session
- **BREAK START** - Start a break
- **BREAK STOP** - End break and resume work
- Today's Activity list showing all punch/break events

### 3. System Tray
- Runs minimized in system tray
- Shows company name and status
- Quick access menu:
  - Open Dashboard
  - Force Sync
  - About
  - Exit (Admin Only - password protected)

## Installation

### For Deployment
1. Copy the `publish` folder to the target machine
2. Run `EmployeeAttendance.exe`
3. On first launch, enter the activation key and user details

### Build from Source
```powershell
cd EmployeeAttendance
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

## Database Connection

Connects to the same PostgreSQL database as the Admin Desktop Controller:
- Host: 72.61.170.243
- Port: 9095
- Database: controller_application
- Table: `punch_log_consolidated`

## Table Structure (punch_log_consolidated)

| Column | Type | Description |
|--------|------|-------------|
| id | SERIAL | Primary Key |
| activation_key | VARCHAR | Company identifier |
| company_name | VARCHAR | Company name |
| username | VARCHAR | Employee username |
| system_name | VARCHAR | Computer name |
| ip_address | VARCHAR | Network IP |
| machine_id | VARCHAR | Unique machine ID |
| punch_in_time | TIMESTAMP | When employee punched in |
| punch_out_time | TIMESTAMP | When employee punched out |
| break_start_time | TIMESTAMP | When break started |
| break_end_time | TIMESTAMP | When break ended |
| break_duration_seconds | INTEGER | Total break time |
| total_work_duration_seconds | INTEGER | Total work time |
| created_at | TIMESTAMP | Record creation time |
| updated_at | TIMESTAMP | Last update time |

## Usage

1. **Launch Application** - Opens activation form (first time) or dashboard
2. **Punch In** - Click green "PUNCH IN" button to start work
3. **Take Break** - Click yellow "BREAK START" button
4. **Resume Work** - Click purple "BREAK STOP" button
5. **Punch Out** - Click red "PUNCH OUT" button when done
6. **Minimize** - Close window to minimize to tray
7. **Exit** - Right-click tray icon → Exit (requires admin password: `Admin@tracker$%000`)

## File Locations

- **Activation Data**: `%APPDATA%\EmployeeAttendance\activation.dat`
- **Error Logs**: `%APPDATA%\EmployeeAttendance\error.log`

## Differences from Admin Desktop Controller

| Feature | Employee Attendance | Desktop Controller (Admin) |
|---------|--------------------|-----------------------------|
| Purpose | Employee time tracking | Admin system control |
| UI | Modern dark theme | Full-featured admin panel |
| Features | Punch In/Out, Breaks | System controls, blocking, audit |
| Exit | Password protected | Normal |
| Tabs | None | Multiple control tabs |

## Version

- Version: 1.0.0
- Framework: .NET 6.0 Windows Forms
- Self-contained: Yes (no .NET runtime required)
