#!/bin/bash

# Verbose debug script showing full TLS handshake details
# Use this to debug certificate issues, cipher selection, etc.

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"

echo "=== OpenSSL Verbose Debug ==="
echo "Target: https://$HOST:$PORT/"
echo ""

# sleep keeps stdin open long enough to receive the response
# -alpn http/1.1 ensures compatibility with both DirectSsl and default Kestrel (SslStream)
(echo -e "GET / HTTP/1.1\r\nHost: $HOST\r\nConnection: close\r\n\r\n"; sleep 2) | \
    openssl s_client -connect "$HOST:$PORT" \
    -servername "$HOST" \
    -alpn http/1.1 \
    -state \
    -debug \
    -msg \
    2>&1

echo ""
echo "=== Done ==="
