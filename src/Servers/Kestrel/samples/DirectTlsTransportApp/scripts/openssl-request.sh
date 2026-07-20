#!/bin/bash

# Simple script to send a single HTTPS request using openssl s_client
# Shows brief TLS info + HTTP response

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"

echo "=== OpenSSL Request ==="
echo "Target: https://$HOST:$PORT/"
echo ""

# -ign_eof: Wait for server response before closing
# -brief: Show brief connection info
(echo -e "GET / HTTP/1.1\r\nHost: $HOST\r\nConnection: close\r\n\r\n"; sleep 1) | \
    openssl s_client -connect "$HOST:$PORT" \
    -servername "$HOST" \
    -ign_eof \
    -brief \
    2>&1

echo ""
echo "=== Done ==="
