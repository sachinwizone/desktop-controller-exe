const {Client} = require('pg');
const client = new Client({
  host: '72.61.170.243',
  port: 9095,
  database: 'controller_application',
  user: 'appuser',
  password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

async function test() {
  try {
    await client.connect();
    console.log('Connected!');
    const result = await client.query('SELECT * FROM company_users LIMIT 1');
    console.log('Query result:', result.rows);
    await client.end();
    console.log('Closed!');
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

test();
