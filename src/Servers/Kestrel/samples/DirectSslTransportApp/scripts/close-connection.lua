-- wrk lua script that forces Connection: close header
-- This causes a new TLS handshake per request

wrk.method = "GET"
wrk.headers["Connection"] = "close"

done = function(summary, latency, requests)
    print("")
    print("=== TLS Handshake Performance Summary ===")
    print(string.format("  Total requests:     %d", summary.requests))
    print(string.format("  Total errors:       %d", summary.errors.connect + summary.errors.read + summary.errors.write + summary.errors.status + summary.errors.timeout))
    print(string.format("  Requests/sec:       %.2f (= TLS handshakes/sec)", summary.requests / (summary.duration / 1000000)))
    print(string.format("  Avg latency:        %.2f ms", latency.mean / 1000))
    print(string.format("  Max latency:        %.2f ms", latency.max / 1000))
    print(string.format("  99th percentile:    %.2f ms", latency:percentile(99) / 1000))
end
