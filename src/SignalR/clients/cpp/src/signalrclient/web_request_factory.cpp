// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "web_request_factory.h"
#include "make_unique.h"

namespace signalr
{
    std::unique_ptr<web_request> web_request_factory::create_web_request(const web::uri &url)
    {
        return std::make_unique<web_request>(url);
    }

    web_request_factory::~web_request_factory()
    {}
}
