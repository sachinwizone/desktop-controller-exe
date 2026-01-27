const http = require('http');

async function testAPI(action, data) {
    return new Promise((resolve, reject) => {
        const postData = JSON.stringify(data);
        const options = {
            hostname: 'localhost',
            port: 8889,
            path: `/api.php?action=${action}`,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': postData.length
            }
        };

        const req = http.request(options, (res) => {
            let responseData = '';
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            res.on('end', () => {
                resolve(JSON.parse(responseData));
            });
        });

        req.on('error', (e) => {
            reject(e);
        });

        req.write(postData);
        req.end();
    });
}

async function runTests() {
    console.log('===== TESTING API ENDPOINTS =====\n');
    
    try {
        // Test get_daily_working_hours
        console.log('1. Testing get_daily_working_hours...');
        const dailyHoursResult = await testAPI('get_daily_working_hours', {
            employee: 'ashutosh',
            date: '2026-01-19',
            company_name: 'WIZONE IT NETWORK INDIA PVT LTD'
        });
        console.log('Result:', JSON.stringify(dailyHoursResult, null, 2));
        console.log('\n');

        // Test get_web_logs
        console.log('2. Testing get_web_logs...');
        const webLogsResult = await testAPI('get_web_logs', {
            company_name: 'WIZONE IT NETWORK INDIA PVT LTD',
            page: 1,
            limit: 5
        });
        console.log('Success:', webLogsResult.success);
        console.log('Records found:', webLogsResult.data?.pagination?.total_records);
        console.log('Sample record:', webLogsResult.data?.records?.[0]);
        console.log('\n');

        // Test get_application_logs
        console.log('3. Testing get_application_logs...');
        const appLogsResult = await testAPI('get_application_logs', {
            company_name: 'WIZONE IT NETWORK INDIA PVT LTD',
            page: 1,
            limit: 5
        });
        console.log('Success:', appLogsResult.success);
        console.log('Records found:', appLogsResult.data?.pagination?.total_records);
        console.log('Sample record:', appLogsResult.data?.records?.[0]);
        console.log('\n');

        // Test get_inactivity_logs
        console.log('4. Testing get_inactivity_logs...');
        const inactivityResult = await testAPI('get_inactivity_logs', {
            company_name: 'WIZONE IT NETWORK INDIA PVT LTD',
            page: 1,
            limit: 5
        });
        console.log('Success:', inactivityResult.success);
        console.log('Records found:', inactivityResult.data?.pagination?.total_records);
        console.log('Sample record:', inactivityResult.data?.records?.[0]);
        console.log('\n');

        // Test get_screenshots
        console.log('5. Testing get_screenshots...');
        const screenshotsResult = await testAPI('get_screenshots', {
            company_name: 'WIZONE IT NETWORK INDIA PVT LTD',
            page: 1,
            limit: 5
        });
        console.log('Success:', screenshotsResult.success);
        console.log('Records found:', screenshotsResult.data?.pagination?.total_records);
        console.log('Sample record (without screenshot_data):', {
            id: screenshotsResult.data?.records?.[0]?.id,
            username: screenshotsResult.data?.records?.[0]?.username,
            log_time_formatted: screenshotsResult.data?.records?.[0]?.log_time_formatted,
            has_screenshot: screenshotsResult.data?.records?.[0]?.has_screenshot,
            screen_width: screenshotsResult.data?.records?.[0]?.screen_width,
            screen_height: screenshotsResult.data?.records?.[0]?.screen_height
        });
        console.log('\n');

        console.log('===== ALL TESTS COMPLETED =====');
    } catch (error) {
        console.error('Test error:', error);
    }
}

runTests();
