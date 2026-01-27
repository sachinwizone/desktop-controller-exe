const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function displayReport() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        const result = await client.query(
            `SELECT 
              id,
              username,
              company_name,
              punch_in_time,
              punch_out_time,
              total_work_duration_seconds,
              break_start_time,
              break_end_time,
              break_duration_seconds
            FROM punch_log_consolidated 
            ORDER BY id DESC`
        );
        
        console.log('\n╔═══════════════════════════════════════════════════════════════════════════════╗');
        console.log('║                   PUNCH LOG CONSOLIDATED REPORT                              ║');
        console.log('╚═══════════════════════════════════════════════════════════════════════════════╝\n');
        
        if (result.rows.length === 0) {
            console.log('No punch records yet. Records will appear after user punches in.\n');
        } else {
            console.log(`Total Punch Sessions: ${result.rows.length}\n`);
            
            result.rows.forEach((row, idx) => {
                console.log(`\n${idx + 1}. ${row.username} - ${row.company_name}`);
                console.log('   ' + '─'.repeat(75));
                
                if (row.punch_in_time) {
                    console.log(`   Punch In:  ${new Date(row.punch_in_time).toLocaleString('en-IN')}`);
                } else {
                    console.log(`   Punch In:  Not set`);
                }
                
                if (row.punch_out_time) {
                    console.log(`   Punch Out: ${new Date(row.punch_out_time).toLocaleString('en-IN')}`);
                    if (row.total_work_duration_seconds) {
                        const hrs = Math.floor(row.total_work_duration_seconds / 3600);
                        const mins = Math.floor((row.total_work_duration_seconds % 3600) / 60);
                        const secs = row.total_work_duration_seconds % 60;
                        console.log(`   Duration:  ${hrs}h ${mins}m ${secs}s`);
                    }
                } else {
                    console.log(`   Punch Out: Not set (Still working...)`);
                }
                
                if (row.break_start_time) {
                    console.log(`   `);
                    console.log(`   Break Start: ${new Date(row.break_start_time).toLocaleString('en-IN')}`);
                    
                    if (row.break_end_time) {
                        console.log(`   Break End:   ${new Date(row.break_end_time).toLocaleString('en-IN')}`);
                        if (row.break_duration_seconds) {
                            const mins = Math.floor(row.break_duration_seconds / 60);
                            const secs = row.break_duration_seconds % 60;
                            console.log(`   Break Time:  ${mins}m ${secs}s`);
                        }
                    } else {
                        console.log(`   Break End:   Not set (Still on break...)`);
                    }
                }
            });
            
            console.log('\n');
        }
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

displayReport();
