#!/bin/bash
#
# Generates the self-signed test certificates the sample expects, into the project
# directory (one level up from this script). Nothing here is committed - the sample's
# .gitignore excludes *.crt/*.key/*.pfx, so run this once before launching the app.
#
# Produced files (CN=localhost, SAN: localhost / 127.0.0.1 / ::1, valid 365 days):
#   server-p256.crt / .key / .pfx   (ECDSA P-256)
#   server-p384.crt / .key / .pfx   (ECDSA P-384)
#
# Program.cs uses the .crt + .key pairs for the DirectTls endpoints (per-SNI selection
# picks P-384 for host "p384.example", P-256 otherwise) and server-p256.pfx for the
# standard SslStream path (dotnet run -- --standard-tls).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CERT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PASSWORD="testpassword"
SUBJECT="/C=US/ST=Test/L=Test/O=Test/CN=localhost"
SAN="subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1"

cd "$CERT_DIR"

# generate_cert <name> <openssl-curve>
generate_cert() {
    local name="$1"
    local curve="$2"

    echo "=== Generating ${name} (${curve}) ==="

    # Private key on the requested curve.
    openssl ecparam -name "$curve" -genkey -noout -out "${name}.key"

    # Self-signed certificate with SAN.
    openssl req -new -x509 -key "${name}.key" -out "${name}.crt" -days 365 \
        -subj "$SUBJECT" -addext "$SAN"

    # PKCS#12 bundle for Kestrel's default UseHttps path.
    openssl pkcs12 -export -out "${name}.pfx" \
        -inkey "${name}.key" -in "${name}.crt" -passout "pass:${PASSWORD}"
}

generate_cert "server-p256" "prime256v1"
generate_cert "server-p384" "secp384r1"

echo ""
echo "Generated in ${CERT_DIR}:"
echo "  server-p256.crt / .key / .pfx   (ECDSA P-256)"
echo "  server-p384.crt / .key / .pfx   (ECDSA P-384)"
echo ""
echo "DirectTls transport: uses .crt + .key"
echo "Standard SslStream:  uses server-p256.pfx (password '${PASSWORD}')"
