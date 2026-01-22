#!/usr/bin/env node
console.log('[1] Script started');

try {
    console.log('[2] Loading http module');
    const http = require('http');
    console.log('[3] http module loaded');
    
    const server = http.createServer((req, res) => {
        console.log('[6] Request handler called');
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        res.end('Hello');
    });
    
    console.log('[4] Server object created');
    
    server.on('error', (err) => {
        console.log('[ERROR] Server error:', err.message);
    });
    
    server.listen(8888, '0.0.0.0', () => {
        console.log('[5] Server listening on port 8888');
    });
} catch (err) {
    console.log('[CATCH] Exception:', err.message);
    console.log(err.stack);
}
