const pg = require('pg');
const pool = new pg.Pool({
  host: '72.61.170.243',
  port: 9095,
  database: 'controller_application',
  user: 'appuser',
  password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

pool.query(`
  SELECT 
    down_start, 
    down_end, 
    duration_minutes, 
    EXTRACT(EPOCH FROM (down_end - down_start)) as calculated_seconds,
    (EXTRACT(EPOCH FROM (down_end - down_start)) / 60) as calculated_minutes
  FROM website_downtime 
  WHERE down_end IS NOT NULL 
  LIMIT 5
`).then(res => {
  console.log('Downtime Records with Duration Calculation:');
  console.table(res.rows.map(row => ({
    stored_duration_min: row.duration_minutes,
    calc_minutes: Math.round(row.calculated_minutes),
    calc_seconds: Math.round(row.calculated_seconds)
  })));
  process.exit();
}).catch(err => {
  console.error('Error:', err.message);
  process.exit(1);
});
