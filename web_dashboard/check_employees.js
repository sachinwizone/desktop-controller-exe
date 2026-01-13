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
    console.log('Connected\n');
    
    // Check company_employees
    console.log('=== REGISTERED EMPLOYEES (company_employees) ===');
    const emp = await client.query('SELECT employee_id, full_name, company_name FROM company_employees ORDER BY employee_id');
    emp.rows.forEach(r => console.log(`  ${r.employee_id}: ${r.full_name} [${r.company_name}]`));
    
    // Check connected_systems
    console.log('\n=== CONNECTED SYSTEMS (connected_systems) ===');
    const sys = await client.query('SELECT employee_id, display_name, company_name, machine_name, system_id, is_online, last_heartbeat FROM connected_systems ORDER BY last_heartbeat DESC');
    sys.rows.forEach(r => console.log(`  ${r.employee_id}: ${r.display_name} | Machine: ${r.machine_name} | ID: ${r.system_id} | Online: ${r.is_online} | Last: ${r.last_heartbeat}`));
    
    await client.end();
}

run().catch(e => console.error(e.message));
