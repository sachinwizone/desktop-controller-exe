const { Pool } = require('pg');

const pool = new Pool({
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
});

async function addIdleThresholdColumn() {
    try {
        console.log('Adding significant_idle_threshold_minutes column to company_employees...');
        
        await pool.query(`
            ALTER TABLE company_employees 
            ADD COLUMN IF NOT EXISTS significant_idle_threshold_minutes INTEGER DEFAULT 10 NOT NULL;
        `);
        
        console.log('✓ Column added successfully!');
        console.log('Default significant idle threshold set to 10 minutes for all employees');
        
        // Verify the column was added
        const result = await pool.query(`
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'company_employees' 
            AND column_name = 'significant_idle_threshold_minutes'
        `);
        
        if (result.rows.length > 0) {
            console.log('✓ Verification successful:', result.rows[0]);
        }
        
        pool.end();
    } catch (error) {
        console.error('Error adding column:', error.message);
        pool.end();
        process.exit(1);
    }
}

addIdleThresholdColumn();
