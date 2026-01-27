const { Client } = require('pg');

const c = new Client({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

c.connect()
    .then(() => c.query("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"))
    .then(r => {
        console.log('All tables:');
        r.rows.forEach(row => console.log(' -', row.table_name));
        return c.end();
    })
    .catch(e => {
        console.error(e.message);
        c.end();
    });
