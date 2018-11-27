// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "web_request_factory.h"
#include "web_request_stub.h"

using namespace signalr;

class test_web_request_factory : public web_request_factory
{
public:
    explicit test_web_request_factory(std::function<std::unique_ptr<web_request>(const web::uri &url)> create_web_request_fn);

    virtual std::unique_ptr<web_request> create_web_request(const web::uri &url) override;

private:
    std::function<std::unique_ptr<web_request>(const web::uri &url)> m_create_web_request_fn;
};
