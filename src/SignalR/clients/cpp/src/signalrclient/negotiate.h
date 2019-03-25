// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "signalrclient/signalr_client_config.h"
#include "signalrclient/transport_type.h"
#include "web_request_factory.h"
#include "negotiation_response.h"
#include "signalrclient/http_client.h"

namespace signalr
{
    namespace negotiate
    {
        pplx::task<negotiation_response> negotiate(http_client& client, const std::string& base_url,
            const signalr_client_config& signalr_client_config = signalr::signalr_client_config{});
    }
}
