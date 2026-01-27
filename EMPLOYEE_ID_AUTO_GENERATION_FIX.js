/**
 * FIX: Employee ID Auto-Generation
 * 
 * The problem: Employee ID is NOT being auto-generated
 * Root cause: Server is not generating employee_id, and response format is wrong
 * 
 * The fix has 2 parts:
 * 1. Fix server.js add_employee() to generate employee ID
 * 2. Fix API response format to match documentation
 */

// ============================================================
// PART 1: FIX SERVER.JS (server.js line 814+)
// ============================================================

// FIND THIS CODE:
/*
    // Add new employee
    async add_employee(body) {
        try {
            const { company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes } = body;
            
            if (!company_name || !full_name) {
                return { success: false, error: 'Company name and full name are required' };
            }
            
            const result = await queryDB(
                `INSERT INTO company_employees (company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes, is_active, created_at, updated_at)
                 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, true, NOW(), NOW())
                 RETURNING id`,
                [company_name, employee_id || '', full_name, email || '', phone || '', department || '', designation || '', lunch_duration || 60, significant_idle_threshold_minutes || 10]
            );
            
            return { success: true, message: 'Employee added successfully', id: result[0]?.id };
        } catch (err) {
            console.error('Add employee error:', err.message);
            return { success: false, error: err.message };
        }
    }
*/

// REPLACE WITH:
/*
    // Add new employee
    async add_employee(body) {
        try {
            const { company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes } = body;
            
            if (!company_name || !full_name) {
                return { success: false, error: 'Company name and full name are required' };
            }
            
            // AUTO-GENERATE EMPLOYEE ID if not provided
            let generatedEmployeeId = employee_id;
            if (!employee_id) {
                // Get company initials (e.g., "WIZONE IT NETWORK..." → "WIN")
                const companyInitials = company_name
                    .split(' ')
                    .slice(0, 3)
                    .map(word => word[0])
                    .join('')
                    .toUpperCase()
                    .substring(0, 3) || 'EMP';
                
                // Get current year (e.g., 2026)
                const year = new Date().getFullYear();
                
                // Get next employee number
                const countResult = await queryDB(
                    `SELECT COUNT(*) as count FROM company_employees WHERE company_name = $1`,
                    [company_name]
                );
                
                const nextNumber = (countResult[0]?.count || 0) + 1;
                const paddedNumber = String(nextNumber).padStart(3, '0');
                
                generatedEmployeeId = `${companyInitials}-${year}-${paddedNumber}`;
                console.log(`Generated Employee ID: ${generatedEmployeeId}`);
            }
            
            const result = await queryDB(
                `INSERT INTO company_employees (company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes, is_active, created_at, updated_at)
                 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, true, NOW(), NOW())
                 RETURNING id`,
                [company_name, generatedEmployeeId, full_name, email || '', phone || '', department || '', designation || '', lunch_duration || 60, significant_idle_threshold_minutes || 10]
            );
            
            // Return in correct format with data wrapper
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
*/

// ============================================================
// KEY CHANGES:
// ============================================================
/*
1. Auto-generate employee_id if not provided:
   - Extract company initials (first 3 words)
   - Add current year
   - Add auto-incrementing number (001, 002, 003...)
   - Example: "WIN-2026-001"

2. Wrap response in data object:
   - Return { success, message, data: { ... } }
   - Include all employee details
   - This matches API documentation and fixes the client error

3. Add logging:
   - Console.log shows generated ID for debugging
*/

// ============================================================
// PART 2: VERIFY CLIENT RESPONSE HANDLER
// ============================================================

// In your app.js or wherever saveNewEmployee() is:
// Make sure it accesses: response.data.employee_id

// BEFORE (❌ WRONG):
/*
const response = await apiCall('add_employee', {...});
const empId = response.employee_id;  // Undefined!
*/

// AFTER (✅ CORRECT):
/*
const response = await apiCall('add_employee', {...});
const empId = response.data.employee_id;  // Works!
*/

// ============================================================
// TESTING:
// ============================================================
/*
After applying the fix:

1. Stop the web server (npm stop)
2. Update server.js with the new add_employee() function
3. Start the web server (npm start)
4. Refresh browser (Ctrl+F5)
5. Try to add an employee
6. Should see in console: "Generated Employee ID: WIN-2026-001" (or similar)
7. Employee should be created successfully
8. No "Cannot read properties of undefined" error
*/

// ============================================================
// EXAMPLE API CALL:
// ============================================================
/*
Request:
{
  "company_name": "WIZONE IT NETWORK INDIA PVT LTD",
  "full_name": "John Doe",
  "email": "john@company.com",
  "phone": "9876543210",
  "department": "Engineering",
  "designation": "Developer"
  // Note: NO employee_id sent (will be auto-generated)
}

Response (CORRECT):
{
  "success": true,
  "message": "Employee added successfully",
  "data": {
    "id": 42,
    "employee_id": "WIN-2026-012",  // ✅ Auto-generated!
    "full_name": "John Doe",
    "email": "john@company.com",
    "phone": "9876543210",
    "department": "Engineering",
    "designation": "Developer",
    "company_name": "WIZONE IT NETWORK INDIA PVT LTD"
  }
}
*/
