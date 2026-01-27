const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function addDisplayNameColumn() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        console.log('Connected to database');
        
        // Check if column exists
        const checkResult = await client.query(`
            SELECT column_name FROM information_schema.columns 
            WHERE table_name = 'punch_log_consolidated' AND column_name = 'display_name'
        `);
        
        if (checkResult.rows.length === 0) {
            console.log('Adding display_name column to punch_log_consolidated...');
            await client.query(`
                ALTER TABLE punch_log_consolidated 
                ADD COLUMN display_name VARCHAR(255)
            `);
            console.log('✓ Column added successfully!');
            
            // Copy existing username values to display_name for existing records
            await client.query(`
                UPDATE punch_log_consolidated 
                SET display_name = username 
                WHERE display_name IS NULL
            `);
            console.log('✓ Existing records updated with display_name from username');
        } else {
            console.log('display_name column already exists');
        }
        
        // Show current table structure
        console.log('\nCurrent table structure:');
        const schemaResult = await client.query(`
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'punch_log_consolidated' 
            ORDER BY ordinal_position
        `);
        
        schemaResult.rows.forEach(row => {
            console.log(`  ${row.column_name}: ${row.data_type}`);
        });
        
        // Show sample data
        console.log('\nSample data:');
        const dataResult = await client.query(`
            SELECT id, username, display_name, company_name, punch_in_time 
            FROM punch_log_consolidated 
            ORDER BY id DESC LIMIT 5
        `);
        
        dataResult.rows.forEach(row => {
            console.log(`  ID ${row.id}: username=${row.username}, display_name=${row.display_name}`);
        });
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
        console.log('\nDone!');
    }
}

addDisplayNameColumn();
