const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function createUnifiedPunchTable() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        console.log('Creating unified punch_log_consolidated table...');
        
        // Drop old table if exists
        await client.query('DROP TABLE IF EXISTS punch_log_consolidated CASCADE');
        
        // Create new unified table
        const createTableSQL = `
            CREATE TABLE punch_log_consolidated (
                id SERIAL PRIMARY KEY,
                activation_key VARCHAR(100),
                company_name VARCHAR(255),
                username VARCHAR(100) NOT NULL,
                system_name VARCHAR(255),
                ip_address VARCHAR(50),
                machine_id VARCHAR(100),
                
                punch_in_time TIMESTAMP WITH TIME ZONE,
                punch_out_time TIMESTAMP WITH TIME ZONE,
                
                break_start_time TIMESTAMP WITH TIME ZONE,
                break_end_time TIMESTAMP WITH TIME ZONE,
                break_duration_seconds INTEGER DEFAULT 0,
                
                total_work_duration_seconds INTEGER DEFAULT 0,
                
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            )
        `;
        
        await client.query(createTableSQL);
        console.log('✓ Table created successfully!');
        
        // Show table structure
        console.log('\nTable structure:');
        const schemaResult = await client.query(
            "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'punch_log_consolidated' ORDER BY ordinal_position"
        );
        
        schemaResult.rows.forEach(row => {
            console.log(`  ${row.column_name}: ${row.data_type} (nullable=${row.is_nullable})`);
        });
        
        console.log('\nTable ready for consolidated punch logging!');
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

createUnifiedPunchTable();
