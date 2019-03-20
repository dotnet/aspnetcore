// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "signalrclient/http_client.h"

using namespace signalr;

class test_http_client : public http_client
{
public:
    test_http_client(std::function<http_response(const std::string& url, http_request request)> create_http_response_fn);
    void send(std::string url, http_request request, std::function<void(http_response, std::exception_ptr)> callback) override;
private:
    std::function<http_response(const std::string& url, http_request request)> m_http_response;
};
