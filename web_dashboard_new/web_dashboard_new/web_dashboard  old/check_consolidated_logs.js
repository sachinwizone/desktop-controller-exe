const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function checkConsolidatedLogs() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        const result = await client.query(
            `SELECT * FROM punch_log_consolidated ORDER BY id DESC LIMIT 20`
        );
        
        console.log(`Total consolidated punch logs: ${result.rows.length}\n`);
        
        if (result.rows.length > 0) {
            console.log('Consolidated Punch Log Records:');
            console.log('='.repeat(150));
            
            result.rows.forEach((row, i) => {
                console.log(`\n[Record ${i + 1}]`);
                console.log(`  ID: ${row.id}`);
                console.log(`  Username: ${row.username}`);
                console.log(`  Company: ${row.company_name || 'N/A'}`);
                console.log(`  Activation Key: ${row.activation_key || 'N/A'}`);
                console.log(`  `);
                console.log(`  PUNCH IN: ${row.punch_in_time || 'Not set'}`);
                console.log(`  PUNCH OUT: ${row.punch_out_time || 'Not set'}`);
                console.log(`  Total Work Duration: ${row.total_work_duration_seconds || 0} seconds`);
                console.log(`  `);
                console.log(`  BREAK START: ${row.break_start_time || 'Not set'}`);
                console.log(`  BREAK END: ${row.break_end_time || 'Not set'}`);
                console.log(`  Break Duration: ${row.break_duration_seconds || 0} seconds`);
                console.log(`  `);
                console.log(`  Created: ${row.created_at}`);
                console.log(`  Updated: ${row.updated_at}`);
            });
        } else {
            console.log('No consolidated punch logs yet. New logs will appear after user punches in.');
        }
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

checkConsolidatedLogs();
