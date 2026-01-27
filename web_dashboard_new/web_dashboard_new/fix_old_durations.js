const pg = require('pg');
const pool = new pg.Pool({
  host: '72.61.170.243',
  port: 9095,
  database: 'controller_application',
  user: 'appuser',
  password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

// Recalculate duration_minutes for all records with down_end set
pool.query(`
  UPDATE website_downtime 
  SET duration_minutes = EXTRACT(EPOCH FROM (down_end - down_start)) / 60
  WHERE down_end IS NOT NULL AND duration_minutes IS NOT NULL
  RETURNING id, down_start, down_end, duration_minutes
`).then(res => {
  console.log('Recalculated ' + res.rowCount + ' downtime records');
  console.table(res.rows.map((r, i) => ({
    record: i + 1,
    duration_min: Math.round(r.duration_minutes * 10) / 10
  })));
  process.exit();
}).catch(err => {
  console.error('Error:', err.message);
  process.exit(1);
});
