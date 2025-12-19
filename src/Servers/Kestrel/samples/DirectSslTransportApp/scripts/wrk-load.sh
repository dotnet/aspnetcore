#!/bin/bash

# Load test script for DirectSslTransportApp
# Forces new TLS handshake per request with "Connection: close"

PORT=${1:-5001}
DURATION=${2:-10}
THREADS=${3:-64}
CONNECTIONS=${4:-500}

echo "=== TLS Handshake Benchmark ==="
echo "Target: https://localhost:$PORT/"
echo "Duration: ${DURATION}s, Threads: $THREADS, Connections: $CONNECTIONS"
echo "Mode: Connection: close (new handshake per request)"
echo ""

wrk -t$THREADS -c$CONNECTIONS -d${DURATION}s \
    -s close-connection.lua \
    https://localhost:$PORT/ \
    --latency

echo ""
echo "Usage: $0 [port] [duration] [threads] [connections]"
echo "Example: $0 5001 10 4 500"