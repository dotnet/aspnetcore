#!/bin/bash

# Verbose debug script showing full TLS handshake details
# Use this to debug certificate issues, cipher selection, etc.

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"

echo "=== OpenSSL Verbose Debug ==="
echo "Target: https://$HOST:$PORT/"
echo ""

# Use -ign_eof to wait for server response before closing
# sleep gives server time to respond
(echo -e "GET / HTTP/1.1\r\nHost: $HOST\r\nConnection: close\r\n\r\n"; sleep 2) | \
    openssl s_client -connect "$HOST:$PORT" \
    -servername "$HOST" \
    -ign_eof \
    -state \
    -debug \
    -msg \
    2>&1

echo ""
echo "=== Done ==="
