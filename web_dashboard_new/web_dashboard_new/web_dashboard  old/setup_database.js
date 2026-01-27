const { Client } = require('pg');

const client = new Client({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

async function setupDatabase() {
    await client.connect();
    console.log('Connected to database\n');
    
    try {
        // Ensure connected_systems table has proper structure
        console.log('Setting up connected_systems table...');
        
        // Add unique constraint if not exists
        await client.query(`
            DO $$ 
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint 
                    WHERE conname = 'connected_systems_company_emp_machine_key'
                ) THEN
                    ALTER TABLE connected_systems 
                    ADD CONSTRAINT connected_systems_company_emp_machine_key 
                    UNIQUE (company_name, employee_id, machine_id);
                END IF;
            EXCEPTION WHEN duplicate_object THEN
                NULL;
            END $$;
        `);
        console.log('✓ Unique constraint ready');
        
        // Ensure system_id column exists
        await client.query(`
            ALTER TABLE connected_systems 
            ADD COLUMN IF NOT EXISTS system_id VARCHAR(50)
        `);
        console.log('✓ system_id column ready');
        
        // Show current structure
        const cols = await client.query(`
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'connected_systems'
            ORDER BY ordinal_position
        `);
        console.log('\nTable columns:');
        cols.rows.forEach(r => console.log(`  - ${r.column_name}: ${r.data_type}`));
        
        // Show current data
        const data = await client.query(`
            SELECT company_name, employee_id, display_name, system_id, is_online, last_heartbeat
            FROM connected_systems
            ORDER BY last_heartbeat DESC
        `);
        console.log('\nCurrent connected systems:');
        data.rows.forEach(r => {
            console.log(`  ${r.employee_id}: ${r.display_name} | Company: ${r.company_name} | ID: ${r.system_id} | Online: ${r.is_online}`);
        });
        
    } catch (err) {
        console.error('Error:', err.message);
    }
    
    await client.end();
    console.log('\nDone');
}

setupDatabase();
