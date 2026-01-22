const http = require('http');
const fs = require('fs');
const path = require('path');
const { Client } = require('pg');

const PORT = 8888;

// PostgreSQL connection config (same as Desktop Controller)
const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

// MIME types for static files
const mimeTypes = {
    '.html': 'text/html',
    '.css': 'text/css',
    '.js': 'application/javascript',
    '.json': 'application/json',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.gif': 'image/gif',
    '.ico': 'image/x-icon'
};

// Database query helper
async function queryDB(sql, params = []) {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        const result = await client.query(sql, params);
        return result.rows;
    } catch (err) {
        console.error('Database error:', err.message);
        throw err;
    } finally {
        await client.end();
    }
}

// API handlers
const apiHandlers = {
    // Step 1: Verify activation key from activation_keys table
    async verify_key(body) {
        const { activation_key } = body;
        if (!activation_key) {
            return { success: false, error: 'Activation key is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT id, activation_key, company_name, is_active, created_at 
                 FROM activation_keys 
                 WHERE activation_key = $1 AND is_active = TRUE 
                 LIMIT 1`,
                [activation_key.toUpperCase()]
            );
            
            if (rows.length > 0) {
                const keyData = rows[0];
                return {
                    success: true,
                    data: {
                        id: keyData.id,
                        activation_key: keyData.activation_key,
                        company_name: keyData.company_name
                    }
                };
            }
            return { success: false, error: 'Invalid or inactive activation key' };
        } catch (err) {
            console.error('Verify key error:', err.message);
            return { success: false, error: 'Database error: ' + err.message };
        }
    },

    // Login - directly with username/password from company_users table
    async login(body) {
        const { username, password, role } = body;
        
        if (!username || !password) {
            return { success: false, error: 'Username and password are required' };
        }
        
        try {
            // Get user from company_users table along with activation key info
            const userRows = await queryDB(
                `SELECT cu.id, cu.username, cu.full_name, cu.email, cu.role_id, cu.company_name, 
                        cu.is_active, cu.activation_key_id, ak.activation_key
                 FROM company_users cu
                 LEFT JOIN activation_keys ak ON cu.activation_key_id = ak.id
                 WHERE cu.username = $1 AND cu.password = $2 AND cu.is_active = TRUE 
                 LIMIT 1`,
                [username, password]
            );
            
            if (userRows.length > 0) {
                const user = userRows[0];
                return {
                    success: true,
                    user: {
                        id: user.id,
                        username: user.username,
                        full_name: user.full_name || user.username,
                        email: user.email,
                        role: user.role_id === 1 ? 'admin' : 'user',
                        company_name: user.company_name,
                        activation_key: user.activation_key
                    }
                };
            }
            
            return { success: false, error: 'Invalid username or password' };
        } catch (err) {
            console.error('Login error:', err.message);
            return { success: false, error: 'Database error: ' + err.message };
        }
    },

    // Admin login - alias for login with admin-only check
    async admin_login(body) {
        const { username, password } = body;
        
        if (!username || !password) {
            return { success: false, error: 'Username and password are required' };
        }
        
        try {
            const userRows = await queryDB(
                `SELECT cu.id, cu.username, cu.full_name, cu.email, cu.role_id, cu.company_name, 
                        cu.is_active, cu.activation_key_id, ak.activation_key
                 FROM company_users cu
                 LEFT JOIN activation_keys ak ON cu.activation_key_id = ak.id
                 WHERE cu.username = $1 AND cu.password = $2 AND cu.is_active = TRUE AND cu.role_id = 1
                 LIMIT 1`,
                [username, password]
            );
            
            if (userRows.length > 0) {
                const user = userRows[0];
                return {
                    success: true,
                    user: {
                        id: user.id,
                        username: user.username,
                        full_name: user.full_name || user.username,
                        email: user.email || '',
                        role: 'admin',
                        company_name: user.company_name || 'WIZONE IT NETWORK'
                    }
                };
            }
            
            return { success: false, error: 'Invalid username or password, or insufficient permissions' };
        } catch (err) {
            console.error('Admin login error:', err.message);
            return { success: false, error: 'Database error: ' + err.message };
        }
    },

    // Get dashboard stats from punch_log_consolidated
    async get_dashboard_stats(query) {
        try {
            const companyName = query.company_name;
            
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const today = new Date().toISOString().split('T')[0];
            
            // Total registered employees
            const totalEmployees = await queryDB(
                `SELECT COUNT(*) as count FROM company_employees WHERE company_name = $1`,
                [companyName]
            );
            
            // Present today (punched in)
            const presentToday = await queryDB(
                `SELECT COUNT(DISTINCT username) as count FROM punch_log_consolidated 
                 WHERE company_name = $1 AND DATE(punch_in_time) = $2`,
                [companyName, today]
            );
            
            // Currently working (punched in but not out)
            const currentlyWorking = await queryDB(
                `SELECT COUNT(*) as count FROM punch_log_consolidated 
                 WHERE company_name = $1 AND DATE(punch_in_time) = $2 AND punch_out_time IS NULL`,
                [companyName, today]
            );
            
            // Completed sessions today
            const completedSessions = await queryDB(
                `SELECT COUNT(*) as count FROM punch_log_consolidated 
                 WHERE company_name = $1 AND DATE(punch_in_time) = $2 AND punch_out_time IS NOT NULL`,
                [companyName, today]
            );

            return {
                success: true,
                data: {
                    total_employees: parseInt(totalEmployees[0]?.count || 0),
                    present_today: parseInt(presentToday[0]?.count || 0),
                    currently_working: parseInt(currentlyWorking[0]?.count || 0),
                    completed_sessions: parseInt(completedSessions[0]?.count || 0)
                }
            };
        } catch (err) {
            console.error('Dashboard stats error:', err.message);
            return { success: false, error: err.message, data: { total_employees: 0, present_today: 0, currently_working: 0, completed_sessions: 0 }};
        }
    },

    // Get today's attendance from punch_log_consolidated
    async get_today_attendance(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const today = new Date().toISOString().split('T')[0];
            
            const rows = await queryDB(`
                SELECT p.id, p.punch_in_time, p.punch_out_time, p.break_duration_seconds,
                       p.total_work_duration_seconds, p.system_name, p.username, p.company_name,
                       p.ip_address, p.machine_id,
                       COALESCE(p.display_name, ce.full_name, p.username) as display_name, 
                       ce.department
                FROM punch_log_consolidated p
                LEFT JOIN company_employees ce ON ce.employee_id = p.username AND ce.company_name = p.company_name
                WHERE p.company_name = $1 AND DATE(p.punch_in_time) = $2
                ORDER BY p.punch_in_time DESC
            `, [companyName, today]);
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Today attendance error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get all attendance records from punch_log_consolidated
    async get_attendance(query) {
        try {
            const companyName = query.company_name;
            console.log('get_attendance called for company:', companyName, 'dates:', query.start_date, 'to', query.end_date);
            
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            let sql = `
                SELECT p.id, p.punch_in_time, p.punch_out_time, p.break_duration_seconds,
                       p.total_work_duration_seconds, p.system_name, p.username, p.company_name,
                       p.ip_address, p.machine_id, p.created_at,
                       COALESCE(p.display_name, ce.full_name, p.username) as display_name, 
                       ce.department
                FROM punch_log_consolidated p
                LEFT JOIN company_employees ce ON ce.employee_id = p.username AND ce.company_name = p.company_name
                WHERE p.company_name = $1
            `;
            let params = [companyName];
            let paramIndex = 2;
            
            if (query.start_date) {
                sql += ` AND DATE(p.punch_in_time AT TIME ZONE 'Asia/Kolkata') >= $${paramIndex}`;
                params.push(query.start_date);
                paramIndex++;
            }
            
            if (query.end_date) {
                sql += ` AND DATE(p.punch_in_time AT TIME ZONE 'Asia/Kolkata') <= $${paramIndex}`;
                params.push(query.end_date);
                paramIndex++;
            }
            
            if (query.employee_id) {
                sql += ` AND p.username = $${paramIndex}`;
                params.push(query.employee_id);
                paramIndex++;
            }
            
            if (query.department) {
                sql += ` AND ce.department = $${paramIndex}`;
                params.push(query.department);
                paramIndex++;
            }
            
            sql += ' ORDER BY p.punch_in_time DESC LIMIT 500';
            
            console.log('SQL:', sql);
            console.log('Params:', params);
            
            const rows = await queryDB(sql, params);
            console.log('Found', rows.length, 'attendance records');
            return { success: true, data: rows };
        } catch (err) {
            console.error('Attendance error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get daily work hours summary for an employee on a specific date
    async get_daily_work_hours(query) {
        try {
            const companyName = query.company_name;
            const employeeId = query.employee_id;
            const date = query.start_date || query.date;
            
            if (!companyName || !employeeId || !date) {
                return { success: false, error: 'Company name, employee ID, and date are required' };
            }
            
            console.log('get_daily_work_hours called for:', { companyName, employeeId, date });
            
            // Get all punch records for the employee on the given date
            const sql = `
                SELECT p.id, p.punch_in_time, p.punch_out_time, p.break_duration_seconds,
                       p.total_work_duration_seconds, p.system_name, p.username, p.company_name,
                       p.ip_address, p.machine_id, p.created_at,
                       COALESCE(p.display_name, ce.full_name, p.username) as display_name, 
                       ce.department
                FROM punch_log_consolidated p
                LEFT JOIN company_employees ce ON ce.employee_id = p.username AND ce.company_name = p.company_name
                WHERE p.company_name = $1 
                  AND p.username = $2
                  AND DATE(p.punch_in_time AT TIME ZONE 'Asia/Kolkata') = $3
                ORDER BY p.punch_in_time ASC
            `;
            
            const rows = await queryDB(sql, [companyName, employeeId, date]);
            
            // Calculate aggregated statistics and format sessions
            let totalWorkSeconds = 0;
            let totalBreakSeconds = 0;
            let firstPunchIn = null;
            let lastPunchOut = null;
            
            const sessions = rows.map(row => {
                // Calculate work duration from punch times if available
                let sessionWorkSeconds = 0;
                if (row.punch_in_time && row.punch_out_time) {
                    const inTime = new Date(row.punch_in_time);
                    const outTime = new Date(row.punch_out_time);
                    sessionWorkSeconds = (outTime - inTime) / 1000;
                    totalWorkSeconds += sessionWorkSeconds;
                    
                    if (!firstPunchIn) {
                        firstPunchIn = inTime;
                    }
                    lastPunchOut = outTime;
                }
                
                // Add break duration
                if (row.break_duration_seconds) {
                    totalBreakSeconds += row.break_duration_seconds;
                }
                
                // Format times for frontend display
                const punchInTime = row.punch_in_time ? new Date(row.punch_in_time) : null;
                const punchOutTime = row.punch_out_time ? new Date(row.punch_out_time) : null;
                
                return {
                    ...row,
                    punch_in_formatted: punchInTime ? this.formatDateTime(punchInTime) : 'N/A',
                    punch_out_formatted: punchOutTime ? this.formatDateTime(punchOutTime) : 'Still working',
                    work_hours: sessionWorkSeconds / 3600,
                    break_minutes: row.break_duration_seconds ? Math.round(row.break_duration_seconds / 60) : 0
                };
            });
            
            const totalWorkHours = totalWorkSeconds / 3600;
            const totalBreakMinutes = Math.round(totalBreakSeconds / 60);
            
            const result = {
                sessions: sessions,
                total_work_hours: parseFloat(totalWorkHours.toFixed(2)),
                total_break_minutes: totalBreakMinutes,
                first_punch_in: firstPunchIn ? this.formatTime(firstPunchIn) : 'N/A',
                last_punch_out: lastPunchOut ? this.formatTime(lastPunchOut) : 'N/A',
                session_count: sessions.length
            };
            
            console.log('Calculated daily work hours:', result);
            return { success: true, data: result };
        } catch (err) {
            console.error('Daily work hours error:', err.message);
            return { success: false, error: err.message, data: {} };
        }
    },

    // Helper function to format time from database (no conversion)
    formatTime(date) {
        const d = new Date(date);
        const hours = String(d.getUTCHours()).padStart(2, '0');
        const minutes = String(d.getUTCMinutes()).padStart(2, '0');
        return `${hours}:${minutes}`;
    },

    // Helper function to format date and time from database (no conversion)
    formatDateTime(date) {
        const d = new Date(date);
        const day = String(d.getUTCDate()).padStart(2, '0');
        const month = String(d.getUTCMonth() + 1).padStart(2, '0');
        const year = d.getUTCFullYear();
        const hours = String(d.getUTCHours()).padStart(2, '0');
        const minutes = String(d.getUTCMinutes()).padStart(2, '0');
        return `${day}/${month}/${year} ${hours}:${minutes}`;
    },

    // Get employees who have punch records (for attendance filter)
    async get_punch_employees(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const rows = await queryDB(`
                SELECT DISTINCT p.username as employee_id, 
                       COALESCE(p.display_name, ce.full_name, p.username) as display_name,
                       ce.department
                FROM punch_log_consolidated p
                LEFT JOIN company_employees ce ON ce.employee_id = p.username AND ce.company_name = p.company_name
                WHERE p.company_name = $1
                ORDER BY display_name
            `, [companyName]);
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Punch employees error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get employees from web logs with display names
    async get_log_employees(query) {
        try {
            const companyName = query.company_name;
            const logType = query.log_type || 'web'; // web, app, inactivity, screenshot
            
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            let tableName = 'web_logs';
            if (logType === 'app') tableName = 'application_logs';
            else if (logType === 'inactivity') tableName = 'inactivity_logs';
            else if (logType === 'screenshot') tableName = 'screenshot_logs';
            
            const rows = await queryDB(`
                SELECT DISTINCT username as employee_id, 
                       COALESCE(display_user_name, username) as display_name
                FROM ${tableName}
                WHERE company_name = $1
                ORDER BY display_name
            `, [companyName]);
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Log employees error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get registered employees from company_employees table
    async get_employees(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const rows = await queryDB(`
                SELECT ce.id, ce.employee_id, ce.full_name as display_name, ce.email, ce.department, ce.designation, 
                       ce.phone, ce.is_active, ce.created_at,
                       cs.system_id, cs.is_online, cs.last_heartbeat, cs.machine_name, cs.ip_address
                FROM company_employees ce
                LEFT JOIN connected_systems cs ON cs.employee_id = ce.employee_id AND cs.company_name = ce.company_name
                WHERE ce.company_name = $1
                ORDER BY ce.full_name
            `, [companyName]);
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Employees error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get company users from company_users table
    async get_users(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const rows = await queryDB(`
                SELECT id, username, full_name, email, role_id, company_name, is_active, created_at
                FROM company_users
                WHERE company_name = $1
                ORDER BY created_at DESC
            `, [companyName]);
            
            // Map role_id to role name
            const mappedRows = rows.map(u => ({
                ...u,
                role: u.role_id === 1 ? 'admin' : 'user'
            }));
            
            return { success: true, data: mappedRows };
        } catch (err) {
            console.error('Users error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get activity logs (recent punches)
    async get_activity_logs(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const limit = parseInt(query.limit) || 20;
            
            const rows = await queryDB(`
                SELECT p.id, p.punch_in_time, p.punch_out_time, p.system_name, p.username, 
                       p.company_name, p.ip_address, ce.display_name
                FROM punch_log_consolidated p
                LEFT JOIN company_employees ce ON ce.employee_id = p.username AND ce.company_name = p.company_name
                WHERE p.company_name = $1
                ORDER BY p.punch_in_time DESC 
                LIMIT $2
            `, [companyName, limit]);
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Activity logs error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // ==================== CORE MONITORING APIS ====================

    // Get Web Browsing Logs from web_logs table
    async get_web_logs(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            let whereClause = 'company_name = $1';
            let params = [companyName];
            let paramIndex = 2;
            
            if (query.start_date) {
                whereClause += ` AND DATE(visit_time) >= $${paramIndex}`;
                params.push(query.start_date);
                paramIndex++;
            }
            
            if (query.end_date) {
                whereClause += ` AND DATE(visit_time) <= $${paramIndex}`;
                params.push(query.end_date);
                paramIndex++;
            }
            
            if (query.search) {
                whereClause += ` AND (website_url ILIKE $${paramIndex} OR page_title ILIKE $${paramIndex} OR browser_name ILIKE $${paramIndex})`;
                params.push(`%${query.search}%`);
                paramIndex++;
            }
            
            if (query.employee_id || query.employee) {
                const empId = query.employee_id || query.employee;
                whereClause += ` AND username = $${paramIndex}`;
                params.push(empId);
                paramIndex++;
            }
            
            // Use subquery to limit rows before ordering (much faster)
            const sql = `SELECT id, log_timestamp, system_name, username, browser_name, website_url, page_title, category, visit_time, duration_seconds, ip_address, display_user_name FROM (SELECT * FROM web_logs WHERE ${whereClause} LIMIT 1000) sub ORDER BY visit_time DESC LIMIT 500`;
            
            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            console.error('Web logs error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get Application Usage Logs from application_logs table
    async get_application_logs(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            let whereClause = 'company_name = $1';
            let params = [companyName];
            let paramIndex = 2;
            
            if (query.start_date) {
                whereClause += ` AND DATE(start_time) >= $${paramIndex}`;
                params.push(query.start_date);
                paramIndex++;
            }
            
            if (query.end_date) {
                whereClause += ` AND DATE(start_time) <= $${paramIndex}`;
                params.push(query.end_date);
                paramIndex++;
            }
            
            if (query.search) {
                whereClause += ` AND (app_name ILIKE $${paramIndex} OR window_title ILIKE $${paramIndex})`;
                params.push(`%${query.search}%`);
                paramIndex++;
            }
            
            if (query.employee_id || query.employee) {
                const empId = query.employee_id || query.employee;
                whereClause += ` AND username = $${paramIndex}`;
                params.push(empId);
                paramIndex++;
            }
            
            // Use subquery to limit rows before ordering (much faster)
            const sql = `SELECT id, log_timestamp, system_name, username, app_name, window_title, start_time, end_time, duration_seconds, is_active, ip_address, display_user_name FROM (SELECT * FROM application_logs WHERE ${whereClause} LIMIT 1000) sub ORDER BY start_time DESC LIMIT 500`;
            
            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            console.error('Application logs error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get Inactivity Logs from inactivity_logs table
    async get_inactivity_logs(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            let whereClause = 'company_name = $1';
            let params = [companyName];
            let paramIndex = 2;
            
            if (query.start_date) {
                whereClause += ` AND DATE(start_time) >= $${paramIndex}`;
                params.push(query.start_date);
                paramIndex++;
            }
            
            if (query.end_date) {
                whereClause += ` AND DATE(start_time) <= $${paramIndex}`;
                params.push(query.end_date);
                paramIndex++;
            }
            
            if (query.search) {
                whereClause += ` AND (system_name ILIKE $${paramIndex} OR username ILIKE $${paramIndex})`;
                params.push(`%${query.search}%`);
                paramIndex++;
            }
            
            if (query.employee_id) {
                whereClause += ` AND username = $${paramIndex}`;
                params.push(query.employee_id);
                paramIndex++;
            }
            
            // Use subquery to limit rows before ordering (much faster)
            const sql = `SELECT id, log_timestamp, system_name, username, start_time, end_time, duration_seconds, status, ip_address, display_user_name FROM (SELECT * FROM inactivity_logs WHERE ${whereClause} LIMIT 1000) sub ORDER BY start_time DESC LIMIT 500`;
            
            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            console.error('Inactivity logs error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get Screenshots from screenshot_logs table
    async get_screenshots(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            // Don't select screenshot_data in list query (too large), just metadata
            let whereClause = 'company_name = $1';
            let params = [companyName];
            let paramIndex = 2;
            
            if (query.start_date) {
                whereClause += ` AND DATE(log_timestamp) >= $${paramIndex}`;
                params.push(query.start_date);
                paramIndex++;
            }
            
            if (query.end_date) {
                whereClause += ` AND DATE(log_timestamp) <= $${paramIndex}`;
                params.push(query.end_date);
                paramIndex++;
            }
            
            if (query.employee_id || query.employee) {
                const empId = query.employee_id || query.employee;
                whereClause += ` AND username = $${paramIndex}`;
                params.push(empId);
                paramIndex++;
            }
            
            // Use subquery to limit rows before ordering (much faster)
            const sql = `SELECT id, log_timestamp, system_name, username, log_timestamp as capture_time, screen_width, screen_height, ip_address, display_user_name FROM (SELECT * FROM screenshot_logs WHERE ${whereClause} LIMIT 500) sub ORDER BY log_timestamp DESC LIMIT 100`;
            
            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            console.error('Screenshots error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get single screenshot with image data
    async get_screenshot_image(query) {
        try {
            const id = query.id;
            const companyName = query.company_name;
            
            if (!id || !companyName) {
                return { success: false, error: 'Screenshot ID and company name required' };
            }
            
            const rows = await queryDB(
                `SELECT id, screenshot_data, system_name, username, log_timestamp as capture_time FROM screenshot_logs WHERE id = $1 AND company_name = $2`,
                [id, companyName]
            );
            
            if (rows.length > 0) {
                return { success: true, data: rows[0] };
            }
            return { success: false, error: 'Screenshot not found' };
        } catch (err) {
            console.error('Screenshot image error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // ==================== EMPLOYEE MANAGEMENT ====================
    
    // Get all employees for a company
    async get_employees(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            // Check if specific employee ID (database ID) is requested
            if (query.id) {
                const rows = await queryDB(
                    `SELECT id, employee_id, full_name, email, phone, department, designation, is_active, lunch_duration, significant_idle_threshold_minutes, created_at 
                     FROM company_employees 
                     WHERE company_name = $1 AND id = $2 
                     LIMIT 1`,
                    [companyName, query.id]
                );
                return { success: true, data: rows };
            }
            
            // Check if specific employee_id (username) is requested
            if (query.employee_id) {
                const rows = await queryDB(
                    `SELECT id, employee_id, full_name, email, phone, department, designation, is_active, lunch_duration, significant_idle_threshold_minutes, created_at 
                     FROM company_employees 
                     WHERE company_name = $1 AND employee_id = $2 
                     LIMIT 1`,
                    [companyName, query.employee_id]
                );
                return { success: true, data: rows };
            }
            
            // Otherwise return all employees for the company
            const rows = await queryDB(
                `SELECT id, employee_id, full_name, email, phone, department, designation, is_active, lunch_duration, significant_idle_threshold_minutes, created_at 
                 FROM company_employees 
                 WHERE company_name = $1 
                 ORDER BY full_name`,
                [companyName]
            );
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get employees error:', err.message);
            return { success: false, error: err.message };
        }
    },

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
    },

    // Update employee
    async update_employee(body) {
        try {
            const { id, company_name, employee_id, full_name, email, phone, department, designation, is_active, lunch_duration, significant_idle_threshold_minutes } = body;
            
            if (!id || !company_name) {
                return { success: false, error: 'Employee ID and company name are required' };
            }
            
            await queryDB(
                `UPDATE company_employees 
                 SET employee_id = $1, full_name = $2, email = $3, phone = $4, department = $5, designation = $6, is_active = $7, lunch_duration = $8, significant_idle_threshold_minutes = $9, updated_at = NOW()
                 WHERE id = $10 AND company_name = $11`,
                [employee_id || '', full_name, email || '', phone || '', department || '', designation || '', is_active !== false, lunch_duration || 60, significant_idle_threshold_minutes || 10, id, company_name]
            );
            
            return { success: true, message: 'Employee updated successfully' };
        } catch (err) {
            console.error('Update employee error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Delete employee
    async delete_employee(body) {
        try {
            const { id, company_name } = body;
            
            if (!id || !company_name) {
                return { success: false, error: 'Employee ID and company name are required' };
            }
            
            await queryDB(
                `DELETE FROM company_employees WHERE id = $1 AND company_name = $2`,
                [id, company_name]
            );
            
            return { success: true, message: 'Employee deleted successfully' };
        } catch (err) {
            console.error('Delete employee error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // ==================== DEPARTMENT MANAGEMENT ====================
    
    // Get all departments for a company
    async get_departments(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) {
                return { success: false, error: 'Company name required' };
            }
            
            const rows = await queryDB(
                `SELECT id, department_name, department_code, description, is_active, created_at 
                 FROM company_departments 
                 WHERE company_name = $1 
                 ORDER BY department_name`,
                [companyName]
            );
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get departments error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Add new department
    async add_department(body) {
        try {
            const { company_name, department_name, department_code, description } = body;
            
            if (!company_name || !department_name) {
                return { success: false, error: 'Company name and department name are required' };
            }
            
            // Get company_id from activation_keys
            const keyResult = await queryDB(
                `SELECT company_id FROM activation_keys WHERE company_name = $1 LIMIT 1`,
                [company_name]
            );
            const companyId = keyResult[0]?.company_id || 1;
            
            const result = await queryDB(
                `INSERT INTO company_departments (company_id, company_name, department_name, department_code, description, is_active, created_at, updated_at)
                 VALUES ($1, $2, $3, $4, $5, true, NOW(), NOW())
                 RETURNING id`,
                [companyId, company_name, department_name, department_code || department_name.substring(0, 3).toUpperCase(), description || '']
            );
            
            return { success: true, message: 'Department added successfully', id: result[0]?.id };
        } catch (err) {
            console.error('Add department error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Update department
    async update_department(body) {
        try {
            const { id, company_name, department_name, department_code, description, is_active } = body;
            
            if (!id || !company_name) {
                return { success: false, error: 'Department ID and company name are required' };
            }
            
            await queryDB(
                `UPDATE company_departments 
                 SET department_name = $1, department_code = $2, description = $3, is_active = $4, updated_at = NOW()
                 WHERE id = $5 AND company_name = $6`,
                [department_name, department_code || '', description || '', is_active !== false, id, company_name]
            );
            
            return { success: true, message: 'Department updated successfully' };
        } catch (err) {
            console.error('Update department error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Delete department
    async delete_department(body) {
        try {
            const { id, company_name } = body;
            
            if (!id || !company_name) {
                return { success: false, error: 'Department ID and company name are required' };
            }
            
            await queryDB(
                `DELETE FROM company_departments WHERE id = $1 AND company_name = $2`,
                [id, company_name]
            );
            
            return { success: true, message: 'Department deleted successfully' };
        } catch (err) {
            console.error('Delete department error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // ==================== CONNECTED SYSTEMS (LIVE TRACKING) ====================
    
    // Heartbeat - EXE sends this regularly to report it's online
    async system_heartbeat(body) {
        try {
            const { company_name, employee_id, display_name, department, machine_id, system_id, machine_name, ip_address, os_version, app_version, status } = body;
            
            if (!company_name || !employee_id || !machine_id) {
                return { success: false, error: 'Company name, employee ID and machine ID are required' };
            }
            
            // Try to add system_id column if it doesn't exist (migration)
            try {
                await queryDB(`ALTER TABLE connected_systems ADD COLUMN IF NOT EXISTS system_id VARCHAR(50)`);
            } catch (e) {}
            
            // Upsert - insert or update on conflict
            await queryDB(
                `INSERT INTO connected_systems 
                 (company_name, employee_id, display_name, department, machine_id, system_id, machine_name, ip_address, os_version, app_version, last_heartbeat, is_online, status)
                 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, NOW(), true, $11)
                 ON CONFLICT (company_name, employee_id, machine_id) 
                 DO UPDATE SET 
                    display_name = EXCLUDED.display_name,
                    department = EXCLUDED.department,
                    system_id = EXCLUDED.system_id,
                    machine_name = EXCLUDED.machine_name,
                    ip_address = EXCLUDED.ip_address,
                    os_version = EXCLUDED.os_version,
                    app_version = EXCLUDED.app_version,
                    last_heartbeat = NOW(),
                    is_online = true,
                    status = EXCLUDED.status`,
                [company_name, employee_id, display_name || '', department || '', machine_id, system_id || '', machine_name || '', ip_address || '', os_version || '', app_version || '1.0', status || 'active']
            );
            
            return { success: true, message: 'Heartbeat received' };
        } catch (err) {
            console.error('Heartbeat error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get all connected systems for a company (includes all registered employees)
    async get_connected_systems(query) {
        try {
            const companyName = query.company_name;
            
            // Mark all systems as offline if no heartbeat in last 2 minutes
            await queryDB(
                `UPDATE connected_systems 
                 SET is_online = false 
                 WHERE last_heartbeat < NOW() - INTERVAL '2 minutes'`
            );
            
            // Determine the company to query
            const targetCompany = (!companyName || companyName === 'undefined' || companyName === 'Demo Company') 
                ? null : companyName;
            
            // Get ALL employees from company_employees and LEFT JOIN with connected_systems
            // This shows all registered employees, even those who haven't connected yet
            let rows;
            if (targetCompany) {
                rows = await queryDB(
                    `SELECT 
                        COALESCE(cs.id, 0) as id,
                        ce.company_name,
                        ce.employee_id,
                        ce.full_name as display_name,
                        ce.department,
                        COALESCE(cs.machine_id, '') as machine_id,
                        COALESCE(cs.machine_name, 'Not Connected') as machine_name,
                        COALESCE(cs.ip_address, '-') as ip_address,
                        COALESCE(cs.os_version, '-') as os_version,
                        COALESCE(cs.app_version, '-') as app_version,
                        cs.last_heartbeat,
                        cs.first_connected,
                        COALESCE(cs.is_online, false) as is_online,
                        COALESCE(cs.status, 'not-connected') as status,
                        COALESCE(cs.system_id, '') as system_id
                     FROM company_employees ce
                     LEFT JOIN connected_systems cs 
                        ON ce.employee_id = cs.employee_id 
                        AND ce.company_name = cs.company_name
                     WHERE ce.company_name = $1 AND ce.is_active = true
                     ORDER BY cs.is_online DESC NULLS LAST, ce.full_name ASC`,
                    [targetCompany]
                );
            } else {
                // Admin view - show all employees from all companies
                rows = await queryDB(
                    `SELECT 
                        COALESCE(cs.id, 0) as id,
                        ce.company_name,
                        ce.employee_id,
                        ce.full_name as display_name,
                        ce.department,
                        COALESCE(cs.machine_id, '') as machine_id,
                        COALESCE(cs.machine_name, 'Not Connected') as machine_name,
                        COALESCE(cs.ip_address, '-') as ip_address,
                        COALESCE(cs.os_version, '-') as os_version,
                        COALESCE(cs.app_version, '-') as app_version,
                        cs.last_heartbeat,
                        cs.first_connected,
                        COALESCE(cs.is_online, false) as is_online,
                        COALESCE(cs.status, 'not-connected') as status,
                        COALESCE(cs.system_id, '') as system_id
                     FROM company_employees ce
                     LEFT JOIN connected_systems cs 
                        ON ce.employee_id = cs.employee_id 
                        AND ce.company_name = cs.company_name
                     WHERE ce.is_active = true
                     ORDER BY cs.is_online DESC NULLS LAST, ce.company_name, ce.full_name ASC`
                );
            }
            
            // Calculate online/offline/not-connected counts
            const onlineCount = rows.filter(r => r.is_online).length;
            const offlineCount = rows.filter(r => !r.is_online && r.machine_id).length;
            const notConnectedCount = rows.filter(r => !r.machine_id).length;
            
            return { 
                success: true, 
                data: rows,
                stats: {
                    total: rows.length,
                    online: onlineCount,
                    offline: offlineCount,
                    notConnected: notConnectedCount
                }
            };
        } catch (err) {
            console.error('Get connected systems error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // System disconnect - when EXE closes
    async system_disconnect(body) {
        try {
            const { company_name, employee_id, machine_id } = body;
            
            if (!company_name || !employee_id || !machine_id) {
                return { success: false, error: 'Company name, employee ID and machine ID are required' };
            }
            
            await queryDB(
                `UPDATE connected_systems 
                 SET is_online = false, last_heartbeat = NOW() 
                 WHERE company_name = $1 AND employee_id = $2 AND machine_id = $3`,
                [company_name, employee_id, machine_id]
            );
            
            return { success: true, message: 'System disconnected' };
        } catch (err) {
            console.error('Disconnect error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // ==================== LIVE VIEWING / REMOTE VIEW ====================
    
    // Start viewing session - tells EXE to start streaming
    async start_viewing(body) {
        try {
            const { system_id, viewer_id } = body;
            
            if (!system_id) {
                return { success: false, error: 'System ID is required' };
            }
            
            // Create or update live session
            await queryDB(
                `INSERT INTO live_sessions (system_id, viewer_id, is_active, started_at)
                 VALUES ($1, $2, true, NOW())
                 ON CONFLICT (system_id, viewer_id) 
                 DO UPDATE SET is_active = true, started_at = NOW()`,
                [system_id, viewer_id || 'admin']
            );
            
            return { success: true, message: 'Viewing session started' };
        } catch (err) {
            // If table doesn't exist, create it
            if (err.message.includes('does not exist')) {
                try {
                    await queryDB(`
                        CREATE TABLE IF NOT EXISTS live_sessions (
                            id SERIAL PRIMARY KEY,
                            system_id VARCHAR(100) NOT NULL,
                            viewer_id VARCHAR(100) DEFAULT 'admin',
                            is_active BOOLEAN DEFAULT TRUE,
                            started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            UNIQUE(system_id, viewer_id)
                        )
                    `);
                    // Retry insert
                    await queryDB(
                        `INSERT INTO live_sessions (system_id, viewer_id, is_active, started_at)
                         VALUES ($1, $2, true, NOW())
                         ON CONFLICT (system_id, viewer_id) 
                         DO UPDATE SET is_active = true, started_at = NOW()`,
                        [system_id, viewer_id || 'admin']
                    );
                    return { success: true, message: 'Viewing session started' };
                } catch (e) {
                    return { success: false, error: e.message };
                }
            }
            console.error('Start viewing error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Stop viewing session
    async stop_viewing(body) {
        try {
            const { system_id, viewer_id } = body;
            
            if (!system_id) {
                return { success: false, error: 'System ID is required' };
            }
            
            await queryDB(
                `UPDATE live_sessions SET is_active = false WHERE system_id = $1 AND viewer_id = $2`,
                [system_id, viewer_id || 'admin']
            );
            
            return { success: true, message: 'Viewing session stopped' };
        } catch (err) {
            console.error('Stop viewing error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get latest frame from a system
    async get_live_frame(query) {
        try {
            const systemId = query.system_id;
            
            if (!systemId) {
                return { success: false, error: 'System ID is required' };
            }
            
            // Get the most recent frame
            const rows = await queryDB(
                `SELECT frame_data, screen_width, screen_height, capture_time
                 FROM live_stream_frames 
                 WHERE system_id = $1 
                 ORDER BY capture_time DESC 
                 LIMIT 1`,
                [systemId]
            );
            
            if (rows.length > 0) {
                return { 
                    success: true, 
                    data: {
                        frame: rows[0].frame_data,
                        width: rows[0].screen_width,
                        height: rows[0].screen_height,
                        captureTime: rows[0].capture_time
                    }
                };
            }
            
            return { success: false, error: 'No frame available', waiting: true };
        } catch (err) {
            console.error('Get live frame error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get system info by machine_id or system_id
    async get_system_info(query) {
        try {
            const machineId = query.machine_id;
            
            if (!machineId) {
                return { success: false, error: 'Machine ID is required' };
            }
            
            // Try to find by machine_id first, then by system_id
            let rows = await queryDB(
                `SELECT * FROM connected_systems WHERE machine_id = $1 LIMIT 1`,
                [machineId]
            );
            
            // If not found, try system_id
            if (rows.length === 0) {
                rows = await queryDB(
                    `SELECT * FROM connected_systems WHERE system_id = $1 LIMIT 1`,
                    [machineId]
                );
            }
            
            if (rows.length > 0) {
                return { success: true, data: rows[0] };
            }
            
            return { success: false, error: 'System not found' };
        } catch (err) {
            console.error('Get system info error:', err.message);
            return { success: false, error: err.message };
        }
    }
};

// Parse URL query string
function parseQuery(url) {
    const queryString = url.split('?')[1] || '';
    const query = {};
    queryString.split('&').forEach(pair => {
        const [key, value] = pair.split('=');
        if (key) {
            // Replace + with space before decoding (browsers encode spaces as +)
            const decodedKey = decodeURIComponent(key.replace(/\+/g, ' '));
            const decodedValue = decodeURIComponent((value || '').replace(/\+/g, ' '));
            query[decodedKey] = decodedValue;
        }
    });
    return query;
}

// Parse JSON body
function parseBody(req) {
    return new Promise((resolve, reject) => {
        // For GET requests, there's typically no body
        if (req.method === 'GET') {
            resolve({});
            return;
        }
        
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            try {
                resolve(body ? JSON.parse(body) : {});
            } catch (e) {
                resolve({});
            }
        });
        req.on('error', reject);
    });
}

// Serve static files
function serveStatic(res, filePath) {
    const ext = path.extname(filePath).toLowerCase();
    const contentType = mimeTypes[ext] || 'application/octet-stream';
    
    fs.readFile(filePath, (err, data) => {
        if (err) {
            res.writeHead(404, { 'Content-Type': 'text/plain' });
            res.end('File not found');
            return;
        }
        res.writeHead(200, { 'Content-Type': contentType });
        res.end(data);
    });
}

// Main server
const server = http.createServer(async (req, res) => {
    // Enable CORS
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    
    if (req.method === 'OPTIONS') {
        res.writeHead(204);
        res.end();
        return;
    }

    const url = req.url.split('?')[0];
    
    // API endpoints
    if (url === '/api.php' || url.startsWith('/api.php?')) {
        const query = parseQuery(req.url);
        const body = await parseBody(req);
        const action = query.action || body.action;
        
        // Log API request with client IP for heartbeats
        const clientIP = req.socket.remoteAddress;
        if (action === 'system_heartbeat') {
            console.log(`API Request: ${action} from ${clientIP} (${body.employee_id || 'unknown'})`);
        } else {
            console.log(`API Request: ${action}`);
        }
        
        try {
            if (apiHandlers[action]) {
                const mergedParams = { ...query, ...body };
                const result = await apiHandlers[action](mergedParams);
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify(result));
            } else {
                res.writeHead(400, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ success: false, error: 'Unknown action: ' + action }));
            }
        } catch (err) {
            console.error('API Error:', err);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: false, error: 'Server error: ' + err.message }));
        }
        return;
    }
    
    // Static files
    let filePath = path.join(__dirname, url === '/' ? 'index.html' : url);
    serveStatic(res, filePath);
});

// Listen on all network interfaces (0.0.0.0) so other machines can connect
server.listen(PORT, '0.0.0.0', () => {
    console.log(`\n========================================`);
    console.log(`  DTA Web Dashboard Server`);
    console.log(`  Running on: http://0.0.0.0:${PORT}`);
    console.log(`  Local: http://localhost:${PORT}`);
    console.log(`  Network: http://192.168.1.5:${PORT}`);
    console.log(`  Database: ${dbConfig.host}:${dbConfig.port}/${dbConfig.database}`);
    console.log(`========================================\n`);
    console.log('Authentication Flow:');
    console.log('  1. verify_key -> activation_keys table');
    console.log('  2. login -> company_users table');
    console.log('');
    console.log('Data Tables:');
    console.log('  - punch_logs (attendance records)');
    console.log('');
});
