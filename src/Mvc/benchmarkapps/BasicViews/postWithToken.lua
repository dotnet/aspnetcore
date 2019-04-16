-- script that retrieves an antiforgery token to send in all future requests and adds a body for those requests

-- do not use wrk's default request
local req = nil

-- use token for at most maxRequests, default throughout test
local counter = 0
local maxRequests = -1

-- marker that we have completed the first request
local token = nil

function init(args)
    -- initialize first (empty) request
    req = wrk.format("GET")
end

function request()
    return req
end

function response(status, headers, body)
    if not token and status == 200 then
        local cookie = string.gsub(headers["Set-Cookie"], "^([^;]*)(;.*)?$", "%1")
        if not cookie or cookie == "" then
            print("Unable to find antiforgery cookie in initial response!")
            wrk.thread:stop()
        end

        token = string.gsub(body, '^.* name="__RequestVerificationToken".* value="([^"]*)"[ >].*$', "%1")
        if not token or token == "" then
            print("Unable to find antiforgery token in initial response!")
            wrk.thread:stop()
        end

        wrk.body = "Age=12&BirthDate=2006-03-01T09%3A51%3A43.041-07%3A00&Name=George&__RequestVerificationToken=" .. token
        wrk.headers["Content-Type"] = "application/x-www-form-urlencoded"
        wrk.headers["Cookie"] = cookie
        wrk.method = "POST"

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
