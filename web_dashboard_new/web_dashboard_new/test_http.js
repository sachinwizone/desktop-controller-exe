const http = require('http');

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
    }
};

const req = http.request(options, (res) => {
    console.log(`Status: ${res.statusCode}`);
    
    let responseData = '';
    res.on('data', (chunk) => {
        responseData += chunk;
    });
    
    res.on('end', () => {
        console.log('Response:');
        try {
            const json = JSON.parse(responseData);
            console.log(JSON.stringify(json, null, 2).substring(0, 500));
        } catch (e) {
            console.log(responseData);
        }
    });
});

req.on('error', (error) => {
    console.error('Request error:', error);
});

req.write(data);
req.end();
