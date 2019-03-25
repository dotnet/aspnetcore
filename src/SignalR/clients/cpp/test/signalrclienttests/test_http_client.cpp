// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_http_client.h"

test_http_client::test_http_client(std::function<http_response(const std::string& url, http_request request)> create_http_response_fn)
    : m_http_response(create_http_response_fn)
{
}

void test_http_client::send(std::string url, http_request request, std::function<void(http_response, std::exception_ptr)> callback)
{
    http_response response;
    std::exception_ptr exception;
    try
    {
        response = m_http_response(url, request);
    }
    catch (...)
    {
        exception = std::current_exception();
    }

    callback(std::move(response), exception);
}
