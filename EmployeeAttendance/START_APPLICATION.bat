@echo off
REM ========================================
REM Employee Attendance System - Quick Start
REM ========================================

echo.
echo ╔════════════════════════════════════════╗
echo ║   EMPLOYEE ATTENDANCE SYSTEM - LAUNCHER │
echo ║          Device Control Edition         │
echo ╚════════════════════════════════════════╝
echo.

REM Get the script location
setlocal enabledelayedexpansion
set SCRIPT_DIR=%~dp0

echo 📋 System Status Check...
echo.

REM Check if App.config exists
if exist "%SCRIPT_DIR%bin\x64\Release\net6.0-windows\win-x64\EmployeeAttendance.dll.config" (
    echo ✅ Configuration file found
) else (
    echo ❌ Configuration file NOT found!
    echo    Location: %SCRIPT_DIR%bin\x64\Release\net6.0-windows\win-x64\EmployeeAttendance.dll.config
    pause
    exit /b 1
)

REM Check if EXE exists
if exist "%SCRIPT_DIR%bin\x64\Release\net6.0-windows\win-x64\EmployeeAttendance.exe" (
    echo ✅ Application executable found
) else (
    echo ❌ Application NOT found!
    echo    Location: %SCRIPT_DIR%bin\x64\Release\net6.0-windows\win-x64\EmployeeAttendance.exe
    pause
    exit /b 1
)

echo ✅ All components verified
echo.
echo 🚀 Starting application...
echo.

REM Start the application
cd /d "%SCRIPT_DIR%bin\x64\Release\net6.0-windows\win-x64"
start EmployeeAttendance.exe

echo.
echo ╔════════════════════════════════════════╗
echo ║  Application Started Successfully! 🎉   │
echo ╚════════════════════════════════════════╝
echo.
echo 📌 What to do next:
echo.
echo 1. Open your web browser to: http://localhost:8888
echo.
echo 2. In the left sidebar, find "DEVICE CONTROL" section
echo.
echo 3. Click on:
echo    🖥️  Device Management
echo    💾 System Information
echo    📦 Installed Applications
echo    🎮 Remote Control
echo.
echo 4. The application will start sending data to the web server
echo    - System info every 5 minutes
echo    - App list every 1 hour
echo.
echo 📝 Configuration:
echo    - API Server: http://localhost:8888
echo    - Update intervals in App.config if needed
echo    - Location: bin\x64\Release\net6.0-windows\win-x64\EmployeeAttendance.dll.config
echo.
echo 🔍 To view debug output:
echo    - Run from Visual Studio in Debug mode
echo    - Check Output window (Debug filter)
echo.
echo 💡 Need help?
echo    - See SYSTEM_READY_STATUS.md for full documentation
echo    - Check TESTING_GUIDE.md for testing instructions
echo.
pause
