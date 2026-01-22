const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function testQuery() {
    const client = new Client(dbConfig);
    
    try {
        console.log('Connecting...');
        await client.connect();
        console.log('Connected!');

        const company = 'WIZONE IT NETWORK INDIA PVT LTD';
        
        console.log('\nTesting optimized web logs query...');
        const startTime = Date.now();
        
        const sql = `SELECT id, log_timestamp, system_name, username, browser_name, website_url, page_title, category, visit_time, duration_seconds, ip_address, display_user_name FROM (SELECT * FROM web_logs WHERE company_name = $1 LIMIT 1000) sub ORDER BY visit_time DESC LIMIT 500`;
        
        const result = await client.query(sql, [company]);
        
        const duration = Date.now() - startTime;
        console.log(`Query completed in ${duration}ms`);
        console.log(`Records returned: ${result.rows.length}`);
        
        if (result.rows.length > 0) {
            console.log('Sample:', JSON.stringify(result.rows[0], null, 2).substring(0, 300));
        }

    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

testQuery();
