/**
 * Chat Module for Web Dashboard
 * Real-time messaging between web portal and desktop applications
 * 
 * Integration:
 * 1. Include this file in HTML: <script src="chat-module.js"></script>
 * 2. Add chat HTML container to your page
 * 3. Initialize with: ChatModule.initialize()
 */

const ChatModule = (() => {
    const API_BASE = 'http://localhost:8888/api/chat';
    let currentDeviceId = null;
    let messageRefreshInterval = null;
    let currentConversation = null;

    // Initialize chat module
    const initialize = (deviceId = null) => {
        currentDeviceId = deviceId || getDeviceIdentifier();
        console.log('[Chat] Initializing with device ID:', currentDeviceId);
        
        // Create chat UI if not exists
        createChatUI();
        
        // Load initial messages
        loadMessages();
        
        // Start auto-refresh
        startAutoRefresh();
    };

    // Get or create device identifier
    const getDeviceIdentifier = () => {
        let deviceId = localStorage.getItem('chat_device_id');
        if (!deviceId) {
            deviceId = 'web-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('chat_device_id', deviceId);
        }
        return deviceId;
    };

    // Create chat UI
    const createChatUI = () => {
        if (document.getElementById('chat-container')) {
            return; // Already created
        }

        const chatHTML = `
            <div id="chat-container" class="chat-container" style="
                position: fixed;
                bottom: 20px;
                right: 20px;
                width: 400px;
                height: 500px;
                background: white;
                border: 1px solid #ddd;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                display: flex;
                flex-direction: column;
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                z-index: 10000;
            ">
                <div class="chat-header" style="
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 15px;
                    border-radius: 8px 8px 0 0;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                ">
                    <h3 style="margin: 0; font-size: 16px;">Chat - Desktop & Web</h3>
                    <button id="chat-toggle" style="
                        background: none;
                        border: none;
                        color: white;
                        cursor: pointer;
                        font-size: 20px;
                    ">−</button>
                </div>

                <div class="chat-conversations" id="chat-conversations" style="
                    overflow-y: auto;
                    padding: 10px;
                    flex: 0 0 60px;
                    border-bottom: 1px solid #eee;
                    display: none;
                ">
                    <!-- Conversations list will be populated here -->
                </div>

                <div class="chat-messages" id="chat-messages" style="
                    flex: 1;
                    overflow-y: auto;
                    padding: 15px;
                    background: #f9f9f9;
                ">
                    <div style="text-align: center; color: #999; padding: 20px;">
                        Loading messages...
                    </div>
                </div>

                <div class="chat-input-area" style="
                    padding: 10px;
                    border-top: 1px solid #eee;
                    display: flex;
                    gap: 8px;
                ">
                    <input 
                        type="text" 
                        id="chat-input" 
                        placeholder="Type a message..."
                        style="
                            flex: 1;
                            padding: 10px;
                            border: 1px solid #ddd;
                            border-radius: 4px;
                            font-family: 'Segoe UI', sans-serif;
                        "
                    />
                    <button 
                        id="chat-send" 
                        style="
                            padding: 10px 20px;
                            background: #667eea;
                            color: white;
                            border: none;
                            border-radius: 4px;
                            cursor: pointer;
                            font-weight: bold;
                        "
                    >Send</button>
                </div>

                <div id="chat-status" style="
                    padding: 8px;
                    text-align: center;
                    font-size: 12px;
                    color: #999;
                    border-top: 1px solid #eee;
                ">
                    Connected
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', chatHTML);

        // Attach event listeners
        document.getElementById('chat-send').addEventListener('click', sendMessage);
        document.getElementById('chat-input').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                sendMessage();
            }
        });

        document.getElementById('chat-toggle').addEventListener('click', toggleChat);
    };

    // Send message
    const sendMessage = async () => {
        const input = document.getElementById('chat-input');
        const message = input.value.trim();

        if (!message) {
            return;
        }

        try {
            const response = await fetch(API_BASE + '/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    device_id: currentDeviceId,
                    sender: 'Web Dashboard',
                    message: message,
                    timestamp: new Date().toISOString(),
                    is_from_desktop: false,
                    conversation_id: currentConversation
                })
            });

            if (response.ok) {
                input.value = '';
                await loadMessages();
                console.log('[Chat] Message sent successfully');
            } else {
                showStatus('Failed to send message', 'error');
            }
        } catch (error) {
            console.error('[Chat] Error sending message:', error);
            showStatus('Error sending message', 'error');
        }
    };

    // Load messages
    const loadMessages = async () => {
        try {
            const response = await fetch(
                `${API_BASE}/get?device_id=${currentDeviceId}&limit=50&offset=0`
            );

            if (!response.ok) {
                throw new Error('Failed to load messages');
            }

            const result = await response.json();
            const messages = result.data || [];

            const messagesContainer = document.getElementById('chat-messages');
            messagesContainer.innerHTML = '';

            if (messages.length === 0) {
                messagesContainer.innerHTML = '<div style="text-align: center; color: #999; padding: 20px;">No messages yet</div>';
                return;
            }

            messages.forEach(msg => {
                const msgElement = document.createElement('div');
                msgElement.className = 'chat-message';
                
                const isOwn = msg.is_from_desktop === false;
                msgElement.style.cssText = `
                    margin-bottom: 12px;
                    display: flex;
                    flex-direction: column;
                    align-items: ${isOwn ? 'flex-end' : 'flex-start'};
                `;

                const msgBubble = document.createElement('div');
                msgBubble.style.cssText = `
                    background: ${isOwn ? '#667eea' : '#e0e0e0'};
                    color: ${isOwn ? 'white' : 'black'};
                    padding: 10px 15px;
                    border-radius: 12px;
                    max-width: 80%;
                    word-wrap: break-word;
                `;

                const sender = msg.sender || (msg.is_from_desktop ? 'Desktop' : 'Web');
                const time = new Date(msg.timestamp).toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit'
                });

                msgBubble.innerHTML = `
                    <div style="font-weight: bold; font-size: 12px; margin-bottom: 4px;">
                        ${sender} - ${time}
                    </div>
                    <div>${escapeHtml(msg.message)}</div>
                `;

                msgElement.appendChild(msgBubble);
                messagesContainer.appendChild(msgElement);
            });

            // Auto-scroll to bottom
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        } catch (error) {
            console.error('[Chat] Error loading messages:', error);
            showStatus('Error loading messages', 'error');
        }
    };

    // Load conversations
    const loadConversations = async () => {
        try {
            const response = await fetch(
                `${API_BASE}/conversations?device_id=${currentDeviceId}`
            );

            if (!response.ok) {
                throw new Error('Failed to load conversations');
            }

            const result = await response.json();
            const conversations = result.data || [];

            const convContainer = document.getElementById('chat-conversations');
            convContainer.innerHTML = '';

            conversations.forEach(conv => {
                const convElement = document.createElement('button');
                convElement.style.cssText = `
                    display: block;
                    width: 100%;
                    padding: 10px;
                    text-align: left;
                    border: 1px solid #ddd;
                    background: white;
                    cursor: pointer;
                    margin-bottom: 5px;
                    border-radius: 4px;
                    font-size: 13px;
                `;

                const unreadBadge = conv.unread_count > 0 ? 
                    `<span style="background: #667eea; color: white; padding: 2px 6px; border-radius: 10px; margin-left: 5px; font-size: 11px;">${conv.unread_count}</span>` : '';

                convElement.innerHTML = `
                    <strong>${conv._id}</strong> ${unreadBadge}
                    <div style="font-size: 12px; color: #666;">${conv.last_message.substring(0, 40)}...</div>
                `;

                convElement.addEventListener('click', () => {
                    currentConversation = conv._id;
                    loadMessages();
                });

                convContainer.appendChild(convElement);
            });
        } catch (error) {
            console.error('[Chat] Error loading conversations:', error);
        }
    };

    // Toggle chat visibility
    const toggleChat = () => {
        const container = document.getElementById('chat-container');
        const isVisible = container.style.height !== '60px';
        
        if (isVisible) {
            container.style.height = '60px';
            document.querySelector('.chat-messages').style.display = 'none';
            document.querySelector('.chat-input-area').style.display = 'none';
            document.querySelector('.chat-status').style.display = 'none';
        } else {
            container.style.height = '500px';
            document.querySelector('.chat-messages').style.display = 'block';
            document.querySelector('.chat-input-area').style.display = 'flex';
            document.querySelector('.chat-status').style.display = 'block';
        }
    };

    // Show status message
    const showStatus = (message, type = 'info') => {
        const statusEl = document.getElementById('chat-status');
        statusEl.textContent = message;
        statusEl.style.color = type === 'error' ? '#d32f2f' : '#667eea';
        
        setTimeout(() => {
            statusEl.textContent = 'Connected';
            statusEl.style.color = '#999';
        }, 3000);
    };

    // Start auto-refresh
    const startAutoRefresh = () => {
        if (messageRefreshInterval) {
            clearInterval(messageRefreshInterval);
        }

        messageRefreshInterval = setInterval(() => {
            loadMessages();
            loadConversations();
        }, 3000);
    };

    // Stop auto-refresh
    const stopAutoRefresh = () => {
        if (messageRefreshInterval) {
            clearInterval(messageRefreshInterval);
        }
    };

    // Escape HTML special characters
    const escapeHtml = (text) => {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    };

    // Public API
    return {
        initialize,
        sendMessage,
        loadMessages,
        loadConversations,
        getDeviceId: () => currentDeviceId,
        setDeviceId: (id) => { currentDeviceId = id; },
        stop: stopAutoRefresh
    };
})();

// Auto-initialize on page load if not already initialized
document.addEventListener('DOMContentLoaded', () => {
    console.log('[Chat] Module loaded');
});
