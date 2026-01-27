const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function checkSchema() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        const result = await client.query(
            "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = 'punch_logs' ORDER BY ordinal_position"
        );
        console.log('Column Schema:');
        result.rows.forEach(row => {
            console.log(`  ${row.column_name}: ${row.data_type} (nullable=${row.is_nullable}, default=${row.column_default || 'NONE'})`);
        });
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

checkSchema();
