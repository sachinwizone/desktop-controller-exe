/**
 * SYSTEM MANAGEMENT CONTROL PANEL
 * For: web_dashboard_new ONLY (NEW DASHBOARD)
 * 
 * This module provides comprehensive system management including:
 * - View all active users and devices
 * - Monitor system information (hardware, OS, storage)
 * - View installed applications
 * - Execute control commands (restart, shutdown, uninstall, block apps)
 * - Real-time system monitoring and refresh
 */

class SystemManagementControlPanel {
    constructor(apiBaseUrl, apiKey) {
        this.apiBaseUrl = apiBaseUrl;
        this.apiKey = apiKey;
        this.refreshInterval = 30000; // 30 seconds
        this.currentCompany = localStorage.getItem('currentCompany') || '';
        this.init();
    }

    /**
     * Initialize the control panel
     */
    init() {
        this.setupEventListeners();
        this.loadDashboardData();
    }

    /**
     * Setup all event listeners
     */
    setupEventListeners() {
        // Tab switching
        document.querySelectorAll('.control-panel-tab').forEach(tab => {
            tab.addEventListener('click', (e) => this.switchTab(e));
        });

        // Refresh buttons
        document.querySelectorAll('[data-action="refresh"]').forEach(btn => {
            btn.addEventListener('click', () => this.loadDashboardData());
        });

        // Search
        const searchInputs = document.querySelectorAll('.system-search-input');
        searchInputs.forEach(input => {
            input.addEventListener('keyup', (e) => this.handleSearch(e));
        });

        // Auto-refresh toggle
        const autoRefreshToggle = document.getElementById('autoRefreshToggle');
        if (autoRefreshToggle) {
            autoRefreshToggle.addEventListener('change', (e) => this.setAutoRefresh(e.target.checked));
        }

        // Command execution buttons
        document.querySelectorAll('[data-command]').forEach(btn => {
            btn.addEventListener('click', (e) => this.showCommandDialog(e));
        });
    }

    /**
     * Switch between tabs
     */
    switchTab(e) {
        const tabName = e.target.dataset.tab;
        
        // Hide all tabs
        document.querySelectorAll('.control-panel-content').forEach(tab => {
            tab.classList.remove('active');
        });

        // Remove active from buttons
        document.querySelectorAll('.control-panel-tab').forEach(btn => {
            btn.classList.remove('active');
        });

        // Show selected tab
        const tabContent = document.getElementById(`tab-${tabName}`);
        if (tabContent) {
            tabContent.classList.add('active');
        }
        e.target.classList.add('active');

        // Load tab-specific data
        this.loadTabData(tabName);
    }

    /**
     * Load data for specific tab
     */
    async loadTabData(tabName) {
        try {
            switch (tabName) {
                case 'devices':
                    await this.loadDevices();
                    break;
                case 'users':
                    await this.loadActiveUsers();
                    break;
                case 'apps':
                    await this.loadApplications();
                    break;
                case 'stats':
                    await this.loadStatistics();
                    break;
            }
        } catch (error) {
            console.error(`Error loading ${tabName}:`, error);
            this.showNotification(`Failed to load ${tabName}`, 'error');
        }
    }

    /**
     * Load all dashboard data
     */
    async loadDashboardData() {
        try {
            await Promise.all([
                this.loadStatistics(),
                this.loadActiveUsers(),
                this.loadDevices()
            ]);
        } catch (error) {
            console.error('Error loading dashboard:', error);
            this.showNotification('Failed to load dashboard data', 'error');
        }
    }

