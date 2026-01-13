const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function showTableStructure() {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        
        console.log('╔════════════════════════════════════════════════════════════════════════════╗');
        console.log('║         UNIFIED PUNCH LOG TABLE STRUCTURE                                  ║');
        console.log('║         (punch_log_consolidated)                                           ║');
        console.log('╚════════════════════════════════════════════════════════════════════════════╝');
        console.log('');
        
        const result = await client.query(
            "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = 'punch_log_consolidated' ORDER BY ordinal_position"
        );
        
        console.log('EMPLOYEE IDENTIFICATION:');
        const idCols = result.rows.filter(r => ['id', 'activation_key', 'company_name', 'username', 'system_name', 'ip_address', 'machine_id'].includes(r.column_name));
        idCols.forEach(col => {
            console.log(`  ✓ ${col.column_name.padEnd(25)} : ${col.data_type.padEnd(30)} (nullable: ${col.is_nullable})`);
        });
        
        console.log('');
        console.log('PUNCH IN/OUT TRACKING:');
        const punchCols = result.rows.filter(r => ['punch_in_time', 'punch_out_time', 'total_work_duration_seconds'].includes(r.column_name));
        punchCols.forEach(col => {
            console.log(`  ✓ ${col.column_name.padEnd(25)} : ${col.data_type.padEnd(30)} (nullable: ${col.is_nullable})`);
        });
        
        console.log('');
        console.log('BREAK TRACKING:');
        const breakCols = result.rows.filter(r => ['break_start_time', 'break_end_time', 'break_duration_seconds'].includes(r.column_name));
        breakCols.forEach(col => {
            console.log(`  ✓ ${col.column_name.padEnd(25)} : ${col.data_type.padEnd(30)} (nullable: ${col.is_nullable})`);
        });
        
        console.log('');
        console.log('AUDIT TIMESTAMPS:');
        const auditCols = result.rows.filter(r => ['created_at', 'updated_at'].includes(r.column_name));
        auditCols.forEach(col => {
            console.log(`  ✓ ${col.column_name.padEnd(25)} : ${col.data_type.padEnd(30)} (nullable: ${col.is_nullable})`);
        });
        
        console.log('');
        console.log('═'.repeat(80));
        console.log('HOW IT WORKS:');
        console.log('═'.repeat(80));
        console.log('');
        console.log('1. User PUNCHES IN');
        console.log('   → Creates NEW record with punch_in_time = NOW()');
        console.log('');
        console.log('2. User STARTS BREAK');
        console.log('   → Updates SAME record: break_start_time = NOW()');
        console.log('');
        console.log('3. User RESUMES WORK');
        console.log('   → Updates SAME record:');
        console.log('     - break_end_time = NOW()');
        console.log('     - break_duration_seconds = calculated from break start to end');
        console.log('');
        console.log('4. User PUNCHES OUT');
        console.log('   → Updates SAME record:');
        console.log('     - punch_out_time = NOW()');
        console.log('     - total_work_duration_seconds = calculated from punch in to out');
        console.log('');
        console.log('RESULT: ONE RECORD per punch session with ALL information!');
        console.log('');
        
    } catch (err) {
        console.error('Error:', err.message);
    } finally {
        await client.end();
    }
}

showTableStructure();
