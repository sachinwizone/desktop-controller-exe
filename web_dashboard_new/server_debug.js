const http = require('http');
const fs = require('fs');
const path = require('path');
const { Client } = require('pg');

const PORT = 8888;

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function queryDB(sql, params = []) {
    console.log('[DB] Creating client...');
    const client = new Client(dbConfig);
    try {
        console.log('[DB] Connecting...');
        await client.connect();
        console.log('[DB] Connected!');
        const result = await client.query(sql, params);
        console.log('[DB] Query executed, got', result.rows.length, 'rows');
        return result.rows;
    } catch (err) {
        console.error('[DB] Error:', err.message);
        throw err;
    } finally {
        try {
            console.log('[DB] Ending connection...');
            await client.end();
            console.log('[DB] Connection ended');
        } catch (e) {
            console.error('[DB] Error ending connection:', e.message);
        }
    }
}

const apiHandlers = {
    async admin_login(body) {
        console.log('[API] admin_login called with username:', body.username);
        const { username, password } = body;
        
        if (!username || !password) {
            return { success: false, error: 'Username and password are required' };
        }
        
        try {
            console.log('[API] Querying database for user:', username);
            const userRows = await queryDB(
                `SELECT id, username, password, full_name, email, is_active, role_id, company_name 
                 FROM company_users 
                 WHERE username = $1 AND password = $2 AND is_active = TRUE AND role_id = 1 
                 LIMIT 1`,
                [username, password]
            );
            
            console.log('[API] Got', userRows.length, 'user rows');
            if (userRows.length > 0) {
                const user = userRows[0];
                console.log('[API] Login successful for', user.username);
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
            console.log('[API] No user found, login failed');
            return { success: false, error: 'Invalid username or password' };
        } catch (err) {
            console.error('[API] Login error:', err.message);
            return { success: false, error: 'Database error: ' + err.message };
        }
    }
};

function parseBody(req) {
    return new Promise((resolve, reject) => {
        console.log('[BODY] Starting to parse body...');
        let body = '';
        req.on('data', chunk => {
            console.log('[BODY] Got chunk:', chunk.length, 'bytes');
            body += chunk;
        });
        req.on('end', () => {
            console.log('[BODY] End event, body length:', body.length);
            try {
                const parsed = body ? JSON.parse(body) : {};
                console.log('[BODY] Parsed JSON successfully');
                resolve(parsed);
            } catch (e) {
                console.log('[BODY] JSON parse error:', e.message);
                resolve({});
            }
        });
        req.on('error', (err) => {
            console.error('[BODY] Request error:', err.message);
            reject(err);
        });
    });
}

const server = http.createServer(async (req, res) => {
    console.log('[SERVER] Received', req.method, 'request for', req.url);
    
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    
    if (req.method === 'OPTIONS') {
        console.log('[SERVER] OPTIONS request, sending 204');
        res.writeHead(204);
        res.end();
        return;
    }

    try {
        const url = req.url.split('?')[0];
        console.log('[SERVER] URL:', url);
        
        if (url === '/api.php' || url.startsWith('/api.php?')) {
            console.log('[SERVER] API request detected');
            const body = await parseBody(req);
            const action = body.action;
            console.log('[SERVER] API action:', action);
            
            try {
                if (apiHandlers[action]) {
                    console.log('[SERVER] Handler found for', action);
                    const result = await apiHandlers[action](body);
                    console.log('[SERVER] Handler returned result');
                    if (!res.writableEnded) {
                        console.log('[SERVER] Writing response...');
                        res.writeHead(200, { 'Content-Type': 'application/json' });
                        const jsonStr = JSON.stringify(result);
                        console.log('[SERVER] JSON:', jsonStr.substring(0, 100) + '...');
                        res.end(jsonStr);
                        console.log('[SERVER] Response sent');
                    }
                } else {
                    console.log('[SERVER] No handler for', action);
                    if (!res.writableEnded) {
                        res.writeHead(400, { 'Content-Type': 'application/json' });
                        res.end(JSON.stringify({ success: false, error: 'Unknown action' }));
                    }
                }
            } catch (err) {
                console.error('[SERVER] API handler error:', err.message);
                if (!res.writableEnded) {
                    res.writeHead(500, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify({ success: false, error: 'Server error: ' + err.message }));
                }
            }
            return;
        }
        
        console.log('[SERVER] Static file request');
        res.writeHead(404);
        res.end('Not found');
    } catch (err) {
        console.error('[SERVER] Request handler error:', err.message);
        console.error(err.stack);
        try {
            if (!res.writableEnded) {
                res.writeHead(500, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ success: false, error: 'Server error' }));
            }
        } catch (e) {
            console.error('[SERVER] Error sending error response:', e.message);
        }
    }
});

server.listen(PORT, '0.0.0.0', () => {
    console.log('\n[STARTUP] Server started on port', PORT);
});

process.on('uncaughtException', (err) => {
    console.error('[ERROR] UNCAUGHT EXCEPTION:', err.message);
    console.error(err.stack);
    process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
    console.error('[ERROR] UNHANDLED REJECTION:', reason);
});
