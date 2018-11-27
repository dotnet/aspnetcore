// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/base_uri.h"
#include "signalrclient/signalr_client_config.h"
#include "signalrclient/transport_type.h"
#include "web_request_factory.h"
#include "negotiation_response.h"


namespace signalr
{
    namespace request_sender
    {
        pplx::task<negotiation_response> negotiate(web_request_factory& request_factory, const web::uri &base_url,
            const utility::string_t& query_string, const signalr_client_config& signalr_client_config = signalr::signalr_client_config{});
    }
}