const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function getAllPunchData() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        const result = await client.query(
            `SELECT 
              id, 
              activation_key,
              company_name, 
              username,
              punch_type,
              punch_time,
              punch_out_time,
              total_duration_seconds,
              log_timestamp
            FROM punch_logs 
            ORDER BY id DESC 
            LIMIT 20`
        );
        
        console.log('All Punch Log Data with Complete Details:');
        console.log('='.repeat(120));
        
        result.rows.forEach((row, i) => {
            console.log(`\nRecord ${i + 1}:`);
            console.log(`  ID: ${row.id}`);
            console.log(`  Activation Key: ${row.activation_key || 'N/A'}`);
            console.log(`  Company: ${row.company_name || 'N/A'}`);
            console.log(`  Username: ${row.username}`);
            console.log(`  Type: ${row.punch_type}`);
            console.log(`  Punch Time: ${row.punch_time}`);
            console.log(`  Punch Out Time: ${row.punch_out_time || 'N/A'}`);
            console.log(`  Duration (seconds): ${row.total_duration_seconds}`);
            console.log(`  Log Timestamp: ${row.log_timestamp}`);
        });
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

getAllPunchData();
