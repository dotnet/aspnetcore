local mode = "stable"
local sequence = 0

init = function(args)
    mode = args[1] or "stable"
    assert(mode == "stable" or mode == "changing" or
        mode == "duplicate-known" or mode == "duplicate-unknown" or mode == "budget")
end

request = function()
    sequence = sequence + 1
    local suffix = "000000000000"
    if mode == "changing" then
        suffix = string.format("%012d", sequence)
    end

    local extra = ""
    if mode == "duplicate-known" then
        extra = "Accept: application/second\r\n"
    elseif mode == "duplicate-unknown" then
        extra = "x-example: second-example\r\n"
    elseif mode == "budget" then
        extra = "X-Budget: " .. string.rep("b", 4000) .. "\r\n"
    end

    return "GET / HTTP/1.1\r\nHost: " .. wrk.host ..
        "\r\nAccept: application/poc-" .. suffix ..
        "\r\nAccept-Language: en-x-poc-" .. suffix ..
        "\r\nUser-Agent: Mozilla/5.0 changing-user-agent-" .. suffix ..
        "\r\nX-Example: example-value-" .. suffix ..
        "\r\nX-Request-Nonce: nonce-value-" .. suffix .. "\r\n" .. extra .. "\r\n"
end
