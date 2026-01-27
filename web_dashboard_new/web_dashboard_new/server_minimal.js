const http = require('http');

const server = http.createServer((req, res) => {
    console.log('Request received:', req.url);
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('Hello');
});

server.listen(8888, '0.0.0.0', () => {
    console.log('Server started on port 8888');
});
