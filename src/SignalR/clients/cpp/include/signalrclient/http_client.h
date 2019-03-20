// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <string>
#include <functional>
#include <map>
#include <chrono>

namespace signalr
{
    enum class http_method
    {
        GET,
        POST
    };

    class http_request
    {
    public:
        http_method method;
        std::map<std::string, std::string> headers;
        std::string content;
        std::chrono::seconds timeout;
    };

    class http_response
    {
    public:
        http_response() {}
        http_response(http_response&& rhs) noexcept : status_code(rhs.status_code), content(std::move(rhs.content)) {}
        http_response(int code, const std::string& content) : status_code(code), content(content) {}

        http_response& operator=(http_response&& rhs)
        {
            status_code = rhs.status_code;
            content = std::move(rhs.content);

            return *this;
        }

        int status_code = 0;
        std::string content;
    };

    class http_client
    {
    public:
        virtual void send(std::string url, http_request request, std::function<void(http_response, std::exception_ptr)> callback) = 0;

        virtual ~http_client() {}
    };
}
