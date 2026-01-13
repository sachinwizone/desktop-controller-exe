const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function checkData() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        console.log('Connected to database\n');
        
        // Check punch_log_consolidated data
        console.log('=== PUNCH LOG CONSOLIDATED DATA ===');
        const punchResult = await client.query(`
            SELECT id, username, display_name, company_name, 
                   punch_in_time, punch_out_time, 
                   break_duration_seconds, total_work_duration_seconds
            FROM punch_log_consolidated 
            ORDER BY id DESC 
            LIMIT 15
        `);
        
        console.log(`Found ${punchResult.rows.length} records:\n`);
        punchResult.rows.forEach(row => {
            console.log(`ID: ${row.id}`);
            console.log(`  Username: ${row.username}`);
            console.log(`  Display Name: ${row.display_name || 'NULL'}`);
            console.log(`  Company: ${row.company_name}`);
            console.log(`  Punch In: ${row.punch_in_time}`);
            console.log(`  Punch Out: ${row.punch_out_time || 'Still Working'}`);
            console.log(`  Break: ${row.break_duration_seconds || 0} seconds`);
            console.log(`  Total Work: ${row.total_work_duration_seconds || 0} seconds`);
            console.log('');
        });

        // Check company_employees to understand the name mapping
        console.log('\n=== COMPANY EMPLOYEES ===');
        const empResult = await client.query(`
            SELECT employee_id, full_name, company_name, department 
            FROM company_employees 
            LIMIT 10
        `);
        
        empResult.rows.forEach(row => {
            console.log(`  ${row.employee_id}: ${row.full_name} (${row.department}) - ${row.company_name}`);
        });

    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

checkData();
