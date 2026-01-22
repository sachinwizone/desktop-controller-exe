const { Client } = require('pg');

async function testDB() {
    const client = new Client({
        host: '72.61.170.243',
        port: 9095,
        database: 'controller_application',
        user: 'appuser',
        password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
    });

    try {
        await client.connect();
        console.log('Connected to database');

        // Try to get all tables
        const tables = await client.query(`
            SELECT table_name FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name
        `);
        
        console.log('\nAll tables in public schema:');
        tables.rows.forEach(row => console.log('  -', row.table_name));

        // Check for specific tables
        const compEmpExists = await client.query(`
            SELECT COUNT(*) FROM information_schema.tables 
            WHERE table_name = 'company_employees'
        `);
        console.log('\ncompany_employees exists:', parseInt(compEmpExists.rows[0].count) > 0);

        const compDeptExists = await client.query(`
            SELECT COUNT(*) FROM information_schema.tables 
            WHERE table_name = 'company_departments'
        `);
        console.log('company_departments exists:', parseInt(compDeptExists.rows[0].count) > 0);

        // If company_employees exists, check its columns
        if (parseInt(compEmpExists.rows[0].count) > 0) {
            const cols = await client.query(`
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_name = 'company_employees'
                ORDER BY ordinal_position
            `);
            console.log('\ncompany_employees columns:');
            cols.rows.forEach(row => console.log(`  - ${row.column_name}: ${row.data_type}`));
        }

        // If company_departments exists, check its columns
        if (parseInt(compDeptExists.rows[0].count) > 0) {
            const cols = await client.query(`
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_name = 'company_departments'
                ORDER BY ordinal_position
            `);
            console.log('\ncompany_departments columns:');
            cols.rows.forEach(row => console.log(`  - ${row.column_name}: ${row.data_type}`));
        }

    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

testDB();
