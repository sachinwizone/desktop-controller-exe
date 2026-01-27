<?php
/**
 * Desktop Time & Attendance API
 * Connects to PostgreSQL database for attendance data
 * WIZONE IT NETWORK INDIA PVT LTD
 */

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization, X-Company-Key');

// Handle preflight requests
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit();
}

// Database Configuration
$db_config = [
    'host' => '72.61.170.243',
    'port' => '9095',
    'dbname' => 'controller_application',
    'user' => 'appuser',
    'password' => 'Wizone@2025@Sachin'
];

// Connect to PostgreSQL
function getConnection($config) {
    $conn_string = "host={$config['host']} port={$config['port']} dbname={$config['dbname']} user={$config['user']} password={$config['password']}";
    $conn = pg_connect($conn_string);
    if (!$conn) {
        throw new Exception("Database connection failed");
    }
    return $conn;
}

// Get request data
function getRequestData() {
    $json = file_get_contents('php://input');
    return json_decode($json, true) ?? [];
}

// Send JSON response
function sendResponse($data, $status = 200) {
    http_response_code($status);
    echo json_encode($data);
    exit();
}

// Send error response
function sendError($message, $status = 400) {
    sendResponse(['success' => false, 'error' => $message], $status);
}

// Validate company access (activation key)
function validateCompanyAccess($conn, $activation_key) {
    if (empty($activation_key)) {
        return null;
    }
    
    // Check if activation key exists and get company info
    $query = "SELECT DISTINCT company_name FROM punch_logs WHERE activation_key = $1 LIMIT 1";
    $result = pg_query_params($conn, $query, [$activation_key]);
    
    if ($result && pg_num_rows($result) > 0) {
        $row = pg_fetch_assoc($result);
        return $row['company_name'];
    }
    
    // Also check registered_systems table
    $query2 = "SELECT company_name FROM registered_systems WHERE activation_key = $1 LIMIT 1";
    $result2 = pg_query_params($conn, $query2, [$activation_key]);
    
    if ($result2 && pg_num_rows($result2) > 0) {
        $row = pg_fetch_assoc($result2);
        return $row['company_name'];
    }
    
    return null;
}

// Get company from header or request
function getCompanyKey() {
    // Check header first
    $headers = getallheaders();
    if (isset($headers['X-Company-Key'])) {
        return $headers['X-Company-Key'];
    }
    // Check GET parameter
    if (isset($_GET['activation_key'])) {
        return $_GET['activation_key'];
    }
    // Check POST data
    $data = getRequestData();
    if (isset($data['activation_key'])) {
        return $data['activation_key'];
    }
    return null;
}

// API Router
$action = $_GET['action'] ?? '';

