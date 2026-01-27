/**
 * System Control API - Backend Endpoints
 * Handles system info collection, device management, and remote control
 */

const express = require('express');
const router = express.Router();
const { MongoClient } = require('mongodb');

let mongoClient = null;
let systemsDb = null;

// Initialize MongoDB connection
async function initializeDB() {
    try {
        if (!mongoClient) {
            mongoClient = new MongoClient('mongodb://72.61.170.243:9095');
            await mongoClient.connect();
            systemsDb = mongoClient.db('controller_application');
        }
        
        const systemsCollection = systemsDb.collection('registered_systems');
        const devicesCollection = systemsDb.collection('devices');
        const systemControlCollection = systemsDb.collection('system_control_commands');
        
        // Create indexes
        await systemsCollection.createIndex({ device_id: 1, timestamp: -1 });
        await systemsCollection.createIndex({ company_name: 1 });
        await devicesCollection.createIndex({ device_id: 1 });
        await devicesCollection.createIndex({ company_name: 1, username: 1 });
        await systemControlCollection.createIndex({ device_id: 1, created_at: -1 });
        
    } catch (err) {
        console.error('[System Control] DB Init Error:', err.message);
    }
}

/**
 * POST /api/system/register-device
 * Register a new system/device
 */
router.post('/register-device', async (req, res) => {
    try {
        await initializeDB();
        const { device_id, hostname, username, company_name, machine_id, ip_address } = req.body;
        
        if (!device_id || !hostname || !username || !company_name) {
            return res.json({ success: false, error: 'Missing required fields' });
        }
        
        const devicesCollection = systemsDb.collection('devices');
        
        const result = await devicesCollection.updateOne(
            { device_id },
            {
                $set: {
                    device_id,
                    hostname,
                    username,
                    company_name,
                    machine_id,
                    ip_address,
                    registered_at: new Date(),
                    last_seen: new Date(),
                    is_active: true,
                    status: 'online'
                }
            },
            { upsert: true }
        );
        
        res.json({ 
            success: true, 
            message: 'Device registered successfully',
            data: { device_id, hostname, username }
        });
    } catch (err) {
        console.error('[System Control] Register Device Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * POST /api/system/info
 * Receive and store system information from client
 */
router.post('/info', async (req, res) => {
    try {
        await initializeDB();
        const systemInfo = req.body;
        const systemsCollection = systemsDb.collection('registered_systems');
        
        const result = await systemsCollection.insertOne({
            ...systemInfo,
            timestamp: new Date(),
            _timestamp: Math.floor(Date.now() / 1000)
        });
        
        res.json({ 
            success: true, 
            message: 'System info received',
            id: result.insertedId
        });
    } catch (err) {
        console.error('[System Control] Info Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * GET /api/system/get-devices
 * Get all registered devices for a company
 */
router.get('/get-devices', async (req, res) => {
    try {
        await initializeDB();
        const { company_name } = req.query;
        
        if (!company_name) {
            return res.json({ success: false, error: 'company_name required' });
        }
        
        const devicesCollection = systemsDb.collection('devices');
        const devices = await devicesCollection
            .find({ company_name })
            .sort({ last_seen: -1 })
            .toArray();
        
        res.json({ 
            success: true, 
            data: devices,
            count: devices.length
        });
    } catch (err) {
        console.error('[System Control] Get Devices Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * GET /api/system/get-system-info
 * Get latest system information for a device
 */
router.get('/get-system-info', async (req, res) => {
    try {
        await initializeDB();
        const { device_id } = req.query;
        
        if (!device_id) {
            return res.json({ success: false, error: 'device_id required' });
        }
        
        const systemsCollection = systemsDb.collection('registered_systems');
        const systemInfo = await systemsCollection
            .findOne({ DeviceId: device_id }, { sort: { timestamp: -1 } });
        
        if (!systemInfo) {
            return res.json({ success: false, error: 'No data found for device' });
        }
        
        res.json({ 
            success: true, 
            data: systemInfo
        });
    } catch (err) {
        console.error('[System Control] Get System Info Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * GET /api/system/get-all-registered-users
 * Get all registered users for a company
 */
router.get('/get-all-registered-users', async (req, res) => {
    try {
        await initializeDB();
        const { company_name } = req.query;
        
        if (!company_name) {
            return res.json({ success: false, error: 'company_name required' });
        }
        
        const devicesCollection = systemsDb.collection('devices');
        const users = await devicesCollection
            .aggregate([
                { $match: { company_name } },
                { $group: { _id: '$username', count: { $sum: 1 }, devices: { $push: '$$ROOT' } } },
                { $sort: { _id: 1 } }
            ])
            .toArray();
        
        res.json({ 
            success: true, 
            data: users,
            total: users.length
        });
    } catch (err) {
        console.error('[System Control] Get Users Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * POST /api/system/send-command
 * Send control command to a device
 */
router.post('/send-command', async (req, res) => {
    try {
        await initializeDB();
        const { device_id, command, parameters, issued_by } = req.body;
        
        if (!device_id || !command) {
            return res.json({ success: false, error: 'device_id and command required' });
        }
        
        const systemControlCollection = systemsDb.collection('system_control_commands');
        
        const result = await systemControlCollection.insertOne({
            device_id,
            command,
            parameters: parameters || {},
            issued_by,
            status: 'pending',
            created_at: new Date(),
            executed_at: null,
            result: null
        });
        
        res.json({ 
            success: true, 
            message: 'Command sent',
            command_id: result.insertedId
        });
    } catch (err) {
        console.error('[System Control] Send Command Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * GET /api/system/get-pending-commands
 * Get pending commands for a device
 */
router.get('/get-pending-commands', async (req, res) => {
    try {
        await initializeDB();
        const { device_id } = req.query;
        
        if (!device_id) {
            return res.json({ success: false, error: 'device_id required' });
        }
        
        const systemControlCollection = systemsDb.collection('system_control_commands');
        const commands = await systemControlCollection
            .find({ device_id, status: 'pending' })
            .toArray();
        
        res.json({ 
            success: true, 
            data: commands,
            count: commands.length
        });
    } catch (err) {
        console.error('[System Control] Get Commands Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * PUT /api/system/update-command-status
 * Update command execution status
 */
router.put('/update-command-status', async (req, res) => {
    try {
        await initializeDB();
        const { command_id, status, result } = req.body;
        
        if (!command_id || !status) {
            return res.json({ success: false, error: 'command_id and status required' });
        }
        
        const systemControlCollection = systemsDb.collection('system_control_commands');
        const { ObjectId } = require('mongodb');
        
        const updateData = {
            status,
            executed_at: new Date()
        };
        
        if (result) {
            updateData.result = result;
        }
        
        await systemControlCollection.updateOne(
            { _id: new ObjectId(command_id) },
            { $set: updateData }
        );
        
        res.json({ 
            success: true, 
            message: 'Command status updated'
        });
    } catch (err) {
        console.error('[System Control] Update Command Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

/**
 * GET /api/system/device-stats
 * Get statistics for all devices
 */
router.get('/device-stats', async (req, res) => {
    try {
        await initializeDB();
        const { company_name } = req.query;
        
        const devicesCollection = systemsDb.collection('devices');
        
        let query = {};
        if (company_name) {
            query = { company_name };
        }
        
        const stats = await devicesCollection.aggregate([
            { $match: query },
            {
                $group: {
                    _id: '$company_name',
                    total_devices: { $sum: 1 },
                    online_devices: {
                        $sum: { $cond: [{ $eq: ['$status', 'online'] }, 1, 0] }
                    },
                    total_users: { $sum: 1 }
                }
            }
        ]).toArray();
        
        res.json({ 
            success: true, 
            data: stats
        });
    } catch (err) {
        console.error('[System Control] Stats Error:', err.message);
        res.json({ success: false, error: err.message });
    }
});

module.exports = router;
