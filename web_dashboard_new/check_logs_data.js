const { Client } = require('pg');

// PostgreSQL connection config
const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function checkDatabase() {
    const client = new Client(dbConfig);
    
    try {
        await client.connect();
        console.log('✓ Connected to database\n');

        // Check web_logs table
        console.log('=== WEB_LOGS TABLE ===');
        const webLogs = await client.query(`
            SELECT COUNT(*) as count FROM web_logs 
            WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
        `);
        console.log(`Records: ${webLogs.rows[0].count}`);
        
        if (webLogs.rows[0].count > 0) {
            const sample = await client.query(`
                SELECT * FROM web_logs 
                WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
                LIMIT 1
            `);
            console.log('Sample:', JSON.stringify(sample.rows[0], null, 2));
        }

        // Check application_logs table
        console.log('\n=== APPLICATION_LOGS TABLE ===');
        const appLogs = await client.query(`
            SELECT COUNT(*) as count FROM application_logs 
            WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
        `);
        console.log(`Records: ${appLogs.rows[0].count}`);
        
        if (appLogs.rows[0].count > 0) {
            const sample = await client.query(`
                SELECT * FROM application_logs 
                WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
                LIMIT 1
            `);
            console.log('Sample:', JSON.stringify(sample.rows[0], null, 2));
        }

        // Check inactivity_logs table
        console.log('\n=== INACTIVITY_LOGS TABLE ===');
        const inactLogs = await client.query(`
            SELECT COUNT(*) as count FROM inactivity_logs 
            WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
        `);
        console.log(`Records: ${inactLogs.rows[0].count}`);
        
        if (inactLogs.rows[0].count > 0) {
            const sample = await client.query(`
                SELECT * FROM inactivity_logs 
                WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
                LIMIT 1
            `);
            console.log('Sample:', JSON.stringify(sample.rows[0], null, 2));
        }

        // Check screenshot_logs table
        console.log('\n=== SCREENSHOT_LOGS TABLE ===');
        const screenshots = await client.query(`
            SELECT COUNT(*) as count FROM screenshot_logs 
            WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
        `);
        console.log(`Records: ${screenshots.rows[0].count}`);
        
        if (screenshots.rows[0].count > 0) {
            const sample = await client.query(`
                SELECT id, username, system_name, log_timestamp, company_name FROM screenshot_logs 
                WHERE company_name = 'WIZONE IT NETWORK INDIA PVT LTD'
                LIMIT 1
            `);
            console.log('Sample:', JSON.stringify(sample.rows[0], null, 2));
        }

    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

checkDatabase();
