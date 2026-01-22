const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 8888;

const server = http.createServer((req, res) => {
    console.log('[ REQUEST ]', req.method, req.url);
    
    // Serve index.html for root
    let filePath = req.url === '/' ? 'index.html' : req.url.substring(1);
    filePath = path.join(__dirname, filePath);
    
    fs.readFile(filePath, (err, data) => {
        if (err) {
            res.writeHead(404, { 'Content-Type': 'text/plain' });
            res.end('404 Not Found');
            return;
        }
        
        const ext = path.extname(filePath);
        const contentTypes = {
            '.html': 'text/html',
            '.js': 'application/javascript',
            '.css': 'text/css',
            '.json': 'application/json'
        };
        
        res.writeHead(200, { 'Content-Type': contentTypes[ext] || 'text/plain' });
        res.end(data);
    });
});

server.listen(PORT, () => {
    console.log('Static server running on port', PORT);
});

process.on('uncaughtException', (err) => {
    console.error('[UNCAUGHT]', err.message);
});
