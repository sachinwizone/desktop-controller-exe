// Direct HTTP test with Node.js fetch equivalent
const http = require('http');
const url = require('url');

function makeRequest(pathname, query) {
    return new Promise((resolve, reject) => {
        const fullUrl = `http://localhost:8888${pathname}?${query}`;
        console.log(`\nTesting: ${pathname}`);
        console.log(`URL: ${fullUrl}`);
        
        const options = {
            hostname: 'localhost',
            port: 8888,
            path: `${pathname}?${query}`,
            method: 'GET',
            timeout: 10000
        };
        
        console.log('Sending request...');
        const startTime = Date.now();
        
        const req = http.request(options, (res) => {
            let data = '';
            let chunks = 0;
            
            console.log(`Response status: ${res.statusCode}`);
            console.log(`Response headers:`, res.headers);
            
            res.on('data', (chunk) => {
                chunks++;
                data += chunk;
            });
            
            res.on('end', () => {
                const duration = Date.now() - startTime;
                console.log(`Response received in ${duration}ms (${chunks} chunks, ${data.length} bytes)`);
                
                try {
                    const json = JSON.parse(data);
                    console.log(`Success: ${json.success}`);
                    if (json.data) {
                        const count = Array.isArray(json.data) ? json.data.length : typeof json.data;
                        console.log(`Data: ${count} records/items`);
                        if (Array.isArray(json.data) && json.data[0]) {
                            console.log('Sample:', JSON.stringify(json.data[0]).substring(0, 200));
                        }
                    }
                    resolve(json);
                } catch (e) {
                    console.error('Parse error:', e.message);
                    console.log('Raw response (first 500 chars):', data.substring(0, 500));
                    reject(e);
                }
            });
        });
        
        req.on('timeout', () => {
            console.error('Request timeout!');
            req.destroy();
            reject(new Error('Timeout'));
        });
        
        req.on('error', (err) => {
            console.error('Request error:', err.message);
            reject(err);
        });
        
        req.end();
    });
}

async function runTests() {
    const company = encodeURIComponent('WIZONE IT NETWORK INDIA PVT LTD');
    
    try {
        await makeRequest('/api.php', `action=get_web_logs&company_name=${company}`);
        console.log('\n✓ Web logs test completed\n');
        
        await makeRequest('/api.php', `action=get_application_logs&company_name=${company}`);
        console.log('\n✓ Application logs test completed\n');
        
        await makeRequest('/api.php', `action=get_inactivity_logs&company_name=${company}`);
        console.log('\n✓ Inactivity logs test completed\n');
        
        await makeRequest('/api.php', `action=get_screenshots&company_name=${company}`);
        console.log('\n✓ Screenshots test completed\n');
        
    } catch (err) {
        console.error('Test failed:', err.message);
    }
}

runTests();
