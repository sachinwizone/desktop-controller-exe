# 🛑 How to Stop and Uninstall Employee Attendance

## Problem
When trying to uninstall, you see: **"Application is running - cannot uninstall"**

## Solution
You need to **forcefully stop the application** first, then uninstall.

---

## 📋 Method 1: Using the Batch File (EASIEST)

### **Step 1: Run the Stop Script**
1. **Right-click** on `STOP_EmployeeAttendance.bat`
2. Select **"Run as Administrator"**
3. Press any key when prompted
4. Application will be forcefully stopped

### **Step 2: Uninstall**
1. Go to **Control Panel** → **Programs and Features**
2. Find **"Employee Attendance System"**
3. Click **Uninstall**
4. Follow the wizard

---

## 📋 Method 2: Using Command Prompt (MANUAL)

### **Step 1: Open Command Prompt as Administrator**
1. Press **Windows Key**
2. Type: `cmd`
3. Right-click **Command Prompt**
4. Select **"Run as administrator"**

### **Step 2: Stop the Application**
Copy and paste this command:
```batch
taskkill /F /IM "EmployeeAttendance.exe"
```

Press **Enter**

### **Step 3: Verify It Stopped**
```batch
tasklist | findstr "EmployeeAttendance"
```

If nothing shows, the application is stopped ✅

### **Step 4: Uninstall**
Now you can uninstall from Control Panel.

---

## 📋 Method 3: Using Task Manager (GUI)

### **Step 1: Open Task Manager**
- Press **Ctrl + Shift + Esc**
- OR Right-click Taskbar → **Task Manager**

### **Step 2: Find the Process**
1. Go to **Details** tab
2. Look for **EmployeeAttendance.exe**
3. Right-click on it
4. Select **"End Task"**

### **Step 3: Confirm Termination**
- Click **"End Process"** if prompted

### **Step 4: Uninstall**
Now uninstall from Control Panel.

---

## 📋 Method 4: PowerShell (ADVANCED)

### **Step 1: Open PowerShell as Administrator**
1. Press **Windows Key**
2. Type: `powershell`
3. Right-click **Windows PowerShell**
4. Select **"Run as administrator"**

### **Step 2: Stop the Application**
```powershell
Stop-Process -Name "EmployeeAttendance" -Force
```

### **Step 3: Verify**
```powershell
Get-Process | Where-Object {$_.Name -eq "EmployeeAttendance"}
```

If empty, application is stopped ✅

---

## 🔧 Complete Stop Commands

### **Single Command (Batch/CMD):**
```batch
taskkill /F /IM "EmployeeAttendance.exe"
```

### **PowerShell Command:**
```powershell
Stop-Process -Name "EmployeeAttendance" -Force
```

### **What These Commands Do:**
- `taskkill` = Windows command to terminate processes
- `/F` = Force termination (doesn't ask for confirmation)
- `/IM` = By image name (executable name)
- `EmployeeAttendance.exe` = The process to stop

---

## ⚠️ Important Notes

### **Why Application Keeps Running:**
1. **Auto-Start Enabled** - Runs on Windows login
2. **System Tray** - Runs in background (hidden)
3. **Protected** - Designed to restart if closed normally

### **Force Stop is Safe:**
- ✅ Does NOT damage the system
- ✅ Does NOT corrupt files
- ✅ Just stops the process immediately
- ✅ Required for uninstallation

### **After Stopping:**
- Application will **NOT restart automatically** until next Windows login
- You have time to uninstall it
- If Windows reboots before uninstall, it will start again

---

## 🚫 Disable Auto-Start (Before Uninstalling)

### **Optional: Prevent Auto-Start**

If you want to stop it from starting on next login:

**Using Registry Editor:**
1. Press **Windows Key + R**
2. Type: `regedit`
3. Navigate to: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
4. Delete the entry: **"EmployeeAttendance"**

**Using Task Manager:**
1. Open **Task Manager** (Ctrl + Shift + Esc)
2. Go to **"Startup"** tab
3. Find **"EmployeeAttendance"**
4. Right-click → **Disable**

---

## 📝 Step-by-Step Uninstall Procedure

### **Complete Uninstall Process:**

1. **Stop the Application:**
   ```batch
   taskkill /F /IM "EmployeeAttendance.exe"
   ```

2. **Disable Auto-Start** (Optional):
   - Task Manager → Startup tab → Disable EmployeeAttendance

3. **Uninstall:**
   - Control Panel → Programs and Features
   - Find "Employee Attendance System"
   - Uninstall

4. **Clean Registry** (Optional):
   - Delete: `HKEY_CURRENT_USER\SOFTWARE\EmployeeAttendance`
   - Delete: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\EmployeeAttendance`

5. **Delete Local Data** (Optional):
   - Delete: `C:\Users\[YourName]\AppData\Local\EmployeeAttendance`

---

## 🔄 Quick Reference Card

### **To Stop Application:**
```batch
# Right-click this file → Run as Administrator
STOP_EmployeeAttendance.bat
```

**OR**

```batch
# Open CMD as Admin, then run:
taskkill /F /IM "EmployeeAttendance.exe"
```

### **To Check if Running:**
```batch
tasklist | findstr "EmployeeAttendance"
```

### **To Uninstall:**
```
Control Panel → Programs → Uninstall a program → Employee Attendance System → Uninstall
```

---

## ❓ Troubleshooting

### **Problem: "Access Denied" when running taskkill**
**Solution:** Run Command Prompt as **Administrator**

### **Problem: "Process not found"**
**Solution:** Application is already stopped. Proceed to uninstall.

### **Problem: Application restarts after stopping**
**Solution:**
1. Stop it again
2. Immediately go to Control Panel and start uninstall
3. OR disable auto-start first

### **Problem: Uninstaller not found**
**Solution:** Use this command to find it:
```batch
wmic product where "name like '%Employee%'" get name,version
```

---

## 📦 Files Provided

### **1. STOP_EmployeeAttendance.bat**
- Double-click to stop the application
- Must run as Administrator
- Simple and easy to use

### **2. This Guide (FORCE_STOP_AND_UNINSTALL.md)**
- Complete instructions
- Multiple methods
- Troubleshooting

---

## ✅ Summary

### **Quickest Way to Uninstall:**

1. **Download/Locate:** `STOP_EmployeeAttendance.bat`
2. **Right-click** → **Run as Administrator**
3. **Wait** for "SUCCESS" message
4. **Go to Control Panel** → **Uninstall**

**That's it!** 🎉

---

## 🔒 Security Note

**These commands are safe:**
- Standard Windows commands
- Used by IT professionals
- No data loss
- No system damage
- Only stops the application process

**The application is designed to:**
- Run continuously
- Auto-restart on login
- Stay in system tray
- This is normal for monitoring software

**Force stop is the intended way to shut it down for uninstallation.**

---

**Created:** February 10, 2026
**Application:** Employee Attendance System
**Process Name:** EmployeeAttendance.exe
