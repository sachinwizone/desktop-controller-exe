const http = require('http');

console.log('Testing employee load API...\n');

const data = JSON.stringify({
    company_name: 'WIZONE IT NETWORK INDIA PVT LTD',
    page: 1,
    limit: 100
});

const options = {
    hostname: 'localhost',
    port: 8889,
    path: '/api.php?action=get_company_employees',
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Content-Length': data.length
    },
    timeout: 5000
};

const req = http.request(options, (res) => {
    console.log(`✓ Response Status: ${res.statusCode}`);
    
    let responseData = '';
    res.on('data', (chunk) => {
        responseData += chunk;
    });
    
    res.on('end', () => {
        console.log('\nResponse received:');
        try {
            const json = JSON.parse(responseData);
            if (json.success) {
                console.log('✓ API returned success!');
                console.log('  Total employees:', json.data.pagination.total_records);
                console.log('  Employees in page:', json.data.records.length);
                if (json.data.records.length > 0) {
                    console.log('\nFirst employee:');
                    const emp = json.data.records[0];
                    console.log('  ID:', emp.employee_id);
                    console.log('  Name:', emp.full_name);
                    console.log('  Email:', emp.email);
                    console.log('  Department:', emp.department);
                    console.log('  Status:', emp.is_active ? 'Active' : 'Inactive');
                }
            } else {
                console.log('✗ API returned error:', json.error);
            }
        } catch (e) {
            console.log('✗ Response not JSON:', responseData);
        }
    });
});

req.on('error', (error) => {
    console.error('✗ Request error:', error.message);
    process.exit(1);
});

req.on('timeout', () => {
    console.error('✗ Request timeout');
    req.destroy();
    process.exit(1);
});

console.log('Sending request...');
req.write(data);
req.end();

// Exit after response
setTimeout(() => {
    console.log('\nTest complete.');
    process.exit(0);
}, 2000);
