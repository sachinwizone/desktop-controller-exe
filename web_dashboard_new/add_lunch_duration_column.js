const { Pool } = require('pg');

const pool = new Pool({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j',
});

async function addLunchDurationColumn() {
    const client = await pool.connect();
    try {
        console.log('Starting migration: Adding lunch_duration column to company_employees...');
        
        // Check if column already exists
        const checkColumn = await client.query(`
            SELECT column_name 
            FROM information_schema.columns 
            WHERE table_name = 'company_employees' 
            AND column_name = 'lunch_duration'
        `);
        
        if (checkColumn.rows.length > 0) {
            console.log('✅ lunch_duration column already exists');
            return;
        }
        
        // Add the column
        await client.query(`
            ALTER TABLE company_employees 
            ADD COLUMN lunch_duration INTEGER DEFAULT 60 NOT NULL
        `);
        
        console.log('✅ Successfully added lunch_duration column to company_employees table');
        console.log('   - Column type: INTEGER');
        console.log('   - Default value: 60 (minutes)');
        console.log('   - NOT NULL constraint applied');
        
        // Verify the column was added
        const verifyColumn = await client.query(`
            SELECT column_name, data_type, column_default, is_nullable
            FROM information_schema.columns 
            WHERE table_name = 'company_employees' 
            AND column_name = 'lunch_duration'
        `);
        
        if (verifyColumn.rows.length > 0) {
            const col = verifyColumn.rows[0];
            console.log('\n✅ Verification:');
            console.log(`   - Column Name: ${col.column_name}`);
            console.log(`   - Data Type: ${col.data_type}`);
            console.log(`   - Default: ${col.column_default}`);
            console.log(`   - Nullable: ${col.is_nullable}`);
        }
        
    } catch (error) {
        console.error('❌ Error adding lunch_duration column:', error.message);
        throw error;
    } finally {
        client.release();
        await pool.end();
    }
}

addLunchDurationColumn().then(() => {
    console.log('\n✅ Migration completed successfully');
    process.exit(0);
}).catch(err => {
    console.error('Migration failed:', err);
    process.exit(1);
});
