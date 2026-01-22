const http = require('http');

const server = http.createServer((req, res) => {
    console.log('[REQUEST]', req.url);
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('Hello');
});

server.listen(8888, '127.0.0.1', () => {
    console.log('Server on localhost:8888');
});
