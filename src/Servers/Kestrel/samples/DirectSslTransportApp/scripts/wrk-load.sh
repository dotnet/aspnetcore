#!/bin/bash

# Load test script for DirectSslTransportApp
# Forces new TLS handshake per request with "Connection: close"

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"
DURATION="${DURATION:-10s}"
THREADS="${THREADS:-4}"
CONNECTIONS="${CONNECTIONS:-100}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "=== DirectSsl TLS Handshake Load Test ==="
echo "Target: https://$HOST:$PORT/"
echo "Duration: $DURATION"
echo "Threads: $THREADS"
echo "Connections: $CONNECTIONS"
echo "Note: Connection: close forces new TLS handshake per request"
echo ""

wrk -t"$THREADS" -c"$CONNECTIONS" -d"$DURATION" \
    -s "$SCRIPT_DIR/close-connection.lua" \
    "https://$HOST:$PORT/"
