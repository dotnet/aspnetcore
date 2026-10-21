local mode = "stable"
local sequence = 0

init = function(args)
    mode = args[1] or "stable"
    assert(mode == "stable" or mode == "changing" or mode == "duplicate")
end

request = function()
    sequence = sequence + 1
    local value = "Mozilla/5.0 changing-user-agent-A"
    if mode == "changing" and sequence % 2 == 0 then
        value = "Mozilla/5.0 changing-user-agent-B"
    end

    local extra = ""
    if mode == "duplicate" then
        extra = "User-Agent: second-agent\r\n"
    end

    return "GET / HTTP/1.1\r\nHost: " .. wrk.host ..
        "\r\nUser-Agent: " .. value .. "\r\n" .. extra ..
        "X-Example: unknown-value\r\nAccept: text/plain\r\n\r\n"
end
