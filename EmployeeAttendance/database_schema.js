/**
 * MongoDB Database Schema Definitions
 * Place this in: backend/models/
 */

// models/SystemInfo.js
const mongoose = require('mongoose');

const systemInfoSchema = new mongoose.Schema({
    activationKey: { type: String, required: true, index: true },
    companyName: { type: String, required: true, index: true },
    computerName: { type: String, required: true, index: true },
    userName: String,

    // Operating System
    operatingSystem: {
        name: String,
        version: String,
        buildNumber: String,
        installDate: String,
        systemDrive: String,
        windowsDirectory: String,
        lastBootUpTime: String,
        serialNumber: String
    },

    // Processor
    processorInfo: {
        name: String,
        manufacturer: String,
        cores: String,
        logicalProcessors: String,
        maxClockSpeed: String,
        processerId: String
    },

    // Memory
    memoryInfo: {
        totalMemoryGB: Number,
        availableMemoryGB: String,
        memoryDevices: [{
            manufacturer: String,
            partNumber: String,
            serialNumber: String,
            capacityGB: String,
            speed: String
        }]
    },

    // Storage
    storageInfo: [{
        driveLetter: String,
        driveType: String,
        fileSystem: String,
        volumeName: String,
        totalSizeGB: String,
        freeSpaceGB: String,
        model: String,
        serialNumber: String,
        manufacturer: String,
        interfaceType: String
    }],

    // Network
    networkInfo: [{
        name: String,
        macAddress: String,
        manufacturer: String,
        netConnectionID: String,
        speed: String
    }],

    // Motherboard
    motherboardInfo: {
        manufacturer: String,
        product: String,
        serialNumber: String,
        version: String
    },

    // BIOS
    biosInfo: {
        manufacturer: String,
        version: String,
        releaseDate: String,
        serialNumber: String
    },

    // Display
    displayInfo: [{
        name: String,
        manufacturer: String,
        driverVersion: String,
        videoMemory: String
    }],

    timeZone: String,
    systemArchitecture: String,
    collectedAt: { type: Date, default: Date.now },
    lastSyncAt: { type: Date, default: Date.now },
    updatedAt: { type: Date, default: Date.now }
});

systemInfoSchema.index({ activationKey: 1, computerName: 1 });
systemInfoSchema.index({ companyName: 1, lastSyncAt: -1 });

module.exports = mongoose.model('SystemInfo', systemInfoSchema);


// models/InstalledApp.js

const installedAppSchema = new mongoose.Schema({
    activationKey: { type: String, required: true, index: true },
    computerName: { type: String, required: true, index: true },
    companyName: { type: String, index: true },
    userName: String,

    name: { type: String, required: true },
    version: String,
    publisher: String,
    installDate: String,
    uninstallString: String,
    installLocation: String,
    size: String,
    registryPath: String,
    hive: String, // x64 or x86

    lastSyncAt: { type: Date, default: Date.now },
    updatedAt: { type: Date, default: Date.now }
});

installedAppSchema.index({ activationKey: 1, computerName: 1, name: 1 });
installedAppSchema.index({ companyName: 1, name: 1 });
installedAppSchema.index({ name: 'text' });

module.exports = mongoose.model('InstalledApp', installedAppSchema);


// models/ControlCommand.js

const controlCommandSchema = new mongoose.Schema({
    activationKey: { type: String, required: true, index: true },
    computerName: { type: String, required: true, index: true },
    companyName: { type: String, index: true },

    type: { type: String, required: true }, // restart, shutdown, uninstall_app, block_application, etc.
    parameters: mongoose.Schema.Types.Mixed,

    createdAt: { type: Date, default: Date.now, index: true },
    executedAt: Date,
    executed: { type: Boolean, default: false, index: true },

    createdBy: String, // Admin user who created the command
    notes: String
});

controlCommandSchema.index({ activationKey: 1, computerName: 1, executed: 1 });
controlCommandSchema.index({ companyName: 1, createdAt: -1 });

module.exports = mongoose.model('ControlCommand', controlCommandSchema);


// models/CommandResult.js

const commandResultSchema = new mongoose.Schema({
    commandId: { type: String, required: true, index: true },
    activationKey: String,
    computerName: String,

    status: { type: String, required: true }, // success, error
    message: String,

    executedAt: { type: Date, default: Date.now, index: true }
});

commandResultSchema.index({ commandId: 1 });
commandResultSchema.index({ activationKey: 1, computerName: 1, executedAt: -1 });

module.exports = mongoose.model('CommandResult', commandResultSchema);


// models/ActiveUser.js

const activeUserSchema = new mongoose.Schema({
    activationKey: { type: String, required: true, index: true },
    computerName: { type: String, required: true, index: true },
    companyName: { type: String, index: true },
    userName: { type: String, required: true },

    isActive: { type: Boolean, default: true },
    lastSeenAt: { type: Date, default: Date.now, index: true },
    loginTime: Date,
    logoutTime: Date
});

activeUserSchema.index({ activationKey: 1, computerName: 1, userName: 1 });
activeUserSchema.index({ companyName: 1, isActive: 1, lastSeenAt: -1 });

module.exports = mongoose.model('ActiveUser', activeUserSchema);
