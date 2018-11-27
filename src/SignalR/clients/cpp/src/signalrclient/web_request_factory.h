// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/base_uri.h"
#include "web_request.h"

namespace signalr
{
    class web_request_factory
    {
    public:
        virtual std::unique_ptr<web_request> create_web_request(const web::uri &url);

        virtual ~web_request_factory();
    };
}