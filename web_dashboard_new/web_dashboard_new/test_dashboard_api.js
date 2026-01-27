// Simple test to check if server is running
const http = require('http');

console.log('Testing server at localhost:8888...\n');

const req = http.request({
    hostname: 'localhost',
    port: 8888,
    path: '/',
    method: 'GET'
}, (res) => {
    console.log('✓ Server is responding (Status:', res.statusCode + ')');
    let data = '';
    res.on('data', chunk => data += chunk);
    res.on('end', () => {
        console.log('Response length:', data.length, 'bytes');
        if (data.includes('loginContainer')) {
            console.log('✓ Login page HTML found');
        }
        process.exit(0);
    });
});

req.on('error', (err) => {
    console.error('✗ Server connection failed:', err.code);
    console.error('Error:', err.message);
    process.exit(1);
});

setTimeout(() => {
    console.error('✗ Request timeout');
    process.exit(1);
}, 3000);

req.end();
