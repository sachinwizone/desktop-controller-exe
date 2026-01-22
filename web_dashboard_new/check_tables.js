const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function checkTables() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        // Check for tables
        const tables = ['web_logs', 'application_logs', 'inactivity_logs', 'screenshot_logs', 'punch_log_consolidated'];
        
        for (const table of tables) {
            console.log('\n===== TABLE: ' + table + ' =====');
            try {
                const result = await client.query(
                    `SELECT column_name, data_type, is_nullable, column_default
                    FROM information_schema.columns 
                    WHERE table_name = $1 AND table_schema = 'public'
                    ORDER BY ordinal_position`,
                    [table]
                );
                
                if (result.rows.length === 0) {
                    console.log('Table does not exist');
                } else {
                    console.log('Columns:');
                    result.rows.forEach(col => {
                        console.log('  - ' + col.column_name + ' (' + col.data_type + ')' + (col.is_nullable === 'NO' ? ' NOT NULL' : ''));
                    });
                    
                    // Count records
                    const countResult = await client.query(`SELECT COUNT(*) as count FROM ${table}`);
                    console.log('Records: ' + countResult.rows[0].count);
                }
            } catch (err) {
                console.log('Error checking table: ' + err.message);
            }
        }
    } finally {
        await client.end();
    }
}

checkTables().catch(console.error);
