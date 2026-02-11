const http = require('http');
const fs = require('fs');
const path = require('path');
const { Client } = require('pg');

const PORT = 8888;

// PostgreSQL connection config (same as Desktop Controller)
const dbConfig = {
    host: '72.61.235.203',
    port: 9095,
    database: 'controller',
    user: 'controller_dbuser',
    password: 'hwrw*&^hdg2gsGDGJHAU&838373h'
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

// Initialize database tables
async function initializeDatabaseTables() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        // Create system_info table if it doesn't exist
        await client.query(`
            CREATE TABLE IF NOT EXISTS system_info (
                id SERIAL PRIMARY KEY,
                activation_key VARCHAR(100),
                company_name VARCHAR(255),
                system_name VARCHAR(255),
                user_name VARCHAR(255),
                tracking_id VARCHAR(50),
                os_version VARCHAR(255),
                processor_info VARCHAR(255),
                total_memory VARCHAR(100),
                installed_apps TEXT,
                system_serial_number VARCHAR(255),
                captured_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(company_name, system_name, user_name, captured_at)
            );
        `);
        
        // Add tracking_id column if it doesn't exist
        try {
            await client.query(`
                ALTER TABLE system_info ADD COLUMN tracking_id VARCHAR(50)
            `);
        } catch (err) {
            // Column might already exist, ignore error
        }
        
        // Create indexes
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_system_info_company ON system_info(company_name);
            CREATE INDEX IF NOT EXISTS idx_system_info_user ON system_info(user_name);
            CREATE INDEX IF NOT EXISTS idx_system_info_tracking ON system_info(tracking_id);
        `);
        
        // Create system_tracking table for generating and storing tracking IDs
        await client.query(`
            CREATE TABLE IF NOT EXISTS system_tracking (
                id SERIAL PRIMARY KEY,
                activation_key VARCHAR(100) NOT NULL,
                user_name VARCHAR(255) NOT NULL,
                system_name VARCHAR(255),
                tracking_id VARCHAR(50) UNIQUE NOT NULL,
                company_name VARCHAR(255),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN DEFAULT TRUE,
                UNIQUE(activation_key, user_name, system_name)
            );
        `);
        
        // Create system_tracking indexes
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_system_tracking_key ON system_tracking(activation_key);
            CREATE INDEX IF NOT EXISTS idx_system_tracking_user ON system_tracking(user_name);
            CREATE INDEX IF NOT EXISTS idx_system_tracking_id ON system_tracking(tracking_id);
        `);

        // Create USB file transfer logs table
        await client.query(`
            CREATE TABLE IF NOT EXISTS usb_file_transfer_logs (
                id SERIAL PRIMARY KEY,
                activation_key VARCHAR(100),
                company_name VARCHAR(255),
                system_name VARCHAR(255),
                ip_address VARCHAR(50),
                username VARCHAR(255),
                display_user_name VARCHAR(255),
                machine_id VARCHAR(255),
                file_name VARCHAR(500),
                file_path TEXT,
                file_size BIGINT DEFAULT 0,
                file_extension VARCHAR(50),
                transfer_type VARCHAR(20) DEFAULT 'COPY',
                source_path TEXT,
                destination_path TEXT,
                drive_letter VARCHAR(10),
                drive_label VARCHAR(255),
                transfer_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        `);

        // Create USB file transfer indexes
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_usb_transfer_company ON usb_file_transfer_logs(company_name);
            CREATE INDEX IF NOT EXISTS idx_usb_transfer_user ON usb_file_transfer_logs(username);
            CREATE INDEX IF NOT EXISTS idx_usb_transfer_time ON usb_file_transfer_logs(transfer_time);
            CREATE INDEX IF NOT EXISTS idx_usb_transfer_type ON usb_file_transfer_logs(transfer_type);
        `);

        // Create blocked_applications table
        await client.query(`
            CREATE TABLE IF NOT EXISTS blocked_applications (
                id SERIAL PRIMARY KEY,
                company_name VARCHAR(255),
                system_name VARCHAR(255),
                app_name VARCHAR(255),
                app_path TEXT,
                exe_name VARCHAR(255),
                blocked_by VARCHAR(255) DEFAULT 'admin',
                blocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                is_blocked BOOLEAN DEFAULT TRUE,
                UNIQUE(company_name, system_name, app_name)
            );
        `);

        // Create system_restrictions table
        await client.query(`
            CREATE TABLE IF NOT EXISTS system_restrictions (
                id SERIAL PRIMARY KEY,
                company_name VARCHAR(255),
                system_name VARCHAR(255),
                restriction_type VARCHAR(100),
                is_active BOOLEAN DEFAULT FALSE,
                changed_by VARCHAR(255) DEFAULT 'admin',
                changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(company_name, system_name, restriction_type)
            );
        `);

        // Create indexes for new tables
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_blocked_apps_company ON blocked_applications(company_name);
            CREATE INDEX IF NOT EXISTS idx_blocked_apps_system ON blocked_applications(company_name, system_name);
            CREATE INDEX IF NOT EXISTS idx_restrictions_company ON system_restrictions(company_name);
            CREATE INDEX IF NOT EXISTS idx_restrictions_system ON system_restrictions(company_name, system_name);
        `);

        // Create company_admin_users table for separate admin login
        await client.query(`
            CREATE TABLE IF NOT EXISTS company_admin_users (
                id SERIAL PRIMARY KEY,
                company_name VARCHAR(255) NOT NULL,
                username VARCHAR(100) UNIQUE NOT NULL,
                password VARCHAR(255) NOT NULL,
                full_name VARCHAR(255),
                email VARCHAR(255),
                phone VARCHAR(50),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_login TIMESTAMP,
                is_active BOOLEAN DEFAULT TRUE,
                created_by VARCHAR(100),
                UNIQUE(company_name, username)
            );
        `);

        // Create indexes for admin users
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_admin_users_company ON company_admin_users(company_name);
            CREATE INDEX IF NOT EXISTS idx_admin_users_username ON company_admin_users(username);
        `);

        // Create wizone_meetings table for AI meeting system
        await client.query(`
            CREATE TABLE IF NOT EXISTS wizone_meetings (
                id SERIAL PRIMARY KEY,
                meeting_id VARCHAR(100) UNIQUE NOT NULL,
                company_name VARCHAR(255) NOT NULL,
                meeting_name VARCHAR(255),
                meeting_description TEXT,
                created_by VARCHAR(255) NOT NULL,
                created_by_name VARCHAR(255),
                start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                end_time TIMESTAMP,
                status VARCHAR(50) DEFAULT 'active',
                meeting_link TEXT,
                max_participants INT DEFAULT 100,
                is_recording_enabled BOOLEAN DEFAULT TRUE,
                recording_status VARCHAR(50) DEFAULT 'not_started',
                recording_file_path TEXT,
                recording_size_mb DECIMAL(10,2),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        `);

        // Create meeting_participants table
        await client.query(`
            CREATE TABLE IF NOT EXISTS meeting_participants (
                id SERIAL PRIMARY KEY,
                meeting_id VARCHAR(100) NOT NULL,
                company_name VARCHAR(255) NOT NULL,
                participant_username VARCHAR(255) NOT NULL,
                participant_name VARCHAR(255),
                joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                left_at TIMESTAMP,
                is_online BOOLEAN DEFAULT TRUE,
                connection_quality VARCHAR(50),
                UNIQUE(meeting_id, participant_username)
            );
        `);

        // Create meeting_recordings table
        await client.query(`
            CREATE TABLE IF NOT EXISTS meeting_recordings (
                id SERIAL PRIMARY KEY,
                meeting_id VARCHAR(100) NOT NULL,
                company_name VARCHAR(255) NOT NULL,
                recording_name VARCHAR(255),
                recording_file_path TEXT,
                recording_size_mb DECIMAL(10,2),
                recording_duration_minutes INT,
                started_at TIMESTAMP,
                ended_at TIMESTAMP,
                uploaded_by VARCHAR(255),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                is_available BOOLEAN DEFAULT TRUE
            );
        `);

        // Create indexes for meetings
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_meetings_company ON wizone_meetings(company_name);
            CREATE INDEX IF NOT EXISTS idx_meetings_id ON wizone_meetings(meeting_id);
            CREATE INDEX IF NOT EXISTS idx_meetings_created_by ON wizone_meetings(created_by);
            CREATE INDEX IF NOT EXISTS idx_participants_meeting ON meeting_participants(meeting_id);
            CREATE INDEX IF NOT EXISTS idx_recordings_meeting ON meeting_recordings(meeting_id);
        `);

        console.log('✓ Database tables initialized successfully');
    } catch (err) {
        console.error('Database initialization error:', err.message);
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
            // First, try to get user from company_users table
            const userRows = await queryDB(
                `SELECT cu.id, cu.username, cu.full_name, cu.email, cu.role_id, cu.company_name,
                        cu.is_active, cu.activation_key_id, ak.activation_key,
                        COALESCE(cr.role_name, 'Administrator') as role_name
                 FROM company_users cu
                 LEFT JOIN activation_keys ak ON cu.activation_key_id = ak.id
                 LEFT JOIN company_roles cr ON cu.role_id = cr.id
                 WHERE cu.username = $1 AND cu.password = $2 AND cu.is_active = TRUE
                 LIMIT 1`,
                [username, password]
            );

            if (userRows.length > 0) {
                const user = userRows[0];
                const roleName = (user.role_name || '').toLowerCase();
                return {
                    success: true,
                    user: {
                        id: user.id,
                        username: user.username,
                        full_name: user.full_name || user.username,
                        email: user.email,
                        role: roleName === 'administrator' ? 'admin' : 'user',
                        company_name: user.company_name,
                        activation_key: user.activation_key
                    }
                };
            }

            // If not found in company_users, try company_admin_users table
            const adminRows = await queryDB(
                `SELECT id, company_name, username, full_name, email, phone
                 FROM company_admin_users
                 WHERE username = $1 AND password = $2 AND is_active = TRUE
                 LIMIT 1`,
                [username, password]
            );

            if (adminRows.length > 0) {
                const user = adminRows[0];

                // Update last login time
                await queryDB(
                    'UPDATE company_admin_users SET last_login = NOW() WHERE id = $1',
                    [user.id]
                );

                return {
                    success: true,
                    user: {
                        id: user.id,
                        username: user.username,
                        full_name: user.full_name || user.username,
                        email: user.email || '',
                        phone: user.phone || '',
                        role: 'admin',
                        company_name: user.company_name
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
                        cu.is_active, cu.activation_key_id, ak.activation_key,
                        COALESCE(cr.role_name, 'Administrator') as role_name
                 FROM company_users cu
                 LEFT JOIN activation_keys ak ON cu.activation_key_id = ak.id
                 LEFT JOIN company_roles cr ON cu.role_id = cr.id
                 WHERE cu.username = $1 AND cu.password = $2 AND cu.is_active = TRUE
                   AND (cr.role_name = 'Administrator' OR cu.role_id IS NULL)
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

            // Auto-generate employee_id if not provided (format: WIZxxxx-001)
            let empId = employee_id || '';
            if (!empId) {
                const randomNum = Math.floor(1000 + Math.random() * 9000);
                empId = 'WIZ' + randomNum + '-001';
                // Check for duplicates and regenerate if needed
                const existing = await queryDB(
                    'SELECT employee_id FROM company_employees WHERE company_name = $1 AND employee_id = $2',
                    [company_name, empId]
                );
                if (existing.length > 0) {
                    empId = 'WIZ' + Math.floor(1000 + Math.random() * 9000) + '-001';
                }
            }

            const result = await queryDB(
                `INSERT INTO company_employees (company_name, employee_id, full_name, email, phone, department, designation, lunch_duration, significant_idle_threshold_minutes, is_active, created_at, updated_at)
                 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, true, NOW(), NOW())
                 RETURNING id, employee_id`,
                [company_name, empId, full_name, email || '', phone || '', department || '', designation || '', lunch_duration || 60, significant_idle_threshold_minutes || 10]
            );

            return { success: true, message: 'Employee added successfully', data: { id: result[0]?.id, employee_id: result[0]?.employee_id } };
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
                        COALESCE(cs.system_id, '') as system_id,
                        COALESCE(cs.cpu_usage, 0) as cpu_usage,
                        COALESCE(cs.ram_usage, 0) as ram_usage
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
                        COALESCE(cs.system_id, '') as system_id,
                        COALESCE(cs.cpu_usage, 0) as cpu_usage,
                        COALESCE(cs.ram_usage, 0) as ram_usage
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
    },

    // ===== WEBSITE MONITORING HANDLERS =====
    
    // Add a new monitored website
    async add_monitored_site(body) {
        const { site_name, site_url, company_name, check_interval } = body;
        
        if (!site_name || !site_url || !company_name) {
            return { success: false, error: 'Site name, URL, and company name are required' };
        }
        
        try {
            // First, create table if it doesn't exist
            await queryDB(`
                CREATE TABLE IF NOT EXISTS monitored_websites (
                    id SERIAL PRIMARY KEY,
                    site_name VARCHAR(255) NOT NULL,
                    site_url VARCHAR(500) NOT NULL,
                    company_name VARCHAR(255) NOT NULL,
                    check_interval INTEGER DEFAULT 30,
                    current_status VARCHAR(50) DEFAULT 'unknown',
                    uptime DECIMAL(5,2) DEFAULT 100,
                    response_time INTEGER DEFAULT 0,
                    total_traffic INTEGER DEFAULT 0,
                    last_checked TIMESTAMP,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            `);
            
            // Insert the new site
            const result = await queryDB(
                `INSERT INTO monitored_websites (site_name, site_url, company_name, check_interval, last_checked)
                 VALUES ($1, $2, $3, $4, CURRENT_TIMESTAMP)
                 RETURNING id, site_name, site_url, company_name, check_interval, current_status, created_at`,
                [site_name, site_url, company_name, check_interval || 30]
            );
            
            if (result.length > 0) {
                return { success: true, data: result[0], message: 'Site added successfully' };
            }
            
            return { success: false, error: 'Failed to add site' };
        } catch (err) {
            console.error('Add monitored site error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get monitored websites by company
    async get_monitored_sites(body) {
        const { company_name } = body;
        
        if (!company_name) {
            return { success: false, error: 'Company name is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT * FROM monitored_websites WHERE company_name = $1 ORDER BY created_at DESC`,
                [company_name]
            );
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get monitored sites error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Update monitored site
    async update_monitored_site(body) {
        const { site_id, site_name, site_url, check_interval } = body;
        
        if (!site_id || !site_name || !site_url) {
            return { success: false, error: 'Site ID, name, and URL are required' };
        }
        
        try {
            const result = await queryDB(
                `UPDATE monitored_websites 
                 SET site_name = $1, site_url = $2, check_interval = $3, updated_at = CURRENT_TIMESTAMP
                 WHERE id = $4
                 RETURNING id, site_name, site_url, company_name, check_interval, current_status, updated_at`,
                [site_name, site_url, check_interval || 30, site_id]
            );
            
            if (result.length > 0) {
                return { success: true, data: result[0], message: 'Site updated successfully' };
            }
            
            return { success: false, error: 'Site not found' };
        } catch (err) {
            console.error('Update monitored site error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Delete monitored site
    async delete_monitored_site(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            // Delete associated downtime records
            await queryDB(
                `DELETE FROM website_downtime WHERE site_id = $1`,
                [site_id]
            );
            
            // Delete associated traffic records
            await queryDB(
                `DELETE FROM website_traffic WHERE site_id = $1`,
                [site_id]
            );
            
            // Delete associated analytics
            await queryDB(
                `DELETE FROM website_analytics WHERE site_id = $1`,
                [site_id]
            );
            
            // Delete associated pages
            await queryDB(
                `DELETE FROM website_pages WHERE site_id = $1`,
                [site_id]
            );
            
            // Delete the site itself
            const result = await queryDB(
                `DELETE FROM monitored_websites WHERE id = $1 RETURNING id, site_name`,
                [site_id]
            );
            
            if (result.length > 0) {
                return { success: true, data: result[0], message: 'Site deleted successfully' };
            }
            
            return { success: false, error: 'Site not found' };
        } catch (err) {
            console.error('Delete monitored site error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Delete monitored website
    async delete_monitored_site(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            // Delete downtime records first
            await queryDB(
                `DELETE FROM website_downtime WHERE site_id = $1`,
                [site_id]
            );
            
            // Delete the site
            const result = await queryDB(
                `DELETE FROM monitored_websites WHERE id = $1 RETURNING id`,
                [site_id]
            );
            
            if (result.length > 0) {
                return { success: true, message: 'Site deleted successfully' };
            }
            
            return { success: false, error: 'Site not found' };
        } catch (err) {
            console.error('Delete monitored site error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Record website downtime
    async record_downtime(body) {
        const { site_id, company_name, status } = body;
        
        if (!site_id || !company_name) {
            return { success: false, error: 'Site ID and company name are required' };
        }
        
        try {
            // Create downtime table if doesn't exist
            await queryDB(`
                CREATE TABLE IF NOT EXISTS website_downtime (
                    id SERIAL PRIMARY KEY,
                    site_id INTEGER NOT NULL REFERENCES monitored_websites(id),
                    down_start TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    down_end TIMESTAMP,
                    duration_minutes INTEGER,
                    status VARCHAR(50),
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS website_traffic (
                    id SERIAL PRIMARY KEY,
                    site_id INTEGER NOT NULL REFERENCES monitored_websites(id),
                    created_date DATE DEFAULT CURRENT_DATE,
                    page_views INTEGER DEFAULT 0,
                    response_time INTEGER DEFAULT 0,
                    traffic_count INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS website_analytics (
                    id SERIAL PRIMARY KEY,
                    site_id INTEGER NOT NULL REFERENCES monitored_websites(id),
                    metric_type VARCHAR(50),
                    source_type VARCHAR(100),
                    device_type VARCHAR(50),
                    country_code VARCHAR(10),
                    country_name VARCHAR(100),
                    count INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE TABLE IF NOT EXISTS website_pages (
                    id SERIAL PRIMARY KEY,
                    site_id INTEGER NOT NULL REFERENCES monitored_websites(id),
                    page_path VARCHAR(500),
                    view_count INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            `);
            
            if (status === 'down') {
                // Record downtime start
                const result = await queryDB(
                    `INSERT INTO website_downtime (site_id, down_start, status)
                     VALUES ($1, CURRENT_TIMESTAMP, 'down')
                     RETURNING id, down_start`,
                    [site_id]
                );
                
                // Update site status
                await queryDB(
                    `UPDATE monitored_websites SET current_status = 'down', updated_at = CURRENT_TIMESTAMP WHERE id = $1`,
                    [site_id]
                );
                
                return { success: true, data: result[0], message: 'Downtime recorded' };
            } else if (status === 'up') {
                // Find the latest open downtime record
                const downtimeRecords = await queryDB(
                    `SELECT id, down_start FROM website_downtime WHERE site_id = $1 AND down_end IS NULL ORDER BY down_start DESC LIMIT 1`,
                    [site_id]
                );
                
                if (downtimeRecords.length > 0) {
                    // Update downtime record with end time and calculate duration using PostgreSQL
                    await queryDB(
                        `UPDATE website_downtime 
                         SET down_end = CURRENT_TIMESTAMP, 
                             duration_minutes = EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - down_start)) / 60,
                             status = 'recovered'
                         WHERE id = $1`,
                        [downtimeRecords[0].id]
                    );
                }
                
                // Update site status
                await queryDB(
                    `UPDATE monitored_websites SET current_status = 'up', updated_at = CURRENT_TIMESTAMP WHERE id = $1`,
                    [site_id]
                );
                
                return { success: true, message: 'Site recovered - downtime closed' };
            }
        } catch (err) {
            console.error('Record downtime error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get downtime history for a site
    async get_downtime_history(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT * FROM website_downtime WHERE site_id = $1 ORDER BY down_start DESC`,
                [site_id]
            );
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get downtime history error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get site metrics
    async get_site_metrics(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT 
                    COALESCE(SUM(CASE WHEN created_date = CURRENT_DATE THEN traffic_count ELSE 0 END), 0) as visitors_today,
                    COALESCE(SUM(CASE WHEN created_date = CURRENT_DATE THEN page_views ELSE 0 END), 0) as page_views_today,
                    COALESCE(ROUND(AVG(CASE WHEN created_date = CURRENT_DATE THEN response_time ELSE NULL END)::numeric, 0)::int, 0) as avg_load_time
                 FROM website_traffic 
                 WHERE site_id = $1`,
                [site_id]
            );
            
            const data = rows[0] || { visitors_today: 0, page_views_today: 0, avg_load_time: 0 };
            // If no data, return reasonable defaults
            if (data.visitors_today === 0) {
                data.visitors_today = Math.floor(Math.random() * 5000 + 1000);
                data.page_views_today = Math.floor(Math.random() * 15000 + 3000);
                data.avg_load_time = Math.floor(Math.random() * 300 + 100);
            }
            
            return { success: true, data };
        } catch (err) {
            console.error('Get site metrics error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get traffic sources
    async get_traffic_sources(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT source_type, SUM(count) as count 
                 FROM website_analytics 
                 WHERE site_id = $1 AND metric_type = 'traffic_source'
                 GROUP BY source_type
                 ORDER BY count DESC`,
                [site_id]
            );
            
            // If no data, return demo data
            if (!rows || rows.length === 0) {
                return { success: true, data: [
                    { source_type: 'Google', count: Math.floor(Math.random() * 500 + 200) },
                    { source_type: 'Social Media', count: Math.floor(Math.random() * 300 + 100) },
                    { source_type: 'Direct', count: Math.floor(Math.random() * 400 + 150) },
                    { source_type: 'Other', count: Math.floor(Math.random() * 200 + 50) }
                ]};
            }
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get traffic sources error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get device distribution
    async get_device_distribution(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT device_type, SUM(count) as count 
                 FROM website_analytics 
                 WHERE site_id = $1 AND metric_type = 'device_type'
                 GROUP BY device_type
                 ORDER BY count DESC`,
                [site_id]
            );
            
            // If no data, return demo data
            if (!rows || rows.length === 0) {
                return { success: true, data: [
                    { device_type: 'Desktop', count: Math.floor(Math.random() * 600 + 400) },
                    { device_type: 'Mobile', count: Math.floor(Math.random() * 800 + 600) },
                    { device_type: 'Tablet', count: Math.floor(Math.random() * 200 + 50) }
                ]};
            }
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get device distribution error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get top countries
    async get_top_countries(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT country_code, country_name, SUM(count) as count 
                 FROM website_analytics 
                 WHERE site_id = $1 AND metric_type = 'country'
                 GROUP BY country_code, country_name
                 ORDER BY count DESC
                 LIMIT 10`,
                [site_id]
            );
            
            // If no data, return demo data
            if (!rows || rows.length === 0) {
                return { success: true, data: [
                    { country_code: 'IN', country_name: 'India', count: Math.floor(Math.random() * 2000 + 1000) },
                    { country_code: 'US', country_name: 'United States', count: Math.floor(Math.random() * 1500 + 800) },
                    { country_code: 'GB', country_name: 'United Kingdom', count: Math.floor(Math.random() * 800 + 300) }
                ]};
            }
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get top countries error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get popular pages
    async get_popular_pages(body) {
        const { site_id } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const rows = await queryDB(
                `SELECT page_path, SUM(view_count) as view_count 
                 FROM website_pages 
                 WHERE site_id = $1
                 GROUP BY page_path
                 ORDER BY view_count DESC
                 LIMIT 10`,
                [site_id]
            );
            
            // If no data, return demo data
            if (!rows || rows.length === 0) {
                return { success: true, data: [
                    { page_path: '/home', view_count: Math.floor(Math.random() * 5000 + 2000) },
                    { page_path: '/about', view_count: Math.floor(Math.random() * 3000 + 1000) },
                    { page_path: '/contact', view_count: Math.floor(Math.random() * 2000 + 500) }
                ]};
            }
            
            return { success: true, data: rows };
        } catch (err) {
            console.error('Get popular pages error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Record downtime
    async record_downtime(body) {
        const { site_id, company_name } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            // Check if there's already an open downtime record for this site
            const existing = await queryDB(
                `SELECT id FROM website_downtime WHERE site_id = $1 AND down_end IS NULL LIMIT 1`,
                [site_id]
            );
            
            if (existing.length === 0) {
                // Create new downtime record
                const result = await queryDB(
                    `INSERT INTO website_downtime (site_id, down_start, status)
                     VALUES ($1, CURRENT_TIMESTAMP, 'down')
                     RETURNING id`,
                    [site_id]
                );
                return { success: true, data: result[0] };
            }
            
            return { success: true, message: 'Downtime already recorded' };
        } catch (err) {
            console.error('Record downtime error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Update site traffic
    async update_site_traffic(body) {
        const { site_id, traffic_count } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const result = await queryDB(
                `UPDATE monitored_websites 
                 SET total_traffic = total_traffic + $2, updated_at = CURRENT_TIMESTAMP
                 WHERE id = $1
                 RETURNING id, total_traffic`,
                [site_id, traffic_count || 1]
            );
            
            if (result.length > 0) {
                return { success: true, data: result[0] };
            }
            
            return { success: false, error: 'Site not found' };
        } catch (err) {
            console.error('Update site traffic error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Update site status and response time
    async update_site_status(body) {
        const { site_id, status, response_time } = body;
        
        if (!site_id) {
            return { success: false, error: 'Site ID is required' };
        }
        
        try {
            const result = await queryDB(
                `UPDATE monitored_websites 
                 SET current_status = $2, response_time = $3, last_checked = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP
                 WHERE id = $1
                 RETURNING *`,
                [site_id, status || 'unknown', response_time || 0]
            );
            
            if (result.length > 0) {
                // If status is DOWN, record downtime
                if (status === 'down') {
                    // Check if there's already an open downtime record
                    const existing = await queryDB(
                        `SELECT id FROM website_downtime WHERE site_id = $1 AND down_end IS NULL LIMIT 1`,
                        [site_id]
                    );
                    
                    if (existing.length === 0) {
                        // Create new downtime record
                        await queryDB(
                            `INSERT INTO website_downtime (site_id, down_start, status)
                             VALUES ($1, CURRENT_TIMESTAMP, 'down')`,
                            [site_id]
                        );
                    }
                }
                // If status is UP, close any open downtime records
                else if (status === 'up') {
                    await queryDB(
                        `UPDATE website_downtime 
                         SET down_end = CURRENT_TIMESTAMP, duration_minutes = EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - down_start))/60, status = 'resolved'
                         WHERE site_id = $1 AND down_end IS NULL`,
                        [site_id]
                    );
                }
                
                return { success: true, data: result[0] };
            }
            
            return { success: false, error: 'Site not found' };
        } catch (err) {
            console.error('Update site status error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // ==================== SYSTEM DATA / INSTALLED SOFTWARE ====================

    // Generate or retrieve tracking ID for a system
    async get_or_create_tracking_id(body) {
        try {
            const { activation_key, user_name, system_name, company_name } = body;
            
            if (!activation_key || !user_name || !system_name) {
                return { success: false, error: 'activation_key, user_name, and system_name are required' };
            }
            
            // Check if tracking ID already exists for this system
            const existingRows = await queryDB(
                `SELECT tracking_id FROM system_tracking 
                 WHERE activation_key = $1 AND user_name = $2 AND system_name = $3`,
                [activation_key, user_name, system_name]
            );
            
            if (existingRows.length > 0) {
                // Update last_seen and return existing tracking ID
                await queryDB(
                    `UPDATE system_tracking SET last_seen = NOW() 
                     WHERE activation_key = $1 AND user_name = $2 AND system_name = $3`,
                    [activation_key, user_name, system_name]
                );
                return { success: true, tracking_id: existingRows[0].tracking_id };
            }
            
            // Generate new tracking ID: SYSTM-XXXXXXX-XXXXXXX
            const uniqueId = Math.random().toString(36).substring(2, 8).toUpperCase() + 
                            Math.random().toString(36).substring(2, 8).toUpperCase();
            const trackingId = `SYSTM-${uniqueId}`;
            
            // Insert new tracking record
            await queryDB(
                `INSERT INTO system_tracking (activation_key, user_name, system_name, tracking_id, company_name)
                 VALUES ($1, $2, $3, $4, $5)`,
                [activation_key, user_name, system_name, trackingId, company_name || '']
            );
            
            return { success: true, tracking_id: trackingId };
        } catch (err) {
            console.error('Get or create tracking ID error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get system data by tracking ID
    async get_system_by_tracking_id(query) {
        try {
            const trackingId = query.tracking_id;
            
            if (!trackingId) {
                return { success: false, error: 'Tracking ID is required' };
            }
            
            // Get tracking info
            const trackingRows = await queryDB(
                `SELECT * FROM system_tracking WHERE tracking_id = $1`,
                [trackingId]
            );
            
            if (trackingRows.length === 0) {
                return { success: false, error: 'Tracking ID not found' };
            }
            
            const tracking = trackingRows[0];
            
            // Get latest system info for this tracking ID
            const systemRows = await queryDB(
                `SELECT * FROM system_info 
                 WHERE activation_key = $1 AND user_name = $2 AND system_name = $3
                 ORDER BY captured_at DESC LIMIT 1`,
                [tracking.activation_key, tracking.user_name, tracking.system_name]
            );
            
            if (systemRows.length > 0) {
                const sysInfo = systemRows[0];
                // Parse installed_apps if needed
                if (typeof sysInfo.installed_apps === 'string') {
                    try {
                        sysInfo.installed_apps = JSON.parse(sysInfo.installed_apps);
                    } catch (e) {
                        sysInfo.installed_apps = [];
                    }
                }
                
                return { 
                    success: true, 
                    data: {
                        ...sysInfo,
                        tracking_id: trackingId,
                        last_seen: tracking.last_seen
                    }
                };
            }
            
            return { success: false, error: 'No system info found for this tracking ID' };
        } catch (err) {
            console.error('Get system by tracking ID error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get system data for all systems or specific user
    async get_system_data(query) {

        try {
            const companyName = query.company_name;
            const userName = query.user_name;

            if (!companyName) {
                return { success: false, error: 'Company name is required' };
            }

            let sql = `
                SELECT
                    si.id, si.activation_key, si.company_name, si.system_name, si.user_name,
                    si.os_version, si.processor_info, si.total_memory, si.installed_apps,
                    si.system_serial_number, si.tracking_id, si.captured_at,
                    COALESCE(ce.full_name, si.user_name) as display_name,
                    COALESCE(cs.ip_address, '-') as ip_address
                FROM system_info si
                LEFT JOIN company_employees ce ON ce.employee_id = si.user_name AND ce.company_name = si.company_name
                LEFT JOIN connected_systems cs ON cs.employee_id = si.user_name AND cs.company_name = si.company_name
                WHERE si.company_name = $1
            `;
            let params = [companyName];

            // Filter by specific user if provided
            if (userName) {
                sql += ` AND si.user_name = $2`;
                params.push(userName);
            }

            sql += ` ORDER BY si.captured_at DESC LIMIT 100`;

            const rows = await queryDB(sql, params);

            return { success: true, data: rows };
        } catch (err) {
            console.error('Get system data error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get detailed system info (HDD/SSD, BIOS, Motherboard, GPU, CPU, RAM, OS, etc.)
    async get_system_data_detailed(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) return { success: false, error: 'Company name required' };

            let sql = `SELECT * FROM system_info_detailed WHERE company_name = $1`;
            let params = [companyName];
            let idx = 2;

            if (query.user_name) {
                sql += ` AND user_name = $${idx}`;
                params.push(query.user_name);
                idx++;
            }

            sql += ` ORDER BY last_updated DESC LIMIT 100`;
            const rows = await queryDB(sql, params);

            // Parse JSON fields
            rows.forEach(row => {
                try { row.storage_info = JSON.parse(row.storage_info || '[]'); } catch(e) { row.storage_info = []; }
                try { row.network_info = JSON.parse(row.network_info || '[]'); } catch(e) { row.network_info = []; }
                try { row.memory_details = JSON.parse(row.memory_details || '[]'); } catch(e) { row.memory_details = []; }
                try { row.gpu_info = JSON.parse(row.gpu_info || '[]'); } catch(e) { row.gpu_info = []; }
                try { row.raw_data = JSON.parse(row.raw_data || '{}'); } catch(e) { row.raw_data = {}; }
            });

            return { success: true, data: rows };
        } catch (err) {
            console.error('Get system data detailed error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // Get system details for a specific system
    async get_system_details(query) {
        try {
            const systemName = query.system_name;
            const companyName = query.company_name;
            
            if (!systemName || !companyName) {
                return { success: false, error: 'System name and company name are required' };
            }
            
            const rows = await queryDB(
                `SELECT * FROM system_info 
                 WHERE system_name = $1 AND company_name = $2 
                 ORDER BY captured_at DESC 
                 LIMIT 1`,
                [systemName, companyName]
            );
            
            if (rows.length > 0) {
                const record = rows[0];
                // Parse installed_apps JSON if it's a string
                if (typeof record.installed_apps === 'string') {
                    try {
                        record.installed_apps = JSON.parse(record.installed_apps);
                    } catch (e) {
                        record.installed_apps = [];
                    }
                }
                return { success: true, data: record };
            }
            
            return { success: false, error: 'System not found' };
        } catch (err) {
            console.error('Get system details error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get installed applications for a system
    async get_installed_applications(query) {
        try {
            const systemName = query.system_name;
            const companyName = query.company_name;
            
            if (!systemName || !companyName) {
                return { success: false, error: 'System name and company name are required' };
            }
            
            const rows = await queryDB(
                `SELECT installed_apps FROM system_info 
                 WHERE system_name = $1 AND company_name = $2 
                 ORDER BY captured_at DESC 
                 LIMIT 1`,
                [systemName, companyName]
            );
            
            if (rows.length > 0) {
                let apps = rows[0].installed_apps;
                
                // Parse JSON if it's a string
                if (typeof apps === 'string') {
                    try {
                        apps = JSON.parse(apps);
                    } catch (e) {
                        apps = [];
                    }
                }
                
                // Ensure it's an array
                if (!Array.isArray(apps)) {
                    apps = [];
                }
                
                return { success: true, data: apps, count: apps.length };
            }
            
            return { success: false, error: 'System not found', data: [] };
        } catch (err) {
            console.error('Get installed applications error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    // ===== SYSTEM CONTROL COMMANDS =====

    async send_system_command(body) {
        try {
            const { company_name, system_name, command_type, parameters, created_by } = body;
            if (!company_name || !system_name || !command_type) {
                return { success: false, error: 'Company name, system name and command type are required' };
            }
            await queryDB(`CREATE TABLE IF NOT EXISTS system_control_commands (
                id SERIAL PRIMARY KEY, company_name VARCHAR(255), system_name VARCHAR(255),
                user_name VARCHAR(255), command_type VARCHAR(100), parameters JSONB DEFAULT '{}',
                status VARCHAR(50) DEFAULT 'pending', result TEXT, created_by VARCHAR(255),
                created_at TIMESTAMP DEFAULT NOW(), executed_at TIMESTAMP
            )`);
            const rows = await queryDB(
                `INSERT INTO system_control_commands (company_name, system_name, command_type, parameters, status, created_by)
                 VALUES ($1, $2, $3, $4, 'pending', $5) RETURNING id, command_type, status, created_at`,
                [company_name, system_name, command_type, JSON.stringify(parameters || {}), created_by || 'admin']
            );
            return { success: true, data: rows[0], message: `Command '${command_type}' queued for ${system_name}` };
        } catch (err) {
            console.error('Send command error:', err.message);
            return { success: false, error: err.message };
        }
    },

    async get_command_history(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) return { success: false, error: 'Company name is required' };
            const rows = await queryDB(
                `SELECT * FROM system_control_commands WHERE company_name = $1 ORDER BY created_at DESC LIMIT 50`,
                [companyName]
            );
            return { success: true, data: rows, count: rows.length };
        } catch (err) {
            return { success: true, data: [], count: 0 };
        }
    },

    async get_pending_commands(query) {
        try {
            const { system_name, company_name } = query;
            if (!system_name) return { success: false, error: 'System name is required' };
            const rows = await queryDB(
                `SELECT * FROM system_control_commands WHERE system_name = $1 AND status = 'pending' ORDER BY created_at ASC`,
                [system_name]
            );
            return { success: true, data: rows, count: rows.length };
        } catch (err) {
            return { success: true, data: [], count: 0 };
        }
    },

    async update_command_status(body) {
        try {
            const { command_id, status, result } = body;
            if (!command_id || !status) return { success: false, error: 'Command ID and status required' };
            await queryDB(
                `UPDATE system_control_commands SET status = $1, result = $2, executed_at = NOW() WHERE id = $3`,
                [status, result || '', command_id]
            );
            return { success: true };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    // ===== INSTALLED APPS DETAILED =====

    async get_installed_apps_detailed(query) {
        try {
            const companyName = query.company_name;
            const systemName = query.system_name;
            if (!companyName) return { success: false, error: 'Company name required' };

            // Try installed_apps_detailed table first
            try {
                let sql = 'SELECT * FROM installed_apps_detailed WHERE company_name = $1';
                let params = [companyName];
                if (systemName) { sql += ' AND system_name = $2'; params.push(systemName); }
                sql += ' ORDER BY app_name ASC';
                const rows = await queryDB(sql, params);
                if (rows.length > 0) return { success: true, data: rows, count: rows.length };
            } catch(e) { console.log('installed_apps_detailed query error:', e.message); }

            // Fallback 1: Check system_info_detailed raw_data for installed apps
            try {
                let sql = `SELECT raw_data, computer_name, user_name FROM system_info_detailed WHERE company_name = $1`;
                let params = [companyName];
                if (systemName) { sql += ' AND computer_name = $2'; params.push(systemName); }
                sql += ' ORDER BY updated_at DESC LIMIT 1';
                const rows = await queryDB(sql, params);
                if (rows.length > 0 && rows[0].raw_data && rows[0].raw_data.installed_apps) {
                    const appsData = rows[0].raw_data.installed_apps;
                    if (typeof appsData === 'string') {
                        const apps = appsData.split('|').filter(a => a.trim()).map(a => {
                            const parts = a.trim().split(' - ');
                            return { app_name: parts[0] || a.trim(), app_version: parts[1] || '', system_name: rows[0].computer_name, user_name: rows[0].user_name };
                        });
                        if (apps.length > 0) return { success: true, data: apps, count: apps.length, source: 'system_info_detailed' };
                    } else if (Array.isArray(appsData)) {
                        return { success: true, data: appsData, count: appsData.length, source: 'system_info_detailed' };
                    }
                }
            } catch(e) { console.log('system_info_detailed fallback error:', e.message); }

            // Fallback 2: parse pipe-separated text from system_info
            try {
                let sql = `SELECT installed_apps, system_name, user_name FROM system_info WHERE company_name = $1`;
                let params = [companyName];
                if (systemName) { sql += ' AND system_name = $2'; params.push(systemName); }
                sql += ' ORDER BY captured_at DESC LIMIT 1';
                const rows = await queryDB(sql, params);
                if (rows.length > 0 && rows[0].installed_apps) {
                    const appsText = rows[0].installed_apps;
                    const apps = appsText.split('|').filter(a => a.trim()).map(a => {
                        const parts = a.trim().split(' - ');
                        return { app_name: parts[0] || a.trim(), app_version: parts[1] || '', system_name: rows[0].system_name, user_name: rows[0].user_name };
                    });
                    return { success: true, data: apps, count: apps.length, source: 'system_info' };
                }
            } catch(e) { console.log('system_info fallback error:', e.message); }

            return { success: true, data: [], count: 0, message: 'No installed apps data found. EXE needs to sync installed apps first.' };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    // ===== FILE & EMAIL ACTIVITY =====

    async get_file_activity_logs(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) return { success: false, error: 'Company name required' };

            await queryDB(`CREATE TABLE IF NOT EXISTS file_activity_logs (
                id SERIAL PRIMARY KEY, user_name VARCHAR(255), system_name VARCHAR(255),
                company_name VARCHAR(255), activity_type VARCHAR(100),
                file_name VARCHAR(1000), old_file_name VARCHAR(1000), file_path TEXT,
                file_size BIGINT, created_at TIMESTAMP DEFAULT NOW()
            )`);
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW()`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS old_file_name VARCHAR(1000)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS new_file_name VARCHAR(1000)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS file_size BIGINT`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_sender VARCHAR(500)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_recipient VARCHAR(500)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_subject VARCHAR(1000)`); } catch(e) {}

            let sql = `SELECT f.*,
                       COALESCE(ce.full_name, f.user_name) as display_user_name,
                       COALESCE(cs.ip_address, '-') as ip_address
                       FROM file_activity_logs f
                       LEFT JOIN company_employees ce ON ce.employee_id = f.user_name AND ce.company_name = f.company_name
                       LEFT JOIN connected_systems cs ON cs.employee_id = f.user_name AND cs.company_name = f.company_name
                       WHERE f.company_name = $1`;
            let params = [companyName];
            let idx = 2;

            if (query.employee_id) { sql += ` AND f.user_name = $${idx}`; params.push(query.employee_id); idx++; }
            if (query.start_date) { sql += ` AND f.created_at >= $${idx}::date`; params.push(query.start_date); idx++; }
            if (query.end_date) { sql += ` AND f.created_at <= ($${idx}::date + interval '1 day')`; params.push(query.end_date); idx++; }

            sql += ' ORDER BY f.created_at DESC LIMIT 500';
            const rows = await queryDB(sql, params);
            return { success: true, data: rows, count: rows.length };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    async save_file_activity(body) {
        try {
            const { user_name, system_name, company_name, activity_type, file_name, old_file_name, new_file_name, file_path, file_size, email_sender, email_recipient, email_subject } = body;
            await queryDB(`CREATE TABLE IF NOT EXISTS file_activity_logs (
                id SERIAL PRIMARY KEY, user_name VARCHAR(255), system_name VARCHAR(255),
                company_name VARCHAR(255), activity_type VARCHAR(100),
                file_name VARCHAR(1000), old_file_name VARCHAR(1000), new_file_name VARCHAR(1000), file_path TEXT,
                file_size BIGINT, email_sender VARCHAR(500), email_recipient VARCHAR(500), email_subject VARCHAR(1000),
                created_at TIMESTAMP DEFAULT NOW()
            )`);
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS new_file_name VARCHAR(1000)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_sender VARCHAR(500)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_recipient VARCHAR(500)`); } catch(e) {}
            try { await queryDB(`ALTER TABLE file_activity_logs ADD COLUMN IF NOT EXISTS email_subject VARCHAR(1000)`); } catch(e) {}
            await queryDB(
                `INSERT INTO file_activity_logs (user_name, system_name, company_name, activity_type, file_name, old_file_name, new_file_name, file_path, file_size, email_sender, email_recipient, email_subject)
                 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12)`,
                [user_name, system_name, company_name, activity_type, file_name, old_file_name || '', new_file_name || '', file_path || '', file_size || 0, email_sender || '', email_recipient || '', email_subject || '']
            );
            return { success: true };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    // ===== USB FILE TRANSFER LOGS =====

    async get_usb_file_logs(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) return { success: false, error: 'Company name required' };

            let sql = `SELECT u.*,
                       COALESCE(ce.full_name, u.username) as display_user_name,
                       COALESCE(cs.ip_address, u.ip_address) as display_ip_address
                       FROM usb_file_transfer_logs u
                       LEFT JOIN company_employees ce ON ce.employee_id = u.username AND ce.company_name = u.company_name
                       LEFT JOIN connected_systems cs ON cs.employee_id = u.username AND cs.company_name = u.company_name
                       WHERE u.company_name = $1`;
            let params = [companyName];
            let idx = 2;

            if (query.employee_id || query.employee) {
                const empId = query.employee_id || query.employee;
                sql += ` AND u.username = $${idx}`;
                params.push(empId);
                idx++;
            }

            if (query.start_date) {
                sql += ` AND u.transfer_time >= $${idx}::date`;
                params.push(query.start_date);
                idx++;
            }

            if (query.end_date) {
                sql += ` AND u.transfer_time <= ($${idx}::date + interval '1 day')`;
                params.push(query.end_date);
                idx++;
            }

            if (query.transfer_type) {
                sql += ` AND u.transfer_type = $${idx}`;
                params.push(query.transfer_type);
                idx++;
            }

            if (query.search) {
                sql += ` AND (u.file_name ILIKE $${idx} OR u.file_path ILIKE $${idx} OR u.drive_label ILIKE $${idx})`;
                params.push(`%${query.search}%`);
                idx++;
            }

            sql += ' ORDER BY u.transfer_time DESC LIMIT 500';
            const rows = await queryDB(sql, params);

            // Calculate summary stats
            let totalSize = 0;
            let copyCount = 0, moveCount = 0, deleteCount = 0;
            rows.forEach(r => {
                totalSize += parseInt(r.file_size) || 0;
                if (r.transfer_type === 'TO_USB') copyCount++;
                else if (r.transfer_type === 'FROM_USB') moveCount++;
                else if (r.transfer_type === 'DELETE_USB') deleteCount++;
            });

            return {
                success: true,
                data: rows,
                count: rows.length,
                summary: {
                    total_transfers: rows.length,
                    total_size: totalSize,
                    to_usb: copyCount,
                    from_usb: moveCount,
                    deleted: deleteCount
                }
            };
        } catch (err) {
            console.error('USB file logs error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    async save_usb_file_log(body) {
        try {
            const { activation_key, company_name, system_name, ip_address, username, display_user_name,
                    machine_id, file_name, file_path, file_size, file_extension, transfer_type,
                    source_path, destination_path, drive_letter, drive_label } = body;

            if (!company_name || !username) return { success: false, error: 'Company name and username required' };

            await queryDB(
                `INSERT INTO usb_file_transfer_logs (activation_key, company_name, system_name, ip_address, username, display_user_name,
                    machine_id, file_name, file_path, file_size, file_extension, transfer_type,
                    source_path, destination_path, drive_letter, drive_label, transfer_time)
                 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, NOW())`,
                [activation_key || '', company_name, system_name || '', ip_address || '', username, display_user_name || '',
                 machine_id || '', file_name || '', file_path || '', file_size || 0, file_extension || '', transfer_type || 'COPY',
                 source_path || '', destination_path || '', drive_letter || '', drive_label || '']
            );
            return { success: true, message: 'USB file transfer logged' };
        } catch (err) {
            console.error('Save USB file log error:', err.message);
            return { success: false, error: err.message };
        }
    },

    async get_usb_transfer_stats(query) {
        try {
            const companyName = query.company_name;
            if (!companyName) return { success: false, error: 'Company name required' };

            const stats = await queryDB(`
                SELECT
                    COUNT(*) as total_transfers,
                    COUNT(DISTINCT username) as unique_users,
                    COUNT(DISTINCT drive_label) as unique_devices,
                    COALESCE(SUM(file_size), 0) as total_size,
                    COUNT(CASE WHEN transfer_type = 'TO_USB' THEN 1 END) as to_usb_count,
                    COUNT(CASE WHEN transfer_type = 'FROM_USB' THEN 1 END) as from_usb_count,
                    COUNT(CASE WHEN transfer_type = 'DELETE_USB' THEN 1 END) as delete_count,
                    COUNT(CASE WHEN transfer_time >= NOW() - interval '24 hours' THEN 1 END) as last_24h
                FROM usb_file_transfer_logs
                WHERE company_name = $1
            `, [companyName]);

            return { success: true, data: stats[0] || {} };
        } catch (err) {
            return { success: false, error: err.message, data: {} };
        }
    },

    // ===== CHAT MESSAGES =====

    async send_chat_message(body) {
        try {
            const { sender, recipient, message, company_name, message_type, file_name, file_data, file_size } = body;
            if (!sender || !recipient) return { success: false, error: 'Sender and recipient required' };

            await queryDB(`CREATE TABLE IF NOT EXISTS chat_messages (
                id SERIAL PRIMARY KEY, sender VARCHAR(255) NOT NULL, recipient VARCHAR(255) NOT NULL,
                message TEXT, company_name VARCHAR(255), is_read BOOLEAN DEFAULT FALSE,
                message_type VARCHAR(50) DEFAULT 'text', file_name VARCHAR(500),
                file_data BYTEA, file_size BIGINT, created_at TIMESTAMP DEFAULT NOW()
            )`);

            let fileBuffer = null;
            if (file_data) { fileBuffer = Buffer.from(file_data, 'base64'); }

            const rows = await queryDB(
                `INSERT INTO chat_messages (sender, recipient, message, company_name, is_read, message_type, file_name, file_data, file_size)
                 VALUES ($1, $2, $3, $4, FALSE, $5, $6, $7, $8) RETURNING id, created_at`,
                [sender, recipient, message || '', company_name || '', message_type || 'text', file_name || null, fileBuffer, file_size || null]
            );
            return { success: true, data: rows[0] };
        } catch (err) {
            console.error('Send chat error:', err.message);
            return { success: false, error: err.message };
        }
    },

    async get_chat_messages(query) {
        try {
            const { sender, recipient, company_name, limit, after_id } = query;
            if (!sender || !recipient) return { success: false, error: 'Sender and recipient required' };

            let sql = `SELECT id, sender, recipient, message, company_name, is_read, message_type, file_name, file_size, created_at
                       FROM chat_messages WHERE company_name = $1
                       AND ((sender = $2 AND recipient = $3) OR (sender = $3 AND recipient = $2))`;
            let params = [company_name || '', sender, recipient];
            let idx = 4;

            if (after_id) { sql += ` AND id > $${idx}`; params.push(after_id); idx++; }
            sql += ` ORDER BY created_at ASC`;
            if (limit && !after_id) { sql += ` LIMIT $${idx}`; params.push(parseInt(limit)); }

            const rows = await queryDB(sql, params);
            return { success: true, data: rows, count: rows.length };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    async get_chat_contacts(query) {
        try {
            const { company_name, current_user } = query;
            if (!company_name) return { success: false, error: 'Company name required' };

            const rows = await queryDB(
                `SELECT DISTINCT ON (cs.employee_id)
                        cs.employee_id, cs.display_name, cs.machine_name,
                        cs.is_online, cs.last_heartbeat, cs.ip_address,
                        COALESCE(unread.cnt, 0) as unread_count,
                        last_msg.message as last_message,
                        last_msg.created_at as last_message_time
                 FROM connected_systems cs
                 LEFT JOIN LATERAL (
                    SELECT COUNT(*) as cnt FROM chat_messages
                    WHERE sender = cs.employee_id AND recipient = $2 AND is_read = FALSE AND company_name = $1
                 ) unread ON TRUE
                 LEFT JOIN LATERAL (
                    SELECT message, created_at FROM chat_messages
                    WHERE company_name = $1 AND ((sender = cs.employee_id AND recipient = $2) OR (sender = $2 AND recipient = cs.employee_id))
                    ORDER BY created_at DESC LIMIT 1
                 ) last_msg ON TRUE
                 WHERE cs.company_name = $1
                 ORDER BY cs.employee_id, cs.last_heartbeat DESC NULLS LAST`,
                [company_name, current_user || 'admin']
            );
            // Sort by last_message_time after dedup
            rows.sort((a, b) => {
                if (a.last_message_time && b.last_message_time) return new Date(b.last_message_time) - new Date(a.last_message_time);
                if (a.last_message_time) return -1;
                if (b.last_message_time) return 1;
                return (a.display_name || '').localeCompare(b.display_name || '');
            });
            return { success: true, data: rows, count: rows.length };
        } catch (err) {
            console.error('Get chat contacts error:', err.message);
            return { success: false, error: err.message, data: [] };
        }
    },

    async mark_chat_read(body) {
        try {
            const { sender, recipient, company_name } = body;
            if (!sender || !recipient) return { success: false, error: 'Sender and recipient required' };
            await queryDB(
                `UPDATE chat_messages SET is_read = TRUE WHERE sender = $1 AND recipient = $2 AND company_name = $3 AND is_read = FALSE`,
                [sender, recipient, company_name || '']
            );
            return { success: true };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async download_chat_file(query) {
        try {
            const messageId = query.message_id;
            if (!messageId) return { success: false, error: 'Message ID required' };
            const rows = await queryDB(
                `SELECT file_data, file_name, file_size FROM chat_messages WHERE id = $1`, [messageId]
            );
            if (rows.length > 0 && rows[0].file_data) {
                return { success: true, data: { file_data: rows[0].file_data.toString('base64'), file_name: rows[0].file_name, file_size: rows[0].file_size } };
            }
            return { success: false, error: 'File not found' };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    // ===== VOICE MESSAGE =====

    async send_voice_message(body) {
        try {
            const { sender, recipient, company_name, voice_data, duration } = body;
            if (!sender || !recipient || !voice_data) return { success: false, error: 'Sender, recipient and voice data required' };

            const voiceBuffer = Buffer.from(voice_data, 'base64');
            await queryDB(
                `INSERT INTO chat_messages (sender, recipient, message, company_name, message_type, file_name, file_data, file_size, created_at)
                 VALUES ($1, $2, $3, $4, 'voice', $5, $6, $7, NOW())`,
                [sender, recipient, `Voice message (${duration || '0'}s)`, company_name || '', `voice_${Date.now()}.webm`, voiceBuffer, voiceBuffer.length]
            );
            return { success: true, message: 'Voice message sent' };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    // ===== WEBRTC CALL SIGNALING =====

    async initiate_call(body) {
        try {
            const { caller, callee, company_name, call_type } = body;
            if (!caller || !callee) return { success: false, error: 'Caller and callee required' };

            // Create call_signaling table if not exists
            await queryDB(`CREATE TABLE IF NOT EXISTS call_signaling (
                id SERIAL PRIMARY KEY,
                caller VARCHAR(255) NOT NULL,
                callee VARCHAR(255) NOT NULL,
                company_name VARCHAR(255),
                call_type VARCHAR(20) DEFAULT 'audio',
                status VARCHAR(20) DEFAULT 'ringing',
                signal_data TEXT,
                ice_candidates TEXT DEFAULT '[]',
                started_at TIMESTAMP DEFAULT NOW(),
                answered_at TIMESTAMP,
                ended_at TIMESTAMP
            )`);

            const rows = await queryDB(
                `INSERT INTO call_signaling (caller, callee, company_name, call_type, status, started_at)
                 VALUES ($1, $2, $3, $4, 'ringing', NOW()) RETURNING id`,
                [caller, callee, company_name || '', call_type || 'audio']
            );
            return { success: true, call_id: rows[0]?.id };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async update_call_signal(body) {
        try {
            const { call_id, signal_data, ice_candidate, status } = body;
            if (!call_id) return { success: false, error: 'Call ID required' };

            if (signal_data) {
                await queryDB(`UPDATE call_signaling SET signal_data = $1 WHERE id = $2`, [JSON.stringify(signal_data), call_id]);
            }
            if (ice_candidate) {
                await queryDB(`UPDATE call_signaling SET ice_candidates = ice_candidates::text::jsonb || $1::jsonb WHERE id = $2`,
                    [JSON.stringify([ice_candidate]), call_id]);
            }
            if (status) {
                if (status === 'answered') {
                    await queryDB(`UPDATE call_signaling SET status = $1, answered_at = NOW() WHERE id = $2`, [status, call_id]);
                } else if (status === 'ended' || status === 'rejected' || status === 'missed') {
                    await queryDB(`UPDATE call_signaling SET status = $1, ended_at = NOW() WHERE id = $2`, [status, call_id]);
                } else {
                    await queryDB(`UPDATE call_signaling SET status = $1 WHERE id = $2`, [status, call_id]);
                }
            }
            return { success: true };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async check_incoming_call(query) {
        try {
            const { callee, company_name } = query;
            if (!callee) return { success: false, error: 'Callee required' };

            const rows = await queryDB(
                `SELECT * FROM call_signaling WHERE callee = $1 AND company_name = $2 AND status = 'ringing' AND started_at > NOW() - interval '30 seconds' ORDER BY started_at DESC LIMIT 1`,
                [callee, company_name || '']
            );
            if (rows.length > 0) {
                return { success: true, has_call: true, call: rows[0] };
            }
            return { success: true, has_call: false };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async get_call_signal(query) {
        try {
            const { call_id } = query;
            if (!call_id) return { success: false, error: 'Call ID required' };
            const rows = await queryDB(`SELECT * FROM call_signaling WHERE id = $1`, [call_id]);
            if (rows.length > 0) {
                try { rows[0].signal_data = JSON.parse(rows[0].signal_data || 'null'); } catch(e) {}
                try { rows[0].ice_candidates = JSON.parse(rows[0].ice_candidates || '[]'); } catch(e) {}
                return { success: true, data: rows[0] };
            }
            return { success: false, error: 'Call not found' };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async get_call_history(query) {
        try {
            const { company_name, user } = query;
            if (!company_name) return { success: false, error: 'Company name required' };

            let sql = `SELECT * FROM call_signaling WHERE company_name = $1`;
            let params = [company_name];
            if (user) { sql += ` AND (caller = $2 OR callee = $2)`; params.push(user); }
            sql += ` ORDER BY started_at DESC LIMIT 50`;

            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    // ==================== DANGER ZONE / SYSTEM RESTRICTIONS ====================

    async toggle_system_restriction(query) {
        try {
            const { company_name, system_name, restriction_type, is_active, changed_by } = query;
            if (!company_name || !system_name || !restriction_type) {
                return { success: false, error: 'company_name, system_name, and restriction_type required' };
            }

            const active = is_active === 'true' || is_active === '1';
            const commandType = active ? `block_${restriction_type}` : `unblock_${restriction_type}`;

            // Map restriction types to command types
            const commandMap = {
                'cmd': active ? 'block_cmd' : 'unblock_cmd',
                'powershell': active ? 'block_powershell' : 'unblock_powershell',
                'regedit': active ? 'block_regedit' : 'unblock_regedit',
                'copy_paste': active ? 'block_copy_paste' : 'unblock_copy_paste',
                'software_install': active ? 'block_software_install' : 'unblock_software_install',
                'delete': active ? 'block_delete' : 'unblock_delete',
                'ip_change': active ? 'lock_ip' : 'unlock_ip',
                'task_manager': active ? 'block_task_manager' : 'unblock_task_manager',
                'control_panel': active ? 'block_control_panel' : 'unblock_control_panel'
            };

            const cmdType = commandMap[restriction_type] || commandType;

            // Upsert restriction state
            await queryDB(`
                INSERT INTO system_restrictions (company_name, system_name, restriction_type, is_active, changed_by, changed_at)
                VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP)
                ON CONFLICT (company_name, system_name, restriction_type)
                DO UPDATE SET is_active = $4, changed_by = $5, changed_at = CURRENT_TIMESTAMP
            `, [company_name, system_name, restriction_type, active, changed_by || 'admin']);

            // Insert command into system_control_commands for the EXE to pick up
            await queryDB(`
                INSERT INTO system_control_commands (system_name, company_name, command_type, parameters, status, created_at)
                VALUES ($1, $2, $3, $4, 'pending', CURRENT_TIMESTAMP)
            `, [system_name, company_name, cmdType, JSON.stringify({})]);

            return { success: true, message: `Restriction '${restriction_type}' ${active ? 'enabled' : 'disabled'} for ${system_name}` };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async get_system_restrictions(query) {
        try {
            const { company_name, system_name } = query;
            if (!company_name) return { success: false, error: 'company_name required' };

            let sql = `SELECT * FROM system_restrictions WHERE company_name = $1`;
            let params = [company_name];

            if (system_name) {
                sql += ` AND system_name = $2`;
                params.push(system_name);
            }

            sql += ` ORDER BY restriction_type`;

            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    async toggle_full_restriction(query) {
        try {
            const { company_name, system_name, is_active, changed_by } = query;
            if (!company_name || !system_name) {
                return { success: false, error: 'company_name and system_name required' };
            }

            const active = is_active === 'true' || is_active === '1';
            const restrictions = ['cmd', 'powershell', 'regedit', 'copy_paste', 'software_install', 'delete', 'ip_change', 'task_manager', 'control_panel'];

            // Update all restrictions
            for (const r of restrictions) {
                await queryDB(`
                    INSERT INTO system_restrictions (company_name, system_name, restriction_type, is_active, changed_by, changed_at)
                    VALUES ($1, $2, $3, $4, $5, CURRENT_TIMESTAMP)
                    ON CONFLICT (company_name, system_name, restriction_type)
                    DO UPDATE SET is_active = $4, changed_by = $5, changed_at = CURRENT_TIMESTAMP
                `, [company_name, system_name, r, active, changed_by || 'admin']);
            }

            // Send the full restriction command
            const cmdType = active ? 'full_system_restriction' : 'remove_full_restriction';
            await queryDB(`
                INSERT INTO system_control_commands (system_name, company_name, command_type, parameters, status, created_at)
                VALUES ($1, $2, $3, $4, 'pending', CURRENT_TIMESTAMP)
            `, [system_name, company_name, cmdType, JSON.stringify({})]);

            return { success: true, message: `Full system restriction ${active ? 'applied' : 'removed'} for ${system_name}` };
        } catch (err) {
            return { success: false, error: err.message };
        }
    },

    async get_blocked_apps(query) {
        try {
            const { company_name, system_name } = query;
            if (!company_name) return { success: false, error: 'company_name required' };

            let sql = `SELECT * FROM blocked_applications WHERE company_name = $1`;
            let params = [company_name];

            if (system_name) {
                sql += ` AND system_name = $2`;
                params.push(system_name);
            }

            sql += ` ORDER BY blocked_at DESC`;

            const rows = await queryDB(sql, params);
            return { success: true, data: rows };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    async get_online_systems(query) {
        try {
            const { company_name } = query;
            if (!company_name) return { success: false, error: 'company_name required' };

            // Get systems that sent heartbeat in last 2 minutes (online)
            // Use machine_name as system_name (this is what EXE uses for command polling)
            const rows = await queryDB(`
                SELECT cs.machine_name as system_name, cs.ip_address, cs.last_heartbeat, cs.status,
                       cs.employee_id, cs.display_name, cs.machine_id,
                       COALESCE(cs.cpu_usage, 0) as cpu_usage, COALESCE(cs.ram_usage, 0) as ram_usage,
                       sid.os_name, sid.os_version, sid.os_build, sid.processor_name, sid.processor_cores,
                       sid.processor_logical, sid.processor_speed, sid.total_ram, sid.available_ram,
                       sid.storage_info, sid.gpu_info, sid.network_info,
                       sid.motherboard_manufacturer, sid.motherboard_product, sid.motherboard_serial,
                       sid.bios_version, sid.memory_details,
                       sid.os_install_date, sid.last_boot_time, sid.os_serial
                FROM connected_systems cs
                LEFT JOIN system_info_detailed sid ON cs.company_name = sid.company_name AND cs.machine_name = sid.system_name
                WHERE cs.company_name = $1
                ORDER BY cs.last_heartbeat DESC
            `, [company_name]);

            // Mark online/offline
            const now = new Date();
            rows.forEach(row => {
                const lastBeat = new Date(row.last_heartbeat);
                const diffMs = now - lastBeat;
                row.is_online = diffMs < 120000; // 2 minutes
                // Parse JSON fields
                try { row.storage_info = JSON.parse(row.storage_info || '[]'); } catch(e) {}
                try { row.gpu_info = JSON.parse(row.gpu_info || '[]'); } catch(e) {}
            });

            return { success: true, data: rows };
        } catch (err) {
            return { success: false, error: err.message, data: [] };
        }
    },

    // ===== ADMIN USER MANAGEMENT =====

    // Create new admin user for company
    async create_admin_user(body) {
        try {
            const { company_name, username, password, full_name, email, phone, created_by } = body;

            if (!company_name || !username || !password) {
                return { success: false, error: 'Company name, username, and password are required' };
            }

            // Check if username already exists
            const existing = await queryDB(
                'SELECT id FROM company_admin_users WHERE username = $1',
                [username]
            );

            if (existing.length > 0) {
                return { success: false, error: 'Username already exists' };
            }

            await queryDB(
                `INSERT INTO company_admin_users
                (company_name, username, password, full_name, email, phone, created_by)
                VALUES ($1, $2, $3, $4, $5, $6, $7)`,
                [company_name, username, password, full_name || '', email || '', phone || '', created_by || 'system']
            );

            return { success: true, message: 'Admin user created successfully' };
        } catch (err) {
            console.error('Create admin user error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Admin login
    async admin_user_login(body) {
        try {
            const { username, password } = body;

            if (!username || !password) {
                return { success: false, error: 'Username and password are required' };
            }

            const rows = await queryDB(
                `SELECT id, company_name, username, full_name, email, phone, last_login
                FROM company_admin_users
                WHERE username = $1 AND password = $2 AND is_active = TRUE`,
                [username, password]
            );

            if (rows.length === 0) {
                return { success: false, error: 'Invalid username or password' };
            }

            const user = rows[0];

            // Update last login time
            await queryDB(
                'UPDATE company_admin_users SET last_login = NOW() WHERE id = $1',
                [user.id]
            );

            return {
                success: true,
                user: {
                    id: user.id,
                    company_name: user.company_name,
                    username: user.username,
                    full_name: user.full_name || user.username,
                    email: user.email || '',
                    phone: user.phone || '',
                    role: 'admin'
                }
            };
        } catch (err) {
            console.error('Admin login error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get all admin users for a company
    async get_admin_users(query) {
        try {
            const { company_name } = query;

            if (!company_name) {
                return { success: false, error: 'Company name required' };
            }

            const rows = await queryDB(
                `SELECT id, company_name, username, full_name, email, phone,
                created_at, last_login, is_active, created_by
                FROM company_admin_users
                WHERE company_name = $1
                ORDER BY created_at DESC`,
                [company_name]
            );

            return { success: true, data: rows };
        } catch (err) {
            console.error('Get admin users error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Update admin user
    async update_admin_user(body) {
        try {
            const { id, full_name, email, phone, password, is_active } = body;

            if (!id) {
                return { success: false, error: 'User ID required' };
            }

            let sql = 'UPDATE company_admin_users SET ';
            let params = [];
            let idx = 1;
            let updates = [];

            if (full_name !== undefined) { updates.push(`full_name = $${idx}`); params.push(full_name); idx++; }
            if (email !== undefined) { updates.push(`email = $${idx}`); params.push(email); idx++; }
            if (phone !== undefined) { updates.push(`phone = $${idx}`); params.push(phone); idx++; }
            if (password !== undefined) { updates.push(`password = $${idx}`); params.push(password); idx++; }
            if (is_active !== undefined) { updates.push(`is_active = $${idx}`); params.push(is_active); idx++; }

            if (updates.length === 0) {
                return { success: false, error: 'No fields to update' };
            }

            sql += updates.join(', ') + ` WHERE id = $${idx}`;
            params.push(id);

            await queryDB(sql, params);

            return { success: true, message: 'Admin user updated successfully' };
        } catch (err) {
            console.error('Update admin user error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Delete admin user
    async delete_admin_user(body) {
        try {
            const { id } = body;

            if (!id) {
                return { success: false, error: 'User ID required' };
            }

            await queryDB('DELETE FROM company_admin_users WHERE id = $1', [id]);

            return { success: true, message: 'Admin user deleted successfully' };
        } catch (err) {
            console.error('Delete admin user error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // ===== WIZONE AI MEETING SYSTEM =====

    // Create new meeting
    async create_meeting(body) {
        try {
            const { company_name, meeting_name, meeting_description, created_by, created_by_name, max_participants, is_recording_enabled, _req } = body;

            if (!company_name || !meeting_name || !created_by) {
                return { success: false, error: 'Company name, meeting name, and creator required' };
            }

            // Generate unique meeting ID
            const meeting_id = 'WIZ-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9).toUpperCase();

            // Get host from request or use default
            const host = _req ? (_req.headers.host || 'localhost:8888') : 'localhost:8888';
            const protocol = host.includes('localhost') ? 'http' : 'https';
            const meeting_link = `${protocol}://${host}/meeting.html?id=${meeting_id}`;

            await queryDB(
                `INSERT INTO wizone_meetings
                (meeting_id, company_name, meeting_name, meeting_description, created_by, created_by_name,
                meeting_link, max_participants, is_recording_enabled)
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)`,
                [meeting_id, company_name, meeting_name, meeting_description || '', created_by,
                created_by_name || created_by, meeting_link, max_participants || 100, is_recording_enabled !== false]
            );

            return {
                success: true,
                meeting_id: meeting_id,
                meeting_link: meeting_link,
                message: 'Meeting created successfully'
            };
        } catch (err) {
            console.error('Create meeting error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get meeting details
    async get_meeting(query) {
        try {
            const { meeting_id } = query;

            if (!meeting_id) {
                return { success: false, error: 'Meeting ID required' };
            }

            const rows = await queryDB(
                `SELECT * FROM wizone_meetings WHERE meeting_id = $1`,
                [meeting_id]
            );

            if (rows.length === 0) {
                return { success: false, error: 'Meeting not found' };
            }

            // Get participants
            const participants = await queryDB(
                `SELECT * FROM meeting_participants
                WHERE meeting_id = $1
                ORDER BY joined_at DESC`,
                [meeting_id]
            );

            return {
                success: true,
                meeting: rows[0],
                participants: participants
            };
        } catch (err) {
            console.error('Get meeting error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get all meetings for company
    async get_company_meetings(query) {
        try {
            const { company_name, status } = query;

            if (!company_name) {
                return { success: false, error: 'Company name required' };
            }

            let sql = `SELECT m.*,
                      (SELECT COUNT(*) FROM meeting_participants WHERE meeting_id = m.meeting_id AND is_online = TRUE) as active_participants
                      FROM wizone_meetings m
                      WHERE m.company_name = $1`;
            let params = [company_name];

            if (status) {
                sql += ` AND m.status = $2`;
                params.push(status);
            }

            sql += ` ORDER BY m.created_at DESC LIMIT 100`;

            const rows = await queryDB(sql, params);

            return { success: true, data: rows };
        } catch (err) {
            console.error('Get company meetings error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Join meeting
    async join_meeting(body) {
        try {
            const { meeting_id, company_name, participant_username, participant_name } = body;

            if (!meeting_id || !participant_username) {
                return { success: false, error: 'Meeting ID and participant username required' };
            }

            // Check if meeting exists and is active
            const meeting = await queryDB(
                `SELECT * FROM wizone_meetings WHERE meeting_id = $1 AND status = 'active'`,
                [meeting_id]
            );

            if (meeting.length === 0) {
                return { success: false, error: 'Meeting not found or not active' };
            }

            // Check if already joined
            const existing = await queryDB(
                `SELECT id FROM meeting_participants WHERE meeting_id = $1 AND participant_username = $2`,
                [meeting_id, participant_username]
            );

            if (existing.length > 0) {
                // Update to online
                await queryDB(
                    `UPDATE meeting_participants SET is_online = TRUE, joined_at = NOW() WHERE id = $1`,
                    [existing[0].id]
                );
            } else {
                // Insert new participant
                await queryDB(
                    `INSERT INTO meeting_participants (meeting_id, company_name, participant_username, participant_name)
                    VALUES ($1, $2, $3, $4)`,
                    [meeting_id, company_name || meeting[0].company_name, participant_username, participant_name || participant_username]
                );
            }

            return { success: true, message: 'Joined meeting successfully', meeting: meeting[0] };
        } catch (err) {
            console.error('Join meeting error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Leave meeting
    async leave_meeting(body) {
        try {
            const { meeting_id, participant_username } = body;

            if (!meeting_id || !participant_username) {
                return { success: false, error: 'Meeting ID and participant username required' };
            }

            await queryDB(
                `UPDATE meeting_participants
                SET is_online = FALSE, left_at = NOW()
                WHERE meeting_id = $1 AND participant_username = $2`,
                [meeting_id, participant_username]
            );

            return { success: true, message: 'Left meeting successfully' };
        } catch (err) {
            console.error('Leave meeting error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // End meeting
    async end_meeting(body) {
        try {
            const { meeting_id } = body;

            if (!meeting_id) {
                return { success: false, error: 'Meeting ID required' };
            }

            await queryDB(
                `UPDATE wizone_meetings SET status = 'ended', end_time = NOW() WHERE meeting_id = $1`,
                [meeting_id]
            );

            // Mark all participants as offline
            await queryDB(
                `UPDATE meeting_participants SET is_online = FALSE, left_at = NOW() WHERE meeting_id = $1`,
                [meeting_id]
            );

            return { success: true, message: 'Meeting ended successfully' };
        } catch (err) {
            console.error('End meeting error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Start recording
    async start_recording(body) {
        try {
            const { meeting_id } = body;

            if (!meeting_id) {
                return { success: false, error: 'Meeting ID required' };
            }

            await queryDB(
                `UPDATE wizone_meetings SET recording_status = 'recording' WHERE meeting_id = $1`,
                [meeting_id]
            );

            return { success: true, message: 'Recording started' };
        } catch (err) {
            console.error('Start recording error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Stop recording
    async stop_recording(body) {
        try {
            const { meeting_id, recording_file_path, recording_size_mb, recording_duration_minutes } = body;

            if (!meeting_id) {
                return { success: false, error: 'Meeting ID required' };
            }

            await queryDB(
                `UPDATE wizone_meetings
                SET recording_status = 'completed', recording_file_path = $2, recording_size_mb = $3
                WHERE meeting_id = $1`,
                [meeting_id, recording_file_path || '', recording_size_mb || 0]
            );

            // Save to recordings table
            const meeting = await queryDB('SELECT * FROM wizone_meetings WHERE meeting_id = $1', [meeting_id]);
            if (meeting.length > 0) {
                await queryDB(
                    `INSERT INTO meeting_recordings
                    (meeting_id, company_name, recording_name, recording_file_path, recording_size_mb,
                    recording_duration_minutes, started_at, ended_at)
                    VALUES ($1, $2, $3, $4, $5, $6, $7, NOW())`,
                    [meeting_id, meeting[0].company_name, meeting[0].meeting_name,
                    recording_file_path || '', recording_size_mb || 0, recording_duration_minutes || 0,
                    meeting[0].start_time]
                );
            }

            return { success: true, message: 'Recording stopped and saved' };
        } catch (err) {
            console.error('Stop recording error:', err.message);
            return { success: false, error: err.message };
        }
    },

    // Get meeting recordings
    async get_meeting_recordings(query) {
        try {
            const { company_name, meeting_id } = query;

            if (!company_name) {
                return { success: false, error: 'Company name required' };
            }

            let sql = `SELECT * FROM meeting_recordings WHERE company_name = $1`;
            let params = [company_name];

            if (meeting_id) {
                sql += ` AND meeting_id = $2`;
                params.push(meeting_id);
            }

            sql += ` ORDER BY created_at DESC LIMIT 100`;

            const rows = await queryDB(sql, params);

            return { success: true, data: rows };
        } catch (err) {
            console.error('Get recordings error:', err.message);
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
                // Add request object for handlers that need host info
                mergedParams._req = req;
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
    
    // Initialize database tables
    initializeDatabaseTables().catch(err => {
        console.error('Failed to initialize database:', err);
    });
});

