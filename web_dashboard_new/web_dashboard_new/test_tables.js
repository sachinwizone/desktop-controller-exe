const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function testDatabase() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        console.log('✓ Database connected');

        // Check if company_employees table exists
        const empTableResult = await client.query(`
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'company_employees'
            );
        `);
        console.log('company_employees table exists:', empTableResult.rows[0].exists);

        if (empTableResult.rows[0].exists) {
            const empCount = await client.query('SELECT COUNT(*) FROM company_employees');
            console.log('Employees in database:', empCount.rows[0].count);
        }

        // Check if company_departments table exists
        const deptTableResult = await client.query(`
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'company_departments'
            );
        `);
        console.log('company_departments table exists:', deptTableResult.rows[0].exists);

        if (deptTableResult.rows[0].exists) {
            const deptCount = await client.query('SELECT COUNT(*) FROM company_departments');
            console.log('Departments in database:', deptCount.rows[0].count);
        }

        // Try to get departments
        console.log('\n--- Testing get_departments ---');
        try {
            const depts = await client.query(
                'SELECT id, department_name, company_name FROM company_departments LIMIT 5'
            );
            console.log('Departments:', depts.rows);
        } catch (err) {
            console.log('Error fetching departments:', err.message);
        }

        // Try to get employees
        console.log('\n--- Testing get_company_employees ---');
        try {
            const emps = await client.query(
                'SELECT id, employee_id, full_name, email FROM company_employees LIMIT 5'
            );
            console.log('Employees:', emps.rows);
        } catch (err) {
            console.log('Error fetching employees:', err.message);
        }

    } catch (err) {
        console.error('Database error:', err.message);
    } finally {
        await client.end();
    }
}

testDatabase();
