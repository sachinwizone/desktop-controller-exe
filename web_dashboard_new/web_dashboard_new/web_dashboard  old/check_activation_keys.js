const { Client } = require('pg');

const client = new Client({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

async function main() {
    await client.connect();
    
    // Create company_employees table if not exists
    await client.query(`
        CREATE TABLE IF NOT EXISTS company_employees (
            id SERIAL PRIMARY KEY,
            company_name VARCHAR(255) NOT NULL,
            employee_id VARCHAR(50),
            full_name VARCHAR(255) NOT NULL,
            email VARCHAR(255),
            phone VARCHAR(50),
            department VARCHAR(100),
            designation VARCHAR(100),
            is_active BOOLEAN DEFAULT true,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    `);
    console.log('company_employees table ready');
    
    // Check columns
    const cols = await client.query("SELECT column_name FROM information_schema.columns WHERE table_name = 'company_employees' ORDER BY ordinal_position");
    console.log('Columns:', cols.rows.map(r => r.column_name));
    
    await client.end();
}

main().catch(e => {
    console.error(e.message);
    client.end();
});
