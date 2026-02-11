#!/bin/bash
# Generate self-signed SSL certificate for HTTPS

# Create ssl directory if it doesn't exist
mkdir -p ssl
cd ssl

# Generate private key and self-signed certificate (valid for 365 days)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout server.key \
    -out server.crt \
    -subj "/C=IN/ST=State/L=City/O=WIZONE/OU=IT/CN=103.122.85.118"

echo ""
echo "SSL certificates generated successfully!"
echo "  Key: ssl/server.key"
echo "  Cert: ssl/server.crt"
echo ""
echo "Now restart the server:"
echo "  pm2 restart all"
echo ""
echo "Access via HTTPS:"
echo "  https://103.122.85.118:8443"
echo ""
echo "Note: Browser will show security warning for self-signed cert."
echo "Click 'Advanced' -> 'Proceed to site' to continue."
