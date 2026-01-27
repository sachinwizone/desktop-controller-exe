/**
 * System Management API Endpoints
 * Node.js/Express Backend Controllers
 * Place this in: backend/controllers/systemManagementController.js
 */

const express = require('express');
const router = express.Router();
const SystemInfo = require('../models/SystemInfo');
const InstalledApp = require('../models/InstalledApp');
const ControlCommand = require('../models/ControlCommand');
const CommandResult = require('../models/CommandResult');
const ActiveUser = require('../models/ActiveUser');

// Middleware for authentication
const authenticateApiKey = (req, res, next) => {
    const apiKey = req.headers['x-api-key'];
    if (!apiKey || apiKey !== process.env.API_KEY) {
        return res.status(401).json({ error: 'Unauthorized' });
    }
    next();
};

// ==================== SYSTEM INFO ENDPOINTS ====================

/**
 * POST /api/system-info/sync
 * Receive and store system information from client
 */
router.post('/system-info/sync', authenticateApiKey, async (req, res) => {
    try {
        const systemInfo = req.body;

        const record = await SystemInfo.findOneAndUpdate(
            { activationKey: systemInfo.activationKey, computerName: systemInfo.computerName },
            {
                ...systemInfo,
                lastSyncAt: new Date(),
                updatedAt: new Date()
            },
            { upsert: true, new: true }
        );

        res.json({
            success: true,
            message: 'System information synced successfully',
            recordId: record._id
        });
    } catch (error) {
        console.error('Error syncing system info:', error);
        res.status(500).json({ error: 'Failed to sync system information' });
    }
});

/**
 * GET /api/system-info/:activationKey/:computerName
 * Get latest system information for a device
 */
router.get('/system-info/:activationKey/:computerName', authenticateApiKey, async (req, res) => {
    try {
        const { activationKey, computerName } = req.params;

        const systemInfo = await SystemInfo.findOne({
            activationKey,
            computerName
        }).sort({ collectedAt: -1 });

        if (!systemInfo) {
            return res.status(404).json({ error: 'System information not found' });
        }

        res.json(systemInfo);
    } catch (error) {
        console.error('Error fetching system info:', error);
        res.status(500).json({ error: 'Failed to fetch system information' });
    }
});

/**
 * GET /api/system-info/company/:companyName
 * Get system info for all devices in a company
 */
router.get('/system-info/company/:companyName', authenticateApiKey, async (req, res) => {
    try {
        const { companyName } = req.params;

        const systemInfos = await SystemInfo.find({ companyName })
            .sort({ lastSyncAt: -1 })
            .limit(100);

        res.json({
            count: systemInfos.length,
            data: systemInfos
        });
    } catch (error) {
        console.error('Error fetching company system info:', error);
        res.status(500).json({ error: 'Failed to fetch system information' });
    }
});

// ==================== INSTALLED APPS ENDPOINTS ====================

/**
 * POST /api/installed-apps/sync
 * Receive and store installed applications list
 */
router.post('/installed-apps/sync', authenticateApiKey, async (req, res) => {
    try {
        const { activationKey, computerName, userName, applications, totalApps } = req.body;

        // Store each app
        const appRecords = [];
        for (const app of applications) {
            const record = await InstalledApp.findOneAndUpdate(
                {
                    activationKey,
                    computerName,
                    name: app.name
                },
                {
                    activationKey,
                    computerName,
                    userName,
                    ...app,
                    lastSyncAt: new Date(),
                    updatedAt: new Date()
                },
                { upsert: true, new: true }
            );
            appRecords.push(record);
        }

        res.json({
            success: true,
            message: `Synced ${appRecords.length} applications`,
            totalApps,
            syncedCount: appRecords.length
        });
    } catch (error) {
        console.error('Error syncing installed apps:', error);
        res.status(500).json({ error: 'Failed to sync installed applications' });
    }
});

/**
 * GET /api/installed-apps/:activationKey/:computerName
 * Get installed apps for a device
 */
router.get('/installed-apps/:activationKey/:computerName', authenticateApiKey, async (req, res) => {
    try {
        const { activationKey, computerName } = req.params;

        const apps = await InstalledApp.find({
            activationKey,
            computerName
        }).sort({ name: 1 });

        res.json({
            count: apps.length,
            data: apps
        });
    } catch (error) {
        console.error('Error fetching installed apps:', error);
        res.status(500).json({ error: 'Failed to fetch installed applications' });
    }
});

/**
 * GET /api/installed-apps/search
 * Search installed apps across all devices
 */
router.get('/installed-apps/search', authenticateApiKey, async (req, res) => {
    try {
        const { appName } = req.query;

        if (!appName) {
            return res.status(400).json({ error: 'appName parameter required' });
        }

        const apps = await InstalledApp.find({
            name: { $regex: appName, $options: 'i' }
        }).sort({ name: 1 });

        res.json({
            count: apps.length,
            searchQuery: appName,
            data: apps
        });
    } catch (error) {
        console.error('Error searching apps:', error);
        res.status(500).json({ error: 'Failed to search applications' });
    }
});

// ==================== CONTROL COMMAND ENDPOINTS ====================

/**
 * POST /api/control/commands
 * Create a new control command for a device
 */
