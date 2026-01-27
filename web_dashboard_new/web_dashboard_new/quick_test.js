// Quick test to see if server responds
const http = require('http');

const req = http.request({
    hostname: 'localhost',
    port: 8888,
    path: '/api.php?action=get_web_logs&company_name=TEST',
    method: 'GET',
    timeout: 5000
}, (res) => {
    let data = '';
    console.log('Response Status:', res.statusCode);
    res.on('data', chunk => {
        data += chunk;
        console.log('Got chunk, size:', chunk.length);
    });
    res.on('end', () => {
        console.log('Response received, size:', data.length);
        if (data.length > 0) {
            try {
                const json = JSON.parse(data);
                console.log('Success:', json.success);
                if (json.data) {
                    console.log('Data count:', Array.isArray(json.data) ? json.data.length : typeof json.data);
                }
            } catch (e) {
                console.log('First 200 chars:', data.substring(0, 200));
            }
        }
    });
});

req.on('timeout', () => {
    console.log('Request timeout!');
    req.destroy();
});

req.on('error', (e) => {
    console.error('Request error:', e.message);
});

console.log('Sending request...');
req.end();
