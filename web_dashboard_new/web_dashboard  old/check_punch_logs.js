const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function checkPunchLogs() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        const result = await client.query(
            `SELECT * FROM punch_logs ORDER BY id DESC LIMIT 10`
        );
        
        console.log(`Total punch logs in database: ${result.rows.length}`);
        if (result.rows.length > 0) {
            console.log('\nRecent punch logs:');
            result.rows.forEach((row, index) => {
                console.log(`\n[${index + 1}] ID: ${row.id}`);
                console.log(`  Type: ${row.punch_type}`);
                console.log(`  Username: ${row.username}`);
                console.log(`  Company: ${row.company_name}`);
                console.log(`  Punch Time: ${row.punch_time}`);
                console.log(`  Duration: ${row.total_duration_seconds}s`);
            });
        }
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

checkPunchLogs();
