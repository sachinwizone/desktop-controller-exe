/**
 * ENHANCED WEB DASHBOARD NAVIGATION
 * Complete system control and management interface
 * Features: System Control, Chat, Users, Applications, Settings, Reports
 */

class EnhancedDashboardNavigation {
    constructor(apiBaseUrl, companyName, userName) {
        this.apiBaseUrl = apiBaseUrl;
        this.companyName = companyName;
        this.userName = userName;
        this.currentSection = 'dashboard';
        this.initNavigation();
    }

    initNavigation() {
        this.setupNavMenu();
        this.setupEventListeners();
    }

    setupNavMenu() {
        const navHTML = `
            <nav class="enhanced-dashboard-nav">
                <div class="nav-header">
                    <h2>System Control Panel</h2>
                    <span class="user-info">${this.userName} @ ${this.companyName}</span>
                </div>
                
                <div class="nav-items">
                    <!-- MAIN SECTION -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">📊 DASHBOARD</h3>
                        <a class="nav-item" data-section="dashboard" href="#dashboard">
                            <span class="icon">📈</span> Overview
                        </a>
                        <a class="nav-item" data-section="live-stats" href="#live-stats">
                            <span class="icon">⚡</span> Live Statistics
                        </a>
                    </div>

                    <!-- DEVICES & SYSTEMS -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">💻 DEVICES & SYSTEMS</h3>
                        <a class="nav-item" data-section="all-devices" href="#all-devices">
                            <span class="icon">🖥️</span> All Devices
                        </a>
                        <a class="nav-item" data-section="system-info" href="#system-info">
                            <span class="icon">ℹ️</span> System Information
                        </a>
                        <a class="nav-item" data-section="device-monitor" href="#device-monitor">
                            <span class="icon">📡</span> Device Monitor
                        </a>
                        <a class="nav-item" data-section="offline-devices" href="#offline-devices">
                            <span class="icon">❌</span> Offline Devices
                        </a>
                    </div>

                    <!-- USERS MANAGEMENT -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">👥 USERS MANAGEMENT</h3>
                        <a class="nav-item" data-section="registered-users" href="#registered-users">
                            <span class="icon">✓</span> Registered Users
                        </a>
                        <a class="nav-item" data-section="active-users" href="#active-users">
                            <span class="icon">🟢</span> Active Users
                        </a>
                        <a class="nav-item" data-section="user-devices" href="#user-devices">
                            <span class="icon">📋</span> User Devices
                        </a>
                        <a class="nav-item" data-section="user-activity" href="#user-activity">
                            <span class="icon">📊</span> User Activity
                        </a>
                    </div>

                    <!-- APPLICATIONS MANAGEMENT -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">📦 APPLICATIONS</h3>
                        <a class="nav-item" data-section="installed-apps" href="#installed-apps">
                            <span class="icon">📦</span> Installed Apps
                        </a>
                        <a class="nav-item" data-section="running-processes" href="#running-processes">
                            <span class="icon">⚙️</span> Running Processes
                        </a>
                        <a class="nav-item" data-section="app-control" href="#app-control">
                            <span class="icon">🎮</span> Application Control
                        </a>
                    </div>

                    <!-- SYSTEM CONTROL -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">⚙️ SYSTEM CONTROL</h3>
                        <a class="nav-item" data-section="remote-commands" href="#remote-commands">
                            <span class="icon">💾</span> Remote Commands
                        </a>
                        <a class="nav-item" data-section="system-settings" href="#system-settings">
                            <span class="icon">⚙️</span> System Settings
                        </a>
                        <a class="nav-item" data-section="network-control" href="#network-control">
                            <span class="icon">🌐</span> Network Control
                        </a>
                        <a class="nav-item" data-section="power-control" href="#power-control">
                            <span class="icon">⚡</span> Power Control
                        </a>
                    </div>

                    <!-- COMMUNICATION -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">💬 COMMUNICATION</h3>
                        <a class="nav-item" data-section="chat" href="#chat">
                            <span class="icon">💬</span> Chat
                        </a>
                        <a class="nav-item" data-section="notifications" href="#notifications">
                            <span class="icon">🔔</span> Notifications
                        </a>
                        <a class="nav-item" data-section="alerts" href="#alerts">
                            <span class="icon">⚠️</span> Alerts & Logs
                        </a>
                    </div>

                    <!-- REPORTS & ANALYTICS -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">📊 REPORTS & ANALYTICS</h3>
                        <a class="nav-item" data-section="usage-reports" href="#usage-reports">
                            <span class="icon">📈</span> Usage Reports
                        </a>
                        <a class="nav-item" data-section="activity-logs" href="#activity-logs">
                            <span class="icon">📝</span> Activity Logs
                        </a>
                        <a class="nav-item" data-section="compliance" href="#compliance">
                            <span class="icon">✅</span> Compliance
                        </a>
                    </div>

                    <!-- SETTINGS -->
                    <div class="nav-group">
                        <h3 class="nav-group-title">⚙️ SETTINGS</h3>
                        <a class="nav-item" data-section="company-settings" href="#company-settings">
                            <span class="icon">🏢</span> Company Settings
                        </a>
                        <a class="nav-item" data-section="access-control" href="#access-control">
                            <span class="icon">🔐</span> Access Control
                        </a>
                        <a class="nav-item" data-section="preferences" href="#preferences">
                            <span class="icon">⚙️</span> Preferences
                        </a>
                    </div>
                </div>

                <div class="nav-footer">
                    <button class="nav-logout-btn">Logout</button>
                </div>
            </nav>
        `;

        const navContainer = document.querySelector('.dashboard-navbar') || document.body.insertAdjacentElement('afterbegin', document.createElement('div'));
        navContainer.innerHTML = navHTML;
    }

