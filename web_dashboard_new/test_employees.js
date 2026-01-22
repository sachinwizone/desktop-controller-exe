const { Client } = require('pg');

const dbConfig = {
    host: '72.61.170.243',
    port: 9095,
    database: 'controller_application',
    user: 'appuser',
    password: 'jksdj$&^&*YUG*^%&THJHIO4546GHG&j'
};

async function queryDB(sql, params = []) {
    const client = new Client(dbConfig);
    try {
        await client.connect();
        console.log('Connected to DB');
        const result = await client.query(sql, params);
        console.log('Query succeeded:', result.rows.length, 'rows');
        return result.rows;
    } catch (err) {
        console.error('Database error:', err.message);
        throw err;
    } finally {
        await client.end();
    }
}

async function test() {
    try {
        const { company_name, page = 1, limit = 50 } = {
            company_name: 'WIZONE IT NETWORK INDIA PVT LTD',
            page: 1,
            limit: 100
        };
        
        let whereClause = '';
        let params = [];

        if (company_name) {
            whereClause = `WHERE company_name = $1`;
            params = [company_name];
        }

        // Get total count
        const countSql = `SELECT COUNT(*) as total FROM company_employees ${whereClause}`;
        console.log('Count SQL:', countSql);
        console.log('Count Params:', params);
        
        const countResult = await queryDB(countSql, params);
        console.log('Count result:', countResult);
        const totalRecords = parseInt(countResult[0]?.total || 0);
        console.log('Total records:', totalRecords);
        
        const totalPages = Math.ceil(totalRecords / limit);
        const offset = (page - 1) * limit;

        // Get paginated records
        const paramIndex = params.length + 1;
        const recordsSql = `
            SELECT 
                id,
                employee_id,
                full_name,
                email,
                phone,
                department,
                designation,
                is_active,
                company_name,
                created_at,
                updated_at
            FROM company_employees
            ${whereClause}
            ORDER BY created_at DESC
            LIMIT $${paramIndex} OFFSET $${paramIndex + 1}
        `;
        console.log('Records SQL:', recordsSql);
        console.log('Records Params:', [...params, limit, offset]);
        
        const records = await queryDB(recordsSql, [...params, limit, offset]);
        console.log('Records fetched:', records.length);
        
        console.log('\nSuccess! Response would be:');
        console.log(JSON.stringify({
            success: true,
            data: {
                records,
                pagination: {
                    current_page: parseInt(page),
                    total_pages: totalPages,
                    total_records: totalRecords,
                    limit: parseInt(limit)
                }
            }
        }, null, 2).substring(0, 500));

    } catch (err) {
        console.error('Test error:', err.message);
        console.error('Stack:', err.stack);
    }
}

test();
