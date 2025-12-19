#!/bin/bash

# Verbose debug script showing full TLS handshake details
# Use this to debug certificate issues, cipher selection, etc.

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"

echo "=== OpenSSL Verbose Debug ==="
echo "Target: https://$HOST:$PORT/"
echo ""

# Use timeout instead of -ign_eof to avoid hanging if server doesn't close
# The response should complete within 5 seconds
timeout 20 bash -c "echo -e 'GET / HTTP/1.1\r\nHost: $HOST\r\nConnection: close\r\n\r\n' | \
    openssl s_client -connect '$HOST:$PORT' \
    -servername '$HOST' \
    -ign_eof \
    -state \
    -debug \
    -msg \
    2>&1" || echo "(Timed out - server may not have closed connection)"

echo ""
echo "=== Done ==="
