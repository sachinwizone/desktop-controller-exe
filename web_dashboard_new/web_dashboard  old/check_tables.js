const { Client } = require('pg');

const c = new Client({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

c.connect()
    .then(() => c.query(`
        SELECT table_name, column_name 
        FROM information_schema.columns 
        WHERE table_name IN ('web_logs', 'application_logs', 'inactivity_logs', 'screenshot') 
        ORDER BY table_name, ordinal_position
    `))
    .then(r => {
        console.log('=== TABLE COLUMNS ===');
        let currentTable = '';
        r.rows.forEach(x => {
            if (x.table_name !== currentTable) {
                currentTable = x.table_name;
                console.log('\n' + currentTable.toUpperCase() + ':');
            }
            console.log('  - ' + x.column_name);
        });
        return c.end();
    })
    .catch(e => {
        console.error('Error:', e.message);
        c.end();
    });
