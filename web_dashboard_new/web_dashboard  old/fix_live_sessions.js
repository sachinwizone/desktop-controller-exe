const { Client } = require('pg');

const client = new Client({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

async function run() {
    await client.connect();
    console.log('Connected to database');
    
    try {
        // Drop old table
        await client.query('DROP TABLE IF EXISTS live_sessions');
        console.log('Dropped old live_sessions table');
        
        // Create new table with correct schema
        await client.query(`
            CREATE TABLE live_sessions (
                id SERIAL PRIMARY KEY,
                system_id VARCHAR(100) NOT NULL,
                viewer_id VARCHAR(100) NOT NULL DEFAULT 'admin',
                is_active BOOLEAN DEFAULT TRUE,
                started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(system_id, viewer_id)
            )
        `);
        console.log('Created new live_sessions table with viewer_id column');
        
        // Verify table structure
        const result = await client.query(`
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'live_sessions'
        `);
        console.log('Table columns:', result.rows);
        
    } catch (err) {
        console.error('Error:', err.message);
    }
    
    await client.end();
    console.log('Done');
}

run();
