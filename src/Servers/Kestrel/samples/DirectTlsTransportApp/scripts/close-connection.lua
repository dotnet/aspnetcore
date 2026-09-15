-- wrk lua script that forces Connection: close header
-- This causes a new TLS handshake per request

request = function()
    return wrk.format("GET", "/", {["Connection"] = "close"})
end