/**
 * Chat API Endpoints
 * Node.js/Express Backend Chat Controller
 * Handles real-time messaging between desktop EXE and web dashboard
 */

const express = require('express');
const router = express.Router();
const { MongoClient, ObjectId } = require('mongodb');

// Database connection details
const MONGO_URL = process.env.MONGODB_URL || 'mongodb://72.61.170.243:9095/controller_application';
let mongoClient;
let db;

// Initialize MongoDB connection
async function initializeDB() {
    if (!mongoClient) {
        mongoClient = new MongoClient(MONGO_URL);
        await mongoClient.connect();
        db = mongoClient.db('controller_application');
        
        // Create indexes for efficient querying
        const messagesCol = db.collection('chat_messages');
        await messagesCol.createIndex({ device_id: 1, timestamp: -1 });
        await messagesCol.createIndex({ timestamp: -1 });
    }
    return db;
}

// Initialize DB on startup
initializeDB().catch(console.error);

// ==================== CHAT MESSAGE ENDPOINTS ====================

/**
 * POST /api/chat/send
 * Send a message from desktop app to web dashboard or vice versa
 */
router.post('/send', async (req, res) => {
    try {
        await initializeDB();
        
        const {
            device_id,
            sender,
            message,
            timestamp,
            is_from_desktop,
            recipient_id = null,
            conversation_id = null
        } = req.body;

        // Validate required fields
        if (!device_id || !sender || !message) {
            return res.status(400).json({
                success: false,
                message: 'Missing required fields: device_id, sender, message'
            });
        }

        const chatMessage = {
            _id: new ObjectId(),
            device_id,
            sender,
            message,
            timestamp: timestamp ? new Date(timestamp) : new Date(),
            is_from_desktop: is_from_desktop || false,
            recipient_id: recipient_id || null,
            conversation_id: conversation_id || device_id,
            read: false,
            created_at: new Date(),
            updated_at: new Date()
        };

        const messagesCol = db.collection('chat_messages');
        const result = await messagesCol.insertOne(chatMessage);

        console.log(`[Chat] Message saved: ${chatMessage._id} from ${sender}`);

        res.json({
            success: true,
            message: 'Message sent successfully',
            data: {
                id: result.insertedId,
                ...chatMessage
            }
        });
    } catch (error) {
        console.error('[Chat] Error sending message:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to send message',
            error: error.message
        });
    }
});

/**
 * GET /api/chat/get
 * Retrieve messages for a device
 * Query params: device_id, conversation_id (optional), limit (default 50), offset (default 0)
 */
router.get('/get', async (req, res) => {
    try {
        await initializeDB();
        
        const {
            device_id,
            conversation_id = null,
            limit = 50,
            offset = 0
        } = req.query;

        if (!device_id) {
            return res.status(400).json({
                success: false,
                message: 'Missing required parameter: device_id'
            });
        }

        const messagesCol = db.collection('chat_messages');
        
        // Build query
        const query = {
            $or: [
                { device_id: device_id },
                { recipient_id: device_id }
            ]
        };

        if (conversation_id) {
            query.conversation_id = conversation_id;
        }

        // Fetch messages
        const messages = await messagesCol
            .find(query)
            .sort({ timestamp: -1 })
            .limit(parseInt(limit))
            .skip(parseInt(offset))
            .toArray();

        // Reverse to show chronological order
        messages.reverse();

        console.log(`[Chat] Retrieved ${messages.length} messages for device: ${device_id}`);

        res.json({
            success: true,
            message: 'Messages retrieved successfully',
            data: messages,
            count: messages.length
        });
    } catch (error) {
        console.error('[Chat] Error retrieving messages:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to retrieve messages',
            error: error.message
        });
    }
});

/**
 * GET /api/chat/conversations
 * Get list of all conversations for a device
 */