    setupEventListeners() {
        // Navigation item clicks
        document.querySelectorAll('.nav-item').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const section = item.dataset.section;
                this.loadSection(section);
                
                // Update active state
                document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
                item.classList.add('active');
            });
        });

        // Logout
        document.querySelector('.nav-logout-btn').addEventListener('click', () => {
            this.logout();
        });
    }

    async loadSection(section) {
        this.currentSection = section;
        const contentArea = document.querySelector('.dashboard-content') || this.createContentArea();
        
        try {
            switch(section) {
                case 'dashboard':
                    await this.loadDashboard(contentArea);
                    break;
                case 'all-devices':
                    await this.loadAllDevices(contentArea);
                    break;
                case 'system-info':
                    await this.loadSystemInfo(contentArea);
                    break;
                case 'registered-users':
                    await this.loadRegisteredUsers(contentArea);
                    break;
                case 'active-users':
                    await this.loadActiveUsers(contentArea);
                    break;
                case 'installed-apps':
                    await this.loadInstalledApps(contentArea);
                    break;
                case 'running-processes':
                    await this.loadRunningProcesses(contentArea);
                    break;
                case 'remote-commands':
                    await this.loadRemoteCommands(contentArea);
                    break;
                case 'chat':
                    await this.loadChat(contentArea);
                    break;
                case 'user-activity':
                    await this.loadUserActivity(contentArea);
                    break;
                default:
                    contentArea.innerHTML = `<h2>Loading ${section}...</h2>`;
            }
        } catch (err) {
            contentArea.innerHTML = `<div class="error">Error loading section: ${err.message}</div>`;
        }
    }

    async loadDashboard(contentArea) {
        contentArea.innerHTML = `
            <div class="dashboard-overview">
                <h1>System Control Dashboard</h1>
                <div class="stats-grid">
                    <div class="stat-card">
                        <div class="stat-value" id="total-devices">0</div>
                        <div class="stat-label">Total Devices</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value" id="online-devices">0</div>
                        <div class="stat-label">Online</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value" id="offline-devices">0</div>
                        <div class="stat-label">Offline</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-value" id="total-users">0</div>
                        <div class="stat-label">Registered Users</div>
                    </div>
                </div>
                <div id="dashboard-content" class="loading">Loading data...</div>
            </div>
        `;

        try {
            const devicesRes = await fetch(`${this.apiBaseUrl}/api/system/get-devices?company_name=${this.companyName}`);
            const devicesData = await devicesRes.json();
            
            const usersRes = await fetch(`${this.apiBaseUrl}/api/system/get-all-registered-users?company_name=${this.companyName}`);
            const usersData = await usersRes.json();

            if (devicesData.success) {
                const total = devicesData.count || 0;
                const online = devicesData.data.filter(d => d.status === 'online').length;
                const offline = total - online;

                document.getElementById('total-devices').textContent = total;
                document.getElementById('online-devices').textContent = online;
                document.getElementById('offline-devices').textContent = offline;
            }

            if (usersData.success) {
                document.getElementById('total-users').textContent = usersData.total || 0;
            }

            document.getElementById('dashboard-content').innerHTML = '<p>Dashboard ready. Select a section from the menu.</p>';
        } catch (err) {
            console.error('Dashboard error:', err);
        }
    }

    async loadAllDevices(contentArea) {
        contentArea.innerHTML = `
            <div class="devices-section">
                <h2>All Registered Devices</h2>
                <div class="filter-bar">
                    <input type="text" placeholder="Search devices..." class="search-input" />
                    <button class="btn-refresh">Refresh</button>
                </div>
                <div id="devices-list" class="loading">Loading devices...</div>
            </div>
        `;

        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-devices?company_name=${this.companyName}`);
            const data = await res.json();

            if (data.success && data.data) {
                let html = '<div class="devices-table"><table>';
                html += '<tr><th>Hostname</th><th>Username</th><th>IP Address</th><th>Status</th><th>Last Seen</th><th>Actions</th></tr>';

                data.data.forEach(device => {
                    const status = device.status === 'online' ? '<span class="status-online">Online</span>' : '<span class="status-offline">Offline</span>';
                    html += `
                        <tr>
                            <td>${device.hostname}</td>
                            <td>${device.username}</td>
                            <td>${device.ip_address || 'N/A'}</td>
                            <td>${status}</td>
                            <td>${new Date(device.last_seen).toLocaleString()}</td>
                            <td>
                                <button class="btn-view" data-device-id="${device.device_id}">View</button>
                                <button class="btn-control" data-device-id="${device.device_id}">Control</button>
                            </td>
                        </tr>
                    `;
                });
                html += '</table></div>';
                document.getElementById('devices-list').innerHTML = html;
            }
        } catch (err) {
            console.error('Devices error:', err);
            document.getElementById('devices-list').innerHTML = `<div class="error">Error loading devices: ${err.message}</div>`;
        }
    }

    async loadRegisteredUsers(contentArea) {
        contentArea.innerHTML = `
            <div class="users-section">
                <h2>Registered Users</h2>
                <div id="users-list" class="loading">Loading users...</div>
            </div>
        `;

        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-all-registered-users?company_name=${this.companyName}`);
            const data = await res.json();

            if (data.success && data.data) {
                let html = '<div class="users-grid">';
                data.data.forEach(user => {
                    html += `
                        <div class="user-card">
                            <div class="user-name">${user._id}</div>
                            <div class="user-devices">Devices: ${user.count}</div>
                        </div>
                    `;
                });
                html += '</div>';
                document.getElementById('users-list').innerHTML = html;
            }
        } catch (err) {
            console.error('Users error:', err);
            document.getElementById('users-list').innerHTML = `<div class="error">Error loading users: ${err.message}</div>`;
        }
    }

    async loadSystemInfo(contentArea) {
        contentArea.innerHTML = `
            <div class="system-info-section">
                <h2>System Information</h2>
                <div class="device-selector">
                    <label>Select Device:</label>
                    <select id="device-selector" class="device-select">
                        <option value="">Loading devices...</option>
                    </select>
                </div>
                <div id="system-info-details" class="loading">Select a device to view details</div>
            </div>
        `;

        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-devices?company_name=${this.companyName}`);
            const data = await res.json();

            const select = document.getElementById('device-selector');
            select.innerHTML = '<option value="">Select a device</option>';
            
            if (data.data) {
                data.data.forEach(device => {
                    const option = document.createElement('option');
                    option.value = device.device_id;
                    option.textContent = `${device.hostname} (${device.username})`;
                    select.appendChild(option);
                });

                select.addEventListener('change', async (e) => {
                    if (e.target.value) {
                        await this.loadDeviceSystemInfo(e.target.value);
                    }
                });
            }
        } catch (err) {
            console.error('System info error:', err);
        }
    }

    async loadDeviceSystemInfo(deviceId) {
        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-system-info?device_id=${deviceId}`);
            const data = await res.json();

            if (data.success && data.data) {
                const info = data.data;
                let html = '<div class="system-info-detail">';
                html += `<h3>${info.Hostname} (${info.Username})</h3>`;
                html += '<table>';
                html += `<tr><td>OS:</td><td>${info.OSVersion}</td></tr>`;
                html += `<tr><td>Build:</td><td>${info.OSBuild}</td></tr>`;
                html += `<tr><td>Architecture:</td><td>${info.Architecture}</td></tr>`;
                html += `<tr><td>Processors:</td><td>${info.ProcessorCount}</td></tr>`;
                html += `<tr><td>RAM:</td><td>${(info.TotalMemory / (1024 * 1024 * 1024)).toFixed(2)} GB</td></tr>`;
                html += `<tr><td>Uptime:</td><td>${info.SystemUptime}</td></tr>`;
                html += '</table>';
                html += '</div>';

                document.getElementById('system-info-details').innerHTML = html;
            }
        } catch (err) {
            console.error('Device info error:', err);
            document.getElementById('system-info-details').innerHTML = `<div class="error">Error: ${err.message}</div>`;
        }
    }

    async loadRemoteCommands(contentArea) {
        contentArea.innerHTML = `
            <div class="remote-commands-section">
                <h2>Remote Commands</h2>
                <div class="command-form">
                    <div class="form-group">
                        <label>Select Device:</label>
                        <select id="command-device-selector" class="device-select">
                            <option value="">Loading devices...</option>
                        </select>
                    </div>
                    <div class="form-group">
                        <label>Command:</label>
                        <select id="command-selector">
                            <option value="">Select Command</option>
                            <option value="restart">Restart System</option>
                            <option value="shutdown">Shutdown</option>
                            <option value="lock">Lock Screen</option>
                            <option value="sleep">Sleep</option>
                            <option value="cmd">Execute Command</option>
                        </select>
                    </div>
                    <button class="btn-execute">Execute Command</button>
                </div>
                <div id="command-results"></div>
            </div>
        `;

        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-devices?company_name=${this.companyName}`);
            const data = await res.json();

            const select = document.getElementById('command-device-selector');
            select.innerHTML = '<option value="">Select a device</option>';
            
            if (data.data) {
                data.data.forEach(device => {
                    const option = document.createElement('option');
                    option.value = device.device_id;
                    option.textContent = `${device.hostname} (${device.username})`;
                    select.appendChild(option);
                });
            }

            document.querySelector('.btn-execute').addEventListener('click', async () => {
                const deviceId = document.getElementById('command-device-selector').value;
                const command = document.getElementById('command-selector').value;

                if (!deviceId || !command) {
                    alert('Please select device and command');
                    return;
                }

                await this.sendCommand(deviceId, command);
            });
        } catch (err) {
            console.error('Commands error:', err);
        }
    }

    async sendCommand(deviceId, command) {
        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/send-command`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    device_id: deviceId,
                    command: command,
                    issued_by: this.userName
                })
            });

            const data = await res.json();
            if (data.success) {
                document.getElementById('command-results').innerHTML = `<div class="success">Command sent successfully!</div>`;
            }
        } catch (err) {
            document.getElementById('command-results').innerHTML = `<div class="error">Error: ${err.message}</div>`;
        }
    }

    async loadChat(contentArea) {
        contentArea.innerHTML = `
            <div class="chat-section">
                <h2>System Chat</h2>
                <div id="chat-container"></div>
            </div>
        `;
        // ChatModule will handle chat display
    }

    async loadInstalledApps(contentArea) {
        contentArea.innerHTML = `
            <div class="apps-section">
                <h2>Installed Applications</h2>
                <div class="device-selector">
                    <label>Select Device:</label>
                    <select id="apps-device-selector" class="device-select">
                        <option value="">Loading devices...</option>
                    </select>
                </div>
                <div id="apps-list" class="loading">Select a device</div>
            </div>
        `;

        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-devices?company_name=${this.companyName}`);
            const data = await res.json();

            const select = document.getElementById('apps-device-selector');
            select.innerHTML = '<option value="">Select a device</option>';
            
            if (data.data) {
                data.data.forEach(device => {
                    const option = document.createElement('option');
                    option.value = device.device_id;
                    option.textContent = `${device.hostname} (${device.username})`;
                    select.appendChild(option);
                });

                select.addEventListener('change', async (e) => {
                    if (e.target.value) {
                        await this.loadDeviceApps(e.target.value);
                    }
                });
            }
        } catch (err) {
            console.error('Apps error:', err);
        }
    }

    async loadDeviceApps(deviceId) {
        try {
            const res = await fetch(`${this.apiBaseUrl}/api/system/get-system-info?device_id=${deviceId}`);
            const data = await res.json();

            if (data.success && data.data && data.data.InstalledApplications) {
                let html = '<div class="apps-list"><table>';
                html += '<tr><th>Name</th><th>Version</th><th>Vendor</th></tr>';

                data.data.InstalledApplications.forEach(app => {
                    html += `<tr><td>${app.Name}</td><td>${app.Version}</td><td>${app.Vendor}</td></tr>`;
                });

                html += '</table></div>';
                document.getElementById('apps-list').innerHTML = html;
            }
        } catch (err) {
            document.getElementById('apps-list').innerHTML = `<div class="error">Error: ${err.message}</div>`;
        }
    }

    async loadActiveUsers(contentArea) {
        contentArea.innerHTML = `<div class="loading">Loading active users...</div>`;
        // Load and display currently active users
    }

    async loadUserActivity(contentArea) {
        contentArea.innerHTML = `<div class="loading">Loading activity logs...</div>`;
        // Load user activity logs from database
    }

    async loadRunningProcesses(contentArea) {
        contentArea.innerHTML = `<div class="loading">Loading processes...</div>`;
        // Load running processes
    }

    createContentArea() {
        const area = document.createElement('div');
        area.className = 'dashboard-content';
        document.body.appendChild(area);
        return area;
    }

    logout() {
        localStorage.removeItem('authToken');
        localStorage.removeItem('currentCompany');
        window.location.href = '/login.html';
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const apiBaseUrl = 'http://localhost:8888';
    const companyName = localStorage.getItem('currentCompany') || 'Default Company';
    const userName = localStorage.getItem('currentUser') || 'Admin';

    window.dashboardNav = new EnhancedDashboardNavigation(apiBaseUrl, companyName, userName);
});