    /**
     * Load statistics
     */
    async loadStatistics() {
        try {
            const response = await this.apiCall(
                `GET`,
                `/statistics/company/${this.currentCompany}`
            );

            const statsHtml = `
                <div class="stats-grid">
                    <div class="stat-card">
                        <div class="stat-number">${response.totalDevices || 0}</div>
                        <div class="stat-label">Total Devices</div>
                    </div>
                    <div class="stat-card active">
                        <div class="stat-number">${response.activeDevices || 0}</div>
                        <div class="stat-label">Active Devices</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${response.activeUsers || 0}</div>
                        <div class="stat-label">Active Users</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${response.totalAppsAcross || 0}</div>
                        <div class="stat-label">Total Applications</div>
                    </div>
                </div>

                <div class="common-apps-section">
                    <h3>Most Common Applications</h3>
                    <div class="common-apps-list">
                        ${response.mostCommonApps?.map(app => `
                            <div class="common-app-item">
                                <span class="app-name">${app._id}</span>
                                <span class="app-count">${app.count} devices</span>
                            </div>
                        `).join('') || '<p>No data available</p>'}
                    </div>
                </div>
            `;

            const statsContainer = document.getElementById('tab-stats');
            if (statsContainer) {
                statsContainer.innerHTML = statsHtml;
            }
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }

    /**
     * Load active users
     */
    async loadActiveUsers() {
        try {
            const response = await this.apiCall(
                `GET`,
                `/active-users/${this.currentCompany}`
            );

            const usersHtml = `
                <div class="users-list">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>User Name</th>
                                <th>Computer Name</th>
                                <th>Last Seen</th>
                                <th>Status</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${response.data?.map(user => `
                                <tr>
                                    <td>${user.userName || 'N/A'}</td>
                                    <td>${user.computerName || 'N/A'}</td>
                                    <td>${this.formatDate(user.lastSeenAt)}</td>
                                    <td><span class="status-badge active">Active</span></td>
                                    <td>
                                        <button class="action-btn" onclick="systemControl.lockUserScreen('${user.computerName}')">
                                            🔒 Lock
                                        </button>
                                        <button class="action-btn" onclick="systemControl.sendMessage('${user.computerName}')">
                                            💬 Message
                                        </button>
                                    </td>
                                </tr>
                            `).join('') || '<tr><td colspan="5" class="no-data">No active users</td></tr>'}
                        </tbody>
                    </table>
                </div>
            `;

            const usersContainer = document.getElementById('tab-users');
            if (usersContainer) {
                usersContainer.innerHTML = usersHtml;
            }
        } catch (error) {
            console.error('Error loading users:', error);
        }
    }

    /**
     * Load devices
     */
    async loadDevices() {
        try {
            const response = await this.apiCall(
                `GET`,
                `/system-info/company/${this.currentCompany}`
            );

            const devicesHtml = `
                <div class="devices-list">
                    <div class="devices-grid">
                        ${response.data?.map(device => `
                            <div class="device-card">
                                <div class="device-header">
                                    <h3>${device.computerName}</h3>
                                    <span class="device-status">Active</span>
                                </div>
                                
                                <div class="device-info">
                                    <div class="info-row">
                                        <span class="label">OS:</span>
                                        <span class="value">${device.operatingSystem?.name || 'N/A'}</span>
                                    </div>
                                    <div class="info-row">
                                        <span class="label">User:</span>
                                        <span class="value">${device.userName || 'N/A'}</span>
                                    </div>
                                    <div class="info-row">
                                        <span class="label">CPU:</span>
                                        <span class="value">${device.processorInfo?.name?.substring(0, 40) || 'N/A'}...</span>
                                    </div>
                                    <div class="info-row">
                                        <span class="label">RAM:</span>
                                        <span class="value">${device.memoryInfo?.totalMemoryGB} GB</span>
                                    </div>
                                </div>

                                <div class="storage-info">
                                    ${device.storageInfo?.slice(0, 2).map(storage => `
                                        <div class="storage-row">
                                            <span>${storage.driveLetter}: ${storage.freeSpaceGB}GB free / ${storage.totalSizeGB}GB</span>
                                        </div>
                                    `).join('')}
                                </div>

                                <div class="device-actions">
                                    <button class="action-btn" onclick="systemControl.viewDeviceDetails('${device._id}')">
                                        📊 Details
                                    </button>
                                    <button class="action-btn" onclick="systemControl.sendCommandToDevice('${device.computerName}', 'restart')">
                                        🔄 Restart
                                    </button>
                                    <button class="action-btn danger" onclick="systemControl.sendCommandToDevice('${device.computerName}', 'shutdown')">
                                        🛑 Shutdown
                                    </button>
                                </div>
                            </div>
                        `).join('') || '<p class="no-data">No devices found</p>'}
                    </div>
                </div>
            `;

            const devicesContainer = document.getElementById('tab-devices');
            if (devicesContainer) {
                devicesContainer.innerHTML = devicesHtml;
            }
        } catch (error) {
            console.error('Error loading devices:', error);
        }
    }

    /**
     * Load applications
     */
    async loadApplications() {
        try {
            const response = await this.apiCall(
                `GET`,
                `/installed-apps/search?appName=`
            );

            const appsHtml = `
                <div class="apps-section">
                    <div class="apps-filter">
                        <input type="text" 
                               id="appSearchInput" 
                               class="system-search-input" 
                               placeholder="Search applications...">
                    </div>

                    <div class="apps-list">
                        <table class="data-table">
                            <thead>
                                <tr>
                                    <th>Application Name</th>
                                    <th>Version</th>
                                    <th>Publisher</th>
                                    <th>Devices</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${response.data?.map((app, idx) => `
                                    <tr>
                                        <td>${app.name}</td>
                                        <td>${app.version || 'N/A'}</td>
                                        <td>${app.publisher || 'N/A'}</td>
                                        <td>
                                            <span class="device-count">${app.computerName || 'Multiple'}</span>
                                        </td>
                                        <td>
                                            <button class="action-btn" onclick="systemControl.uninstallApp('${app.name}')">
                                                🗑️ Uninstall
                                            </button>
                                            <button class="action-btn" onclick="systemControl.blockApp('${app.name}')">
                                                🚫 Block
                                            </button>
                                        </td>
                                    </tr>
                                `).join('') || '<tr><td colspan="5" class="no-data">No applications found</td></tr>'}
                            </tbody>
                        </table>
                    </div>
                </div>
            `;

            const appsContainer = document.getElementById('tab-apps');
            if (appsContainer) {
                appsContainer.innerHTML = appsHtml;
            }
        } catch (error) {
            console.error('Error loading apps:', error);
        }
    }

    /**
     * Send command to device
     */
    async sendCommandToDevice(computerName, commandType) {
        const confirmMsg = `Are you sure you want to ${commandType} ${computerName}?`;
        if (!confirm(confirmMsg)) return;

        try {
            await this.apiCall('POST', '/control/commands', {
                computerName,
                activationKey: localStorage.getItem('activationKey'),
                type: commandType,
                parameters: {}
            });

            this.showNotification(`${commandType} command sent to ${computerName}`, 'success');
        } catch (error) {
            this.showNotification(`Failed to send command: ${error.message}`, 'error');
        }
    }

    /**
     * Uninstall application
     */
    async uninstallApp(appName) {
        const computerName = prompt('Enter computer name to uninstall from:');
        if (!computerName) return;

        if (!confirm(`Uninstall ${appName} from ${computerName}?`)) return;

        try {
            await this.apiCall('POST', '/control/commands', {
                computerName,
                activationKey: localStorage.getItem('activationKey'),
                type: 'uninstall_app',
                parameters: { appName }
            });

            this.showNotification(`Uninstall command sent for ${appName}`, 'success');
        } catch (error) {
            this.showNotification(`Failed: ${error.message}`, 'error');
        }
    }

    /**
     * Block application
     */
    async blockApp(appName) {
        const computerName = prompt('Enter computer name:');
        if (!computerName) return;

        if (!confirm(`Block ${appName} on ${computerName}?`)) return;

        try {
            await this.apiCall('POST', '/control/commands', {
                computerName,
                activationKey: localStorage.getItem('activationKey'),
                type: 'block_application',
                parameters: { appName, appPath: '' }
            });

            this.showNotification(`Block command sent for ${appName}`, 'success');
        } catch (error) {
            this.showNotification(`Failed: ${error.message}`, 'error');
        }
    }

    /**
     * Lock user screen
     */
    async lockUserScreen(computerName) {
        if (!confirm(`Lock screen on ${computerName}?`)) return;

        try {
            await this.apiCall('POST', '/control/commands', {
                computerName,
                activationKey: localStorage.getItem('activationKey'),
                type: 'lock_screen',
                parameters: {}
            });

            this.showNotification(`Lock command sent to ${computerName}`, 'success');
        } catch (error) {
            this.showNotification(`Failed: ${error.message}`, 'error');
        }
    }

    /**
     * Send message to user
     */
    sendMessage(computerName) {
        const message = prompt('Enter message to send:');
        if (!message) return;

        this.apiCall('POST', '/control/commands', {
            computerName,
            activationKey: localStorage.getItem('activationKey'),
            type: 'show_message',
            parameters: { message }
        }).then(() => {
            this.showNotification(`Message sent to ${computerName}`, 'success');
        }).catch(error => {
            this.showNotification(`Failed: ${error.message}`, 'error');
        });
    }

    /**
     * Handle search
     */
    handleSearch(e) {
        const query = e.target.value.toLowerCase();
        const rows = document.querySelectorAll('.data-table tbody tr');
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(query) ? '' : 'none';
        });
    }

    /**
     * API Call helper
     */
    async apiCall(method, endpoint, data = null) {
        const options = {
            method,
            headers: {
                'Content-Type': 'application/json',
                'x-api-key': this.apiKey
            }
        };

        if (data && (method === 'POST' || method === 'PUT')) {
            options.body = JSON.stringify(data);
        }

        const response = await fetch(`${this.apiBaseUrl}${endpoint}`, options);
        
        if (!response.ok) {
            throw new Error(`API Error: ${response.status} ${response.statusText}`);
        }

        return response.json();
    }

    /**
     * Show notification
     */
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <span>${message}</span>
            <button onclick="this.parentElement.remove()">✕</button>
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 5000);
    }

    /**
     * Format date
     */
    formatDate(dateString) {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleString();
    }

    /**
     * Set auto-refresh
     */
    setAutoRefresh(enabled) {
        if (enabled) {
            if (this.autoRefreshTimer) clearInterval(this.autoRefreshTimer);
            this.autoRefreshTimer = setInterval(() => this.loadDashboardData(), this.refreshInterval);
        } else {
            if (this.autoRefreshTimer) clearInterval(this.autoRefreshTimer);
        }
    }

    /**
     * View device details
     */
    viewDeviceDetails(deviceId) {
        // Open detailed view modal
        alert('Opening detailed view for device: ' + deviceId);
    }
}

// Export for use in main app
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SystemManagementControlPanel;
}
