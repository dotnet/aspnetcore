#!/bin/bash

# Verbose debug script showing full TLS handshake details
# Use this to debug certificate issues, cipher selection, etc.

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"

echo "=== OpenSSL Verbose Debug ==="
echo "Target: https://$HOST:$PORT/"
echo ""

echo "GET / HTTP/1.1
Host: $HOST
Connection: close

" | openssl s_client -connect "$HOST:$PORT" \
    -servername "$HOST" \
    -state \
    -debug \
    -msg \
    2>&1

echo ""
echo "=== Done ==="
