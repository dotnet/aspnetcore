// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"
#include "cpprest/details/basic_types.h"
#include "signalrclient/signalr_client_config.h"
#include "web_request_factory.h"

namespace signalr
{
    namespace http_sender
    {
        pplx::task<utility::string_t> get(web_request_factory& request_factory, const web::uri& url,
            const signalr_client_config& client_config = signalr_client_config{});

        pplx::task<utility::string_t> post(web_request_factory& request_factory, const web::uri& url,
            const signalr_client_config& client_config = signalr_client_config{});
    }
}