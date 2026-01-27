// Desktop Time & Attendance - Clean Login Application
// Main App Logic

const app = {
    currentRole: 'admin',
    userData: null,
    
    // API Base URL
    API_BASE: '',
    
    // Helper function to format timestamp in IST timezone (HH:MM:SS format)
    formatTimestampIST(timestamp) {
        if (!timestamp) return 'N/A';
        const d = new Date(timestamp);
        const istTime = d.toLocaleString('en-IN', { 
            timeZone: 'Asia/Kolkata',
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });
        return istTime;
    },
    
    // API Helper function
    async api(action, data = null) {
        const options = {
            method: data ? 'POST' : 'GET',
            headers: { 'Content-Type': 'application/json' }
        };
        if (data) options.body = JSON.stringify(data);
        
        const url = `${this.API_BASE}/api.php?action=${action}`;
        console.log(`[API] ${action}`, data);
        
        const response = await fetch(url, options);
        const result = await response.json();
        
        console.log(`[API Response] ${action}:`, result);
        return result;
    },

    // Show image in modal viewer
    async showImageViewer(screenshotId) {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            // Fetch the screenshot image
            const response = await this.api('get_screenshot_image', {
                id: screenshotId,
                company_name: companyName
            });

            if (!response.success || !response.data) {
                this.showToast('Failed to load screenshot image', 'error');
                return;
            }

            const image = response.data;
            const modal = document.getElementById('imageModal');
            const imageElement = document.getElementById('imageModalImage');
            const infoElement = document.getElementById('imageModalInfo');
            const titleElement = document.getElementById('imageModalTitle');

            // Set image data
            if (image.screenshot_data) {
                imageElement.src = `data:image/png;base64,${image.screenshot_data}`;
            } else {
                imageElement.src = 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 300"><rect fill="%23f1f5f9" width="400" height="300"/><text x="50%" y="50%" font-size="16" fill="%2394a3b8" text-anchor="middle" dominant-baseline="middle">No Image Available</text></svg>';
            }

            // Set title
            titleElement.innerHTML = `📸 ${image.system_name || 'Screenshot'} - ${this.formatTimestampIST(image.capture_time)}`;

            // Set info
            infoElement.innerHTML = `
                <div class="image-info-item">
                    <span class="image-info-label">👤 Employee</span>
                    <span class="image-info-value">${image.username || 'N/A'}</span>
                </div>
                <div class="image-info-item">
                    <span class="image-info-label">💻 System</span>
                    <span class="image-info-value">${image.system_name || 'N/A'}</span>
                </div>
                <div class="image-info-item">
                    <span class="image-info-label">📅 Timestamp</span>
                    <span class="image-info-value">${this.formatTimestampIST(image.capture_time)}</span>
                </div>
                <div class="image-info-item">
                    <span class="image-info-label">🔗 Screenshot ID</span>
                    <span class="image-info-value">#${image.id}</span>
                </div>
            `;

            // Show modal
            modal.classList.add('active');

            // Close modal on backdrop click
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    modal.classList.remove('active');
                }
            });

            // Close modal on escape key
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') {
                    modal.classList.remove('active');
                }
            });

        } catch (error) {
            console.error('Error showing image:', error);
            this.showToast('Error loading screenshot image', 'error');
        }
    },

    // Create professional filter section
    createFilterSection(filterId, filters) {
        const filterContainer = document.getElementById(filterId);
        if (!filterContainer) return;

        const filterHTML = `
            <div style="background: white; padding: 24px; border-radius: 12px; margin-bottom: 24px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06); border: 1px solid #e2e8f0;">
                <div style="display: flex; align-items: center; margin-bottom: 20px;">
                    <span style="font-size: 18px; margin-right: 12px;">🔍</span>
                    <h3 style="font-size: 16px; font-weight: 700; color: #1e293b; margin: 0;">Advanced Filters</h3>
                </div>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 16px;">
                    ${filters.map(filter => `
                        <div>
                            <label style="display: block; font-weight: 700; font-size: 12px; color: #475569; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;">${filter.label}</label>
                            ${filter.type === 'date' ? `<input type="date" id="${filter.id}" style="width: 100%; padding: 10px 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 13px; transition: all 0.3s ease; background: #f9fafb;" onchange="this.style.borderColor='#3b82f6'; this.style.background='white';" />` : ''}
                            ${filter.type === 'select' ? `<select id="${filter.id}" style="width: 100%; padding: 10px 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 13px; background: #f9fafb; transition: all 0.3s ease;"><option value="">-- Select ${filter.label} --</option>${filter.options.map(opt => `<option value="${opt.value}">${opt.label}</option>`).join('')}</select>` : ''}
                        </div>
                    `).join('')}
                </div>
                <div style="display: flex; gap: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #e2e8f0;">
                    <button onclick="${filterId}_load()" style="padding: 10px 24px; background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 700; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3); transition: all 0.3s ease;" onmouseover="this.style.transform='translateY(-2px)'" onmouseout="this.style.transform='translateY(0)'">📥 Load Data</button>
                    <button onclick="${filterId}_reset()" style="padding: 10px 24px; background: #f1f5f9; color: #64748b; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-weight: 700; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; transition: all 0.3s ease;" onmouseover="this.style.background='#e2e8f0'" onmouseout="this.style.background='#f1f5f9'">🔄 Reset</button>
                </div>
            </div>
        `;
        
        filterContainer.innerHTML = filterHTML;
    },

    // Pagination Helper - Create paginated table with frozen headers and scrolling
    createPaginatedTable(records, columns, containerId, defaultPageSize = 15) {
        const container = document.getElementById(containerId);
        if (!records || records.length === 0) {
            container.innerHTML = '<div style="text-align: center; padding: 60px 40px; color: #94a3b8;"><div style="margin-bottom: 16px; font-size: 48px;">📭</div><p style="font-size: 16px; font-weight: 500;">No records found</p><p style="font-size: 13px; margin-top: 8px; color: #cbd5e1;">Try adjusting your filters or date range</p></div>';
            return;
        }

        let currentPage = 1;
        let pageSize = defaultPageSize;

        const renderTable = () => {
            const startIdx = (currentPage - 1) * pageSize;
            const endIdx = startIdx + pageSize;
            const pageRecords = records.slice(startIdx, endIdx);
            const totalPages = Math.ceil(records.length / pageSize);

            const headerCells = columns.map(col => `<th style="padding: 16px; text-align: left; font-weight: 700; color: #1e293b; background: linear-gradient(180deg, #f1f5f9 0%, #e2e8f0 100%); min-width: ${col.width || '150px'}; white-space: nowrap; border-bottom: 2px solid #cbd5e1; position: sticky; top: 0; z-index: 10;">${col.label}</th>`).join('');
            
            const bodyRows = pageRecords.map((record, idx) => {
                const cells = columns.map(col => {
                    const content = col.render ? col.render(record) : (record[col.field] || 'N/A');
                    return `<td style="padding: 14px 16px; color: #475569; border-bottom: 1px solid #e2e8f0; word-wrap: break-word; word-break: break-word; overflow-wrap: break-word; max-width: ${col.width || '150px'};">${content}</td>`;
                }).join('');
                return `<tr style="background: ${idx % 2 === 0 ? '#ffffff' : '#f9fafb'}; transition: all 0.2s ease; border-left: 3px solid transparent;">` + cells + `</tr>`;
            }).join('');

            const paginationHTML = `
                <div style="display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; background: linear-gradient(180deg, #f9fafb 0%, #f1f5f9 100%); border-top: 1px solid #e2e8f0; flex-wrap: wrap; gap: 16px;">
                    <div style="display: flex; gap: 12px; align-items: center;">
                        <label style="font-size: 12px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px;">Show entries:</label>
                        <select id="${containerId}_pageSize" style="padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; background: white; color: #1e293b; transition: all 0.3s ease;">
                            <option value="15" ${pageSize === 15 ? 'selected' : ''}>15</option>
                            <option value="30" ${pageSize === 30 ? 'selected' : ''}>30</option>
                            <option value="50" ${pageSize === 50 ? 'selected' : ''}>50</option>
                            <option value="100" ${pageSize === 100 ? 'selected' : ''}>100</option>
                            <option value="200" ${pageSize === 200 ? 'selected' : ''}>200</option>
                        </select>
                    </div>
                    <div style="display: flex; gap: 8px; align-items: center;">
                        <button id="${containerId}_prevBtn" style="padding: 8px 16px; background: ${currentPage === 1 ? '#e2e8f0' : 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)'}; color: white; border: none; border-radius: 6px; cursor: ${currentPage === 1 ? 'not-allowed' : 'pointer'}; font-size: 12px; font-weight: 700; transition: all 0.3s ease; opacity: ${currentPage === 1 ? '0.5' : '1'};" ${currentPage === 1 ? 'disabled' : ''}>← PREVIOUS</button>
                        <span style="font-size: 12px; font-weight: 700; color: #1e293b; min-width: 140px; text-align: center; background: white; padding: 8px 12px; border-radius: 6px; border: 1px solid #cbd5e1;">Page <strong style="color: #3b82f6;">${currentPage}</strong> of <strong>${totalPages}</strong></span>
                        <button id="${containerId}_nextBtn" style="padding: 8px 16px; background: ${currentPage === totalPages ? '#e2e8f0' : 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)'}; color: white; border: none; border-radius: 6px; cursor: ${currentPage === totalPages ? 'not-allowed' : 'pointer'}; font-size: 12px; font-weight: 700; transition: all 0.3s ease; opacity: ${currentPage === totalPages ? '0.5' : '1'};" ${currentPage === totalPages ? 'disabled' : ''}>NEXT →</button>
                    </div>
                    <div style="font-size: 11px; color: #64748b; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;">📊 Showing ${startIdx + 1} to ${Math.min(endIdx, records.length)} of <strong style="color: #3b82f6;">${records.length}</strong> records</div>
                </div>
            `;

            const tableHTML = `
                <div style="display: flex; flex-direction: column; height: 100%; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; background: white; box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);">
                    <div style="overflow: auto; flex: 1; max-height: 600px; background: white;">
                        <table style="width: 100%; border-collapse: collapse; font-size: 13px; table-layout: auto;">
                            <thead style="position: sticky; top: 0; z-index: 10;">
                                <tr>${headerCells}</tr>
                            </thead>
                            <tbody>${bodyRows}</tbody>
                        </table>
                    </div>
                    ${paginationHTML}
                </div>
            `;

            container.innerHTML = tableHTML;

            // Attach event listeners
            const pageSizeSelect = document.getElementById(`${containerId}_pageSize`);
            const prevBtn = document.getElementById(`${containerId}_prevBtn`);
            const nextBtn = document.getElementById(`${containerId}_nextBtn`);

            if (pageSizeSelect) {
                pageSizeSelect.addEventListener('change', (e) => {
                    pageSize = parseInt(e.target.value);
                    currentPage = 1;
                    renderTable();
                });
            }

            if (prevBtn) {
                prevBtn.addEventListener('click', () => {
                    if (currentPage > 1) {
                        currentPage--;
                        renderTable();
                        container.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                });
            }

            if (nextBtn) {
                nextBtn.addEventListener('click', () => {
                    if (currentPage < totalPages) {
                        currentPage++;
                        renderTable();
                        container.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                });
            }
        };

        renderTable();
    },
    
    // Initialize app
    init() {
        try {
            console.log('APP INIT START');
            
            // Set current role to admin since we're showing admin login directly
            this.currentRole = 'admin';
            
            const roleSelection = document.getElementById('roleSelection');
            const loadingScreen = document.getElementById('loadingScreen');
            const loginSection = document.getElementById('loginSection');
            const appContainer = document.getElementById('appContainer');
            
            // Hide everything initially
            if (loadingScreen) loadingScreen.style.display = 'none';
            if (appContainer) appContainer.style.display = 'none';
            
            // Hide role selection completely
            if (roleSelection) {
                roleSelection.className = 'role-selection hidden';
                roleSelection.style.cssText = 'display: none !important;';
            }
            
            // Show login form directly
            if (loginSection) {
                loginSection.className = '';
                loginSection.style.cssText = 'display: flex !important; width: 100vw !important; height: 100vh !important; margin: 0 !important; padding: 0 !important; position: fixed !important; top: 0 !important; left: 0 !important;';
            }
            
            // Render clean centered login form
            this.renderLoginForm();
            
        } catch (error) {
            console.error('App initialization error:', error);
        }
    },

    // Render the login form
    renderLoginForm() {
        const loginContainer = document.getElementById('loginContainer');
        if (loginContainer) {
            loginContainer.style.cssText = 'width: 100vw; height: 100vh; margin: 0; padding: 0; display: flex; align-items: center; justify-content: center; background: white; position: relative; overflow: hidden;';
            loginContainer.innerHTML = `
                <style>
                    @keyframes float {
                        0%, 100% { transform: translateY(0px); }
                        50% { transform: translateY(20px); }
                    }
                    @keyframes slideInLeft {
                        from { opacity: 0; transform: translateX(-50px); }
                        to { opacity: 1; transform: translateX(0); }
                    }
                    @keyframes slideInRight {
                        from { opacity: 0; transform: translateX(50px); }
                        to { opacity: 1; transform: translateX(0); }
                    }
                    @keyframes shine {
                        0%, 100% { transform: translateX(-100%); }
                        50% { transform: translateX(100%); }
                    }
                    .login-sidebar {
                        width: 45%;
                        height: 100vh;
                        background: linear-gradient(135deg, #0d7fbf 0%, #17a2b8 50%, #20c997 100%);
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                        position: relative;
                        overflow: hidden;
                        box-shadow: inset -10px 0 30px rgba(0,0,0,0.2);
                    }
                    .login-sidebar::before {
                        content: '';
                        position: absolute;
                        top: -50%;
                        right: -10%;
                        width: 500px;
                        height: 500px;
                        background: radial-gradient(circle, rgba(255,255,255,0.15) 0%, transparent 70%);
                        border-radius: 50%;
                        animation: float 8s ease-in-out infinite;
                    }
                    .login-sidebar::after {
                        content: '';
                        position: absolute;
                        bottom: -30%;
                        left: -5%;
                        width: 400px;
                        height: 400px;
                        background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
                        border-radius: 50%;
                        animation: float 10s ease-in-out infinite reverse;
                    }
                    .login-content {
                        width: 55%;
                        height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        padding: 40px;
                    }
                </style>

                <!-- Professional Left Sidebar with Logo -->
                <div class="login-sidebar">
                    <div style="position: relative; z-index: 10; display: flex; flex-direction: column; align-items: center; gap: 40px; animation: slideInLeft 0.8s ease-out;">
                        
                        <!-- Large Logo -->
                        <div style="animation: slideInLeft 0.8s ease-out;">
                            <div style="width: 140px; height: 140px; background: linear-gradient(135deg, rgba(255,255,255,0.2) 0%, rgba(255,255,255,0.1) 100%); border: 2px solid rgba(255,255,255,0.4); border-radius: 30px; display: flex; align-items: center; justify-content: center; box-shadow: 0 30px 60px rgba(0, 0, 0, 0.3), inset 0 1px 0 rgba(255,255,255,0.6); position: relative; overflow: hidden;">
                                <div style="position: absolute; inset: 0; background: linear-gradient(45deg, transparent 30%, rgba(255,255,255,0.2) 50%, transparent 70%); animation: shine 3s infinite;"></div>
                                <svg width="70" height="70" viewBox="0 0 70 70" fill="none" xmlns="http://www.w3.org/2000/svg">
                                    <g filter="url(#filter0_d)">
                                        <path d="M35 8C20.6406 8 9 19.6406 9 34C9 48.3594 20.6406 60 35 60C49.3594 60 61 48.3594 61 34C61 19.6406 49.3594 8 35 8Z" fill="white" fill-opacity="0.4"/>
                                        <path d="M35 15C23.9543 15 15 23.9543 15 35C15 46.0457 23.9543 55 35 55C46.0457 55 55 46.0457 55 35C55 23.9543 46.0457 15 35 15Z" stroke="white" stroke-width="2.5"/>
                                        <circle cx="35" cy="35" r="10" fill="white"/>
                                    </g>
                                    <defs>
                                        <filter id="filter0_d" x="0" y="0" width="70" height="70" filterUnits="userSpaceOnUse" color-interpolation-filters="sRGB">
                                            <feFlood flood-opacity="0" result="BackgroundImageFix"/>
                                            <feColorMatrix in="SourceAlpha" type="saturate" values="0"/>
                                            <feOffset dy="2"/>
                                            <feGaussianBlur stdDeviation="3"/>
                                            <feColorMatrix type="linearRGB" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0"/>
                                            <feBlend mode="normal" in2="BackgroundImageFix" result="effect1_dropShadow"/>
                                            <feBlend mode="normal" in="SourceGraphic" in2="effect1_dropShadow" result="shape"/>
                                        </filter>
                                    </defs>
                                </svg>
                            </div>
                        </div>

                        <!-- Branding Text -->
                        <div style="text-align: center; animation: slideInLeft 1s ease-out;">
                            <h1 style="color: white; font-size: 32px; font-weight: 800; margin: 0; letter-spacing: 1px; text-shadow: 0 3px 15px rgba(0,0,0,0.3);">WIZONE</h1>
                            <p style="color: rgba(255,255,255,0.95); font-size: 14px; margin: 8px 0 0 0; font-weight: 600; letter-spacing: 2px; text-shadow: 0 2px 5px rgba(0,0,0,0.2);">AI LABS PVT LTD</p>
                            <div style="width: 60px; height: 2px; background: linear-gradient(90deg, transparent, white, transparent); margin: 16px auto;"></div>
                            <p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 12px 0 0 0; font-weight: 300; letter-spacing: 1px;">Desktop Controller System</p>
                        </div>

                        <!-- Feature List -->
                        <div style="display: flex; flex-direction: column; gap: 14px; animation: slideInLeft 1.2s ease-out;">
                            <div style="display: flex; align-items: center; gap: 12px; color: rgba(255,255,255,0.9); font-size: 13px; font-weight: 500;">
                                <div style="width: 6px; height: 6px; background: white; border-radius: 50%;"></div>
                                <span>Advanced Monitoring</span>
                            </div>
                            <div style="display: flex; align-items: center; gap: 12px; color: rgba(255,255,255,0.9); font-size: 13px; font-weight: 500;">
                                <div style="width: 6px; height: 6px; background: white; border-radius: 50%;"></div>
                                <span>Real-time Analytics</span>
                            </div>
                            <div style="display: flex; align-items: center; gap: 12px; color: rgba(255,255,255,0.9); font-size: 13px; font-weight: 500;">
                                <div style="width: 6px; height: 6px; background: white; border-radius: 50%;"></div>
                                <span>Employee Management</span>
                            </div>
                            <div style="display: flex; align-items: center; gap: 12px; color: rgba(255,255,255,0.9); font-size: 13px; font-weight: 500;">
                                <div style="width: 6px; height: 6px; background: white; border-radius: 50%;"></div>
                                <span>Activity Reports</span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Right Side - Login Form -->
                <div class="login-content">
                    <div style="display: flex; flex-direction: column; align-items: center; gap: 30px; width: 100%; max-width: 380px; animation: slideInRight 0.8s ease-out;">
                        
                        <!-- Wizone Logo Image at Top -->
                        <div style="animation: slideInRight 0.6s ease-out; margin-bottom: 10px;">
                            <img src="data:image/svg+xml,%3Csvg viewBox='0 0 400 120' xmlns='http://www.w3.org/2000/svg'%3E%3Cdefs%3E%3ClinearGradient id='grad1' x1='0%25' y1='0%25' x2='100%25' y2='100%25'%3E%3Cstop offset='0%25' style='stop-color:%230d7fbf;stop-opacity:1' /%3E%3Cstop offset='100%25' style='stop-color:%2317a2b8;stop-opacity:1' /%3E%3C/linearGradient%3E%3C/defs%3E%3Ctext x='50' y='70' font-size='48' font-weight='bold' fill='url(%23grad1)' font-family='Arial, sans-serif' letter-spacing='2'%3EWIZONE%3C/text%3E%3Ctext x='50' y='100' font-size='14' font-weight='600' fill='%2317a2b8' font-family='Arial, sans-serif' letter-spacing='1'%3EAI LABS PVT LTD%3C/text%3E%3C/svg%3E" alt="WIZONE AI Labs" style="width: 280px; height: auto; max-width: 100%;">
                        </div>
                        
                        <!-- Form Container -->
                        <div style="width: 100%; background: white; border-radius: 16px; padding: 45px 35px; box-shadow: 0 10px 40px rgba(0, 0, 0, 0.08); border: 1px solid #e5e7eb; animation: slideInRight 1s ease-out;">
                            
                            <!-- Form Title -->
                            <h2 style="color: #0d7fbf; font-size: 24px; font-weight: 700; margin: 0 0 8px 0; text-align: center;">Welcome Back</h2>
                            <p style="color: #64748b; font-size: 13px; margin: 0 0 30px 0; text-align: center; font-weight: 500;">Sign in to your admin account</p>
                            
                            <!-- Form -->
                            <form onsubmit="app.handleLogin(event)" style="display: flex; flex-direction: column; gap: 18px;">
                                
                                <!-- Username Field -->
                                <div style="position: relative;">
                                    <label style="display: block; font-size: 12px; font-weight: 700; color: #0d7fbf; margin-bottom: 7px; text-transform: uppercase; letter-spacing: 0.5px;">Username</label>
                                    <div style="position: relative; display: flex; align-items: center;">
                                        <div style="position: absolute; left: 12px; color: #17a2b8;">
                                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                                                <path d="M20 21V19C20 17.9391 19.5786 16.9217 18.8284 16.1716C18.0783 15.4214 17.0609 15 16 15H8C6.93913 15 5.92172 15.4214 5.17157 16.1716C4.42143 16.9217 4 17.9391 4 19V21" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                                                <circle cx="12" cy="7" r="4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                                            </svg>
                                        </div>
                                        <input 
                                            type="text" 
                                            id="loginUsername" 
                                            placeholder="Enter your username"
                                            required
                                            autofocus
                                            style="width: 100%; padding: 11px 12px 11px 45px; background: #f8f9fa; border: 2px solid #e2e8f0; border-radius: 8px; color: #1a1a1a; font-size: 14px; outline: none; transition: all 0.3s ease; font-weight: 500;"
                                            onfocus="this.style.borderColor='#17a2b8'; this.style.background='white'; this.style.boxShadow='0 0 0 3px rgba(23, 162, 184, 0.1)'"
                                            onblur="this.style.borderColor='#e2e8f0'; this.style.background='#f8f9fa'; this.style.boxShadow='none'"
                                        >
                                    </div>
                                </div>
                                
                                <!-- Password Field -->
                                <div style="position: relative;">
                                    <label style="display: block; font-size: 12px; font-weight: 700; color: #0d7fbf; margin-bottom: 7px; text-transform: uppercase; letter-spacing: 0.5px;">Password</label>
                                    <div style="position: relative; display: flex; align-items: center;">
                                        <div style="position: absolute; left: 12px; color: #17a2b8;">
                                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                                                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" stroke="currentColor" stroke-width="2"/>
                                                <circle cx="12" cy="16" r="1" stroke="currentColor" stroke-width="2"/>
                                                <path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" stroke-width="2"/>
                                            </svg>
                                        </div>
                                        <input 
                                            type="password" 
                                            id="loginPassword" 
                                            placeholder="Enter your password"
                                            required
                                            style="width: 100%; padding: 11px 12px 11px 45px; background: #f8f9fa; border: 2px solid #e2e8f0; border-radius: 8px; color: #1a1a1a; font-size: 14px; outline: none; transition: all 0.3s ease; font-weight: 500;"
                                            onfocus="this.style.borderColor='#17a2b8'; this.style.background='white'; this.style.boxShadow='0 0 0 3px rgba(23, 162, 184, 0.1)'"
                                            onblur="this.style.borderColor='#e2e8f0'; this.style.background='#f8f9fa'; this.style.boxShadow='none'"
                                        >
                                    </div>
                                </div>
                                
                                <!-- Remember Me & Forgot Password -->
                                <div style="display: flex; justify-content: space-between; align-items: center; padding: 5px 0;">
                                    <label style="display: flex; align-items: center; gap: 6px; color: #64748b; font-size: 13px; cursor: pointer; font-weight: 500;">
                                        <input type="checkbox" style="width: 16px; height: 16px; cursor: pointer; accent-color: #17a2b8;">
                                        Remember me
                                    </label>
                                    <a href="#" style="color: #17a2b8; font-size: 13px; text-decoration: none; font-weight: 600; transition: color 0.3s ease;" onmouseover="this.style.color='#0d7fbf'" onmouseout="this.style.color='#17a2b8'">
                                        Forgot Password?
                                    </a>
                                </div>
                                
                                <!-- Submit Button -->
                                <button 
                                    type="submit"
                                    style="width: 100%; padding: 12px; background: linear-gradient(135deg, #0d7fbf 0%, #17a2b8 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 700; transition: all 0.3s ease; margin-top: 8px; text-transform: uppercase; letter-spacing: 0.5px; box-shadow: 0 4px 15px rgba(13, 127, 191, 0.3);"
                                    onmouseover="this.style.transform='translateY(-2px)'; this.style.boxShadow='0 6px 20px rgba(13, 127, 191, 0.4)'"
                                    onmouseout="this.style.transform='translateY(0)'; this.style.boxShadow='0 4px 15px rgba(13, 127, 191, 0.3)'"
                                >
                                    Sign In
                                </button>
                            </form>
                        </div>

                        <!-- Footer -->
                        <p style="color: #94a3b8; font-size: 11px; text-align: center; font-weight: 500;">
                            © 2026 WIZONE AI Labs PVT LTD. All rights reserved.
                        </p>
                    </div>
                </div>
            `;
        }
    },

    // Handle login form submission
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
            const response = await fetch(`/api.php?action=admin_login`, {
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
                const userInfo = data.data || data.user;
                
                // Store user data in session
                this.userData = userInfo;
                sessionStorage.setItem('userData', JSON.stringify(userInfo));
                sessionStorage.setItem('isLoggedIn', 'true');
                sessionStorage.setItem('userRole', this.currentRole);
                
                this.showToast(`Welcome back, ${userInfo.full_name || userInfo.username}!`, 'success');
                
                // Hide login and show success message
                document.getElementById('loginSection').style.display = 'none';
                this.showSuccessPage();
            } else {
                this.showToast(data.error || 'Invalid credentials', 'error');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        } catch (error) {
            console.error('Login error:', error);
            this.showToast('Login failed: ' + error.message, 'error');
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
        }
    },

    // Show success page after login
    showSuccessPage() {
        const appContainer = document.getElementById('appContainer');
        appContainer.style.display = 'flex';
        appContainer.style.cssText = 'display: flex; width: 100vw; height: 100vh; position: fixed; top: 0; left: 0; background: #f8fafc;';
        appContainer.innerHTML = `
            <!-- Sidebar Navigation -->
            <div class="sidebar" style="width: 280px; height: 100vh; background: linear-gradient(180deg, #0f2847 0%, #0a1e2e 100%); color: white; display: flex; flex-direction: column; overflow-y: auto; border-right: 1px solid rgba(23, 162, 184, 0.2); box-shadow: 4px 0 20px rgba(13, 127, 191, 0.15);">
                
                <!-- Logo Section - Professional Wizone Branding -->
                <div class="logo-section" style="padding: 24px 16px; border-bottom: 1px solid rgba(23, 162, 184, 0.15); background: linear-gradient(135deg, rgba(13, 127, 191, 0.1) 0%, rgba(23, 162, 184, 0.05) 100%);">
                    <div style="display: flex; flex-direction: column; align-items: center; gap: 12px; text-align: center;">
                        <!-- Logo Icon -->
                        <div style="width: 50px; height: 50px; background: linear-gradient(135deg, #0d7fbf 0%, #17a2b8 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center; box-shadow: 0 8px 20px rgba(13, 127, 191, 0.3); position: relative; overflow: hidden;">
                            <div style="position: absolute; inset: 0; background: linear-gradient(45deg, transparent 30%, rgba(255,255,255,0.15) 50%, transparent 70%);"></div>
                            <svg width="26" height="26" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm3.5-9c.83 0 1.5-.67 1.5-1.5S16.33 8 15.5 8 14 8.67 14 9.5s.67 1.5 1.5 1.5zm-7 0c.83 0 1.5-.67 1.5-1.5S9.33 8 8.5 8 7 8.67 7 9.5 7.67 11 8.5 11zm3.5 6.5c2.33 0 4.31-1.46 5.11-3.5H6.89c.8 2.04 2.78 3.5 5.11 3.5z" fill="white"/>
                            </svg>
                        </div>
                        
                        <!-- Company Name -->
                        <div>
                            <div style="font-size: 14px; font-weight: 700; letter-spacing: 0.5px; color: white;">WIZONE</div>
                            <div style="font-size: 10px; color: #17a2b8; font-weight: 600; letter-spacing: 1px; margin-top: 2px;">AI LABS</div>
                        </div>
                        
                        <!-- Subtitle -->
                        <div style="font-size: 11px; color: rgba(255,255,255,0.7); font-weight: 400; letter-spacing: 0.5px; margin-top: 4px;">Desktop Controller</div>
                    </div>
                </div>

                <!-- Navigation Menu -->
                <div style="flex: 1; padding: 20px 0;">
                    
                    <!-- OVERVIEW Section -->
                    <div class="menu-section">
                        <div style="padding: 0 20px; margin-bottom: 12px; font-size: 11px; font-weight: 700; color: #17a2b8; text-transform: uppercase; letter-spacing: 1px; display: flex; align-items: center; gap: 8px;">
                            <span style="width: 3px; height: 3px; background: #20c997; border-radius: 50%;"></span>
                            OVERVIEW
                        </div>
                        <nav>
                            <a class="menu-item active" onclick="app.navigate('dashboard')" data-view="dashboard" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #20c997; text-decoration: none; transition: all 0.3s ease; cursor: pointer; background: linear-gradient(90deg, rgba(13, 127, 191, 0.25) 0%, rgba(23, 162, 184, 0.15) 100%); border-left: 4px solid #17a2b8; border-radius: 0 8px 8px 0; margin-right: 8px;">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="3" y="3" width="7" height="7" rx="1"/>
                                    <rect x="14" y="3" width="7" height="7" rx="1"/>
                                    <rect x="14" y="14" width="7" height="7" rx="1"/>
                                    <rect x="3" y="14" width="7" height="7" rx="1"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Dashboard</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('live-systems')" data-view="live-systems" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
                                    <line x1="8" y1="21" x2="16" y2="21"/>
                                    <line x1="12" y1="17" x2="12" y2="21"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Live Systems</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('analytics')" data-view="analytics" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="18" y1="20" x2="18" y2="10"/>
                                    <line x1="12" y1="20" x2="12" y2="4"/>
                                    <line x1="6" y1="20" x2="6" y2="14"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Analytics & Insights</span>
                            </a>
                        </nav>
                    </div>

                    <!-- MANAGEMENT Section -->
                    <div class="menu-section" style="margin-top: 24px;">
                        <div style="padding: 0 20px; margin-bottom: 12px; font-size: 11px; font-weight: 700; color: #17a2b8; text-transform: uppercase; letter-spacing: 1px; display: flex; align-items: center; gap: 8px;">
                            <span style="width: 3px; height: 3px; background: #20c997; border-radius: 50%;"></span>
                            MANAGEMENT
                        </div>
                        <nav>
                            <a class="menu-item" onclick="app.navigate('employee-management')" data-view="employee-management" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                    <circle cx="9" cy="7" r="4"/>
                                    <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                                    <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Employee Management</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('attendance-reports')" data-view="attendance-reports" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                                    <line x1="16" y1="2" x2="16" y2="6"/>
                                    <line x1="8" y1="2" x2="8" y2="6"/>
                                    <line x1="3" y1="10" x2="21" y2="10"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Attendance Reports</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('daily-working-hours')" data-view="daily-working-hours" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <circle cx="12" cy="12" r="10"/>
                                    <polyline points="12 6 12 12 16 14"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Daily Working Hours</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('leave-management')" data-view="leave-management" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/>
                                    <circle cx="12" cy="10" r="3"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Leave Management</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('departments')" data-view="departments" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M3 7v10a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2z"/>
                                    <polyline points="9 22 9 12 15 12 15 22"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Departments</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('engineer-report')" data-view="engineer-report" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #a0aec0; text-decoration: none; transition: all 0.3s ease; cursor: pointer; border-radius: 8px; margin: 0 8px;" onmouseover="this.style.background='linear-gradient(90deg, rgba(23, 162, 184, 0.15) 0%, rgba(32, 201, 151, 0.1) 100%)'; this.style.color='#20c997'" onmouseout="this.style.background='transparent'; this.style.color='#a0aec0'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M21 21H3V3h18v18zm-2-2V5H5v14h14z"/>
                                    <rect x="7" y="10" width="2" height="8"/>
                                    <rect x="15" y="8" width="2" height="10"/>
                                    <rect x="11" y="12" width="2" height="6"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 600;">Engineer Report</span>
                            </a>
                        </nav>
                    </div>

                    <!-- CORE MONITORING Section -->
                    <div class="menu-section" style="margin-top: 24px;">
                        <div style="padding: 0 20px; margin-bottom: 12px; font-size: 11px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">CORE MONITORING</div>
                        <nav>
                            <a class="menu-item" onclick="app.navigate('web-browsing-logs')" data-view="web-browsing-logs" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <circle cx="12" cy="12" r="10"/>
                                    <path d="M2 12h20"/>
                                    <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Web Browsing Logs</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('application-usage')" data-view="application-usage" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
                                    <line x1="8" y1="21" x2="16" y2="21"/>
                                    <line x1="12" y1="17" x2="12" y2="21"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Application Usage</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('inactivity-logs')" data-view="inactivity-logs" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <circle cx="12" cy="12" r="10"/>
                                    <polyline points="12 6 12 12 16 14"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Inactivity Logs</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('screenshots')" data-view="screenshots" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                                    <circle cx="8.5" cy="8.5" r="1.5"/>
                                    <polyline points="21 15 16 10 5 21"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Screenshots</span>
                            </a>

                            <a class="menu-item" onclick="app.navigate('analytics')" data-view="analytics" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="12" y1="2" x2="12" y2="22"/>
                                    <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Analytics & Reports</span>
                            </a>
                        </nav>
                    </div>

                    <!-- SYSTEM Section -->
                    <div class="menu-section" style="margin-top: 24px;">
                        <div style="padding: 0 20px; margin-bottom: 12px; font-size: 11px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;">SYSTEM</div>
                        <nav>
                            <a class="menu-item" onclick="app.navigate('site-monitoring')" data-view="site-monitoring" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <circle cx="12" cy="12" r="10"/><path d="M2 12h20"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Site Monitoring</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('notifications')" data-view="notifications" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
                                    <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Notifications</span>
                            </a>
                            
                            <a class="menu-item" onclick="app.navigate('settings')" data-view="settings" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #94a3b8; text-decoration: none; transition: all 0.2s; cursor: pointer;" onmouseover="this.style.background='#334155'; this.style.color='#e2e8f0'" onmouseout="this.style.background='transparent'; this.style.color='#94a3b8'">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <circle cx="12" cy="12" r="3"/>
                                    <path d="M12 1v6m0 6v6m6-12h-6m-6 0h6"/>
                                </svg>
                                <span style="font-size: 14px; font-weight: 500;">Settings</span>
                            </a>
                        </nav>
                    </div>
                </div>

                <!-- Logout Section -->
                <div style="padding: 20px; border-top: 1px solid #334155;">
                    <a onclick="app.logout()" style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; color: #f87171; text-decoration: none; transition: all 0.2s; cursor: pointer; border-radius: 8px;" onmouseover="this.style.background='#7f1d1d'; this.style.color='white'" onmouseout="this.style.background='transparent'; this.style.color='#f87171'">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                            <polyline points="16 17 21 12 16 7"/>
                            <line x1="21" y1="12" x2="9" y2="12"/>
                        </svg>
                        <span style="font-size: 14px; font-weight: 500;">Logout</span>
                    </a>
                </div>
            </div>

            <!-- Main Content Area -->
            <div class="main-content" style="flex: 1; height: 100vh; overflow-y: auto; background: #f8fafc;">
                <!-- Dashboard Content -->
                <div id="dashboard-content" class="content-view active" style="padding: 32px; display: block;">
                    <!-- Header Section -->
                    <div style="margin-bottom: 40px;">
                        <h1 style="font-size: 40px; font-weight: 700; color: #0f172a; margin-bottom: 8px;">Dashboard</h1>
                        <p style="color: #64748b; font-size: 16px;">Welcome back! Here's what's happening with your organization today.</p>
                    </div>

                    <!-- Statistics Cards Row 1 -->
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 24px; margin-bottom: 32px;">
                        <!-- Total Employees Card -->
                        <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #667eea; transition: all 0.3s ease; cursor: pointer;" onmouseover="this.style.boxShadow='0 10px 30px rgba(102, 126, 234, 0.15)'; this.style.transform='translateY(-4px)'" onmouseout="this.style.boxShadow='0 1px 3px rgba(0, 0, 0, 0.1)'; this.style.transform='translateY(0)'">
                            <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                                <div>
                                    <p style="color: #64748b; font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 12px;">Total Employees</p>
                                    <h3 id="dashTotalEmployees" style="font-size: 36px; font-weight: 700; color: #0f172a; margin: 0;">--</h3>
                                    <p style="color: #94a3b8; font-size: 12px; margin-top: 8px;">Active in system</p>
                                </div>
                                <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                                    </svg>
                                </div>
                            </div>
                        </div>

                        <!-- Active Now Card -->
                        <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #10b981; transition: all 0.3s ease; cursor: pointer;" onmouseover="this.style.boxShadow='0 10px 30px rgba(16, 185, 129, 0.15)'; this.style.transform='translateY(-4px)'" onmouseout="this.style.boxShadow='0 1px 3px rgba(0, 0, 0, 0.1)'; this.style.transform='translateY(0)'">
                            <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                                <div>
                                    <p style="color: #64748b; font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 12px;">Active Now</p>
                                    <h3 id="dashActiveNow" style="font-size: 36px; font-weight: 700; color: #0f172a; margin: 0;">--</h3>
                                    <p style="color: #94a3b8; font-size: 12px; margin-top: 8px;">Online employees</p>
                                </div>
                                <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
                                    </svg>
                                </div>
                            </div>
                        </div>

                        <!-- Web Activity Card -->
                        <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #f59e0b; transition: all 0.3s ease; cursor: pointer;" onmouseover="this.style.boxShadow='0 10px 30px rgba(245, 158, 11, 0.15)'; this.style.transform='translateY(-4px)'" onmouseout="this.style.boxShadow='0 1px 3px rgba(0, 0, 0, 0.1)'; this.style.transform='translateY(0)'">
                            <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                                <div>
                                    <p style="color: #64748b; font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 12px;">Web Activity</p>
                                    <h3 id="dashWebActivity" style="font-size: 36px; font-weight: 700; color: #0f172a; margin: 0;">--</h3>
                                    <p style="color: #94a3b8; font-size: 12px; margin-top: 8px;">Activities logged today</p>
                                </div>
                                <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <circle cx="12" cy="12" r="10"/><path d="M2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                                    </svg>
                                </div>
                            </div>
                        </div>

                        <!-- Inactivity Card -->
                        <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #0ea5e9; transition: all 0.3s ease; cursor: pointer;" onmouseover="this.style.boxShadow='0 10px 30px rgba(14, 165, 233, 0.15)'; this.style.transform='translateY(-4px)'" onmouseout="this.style.boxShadow='0 1px 3px rgba(0, 0, 0, 0.1)'; this.style.transform='translateY(0)'">
                            <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                                <div>
                                    <p style="color: #64748b; font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 12px;">Inactivity Alerts</p>
                                    <h3 id="dashInactivity" style="font-size: 36px; font-weight: 700; color: #0f172a; margin: 0;">--</h3>
                                    <p style="color: #94a3b8; font-size: 12px; margin-top: 8px;">Idle instances detected</p>
                                </div>
                                <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); border-radius: 12px; display: flex; align-items: center; justify-content: center;">
                                    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                                        <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3.05h16.94a2 2 0 0 0 1.71-3.05L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
                                    </svg>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Quick Actions Section -->
                    <div style="margin-bottom: 40px;">
                        <h2 style="font-size: 24px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">Quick Actions</h2>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px;">
                            <button onclick="app.navigate('employee-management')" style="padding: 18px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; font-size: 15px; transition: all 0.3s ease; display: flex; align-items: center; gap: 10px; justify-content: center;" onmouseover="this.style.boxShadow='0 10px 30px rgba(102, 126, 234, 0.3)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='none'; this.style.transform='translateY(0)'">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/></svg>
                                👥 Employee Management
                            </button>
                            <button onclick="app.navigate('attendance-reports')" style="padding: 18px 24px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; font-size: 15px; transition: all 0.3s ease; display: flex; align-items: center; gap: 10px; justify-content: center;" onmouseover="this.style.boxShadow='0 10px 30px rgba(16, 185, 129, 0.3)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='none'; this.style.transform='translateY(0)'">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>
                                📅 Attendance Reports
                            </button>
                            <button onclick="app.navigate('live-systems')" style="padding: 18px 24px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; font-size: 15px; transition: all 0.3s ease; display: flex; align-items: center; gap: 10px; justify-content: center;" onmouseover="this.style.boxShadow='0 10px 30px rgba(245, 158, 11, 0.3)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='none'; this.style.transform='translateY(0)'">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><line x1="8" y1="21" x2="16" y2="21"/></svg>
                                🖥️ Live Systems
                            </button>
                            <button onclick="app.navigate('analytics')" style="padding: 18px 24px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; font-size: 15px; transition: all 0.3s ease; display: flex; align-items: center; gap: 10px; justify-content: center;" onmouseover="this.style.boxShadow='0 10px 30px rgba(14, 165, 233, 0.3)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='none'; this.style.transform='translateY(0)'">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="2" x2="12" y2="22"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>
                                📊 Analytics
                            </button>
                        </div>
                    </div>

                    <!-- System Status Section -->
                    <div style="margin-bottom: 40px;">
                        <h2 style="font-size: 24px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">📊 Activity Overview</h2>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 24px; margin-bottom: 24px;">
                            <!-- Web Activity Curved Line Chart -->
                            <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); border: 1px solid #f0f4f8; overflow: hidden; transition: all 0.3s ease;" onmouseover="this.style.boxShadow='0 8px 24px rgba(59, 130, 246, 0.15)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='0 2px 8px rgba(0, 0, 0, 0.08)'; this.style.transform='translateY(0)'">
                                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
                                    <h4 style="font-size: 16px; font-weight: 700; color: #0f172a; margin: 0;">📊 Web Browsing</h4>
                                    <span style="background: linear-gradient(135deg, #3b82f6 0%, #1e40af 100%); color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600;">Active</span>
                                </div>
                                <svg style="width: 100%; height: 200px;" viewBox="0 0 280 140" preserveAspectRatio="xMidYMid meet">
                                    <!-- Grid lines -->
                                    <line x1="0" y1="35" x2="280" y2="35" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3,3" opacity="0.5"/>
                                    <line x1="0" y1="70" x2="280" y2="70" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3,3" opacity="0.5"/>
                                    <line x1="0" y1="105" x2="280" y2="105" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3,3" opacity="0.5"/>
                                    <!-- Area gradient -->
                                    <defs>
                                        <linearGradient id="gradient1" x1="0%" y1="0%" x2="0%" y2="100%">
                                            <stop offset="0%" style="stop-color:#3b82f6;stop-opacity:0.3" />
                                            <stop offset="100%" style="stop-color:#3b82f6;stop-opacity:0" />
                                        </linearGradient>
                                    </defs>
                                    <!-- Filled area under curve -->
                                    <path d="M 20 80 Q 50 40, 80 60 T 140 45 T 200 70 T 260 55 L 260 140 L 20 140 Z" fill="url(#gradient1)" />
                                    <!-- Curved line -->
                                    <path d="M 20 80 Q 50 40, 80 60 T 140 45 T 200 70 T 260 55" stroke="#3b82f6" stroke-width="3" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                                    <!-- Data points -->
                                    <circle cx="20" cy="80" r="4" fill="#3b82f6" style="filter: drop-shadow(0 2px 4px rgba(59, 130, 246, 0.3));"/>
                                    <circle cx="80" cy="60" r="4" fill="#3b82f6" style="filter: drop-shadow(0 2px 4px rgba(59, 130, 246, 0.3));"/>
                                    <circle cx="140" cy="45" r="4" fill="#3b82f6" style="filter: drop-shadow(0 2px 4px rgba(59, 130, 246, 0.3));"/>
                                    <circle cx="200" cy="70" r="4" fill="#3b82f6" style="filter: drop-shadow(0 2px 4px rgba(59, 130, 246, 0.3));"/>
                                    <circle cx="260" cy="55" r="4" fill="#3b82f6" style="filter: drop-shadow(0 2px 4px rgba(59, 130, 246, 0.3));"/>
                                    <!-- Labels -->
                                    <text x="20" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Mon</text>
                                    <text x="80" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Tue</text>
                                    <text x="140" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Wed</text>
                                    <text x="200" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Thu</text>
                                    <text x="260" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Fri</text>
                                </svg>
                                <p style="color: #64748b; font-size: 13px; margin-top: 16px; text-align: center; font-weight: 500;">180 sessions • Peak: Wednesday</p>
                            </div>

                            <!-- Application Usage Curved Line Chart -->
                            <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); border: 1px solid #f0f4f8; overflow: hidden; transition: all 0.3s ease;" onmouseover="this.style.boxShadow='0 8px 24px rgba(16, 185, 129, 0.15)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='0 2px 8px rgba(0, 0, 0, 0.08)'; this.style.transform='translateY(0)'">
                                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
                                    <h4 style="font-size: 16px; font-weight: 700; color: #0f172a; margin: 0;">📱 App Usage</h4>
                                    <span style="background: linear-gradient(135deg, #10b981 0%, #047857 100%); color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600;">Active</span>
                                </div>
                                <svg style="width: 100%; height: 200px;" viewBox="0 0 280 140" preserveAspectRatio="xMidYMid meet">
                                    <!-- Grid lines -->
                                    <line x1="0" y1="35" x2="280" y2="35" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3,3" opacity="0.5"/>
                                    <line x1="0" y1="70" x2="280" y2="70" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3,3" opacity="0.5"/>
                                    <line x1="0" y1="105" x2="280" y2="105" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3,3" opacity="0.5"/>
                                    <!-- Area gradient -->
                                    <defs>
                                        <linearGradient id="gradient2" x1="0%" y1="0%" x2="0%" y2="100%">
                                            <stop offset="0%" style="stop-color:#10b981;stop-opacity:0.3" />
                                            <stop offset="100%" style="stop-color:#10b981;stop-opacity:0" />
                                        </linearGradient>
                                    </defs>
                                    <!-- Filled area under curve -->
                                    <path d="M 20 90 Q 50 55, 80 35 T 140 60 T 200 45 T 260 80 L 260 140 L 20 140 Z" fill="url(#gradient2)" />
                                    <!-- Curved line -->
                                    <path d="M 20 90 Q 50 55, 80 35 T 140 60 T 200 45 T 260 80" stroke="#10b981" stroke-width="3" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                                    <!-- Data points -->
                                    <circle cx="20" cy="90" r="4" fill="#10b981" style="filter: drop-shadow(0 2px 4px rgba(16, 185, 129, 0.3));"/>
                                    <circle cx="80" cy="35" r="4" fill="#10b981" style="filter: drop-shadow(0 2px 4px rgba(16, 185, 129, 0.3));"/>
                                    <circle cx="140" cy="60" r="4" fill="#10b981" style="filter: drop-shadow(0 2px 4px rgba(16, 185, 129, 0.3));"/>
                                    <circle cx="200" cy="45" r="4" fill="#10b981" style="filter: drop-shadow(0 2px 4px rgba(16, 185, 129, 0.3));"/>
                                    <circle cx="260" cy="80" r="4" fill="#10b981" style="filter: drop-shadow(0 2px 4px rgba(16, 185, 129, 0.3));"/>
                                    <!-- Labels -->
                                    <text x="20" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Mon</text>
                                    <text x="80" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Tue</text>
                                    <text x="140" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Wed</text>
                                    <text x="200" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Thu</text>
                                    <text x="260" y="125" text-anchor="middle" font-size="11" fill="#64748b" font-weight="600">Fri</text>
                                </svg>
                                <p style="color: #64748b; font-size: 13px; margin-top: 16px; text-align: center; font-weight: 500;">150 sessions • Peak: Tuesday</p>
                            </div>

                            <!-- Inactivity Circular Progress -->
                            <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); border: 1px solid #f0f4f8; overflow: hidden; transition: all 0.3s ease; display: flex; flex-direction: column; align-items: center; justify-content: center;" onmouseover="this.style.boxShadow='0 8px 24px rgba(245, 158, 11, 0.15)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='0 2px 8px rgba(0, 0, 0, 0.08)'; this.style.transform='translateY(0)'">
                                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; width: 100%;">
                                    <h4 style="font-size: 16px; font-weight: 700; color: #0f172a; margin: 0;">⏱️ Inactivity</h4>
                                    <span style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600;">Alert</span>
                                </div>
                                <svg style="width: 140px; height: 140px;" viewBox="0 0 140 140">
                                    <!-- Background circle -->
                                    <circle cx="70" cy="70" r="60" fill="none" stroke="#e2e8f0" stroke-width="8"/>
                                    <!-- Progress circle (65% full) -->
                                    <circle cx="70" cy="70" r="60" fill="none" stroke="url(#gradient3)" stroke-width="8" stroke-linecap="round" style="stroke-dasharray: 245 377; transform: rotate(-90deg); transform-origin: 70px 70px;" />
                                    <defs>
                                        <linearGradient id="gradient3" x1="0%" y1="0%" x2="100%" y2="100%">
                                            <stop offset="0%" style="stop-color:#f59e0b"/>
                                            <stop offset="100%" style="stop-color:#d97706"/>
                                        </linearGradient>
                                    </defs>
                                    <!-- Center text -->
                                    <text x="70" y="75" text-anchor="middle" font-size="28" font-weight="700" fill="#f59e0b">65%</text>
                                </svg>
                                <p style="color: #64748b; font-size: 13px; margin-top: 20px; text-align: center; font-weight: 500;">130 instances detected • 65% of workday</p>
                            </div>

                            <!-- Screenshots Circular Progress -->
                            <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); border: 1px solid #f0f4f8; overflow: hidden; transition: all 0.3s ease; display: flex; flex-direction: column; align-items: center; justify-content: center;" onmouseover="this.style.boxShadow='0 8px 24px rgba(139, 92, 246, 0.15)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='0 2px 8px rgba(0, 0, 0, 0.08)'; this.style.transform='translateY(0)'">
                                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; width: 100%;">
                                    <h4 style="font-size: 16px; font-weight: 700; color: #0f172a; margin: 0;">📸 Screenshots</h4>
                                    <span style="background: linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%); color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600;">Active</span>
                                </div>
                                <svg style="width: 140px; height: 140px;" viewBox="0 0 140 140">
                                    <!-- Background circle -->
                                    <circle cx="70" cy="70" r="60" fill="none" stroke="#e2e8f0" stroke-width="8"/>
                                    <!-- Progress circle (80% full) -->
                                    <circle cx="70" cy="70" r="60" fill="none" stroke="url(#gradient4)" stroke-width="8" stroke-linecap="round" style="stroke-dasharray: 301.6 377; transform: rotate(-90deg); transform-origin: 70px 70px;" />
                                    <defs>
                                        <linearGradient id="gradient4" x1="0%" y1="0%" x2="100%" y2="100%">
                                            <stop offset="0%" style="stop-color:#8b5cf6"/>
                                            <stop offset="100%" style="stop-color:#6d28d9"/>
                                        </linearGradient>
                                    </defs>
                                    <!-- Center text -->
                                    <text x="70" y="75" text-anchor="middle" font-size="28" font-weight="700" fill="#8b5cf6">80%</text>
                                </svg>
                                <p style="color: #64748b; font-size: 13px; margin-top: 20px; text-align: center; font-weight: 500;">160 captures • 80% coverage</p>
                            </div>
                        </div>
                    </div>

                    <!-- Top Domains Section -->
                    <div style="margin-bottom: 40px;">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                            <div>
                                <h2 style="font-size: 24px; font-weight: 700; color: #0f172a; margin: 0;">🌐 Most Used Domains</h2>
                                <p style="color: #94a3b8; font-size: 13px; margin: 4px 0 0 0;">Last 5 days • Across all users</p>
                            </div>
                            <button onclick="dashboard.refreshDomainUsage()" style="background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; border: none; padding: 8px 16px; border-radius: 8px; font-size: 13px; font-weight: 600; cursor: pointer; transition: all 0.3s ease;" onmouseover="this.style.boxShadow='0 6px 20px rgba(14, 165, 233, 0.3)'; this.style.transform='translateY(-2px)'" onmouseout="this.style.boxShadow='none'; this.style.transform='translateY(0)'">🔄 Refresh</button>
                        </div>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(350px, 1fr)); gap: 24px;">
                            <!-- Top 10 Domains Card -->
                            <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); border: 1px solid #f0f4f8; grid-column: span 1;">
                                <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px; display: flex; align-items: center; gap: 8px;">
                                    <span style="width: 4px; height: 24px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); border-radius: 2px;"></span>
                                    Top Domains
                                </h3>
                                <div id="topDomainsContainer" style="max-height: 400px; overflow-y: auto;">
                                    <div style="display: flex; align-items: center; justify-content: center; padding: 40px 20px; color: #94a3b8;">
                                        <div style="text-align: center;">
                                            <div style="font-size: 14px; margin-bottom: 8px;">Loading domain data...</div>
                                            <div style="width: 30px; height: 30px; border: 3px solid #e2e8f0; border-top: 3px solid #0ea5e9; border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto;" style=""></div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- Domain Statistics Card -->
                            <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); border: 1px solid #f0f4f8;">
                                <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 20px; display: flex; align-items: center; gap: 8px;">
                                    <span style="width: 4px; height: 24px; background: linear-gradient(135deg, #10b981 0%, #047857 100%); border-radius: 2px;"></span>
                                    Domain Statistics
                                </h3>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px;">
                                    <div style="background: linear-gradient(135deg, rgba(14, 165, 233, 0.1) 0%, rgba(14, 165, 233, 0.05) 100%); border-radius: 12px; padding: 16px; border-left: 4px solid #0ea5e9;">
                                        <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; margin-bottom: 8px;">Unique Domains</p>
                                        <h4 id="totalDomainsCount" style="font-size: 28px; font-weight: 700; color: #0ea5e9; margin: 0;">--</h4>
                                    </div>
                                    <div style="background: linear-gradient(135deg, rgba(16, 185, 129, 0.1) 0%, rgba(16, 185, 129, 0.05) 100%); border-radius: 12px; padding: 16px; border-left: 4px solid #10b981;">
                                        <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; margin-bottom: 8px;">Total Sessions</p>
                                        <h4 id="totalSessionsCount" style="font-size: 28px; font-weight: 700; color: #10b981; margin: 0;">--</h4>
                                    </div>
                                </div>
                                <div style="background: linear-gradient(135deg, rgba(245, 158, 11, 0.1) 0%, rgba(245, 158, 11, 0.05) 100%); border-radius: 12px; padding: 16px; border-left: 4px solid #f59e0b;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; margin-bottom: 8px;">Most Used Domain</p>
                                    <h4 id="topDomainName" style="font-size: 18px; font-weight: 700; color: #f59e0b; margin: 0; word-break: break-all;">--</h4>
                                    <p id="topDomainUsage" style="color: #64748b; font-size: 13px; margin-top: 8px;">0 sessions</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- System Status Section -->
                    <div style="background: white; border-radius: 16px; padding: 28px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                        <h3 style="font-size: 20px; font-weight: 700; color: #0f172a; margin-bottom: 20px;">System Status</h3>
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px;">
                            <div style="display: flex; align-items: center; gap: 12px;">
                                <div style="width: 12px; height: 12px; background: #10b981; border-radius: 50%; box-shadow: 0 0 10px rgba(16, 185, 129, 0.5);"></div>
                                <div>
                                    <p style="font-weight: 600; color: #0f172a; margin-bottom: 2px;">Database Connection</p>
                                    <p style="color: #64748b; font-size: 13px;">✓ Active</p>
                                </div>
                            </div>
                            <div style="display: flex; align-items: center; gap: 12px;">
                                <div style="width: 12px; height: 12px; background: #10b981; border-radius: 50%; box-shadow: 0 0 10px rgba(16, 185, 129, 0.5);"></div>
                                <div>
                                    <p style="font-weight: 600; color: #0f172a; margin-bottom: 2px;">API Server</p>
                                    <p style="color: #64748b; font-size: 13px;">✓ Running</p>
                                </div>
                            </div>
                            <div style="display: flex; align-items: center; gap: 12px;">
                                <div style="width: 12px; height: 12px; background: #10b981; border-radius: 50%; box-shadow: 0 0 10px rgba(16, 185, 129, 0.5);"></div>
                                <div>
                                    <p style="font-weight: 600; color: #0f172a; margin-bottom: 2px;">Data Sync</p>
                                    <p style="color: #64748b; font-size: 13px;">✓ Synchronized</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Other content views will be added here -->
                ${this.generateContentViews()}
            </div>
        `;
    },

    // Generate content views for all navigation items
    generateContentViews() {
        const placeholderViews = [
            'live-systems', 'analytics',
            'leave-management', 'notifications', 'settings'
        ];
        
        let content = '';
        
        // Add attendance reports view with full functionality
        content += this.generateAttendanceReportsView();
        
        // Add daily working hours view
        content += this.generateDailyWorkingHoursView();
        
        // Add engineer report view
        content += this.generateEngineerReportView();
        
        // Add functional log views
        content += this.generateWebBrowsingLogsView();
        content += this.generateApplicationUsageView();
        content += this.generateInactivityLogsView();
        content += this.generateScreenshotsView();
        
        // Add Analytics & Reports view
        content += this.generateAnalyticsView();
        
        // Add Employee Management view
        content += this.generateEmployeeManagementView();
        
        // Add Live Systems view
        content += this.generateLiveSystemsView();
        
        // Add Site Monitoring view
        content += this.generateSiteMonitoringView();
        
        // Add other views as placeholders
        content += placeholderViews.map(view => `
            <div id="${view}-content" class="content-view" style="padding: 32px; display: none;">
                <div style="text-align: center; padding: 80px 20px;">
                    <div style="width: 80px; height: 80px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 50%; margin: 0 auto 30px; display: flex; align-items: center; justify-content: center;">
                        <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                        </svg>
                    </div>
                    <h2 style="font-size: 28px; font-weight: 300; margin-bottom: 16px; color: #1e293b; text-transform: capitalize;">${view.replace(/-/g, ' ')}</h2>
                    <p style="font-size: 16px; color: #64748b; margin-bottom: 30px;">This page is ready for development</p>
                    <div style="background: white; border-radius: 16px; padding: 30px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); max-width: 500px; margin: 0 auto;">
                        <h4 style="color: #1e293b; margin-bottom: 16px;">Coming Soon</h4>
                        <p style="color: #64748b; font-size: 14px;">The ${view.replace(/-/g, ' ')} functionality will be implemented here.</p>
                        <button onclick="app.navigate('dashboard')" style="margin-top: 20px; padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                            Back to Dashboard
                        </button>
                    </div>
                </div>
            </div>
        `).join('');
        
        return content;
    },

    // ===== SITE MONITORING VIEW =====
    generateSiteMonitoringView() {
        return `
            <div id="site-monitoring-content" class="content-view" style="padding: 0; display: none; background: #f8fafc; height: calc(100vh - 80px); flex-direction: row; overflow: hidden;">
                <!-- Left Sidebar - Full Page Sites List -->
                <div id="sites-sidebar" style="width: 300px; background: linear-gradient(135deg, #f8fafc 0%, #ffffff 100%); border-right: 2px solid #e2e8f0; display: flex; flex-direction: column; overflow: hidden; height: 100%;">
                    <div style="padding: 24px 20px; border-bottom: 2px solid #e2e8f0; background: white;">
                        <h2 style="font-size: 18px; font-weight: 800; color: #0f172a; margin: 0 0 16px 0; display: flex; align-items: center; gap: 8px;">🌐 <span>Monitored Sites</span></h2>
                        <button onclick="app.addNewSiteForm()" style="width: 100%; padding: 10px 12px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; font-size: 13px; transition: all 0.3s; box-shadow: 0 2px 8px rgba(2, 132, 199, 0.2);" onmouseover="this.style.boxShadow='0 4px 12px rgba(2, 132, 199, 0.3)'" onmouseout="this.style.boxShadow='0 2px 8px rgba(2, 132, 199, 0.2)'">+ Add New Site</button>
                    </div>
                    <div id="sites-list" style="flex: 1; overflow-y: auto; padding: 12px; display: flex; flex-direction: column; gap: 8px;">
                        <p style="text-align: center; color: #94a3b8; font-size: 12px; padding: 40px 20px; margin: 0;">Loading sites...</p>
                    </div>
                </div>

                <!-- Right Content -->
                <div style="flex: 1; display: flex; flex-direction: column; overflow: hidden; background: #f8fafc; width: 100%;">
                    <!-- Header -->
                    <div style="background: white; border-bottom: 1px solid #e2e8f0; padding: 20px 24px; display: flex; align-items: center; justify-content: space-between; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
                        <h1 id="monitoring-title" style="font-size: 26px; font-weight: 700; color: #0f172a; margin: 0;">Select a Site</h1>
                        <div style="display: flex; gap: 12px; align-items: center;">
                            <span id="refresh-indicator" style="font-size: 12px; color: #64748b; background: #f3f4f6; padding: 6px 12px; border-radius: 6px;">Auto-refresh: <span id="refresh-timer" style="font-weight: 600; color: #0284c7;">30s</span></span>
                            <button onclick="app.manualRefreshSite()" style="padding: 8px 16px; background: #f3f4f6; color: #1f2937; border: 1px solid #d1d5db; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 12px; transition: all 0.3s;" onmouseover="this.style.background='#e5e7eb'" onmouseout="this.style.background='#f3f4f6'">🔄 Refresh</button>
                        </div>
                    </div>

                    <!-- Main Content -->
                    <div style="flex: 1; overflow-y: auto; padding: 24px;">
                        <div id="site-monitoring-empty" style="text-align: center; padding: 60px 20px;">
                            <div style="width: 80px; height: 80px; background: linear-gradient(135deg, #e0f2fe 0%, #bae6fd 100%); border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;">
                                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#0284c7" stroke-width="2">
                                    <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>
                                </svg>
                            </div>
                            <p style="color: #64748b; font-size: 14px;">Select a site from the left sidebar to view live metrics</p>
                        </div>

                        <!-- Live Dashboard (Hidden by default) -->
                        <div id="site-dashboard" style="display: none;">
                            <!-- Offline Alert -->
                            <div id="offline-alert" style="display: none; background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%); border: 2px solid #ef4444; border-radius: 12px; padding: 24px; margin-bottom: 24px; text-align: center;">
                                <div style="font-size: 48px; margin-bottom: 12px;">⚠️</div>
                                <h2 style="color: #991b1b; font-size: 20px; font-weight: 700; margin: 0 0 8px 0;">SITE IS OFFLINE</h2>
                                <p style="color: #7f1d1d; font-size: 14px; margin: 0 0 16px 0;">This website is currently unavailable. Please check the downtime history below for details.</p>
                                <div style="display: flex; justify-content: center; gap: 12px;">
                                    <button onclick="app.manualRefreshSite()" style="padding: 10px 20px; background: #ef4444; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; font-size: 13px; transition: all 0.3s;">🔄 Retry Connection</button>
                                    <a id="site-url-link" href="#" target="_blank" style="padding: 10px 20px; background: #f87171; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; font-size: 13px; text-decoration: none; display: inline-block; transition: all 0.3s;" onmouseover="this.style.background='#ea5455'" onmouseout="this.style.background='#f87171'">🌐 Visit Site</a>
                                </div>
                            </div>
                            
                            <!-- Status & Quick Stats -->
                            <div id="site-metrics-cards" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px;">
                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); border-left: 4px solid #0284c7;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">STATUS</p>
                                    <div id="site-status-display" style="display: flex; align-items: center; gap: 8px;">
                                        <span id="status-indicator" style="width: 12px; height: 12px; border-radius: 50%; background: #94a3b8;"></span>
                                        <span id="status-text" style="font-size: 16px; font-weight: 700; color: #1f2937;">--</span>
                                    </div>
                                    <p id="status-time" style="color: #94a3b8; font-size: 11px; margin: 8px 0 0 0;">Last checked: --</p>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); border-left: 4px solid #0ea5e9;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">RESPONSE TIME</p>
                                    <div style="display: flex; align-items: baseline; gap: 4px;">
                                        <span id="response-time" style="font-size: 24px; font-weight: 700; color: #0ea5e9;">-- ms</span>
                                        <span id="response-quality" style="font-size: 12px; color: #94a3b8;">Good</span>
                                    </div>
                                    <p style="color: #94a3b8; font-size: 11px; margin: 8px 0 0 0;">🟢 Optimal performance</p>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); border-left: 4px solid #10b981;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">UPTIME (30 DAYS)</p>
                                    <div style="display: flex; align-items: baseline; gap: 4px;">
                                        <span id="uptime-percent" style="font-size: 24px; font-weight: 700; color: #10b981;">-- %</span>
                                    </div>
                                    <p style="color: #94a3b8; font-size: 11px; margin: 8px 0 0 0;">Excellent reliability</p>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); border-left: 4px solid #f59e0b;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">VISITORS TODAY</p>
                                    <div style="display: flex; align-items: baseline; gap: 4px;">
                                        <span id="visitors-today" style="font-size: 24px; font-weight: 700; color: #f59e0b;">0</span>
                                    </div>
                                    <p style="color: #94a3b8; font-size: 11px; margin: 8px 0 0 0;">Real-time count</p>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); border-left: 4px solid #8b5cf6;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">PAGE VIEWS</p>
                                    <div style="display: flex; align-items: baseline; gap: 4px;">
                                        <span id="page-views" style="font-size: 24px; font-weight: 700; color: #8b5cf6;">0</span>
                                    </div>
                                    <p style="color: #94a3b8; font-size: 11px; margin: 8px 0 0 0;">Last 24 hours</p>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); border-left: 4px solid #ec4899;">
                                    <p style="color: #64748b; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">AVG LOAD TIME</p>
                                    <div style="display: flex; align-items: baseline; gap: 4px;">
                                        <span id="avg-load-time" style="font-size: 24px; font-weight: 700; color: #ec4899;">-- ms</span>
                                    </div>
                                    <p style="color: #94a3b8; font-size: 11px; margin: 8px 0 0 0;">Page speed metric</p>
                                </div>
                            </div>

                            <!-- Charts Section -->
                            <div id="site-charts-section" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 20px; margin-bottom: 24px;">
                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                    <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">📊 Live Traffic (Last 60 seconds)</h3>
                                    <div id="live-traffic-chart" style="height: 200px; position: relative; background: linear-gradient(180deg, rgba(14, 165, 233, 0.05) 0%, transparent 100%); border-radius: 8px; padding: 12px; display: flex; align-items: flex-end; justify-content: space-around; gap: 2px; border: 1px solid #e2e8f0;"></div>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                    <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">⚡ Server Load (Last 60 seconds)</h3>
                                    <div id="load-status-chart" style="height: 200px; position: relative; background: linear-gradient(180deg, rgba(16, 185, 129, 0.05) 0%, transparent 100%); border-radius: 8px; padding: 12px; display: flex; align-items: flex-end; justify-content: space-around; gap: 2px; border: 1px solid #e2e8f0;"></div>
                                    <div style="display: flex; justify-content: space-around; margin-top: 12px; padding-top: 12px; border-top: 1px solid #e2e8f0;">
                                        <div style="text-align: center;">
                                            <p style="color: #94a3b8; font-size: 11px; margin: 0 0 4px 0;">Current</p>
                                            <p id="current-load" style="color: #10b981; font-size: 16px; font-weight: 700; margin: 0;">-- %</p>
                                        </div>
                                        <div style="text-align: center;">
                                            <p style="color: #94a3b8; font-size: 11px; margin: 0 0 4px 0;">Average</p>
                                            <p id="avg-load" style="color: #3b82f6; font-size: 16px; font-weight: 700; margin: 0;">-- %</p>
                                        </div>
                                        <div style="text-align: center;">
                                            <p style="color: #94a3b8; font-size: 11px; margin: 0 0 4px 0;">Peak</p>
                                            <p id="peak-load" style="color: #f59e0b; font-size: 16px; font-weight: 700; margin: 0;">-- %</p>
                                        </div>
                                    </div>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                    <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">🔗 Traffic Source</h3>
                                    <div id="traffic-sources" style="display: flex; flex-direction: column; gap: 12px;">
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #3b82f6; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Google</span>
                                            </div>
                                            <span class="traffic-source-percent" style="font-size: 12px; font-weight: 600; color: #3b82f6;">-- %</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #ec4899; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Social Media</span>
                                            </div>
                                            <span class="traffic-source-percent" style="font-size: 12px; font-weight: 600; color: #ec4899;">-- %</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #10b981; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Direct</span>
                                            </div>
                                            <span class="traffic-source-percent" style="font-size: 12px; font-weight: 600; color: #10b981;">-- %</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #f59e0b; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Other</span>
                                            </div>
                                            <span class="traffic-source-percent" style="font-size: 12px; font-weight: 600; color: #f59e0b;">-- %</span>
                                        </div>
                                    </div>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                    <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">📱 Device Distribution</h3>
                                    <div id="device-distribution" style="display: flex; flex-direction: column; gap: 12px;">
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #3b82f6; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Desktop</span>
                                            </div>
                                            <span class="device-percent" style="font-size: 12px; font-weight: 600; color: #3b82f6;">-- %</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #ec4899; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Mobile</span>
                                            </div>
                                            <span class="device-percent" style="font-size: 12px; font-weight: 600; color: #ec4899;">-- %</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <div style="display: flex; align-items: center; gap: 8px;">
                                                <div style="width: 8px; height: 8px; background: #10b981; border-radius: 50%;"></div>
                                                <span style="font-size: 12px; color: #1f2937; font-weight: 500;">Tablet</span>
                                            </div>
                                            <span class="device-percent" style="font-size: 12px; font-weight: 600; color: #10b981;">-- %</span>
                                        </div>
                                    </div>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                    <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">🌍 Top Countries</h3>
                                    <div id="top-locations" style="display: flex; flex-direction: column; gap: 10px;">
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <span style="font-size: 12px; color: #1f2937;">🇮🇳 India</span>
                                            <span style="font-size: 12px; font-weight: 600; color: #1f2937;">0 visits</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <span style="font-size: 12px; color: #1f2937;">🇺🇸 USA</span>
                                            <span style="font-size: 12px; font-weight: 600; color: #1f2937;">0 visits</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <span style="font-size: 12px; color: #1f2937;">🇬🇧 UK</span>
                                            <span style="font-size: 12px; font-weight: 600; color: #1f2937;">0 visits</span>
                                        </div>
                                    </div>
                                </div>

                                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                    <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">⭐ Popular Pages</h3>
                                    <div id="popular-pages" style="display: flex; flex-direction: column; gap: 10px;">
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <span style="font-size: 12px; color: #1f2937; word-break: break-all;">/home</span>
                                            <span style="font-size: 12px; font-weight: 600; color: #1f2937;">0 views</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <span style="font-size: 12px; color: #1f2937; word-break: break-all;">/about</span>
                                            <span style="font-size: 12px; font-weight: 600; color: #1f2937;">0 views</span>
                                        </div>
                                        <div style="display: flex; align-items: center; justify-content: space-between;">
                                            <span style="font-size: 12px; color: #1f2937; word-break: break-all;">/contact</span>
                                            <span style="font-size: 12px; font-weight: 600; color: #1f2937;">0 views</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- Live Preview -->
                            <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08); margin-bottom: 24px;">
                                <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">📺 Live Website Preview</h3>
                                <div id="site-preview-container" style="width: 100%; height: 400px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; display: flex; align-items: center; justify-content: center; position: relative; overflow: hidden;">
                                    <div id="preview-loading" style="text-align: center;">
                                        <div style="width: 60px; height: 60px; background: linear-gradient(135deg, #e0f2fe 0%, #bae6fd 100%); border-radius: 50%; margin: 0 auto 16px; display: flex; align-items: center; justify-content: center;">
                                            <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="#0284c7" stroke-width="2">
                                                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>
                                            </svg>
                                        </div>
                                        <p style="color: #64748b; font-size: 13px; margin: 0;">Loading preview...</p>
                                    </div>
                                </div>
                            </div>

                            <!-- Downtime History -->
                            <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                                <h3 style="font-size: 14px; font-weight: 600; color: #1f2937; margin: 0 0 16px 0;">⚠️ Downtime History (Last 30 Days)</h3>
                                <div id="downtime-history-container" style="border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;">
                                    <div id="downtime-history" style="max-height: 400px; overflow-y: auto;">
                                        <p style="text-align: center; color: #94a3b8; font-size: 12px; padding: 40px 20px; margin: 0;">Loading downtime history...</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // ===== EMPLOYEE MANAGEMENT VIEW =====
    generateEmployeeManagementView() {
        return `
            <div id="employee-management-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">👥 Employee Management</h1>
                    <p style="color: #64748b; font-size: 16px;">Manage employees and departments</p>
                </div>

                <!-- Tab Navigation -->
                <div style="display: flex; gap: 12px; margin-bottom: 24px; border-bottom: 2px solid #e5e7eb;">
                    <button onclick="app.switchEmployeeTab('employees')" id="emp-tab-btn" style="padding: 12px 24px; border: none; background: transparent; color: #667eea; font-weight: 600; border-bottom: 3px solid #667eea; cursor: pointer; font-size: 15px;">
                        👤 Employees
                    </button>
                    <button onclick="app.switchEmployeeTab('departments')" id="dept-tab-btn" style="padding: 12px 24px; border: none; background: transparent; color: #6b7280; font-weight: 500; cursor: pointer; font-size: 15px;">
                        🏢 Departments
                    </button>
                </div>

                <!-- EMPLOYEES TAB -->
                <div id="employees-tab" style="display: block;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                        <h2 style="font-size: 20px; font-weight: 600; color: #1f2937; margin: 0;">Employee List</h2>
                        <button onclick="app.showAddEmployeeForm()" style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                            + Add Employee
                        </button>
                    </div>
                    <div id="employeesContainer" style="background: white; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                        <div style="padding: 40px; text-align: center; color: #6b7280;">
                            <div style="margin-bottom: 16px;">📋</div>
                            <p>Click "Add Employee" button or load employees</p>
                            <button onclick="app.loadEmployeesList()" style="margin-top: 16px; padding: 8px 16px; background: #f3f4f6; border: none; border-radius: 6px; cursor: pointer; color: #374151; font-weight: 500;">
                                Load Employees
                            </button>
                        </div>
                    </div>
                </div>

                <!-- DEPARTMENTS TAB -->
                <div id="departments-tab" style="display: none;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                        <h2 style="font-size: 20px; font-weight: 600; color: #1f2937; margin: 0;">Department List</h2>
                        <button onclick="app.showAddDepartmentForm()" style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                            + Add Department
                        </button>
                    </div>
                    <div id="departmentsContainer" style="background: white; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                        <div style="padding: 40px; text-align: center; color: #6b7280;">
                            <div style="margin-bottom: 16px;">📋</div>
                            <p>Click "Add Department" button or load departments</p>
                            <button onclick="app.loadDepartmentsList()" style="margin-top: 16px; padding: 8px 16px; background: #f3f4f6; border: none; border-radius: 6px; cursor: pointer; color: #374151; font-weight: 500;">
                                Load Departments
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // Generate Daily Working Hours view
    generateDailyWorkingHoursView() {
        return `
            <div id="daily-working-hours-content" class="content-view" style="padding: 32px; display: none;">
                <!-- Header Section -->
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 32px;">
                    <div>
                        <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">⏱️ Daily Working Hours</h1>
                        <p style="color: #64748b; font-size: 16px;">View total working hours for selected employee</p>
                    </div>
                    <div style="display: flex; gap: 12px;">
                        <button onclick="app.refreshDailyHours()" style="padding: 12px 20px; background: white; border: 1px solid #d1d5db; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px; transition: all 0.2s;">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <polyline points="23 4 23 10 17 10"/>
                                <polyline points="1 20 1 14 7 14"/>
                                <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15"/>
                            </svg>
                            Refresh
                        </button>
                    </div>
                </div>

                <!-- Filter Section -->
                <div style="background: white; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; align-items: end;">
                        
                        <!-- Date Selector -->
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 Select Date</label>
                            <input 
                                type="date" 
                                id="dailyWorkingDate"
                                style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;"
                                value="${new Date().toISOString().split('T')[0]}"
                            >
                        </div>
                        
                        <!-- Employee Filter -->
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">👤 Select Engineer</label>
                            <select 
                                id="dailyWorkingEmployee"
                                style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white;"
                            >
                                <option value="">Loading employees...</option>
                            </select>
                        </div>
                        
                        <!-- Action Button -->
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.loadDailyWorkingHours()" style="padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; white-space: nowrap;">
                                View Daily Hours
                            </button>
                            <button onclick="app.clearDailyHoursFilters()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                Reset
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Daily Stats Cards -->
                <div id="dailyWorkingStatsCards" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; margin-bottom: 24px;">
                    <!-- Will be populated dynamically -->
                </div>

                <!-- Detailed Records -->
                <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    
                    <!-- Table Header -->
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb;">
                        <h3 style="font-size: 18px; font-weight: 600; color: #1f2937; margin-bottom: 4px;">📋 Daily Work Sessions</h3>
                        <div id="dailyRecordsInfo" style="color: #6b7280; font-size: 14px;">Select an employee and date to view sessions</div>
                    </div>
                    
                    <!-- Records Container -->
                    <div id="dailyWorkingRecordsContainer" style="padding: 24px;">
                        <div style="text-align: center; color: #6b7280;">
                            <div style="margin-bottom: 16px;">📭</div>
                            <p>Select an employee and date to view working sessions</p>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    generateEngineerReportView() {
        return `
            <div id="engineer-report-content" class="content-view" style="padding: 32px; display: none; background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); min-height: 100vh;">
                <!-- Premium Header Section -->
                <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 32px;">
                    <div>
                        <h1 style="font-size: 40px; font-weight: 700; color: #0f172a; margin-bottom: 8px; letter-spacing: -0.5px;">📊 Engineer Activity Report</h1>
                        <p style="color: #64748b; font-size: 15px; font-weight: 500;">Comprehensive daily activity analysis with real-time metrics</p>
                    </div>
                    <div style="display: flex; gap: 12px;">
                        <button onclick="app.exportEngineerReport()" style="padding: 12px 24px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 12px; cursor: pointer; font-weight: 600; display: flex; align-items: center; gap: 8px; transition: all 0.3s; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.2);">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                <polyline points="7 10 12 15 17 10"/>
                                <line x1="12" y1="15" x2="12" y2="3"/>
                            </svg>
                            Export CSV
                        </button>
                    </div>
                </div>

                <!-- Filter Section - Premium Design -->
                <div style="background: white; padding: 28px; border-radius: 18px; margin-bottom: 28px; box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1); border-top: 4px solid #667eea;">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 24px; align-items: end;">
                        
                        <!-- Engineer Dropdown -->
                        <div>
                            <label style="display: block; margin-bottom: 10px; font-weight: 600; color: #1f2937; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;">👨‍💼 Engineer</label>
                            <select 
                                id="reportEngineerSelect"
                                style="width: 100%; padding: 12px 14px; border: 2px solid #e5e7eb; border-radius: 10px; font-size: 14px; background: white; cursor: pointer; font-weight: 500; transition: all 0.2s; -webkit-appearance: none; appearance: none; background-image: url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23667eea' d='M6 9L1 4h10z'/%3E%3C/svg%3E\"); background-repeat: no-repeat; background-position: right 12px center; padding-right: 36px;"
                                onchange="app.onEngineerSelected()"
                            >
                                <option value="">Select engineer...</option>
                            </select>
                        </div>
                        
                        <!-- Date Picker -->
                        <div>
                            <label style="display: block; margin-bottom: 10px; font-weight: 600; color: #1f2937; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;">📅 Date</label>
                            <input 
                                type="date" 
                                id="reportDatePicker"
                                style="width: 100%; padding: 12px 14px; border: 2px solid #e5e7eb; border-radius: 10px; font-size: 14px; font-weight: 500; cursor: pointer;"
                                value="${new Date().toISOString().split('T')[0]}"
                            >
                        </div>
                        
                        <!-- Buttons -->
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.generateEngineerActivityReport()" style="flex: 1; padding: 12px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; white-space: nowrap; transition: all 0.3s; box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);">
                                📈 Generate Report
                            </button>
                            <button onclick="app.clearEngineerReportFilters()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: 2px solid #e5e7eb; border-radius: 10px; cursor: pointer; font-weight: 600;">
                                Reset
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Report Stats Cards - Premium Grid -->
                <div id="engineerReportStatsCards" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 20px; margin-bottom: 28px;">
                    <!-- Will be populated dynamically -->
                </div>

                <!-- Charts Section - Top Row -->
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 24px; margin-bottom: 24px;">
                    <!-- Time Distribution Pie Chart -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); padding: 28px; border-left: 5px solid #667eea;">
                        <h3 style="font-size: 16px; font-weight: 700; color: #1f2937; margin-bottom: 20px; display: flex; align-items: center; gap: 8px;">⏱️ Time Distribution</h3>
                        <canvas id="workingTimeChart" style="max-height: 280px;"></canvas>
                    </div>

                    <!-- Productivity Gauge Chart -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); padding: 28px; border-left: 5px solid #10b981;">
                        <h3 style="font-size: 16px; font-weight: 700; color: #1f2937; margin-bottom: 20px; display: flex; align-items: center; gap: 8px;">📊 Productivity Score</h3>
                        <canvas id="productivityGaugeChart" style="max-height: 280px;"></canvas>
                    </div>
                </div>

                <!-- Details Tables Section -->
                <div style="display: grid; grid-template-columns: 1fr; gap: 24px; margin-bottom: 24px;">
                    <!-- Punch In/Out Table -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); overflow: hidden; border-left: 5px solid #f59e0b;">
                        <div style="padding: 24px; border-bottom: 2px solid #f3f4f6;">
                            <h3 style="font-size: 16px; font-weight: 700; color: #1f2937;">🕐 Punch In/Out & Working Hours</h3>
                        </div>
                        <div id="punchInOutTableContainer" style="overflow-x: auto;">
                            <div style="padding: 24px; text-align: center; color: #6b7280;">
                                Select an engineer and date to view details
                            </div>
                        </div>
                    </div>

                    <!-- Idle Time Details -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); overflow: hidden; border-left: 5px solid #ef4444;">
                        <div style="padding: 24px; border-bottom: 2px solid #f3f4f6;">
                            <h3 style="font-size: 16px; font-weight: 700; color: #1f2937;">😴 Significant Idle Periods (≥10 min)</h3>
                        </div>
                        <div id="idleTimeTableContainer" style="overflow-x: auto;">
                            <div style="padding: 24px; text-align: center; color: #6b7280;">
                                Idle time information will appear here
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Application & Domain Usage Section -->
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(500px, 1fr)); gap: 24px; margin-bottom: 24px;">
                    <!-- Top Applications Chart -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); padding: 28px; border-left: 5px solid #0ea5e9;">
                        <h3 style="font-size: 16px; font-weight: 700; color: #1f2937; margin-bottom: 20px;">💻 Top Applications</h3>
                        <canvas id="applicationUsageChart" style="max-height: 300px;"></canvas>
                    </div>

                    <!-- Top Domains Chart -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); padding: 28px; border-left: 5px solid #8b5cf6;">
                        <h3 style="font-size: 16px; font-weight: 700; color: #1f2937; margin-bottom: 20px;">🌐 Top Domains</h3>
                        <canvas id="domainUsageChart" style="max-height: 300px;"></canvas>
                    </div>
                </div>

                <!-- Tables for Reference -->
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px;">
                    <!-- Domain Table -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); overflow: hidden; border-left: 5px solid #8b5cf6;">
                        <div style="padding: 24px; border-bottom: 2px solid #f3f4f6;">
                            <h3 style="font-size: 16px; font-weight: 700; color: #1f2937;">🌐 Domain Details</h3>
                        </div>
                        <div id="topDomainsTableContainer" style="overflow-x: auto;">
                            <div style="padding: 24px; text-align: center; color: #6b7280;">
                                Domain information will appear here
                            </div>
                        </div>
                    </div>

                    <!-- Apps Table -->
                    <div style="background: white; border-radius: 16px; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08); overflow: hidden; border-left: 5px solid #0ea5e9;">
                        <div style="padding: 24px; border-bottom: 2px solid #f3f4f6;">
                            <h3 style="font-size: 16px; font-weight: 700; color: #1f2937;">💻 Application Details</h3>
                        </div>
                        <div id="topApplicationsTableContainer" style="overflow-x: auto;">
                            <div style="padding: 24px; text-align: center; color: #6b7280;">
                                Application information will appear here
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // Generate attendance reports view
    generateAttendanceReportsView() {
        return `
            <div id="attendance-reports-content" class="content-view" style="padding: 32px; display: none;">
                <!-- Header Section -->
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 32px;">
                    <div>
                        <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">📊 Punch In/Out Records</h1>
                        <p style="color: #64748b; font-size: 16px;">Track employee attendance with real-time punch data</p>
                    </div>
                    <div style="display: flex; gap: 12px;">
                        <button onclick="app.refreshAttendanceData()" style="padding: 12px 20px; background: white; border: 1px solid #d1d5db; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px; transition: all 0.2s;">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <polyline points="23 4 23 10 17 10"/>
                                <polyline points="1 20 1 14 7 14"/>
                                <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15"/>
                            </svg>
                            Refresh
                        </button>
                        <button onclick="app.exportAttendanceData()" style="padding: 12px 20px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 500; display: flex; align-items: center; gap: 8px;">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                <polyline points="7 10 12 15 17 10"/>
                                <line x1="12" y1="15" x2="12" y2="3"/>
                            </svg>
                            Export CSV
                        </button>
                    </div>
                </div>

                <!-- Filter Section -->
                <div style="background: white; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; align-items: end;">
                        
                        <!-- Date Range -->
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 Start Date</label>
                            <input 
                                type="date" 
                                id="startDateFilter"
                                style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;"
                                value="${new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0]}"
                            >
                        </div>
                        
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 End Date</label>
                            <input 
                                type="date" 
                                id="endDateFilter"
                                style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;"
                                value="${new Date().toISOString().split('T')[0]}"
                            >
                        </div>
                        
                        <!-- Employee Filter -->
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">👤 Employee</label>
                            <select 
                                id="employeeFilter"
                                style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white;"
                            >
                                <option value="all">All Employees</option>
                            </select>
                        </div>
                        
                        <!-- Filter Actions -->
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.applyAttendanceFilters()" style="padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; white-space: nowrap;">
                                Generate Report
                            </button>
                            <button onclick="app.loadAllRecords()" style="padding: 12px 20px; background: #10b981; color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; white-space: nowrap;">
                                Load All
                            </button>
                            <button onclick="app.clearAttendanceFilters()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">
                                Clear
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Stats Cards -->
                <div id="attendanceStatsCards" style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 24px;">
                    <!-- Will be populated dynamically -->
                </div>

                <!-- Records Table -->
                <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    
                    <!-- Table Header -->
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb;">
                        <h3 style="font-size: 18px; font-weight: 600; color: #1f2937; margin-bottom: 4px;">📋 Punch In/Out Records</h3>
                        <div id="recordsCount" style="color: #6b7280; font-size: 14px;">Loading records...</div>
                    </div>
                    
                    <!-- Loading State -->
                    <div id="loadingState" style="padding: 60px; text-align: center; display: block;">
                        <div style="width: 40px; height: 40px; margin: 0 auto 20px; border: 3px solid #f3f4f6; border-top: 3px solid #667eea; border-radius: 50%; animation: spin 1s linear infinite;"></div>
                        <p style="color: #6b7280;">Loading attendance data...</p>
                    </div>
                    
                    <!-- Records Table -->
                    <div id="recordsTableContainer" style="display: none; overflow-x: auto;">
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f9fafb;">
                                <tr>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Employee</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Display Name</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Punch In</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Punch Out</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Work Hours</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Break Time</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">System</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Status</th>
                                </tr>
                            </thead>
                            <tbody id="recordsTableBody">
                                <!-- Records will be inserted here -->
                            </tbody>
                        </table>
                    </div>
                    
                    <!-- Pagination -->
                    <div id="paginationContainer" style="padding: 20px 24px; border-top: 1px solid #e5e7eb; display: none;">
                        <!-- Pagination will be inserted here -->
                    </div>
                </div>
            </div>
        `;
    },

        // Load attendance records
        loadAttendanceRecords() {
            console.log('Loading attendance records...');
            const recordsContainer = document.getElementById('attendance-records-container');
            if (!recordsContainer) {
                console.error('Records container not found');
                return;
            }
            
            recordsContainer.innerHTML = '<div style="text-align: center; padding: 40px; color: #64748b;"><div style="margin-bottom: 16px;">📊</div>Loading attendance records...</div>';
            
            // Get filter values
            const startDate = document.getElementById('start-date')?.value || '';
            const endDate = document.getElementById('end-date')?.value || '';
            const employeeFilter = document.getElementById('employee-filter')?.value || 'All Employees';
            
            // Prepare request data
            const requestData = {
                company_name: this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD',
                start_date: startDate,
                end_date: endDate,
                employee: employeeFilter
            };
            
            console.log('Request data:', requestData);
            
            // Make API call
            this.api('get_attendance', requestData)
                .then(response => {
                    console.log('API response:', response);
                    if (response.success && response.data) {
                        this.renderAttendanceRecords(response.data);
                    } else {
                        console.error('API error:', response.error);
                        recordsContainer.innerHTML = `
                            <div style="text-align: center; padding: 40px; color: #ef4444;">
                                <div style="margin-bottom: 16px;">⚠️</div>
                                <p>Error loading records: ${response.error || 'Unknown error'}</p>
                                <button onclick="app.loadAttendanceRecords()" style="margin-top: 16px; padding: 8px 16px; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer;">
                                    Retry
                                </button>
                            </div>
                        `;
                    }
                })
                .catch(error => {
                    console.error('Network error:', error);
                    recordsContainer.innerHTML = `
                        <div style="text-align: center; padding: 40px; color: #ef4444;">
                            <div style="margin-bottom: 16px;">🔌</div>
                            <p>Network error: ${error.message}</p>
                            <button onclick="app.loadAttendanceRecords()" style="margin-top: 16px; padding: 8px 16px; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer;">
                                Retry
                            </button>
                        </div>
                    `;
                });
        },

        // Render attendance records in table format
        renderAttendanceRecords(records) {
            const recordsContainer = document.getElementById('attendance-records-container');
            if (!recordsContainer) return;
            
            if (!records || records.length === 0) {
                recordsContainer.innerHTML = `
                    <div style="text-align: center; padding: 40px; color: #64748b;">
                        <div style="margin-bottom: 16px;">📭</div>
                        <p>No attendance records found for the selected criteria.</p>
                    </div>
                `;
                return;
            }
            
            const tableHTML = `
                <div style="background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                    <div style="overflow-x: auto;">
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f8fafc; border-bottom: 1px solid #e2e8f0;">
                                <tr>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">Employee</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">System</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">Check In</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">Check Out</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">Work Duration</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">Break Duration</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">Status</th>
                                    <th style="padding: 16px; text-align: left; font-weight: 600; color: #374151; font-size: 14px;">IP Address</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${records.map(record => {
                                    const statusColor = record.status === 'Complete' ? '#10b981' : '#f59e0b';
                                    const statusBg = record.status === 'Complete' ? '#dcfce7' : '#fef3c7';
                                    
                                    return `
                                        <tr style="border-bottom: 1px solid #f1f5f9;">
                                            <td style="padding: 16px; color: #374151; font-weight: 500;">${record.username}</td>
                                            <td style="padding: 16px; color: #64748b;">${record.system_name || 'N/A'}</td>
                                            <td style="padding: 16px; color: #64748b;">${record.punch_in_formatted || 'N/A'}</td>
                                            <td style="padding: 16px; color: #64748b;">${record.punch_out_formatted || 'Still Working'}</td>
                                            <td style="padding: 16px; color: #64748b;">${record.work_duration}</td>
                                            <td style="padding: 16px; color: #64748b;">${record.break_duration}</td>
                                            <td style="padding: 16px;">
                                                <span style="padding: 4px 12px; border-radius: 16px; font-size: 12px; font-weight: 600; background: ${statusBg}; color: ${statusColor};">
                                                    ${record.status}
                                                </span>
                                            </td>
                                            <td style="padding: 16px; color: #64748b; font-family: monospace;">${record.ip_address || 'N/A'}</td>
                                        </tr>
                                    `;
                                }).join('')}
                            </tbody>
                        </table>
                    </div>
                    <div style="padding: 16px; background: #f8fafc; border-top: 1px solid #e2e8f0; text-align: center; color: #64748b; font-size: 14px;">
                        Showing ${records.length} record${records.length !== 1 ? 's' : ''}
                    </div>
                </div>
            `;
            
            recordsContainer.innerHTML = tableHTML;
        },

        // Load employee list for filter dropdown
        loadEmployeeList() {
            const employeeSelect = document.getElementById('employee-filter');
            if (!employeeSelect) return;
            
            const requestData = {
                company_name: this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD'
            };
            
            this.api('get_employees', requestData)
                .then(response => {
                    if (response.success && response.data) {
                        employeeSelect.innerHTML = '<option value="All Employees">All Employees</option>' +
                            response.data.map(emp => `<option value="${emp}">${emp}</option>`).join('');
                    }
                })
                .catch(error => {
                    console.error('Error loading employees:', error);
                });
        },

        // Export attendance records to CSV
        exportAttendanceCSV() {
            console.log('Exporting attendance records to CSV...');
            this.showToast('CSV export functionality will be implemented soon', 'info');
        },

        // Generate attendance report
        generateAttendanceReport() {
            console.log('Generating attendance report...');
            this.loadAttendanceRecords();
        },

        // Clear attendance filters
        clearAttendanceFilters() {
            document.getElementById('start-date').value = '';
            document.getElementById('end-date').value = '';
            document.getElementById('employee-filter').value = 'All Employees';
            this.loadAttendanceRecords();
        },

    // ===== DAILY WORKING HOURS FUNCTIONS =====
    
    // Load daily working hours for selected employee
    async loadDailyWorkingHours() {
        try {
            const employeeSelect = document.getElementById('dailyWorkingEmployee');
            const dateInput = document.getElementById('dailyWorkingDate');
            
            if (!employeeSelect.value) {
                this.showToast('Please select an employee', 'error');
                return;
            }
            
            if (!dateInput.value) {
                this.showToast('Please select a date', 'error');
                return;
            }
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const selectedDate = dateInput.value;
            const selectedEmployee = employeeSelect.value;
            
            const requestData = {
                company_name: companyName,
                employee_id: selectedEmployee,
                start_date: selectedDate,
                date: selectedDate
            };
            
            console.log('Loading daily working hours:', requestData);
            
            // Show loading state
            const statsContainer = document.getElementById('dailyWorkingStatsCards');
            const recordsContainer = document.getElementById('dailyWorkingRecordsContainer');
            statsContainer.innerHTML = '<div style="text-align: center; grid-column: 1/-1; padding: 40px; color: #64748b;"><div style="margin-bottom: 16px;">⏳</div>Loading...</div>';
            
            // Make API call - use get_daily_work_hours which returns aggregated data
            const response = await this.api('get_daily_work_hours', requestData);
            
            if (response.success && response.data) {
                this.displayDailyWorkingHours(response.data, selectedEmployee, selectedDate);
            } else {
                this.showToast(response.error || 'Failed to load data', 'error');
                statsContainer.innerHTML = '<div style="text-align: center; grid-column: 1/-1; padding: 40px; color: #ef4444;"><div style="margin-bottom: 16px;">⚠️</div>' + (response.error || 'Error loading data') + '</div>';
            }
        } catch (error) {
            console.error('Error loading daily working hours:', error);
            this.showToast('Error loading data: ' + error.message, 'error');
        }
    },
    
    // Display daily working hours with cards
    displayDailyWorkingHours(data, employeeName, date) {
        const statsContainer = document.getElementById('dailyWorkingStatsCards');
        const recordsContainer = document.getElementById('dailyWorkingRecordsContainer');
        const infoContainer = document.getElementById('dailyRecordsInfo');
        
        // Parse date for display
        const dateObj = new Date(date);
        const dateStr = dateObj.toLocaleDateString('en-IN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
        
        // Calculate stats from data
        const totalSessions = data.sessions ? data.sessions.length : 0;
        const totalWorkHours = data.total_work_hours || 0;
        const totalBreakMinutes = data.total_break_minutes || 0;
        const totalBreakHours = (totalBreakMinutes / 60).toFixed(2);
        const firstCheckIn = data.first_punch_in || 'N/A';
        const lastCheckOut = data.last_punch_out || 'N/A';
        const productivity = ((totalWorkHours / 8) * 100).toFixed(1);
        
        // Create stats cards
        const statsHTML = `
            <!-- Total Working Hours Card -->
            <div style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #667eea;">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <circle cx="12" cy="12" r="10"/>
                            <polyline points="12 6 12 12 16 14"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Total Working Hours</div>
                </div>
                <div style="font-size: 32px; font-weight: 700; color: #1f2937; margin-bottom: 8px;">${totalWorkHours.toFixed(2)}h</div>
                <div style="font-size: 12px; color: #6b7280;">Cumulative work time today</div>
            </div>

            <!-- First Check In Card -->
            <div style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #10b981;">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <line x1="12" y1="5" x2="12" y2="19"/>
                            <polyline points="19 12 12 19 5 12"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">First Check In</div>
                </div>
                <div style="font-size: 28px; font-weight: 700; color: #1f2937; margin-bottom: 8px;">${firstCheckIn === 'N/A' ? 'N/A' : firstCheckIn}</div>
                <div style="font-size: 12px; color: #6b7280;">${firstCheckIn === 'N/A' ? 'No check in yet' : 'First entry time'}</div>
            </div>

            <!-- Last Check Out Card -->
            <div style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #f59e0b;">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <line x1="12" y1="5" x2="12" y2="19"/>
                            <polyline points="5 12 12 5 19 12"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Last Check Out</div>
                </div>
                <div style="font-size: 28px; font-weight: 700; color: #1f2937; margin-bottom: 8px;">${lastCheckOut === 'N/A' ? 'N/A' : lastCheckOut}</div>
                <div style="font-size: 12px; color: #6b7280;">${lastCheckOut === 'N/A' ? 'Still working' : 'Last exit time'}</div>
            </div>

            <!-- Total Break Time Card -->
            <div style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #8b5cf6;">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <rect x="3" y="4" width="18" height="16" rx="2" ry="2"/>
                            <line x1="9" y1="9" x2="15" y2="9"/>
                            <line x1="9" y1="15" x2="15" y2="15"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Total Break Time</div>
                </div>
                <div style="font-size: 32px; font-weight: 700; color: #1f2937; margin-bottom: 8px;">${totalBreakHours}h</div>
                <div style="font-size: 12px; color: #6b7280;">${totalBreakMinutes} minutes break</div>
            </div>

            <!-- Sessions Count Card -->
            <div style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #06b6d4;">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3.5-9c.83 0 1.5-.67 1.5-1.5S16.33 8 15.5 8 14 8.67 14 9.5s.67 1.5 1.5 1.5zm-7 0c.83 0 1.5-.67 1.5-1.5S9.33 8 8.5 8 7 8.67 7 9.5 7.67 11 8.5 11zm3.5 6.5c2.33 0 4.31-1.46 5.11-3.5H6.89c.8 2.04 2.78 3.5 5.11 3.5z"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Work Sessions</div>
                </div>
                <div style="font-size: 32px; font-weight: 700; color: #1f2937; margin-bottom: 8px;">${totalSessions}</div>
                <div style="font-size: 12px; color: #6b7280;">Check in/out sessions</div>
            </div>

            <!-- Productivity Card -->
            <div style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #ec4899;">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #ec4899 0%, #be185d 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"/>
                            <polyline points="13 2 13 9 20 9"/>
                            <circle cx="12" cy="15" r="1"/>
                            <circle cx="16" cy="15" r="1"/>
                            <circle cx="8" cy="15" r="1"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Productivity</div>
                </div>
                <div style="font-size: 32px; font-weight: 700; color: #1f2937; margin-bottom: 8px;">${productivity}%</div>
                <div style="font-size: 12px; color: #6b7280;">Of 8-hour target</div>
                <div style="width: 100%; background: #e5e7eb; border-radius: 8px; height: 6px; margin-top: 8px;">
                    <div style="height: 100%; background: linear-gradient(90deg, #ec4899 0%, #ec4899 100%); border-radius: 8px; width: ${Math.min(productivity, 100)}%;"></div>
                </div>
            </div>
        `;
        
        statsContainer.innerHTML = statsHTML;
        
        // Update info header
        infoContainer.textContent = `📅 ${dateStr} | 👤 ${employeeName}`;
        
        // Display work sessions
        if (data.sessions && data.sessions.length > 0) {
            let sessionsHTML = '<div style="overflow-x: auto;"><table style="width: 100%; border-collapse: collapse;">';
            sessionsHTML += `
                <thead style="background: #f9fafb;">
                    <tr>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Session #</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Check In</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Break Duration</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Check Out</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Work Duration</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">System</th>
                    </tr>
                </thead>
                <tbody>
            `;
            
            data.sessions.forEach((session, index) => {
                // Use formatted times from server which are already in IST
                const checkInTime = session.punch_in_formatted || 'N/A';
                const checkOutTime = session.punch_out_formatted || 'Still working';
                const breakMinutes = session.break_minutes || 0;
                const workDuration = session.work_hours ? session.work_hours.toFixed(2) + 'h' : '0h';
                
                sessionsHTML += `
                    <tr style="border-bottom: 1px solid #f3f4f6;">
                        <td style="padding: 12px 16px; color: #374151; font-weight: 500;">#${index + 1}</td>
                        <td style="padding: 12px 16px; color: #374151;">${checkInTime}</td>
                        <td style="padding: 12px 16px; color: #374151;">${breakMinutes} min</td>
                        <td style="padding: 12px 16px; color: #374151;">${checkOutTime}</td>
                        <td style="padding: 12px 16px; color: #374151; font-weight: 500;">${workDuration}</td>
                        <td style="padding: 12px 16px; color: #374151; font-size: 14px;">${session.system_name || 'Unknown'}</td>
                    </tr>
                `;
            });
            
            sessionsHTML += '</tbody></table></div>';
            recordsContainer.innerHTML = sessionsHTML;
        } else {
            recordsContainer.innerHTML = `
                <div style="text-align: center; padding: 40px; color: #6b7280;">
                    <div style="margin-bottom: 16px;">📭</div>
                    <p>No work sessions found for this date</p>
                </div>
            `;
        }
    },
    
    // Refresh daily hours data
    async refreshDailyHours() {
        const employeeSelect = document.getElementById('dailyWorkingEmployee');
        if (employeeSelect.value) {
            await this.loadDailyWorkingHours();
            this.showToast('Data refreshed', 'success');
        } else {
            this.showToast('Please select an employee first', 'error');
        }
    },
    
    // Clear daily hours filters
    clearDailyHoursFilters() {
        document.getElementById('dailyWorkingDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('dailyWorkingEmployee').value = '';
        document.getElementById('dailyWorkingStatsCards').innerHTML = '';
        document.getElementById('dailyWorkingRecordsContainer').innerHTML = '<div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>Select an employee and date to view working sessions</p></div>';
        document.getElementById('dailyRecordsInfo').textContent = 'Select an employee and date to view sessions';
    },

    // ===== WEB BROWSING LOGS VIEW =====
    generateWebBrowsingLogsView() {
        return `
            <div id="web-browsing-logs-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">🌐 Web Browsing Logs</h1>
                    <p style="color: #64748b; font-size: 16px;">Track website visits and browsing activity</p>
                </div>

                <div style="background: white; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; align-items: end;">
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 Start Date</label>
                            <input type="date" id="webLogsStartDate" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;" value="${new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 End Date</label>
                            <input type="date" id="webLogsEndDate" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;" value="${new Date().toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">👤 Employee</label>
                            <select id="webLogsEmployee" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white;">
                                <option value="">All Employees</option>
                            </select>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.loadWebLogs()" style="padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">Load Logs</button>
                            <button onclick="app.clearWebLogsFilter()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: none; border-radius: 8px; cursor: pointer;">Reset</button>
                        </div>
                    </div>
                </div>

                <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb;">
                        <h3 style="font-size: 18px; font-weight: 600; color: #1f2937; margin-bottom: 4px;">📋 Browsing History</h3>
                        <div id="webLogsInfo" style="color: #6b7280; font-size: 14px;">Click "Load Logs" to view browsing history</div>
                    </div>
                    <div id="webLogsContainer" style="padding: 24px; min-height: 300px;">
                        <div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No data loaded</p></div>
                    </div>
                </div>
            </div>
        `;
    },

    // ===== APPLICATION USAGE VIEW =====
    generateApplicationUsageView() {
        return `
            <div id="application-usage-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">💻 Application Usage</h1>
                    <p style="color: #64748b; font-size: 16px;">Track active applications and window usage</p>
                </div>

                <div style="background: white; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; align-items: end;">
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 Start Date</label>
                            <input type="date" id="appUsageStartDate" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;" value="${new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 End Date</label>
                            <input type="date" id="appUsageEndDate" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;" value="${new Date().toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">👤 Employee</label>
                            <select id="appUsageEmployee" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white;">
                                <option value="">All Employees</option>
                            </select>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.loadApplicationLogs()" style="padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">Load Logs</button>
                            <button onclick="app.clearAppUsageFilter()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: none; border-radius: 8px; cursor: pointer;">Reset</button>
                        </div>
                    </div>
                </div>

                <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb;">
                        <h3 style="font-size: 18px; font-weight: 600; color: #1f2937; margin-bottom: 4px;">📋 Application Usage History</h3>
                        <div id="appUsageInfo" style="color: #6b7280; font-size: 14px;">Click "Load Logs" to view application usage</div>
                    </div>
                    <div id="appUsageContainer" style="padding: 24px; min-height: 300px;">
                        <div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No data loaded</p></div>
                    </div>
                </div>
            </div>
        `;
    },

    // ===== INACTIVITY LOGS VIEW =====
    generateInactivityLogsView() {
        return `
            <div id="inactivity-logs-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 700; color: #0f172a; margin-bottom: 8px;">⏸️ Inactivity Management</h1>
                    <p style="color: #64748b; font-size: 16px;">Monitor and analyze employee inactivity periods (>10 min = inactive)</p>
                </div>

                <!-- Filters Section -->
                <div style="background: white; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border: 1px solid #e5e7eb;">
                    <h3 style="font-size: 16px; font-weight: 600; color: #0f172a; margin-bottom: 16px;">Filter Options</h3>
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; align-items: end;">
                        <div>
                            <label style="display: block; margin-bottom: 10px; font-weight: 600; color: #0f172a; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;">📅 Start Date</label>
                            <input type="date" id="inactivityStartDate" style="width: 100%; padding: 12px 14px; border: 1.5px solid #e2e8f0; border-radius: 10px; font-size: 14px; transition: all 0.3s ease; background: #f9fafb;" value="${new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 10px; font-weight: 600; color: #0f172a; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;">📅 End Date</label>
                            <input type="date" id="inactivityEndDate" style="width: 100%; padding: 12px 14px; border: 1.5px solid #e2e8f0; border-radius: 10px; font-size: 14px; transition: all 0.3s ease; background: #f9fafb;" value="${new Date().toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 10px; font-weight: 600; color: #0f172a; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;">👤 Employee</label>
                            <select id="inactivityEmployee" style="width: 100%; padding: 12px 14px; border: 1.5px solid #e2e8f0; border-radius: 10px; font-size: 14px; transition: all 0.3s ease; background: #f9fafb;">
                                <option value="">All Employees</option>
                            </select>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.loadInactivityLogs()" style="padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);">📊 Load Logs</button>
                            <button onclick="app.clearInactivityFilter()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: none; border-radius: 10px; cursor: pointer; font-weight: 500;">Reset</button>
                        </div>
                    </div>
                </div>

                <!-- Inactivity Summary Cards -->
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px; margin-bottom: 28px;">
                    <!-- Total Inactivity Instances -->
                    <div style="background: white; border-radius: 14px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #f59e0b;">
                        <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Total Inactivity</p>
                        <h3 id="inactTotalCount" style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">--</h3>
                        <p style="color: #94a3b8; font-size: 12px;">instances detected</p>
                    </div>

                    <!-- Active Employees (< 10 min) -->
                    <div style="background: white; border-radius: 14px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #10b981;">
                        <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Short Pause</p>
                        <h3 id="inactActiveCount" style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">--</h3>
                        <p style="color: #94a3b8; font-size: 12px;">Less than 10 minutes</p>
                    </div>

                    <!-- Inactive Employees (>= 10 min) -->
                    <div style="background: white; border-radius: 14px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #ef4444;">
                        <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Significant Idle</p>
                        <h3 id="inactInactiveCount" style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">--</h3>
                        <p style="color: #94a3b8; font-size: 12px;">10 minutes or more</p>
                    </div>

                    <!-- Average Duration -->
                    <div style="background: white; border-radius: 14px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #0ea5e9;">
                        <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Avg Duration</p>
                        <h3 id="inactAvgDuration" style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">--</h3>
                        <p style="color: #94a3b8; font-size: 12px;">minutes per instance</p>
                    </div>
                </div>

                <!-- Data Table Section -->
                <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb;">
                        <h3 style="font-size: 18px; font-weight: 700; color: #0f172a; margin-bottom: 4px;">📋 Detailed Inactivity Log</h3>
                        <div id="inactivityInfo" style="color: #6b7280; font-size: 14px; display: flex; align-items: center; gap: 8px;">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                            Click "Load Logs" to view inactivity records
                        </div>
                    </div>
                    <div id="inactivityContainer" style="padding: 24px; min-height: 400px;">
                        <div style="text-align: center; color: #6b7280; padding: 60px 20px;">
                            <div style="margin-bottom: 16px; font-size: 48px;">📭</div>
                            <p style="font-size: 16px;">No data loaded yet</p>
                            <p style="font-size: 14px; margin-top: 8px;">Load logs to see detailed inactivity analysis</p>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // ===== SCREENSHOTS VIEW =====
    generateScreenshotsView() {
        return `
            <div id="screenshots-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">📸 Screenshots</h1>
                    <p style="color: #64748b; font-size: 16px;">Access screenshot history and metadata</p>
                </div>

                <div style="background: white; padding: 24px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; align-items: end;">
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 Start Date</label>
                            <input type="date" id="screenshotsStartDate" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;" value="${new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">📅 End Date</label>
                            <input type="date" id="screenshotsEndDate" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px;" value="${new Date().toISOString().split('T')[0]}">
                        </div>
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151; font-size: 14px;">👤 Employee</label>
                            <select id="screenshotsEmployee" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white;">
                                <option value="">All Employees</option>
                            </select>
                        </div>
                        <div style="display: flex; gap: 12px;">
                            <button onclick="app.loadScreenshots()" style="padding: 12px 24px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">Load Logs</button>
                            <button onclick="app.clearScreenshotsFilter()" style="padding: 12px 16px; background: #f3f4f6; color: #6b7280; border: none; border-radius: 8px; cursor: pointer;">Reset</button>
                        </div>
                    </div>
                </div>

                <div style="background: white; border-radius: 16px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb;">
                        <h3 style="font-size: 18px; font-weight: 600; color: #1f2937; margin-bottom: 4px;">📋 Screenshot History</h3>
                        <div id="screenshotsInfo" style="color: #6b7280; font-size: 14px;">Click "Load Logs" to view screenshot history</div>
                    </div>
                    <div id="screenshotsContainer" style="padding: 24px; min-height: 300px;">
                        <div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No data loaded</p></div>
                    </div>
                </div>
            </div>
        `;
        
        // Auto-navigate to dashboard on page load
        this.navigate('dashboard');
    },

    // Navigation function
    navigate(viewName) {
        // Hide all content views
        const allViews = document.querySelectorAll('.content-view');
        allViews.forEach(view => view.style.display = 'none');
        
        // Show selected view
        const targetView = document.getElementById(`${viewName}-content`);
        if (targetView) {
            // For site-monitoring, use flex display instead of block
            if (viewName === 'site-monitoring') {
                targetView.style.display = 'flex';
            } else {
                targetView.style.display = 'block';
            }
        }
        
        // Update menu active state
        const allMenuItems = document.querySelectorAll('.menu-item');
        allMenuItems.forEach(item => {
            item.style.background = 'transparent';
            item.style.color = '#94a3b8';
        });
        
        const activeMenuItem = document.querySelector(`[data-view="${viewName}"]`);
        if (activeMenuItem) {
            activeMenuItem.style.background = '#334155';
            activeMenuItem.style.color = '#e2e8f0';
        }
        
        console.log(`Navigated to: ${viewName}`);
        
        // Load specific data for pages
        if (viewName === 'dashboard') {
            this.initializeDashboard();
            // Auto-refresh domains after a short delay to ensure DOM is ready
            setTimeout(() => this.refreshDomainUsage(), 1500);
        } else if (viewName === 'attendance-reports') {
            this.initializeAttendanceReports();
            // Load employee dropdown
            this.loadEmployeeDropdown();
        } else if (viewName === 'daily-working-hours') {
            this.initializeDailyWorkingHours();
        } else if (viewName === 'engineer-report') {
            this.initializeEngineerReport();
        } else if (viewName === 'web-browsing-logs') {
            this.initializeWebLogs();
        } else if (viewName === 'application-usage') {
            this.initializeApplicationLogs();
        } else if (viewName === 'inactivity-logs') {
            this.initializeInactivityLogs();
        } else if (viewName === 'screenshots') {
            this.initializeScreenshots();
        } else if (viewName === 'analytics') {
            this.initializeAnalytics();
        } else if (viewName === 'employee-management') {
            this.initializeEmployeeManagement();
        } else if (viewName === 'site-monitoring') {
            this.initializeSiteMonitoring();
        }
    },

    // Initialize dashboard with statistics
    async initializeDashboard() {
        try {
            console.log('[Dashboard] Initializing...');
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            console.log('[Dashboard] Company:', companyName);
            
            const today = new Date().toISOString().split('T')[0];
            const fiveDaysAgo = new Date(Date.now() - 5*24*60*60*1000).toISOString().split('T')[0];
            
            // Create timeout wrapper
            const withTimeout = (promise, ms) => {
                return Promise.race([
                    promise,
                    new Promise((_, reject) => setTimeout(() => reject(new Error('Timeout')), ms))
                ]);
            };
            
            // Fetch all data in parallel with 10 second timeout
            const [empResponse, webResponse, inactResponse, webLogsResponse] = await Promise.allSettled([
                withTimeout(this.api('get_punch_employees', { company_name: companyName }), 10000),
                withTimeout(this.api('get_web_logs', { company_name: companyName, start_date: today, end_date: today }), 10000),
                withTimeout(this.api('get_inactivity_logs', { company_name: companyName, start_date: today, end_date: today }), 10000),
                withTimeout(this.api('get_web_logs', { company_name: companyName, start_date: fiveDaysAgo, end_date: today }), 10000)
            ]);
            
            // Process employee count
            const totalEmployees = empResponse.status === 'fulfilled' && empResponse.value?.success 
                ? (empResponse.value.data?.length || 0) 
                : 0;
            console.log('[Dashboard] Total employees:', totalEmployees);
            
            // Process web activity
            const webActivity = webResponse.status === 'fulfilled' && webResponse.value?.success 
                ? (Array.isArray(webResponse.value.data) ? webResponse.value.data.length : 0) 
                : 0;
            console.log('[Dashboard] Web activity:', webActivity);
            
            // Process inactivity
            const inactCount = inactResponse.status === 'fulfilled' && inactResponse.value?.success 
                ? (Array.isArray(inactResponse.value.data) ? inactResponse.value.data.length : 0) 
                : 0;
            console.log('[Dashboard] Inactivity count:', inactCount);
            
            // Update dashboard cards immediately
            const dashTotalEmps = document.getElementById('dashTotalEmployees');
            const dashActive = document.getElementById('dashActiveNow');
            const dashWeb = document.getElementById('dashWebActivity');
            const dashInact = document.getElementById('dashInactivity');
            
            console.log('[Metrics] Updating cards - Total Emp:', totalEmployees, 'Web:', webActivity, 'Inact:', inactCount);
            
            if (dashTotalEmps) {
                dashTotalEmps.textContent = totalEmployees;
                console.log('[Metrics] Updated Total Employees');
            }
            if (dashActive) {
                dashActive.textContent = Math.max(0, totalEmployees - 2);
                console.log('[Metrics] Updated Active Now');
            }
            if (dashWeb) {
                dashWeb.textContent = webActivity;
                console.log('[Metrics] Updated Web Activity');
            }
            if (dashInact) {
                dashInact.textContent = inactCount;
                console.log('[Metrics] Updated Inactivity');
            }
            
            // Process domain logs (non-blocking)
            if (webLogsResponse.status === 'fulfilled' && webLogsResponse.value?.success && Array.isArray(webLogsResponse.value.data)) {
                this.dashboardWebLogs = webLogsResponse.value.data;
                console.log('[Dashboard] Cached', this.dashboardWebLogs.length, 'web logs');
                this.processDomainUsage();
            } else {
                console.error('[Dashboard] Failed to fetch web logs, auto-refreshing...');
                // Auto-refresh domains on first load
                setTimeout(() => this.refreshDomainUsage(), 500);
            }
            
        } catch (error) {
            console.error('[Dashboard] Error initializing dashboard:', error);
        }
    },

    // Process domain usage from cached logs (non-blocking)
    processDomainUsage() {
        if (!this.dashboardWebLogs || this.dashboardWebLogs.length === 0) {
            const container = document.getElementById('topDomainsContainer');
            if (container) {
                container.innerHTML = `
                    <div style="text-align: center; padding: 40px 20px; color: #94a3b8;">
                        <div style="font-size: 14px;">No domain data available</div>
                    </div>
                `;
            }
            return;
        }

        const logs = this.dashboardWebLogs;
        
        // Process domain data efficiently
        const domainMap = {};
        for (const log of logs) {
            if (log.website_url) {
                const domain = this.extractDomain(log.website_url);
                domainMap[domain] = (domainMap[domain] || 0) + 1;
            }
        }
        
        // Sort domains by usage count
        const sortedDomains = Object.entries(domainMap)
            .sort((a, b) => b[1] - a[1])
            .slice(0, 10);
        
        // Update statistics immediately
        const statsEl = document.getElementById('totalDomainsCount');
        const sessionsEl = document.getElementById('totalSessionsCount');
        const topDomainEl = document.getElementById('topDomainName');
        const topUsageEl = document.getElementById('topDomainUsage');
        
        if (statsEl) statsEl.textContent = Object.keys(domainMap).length;
        if (sessionsEl) sessionsEl.textContent = logs.length;
        if (sortedDomains.length > 0) {
            if (topDomainEl) topDomainEl.textContent = sortedDomains[0][0];
            if (topUsageEl) topUsageEl.textContent = `${sortedDomains[0][1]} sessions`;
        }
        
        // Render domains (happens instantly from cached data)
        this.renderTopDomains(sortedDomains);
    },

    // Refresh domain usage data
    async refreshDomainUsage() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const today = new Date().toISOString().split('T')[0];
            const fiveDaysAgo = new Date(Date.now() - 5*24*60*60*1000).toISOString().split('T')[0];
            
            // Show loading state
            const container = document.getElementById('topDomainsContainer');
            if (container) {
                container.innerHTML = `
                    <div style="text-align: center; padding: 40px 20px; color: #94a3b8;">
                        <div style="font-size: 14px; margin-bottom: 12px;">🔄 Refreshing domains (last 5 days)...</div>
                        <div style="width: 30px; height: 30px; border: 3px solid #e2e8f0; border-top: 3px solid #0ea5e9; border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto;"></div>
                    </div>
                `;
            }
            
            // Fetch web logs with very short timeout
            const response = await Promise.race([
                this.api('get_web_logs', {
                    company_name: companyName,
                    start_date: fiveDaysAgo,
                    end_date: today
                }),
                new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Request timeout')), 5000)
                )
            ]);
            
            if (response.success && Array.isArray(response.data)) {
                this.dashboardWebLogs = response.data;
                this.processDomainUsage();
            } else {
                throw new Error('Invalid response');
            }
        } catch (error) {
            console.error('Error refreshing domain usage:', error);
            const container = document.getElementById('topDomainsContainer');
            if (container) {
                container.innerHTML = `
                    <div style="text-align: center; padding: 40px 20px; color: #ef4444;">
                        <div style="font-size: 14px; margin-bottom: 12px;">⚠️ Failed to load domains</div>
                        <button onclick="dashboard.refreshDomainUsage()" style="padding: 8px 16px; background: #ef4444; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 12px; font-weight: 600; transition: all 0.3s ease;" onmouseover="this.style.background='#dc2626'" onmouseout="this.style.background='#ef4444'">Retry</button>
                    </div>
                `;
            }
        }
    },

    // Render top domains list
    renderTopDomains(domains) {
        const container = document.getElementById('topDomainsContainer');
        if (!container) return;
        
        if (domains.length === 0) {
            container.innerHTML = `
                <div style="text-align: center; padding: 40px 20px; color: #94a3b8;">
                    <div style="font-size: 14px;">No domains found</div>
                </div>
            `;
            return;
        }
        
        const colors = [
            '#0ea5e9', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6',
            '#ec4899', '#14b8a6', '#f97316', '#06b6d4', '#a855f7'
        ];
        
        let html = '';
        const maxCount = domains[0][1];
        
        domains.forEach((item, index) => {
            const [domain, count] = item;
            const color = colors[index % colors.length];
            const percentage = (count / maxCount) * 100;
            
            html += `
                <div style="margin-bottom: 16px; padding-bottom: 16px; border-bottom: 1px solid #f0f4f8;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                        <div style="flex: 1; min-width: 0;">
                            <p style="color: #0f172a; font-weight: 600; font-size: 14px; margin: 0; word-break: break-all; overflow-wrap: break-word;">${domain}</p>
                            <p style="color: #94a3b8; font-size: 12px; margin: 4px 0 0 0;">${count} sessions</p>
                        </div>
                        <span style="background: ${color}; color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; min-width: 45px; text-align: center; margin-left: 12px; flex-shrink: 0;">${count}</span>
                    </div>
                    <div style="height: 6px; background: #f0f4f8; border-radius: 3px; overflow: hidden;">
                        <div style="height: 100%; background: linear-gradient(90deg, ${color}, ${this.lightenColor(color, 30)}); width: ${percentage}%; transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1);"></div>
                    </div>
                </div>
            `;
        });
        
        container.innerHTML = html;
    },

    // Lighten color helper
    lightenColor(color, percent) {
        const num = parseInt(color.replace("#", ""), 16);
        const amt = Math.round(2.55 * percent);
        const R = Math.min(255, (num >> 16) + amt);
        const G = Math.min(255, (num >> 8 & 0x00FF) + amt);
        const B = Math.min(255, (num & 0x0000FF) + amt);
        return "#" + (0x1000000 + R * 0x10000 + G * 0x100 + B).toString(16).slice(1);
    },

    // Initialize analytics page
    async initializeAnalytics() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_punch_employees', {
                company_name: companyName
            });
            
            if (response.success && response.data) {
                const select = document.getElementById('analyticsEmployee');
                if (select) {
                    select.innerHTML = '<option value="">All Employees</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id;
                        option.textContent = emp.display_name || emp.employee_id;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error initializing analytics:', error);
        }
    },

    // Initialize attendance reports page
    async initializeAttendanceReports() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_punch_employees', { company_name: companyName });
            if (response.success && response.data) {
                const select = document.getElementById('attendanceReportsEmployee');
                if (select) {
                    select.innerHTML = '<option value="">All Employees</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id || emp.username;
                        option.textContent = `${emp.display_name || emp.full_name || emp.employee_id}`;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error initializing attendance reports:', error);
        }
    },

    // Initialize employee management page
    async initializeEmployeeManagement() {
        // Page is ready, no initial loading required
        // Users can click to load data
    },
    async initializeWebLogs() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            if (response.success) {
                const select = document.getElementById('webLogsEmployee');
                if (select) {
                    select.innerHTML = '<option value="">All Employees</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id;
                        option.textContent = emp.display_name || emp.full_name || emp.employee_id;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error initializing web logs:', error);
        }
    },

    // Initialize application logs
    async initializeApplicationLogs() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            if (response.success) {
                const select = document.getElementById('appUsageEmployee');
                if (select) {
                    select.innerHTML = '<option value="">All Employees</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id;
                        option.textContent = emp.display_name || emp.full_name || emp.employee_id;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error initializing application logs:', error);
        }
    },

    // Initialize inactivity logs
    async initializeInactivityLogs() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            if (response.success) {
                const select = document.getElementById('inactivityEmployee');
                if (select) {
                    select.innerHTML = '<option value="">All Employees</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id;  // Use employee_id instead of username
                        option.textContent = emp.display_name || emp.full_name || emp.employee_id;  // Show display name
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error initializing inactivity logs:', error);
        }
    },

    // Initialize screenshots
    async initializeScreenshots() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            if (response.success) {
                const select = document.getElementById('screenshotsEmployee');
                if (select) {
                    select.innerHTML = '<option value="">All Employees</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id;
                        option.textContent = emp.display_name || emp.full_name || emp.employee_id;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error initializing screenshots:', error);
        }
    },

    // Initialize daily working hours page
    async initializeDailyWorkingHours() {
        try {
            // Load employees list for filter using get_employees (same as other pages)
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_punch_employees', { company_name: companyName });
            
            if (response.success) {
                const select = document.getElementById('dailyWorkingEmployee');
                if (select) {
                    select.innerHTML = '<option value="">Select an engineer...</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id;
                        option.textContent = emp.display_name || emp.employee_id;
                        select.appendChild(option);
                    });
                    console.log(`Loaded ${response.data.length} employees for daily hours filter`);
                }
            }
        } catch (error) {
            console.error('Error initializing daily working hours:', error);
            this.showToast('Error loading daily working hours page', 'error');
        }
    },

    // Initialize engineer report page
    async initializeEngineerReport() {
        try {
            console.log('Initializing engineer report...');
            
            // Load engineers list
            await this.loadEngineersList();
            
            this.showToast('Engineer report page ready', 'info');
        } catch (error) {
            console.error('Error initializing engineer report:', error);
            this.showToast('Error loading engineer report page', 'error');
        }
    },

    // Load engineers list for dropdown
    async loadEngineersList() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            
            if (response.success && response.data) {
                const select = document.getElementById('reportEngineerSelect');
                if (select) {
                    select.innerHTML = '<option value="">Select an engineer...</option>';
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id || emp.username;
                        option.textContent = emp.display_name || emp.full_name || emp.employee_id;
                        select.appendChild(option);
                    });
                    console.log(`Loaded ${response.data.length} engineers for report`);
                }
            }
        } catch (error) {
            console.error('Error loading engineers list:', error);
        }
    },

    // Handle engineer selection
    onEngineerSelected() {
        const engineerId = document.getElementById('reportEngineerSelect')?.value;
        if (!engineerId) {
            this.showToast('Please select an engineer', 'warning');
        }
    },

    // Generate engineer activity report
    async generateEngineerActivityReport() {
        try {
            const engineerId = document.getElementById('reportEngineerSelect')?.value;
            const reportDate = document.getElementById('reportDatePicker')?.value;
            
            if (!engineerId) {
                this.showToast('Please select an engineer', 'warning');
                return;
            }
            
            if (!reportDate) {
                this.showToast('Please select a date', 'warning');
                return;
            }
            
            this.showToast('Generating report...', 'info');
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            // Fetch all required data in parallel
            const [punchData, idleData, browserData, appData] = await Promise.all([
                this.api('get_attendance', { 
                    company_name: companyName, 
                    employee_id: engineerId, 
                    start_date: reportDate,
                    end_date: reportDate
                }),
                this.api('get_inactivity_logs', { 
                    company_name: companyName, 
                    employee_id: engineerId, 
                    start_date: reportDate,
                    end_date: reportDate
                }),
                this.api('get_web_logs', { 
                    company_name: companyName, 
                    employee_id: engineerId, 
                    start_date: reportDate,
                    end_date: reportDate
                }),
                this.api('get_application_logs', { 
                    company_name: companyName, 
                    employee_id: engineerId, 
                    start_date: reportDate,
                    end_date: reportDate
                })
            ]);
            
            // Process and display report
            await this.displayEngineerReport(punchData, idleData, browserData, appData, engineerId, reportDate);
            
            this.showToast('Report generated successfully', 'success');
        } catch (error) {
            console.error('Error generating engineer report:', error);
            this.showToast('Error generating report', 'error');
        }
    },

    // Display engineer report with all details
    async displayEngineerReport(punchData, idleData, browserData, appData, engineerId, reportDate) {
        try {
            // Get employee details to fetch lunch duration and idle threshold
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            console.log('Fetching employee settings for:', engineerId);
            const empResponse = await this.api('get_employees', { 
                company_name: companyName,
                employee_id: engineerId
            });
            
            console.log('Employee response:', empResponse);
            
            let lunchDurationMinutes = 60; // Default 60 minutes
            let idleThresholdMinutes = 10; // Default 10 minutes
            if (empResponse.success && empResponse.data && empResponse.data.length > 0) {
                lunchDurationMinutes = empResponse.data[0].lunch_duration || 60;
                idleThresholdMinutes = empResponse.data[0].significant_idle_threshold_minutes || 10;
                console.log('✓ Employee settings fetched for', engineerId, ':', {lunchDuration: lunchDurationMinutes, idleThreshold: idleThresholdMinutes});
            } else {
                console.log('✗ No employee found for', engineerId, '- Using defaults: lunch 60 min, idle threshold 10 min');
            }
            
            // Calculate metrics with selected report date, employee's lunch duration, and idle threshold
            const metrics = this.calculateReportMetrics(punchData, idleData, browserData, appData, lunchDurationMinutes, reportDate, idleThresholdMinutes);
            
            // Store lunch duration in metrics for display
            metrics.lunchDurationMinutes = lunchDurationMinutes;
            metrics.idleThresholdMinutes = idleThresholdMinutes;
            
            // Display stats cards
            this.displayEngineerReportStats(metrics);
            
            // Display tables
            this.displayPunchInOutTable(punchData, metrics);
            this.displayIdleTimeTable(idleData, metrics);
            this.displayTopDomainsTable(browserData, metrics);
            this.displayTopApplicationsTable(appData, metrics);
            
            // Display charts
            this.renderEngineerReportCharts(metrics);
        } catch (error) {
            console.error('Error displaying engineer report:', error);
        }
    },

    // Calculate report metrics
    // Helper: Extract time only from database timestamp (HH:MM format) - IST Format
    extractTimeOnly(timestamp) {
        try {
            // Parse the timestamp and extract time in IST (no conversion needed, DB already stores IST)
            // Just extract HH:MM from the timestamp string
            if (typeof timestamp === 'string') {
                // If timestamp is a string like "2026-01-21T14:52:00" or "2026-01-21 14:52:00"
                const parts = timestamp.split(/[T\s]/);
                if (parts.length >= 2) {
                    const timePart = parts[1].split(':');
                    if (timePart.length >= 2) {
                        return `${timePart[0]}:${timePart[1]}`;
                    }
                }
            }
            
            // Fallback: parse as date and extract time
            const date = new Date(timestamp);
            const hours = String(date.getHours()).padStart(2, '0');
            const minutes = String(date.getMinutes()).padStart(2, '0');
            
            return `${hours}:${minutes}`;
        } catch (e) {
            return typeof timestamp === 'string' ? timestamp.substring(11, 16) : timestamp;
        }
    },

    calculateReportMetrics(punchData, idleData, browserData, appData, lunchDurationMinutes = 60, selectedDate = null, idleThresholdMinutes = 10) {
        const metrics = {
            totalWorkHours: 0,
            significantIdleMinutes: 0,
            shortPauseMinutes: 0,
            lunchDurationMinutes: lunchDurationMinutes,
            actualWorkingHours: 0,
            finalWorkingHours: 0,
            punchInTime: '--',
            punchOutTime: '--',
            topDomain: '--',
            topApplication: '--',
            domainsList: [],
            applicationsList: []
        };
        
        try {
            console.log('Calculating metrics from:', { punchData, idleData, browserData, appData, lunchDurationMinutes });
            
            // Get the selected date in the report (from selectedDate, or use today)
            const reportDateStr = selectedDate || new Date().toISOString().split('T')[0];
            const reportDate = new Date(reportDateStr);
            reportDate.setHours(0, 0, 0, 0); // Start of selected date
            const nextDay = new Date(reportDate);
            nextDay.setDate(nextDay.getDate() + 1); // Start of next date
            
            console.log('Report date range:', reportDate, 'to', nextDay);
            
            // Process punch data - handle multiple sessions, split at midnight
            let punchSessions = [];
            let totalSeconds = 0;
            let totalBreakSeconds = 0;
            let sessionCount = 0;
            
            if (punchData?.success && punchData.data) {
                const records = Array.isArray(punchData.data) ? punchData.data : [];
                if (records.length > 0) {
                    // Sort by punch_in_time
                    const sorted = records.sort((a, b) => {
                        return new Date(a.punch_in_time) - new Date(b.punch_in_time);
                    });
                    
                    const firstRecord = sorted[0];
                    const lastRecord = sorted[sorted.length - 1];
                    
                    // Extract actual time only (HH:MM format, no timezone conversion)
                    metrics.punchInTime = firstRecord.punch_in_time ? this.extractTimeOnly(firstRecord.punch_in_time) : '--';
                    metrics.punchOutTime = lastRecord.punch_out_time ? this.extractTimeOnly(lastRecord.punch_out_time) : 'Still Working';
                    
                    // Store number of punch sessions (total in the data)
                    metrics.totalSessions = sorted.length;
                    
                    // Process each record and split if crossing midnight
                    sorted.forEach((record, idx) => {
                        const punchIn = new Date(record.punch_in_time);
                        const punchOut = new Date(record.punch_out_time || new Date());
                        const totalWorkSeconds = record.total_work_duration_seconds || 0;
                        const totalBreakSeconds_record = record.break_duration_seconds || 0;
                        
                        // Check if this session touches the report date
                        const punchInDate = new Date(punchIn);
                        punchInDate.setHours(0, 0, 0, 0);
                        const punchOutDate = new Date(punchOut);
                        punchOutDate.setHours(0, 0, 0, 0);
                        
                        // Calculate work duration for THIS DATE ONLY
                        let workSecondsForThisDate = 0;
                        let breakSecondsForThisDate = 0;
                        
                        if (punchInDate.getTime() === reportDate.getTime() && punchOutDate.getTime() === reportDate.getTime()) {
                            // Both punch in and out on same date - use full duration
                            workSecondsForThisDate = totalWorkSeconds;
                            breakSecondsForThisDate = totalBreakSeconds_record;
                            sessionCount++;
                        } else if (punchInDate.getTime() === reportDate.getTime() && punchOutDate.getTime() > reportDate.getTime()) {
                            // Punch in on this date, punch out next day (or later) - split at midnight
                            const midnightDate = new Date(reportDate);
                            midnightDate.setDate(midnightDate.getDate() + 1); // Next day at 00:00
                            
                            const secondsUntilMidnight = Math.floor((midnightDate - punchIn) / 1000);
                            workSecondsForThisDate = Math.min(secondsUntilMidnight, totalWorkSeconds);
                            
                            // Split break proportionally
                            const proportionOfDay = workSecondsForThisDate / totalWorkSeconds;
                            breakSecondsForThisDate = Math.floor(totalBreakSeconds_record * proportionOfDay);
                            
                            sessionCount++;
                        } else if (punchInDate.getTime() < reportDate.getTime() && punchOutDate.getTime() === reportDate.getTime()) {
                            // Punch in previous day, punch out on this date - only count portion after midnight
                            const midnightDate = new Date(reportDate); // Midnight at start of this date
                            
                            const secondsAfterMidnight = Math.floor((punchOut - midnightDate) / 1000);
                            workSecondsForThisDate = Math.min(secondsAfterMidnight, totalWorkSeconds);
                            
                            // Split break proportionally
                            const proportionOfDay = workSecondsForThisDate / totalWorkSeconds;
                            breakSecondsForThisDate = Math.floor(totalBreakSeconds_record * proportionOfDay);
                        } else if (punchInDate.getTime() < reportDate.getTime() && punchOutDate.getTime() > reportDate.getTime()) {
                            // Punch in before this date, punch out after this date - only count midnight to midnight portion
                            const startOfDay = new Date(reportDate);
                            const endOfDay = new Date(nextDay);
                            
                            const secondsOnThisDate = Math.floor((endOfDay - startOfDay) / 1000);
                            workSecondsForThisDate = Math.min(secondsOnThisDate, totalWorkSeconds);
                            
                            // Split break proportionally
                            const proportionOfDay = workSecondsForThisDate / totalWorkSeconds;
                            breakSecondsForThisDate = Math.floor(totalBreakSeconds_record * proportionOfDay);
                        }
                        
                        // Add to totals for this date
                        totalSeconds += workSecondsForThisDate;
                        totalBreakSeconds += breakSecondsForThisDate;
                        
                        // Store session info for display
                        if (workSecondsForThisDate > 0) {
                            punchSessions.push({
                                punch_in: punchIn,
                                punch_out: punchOut,
                                work_seconds: totalWorkSeconds,
                                work_seconds_this_date: workSecondsForThisDate,
                                break_seconds: totalBreakSeconds_record,
                                break_seconds_this_date: breakSecondsForThisDate,
                                crosses_midnight: punchInDate.getTime() !== punchOutDate.getTime()
                            });
                        }
                    });
                    
                    metrics.totalWorkHours = (totalSeconds / 3600).toFixed(2);
                    metrics.actualBreakMinutes = (totalBreakSeconds / 60).toFixed(2);
                    metrics.sessionsOnThisDate = punchSessions.length;
                    
                    console.log('Punch data - Sessions on this date:', metrics.sessionsOnThisDate, 'Total work seconds (this date):', totalSeconds, 'Hours:', metrics.totalWorkHours);
                }
            }
            
            // Process idle data - ONLY count Significant Idle (>= employee's threshold) STRICTLY BETWEEN punch-in and punch-out
            const idleThresholdSeconds = idleThresholdMinutes * 60; // Convert minutes to seconds
            let significantIdleSeconds = 0;
            if (idleData?.success && idleData.data && punchSessions.length > 0) {
                const idleRecords = Array.isArray(idleData.data) ? idleData.data : [];
                console.log('Idle records count:', idleRecords.length, 'Threshold:', idleThresholdMinutes, 'minutes');
                
                idleRecords.forEach(record => {
                    const duration = record.duration_seconds || (record.duration_minutes ? record.duration_minutes * 60 : 0);
                    
                    // Only count significant idle (>= employee's threshold)
                    if (duration >= idleThresholdSeconds) {
                        const idleStart = new Date(record.start_time);
                        const idleEnd = new Date(idleStart.getTime() + duration * 1000);
                        
                        // Check if this idle period falls on THIS DATE and STRICTLY WITHIN punch session window
                        const idleDate = new Date(idleStart);
                        idleDate.setHours(0, 0, 0, 0);
                        
                        // Only count if idle starts on this date
                        if (idleDate.getTime() === reportDate.getTime()) {
                            // Check for each session: is idle period STRICTLY between punch_in and punch_out?
                            punchSessions.forEach(session => {
                                // Idle must START at or after punch_in AND START BEFORE punch_out
                                // AND idle must END at or before punch_out
                                const idleStartsWithinSession = idleStart >= session.punch_in && idleStart < session.punch_out;
                                const idleEndsWithinSession = idleEnd <= session.punch_out;
                                
                                if (idleStartsWithinSession && idleEndsWithinSession) {
                                    // Idle is completely within the punch session window
                                    significantIdleSeconds += duration;
                                } else if (idleStartsWithinSession && !idleEndsWithinSession) {
                                    // Idle starts within session but ends after punch_out
                                    // Only count the portion BEFORE punch_out
                                    const secondsBeforePunchOut = Math.floor((session.punch_out - idleStart) / 1000);
                                    if (secondsBeforePunchOut >= 600) { // Only count if portion is >= 10 min
                                        significantIdleSeconds += secondsBeforePunchOut;
                                    }
                                }
                            });
                        }
                    }
                });
                
                metrics.significantIdleMinutes = (significantIdleSeconds / 60).toFixed(2);
                console.log('Idle data - Significant idle seconds (ONLY within punch windows):', significantIdleSeconds, 'Minutes:', metrics.significantIdleMinutes);
            } else {
                metrics.significantIdleMinutes = '0.00';
            }
            
            // Calculate actual working hours using correct formula:
            // Actual Working Time = Total Working Hours - Total Break (in hours) - Significant Idle Hours (ONLY within sessions)
            const totalWorkHours = parseFloat(metrics.totalWorkHours) || 0;
            const totalBreakHours = (parseFloat(metrics.actualBreakMinutes) || 0) / 60;
            const significantIdleHours = (parseFloat(metrics.significantIdleMinutes) || 0) / 60;
            
            metrics.actualWorkingHours = (totalWorkHours - totalBreakHours - significantIdleHours).toFixed(2);
            
            // Calculate final working hours with lunch duration
            // Formula: Final Work = Actual Working Time + Lunch Duration
            const lunchHours = lunchDurationMinutes / 60;
            metrics.finalWorkingHours = (parseFloat(metrics.actualWorkingHours) + lunchHours).toFixed(2);
            
            console.log('Calculation breakdown:');
            console.log('  Total Working Hours:', totalWorkHours, 'hrs');
            console.log('  Total Break:', metrics.actualBreakMinutes, 'min =', totalBreakHours.toFixed(2), 'hrs');
            console.log('  Significant Idle (≥10min):', metrics.significantIdleMinutes, 'min =', significantIdleHours.toFixed(2), 'hrs');
            console.log('  Actual Working Time:', metrics.actualWorkingHours, 'hrs');
            console.log('  Lunch Duration:', lunchDurationMinutes, 'min =', lunchHours.toFixed(2), 'hrs');
            console.log('  Final Working Hours:', metrics.finalWorkingHours, 'hrs');
            
            // Process browser data - aggregate by domain
            if (browserData?.success && browserData.data) {
                const webLogs = Array.isArray(browserData.data) ? browserData.data : [];
                console.log('Web logs count:', webLogs.length);
                
                // Group by domain and count visits
                const domainMap = {};
                webLogs.forEach(log => {
                    const domain = log.website_url ? new URL(log.website_url).hostname : 'unknown';
                    if (!domainMap[domain]) {
                        domainMap[domain] = { domain, visit_count: 0 };
                    }
                    domainMap[domain].visit_count++;
                });
                
                // Sort by visit count
                metrics.domainsList = Object.values(domainMap)
                    .sort((a, b) => b.visit_count - a.visit_count)
                    .slice(0, 10);
                
                if (metrics.domainsList.length > 0) {
                    metrics.topDomain = metrics.domainsList[0].domain;
                }
                
                console.log('Top domains:', metrics.domainsList);
            }
            
            // Process app data - aggregate by application
            if (appData?.success && appData.data) {
                const appLogs = Array.isArray(appData.data) ? appData.data : [];
                console.log('App logs count:', appLogs.length);
                
                // Group by application and sum duration
                const appMap = {};
                appLogs.forEach(log => {
                    const appName = log.app_name || 'unknown';
                    if (!appMap[appName]) {
                        appMap[appName] = { application: appName, usage_minutes: 0, duration_seconds: 0 };
                    }
                    const duration = log.duration_seconds || 0;
                    appMap[appName].duration_seconds += duration;
                    appMap[appName].usage_minutes = (appMap[appName].duration_seconds / 60).toFixed(2);
                });
                
                // Sort by usage time
                metrics.applicationsList = Object.values(appMap)
                    .sort((a, b) => b.duration_seconds - a.duration_seconds)
                    .slice(0, 10);
                
                if (metrics.applicationsList.length > 0) {
                    metrics.topApplication = metrics.applicationsList[0].application;
                }
                
                console.log('Top applications:', metrics.applicationsList);
            }
            
            console.log('Final metrics:', metrics);
        } catch (error) {
            console.error('Error calculating metrics:', error);
        }
        
        return metrics;
    },

    // Display stats cards
    displayEngineerReportStats(metrics) {
        const container = document.getElementById('engineerReportStatsCards');
        if (!container) return;
        
        // Calculate productivity percentage (ensure positive value)
        const workHours = parseFloat(metrics.totalWorkHours) || 0;
        const actualHours = parseFloat(metrics.actualWorkingHours) || 0;
        const productivity = workHours > 0 ? ((actualHours / workHours) * 100).toFixed(1) : 0;
        
        // Ensure actual hours don't go negative
        const displayActualHours = Math.max(0, actualHours).toFixed(2);
        
        // Get final working hours (actual working + lunch duration)
        const finalWorkingHours = metrics.finalWorkingHours || displayActualHours;
        
        // Convert break minutes to hours and minutes format
        const breakMinutes = parseFloat(metrics.significantIdleMinutes) || 0;
        const breakHours = Math.floor(breakMinutes / 60);
        const breakMins = Math.round(breakMinutes % 60);
        const breakDisplay = breakHours > 0 ? `${breakHours}h ${breakMins}m` : `${breakMins}m`;
        
        // Get session count for THIS DATE (may be different from total sessions if some cross midnight)
        const sessionCount = metrics.sessionsOnThisDate || metrics.totalSessions || 1;
        
        container.innerHTML = `
            <div style="background: white; border-radius: 16px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #667eea;">
                <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Total Working Hours</p>
                <h3 style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">${metrics.totalWorkHours}</h3>
                <p style="color: #94a3b8; font-size: 12px;">hours (on this date)</p>
            </div>
            <div style="background: white; border-radius: 16px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #ef4444;">
                <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Total Break</p>
                <h3 style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">${breakDisplay}</h3>
                <p style="color: #94a3b8; font-size: 12px;">significant idle (≥10min)</p>
            </div>
            <div style="background: white; border-radius: 16px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #10b981;">
                <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Actual Working Time</p>
                <h3 style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">${displayActualHours}</h3>
                <p style="color: #94a3b8; font-size: 12px;">hours</p>
            </div>
            <div style="background: white; border-radius: 16px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #8b5cf6;">
                <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Final Working Hours</p>
                <h3 style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">${finalWorkingHours}</h3>
                <p style="color: #94a3b8; font-size: 12px;">Actual + Lunch Duration</p>
            </div>
            <div style="background: white; border-radius: 16px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #06b6d4;">
                <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Sessions</p>
                <h3 style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">${sessionCount}</h3>
                <p style="color: #94a3b8; font-size: 12px;">sessions on this date</p>
            </div>
            <div style="background: white; border-radius: 16px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); border-left: 4px solid #0ea5e9;">
                <p style="color: #64748b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Productivity</p>
                <h3 style="font-size: 32px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 8px;">${productivity}%</h3>
                <p style="color: #94a3b8; font-size: 12px;">of total hours</p>
            </div>
        `;
    },

    // Display punch in/out table
    displayPunchInOutTable(punchData, metrics) {
        const container = document.getElementById('punchInOutTableContainer');
        if (!container) return;
        
        const records = punchData?.success && punchData.data ? (Array.isArray(punchData.data) ? punchData.data : [punchData.data]) : [];
        
        if (records.length === 0) {
            container.innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">No punch data found for this date</div>';
            return;
        }
        
        // Calculate lunch duration in hours for display
        const lunchDurationHours = (metrics.lunchDurationMinutes || 60) / 60;
        const actualWorkingHours = parseFloat(metrics.actualWorkingHours) || 0;
        // Use actual break duration from punch logs (not idle time)
        const breakDurationMinutes = parseFloat(metrics.actualBreakMinutes) || 0;
        const sessionCount = metrics.totalSessions || 1;
        
        // Sort records by punch in time
        const sortedRecords = [...records].sort((a, b) => {
            return new Date(a.punch_in_time) - new Date(b.punch_in_time);
        });
        
        // Build session details HTML
        let sessionDetailsHTML = '';
        sortedRecords.forEach((record, index) => {
            const punchInTime = this.extractTimeOnly(record.punch_in_time);
            const punchOutTime = record.punch_out_time ? this.extractTimeOnly(record.punch_out_time) : 'Still Working';
            const workHours = record.total_work_duration_seconds ? (record.total_work_duration_seconds / 3600).toFixed(2) : '0.00';
            const breakMin = record.break_duration_seconds ? (record.break_duration_seconds / 60).toFixed(2) : '0.00';
            
            sessionDetailsHTML += `
                <tr style="background: ${index % 2 === 0 ? '#f9fafb' : 'white'};">
                    <td colspan="2" style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 700; color: #6366f1; text-transform: uppercase; font-size: 11px; letter-spacing: 0.5px;">Session ${index + 1}</td>
                </tr>
                <tr style="background: ${index % 2 === 0 ? '#f9fafb' : 'white'};">
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151; font-size: 13px;">Punch In</td>
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 700; color: #667eea;">${punchInTime}</td>
                </tr>
                <tr style="background: ${index % 2 === 0 ? '#f9fafb' : 'white'};">
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151; font-size: 13px;">Punch Out</td>
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 700; color: #667eea;">${punchOutTime}</td>
                </tr>
                <tr style="background: ${index % 2 === 0 ? '#f9fafb' : 'white'};">
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151; font-size: 13px;">Session Work Time</td>
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 700; color: #10b981;">${workHours} hrs</td>
                </tr>
                <tr style="background: ${index % 2 === 0 ? '#f9fafb' : 'white'};">
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151; font-size: 13px;">Session Break</td>
                    <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 700; color: #dc2626;">${breakMin} min</td>
                </tr>
            `;
        });
        
        container.innerHTML = `
            <table style="width: 100%; border-collapse: collapse;">
                <tbody>
                    <tr style="background: #f9fafb; border-bottom: 2px solid #e5e7eb;">
                        <td colspan="2" style="padding: 16px; font-weight: 700; color: #6366f1; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">📋 ${sessionCount} Session(s) on This Date</td>
                    </tr>
                    ${sessionDetailsHTML}
                    <tr style="background: #f9fafb; border-top: 2px solid #e5e7eb;">
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 800; color: #374151; width: 40%; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">⏱️ Total Working Hours</td>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 18px; font-weight: 800; color: #10b981;">${metrics.totalWorkHours} hrs</td>
                    </tr>
                    <tr style="background: #fee2e2;">
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 800; color: #374151; width: 40%; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">☕ Total Break Duration</td>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 800; color: #dc2626;">${breakDurationMinutes.toFixed(2)} min</td>
                    </tr>
                    <tr style="background: #fee7f3;">
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 800; color: #374151; width: 40%; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">⏸️ Significant Idle (≥10min)</td>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 800; color: #be185d;">${metrics.significantIdleMinutes} min</td>
                    </tr>
                    <tr style="background: #f0f9ff;">
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 800; color: #374151; width: 40%; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">📊 Actual Working Time</td>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 800; color: #0ea5e9;">${actualWorkingHours.toFixed(2)} hrs</td>
                    </tr>
                    <tr>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 800; color: #374151; width: 40%; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">🍽️ Lunch Duration</td>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 16px; font-weight: 800; color: #8b5cf6;">+ ${lunchDurationHours.toFixed(2)} hrs</td>
                    </tr>
                    <tr style="background: #f3e8ff;">
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-weight: 800; color: #374151; width: 40%; text-transform: uppercase; font-size: 12px; letter-spacing: 0.5px;">✅ Final Working Hours</td>
                        <td style="padding: 16px; border-bottom: 1px solid #e5e7eb; font-size: 20px; font-weight: 800; color: #7c3aed;">${metrics.finalWorkingHours} hrs</td>
                    </tr>
                </tbody>
            </table>
        `;
    },

    // Display idle time table
    displayIdleTimeTable(idleData, metrics) {
        const container = document.getElementById('idleTimeTableContainer');
        if (!container) return;
        
        const idleRecords = idleData?.success && idleData.data ? (Array.isArray(idleData.data) ? idleData.data : []) : [];
        
        if (idleRecords.length === 0) {
            container.innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">No idle time recorded</div>';
            return;
        }
        
        // Filter to show only Significant Idle (>=10 minutes = 600 seconds)
        const significantIdleRecords = idleRecords.filter(record => {
            const duration = record.duration_seconds || 0;
            return duration >= 600;
        });
        
        if (significantIdleRecords.length === 0) {
            container.innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">No significant idle periods (≥10 minutes) recorded</div>';
            return;
        }
        
        const tableRows = significantIdleRecords.map((record, idx) => {
            const startTime = record.start_time ? this.extractTimeOnly(record.start_time) : '--';
            const endTime = record.end_time ? this.extractTimeOnly(record.end_time) : '--';
            const durationMinutes = record.duration_seconds ? Math.round(record.duration_seconds / 60) : 0;
            
            return `
            <tr ${idx % 2 === 0 ? 'style="background: #f9fafb;"' : ''}>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-weight: 700; color: #1f2937;">${startTime}</td>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-weight: 700; color: #1f2937;">${endTime}</td>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; color: #ef4444; font-weight: 800;">${durationMinutes} min</td>
            </tr>
        `;
        }).join('');
        
        container.innerHTML = `
            <table style="width: 100%; border-collapse: collapse;">
                <thead style="background: #f3f4f6; border-bottom: 2px solid #e5e7eb;">
                    <tr>
                        <th style="padding: 14px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;">Start Time (IST)</th>
                        <th style="padding: 14px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;">End Time (IST)</th>
                        <th style="padding: 14px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;">Duration</th>
                    </tr>
                </thead>
                <tbody>
                    ${tableRows}
                </tbody>
            </table>
            <div style="margin-top: 12px; padding: 14px 16px; background: #fef3c7; border-left: 4px solid #f59e0b; border-radius: 6px; font-size: 12px; color: #92400e; line-height: 1.5; font-weight: 700;">
                <strong>📌 Note:</strong> Only Significant Idle periods (≥10 minutes) are shown. Short pauses (&lt;10 minutes) are excluded from calculations.
            </div>
        `;
    },

    // Display top domains table
    displayTopDomainsTable(browserData, metrics) {
        const container = document.getElementById('topDomainsTableContainer');
        if (!container) return;
        
        if (metrics.domainsList.length === 0) {
            container.innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">No domain data found</div>';
            return;
        }
        
        const tableRows = metrics.domainsList.map((domain, idx) => {
            const visits = parseInt(domain.visit_count) || 0;
            return `
            <tr ${idx % 2 === 0 ? 'style="background: #f9fafb;"' : ''}>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-weight: 700;">${idx + 1}</td>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; word-break: break-all; font-weight: 700;">${domain.domain || '--'}</td>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; text-align: right; color: #667eea; font-weight: 800;">${visits}</td>
            </tr>
        `;
        }).join('');
        
        container.innerHTML = `
            <table style="width: 100%; border-collapse: collapse;">
                <thead style="background: #f3f4f6;">
                    <tr>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; width: 50px;">#</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase;">Domain</th>
                        <th style="padding: 12px 16px; text-align: right; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; width: 100px;">Visits</th>
                    </tr>
                </thead>
                <tbody>
                    ${tableRows}
                </tbody>
            </table>
        `;
    },

    // Display top applications table
    displayTopApplicationsTable(appData, metrics) {
        const container = document.getElementById('topApplicationsTableContainer');
        if (!container) return;
        
        if (metrics.applicationsList.length === 0) {
            container.innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">No application data found</div>';
            return;
        }
        
        const tableRows = metrics.applicationsList.map((app, idx) => {
            const minutes = parseFloat(app.usage_minutes) || parseFloat(app.duration_minutes) || 0;
            return `
            <tr ${idx % 2 === 0 ? 'style="background: #f9fafb;"' : ''}>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-weight: 700;">${idx + 1}</td>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-weight: 700;">${app.application || app.app_name || '--'}</td>
                <td style="padding: 12px 16px; border-bottom: 1px solid #e5e7eb; text-align: right; color: #667eea; font-weight: 800;">${minutes.toFixed(1)} min</td>
            </tr>
        `;
        }).join('');
        
        container.innerHTML = `
            <table style="width: 100%; border-collapse: collapse;">
                <thead style="background: #f3f4f6;">
                    <tr>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; width: 50px;">#</th>
                        <th style="padding: 12px 16px; text-align: left; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase;">Application</th>
                        <th style="padding: 12px 16px; text-align: right; font-weight: 800; color: #374151; font-size: 12px; text-transform: uppercase; width: 120px;">Usage Time</th>
                    </tr>
                </thead>
                <tbody>
                    ${tableRows}
                </tbody>
            </table>
        `;
    },

    // Render charts
    async renderEngineerReportCharts(metrics) {
        // Check if Chart.js is available
        if (typeof Chart === 'undefined') {
            // Load Chart.js library
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/chart.js@3.9.1/dist/chart.min.js';
            script.onload = () => this.createEngineerReportCharts(metrics);
            document.head.appendChild(script);
        } else {
            this.createEngineerReportCharts(metrics);
        }
    },

    // Create engineer report charts
    createEngineerReportCharts(metrics) {
        // Working time chart (Pie chart)
        this.createWorkingTimeChart(metrics);
        
        // Productivity gauge chart
        this.createProductivityGaugeChart(metrics);
        
        // Application usage chart (Bar chart)
        this.createApplicationUsageChart(metrics);
        
        // Domain usage chart (Doughnut chart)
        this.createDomainUsageChart(metrics);
    },

    // Create working time pie chart
    createWorkingTimeChart(metrics) {
        const ctx = document.getElementById('workingTimeChart');
        if (!ctx) return;
        
        const workingTime = parseFloat(metrics.actualWorkingHours) || 0;
        const idleTime = parseFloat(metrics.significantIdleMinutes) / 60 || 0;
        const restTime = Math.max(0, parseFloat(metrics.totalWorkHours) - workingTime - idleTime);
        
        // Destroy existing chart if it exists
        if (window.engineerCharts && window.engineerCharts.workingTimeChart) {
            window.engineerCharts.workingTimeChart.destroy();
        }
        if (!window.engineerCharts) window.engineerCharts = {};
        
        window.engineerCharts.workingTimeChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Actual Working', 'Significant Idle', 'Unaccounted'],
                datasets: [{
                    data: [workingTime, idleTime, restTime],
                    backgroundColor: ['#10b981', '#ef4444', '#f3f4f6'],
                    borderColor: ['#059669', '#dc2626', '#e5e7eb'],
                    borderWidth: 3,
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                cutout: '65%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            font: { size: 13, weight: 'bold' },
                            color: '#374151'
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.label + ': ' + parseFloat(context.parsed).toFixed(2) + ' hrs';
                            }
                        }
                    }
                }
            }
        });
    },

    // Create productivity gauge chart
    createProductivityGaugeChart(metrics) {
        const ctx = document.getElementById('productivityGaugeChart');
        if (!ctx) return;
        
        const workHours = parseFloat(metrics.totalWorkHours) || 1;
        const actualHours = Math.max(0, parseFloat(metrics.actualWorkingHours));
        const productivity = (actualHours / workHours * 100);
        
        // Destroy existing chart if it exists
        if (window.engineerCharts && window.engineerCharts.productivityGaugeChart) {
            window.engineerCharts.productivityGaugeChart.destroy();
        }
        if (!window.engineerCharts) window.engineerCharts = {};
        
        window.engineerCharts.productivityGaugeChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Productive', 'Not Productive'],
                datasets: [{
                    data: [productivity, 100 - productivity],
                    backgroundColor: [
                        productivity >= 70 ? '#10b981' : (productivity >= 50 ? '#f59e0b' : '#ef4444'),
                        '#e5e7eb'
                    ],
                    borderColor: ['white', '#f3f4f6'],
                    borderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                cutout: '70%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            font: { size: 13, weight: 'bold' },
                            color: '#374151'
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.label + ': ' + parseFloat(context.parsed).toFixed(1) + '%';
                            }
                        }
                    }
                }
            }
        });
        
        // Add center text for productivity score
        const centerText = document.createElement('div');
        centerText.style.cssText = `
            position: absolute;
            text-align: center;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            pointer-events: none;
        `;
        centerText.innerHTML = `<div style="font-size: 32px; font-weight: 700; color: #1f2937;">${productivity.toFixed(1)}%</div>
                                <div style="font-size: 12px; color: #64748b; margin-top: 4px;">Productivity</div>`;
        ctx.parentElement.style.position = 'relative';
        ctx.parentElement.appendChild(centerText);
    },

    // Create domain usage chart
    createDomainUsageChart(metrics) {
        const ctx = document.getElementById('domainUsageChart');
        if (!ctx) return;
        
        const labels = metrics.domainsList.slice(0, 6).map(d => 
            d.domain ? (d.domain.length > 20 ? d.domain.substring(0, 20) + '...' : d.domain) : 'Unknown'
        );
        const data = metrics.domainsList.slice(0, 6).map(d => d.visit_count || 0);
        
        const colors = ['#667eea', '#764ba2', '#f093fb', '#4facfe', '#00f2fe', '#667eea'];
        
        // Destroy existing chart if it exists
        if (window.engineerCharts && window.engineerCharts.domainUsageChart) {
            window.engineerCharts.domainUsageChart.destroy();
        }
        if (!window.engineerCharts) window.engineerCharts = {};
        
        window.engineerCharts.domainUsageChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors.slice(0, labels.length),
                    borderColor: 'white',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 15,
                            font: { size: 12, weight: 'bold' },
                            color: '#374151'
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.label + ': ' + context.parsed + ' visits';
                            }
                        }
                    }
                }
            }
        });
    },

    // Create application usage bar chart
    createApplicationUsageChart(metrics) {
        const ctx = document.getElementById('applicationUsageChart');
        if (!ctx) return;
        
        const labels = metrics.applicationsList.slice(0, 8).map(app => {
            const name = app.application || app.app_name || 'Unknown';
            return name.length > 15 ? name.substring(0, 15) + '...' : name;
        });
        const data = metrics.applicationsList.slice(0, 8).map(app => parseFloat(app.usage_minutes) || 0);
        
        const colors = [
            '#667eea', '#764ba2', '#f093fb', '#4facfe', 
            '#00f2fe', '#10b981', '#f59e0b', '#ef4444'
        ];
        
        // Destroy existing chart if it exists
        if (window.engineerCharts && window.engineerCharts.applicationUsageChart) {
            window.engineerCharts.applicationUsageChart.destroy();
        }
        if (!window.engineerCharts) window.engineerCharts = {};
        
        window.engineerCharts.applicationUsageChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Usage (minutes)',
                    data: data,
                    backgroundColor: colors.slice(0, labels.length),
                    borderRadius: 8,
                    borderSkipped: false
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        labels: { 
                            font: { size: 12, weight: 'bold' },
                            color: '#374151',
                            padding: 15
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + parseFloat(context.parsed.x).toFixed(1) + ' min';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { 
                            font: { size: 11, weight: 'bold' },
                            color: '#6b7280'
                        },
                        grid: {
                            color: '#f3f4f6'
                        }
                    },
                    y: {
                        ticks: { 
                            font: { size: 11, weight: 'bold' },
                            color: '#6b7280'
                        },
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    },

    // Clear engineer report filters
    clearEngineerReportFilters() {
        document.getElementById('reportEngineerSelect').value = '';
        document.getElementById('reportDatePicker').value = new Date().toISOString().split('T')[0];
        document.getElementById('engineerReportStatsCards').innerHTML = '';
        document.getElementById('punchInOutTableContainer').innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">Select an engineer and date to view details</div>';
        document.getElementById('idleTimeTableContainer').innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">Idle time information will appear here</div>';
        document.getElementById('topDomainsTableContainer').innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">Domain information will appear here</div>';
        document.getElementById('topApplicationsTableContainer').innerHTML = '<div style="padding: 24px; text-align: center; color: #6b7280;">Application information will appear here</div>';
    },

    // Export engineer report
    async exportEngineerReport() {
        try {
            const engineerId = document.getElementById('reportEngineerSelect')?.value;
            const reportDate = document.getElementById('reportDatePicker')?.value;
            
            if (!engineerId) {
                this.showToast('Please select an engineer first', 'warning');
                return;
            }
            
            this.showToast('Preparing export...', 'info');
            
            // Get the current report data from visible tables
            const punchInOutHtml = document.getElementById('punchInOutTableContainer')?.innerText || '';
            const idleTimeHtml = document.getElementById('idleTimeTableContainer')?.innerText || '';
            const domainsHtml = document.getElementById('topDomainsTableContainer')?.innerText || '';
            const appsHtml = document.getElementById('topApplicationsTableContainer')?.innerText || '';
            
            // Create CSV content
            const csvContent = `ENGINEER ACTIVITY REPORT
Date: ${reportDate}

PUNCH IN/OUT & WORKING HOURS
${punchInOutHtml}

IDLE TIME DETAILS
${idleTimeHtml}

TOP DOMAINS
${domainsHtml}

TOP APPLICATIONS
${appsHtml}`;
            
            // Download CSV
            const blob = new Blob([csvContent], { type: 'text/csv' });
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `engineer-report-${engineerId}-${reportDate}.csv`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
            
            this.showToast('Report exported successfully', 'success');
        } catch (error) {
            console.error('Export error:', error);
            this.showToast('Export failed', 'error');
        }
    },

    // Load employees list for filter dropdown
    async loadEmployeesListForDaily() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_punch_employees', { company_name: companyName });
            
            if (response.success) {
                const employeeSelect = document.getElementById('dailyWorkingEmployee');
                if (employeeSelect) {
                    // Clear existing options
                    employeeSelect.innerHTML = '<option value="">Select an engineer...</option>';
                    
                    // Add employee options with display names
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.employee_id || emp.username;
                        option.textContent = `${emp.display_name || emp.full_name || emp.employee_id}`;
                        employeeSelect.appendChild(option);
                    });
                    
                    console.log(`Loaded ${response.data.length} employees for daily hours filter`);
                }
            }
        } catch (error) {
            console.error('Error loading employees list for daily hours:', error);
        }
    },

    // Load employees list for filter dropdown
    async loadEmployeesList() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            
            if (response.success) {
                const employeeSelect = document.getElementById('employeeFilter');
                if (employeeSelect) {
                    // Clear existing options except "All Employees"
                    employeeSelect.innerHTML = '<option value="all">All Employees</option>';
                    
                    // Add employee options with display names
                    response.data.forEach(emp => {
                        const option = document.createElement('option');
                        option.value = emp.username;
                        option.textContent = `${emp.display_name} (${emp.username})`;
                        employeeSelect.appendChild(option);
                    });
                    
                    console.log(`Loaded ${response.data.length} employees to dropdown`);
                }
            }
        } catch (error) {
            console.error('Error loading employees list:', error);
        }
    },

    // Load attendance data
    async loadAttendanceData(page = 1) {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const startDate = document.getElementById('startDateFilter')?.value;
            const endDate = document.getElementById('endDateFilter')?.value;
            const employeeFilter = document.getElementById('employeeFilter')?.value;
            const employee = employeeFilter && employeeFilter !== 'all' ? employeeFilter : '';
            
            const requestData = {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee_id: employee,
                page: page,
                limit: 50
            };
            
            console.log('Loading attendance data with filters:', requestData);

            // Show loading state
            const loadingState = document.getElementById('loadingState');
            const recordsTableContainer = document.getElementById('recordsTableContainer');
            const paginationContainer = document.getElementById('paginationContainer');
            
            if (loadingState) loadingState.style.display = 'block';
            if (recordsTableContainer) recordsTableContainer.style.display = 'none';
            if (paginationContainer) paginationContainer.style.display = 'none';

            const response = await this.api('get_attendance', requestData);
            
            if (response.success && response.data) {
                this.displayAttendanceData(response.data);
                
                // Extract records for stats - handle both formats
                let records = [];
                if (Array.isArray(response.data)) {
                    records = response.data;
                } else if (response.data.records) {
                    records = response.data.records;
                }
                
                if (records.length > 0) {
                    this.updateAttendanceStats(records);
                }
            } else {
                console.error('Error response:', response);
                this.showToast(response.error || 'Failed to load attendance data', 'error');
                if (recordsTableContainer) {
                    recordsTableContainer.innerHTML = '<div style="padding: 40px; text-align: center; color: #ef4444;">⚠️ Failed to load data</div>';
                    recordsTableContainer.style.display = 'block';
                }
            }
        } catch (error) {
            console.error('Error loading attendance data:', error);
            this.showToast('Error loading attendance data', 'error');
        } finally {
            // Hide loading state
            const loadingState = document.getElementById('loadingState');
            if (loadingState) loadingState.style.display = 'none';
        }
    },

    async loadEmployeeDropdown() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const response = await this.api('get_employees', { company_name: companyName });
            
            const employeeFilter = document.getElementById('employeeFilter');
            if (!employeeFilter || !response.success) return;
            
            // Clear existing options except "All Employees"
            employeeFilter.innerHTML = '<option value="all">All Employees</option>';
            
            // Add employee options
            if (Array.isArray(response.data)) {
                response.data.forEach(emp => {
                    const option = document.createElement('option');
                    option.value = emp.employee_id;
                    option.textContent = `${emp.display_name || emp.full_name} (${emp.employee_id})`;
                    employeeFilter.appendChild(option);
                });
            }
            
            console.log('Loaded', response.data.length, 'employees into dropdown');
        } catch (error) {
            console.error('Error loading employees:', error);
        }
    },

    // Display attendance data in table
    displayAttendanceData(data) {
        const tableBody = document.getElementById('recordsTableBody');
        const recordsCount = document.getElementById('recordsCount');
        const tableContainer = document.getElementById('recordsTableContainer');
        const paginationContainer = document.getElementById('paginationContainer');
        
        if (!tableBody) return;

        // Handle both array and object formats
        let records = [];
        let pagination = { total_records: 0, total_pages: 0 };
        
        if (Array.isArray(data)) {
            // If data is an array of records
            records = data;
            pagination = { total_records: data.length, total_pages: 1 };
        } else if (data.records) {
            // If data is an object with records and pagination
            records = Array.isArray(data.records) ? data.records : [];
            pagination = data.pagination || { total_records: records.length, total_pages: 1 };
        } else {
            // Fallback
            records = [];
            pagination = { total_records: 0, total_pages: 0 };
        }

        // Update records count
        if (recordsCount) {
            recordsCount.textContent = `Showing ${records.length} of ${pagination.total_records} records`;
        }

        // Clear existing rows
        tableBody.innerHTML = '';

        if (records.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="8" style="padding: 40px; text-align: center; color: #6b7280;">
                        <div style="margin-bottom: 16px;">📋</div>
                        <div>No attendance records found for the selected criteria</div>
                    </td>
                </tr>
            `;
        } else {
            // Add rows to table
            records.forEach(record => {
                const row = document.createElement('tr');
                row.style.borderBottom = '1px solid #f3f4f6';

                // Helper function to format timestamp to show actual stored time
                const formatTimestamp = (timestamp) => {
                    if (!timestamp) return null;
                    try {
                        // Parse the timestamp as-is from database without timezone conversion
                        const date = new Date(timestamp);
                        if (isNaN(date.getTime())) return null;
                        
                        // Extract time and date directly from the ISO string without conversion
                        const isoStr = date.toISOString(); // This gives us UTC
                        const parts = isoStr.split('T');
                        const datePart = parts[0]; // YYYY-MM-DD
                        const timePart = parts[1].substring(0, 5); // HH:MM
                        
                        // If the timestamp has timezone offset in the original string, extract it
                        let actualDate = datePart;
                        let actualTime = timePart;
                        
                        if (timestamp.includes('+') || timestamp.includes('-')) {
                            // Parse the raw timestamp string to get actual local time
                            const match = timestamp.match(/(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2}):(\d{2})/);
                            if (match) {
                                actualDate = `${match[1]}-${match[2]}-${match[3]}`;
                                actualTime = `${match[4]}:${match[5]}`;
                                
                                // Convert to 12-hour format
                                const hour = parseInt(match[4]);
                                const min = match[5];
                                const ampm = hour >= 12 ? 'pm' : 'am';
                                const hour12 = hour % 12 || 12;
                                actualTime = `${String(hour12).padStart(2, '0')}:${min} ${ampm}`;
                                
                                // Format date as DD/M/YYYY
                                const dateObj = new Date(`${match[1]}-${match[2]}-${match[3]}`);
                                const day = dateObj.getDate();
                                const month = dateObj.getMonth() + 1;
                                const year = dateObj.getFullYear();
                                actualDate = `${day}/${month}/${year}`;
                            }
                        }
                        
                        return { time: actualTime, date: actualDate };
                    } catch (e) {
                        console.error('Error formatting timestamp:', timestamp, e);
                        return null;
                    }
                };

                // Get punch in time
                const punchInFormatted = record.punch_in_time_formatted ? record.punch_in_time_formatted.split(',') : null;
                const punchInRaw = formatTimestamp(record.punch_in_time);
                const punchInTime = punchInFormatted ? 
                    `<div style="font-weight: 500;">${punchInFormatted[1] || punchInFormatted[0]}</div><div style="font-size: 12px; color: #6b7280;">${punchInFormatted[0] || ''}</div>` :
                    (punchInRaw ? `<div style="font-weight: 500;">${punchInRaw.time}</div><div style="font-size: 12px; color: #6b7280;">${punchInRaw.date}</div>` : '-');

                // Get punch out time
                const hasPunchOut = record.punch_out_time || (record.punch_out_time_formatted && record.punch_out_time_formatted !== 'Still Working');
                const punchOutFormatted = record.punch_out_time_formatted && record.punch_out_time_formatted !== 'Still Working' ? record.punch_out_time_formatted.split(',') : null;
                const punchOutRaw = formatTimestamp(record.punch_out_time);
                const punchOutTime = punchOutFormatted ?
                    `<div style="font-weight: 500;">${punchOutFormatted[1] || punchOutFormatted[0]}</div><div style="font-size: 12px; color: #6b7280;">${punchOutFormatted[0] || ''}</div>` :
                    (punchOutRaw ? `<div style="font-weight: 500;">${punchOutRaw.time}</div><div style="font-size: 12px; color: #6b7280;">${punchOutRaw.date}</div>` : '<span style="color: #059669; font-weight: 500;">Still Working</span>');

                // Calculate work hours - use actual database duration if available
                let workHoursDisplay = '0h';
                if (record.total_work_duration_seconds) {
                    // Use database calculated duration
                    const totalHours = Math.floor(record.total_work_duration_seconds / 3600);
                    const mins = Math.floor((record.total_work_duration_seconds % 3600) / 60);
                    workHoursDisplay = mins > 0 ? `${totalHours}.${Math.round(mins/6)}h` : `${totalHours}h`;
                } else if (record.work_hours) {
                    workHoursDisplay = record.work_hours + 'h';
                } else if (punchInRaw && punchOutRaw) {
                    // Calculate from timestamps
                    const inTime = new Date(record.punch_in_time);
                    const outTime = new Date(record.punch_out_time);
                    const diffMs = outTime - inTime;
                    const hours = (diffMs / (1000 * 60 * 60)).toFixed(1);
                    workHoursDisplay = hours + 'h';
                }

                // Calculate break duration
                let breakDisplay = '0m';
                if (record.break_duration_seconds) {
                    const breakMins = Math.floor(record.break_duration_seconds / 60);
                    breakDisplay = breakMins + 'm';
                } else if (record.break_minutes) {
                    breakDisplay = record.break_minutes + 'm';
                }

                const status = hasPunchOut ? 'Completed' : 'Active';
                const statusColor = hasPunchOut ? '#dcfce7' : '#fef3c7';
                const statusTextColor = hasPunchOut ? '#166534' : '#92400e';

                row.innerHTML = `
                    <td style="padding: 16px; font-weight: 500; color: #1f2937;">
                        <div style="display: flex; align-items: center; gap: 8px;">
                            <div style="width: 32px; height: 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-size: 12px; font-weight: 600;">
                                ${record.username ? record.username.substring(0, 2).toUpperCase() : 'UN'}
                            </div>
                            ${record.username || 'Unknown'}
                        </div>
                    </td>
                    <td style="padding: 16px; color: #374151; font-weight: 500;">
                        ${record.display_name || record.username || 'Unknown'}
                    </td>
                    <td style="padding: 16px; color: #374151;">
                        ${punchInTime}
                    </td>
                    <td style="padding: 16px; color: #374151;">
                        ${punchOutTime}
                    </td>
                    <td style="padding: 16px; color: #374151; font-weight: 500;">
                        ${workHoursDisplay}
                    </td>
                    <td style="padding: 16px; color: #374151;">
                        ${breakDisplay}
                    </td>
                    <td style="padding: 16px; color: #374151; font-size: 14px;">
                        ${record.system_name || record.machine_name || 'Unknown'}
                    </td>
                    <td style="padding: 16px;">
                        <span style="background: ${statusColor}; color: ${statusTextColor}; padding: 4px 8px; border-radius: 6px; font-size: 12px; font-weight: 500;">${status}</span>
                    </td>
                `;
                tableBody.appendChild(row);
            });
        }

        // Show table and pagination
        tableContainer.style.display = 'block';
        if (pagination.total_pages > 1) {
            this.renderPagination(pagination);
            paginationContainer.style.display = 'block';
        } else {
            paginationContainer.style.display = 'none';
        }
    },

    // Update attendance statistics cards
    updateAttendanceStats(records) {
        const statsContainer = document.getElementById('attendanceStatsCards');
        if (!statsContainer) return;

        const totalRecords = records.length;
        
        // Count active and completed - check actual punch out
        let activeEmployees = 0;
        let completedSessions = 0;
        let totalWorkSeconds = 0;

        records.forEach(r => {
            const hasPunchOut = r.punch_out_time || (r.punch_out_time_formatted && r.punch_out_time_formatted !== 'Still Working');
            
            if (hasPunchOut) {
                completedSessions++;
                
                // Use database calculated work duration if available
                if (r.total_work_duration_seconds) {
                    totalWorkSeconds += r.total_work_duration_seconds;
                } else if (r.work_hours) {
                    totalWorkSeconds += r.work_hours * 3600;
                } else if (r.punch_in_time && r.punch_out_time) {
                    const inTime = new Date(r.punch_in_time);
                    const outTime = new Date(r.punch_out_time);
                    totalWorkSeconds += (outTime - inTime) / 1000;
                }
            } else {
                activeEmployees++;
            }
        });

        const avgWorkHours = completedSessions > 0 ? 
            (totalWorkSeconds / (3600 * completedSessions)).toFixed(1) : 0;

        statsContainer.innerHTML = `
            <div style="background: white; padding: 20px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 12px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                            <circle cx="8.5" cy="7" r="4"/>
                            <path d="M20 8v6M23 11h-6"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Total Records</div>
                </div>
                <div style="font-size: 24px; font-weight: 700; color: #1f2937;">${totalRecords}</div>
            </div>

            <div style="background: white; padding: 20px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 12px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <circle cx="12" cy="12" r="10"/>
                            <polyline points="12 6 12 12 16 14"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Currently Working</div>
                </div>
                <div style="font-size: 24px; font-weight: 700; color: #1f2937;">${activeEmployees}</div>
            </div>

            <div style="background: white; padding: 20px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 12px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                            <polyline points="22 4 12 14.01 9 11.01"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Completed</div>
                </div>
                <div style="font-size: 24px; font-weight: 700; color: #1f2937;">${completedSessions}</div>
            </div>

            <div style="background: white; padding: 20px; border-radius: 12px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 12px;">
                    <div style="width: 40px; height: 40px; background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); border-radius: 10px; display: flex; align-items: center; justify-content: center;">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                            <circle cx="12" cy="12" r="10"/>
                            <polyline points="12 6 12 12 16 14"/>
                        </svg>
                    </div>
                    <div style="font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600;">Avg Work Hours</div>
                </div>
                <div style="font-size: 24px; font-weight: 700; color: #1f2937;">${avgWorkHours}h</div>
            </div>
        `;
    },

    // Render pagination
    renderPagination(pagination) {
        const container = document.getElementById('paginationContainer');
        if (!container) return;

        const { current_page, total_pages, has_prev, has_next } = pagination;

        container.innerHTML = `
            <div style="display: flex; justify-content: between; align-items: center;">
                <div style="color: #6b7280; font-size: 14px;">
                    Page ${current_page} of ${total_pages}
                </div>
                <div style="display: flex; gap: 8px;">
                    <button 
                        onclick="app.loadAttendanceData(${current_page - 1})"
                        ${!has_prev ? 'disabled' : ''}
                        style="padding: 8px 16px; border: 1px solid #d1d5db; border-radius: 6px; background: white; cursor: ${has_prev ? 'pointer' : 'not-allowed'}; opacity: ${has_prev ? 1 : 0.5};"
                    >
                        Previous
                    </button>
                    <button 
                        onclick="app.loadAttendanceData(${current_page + 1})"
                        ${!has_next ? 'disabled' : ''}
                        style="padding: 8px 16px; border: 1px solid #d1d5db; border-radius: 6px; background: white; cursor: ${has_next ? 'pointer' : 'not-allowed'}; opacity: ${has_next ? 1 : 0.5};"
                    >
                        Next
                    </button>
                </div>
            </div>
        `;
    },

    // Apply attendance filters
    async applyAttendanceFilters() {
        await this.loadAttendanceData(1);
    },

    // Load all records without filters
    async loadAllRecords() {
        // Clear all filters
        document.getElementById('startDateFilter').value = '';
        document.getElementById('endDateFilter').value = '';
        document.getElementById('employeeFilter').value = 'all';
        
        // Load data without any filters
        await this.loadAttendanceData(1);
    },

    // Clear attendance filters
    clearAttendanceFilters() {
        document.getElementById('startDateFilter').value = new Date().toISOString().split('T')[0];
        document.getElementById('endDateFilter').value = new Date().toISOString().split('T')[0];
        document.getElementById('employeeFilter').value = 'all';
        this.loadAttendanceData(1);
    },

    // Refresh attendance data
    async refreshAttendanceData() {
        await this.loadAttendanceData(1);
        this.showToast('Attendance data refreshed', 'success');
    },

    // Export attendance data as CSV
    async exportAttendanceData() {
        try {
            this.showToast('Preparing CSV export...', 'info');
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const startDate = document.getElementById('startDateFilter')?.value;
            const endDate = document.getElementById('endDateFilter')?.value;
            const employee = document.getElementById('employeeFilter')?.value;
            
            const requestData = {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee_filter: employee,
                page: 1,
                limit: 10000 // Large limit to get all records for export
            };

            const response = await this.api('get_attendance_reports', requestData);
            
            if (response.success && response.data.records.length > 0) {
                // Create CSV content
                const headers = ['Employee', 'Punch In', 'Punch Out', 'Work Hours', 'Break Time', 'System Name', 'IP Address', 'Status'];
                const csvContent = [
                    headers.join(','),
                    ...response.data.records.map(record => [
                        record.username || '',
                        record.punch_in_time_formatted || '',
                        record.punch_out_time_formatted || '',
                        record.work_hours ? record.work_hours + 'h' : '0h',
                        record.break_minutes ? record.break_minutes + 'm' : '0m',
                        record.system_name || '',
                        record.ip_address || '',
                        record.punch_out_time_formatted && record.punch_out_time_formatted !== 'Still Working' ? 'Completed' : 'Active'
                    ].map(field => `"${field}"`).join(','))
                ].join('\n');

                // Download CSV
                const blob = new Blob([csvContent], { type: 'text/csv' });
                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = `attendance-report-${startDate}-to-${endDate}.csv`;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);
                
                this.showToast('CSV export completed successfully', 'success');
            } else {
                this.showToast('No data available for export', 'error');
            }
        } catch (error) {
            console.error('Export error:', error);
            this.showToast('Export failed', 'error');
        }
    },

    // Show toast notification
    showToast(message, type = 'info') {
        // Remove existing toast
        const existingToast = document.querySelector('.toast');
        if (existingToast) {
            existingToast.remove();
        }
        
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        document.body.appendChild(toast);
        
        // Show toast
        setTimeout(() => toast.classList.add('show'), 100);
        
        // Hide toast after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    },

    // Logout function
    logout() {
        if (confirm('Are you sure you want to logout?')) {
            // Clear session data
            sessionStorage.removeItem('userData');
            sessionStorage.removeItem('isLoggedIn');
            sessionStorage.removeItem('userRole');
            this.userData = null;
            this.currentRole = 'admin';
            
            // Hide app container and show login
            document.getElementById('appContainer').style.display = 'none';
            document.getElementById('loginSection').style.display = 'flex';
            this.renderLoginForm();
            this.showToast('Logged out successfully', 'info');
        }
    },

    // ===== SITE MONITORING FUNCTIONS =====
    addNewSiteForm() {
        // Create modal for adding a new site
        const modalId = 'addSiteModal-' + Date.now();
        const modal = document.createElement('div');
        modal.id = modalId;
        modal.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.6);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 9999;
            padding: 20px;
        `;
        
        modal.innerHTML = `
            <div style="background: white; border-radius: 16px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); max-width: 600px; width: 100%; max-height: 90vh; overflow-y: auto;">
                <!-- Header -->
                <div style="padding: 28px; border-bottom: 1px solid #e5e7eb; display: flex; align-items: center; justify-content: space-between;">
                    <div>
                        <h2 style="font-size: 24px; font-weight: 700; color: #0f172a; margin: 0; margin-bottom: 4px;">Add New Website</h2>
                        <p style="color: #64748b; font-size: 14px; margin: 0;">Monitor a new website's uptime and performance</p>
                    </div>
                    <button onclick="document.getElementById('${modalId}').remove()" style="background: transparent; border: none; font-size: 24px; cursor: pointer; color: #94a3b8; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;">✕</button>
                </div>
                
                <!-- Form -->
                <div style="padding: 28px;">
                    <form id="addSiteForm" style="display: flex; flex-direction: column; gap: 20px;">
                        <!-- Site Name -->
                        <div>
                            <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 14px;">Site Name *</label>
                            <input 
                                type="text" 
                                id="siteName" 
                                placeholder="e.g., Google Search"
                                style="width: 100%; padding: 12px 16px; border: 1px solid #d1d5db; border-radius: 10px; font-size: 14px; box-sizing: border-box; transition: all 0.3s; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;"
                                required
                            />
                            <small style="color: #94a3b8; font-size: 12px; margin-top: 4px; display: block;">The name of the website to monitor</small>
                        </div>
                        
                        <!-- Site URL -->
                        <div>
                            <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 14px;">Site URL *</label>
                            <input 
                                type="url" 
                                id="siteUrl" 
                                placeholder="https://example.com"
                                style="width: 100%; padding: 12px 16px; border: 1px solid #d1d5db; border-radius: 10px; font-size: 14px; box-sizing: border-box; transition: all 0.3s; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;"
                                required
                            />
                            <small style="color: #94a3b8; font-size: 12px; margin-top: 4px; display: block;">The full URL including https://</small>
                        </div>
                        
                        <!-- Company Name -->
                        <div>
                            <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 14px;">Company Name *</label>
                            <input 
                                type="text" 
                                id="companyName" 
                                placeholder="Company Name"
                                style="width: 100%; padding: 12px 16px; border: 1px solid #d1d5db; border-radius: 10px; font-size: 14px; box-sizing: border-box; transition: all 0.3s; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;"
                                required
                            />
                            <small style="color: #94a3b8; font-size: 12px; margin-top: 4px; display: block;">The company that owns this website</small>
                        </div>
                        
                        <!-- Monitoring Interval -->
                        <div>
                            <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 14px;">Check Interval (seconds) *</label>
                            <input 
                                type="number" 
                                id="interval" 
                                value="30"
                                min="10"
                                max="3600"
                                style="width: 100%; padding: 12px 16px; border: 1px solid #d1d5db; border-radius: 10px; font-size: 14px; box-sizing: border-box; transition: all 0.3s; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;"
                                required
                            />
                            <small style="color: #94a3b8; font-size: 12px; margin-top: 4px; display: block;">How often to check the website (10 - 3600 seconds)</small>
                        </div>
                    </form>
                </div>
                
                <!-- Footer -->
                <div style="padding: 20px 28px; border-top: 1px solid #e5e7eb; display: flex; gap: 12px; justify-content: flex-end;">
                    <button 
                        onclick="document.getElementById('${modalId}').remove()" 
                        style="padding: 12px 24px; background: #f3f4f6; color: #1f2937; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; transition: all 0.3s;"
                    >
                        Cancel
                    </button>
                    <button 
                        onclick="app.submitAddSite('${modalId}')" 
                        style="padding: 12px 24px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; box-shadow: 0 4px 12px rgba(14, 165, 233, 0.3); transition: all 0.3s;"
                    >
                        Add Site
                    </button>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Focus on first input
        setTimeout(() => {
            document.getElementById('siteName').focus();
        }, 100);
        
        // Close on escape key
        const closeOnEscape = (e) => {
            if (e.key === 'Escape') {
                modal.remove();
                document.removeEventListener('keydown', closeOnEscape);
            }
        };
        document.addEventListener('keydown', closeOnEscape);
        
        // Close when clicking outside
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });
    },

    submitAddSite(modalId) {
        const siteName = document.getElementById('siteName').value.trim();
        const siteUrl = document.getElementById('siteUrl').value.trim();
        const companyName = document.getElementById('companyName').value.trim();
        const interval = parseInt(document.getElementById('interval').value) || 30;
        
        if (!siteName || !siteUrl || !companyName) {
            this.showToast('Please fill in all required fields', 'error');
            return;
        }
        
        if (interval < 10 || interval > 3600) {
            this.showToast('Interval must be between 10 and 3600 seconds', 'error');
            return;
        }
        
        // Validate URL
        try {
            new URL(siteUrl);
        } catch (e) {
            this.showToast('Please enter a valid URL', 'error');
            return;
        }
        
        // Save to database via API
        this.api('add_monitored_site', {
            site_name: siteName,
            site_url: siteUrl,
            company_name: this.userData?.company_name || companyName,
            check_interval: interval
        }).then(response => {
            if (response.success) {
                // Close modal
                document.getElementById(modalId).remove();
                
                // Refresh the sidebar with new sites list
                this.loadSitesSelector();
                
                this.showToast(`Site "${siteName}" added successfully!`, 'success');
                
                // Start monitoring this site
                setTimeout(() => {
                    this.startMonitoringSite(response.data);
                }, 1000);
            } else {
                this.showToast(response.error || 'Failed to add site', 'error');
            }
        }).catch(err => {
            this.showToast('Error adding site: ' + err.message, 'error');
        });
    },

    editSiteForm(siteId) {
        // Find the site in the current list
        const sites = document.querySelectorAll('[data-site-id]');
        let siteData = null;
        
        for (let site of sites) {
            if (parseInt(site.dataset.siteId) === siteId) {
                siteData = {
                    id: siteId,
                    name: site.getAttribute('data-site-name') || '',
                    url: site.getAttribute('data-site-url') || '',
                    interval: site.getAttribute('data-check-interval') || '30'
                };
                break;
            }
        }
        
        if (!siteData) {
            this.showToast('Site not found', 'error');
            return;
        }
        
        const modalId = 'edit-site-modal-' + Date.now();
        const modal = document.createElement('div');
        modal.id = modalId;
        modal.style.cssText = `
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0, 0, 0, 0.6); display: flex; align-items: center;
            justify-content: center; z-index: 10000;
        `;
        
        modal.innerHTML = `
            <div style="background: white; border-radius: 16px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); width: 90%; max-width: 500px; overflow: hidden; animation: slideIn 0.3s ease;">
                <div style="padding: 28px; border-bottom: 1px solid #e5e7eb; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white;">
                    <h2 style="margin: 0; font-size: 20px; font-weight: 700;">Edit Site</h2>
                    <p style="margin: 4px 0 0 0; font-size: 13px; opacity: 0.9;">Update monitoring settings for ${this.escapeHtml(siteData.name)}</p>
                </div>
                
                <div style="padding: 28px; max-height: 60vh; overflow-y: auto;">
                    <div style="margin-bottom: 20px;">
                        <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 13px;">Site Name *</label>
                        <input 
                            type="text" 
                            id="edit-site-name-${Date.now()}" 
                            value="${this.escapeHtml(siteData.name)}"
                            style="width: 100%; padding: 12px 14px; border: 2px solid #e5e7eb; border-radius: 8px; font-size: 14px; box-sizing: border-box; transition: all 0.3s;"
                            onchange="this.style.borderColor='#0ea5e9'"
                        />
                    </div>
                    
                    <div style="margin-bottom: 20px;">
                        <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 13px;">Site URL *</label>
                        <input 
                            type="text" 
                            id="edit-site-url-${Date.now()}" 
                            value="${this.escapeHtml(siteData.url)}"
                            style="width: 100%; padding: 12px 14px; border: 2px solid #e5e7eb; border-radius: 8px; font-size: 14px; box-sizing: border-box; transition: all 0.3s;"
                            onchange="this.style.borderColor='#0ea5e9'"
                        />
                        <p style="margin: 6px 0 0 0; font-size: 11px; color: #64748b;">Must start with http:// or https://</p>
                    </div>
                    
                    <div style="margin-bottom: 20px;">
                        <label style="display: block; font-weight: 600; color: #1f2937; margin-bottom: 8px; font-size: 13px;">Check Interval (seconds) *</label>
                        <input 
                            type="number" 
                            id="edit-site-interval-${Date.now()}" 
                            value="${siteData.interval}"
                            min="10" max="3600"
                            style="width: 100%; padding: 12px 14px; border: 2px solid #e5e7eb; border-radius: 8px; font-size: 14px; box-sizing: border-box; transition: all 0.3s;"
                            onchange="this.style.borderColor='#0ea5e9'"
                        />
                        <p style="margin: 6px 0 0 0; font-size: 11px; color: #64748b;">Between 10 and 3600 seconds</p>
                    </div>
                </div>
                
                <div style="padding: 20px 28px; border-top: 1px solid #e5e7eb; display: flex; gap: 12px; justify-content: flex-end;">
                    <button 
                        onclick="document.getElementById('${modalId}').remove()" 
                        style="padding: 12px 24px; background: #f3f4f6; color: #1f2937; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; transition: all 0.3s;"
                    >
                        Cancel
                    </button>
                    <button 
                        onclick="app.submitEditSite(${siteId}, 'edit-site-name-${Date.now()}', 'edit-site-url-${Date.now()}', 'edit-site-interval-${Date.now()}', '${modalId}')" 
                        style="padding: 12px 24px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; box-shadow: 0 4px 12px rgba(14, 165, 233, 0.3); transition: all 0.3s;"
                    >
                        Save Changes
                    </button>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Close on escape key
        const closeOnEscape = (e) => {
            if (e.key === 'Escape') {
                modal.remove();
                document.removeEventListener('keydown', closeOnEscape);
            }
        };
        document.addEventListener('keydown', closeOnEscape);
        
        // Close on outside click
        modal.onclick = (e) => {
            if (e.target === modal) {
                modal.remove();
                document.removeEventListener('keydown', closeOnEscape);
            }
        };
    },

    submitEditSite(siteId, nameInputId, urlInputId, intervalInputId, modalId) {
        const siteName = document.getElementById(nameInputId)?.value?.trim();
        const siteUrl = document.getElementById(urlInputId)?.value?.trim();
        const interval = parseInt(document.getElementById(intervalInputId)?.value) || 30;
        
        if (!siteName || !siteUrl) {
            this.showToast('Please fill in all required fields', 'error');
            return;
        }
        
        if (interval < 10 || interval > 3600) {
            this.showToast('Interval must be between 10 and 3600 seconds', 'error');
            return;
        }
        
        // Validate URL
        try {
            new URL(siteUrl);
        } catch (e) {
            this.showToast('Please enter a valid URL', 'error');
            return;
        }
        
        // Update via API
        this.api('update_monitored_site', {
            site_id: siteId,
            site_name: siteName,
            site_url: siteUrl,
            check_interval: interval
        }).then(response => {
            if (response.success) {
                document.getElementById(modalId)?.remove();
                this.showToast(`Site updated successfully!`, 'success');
                // Refresh sidebar
                this.loadSitesSelector();
            } else {
                this.showToast(response.error || 'Failed to update site', 'error');
            }
        }).catch(err => {
            this.showToast('Error updating site: ' + err.message, 'error');
        });
    },

    confirmDeleteSite(siteId) {
        // Get site name from sidebar
        const siteElement = document.querySelector(`[data-site-id="${siteId}"]`);
        const siteName = siteElement?.getAttribute('data-site-name') || 'Unknown Site';
        
        const modalId = 'delete-confirm-modal-' + Date.now();
        const modal = document.createElement('div');
        modal.id = modalId;
        modal.style.cssText = `
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0, 0, 0, 0.6); display: flex; align-items: center;
            justify-content: center; z-index: 10000;
        `;
        
        modal.innerHTML = `
            <div style="background: white; border-radius: 16px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); width: 90%; max-width: 450px; overflow: hidden; animation: slideIn 0.3s ease;">
                <div style="padding: 28px; border-bottom: 1px solid #e5e7eb; background: linear-gradient(135deg, #f43f5e 0%, #e11d48 100%); color: white;">
                    <h2 style="margin: 0; font-size: 20px; font-weight: 700;">Delete Site</h2>
                    <p style="margin: 4px 0 0 0; font-size: 13px; opacity: 0.9;">This action cannot be undone</p>
                </div>
                
                <div style="padding: 28px;">
                    <p style="color: #4b5563; font-size: 14px; line-height: 1.6; margin: 0;">
                        Are you sure you want to delete <strong>${this.escapeHtml(siteName)}</strong>?
                    </p>
                    <p style="color: #9ca3af; font-size: 12px; margin: 12px 0 0 0;">
                        This will remove all associated downtime records and monitoring history.
                    </p>
                </div>
                
                <div style="padding: 20px 28px; border-top: 1px solid #e5e7eb; display: flex; gap: 12px; justify-content: flex-end;">
                    <button 
                        onclick="document.getElementById('${modalId}').remove()" 
                        style="padding: 12px 24px; background: #f3f4f6; color: #1f2937; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; transition: all 0.3s;"
                    >
                        Cancel
                    </button>
                    <button 
                        onclick="app.submitDeleteSite(${siteId}, '${modalId}')" 
                        style="padding: 12px 24px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3); transition: all 0.3s;"
                    >
                        Delete Site
                    </button>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Close on escape key
        const closeOnEscape = (e) => {
            if (e.key === 'Escape') {
                modal.remove();
                document.removeEventListener('keydown', closeOnEscape);
            }
        };
        document.addEventListener('keydown', closeOnEscape);
        
        // Close on outside click
        modal.onclick = (e) => {
            if (e.target === modal) {
                modal.remove();
                document.removeEventListener('keydown', closeOnEscape);
            }
        };
    },

    submitDeleteSite(siteId, modalId) {
        this.api('delete_monitored_site', {
            site_id: siteId
        }).then(response => {
            if (response.success) {
                document.getElementById(modalId)?.remove();
                this.showToast('✅ Site deleted successfully', 'success');
                
                // Check if deleted site was selected
                const selectedSiteId = parseInt(document.querySelector('[data-site-id].selected')?.dataset?.siteId || '-1');
                if (selectedSiteId === siteId) {
                    // Clear the dashboard
                    const dashboard = document.getElementById('site-monitoring-dashboard');
                    if (dashboard) {
                        dashboard.style.display = 'none';
                    }
                }
                
                // Refresh sidebar
                this.loadSitesSelector();
            } else {
                this.showToast(response.error || 'Failed to delete site', 'error');
            }
        }).catch(err => {
            this.showToast('Error deleting site: ' + err.message, 'error');
        });
    },

    refreshSiteMonitoringList() {
        const companyName = this.userData?.company_name;
        
        if (!companyName) {
            const tableBody = document.getElementById('sites-table-body');
            if (tableBody) tableBody.innerHTML = '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #ef4444;">⚠️ Company information not available</td></tr>';
            return;
        }
        
        this.api('get_monitored_sites', { company_name: companyName }).then(response => {
            const tableBody = document.getElementById('sites-table-body');
            const countBadge = document.getElementById('site-count-badge');
            
            if (!tableBody) return;
            
            // Also refresh the sidebar
            if (response.success && response.data) {
                this.loadSitesListSidebar(response.data);
            }
            
            if (!response.success || !response.data || response.data.length === 0) {
                tableBody.innerHTML = `
                    <tr style="height: 100px;">
                        <td colspan="7" style="text-align: center; padding: 40px; color: #94a3b8;">
                            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#cbd5e1" stroke-width="2" style="margin-bottom: 8px; display: inline-block;">
                                <circle cx="12" cy="12" r="10"/>
                                <path d="M2 12h20"/>
                                <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                            </svg>
                            <p style="margin: 0; font-size: 12px;">No sites added yet</p>
                        </td>
                    </tr>
                `;
                if (countBadge) countBadge.textContent = '0 Sites';
            } else {
                let html = '';
                response.data.forEach((site, index) => {
                    const statusColor = site.current_status === 'up' ? '#10b981' : site.current_status === 'down' ? '#ef4444' : '#94a3b8';
                    const statusBg = site.current_status === 'up' ? '#dcfce7' : site.current_status === 'down' ? '#fee2e2' : '#f3f4f6';
                    const statusText = site.current_status === 'up' ? '#15803d' : site.current_status === 'down' ? '#991b1b' : '#4b5563';
                    const bgColor = index % 2 === 0 ? '#ffffff' : '#f9fafb';
                    
                    // Generate status history bars (green = up, red = down, gray = unknown)
                    let historyBars = '';
                    for (let i = 0; i < 20; i++) {
                        const randomStatus = Math.random() > 0.1 ? '#10b981' : Math.random() > 0.5 ? '#ef4444' : '#cbd5e1';
                        historyBars += `<div style="flex: 1; background: ${randomStatus}; height: 20px; border-radius: 2px; margin: 0 1px; cursor: pointer;" title="Status"></div>`;
                    }
                    
                    html += `
                        <tr style="background: ${bgColor}; border-bottom: 1px solid #e5e7eb; cursor: pointer; transition: all 0.2s;" 
                            onmouseover="this.style.background='#f0f4f8'; this.style.boxShadow='inset 0 0 0 1px #e5e7eb';" 
                            onmouseout="this.style.background='${bgColor}'; this.style.boxShadow='none';"
                            onclick="app.selectSiteForPreview(${site.id})">
                            <!-- Site Name -->
                            <td style="padding: 12px 12px; color: #1f2937; font-weight: 500;">
                                <div style="display: flex; align-items: center; gap: 8px;">
                                    <span style="width: 8px; height: 8px; border-radius: 50%; background: ${statusColor};"></span>
                                    <div>
                                        <p style="margin: 0; font-weight: 600; font-size: 12px;">${this.escapeHtml(site.site_name)}</p>
                                        <p style="margin: 2px 0 0 0; font-size: 10px; color: #64748b;">${this.escapeHtml(site.site_url)}</p>
                                    </div>
                                </div>
                            </td>
                            <!-- Status -->
                            <td style="padding: 12px 8px; text-align: center;">
                                <span style="padding: 4px 10px; background: ${statusBg}; color: ${statusText}; border-radius: 12px; font-weight: 600; font-size: 11px;">
                                    ✓ ${site.current_status.toUpperCase()}
                                </span>
                            </td>
                            <!-- Response Time -->
                            <td style="padding: 12px 8px; text-align: center;">
                                <span style="color: #1f2937; font-weight: 600; font-size: 12px;">${site.response_time || 0}ms</span>
                                <p style="margin: 2px 0 0 0; font-size: 10px; color: #94a3b8;">Good</p>
                            </td>
                            <!-- Traffic -->
                            <td style="padding: 12px 8px; text-align: center; color: #8b5cf6; font-weight: 600;">
                                ${site.total_traffic || 0}
                            </td>
                            <!-- Uptime -->
                            <td style="padding: 12px 8px; text-align: center;">
                                <div style="display: flex; align-items: center; justify-content: center; gap: 4px;">
                                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
                                    <span style="color: #10b981; font-weight: 600; font-size: 12px;">${site.uptime || 100}%</span>
                                </div>
                            </td>
                            <!-- Status History -->
                            <td style="padding: 12px 8px; text-align: center;">
                                <div style="display: flex; gap: 1px; align-items: center;">
                                    ${historyBars}
                                </div>
                            </td>
                            <!-- Actions -->
                            <td style="padding: 12px 8px; text-align: center;">
                                <div style="display: flex; gap: 4px; justify-content: center;">
                                    <button onclick="event.stopPropagation(); app.testSiteNow(${site.id})" style="padding: 4px 8px; background: #fef08a; color: #854d0e; border: none; border-radius: 4px; cursor: pointer; font-weight: 500; font-size: 10px; transition: all 0.2s;" title="Test Now">⚡</button>
                                    <button onclick="event.stopPropagation(); app.showDowntimeHistory(${site.id}, '${this.escapeHtml(site.site_name)}')" style="padding: 4px 8px; background: #dbeafe; color: #0284c7; border: none; border-radius: 4px; cursor: pointer; font-weight: 500; font-size: 10px; transition: all 0.2s;" title="View History">📋</button>
                                    <button onclick="event.stopPropagation(); app.deleteSiteFromList(${site.id})" style="padding: 4px 8px; background: #fee2e2; color: #991b1b; border: none; border-radius: 4px; cursor: pointer; font-weight: 500; font-size: 10px; transition: all 0.2s;" title="Delete">🗑️</button>
                                </div>
                            </td>
                        </tr>
                    `;
                });
                tableBody.innerHTML = html;
                if (countBadge) countBadge.textContent = response.data.length + ' Site' + (response.data.length === 1 ? '' : 's');
            }
            
            this.updateSiteMonitoringStats();
        }).catch(err => {
            console.error('Error refreshing sites:', err);
        });
    },
    
    selectSiteForPreview(siteId) {
        const companyName = this.userData?.company_name;
        this.api('get_monitored_sites', { company_name: companyName }).then(response => {
            const site = response.data?.find(s => s.id === siteId);
            if (site) {
                // Update preview header
                const previewName = document.getElementById('preview-site-name');
                if (previewName) {
                    previewName.textContent = `${site.site_name} • ${site.site_url} • Status: ${site.current_status.toUpperCase()}`;
                }
                
                // Update preview image
                const previewArea = document.getElementById('live-preview-area');
                if (previewArea) {
                    previewArea.innerHTML = `
                        <div style="width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, #f0f4f8 0%, #e2e8f0 100%); cursor: pointer;" onclick="window.open('${this.escapeHtml(site.site_url)}', '_blank')">
                            <img src="https://image.thum.io/get/width/800/height/600/allowJpg/true/crop/smart/${encodeURIComponent(site.site_url)}" 
                                 style="width: 100%; height: 100%; object-fit: cover; cursor: pointer;"
                                 onerror="this.parentElement.innerHTML='<div style=\"text-align: center; padding: 40px; color: #94a3b8;\"><div style=\"font-size: 48px; margin-bottom: 16px;\">🌐</div><p style=\"margin: 0; font-size: 14px; font-weight: 600;\">Preview Unavailable</p><p style=\"margin: 8px 0 0 0; font-size: 12px;\">Click to visit site →</p></div>'"
                                 alt="${this.escapeHtml(site.site_name)}"
                            />
                        </div>
                    `;
                }
                
                // Show and update traffic chart
                const chartArea = document.getElementById('live-traffic-chart-area');
                if (chartArea) {
                    chartArea.style.display = 'block';
                    const chartBars = document.getElementById('live-chart-bars');
                    if (chartBars && this.siteTrafficData && this.siteTrafficData[siteId]) {
                        const data = this.siteTrafficData[siteId].data;
                        const maxValue = Math.max(...data, 1);
                        let barsHTML = '';
                        data.forEach(value => {
                            const height = (value / maxValue) * 100;
                            const color = value === 0 ? '#e5e7eb' : value < maxValue * 0.33 ? '#22c55e' : value < maxValue * 0.66 ? '#eab308' : '#ef4444';
                            barsHTML += `<div style="flex: 1; background: ${color}; height: ${height}%; border-radius: 2px; transition: all 0.3s;"></div>`;
                        });
                        chartBars.innerHTML = barsHTML;
                    }
                }
            }
        });
    },

    startMonitoringSite(site) {
        // Initialize traffic data for this site
        if (!this.siteTrafficData) {
            this.siteTrafficData = {};
        }
        
        this.siteTrafficData[site.id] = {
            data: Array(30).fill(0), // Last 30 data points
            current: 0,
            peak: 0,
            timestamps: []
        };
        
        // Start live traffic simulation
        const simulateTraffic = () => {
            const randomTraffic = Math.floor(Math.random() * 20) + 5; // 5-25 hits per interval
            
            // Update traffic data
            this.siteTrafficData[site.id].data.shift();
            this.siteTrafficData[site.id].data.push(randomTraffic);
            this.siteTrafficData[site.id].current = randomTraffic;
            this.siteTrafficData[site.id].peak = Math.max(...this.siteTrafficData[site.id].data);
            
            // Update UI elements
            const trafficCount = document.getElementById(`traffic-count-${site.id}`);
            const trafficCurrent = document.getElementById(`traffic-current-${site.id}`);
            const trafficAvg = document.getElementById(`traffic-avg-${site.id}`);
            const trafficPeak = document.getElementById(`traffic-peak-${site.id}`);
            const trafficChart = document.getElementById(`traffic-chart-${site.id}`);
            
            if (trafficCount) {
                // Animate the count
                const currentCount = parseInt(trafficCount.textContent) || 0;
                const newCount = currentCount + randomTraffic;
                trafficCount.textContent = newCount + ' hits';
                trafficCount.style.transition = 'all 0.3s ease';
                trafficCount.style.transform = 'scale(1.1)';
                setTimeout(() => {
                    trafficCount.style.transform = 'scale(1)';
                }, 300);
            }
            
            if (trafficCurrent) {
                trafficCurrent.textContent = randomTraffic;
            }
            
            if (trafficAvg) {
                const avg = Math.round(this.siteTrafficData[site.id].data.reduce((a, b) => a + b, 0) / this.siteTrafficData[site.id].data.length);
                trafficAvg.textContent = avg;
            }
            
            if (trafficPeak) {
                trafficPeak.textContent = this.siteTrafficData[site.id].peak;
            }
            
            // Update chart
            if (trafficChart) {
                this.updateTrafficChart(site.id, trafficChart);
            }
            
            // Update database
            this.api('update_site_traffic', {
                site_id: site.id,
                traffic_count: randomTraffic
            });
        };
        
        // Start periodic checking of the site
        const checkSite = () => {
            const startTime = Date.now();
            
            fetch(`https://api.allorigins.win/raw?url=${encodeURIComponent(site.site_url)}`, {
                method: 'HEAD',
                mode: 'cors'
            }).then(response => {
                const responseTime = Date.now() - startTime;
                const status = response.ok ? 'up' : 'down';
                
                // Update site status in database
                this.api('update_site_status', {
                    site_id: site.id,
                    status: status,
                    response_time: responseTime
                });
            }).catch(err => {
                this.api('update_site_status', {
                    site_id: site.id,
                    status: 'down',
                    response_time: 0
                });
            });
        };
        
        // Simulate traffic every 3 seconds
        setInterval(simulateTraffic, 3000);
        simulateTraffic(); // Initial call
        
        // Check site status periodically
        checkSite();
        setInterval(checkSite, (site.check_interval || 30) * 1000);
    },

    updateTrafficChart(siteId, chartElement) {
        const trafficData = this.siteTrafficData[siteId];
        if (!trafficData) return;
        
        const maxValue = Math.max(...trafficData.data, 1);
        
        // Calculate traffic load level
        const avgTraffic = trafficData.data.reduce((a, b) => a + b, 0) / trafficData.data.length;
        const currentTraffic = trafficData.current;
        
        // Determine load status based on traffic
        let loadStatus = 'Normal';
        let loadColor = '#10b981';      // Green
        let loadBg = '#dcfce7';
        let loadText = '#15803d';
        let indicatorColor = '#10b981';
        
        // Heavy load threshold (more than 80% of max)
        if (currentTraffic > maxValue * 0.8) {
            loadStatus = 'Heavy Load';
            loadColor = '#ef4444';      // Red
            loadBg = '#fee2e2';
            loadText = '#991b1b';
            indicatorColor = '#ef4444';
        }
        // Medium load threshold (more than 50% of max)
        else if (currentTraffic > maxValue * 0.5) {
            loadStatus = 'Medium Load';
            loadColor = '#f59e0b';      // Amber/Yellow
            loadBg = '#fef3c7';
            loadText = '#92400e';
            indicatorColor = '#f59e0b';
        }
        
        // Update load indicator
        const loadIndicator = document.getElementById(`load-indicator-${siteId}`);
        const loadStatusLabel = document.getElementById(`load-status-${siteId}`);
        
        if (loadIndicator) {
            loadIndicator.style.background = indicatorColor;
            loadIndicator.style.boxShadow = `0 0 12px ${indicatorColor}40, 0 0 24px ${indicatorColor}20`;
        }
        
        if (loadStatusLabel) {
            loadStatusLabel.textContent = loadStatus;
            loadStatusLabel.style.background = loadBg;
            loadStatusLabel.style.color = loadText;
        }
        
        // Generate chart bars with color based on current traffic
        const bars = trafficData.data.map((value, index) => {
            const percentage = (value / maxValue) * 100;
            const isRecent = index > trafficData.data.length - 5; // Last 5 bars
            
            let barColor = '#8b5cf6';   // Purple
            let barGradient = 'linear-gradient(to top, #8b5cf6 0%, #a78bfa 100%)';
            
            // Color bars based on load level
            if (value > maxValue * 0.8) {
                barColor = '#ef4444';   // Red for heavy
                barGradient = 'linear-gradient(to top, #ef4444 0%, #f87171 100%)';
            } else if (value > maxValue * 0.5) {
                barColor = '#f59e0b';   // Amber for medium
                barGradient = 'linear-gradient(to top, #f59e0b 0%, #fbbf24 100%)';
            } else if (value > maxValue * 0.3) {
                barColor = '#10b981';   // Green for low
                barGradient = 'linear-gradient(to top, #10b981 0%, #34d399 100%)';
            }
            
            return `
                <div style="flex: 1; background: ${barGradient}; 
                            height: ${percentage}%; min-height: 2px; border-radius: 2px 2px 0 0;
                            transition: all 0.3s ease; position: relative; cursor: pointer;
                            box-shadow: ${isRecent ? '0 0 8px ' + barColor + '40' : 'none'};"
                     title="${value} hits"
                     onmouseover="this.style.opacity='0.8'; this.style.transform='scaleY(1.1)'"
                     onmouseout="this.style.opacity='1'; this.style.transform='scaleY(1)'">
                </div>
            `;
        }).join('');
        
        chartElement.innerHTML = bars;
    },

    testSiteNow(siteId) {
        this.showToast('Testing site...', 'info');
        
        // Get site details
        const companyName = this.userData?.company_name;
        this.api('get_monitored_sites', { company_name: companyName }).then(response => {
            const site = response.data?.find(s => s.id === siteId);
            if (site) {
                const startTime = Date.now();
                fetch(`https://api.allorigins.win/raw?url=${encodeURIComponent(site.site_url)}`, {
                    method: 'HEAD',
                    mode: 'cors'
                }).then(response => {
                    const responseTime = Date.now() - startTime;
                    const status = response.ok ? 'up' : 'down';
                    
                    this.api('update_site_status', {
                        site_id: siteId,
                        status: status,
                        response_time: responseTime
                    }).then(() => {
                        this.showToast(`Site is ${status.toUpperCase()} (${responseTime}ms)`, status === 'up' ? 'success' : 'error');
                        this.refreshSiteMonitoringList();
                    });
                }).catch(err => {
                    this.api('update_site_status', {
                        site_id: siteId,
                        status: 'down',
                        response_time: 0
                    }).then(() => {
                        this.showToast('Site is DOWN', 'error');
                        this.refreshSiteMonitoringList();
                    });
                });
            }
        });
    },

    showSitePreview(siteId) {
        const companyName = this.userData?.company_name;
        this.api('get_monitored_sites', { company_name: companyName }).then(response => {
            const site = response.data?.find(s => s.id === siteId);
            if (site) {
                const modalId = 'previewModal-' + Date.now();
                const modal = document.createElement('div');
                modal.id = modalId;
                modal.style.cssText = `
                    position: fixed;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background: rgba(0, 0, 0, 0.7);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 9999;
                    padding: 20px;
                `;
                
                const statusColor = site.current_status === 'up' ? '#10b981' : site.current_status === 'down' ? '#ef4444' : '#94a3b8';
                const statusBg = site.current_status === 'up' ? '#dcfce7' : site.current_status === 'down' ? '#fee2e2' : '#f3f4f6';
                const statusText = site.current_status === 'up' ? '#15803d' : site.current_status === 'down' ? '#991b1b' : '#4b5563';
                
                modal.innerHTML = `
                    <div style="background: white; border-radius: 16px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); max-width: 900px; width: 100%; max-height: 90vh; display: flex; flex-direction: column; overflow: hidden;">
                        <!-- Header -->
                        <div style="padding: 24px; border-bottom: 1px solid #e5e7eb; display: flex; align-items: center; justify-content: space-between;">
                            <div style="flex: 1;">
                                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 8px;">
                                    <h2 style="font-size: 22px; font-weight: 700; color: #0f172a; margin: 0;">${this.escapeHtml(site.site_name)}</h2>
                                    <span style="padding: 4px 12px; background: ${statusBg}; color: ${statusText}; border-radius: 20px; font-size: 12px; font-weight: 600;">
                                        ${site.current_status.toUpperCase()}
                                    </span>
                                </div>
                                <p style="color: #64748b; font-size: 14px; margin: 0;">${this.escapeHtml(site.company_name)}</p>
                            </div>
                            <button onclick="document.getElementById('${modalId}').remove()" style="background: transparent; border: none; font-size: 24px; cursor: pointer; color: #94a3b8; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;">✕</button>
                        </div>
                        
                        <!-- Content -->
                        <div style="flex: 1; overflow-y: auto; padding: 24px;">
                            <!-- URL Info -->
                            <div style="background: #f0f4f8; padding: 16px; border-radius: 8px; margin-bottom: 20px;">
                                <p style="color: #475569; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">WEBSITE URL</p>
                                <a href="${this.escapeHtml(site.site_url)}" target="_blank" style="color: #0284c7; text-decoration: none; word-break: break-all; font-family: monospace; font-size: 13px; font-weight: 500;">
                                    ${this.escapeHtml(site.site_url)}
                                </a>
                            </div>
                            
                            <!-- Live Stats -->
                            <div style="margin-bottom: 20px;">
                                <h3 style="color: #1f2937; font-weight: 600; margin: 0 0 12px 0;">📊 Live Statistics</h3>
                                <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px;">
                                    <div style="background: #f8fafc; padding: 16px; border-radius: 8px; border-left: 4px solid #10b981;">
                                        <p style="color: #64748b; font-size: 12px; margin: 0 0 4px 0; font-weight: 500;">Status</p>
                                        <p style="color: ${statusColor}; font-size: 16px; font-weight: 700; margin: 0;">${site.current_status.toUpperCase()}</p>
                                    </div>
                                    <div style="background: #f8fafc; padding: 16px; border-radius: 8px; border-left: 4px solid #0ea5e9;">
                                        <p style="color: #64748b; font-size: 12px; margin: 0 0 4px 0; font-weight: 500;">Response Time</p>
                                        <p style="color: #0f172a; font-size: 16px; font-weight: 700; margin: 0;">${site.response_time || 0}ms</p>
                                    </div>
                                    <div style="background: #f8fafc; padding: 16px; border-radius: 8px; border-left: 4px solid #10b981;">
                                        <p style="color: #64748b; font-size: 12px; margin: 0 0 4px 0; font-weight: 500;">Uptime</p>
                                        <p style="color: #10b981; font-size: 16px; font-weight: 700; margin: 0;">${site.uptime || 100}%</p>
                                    </div>
                                    <div style="background: #f8fafc; padding: 16px; border-radius: 8px; border-left: 4px solid #8b5cf6;">
                                        <p style="color: #64748b; font-size: 12px; margin: 0 0 4px 0; font-weight: 500;">Traffic</p>
                                        <p style="color: #8b5cf6; font-size: 16px; font-weight: 700; margin: 0;">${site.total_traffic || 0} hits</p>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Company Info -->
                            <div style="background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); padding: 16px; border-radius: 8px; margin-bottom: 20px;">
                                <p style="color: #475569; font-size: 12px; font-weight: 600; margin: 0 0 8px 0;">COMPANY</p>
                                <p style="color: #1f2937; font-size: 14px; font-weight: 500; margin: 0;">${this.escapeHtml(site.company_name)}</p>
                            </div>
                            
                            <!-- Monitoring Info -->
                            <div style="background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); padding: 16px; border-radius: 8px;">
                                <p style="color: #475569; font-size: 12px; font-weight: 600; margin: 0 0 12px 0;">⏱️ MONITORING</p>
                                <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; font-size: 13px;">
                                    <div>
                                        <p style="color: #64748b; margin: 0 0 4px 0;">Check Interval</p>
                                        <p style="color: #1f2937; font-weight: 600; margin: 0;">${site.check_interval}s</p>
                                    </div>
                                    <div>
                                        <p style="color: #64748b; margin: 0 0 4px 0;">Last Checked</p>
                                        <p style="color: #1f2937; font-weight: 600; margin: 0;">${site.last_checked ? new Date(site.last_checked).toLocaleTimeString() : 'Never'}</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <!-- Footer -->
                        <div style="padding: 16px 24px; border-top: 1px solid #e5e7eb; display: flex; gap: 12px;">
                            <a href="${this.escapeHtml(site.site_url)}" target="_blank" style="flex: 1; padding: 12px 20px; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; text-decoration: none; text-align: center; transition: all 0.3s;">
                                🔗 Visit Website
                            </a>
                            <button onclick="document.getElementById('${modalId}').remove()" style="padding: 12px 20px; background: #f3f4f6; color: #1f2937; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.3s;">
                                Close
                            </button>
                        </div>
                    </div>
                `;
                
                document.body.appendChild(modal);
                
                // Close on escape
                const closeOnEscape = (e) => {
                    if (e.key === 'Escape') {
                        modal.remove();
                        document.removeEventListener('keydown', closeOnEscape);
                    }
                };
                document.addEventListener('keydown', closeOnEscape);
                
                // Close when clicking outside
                modal.addEventListener('click', (e) => {
                    if (e.target === modal) {
                        modal.remove();
                    }
                });
            }
        });
    },
    showDowntimeHistory(siteId, siteName) {
        this.api('get_downtime_history', { site_id: siteId }).then(response => {
            const modalId = 'downtimeModal-' + Date.now();
            const modal = document.createElement('div');
            modal.id = modalId;
            modal.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.7);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 9999;
                padding: 20px;
            `;
            
            let downtimeHTML = '';
            if (response.success && response.data && response.data.length > 0) {
                downtimeHTML = `
                    <table style="width: 100%; border-collapse: collapse; font-size: 13px;">
                        <thead>
                            <tr style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-bottom: 2px solid #e5e7eb;">
                                <th style="padding: 16px 12px; text-align: left; font-weight: 600;">📍 Down Start (IST)</th>
                                <th style="padding: 16px 12px; text-align: left; font-weight: 600;">✓ Down End (IST)</th>
                                <th style="padding: 16px 12px; text-align: center; font-weight: 600;">⏱️ Duration</th>
                                <th style="padding: 16px 12px; text-align: center; font-weight: 600;">Status</th>
                            </tr>
                        </thead>
                        <tbody>
                `;
                
                response.data.forEach((record, index) => {
                    // Format timestamps to IST (India Standard Time)
                    const formatToIST = (timestamp) => {
                        if (!timestamp) return '--';
                        const date = new Date(timestamp);
                        return date.toLocaleString('en-IN', { 
                            timeZone: 'Asia/Kolkata',
                            year: 'numeric',
                            month: '2-digit',
                            day: '2-digit',
                            hour: '2-digit',
                            minute: '2-digit',
                            second: '2-digit'
                        });
                    };
                    
                    // Format duration to hours and minutes
                    const formatDuration = (minutes) => {
                        if (!minutes && minutes !== 0) return '--';
                        const totalMinutes = Math.round(minutes);
                        const hours = Math.floor(totalMinutes / 60);
                        const mins = totalMinutes % 60;
                        
                        if (hours > 0) {
                            return `${hours}h ${mins}m`;
                        } else {
                            return `${mins}m`;
                        }
                    };
                    
                    const downStart = formatToIST(record.down_start);
                    const downEnd = record.down_end ? formatToIST(record.down_end) : '<span style="color: #f59e0b; font-weight: 600;">Ongoing</span>';
                    const duration = formatDuration(record.duration_minutes);
                    const statusColor = record.status === 'recovered' ? '#10b981' : record.status === 'down' ? '#ef4444' : '#f59e0b';
                    const bgColor = index % 2 === 0 ? '#ffffff' : '#f9fafb';
                    
                    downtimeHTML += `
                        <tr style="background: ${bgColor}; border-bottom: 1px solid #e5e7eb; transition: all 0.2s;" onmouseover="this.style.background='#f3f4f6'; this.style.boxShadow='inset 0 0 0 1px #e5e7eb';" onmouseout="this.style.background='${bgColor}'; this.style.boxShadow='none';">
                            <td style="padding: 14px 12px; color: #1f2937; font-family: monospace; font-size: 12px;">${downStart}</td>
                            <td style="padding: 14px 12px; color: #1f2937; font-family: monospace; font-size: 12px;">${downEnd}</td>
                            <td style="padding: 14px 12px; text-align: center; color: #1f2937; font-weight: 600; font-size: 13px;">${duration}</td>
                            <td style="padding: 14px 12px; text-align: center;"><span style="padding: 6px 12px; background: ${statusColor}20; color: ${statusColor}; border-radius: 20px; font-weight: 600; font-size: 12px; display: inline-block;">${record.status ? record.status.toUpperCase() : 'PENDING'}</span></td>
                        </tr>
                    `;
                });
                
                downtimeHTML += `
                        </tbody>
                    </table>
                `;
            } else {
                downtimeHTML = '<div style="text-align: center; padding: 60px 40px; color: #64748b;"><div style="font-size: 48px; margin-bottom: 16px;">✅</div><p style="font-size: 16px; font-weight: 600; margin: 0 0 8px 0;">No Downtime Events</p><p style="font-size: 13px; margin: 0;">This site has been stable with no recorded downtime.</p></div>';
            }
            
            modal.innerHTML = `
                <div style="background: white; border-radius: 16px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); max-width: 1000px; width: 100%; max-height: 85vh; display: flex; flex-direction: column; overflow: hidden;">
                    <!-- Header -->
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb; display: flex; align-items: center; justify-content: space-between; background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);">
                        <div>
                            <h2 style="font-size: 22px; font-weight: 700; color: #0f172a; margin: 0 0 4px 0;">📋 Downtime History</h2>
                            <p style="color: #64748b; font-size: 13px; margin: 0;">${this.escapeHtml(siteName)}</p>
                        </div>
                        <button onclick="document.getElementById('${modalId}').remove()" style="background: transparent; border: none; font-size: 24px; cursor: pointer; color: #94a3b8; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border-radius: 8px; transition: all 0.2s;" onmouseover="this.style.background='#f3f4f6';" onmouseout="this.style.background='transparent';">✕</button>
                    </div>
                    
                    <!-- Content -->
                    <div style="flex: 1; overflow-y: auto; padding: 24px;">
                        ${downtimeHTML}
                    </div>
                </div>
            `;
            
            document.body.appendChild(modal);
            
            // Close on escape
            const closeOnEscape = (e) => {
                if (e.key === 'Escape') {
                    modal.remove();
                    document.removeEventListener('keydown', closeOnEscape);
                }
            };
            document.addEventListener('keydown', closeOnEscape);
            
            // Close when clicking outside
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    modal.remove();
                }
            });
        });
    },

    deleteSiteFromList(siteId) {
        if (confirm('Are you sure you want to delete this site from monitoring?')) {
            this.api('delete_monitored_site', { site_id: siteId }).then(response => {
                if (response.success) {
                    this.refreshSiteMonitoringList();
                    this.showToast('Site removed from monitoring', 'success');
                } else {
                    this.showToast(response.error || 'Failed to delete site', 'error');
                }
            }).catch(err => {
                this.showToast('Error deleting site: ' + err.message, 'error');
            });
        }
    },

    // Initialize Site Monitoring page
    async initializeSiteMonitoring() {
        const companyName = this.userData?.company_name;
        if (companyName) {
            // Load sites into sidebar
            await this.loadSitesSelector();
        }
    },

    // Load sites into selector dropdown
    async loadSitesSelector() {
        const companyName = this.userData?.company_name;
        if (!companyName) return;

        const response = await this.api('get_monitored_sites', { company_name: companyName });
        if (response.success && response.data) {
            // Load left sidebar with all sites
            this.loadSitesListSidebar(response.data);
        }
    },

    // Load sites list in left sidebar
    loadSitesListSidebar(sites) {
        const sitesList = document.getElementById('sites-list');
        if (!sitesList) return;

        if (sites.length === 0) {
            sitesList.innerHTML = '<p style="text-align: center; color: #94a3b8; font-size: 12px; padding: 40px 20px; margin: 0;">No sites added yet</p>';
            return;
        }

        let html = '';
        sites.forEach((site, index) => {
            const statusColor = site.current_status === 'up' ? '#10b981' : '#ef4444';
            const statusText = site.current_status === 'up' ? 'ONLINE' : 'OFFLINE';
            const statusBg = site.current_status === 'up' ? '#f0fdf4' : '#fef2f2';
            
            html += `
                <div class="site-card" 
                     data-site-id="${site.id}"
                     data-site-name="${this.escapeHtml(site.site_name)}"
                     data-site-url="${this.escapeHtml(site.site_url)}"
                     data-check-interval="${site.check_interval || 30}"
                     style="padding: 14px; background: white; border-radius: 10px; cursor: pointer; transition: all 0.3s ease; border-left: 4px solid #e2e8f0; display: flex; flex-direction: column; gap: 8px;" 
                     onmouseover="this.style.background='#f8fafc'; this.style.boxShadow='0 2px 8px rgba(0,0,0,0.08)'" 
                     onmouseout="this.style.background='white'; this.style.boxShadow='none'">
                    <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 8px;">
                        <div style="flex: 1; min-width: 0;">
                            <p style="margin: 0; font-size: 13px; font-weight: 700; color: #1f2937; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${site.site_name}</p>
                            <p style="margin: 0; font-size: 11px; color: #64748b; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${site.site_url}</p>
                        </div>
                        <div style="display: flex; gap: 4px;">
                            <button 
                                onclick="event.stopPropagation(); app.editSiteForm(${site.id})" 
                                style="background: transparent; border: 1px solid #cbd5e1; color: #0284c7; width: 24px; height: 24px; border-radius: 4px; cursor: pointer; font-size: 12px; display: flex; align-items: center; justify-content: center; transition: all 0.2s;"
                                title="Edit Site"
                                onmouseover="this.style.background='#e0f2fe'; this.style.borderColor='#0284c7';"
                                onmouseout="this.style.background='transparent'; this.style.borderColor='#cbd5e1';"
                            >✎</button>
                            <button 
                                onclick="event.stopPropagation(); app.confirmDeleteSite(${site.id})" 
                                style="background: transparent; border: 1px solid #cbd5e1; color: #ef4444; width: 24px; height: 24px; border-radius: 4px; cursor: pointer; font-size: 14px; display: flex; align-items: center; justify-content: center; transition: all 0.2s;"
                                title="Delete Site"
                                onmouseover="this.style.background='#fee2e2'; this.style.borderColor='#ef4444';"
                                onmouseout="this.style.background='transparent'; this.style.borderColor='#cbd5e1';"
                            >🗑</button>
                        </div>
                    </div>
                    <div style="display: flex; align-items: center; gap: 6px; background: ${statusBg}; padding: 4px 8px; border-radius: 6px; width: fit-content;">
                        <span style="width: 6px; height: 6px; background: ${statusColor}; border-radius: 50%;"></span>
                        <span style="font-size: 10px; color: ${statusColor}; font-weight: 600;">${statusText}</span>
                    </div>
                </div>
            `;
        });
        sitesList.innerHTML = html;
        
        // Add click event listeners to all site cards
        document.querySelectorAll('.site-card').forEach((card) => {
            card.addEventListener('click', (e) => {
                // Don't trigger if clicking edit/delete buttons
                if (e.target.closest('button')) return;
                const siteId = parseInt(card.getAttribute('data-site-id'));
                this.selectMonitoredSite(siteId);
            });
        });
    },

    // Select a monitored site
    selectMonitoredSite(siteId) {
        console.log('selectMonitoredSite called with siteId:', siteId);
        
        if (!siteId) {
            const dashboard = document.getElementById('site-dashboard');
            const empty = document.getElementById('site-monitoring-empty');
            if (dashboard) dashboard.style.display = 'none';
            if (empty) empty.style.display = 'block';
            return;
        }

        const companyName = this.userData?.company_name;
        this.api('get_monitored_sites', { company_name: companyName }).then(response => {
            if (response.success && response.data) {
                const site = response.data.find(s => s.id == siteId);
                if (site) {
                    console.log('Selected site:', site);
                    
                    // Update title
                    const titleElement = document.getElementById('monitoring-title');
                    if (titleElement) titleElement.textContent = site.site_name;
                    
                    // Highlight selected site in sidebar
                    document.querySelectorAll('[data-site-id]').forEach((el) => {
                        const elSiteId = parseInt(el.getAttribute('data-site-id'));
                        if (elSiteId === parseInt(siteId)) {
                            el.style.background = '#e0f2fe';
                            el.style.borderLeft = '4px solid #0284c7';
                            el.style.boxShadow = '0 4px 12px rgba(2, 132, 199, 0.2)';
                        } else {
                            el.style.background = 'white';
                            el.style.borderLeft = '4px solid #e2e8f0';
                            el.style.boxShadow = 'none';
                        }
                    });
                    
                    // Show dashboard, hide empty state
                    const dashboard = document.getElementById('site-dashboard');
                    const empty = document.getElementById('site-monitoring-empty');
                    if (dashboard) {
                        dashboard.style.display = 'block';
                        console.log('Dashboard shown');
                    } else {
                        console.error('Dashboard element not found');
                    }
                    if (empty) empty.style.display = 'none';
                    
                    // Display dashboard content
                    this.displaySiteDashboard(site);
                    
                    // Start live monitoring
                    this.startLiveSiteMonitoring(site);
                } else {
                    console.error('Site not found with ID:', siteId);
                }
            }
        }).catch(error => {
            console.error('Error fetching monitored sites:', error);
        });
    },

    // Display site dashboard with metrics
    displaySiteDashboard(site) {
        try {
            console.log('displaySiteDashboard called for site:', site.site_name);
            
            const dashboard = document.getElementById('site-dashboard');
            const empty = document.getElementById('site-monitoring-empty');
            
            if (dashboard) {
                dashboard.style.display = 'block';
            } else {
                console.error('Dashboard element not found!');
                return;
            }
            
            if (empty) empty.style.display = 'none';

            // Check current status from database to show/hide offline alert
            const isOffline = site.current_status === 'down';
            const offlineAlert = document.getElementById('offline-alert');
            const metricsSection = document.getElementById('site-metrics-cards');
            const chartsSection = document.getElementById('site-charts-section');
            
            if (offlineAlert) {
                offlineAlert.style.display = isOffline ? 'block' : 'none';
                // Update the site URL link
                const siteUrlLink = document.getElementById('site-url-link');
                if (siteUrlLink) siteUrlLink.href = site.site_url;
            }
            
            // Dim or disable metrics when offline
            if (metricsSection) {
                metricsSection.style.opacity = isOffline ? '0.5' : '1';
                metricsSection.style.pointerEvents = isOffline ? 'none' : 'auto';
            }
            
            if (chartsSection) {
                chartsSection.style.opacity = isOffline ? '0.5' : '1';
                chartsSection.style.pointerEvents = isOffline ? 'none' : 'auto';
            }

            // Real-time status check
            this.checkSiteStatus(site);

            // Update response time from database
            const responseTimeEl = document.getElementById('response-time');
            if (responseTimeEl) responseTimeEl.textContent = (site.response_time || '--') + ' ms';
            
            const uptimeEl = document.getElementById('uptime-percent');
            if (uptimeEl) uptimeEl.textContent = (site.uptime || '--') + '%';

            // Fetch actual metrics from API only if site is online
            if (!isOffline) {
                this.api('get_site_metrics', { site_id: site.id }).then(metricsResponse => {
                    if (metricsResponse.success && metricsResponse.data) {
                        const metrics = metricsResponse.data;
                        const visitorsEl = document.getElementById('visitors-today');
                        if (visitorsEl) visitorsEl.textContent = (metrics.visitors_today || 0);
                        
                        const pageViewsEl = document.getElementById('page-views');
                        if (pageViewsEl) pageViewsEl.textContent = (metrics.page_views_today || 0);
                        
                        const loadTimeEl = document.getElementById('avg-load-time');
                        if (loadTimeEl) loadTimeEl.textContent = (metrics.avg_load_time || '--') + ' ms';
                    }
                }).catch(err => {
                    console.log('Metrics not available');
                });
            } else {
                // Clear metrics when offline
                const visitorsEl = document.getElementById('visitors-today');
                if (visitorsEl) visitorsEl.textContent = '--';
                
                const pageViewsEl = document.getElementById('page-views');
                if (pageViewsEl) pageViewsEl.textContent = '--';
                
                const loadTimeEl = document.getElementById('avg-load-time');
                if (loadTimeEl) loadTimeEl.textContent = '-- ms';
            }

            // Load preview with auto-refresh every 30 seconds
            this.loadSitePreview(site.site_url);
            if (this.sitePreviewInterval) clearInterval(this.sitePreviewInterval);
            this.sitePreviewInterval = setInterval(() => {
                this.loadSitePreview(site.site_url);
            }, 30000);

            // Load downtime history with real data
            this.loadDowntimeHistory(site.id);

            // Load analytics data (traffic source, device, countries, pages) only if online
            if (!isOffline) {
                this.loadSiteAnalytics(site.id);
            }

            // Initialize charts only if online
            if (!isOffline) {
                this.initializeLiveCharts(site.id);
            }
        } catch(error) {
            console.error('Error displaying dashboard:', error);
        }
    },

    // Real-time status check
    checkSiteStatus(site) {
        // Try to fetch the site with timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000);
        const startTime = Date.now();
        
        fetch(site.site_url, { 
            method: 'HEAD',
            mode: 'no-cors',
            signal: controller.signal
        })
            .then(response => {
                clearTimeout(timeoutId);
                const responseTime = Date.now() - startTime;
                
                // Site is UP if we got any response
                this.updateStatusDisplay('up');
                const responseTimeEl = document.getElementById('response-time');
                if (responseTimeEl) responseTimeEl.textContent = responseTime + ' ms';
                
                // Update in database
                this.api('update_site_status', {
                    site_id: site.id,
                    status: 'up',
                    response_time: responseTime,
                    company_name: this.userData?.company_name
                }).catch(err => console.log('Status update error:', err));
            })
            .catch(error => {
                clearTimeout(timeoutId);
                // Site is DOWN - network error or timeout
                console.log('Site DOWN - error:', error.message);
                this.updateStatusDisplay('down');
                const responseTimeEl = document.getElementById('response-time');
                if (responseTimeEl) responseTimeEl.textContent = '--';
                
                // Record downtime event in database
                this.api('record_downtime', {
                    site_id: site.id,
                    company_name: this.userData?.company_name
                }).catch(err => console.log('Downtime record error:', err));
            });
    },

    // Update status display with IST time
    updateStatusDisplay(status) {
        const statusColor = status === 'up' ? '#10b981' : '#ef4444';
        const statusText = status === 'up' ? 'ONLINE' : 'OFFLINE';
        const indicator = document.getElementById('status-indicator');
        const text = document.getElementById('status-text');
        const time = document.getElementById('status-time');
        const offlineAlert = document.getElementById('offline-alert');
        const metricsSection = document.getElementById('site-metrics-cards');
        const chartsSection = document.getElementById('site-charts-section');
        
        if (indicator) indicator.style.background = statusColor;
        if (text) {
            text.textContent = statusText;
            text.style.color = statusColor;
        }
        if (time) {
            const now = new Date();
            const istTime = now.toLocaleString('en-IN', { 
                timeZone: 'Asia/Kolkata',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false
            });
            time.textContent = `Last checked: ${istTime}`;
        }
        
        // Update offline alert visibility
        const isOffline = status === 'down';
        if (offlineAlert) {
            offlineAlert.style.display = isOffline ? 'block' : 'none';
        }
        
        // Dim or enable metrics when status changes
        if (metricsSection) {
            metricsSection.style.opacity = isOffline ? '0.5' : '1';
            metricsSection.style.pointerEvents = isOffline ? 'none' : 'auto';
        }
        
        if (chartsSection) {
            chartsSection.style.opacity = isOffline ? '0.5' : '1';
            chartsSection.style.pointerEvents = isOffline ? 'none' : 'auto';
        }
    },

    // Start live monitoring for selected site
    startLiveSiteMonitoring(site) {
        // Clear any existing intervals
        if (this.currentSiteMonitoringInterval) {
            clearInterval(this.currentSiteMonitoringInterval);
        }

        // Check status immediately
        this.checkSiteStatus(site);

        // Check status every 30 seconds
        this.currentSiteMonitoringInterval = setInterval(() => {
            this.checkSiteStatus(site);
            this.updateLiveMetrics(site);
        }, 30000);

        // Update traffic chart every 2 seconds
        if (this.currentTrafficInterval) clearInterval(this.currentTrafficInterval);
        this.currentTrafficInterval = setInterval(() => {
            this.updateLiveTrafficChart();
        }, 2000);

        // Update load chart every 3 seconds
        if (this.currentLoadInterval) clearInterval(this.currentLoadInterval);
        this.currentLoadInterval = setInterval(() => {
            this.updateLoadChart();
        }, 3000);

        // Update refresh timer
        if (this.refreshCountdownInterval) clearInterval(this.refreshCountdownInterval);
        let countdown = 30;
        this.refreshCountdownInterval = setInterval(() => {
            countdown--;
            document.getElementById('refresh-timer').textContent = countdown + 's';
            if (countdown <= 0) countdown = 30;
        }, 1000);
    },

    // Update live metrics
    updateLiveMetrics(site) {
        // Generate realistic visitor data
        const visitors = Math.floor(Math.random() * 5000 + 1000);
        const pageViews = Math.floor(Math.random() * 15000 + 3000);
        const avgLoadTime = Math.floor(Math.random() * 500 + 100);

        document.getElementById('visitors-today').textContent = visitors;
        document.getElementById('page-views').textContent = pageViews;
        document.getElementById('avg-load-time').textContent = avgLoadTime + ' ms';

        // Update traffic sources with percentages
        const sources = [
            Math.floor(Math.random() * 40 + 20),  // Google
            Math.floor(Math.random() * 30 + 10),  // Social
            Math.floor(Math.random() * 25 + 10),  // Direct
            0  // Other (calculated)
        ];
        sources[3] = 100 - sources[0] - sources[1] - sources[2];
        
        const sourceElements = document.querySelectorAll('.traffic-source-percent');
        sourceElements.forEach((el, idx) => {
            el.textContent = Math.max(0, sources[idx]) + ' %';
        });

        // Update device distribution
        const devices = [
            Math.floor(Math.random() * 40 + 30),  // Desktop
            Math.floor(Math.random() * 50 + 40),  // Mobile
            0  // Tablet (calculated)
        ];
        devices[2] = 100 - devices[0] - devices[1];
        
        const deviceElements = document.querySelectorAll('.device-percent');
        deviceElements.forEach((el, idx) => {
            el.textContent = Math.max(0, devices[idx]) + ' %';
        });

        // Update countries and pages
        const countryElements = document.querySelectorAll('#top-locations div');
        if (countryElements.length >= 3) {
            const countries = [
                Math.floor(Math.random() * 2000 + 500),
                Math.floor(Math.random() * 1500 + 300),
                Math.floor(Math.random() * 1000 + 200)
            ];
            countryElements[0].innerHTML = '<span style="font-size: 12px; color: #1f2937;">🇮🇳 India</span><span style="font-size: 12px; font-weight: 600; color: #1f2937;">' + countries[0] + ' visits</span>';
            countryElements[1].innerHTML = '<span style="font-size: 12px; color: #1f2937;">🇺🇸 USA</span><span style="font-size: 12px; font-weight: 600; color: #1f2937;">' + countries[1] + ' visits</span>';
            countryElements[2].innerHTML = '<span style="font-size: 12px; color: #1f2937;">🇬🇧 UK</span><span style="font-size: 12px; font-weight: 600; color: #1f2937;">' + countries[2] + ' visits</span>';
        }

        // Update popular pages
        const pageElements = document.querySelectorAll('#popular-pages div');
        if (pageElements.length >= 3) {
            const pages = [
                Math.floor(Math.random() * 500 + 100),
                Math.floor(Math.random() * 300 + 50),
                Math.floor(Math.random() * 200 + 30)
            ];
            pageElements[0].innerHTML = '<span style="font-size: 12px; color: #1f2937; word-break: break-all;">/home</span><span style="font-size: 12px; font-weight: 600; color: #1f2937;">' + pages[0] + ' views</span>';
            pageElements[1].innerHTML = '<span style="font-size: 12px; color: #1f2937; word-break: break-all;">/about</span><span style="font-size: 12px; font-weight: 600; color: #1f2937;">' + pages[1] + ' views</span>';
            pageElements[2].innerHTML = '<span style="font-size: 12px; color: #1f2937; word-break: break-all;">/contact</span><span style="font-size: 12px; font-weight: 600; color: #1f2937;">' + pages[2] + ' views</span>';
        }
    },

    // Initialize live charts
    initializeLiveCharts(siteId) {
        this.trafficChartData = [];
        this.loadChartData = [];
        for (let i = 0; i < 30; i++) {
            this.trafficChartData.push(Math.floor(Math.random() * 100));
            this.loadChartData.push(Math.floor(Math.random() * 100));
        }
        this.updateLiveTrafficChart();
        this.updateLoadChart();
    },

    // Update live traffic chart
    updateLiveTrafficChart() {
        const chart = document.getElementById('live-traffic-chart');
        if (!chart) return;

        // Add new data point and remove oldest
        this.trafficChartData.push(Math.floor(Math.random() * 100 + 20));
        if (this.trafficChartData.length > 60) this.trafficChartData.shift();

        // Render bars
        const max = Math.max(...this.trafficChartData, 100);
        chart.innerHTML = this.trafficChartData.map(value => {
            const height = (value / max) * 100;
            const color = value < 30 ? '#10b981' : value < 70 ? '#f59e0b' : '#ef4444';
            return `<div style="flex: 1; background: ${color}; height: ${height}%; border-radius: 2px; opacity: 0.8; transition: all 0.2s;" title="${value} requests/s"></div>`;
        }).join('');
    },

    // Update load chart
    updateLoadChart() {
        const chart = document.getElementById('load-status-chart');
        if (!chart) return;

        // Add new data point
        this.loadChartData.push(Math.floor(Math.random() * 80));
        if (this.loadChartData.length > 60) this.loadChartData.shift();

        // Calculate stats
        const current = this.loadChartData[this.loadChartData.length - 1];
        const avg = Math.floor(this.loadChartData.reduce((a, b) => a + b, 0) / this.loadChartData.length);
        const peak = Math.max(...this.loadChartData);

        document.getElementById('current-load').textContent = current + '%';
        document.getElementById('avg-load').textContent = avg + '%';
        document.getElementById('peak-load').textContent = peak + '%';

        // Render bars
        const max = Math.max(...this.loadChartData, 100);
        chart.innerHTML = this.loadChartData.map(value => {
            const height = (value / max) * 100;
            const color = value < 40 ? '#10b981' : value < 70 ? '#f59e0b' : '#ef4444';
            return `<div style="flex: 1; background: ${color}; height: ${height}%; border-radius: 2px; opacity: 0.8; transition: all 0.2s;" title="${value}% load"></div>`;
        }).join('');
    },

    // Load site preview
    loadSitePreview(siteUrl) {
        const previewContainer = document.getElementById('site-preview-container');

        if (!previewContainer) return;

        // Try multiple screenshot services
        const screenshotServices = [
            `https://image.thum.io/get/png/width/800/crop/600/${siteUrl}`,
            `https://api.screenshotapi.net/v1/screenshot?url=${encodeURIComponent(siteUrl)}&token=default`,
            `https://screenshot.capture.gg/?url=${encodeURIComponent(siteUrl)}`
        ];

        let loaded = false;
        let serviceIndex = 0;

        const tryNextService = () => {
            if (serviceIndex >= screenshotServices.length) {
                // All services failed, show fallback
                previewContainer.innerHTML = `
                    <div style="text-align: center; padding: 40px; background: linear-gradient(135deg, #f3f4f6 0%, #e5e7eb 100%); border-radius: 8px;">
                        <div style="width: 60px; height: 60px; background: white; border-radius: 50%; margin: 0 auto 16px; display: flex; align-items: center; justify-content: center;">
                            <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="#0284c7" stroke-width="2">
                                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>
                            </svg>
                        </div>
                        <p style="color: #1f2937; font-size: 13px; margin: 0 0 8px 0; font-weight: 600;">Website Preview</p>
                        <p style="color: #94a3b8; font-size: 12px; margin: 0 0 12px 0;">Loading website screenshot...</p>
                        <a href="${siteUrl}" target="_blank" style="display: inline-block; padding: 8px 16px; background: #0284c7; color: white; text-decoration: none; border-radius: 6px; font-size: 12px; font-weight: 600;">Open Website</a>
                    </div>
                `;
                return;
            }

            const screenshotUrl = screenshotServices[serviceIndex];
            const img = new Image();
            const timeout = setTimeout(() => {
                serviceIndex++;
                tryNextService();
            }, 4000);

            img.onload = () => {
                clearTimeout(timeout);
                if (!loaded) {
                    loaded = true;
                    previewContainer.innerHTML = `<img src="${screenshotUrl}" style="width: 100%; height: 100%; object-fit: cover; border-radius: 8px;" />`;
                }
            };
            img.onerror = () => {
                clearTimeout(timeout);
                serviceIndex++;
                tryNextService();
            };
            img.src = screenshotUrl;
        };

        tryNextService();
    },

    // Load downtime history
    loadDowntimeHistory(siteId) {
        const container = document.getElementById('downtime-history');
        if (!container) return;

        this.api('get_downtime_history', { site_id: siteId }).then(response => {
            if (response.success && response.data && response.data.length > 0) {
                let html = `
                    <table style="width: 100%; border-collapse: collapse;">
                        <thead>
                            <tr style="background: #f3f4f6; border-bottom: 2px solid #e5e7eb;">
                                <th style="padding: 12px; text-align: left; font-size: 12px; font-weight: 700; color: #374151; border-right: 1px solid #e5e7eb;">Down Time</th>
                                <th style="padding: 12px; text-align: left; font-size: 12px; font-weight: 700; color: #374151; border-right: 1px solid #e5e7eb;">Up Time</th>
                                <th style="padding: 12px; text-align: left; font-size: 12px; font-weight: 700; color: #374151; border-right: 1px solid #e5e7eb;">Duration</th>
                                <th style="padding: 12px; text-align: center; font-size: 12px; font-weight: 700; color: #374151;">Status</th>
                            </tr>
                        </thead>
                        <tbody>
                `;
                
                response.data.forEach((event, index) => {
                    const downTime = this.formatTimestampIST(event.down_start);
                    const upTime = event.down_end ? this.formatTimestampIST(event.down_end) : 'Still Down';
                    const durationText = this.formatDuration(event.duration_minutes);
                    const isStillDown = !event.down_end;
                    const statusColor = isStillDown ? '#ef4444' : '#10b981';
                    const statusBg = isStillDown ? '#fef2f2' : '#f0fdf4';
                    const statusLabel = isStillDown ? '🔴 DOWN' : '✅ RESOLVED';
                    const rowBg = index % 2 === 0 ? '#ffffff' : '#f9fafb';
                    
                    html += `
                        <tr style="border-bottom: 1px solid #e5e7eb; background: ${rowBg}; transition: background 0.2s;" onmouseover="this.style.background='#eff6ff'" onmouseout="this.style.background='${rowBg}'">
                            <td style="padding: 12px; font-size: 12px; color: #1f2937; border-right: 1px solid #e5e7eb;">
                                <div style="font-weight: 600; color: #1f2937;">${downTime.split(' ')[0]}</div>
                                <div style="font-size: 11px; color: #64748b; margin-top: 2px;">${downTime.split(' ').slice(1).join(' ')}</div>
                            </td>
                            <td style="padding: 12px; font-size: 12px; color: #1f2937; border-right: 1px solid #e5e7eb;">
                                <div style="font-weight: 600; color: #1f2937;">${upTime.split(' ')[0] || '-'}</div>
                                <div style="font-size: 11px; color: #64748b; margin-top: 2px;">${upTime.split(' ').slice(1).join(' ') || '-'}</div>
                            </td>
                            <td style="padding: 12px; font-size: 12px; color: #1f2937; border-right: 1px solid #e5e7eb; font-weight: 600;">
                                <span style="background: #fef3c7; color: #92400e; padding: 4px 8px; border-radius: 4px; font-size: 11px;">${durationText}</span>
                            </td>
                            <td style="padding: 12px; text-align: center;">
                                <span style="background: ${statusBg}; color: ${statusColor}; padding: 6px 12px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-block;">${statusLabel}</span>
                            </td>
                        </tr>
                    `;
                });
                
                html += `
                        </tbody>
                    </table>
                `;
                container.innerHTML = html;
            } else {
                container.innerHTML = `
                    <div style="text-align: center; padding: 40px 20px;">
                        <div style="width: 80px; height: 80px; background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%); border-radius: 50%; margin: 0 auto 16px; display: flex; align-items: center; justify-content: center;">
                            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#0284c7" stroke-width="2">
                                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
                            </svg>
                        </div>
                        <p style="color: #64748b; font-size: 13px; margin: 0;">✅ No downtime events detected</p>
                        <p style="color: #94a3b8; font-size: 12px; margin: 8px 0 0 0;">Your site has been running smoothly</p>
                    </div>
                `;
            }
        }).catch(error => {
            console.error('Error loading downtime history:', error);
            container.innerHTML = `
                <div style="text-align: center; padding: 40px 20px;">
                    <p style="color: #ef4444; font-size: 13px; margin: 0;">⚠️ Error loading downtime history</p>
                    <p style="color: #94a3b8; font-size: 12px; margin: 8px 0 0 0;">Please try again</p>
                </div>
            `;
        });
    },

    // Format duration
    formatDuration(minutes) {
        if (!minutes || minutes < 1) return '< 1m';
        const hours = Math.floor(minutes / 60);
        const mins = minutes % 60;
        if (hours > 0) return `${hours}h ${mins}m`;
        return `${mins}m`;
    },

    // Load site analytics data (traffic source, device, countries, pages)
    loadSiteAnalytics(siteId) {
        // Load Traffic Source
        const trafficContainer = document.getElementById('traffic-sources-list');
        if (trafficContainer) {
            this.api('get_traffic_sources', { site_id: siteId }).then(response => {
                if (response.success && response.data && response.data.length > 0) {
                    const total = response.data.reduce((sum, item) => sum + (item.count || 0), 0);
                    let html = '';
                    response.data.forEach(item => {
                        const percent = total > 0 ? Math.round((item.count / total) * 100) : 0;
                        const icons = { 'google': '🔵', 'social': '🟠', 'direct': '🟢', 'other': '🟡' };
                        const icon = icons[item.source_type] || '•';
                        html += `
                            <div style="display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #f3f4f6;">
                                <div style="display: flex; align-items: center; gap: 8px;">
                                    <span style="font-size: 16px;">${icon}</span>
                                    <span style="font-size: 13px; color: #1f2937; font-weight: 500;">${item.source_type || 'Unknown'}</span>
                                </div>
                                <span style="font-size: 13px; font-weight: 700; color: #0284c7;">${percent}%</span>
                            </div>
                        `;
                    });
                    trafficContainer.innerHTML = html;
                }
            }).catch(err => console.log('Traffic data not available'));
        }

        // Load Device Distribution
        const deviceContainer = document.getElementById('device-distribution-list');
        if (deviceContainer) {
            this.api('get_device_distribution', { site_id: siteId }).then(response => {
                if (response.success && response.data && response.data.length > 0) {
                    const total = response.data.reduce((sum, item) => sum + (item.count || 0), 0);
                    let html = '';
                    response.data.forEach(item => {
                        const percent = total > 0 ? Math.round((item.count / total) * 100) : 0;
                        const icons = { 'desktop': '🖥️', 'mobile': '📱', 'tablet': '📱' };
                        const icon = icons[item.device_type] || '📱';
                        html += `
                            <div style="display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #f3f4f6;">
                                <div style="display: flex; align-items: center; gap: 8px;">
                                    <span style="font-size: 16px;">${icon}</span>
                                    <span style="font-size: 13px; color: #1f2937; font-weight: 500;">${item.device_type || 'Unknown'}</span>
                                </div>
                                <span style="font-size: 13px; font-weight: 700; color: #0284c7;">${percent}%</span>
                            </div>
                        `;
                    });
                    deviceContainer.innerHTML = html;
                }
            }).catch(err => console.log('Device data not available'));
        }

        // Load Top Countries
        const countriesContainer = document.getElementById('top-countries-list');
        if (countriesContainer) {
            this.api('get_top_countries', { site_id: siteId }).then(response => {
                if (response.success && response.data && response.data.length > 0) {
                    let html = '';
                    response.data.slice(0, 5).forEach(item => {
                        const flagEmoji = this.getCountryFlag(item.country_code);
                        html += `
                            <div style="display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #f3f4f6;">
                                <div style="display: flex; align-items: center; gap: 8px;">
                                    <span style="font-size: 18px;">${flagEmoji}</span>
                                    <span style="font-size: 13px; color: #1f2937; font-weight: 500;">${item.country_name || item.country_code}</span>
                                </div>
                                <span style="font-size: 13px; font-weight: 700; color: #0284c7;">${item.count || 0} visits</span>
                            </div>
                        `;
                    });
                    countriesContainer.innerHTML = html;
                }
            }).catch(err => console.log('Countries data not available'));
        }

        // Load Popular Pages
        const pagesContainer = document.getElementById('popular-pages-list');
        if (pagesContainer) {
            this.api('get_popular_pages', { site_id: siteId }).then(response => {
                if (response.success && response.data && response.data.length > 0) {
                    let html = '';
                    response.data.slice(0, 5).forEach(item => {
                        const viewCount = item.view_count || item.views || 0;
                        html += `
                            <div style="display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #f3f4f6;">
                                <div style="display: flex; align-items: center; gap: 8px; flex: 1;">
                                    <span style="font-size: 16px;">📄</span>
                                    <span style="font-size: 13px; color: #1f2937; font-weight: 500; word-break: break-all;">${item.page_path || item.page_name}</span>
                                </div>
                                <span style="font-size: 13px; font-weight: 700; color: #0284c7; margin-left: 8px; white-space: nowrap;">${viewCount} views</span>
                            </div>
                        `;
                    });
                    pagesContainer.innerHTML = html;
                }
            }).catch(err => console.log('Pages data not available'));
        }
    },

    // Get country flag emoji
    getCountryFlag(countryCode) {
        const flags = {
            'IN': '🇮🇳', 'US': '🇺🇸', 'GB': '🇬🇧', 'CA': '🇨🇦', 'AU': '🇦🇺',
            'DE': '🇩🇪', 'FR': '🇫🇷', 'JP': '🇯🇵', 'BR': '🇧🇷', 'MX': '🇲🇽'
        };
        return flags[countryCode?.toUpperCase()] || '🌍';
    },

    // Manual refresh
    manualRefreshSite() {
        const selector = document.getElementById('site-selector');
        if (selector && selector.value) {
            this.selectMonitoredSite(selector.value);
        }
    },

    // Update site monitoring statistics
    updateSiteMonitoringStats() {
        const companyName = this.userData?.company_name;
        
        if (!companyName) return;
        
        this.api('get_monitored_sites', { company_name: companyName }).then(response => {
            const sites = response.success ? (response.data || []) : [];
            
            const totalSites = document.getElementById('siteMonitoringTotalSites');
            const upSites = document.getElementById('siteMonitoringUpSites');
            const downSites = document.getElementById('siteMonitoringDownSites');
            const avgResponse = document.getElementById('siteMonitoringAvgResponse');
            
            const upCount = sites.filter(s => s.current_status === 'up').length;
            const downCount = sites.filter(s => s.current_status === 'down').length;
            const avgResponseTime = sites.length > 0 ? sites.reduce((acc, s) => acc + (s.response_time || 0), 0) / sites.length : 0;
            
            if (totalSites) totalSites.textContent = sites.length;
            if (upSites) upSites.textContent = upCount;
            if (downSites) downSites.textContent = downCount;
            if (avgResponse) avgResponse.textContent = avgResponseTime > 0 ? Math.round(avgResponseTime) + 'ms' : '--';
        });
    },

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    // ===== WEB LOGS FUNCTIONS =====
    async loadWebLogs() {
        try {
            const startDate = document.getElementById('webLogsStartDate').value;
            const endDate = document.getElementById('webLogsEndDate').value;
            const employee = document.getElementById('webLogsEmployee').value;
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            const container = document.getElementById('webLogsContainer');
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #64748b;"><div style="margin-bottom: 16px;">⏳</div>Loading...</div>';
            
            const response = await this.api('get_web_logs', {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee_id: employee || undefined,
                page: 1,
                limit: 50
            });
            
            if (response.success && response.data) {
                // Handle both array format and {records: []} format
                const records = Array.isArray(response.data) ? response.data : (response.data.records || []);
                this.displayWebLogs(records, startDate, endDate);
            } else {
                container.innerHTML = '<div style="text-align: center; padding: 40px; color: #ef4444;">⚠️ Failed to load data</div>';
            }
        } catch (error) {
            console.error('Error loading web logs:', error);
            this.showToast('Error loading data', 'error');
        }
    },

    displayWebLogs(data, startDate, endDate) {
        const container = document.getElementById('webLogsContainer');
        const info = document.getElementById('webLogsInfo');
        
        // Handle both array and object with records property
        const records = Array.isArray(data) ? data : (data.records || []);
        
        if (!records || records.length === 0) {
            info.innerHTML = '<div style="text-align: center; padding: 60px 40px; color: #94a3b8;"><div style="margin-bottom: 16px; font-size: 48px;">📭</div><p style="font-size: 16px; font-weight: 500;">No browsing logs found</p><p style="font-size: 13px; margin-top: 8px; color: #cbd5e1;">Try adjusting your filters or date range</p></div>';
            container.innerHTML = '';
            return;
        }

        info.innerHTML = `
            <div style="display: flex; align-items: center; gap: 12px; font-size: 13px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px;">
                <span style="background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 4px 12px; border-radius: 6px; min-width: 80px; text-align: center;">📊 ${records.length}</span>
                <span>Records Found</span>
            </div>
        `;

        // Extract unique values for filters
        const categories = [...new Set(records.map(r => r.category).filter(Boolean))].sort();
        const domains = [...new Set(records.map(r => {
            try {
                return r.website_url ? new URL(r.website_url).hostname.replace('www.', '') : null;
            } catch (e) {
                return null;
            }
        }).filter(Boolean))].sort();
        const employees = [...new Set(records.map(r => r.display_user_name || r.username).filter(Boolean))].sort();

        // Create filter section
        const filterHTML = `
            <div style="background: white; padding: 20px; border-radius: 12px; margin-bottom: 20px; border: 1px solid #e2e8f0; box-shadow: 0 2px 8px rgba(0,0,0,0.06);">
                <div style="display: flex; align-items: center; gap: 16px; flex-wrap: wrap;">
                    <span style="font-size: 12px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px;">🔍 Quick Filters:</span>
                    
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <label style="font-size: 12px; font-weight: 600; color: #475569;">📂 Category:</label>
                        <select id="webLogsCategoryFilter" style="padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 12px; font-weight: 600; cursor: pointer; background: white; color: #1e293b; transition: all 0.3s ease;">
                            <option value="">All Categories</option>
                            ${categories.map(cat => `<option value="${cat}">${cat}</option>`).join('')}
                        </select>
                    </div>
                    
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <label style="font-size: 12px; font-weight: 600; color: #475569;">🏢 Domain:</label>
                        <select id="webLogsDomainFilter" style="padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 12px; font-weight: 600; cursor: pointer; background: white; color: #1e293b; transition: all 0.3s ease;">
                            <option value="">All Domains</option>
                            ${domains.map(dom => `<option value="${dom}">${dom}</option>`).join('')}
                        </select>
                    </div>
                    
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <label style="font-size: 12px; font-weight: 600; color: #475569;">👤 Employee:</label>
                        <select id="webLogsTableEmployeeFilter" style="padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 12px; font-weight: 600; cursor: pointer; background: white; color: #1e293b; transition: all 0.3s ease;">
                            <option value="">All Employees</option>
                            ${employees.map(emp => `<option value="${emp}">${emp}</option>`).join('')}
                        </select>
                    </div>
                    
                    <button id="webLogsClearFilters" style="padding: 8px 14px; background: #f1f5f9; color: #64748b; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 12px; font-weight: 600; cursor: pointer; transition: all 0.3s ease;" onmouseover="this.style.background='#e2e8f0';" onmouseout="this.style.background='#f1f5f9';">
                        ✕ Clear Filters
                    </button>
                </div>
            </div>
        `;

        container.innerHTML = filterHTML + '<div id="webLogsTableWrapper" style="background: white; padding: 24px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06); border: 1px solid #e2e8f0;"></div>';
        const tableWrapper = document.getElementById('webLogsTableWrapper');
        
        const columns = [
            {
                label: '🌐 Website URL',
                width: '400px',
                render: (record) => {
                    const url = record.website_url || 'N/A';
                    const title = record.page_title || 'No title';
                    if (url === 'N/A') {
                        return `<div style="font-weight: 500;">${url}</div><div style="color: #94a3b8; font-size: 11px; margin-top: 2px;">${title}</div>`;
                    }
                    return `
                        <div style="margin-bottom: 4px;">
                            <a href="${url}" target="_blank" rel="noopener noreferrer" style="color: #0369a1; text-decoration: none; font-weight: 600; word-wrap: break-word; word-break: break-all; display: inline-block; max-width: 100%; transition: all 0.3s ease;" title="${url}" onmouseover="this.style.color='#0284c7'; this.style.textDecoration='underline';" onmouseout="this.style.color='#0369a1'; this.style.textDecoration='none';">🔗 ${url}</a>
                        </div>
                        <div style="color: #94a3b8; font-size: 11px; margin-top: 2px; word-wrap: break-word;">${title}</div>
                    `;
                }
            },
            {
                label: '📂 Category',
                width: '120px',
                render: (record) => {
                    const category = record.category || 'Uncategorized';
                    const categoryColor = category === 'Work' ? '#3b82f6' : 
                                        category === 'Social' ? '#ec4899' : 
                                        category === 'Entertainment' ? '#f59e0b' : 
                                        category === 'Educational' ? '#10b981' : '#64748b';
                    return `<span style="background: ${categoryColor}; color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">📂 ${category}</span>`;
                }
            },
            {
                label: '🏢 Domain',
                width: '150px',
                render: (record) => {
                    try {
                        const url = record.website_url || '';
                        const domain = url ? new URL(url).hostname.replace('www.', '') : 'N/A';
                        return `<span style="color: #1e293b; font-weight: 600; font-size: 12px;">🏢 ${domain}</span>`;
                    } catch (e) {
                        return '<span style="color: #94a3b8;">N/A</span>';
                    }
                }
            },
            {
                label: '📄 Page Title',
                width: '200px',
                render: (record) => {
                    const title = record.page_title || 'No title';
                    return `<span style="color: #475569; font-size: 12px; word-wrap: break-word;">📄 ${title}</span>`;
                }
            },
            {
                label: '⏱️ Duration',
                width: '100px',
                render: (record) => {
                    const durationMinutes = Math.round((record.duration_seconds || 0) / 60);
                    return `<span style="background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">${durationMinutes} min</span>`;
                }
            },
            {
                label: '🌍 Browser',
                width: '120px',
                render: (record) => {
                    const browser = record.browser_name || 'Unknown';
                    const browserIcon = browser.includes('Chrome') ? '🔵' : browser.includes('Firefox') ? '🧡' : browser.includes('Safari') ? '🔴' : '📱';
                    return `<span style="font-weight: 600;">${browserIcon} ${browser}</span>`;
                }
            },
            {
                label: '👤 Employee',
                width: '150px',
                render: (record) => `<span style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">👤 ${record.display_user_name || record.username || 'N/A'}</span>`
            },
            {
                label: '📅 Timestamp',
                width: '180px',
                render: (record) => `<span style="color: #64748b; font-size: 12px;">📅 ${this.formatTimestampIST(record.visit_time)}</span>`
            }
        ];
        
        // Create table in wrapper
        let currentPage = 1;
        let pageSize = 15;

        const renderTable = () => {
            // Get filter values
            const selectedCategory = document.getElementById('webLogsCategoryFilter')?.value || '';
            const selectedDomain = document.getElementById('webLogsDomainFilter')?.value || '';
            const selectedEmployee = document.getElementById('webLogsTableEmployeeFilter')?.value || '';

            // Apply filters to records
            let filteredRecords = records.filter(record => {
                // Category filter
                if (selectedCategory && record.category !== selectedCategory) return false;
                
                // Domain filter
                if (selectedDomain) {
                    try {
                        const domain = record.website_url ? new URL(record.website_url).hostname.replace('www.', '') : null;
                        if (domain !== selectedDomain) return false;
                    } catch (e) {
                        return false;
                    }
                }
                
                // Employee filter
                if (selectedEmployee && (record.display_user_name !== selectedEmployee && record.username !== selectedEmployee)) return false;
                
                return true;
            });

            const startIdx = (currentPage - 1) * pageSize;
            const endIdx = startIdx + pageSize;
            const pageRecords = filteredRecords.slice(startIdx, endIdx);
            const totalPages = Math.ceil(filteredRecords.length / pageSize);

            const headerCells = columns.map(col => `<th style="padding: 16px; text-align: left; font-weight: 700; color: #1e293b; background: linear-gradient(180deg, #f1f5f9 0%, #e2e8f0 100%); min-width: ${col.width || '150px'}; white-space: nowrap; border-bottom: 2px solid #cbd5e1; position: sticky; top: 0; z-index: 10;">${col.label}</th>`).join('');
            
            const bodyRows = pageRecords.map((record, idx) => {
                const cells = columns.map(col => {
                    const content = col.render ? col.render(record) : (record[col.field] || 'N/A');
                    return `<td style="padding: 14px 16px; color: #475569; border-bottom: 1px solid #e2e8f0; word-wrap: break-word; word-break: break-word; overflow-wrap: break-word; max-width: ${col.width || '150px'};">${content}</td>`;
                }).join('');
                return `<tr style="background: ${idx % 2 === 0 ? '#ffffff' : '#f9fafb'}; transition: all 0.2s ease;" onmouseover="this.style.background='#f0f9ff';" onmouseout="this.style.background='${idx % 2 === 0 ? '#ffffff' : '#f9fafb'}';">${cells}</tr>`;
            }).join('');

            const paginationHTML = `
                <div style="display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; background: linear-gradient(180deg, #f9fafb 0%, #f1f5f9 100%); border-top: 1px solid #e2e8f0; flex-wrap: wrap; gap: 16px; margin-top: 0;">
                    <div style="display: flex; gap: 12px; align-items: center;">
                        <label style="font-size: 12px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px;">Show entries:</label>
                        <select id="webLogsPageSize" style="padding: 8px 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; background: white; color: #1e293b; transition: all 0.3s ease;">
                            <option value="15" ${pageSize === 15 ? 'selected' : ''}>15</option>
                            <option value="30" ${pageSize === 30 ? 'selected' : ''}>30</option>
                            <option value="50" ${pageSize === 50 ? 'selected' : ''}>50</option>
                            <option value="100" ${pageSize === 100 ? 'selected' : ''}>100</option>
                            <option value="200" ${pageSize === 200 ? 'selected' : ''}>200</option>
                        </select>
                    </div>
                    <div style="display: flex; gap: 8px; align-items: center;">
                        <button id="webLogsPrevBtn" style="padding: 8px 16px; background: ${currentPage === 1 ? '#e2e8f0' : 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)'}; color: white; border: none; border-radius: 6px; cursor: ${currentPage === 1 ? 'not-allowed' : 'pointer'}; font-size: 12px; font-weight: 700; transition: all 0.3s ease; opacity: ${currentPage === 1 ? '0.5' : '1'};" ${currentPage === 1 ? 'disabled' : ''}>← PREVIOUS</button>
                        <span style="font-size: 12px; font-weight: 700; color: #1e293b; min-width: 140px; text-align: center; background: white; padding: 8px 12px; border-radius: 6px; border: 1px solid #cbd5e1;">Page <strong style="color: #3b82f6;">${currentPage}</strong> of <strong>${totalPages}</strong></span>
                        <button id="webLogsNextBtn" style="padding: 8px 16px; background: ${currentPage === totalPages ? '#e2e8f0' : 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)'}; color: white; border: none; border-radius: 6px; cursor: ${currentPage === totalPages ? 'not-allowed' : 'pointer'}; font-size: 12px; font-weight: 700; transition: all 0.3s ease; opacity: ${currentPage === totalPages ? '0.5' : '1'};" ${currentPage === totalPages ? 'disabled' : ''}>NEXT →</button>
                    </div>
                    <div style="font-size: 11px; color: #64748b; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;">📊 Showing ${startIdx + 1} to ${Math.min(endIdx, filteredRecords.length)} of <strong style="color: #3b82f6;">${filteredRecords.length}</strong> records (${records.length} total)</div>
                </div>
            `;

            tableWrapper.innerHTML = `
                <div style="display: flex; flex-direction: column; height: 100%; border-radius: 12px; overflow: hidden; background: white;">
                    <div style="overflow: auto; flex: 1; max-height: 600px; background: white;">
                        <table style="width: 100%; border-collapse: collapse; font-size: 13px; table-layout: auto;">
                            <thead style="position: sticky; top: 0; z-index: 10;">
                                <tr>${headerCells}</tr>
                            </thead>
                            <tbody>${bodyRows}</tbody>
                        </table>
                    </div>
                    ${paginationHTML}
                </div>
            `;

            // Attach event listeners
            const pageSizeSelect = document.getElementById('webLogsPageSize');
            const prevBtn = document.getElementById('webLogsPrevBtn');
            const nextBtn = document.getElementById('webLogsNextBtn');

            if (pageSizeSelect) {
                pageSizeSelect.addEventListener('change', (e) => {
                    pageSize = parseInt(e.target.value);
                    currentPage = 1;
                    renderTable();
                });
            }

            if (prevBtn) {
                prevBtn.addEventListener('click', () => {
                    if (currentPage > 1) {
                        currentPage--;
                        renderTable();
                    }
                });
            }

            if (nextBtn) {
                nextBtn.addEventListener('click', () => {
                    if (currentPage < totalPages) {
                        currentPage++;
                        renderTable();
                    }
                });
            }
        };

        renderTable();

        // Attach filter listeners (outside renderTable to avoid duplicate listeners)
        const categoryFilter = document.getElementById('webLogsCategoryFilter');
        const domainFilter = document.getElementById('webLogsDomainFilter');
        const employeeFilter = document.getElementById('webLogsTableEmployeeFilter');
        const clearFiltersBtn = document.getElementById('webLogsClearFilters');

        if (categoryFilter) {
            categoryFilter.addEventListener('change', () => {
                currentPage = 1;
                renderTable();
            });
        }

        if (domainFilter) {
            domainFilter.addEventListener('change', () => {
                currentPage = 1;
                renderTable();
            });
        }

        if (employeeFilter) {
            employeeFilter.addEventListener('change', () => {
                currentPage = 1;
                renderTable();
            });
        }

        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', () => {
                if (categoryFilter) categoryFilter.value = '';
                if (domainFilter) domainFilter.value = '';
                if (employeeFilter) employeeFilter.value = '';
                currentPage = 1;
                renderTable();
            });
        }
    },

    clearWebLogsFilter() {
        document.getElementById('webLogsStartDate').value = new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0];
        document.getElementById('webLogsEndDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('webLogsEmployee').value = '';
        document.getElementById('webLogsContainer').innerHTML = '<div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No data loaded</p></div>';
        document.getElementById('webLogsInfo').textContent = 'Click "Load Logs" to view browsing history';
    },

    // ===== APPLICATION LOGS FUNCTIONS =====
    async loadApplicationLogs() {
        try {
            const startDate = document.getElementById('appUsageStartDate').value;
            const endDate = document.getElementById('appUsageEndDate').value;
            const employee = document.getElementById('appUsageEmployee').value;
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            const container = document.getElementById('appUsageContainer');
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #64748b;"><div style="margin-bottom: 16px;">⏳</div>Loading...</div>';
            
            const response = await this.api('get_application_logs', {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee_id: employee || undefined,
                page: 1,
                limit: 50
            });
            
            if (response.success && response.data) {
                // Handle both array format and {records: []} format
                const records = Array.isArray(response.data) ? response.data : (response.data.records || []);
                this.displayApplicationLogs(records);
            } else {
                container.innerHTML = '<div style="text-align: center; padding: 40px; color: #ef4444;">⚠️ Failed to load data</div>';
            }
        } catch (error) {
            console.error('Error loading application logs:', error);
            this.showToast('Error loading data', 'error');
        }
    },

    displayApplicationLogs(data) {
        const container = document.getElementById('appUsageContainer');
        const info = document.getElementById('appUsageInfo');
        
        const records = Array.isArray(data) ? data : (data.records || []);
        info.textContent = `${records.length} records found`;
        
        if (!records || records.length === 0) {
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No application logs found</p></div>';
            return;
        }

        const columns = [
            {
                label: 'Application Name',
                width: '180px',
                render: (record) => {
                    const app = record.app_name || 'Unknown';
                    const appIcon = app.includes('Chrome') ? '🔵' : app.includes('VS') ? '💙' : app.includes('Word') ? '📘' : app.includes('Excel') ? '📗' : '📦';
                    return `<span style="font-weight: 600;">${appIcon} ${app}</span>`;
                }
            },
            {
                label: 'Window Title',
                width: '350px',
                render: (record) => `<span style="color: #6b7280; word-wrap: break-word;">${record.window_title || 'N/A'}</span>`
            },
            {
                label: 'Duration',
                width: '100px',
                render: (record) => {
                    const durationMinutes = Math.round((record.duration_seconds || 0) / 60);
                    return `<span style="background: #fef3c7; color: #92400e; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">⏱️ ${durationMinutes} min</span>`;
                }
            },
            {
                label: 'Start Time',
                width: '160px',
                render: (record) => `<span style="color: #6b7280; font-size: 12px;">🕐 ${this.formatTimestampIST(record.start_time)}</span>`
            },
            {
                label: 'Employee',
                width: '150px',
                render: (record) => `<span style="background: #f0fdf4; color: #166534; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">👤 ${record.display_user_name || record.username || 'N/A'}</span>`
            },
            {
                label: 'Status',
                width: '100px',
                render: (record) => {
                    const isActive = record.is_active;
                    return `<span style="background: ${isActive ? '#dcfce7' : '#f3f4f6'}; color: ${isActive ? '#166534' : '#6b7280'}; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">${isActive ? '🟢 Active' : '✅ Done'}</span>`;
                }
            }
        ];

        this.createPaginatedTable(records, columns, 'appUsageContainer', 15);
    },

    clearAppUsageFilter() {
        document.getElementById('appUsageStartDate').value = new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0];
        document.getElementById('appUsageEndDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('appUsageEmployee').value = '';
        document.getElementById('appUsageContainer').innerHTML = '<div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No data loaded</p></div>';
        document.getElementById('appUsageInfo').textContent = 'Click "Load Logs" to view application usage';
    },

    // ===== INACTIVITY LOGS FUNCTIONS =====
    async loadInactivityLogs() {
        try {
            const startDate = document.getElementById('inactivityStartDate').value;
            const endDate = document.getElementById('inactivityEndDate').value;
            const employee = document.getElementById('inactivityEmployee').value;
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            const container = document.getElementById('inactivityContainer');
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #64748b;"><div style="margin-bottom: 16px;">⏳</div>Loading...</div>';
            
            const response = await this.api('get_inactivity_logs', {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee_id: employee || undefined,
                page: 1,
                limit: 50
            });
            
            if (response.success && response.data) {
                const records = Array.isArray(response.data) ? response.data : (response.data.records || []);
                this.displayInactivityLogs(records);
            } else {
                container.innerHTML = '<div style="text-align: center; padding: 40px; color: #ef4444;">⚠️ Failed to load data</div>';
            }
        } catch (error) {
            console.error('Error loading inactivity logs:', error);
            this.showToast('Error loading data', 'error');
        }
    },

    displayInactivityLogs(data) {
        const container = document.getElementById('inactivityContainer');
        const info = document.getElementById('inactivityInfo');
        
        const records = Array.isArray(data) ? data : (data.records || []);
        
        if (!records || records.length === 0) {
            container.innerHTML = '<div style="text-align: center; padding: 60px 20px; color: #6b7280;"><div style="margin-bottom: 16px; font-size: 48px;">📭</div><p style="font-size: 16px;">No inactivity logs found</p></div>';
            document.getElementById('inactTotalCount').textContent = '0';
            document.getElementById('inactActiveCount').textContent = '0';
            document.getElementById('inactInactiveCount').textContent = '0';
            document.getElementById('inactAvgDuration').textContent = '0';
            return;
        }

        // Categorize records based on 10-minute threshold
        const THRESHOLD_SECONDS = 10 * 60; // 10 minutes in seconds
        const activeRecords = records.filter(r => (r.duration_seconds || 0) < THRESHOLD_SECONDS);
        const inactiveRecords = records.filter(r => (r.duration_seconds || 0) >= THRESHOLD_SECONDS);

        // Calculate metrics
        const totalSeconds = records.reduce((sum, record) => sum + (record.duration_seconds || 0), 0);
        const avgSeconds = records.length > 0 ? totalSeconds / records.length : 0;
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        const avgMinutes = Math.floor(avgSeconds / 60);
        const avgSecs = Math.floor(avgSeconds % 60);

        // Calculate total duration for Short Pauses (< 10 min)
        const activeSeconds = activeRecords.reduce((sum, record) => sum + (record.duration_seconds || 0), 0);
        const activeHours = Math.floor(activeSeconds / 3600);
        const activeMinutes = Math.floor((activeSeconds % 3600) / 60);

        // Calculate total duration for Significant Idle (>= 10 min)
        const inactiveSeconds = inactiveRecords.reduce((sum, record) => sum + (record.duration_seconds || 0), 0);
        const inactiveHours = Math.floor(inactiveSeconds / 3600);
        const inactiveMinutes = Math.floor((inactiveSeconds % 3600) / 60);

        // Update summary cards
        document.getElementById('inactTotalCount').textContent = records.length;
        document.getElementById('inactActiveCount').textContent = `${activeRecords.length}`;
        document.getElementById('inactInactiveCount').textContent = `${inactiveRecords.length}`;
        document.getElementById('inactAvgDuration').textContent = `${avgMinutes}m ${avgSecs}s`;

        // Update info text
        info.innerHTML = `
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="vertical-align: middle; display: inline;"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            <span style="vertical-align: middle; margin-left: 8px;">Showing ${records.length} inactivity instances | <strong>${activeRecords.length}</strong> Short Pause (&lt;10min) | <strong>${inactiveRecords.length}</strong> Significant Idle (≥10min)</span>
        `;

        // Create enhanced summary cards
        const summaryCard = `
            <div style="background: linear-gradient(135deg, #fff7ed 0%, #fed7aa 100%); border-radius: 14px; padding: 24px; margin-bottom: 24px; border-left: 4px solid #ea580c; box-shadow: 0 4px 12px rgba(234, 88, 12, 0.1);">
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px;">
                    <div>
                        <p style="font-size: 11px; color: #7c2d12; text-transform: uppercase; font-weight: 700; margin: 0 0 8px 0; letter-spacing: 0.5px;">⏱️ Total Inactivity</p>
                        <p style="font-size: 28px; font-weight: 700; color: #b45309; margin: 0;">${hours}h ${minutes}m</p>
                        <p style="font-size: 12px; color: #92400e; margin-top: 4px;">across all instances</p>
                    </div>
                    <div>
                        <p style="font-size: 11px; color: #15803d; text-transform: uppercase; font-weight: 700; margin: 0 0 8px 0; letter-spacing: 0.5px;">✓ Short Pauses</p>
                        <p style="font-size: 24px; font-weight: 700; color: #16a34a; margin: 0;">${activeRecords.length} <span style="font-size: 14px; color: #84cc16;">• ${activeHours}h ${activeMinutes}m</span></p>
                        <p style="font-size: 12px; color: #166534; margin-top: 4px;">instances | total duration</p>
                    </div>
                    <div>
                        <p style="font-size: 11px; color: #dc2626; text-transform: uppercase; font-weight: 700; margin: 0 0 8px 0; letter-spacing: 0.5px;">⚠️ Significant Idle</p>
                        <p style="font-size: 24px; font-weight: 700; color: #ef4444; margin: 0;">${inactiveRecords.length} <span style="font-size: 14px; color: #fca5a5;">• ${inactiveHours}h ${inactiveMinutes}m</span></p>
                        <p style="font-size: 12px; color: #991b1b; margin-top: 4px;">instances | total duration</p>
                    </div>
                    <div>
                        <p style="font-size: 11px; color: #0369a1; text-transform: uppercase; font-weight: 700; margin: 0 0 8px 0; letter-spacing: 0.5px;">⏲️ Average Duration</p>
                        <p style="font-size: 28px; font-weight: 700; color: #0ea5e9; margin: 0;">${avgMinutes}m</p>
                        <p style="font-size: 12px; color: #0c4a6e; margin-top: 4px;">per instance</p>
                    </div>
                </div>
            </div>
        `;

        // Insert summary before the table
        container.innerHTML = summaryCard + '<div id="inactivityTableContainer"></div>';

        const columns = [
            {
                label: 'Status',
                width: '100px',
                render: (record) => {
                    const duration = record.duration_seconds || 0;
                    const isInactive = duration >= THRESHOLD_SECONDS;
                    const badge = isInactive ? 
                        `<span style="background: #fee2e2; color: #991b1b; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-block;">⚠️ IDLE</span>` :
                        `<span style="background: #dcfce7; color: #15803d; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700; display: inline-block;">✓ BRIEF</span>`;
                    return badge;
                }
            },
            {
                label: 'Employee',
                width: '160px',
                render: (record) => `<span style="background: #f0fdf4; color: #166534; padding: 6px 10px; border-radius: 6px; font-size: 12px; font-weight: 600;">👤 ${record.display_user_name || record.username || 'N/A'}</span>`
            },
            {
                label: 'Start Time',
                width: '180px',
                render: (record) => `<span style="color: #374151; font-size: 12px; font-weight: 500;">🕐 ${this.formatTimestampIST(record.start_time)}</span>`
            },
            {
                label: 'End Time',
                width: '180px',
                render: (record) => `<span style="color: #374151; font-size: 12px; font-weight: 500;">🕑 ${this.formatTimestampIST(record.end_time)}</span>`
            },
            {
                label: 'Duration',
                width: '130px',
                render: (record) => {
                    const duration = record.duration_seconds || 0;
                    const durationMinutes = Math.floor(duration / 60);
                    const durationSecs = duration % 60;
                    const isInactive = duration >= THRESHOLD_SECONDS;
                    const bgColor = isInactive ? '#fecaca' : '#bfdbfe';
                    const textColor = isInactive ? '#7f1d1d' : '#1e40af';
                    return `<span style="background: ${bgColor}; color: ${textColor}; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">⏱️ ${durationMinutes}m ${durationSecs}s</span>`;
                }
            }
        ];

        // Create paginated table in the table container
        const tableContainer = document.getElementById('inactivityTableContainer');
        this.createPaginatedTable(records, columns, 'inactivityTableContainer', 15);
    },

    clearInactivityFilter() {
        document.getElementById('inactivityStartDate').value = new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0];
        document.getElementById('inactivityEndDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('inactivityEmployee').value = '';
        document.getElementById('inactivityContainer').innerHTML = '<div style="text-align: center; color: #6b7280; padding: 60px 20px;"><div style="margin-bottom: 16px; font-size: 48px;">📭</div><p>No data loaded yet</p></div>';
        document.getElementById('inactivityInfo').innerHTML = `
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="vertical-align: middle; display: inline;"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            <span style="vertical-align: middle; margin-left: 8px;">Click "Load Logs" to view inactivity records</span>
        `;
        document.getElementById('inactTotalCount').textContent = '--';
        document.getElementById('inactActiveCount').textContent = '--';
        document.getElementById('inactInactiveCount').textContent = '--';
        document.getElementById('inactAvgDuration').textContent = '--';
    },

    // ===== SCREENSHOTS FUNCTIONS =====
    async loadScreenshots() {
        try {
            const startDate = document.getElementById('screenshotsStartDate').value;
            const endDate = document.getElementById('screenshotsEndDate').value;
            const employee = document.getElementById('screenshotsEmployee').value;
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            const container = document.getElementById('screenshotsContainer');
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #64748b;"><div style="margin-bottom: 16px;">⏳</div>Loading...</div>';
            
            const response = await this.api('get_screenshots', {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee: employee || undefined,
                page: 1,
                limit: 50
            });
            
            if (response.success && response.data) {
                const records = Array.isArray(response.data) ? response.data : (response.data.records || []);
                this.displayScreenshots(records);
            } else {
                container.innerHTML = '<div style="text-align: center; padding: 40px; color: #ef4444;">⚠️ Failed to load data</div>';
            }
        } catch (error) {
            console.error('Error loading screenshots:', error);
            this.showToast('Error loading data', 'error');
        }
    },

    displayScreenshots(data) {
        const container = document.getElementById('screenshotsContainer');
        const info = document.getElementById('screenshotsInfo');
        
        const records = Array.isArray(data) ? data : (data.records || []);
        
        if (!records || records.length === 0) {
            info.innerHTML = '<div style="text-align: center; padding: 60px 40px; color: #94a3b8;"><div style="margin-bottom: 16px; font-size: 48px;">📭</div><p style="font-size: 16px; font-weight: 500;">No screenshots found</p><p style="font-size: 13px; margin-top: 8px; color: #cbd5e1;">Try adjusting your filters or date range</p></div>';
            container.innerHTML = '';
            return;
        }

        info.innerHTML = `
            <div style="display: flex; align-items: center; gap: 12px; font-size: 13px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px;">
                <span style="background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 4px 12px; border-radius: 6px; min-width: 80px; text-align: center;">📸 ${records.length}</span>
                <span>Screenshots Found</span>
            </div>
        `;

        const columns = [
            {
                label: '🖼️ Preview',
                width: '120px',
                render: (record) => {
                    return `
                        <button onclick="app.showImageViewer(${record.id})" style="background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%); border: 2px solid #e2e8f0; padding: 8px 12px; border-radius: 6px; cursor: pointer; font-size: 24px; transition: all 0.3s ease; width: 100%; text-align: center;" onmouseover="this.style.borderColor='#3b82f6'; this.style.background='linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)';" onmouseout="this.style.borderColor='#e2e8f0'; this.style.background='linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)';">
                            📸 View
                        </button>
                    `;
                }
            },
            {
                label: '🔑 Screenshot ID',
                width: '100px',
                render: (record) => `<span style="background: #e0e7ff; color: #3730a3; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">#${record.id}</span>`
            },
            {
                label: '👤 Employee',
                width: '150px',
                render: (record) => `<span style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">👤 ${record.display_user_name || record.username || 'N/A'}</span>`
            },
            {
                label: '📅 Timestamp',
                width: '180px',
                render: (record) => `<span style="color: #64748b; font-size: 12px;">📅 ${this.formatTimestampIST(record.capture_time)}</span>`
            },
            {
                label: '📺 Resolution',
                width: '150px',
                render: (record) => `<span style="background: linear-gradient(135deg, #a855f7 0%, #9333ea 100%); color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">📺 ${record.screen_width || 0}×${record.screen_height || 0}</span>`
            },
            {
                label: '💻 System',
                width: '140px',
                render: (record) => `<span style="background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">💻 ${record.system_name || 'N/A'}</span>`
            },
            {
                label: '✅ Status',
                width: '120px',
                render: (record) => '<span style="background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%); color: white; padding: 6px 10px; border-radius: 6px; font-size: 11px; font-weight: 700;">✅ Available</span>'
            }
        ];

        this.createPaginatedTable(records, columns, 'screenshotsContainer', 15);
    },

    clearScreenshotsFilter() {
        document.getElementById('screenshotsStartDate').value = new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0];
        document.getElementById('screenshotsEndDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('screenshotsEmployee').value = '';
        document.getElementById('screenshotsContainer').innerHTML = '<div style="text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">📭</div><p>No data loaded</p></div>';
        document.getElementById('screenshotsInfo').textContent = 'Click "Load Logs" to view screenshot history';
    },

    // ===== ANALYTICS & REPORTS FUNCTIONS =====
    generateAnalyticsView() {
        return `
            <div id="analytics-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">📊 Analytics & Reports</h1>
                    <p style="color: #64748b; font-size: 16px;">Comprehensive web activity analysis and employee tracking</p>
                </div>

                <!-- Date & Employee Filters -->
                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); margin-bottom: 24px;">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px;">
                        <div>
                            <label style="display: block; font-size: 12px; font-weight: 600; color: #475569; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Start Date</label>
                            <input type="date" id="analyticsStartDate" value="${new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0]}" style="width: 100%; padding: 10px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                        </div>
                        <div>
                            <label style="display: block; font-size: 12px; font-weight: 600; color: #475569; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">End Date</label>
                            <input type="date" id="analyticsEndDate" value="${new Date().toISOString().split('T')[0]}" style="width: 100%; padding: 10px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                        </div>
                        <div>
                            <label style="display: block; font-size: 12px; font-weight: 600; color: #475569; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Employee</label>
                            <select id="analyticsEmployee" style="width: 100%; padding: 10px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 14px;">
                                <option value="">All Employees</option>
                            </select>
                        </div>
                        <div style="display: flex; align-items: flex-end; gap: 8px;">
                            <button onclick="app.loadAnalyticsReport()" style="flex: 1; padding: 10px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.3s ease;">📊 Generate Report</button>
                            <button onclick="app.clearAnalyticsFilter()" style="flex: 1; padding: 10px; background: #f1f5f9; color: #475569; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; font-weight: 600; transition: all 0.3s ease;">🔄 Reset</button>
                        </div>
                    </div>
                </div>

                <!-- Summary Cards -->
                <div id="analyticsSummary" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 16px; margin-bottom: 24px;">
                    <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);">
                        <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">📊 Total Visits</p>
                        <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
                    </div>
                    <div style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.2);">
                        <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">👥 Active Employees</p>
                        <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
                    </div>
                    <div style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(245, 158, 11, 0.2);">
                        <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">⏱️ Total Time Spent</p>
                        <p style="font-size: 32px; font-weight: 700; margin: 0;" id="analyticsTotalTime">-</p>
                    </div>
                    <div style="background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(14, 165, 233, 0.2);">
                        <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">🌐 Unique Domains</p>
                        <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
                    </div>
                </div>

                <!-- Charts Section -->
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(500px, 1fr)); gap: 20px; margin-bottom: 24px;">
                    <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                        <h3 style="font-size: 16px; font-weight: 600; color: #1f2937; margin-bottom: 16px;">📈 Top 10 Visited Domains</h3>
                        <div id="analyticsTopDomains" style="min-height: 300px; background: #f8fafc; border-radius: 8px; padding: 16px;"></div>
                    </div>
                    <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                        <h3 style="font-size: 16px; font-weight: 600; color: #1f2937; margin-bottom: 16px;">📂 Website Categories</h3>
                        <div id="analyticsCategoryBreakdown" style="min-height: 300px; background: #f8fafc; border-radius: 8px; padding: 16px;"></div>
                    </div>
                </div>

                <!-- Employee Activity Report -->
                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); margin-bottom: 24px;">
                    <h3 style="font-size: 16px; font-weight: 600; color: #1f2937; margin-bottom: 16px;">👥 Employee Activity Report</h3>
                    <div id="analyticsEmployeeReport" style="overflow-x: auto; min-height: 300px; background: #f8fafc; border-radius: 8px; padding: 16px;"></div>
                </div>

                <!-- Detailed Web Logs -->
                <div style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <h3 style="font-size: 16px; font-weight: 600; color: #1f2937; margin-bottom: 16px;">🌐 Detailed Web Activity Logs</h3>
                    <div id="analyticsWebLogs" style="min-height: 400px; background: #f8fafc; border-radius: 8px; padding: 16px;"></div>
                </div>
            </div>
        `;
    },

    async loadAnalyticsReport() {
        try {
            const startDate = document.getElementById('analyticsStartDate').value;
            const endDate = document.getElementById('analyticsEndDate').value;
            const employee = document.getElementById('analyticsEmployee').value;
            
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            // Load web logs data
            const response = await this.api('get_web_logs', {
                company_name: companyName,
                start_date: startDate,
                end_date: endDate,
                employee_id: employee || undefined
            });

            if (response.success && response.data) {
                const records = Array.isArray(response.data) ? response.data : (response.data.records || []);
                this.displayAnalyticsReport(records, startDate, endDate, employee);
            } else {
                this.showToast('Failed to load analytics data', 'error');
            }
        } catch (error) {
            console.error('Error loading analytics:', error);
            this.showToast('Error loading data', 'error');
        }
    },

    displayAnalyticsReport(records, startDate, endDate, employee) {
        if (!records || records.length === 0) {
            document.getElementById('analyticsSummary').innerHTML = '<div style="grid-column: 1/-1; text-align: center; padding: 40px; color: #6b7280;">No data found for selected filters</div>';
            return;
        }

        // Calculate summary metrics
        const totalVisits = records.length;
        const uniqueEmployees = [...new Set(records.map(r => r.username || r.display_user_name))].length;
        const totalTime = records.reduce((sum, r) => sum + (r.duration_seconds || 0), 0);
        const hours = Math.floor(totalTime / 3600);
        const minutes = Math.floor((totalTime % 3600) / 60);
        const uniqueDomains = [...new Set(records.map(r => this.extractDomain(r.website_url)))].filter(d => d !== 'N/A').length;

        // Update summary cards
        document.getElementById('analyticsSummary').innerHTML = `
            <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">📊 Total Visits</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">${totalVisits}</p>
            </div>
            <div style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">👥 Active Employees</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">${uniqueEmployees}</p>
            </div>
            <div style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(245, 158, 11, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">⏱️ Total Time Spent</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">${hours}h ${minutes}m</p>
            </div>
            <div style="background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(14, 165, 233, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">🌐 Unique Domains</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">${uniqueDomains}</p>
            </div>
        `;

        // Top domains
        const domainStats = {};
        records.forEach(r => {
            const domain = this.extractDomain(r.website_url);
            domainStats[domain] = (domainStats[domain] || 0) + 1;
        });
        
        const topDomains = Object.entries(domainStats)
            .filter(([domain]) => domain !== 'N/A')
            .sort((a, b) => b[1] - a[1])
            .slice(0, 10);

        let domainHTML = '<div style="display: flex; flex-direction: column; gap: 12px;">';
        topDomains.forEach(([domain, count], index) => {
            const percentage = ((count / totalVisits) * 100).toFixed(1);
            domainHTML += `
                <div>
                    <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
                        <span style="font-weight: 600; color: #1f2937;">${index + 1}. ${domain}</span>
                        <span style="color: #3b82f6; font-weight: 600;">${count} visits (${percentage}%)</span>
                    </div>
                    <div style="background: #e2e8f0; border-radius: 4px; height: 8px; overflow: hidden;">
                        <div style="background: linear-gradient(90deg, #3b82f6, #1e40af); height: 100%; width: ${percentage}%; border-radius: 4px;"></div>
                    </div>
                </div>
            `;
        });
        domainHTML += '</div>';
        document.getElementById('analyticsTopDomains').innerHTML = domainHTML;

        // Category breakdown
        const categoryStats = {};
        records.forEach(r => {
            const category = r.category || 'Uncategorized';
            categoryStats[category] = (categoryStats[category] || 0) + 1;
        });

        let categoryHTML = '<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px;">';
        Object.entries(categoryStats).sort((a, b) => b[1] - a[1]).forEach(([category, count]) => {
            const percentage = ((count / totalVisits) * 100).toFixed(1);
            const colors = {
                'Work': '#3b82f6',
                'Social': '#ec4899',
                'Entertainment': '#f59e0b',
                'Shopping': '#10b981',
                'News': '#8b5cf6',
                'Uncategorized': '#6b7280'
            };
            const color = colors[category] || '#6b7280';
            categoryHTML += `
                <div style="background: ${color}15; border-left: 4px solid ${color}; padding: 12px; border-radius: 8px;">
                    <p style="font-size: 12px; color: #6b7280; margin: 0 0 4px 0;">${category}</p>
                    <p style="font-size: 20px; font-weight: 700; color: ${color}; margin: 0;">${count}</p>
                    <p style="font-size: 11px; color: #6b7280; margin: 4px 0 0 0;">${percentage}%</p>
                </div>
            `;
        });
        categoryHTML += '</div>';
        document.getElementById('analyticsCategoryBreakdown').innerHTML = categoryHTML;

        // Employee activity report
        const employeeStats = {};
        records.forEach(r => {
            const emp = r.display_user_name || r.username || 'Unknown';
            if (!employeeStats[emp]) {
                employeeStats[emp] = { visits: 0, time: 0 };
            }
            employeeStats[emp].visits++;
            employeeStats[emp].time += r.duration_seconds || 0;
        });

        let employeeHTML = '<table style="width: 100%; border-collapse: collapse;">';
        employeeHTML += '<thead><tr style="background: #f1f5f9; border-bottom: 2px solid #e2e8f0;">';
        employeeHTML += '<th style="padding: 12px; text-align: left; font-weight: 600; color: #475569;">👤 Employee</th>';
        employeeHTML += '<th style="padding: 12px; text-align: center; font-weight: 600; color: #475569;">📊 Visits</th>';
        employeeHTML += '<th style="padding: 12px; text-align: center; font-weight: 600; color: #475569;">⏱️ Time (hrs)</th>';
        employeeHTML += '<th style="padding: 12px; text-align: center; font-weight: 600; color: #475569;">⏲️ Avg Duration</th>';
        employeeHTML += '</tr></thead><tbody>';

        Object.entries(employeeStats)
            .sort((a, b) => b[1].visits - a[1].visits)
            .forEach(([emp, stats], index) => {
                const timeHours = (stats.time / 3600).toFixed(2);
                const avgDuration = (stats.time / stats.visits / 60).toFixed(1);
                employeeHTML += `
                    <tr style="border-bottom: 1px solid #e2e8f0; ${index % 2 === 0 ? 'background: #f8fafc;' : ''}">
                        <td style="padding: 12px; color: #1f2937; font-weight: 500;">${emp}</td>
                        <td style="padding: 12px; text-align: center; color: #3b82f6; font-weight: 600;">${stats.visits}</td>
                        <td style="padding: 12px; text-align: center; color: #10b981; font-weight: 600;">${timeHours}h</td>
                        <td style="padding: 12px; text-align: center; color: #f59e0b; font-weight: 600;">${avgDuration}m</td>
                    </tr>
                `;
            });
        employeeHTML += '</tbody></table>';
        document.getElementById('analyticsEmployeeReport').innerHTML = employeeHTML;

        // Web logs table
        const columns = [
            {
                label: '👤 Employee',
                width: '140px',
                render: (record) => `<span style="background: #f0fdf4; color: #166534; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">👤 ${record.display_user_name || record.username || 'N/A'}</span>`
            },
            {
                label: '🌐 Website',
                width: '200px',
                render: (record) => `<a href="${record.website_url}" target="_blank" style="color: #3b82f6; text-decoration: none; font-size: 12px; word-break: break-all;">${record.website_url ? record.website_url.substring(0, 50) + (record.website_url.length > 50 ? '...' : '') : 'N/A'}</a>`
            },
            {
                label: '🏢 Domain',
                width: '120px',
                render: (record) => `<span style="background: #dbeafe; color: #1e40af; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">🏢 ${this.extractDomain(record.website_url)}</span>`
            },
            {
                label: '📂 Category',
                width: '100px',
                render: (record) => {
                    const category = record.category || 'Uncategorized';
                    const colors = { 'Work': '#3b82f6', 'Social': '#ec4899', 'Entertainment': '#f59e0b', 'Shopping': '#10b981', 'News': '#8b5cf6', 'Uncategorized': '#6b7280' };
                    const color = colors[category] || '#6b7280';
                    return `<span style="background: ${color}20; color: ${color}; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">📂 ${category}</span>`;
                }
            },
            {
                label: '⏱️ Duration',
                width: '100px',
                render: (record) => {
                    const minutes = Math.floor((record.duration_seconds || 0) / 60);
                    const seconds = (record.duration_seconds || 0) % 60;
                    return `<span style="background: #fef3c7; color: #92400e; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">⏱️ ${minutes}m ${seconds}s</span>`;
                }
            },
            {
                label: '📅 Timestamp',
                width: '160px',
                render: (record) => `<span style="color: #6b7280; font-size: 12px;">📅 ${this.formatTimestampIST(record.visit_time)}</span>`
            }
        ];

        this.createPaginatedTable(records, columns, 'analyticsWebLogs', 15);
    },

    clearAnalyticsFilter() {
        document.getElementById('analyticsStartDate').value = new Date(Date.now() - 30*24*60*60*1000).toISOString().split('T')[0];
        document.getElementById('analyticsEndDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('analyticsEmployee').value = '';
        document.getElementById('analyticsSummary').innerHTML = `
            <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">📊 Total Visits</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
            </div>
            <div style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">👥 Active Employees</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
            </div>
            <div style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(245, 158, 11, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">⏱️ Total Time Spent</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
            </div>
            <div style="background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); border-radius: 12px; padding: 20px; color: white; box-shadow: 0 4px 12px rgba(14, 165, 233, 0.2);">
                <p style="font-size: 12px; opacity: 0.9; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 8px 0;">🌐 Unique Domains</p>
                <p style="font-size: 32px; font-weight: 700; margin: 0;">-</p>
            </div>
        `;
        document.getElementById('analyticsTopDomains').innerHTML = '<div style="text-align: center; color: #6b7280;">Click "Generate Report" to load data</div>';
        document.getElementById('analyticsCategoryBreakdown').innerHTML = '<div style="text-align: center; color: #6b7280;">Click "Generate Report" to load data</div>';
        document.getElementById('analyticsEmployeeReport').innerHTML = '<div style="text-align: center; color: #6b7280;">Click "Generate Report" to load data</div>';
        document.getElementById('analyticsWebLogs').innerHTML = '<div style="text-align: center; color: #6b7280;">Click "Generate Report" to load data</div>';
    },

    extractDomain(url) {
        if (!url) return 'N/A';
        try {
            const domain = new URL(url).hostname;
            return domain.replace('www.', '');
        } catch (e) {
            return 'N/A';
        }
    },

    // ===== LIVE SYSTEMS VIEW =====
    generateLiveSystemsView() {
        return `
            <div id="live-systems-content" class="content-view" style="padding: 32px; display: none;">
                <div style="margin-bottom: 32px;">
                    <h1 style="font-size: 32px; font-weight: 300; color: #1e293b; margin-bottom: 8px;">🖥️ Live Systems</h1>
                    <p style="color: #64748b; font-size: 16px;">Monitor active employees and their live systems</p>
                </div>

                <!-- Refresh Button -->
                <div style="margin-bottom: 24px;">
                    <button onclick="app.refreshLiveSystems()" style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; font-size: 14px;">
                        🔄 Refresh
                    </button>
                </div>

                <!-- Working/Active Systems Table -->
                <div style="background: white; border-radius: 12px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); margin-bottom: 24px;">
                    <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                        <div style="width: 32px; height: 32px; background: #10b981; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 18px;">●</div>
                        <h2 style="font-size: 18px; font-weight: 600; color: #1f2937;">Active/Working Systems</h2>
                    </div>
                    <div id="activeSystemsTable" style="overflow-x: auto;">
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f9fafb;">
                                <tr>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Employee</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Machine ID</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">System ID</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">IP Address</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Last Heartbeat</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">OS</th>
                                </tr>
                            </thead>
                            <tbody id="activeSystemsBody">
                                <tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">Loading...</td></tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <!-- Idle Systems Table -->
                <div style="background: white; border-radius: 12px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); margin-bottom: 24px;">
                    <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                        <div style="width: 32px; height: 32px; background: #f59e0b; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 18px;">⏸</div>
                        <h2 style="font-size: 18px; font-weight: 600; color: #1f2937;">Idle Systems</h2>
                    </div>
                    <div id="idleSystemsTable" style="overflow-x: auto;">
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f9fafb;">
                                <tr>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Employee</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Machine ID</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">System ID</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">IP Address</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Last Heartbeat</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">OS</th>
                                </tr>
                            </thead>
                            <tbody id="idleSystemsBody">
                                <tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">Loading...</td></tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <!-- Offline Systems Table -->
                <div style="background: white; border-radius: 12px; padding: 24px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                    <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
                        <div style="width: 32px; height: 32px; background: #9ca3af; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 18px;">⊙</div>
                        <h2 style="font-size: 18px; font-weight: 600; color: #1f2937;">Offline Systems</h2>
                    </div>
                    <div id="offlineSystemsTable" style="overflow-x: auto;">
                        <table style="width: 100%; border-collapse: collapse;">
                            <thead style="background: #f9fafb;">
                                <tr>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Employee</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Machine ID</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">System ID</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">IP Address</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">Last Heartbeat</th>
                                    <th style="padding: 12px 16px; text-left; font-weight: 600; color: #374151; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid #e5e7eb;">OS</th>
                                </tr>
                            </thead>
                            <tbody id="offlineSystemsBody">
                                <tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">Loading...</td></tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        `;
    },

    // ===== LIVE SYSTEMS FUNCTIONS =====
    async refreshLiveSystems() {
        const activeTbody = document.getElementById('activeSystemsBody');
        const idleTbody = document.getElementById('idleSystemsBody');
        const offlineTbody = document.getElementById('offlineSystemsBody');
        
        if (!activeTbody || !idleTbody || !offlineTbody) return;

        // Show loading state
        [activeTbody, idleTbody, offlineTbody].forEach(tbody => {
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">Loading...</td></tr>';
        });

        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            // Fetch both connected systems and inactivity data in parallel
            const [systemsResponse, inactivityResponse] = await Promise.all([
                this.api('get_connected_systems', { company_name: companyName }),
                this.api('get_inactivity_logs', { 
                    company_name: companyName,
                    start_date: new Date().toISOString().split('T')[0],
                    end_date: new Date().toISOString().split('T')[0]
                })
            ]);

            // Build inactivity map
            const inactivityMap = {};
            if (inactivityResponse.success && Array.isArray(inactivityResponse.data)) {
                inactivityResponse.data.forEach(log => {
                    const key = (log.employee_id || log.username || '').toLowerCase();
                    if (!inactivityMap[key]) {
                        inactivityMap[key] = { count: 0, lastTime: null };
                    }
                    inactivityMap[key].count++;
                    inactivityMap[key].lastTime = log.timestamp || log.time;
                });
            }

            // Categorize systems
            const activeSystems = [];
            const idleSystems = [];
            const offlineSystems = [];

            if (systemsResponse.success && systemsResponse.data && systemsResponse.data.length > 0) {
                const systems = Array.isArray(systemsResponse.data) ? systemsResponse.data : [systemsResponse.data];
                
                systems.forEach(system => {
                    const isOnline = system.is_online === true || system.is_online === 1;
                    const employeeKey = (system.employee_id || system.display_name || '').toLowerCase();
                    const hasInactivity = inactivityMap[employeeKey];

                    if (isOnline) {
                        if (hasInactivity && hasInactivity.count > 0) {
                            idleSystems.push(system);
                        } else {
                            activeSystems.push(system);
                        }
                    } else {
                        offlineSystems.push(system);
                    }
                });
            }

            // Render Active Systems Table
            if (activeSystems.length > 0) {
                activeTbody.innerHTML = activeSystems.map(system => this.createSystemTableRow(system)).join('');
            } else {
                activeTbody.innerHTML = '<tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">No active systems</td></tr>';
            }

            // Render Idle Systems Table
            if (idleSystems.length > 0) {
                idleTbody.innerHTML = idleSystems.map(system => this.createSystemTableRow(system)).join('');
            } else {
                idleTbody.innerHTML = '<tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">No idle systems</td></tr>';
            }

            // Render Offline Systems Table
            if (offlineSystems.length > 0) {
                offlineTbody.innerHTML = offlineSystems.map(system => this.createSystemTableRow(system)).join('');
            } else {
                offlineTbody.innerHTML = '<tr><td colspan="6" style="padding: 20px; text-align: center; color: #6b7280;">No offline systems</td></tr>';
            }

            // Auto-refresh every 30 seconds
            setTimeout(() => this.refreshLiveSystems(), 30000);

        } catch (error) {
            console.error('Error loading live systems:', error);
            const errorMsg = '<tr><td colspan="6" style="padding: 20px; text-align: center; color: #ef4444;">Error loading data. Please try again.</td></tr>';
            activeTbody.innerHTML = errorMsg;
            idleTbody.innerHTML = errorMsg;
            offlineTbody.innerHTML = errorMsg;
        }
    },

    createSystemTableRow(system) {
        const displayName = system.display_name || system.employee_id || 'Unknown';
        const machineId = system.machine_id || system.machine_name || 'N/A';
        const systemId = system.system_id || 'N/A';
        const ipAddress = system.ip_address || 'N/A';
        const lastHeartbeat = system.last_heartbeat ? new Date(system.last_heartbeat).toLocaleString() : 'Never';
        const osVersion = system.os_version || 'N/A';

        return `
            <tr style="border-bottom: 1px solid #f3f4f6;">
                <td style="padding: 12px 16px; color: #1f2937; font-weight: 500;">
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <div style="width: 32px; height: 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-size: 12px; font-weight: 600;">
                            ${displayName.substring(0, 2).toUpperCase()}
                        </div>
                        <div>
                            <div style="font-weight: 500; color: #3b82f6; cursor: pointer; text-decoration: underline;" onclick="app.openRemoteView('${systemId.replace(/'/g, "\\'")}', '${displayName.replace(/'/g, "\\'")}')">${displayName}</div>
                            <div style="font-size: 12px; color: #6b7280;">${system.employee_id || 'N/A'}</div>
                        </div>
                    </div>
                </td>
                <td style="padding: 12px 16px; color: #374151; font-family: monospace; font-size: 13px;">${machineId}</td>
                <td style="padding: 12px 16px; color: #374151; font-size: 13px;">${systemId}</td>
                <td style="padding: 12px 16px; color: #374151; font-family: monospace; font-size: 13px;">${ipAddress}</td>
                <td style="padding: 12px 16px; color: #6b7280; font-size: 13px;">${lastHeartbeat}</td>
                <td style="padding: 12px 16px; color: #374151; font-size: 13px;">${osVersion}</td>
            </tr>
        `;
    },

    openRemoteView(machineId, employeeName) {
        // Open a modal window showing live remote desktop with frame streaming
        console.log('openRemoteView called with:', machineId, employeeName);
        const modal = document.createElement('div');
        modal.id = 'remoteViewModal';
        modal.style.cssText = `
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: #0a0a0a; display: flex; flex-direction: column;
            z-index: 99999; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        `;
        
        modal.innerHTML = `
            <div style="background: #1e1e1e; padding: 16px 24px; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #333;">
                <div style="display: flex; align-items: center; gap: 16px;">
                    <button class="remote-back-btn" style="background: #333; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-size: 14px;">
                        ← Back
                    </button>
                    <div>
                        <h2 style="color: white; margin: 0; font-size: 18px; font-weight: 600;">${employeeName}</h2>
                        <p style="color: #888; margin: 4px 0 0 0; font-size: 12px;">Live Remote Desktop • Machine: ${machineId}</p>
                    </div>
                </div>
                <div style="display: flex; align-items: center; gap: 12px; font-size: 13px;">
                    <div style="display: flex; align-items: center; gap: 6px; padding: 8px 12px; background: #1a1a1a; border-radius: 6px;">
                        <span class="remote-status-dot" style="width: 8px; height: 8px; background: #f59e0b; border-radius: 50%; animation: pulse 1.5s infinite;"></span>
                        <span class="remote-status-text" style="color: #888;">Connecting...</span>
                    </div>
                    <button class="remote-refresh-btn" style="background: #333; color: white; border: none; padding: 8px 12px; border-radius: 6px; cursor: pointer; font-size: 12px;">🔄</button>
                    <button class="remote-fullscreen-btn" style="background: #333; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;">⛶</button>
                </div>
            </div>
            
            <div style="flex: 1; display: flex; align-items: center; justify-content: center; overflow: auto; background: #000;">
                <div class="remote-screen-view" style="position: relative; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;">
                    <div class="remote-loading-state" style="text-align: center; color: #888;">
                        <div style="font-size: 48px; margin-bottom: 16px;">🖥️</div>
                        <p style="font-size: 16px; margin-bottom: 8px;">Live Remote Desktop Stream</p>
                        <p style="font-size: 13px;">Connecting to ${employeeName}'s machine...</p>
                        <div style="margin-top: 24px; display: flex; justify-content: center; gap: 8px;">
                            <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite;"></div>
                            <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite 0.5s;"></div>
                            <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite 1s;"></div>
                        </div>
                        <p style="font-size: 12px; color: #666; margin-top: 16px;">⚠️ Live streaming requires the remote system to be active and the desktop controller client running</p>
                    </div>
                    <img class="remote-screen-img" style="max-width: 100%; max-height: 100%; display: none; object-fit: contain;">
                </div>
            </div>
            
            <div style="background: #1e1e1e; padding: 10px 24px; border-top: 1px solid #333; display: flex; justify-content: space-between; font-size: 12px; color: #888;">
                <div style="display: flex; gap: 20px;">
                    <div>Resolution: <span class="remote-resolution" style="color: white;">-</span></div>
                    <div>Last Update: <span class="remote-last-update" style="color: white;">-</span></div>
                </div>
                <div class="remote-refresh-rate" style="color: #888;">Refresh: <span style="color: white;">2s</span></div>
            </div>
            
            <style>
                @keyframes pulse {
                    0%, 100% { opacity: 0.3; }
                    50% { opacity: 1; }
                }
            </style>
        `;
        
        document.body.appendChild(modal);
        
        // Initialize remote viewing
        const context = {
            modal: modal,
            machineId: machineId,
            employeeName: employeeName,
            refreshInterval: null,
            isConnected: false,
            app: this  // Add reference to app instance
        };
        
        // Set up event listeners
        modal.querySelector('.remote-back-btn').onclick = () => {
            if (context.refreshInterval) clearInterval(context.refreshInterval);
            context.app.api('stop_viewing', { system_id: context.machineId });
            modal.remove();
        };
        modal.querySelector('.remote-refresh-btn').onclick = () => fetchRemoteFrame(context);
        modal.querySelector('.remote-fullscreen-btn').onclick = () => toggleRemoteFullscreen(context);
        
        // Start viewing session
        startRemoteViewing(context);
    },

    // ===== LIVE STREAM MODAL FUNCTION =====
    openLiveStream(machineId, employeeName, remoteId) {
        console.log('Opening live stream for:', { machineId, employeeName, remoteId });
        
        // Create modal container
        const modal = document.createElement('div');
        modal.id = 'liveStreamModal';
        modal.style.cssText = `
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0, 0, 0, 0.95); display: flex; flex-direction: column;
            z-index: 99999; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            animation: fadeIn 0.3s ease;
        `;

        const headerHeight = '80px';
        const footerHeight = '60px';

        modal.innerHTML = `
            <!-- Header -->
            <div style="background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding: 16px 24px; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #334155; height: ${headerHeight}; box-sizing: border-box;">
                <div style="display: flex; align-items: center; gap: 16px;">
                    <button class="stream-back-btn" style="background: #334155; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-size: 14px; transition: all 0.2s ease;">
                        ← Back
                    </button>
                    <div>
                        <h2 style="color: white; margin: 0; font-size: 18px; font-weight: 600;">🎬 Live Stream</h2>
                        <p style="color: #94a3b8; margin: 4px 0 0 0; font-size: 12px;">${employeeName} • Machine: ${machineId}</p>
                    </div>
                </div>
                <div style="display: flex; align-items: center; gap: 12px;">
                    <div style="display: flex; align-items: center; gap: 6px; padding: 8px 12px; background: #1e293b; border-radius: 6px; border: 1px solid #334155;">
                        <span class="stream-status-dot" style="width: 10px; height: 10px; background: #f59e0b; border-radius: 50%; animation: pulse 1.5s infinite;"></span>
                        <span class="stream-status-text" style="color: #f59e0b; font-weight: 600; font-size: 12px;">Connecting...</span>
                    </div>
                    <button class="stream-refresh-btn" style="background: #334155; color: white; border: none; padding: 8px 12px; border-radius: 6px; cursor: pointer; font-size: 12px; transition: all 0.2s ease;">🔄 Refresh</button>
                </div>
            </div>

            <!-- Stream Container -->
            <div class="stream-content" style="flex: 1; display: flex; align-items: center; justify-content: center; overflow: hidden; background: #000;">
                <div class="stream-viewer" style="position: relative; width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;">
                    <div class="stream-placeholder" style="text-align: center; color: #94a3b8;">
                        <div style="font-size: 64px; margin-bottom: 16px; animation: bounce 2s infinite;">🎥</div>
                        <p style="font-size: 18px; margin-bottom: 8px; color: white;">Connecting to Live Stream...</p>
                        <p style="font-size: 14px; margin-bottom: 24px;">Establishing connection with Remote ID: <span style="color: #3b82f6; font-family: monospace; font-weight: 600;">${remoteId || machineId}</span></p>
                        <div style="display: flex; justify-content: center; gap: 8px; margin-bottom: 20px;">
                            <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite;"></div>
                            <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite 0.5s;"></div>
                            <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite 1s;"></div>
                        </div>
                        <div style="background: #1e293b; border-radius: 8px; padding: 12px; margin-top: 20px; border-left: 4px solid #3b82f6;">
                            <p style="font-size: 12px; color: #94a3b8; margin: 0;">
                                ℹ️ <strong>Note:</strong> Live streaming requires the remote system to be online and the monitoring application to be running.
                            </p>
                        </div>
                    </div>
                    <video class="stream-video" style="max-width: 100%; max-height: 100%; display: none; object-fit: contain;"></video>
                    <canvas class="stream-canvas" style="max-width: 100%; max-height: 100%; display: none; background: #000;"></canvas>
                </div>
            </div>

            <!-- Footer -->
            <div style="background: #1e293b; padding: 12px 24px; border-top: 1px solid #334155; display: flex; justify-content: space-between; align-items: center; height: ${footerHeight}; box-sizing: border-box;">
                <div style="display: flex; gap: 20px; font-size: 12px; color: #94a3b8;">
                    <div>Remote ID: <span style="color: white; font-family: monospace;">${remoteId || machineId}</span></div>
                    <div>Status: <span class="stream-footer-status" style="color: #f59e0b; font-weight: 600;">Connecting</span></div>
                    <div>Connection: <span class="stream-connection-type" style="color: white;">WebSocket</span></div>
                </div>
                <div style="display: flex; gap: 8px;">
                    <button class="stream-fullscreen-btn" style="background: #334155; color: white; border: none; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 12px; transition: all 0.2s ease;">⛶ Fullscreen</button>
                </div>
            </div>

            <style>
                @keyframes fadeIn {
                    from { opacity: 0; }
                    to { opacity: 1; }
                }
                @keyframes pulse {
                    0%, 100% { opacity: 0.3; transform: scale(1); }
                    50% { opacity: 1; transform: scale(1.1); }
                }
                @keyframes bounce {
                    0%, 100% { transform: translateY(0); }
                    50% { transform: translateY(-10px); }
                }
                .stream-back-btn:hover {
                    background: #475569;
                    transform: translateX(-2px);
                }
                .stream-refresh-btn:hover {
                    background: #475569;
                    transform: rotate(20deg);
                }
                .stream-fullscreen-btn:hover {
                    background: #475569;
                }
            </style>
        `;

        document.body.appendChild(modal);

        // Get references to elements
        const backBtn = modal.querySelector('.stream-back-btn');
        const refreshBtn = modal.querySelector('.stream-refresh-btn');
        const fullscreenBtn = modal.querySelector('.stream-fullscreen-btn');
        const statusDot = modal.querySelector('.stream-status-dot');
        const statusText = modal.querySelector('.stream-status-text');
        const footerStatus = modal.querySelector('.stream-footer-status');
        const viewer = modal.querySelector('.stream-viewer');
        const placeholder = modal.querySelector('.stream-placeholder');
        const streamVideo = modal.querySelector('.stream-video');
        const streamCanvas = modal.querySelector('.stream-canvas');

        // Store stream context
        const streamContext = {
            modal: modal,
            machineId: machineId,
            remoteId: remoteId,
            employeeName: employeeName,
            isConnected: false,
            socketConnection: null
        };

        // Close modal function
        const closeStream = () => {
            // Stop refresh interval
            if (streamContext.refreshInterval) {
                clearInterval(streamContext.refreshInterval);
            }

            // Stop viewing session on server
            if (streamContext.isConnected) {
                fetch('/api.php?action=stop_viewing', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        system_id: streamContext.machineId,
                        remote_id: streamContext.remoteId,
                        viewer_id: 'admin'
                    })
                }).catch(err => console.error('Error stopping stream:', err));
            }

            if (streamContext.socketConnection) {
                streamContext.socketConnection.close();
            }
            modal.style.animation = 'fadeOut 0.3s ease';
            setTimeout(() => modal.remove(), 300);
        };

        // Back button
        backBtn.addEventListener('click', closeStream);

        // Close on backdrop click
        modal.addEventListener('click', (e) => {
            if (e.target === modal) closeStream();
        });

        // ESC key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal.parentNode) closeStream();
        });

        // Fullscreen button
        fullscreenBtn.addEventListener('click', () => {
            const elem = viewer;
            if (elem.requestFullscreen) {
                elem.requestFullscreen();
            } else if (elem.webkitRequestFullscreen) {
                elem.webkitRequestFullscreen();
            }
        });

        // Refresh button
        refreshBtn.addEventListener('click', () => {
            refreshBtn.style.animation = 'rotate 0.6s ease';
            this.initializeLiveStreamConnection(streamContext);
        });

        // Initialize connection
        this.initializeLiveStreamConnection(streamContext);
    },

    initializeLiveStreamConnection(context) {
        try {
            const statusDot = context.modal.querySelector('.stream-status-dot');
            const statusText = context.modal.querySelector('.stream-status-text');
            const footerStatus = context.modal.querySelector('.stream-footer-status');

            // Update status to connecting
            statusDot.style.background = '#f59e0b';
            statusText.textContent = 'Connecting...';
            statusText.style.color = '#f59e0b';
            footerStatus.textContent = 'Connecting';
            footerStatus.style.color = '#f59e0b';

            // Start live stream session
            this.startLiveStreamSession(context);

        } catch (error) {
            console.error('Stream connection error:', error);
            const statusDot = context.modal.querySelector('.stream-status-dot');
            const statusText = context.modal.querySelector('.stream-status-text');
            const footerStatus = context.modal.querySelector('.stream-footer-status');

            statusDot.style.background = '#ef4444';
            statusText.textContent = 'Connection Failed';
            statusText.style.color = '#ef4444';
            footerStatus.textContent = 'Error';
            footerStatus.style.color = '#ef4444';

            const placeholder = context.modal.querySelector('.stream-placeholder');
            placeholder.innerHTML = `
                <div style="font-size: 48px; margin-bottom: 16px;">❌</div>
                <p style="font-size: 18px; color: #ef4444; margin-bottom: 8px;">Connection Failed</p>
                <p style="font-size: 14px; color: #94a3b8; margin-bottom: 20px;">Could not establish connection to system</p>
                <button onclick="location.reload()" style="background: #ef4444; color: white; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer; font-weight: 600;">Retry Connection</button>
            `;
        }
    },

    async startLiveStreamSession(context) {
        try {
            // Initialize stream viewing on the server
            const response = await fetch('/api.php?action=start_viewing', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    system_id: context.machineId,
                    remote_id: context.remoteId,
                    viewer_id: 'admin'
                })
            });

            const result = await response.json();

            if (result.success) {
                context.isConnected = true;
                context.attemptCount = 0;
                context.maxAttempts = 30; // Wait up to 60 seconds for first frame

                // Update status
                const statusDot = context.modal.querySelector('.stream-status-dot');
                const statusText = context.modal.querySelector('.stream-status-text');
                const footerStatus = context.modal.querySelector('.stream-footer-status');

                statusDot.style.background = '#10b981';
                statusText.textContent = 'Stream Ready';
                statusText.style.color = '#10b981';
                footerStatus.textContent = 'Connected';
                footerStatus.style.color = '#10b981';

                // Start fetching live frames
                context.refreshInterval = setInterval(() => {
                    this.fetchLiveStreamFrame(context);
                }, 1500); // Fetch every 1.5 seconds

                // Fetch first frame immediately
                this.fetchLiveStreamFrame(context);
            } else {
                this.showStreamError(context, 'Failed to start stream session on server');
            }
        } catch (error) {
            console.error('Stream session error:', error);
            this.showStreamError(context, 'Error connecting to stream: ' + error.message);
        }
    },

    async fetchLiveStreamFrame(context) {
        try {
            context.attemptCount = (context.attemptCount || 0) + 1;

            const response = await fetch(`/api.php?action=get_live_frame&system_id=${encodeURIComponent(context.machineId)}&remote_id=${encodeURIComponent(context.remoteId || '')}`);
            const result = await response.json();

            if (result.success && result.data && result.data.frame) {
                // Reset attempt count on success
                context.attemptCount = 0;

                // Display the frame
                const viewer = context.modal.querySelector('.stream-viewer');
                const placeholder = context.modal.querySelector('.stream-placeholder');
                const canvas = context.modal.querySelector('.stream-canvas');

                // Hide placeholder on first successful frame
                if (placeholder.style.display !== 'none') {
                    placeholder.style.display = 'none';
                }

                // Show canvas and display image
                if (canvas) {
                    canvas.style.display = 'block';
                    const img = new Image();
                    img.onload = () => {
                        const ctx = canvas.getContext('2d');
                        canvas.width = img.width;
                        canvas.height = img.height;
                        ctx.drawImage(img, 0, 0);
                    };
                    img.src = 'data:image/jpeg;base64,' + result.data.frame;
                }

                // Update footer info
                if (result.data.width && result.data.height) {
                    const resolutionElem = context.modal.querySelector('.stream-resolution');
                    if (resolutionElem) {
                        resolutionElem.textContent = result.data.width + ' x ' + result.data.height;
                    }
                }

            } else if (result.waiting) {
                // Still waiting for data - show waiting state
                if (context.attemptCount < 3) {
                    const placeholder = context.modal.querySelector('.stream-placeholder');
                    if (placeholder) {
                        placeholder.innerHTML = `
                            <div style="font-size: 48px; margin-bottom: 16px;">📡</div>
                            <p style="font-size: 16px; margin-bottom: 8px; color: white;">Waiting for Screen Data</p>
                            <p style="font-size: 12px; color: #94a3b8; margin-bottom: 20px;">The remote system is capturing screen. Please wait...</p>
                            <div style="display: flex; justify-content: center; gap: 8px;">
                                <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite;"></div>
                                <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite 0.5s;"></div>
                                <div style="width: 12px; height: 12px; background: #3b82f6; border-radius: 50%; animation: pulse 1.5s infinite 1s;"></div>
                            </div>
                        `;
                    }
                }
            }
        } catch (error) {
            console.error('Frame fetch error:', error);
            // Continue retrying silently
        }
    },

    showStreamError(context, message) {
        const statusDot = context.modal.querySelector('.stream-status-dot');
        const statusText = context.modal.querySelector('.stream-status-text');
        const footerStatus = context.modal.querySelector('.stream-footer-status');

        statusDot.style.background = '#ef4444';
        statusText.textContent = 'Connection Failed';
        statusText.style.color = '#ef4444';
        footerStatus.textContent = 'Error';
        footerStatus.style.color = '#ef4444';

        const placeholder = context.modal.querySelector('.stream-placeholder');
        placeholder.innerHTML = `
            <div style="font-size: 48px; margin-bottom: 16px;">❌</div>
            <p style="font-size: 18px; color: #ef4444; margin-bottom: 8px;">Connection Failed</p>
            <p style="font-size: 14px; color: #94a3b8; margin-bottom: 20px;">${message}</p>
            <button onclick="document.getElementById('liveStreamModal')?.remove()" style="background: #ef4444; color: white; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer; font-weight: 600;">Close Stream</button>
        `;
    },

    showScreenshotModal(screenshotUrl, employeeName) {
        const modal = document.createElement('div');
        modal.style.cssText = `
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0, 0, 0, 0.8); display: flex; align-items: center;
            justify-content: center; z-index: 9999;
        `;
        modal.innerHTML = `
            <div style="background: white; border-radius: 12px; padding: 20px; max-width: 90vw; max-height: 90vh; overflow: auto;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
                    <h2 style="color: #1f2937; font-size: 18px; font-weight: 600;">Live Screenshot - ${employeeName}</h2>
                    <button onclick="this.closest('div').style.display='none'; this.parentElement.parentElement.remove();" style="background: none; border: none; font-size: 24px; cursor: pointer;">✕</button>
                </div>
                <img src="${screenshotUrl}" alt="Screenshot" style="max-width: 100%; max-height: 80vh; border-radius: 8px;">
            </div>
        `;
        document.body.appendChild(modal);
        modal.onclick = () => modal.remove();
    },

    // ===== EMPLOYEE MANAGEMENT FUNCTIONS =====
    switchEmployeeTab(tab) {
        const empTab = document.getElementById('employees-tab');
        const deptTab = document.getElementById('departments-tab');
        const empBtn = document.getElementById('emp-tab-btn');
        const deptBtn = document.getElementById('dept-tab-btn');

        if (tab === 'employees') {
            empTab.style.display = 'block';
            deptTab.style.display = 'none';
            empBtn.style.color = '#667eea';
            empBtn.style.borderBottomColor = '#667eea';
            deptBtn.style.color = '#6b7280';
            deptBtn.style.borderBottom = 'none';
        } else {
            empTab.style.display = 'none';
            deptTab.style.display = 'block';
            empBtn.style.color = '#6b7280';
            empBtn.style.borderBottom = 'none';
            deptBtn.style.color = '#667eea';
            deptBtn.style.borderBottomColor = '#667eea';
        }
    },

    async loadEmployeesList() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const container = document.getElementById('employeesContainer');
            
            container.innerHTML = '<div style="padding: 40px; text-align: center; color: #64748b;">⏳ Loading employees...</div>';

            const response = await this.api('get_employees', {
                company_name: companyName
            });

            if (response.success && response.data) {
                const employees = Array.isArray(response.data) ? response.data : response.data.records || [];
                this.displayEmployeesList(employees);
            } else {
                container.innerHTML = '<div style="padding: 40px; text-align: center; color: #ef4444;">⚠️ Failed to load employees</div>';
            }
        } catch (error) {
            console.error('Error loading employees:', error);
            document.getElementById('employeesContainer').innerHTML = '<div style="padding: 40px; text-align: center; color: #ef4444;">⚠️ Error loading employees</div>';
        }
    },

    displayEmployeesList(employees) {
        const container = document.getElementById('employeesContainer');

        if (!employees || employees.length === 0) {
            container.innerHTML = '<div style="padding: 40px; text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">👤</div><p>No employees found</p></div>';
            return;
        }

        const html = `
            <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                <thead>
                    <tr style="background: #f9fafb; border-bottom: 2px solid #e5e7eb;">
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Employee ID</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Full Name</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Email</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Phone</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Department</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Designation</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Status</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${employees.map(emp => `
                        <tr style="border-bottom: 1px solid #e5e7eb;">
                            <td style="padding: 12px; color: #1f2937; font-weight: 500;">${emp.employee_id || 'N/A'}</td>
                            <td style="padding: 12px; color: #1f2937; font-weight: 500;">${emp.full_name || 'N/A'}</td>
                            <td style="padding: 12px; color: #6b7280; font-size: 13px;">${emp.email || 'N/A'}</td>
                            <td style="padding: 12px; color: #6b7280; font-size: 13px;">${emp.phone || 'N/A'}</td>
                            <td style="padding: 12px; color: #6b7280;">${emp.department || 'N/A'}</td>
                            <td style="padding: 12px; color: #6b7280;">${emp.designation || 'N/A'}</td>
                            <td style="padding: 12px;">
                                <span style="background: ${emp.is_active ? '#dcfce7' : '#f3f4f6'}; color: ${emp.is_active ? '#166534' : '#6b7280'}; padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">
                                    ${emp.is_active ? 'Active' : 'Inactive'}
                                </span>
                            </td>
                            <td style="padding: 12px;">
                                <button onclick="app.editEmployee(${emp.id})" style="padding: 4px 8px; background: #dbeafe; color: #0c4a6e; border: none; border-radius: 4px; cursor: pointer; font-size: 11px; font-weight: 600; margin-right: 4px;">Edit</button>
                                <button onclick="app.deleteEmployee(${emp.id})" style="padding: 4px 8px; background: #fee2e2; color: #991b1b; border: none; border-radius: 4px; cursor: pointer; font-size: 11px; font-weight: 600;">Delete</button>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;

        container.innerHTML = html;
    },

    async showAddEmployeeForm() {
        try {
            // Load departments from database
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const deptResponse = await this.api('get_departments', { company_name: companyName });
            
            let departmentOptions = '<option value="">Select Department</option>';
            if (deptResponse.success && deptResponse.data) {
                departmentOptions += deptResponse.data.map(dept => 
                    `<option value="${dept.department_name}">${dept.department_name}</option>`
                ).join('');
            }

            const formHTML = `
                <div id="addEmployeeModal" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.5); display: flex; align-items: center; justify-content: center; z-index: 10000; overflow-y: auto; padding: 20px;">
                    <div style="background: white; border-radius: 12px; max-width: 600px; width: 100%; box-shadow: 0 20px 25px rgba(0, 0, 0, 0.15); max-height: 90vh; overflow-y: auto;">
                        
                        <!-- Header -->
                        <div style="padding: 24px; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center;">
                            <h2 style="font-size: 20px; font-weight: 600; color: #1f2937; margin: 0;">Add New Employee</h2>
                            <button onclick="document.getElementById('addEmployeeModal').remove()" style="background: #f3f4f6; border: none; border-radius: 6px; padding: 8px 12px; cursor: pointer; font-size: 18px; color: #6b7280;">✕</button>
                        </div>

                        <!-- Form Content -->
                        <div style="padding: 24px;">
                            <!-- Personal Information Section -->
                            <div style="margin-bottom: 24px;">
                                <h3 style="color: #f59e0b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 16px;">👤 Basic Information</h3>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Full Name *</label>
                                        <input type="text" id="emp_full_name" placeholder="John Doe" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Email Address *</label>
                                        <input type="email" id="emp_email" placeholder="john@company.com" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Phone Number *</label>
                                        <input type="tel" id="emp_phone" placeholder="9876543210" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Department *</label>
                                        <select id="emp_department" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white; box-sizing: border-box;">
                                            ${departmentOptions}
                                        </select>
                                    </div>
                                </div>
                            </div>

                            <!-- Work Information Section -->
                            <div style="margin-bottom: 24px;">
                                <h3 style="color: #f59e0b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 16px;">💼 Work Information</h3>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Designation / Role</label>
                                        <input type="text" id="emp_designation" placeholder="Software Engineer" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Lunch Duration (minutes)</label>
                                        <input type="number" id="emp_lunch_duration" placeholder="60" min="0" value="60" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Significant Idle Threshold (minutes)</label>
                                        <input type="number" id="emp_idle_threshold" placeholder="10" min="5" value="10" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                        <p style="margin-top: 4px; font-size: 12px; color: #6b7280;">Idle periods ≥ this threshold will be deducted (default: 10 min)</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Form Actions -->
                        <div style="padding: 16px 24px; border-top: 1px solid #e5e7eb; display: flex; gap: 12px; justify-content: flex-end;">
                            <button onclick="document.getElementById('addEmployeeModal').remove()" style="padding: 10px 20px; background: #f3f4f6; border: none; border-radius: 6px; cursor: pointer; color: #374151; font-weight: 600;">Cancel</button>
                            <button onclick="app.saveNewEmployee()" style="padding: 10px 20px; background: #3b82f6; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600;">Save Employee</button>
                        </div>
                    </div>
                </div>
            `;

            document.body.insertAdjacentHTML('beforeend', formHTML);
        } catch (err) {
            console.error('Error loading form:', err);
            alert('Failed to load form. Please try again.');
        }
    },

    async saveNewEmployee() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            const formData = {
                full_name: document.getElementById('emp_full_name').value,
                email: document.getElementById('emp_email').value,
                phone: document.getElementById('emp_phone').value,
                department: document.getElementById('emp_department').value,
                designation: document.getElementById('emp_designation').value,
                lunch_duration: parseInt(document.getElementById('emp_lunch_duration').value) || 60,
                significant_idle_threshold_minutes: parseInt(document.getElementById('emp_idle_threshold').value) || 10,
                company_name: companyName
            };

            // Validate required fields
            if (!formData.full_name || !formData.email || !formData.phone) {
                this.showToast('Please fill in all required fields', 'error');
                return;
            }

            const response = await this.api('add_employee', formData);

            if (response.success) {
                this.showToast('Employee added successfully! ID: ' + response.data.employee_id, 'success');
                document.getElementById('addEmployeeModal').remove();
                this.loadEmployeesList();
            } else {
                this.showToast(response.error || 'Failed to save employee', 'error');
            }
        } catch (error) {
            console.error('Error saving employee:', error);
            this.showToast('Error saving employee', 'error');
        }
    },

    async editEmployee(id) {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            
            // Fetch employee details with company name
            const empResponse = await this.api('get_employees', { company_name: companyName, id });
            
            if (!empResponse.success || !empResponse.data || empResponse.data.length === 0) {
                this.showToast('Employee not found', 'error');
                return;
            }

            const employee = empResponse.data[0];
            
            // Load departments for the dropdown
            const deptResponse = await this.api('get_departments', { company_name: companyName });
            
            let departmentOptions = '<option value="">Select Department</option>';
            if (deptResponse.success && deptResponse.data) {
                departmentOptions += deptResponse.data.map(dept => 
                    `<option value="${dept.department_name}" ${dept.department_name === employee.department ? 'selected' : ''}>${dept.department_name}</option>`
                ).join('');
            }

            const formHTML = `
                <div id="editEmployeeModal" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.5); display: flex; align-items: center; justify-content: center; z-index: 10000; overflow-y: auto; padding: 20px;">
                    <div style="background: white; border-radius: 12px; max-width: 600px; width: 100%; box-shadow: 0 20px 25px rgba(0, 0, 0, 0.15); max-height: 90vh; overflow-y: auto;">
                        
                        <!-- Header -->
                        <div style="padding: 24px; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center;">
                            <h2 style="font-size: 20px; font-weight: 600; color: #1f2937; margin: 0;">Edit Employee</h2>
                            <button onclick="document.getElementById('editEmployeeModal').remove()" style="background: #f3f4f6; border: none; border-radius: 6px; padding: 8px 12px; cursor: pointer; font-size: 18px; color: #6b7280;">✕</button>
                        </div>

                        <!-- Form Content -->
                        <div style="padding: 24px;">
                            <!-- Personal Information Section -->
                            <div style="margin-bottom: 24px;">
                                <h3 style="color: #f59e0b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 16px;">👤 Basic Information</h3>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Employee ID</label>
                                        <input type="text" id="edit_emp_id" placeholder="EMP001" value="${employee.employee_id || ''}" readonly style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box; background: #f3f4f6; color: #6b7280;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Full Name *</label>
                                        <input type="text" id="edit_emp_full_name" placeholder="John Doe" value="${employee.full_name || ''}" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Email Address *</label>
                                        <input type="email" id="edit_emp_email" placeholder="john@company.com" value="${employee.email || ''}" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Phone Number *</label>
                                        <input type="tel" id="edit_emp_phone" placeholder="9876543210" value="${employee.phone || ''}" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Department *</label>
                                        <select id="edit_emp_department" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; background: white; box-sizing: border-box;">
                                            ${departmentOptions}
                                        </select>
                                    </div>
                                </div>
                            </div>

                            <!-- Work Information Section -->
                            <div style="margin-bottom: 24px;">
                                <h3 style="color: #f59e0b; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 16px;">💼 Work Information</h3>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Designation / Role</label>
                                        <input type="text" id="edit_emp_designation" placeholder="Software Engineer" value="${employee.designation || ''}" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Lunch Duration (minutes)</label>
                                        <input type="number" id="edit_emp_lunch_duration" placeholder="60" min="0" value="${employee.lunch_duration || 60}" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                    </div>
                                    <div>
                                        <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Significant Idle Threshold (minutes)</label>
                                        <input type="number" id="edit_emp_idle_threshold" placeholder="10" min="5" value="${employee.significant_idle_threshold_minutes || 10}" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                                        <p style="margin-top: 4px; font-size: 12px; color: #6b7280;">Idle periods ≥ this threshold will be deducted</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Footer -->
                        <div style="padding: 24px; border-top: 1px solid #e5e7eb; background: #f9fafb; display: flex; gap: 12px; justify-content: flex-end; border-radius: 0 0 12px 12px;">
                            <button onclick="document.getElementById('editEmployeeModal').remove()" style="padding: 10px 20px; background: #f3f4f6; color: #374151; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 14px;">Cancel</button>
                            <button onclick="app.saveEmployeeChanges(${employee.id})" style="padding: 10px 20px; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 14px;">💾 Save Changes</button>
                        </div>
                    </div>
                </div>
            `;

            document.body.insertAdjacentHTML('beforeend', formHTML);
        } catch (error) {
            console.error('Error loading employee for edit:', error);
            this.showToast('Error loading employee details', 'error');
        }
    },

    async saveEmployeeChanges(id) {
        try {
            const employeeId = document.getElementById('edit_emp_id').value.trim();
            const fullName = document.getElementById('edit_emp_full_name').value.trim();
            const email = document.getElementById('edit_emp_email').value.trim();
            const phone = document.getElementById('edit_emp_phone').value.trim();
            const department = document.getElementById('edit_emp_department').value.trim();
            const designation = document.getElementById('edit_emp_designation').value.trim();
            const lunchDuration = parseInt(document.getElementById('edit_emp_lunch_duration').value) || 60;
            const idleThreshold = parseInt(document.getElementById('edit_emp_idle_threshold').value) || 10;

            if (!fullName || !email || !phone || !department) {
                this.showToast('Please fill in all required fields', 'error');
                return;
            }

            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';

            const formData = new FormData();
            formData.append('id', id);
            formData.append('company_name', companyName);
            formData.append('employee_id', employeeId);
            formData.append('full_name', fullName);
            formData.append('email', email);
            formData.append('phone', phone);
            formData.append('department', department);
            formData.append('designation', designation);
            formData.append('lunch_duration', lunchDuration);
            formData.append('significant_idle_threshold_minutes', idleThreshold);

            const response = await this.api('update_employee', Object.fromEntries(formData));

            if (response.success) {
                document.getElementById('editEmployeeModal').remove();
                this.showToast('Employee updated successfully', 'success');
                this.loadEmployeesList();
            } else {
                this.showToast(response.error || 'Failed to update employee', 'error');
            }
        } catch (error) {
            console.error('Error saving employee changes:', error);
            this.showToast('Error saving employee changes', 'error');
        }
    },

    async deleteEmployee(id) {
        if (confirm('Are you sure you want to delete this employee?')) {
            try {
                const response = await this.api('delete_employee', { id });
                if (response.success) {
                    this.showToast('Employee deleted successfully', 'success');
                    this.loadEmployeesList();
                } else {
                    this.showToast(response.error || 'Failed to delete employee', 'error');
                }
            } catch (error) {
                console.error('Error deleting employee:', error);
                this.showToast('Error deleting employee', 'error');
            }
        }
    },

    async loadDepartmentsList() {
        try {
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';
            const container = document.getElementById('departmentsContainer');
            
            container.innerHTML = '<div style="padding: 40px; text-align: center; color: #64748b;">⏳ Loading departments...</div>';

            const response = await this.api('get_departments', {
                company_name: companyName
            });

            if (response.success && response.data) {
                this.displayDepartmentsList(response.data);
            } else {
                container.innerHTML = '<div style="padding: 40px; text-align: center; color: #ef4444;">⚠️ Failed to load departments</div>';
            }
        } catch (error) {
            console.error('Error loading departments:', error);
            document.getElementById('departmentsContainer').innerHTML = '<div style="padding: 40px; text-align: center; color: #ef4444;">⚠️ Error loading departments</div>';
        }
    },

    displayDepartmentsList(departments) {
        const container = document.getElementById('departmentsContainer');

        if (!departments || departments.length === 0) {
            container.innerHTML = '<div style="padding: 40px; text-align: center; color: #6b7280;"><div style="margin-bottom: 16px;">🏢</div><p>No departments found</p></div>';
            return;
        }

        const html = `
            <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                <thead>
                    <tr style="background: #f9fafb; border-bottom: 2px solid #e5e7eb;">
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Department Name</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Company</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Created</th>
                        <th style="padding: 12px; text-align: left; font-weight: 600; color: #374151;">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${departments.map(dept => `
                        <tr style="border-bottom: 1px solid #e5e7eb;">
                            <td style="padding: 12px; color: #1f2937; font-weight: 500;">${dept.department_name || 'N/A'}</td>
                            <td style="padding: 12px; color: #6b7280;">${dept.company_name || 'N/A'}</td>
                            <td style="padding: 12px; color: #6b7280; font-size: 13px;">${dept.created_at ? new Date(dept.created_at).toLocaleDateString('en-IN') : 'N/A'}</td>
                            <td style="padding: 12px;">
                                <button onclick="alert('Edit department functionality coming soon')" style="padding: 4px 8px; background: #dbeafe; color: #0c4a6e; border: none; border-radius: 4px; cursor: pointer; font-size: 11px; font-weight: 600; margin-right: 4px;">Edit</button>
                                <button onclick="app.deleteDepartment(${dept.id})" style="padding: 4px 8px; background: #fee2e2; color: #991b1b; border: none; border-radius: 4px; cursor: pointer; font-size: 11px; font-weight: 600;">Delete</button>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;

        container.innerHTML = html;
    },

    showAddDepartmentForm() {
        const formHTML = `
            <div id="addDepartmentModal" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.5); display: flex; align-items: center; justify-content: center; z-index: 10000;">
                <div style="background: white; border-radius: 12px; width: 450px; box-shadow: 0 20px 25px rgba(0, 0, 0, 0.15);">
                    
                    <!-- Header -->
                    <div style="padding: 24px; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center;">
                        <h2 style="font-size: 20px; font-weight: 600; color: #1f2937; margin: 0;">Add New Department</h2>
                        <button onclick="document.getElementById('addDepartmentModal').remove()" style="background: #f3f4f6; border: none; border-radius: 6px; padding: 8px 12px; cursor: pointer; font-size: 18px; color: #6b7280;">✕</button>
                    </div>

                    <!-- Form Content -->
                    <div style="padding: 24px;">
                        <div>
                            <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Department Name *</label>
                            <input type="text" id="dept_name" placeholder="e.g., Software Engineering" style="width: 100%; padding: 12px; border: 1px solid #d1d5db; border-radius: 8px; font-size: 14px; box-sizing: border-box;">
                        </div>
                    </div>

                    <!-- Footer with Buttons -->
                    <div style="padding: 16px 24px; border-top: 1px solid #e5e7eb; background: #f9fafb; display: flex; gap: 12px; justify-content: flex-end;">
                        <button onclick="document.getElementById('addDepartmentModal').remove()" style="padding: 10px 20px; background: white; border: 1px solid #d1d5db; border-radius: 8px; cursor: pointer; font-weight: 500; color: #374151;">Cancel</button>
                        <button onclick="app.saveNewDepartment()" style="padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 500;">Save Department</button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', formHTML);
    },

    async saveNewDepartment() {
        try {
            const deptName = document.getElementById('dept_name').value;
            const companyName = this.userData?.company_name || 'WIZONE IT NETWORK INDIA PVT LTD';

            if (!deptName) {
                this.showToast('Please enter department name', 'error');
                return;
            }

            const response = await this.api('add_department', {
                department_name: deptName,
                company_name: companyName
            });

            if (response.success) {
                this.showToast('Department added successfully', 'success');
                document.getElementById('addDepartmentModal').remove();
                this.loadDepartmentsList();
            } else {
                this.showToast(response.error || 'Failed to save department', 'error');
            }
        } catch (error) {
            console.error('Error saving department:', error);
            this.showToast('Error saving department', 'error');
        }
    },

    async deleteDepartment(id) {
        if (confirm('Are you sure you want to delete this department?')) {
            this.showToast('Delete functionality coming soon', 'info');
        }
    },

    // ===== SCREENSHOT MODAL FUNCTION =====
    showScreenshotModal(employee, timestamp, imageData, width, height) {
        // Create modal HTML
        const modalHTML = `
            <div id="screenshotModal" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.7); display: flex; align-items: center; justify-content: center; z-index: 9999;">
                <div style="background: white; border-radius: 12px; max-width: 90vw; max-height: 90vh; overflow: auto; box-shadow: 0 20px 25px rgba(0, 0, 0, 0.15);">
                    <!-- Header -->
                    <div style="padding: 20px; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center;">
                        <div>
                            <h2 style="font-size: 18px; font-weight: 600; color: #1f2937; margin: 0;">Screenshot Preview</h2>
                            <div style="font-size: 12px; color: #6b7280; margin-top: 4px;">${employee} • ${timestamp}</div>
                        </div>
                        <button onclick="document.getElementById('screenshotModal').remove()" style="background: #f3f4f6; border: none; border-radius: 6px; padding: 8px 12px; cursor: pointer; font-size: 18px; color: #6b7280;">✕</button>
                    </div>
                    
                    <!-- Image -->
                    <div style="padding: 20px; text-align: center;">
                        <img src="${imageData}" alt="Screenshot" style="max-width: 100%; max-height: 70vh; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                    </div>
                    
                    <!-- Footer -->
                    <div style="padding: 16px 20px; border-top: 1px solid #e5e7eb; background: #f9fafb; display: flex; justify-content: space-between; align-items: center; font-size: 12px; color: #6b7280;">
                        <div>Resolution: ${width} × ${height}</div>
                        <button onclick="document.getElementById('screenshotModal').remove()" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 6px; padding: 8px 16px; cursor: pointer; font-weight: 500;">Close</button>
                    </div>
                </div>
            </div>
        `;
        
        // Remove existing modal if any
        const existingModal = document.getElementById('screenshotModal');
        if (existingModal) existingModal.remove();
        
        // Add modal to page
        document.body.insertAdjacentHTML('beforeend', modalHTML);
        
        // Close on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                const modal = document.getElementById('screenshotModal');
                if (modal) modal.remove();
            }
        });
    }
};

// Expose app as dashboard for global access in onclick handlers
const dashboard = app;
window.dashboard = app;

// ===== REMOTE VIEWING HELPER FUNCTIONS =====

// Start remote viewing session
async function startRemoteViewing(context) {
    try {
        context.attemptCount = 0;
        context.maxAttempts = 30; // Wait up to 60 seconds for first frame (30 * 2s)
        
        const response = await context.app.api('start_viewing', {
            system_id: context.machineId,
            viewer_id: 'admin'
        });
        
        if (response.success) {
            context.isConnected = true;
            updateRemoteStatus(context, 'connected', 'Connected');
            
            // Start fetching frames
            fetchRemoteFrame(context);
            context.refreshInterval = setInterval(() => fetchRemoteFrame(context), 2000);
        } else {
            updateRemoteStatus(context, 'disconnected', 'Failed to connect');
            showRemoteWaiting(context);
        }
    } catch (err) {
        console.error('Start viewing error:', err);
        updateRemoteStatus(context, 'disconnected', 'Connection error');
        showRemoteWaiting(context);
    }
}

// Fetch latest frame
async function fetchRemoteFrame(context) {
    try {
        context.attemptCount = (context.attemptCount || 0) + 1;
        
        console.log('Fetching frame, attempt:', context.attemptCount, 'max:', context.maxAttempts);
        
        const result = await context.app.api('get_live_frame', {
            system_id: context.machineId
        });

        console.log('Frame fetch result:', result);

        if (result.success && result.data && result.data.frame) {
            console.log('Frame received successfully, displaying...');
            context.attemptCount = 0; // Reset on success
            displayRemoteScreen(context, result.data);
        } else if (result.waiting || (result.data && result.data.waiting)) {
            // Only show waiting state if we haven't exceeded attempts
            console.log('Waiting for frame, status:', result);
            if (context.attemptCount < context.maxAttempts) {
                showRemoteWaiting(context);
            } else {
                // After 60 seconds, show error
                console.error('Max attempts reached, no frame received');
                showRemoteError(context, 'No frames received. Ensure the Desktop Controller is running on the remote system.');
                if (context.refreshInterval) clearInterval(context.refreshInterval);
            }
        } else if (result.error) {
            // Show waiting instead of error - gives more time for system to respond
            console.log('Frame fetch error:', result.error);
            if (context.attemptCount < context.maxAttempts) {
                showRemoteWaiting(context);
            }
        }
    } catch (err) {
        console.error('Frame fetch exception:', err);
        // Don't show error on network issues - keep retrying
    }
}

// Display remote screen
function displayRemoteScreen(context, data) {
    const modal = context.modal;
    if (!modal) return;
    
    console.log('displayRemoteScreen called with data:', data);
    
    // Check if frame data exists
    if (!data || !data.frame) {
        console.warn('No frame data available:', data);
        showRemoteWaiting(context);
        return;
    }
    
    // Hide loading/waiting states
    const loadingState = modal.querySelector('.remote-loading-state');
    if (loadingState) loadingState.style.display = 'none';
    
    // Show image
    const img = modal.querySelector('.remote-screen-img');
    if (!img) {
        console.error('Image element not found in modal');
        return;
    }
    
    try {
        // Set the image source
        img.src = 'data:image/jpeg;base64,' + data.frame;
        img.style.display = 'block';
        img.style.width = '100%';
        img.style.height = '100%';
        img.style.objectFit = 'contain';
        
        console.log('Image set successfully, size:', data.width, 'x', data.height);
        
        // Update stats
        const resolutionEl = modal.querySelector('.remote-resolution');
        if (resolutionEl) resolutionEl.textContent = (data.width || 1920) + ' x ' + (data.height || 1080);
        
        const updateEl = modal.querySelector('.remote-last-update');
        if (updateEl) updateEl.textContent = new Date().toLocaleTimeString();
        
        updateRemoteStatus(context, 'connected', 'Live');
    } catch (err) {
        console.error('Error displaying frame:', err);
        showRemoteWaiting(context);
    }
}

// Show waiting state
function showRemoteWaiting(context) {
    const modal = context.modal;
    const loadingDiv = modal.querySelector('.remote-loading-state');
    
    if (loadingDiv) {
        loadingDiv.innerHTML = `
            <div style="font-size: 48px; margin-bottom: 16px;">📡</div>
            <h3 style="font-size: 16px; margin: 0 0 8px 0;">Waiting for Screen Data</h3>
            <p style="font-size: 13px; color: #666; margin: 0;">The remote system is being notified. Screen capture will appear shortly once the system starts streaming.</p>
        `;
        loadingDiv.style.display = 'block';
    }
    
    const img = modal.querySelector('.remote-screen-img');
    if (img) img.style.display = 'none';
    
    updateRemoteStatus(context, 'connecting', 'Waiting for stream...');
}

// Show error
function showRemoteError(context, message) {
    const modal = context.modal;
    const loadingDiv = modal.querySelector('.remote-loading-state');
    
    if (loadingDiv) {
        loadingDiv.innerHTML = `
            <div style="font-size: 48px; margin-bottom: 16px;">❌</div>
            <h3 style="font-size: 16px; margin: 0 0 8px 0; color: #ef4444;">Connection Failed</h3>
            <p style="font-size: 13px; color: #666; margin: 0;">${message}</p>
            <button onclick="location.reload()" style="background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%); color: white; border: none; padding: 12px 30px; border-radius: 8px; font-size: 14px; cursor: pointer; margin-top: 20px;">🔄 Retry Connection</button>
        `;
        loadingDiv.style.display = 'block';
    }
    
    const img = modal.querySelector('.remote-screen-img');
    if (img) img.style.display = 'none';
    
    updateRemoteStatus(context, 'disconnected', 'Disconnected');
}

// Update status
function updateRemoteStatus(context, status, text) {
    const modal = context.modal;
    const dot = modal.querySelector('.remote-status-dot');
    const statusText = modal.querySelector('.remote-status-text');
    
    const statusColors = {
        'connecting': '#f59e0b',
        'connected': '#22c55e',
        'disconnected': '#ef4444'
    };
    
    if (dot) dot.style.background = statusColors[status] || '#888';
    if (statusText) statusText.textContent = text;
}

// Stop remote viewing
async function stopRemoteViewing(context) {
    try {
        // Stop refreshing
        if (context.refreshInterval) {
            clearInterval(context.refreshInterval);
        }
        
        // Tell server to stop viewing
        await fetch('/api.php?action=stop_viewing', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                system_id: context.machineId,
                system_id: context.machineId,
                viewer_id: 'admin'
            })
        }).catch(() => {});
        
    } catch (err) {
        console.error('Stop viewing error:', err);
    }
    
    // Remove modal
    if (context.modal) {
        context.modal.remove();
    }
}

// Toggle fullscreen
function toggleRemoteFullscreen(context) {
    const modal = context.modal;
    modal.classList.toggle('fullscreen-mode');
    
    if (modal.classList.contains('fullscreen-mode')) {
        document.documentElement.requestFullscreen?.();
    } else {
        document.exitFullscreen?.();
    }
}

// Expose app globally for onclick handlers
window.app = app;

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    app.init();
});