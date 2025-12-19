#!/bin/bash

# Generate self-signed P-384 certificate for testing
# Output: server-p384.crt, server-p384.key, server-p384.pfx

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CERT_DIR="$SCRIPT_DIR/.."
PASSWORD="testpassword"

cd "$CERT_DIR"

echo "=== Generating P-384 Certificate ==="

# Generate private key (P-384 curve)
openssl ecparam -name secp384r1 -genkey -noout -out server-p384.key

# Generate self-signed certificate
openssl req -new -x509 -key server-p384.key -out server-p384.crt -days 365 \
    -subj "/C=US/ST=Test/L=Test/O=Test/CN=localhost" \
    -addext "subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1"

# Generate PFX (for Kestrel's default UseHttps with X509Certificate2)
openssl pkcs12 -export -out server-p384.pfx \
    -inkey server-p384.key -in server-p384.crt \
    -password pass:$PASSWORD

echo ""
echo "Generated:"
echo "  - server-p384.crt  (certificate)"
echo "  - server-p384.key  (private key)"  
echo "  - server-p384.pfx  (PKCS#12 bundle, password: $PASSWORD)"
echo ""
echo "For DirectSsl transport: use .crt + .key"
echo "For default Kestrel:     use .pfx with password '$PASSWORD'"