try {
    $conn = getConnection($db_config);
    $activation_key = getCompanyKey();
    
    switch ($action) {
        
        // ==================== AUTHENTICATION ====================
        
        case 'login':
            $data = getRequestData();
            $username = $data['username'] ?? '';
            $password = $data['password'] ?? '';
            $role = $data['role'] ?? 'user';
            $key = $data['activation_key'] ?? '';
            
            if (empty($username) || empty($password) || empty($key)) {
                sendError('Username, password and activation key are required');
            }
            
            // Validate activation key
            $company = validateCompanyAccess($conn, $key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            // For admin, verify company password
            if ($role === 'admin') {
                $query = "SELECT password_hash FROM company_passwords WHERE LOWER(company_name) = LOWER($1)";
                $result = pg_query_params($conn, $query, [trim($company)]);
                
                if (!$result || pg_num_rows($result) === 0) {
                    sendError('Company password not set. Please set password in Desktop Controller app first.', 401);
                }
                
                $row = pg_fetch_assoc($result);
                $stored_hash = $row['password_hash'];
                
                // Hash the input password same way as C# app
                $input_hash = hash('sha256', $password . 'DesktopController_Salt_2025');
                
                if ($input_hash !== $stored_hash) {
                    sendError('Invalid password', 401);
                }
            }
            
            // Get user info if exists
            $user_query = "SELECT DISTINCT username, system_name, department, office_location 
                          FROM punch_logs 
                          WHERE activation_key = $1 AND username ILIKE $2
                          LIMIT 1";
            $user_result = pg_query_params($conn, $user_query, [$key, "%$username%"]);
            
            $user_info = [
                'username' => $username,
                'company' => $company,
                'activation_key' => $key,
                'role' => $role
            ];
            
            if ($user_result && pg_num_rows($user_result) > 0) {
                $user_row = pg_fetch_assoc($user_result);
                $user_info['system_name'] = $user_row['system_name'];
                $user_info['department'] = $user_row['department'];
                $user_info['office_location'] = $user_row['office_location'];
            }
            
            sendResponse([
                'success' => true,
                'message' => 'Login successful',
                'user' => $user_info,
                'token' => base64_encode(json_encode(['key' => $key, 'company' => $company, 'role' => $role, 'exp' => time() + 86400]))
            ]);
            break;
            
        case 'validate_key':
            $data = getRequestData();
            $key = $data['activation_key'] ?? '';
            
            if (empty($key)) {
                sendError('Activation key is required');
            }
            
            $company = validateCompanyAccess($conn, $key);
            if ($company) {
                sendResponse([
                    'success' => true,
                    'valid' => true,
                    'company_name' => $company
                ]);
            } else {
                sendResponse([
                    'success' => true,
                    'valid' => false,
                    'message' => 'No data found for this activation key'
                ]);
            }
            break;
        
        // ==================== ATTENDANCE DATA ====================
        
        case 'get_attendance':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            $date_from = $_GET['date_from'] ?? date('Y-m-01');
            $date_to = $_GET['date_to'] ?? date('Y-m-d');
            $username = $_GET['username'] ?? null;
            
            $query = "SELECT id, username, system_name, department, office_location,
                            punch_type, punch_time, punch_out_time, total_duration_minutes,
                            log_timestamp::date as log_date
                     FROM punch_logs 
                     WHERE activation_key = $1 
                       AND log_timestamp::date >= $2::date 
                       AND log_timestamp::date <= $3::date";
            
            $params = [$activation_key, $date_from, $date_to];
            
            if ($username) {
                $query .= " AND username ILIKE $4";
                $params[] = "%$username%";
            }
            
            $query .= " ORDER BY log_timestamp DESC";
            
            $result = pg_query_params($conn, $query, $params);
            
            $records = [];
            if ($result) {
                while ($row = pg_fetch_assoc($result)) {
                    $records[] = [
                        'id' => $row['id'],
                        'username' => $row['username'],
                        'system_name' => $row['system_name'],
                        'department' => $row['department'],
                        'office_location' => $row['office_location'],
                        'punch_type' => $row['punch_type'],
                        'punch_in' => $row['punch_time'],
                        'punch_out' => $row['punch_out_time'],
                        'duration_minutes' => $row['total_duration_minutes'],
                        'date' => $row['log_date']
                    ];
                }
            }
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'count' => count($records),
                'records' => $records
            ]);
            break;
            
        case 'get_today_attendance':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            $today = date('Y-m-d');
            
            $query = "SELECT username, system_name, department, office_location,
                            punch_type, punch_time, punch_out_time, total_duration_minutes
                     FROM punch_logs 
                     WHERE activation_key = $1 AND log_timestamp::date = $2::date
                     ORDER BY punch_time DESC";
            
            $result = pg_query_params($conn, $query, [$activation_key, $today]);
            
            $records = [];
            $present_count = 0;
            $absent_count = 0;
            
            if ($result) {
                $seen_users = [];
                while ($row = pg_fetch_assoc($result)) {
                    $user = $row['username'];
                    if (!in_array($user, $seen_users)) {
                        $seen_users[] = $user;
                        $present_count++;
                    }
                    $records[] = $row;
                }
            }
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'date' => $today,
                'present' => $present_count,
                'records' => $records
            ]);
            break;
            
        // ==================== EMPLOYEES ====================
        
        case 'get_employees':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            // Get unique employees from punch_logs
            $query = "SELECT DISTINCT ON (username) 
                            username, system_name, department, office_location,
                            MAX(log_timestamp) as last_activity
                     FROM punch_logs 
                     WHERE activation_key = $1
                     GROUP BY username, system_name, department, office_location
                     ORDER BY username, last_activity DESC";
            
            $result = pg_query_params($conn, $query, [$activation_key]);
            
            $employees = [];
            if ($result) {
                while ($row = pg_fetch_assoc($result)) {
                    $employees[] = [
                        'name' => $row['username'],
                        'system' => $row['system_name'],
                        'department' => $row['department'],
                        'location' => $row['office_location'],
                        'last_active' => $row['last_activity'],
                        'status' => 'Active'
                    ];
                }
            }
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'count' => count($employees),
                'employees' => $employees
            ]);
            break;
            
        // ==================== REGISTERED SYSTEMS ====================
        
        case 'get_systems':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            $query = "SELECT system_id, system_name, username, company_name, 
                            is_online, last_seen, registered_at, ip_address
                     FROM registered_systems 
                     WHERE activation_key = $1
                     ORDER BY last_seen DESC";
            
            $result = pg_query_params($conn, $query, [$activation_key]);
            
            $systems = [];
            if ($result) {
                while ($row = pg_fetch_assoc($result)) {
                    $systems[] = $row;
                }
            }
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'count' => count($systems),
                'systems' => $systems
            ]);
            break;
            
        // ==================== DASHBOARD STATS ====================
        
        case 'get_dashboard_stats':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            $today = date('Y-m-d');
            
            // Total employees
            $emp_query = "SELECT COUNT(DISTINCT username) as total FROM punch_logs WHERE activation_key = $1";
            $emp_result = pg_query_params($conn, $emp_query, [$activation_key]);
            $total_employees = pg_fetch_assoc($emp_result)['total'] ?? 0;
            
            // Present today
            $present_query = "SELECT COUNT(DISTINCT username) as present FROM punch_logs WHERE activation_key = $1 AND log_timestamp::date = $2::date";
            $present_result = pg_query_params($conn, $present_query, [$activation_key, $today]);
            $present_today = pg_fetch_assoc($present_result)['present'] ?? 0;
            
            // Currently punched in
            $punched_query = "SELECT COUNT(DISTINCT username) as punched FROM punch_logs WHERE activation_key = $1 AND log_timestamp::date = $2::date AND punch_type = 'IN' AND punch_out_time IS NULL";
            $punched_result = pg_query_params($conn, $punched_query, [$activation_key, $today]);
            $currently_working = pg_fetch_assoc($punched_result)['punched'] ?? 0;
            
            // Average hours this week
            $week_start = date('Y-m-d', strtotime('monday this week'));
            $avg_query = "SELECT AVG(total_duration_minutes) as avg_mins FROM punch_logs WHERE activation_key = $1 AND log_timestamp::date >= $2::date AND total_duration_minutes > 0";
            $avg_result = pg_query_params($conn, $avg_query, [$activation_key, $week_start]);
            $avg_mins = pg_fetch_assoc($avg_result)['avg_mins'] ?? 0;
            $avg_hours = round($avg_mins / 60, 1);
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'stats' => [
                    'total_employees' => (int)$total_employees,
                    'present_today' => (int)$present_today,
                    'absent_today' => max(0, (int)$total_employees - (int)$present_today),
                    'currently_working' => (int)$currently_working,
                    'avg_hours_week' => $avg_hours
                ]
            ]);
            break;
            
        // ==================== ACTIVITY LOGS ====================
        
        case 'get_activity_logs':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            $limit = $_GET['limit'] ?? 50;
            $log_type = $_GET['type'] ?? 'all';
            
            $query = "SELECT * FROM audit_logs WHERE activation_key = $1";
            if ($log_type !== 'all') {
                $query .= " AND action_type = $2";
                $params = [$activation_key, $log_type];
            } else {
                $params = [$activation_key];
            }
            $query .= " ORDER BY created_at DESC LIMIT " . intval($limit);
            
            $result = pg_query_params($conn, $query, $params);
            
            $logs = [];
            if ($result) {
                while ($row = pg_fetch_assoc($result)) {
                    $logs[] = $row;
                }
            }
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'logs' => $logs
            ]);
            break;
            
        // ==================== SCREENSHOTS ====================
        
        case 'get_screenshots':
            if (!$activation_key) {
                sendError('Activation key required', 401);
            }
            
            $company = validateCompanyAccess($conn, $activation_key);
            if (!$company) {
                sendError('Invalid activation key', 401);
            }
            
            $date = $_GET['date'] ?? date('Y-m-d');
            $username = $_GET['username'] ?? null;
            
            $query = "SELECT id, username, system_name, screenshot_path, captured_at, storage_type
                     FROM screenshots 
                     WHERE activation_key = $1 AND captured_at::date = $2::date";
            
            $params = [$activation_key, $date];
            
            if ($username) {
                $query .= " AND username ILIKE $3";
                $params[] = "%$username%";
            }
            
            $query .= " ORDER BY captured_at DESC";
            
            $result = pg_query_params($conn, $query, $params);
            
            $screenshots = [];
            if ($result) {
                while ($row = pg_fetch_assoc($result)) {
                    $screenshots[] = $row;
                }
            }
            
            sendResponse([
                'success' => true,
                'company' => $company,
                'date' => $date,
                'screenshots' => $screenshots
            ]);
            break;
            
        default:
            sendResponse([
                'success' => true,
                'message' => 'Desktop Time & Attendance API',
                'version' => '1.0.0',
                'endpoints' => [
                    'POST /api.php?action=login' => 'Authenticate user',
                    'POST /api.php?action=validate_key' => 'Validate activation key',
                    'GET /api.php?action=get_attendance' => 'Get attendance records',
                    'GET /api.php?action=get_today_attendance' => 'Get today\'s attendance',
                    'GET /api.php?action=get_employees' => 'Get employee list',
                    'GET /api.php?action=get_systems' => 'Get registered systems',
                    'GET /api.php?action=get_dashboard_stats' => 'Get dashboard statistics',
                    'GET /api.php?action=get_activity_logs' => 'Get activity logs',
                    'GET /api.php?action=get_screenshots' => 'Get screenshots'
                ]
            ]);
    }
    
    pg_close($conn);
    
} catch (Exception $e) {
    sendError('Server error: ' . $e->getMessage(), 500);
}
?>
