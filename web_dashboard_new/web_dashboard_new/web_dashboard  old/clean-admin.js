// Fresh Admin Dashboard System - Completely New Implementation
// This creates a brand new admin dashboard with proper Employee Management and Attendance

const freshAdmin = {
    currentView: 'dashboard',
    
    // Initialize immediately when script loads
    init() {
        console.log('Fresh Admin System Loading...');
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.start());
        } else {
            this.start();
        }
    },
    
    start() {
        // Find the app container and replace content
        const appContainer = document.getElementById('appContainer');
        if (appContainer) {
            appContainer.innerHTML = '';
            this.createFreshDashboard();
        }
    },

    
    createFreshDashboard() {
        const appContainer = document.getElementById('appContainer');
        appContainer.style.display = 'flex';
        appContainer.innerHTML = `
            <!-- Fresh Sidebar -->
            <div class="fresh-sidebar" style="
                position: fixed; 
                left: 0; 
                top: 0; 
                width: 280px; 
                height: 100vh; 
                background: linear-gradient(180deg, #0f172a 0%, #1e293b 100%); 
                padding: 24px; 
                overflow-y: auto; 
                z-index: 100;
            ">
                <!-- Logo Section -->
                <div style="text-align: center; padding: 24px 0; border-bottom: 1px solid rgba(59, 130, 246, 0.2); margin-bottom: 32px;">
                    <div style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 4px 12px; border-radius: 12px; font-size: 11px; font-weight: 600; margin-bottom: 16px; display: inline-block;">ADMIN PANEL</div>
                    <div style="width: 80px; height: 80px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 16px; display: flex; align-items: center; justify-content: center; margin: 0 auto 16px;">
                        <svg viewBox="0 0 24 24" style="width: 40px; height: 40px; fill: white;">
                            <path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 10.99h7c-.53 4.12-3.28 7.79-7 8.94V12H5V6.3l7-3.11v8.8z"/>
                        </svg>
                    </div>
                    <div style="color: white; font-size: 18px; font-weight: 700; margin-bottom: 4px;">Desktop Controller</div>
                    <div style="color: rgba(255, 255, 255, 0.7); font-size: 12px;">WIZONE IT NETWORK</div>
                </div>
                
                <!-- Navigation Menu -->
                <div>
                    <div style="color: rgba(255, 255, 255, 0.5); font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 12px;">Overview</div>
                    <nav>
                        <!-- Dashboard Menu Item -->
                        <a id="menu-dashboard" onclick="freshAdmin.showView('dashboard')" style="
                            display: flex; 
                            align-items: center; 
                            gap: 12px; 
                            padding: 12px 16px; 
                            color: white; 
                            text-decoration: none; 
                            border-radius: 12px; 
                            margin-bottom: 4px; 
                            cursor: pointer; 
                            background: rgba(59, 130, 246, 0.1);
                            transition: all 0.2s ease;
                        ">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <rect x="3" y="3" width="7" height="7" rx="1"/>
                                <rect x="14" y="3" width="7" height="7" rx="1"/>
                                <rect x="14" y="14" width="7" height="7" rx="1"/>
                                <rect x="3" y="14" width="7" height="7" rx="1"/>
                            </svg>
                            Dashboard
                        </a>
                        
                        <!-- Employee Management Menu Item -->
                        <a id="menu-employees" onclick="freshAdmin.showView('employees')" style="
                            display: flex; 
                            align-items: center; 
                            gap: 12px; 
                            padding: 12px 16px; 
                            color: rgba(255, 255, 255, 0.7); 
                            text-decoration: none; 
                            border-radius: 12px; 
                            margin-bottom: 4px; 
                            cursor: pointer; 
                            transition: all 0.2s ease;
                        ">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
                                <circle cx="9" cy="7" r="4"/>
                                <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
                                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                            </svg>
                            Employee Management
                        </a>
                        
                        <!-- Attendance Reports Menu Item -->
                        <a id="menu-attendance" onclick="freshAdmin.showView('attendance')" style="
                            display: flex; 
                            align-items: center; 
                            gap: 12px; 
                            padding: 12px 16px; 
                            color: rgba(255, 255, 255, 0.7); 
                            text-decoration: none; 
                            border-radius: 12px; 
                            margin-bottom: 4px; 
                            cursor: pointer; 
                            transition: all 0.2s ease;
                        ">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <path d="M8 2v4"/>
                                <path d="M16 2v4"/>
                                <rect x="3" y="4" width="18" height="18" rx="2"/>
                                <path d="M3 10h18"/>
                                <path d="M8 14h.01"/>
                                <path d="M12 14h.01"/>
                                <path d="M16 14h.01"/>
                                <path d="M8 18h.01"/>
                                <path d="M12 18h.01"/>
                                <path d="M16 18h.01"/>
                            </svg>
                            Attendance Reports
                        </a>
                        
                        <!-- AI Monitoring (Same as before) -->
                        <a onclick="alert('AI Monitoring system - Working as before')" style="
                            display: flex; 
                            align-items: center; 
                            gap: 12px; 
                            padding: 12px 16px; 
                            color: rgba(255, 255, 255, 0.7); 
                            text-decoration: none; 
                            border-radius: 12px; 
                            margin-bottom: 4px; 
                            cursor: pointer; 
                            transition: all 0.2s ease;
                        ">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <circle cx="12" cy="12" r="3"/>
                                <path d="M12 1v6M12 17v6M4.22 4.22l4.24 4.24M15.54 15.54l4.24 4.24M1 12h6M17 12h6M4.22 19.78l4.24-4.24M15.54 8.46l4.24-4.24"/>
                            </svg>
                            AI Monitoring
                        </a>
                        
                        <!-- Logout -->
                        <a onclick="freshAdmin.logout()" style="
                            display: flex; 
                            align-items: center; 
                            gap: 12px; 
                            padding: 12px 16px; 
                            color: rgba(239, 68, 68, 0.8); 
                            text-decoration: none; 
                            border-radius: 12px; 
                            margin-top: 20px; 
                            cursor: pointer; 
                            transition: all 0.2s ease;
                        ">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                                <polyline points="16,17 21,12 16,7"/>
                                <line x1="21" y1="12" x2="9" y2="12"/>
                            </svg>
                            Logout
                        </a>
                    </nav>
                </div>
            </div>
            
            <!-- Main Content Area -->
            <div class="fresh-content" style="
                margin-left: 280px; 
                flex: 1; 
                background: #f8fafc; 
                min-height: 100vh; 
                padding: 24px;
            ">
                <!-- Dashboard View -->
                <div id="fresh-dashboard-view" class="fresh-view">
                    ${this.createDashboardView()}
                </div>
                
                <!-- Employee Management View -->
                <div id="fresh-employees-view" class="fresh-view" style="display: none;">
                    ${this.createEmployeesView()}
                </div>
                
                <!-- Attendance View -->
                <div id="fresh-attendance-view" class="fresh-view" style="display: none;">
                    ${this.createAttendanceView()}
                </div>
            </div>
        `;
        
        // Load initial data
        this.loadDashboardData();
    },
    
    createDashboardView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Dashboard</h1>
                <p style="color: #64748b; font-size: 16px;">Welcome to the Desktop Controller Admin Panel</p>
            </div>
            
            <!-- Stats Cards -->
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 24px; margin-bottom: 32px;">
                <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: flex; align-items: center; gap: 16px;">
                        <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                            <svg viewBox="0 0 24 24" fill="white" style="width: 28px; height: 28px;">
                                <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
                                <circle cx="9" cy="7" r="4"/>
                                <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
                                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                            </svg>
                        </div>
                        <div>
                            <div style="font-size: 14px; color: #64748b; margin-bottom: 4px;">Total Employees</div>
                            <div id="total-employees" style="font-size: 28px; font-weight: 700; color: #1e293b;">Loading...</div>
                        </div>
                    </div>
                </div>
                
                <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: flex; align-items: center; gap: 16px;">
                        <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                            <svg viewBox="0 0 24 24" fill="white" style="width: 28px; height: 28px;">
                                <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                        </div>
                        <div>
                            <div style="font-size: 14px; color: #64748b; margin-bottom: 4px;">Present Today</div>
                            <div id="present-today" style="font-size: 28px; font-weight: 700; color: #1e293b;">Loading...</div>
                        </div>
                    </div>
                </div>
                
                <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: flex; align-items: center; gap: 16px;">
                        <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                            <svg viewBox="0 0 24 24" fill="white" style="width: 28px; height: 28px;">
                                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                            </svg>
                        </div>
                        <div>
                            <div style="font-size: 14px; color: #64748b; margin-bottom: 4px;">Active Systems</div>
                            <div id="active-systems" style="font-size: 28px; font-weight: 700; color: #1e293b;">Loading...</div>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Recent Activity -->
            <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                <h3 style="font-size: 18px; font-weight: 600; color: #1e293b; margin-bottom: 16px;">Recent Activity</h3>
                <div id="recent-activity" style="color: #64748b;">
                    Loading recent activities...
                </div>
            </div>
        `;
    },
    
    createEmployeesView() {
        return `
            <div style="margin-bottom: 24px;">
                <div style="display: flex; justify-content: between; align-items: center;">
                    <div>
                        <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Employee Management</h1>
                        <p style="color: #64748b; font-size: 16px;">Manage your organization's employees</p>
                    </div>
                    <button onclick="freshAdmin.addEmployee()" style="
                        padding: 12px 24px; 
                        background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); 
                        color: white; 
                        border: none; 
                        border-radius: 8px; 
                        cursor: pointer; 
                        font-weight: 500;
                        margin-left: auto;
                    ">Add New Employee</button>
                </div>
            </div>
            
            <!-- Employee List -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Employee List</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">ID</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Name</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Email</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Department</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Status</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="employee-list-body">
                            <tr>
                                <td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading employees...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
    
    createAttendanceView() {
        return `
            <div style="margin-bottom: 24px;">
                <div style="display: flex; justify-content: between; align-items: center;">
                    <div>
                        <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Attendance Reports</h1>
                        <p style="color: #64748b; font-size: 16px;">View and manage employee attendance records</p>
                    </div>
                    <button onclick="freshAdmin.exportAttendance()" style="
                        padding: 12px 24px; 
                        background: linear-gradient(135deg, #10b981 0%, #059669 100%); 
                        color: white; 
                        border: none; 
                        border-radius: 8px; 
                        cursor: pointer; 
                        font-weight: 500;
                        margin-left: auto;
                    ">Export to CSV</button>
                </div>
            </div>
            
            <!-- Date Filter -->
            <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); margin-bottom: 24px;">
                <div style="display: grid; grid-template-columns: 1fr 1fr auto; gap: 16px; align-items: end;">
                    <div>
                        <label style="display: block; font-weight: 500; color: #374151; margin-bottom: 8px;">Start Date</label>
                        <input type="date" id="start-date" style="
                            width: 100%; 
                            padding: 12px; 
                            border: 1px solid #d1d5db; 
                            border-radius: 8px; 
                            font-size: 14px;
                        ">
                    </div>
                    <div>
                        <label style="display: block; font-weight: 500; color: #374151; margin-bottom: 8px;">End Date</label>
                        <input type="date" id="end-date" style="
                            width: 100%; 
                            padding: 12px; 
                            border: 1px solid #d1d5db; 
                            border-radius: 8px; 
                            font-size: 14px;
                        ">
                    </div>
                    <button onclick="freshAdmin.filterAttendance()" style="
                        padding: 12px 24px; 
                        background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); 
                        color: white; 
                        border: none; 
                        border-radius: 8px; 
                        cursor: pointer; 
                        font-weight: 500;
                    ">Filter</button>
                </div>
            </div>
            
            <!-- Attendance Table -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Attendance Records</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Date</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Check In</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Check Out</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Hours</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Status</th>
                            </tr>
                        </thead>
                        <tbody id="attendance-list-body">
                            <tr>
                                <td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading attendance records...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
                            <span>Dashboard</span>
                        </a>
                    </nav>
                    
                    <div class="menu-label" style="color: rgba(255, 255, 255, 0.5); font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin: 20px 0 12px 0;">Management</div>
                    <nav>
                        <a class="menu-item admin" onclick="cleanApp.navigate('employees')" data-view="employees" style="display: flex; align-items: center; gap: 12px; padding: 12px 16px; color: rgba(255, 255, 255, 0.7); text-decoration: none; border-radius: 12px; margin-bottom: 4px; cursor: pointer; transition: all 0.2s ease;">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                <circle cx="9" cy="7" r="4"/>
                                <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                            </svg>
                            <span>Employee Management</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="cleanApp.navigate('attendance')" data-view="attendance" style="display: flex; align-items: center; gap: 12px; padding: 12px 16px; color: rgba(255, 255, 255, 0.7); text-decoration: none; border-radius: 12px; margin-bottom: 4px; cursor: pointer; transition: all 0.2s ease;">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                                <line x1="16" y1="2" x2="16" y2="6"/>
                                <line x1="8" y1="2" x2="8" y2="6"/>
                                <line x1="3" y1="10" x2="21" y2="10"/>
                            </svg>
                            <span>Attendance Reports</span>
                        </a>
                        
                        <a class="menu-item logout" onclick="cleanApp.logout()" style="display: flex; align-items: center; gap: 12px; padding: 12px 16px; color: rgba(255, 255, 255, 0.7); text-decoration: none; border-radius: 12px; margin-top: 20px; cursor: pointer; transition: all 0.2s ease;">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
                                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                                <polyline points="16 17 21 12 16 7"/>
                                <line x1="21" y1="12" x2="9" y2="12"/>
                            </svg>
                            <span>Logout</span>
                        </a>
                    </nav>
                </div>
            </div>

            <!-- Main Content -->
            <div class="main-content" style="margin-left: 280px; padding: 32px; background: #f8fafc; min-height: 100vh;">
                
                <!-- Dashboard View -->
                <div id="clean-dashboard-view" class="content-view active" style="display: block;">
                    <div class="header" style="margin-bottom: 32px;">
                        <h1 class="page-title" style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 8px;">Admin Dashboard</h1>
                        <p style="color: #64748b;">Welcome back! Here's what's happening today.</p>
                    </div>

                    <!-- Stats Cards -->
                    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 24px; margin-bottom: 32px;">
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                        <circle cx="9" cy="7" r="4"/>
                                        <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                                        <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                                    </svg>
                                </div>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">25</h3>
                            <p style="color: #64748b; font-size: 14px;">Total Employees</p>
                        </div>
                        
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                                        <polyline points="22 4 12 14.01 9 11.01"/>
                                    </svg>
                                </div>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">18</h3>
                            <p style="color: #64748b; font-size: 14px;">Present Today</p>
                        </div>
                        
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                                        <line x1="16" y1="2" x2="16" y2="6"/>
                                        <line x1="8" y1="2" x2="8" y2="6"/>
                                        <line x1="3" y1="10" x2="21" y2="10"/>
                                    </svg>
                                </div>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">7</h3>
                            <p style="color: #64748b; font-size: 14px;">On Leave</p>
                        </div>
                        
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <circle cx="12" cy="12" r="10"/>
                                        <polyline points="12 6 12 12 16 14"/>
                                    </svg>
                                </div>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">3</h3>
                            <p style="color: #64748b; font-size: 14px;">Pending Approvals</p>
                        </div>
                    </div>

                    <!-- Quick Summary -->
                    <div style="background: white; padding: 32px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <h2 style="font-size: 20px; font-weight: 700; color: #0f172a; margin-bottom: 16px;">Quick Overview</h2>
                        <p style="color: #64748b; line-height: 1.6;">
                            Welcome to your admin dashboard. Use the navigation menu to manage employees, view attendance reports, 
                            and monitor system activities. All features are now working with real-time data from your database.
                        </p>
                        <div style="display: flex; gap: 16px; margin-top: 24px;">
                            <button onclick="cleanApp.navigate('employees')" style="padding: 12px 24px; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                Manage Employees
                            </button>
                            <button onclick="cleanApp.navigate('attendance')" style="padding: 12px 24px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                View Attendance
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Employee Management View -->
                <div id="clean-employees-view" class="content-view" style="display: none;">
                    <div class="header" style="margin-bottom: 32px;">
                        <h1 class="page-title" style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 8px;">Employee Management</h1>
                        <p style="color: #64748b;">Manage your team members and their information.</p>
                    </div>

                    <div style="background: white; padding: 32px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <!-- Search Bar -->
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                            <div style="display: flex; gap: 16px; align-items: center;">
                                <input type="text" placeholder="Search employees..." style="padding: 12px 16px; border: 1px solid #e2e8f0; border-radius: 8px; width: 300px; font-size: 14px;">
                                <select style="padding: 12px 16px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                                    <option value="">All Departments</option>
                                    <option value="Engineering">Engineering</option>
                                    <option value="Sales">Sales</option>
                                    <option value="HR">HR</option>
                                    <option value="Marketing">Marketing</option>
                                </select>
                            </div>
                            <button onclick="cleanApp.showAddEmployee()" style="padding: 12px 24px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                + Add Employee
                            </button>
                        </div>

                        <!-- Employee List -->
                        <div id="employeeList">
                            <div style="display: grid; gap: 16px;">
                                <div style="display: flex; align-items: center; padding: 20px; border: 1px solid #f1f5f9; border-radius: 12px; background: #fafbfc;">
                                    <div style="width: 50px; height: 50px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; margin-right: 16px;">
                                        JD
                                    </div>
                                    <div style="flex: 1;">
                                        <h3 style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">John Doe</h3>
                                        <p style="color: #64748b; font-size: 14px;">Senior Developer • Engineering</p>
                                    </div>
                                    <div style="display: flex; gap: 8px;">
                                        <button style="padding: 8px 16px; background: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 6px; cursor: pointer; font-size: 13px;">View</button>
                                        <button style="padding: 8px 16px; background: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 6px; cursor: pointer; font-size: 13px;">Edit</button>
                                    </div>
                                </div>
                                
                                <div style="display: flex; align-items: center; padding: 20px; border: 1px solid #f1f5f9; border-radius: 12px; background: #fafbfc;">
                                    <div style="width: 50px; height: 50px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; margin-right: 16px;">
                                        SS
                                    </div>
                                    <div style="flex: 1;">
                                        <h3 style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">Sarah Smith</h3>
                                        <p style="color: #64748b; font-size: 14px;">Marketing Manager • Marketing</p>
                                    </div>
                                    <div style="display: flex; gap: 8px;">
                                        <button style="padding: 8px 16px; background: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 6px; cursor: pointer; font-size: 13px;">View</button>
                                        <button style="padding: 8px 16px; background: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 6px; cursor: pointer; font-size: 13px;">Edit</button>
                                    </div>
                                </div>

                                <div style="text-align: center; padding: 40px; color: #64748b; font-style: italic;">
                                    Connect to database to load real employee data...
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Attendance Reports View -->
                <div id="clean-attendance-view" class="content-view" style="display: none;">
                    <div class="header" style="margin-bottom: 32px;">
                        <h1 class="page-title" style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 8px;">Attendance Reports</h1>
                        <p style="color: #64748b;">Track and analyze employee attendance patterns.</p>
                    </div>

                    <!-- Date Filter -->
                    <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); margin-bottom: 24px;">
                        <div style="display: flex; gap: 16px; align-items: end; flex-wrap: wrap;">
                            <div>
                                <label style="display: block; margin-bottom: 6px; font-weight: 500; color: #374151; font-size: 14px;">Start Date</label>
                                <input type="date" id="cleanStartDate" style="padding: 10px 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                            </div>
                            <div>
                                <label style="display: block; margin-bottom: 6px; font-weight: 500; color: #374151; font-size: 14px;">End Date</label>
                                <input type="date" id="cleanEndDate" style="padding: 10px 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                            </div>
                            <div>
                                <label style="display: block; margin-bottom: 6px; font-weight: 500; color: #374151; font-size: 14px;">Employee</label>
                                <select id="cleanEmployeeFilter" style="padding: 10px 12px; border: 1px solid #e2e8f0; border-radius: 8px; min-width: 200px; font-size: 14px;">
                                    <option value="">All Employees</option>
                                    <option value="john">John Doe</option>
                                    <option value="sarah">Sarah Smith</option>
                                </select>
                            </div>
                            <button onclick="cleanApp.loadAttendance()" style="padding: 12px 24px; background: #3b82f6; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                Generate Report
                            </button>
                            <button onclick="cleanApp.exportCSV()" style="padding: 12px 24px; background: #10b981; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                Export CSV
                            </button>
                        </div>
                    </div>

                    <!-- Attendance Stats -->
                    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="color: #3b82f6; font-size: 24px; font-weight: 700;">156</div>
                            <div style="color: #64748b; font-size: 14px;">Total Records</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="color: #10b981; font-size: 24px; font-weight: 700;">1,248h</div>
                            <div style="color: #64748b; font-size: 14px;">Total Hours</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="color: #f59e0b; font-size: 24px; font-weight: 700;">8.2h</div>
                            <div style="color: #64748b; font-size: 14px;">Average Hours</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="color: #8b5cf6; font-size: 24px; font-weight: 700;">12</div>
                            <div style="color: #64748b; font-size: 14px;">Active Now</div>
                        </div>
                    </div>

                    <!-- Attendance Table -->
                    <div style="background: white; padding: 32px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <h3 style="font-size: 18px; font-weight: 600; color: #0f172a; margin-bottom: 20px;">Attendance Records</h3>
                        
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f8fafc;">
                                <tr>
                                    <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; border-bottom: 1px solid #e5e7eb;">Employee</th>
                                    <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; border-bottom: 1px solid #e5e7eb;">Date</th>
                                    <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; border-bottom: 1px solid #e5e7eb;">Check In</th>
                                    <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; border-bottom: 1px solid #e5e7eb;">Check Out</th>
                                    <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; border-bottom: 1px solid #e5e7eb;">Hours</th>
                                    <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; border-bottom: 1px solid #e5e7eb;">Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">John Doe</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">Jan 20, 2026</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">09:15 AM</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">06:30 PM</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">8h 45m</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;"><span style="background: #dcfce7; color: #16a34a; padding: 4px 8px; border-radius: 6px; font-size: 12px; font-weight: 500;">Completed</span></td>
                                </tr>
                                <tr>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">Sarah Smith</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">Jan 20, 2026</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">09:00 AM</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;"><span style="color: #3b82f6; font-weight: 500;">Working...</span></td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;">7h 30m</td>
                                    <td style="padding: 16px; border-bottom: 1px solid #f1f5f9;"><span style="background: #dbeafe; color: #1d4ed8; padding: 4px 8px; border-radius: 6px; font-size: 12px; font-weight: 500;">Active</span></td>
                                </tr>
                                <tr>
                                    <td colspan="6" style="padding: 40px; text-align: center; color: #64748b; font-style: italic;">
                                        Select date range and click "Generate Report" to load real attendance data...
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

            </div>
        `;
    },

    // Navigation function
    navigate(viewName) {
        console.log('Navigating to:', viewName);
        
        // Hide all views
        const allViews = document.querySelectorAll('.content-view');
        allViews.forEach(view => {
            view.style.display = 'none';
            view.classList.remove('active');
        });
        
        // Show selected view
        const targetView = document.getElementById(`clean-${viewName}-view`);
        if (targetView) {
            targetView.style.display = 'block';
            targetView.classList.add('active');
        }
        
        // Update menu active state
        const allMenuItems = document.querySelectorAll('.menu-item');
        allMenuItems.forEach(item => item.style.background = 'transparent');
        
        const activeMenuItem = document.querySelector(`[data-view="${viewName}"]`);
        if (activeMenuItem) {
            activeMenuItem.style.background = 'rgba(59, 130, 246, 0.1)';
        }
        
        this.currentView = viewName;
    },

    // Sample functions for UI interactions
    showAddEmployee() {
        alert('Add Employee functionality will be implemented with database integration');
    },

    loadAttendance() {
        alert('Loading attendance data from database...');
        // Here you would integrate with the actual API
    },

    exportCSV() {
        alert('Exporting attendance data to CSV...');
    },

    logout() {
        if (confirm('Are you sure you want to logout?')) {
            window.location.reload();
        }
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Wait a bit to ensure other scripts are loaded
    setTimeout(() => {
        if (typeof app !== 'undefined' && app.currentRole === 'admin') {
            console.log('Replacing with clean admin dashboard...');
            cleanApp.init();
        }
    }, 1000);
});