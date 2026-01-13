// Desktop Time & Attendance - Single Page Application
// Main App Logic

// Add CSS for spinner animation
const style = document.createElement('style');
style.textContent = `@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }`;
document.head.appendChild(style);

const app = {
    currentRole: null,
    currentView: 'dashboard',
    userData: null,
    dashboardData: null,
    
    // API Base URL
    API_BASE: '',
    
    // API Helper function
    async api(action, data = null) {
        const options = {
            method: data ? 'POST' : 'GET',
            headers: { 'Content-Type': 'application/json' }
        };
        if (data) options.body = JSON.stringify(data);
        
        const response = await fetch(`${this.API_BASE}/api.php?action=${action}`, options);
        return response.json();
    },
    
    // Initialize app
    init() {
        // Check for existing session
        const isLoggedIn = sessionStorage.getItem('isLoggedIn');
        const userData = sessionStorage.getItem('userData');
        const userRole = sessionStorage.getItem('userRole');
        
        if (isLoggedIn && userData && userRole) {
            this.userData = JSON.parse(userData);
            this.currentRole = userRole;
            // Auto-login - hide all login screens first
            setTimeout(() => {
                const loader = document.getElementById('loadingScreen');
                if (loader) loader.classList.add('hidden');
                const roleSelection = document.getElementById('roleSelection');
                if (roleSelection) roleSelection.style.display = 'none';
                const loginSection = document.getElementById('loginSection');
                if (loginSection) loginSection.style.display = 'none';
                this.loadDashboard();
            }, 500);
        } else {
            // Hide loading screen and reset any previous state
            setTimeout(() => {
                const loader = document.getElementById('loadingScreen');
                if (loader) loader.classList.add('hidden');
                const appContainer = document.getElementById('appContainer');
                if (appContainer) appContainer.classList.remove('active');
            }, 500);
            this.showRoleSelection();
        }
    },

    // ==================== MODAL SYSTEM ====================
    
    showModal(content, size = '') {
        const overlay = document.getElementById('modalOverlay');
        const container = document.getElementById('modalContainer');
        container.className = 'modal' + (size ? ' modal-' + size : '');
        container.innerHTML = content;
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    },

    closeModal(event) {
        if (event && event.target !== event.currentTarget) return;
        const overlay = document.getElementById('modalOverlay');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    },

    // ==================== TOAST NOTIFICATIONS ====================
    
    showToast(message, type = 'success') {
        const container = document.getElementById('toastContainer');
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        
        const icons = {
            success: '<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
            error: '<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>',
            warning: '<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
            info: '<svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>'
        };
        
        toast.innerHTML = `
            <div class="toast-icon">${icons[type]}</div>
            <span class="toast-message">${message}</span>
            <button class="toast-close" onclick="this.parentElement.remove()">×</button>
        `;
        
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 4000);
    },

    // ==================== EMPLOYEE MODALS ====================
    
    showAddEmployeeModal() {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Add New Employee</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">First Name *</label>
                        <input type="text" class="modal-form-input" id="empFirstName" placeholder="Enter first name">
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Last Name *</label>
                        <input type="text" class="modal-form-input" id="empLastName" placeholder="Enter last name">
                    </div>
                </div>
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">Email Address *</label>
                        <input type="email" class="modal-form-input" id="empEmail" placeholder="employee@company.com">
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Phone Number *</label>
                        <input type="tel" class="modal-form-input" id="empPhone" placeholder="+91 98765 43210">
                    </div>
                </div>
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">Department *</label>
                        <select class="modal-form-select" id="empDepartment">
                            <option value="">Select Department</option>
                            <option value="engineering">Engineering</option>
                            <option value="design">Design</option>
                            <option value="marketing">Marketing</option>
                            <option value="sales">Sales</option>
                            <option value="hr">Human Resources</option>
                            <option value="finance">Finance</option>
                        </select>
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Designation *</label>
                        <input type="text" class="modal-form-input" id="empDesignation" placeholder="e.g. Software Engineer">
                    </div>
                </div>
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">Shift Timing</label>
                        <select class="modal-form-select" id="empShift">
                            <option value="general">General (9:00 AM - 6:00 PM)</option>
                            <option value="morning">Morning (6:00 AM - 3:00 PM)</option>
                            <option value="evening">Evening (2:00 PM - 11:00 PM)</option>
                            <option value="night">Night (10:00 PM - 7:00 AM)</option>
                        </select>
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Join Date *</label>
                        <input type="date" class="modal-form-input" id="empJoinDate">
                    </div>
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Address</label>
                    <textarea class="modal-form-textarea" id="empAddress" placeholder="Enter full address" rows="2"></textarea>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-admin" onclick="app.saveEmployee()">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                    Save Employee
                </button>
            </div>
        `, 'lg');
    },

    saveEmployee() {
        const firstName = document.getElementById('empFirstName').value;
        const lastName = document.getElementById('empLastName').value;
        const email = document.getElementById('empEmail').value;
        
        if (!firstName || !lastName || !email) {
            this.showToast('Please fill all required fields', 'error');
            return;
        }
        
        this.showToast('Employee added successfully!', 'success');
        this.closeModal();
    },

    showEditEmployeeModal(empId, name, email, dept, designation) {
        const [firstName, lastName] = name.split(' ');
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Edit Employee</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">First Name *</label>
                        <input type="text" class="modal-form-input" id="editFirstName" value="${firstName || ''}">
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Last Name *</label>
                        <input type="text" class="modal-form-input" id="editLastName" value="${lastName || ''}">
                    </div>
                </div>
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">Email Address *</label>
                        <input type="email" class="modal-form-input" id="editEmail" value="${email}">
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Phone Number</label>
                        <input type="tel" class="modal-form-input" id="editPhone" value="+91 98765 43210">
                    </div>
                </div>
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">Department *</label>
                        <select class="modal-form-select" id="editDepartment">
                            <option value="engineering" ${dept === 'Engineering' ? 'selected' : ''}>Engineering</option>
                            <option value="design" ${dept === 'Design' ? 'selected' : ''}>Design</option>
                            <option value="marketing" ${dept === 'Marketing' ? 'selected' : ''}>Marketing</option>
                            <option value="sales" ${dept === 'Sales' ? 'selected' : ''}>Sales</option>
                            <option value="hr" ${dept === 'HR' ? 'selected' : ''}>Human Resources</option>
                        </select>
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Designation *</label>
                        <input type="text" class="modal-form-input" id="editDesignation" value="${designation}">
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-admin" onclick="app.updateEmployee('${empId}')">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                    Update Employee
                </button>
            </div>
        `, 'lg');
    },

    updateEmployee(empId) {
        this.showToast('Employee updated successfully!', 'success');
        this.closeModal();
    },

    showViewEmployeeModal(name, email, dept, designation, status) {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Employee Details</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div style="text-align: center; margin-bottom: 24px;">
                    <div class="avatar avatar-xl" style="margin: 0 auto 16px;">${name.split(' ').map(n => n[0]).join('')}</div>
                    <h3 style="font-size: 20px; font-weight: 700; color: var(--dark);">${name}</h3>
                    <p style="color: var(--gray-500);">${designation}</p>
                    <span class="badge ${status === 'Active' ? 'badge-success' : 'badge-gray'}" style="margin-top: 8px;">${status}</span>
                </div>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
                    <div style="padding: 16px; background: var(--gray-50); border-radius: 12px;">
                        <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Email</p>
                        <p style="font-weight: 600; color: var(--dark);">${email}</p>
                    </div>
                    <div style="padding: 16px; background: var(--gray-50); border-radius: 12px;">
                        <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Department</p>
                        <p style="font-weight: 600; color: var(--dark);">${dept}</p>
                    </div>
                    <div style="padding: 16px; background: var(--gray-50); border-radius: 12px;">
                        <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Phone</p>
                        <p style="font-weight: 600; color: var(--dark);">+91 98765 43210</p>
                    </div>
                    <div style="padding: 16px; background: var(--gray-50); border-radius: 12px;">
                        <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Join Date</p>
                        <p style="font-weight: 600; color: var(--dark);">15 Mar 2024</p>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Close</button>
                <button class="btn btn-admin" onclick="app.showEditEmployeeModal('1', '${name}', '${email}', '${dept}', '${designation}')">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                    Edit Employee
                </button>
            </div>
        `);
    },

    // ==================== LEAVE MODALS ====================
    
    showApproveLeaveModal(empName, leaveType, dates) {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Approve Leave Request</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div style="background: #dcfce7; padding: 16px; border-radius: 12px; margin-bottom: 24px; display: flex; align-items: center; gap: 12px;">
                    <svg width="24" height="24" fill="none" stroke="#16a34a" stroke-width="2" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
                    <span style="color: #16a34a; font-weight: 500;">You are about to approve this leave request</span>
                </div>
                <div style="background: var(--gray-50); padding: 20px; border-radius: 12px; margin-bottom: 24px;">
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                        <div>
                            <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Employee</p>
                            <p style="font-weight: 600; color: var(--dark);">${empName}</p>
                        </div>
                        <div>
                            <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Leave Type</p>
                            <p style="font-weight: 600; color: var(--dark);">${leaveType}</p>
                        </div>
                        <div style="grid-column: span 2;">
                            <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Dates</p>
                            <p style="font-weight: 600; color: var(--dark);">${dates}</p>
                        </div>
                    </div>
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Comments (Optional)</label>
                    <textarea class="modal-form-textarea" id="approveComments" placeholder="Add any comments for the employee..." rows="3"></textarea>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-success" onclick="app.approveLeave('${empName}')">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg>
                    Approve Leave
                </button>
            </div>
        `);
    },

    approveLeave(empName) {
        this.showToast(`Leave request for ${empName} approved!`, 'success');
        this.closeModal();
    },

    showRejectLeaveModal(empName, leaveType, dates) {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Reject Leave Request</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div style="background: #fee2e2; padding: 16px; border-radius: 12px; margin-bottom: 24px; display: flex; align-items: center; gap: 12px;">
                    <svg width="24" height="24" fill="none" stroke="#dc2626" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>
                    <span style="color: #dc2626; font-weight: 500;">You are about to reject this leave request</span>
                </div>
                <div style="background: var(--gray-50); padding: 20px; border-radius: 12px; margin-bottom: 24px;">
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                        <div>
                            <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Employee</p>
                            <p style="font-weight: 600; color: var(--dark);">${empName}</p>
                        </div>
                        <div>
                            <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Leave Type</p>
                            <p style="font-weight: 600; color: var(--dark);">${leaveType}</p>
                        </div>
                        <div style="grid-column: span 2;">
                            <p style="font-size: 12px; color: var(--gray-500); margin-bottom: 4px;">Dates</p>
                            <p style="font-weight: 600; color: var(--dark);">${dates}</p>
                        </div>
                    </div>
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Reason for Rejection *</label>
                    <textarea class="modal-form-textarea" id="rejectReason" placeholder="Please provide a reason for rejecting this request..." rows="3"></textarea>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-danger" onclick="app.rejectLeave('${empName}')">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                    Reject Leave
                </button>
            </div>
        `);
    },

    rejectLeave(empName) {
        const reason = document.getElementById('rejectReason').value;
        if (!reason) {
            this.showToast('Please provide a reason for rejection', 'error');
            return;
        }
        this.showToast(`Leave request for ${empName} rejected`, 'warning');
        this.closeModal();
    },

    // ==================== NOTIFICATION MODAL ====================
    
    showSendNotificationModal() {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Send Notification</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div class="modal-form-group">
                    <label class="modal-form-label">Send To *</label>
                    <select class="modal-form-select" id="notifRecipient" onchange="app.toggleRecipientOptions()">
                        <option value="all">All Employees</option>
                        <option value="department">Specific Department</option>
                        <option value="individual">Individual Employee</option>
                    </select>
                </div>
                <div class="modal-form-group" id="deptSelectGroup" style="display: none;">
                    <label class="modal-form-label">Select Department</label>
                    <select class="modal-form-select" id="notifDepartment">
                        <option value="engineering">Engineering</option>
                        <option value="design">Design</option>
                        <option value="marketing">Marketing</option>
                        <option value="sales">Sales</option>
                        <option value="hr">Human Resources</option>
                    </select>
                </div>
                <div class="modal-form-group" id="empSelectGroup" style="display: none;">
                    <label class="modal-form-label">Select Employee</label>
                    <select class="modal-form-select" id="notifEmployee">
                        <option value="1">Rahul Sharma</option>
                        <option value="2">Priya Singh</option>
                        <option value="3">Amit Kumar</option>
                        <option value="4">Sneha Patel</option>
                    </select>
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Notification Type</label>
                    <select class="modal-form-select" id="notifType">
                        <option value="info">Information</option>
                        <option value="warning">Warning</option>
                        <option value="alert">Alert / Urgent</option>
                        <option value="success">Success / Achievement</option>
                    </select>
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Title *</label>
                    <input type="text" class="modal-form-input" id="notifTitle" placeholder="Enter notification title">
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Message *</label>
                    <textarea class="modal-form-textarea" id="notifMessage" placeholder="Enter your message here..." rows="4"></textarea>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-admin" onclick="app.sendNotification()">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg>
                    Send Notification
                </button>
            </div>
        `);
    },

    toggleRecipientOptions() {
        const recipient = document.getElementById('notifRecipient').value;
        document.getElementById('deptSelectGroup').style.display = recipient === 'department' ? 'block' : 'none';
        document.getElementById('empSelectGroup').style.display = recipient === 'individual' ? 'block' : 'none';
    },

    sendNotification() {
        const title = document.getElementById('notifTitle').value;
        const message = document.getElementById('notifMessage').value;
        if (!title || !message) {
            this.showToast('Please fill all required fields', 'error');
            return;
        }
        this.showToast('Notification sent successfully!', 'success');
        this.closeModal();
    },

    // ==================== DEPARTMENT MODAL ====================
    
    showAddDepartmentModal() {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Add New Department</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div class="modal-form-group">
                    <label class="modal-form-label">Department Name *</label>
                    <input type="text" class="modal-form-input" id="deptName" placeholder="e.g. Engineering">
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Department Head</label>
                    <select class="modal-form-select" id="deptHead">
                        <option value="">Select Manager</option>
                        <option value="1">Rahul Sharma</option>
                        <option value="2">Priya Singh</option>
                        <option value="3">Amit Kumar</option>
                    </select>
                </div>
                <div class="modal-form-group">
                    <label class="modal-form-label">Description</label>
                    <textarea class="modal-form-textarea" id="deptDescription" placeholder="Brief description of the department..." rows="3"></textarea>
                </div>
                <div class="modal-form-row">
                    <div class="modal-form-group">
                        <label class="modal-form-label">Working Hours</label>
                        <input type="text" class="modal-form-input" id="deptHours" placeholder="9:00 AM - 6:00 PM">
                    </div>
                    <div class="modal-form-group">
                        <label class="modal-form-label">Location</label>
                        <input type="text" class="modal-form-input" id="deptLocation" placeholder="Floor / Building">
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-admin" onclick="app.saveDepartment()">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                    Create Department
                </button>
            </div>
        `);
    },

    saveDepartment() {
        const name = document.getElementById('deptName').value;
        if (!name) {
            this.showToast('Please enter department name', 'error');
            return;
        }
        this.showToast('Department created successfully!', 'success');
        this.closeModal();
    },

    // ==================== CONFIRM DELETE ====================
    
    confirmDelete(name) {
        this.showModal(`
            <div class="modal-header">
                <h2 class="modal-title">Confirm Deletion</h2>
                <button class="modal-close" onclick="app.closeModal()">
                    <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="modal-body">
                <div style="text-align: center; padding: 20px 0;">
                    <div style="width: 64px; height: 64px; background: #fee2e2; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                        <svg width="32" height="32" fill="none" stroke="#dc2626" stroke-width="2" viewBox="0 0 24 24">
                            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
                            <line x1="12" y1="9" x2="12" y2="13"/>
                            <line x1="12" y1="17" x2="12.01" y2="17"/>
                        </svg>
                    </div>
                    <h3 style="font-size: 18px; font-weight: 700; color: var(--dark); margin-bottom: 8px;">Delete ${name}?</h3>
                    <p style="color: var(--gray-500);">This action cannot be undone. All data associated with this record will be permanently removed.</p>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="app.closeModal()">Cancel</button>
                <button class="btn btn-danger" onclick="app.deleteConfirmed('${name}')">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                    Delete
                </button>
            </div>
        `);
    },

    deleteConfirmed(name) {
        this.showToast(`${name} deleted successfully`, 'success');
        this.closeModal();
    },

    // Show role selection screen
    showRoleSelection() {
        const roleSelection = document.getElementById('roleSelection');
        const loginSection = document.getElementById('loginSection');
        const appContainer = document.getElementById('appContainer');
        
        // Show role selection, hide others
        if (roleSelection) {
            roleSelection.classList.remove('hidden');
            roleSelection.style.display = '';
        }
        if (loginSection) loginSection.style.display = 'none';
        if (appContainer) {
            appContainer.classList.remove('active');
            appContainer.innerHTML = '';
        }
        this.currentRole = null;
    },

    // Show login screen
    showLogin(role) {
        this.currentRole = role;
        document.getElementById('roleSelection').classList.add('hidden');
        document.getElementById('loginSection').style.display = 'flex';
        
        // Render login form
        const loginContainer = document.getElementById('loginContainer');
        loginContainer.innerHTML = `
            <button class="back-button" onclick="app.showRoleSelection()">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/>
                </svg>
                Back to Roles
            </button>
            <div class="logo-section">
                <div class="logo-icon" style="background: linear-gradient(135deg, ${role === 'admin' ? '#f59e0b 0%, #d97706' : '#667eea 0%, #764ba2'} 100%);">
                    <svg viewBox="0 0 24 24" style="width: 40px; height: 40px; fill: white;">
                        ${role === 'admin' ? 
                            '<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 10.99h7c-.53 4.12-3.28 7.79-7 8.94V12H5V6.3l7-3.11v8.8z"/>' :
                            '<path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>'
                        }
                    </svg>
                </div>
                <h1 class="app-title" style="color: white; font-size: 26px;">${role === 'admin' ? 'Admin Login' : 'User Login'}</h1>
                <p class="app-subtitle">WIZONE IT NETWORK INDIA PVT LTD</p>
            </div>
            
            <form class="form-section" onsubmit="app.handleLogin(event)">
                <div class="form-group">
                    <label class="form-label">Username or Email</label>
                    <input type="text" class="form-input" id="loginUsername" placeholder="Enter your username or email" required>
                </div>
                
                <div class="form-group">
                    <label class="form-label">Password</label>
                    <input type="password" class="form-input" id="loginPassword" placeholder="Enter your password" required>
                </div>
                
                <button type="submit" class="login-button ${role === 'admin' ? 'admin' : ''}">
                    Sign In
                </button>
            </form>
        `;
    },

    // Handle login
    async handleLogin(event) {
        event.preventDefault();
        const username = document.getElementById('loginUsername').value;
        const password = document.getElementById('loginPassword').value;
        const submitBtn = event.target.querySelector('button[type="submit"]');
        
        if (!username || !password) {
            this.showToast('Please enter username and password', 'error');
            return;
        }
        
        // Show loading state
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<span style="display:flex;align-items:center;justify-content:center;gap:8px;"><svg width="20" height="20" viewBox="0 0 24 24" style="animation:spin 1s linear infinite;"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" fill="none" stroke-dasharray="30 70"/></svg>Signing in...</span>';
        submitBtn.disabled = true;
        
        try {
            const response = await fetch('/api.php?action=login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    username, 
                    password,
                    role: this.currentRole 
                })
            });
            
            const data = await response.json();
            
            if (data.success) {
                // Store user data in session
                this.userData = data.user;
                sessionStorage.setItem('userData', JSON.stringify(data.user));
                sessionStorage.setItem('isLoggedIn', 'true');
                sessionStorage.setItem('userRole', this.currentRole);
                
                this.showToast(`Welcome back, ${data.user.full_name || data.user.username}!`, 'success');
                
                document.getElementById('loginSection').style.display = 'none';
                this.loadDashboard();
            } else {
                this.showToast(data.error || 'Invalid credentials', 'error');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        } catch (error) {
            console.error('Login error:', error);
            this.showToast('Connection error. Please try again.', 'error');
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
        }
    },

    // Load dashboard based on role
    loadDashboard() {
        // Hide login screens first
        const roleSelection = document.getElementById('roleSelection');
        if (roleSelection) roleSelection.style.display = 'none';
        const loginSection = document.getElementById('loginSection');
        if (loginSection) loginSection.style.display = 'none';
        
        const appContainer = document.getElementById('appContainer');
        appContainer.innerHTML = ''; // Clear previous content
        appContainer.classList.add('active');
        
        if (this.currentRole === 'admin') {
            this.renderAdminDashboard();
        } else {
            this.renderUserDashboard();
        }
    },

    // Render User Dashboard
    renderUserDashboard() {
        const appContainer = document.getElementById('appContainer');
        appContainer.innerHTML = `
            <!-- Sidebar -->
            <div class="sidebar">
                <div class="logo-section">
                    <div class="logo-icon" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);">
                        <svg viewBox="0 0 24 24" style="width: 28px; height: 28px; fill: white;">
                            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
                        </svg>
                    </div>
                    <div class="app-title">Desktop Controller</div>
                    <div class="company-tag">WIZONE IT NETWORK</div>
                </div>
                
                <div class="menu-section">
                    <div class="menu-label">Main</div>
                    <nav>
                        <a class="menu-item active" onclick="app.navigate('dashboard')" data-view="dashboard">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="3" width="7" height="7" rx="1"/>
                                <rect x="14" y="3" width="7" height="7" rx="1"/>
                                <rect x="14" y="14" width="7" height="7" rx="1"/>
                                <rect x="3" y="14" width="7" height="7" rx="1"/>
                            </svg>
                            <span>Dashboard</span>
                        </a>
                        
                        <a class="menu-item" onclick="app.navigate('attendance')" data-view="attendance">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                                <line x1="16" y1="2" x2="16" y2="6"/>
                                <line x1="8" y1="2" x2="8" y2="6"/>
                                <line x1="3" y1="10" x2="21" y2="10"/>
                                <path d="M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01M16 18h.01"/>
                            </svg>
                            <span>My Attendance</span>
                        </a>
                        
                        <a class="menu-item" onclick="app.navigate('analytics')" data-view="analytics">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <line x1="18" y1="20" x2="18" y2="10"/>
                                <line x1="12" y1="20" x2="12" y2="4"/>
                                <line x1="6" y1="20" x2="6" y2="14"/>
                            </svg>
                            <span>Analytics & Reports</span>
                        </a>
                    </nav>
                    
                    <div class="menu-label" style="margin-top: 20px;">Management</div>
                    <nav>
                        <a class="menu-item" onclick="app.navigate('leave')" data-view="leave">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                                <circle cx="12" cy="10" r="3"/>
                            </svg>
                            <span>Leave Requests</span>
                        </a>
                        
                        <a class="menu-item" onclick="app.navigate('breaks')" data-view="breaks">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="10"/>
                                <polyline points="12 6 12 12 16 14"/>
                            </svg>
                            <span>Break Management</span>
                        </a>
                    </nav>
                    
                    <div class="menu-label" style="margin-top: 20px;">Personal</div>
                    <nav>
                        <a class="menu-item" onclick="app.navigate('profile')" data-view="profile">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                                <circle cx="12" cy="7" r="4"/>
                            </svg>
                            <span>My Profile</span>
                        </a>
                        
                        <a class="menu-item" onclick="app.navigate('notifications')" data-view="notifications">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
                                <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
                            </svg>
                            <span>Notifications</span>
                        </a>
                        
                        <a class="menu-item logout" onclick="app.logout()">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
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
            <div class="main-content">
                <!-- Dashboard View -->
                <div id="dashboard-view" class="content-view active">
                    <div class="header">
                        <h1 class="page-title">Dashboard</h1>
                        <p style="color: #64748b; margin-top: 4px;">Welcome back! Here's your attendance summary.</p>
                    </div>

                    <!-- Quick Actions -->
                    <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; margin-bottom: 24px;">
                        <button onclick="app.showCheckInModal()" style="padding: 18px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; font-size: 15px; display: flex; align-items: center; justify-content: center; gap: 10px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);">
                            <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                            Check In
                        </button>
                        <button onclick="app.showCheckOutModal()" style="padding: 18px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; font-size: 15px; display: flex; align-items: center; justify-content: center; gap: 10px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);">
                            <svg width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                            Check Out
                        </button>
                    </div>

                    <!-- Stats Cards -->
                    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Today's Status</div>
                            <div style="font-size: 28px; font-weight: 700; color: #10b981; margin-bottom: 4px;">Present</div>
                            <div style="font-size: 12px; color: #94a3b8;">Checked in</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Working Hours</div>
                            <div style="font-size: 28px; font-weight: 700; color: #667eea; margin-bottom: 4px;">4.5h</div>
                            <div style="font-size: 12px; color: #94a3b8;">Today</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Leave Balance</div>
                            <div style="font-size: 28px; font-weight: 700; color: #f59e0b; margin-bottom: 4px;">12 days</div>
                            <div style="font-size: 12px; color: #94a3b8;">Remaining</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">This Month</div>
                            <div style="font-size: 28px; font-weight: 700; color: #8b5cf6; margin-bottom: 4px;">18 days</div>
                            <div style="font-size: 12px; color: #94a3b8;">Attended</div>
                        </div>
                    </div>

                    <!-- Recent Activity & Leave Info -->
                    <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px;">
                        <!-- Recent Activity -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 16px;">Recent Activity</h3>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                ${[
                                    { time: 'Today 9:00 AM', action: 'Checked In', icon: '↓', color: '#10b981' },
                                    { time: 'Yesterday 6:00 PM', action: 'Checked Out', icon: '↑', color: '#ef4444' },
                                    { time: 'Yesterday 9:05 AM', action: 'Checked In', icon: '↓', color: '#10b981' },
                                    { time: '3 days ago', action: 'Leave Approved', icon: '✓', color: '#667eea' }
                                ].map(activity => `
                                    <div style="display: flex; align-items: center; gap: 12px; padding: 12px; background: #f8fafc; border-radius: 8px;">
                                        <div style="width: 36px; height: 36px; background: ${activity.color}20; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: ${activity.color}; font-weight: bold;">${activity.icon}</div>
                                        <div style="flex: 1;">
                                            <div style="font-weight: 500; color: #0f172a; font-size: 14px;">${activity.action}</div>
                                            <div style="font-size: 12px; color: #64748b;">${activity.time}</div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>

                        <!-- Quick Actions Panel -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 16px;">Quick Links</h3>
                            <div style="display: flex; flex-direction: column; gap: 10px;">
                                <button onclick="app.navigate('leave')" style="width: 100%; padding: 12px; background: linear-gradient(135deg, #667eea20 0%, #764ba220 100%); color: #667eea; border: 2px solid #667eea40; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px; transition: all 0.2s ease;">
                                    Request Leave
                                </button>
                                <button onclick="app.navigate('attendance')" style="width: 100%; padding: 12px; background: linear-gradient(135deg, #8b5cf620 0%, #a855f720 100%); color: #8b5cf6; border: 2px solid #8b5cf640; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px; transition: all 0.2s ease;">
                                    View Attendance
                                </button>
                                <button onclick="app.navigate('analytics')" style="width: 100%; padding: 12px; background: linear-gradient(135deg, #06b6d420 0%, #0891b220 100%); color: #0891b2; border: 2px solid #06b6d440; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px; transition: all 0.2s ease;">
                                    View Analytics
                                </button>
                                <button onclick="app.navigate('profile')" style="width: 100%; padding: 12px; background: linear-gradient(135deg, #ec407a20 0%, #c2185b20 100%); color: #ec407a; border: 2px solid #ec407a40; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px; transition: all 0.2s ease;">
                                    Edit Profile
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Attendance View -->
                <div id="attendance-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">My Attendance</h1>
                        <p style="color: #64748b; margin-top: 4px;">Track your daily attendance records and check-in/check-out times</p>
                    </div>

                    <!-- Quick Stats -->
                    <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04); text-align: center;">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px;">This Month</div>
                            <div style="font-size: 32px; font-weight: 700; color: #667eea;">18</div>
                            <div style="font-size: 12px; color: #94a3b8;">Days Present</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04); text-align: center;">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px;">On Time Rate</div>
                            <div style="font-size: 32px; font-weight: 700; color: #10b981;">94%</div>
                            <div style="font-size: 12px; color: #94a3b8;">Excellent</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04); text-align: center;">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px;">Absent Days</div>
                            <div style="font-size: 32px; font-weight: 700; color: #ef4444;">2</div>
                            <div style="font-size: 12px; color: #94a3b8;">With Leave</div>
                        </div>
                    </div>

                    <!-- Attendance Table -->
                    <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a;">Attendance Records</h3>
                            <input type="month" style="padding: 8px 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 13px; color: #0f172a; cursor: pointer;" value="2026-01">
                        </div>
                        <div style="overflow-x: auto;">
                            <table style="width: 100%; font-size: 14px;">
                                <thead>
                                    <tr style="border-bottom: 2px solid #e2e8f0;">
                                        <th style="text-align: left; padding: 12px; color: #64748b; font-weight: 600;">Date</th>
                                        <th style="text-align: left; padding: 12px; color: #64748b; font-weight: 600;">Check In</th>
                                        <th style="text-align: left; padding: 12px; color: #64748b; font-weight: 600;">Check Out</th>
                                        <th style="text-align: left; padding: 12px; color: #64748b; font-weight: 600;">Duration</th>
                                        <th style="text-align: left; padding: 12px; color: #64748b; font-weight: 600;">Status</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${[
                                        { date: '07 Jan 2026', checkin: '9:00 AM', checkout: '6:15 PM', duration: '9h 15m', status: 'Present', color: '#10b981' },
                                        { date: '06 Jan 2026', checkin: '9:05 AM', checkout: '6:00 PM', duration: '8h 55m', status: 'On Time', color: '#667eea' },
                                        { date: '05 Jan 2026', checkin: '9:30 AM', checkout: '6:30 PM', duration: '9h 00m', status: 'Late', color: '#f59e0b' },
                                        { date: '04 Jan 2026', checkin: 'Leave', checkout: '-', duration: '-', status: 'Approved Leave', color: '#8b5cf6' },
                                        { date: '03 Jan 2026', checkin: '8:50 AM', checkout: '6:00 PM', duration: '9h 10m', status: 'Early', color: '#10b981' },
                                        { date: '02 Jan 2026', checkin: '9:10 AM', checkout: '5:50 PM', duration: '8h 40m', status: 'On Time', color: '#667eea' },
                                        { date: '01 Jan 2026', checkin: 'Holiday', checkout: '-', duration: '-', status: 'Holiday', color: '#64748b' }
                                    ].map(record => `
                                        <tr style="border-bottom: 1px solid #e2e8f0; hover: { background: #f8fafc; }">
                                            <td style="padding: 12px; color: #0f172a; font-weight: 500;">${record.date}</td>
                                            <td style="padding: 12px; color: #0f172a;">${record.checkin}</td>
                                            <td style="padding: 12px; color: #0f172a;">${record.checkout}</td>
                                            <td style="padding: 12px; color: #0f172a;">${record.duration}</td>
                                            <td style="padding: 12px;">
                                                <span style="display: inline-block; padding: 6px 12px; background: ${record.color}20; color: ${record.color}; border-radius: 6px; font-size: 12px; font-weight: 600;">${record.status}</span>
                                            </td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Analytics View -->
                <div id="analytics-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">Analytics & Reports</h1>
                        <p style="color: #64748b; margin-top: 4px;">View your attendance trends and productivity insights</p>
                    </div>

                    <!-- Key Metrics -->
                    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Avg Daily Hours</div>
                            <div style="font-size: 28px; font-weight: 700; color: #667eea; margin-bottom: 4px;">8.9h</div>
                            <div style="font-size: 12px; color: #10b981;">↑ 2% from last month</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Punctuality</div>
                            <div style="font-size: 28px; font-weight: 700; color: #10b981; margin-bottom: 4px;">96%</div>
                            <div style="font-size: 12px; color: #10b981;">Outstanding</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Total Hours (Month)</div>
                            <div style="font-size: 28px; font-weight: 700; color: #8b5cf6; margin-bottom: 4px;">166.8h</div>
                            <div style="font-size: 12px; color: #64748b;">vs 160h target</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Consistency Score</div>
                            <div style="font-size: 28px; font-weight: 700; color: #f59e0b; margin-bottom: 4px;">94/100</div>
                            <div style="font-size: 12px; color: #f59e0b;">Excellent</div>
                        </div>
                    </div>

                    <!-- Charts and Breakdown -->
                    <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px; margin-bottom: 24px;">
                        <!-- Weekly Breakdown -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Weekly Hours Breakdown</h3>
                            <div style="display: flex; align-items: flex-end; gap: 12px; height: 250px;">
                                ${['Mon', 'Tue', 'Wed', 'Thu', 'Fri'].map((day, idx) => {
                                    const hours = [9, 9.5, 8.5, 9.2, 8.8][idx];
                                    const height = (hours / 10) * 200;
                                    return `
                                        <div style="flex: 1; display: flex; flex-direction: column; align-items: center;">
                                            <div style="width: 100%; height: ${height}px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 8px 8px 0 0; transition: all 0.3s ease; cursor: pointer;" onmouseover="this.style.opacity='0.8'" onmouseout="this.style.opacity='1'"></div>
                                            <div style="margin-top: 8px; font-weight: 600; color: #0f172a; font-size: 13px;">${day}</div>
                                            <div style="font-size: 12px; color: #64748b; margin-top: 4px;">${hours}h</div>
                                        </div>
                                    `;
                                }).join('')}
                            </div>
                        </div>

                        <!-- Performance Summary -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Performance</h3>
                            <div style="display: flex; flex-direction: column; gap: 16px;">
                                ${[
                                    { label: 'Attendance', value: 96, color: '#10b981' },
                                    { label: 'Punctuality', value: 94, color: '#667eea' },
                                    { label: 'Consistency', value: 92, color: '#f59e0b' }
                                ].map(item => `
                                    <div>
                                        <div style="display: flex; justify-content: space-between; margin-bottom: 6px;">
                                            <span style="font-size: 13px; font-weight: 500; color: #0f172a;">${item.label}</span>
                                            <span style="font-size: 13px; font-weight: 700; color: ${item.color};">${item.value}%</span>
                                        </div>
                                        <div style="width: 100%; height: 8px; background: #e2e8f0; border-radius: 4px; overflow: hidden;">
                                            <div style="width: ${item.value}%; height: 100%; background: ${item.color}; border-radius: 4px;"></div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>

                    <!-- Attendance Summary -->
                    <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">This Month Summary</h3>
                        <div style="display: grid; grid-template-columns: repeat(5, 1fr); gap: 12px;">
                            ${[
                                { label: 'Days Worked', value: '18', icon: '✓', color: '#10b981' },
                                { label: 'On Time', value: '17', icon: '⏰', color: '#667eea' },
                                { label: 'Late Arrivals', value: '1', icon: '⚠', color: '#f59e0b' },
                                { label: 'Approved Leaves', value: '2', icon: '📅', color: '#8b5cf6' },
                                { label: 'Avg. Daily Hours', value: '8.9h', icon: '⏱', color: '#06b6d4' }
                            ].map(item => `
                                <div style="background: linear-gradient(135deg, ${item.color}10 0%, ${item.color}05 100%); padding: 16px; border-radius: 10px; border: 1px solid ${item.color}20; text-align: center;">
                                    <div style="font-size: 20px; margin-bottom: 4px;">${item.icon}</div>
                                    <div style="font-size: 20px; font-weight: 700; color: ${item.color}; margin-bottom: 4px;">${item.value}</div>
                                    <div style="font-size: 12px; color: #64748b;">${item.label}</div>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                </div>

                <!-- Leave View -->
                <div id="leave-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">Leave Requests</h1>
                        <p style="color: #64748b; margin-top: 4px;">Manage your leave applications and view your leave balance</p>
                    </div>

                    <!-- Leave Balance -->
                    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px;">
                        <div style="background: linear-gradient(135deg, #10b98120 0%, #05966920 100%); padding: 20px; border-radius: 12px; border: 1px solid #10b98140;">
                            <div style="font-size: 13px; color: #059669; margin-bottom: 8px; font-weight: 500;">Annual Leave</div>
                            <div style="font-size: 28px; font-weight: 700; color: #10b981; margin-bottom: 4px;">8 / 20</div>
                            <div style="font-size: 12px; color: #059669;">Used days</div>
                        </div>
                        <div style="background: linear-gradient(135deg, #8b5cf620 0%, #6d28d920 100%); padding: 20px; border-radius: 12px; border: 1px solid #8b5cf640;">
                            <div style="font-size: 13px; color: #7c3aed; margin-bottom: 8px; font-weight: 500;">Sick Leave</div>
                            <div style="font-size: 28px; font-weight: 700; color: #8b5cf6; margin-bottom: 4px;">2 / 10</div>
                            <div style="font-size: 12px; color: #7c3aed;">Used days</div>
                        </div>
                        <div style="background: linear-gradient(135deg, #f59e0b20 0%, #d9730620 100%); padding: 20px; border-radius: 12px; border: 1px solid #f59e0b40;">
                            <div style="font-size: 13px; color: #d97706; margin-bottom: 8px; font-weight: 500;">Casual Leave</div>
                            <div style="font-size: 28px; font-weight: 700; color: #f59e0b; margin-bottom: 4px;">3 / 8</div>
                            <div style="font-size: 12px; color: #d97706;">Used days</div>
                        </div>
                        <div style="background: linear-gradient(135deg, #06b6d420 0%, #088989a0 100%); padding: 20px; border-radius: 12px; border: 1px solid #06b6d440;">
                            <div style="font-size: 13px; color: #0891b2; margin-bottom: 8px; font-weight: 500;">Total Balance</div>
                            <div style="font-size: 28px; font-weight: 700; color: #06b6d4; margin-bottom: 4px;">12 days</div>
                            <div style="font-size: 12px; color: #0891b2;">Remaining</div>
                        </div>
                    </div>

                    <!-- Request Leave & History -->
                    <div style="display: grid; grid-template-columns: 1fr 2fr; gap: 24px;">
                        <!-- Request Leave Form -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Request New Leave</h3>
                            <div style="display: flex; flex-direction: column; gap: 16px;">
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Leave Type</label>
                                    <select style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                                        <option>Annual Leave</option>
                                        <option>Sick Leave</option>
                                        <option>Casual Leave</option>
                                    </select>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">From Date</label>
                                    <input type="date" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">To Date</label>
                                    <input type="date" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Reason</label>
                                    <textarea style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a; min-height: 80px; resize: vertical;" placeholder="Enter reason for leave"></textarea>
                                </div>
                                <button onclick="app.submitLeaveRequest()" style="padding: 12px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                    Submit Request
                                </button>
                            </div>
                        </div>

                        <!-- Leave History -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Leave History</h3>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                ${[
                                    { type: 'Annual Leave', dates: '04-05 Jan 2026', reason: 'Personal Work', status: 'Approved', color: '#10b981' },
                                    { type: 'Casual Leave', dates: '22 Dec 2025', reason: 'Family Event', status: 'Approved', color: '#10b981' },
                                    { type: 'Sick Leave', dates: '15 Dec 2025', reason: 'Medical', status: 'Approved', color: '#10b981' },
                                    { type: 'Annual Leave', dates: '10-12 Dec 2025', reason: 'Vacation', status: 'Pending', color: '#f59e0b' },
                                    { type: 'Casual Leave', dates: '08 Dec 2025', reason: 'Personal', status: 'Rejected', color: '#ef4444' }
                                ].map(leave => `
                                    <div style="padding: 14px; background: #f8fafc; border-radius: 10px; border-left: 4px solid ${leave.color};">
                                        <div style="display: flex; justify-content: space-between; align-items: start;">
                                            <div>
                                                <div style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">${leave.type}</div>
                                                <div style="font-size: 13px; color: #64748b; margin-bottom: 4px;">${leave.dates}</div>
                                                <div style="font-size: 12px; color: #94a3b8;">${leave.reason}</div>
                                            </div>
                                            <span style="display: inline-block; padding: 6px 12px; background: ${leave.color}20; color: ${leave.color}; border-radius: 6px; font-size: 12px; font-weight: 600;">${leave.status}</span>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Breaks View -->
                <div id="breaks-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">Break Management</h1>
                        <p style="color: #64748b; margin-top: 4px;">Track and manage your daily break times</p>
                    </div>

                    <!-- Today's Breaks Summary -->
                    <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Total Break Time (Today)</div>
                            <div style="font-size: 28px; font-weight: 700; color: #667eea; margin-bottom: 4px;">45 mins</div>
                            <div style="font-size: 12px; color: #94a3b8;">Out of 60 mins allowed</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Breaks Taken</div>
                            <div style="font-size: 28px; font-weight: 700; color: #10b981; margin-bottom: 4px;">2</div>
                            <div style="font-size: 12px; color: #94a3b8;">This session</div>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="font-size: 13px; color: #64748b; margin-bottom: 8px; font-weight: 500;">Remaining Time</div>
                            <div style="font-size: 28px; font-weight: 700; color: #f59e0b; margin-bottom: 4px;">15 mins</div>
                            <div style="font-size: 12px; color: #94a3b8;">Can take more breaks</div>
                        </div>
                    </div>

                    <!-- Quick Actions -->
                    <div style="display: flex; gap: 16px; margin-bottom: 24px;">
                        <button onclick="app.startBreak()" style="flex: 1; padding: 14px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);">
                            <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                            Start Break
                        </button>
                        <button onclick="app.endBreak()" style="flex: 1; padding: 14px; background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px; box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);">
                            <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                            End Break
                        </button>
                    </div>

                    <!-- Breaks Timeline & History -->
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px;">
                        <!-- Today's Breaks -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Today's Breaks</h3>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                ${[
                                    { time: '12:30 - 1:00 PM', duration: '30 mins', type: 'Lunch Break', status: 'Completed' },
                                    { time: '3:15 - 3:30 PM', duration: '15 mins', type: 'Coffee Break', status: 'Completed' },
                                    { time: 'Pending', duration: '-', type: 'Remaining Break', status: 'Available' }
                                ].map((brk, idx) => `
                                    <div style="padding: 12px; background: #f8fafc; border-radius: 10px; display: flex; justify-content: space-between; align-items: center;">
                                        <div>
                                            <div style="font-weight: 600; color: #0f172a; margin-bottom: 2px;">${brk.type}</div>
                                            <div style="font-size: 13px; color: #64748b;">${brk.time}</div>
                                        </div>
                                        <div style="text-align: right;">
                                            <div style="font-weight: 600; color: #667eea; margin-bottom: 2px;">${brk.duration}</div>
                                            <span style="display: inline-block; padding: 4px 10px; background: ${idx === 2 ? '#667eea20' : '#10b98120'}; color: ${idx === 2 ? '#667eea' : '#10b981'}; border-radius: 4px; font-size: 11px; font-weight: 600;">${brk.status}</span>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>

                        <!-- Break History -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">This Week's Breaks</h3>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                ${[
                                    { day: 'Monday', breaks: '2', totalTime: '45 mins', status: 'Normal' },
                                    { day: 'Tuesday', breaks: '3', totalTime: '60 mins', status: 'Over Limit' },
                                    { day: 'Wednesday', breaks: '2', totalTime: '40 mins', status: 'Normal' },
                                    { day: 'Thursday', breaks: '2', totalTime: '45 mins', status: 'Normal' },
                                    { day: 'Friday', breaks: '2', totalTime: '45 mins', status: 'Normal' }
                                ].map(day => `
                                    <div style="padding: 12px; background: #f8fafc; border-radius: 10px; display: flex; justify-content: space-between; align-items: center;">
                                        <div>
                                            <div style="font-weight: 600; color: #0f172a; margin-bottom: 2px;">${day.day}</div>
                                            <div style="font-size: 13px; color: #64748b;">${day.breaks} breaks</div>
                                        </div>
                                        <div style="text-align: right;">
                                            <div style="font-weight: 600; color: #667eea; margin-bottom: 2px;">${day.totalTime}</div>
                                            <span style="display: inline-block; padding: 4px 10px; background: ${day.status === 'Over Limit' ? '#f59e0b20' : '#10b98120'}; color: ${day.status === 'Over Limit' ? '#f59e0b' : '#10b981'}; border-radius: 4px; font-size: 11px; font-weight: 600;">${day.status}</span>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Profile View -->
                <div id="profile-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">My Profile</h1>
                        <p style="color: #64748b; margin-top: 4px;">View and manage your personal information</p>
                    </div>

                    <div style="display: grid; grid-template-columns: 1fr 2fr; gap: 24px;">
                        <!-- Profile Card -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04); text-align: center;">
                            <div style="width: 100px; height: 100px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 50%; margin: 0 auto 16px; display: flex; align-items: center; justify-content: center;">
                                <svg width="50" height="50" fill="white" viewBox="0 0 24 24"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>
                            </div>
                            <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">John Doe</h2>
                            <p style="color: #64748b; margin-bottom: 16px;">Senior Developer</p>
                            <div style="display: flex; flex-direction: column; gap: 10px;">
                                <button onclick="app.showEditProfileModal()" style="width: 100%; padding: 12px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                                    Edit Profile
                                </button>
                                <button onclick="app.showChangePasswordModal()" style="width: 100%; padding: 12px; background: white; color: #667eea; border: 2px solid #667eea; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>
                                    Change Password
                                </button>
                            </div>
                        </div>

                        <!-- Profile Details -->
                        <div style="background: white; padding: 24px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Profile Information</h3>
                            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 24px;">
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">First Name</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">John</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Last Name</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">Doe</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Email</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">john.doe@wizone.com</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Phone</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">+91 98765 43210</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Department</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">Engineering</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Designation</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">Senior Developer</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Employee ID</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">EMP-2024-001</div>
                                </div>
                                <div>
                                    <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Date of Joining</label>
                                    <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">15 Mar 2024</div>
                                </div>
                            </div>

                            <div style="border-top: 1px solid #e2e8f0; padding-top: 20px;">
                                <h4 style="font-size: 14px; font-weight: 700; color: #0f172a; margin-bottom: 16px;">Emergency Contact</h4>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
                                    <div>
                                        <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Name</label>
                                        <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">Jane Doe</div>
                                    </div>
                                    <div>
                                        <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Phone</label>
                                        <div style="padding: 12px 16px; background: #f8fafc; border-radius: 10px; color: #0f172a; font-weight: 500;">+91 98765 43211</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Notifications View -->
                <div id="notifications-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">Notifications</h1>
                        <p style="color: #64748b; margin-top: 4px;">Stay updated with all your notifications and messages</p>
                    </div>

                    <!-- Filter Tabs -->
                    <div style="display: flex; gap: 12px; margin-bottom: 24px;">
                        <button onclick="app.filterNotifications('all')" style="padding: 10px 18px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px;">
                            All
                        </button>
                        <button onclick="app.filterNotifications('unread')" style="padding: 10px 18px; background: #f8fafc; color: #667eea; border: 2px solid #667eea; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px;">
                            Unread (3)
                        </button>
                        <button onclick="app.filterNotifications('attendance')" style="padding: 10px 18px; background: #f8fafc; color: #0f172a; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px;">
                            Attendance
                        </button>
                        <button onclick="app.filterNotifications('leave')" style="padding: 10px 18px; background: #f8fafc; color: #0f172a; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px;">
                            Leave
                        </button>
                        <button onclick="app.filterNotifications('system')" style="padding: 10px 18px; background: #f8fafc; color: #0f172a; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 13px;">
                            System
                        </button>
                    </div>

                    <!-- Notifications List -->
                    <div style="display: flex; flex-direction: column; gap: 12px;">
                        ${[
                            { type: 'Attendance', title: 'Late Arrival Alert', message: 'You were 15 minutes late today. Ensure timely check-in from tomorrow.', time: '2 hours ago', unread: true, icon: '⏰', color: '#f59e0b' },
                            { type: 'Leave', title: 'Leave Approved', message: 'Your leave request for 04-05 Jan 2026 has been approved by your manager.', time: '4 hours ago', unread: true, icon: '✓', color: '#10b981' },
                            { type: 'System', title: 'System Update', message: 'A new version of the DTA system is now available. Please update your browser.', time: '8 hours ago', unread: true, icon: '⚙', color: '#667eea' },
                            { type: 'Attendance', title: 'Check-in Reminder', message: 'Don\'t forget to check in before 9:00 AM', time: '1 day ago', unread: false, icon: '🔔', color: '#8b5cf6' },
                            { type: 'Leave', title: 'Leave Balance Updated', message: 'Your monthly leave balance has been updated.', time: '2 days ago', unread: false, icon: '📊', color: '#06b6d4' },
                            { type: 'System', title: 'Maintenance Notice', message: 'Scheduled maintenance on Dec 22, 2025 from 10 PM to 2 AM IST.', time: '3 days ago', unread: false, icon: '🛠', color: '#64748b' },
                            { type: 'Attendance', title: 'Monthly Report Ready', message: 'Your attendance report for December is now available.', time: '1 week ago', unread: false, icon: '📋', color: '#06b6d4' }
                        ].map(notif => `
                            <div style="background: white; padding: 18px; border-radius: 12px; border: 1px solid ${notif.unread ? '#667eea40' : '#e2e8f0'}; ${notif.unread ? 'background: linear-gradient(135deg, #667eea05 0%, #764ba205 100%);' : ''} display: flex; gap: 16px; align-items: flex-start;">
                                <div style="width: 48px; height: 48px; background: ${notif.color}20; border-radius: 10px; display: flex; align-items: center; justify-content: center; font-size: 24px; flex-shrink: 0;">
                                    ${notif.icon}
                                </div>
                                <div style="flex: 1;">
                                    <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 4px;">
                                        <h4 style="font-weight: 700; color: #0f172a; margin: 0;">${notif.title}</h4>
                                        ${notif.unread ? '<span style="display: inline-block; width: 8px; height: 8px; background: #667eea; border-radius: 50%;"></span>' : ''}
                                    </div>
                                    <p style="color: #64748b; font-size: 14px; margin: 6px 0 0; line-height: 1.4;">${notif.message}</p>
                                    <div style="display: flex; gap: 16px; margin-top: 10px; font-size: 12px; color: #94a3b8;">
                                        <span>${notif.time}</span>
                                        <span style="color: #667eea; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 4px;">
                                            <svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
                                            Reply
                                        </span>
                                    </div>
                                </div>
                                <button onclick="app.deleteNotification(this)" style="background: none; border: none; color: #cbd5e1; cursor: pointer; font-size: 18px; padding: 4px; flex-shrink: 0;">
                                    ✕
                                </button>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        `;
    },

    // Render Admin Dashboard
    renderAdminDashboard() {
        const appContainer = document.getElementById('appContainer');
        appContainer.innerHTML = `
            <!-- Sidebar -->
            <div class="sidebar">
                <div class="logo-section">
                    <div class="admin-badge">ADMIN PANEL</div>
                    <div class="logo-icon" style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);">
                        <svg viewBox="0 0 24 24" style="width: 28px; height: 28px; fill: white;">
                            <path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 10.99h7c-.53 4.12-3.28 7.79-7 8.94V12H5V6.3l7-3.11v8.8z"/>
                        </svg>
                    </div>
                    <div class="app-title">Desktop Controller</div>
                    <div class="company-tag">WIZONE IT NETWORK</div>
                </div>
                
                <div class="menu-section">
                    <div class="menu-label">Overview</div>
                    <nav>
                        <a class="menu-item admin active" onclick="app.navigate('dashboard')" data-view="dashboard">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="3" width="7" height="7" rx="1"/>
                                <rect x="14" y="3" width="7" height="7" rx="1"/>
                                <rect x="14" y="14" width="7" height="7" rx="1"/>
                                <rect x="3" y="14" width="7" height="7" rx="1"/>
                            </svg>
                            <span>Dashboard</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('analytics')" data-view="analytics">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <line x1="18" y1="20" x2="18" y2="10"/>
                                <line x1="12" y1="20" x2="12" y2="4"/>
                                <line x1="6" y1="20" x2="6" y2="14"/>
                            </svg>
                            <span>Analytics & Insights</span>
                        </a>
                    </nav>
                    
                    <div class="menu-label" style="margin-top: 20px;">Management</div>
                    <nav>
                        <a class="menu-item admin" href="employee_management.html" data-view="employees">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                <circle cx="9" cy="7" r="4"/>
                                <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                            </svg>
                            <span>Employee Management</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('attendance')" data-view="attendance">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                                <line x1="16" y1="2" x2="16" y2="6"/>
                                <line x1="8" y1="2" x2="8" y2="6"/>
                                <line x1="3" y1="10" x2="21" y2="10"/>
                                <path d="M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01M16 18h.01"/>
                            </svg>
                            <span>Attendance Reports</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('leave')" data-view="leave">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                                <circle cx="12" cy="10" r="3"/>
                            </svg>
                            <span>Leave Management</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('departments')" data-view="departments">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
                                <polyline points="9 22 9 12 15 12 15 22"/>
                            </svg>
                            <span>Departments</span>
                        </a>
                    </nav>
                    
                    <div class="menu-label" style="margin-top: 20px;">Core Monitoring</div>
                    <nav>
                        <a class="menu-item admin" onclick="app.navigate('weblogs')" data-view="weblogs">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="10"/>
                                <line x1="2" y1="12" x2="22" y2="12"/>
                                <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                            </svg>
                            <span>Web Browsing Logs</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('applogs')" data-view="applogs">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
                                <line x1="8" y1="21" x2="16" y2="21"/>
                                <line x1="12" y1="17" x2="12" y2="21"/>
                            </svg>
                            <span>Application Usage</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('inactivitylogs')" data-view="inactivitylogs">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="10"/>
                                <polyline points="12 6 12 12 16 14"/>
                            </svg>
                            <span>Inactivity Logs</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('screenshots')" data-view="screenshots">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                                <circle cx="8.5" cy="8.5" r="1.5"/>
                                <polyline points="21 15 16 10 5 21"/>
                            </svg>
                            <span>Screenshots</span>
                        </a>
                    </nav>
                    
                    <div class="menu-label" style="margin-top: 20px;">System</div>
                    <nav>
                        <a class="menu-item admin" onclick="app.navigate('notifications')" data-view="notifications">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
                                <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
                            </svg>
                            <span>Notifications</span>
                        </a>
                        
                        <a class="menu-item admin" onclick="app.navigate('settings')" data-view="settings">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="12" cy="12" r="3"/>
                                <path d="M12 1v6m0 6v6m-9-9h6m6 0h6"/>
                                <path d="M4.2 4.2l4.2 4.2m7.2 0l4.2-4.2M4.2 19.8l4.2-4.2m7.2 0l4.2 4.2"/>
                            </svg>
                            <span>Settings</span>
                        </a>
                        
                        <a class="menu-item logout" onclick="app.logout()">
                            <svg class="menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
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
            <div class="main-content">
                <!-- Dashboard View -->
                <div id="dashboard-view" class="content-view active">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Admin Dashboard</h1>
                            <p style="color: #64748b; margin-top: 4px;">Welcome back! Here's what's happening today.</p>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button style="padding: 10px 20px; background: white; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                    <polyline points="7 10 12 15 17 10"/>
                                    <line x1="12" y1="15" x2="12" y2="3"/>
                                </svg>
                                Export
                            </button>
                            <button onclick="app.showAddEmployeeModal()" style="padding: 10px 20px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="12" y1="5" x2="12" y2="19"/>
                                    <line x1="5" y1="12" x2="19" y2="12"/>
                                </svg>
                                Add Employee
                            </button>
                        </div>
                    </div>

                    <!-- Stats Cards -->
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 24px; margin-bottom: 32px;">
                        <!-- Total Employees -->
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
                                <span style="background: #dcfce7; color: #16a34a; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">+12%</span>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">248</h3>
                            <p style="color: #64748b; font-size: 14px;">Total Employees</p>
                        </div>

                        <!-- Present Today -->
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                                        <polyline points="22 4 12 14.01 9 11.01"/>
                                    </svg>
                                </div>
                                <span style="background: #dcfce7; color: #16a34a; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">95%</span>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">236</h3>
                            <p style="color: #64748b; font-size: 14px;">Present Today</p>
                        </div>

                        <!-- On Leave -->
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
                                <span style="background: #fef3c7; color: #d97706; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">8</span>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">12</h3>
                            <p style="color: #64748b; font-size: 14px;">On Leave</p>
                        </div>

                        <!-- Pending Approvals -->
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <circle cx="12" cy="12" r="10"/>
                                        <polyline points="12 6 12 12 16 14"/>
                                    </svg>
                                </div>
                                <span style="background: #fee2e2; color: #dc2626; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">Urgent</span>
                            </div>
                            <h3 style="font-size: 28px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">5</h3>
                            <p style="color: #64748b; font-size: 14px;">Pending Approvals</p>
                        </div>
                    </div>

                    <!-- Recent Activity & Quick Actions -->
                    <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px; margin-bottom: 32px;">
                        <!-- Recent Activity -->
                        <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Recent Activity</h2>
                            <div style="display: flex; flex-direction: column; gap: 16px;">
                                ${[
                                    { name: 'John Doe', action: 'checked in', time: '2 minutes ago', color: '#10b981' },
                                    { name: 'Sarah Smith', action: 'applied for leave', time: '15 minutes ago', color: '#f59e0b' },
                                    { name: 'Mike Johnson', action: 'checked out', time: '28 minutes ago', color: '#6366f1' },
                                    { name: 'Emma Wilson', action: 'updated profile', time: '1 hour ago', color: '#8b5cf6' },
                                    { name: 'David Brown', action: 'checked in late', time: '2 hours ago', color: '#ef4444' }
                                ].map(activity => `
                                    <div style="display: flex; align-items: center; gap: 12px; padding: 12px; border-radius: 12px; background: #f8fafc;">
                                        <div style="width: 40px; height: 40px; background: ${activity.color}; border-radius: 10px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 14px;">
                                            ${activity.name.split(' ').map(n => n[0]).join('')}
                                        </div>
                                        <div style="flex: 1;">
                                            <p style="color: #0f172a; font-weight: 500; font-size: 14px;">${activity.name} <span style="color: #64748b; font-weight: 400;">${activity.action}</span></p>
                                            <p style="color: #94a3b8; font-size: 12px;">${activity.time}</p>
                                        </div>
                                        <button style="padding: 6px 12px; background: white; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-size: 12px; color: #64748b;">View</button>
                                    </div>
                                `).join('')}
                            </div>
                        </div>

                        <!-- Quick Actions -->
                        <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Quick Actions</h2>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                <button onclick="app.navigate('employees')" style="padding: 16px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 12px; cursor: pointer; text-align: left; font-weight: 500;">
                                    <div style="display: flex; align-items: center; gap: 12px;">
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                            <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                            <circle cx="8.5" cy="7" r="4"/>
                                            <line x1="20" y1="8" x2="20" y2="14"/>
                                            <line x1="23" y1="11" x2="17" y2="11"/>
                                        </svg>
                                        <span>Add New Employee</span>
                                    </div>
                                </button>
                                <button onclick="app.navigate('leave')" style="padding: 16px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">
                                    <div style="display: flex; align-items: center; gap: 12px;">
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                                            <polyline points="22 4 12 14.01 9 11.01"/>
                                        </svg>
                                        <span>Approve Leave Requests</span>
                                    </div>
                                </button>
                                <button onclick="app.navigate('attendance')" style="padding: 16px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">
                                    <div style="display: flex; align-items: center; gap: 12px;">
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                            <polyline points="7 10 12 15 17 10"/>
                                            <line x1="12" y1="15" x2="12" y2="3"/>
                                        </svg>
                                        <span>Generate Reports</span>
                                    </div>
                                </button>
                                <button onclick="app.navigate('settings')" style="padding: 16px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">
                                    <div style="display: flex; align-items: center; gap: 12px;">
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                            <circle cx="12" cy="12" r="3"/>
                                            <path d="M12 1v6m0 6v6"/>
                                        </svg>
                                        <span>System Settings</span>
                                    </div>
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- Department Overview -->
                    <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Department Overview</h2>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px;">
                            ${[
                                { dept: 'Engineering', count: 85, color: '#667eea' },
                                { dept: 'Sales', count: 52, color: '#10b981' },
                                { dept: 'Marketing', count: 34, color: '#f59e0b' },
                                { dept: 'HR', count: 28, color: '#ef4444' },
                                { dept: 'Finance', count: 22, color: '#8b5cf6' },
                                { dept: 'Operations', count: 27, color: '#06b6d4' }
                            ].map(dept => `
                                <div style="padding: 20px; background: #f8fafc; border-radius: 12px; text-align: center;">
                                    <div style="width: 48px; height: 48px; background: ${dept.color}; border-radius: 12px; display: flex; align-items: center; justify-content: center; margin: 0 auto 12px;">
                                        <span style="color: white; font-weight: 700; font-size: 18px;">${dept.count}</span>
                                    </div>
                                    <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 4px;">${dept.dept}</h3>
                                    <p style="color: #64748b; font-size: 13px;">Employees</p>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                </div>

                <!-- Analytics View -->
                <div id="analytics-view" class="content-view">
                    <div class="header">
                        <div>
                            <h1 class="page-title">Analytics & Insights</h1>
                            <p style="color: #64748b; margin-top: 4px;">Comprehensive analytics and performance metrics</p>
                        </div>
                    </div>

                    <!-- Key Metrics -->
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px; margin-bottom: 24px;">
                        <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 24px; border-radius: 16px; color: white;">
                            <p style="opacity: 0.9; font-size: 13px; margin-bottom: 8px;">Avg Attendance Rate</p>
                            <h3 style="font-size: 36px; font-weight: 700; margin-bottom: 8px;">94.5%</h3>
                            <p style="opacity: 0.8; font-size: 12px;">↑ 2.3% from last month</p>
                        </div>
                        <div style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 24px; border-radius: 16px; color: white;">
                            <p style="opacity: 0.9; font-size: 13px; margin-bottom: 8px;">Productivity Score</p>
                            <h3 style="font-size: 36px; font-weight: 700; margin-bottom: 8px;">87.2</h3>
                            <p style="opacity: 0.8; font-size: 12px;">↑ 5.1% improvement</p>
                        </div>
                        <div style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); padding: 24px; border-radius: 16px; color: white;">
                            <p style="opacity: 0.9; font-size: 13px; margin-bottom: 8px;">Avg Work Hours</p>
                            <h3 style="font-size: 36px; font-weight: 700; margin-bottom: 8px;">8.4h</h3>
                            <p style="opacity: 0.8; font-size: 12px;">Per employee daily</p>
                        </div>
                        <div style="background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); padding: 24px; border-radius: 16px; color: white;">
                            <p style="opacity: 0.9; font-size: 13px; margin-bottom: 8px;">Late Arrivals</p>
                            <h3 style="font-size: 36px; font-weight: 700; margin-bottom: 8px;">3.2%</h3>
                            <p style="opacity: 0.8; font-size: 12px;">↓ 1.8% reduced</p>
                        </div>
                    </div>

                    <!-- Charts Section -->
                    <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px; margin-bottom: 24px;">
                        <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Attendance Trend (Last 7 Days)</h2>
                            <div style="height: 250px; display: flex; align-items: end; gap: 12px; padding-top: 20px;">
                                ${['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day, idx) => {
                                    const heights = [95, 92, 94, 96, 93, 45, 38];
                                    return `
                                        <div style="flex: 1; display: flex; flex-direction: column; align-items: center; gap: 8px;">
                                            <span style="font-size: 12px; font-weight: 600; color: #667eea;">${heights[idx]}%</span>
                                            <div style="width: 100%; height: ${heights[idx] * 2}px; background: linear-gradient(180deg, #667eea 0%, #764ba2 100%); border-radius: 8px 8px 0 0;"></div>
                                            <span style="font-size: 12px; color: #64748b; margin-top: 8px;">${day}</span>
                                        </div>
                                    `;
                                }).join('')}
                            </div>
                        </div>
                        <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Leave Distribution</h2>
                            <div style="display: flex; flex-direction: column; gap: 16px; margin-top: 24px;">
                                ${[
                                    { type: 'Casual Leave', count: 45, color: '#667eea' },
                                    { type: 'Sick Leave', count: 28, color: '#ef4444' },
                                    { type: 'Earned Leave', count: 52, color: '#10b981' },
                                    { type: 'Comp Off', count: 15, color: '#f59e0b' }
                                ].map(leave => `
                                    <div>
                                        <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                                            <span style="font-size: 13px; color: #64748b;">${leave.type}</span>
                                            <span style="font-size: 13px; font-weight: 600; color: #0f172a;">${leave.count}</span>
                                        </div>
                                        <div style="width: 100%; height: 8px; background: #f1f5f9; border-radius: 4px; overflow: hidden;">
                                            <div style="width: ${(leave.count / 52) * 100}%; height: 100%; background: ${leave.color};"></div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>

                    <!-- Department Performance -->
                    <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Department Performance</h2>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 16px;">
                            ${[
                                { dept: 'Engineering', attendance: 96, productivity: 92, employees: 85 },
                                { dept: 'Sales', attendance: 94, productivity: 88, employees: 52 },
                                { dept: 'Marketing', attendance: 93, productivity: 85, employees: 34 },
                                { dept: 'HR', attendance: 97, productivity: 94, employees: 28 },
                                { dept: 'Finance', attendance: 95, productivity: 90, employees: 22 },
                                { dept: 'Operations', attendance: 92, productivity: 87, employees: 27 }
                            ].map(dept => `
                                <div style="padding: 20px; background: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0;">
                                    <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 16px;">${dept.dept}</h3>
                                    <div style="display: flex; flex-direction: column; gap: 12px;">
                                        <div>
                                            <div style="display: flex; justify-content: space-between; margin-bottom: 6px;">
                                                <span style="font-size: 12px; color: #64748b;">Attendance</span>
                                                <span style="font-size: 12px; font-weight: 600; color: #10b981;">${dept.attendance}%</span>
                                            </div>
                                            <div style="width: 100%; height: 6px; background: #e2e8f0; border-radius: 3px;">
                                                <div style="width: ${dept.attendance}%; height: 100%; background: #10b981; border-radius: 3px;"></div>
                                            </div>
                                        </div>
                                        <div>
                                            <div style="display: flex; justify-content: space-between; margin-bottom: 6px;">
                                                <span style="font-size: 12px; color: #64748b;">Productivity</span>
                                                <span style="font-size: 12px; font-weight: 600; color: #667eea;">${dept.productivity}%</span>
                                            </div>
                                            <div style="width: 100%; height: 6px; background: #e2e8f0; border-radius: 3px;">
                                                <div style="width: ${dept.productivity}%; height: 100%; background: #667eea; border-radius: 3px;"></div>
                                            </div>
                                        </div>
                                        <p style="font-size: 12px; color: #64748b; margin-top: 4px;">${dept.employees} employees</p>
                                    </div>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                </div>

                <!-- Employees View -->
                <div id="employees-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Employee Management</h1>
                            <p style="color: #64748b; margin-top: 4px;">Manage your team members and their information</p>
                        </div>
                        <button style="padding: 12px 24px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <line x1="12" y1="5" x2="12" y2="19"/>
                                <line x1="5" y1="12" x2="19" y2="12"/>
                            </svg>
                            Add New Employee
                        </button>
                    </div>

                    <!-- Search and Filters -->
                    <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); margin-bottom: 24px;">
                        <div style="display: flex; gap: 16px; flex-wrap: wrap;">
                            <div style="flex: 1; min-width: 250px;">
                                <input type="text" placeholder="Search employees..." style="width: 100%; padding: 12px 16px; border: 1px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            </div>
                            <select style="padding: 12px 16px; border: 1px solid #e2e8f0; border-radius: 10px; font-size: 14px; cursor: pointer;">
                                <option>All Departments</option>
                                <option>Engineering</option>
                                <option>Sales</option>
                                <option>Marketing</option>
                                <option>HR</option>
                                <option>Finance</option>
                            </select>
                            <select style="padding: 12px 16px; border: 1px solid #e2e8f0; border-radius: 10px; font-size: 14px; cursor: pointer;">
                                <option>All Status</option>
                                <option>Active</option>
                                <option>On Leave</option>
                                <option>Inactive</option>
                            </select>
                            <button style="padding: 12px 20px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                    <polyline points="7 10 12 15 17 10"/>
                                    <line x1="12" y1="15" x2="12" y2="3"/>
                                </svg>
                                Export
                            </button>
                        </div>
                    </div>

                    <!-- Employee Table -->
                    <div style="background: white; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); overflow: hidden;">
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f8fafc;">
                                <tr>
                                    <th style="padding: 16px 24px; text-align: left; font-size: 13px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Employee</th>
                                    <th style="padding: 16px 24px; text-align: left; font-size: 13px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Employee ID</th>
                                    <th style="padding: 16px 24px; text-align: left; font-size: 13px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Department</th>
                                    <th style="padding: 16px 24px; text-align: left; font-size: 13px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Designation</th>
                                    <th style="padding: 16px 24px; text-align: left; font-size: 13px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Status</th>
                                    <th style="padding: 16px 24px; text-align: left; font-size: 13px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${[
                                    { name: 'John Doe', id: 'EMP001', dept: 'Engineering', role: 'Senior Developer', status: 'Active', color: '#10b981' },
                                    { name: 'Sarah Smith', id: 'EMP002', dept: 'Marketing', role: 'Marketing Manager', status: 'Active', color: '#10b981' },
                                    { name: 'Mike Johnson', id: 'EMP003', dept: 'Sales', role: 'Sales Executive', status: 'On Leave', color: '#f59e0b' },
                                    { name: 'Emma Wilson', id: 'EMP004', dept: 'HR', role: 'HR Manager', status: 'Active', color: '#10b981' },
                                    { name: 'David Brown', id: 'EMP005', dept: 'Finance', role: 'Accountant', status: 'Active', color: '#10b981' },
                                    { name: 'Lisa Anderson', id: 'EMP006', dept: 'Engineering', role: 'UI/UX Designer', status: 'Active', color: '#10b981' },
                                    { name: 'James Taylor', id: 'EMP007', dept: 'Operations', role: 'Operations Head', status: 'Active', color: '#10b981' },
                                    { name: 'Maria Garcia', id: 'EMP008', dept: 'Marketing', role: 'Content Writer', status: 'Active', color: '#10b981' }
                                ].map((emp, idx) => `
                                    <tr style="border-top: 1px solid #f1f5f9;">
                                        <td style="padding: 20px 24px;">
                                            <div style="display: flex; align-items: center; gap: 12px;">
                                                <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 14px;">
                                                    ${emp.name.split(' ').map(n => n[0]).join('')}
                                                </div>
                                                <div>
                                                    <p style="font-weight: 500; color: #0f172a; font-size: 14px;">${emp.name}</p>
                                                    <p style="color: #94a3b8; font-size: 13px;">${emp.name.toLowerCase().replace(' ', '.')}@wizone.in</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td style="padding: 20px 24px; color: #64748b; font-size: 14px;">${emp.id}</td>
                                        <td style="padding: 20px 24px; color: #0f172a; font-size: 14px; font-weight: 500;">${emp.dept}</td>
                                        <td style="padding: 20px 24px; color: #64748b; font-size: 14px;">${emp.role}</td>
                                        <td style="padding: 20px 24px;">
                                            <span style="background: ${emp.status === 'Active' ? '#dcfce7' : '#fef3c7'}; color: ${emp.status === 'Active' ? '#16a34a' : '#d97706'}; padding: 6px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                                                ${emp.status}
                                            </span>
                                        </td>
                                        <td style="padding: 20px 24px;">
                                            <div style="display: flex; gap: 8px;">
                                                <button onclick="app.showEditEmployeeModal('${emp.id}', '${emp.name}', '${emp.name.toLowerCase().replace(' ', '.')}@wizone.in', '${emp.dept}', '${emp.role}')" style="padding: 8px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer;" title="Edit">
                                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                                                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                                    </svg>
                                                </button>
                                                <button onclick="app.showViewEmployeeModal('${emp.name}', '${emp.name.toLowerCase().replace(' ', '.')}@wizone.in', '${emp.dept}', '${emp.role}', '${emp.status}')" style="padding: 8px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer;" title="View">
                                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                                                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                                                        <circle cx="12" cy="12" r="3"/>
                                                    </svg>
                                                </button>
                                                <button onclick="app.confirmDelete('${emp.name}')" style="padding: 8px; background: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; cursor: pointer;" title="Delete">
                                                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#dc2626" stroke-width="2">
                                                        <polyline points="3 6 5 6 21 6"/>
                                                        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
                                                    </svg>
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                        <div style="padding: 20px 24px; border-top: 1px solid #f1f5f9; display: flex; justify-content: between; align-items: center;">
                            <p style="color: #64748b; font-size: 14px;">Showing 1 to 8 of 248 employees</p>
                            <div style="display: flex; gap: 8px; margin-left: auto;">
                                <button style="padding: 8px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; color: #64748b;">Previous</button>
                                <button style="padding: 8px 12px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border: none; border-radius: 8px; cursor: pointer; color: white; font-weight: 500;">1</button>
                                <button style="padding: 8px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; color: #64748b;">2</button>
                                <button style="padding: 8px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; color: #64748b;">3</button>
                                <button style="padding: 8px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; color: #64748b;">Next</button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Attendance View -->
                <div id="attendance-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Attendance Reports</h1>
                            <p style="color: #64748b; margin-top: 4px;">Track and manage employee attendance records</p>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button style="padding: 10px 20px; background: white; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="23 4 23 10 17 10"/>
                                    <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/>
                                </svg>
                                Refresh
                            </button>
                            <button style="padding: 10px 20px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                    <polyline points="7 10 12 15 17 10"/>
                                    <line x1="12" y1="15" x2="12" y2="3"/>
                                </svg>
                                Export Report
                            </button>
                        </div>
                    </div>

                    <!-- Summary Cards -->
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">Present Today</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #10b981;">236</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">95.2% attendance</p>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">On Leave</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #f59e0b;">12</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">4.8% on leave</p>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">Late Arrivals</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #ef4444;">8</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">3.2% late today</p>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">Avg Work Hours</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #667eea;">8.5</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">hours per employee</p>
                        </div>
                    </div>

                    <!-- Filters -->
                    <div style="background: white; padding: 20px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); margin-bottom: 24px;">
                        <div style="display: flex; gap: 16px; flex-wrap: wrap; align-items: center;">
                            <div>
                                <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Date Range</label>
                                <input type="date" value="2026-01-07" style="padding: 10px 14px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                            </div>
                            <div>
                                <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">To</label>
                                <input type="date" value="2026-01-07" style="padding: 10px 14px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                            </div>
                            <div style="flex: 1; min-width: 200px;">
                                <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Department</label>
                                <select style="width: 100%; padding: 10px 14px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px; cursor: pointer;">
                                    <option>All Departments</option>
                                    <option>Engineering</option>
                                    <option>Sales</option>
                                    <option>Marketing</option>
                                    <option>HR</option>
                                </select>
                            </div>
                            <div style="flex: 1; min-width: 200px;">
                                <label style="display: block; color: #64748b; font-size: 12px; margin-bottom: 6px; font-weight: 500;">Status</label>
                                <select style="width: 100%; padding: 10px 14px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px; cursor: pointer;">
                                    <option>All Status</option>
                                    <option>Present</option>
                                    <option>Absent</option>
                                    <option>Late</option>
                                    <option>Half Day</option>
                                </select>
                            </div>
                            <button style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; margin-top: 18px;">
                                Apply Filter
                            </button>
                        </div>
                    </div>

                    <!-- Attendance Table -->
                    <div style="background: white; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); overflow: hidden;">
                        <div style="padding: 20px 24px; border-bottom: 1px solid #f1f5f9; display: flex; justify-content: space-between; align-items: center;">
                            <h2 style="font-size: 16px; font-weight: 600; color: #0f172a;">Today's Attendance - January 7, 2026</h2>
                            <input type="text" placeholder="Search employee..." style="padding: 8px 16px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px; width: 250px;">
                        </div>
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f8fafc;">
                                <tr>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Employee</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Check In</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Check Out</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Work Hours</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Break Time</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Status</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${[
                                    { name: 'John Doe', dept: 'Engineering', checkIn: '09:00 AM', checkOut: '06:15 PM', hours: '8.5h', break: '45m', status: 'On Time', statusColor: '#10b981' },
                                    { name: 'Sarah Smith', dept: 'Marketing', checkIn: '09:15 AM', checkOut: '06:00 PM', hours: '8.2h', break: '38m', status: 'Late', statusColor: '#f59e0b' },
                                    { name: 'Mike Johnson', dept: 'Sales', checkIn: '-', checkOut: '-', hours: '-', break: '-', status: 'On Leave', statusColor: '#64748b' },
                                    { name: 'Emma Wilson', dept: 'HR', checkIn: '08:55 AM', checkOut: '06:10 PM', hours: '8.7h', break: '52m', status: 'On Time', statusColor: '#10b981' },
                                    { name: 'David Brown', dept: 'Finance', checkIn: '09:30 AM', checkOut: 'Active', hours: '6.2h', break: '28m', status: 'Active', statusColor: '#667eea' },
                                    { name: 'Lisa Anderson', dept: 'Engineering', checkIn: '09:05 AM', checkOut: '06:20 PM', hours: '8.6h', break: '41m', status: 'On Time', statusColor: '#10b981' },
                                    { name: 'James Taylor', dept: 'Operations', checkIn: '08:50 AM', checkOut: '06:05 PM', hours: '8.8h', break: '50m', status: 'On Time', statusColor: '#10b981' },
                                    { name: 'Maria Garcia', dept: 'Marketing', checkIn: '09:45 AM', checkOut: 'Active', hours: '5.8h', break: '22m', status: 'Late', statusColor: '#f59e0b' }
                                ].map(emp => `
                                    <tr style="border-top: 1px solid #f1f5f9;">
                                        <td style="padding: 18px 24px;">
                                            <div style="display: flex; align-items: center; gap: 12px;">
                                                <div style="width: 36px; height: 36px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 13px;">
                                                    ${emp.name.split(' ').map(n => n[0]).join('')}
                                                </div>
                                                <div>
                                                    <p style="font-weight: 500; color: #0f172a; font-size: 14px;">${emp.name}</p>
                                                    <p style="color: #94a3b8; font-size: 12px;">${emp.dept}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td style="padding: 18px 24px; color: #0f172a; font-size: 14px; font-weight: 500;">${emp.checkIn}</td>
                                        <td style="padding: 18px 24px; color: #0f172a; font-size: 14px; font-weight: 500;">${emp.checkOut}</td>
                                        <td style="padding: 18px 24px; color: #64748b; font-size: 14px;">${emp.hours}</td>
                                        <td style="padding: 18px 24px; color: #64748b; font-size: 14px;">${emp.break}</td>
                                        <td style="padding: 18px 24px;">
                                            <span style="background: ${emp.statusColor === '#10b981' ? '#dcfce7' : emp.statusColor === '#f59e0b' ? '#fef3c7' : emp.statusColor === '#667eea' ? '#e0e7ff' : '#f1f5f9'}; color: ${emp.statusColor}; padding: 6px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                                                ${emp.status}
                                            </span>
                                        </td>
                                        <td style="padding: 18px 24px;">
                                            <button style="padding: 6px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-size: 12px; color: #64748b; font-weight: 500;">View Details</button>
                                        </td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                        <div style="padding: 16px 24px; border-top: 1px solid #f1f5f9; display: flex; justify-content: space-between; align-items: center;">
                            <p style="color: #64748b; font-size: 13px;">Showing 8 of 248 employees</p>
                            <div style="display: flex; gap: 6px;">
                                <button style="padding: 6px 10px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer; font-size: 12px; color: #64748b;">Previous</button>
                                <button style="padding: 6px 10px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border: none; border-radius: 6px; cursor: pointer; color: white; font-weight: 500; font-size: 12px;">1</button>
                                <button style="padding: 6px 10px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer; font-size: 12px; color: #64748b;">2</button>
                                <button style="padding: 6px 10px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer; font-size: 12px; color: #64748b;">3</button>
                                <button style="padding: 6px 10px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer; font-size: 12px; color: #64748b;">Next</button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Leave View -->
                <div id="leave-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Leave Management</h1>
                            <p style="color: #64748b; margin-top: 4px;">Review and manage employee leave requests</p>
                        </div>
                        <button style="padding: 10px 20px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                                <line x1="16" y1="2" x2="16" y2="6"/>
                                <line x1="8" y1="2" x2="8" y2="6"/>
                                <line x1="3" y1="10" x2="21" y2="10"/>
                            </svg>
                            Leave Calendar
                        </button>
                    </div>

                    <!-- Leave Stats -->
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 24px;">
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">Pending Requests</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #f59e0b;">5</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">Awaiting approval</p>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">Approved Today</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #10b981;">3</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">Leaves approved</p>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">On Leave Now</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #667eea;">12</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">Currently on leave</p>
                        </div>
                        <div style="background: white; padding: 20px; border-radius: 12px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <p style="color: #64748b; font-size: 13px; margin-bottom: 8px;">This Month</p>
                            <h3 style="font-size: 32px; font-weight: 700; color: #64748b;">28</h3>
                            <p style="color: #64748b; font-size: 12px; margin-top: 4px;">Total leave days</p>
                        </div>
                    </div>

                    <!-- Pending Requests -->
                    <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); margin-bottom: 24px;">
                        <h2 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px; display: flex; align-items: center; gap: 8px;">
                            <span style="background: #fef3c7; color: #d97706; padding: 4px 10px; border-radius: 8px; font-size: 12px; font-weight: 600;">5</span>
                            Pending Approval Requests
                        </h2>
                        <div style="display: flex; flex-direction: column; gap: 16px;">
                            ${[
                                { name: 'Sarah Smith', type: 'Casual Leave', from: 'Jan 10, 2026', to: 'Jan 12, 2026', days: '3', reason: 'Family function', applied: '2 hours ago' },
                                { name: 'Mike Johnson', type: 'Sick Leave', from: 'Jan 08, 2026', to: 'Jan 09, 2026', days: '2', reason: 'Medical checkup', applied: '5 hours ago' },
                                { name: 'Emma Wilson', type: 'Earned Leave', from: 'Jan 15, 2026', to: 'Jan 20, 2026', days: '6', reason: 'Vacation trip', applied: '1 day ago' },
                                { name: 'David Brown', type: 'Comp Off', from: 'Jan 11, 2026', to: 'Jan 11, 2026', days: '1', reason: 'Worked on weekend', applied: '1 day ago' },
                                { name: 'Lisa Anderson', type: 'Casual Leave', from: 'Jan 13, 2026', to: 'Jan 14, 2026', days: '2', reason: 'Personal work', applied: '2 days ago' }
                            ].map(leave => `
                                <div style="padding: 20px; background: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0;">
                                    <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 16px;">
                                        <div style="display: flex; align-items: center; gap: 12px; flex: 1;">
                                            <div style="width: 48px; height: 48px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 16px;">
                                                ${leave.name.split(' ').map(n => n[0]).join('')}
                                            </div>
                                            <div>
                                                <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 4px;">${leave.name}</h3>
                                                <p style="color: #64748b; font-size: 13px;">Applied ${leave.applied}</p>
                                            </div>
                                        </div>
                                        <span style="background: ${leave.type === 'Sick Leave' ? '#fee2e2' : leave.type === 'Casual Leave' ? '#dbeafe' : leave.type === 'Earned Leave' ? '#dcfce7' : '#fef3c7'}; color: ${leave.type === 'Sick Leave' ? '#dc2626' : leave.type === 'Casual Leave' ? '#2563eb' : leave.type === 'Earned Leave' ? '#16a34a' : '#d97706'}; padding: 6px 14px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                                            ${leave.type}
                                        </span>
                                    </div>
                                    <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 16px; padding: 16px; background: white; border-radius: 8px;">
                                        <div>
                                            <p style="color: #94a3b8; font-size: 11px; text-transform: uppercase; margin-bottom: 4px; font-weight: 600;">From Date</p>
                                            <p style="color: #0f172a; font-size: 14px; font-weight: 500;">${leave.from}</p>
                                        </div>
                                        <div>
                                            <p style="color: #94a3b8; font-size: 11px; text-transform: uppercase; margin-bottom: 4px; font-weight: 600;">To Date</p>
                                            <p style="color: #0f172a; font-size: 14px; font-weight: 500;">${leave.to}</p>
                                        </div>
                                        <div>
                                            <p style="color: #94a3b8; font-size: 11px; text-transform: uppercase; margin-bottom: 4px; font-weight: 600;">Duration</p>
                                            <p style="color: #0f172a; font-size: 14px; font-weight: 500;">${leave.days} Days</p>
                                        </div>
                                        <div>
                                            <p style="color: #94a3b8; font-size: 11px; text-transform: uppercase; margin-bottom: 4px; font-weight: 600;">Reason</p>
                                            <p style="color: #0f172a; font-size: 14px; font-weight: 500;">${leave.reason}</p>
                                        </div>
                                    </div>
                                    <div style="display: flex; gap: 12px;">
                                        <button onclick="app.showApproveLeaveModal('${leave.name}', '${leave.type}', '${leave.from} - ${leave.to}')" style="flex: 1; padding: 10px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; display: flex; align-items: center; justify-content: center; gap: 6px;">
                                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                                <polyline points="20 6 9 17 4 12"/>
                                            </svg>
                                            Approve
                                        </button>
                                        <button onclick="app.showRejectLeaveModal('${leave.name}', '${leave.type}', '${leave.from} - ${leave.to}')" style="flex: 1; padding: 10px; background: white; color: #dc2626; border: 1px solid #fecaca; border-radius: 8px; cursor: pointer; font-weight: 500; display: flex; align-items: center; justify-content: center; gap: 6px;">
                                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                                <line x1="18" y1="6" x2="6" y2="18"/>
                                                <line x1="6" y1="6" x2="18" y2="18"/>
                                            </svg>
                                            Reject
                                        </button>
                                        <button style="padding: 10px 20px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-weight: 500; color: #64748b;">View Details</button>
                                    </div>
                                </div>
                            `).join('')}
                        </div>
                    </div>

                    <!-- Recent Leave History -->
                    <div style="background: white; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04); overflow: hidden;">
                        <div style="padding: 20px 24px; border-bottom: 1px solid #f1f5f9;">
                            <h2 style="font-size: 18px; font-weight: 700; color: #0f172a;">Recent Leave History</h2>
                        </div>
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f8fafc;">
                                <tr>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Employee</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Leave Type</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Duration</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Applied On</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Status</th>
                                    <th style="padding: 14px 24px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase;">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${[
                                    { name: 'John Doe', type: 'Casual Leave', duration: 'Jan 5-7, 2026', days: '3 Days', applied: 'Jan 2, 2026', status: 'Approved', statusColor: '#10b981' },
                                    { name: 'James Taylor', type: 'Sick Leave', duration: 'Jan 4, 2026', days: '1 Day', applied: 'Jan 3, 2026', status: 'Approved', statusColor: '#10b981' },
                                    { name: 'Maria Garcia', type: 'Earned Leave', duration: 'Dec 28-31, 2025', days: '4 Days', applied: 'Dec 20, 2025', status: 'Approved', statusColor: '#10b981' },
                                    { name: 'Robert Wilson', type: 'Casual Leave', duration: 'Jan 2, 2026', days: '1 Day', applied: 'Dec 30, 2025', status: 'Rejected', statusColor: '#ef4444' }
                                ].map(leave => `
                                    <tr style="border-top: 1px solid #f1f5f9;">
                                        <td style="padding: 18px 24px;">
                                            <div style="display: flex; align-items: center; gap: 12px;">
                                                <div style="width: 36px; height: 36px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 13px;">
                                                    ${leave.name.split(' ').map(n => n[0]).join('')}
                                                </div>
                                                <p style="font-weight: 500; color: #0f172a; font-size: 14px;">${leave.name}</p>
                                            </div>
                                        </td>
                                        <td style="padding: 18px 24px; color: #0f172a; font-size: 14px;">${leave.type}</td>
                                        <td style="padding: 18px 24px;">
                                            <p style="color: #0f172a; font-size: 14px; font-weight: 500;">${leave.duration}</p>
                                            <p style="color: #94a3b8; font-size: 12px;">${leave.days}</p>
                                        </td>
                                        <td style="padding: 18px 24px; color: #64748b; font-size: 14px;">${leave.applied}</td>
                                        <td style="padding: 18px 24px;">
                                            <span style="background: ${leave.statusColor === '#10b981' ? '#dcfce7' : '#fee2e2'}; color: ${leave.statusColor}; padding: 6px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                                                ${leave.status}
                                            </span>
                                        </td>
                                        <td style="padding: 18px 24px;">
                                            <button style="padding: 6px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-size: 12px; color: #64748b; font-weight: 500;">View</button>
                                        </td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>

                <!-- Departments View -->
                <div id="departments-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Departments</h1>
                            <p style="color: #64748b; margin-top: 4px;">Manage organizational departments</p>
                        </div>
                        <button onclick="app.showAddDepartmentModal()" style="padding: 12px 24px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500;">+ Add Department</button>
                    </div>

                    <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 24px;">
                        ${[
                            { name: 'Engineering', head: 'John Smith', employees: 85, location: 'Building A', color: '#667eea' },
                            { name: 'Sales', head: 'Sarah Johnson', employees: 52, location: 'Building B', color: '#10b981' },
                            { name: 'Marketing', head: 'Mike Davis', employees: 34, location: 'Building A', color: '#f59e0b' },
                            { name: 'HR', head: 'Emma Wilson', employees: 28, location: 'Building C', color: '#ef4444' },
                            { name: 'Finance', head: 'David Brown', employees: 22, location: 'Building B', color: '#8b5cf6' },
                            { name: 'Operations', head: 'Lisa Anderson', employees: 27, location: 'Building C', color: '#06b6d4' }
                        ].map(dept => `
                            <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                                <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 20px;">
                                    <div style="width: 56px; height: 56px; background: ${dept.color}; border-radius: 14px; display: flex; align-items: center; justify-content: center;">
                                        <span style="color: white; font-weight: 700; font-size: 20px;">${dept.employees}</span>
                                    </div>
                                    <button style="padding: 6px 10px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer;">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                                            <circle cx="12" cy="12" r="1"/><circle cx="12" cy="5" r="1"/><circle cx="12" cy="19" r="1"/>
                                        </svg>
                                    </button>
                                </div>
                                <h3 style="font-size: 20px; font-weight: 700; color: #0f172a; margin-bottom: 8px;">${dept.name}</h3>
                                <p style="color: #64748b; font-size: 14px; margin-bottom: 16px;">Department Head: ${dept.head}</p>
                                <div style="display: flex; justify-content: space-between; padding-top: 16px; border-top: 1px solid #f1f5f9;">
                                    <div>
                                        <p style="font-size: 12px; color: #94a3b8; margin-bottom: 4px;">Employees</p>
                                        <p style="font-size: 16px; font-weight: 600; color: #0f172a;">${dept.employees}</p>
                                    </div>
                                    <div>
                                        <p style="font-size: 12px; color: #94a3b8; margin-bottom: 4px;">Location</p>
                                        <p style="font-size: 16px; font-weight: 600; color: #0f172a;">${dept.location}</p>
                                    </div>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>

                <!-- Notifications View -->
                <div id="notifications-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Notifications</h1>
                            <p style="color: #64748b; margin-top: 4px;">Send and manage system notifications</p>
                        </div>
                        <button onclick="app.showSendNotificationModal()" style="padding: 12px 24px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500;">+ Send Notification</button>
                    </div>

                    <div style="display: grid; grid-template-columns: 1fr 2fr; gap: 24px;">
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 16px;">Quick Actions</h3>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                <button onclick="app.showSendNotificationModal()" style="padding: 14px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">Send to All</button>
                                <button onclick="app.showSendNotificationModal()" style="padding: 14px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">Send to Department</button>
                                <button onclick="app.showSendNotificationModal()" style="padding: 14px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">Send to Individual</button>
                                <button onclick="app.showSendNotificationModal()" style="padding: 14px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: left; font-weight: 500; color: #0f172a;">Schedule Notification</button>
                            </div>
                        </div>

                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 16px;">Recent Notifications</h3>
                            <div style="display: flex; flex-direction: column; gap: 12px;">
                                ${[
                                    { title: 'Holiday Announcement', message: 'Office will be closed on Jan 26', time: '2 hours ago', type: 'info' },
                                    { title: 'Attendance Reminder', message: 'Please mark your attendance daily', time: '5 hours ago', type: 'warning' },
                                    { title: 'System Maintenance', message: 'Scheduled maintenance on Saturday', time: '1 day ago', type: 'alert' }
                                ].map(notif => `
                                    <div style="padding: 16px; background: #f8fafc; border-radius: 10px; border-left: 3px solid ${notif.type === 'info' ? '#667eea' : notif.type === 'warning' ? '#f59e0b' : '#ef4444'};">
                                        <div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
                                            <h4 style="font-size: 14px; font-weight: 600; color: #0f172a;">${notif.title}</h4>
                                            <span style="font-size: 12px; color: #94a3b8;">${notif.time}</span>
                                        </div>
                                        <p style="font-size: 13px; color: #64748b;">${notif.message}</p>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Settings View -->
                <div id="settings-view" class="content-view">
                    <div class="header">
                        <h1 class="page-title">System Settings</h1>
                        <p style="color: #64748b; margin-top: 4px;">Configure system preferences and policies</p>
                    </div>

                    <div style="display: grid; grid-template-columns: 1fr 2fr; gap: 24px;">
                        <div style="background: white; padding: 24px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 16px;">Settings Menu</h3>
                            ${[
                                { name: 'General', id: 'general' },
                                { name: 'Attendance', id: 'attendance' },
                                { name: 'Leave Policies', id: 'leave' },
                                { name: 'Notifications', id: 'notifications' },
                                { name: 'Security', id: 'security' },
                                { name: 'Integrations', id: 'integrations' }
                            ].map((item, idx) => `
                                <button onclick="app.showSettingsSection('${item.id}')" style="width: 100%; padding: 14px; margin-bottom: 8px; background: ${idx === 0 ? 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)' : '#f8fafc'}; color: ${idx === 0 ? 'white' : '#0f172a'}; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; transition: all 0.2s ease;">
                                    ${item.name}
                                </button>
                            `).join('')}
                        </div>

                        <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                            <div id="settingsContent">
                                <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">General Settings</h3>
                                <div style="display: flex; flex-direction: column; gap: 24px;">
                                    ${[
                                        { label: 'Company Name', value: 'WIZONE IT NETWORK INDIA PVT LTD', id: 'companyName' },
                                        { label: 'Working Hours', value: '9:00 AM - 6:00 PM', id: 'workingHours' },
                                        { label: 'Working Days', value: 'Monday - Friday', id: 'workingDays' },
                                        { label: 'Time Zone', value: 'Asia/Kolkata (IST)', id: 'timeZone' }
                                    ].map(setting => `
                                        <div>
                                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">${setting.label}</label>
                                            <input type="text" id="${setting.id}" value="${setting.value}" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a; transition: all 0.2s ease;" onchange="app.markSettingsChanged()">
                                        </div>
                                    `).join('')}
                                    <div style="display: flex; gap: 12px; margin-top: 16px;">
                                        <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; transition: all 0.2s ease; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                            <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                            Save Changes
                                        </button>
                                        <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; transition: all 0.2s ease; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                            <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                            Reset
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Web Browsing Logs View -->
                <div id="weblogs-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Web Browsing Logs</h1>
                            <p style="color: #64748b; margin-top: 4px;">Monitor web browsing activity across all systems</p>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.refreshWebLogs()" style="padding: 10px 20px; background: white; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/>
                                </svg>
                                Refresh
                            </button>
                            <button onclick="app.exportWebLogs()" style="padding: 10px 20px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>
                                </svg>
                                Export
                            </button>
                        </div>
                    </div>
                    <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <div style="display: flex; gap: 12px; margin-bottom: 20px;">
                            <input type="text" id="weblogsSearch" placeholder="Search URLs or domains..." style="flex: 1; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            <input type="date" id="weblogsDate" style="padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            <button onclick="app.loadWebLogs()" style="padding: 12px 24px; background: #667eea; color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500;">Search</button>
                        </div>
                        <div id="weblogsTable" style="overflow-x: auto;">
                            <table style="width: 100%; border-collapse: collapse;">
                                <thead>
                                    <tr style="background: #f8fafc;">
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">System</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Username</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">URL/Domain</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Title</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Browser</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Timestamp</th>
                                    </tr>
                                </thead>
                                <tbody id="weblogsBody">
                                    <tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;">Loading web logs...</td></tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Application Usage Logs View -->
                <div id="applogs-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Application Usage Logs</h1>
                            <p style="color: #64748b; margin-top: 4px;">Track application usage across all systems</p>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.refreshAppLogs()" style="padding: 10px 20px; background: white; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/>
                                </svg>
                                Refresh
                            </button>
                            <button onclick="app.exportAppLogs()" style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>
                                </svg>
                                Export
                            </button>
                        </div>
                    </div>
                    <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <div style="display: flex; gap: 12px; margin-bottom: 20px;">
                            <input type="text" id="applogsSearch" placeholder="Search applications..." style="flex: 1; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            <input type="date" id="applogsDate" style="padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            <button onclick="app.loadAppLogs()" style="padding: 12px 24px; background: #667eea; color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500;">Search</button>
                        </div>
                        <div id="applogsTable" style="overflow-x: auto;">
                            <table style="width: 100%; border-collapse: collapse;">
                                <thead>
                                    <tr style="background: #f8fafc;">
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">System</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Username</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Application</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Window Title</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Duration</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Timestamp</th>
                                    </tr>
                                </thead>
                                <tbody id="applogsBody">
                                    <tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;">Loading application logs...</td></tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Inactivity Logs View -->
                <div id="inactivitylogs-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Inactivity Logs</h1>
                            <p style="color: #64748b; margin-top: 4px;">Monitor system inactivity and idle time</p>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.refreshInactivityLogs()" style="padding: 10px 20px; background: white; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/>
                                </svg>
                                Refresh
                            </button>
                            <button onclick="app.exportInactivityLogs()" style="padding: 10px 20px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>
                                </svg>
                                Export
                            </button>
                        </div>
                    </div>
                    <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <div style="display: flex; gap: 12px; margin-bottom: 20px;">
                            <input type="text" id="inactivitySearch" placeholder="Search by system or user..." style="flex: 1; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            <input type="date" id="inactivityDate" style="padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                            <button onclick="app.loadInactivityLogs()" style="padding: 12px 24px; background: #667eea; color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500;">Search</button>
                        </div>
                        <div id="inactivityTable" style="overflow-x: auto;">
                            <table style="width: 100%; border-collapse: collapse;">
                                <thead>
                                    <tr style="background: #f8fafc;">
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">System</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Username</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Inactivity Start</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Inactivity End</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Duration</th>
                                        <th style="padding: 14px; text-align: left; font-weight: 600; color: #64748b; font-size: 13px; border-bottom: 2px solid #e2e8f0;">Type</th>
                                    </tr>
                                </thead>
                                <tbody id="inactivityBody">
                                    <tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;">Loading inactivity logs...</td></tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Screenshots View -->
                <div id="screenshots-view" class="content-view">
                    <div class="header" style="display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h1 class="page-title">Screenshots</h1>
                            <p style="color: #64748b; margin-top: 4px;">View captured screenshots from all systems</p>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.refreshScreenshots()" style="padding: 10px 20px; background: white; border: 1px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/>
                                </svg>
                                Refresh
                            </button>
                            <select id="screenshotsFilter" onchange="app.loadScreenshots()" style="padding: 10px 16px; border: 1px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                                <option value="">All Systems</option>
                            </select>
                            <input type="date" id="screenshotsDate" onchange="app.loadScreenshots()" style="padding: 10px 16px; border: 1px solid #e2e8f0; border-radius: 10px; font-size: 14px;">
                        </div>
                    </div>
                    <div style="background: white; padding: 28px; border-radius: 16px; border: 1px solid rgba(0, 0, 0, 0.04);">
                        <div id="screenshotsGrid" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 20px;">
                            <div style="padding: 40px; text-align: center; color: #94a3b8; grid-column: 1 / -1;">Loading screenshots...</div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // ==================== SETTINGS FUNCTIONS ====================
    
    showSettingsSection(sectionId) {
        const sections = {
            general: {
                title: 'General Settings',
                content: `
                    <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">General Settings</h3>
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Company Name</label>
                            <input type="text" id="companyName" value="WIZONE IT NETWORK INDIA PVT LTD" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Working Hours</label>
                            <input type="text" id="workingHours" value="9:00 AM - 6:00 PM" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Working Days</label>
                            <input type="text" id="workingDays" value="Monday - Friday" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Time Zone</label>
                            <input type="text" id="timeZone" value="Asia/Kolkata (IST)" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div style="display: flex; gap: 12px; margin-top: 16px;">
                            <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                Save Changes
                            </button>
                            <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                Reset
                            </button>
                        </div>
                    </div>
                `
            },
            attendance: {
                title: 'Attendance Settings',
                content: `
                    <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">Attendance Settings</h3>
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Check-in Time Limit (Minutes)</label>
                            <input type="number" value="5" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Check-out Buffer (Minutes)</label>
                            <input type="number" value="10" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                Allow Late Check-in
                            </label>
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                Enable Geolocation Tracking
                            </label>
                        </div>
                        <div style="display: flex; gap: 12px; margin-top: 16px;">
                            <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                Save Changes
                            </button>
                            <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                Reset
                            </button>
                        </div>
                    </div>
                `
            },
            leave: {
                title: 'Leave Policies',
                content: `
                    <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">Leave Policies</h3>
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Annual Leave Allowance (Days)</label>
                            <input type="number" value="20" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Sick Leave Allowance (Days)</label>
                            <input type="number" value="10" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Casual Leave Allowance (Days)</label>
                            <input type="number" value="8" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                Require Approval for Leaves
                            </label>
                        </div>
                        <div style="display: flex; gap: 12px; margin-top: 16px;">
                            <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                Save Changes
                            </button>
                            <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                Reset
                            </button>
                        </div>
                    </div>
                `
            },
            notifications: {
                title: 'Notification Settings',
                content: `
                    <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">Notification Settings</h3>
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                Email Notifications
                            </label>
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                SMS Notifications
                            </label>
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                In-App Notifications
                            </label>
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Notification Frequency</label>
                            <select style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                                <option>Real-time</option>
                                <option>Hourly Digest</option>
                                <option>Daily Digest</option>
                            </select>
                        </div>
                        <div style="display: flex; gap: 12px; margin-top: 16px;">
                            <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                Save Changes
                            </button>
                            <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                Reset
                            </button>
                        </div>
                    </div>
                `
            },
            security: {
                title: 'Security Settings',
                content: `
                    <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">Security Settings</h3>
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                Enable Two-Factor Authentication
                            </label>
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" checked style="margin-right: 8px;">
                                Require Password Change Every 90 Days
                            </label>
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Session Timeout (Minutes)</label>
                            <input type="number" value="30" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                        </div>
                        <div>
                            <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">
                                <input type="checkbox" style="margin-right: 8px;">
                                Enable IP Whitelisting
                            </label>
                        </div>
                        <div style="display: flex; gap: 12px; margin-top: 16px;">
                            <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                Save Changes
                            </button>
                            <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                Reset
                            </button>
                        </div>
                    </div>
                `
            },
            integrations: {
                title: 'Integrations',
                content: `
                    <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 24px;">Integrations</h3>
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div>
                            <h4 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 12px;">Available Integrations</h4>
                            <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px;">
                                <button onclick="app.toggleIntegration('slack')" style="padding: 16px; background: #f8fafc; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: center; transition: all 0.2s ease;">
                                    <div style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">Slack</div>
                                    <div style="font-size: 12px; color: #64748b;">Message notifications</div>
                                </button>
                                <button onclick="app.toggleIntegration('teams')" style="padding: 16px; background: #f8fafc; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: center; transition: all 0.2s ease;">
                                    <div style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">Microsoft Teams</div>
                                    <div style="font-size: 12px; color: #64748b;">Team collaboration</div>
                                </button>
                                <button onclick="app.toggleIntegration('google')" style="padding: 16px; background: #f8fafc; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: center; transition: all 0.2s ease;">
                                    <div style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">Google Workspace</div>
                                    <div style="font-size: 12px; color: #64748b;">Calendar sync</div>
                                </button>
                                <button onclick="app.toggleIntegration('payroll')" style="padding: 16px; background: #f8fafc; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; text-align: center; transition: all 0.2s ease;">
                                    <div style="font-weight: 600; color: #0f172a; margin-bottom: 4px;">Payroll Software</div>
                                    <div style="font-size: 12px; color: #64748b;">Salary processing</div>
                                </button>
                            </div>
                        </div>
                        <div style="display: flex; gap: 12px; margin-top: 16px;">
                            <button onclick="app.saveSettings()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                                Save Changes
                            </button>
                            <button onclick="app.resetSettings()" style="padding: 12px 24px; background: white; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 500; color: #64748b; display: flex; align-items: center; justify-content: center; gap: 8px;">
                                <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2-8.83"/></svg>
                                Reset
                            </button>
                        </div>
                    </div>
                `
            }
        };
        
        const content = document.getElementById('settingsContent');
        if (content && sections[sectionId]) {
            content.innerHTML = sections[sectionId].content;
        }
        
        // Update menu buttons
        const buttons = document.querySelectorAll('[onclick*="showSettingsSection"]');
        buttons.forEach(btn => {
            btn.style.background = btn.getAttribute('onclick').includes(sectionId) 
                ? 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)'
                : '#f8fafc';
            btn.style.color = btn.getAttribute('onclick').includes(sectionId) 
                ? 'white'
                : '#0f172a';
        });
    },

    markSettingsChanged() {
        // Mark that settings have been changed
        console.log('Settings changed');
    },

    saveSettings() {
        this.showToast('Settings saved successfully!', 'success');
    },

    resetSettings() {
        this.showToast('Settings reset to default values', 'info');
    },

    toggleIntegration(integration) {
        this.showToast(`${integration} integration toggled`, 'success');
    },

    // ==================== USER PAGE FUNCTIONS ====================
    
    showCheckInModal() {
        const content = `
            <h2 style="margin: 0 0 20px; color: #0f172a; font-size: 20px; font-weight: 700;">Check In</h2>
            <div style="background: #f8fafc; padding: 16px; border-radius: 10px; margin-bottom: 20px;">
                <div style="color: #64748b; font-size: 13px; margin-bottom: 6px;">Current Time</div>
                <div id="currentTime" style="font-size: 28px; font-weight: 700; color: #667eea;">09:00 AM</div>
            </div>
            <div style="display: flex; gap: 12px;">
                <button onclick="app.confirmCheckIn()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; display: flex; align-items: center; justify-content: center; gap: 8px;">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg>
                    Confirm Check In
                </button>
                <button onclick="app.closeModal()" style="flex: 1; padding: 12px; background: white; color: #64748b; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 600;">
                    Cancel
                </button>
            </div>
        `;
        this.showModal(content, 'sm');
    },

    showCheckOutModal() {
        const content = `
            <h2 style="margin: 0 0 20px; color: #0f172a; font-size: 20px; font-weight: 700;">Check Out</h2>
            <div style="background: #f8fafc; padding: 16px; border-radius: 10px; margin-bottom: 20px;">
                <div style="color: #64748b; font-size: 13px; margin-bottom: 6px;">Current Time</div>
                <div id="currentTime" style="font-size: 28px; font-weight: 700; color: #ef4444;">06:15 PM</div>
            </div>
            <div style="display: flex; gap: 12px;">
                <button onclick="app.confirmCheckOut()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; display: flex; align-items: center; justify-content: center; gap: 8px;">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg>
                    Confirm Check Out
                </button>
                <button onclick="app.closeModal()" style="flex: 1; padding: 12px; background: white; color: #64748b; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 600;">
                    Cancel
                </button>
            </div>
        `;
        this.showModal(content, 'sm');
    },

    confirmCheckIn() {
        this.showToast('✓ Checked in successfully at 09:00 AM', 'success');
        this.closeModal();
    },

    confirmCheckOut() {
        this.showToast('✓ Checked out successfully at 06:15 PM', 'success');
        this.closeModal();
    },

    submitLeaveRequest() {
        this.showToast('✓ Leave request submitted for approval', 'success');
    },

    startBreak() {
        this.showToast('☕ Break started at ' + new Date().toLocaleTimeString('en-IN'), 'success');
    },

    endBreak() {
        this.showToast('✓ Break ended. 15 minutes taken', 'success');
    },

    showEditProfileModal() {
        const content = `
            <h2 style="margin: 0 0 20px; color: #0f172a; font-size: 20px; font-weight: 700;">Edit Profile</h2>
            <div style="display: flex; flex-direction: column; gap: 16px; margin-bottom: 20px;">
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">First Name</label>
                    <input type="text" value="John" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Last Name</label>
                    <input type="text" value="Doe" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Email</label>
                    <input type="email" value="john.doe@wizone.com" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Phone</label>
                    <input type="tel" value="+91 98765 43210" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
            </div>
            <div style="display: flex; gap: 12px;">
                <button onclick="app.saveProfileChanges()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; display: flex; align-items: center; justify-content: center; gap: 8px;">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                    Save Changes
                </button>
                <button onclick="app.closeModal()" style="flex: 1; padding: 12px; background: white; color: #64748b; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 600;">
                    Cancel
                </button>
            </div>
        `;
        this.showModal(content, 'sm');
    },

    showChangePasswordModal() {
        const content = `
            <h2 style="margin: 0 0 20px; color: #0f172a; font-size: 20px; font-weight: 700;">Change Password</h2>
            <div style="display: flex; flex-direction: column; gap: 16px; margin-bottom: 20px;">
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Current Password</label>
                    <input type="password" placeholder="Enter current password" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">New Password</label>
                    <input type="password" placeholder="Enter new password" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
                <div>
                    <label style="display: block; color: #64748b; font-size: 13px; margin-bottom: 8px; font-weight: 500;">Confirm Password</label>
                    <input type="password" placeholder="Confirm new password" style="width: 100%; padding: 12px 16px; border: 2px solid #e2e8f0; border-radius: 10px; font-size: 14px; color: #0f172a;">
                </div>
            </div>
            <div style="display: flex; gap: 12px;">
                <button onclick="app.savePasswordChange()" style="flex: 1; padding: 12px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; display: flex; align-items: center; justify-content: center; gap: 8px;">
                    <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
                    Update Password
                </button>
                <button onclick="app.closeModal()" style="flex: 1; padding: 12px; background: white; color: #64748b; border: 2px solid #e2e8f0; border-radius: 10px; cursor: pointer; font-weight: 600;">
                    Cancel
                </button>
            </div>
        `;
        this.showModal(content, 'sm');
    },

    saveProfileChanges() {
        this.showToast('✓ Profile updated successfully', 'success');
        this.closeModal();
    },

    savePasswordChange() {
        this.showToast('✓ Password changed successfully', 'success');
        this.closeModal();
    },

    filterNotifications(filter) {
        this.showToast(`Filtering notifications by ${filter}`, 'info');
    },

    deleteNotification(button) {
        button.closest('div').style.opacity = '0.5';
        setTimeout(() => {
            button.closest('div').remove();
            this.showToast('✓ Notification deleted', 'success');
        }, 300);
    },

    // ==================== CORE MONITORING FUNCTIONS ====================

    // Web Browsing Logs
    async loadWebLogs() {
        const tbody = document.getElementById('weblogsBody');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;"><svg width="24" height="24" viewBox="0 0 24 24" style="animation:spin 1s linear infinite;margin-right:8px;vertical-align:middle;"><circle cx="12" cy="12" r="10" stroke="#667eea" stroke-width="3" fill="none" stroke-dasharray="30 70"/></svg>Loading...</td></tr>';
        
        try {
            const activationKey = this.userData?.activation_key || '';
            const search = document.getElementById('weblogsSearch')?.value || '';
            const date = document.getElementById('weblogsDate')?.value || '';
            
            const response = await this.api(`get_web_logs&activation_key=${activationKey}&search=${encodeURIComponent(search)}&date=${date}`);
            
            if (response.success && response.data.length > 0) {
                tbody.innerHTML = response.data.map(log => `
                    <tr style="border-bottom: 1px solid #f1f5f9;">
                        <td style="padding: 14px; color: #0f172a; font-size: 14px;">${log.system_name || '-'}</td>
                        <td style="padding: 14px; color: #64748b; font-size: 14px;">${log.username || '-'}</td>
                        <td style="padding: 14px; color: #667eea; font-size: 14px; max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;"><a href="${log.website_url || '#'}" target="_blank" style="color: #667eea; text-decoration: none;">${log.website_url || '-'}</a></td>
                        <td style="padding: 14px; color: #64748b; font-size: 14px; max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${log.page_title || '-'}</td>
                        <td style="padding: 14px; color: #64748b; font-size: 14px;">${log.browser_name || '-'}</td>
                        <td style="padding: 14px; color: #94a3b8; font-size: 13px;">${log.visit_time ? new Date(log.visit_time).toLocaleString() : '-'}</td>
                    </tr>
                `).join('');
            } else {
                tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;">No web browsing logs found</td></tr>';
            }
        } catch (err) {
            console.error('Error loading web logs:', err);
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #ef4444;">Error loading data</td></tr>';
        }
    },

    refreshWebLogs() {
        this.loadWebLogs();
        this.showToast('Web logs refreshed', 'info');
    },

    exportWebLogs() {
        this.showToast('Exporting web logs...', 'info');
        // Export functionality can be implemented here
    },

    // Application Usage Logs
    async loadAppLogs() {
        const tbody = document.getElementById('applogsBody');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;"><svg width="24" height="24" viewBox="0 0 24 24" style="animation:spin 1s linear infinite;margin-right:8px;vertical-align:middle;"><circle cx="12" cy="12" r="10" stroke="#667eea" stroke-width="3" fill="none" stroke-dasharray="30 70"/></svg>Loading...</td></tr>';
        
        try {
            const activationKey = this.userData?.activation_key || '';
            const search = document.getElementById('applogsSearch')?.value || '';
            const date = document.getElementById('applogsDate')?.value || '';
            
            const response = await this.api(`get_application_logs&activation_key=${activationKey}&search=${encodeURIComponent(search)}&date=${date}`);
            
            if (response.success && response.data.length > 0) {
                tbody.innerHTML = response.data.map(log => `
                    <tr style="border-bottom: 1px solid #f1f5f9;">
                        <td style="padding: 14px; color: #0f172a; font-size: 14px;">${log.system_name || '-'}</td>
                        <td style="padding: 14px; color: #64748b; font-size: 14px;">${log.username || '-'}</td>
                        <td style="padding: 14px; font-size: 14px;"><span style="background: #667eea20; color: #667eea; padding: 4px 10px; border-radius: 6px; font-weight: 500;">${log.app_name || '-'}</span></td>
                        <td style="padding: 14px; color: #64748b; font-size: 14px; max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${log.window_title || '-'}</td>
                        <td style="padding: 14px; color: #0f172a; font-size: 14px; font-weight: 500;">${this.formatDuration(log.duration_seconds || 0)}</td>
                        <td style="padding: 14px; color: #94a3b8; font-size: 13px;">${log.start_time ? new Date(log.start_time).toLocaleString() : '-'}</td>
                    </tr>
                `).join('');
            } else {
                tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;">No application logs found</td></tr>';
            }
        } catch (err) {
            console.error('Error loading app logs:', err);
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #ef4444;">Error loading data</td></tr>';
        }
    },

    refreshAppLogs() {
        this.loadAppLogs();
        this.showToast('Application logs refreshed', 'info');
    },

    exportAppLogs() {
        this.showToast('Exporting application logs...', 'info');
    },

    // Inactivity Logs
    async loadInactivityLogs() {
        const tbody = document.getElementById('inactivityBody');
        if (!tbody) return;
        
        tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;"><svg width="24" height="24" viewBox="0 0 24 24" style="animation:spin 1s linear infinite;margin-right:8px;vertical-align:middle;"><circle cx="12" cy="12" r="10" stroke="#667eea" stroke-width="3" fill="none" stroke-dasharray="30 70"/></svg>Loading...</td></tr>';
        
        try {
            const activationKey = this.userData?.activation_key || '';
            const search = document.getElementById('inactivitySearch')?.value || '';
            const date = document.getElementById('inactivityDate')?.value || '';
            
            const response = await this.api(`get_inactivity_logs&activation_key=${activationKey}&search=${encodeURIComponent(search)}&date=${date}`);
            
            if (response.success && response.data.length > 0) {
                tbody.innerHTML = response.data.map(log => `
                    <tr style="border-bottom: 1px solid #f1f5f9;">
                        <td style="padding: 14px; color: #0f172a; font-size: 14px;">${log.system_name || '-'}</td>
                        <td style="padding: 14px; color: #64748b; font-size: 14px;">${log.username || '-'}</td>
                        <td style="padding: 14px; color: #64748b; font-size: 13px;">${log.start_time ? new Date(log.start_time).toLocaleString() : '-'}</td>
                        <td style="padding: 14px; color: #64748b; font-size: 13px;">${log.end_time ? new Date(log.end_time).toLocaleString() : '-'}</td>
                        <td style="padding: 14px; font-size: 14px;"><span style="background: #ef444420; color: #ef4444; padding: 4px 10px; border-radius: 6px; font-weight: 500;">${this.formatDuration(log.duration_seconds || 0)}</span></td>
                        <td style="padding: 14px; font-size: 14px;"><span style="background: #f59e0b20; color: #f59e0b; padding: 4px 10px; border-radius: 6px; font-weight: 500;">${log.status || 'Idle'}</span></td>
                    </tr>
                `).join('');
            } else {
                tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #94a3b8;">No inactivity logs found</td></tr>';
            }
        } catch (err) {
            console.error('Error loading inactivity logs:', err);
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 40px; text-align: center; color: #ef4444;">Error loading data</td></tr>';
        }
    },

    refreshInactivityLogs() {
        this.loadInactivityLogs();
        this.showToast('Inactivity logs refreshed', 'info');
    },

    exportInactivityLogs() {
        this.showToast('Exporting inactivity logs...', 'info');
    },

    // Screenshots
    async loadScreenshots() {
        const grid = document.getElementById('screenshotsGrid');
        if (!grid) return;
        
        grid.innerHTML = '<div style="padding: 40px; text-align: center; color: #94a3b8; grid-column: 1 / -1;"><svg width="24" height="24" viewBox="0 0 24 24" style="animation:spin 1s linear infinite;margin-right:8px;vertical-align:middle;"><circle cx="12" cy="12" r="10" stroke="#667eea" stroke-width="3" fill="none" stroke-dasharray="30 70"/></svg>Loading screenshots...</div>';
        
        try {
            const activationKey = this.userData?.activation_key || '';
            const date = document.getElementById('screenshotsDate')?.value || '';
            const systemName = document.getElementById('screenshotsFilter')?.value || '';
            
            const response = await this.api(`get_screenshots&activation_key=${activationKey}&date=${date}&system_name=${encodeURIComponent(systemName)}`);
            
            if (response.success && response.data.length > 0) {
                grid.innerHTML = response.data.map(shot => `
                    <div style="background: #f8fafc; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; cursor: pointer;" onclick="app.viewScreenshot('${shot.id}')">
                        <div style="position: relative; padding-top: 56.25%; background: linear-gradient(135deg, #667eea20 0%, #764ba220 100%);">
                            <div style="position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); text-align: center;">
                                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#667eea" stroke-width="1.5" style="opacity: 0.5;">
                                    <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                                    <circle cx="8.5" cy="8.5" r="1.5"/>
                                    <polyline points="21 15 16 10 5 21"/>
                                </svg>
                                <div style="color: #667eea; font-size: 12px; margin-top: 8px;">Click to view</div>
                            </div>
                        </div>
                        <div style="padding: 14px;">
                            <div style="font-weight: 600; color: #0f172a; font-size: 14px; margin-bottom: 4px;">${shot.system_name || 'Unknown System'}</div>
                            <div style="color: #64748b; font-size: 13px; margin-bottom: 8px;">${shot.username || '-'}</div>
                            <div style="display: flex; justify-content: space-between; align-items: center;">
                                <span style="color: #94a3b8; font-size: 12px;">${shot.capture_time ? new Date(shot.capture_time).toLocaleString() : '-'}</span>
                                ${shot.screen_width && shot.screen_height ? `<span style="background: #e2e8f0; color: #64748b; padding: 2px 8px; border-radius: 4px; font-size: 11px;">${shot.screen_width}x${shot.screen_height}</span>` : ''}
                            </div>
                        </div>
                    </div>
                `).join('');
            } else {
                grid.innerHTML = '<div style="padding: 40px; text-align: center; color: #94a3b8; grid-column: 1 / -1;">No screenshots found</div>';
            }
        } catch (err) {
            console.error('Error loading screenshots:', err);
            grid.innerHTML = '<div style="padding: 40px; text-align: center; color: #ef4444; grid-column: 1 / -1;">Error loading screenshots</div>';
        }
    },

    refreshScreenshots() {
        this.loadScreenshots();
        this.showToast('Screenshots refreshed', 'info');
    },

    async viewScreenshot(id) {
        try {
            const activationKey = this.userData?.activation_key || '';
            const response = await this.api(`get_screenshot_image&id=${id}&activation_key=${activationKey}`);
            
            if (response.success && response.data) {
                const shot = response.data;
                const imageContent = shot.screenshot_data ? 
                    `<img src="data:image/png;base64,${shot.screenshot_data}" style="max-width: 100%; max-height: 80vh; border-radius: 8px;" />` :
                    `<div style="padding: 40px; text-align: center; color: #94a3b8;">No image data available</div>`;
                
                this.showModal(`
                    <div class="modal-header" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
                        <div>
                            <h2 style="margin: 0; color: #0f172a; font-size: 18px;">Screenshot - ${shot.system_name || 'Unknown'}</h2>
                            <p style="margin: 4px 0 0; color: #64748b; font-size: 13px;">${shot.username || ''} • ${shot.capture_time ? new Date(shot.capture_time).toLocaleString() : ''}</p>
                        </div>
                        <button onclick="app.closeModal()" style="background: none; border: none; cursor: pointer; padding: 8px;">
                            <svg width="24" height="24" fill="none" stroke="#64748b" stroke-width="2" viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                        </button>
                    </div>
                    <div style="text-align: center;">
                        ${imageContent}
                    </div>
                `, 'lg');
            } else {
                this.showToast('Could not load screenshot', 'error');
            }
        } catch (err) {
            console.error('Error loading screenshot:', err);
            this.showToast('Error loading screenshot', 'error');
        }
    },

    // Helper function to format duration
    formatDuration(seconds) {
        if (!seconds || seconds <= 0) return '0s';
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        
        if (hours > 0) return `${hours}h ${minutes}m`;
        if (minutes > 0) return `${minutes}m ${secs}s`;
        return `${secs}s`;
    },

    // Navigate between views
    navigate(viewName) {
        // Hide all views
        const allViews = document.querySelectorAll('.content-view');
        allViews.forEach(view => view.classList.remove('active'));
        
        // Show selected view
        const targetView = document.getElementById(`${viewName}-view`);
        if (targetView) {
            targetView.classList.add('active');
        }
        
        // Update menu active state
        const allMenuItems = document.querySelectorAll('.menu-item');
        allMenuItems.forEach(item => item.classList.remove('active'));
        
        const activeMenuItem = document.querySelector(`[data-view="${viewName}"]`);
        if (activeMenuItem) {
            activeMenuItem.classList.add('active');
        }
        
        this.currentView = viewName;
        
        // Auto-load data for monitoring views
        switch(viewName) {
            case 'weblogs':
                this.loadWebLogs();
                break;
            case 'applogs':
                this.loadAppLogs();
                break;
            case 'inactivitylogs':
                this.loadInactivityLogs();
                break;
            case 'screenshots':
                this.loadScreenshots();
                break;
        }
    },

    // Logout
    logout() {
        if (confirm('Are you sure you want to logout?')) {
            // Clear session data
            sessionStorage.removeItem('userData');
            sessionStorage.removeItem('isLoggedIn');
            sessionStorage.removeItem('userRole');
            this.userData = null;
            this.currentRole = null;
            this.dashboardData = null;
            
            document.getElementById('appContainer').classList.remove('active');
            document.getElementById('appContainer').innerHTML = '';
            this.showRoleSelection();
            this.showToast('Logged out successfully', 'info');
        }
    }
};

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    app.init();
});
