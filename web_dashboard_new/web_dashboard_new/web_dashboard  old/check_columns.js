const { Client } = require('pg');

const c = new Client({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

const tables = ['web_logs', 'application_logs', 'inactivity_logs', 'screenshots', 'punch_logs'];

c.connect()
    .then(async () => {
        for (const table of tables) {
            const r = await c.query("SELECT column_name FROM information_schema.columns WHERE table_name = $1 ORDER BY ordinal_position", [table]);
            if (r.rows.length > 0) {
                console.log(`${table}: ${r.rows.map(x => x.column_name).join(', ')}`);
            } else {
                console.log(`${table}: TABLE NOT FOUND`);
            }
        }
        return c.end();
    })
    .catch(e => {
        console.error(e.message);
        c.end();
    });
