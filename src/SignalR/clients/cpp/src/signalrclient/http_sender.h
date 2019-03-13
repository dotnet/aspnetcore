// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"
#include "signalrclient/signalr_client_config.h"
#include "web_request_factory.h"

namespace signalr
{
    namespace http_sender
    {
        pplx::task<std::string> get(web_request_factory& request_factory, const std::string& url,
            const signalr_client_config& client_config = signalr_client_config{});

        pplx::task<std::string> post(web_request_factory& request_factory, const std::string& url,
            const signalr_client_config& client_config = signalr_client_config{});
    }
}
