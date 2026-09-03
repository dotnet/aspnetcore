# DirectTlsTransportApp

Experimental **DirectTls** Kestrel transport — native fd-bound OpenSSL TLS. **Linux/WSL only**.

## Run

```bash
cd src/Servers/Kestrel/samples/DirectTlsTransportApp
./scripts/generate-certs.sh   # once: creates gitignored server-p256/p384.*
dotnet run                    # after `source activate.sh` from repo root
```

`USE_STANDARD_TLS=1 dotnet run` swaps in standard SslStream for comparison.

Two HTTPS endpoints (HTTP/1.1 + HTTP/2):

| Port | Purpose |
|------|---------|
| 5001 | SNI cert selection + ClientHello callback |
| 5002 | Single cert, no callbacks (perf) |

```bash
./scripts/curl-request.sh                # curl -v -k [https://localhost:5001/](https://localhost:5001/)
./scripts/openssl-request.sh             # raw TLS view; HOST/PORT env-overridable

./scripts/wrk-load.sh 5002 10 64 500     # port duration(s) threads connections; new handshake per request
```
