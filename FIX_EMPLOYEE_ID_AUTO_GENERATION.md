# 🔧 How to Fix Employee ID Auto-Generation

## Problem
Employee ID is NOT being auto-generated when creating new employees. Error: "Cannot read properties of undefined (reading 'employee_id')"

## Root Causes
1. Server's `add_employee()` function doesn't generate employee ID - it just accepts empty string
2. API response format is wrong - returns `id` directly instead of `data.employee_id`
3. Client code tries to access `response.employee_id` but should access `response.data.employee_id`

## Solution (2 Steps)

### STEP 1: Fix the Server (server.js)

**File:** `c:\Users\sachi\Desktop\EXE - DESKTOP CONTROLLER\web_dashboard_new\server.js`

**Location:** Line ~814 (find `async add_employee(body) {`)

**ACTION:** Replace the entire `add_employee()` function with this code:

```javascript
// Add new employee
async add_employee(body) {
    try {
        const { company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes } = body;
        
        if (!company_name || !full_name) {
            return { success: false, error: 'Company name and full name are required' };
        }
        
        // AUTO-GENERATE EMPLOYEE ID if not provided
        let generatedEmployeeId = employee_id;
        if (!employee_id || employee_id.trim() === '') {
            // Get company initials (e.g., "WIZONE IT" → "WIN")
            const words = company_name.toUpperCase().split(' ');
            const companyInitials = words
                .slice(0, Math.min(3, words.length))
                .map(w => w[0])
                .join('')
                .substring(0, 3);
            
            const year = new Date().getFullYear();
            
            // Count existing employees for this company
            const countResult = await queryDB(
                `SELECT COUNT(*) as count FROM company_employees WHERE company_name = $1`,
                [company_name]
            );
            
            const nextNumber = (countResult[0]?.count || 0) + 1;
            const paddedNumber = String(nextNumber).padStart(3, '0');
            
            generatedEmployeeId = `${companyInitials}-${year}-${paddedNumber}`;
            console.log(`[API] Generated Employee ID: ${generatedEmployeeId}`);
        }
        
        const result = await queryDB(
            `INSERT INTO company_employees (company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes, is_active, created_at, updated_at)
             VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, true, NOW(), NOW())
             RETURNING id`,
            [company_name, generatedEmployeeId, full_name, email || '', phone || '', department || '', designation || '', lunch_duration || 60, significant_idle_threshold_minutes || 10]
        );
        
        // Return correct response format with data wrapper
        return { 
            success: true, 
            message: 'Employee added successfully',
            data: {
                id: result[0]?.id,
                employee_id: generatedEmployeeId,
                full_name: full_name,
                email: email || '',
                phone: phone || '',
                department: department || '',
                designation: designation || '',
                company_name: company_name
            }
        };
    } catch (err) {
        console.error('Add employee error:', err.message);
        return { success: false, error: err.message };
    }
}
```

### STEP 2: Restart the Server

1. Stop the web server:
   - In terminal: Press `Ctrl+C`
   
2. Restart it:
   ```
   npm start
   ```

3. Refresh browser:
   - `Ctrl+F5` (or Cmd+Shift+R on Mac)

## Testing

1. **Open Employee Management** in web dashboard
2. **Click "+ Add Employee"**
3. **Fill the form** (name, email, phone, department, designation required)
4. **Click "Save Employee"**
5. **Check console** (F12 → Console tab)
   - Should see: `[API] Generated Employee ID: WIN-2026-001` (or similar)
   - Should NOT see: "Cannot read properties of undefined" error
6. **Check employee list**
   - New employee should appear with generated ID like "WIN-2026-001"

## What the Fix Does

**Auto-Generation Logic:**
```
Company: "WIZONE IT NETWORK INDIA PVT LTD"
         → Initials: "WIN" (first letter of first 3 words)
         → Year: 2026
         → Number: 001 (auto-increment)
         → Result: "WIN-2026-001"
```

**For next employee:**
- "WIN-2026-002"
- "WIN-2026-003"
- etc.

**Response Format:**
- Before: `{ success: true, id: 42 }`
- After: `{ success: true, data: { id: 42, employee_id: "WIN-2026-001", ... } }`

This matches the API documentation and fixes the client error.

## If Still Broken

1. **Check browser console** (F12)
   - Look for actual error message
   - Screenshot it

2. **Check server console** output
   - Look for "[API] Generated Employee ID" message
   - Look for "Add employee error" message

3. **Verify file was edited** correctly
   - Make sure entire function was replaced
   - Check for syntax errors (missing brackets, etc.)

4. **Clear cache and reload**
   - Ctrl+Shift+Delete (or Cmd+Shift+Delete)
   - Select "All time"
   - Clear cookies and cache
   - Refresh page

## Questions?

If it still doesn't work, check:
- ✅ Is server.js in correct location?
- ✅ Was the function fully replaced?
- ✅ Did server restart without errors?
- ✅ Is browser cache cleared?
- ✅ Did you refresh page (Ctrl+F5)?
