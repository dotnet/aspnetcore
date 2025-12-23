#!/bin/bash

# Verbose debug script showing full TLS handshake details
# Use this to debug certificate issues, cipher selection, etc.

HOST="${HOST:-localhost}"
PORT="${PORT:-5001}"
# Force TLS version: tls1, tls1_1, tls1_2, tls1_3 (default: let OpenSSL negotiate)
TLS_VERSION="${TLS_VERSION:-}"

echo "=== OpenSSL Verbose Debug ==="
echo "Target: https://$HOST:$PORT/"
if [ -n "$TLS_VERSION" ]; then
    echo "Forcing TLS version: $TLS_VERSION"
fi
echo ""

# Build TLS version flag if specified
TLS_FLAG=""
if [ -n "$TLS_VERSION" ]; then
    TLS_FLAG="-$TLS_VERSION"
fi

# -ign_eof: Keep connection open after stdin EOF to receive response
# -alpn http/1.1 ensures compatibility with both DirectSsl and default Kestrel (SslStream)
echo -e "GET / HTTP/1.1\r\nHost: $HOST\r\nConnection: close\r\n\r\n" | \
    openssl s_client -connect "$HOST:$PORT" \
    -servername "$HOST" \
    -alpn http/1.1 \
    $TLS_FLAG \
    -state \
    -debug \
    -msg \
    -ign_eof \
    2>&1

echo ""
echo "=== Done ==="
