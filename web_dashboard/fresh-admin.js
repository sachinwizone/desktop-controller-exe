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
        // Find any suitable container and replace content
        let appContainer = document.getElementById('appContainer');
        
        if (!appContainer) {
            // Look for other common container IDs or create one
            appContainer = document.getElementById('app') || 
                          document.getElementById('main') || 
                          document.getElementById('content') ||
                          document.querySelector('.container') ||
                          document.querySelector('main') ||
                          document.body;
        }
        
        if (appContainer) {
            console.log('Fresh Admin System: Found container, replacing content...');
            appContainer.innerHTML = '';
            appContainer.style.margin = '0';
            appContainer.style.padding = '0';
            this.createFreshDashboard(appContainer);
        } else {
            console.error('Fresh Admin System: No suitable container found');
        }
    },
    
    createFreshDashboard(container) {
        const appContainer = container || document.getElementById('appContainer');
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
                        
                        <!-- Live Systems Menu Item -->
                        <a id="menu-live-systems" onclick="freshAdmin.showView('live-systems')" style="
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
                                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
                                <line x1="8" y1="21" x2="16" y2="21"/>
                                <line x1="12" y1="17" x2="12" y2="21"/>
                            </svg>
                            Live Systems
                        </a>
                        
                        <!-- Leave Management Menu Item -->
                        <a id="menu-leave-management" onclick="freshAdmin.showView('leave-management')" style="
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
                                <path d="M9 11H5a2 2 0 0 0-2 2v3a2 2 0 0 0 2 2h4m6-6h4a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-4m-6-6V9a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2m-6 0h6"/>
                            </svg>
                            Leave Management
                        </a>
                        
                        <!-- Departments Menu Item -->
                        <a id="menu-departments" onclick="freshAdmin.showView('departments')" style="
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
                                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                <circle cx="9" cy="7" r="4"/>
                                <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                            </svg>
                            Departments
                        </a>
                    </nav>
                    
                    <!-- LOGS Section -->
                    <div style="color: rgba(255, 255, 255, 0.5); font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin: 24px 0 12px 0;">LOGS & MONITORING</div>
                    <nav>
                        <!-- Web Browsing Logs Menu Item -->
                        <a id="menu-web-logs" onclick="freshAdmin.showView('web-logs')" style="
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
                                <circle cx="12" cy="12" r="10"/>
                                <path d="M2 12h20"/>
                                <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                            </svg>
                            Web Browsing Logs
                        </a>
                        
                        <!-- Application Usage Menu Item -->
                        <a id="menu-app-logs" onclick="freshAdmin.showView('app-logs')" style="
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
                                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                                <rect x="9" y="9" width="6" height="6"/>
                            </svg>
                            Application Usage
                        </a>
                        
                        <!-- Inactivity Logs Menu Item -->
                        <a id="menu-inactivity-logs" onclick="freshAdmin.showView('inactivity-logs')" style="
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
                                <circle cx="12" cy="12" r="10"/>
                                <polyline points="12,6 12,12 16,14"/>
                            </svg>
                            Inactivity Logs
                        </a>
                        
                        <!-- Screenshots Menu Item -->
                        <a id="menu-screenshots" onclick="freshAdmin.showView('screenshots')" style="
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
                                <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/>
                                <circle cx="12" cy="13" r="4"/>
                            </svg>
                            Screenshots
                        </a>
                    </nav>
                    
                    <!-- SYSTEM Section -->
                    <div style="color: rgba(255, 255, 255, 0.5); font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin: 24px 0 12px 0;">SYSTEM</div>
                    <nav>
                        
                        <!-- System Settings Menu Item -->
                        <a id="menu-system-settings" onclick="freshAdmin.showView('system-settings')" style="
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
                                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1 1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
                            </svg>
                            System Settings
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
                
                <!-- Live Systems View -->
                <div id="fresh-live-systems-view" class="fresh-view" style="display: none;">
                    ${this.createLiveSystemsView()}
                </div>
                
                <!-- Leave Management View -->
                <div id="fresh-leave-management-view" class="fresh-view" style="display: none;">
                    ${this.createLeaveManagementView()}
                </div>
                
                <!-- Departments View -->
                <div id="fresh-departments-view" class="fresh-view" style="display: none;">
                    ${this.createDepartmentsView()}
                </div>
                
                <!-- Web Logs View -->
                <div id="fresh-web-logs-view" class="fresh-view" style="display: none;">
                    ${this.createWebLogsView()}
                </div>
                
                <!-- Application Logs View -->
                <div id="fresh-app-logs-view" class="fresh-view" style="display: none;">
                    ${this.createAppLogsView()}
                </div>
                
                <!-- Inactivity Logs View -->
                <div id="fresh-inactivity-logs-view" class="fresh-view" style="display: none;">
                    ${this.createInactivityLogsView()}
                </div>
                
                <!-- Screenshots View -->
                <div id="fresh-screenshots-view" class="fresh-view" style="display: none;">
                    ${this.createScreenshotsView()}
                </div>
                
                <!-- System Settings View -->
                <div id="fresh-system-settings-view" class="fresh-view" style="display: none;">
                    ${this.createSystemSettingsView()}
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
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">#</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee Name</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee ID</th>
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
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Punch In</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Punch Out</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Hours Worked</th>
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
    
    createLiveSystemsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Live Systems</h1>
                <p style="color: #64748b; font-size: 16px;">Monitor active employee systems in real-time</p>
            </div>
            
            <!-- Live Systems Table -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Connected Systems</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">System Name</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">IP Address</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Last Activity</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Status</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="live-systems-body">
                            <tr>
                                <td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading live systems...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
    
    createLeaveManagementView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Leave Management</h1>
                <p style="color: #64748b; font-size: 16px;">Manage employee leave requests and approvals</p>
            </div>
            
            <!-- Leave Requests -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Leave Requests</h3>
                </div>
                <div style="padding: 24px; text-align: center; color: #64748b;">
                    Leave Management functionality will be integrated with your HR system
                </div>
            </div>
        `;
    },
    
    createDepartmentsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Departments</h1>
                <p style="color: #64748b; font-size: 16px;">Manage organizational departments and structure</p>
            </div>
            
            <!-- Departments List -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Department List</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Department</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Total Employees</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Active Today</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="departments-body">
                            <tr>
                                <td colspan="4" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading departments...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
    
    createWebLogsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Web Browsing Logs</h1>
                <p style="color: #64748b; font-size: 16px;">Monitor employee web browsing activity and productivity</p>
            </div>
            
            <!-- Web Logs Table -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Recent Web Activity</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Website</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Time Spent</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Category</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Timestamp</th>
                            </tr>
                        </thead>
                        <tbody id="web-logs-body">
                            <tr>
                                <td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading web browsing logs...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
    
    createAppLogsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Application Usage</h1>
                <p style="color: #64748b; font-size: 16px;">Track application usage and productivity metrics</p>
            </div>
            
            <!-- Application Logs Table -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Application Activity</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Application</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Usage Time</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Productivity Score</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Last Used</th>
                            </tr>
                        </thead>
                        <tbody id="app-logs-body">
                            <tr>
                                <td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading application usage logs...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
    
    createInactivityLogsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Inactivity Logs</h1>
                <p style="color: #64748b; font-size: 16px;">Monitor employee inactivity periods and idle time</p>
            </div>
            
            <!-- Inactivity Logs Table -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Inactivity Records</h3>
                </div>
                <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Employee</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Start Time</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Duration</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Reason</th>
                                <th style="padding: 16px; text-align: left; font-weight: 600; color: #475569;">Status</th>
                            </tr>
                        </thead>
                        <tbody id="inactivity-logs-body">
                            <tr>
                                <td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">
                                    Loading inactivity logs...
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },
    
    createScreenshotsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">Screenshots</h1>
                <p style="color: #64748b; font-size: 16px;">View employee desktop screenshots for productivity monitoring</p>
            </div>
            
            <!-- Screenshot Filters -->
            <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); margin-bottom: 24px;">
                <div style="display: grid; grid-template-columns: 1fr 1fr 1fr auto; gap: 16px; align-items: end;">
                    <div>
                        <label style="display: block; font-weight: 500; color: #374151; margin-bottom: 8px;">Employee</label>
                        <select id="screenshot-employee" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;">
                            <option value="">All Employees</option>
                        </select>
                    </div>
                    <div>
                        <label style="display: block; font-weight: 500; color: #374151; margin-bottom: 8px;">Date</label>
                        <input type="date" id="screenshot-date" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;">
                    </div>
                    <div>
                        <label style="display: block; font-weight: 500; color: #374151; margin-bottom: 8px;">Time Range</label>
                        <select id="screenshot-time" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;">
                            <option value="">All Day</option>
                            <option value="morning">Morning (9AM-12PM)</option>
                            <option value="afternoon">Afternoon (12PM-6PM)</option>
                            <option value="evening">Evening (6PM-9PM)</option>
                        </select>
                    </div>
                    <button onclick="freshAdmin.loadScreenshots()" style="padding: 12px 24px; background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                        Load Screenshots
                    </button>
                </div>
            </div>
            
            <!-- Screenshots Grid -->
            <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                <div style="padding: 24px; border-bottom: 1px solid #e2e8f0;">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b;">Screenshot Gallery</h3>
                </div>
                <div id="screenshots-grid" style="padding: 24px;">
                    <div style="text-align: center; color: #64748b; padding: 40px;">
                        Select filters above to load screenshots
                    </div>
                </div>
            </div>
        `;
    },
    
    createSystemSettingsView() {
        return `
            <div style="margin-bottom: 24px;">
                <h1 style="font-size: 32px; font-weight: 700; color: #1e293b; margin-bottom: 8px;">System Settings</h1>
                <p style="color: #64748b; font-size: 16px;">Configure system preferences and monitoring settings</p>
            </div>
            
            <!-- Settings Categories -->
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 24px;">
                <!-- Monitoring Settings -->
                <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b; margin-bottom: 16px;">Monitoring Settings</h3>
                    <div style="space-y: 12px;">
                        <div style="margin-bottom: 12px;">
                            <label style="display: flex; align-items: center; cursor: pointer;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                <span style="color: #374151;">Enable Screenshot Monitoring</span>
                            </label>
                        </div>
                        <div style="margin-bottom: 12px;">
                            <label style="display: flex; align-items: center; cursor: pointer;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                <span style="color: #374151;">Track Application Usage</span>
                            </label>
                        </div>
                        <div style="margin-bottom: 12px;">
                            <label style="display: flex; align-items: center; cursor: pointer;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                <span style="color: #374151;">Monitor Web Browsing</span>
                            </label>
                        </div>
                    </div>
                </div>
                
                <!-- Notification Settings -->
                <div style="background: white; padding: 24px; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <h3 style="font-size: 18px; font-weight: 600; color: #1e293b; margin-bottom: 16px;">Notifications</h3>
                    <div style="space-y: 12px;">
                        <div style="margin-bottom: 12px;">
                            <label style="display: flex; align-items: center; cursor: pointer;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                <span style="color: #374151;">System Alerts</span>
                            </label>
                        </div>
                        <div style="margin-bottom: 12px;">
                            <label style="display: flex; align-items: center; cursor: pointer;">
                                <input type="checkbox" style="margin-right: 8px;">
                                <span style="color: #374151;">Email Reports</span>
                            </label>
                        </div>
                        <div style="margin-bottom: 12px;">
                            <label style="display: flex; align-items: center; cursor: pointer;">
                                <input type="checkbox" style="margin-right: 8px;">
                                <span style="color: #374151;">Daily Summaries</span>
                            </label>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },
    
    // Navigation function
    showView(viewName) {
        console.log('Navigating to:', viewName);
        
        // Hide all views
        const allViews = document.querySelectorAll('.fresh-view');
        allViews.forEach(view => {
            view.style.display = 'none';
        });
        
        // Show selected view
        const targetView = document.getElementById(`fresh-${viewName}-view`);
        if (targetView) {
            targetView.style.display = 'block';
        }
        
        // Update menu active state
        const allMenuItems = document.querySelectorAll('[id^="menu-"]');
        allMenuItems.forEach(item => {
            item.style.background = 'transparent';
            item.style.color = 'rgba(255, 255, 255, 0.7)';
        });
        
        const activeMenuItem = document.getElementById(`menu-${viewName}`);
        if (activeMenuItem) {
            activeMenuItem.style.background = 'rgba(59, 130, 246, 0.1)';
            activeMenuItem.style.color = 'white';
        }
        
        this.currentView = viewName;
        
        // Load data based on view
        if (viewName === 'employees') {
            this.loadEmployees();
        } else if (viewName === 'attendance') {
            this.loadAttendance();
        } else if (viewName === 'live-systems') {
            this.loadLiveSystems();
        } else if (viewName === 'departments') {
            this.loadDepartments();
        } else if (viewName === 'web-logs') {
            this.loadWebLogs();
        } else if (viewName === 'app-logs') {
            this.loadAppLogs();
        } else if (viewName === 'inactivity-logs') {
            this.loadInactivityLogs();
        } else if (viewName === 'screenshots') {
            this.loadScreenshots();
        }
    },
    
    // Load dashboard data
    loadDashboardData() {
        // Load dashboard stats
        fetch('/api.php?action=get_dashboard_stats')
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    document.getElementById('total-employees').textContent = data.stats.total_employees || '0';
                    document.getElementById('present-today').textContent = data.stats.present_today || '0';
                    document.getElementById('active-systems').textContent = data.stats.active_systems || '0';
                }
            })
            .catch(error => {
                console.error('Error loading dashboard stats:', error);
                document.getElementById('total-employees').textContent = '0';
                document.getElementById('present-today').textContent = '0';
                document.getElementById('active-systems').textContent = '0';
            });
            
        // Load recent activity
        fetch('/api.php?action=get_recent_activity')
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    const activityHtml = data.activities.map(activity => `
                        <div style="padding: 12px 0; border-bottom: 1px solid #f1f5f9; last:border-bottom: none;">
                            <div style="font-weight: 500; color: #1e293b; margin-bottom: 4px;">${activity.employee_name}</div>
                            <div style="font-size: 14px; color: #64748b;">${activity.activity} - ${activity.timestamp}</div>
                        </div>
                    `).join('');
                    document.getElementById('recent-activity').innerHTML = activityHtml || '<div style="color: #64748b;">No recent activities</div>';
                } else {
                    document.getElementById('recent-activity').innerHTML = '<div style="color: #64748b;">No recent activities</div>';
                }
            })
            .catch(error => {
                console.error('Error loading recent activity:', error);
                document.getElementById('recent-activity').innerHTML = '<div style="color: #64748b;">Error loading activities</div>';
            });
    },
    
    // Load employees from company_employees database
    loadEmployees() {
        const tbody = document.getElementById('employee-list-body');
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">Loading employees...</td></tr>';
        
        const companyName = localStorage.getItem('company_name') || 'WIZONE IT NETWORK INDIA PVT LTD';
        
        // Fetch employees using existing API endpoint
        const url = `/api.php?action=get_employees&company_name=${encodeURIComponent(companyName)}`;
        
        fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.success && data.data) {
                const employeeHtml = data.data.map((emp, index) => {
                    const isActive = emp.is_active && emp.is_online;
                    const lastSeen = emp.last_heartbeat ? new Date(emp.last_heartbeat).toLocaleString('en-IN') : 'Never';
                    
                    return `
                        <tr style="border-bottom: 1px solid #f1f5f9;">
                            <td style="padding: 16px; color: #1e293b;">${index + 1}</td>
                            <td style="padding: 16px; color: #1e293b; font-weight: 500;">${emp.display_name || emp.full_name || 'N/A'}</td>
                            <td style="padding: 16px; color: #64748b;">${emp.employee_id || 'N/A'}</td>
                            <td style="padding: 16px; color: #64748b;">${emp.department || 'N/A'}</td>
                            <td style="padding: 16px;">
                                <span style="
                                    padding: 4px 8px; 
                                    border-radius: 12px; 
                                    font-size: 12px; 
                                    font-weight: 500;
                                    background: ${isActive ? '#dcfce7' : '#fef2f2'};
                                    color: ${isActive ? '#166534' : '#dc2626'};
                                ">${isActive ? 'Online' : 'Offline'}</span>
                            </td>
                            <td style="padding: 16px;">
                                <button onclick="freshAdmin.viewEmployeeDetails('${emp.employee_id}', '${emp.display_name || emp.full_name}')" style="
                                    padding: 6px 12px; 
                                    background: #f1f5f9; 
                                    color: #475569; 
                                    border: none; 
                                    border-radius: 6px; 
                                    cursor: pointer; 
                                    font-size: 12px;
                                    margin-right: 8px;
                                ">View Details</button>
                                <button onclick="freshAdmin.viewEmployeeActivity('${emp.employee_id}')" style="
                                    padding: 6px 12px; 
                                    background: #eff6ff; 
                                    color: #1d4ed8; 
                                    border: none; 
                                    border-radius: 6px; 
                                    cursor: pointer; 
                                    font-size: 12px;
                                ">Punch Logs</button>
                            </td>
                        </tr>
                    `;
                }).join('');
                
                tbody.innerHTML = employeeHtml;
            } else {
                tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">No employees found</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading employees:', error);
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #dc2626;">Error loading employees</td></tr>';
        });
    },
    
    // Load attendance from punch_log_consolidated database
    loadAttendance() {
        const tbody = document.getElementById('attendance-list-body');
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">Loading attendance records...</td></tr>';
        
        // Set default dates to show more data - last 30 days to today
        const today = new Date();
        const startDate = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000); // 30 days ago
        document.getElementById('start-date').value = startDate.toISOString().split('T')[0];
        document.getElementById('end-date').value = today.toISOString().split('T')[0];
        
        this.fetchAttendanceData();
    },
    
    // Fetch attendance data from punch_log_consolidated database
    fetchAttendanceData() {
        const startDate = document.getElementById('start-date').value;
        const endDate = document.getElementById('end-date').value;
        const companyName = localStorage.getItem('company_name') || 'WIZONE IT NETWORK INDIA PVT LTD';
        const tbody = document.getElementById('attendance-list-body');
        
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">Loading attendance records...</td></tr>';
        
        // Build URL with parameters for existing get_attendance endpoint
        const url = `/api.php?action=get_attendance&company_name=${encodeURIComponent(companyName)}&start_date=${startDate}&end_date=${endDate}`;
        
        fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.success && data.data) {
                const attendanceHtml = data.data.map(record => {
                    const punchInTime = record.punch_in_time ? new Date(record.punch_in_time).toLocaleString('en-IN') : 'N/A';
                    const punchOutTime = record.punch_out_time ? new Date(record.punch_out_time).toLocaleString('en-IN') : 'Not punched out';
                    const date = record.punch_in_time ? new Date(record.punch_in_time).toLocaleDateString('en-IN') : 'N/A';
                    
                    // Calculate hours worked from duration in seconds
                    let hoursWorked = 'N/A';
                    if (record.total_work_duration_seconds && record.total_work_duration_seconds > 0) {
                        const hours = Math.floor(record.total_work_duration_seconds / 3600);
                        const minutes = Math.floor((record.total_work_duration_seconds % 3600) / 60);
                        hoursWorked = `${hours}h ${minutes}m`;
                    }
                    
                    const status = record.punch_out_time ? 'Complete' : 'In Progress';
                    
                    return `
                        <tr style="border-bottom: 1px solid #f1f5f9;">
                            <td style="padding: 16px; color: #1e293b; font-weight: 500;">${record.display_name || record.username || 'N/A'}</td>
                            <td style="padding: 16px; color: #64748b;">${date}</td>
                            <td style="padding: 16px; color: #64748b;">${punchInTime}</td>
                            <td style="padding: 16px; color: #64748b;">${punchOutTime}</td>
                            <td style="padding: 16px; color: #64748b;">${hoursWorked}</td>
                            <td style="padding: 16px;">
                                <span style="
                                    padding: 4px 8px; 
                                    border-radius: 12px; 
                                    font-size: 12px; 
                                    font-weight: 500;
                                    background: ${status === 'Complete' ? '#dcfce7' : '#fff3cd'};
                                    color: ${status === 'Complete' ? '#166534' : '#856404'};
                                ">${status}</span>
                            </td>
                        </tr>
                    `;
                }).join('');
                
                tbody.innerHTML = attendanceHtml;
            } else {
                tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">No attendance records found for selected date range</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading attendance:', error);
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #dc2626;">Error loading attendance records</td></tr>';
        });
    },
    
    // Helper function to check if activity is recent (within last 24 hours)
    isRecentActivity(activityTime) {
        const now = new Date();
        const activity = new Date(activityTime);
        const diffHours = (now - activity) / (1000 * 60 * 60);
        return diffHours <= 24;
    },
    
    // Utility functions
    addEmployee() {
        alert('Add Employee functionality - This would connect to your employee registration system');
    },
    
    viewEmployeeDetails(employeeId, employeeName) {
        alert(`Employee Details\n\nEmployee ID: ${employeeId}\nName: ${employeeName}\n\nThis will show detailed employee information including:\n- Contact information\n- Department details\n- System access history\n- Performance metrics`);
    },
    
    viewEmployeeActivity(employeeId) {
        const companyName = localStorage.getItem('company_name') || 'WIZONE IT NETWORK INDIA PVT LTD';
        alert(`Opening punch log history for Employee ID: ${employeeId}\n\nThis will show:\n- Daily punch in/out times\n- Total hours worked\n- Break durations\n- System access logs\n\nData from punch_log_consolidated table for company: ${companyName}`);
    },
    
    exportAttendance() {
        const startDate = document.getElementById('start-date').value;
        const endDate = document.getElementById('end-date').value;
        const companyName = localStorage.getItem('company_name') || 'WIZONE IT NETWORK INDIA PVT LTD';
        
        if (!startDate || !endDate) {
            alert('Please select both start and end dates before exporting');
            return;
        }
        
        // Create CSV export URL with parameters (this would need to be implemented in server.js)
        const exportUrl = `/export_attendance?company_name=${encodeURIComponent(companyName)}&start_date=${startDate}&end_date=${endDate}`;
        
        alert(`CSV Export initiated for:\nCompany: ${companyName}\nDate Range: ${startDate} to ${endDate}\n\nExport URL: ${exportUrl}\n\nNote: The export_attendance endpoint needs to be implemented in server.js`);
        
        // Uncomment when export endpoint is ready:
        // const link = document.createElement('a');
        // link.href = exportUrl;
        // link.download = `attendance_${startDate}_to_${endDate}.csv`;
        // document.body.appendChild(link);
        // link.click();
        // document.body.removeChild(link);
    },
    
    filterAttendance() {
        const startDate = document.getElementById('start-date').value;
        const endDate = document.getElementById('end-date').value;
        
        if (!startDate || !endDate) {
            alert('Please select both start and end dates');
            return;
        }
        
        if (new Date(startDate) > new Date(endDate)) {
            alert('Start date cannot be after end date');
            return;
        }
        
        this.fetchAttendanceData();
    },
    
    logout() {
        if (confirm('Are you sure you want to logout?')) {
            window.location.reload();
        }
    },
    
    // Load live systems data
    loadLiveSystems() {
        const tbody = document.getElementById('live-systems-body');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">Loading live systems...</td></tr>';
        
        const companyName = localStorage.getItem('company_name') || 'WIZONE IT NETWORK INDIA PVT LTD';
        
        // For now, show sample data - this would connect to your live systems monitoring endpoint
        setTimeout(() => {
            const sampleSystems = [
                {
                    employee: 'John Smith',
                    systemName: 'DESKTOP-ABC123',
                    ipAddress: '192.168.1.45',
                    lastActivity: new Date(Date.now() - 300000).toLocaleString(), // 5 min ago
                    status: 'Online'
                },
                {
                    employee: 'Mary Johnson',
                    systemName: 'LAPTOP-XYZ789',
                    ipAddress: '192.168.1.67',
                    lastActivity: new Date(Date.now() - 120000).toLocaleString(), // 2 min ago
                    status: 'Online'
                }
            ];
            
            const systemsHtml = sampleSystems.map(system => `
                <tr style="border-bottom: 1px solid #f1f5f9;">
                    <td style="padding: 16px; color: #1e293b; font-weight: 500;">${system.employee}</td>
                    <td style="padding: 16px; color: #64748b;">${system.systemName}</td>
                    <td style="padding: 16px; color: #64748b;">${system.ipAddress}</td>
                    <td style="padding: 16px; color: #64748b;">${system.lastActivity}</td>
                    <td style="padding: 16px;">
                        <span style="padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: 500; background: #dcfce7; color: #166534;">${system.status}</span>
                    </td>
                    <td style="padding: 16px;">
                        <button onclick="alert('Remote view functionality')" style="padding: 6px 12px; background: #eff6ff; color: #1d4ed8; border: none; border-radius: 6px; cursor: pointer; font-size: 12px; margin-right: 8px;">View Screen</button>
                        <button onclick="alert('Remote control functionality')" style="padding: 6px 12px; background: #f0fdf4; color: #166534; border: none; border-radius: 6px; cursor: pointer; font-size: 12px;">Control</button>
                    </td>
                </tr>
            `).join('');
            
            tbody.innerHTML = systemsHtml || '<tr><td colspan="6" style="padding: 24px; text-align: center; color: #64748b;">No active systems found</td></tr>';
        }, 500);
    },
    
    // Load departments data
    loadDepartments() {
        const tbody = document.getElementById('departments-body');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="4" style="padding: 24px; text-align: center; color: #64748b;">Loading departments...</td></tr>';
        
        // Get unique departments from employees
        const companyName = localStorage.getItem('company_name') || 'WIZONE IT NETWORK INDIA PVT LTD';
        const url = `/api.php?action=get_employees&company_name=${encodeURIComponent(companyName)}`;
        
        fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.success && data.data) {
                // Group employees by department
                const departments = {};
                data.data.forEach(emp => {
                    const dept = emp.department || 'Unassigned';
                    if (!departments[dept]) {
                        departments[dept] = { total: 0, active: 0 };
                    }
                    departments[dept].total++;
                    if (emp.is_active && emp.is_online) {
                        departments[dept].active++;
                    }
                });
                
                const deptHtml = Object.keys(departments).map(deptName => `
                    <tr style="border-bottom: 1px solid #f1f5f9;">
                        <td style="padding: 16px; color: #1e293b; font-weight: 500;">${deptName}</td>
                        <td style="padding: 16px; color: #64748b;">${departments[deptName].total}</td>
                        <td style="padding: 16px; color: #64748b;">${departments[deptName].active}</td>
                        <td style="padding: 16px;">
                            <button onclick="freshAdmin.viewDepartmentDetails('${deptName}')" style="padding: 6px 12px; background: #f1f5f9; color: #475569; border: none; border-radius: 6px; cursor: pointer; font-size: 12px;">View Details</button>
                        </td>
                    </tr>
                `).join('');
                
                tbody.innerHTML = deptHtml || '<tr><td colspan="4" style="padding: 24px; text-align: center; color: #64748b;">No departments found</td></tr>';
            } else {
                tbody.innerHTML = '<tr><td colspan="4" style="padding: 24px; text-align: center; color: #64748b;">No departments found</td></tr>';
            }
        })
        .catch(error => {
            console.error('Error loading departments:', error);
            tbody.innerHTML = '<tr><td colspan="4" style="padding: 24px; text-align: center; color: #dc2626;">Error loading departments</td></tr>';
        });
    },
    
    // Load web logs data
    loadWebLogs() {
        const tbody = document.getElementById('web-logs-body');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">Loading web logs...</td></tr>';
        
        // Sample web browsing data - this would connect to your web monitoring endpoint
        setTimeout(() => {
            const sampleLogs = [
                {
                    employee: 'John Smith',
                    website: 'github.com',
                    timeSpent: '45 minutes',
                    category: 'Productive',
                    timestamp: new Date(Date.now() - 1800000).toLocaleString()
                },
                {
                    employee: 'Mary Johnson',
                    website: 'stackoverflow.com',
                    timeSpent: '23 minutes',
                    category: 'Productive',
                    timestamp: new Date(Date.now() - 900000).toLocaleString()
                }
            ];
            
            const logsHtml = sampleLogs.map(log => `
                <tr style="border-bottom: 1px solid #f1f5f9;">
                    <td style="padding: 16px; color: #1e293b; font-weight: 500;">${log.employee}</td>
                    <td style="padding: 16px; color: #64748b;">${log.website}</td>
                    <td style="padding: 16px; color: #64748b;">${log.timeSpent}</td>
                    <td style="padding: 16px;">
                        <span style="padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: 500; background: #dcfce7; color: #166534;">${log.category}</span>
                    </td>
                    <td style="padding: 16px; color: #64748b;">${log.timestamp}</td>
                </tr>
            `).join('');
            
            tbody.innerHTML = logsHtml || '<tr><td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">No web activity found</td></tr>';
        }, 500);
    },
    
    // Load application logs data
    loadAppLogs() {
        const tbody = document.getElementById('app-logs-body');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">Loading application logs...</td></tr>';
        
        // Sample application usage data
        setTimeout(() => {
            const sampleApps = [
                {
                    employee: 'John Smith',
                    application: 'Visual Studio Code',
                    usageTime: '3h 45m',
                    productivityScore: '95%',
                    lastUsed: new Date(Date.now() - 600000).toLocaleString()
                },
                {
                    employee: 'Mary Johnson',
                    application: 'Microsoft Word',
                    usageTime: '2h 12m',
                    productivityScore: '88%',
                    lastUsed: new Date(Date.now() - 300000).toLocaleString()
                }
            ];
            
            const appsHtml = sampleApps.map(app => `
                <tr style="border-bottom: 1px solid #f1f5f9;">
                    <td style="padding: 16px; color: #1e293b; font-weight: 500;">${app.employee}</td>
                    <td style="padding: 16px; color: #64748b;">${app.application}</td>
                    <td style="padding: 16px; color: #64748b;">${app.usageTime}</td>
                    <td style="padding: 16px;">
                        <span style="padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: 500; background: #dcfce7; color: #166534;">${app.productivityScore}</span>
                    </td>
                    <td style="padding: 16px; color: #64748b;">${app.lastUsed}</td>
                </tr>
            `).join('');
            
            tbody.innerHTML = appsHtml || '<tr><td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">No application usage found</td></tr>';
        }, 500);
    },
    
    // Load inactivity logs data
    loadInactivityLogs() {
        const tbody = document.getElementById('inactivity-logs-body');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">Loading inactivity logs...</td></tr>';
        
        // Sample inactivity data
        setTimeout(() => {
            const sampleInactivity = [
                {
                    employee: 'John Smith',
                    startTime: new Date(Date.now() - 7200000).toLocaleString(),
                    duration: '15 minutes',
                    reason: 'Break',
                    status: 'Resolved'
                },
                {
                    employee: 'Mary Johnson',
                    startTime: new Date(Date.now() - 3600000).toLocaleString(),
                    duration: '8 minutes',
                    reason: 'Meeting',
                    status: 'Resolved'
                }
            ];
            
            const inactivityHtml = sampleInactivity.map(item => `
                <tr style="border-bottom: 1px solid #f1f5f9;">
                    <td style="padding: 16px; color: #1e293b; font-weight: 500;">${item.employee}</td>
                    <td style="padding: 16px; color: #64748b;">${item.startTime}</td>
                    <td style="padding: 16px; color: #64748b;">${item.duration}</td>
                    <td style="padding: 16px; color: #64748b;">${item.reason}</td>
                    <td style="padding: 16px;">
                        <span style="padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: 500; background: #dcfce7; color: #166534;">${item.status}</span>
                    </td>
                </tr>
            `).join('');
            
            tbody.innerHTML = inactivityHtml || '<tr><td colspan="5" style="padding: 24px; text-align: center; color: #64748b;">No inactivity records found</td></tr>';
        }, 500);
    },
    
    // Load screenshots data
    loadScreenshots() {
        const grid = document.getElementById('screenshots-grid');
        if (!grid) return;
        
        grid.innerHTML = '<div style="text-align: center; color: #64748b; padding: 40px;">Loading screenshots...</div>';
        
        // Sample screenshots data
        setTimeout(() => {
            const sampleScreenshots = [
                {
                    employee: 'John Smith',
                    timestamp: new Date(Date.now() - 600000).toLocaleString(),
                    thumbnail: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjE1MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjFmNWY5Ii8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxNCIgZmlsbD0iIzY0NzQ4YiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPiNTY3JlZW5zaG90IDE8L3RleHQ+PC9zdmc+'
                },
                {
                    employee: 'Mary Johnson',
                    timestamp: new Date(Date.now() - 1200000).toLocaleString(),
                    thumbnail: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjE1MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjFmNWY5Ii8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxNCIgZmlsbD0iIzY0NzQ4YiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPiNTY3JlZW5zaG90IDI8L3RleHQ+PC9zdmc+'
                }
            ];
            
            const screenshotsHtml = sampleScreenshots.map(shot => `
                <div style="display: inline-block; margin: 12px; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; background: white;">
                    <img src="${shot.thumbnail}" style="width: 200px; height: 150px; object-fit: cover; cursor: pointer;" onclick="alert('Full size screenshot viewer')">
                    <div style="padding: 12px;">
                        <div style="font-weight: 500; color: #1e293b; margin-bottom: 4px;">${shot.employee}</div>
                        <div style="font-size: 12px; color: #64748b;">${shot.timestamp}</div>
                    </div>
                </div>
            `).join('');
            
            grid.innerHTML = screenshotsHtml || '<div style="text-align: center; color: #64748b; padding: 40px;">No screenshots found for selected criteria</div>';
        }, 500);
    },
    
    // Helper functions for new views
    viewDepartmentDetails(deptName) {
        alert(`Department Details: ${deptName}\n\nThis will show:\n- Employee list for this department\n- Department performance metrics\n- Active projects and assignments\n- Department-specific settings`);
    }
};

// Initialize immediately when script loads
document.addEventListener('DOMContentLoaded', () => {
    // Force initialization regardless of existing content
    console.log('Fresh Admin System: DOM ready, initializing...');
    setTimeout(() => {
        freshAdmin.init();
    }, 500); // Small delay to ensure all other scripts load
});

// Also try to initialize immediately if DOM is already ready
if (document.readyState !== 'loading') {
    console.log('Fresh Admin System: Document already ready, initializing...');
    setTimeout(() => {
        freshAdmin.init();
    }, 100);
}

// Override window load as backup
window.addEventListener('load', () => {
    setTimeout(() => {
        console.log('Fresh Admin System: Window loaded, force initializing...');
        freshAdmin.init();
    }, 1000);
});