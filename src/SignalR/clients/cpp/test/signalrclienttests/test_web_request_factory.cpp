// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_web_request_factory.h"

using namespace signalr;

test_web_request_factory::test_web_request_factory(std::function<std::unique_ptr<web_request>(const web::uri &url)> create_web_request_fn)
    : m_create_web_request_fn(create_web_request_fn)
{ }

std::unique_ptr<web_request> test_web_request_factory::create_web_request(const web::uri &url)
{
    return m_create_web_request_fn(url);
}