router.get('/conversations', async (req, res) => {
    try {
        await initializeDB();
        
        const { device_id } = req.query;

        if (!device_id) {
            return res.status(400).json({
                success: false,
                message: 'Missing required parameter: device_id'
            });
        }

        const messagesCol = db.collection('chat_messages');
        
        // Group by conversation_id and get last message
        const conversations = await messagesCol.aggregate([
            {
                $match: {
                    $or: [
                        { device_id: device_id },
                        { recipient_id: device_id }
                    ]
                }
            },
            {
                $sort: { timestamp: -1 }
            },
            {
                $group: {
                    _id: '$conversation_id',
                    last_message: { $first: '$message' },
                    last_sender: { $first: '$sender' },
                    last_timestamp: { $first: '$timestamp' },
                    message_count: { $sum: 1 },
                    unread_count: {
                        $sum: {
                            $cond: [{ $eq: ['$read', false] }, 1, 0]
                        }
                    }
                }
            },
            {
                $sort: { last_timestamp: -1 }
            }
        ]).toArray();

        console.log(`[Chat] Retrieved ${conversations.length} conversations for device: ${device_id}`);

        res.json({
            success: true,
            message: 'Conversations retrieved successfully',
            data: conversations,
            count: conversations.length
        });
    } catch (error) {
        console.error('[Chat] Error retrieving conversations:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to retrieve conversations',
            error: error.message
        });
    }
});

/**
 * PUT /api/chat/mark-read
 * Mark messages as read
 */
router.put('/mark-read', async (req, res) => {
    try {
        await initializeDB();
        
        const { message_ids, device_id } = req.body;

        if (!message_ids || !Array.isArray(message_ids) || message_ids.length === 0) {
            return res.status(400).json({
                success: false,
                message: 'Invalid message_ids provided'
            });
        }

        const messagesCol = db.collection('chat_messages');
        
        const objectIds = message_ids.map(id => new ObjectId(id));
        
        const result = await messagesCol.updateMany(
            {
                _id: { $in: objectIds }
            },
            {
                $set: {
                    read: true,
                    updated_at: new Date()
                }
            }
        );

        console.log(`[Chat] Marked ${result.modifiedCount} messages as read`);

        res.json({
            success: true,
            message: 'Messages marked as read',
            modified_count: result.modifiedCount
        });
    } catch (error) {
        console.error('[Chat] Error marking messages as read:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to mark messages as read',
            error: error.message
        });
    }
});

/**
 * DELETE /api/chat/delete/:message_id
 * Delete a specific message
 */
router.delete('/delete/:message_id', async (req, res) => {
    try {
        await initializeDB();
        
        const { message_id } = req.params;

        const messagesCol = db.collection('chat_messages');
        
        const result = await messagesCol.deleteOne({
            _id: new ObjectId(message_id)
        });

        if (result.deletedCount === 0) {
            return res.status(404).json({
                success: false,
                message: 'Message not found'
            });
        }

        console.log(`[Chat] Deleted message: ${message_id}`);

        res.json({
            success: true,
            message: 'Message deleted successfully'
        });
    } catch (error) {
        console.error('[Chat] Error deleting message:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to delete message',
            error: error.message
        });
    }
});

/**
 * GET /api/chat/stats
 * Get chat statistics
 */
router.get('/stats', async (req, res) => {
    try {
        await initializeDB();
        
        const messagesCol = db.collection('chat_messages');

        const stats = await messagesCol.aggregate([
            {
                $group: {
                    _id: null,
                    total_messages: { $sum: 1 },
                    unread_messages: {
                        $sum: {
                            $cond: [{ $eq: ['$read', false] }, 1, 0]
                        }
                    },
                    desktop_messages: {
                        $sum: {
                            $cond: [{ $eq: ['$is_from_desktop', true] }, 1, 0]
                        }
                    },
                    web_messages: {
                        $sum: {
                            $cond: [{ $eq: ['$is_from_desktop', false] }, 1, 0]
                        }
                    },
                    unique_devices: {
                        $addToSet: '$device_id'
                    }
                }
            },
            {
                $project: {
                    _id: 0,
                    total_messages: 1,
                    unread_messages: 1,
                    desktop_messages: 1,
                    web_messages: 1,
                    unique_device_count: { $size: '$unique_devices' }
                }
            }
        ]).toArray();

        console.log('[Chat] Statistics retrieved');

        res.json({
            success: true,
            message: 'Chat statistics retrieved successfully',
            data: stats[0] || {
                total_messages: 0,
                unread_messages: 0,
                desktop_messages: 0,
                web_messages: 0,
                unique_device_count: 0
            }
        });
    } catch (error) {
        console.error('[Chat] Error retrieving statistics:', error);
        res.status(500).json({
            success: false,
            message: 'Failed to retrieve statistics',
            error: error.message
        });
    }
});

module.exports = router;
