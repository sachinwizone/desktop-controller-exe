const pg = require('pg');
const pool = new pg.Pool({
  host: '72.61.170.243',
  port: 9095,
  database: 'controller_application',
  user: 'appuser',
  password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

// Test the record_downtime with status='up' for site_id=1
pool.query(`
  UPDATE website_downtime 
  SET down_end = CURRENT_TIMESTAMP, 
      duration_minutes = EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - down_start)) / 60,
      status = 'recovered'
  WHERE site_id = 1 AND down_end IS NULL
  RETURNING id, down_start, down_end, duration_minutes
`).then(res => {
  console.log('Updated downtime record:');
  console.table(res.rows);
  process.exit();
}).catch(err => {
  console.error('Error:', err.message);
  process.exit(1);
});
