// Test script to verify API endpoints
const http = require('http');

function testAPI(action, companyName) {
    return new Promise((resolve, reject) => {
        const query = `?action=${action}&company_name=${encodeURIComponent(companyName)}`;
        const url = `http://localhost:8888/api.php${query}`;
        
        console.log(`\nTesting: ${url}`);
        
        http.get(url, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    const json = JSON.parse(data);
                    console.log('Status:', res.statusCode);
                    console.log('Success:', json.success);
                    if (json.data) {
                        console.log('Records count:', Array.isArray(json.data) ? json.data.length : 'single object');
                        if (Array.isArray(json.data) && json.data.length > 0) {
                            console.log('Sample:', JSON.stringify(json.data[0], null, 2).substring(0, 300));
                        }
                    }
                    resolve(json);
                } catch (e) {
                    console.error('Parse error:', e.message);
                    console.log('Raw response:', data.substring(0, 200));
                    reject(e);
                }
            });
        }).on('error', reject);
    });
}

async function runTests() {
    const company = 'WIZONE IT NETWORK INDIA PVT LTD';
    
    try {
        await testAPI('get_web_logs', company);
        await testAPI('get_application_logs', company);
        await testAPI('get_inactivity_logs', company);
        await testAPI('get_screenshots', company);
        console.log('\n✓ All API tests completed');
    } catch (err) {
        console.error('Test error:', err.message);
    }
}

runTests();
