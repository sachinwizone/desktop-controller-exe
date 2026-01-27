const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function testInsert() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        // Test 1: Simple insert WITHOUT system_username
        console.log('Testing insert with corrected columns...');
        const result = await client.query(
            `INSERT INTO punch_logs 
            (activation_key, company_name, system_name, ip_address, username,
             machine_id, punch_type, punch_time, punch_out_time, total_duration_seconds)
            VALUES 
            ('TEST_KEY', 'TEST_COMPANY', 'TEST_SYSTEM', '192.168.1.1', 'test_user',
             'MACHINE123', 'PUNCH_IN', NOW(), NULL, 0)
            RETURNING *`,
            []
        );
        
        console.log('Insert successful! Inserted row:');
        console.log(result.rows[0]);
        
        // Check how many rows are now in the table
        const checkResult = await client.query('SELECT COUNT(*) as count FROM punch_logs');
        console.log(`\nTotal rows in punch_logs: ${checkResult.rows[0].count}`);
        
    } catch (err) {
        console.error('Error:', err.message);
        console.error('Full error:', err);
    } finally {
        await client.end();
    }
}

testInsert();
