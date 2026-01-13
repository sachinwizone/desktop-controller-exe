const { Pool } = require('pg');

const pool = new Pool({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

async function createTable() {
    const client = await pool.connect();
    try {
        await client.query(`
            CREATE TABLE IF NOT EXISTS connected_systems (
                id SERIAL PRIMARY KEY,
                company_name VARCHAR(255) NOT NULL,
                employee_id VARCHAR(100) NOT NULL,
                display_name VARCHAR(255),
                department VARCHAR(100),
                machine_id VARCHAR(255),
                machine_name VARCHAR(255),
                ip_address VARCHAR(50),
                os_version VARCHAR(100),
                app_version VARCHAR(50) DEFAULT '1.0',
                last_heartbeat TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                first_connected TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                is_online BOOLEAN DEFAULT true,
                status VARCHAR(50) DEFAULT 'active',
                UNIQUE(company_name, employee_id, machine_id)
            )
        `);
        console.log('✅ connected_systems table created');
        
        // Create index for faster queries
        await client.query(`
            CREATE INDEX IF NOT EXISTS idx_connected_systems_company 
            ON connected_systems(company_name, is_online)
        `);
        console.log('✅ Index created');
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        client.release();
        pool.end();
    }
}

createTable();
