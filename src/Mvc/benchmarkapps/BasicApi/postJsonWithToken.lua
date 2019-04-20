-- script that retrieves an authentication token to send in all future requests and adds a body for those requests
-- keep this file and getWithToken.lua in sync with respect to token handling

-- do not use wrk's default request
local req = nil

-- use token for at most maxRequests, default throughout test
local counter = 0
local maxRequests = -1

-- request access necessary for both reading and writing by default
local username = "writer@example.com"

-- marker that we have completed the first request
local token = nil

function init(args)
    if args[1] ~= nil then
        maxRequests = args[1]
        print("Max requests: " .. maxRequests)
    end
    if args[2] ~= nil then
        username = args[2]
    end

    local path = "/token?username=" .. username

    -- initialize first (empty) request
    req = wrk.format("GET", path, nil, "")
end

function request()
    return req
end

function response(status, headers, body)
    if not token and status == 200 then
        token = body
        wrk.headers["Authorization"] = "Bearer " .. token
        wrk.headers["Content-Type"] = "application/json"
        wrk.method = "POST"
        wrk.body = [[
{
  "category": {
    "name": "Cats"
  },
  "images": [
    {
        "url": "http://example.com/images/fluffy1.png"
    },
    {
        "url": "http://example.com/images/fluffy2.png"
    },
  ],
  "tags": [
    {
        "name": "orange"
    },
    {
        "name": "kitty"
    }
  ],
  "age": 2,
  "hasVaccinations": "true",
  "name": "fluffy",
  "status": "available"
}]]

        req = wrk.format()
        return
    end

    if not token then
        print("Failed initial request! status: " .. status)
        wrk.thread:stop()
    end

    if counter == maxRequests then
        wrk.thread:stop()
    end

    counter = counter + 1
end