router.post('/control/commands', authenticateApiKey, async (req, res) => {
    try {
        const { activationKey, computerName, type, parameters } = req.body;

        if (!type || !activationKey || !computerName) {
            return res.status(400).json({ error: 'Missing required fields' });
        }

        const command = new ControlCommand({
            activationKey,
            computerName,
            type,
            parameters: parameters || {},
            createdAt: new Date(),
            executed: false
        });

        await command.save();

        res.json({
            success: true,
            commandId: command._id,
            message: 'Command created successfully'
        });
    } catch (error) {
        console.error('Error creating command:', error);
        res.status(500).json({ error: 'Failed to create command' });
    }
});

/**
 * GET /api/control/commands
 * Get pending commands for a device
 */
router.get('/control/commands', authenticateApiKey, async (req, res) => {
    try {
        const { activationKey, computerName } = req.query;

        if (!activationKey || !computerName) {
            return res.status(400).json({ error: 'Missing required parameters' });
        }

        const commands = await ControlCommand.find({
            activationKey,
            computerName,
            executed: false
        }).sort({ createdAt: 1 });

        res.json(commands);
    } catch (error) {
        console.error('Error fetching commands:', error);
        res.status(500).json({ error: 'Failed to fetch commands' });
    }
});

/**
 * POST /api/control/command-result
 * Report command execution result from client
 */
router.post('/control/command-result', authenticateApiKey, async (req, res) => {
    try {
        const { commandId, status, message } = req.body;

        const result = new CommandResult({
            commandId,
            status,
            message,
            executedAt: new Date()
        });

        await result.save();

        // Mark command as executed
        await ControlCommand.findByIdAndUpdate(
            commandId,
            { executed: true, executedAt: new Date() }
        );

        res.json({
            success: true,
            message: 'Command result recorded'
        });
    } catch (error) {
        console.error('Error recording command result:', error);
        res.status(500).json({ error: 'Failed to record command result' });
    }
});

/**
 * DELETE /api/control/commands/:commandId
 * Cancel a pending command
 */
router.delete('/control/commands/:commandId', authenticateApiKey, async (req, res) => {
    try {
        const { commandId } = req.params;

        await ControlCommand.findByIdAndDelete(commandId);

        res.json({
            success: true,
            message: 'Command cancelled'
        });
    } catch (error) {
        console.error('Error cancelling command:', error);
        res.status(500).json({ error: 'Failed to cancel command' });
    }
});

// ==================== ACTIVE USERS ENDPOINTS ====================

/**
 * GET /api/active-users/:companyName
 * Get all active users across all devices in a company
 */
router.get('/active-users/:companyName', authenticateApiKey, async (req, res) => {
    try {
        const { companyName } = req.params;

        const activeUsers = await ActiveUser.find({
            companyName,
            isActive: true,
            lastSeenAt: { $gte: new Date(Date.now() - 5 * 60000) } // Last 5 minutes
        }).sort({ lastSeenAt: -1 });

        res.json({
            count: activeUsers.length,
            data: activeUsers
        });
    } catch (error) {
        console.error('Error fetching active users:', error);
        res.status(500).json({ error: 'Failed to fetch active users' });
    }
});

/**
 * POST /api/active-users/ping
 * Heartbeat from client to mark user as active
 */
router.post('/active-users/ping', authenticateApiKey, async (req, res) => {
    try {
        const { activationKey, computerName, userName } = req.body;

        await ActiveUser.findOneAndUpdate(
            { activationKey, computerName, userName },
            {
                activationKey,
                computerName,
                userName,
                isActive: true,
                lastSeenAt: new Date()
            },
            { upsert: true, new: true }
        );

        res.json({ success: true });
    } catch (error) {
        console.error('Error updating active user:', error);
        res.status(500).json({ error: 'Failed to update active user' });
    }
});

// ==================== DASHBOARD STATISTICS ENDPOINTS ====================

/**
 * GET /api/statistics/company/:companyName
 * Get company-wide statistics
 */
router.get('/statistics/company/:companyName', authenticateApiKey, async (req, res) => {
    try {
        const { companyName } = req.params;

        // Count total devices
        const totalDevices = await SystemInfo.countDocuments({ companyName });

        // Count active devices (seen in last 30 minutes)
        const activeDevices = await SystemInfo.countDocuments({
            companyName,
            lastSyncAt: { $gte: new Date(Date.now() - 30 * 60000) }
        });

        // Count total installed apps across all devices
        const totalAppsAcross = await InstalledApp.countDocuments({ companyName });

        // Get most common apps
        const commonApps = await InstalledApp.aggregate([
            { $match: { companyName } },
            { $group: { _id: '$name', count: { $sum: 1 } } },
            { $sort: { count: -1 } },
            { $limit: 10 }
        ]);

        // Count active users
        const activeUsers = await ActiveUser.countDocuments({
            companyName,
            isActive: true,
            lastSeenAt: { $gte: new Date(Date.now() - 5 * 60000) }
        });

        res.json({
            totalDevices,
            activeDevices,
            totalAppsAcross,
            activeUsers,
            mostCommonApps: commonApps,
            timestamp: new Date()
        });
    } catch (error) {
        console.error('Error fetching statistics:', error);
        res.status(500).json({ error: 'Failed to fetch statistics' });
    }
});

module.exports = router;
