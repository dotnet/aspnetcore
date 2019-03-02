// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/http_client.h"
#include "signalrclient/transport_type.h"

namespace signalr
{
    namespace url_builder
    {
        utility::string_t build_negotiate(const utility::string_t& base_url);
        utility::string_t build_connect(const utility::string_t& base_url, transport_type transport, const utility::string_t& query_string);
        utility::string_t build_start(const utility::string_t& base_url, const utility::string_t& query_string);
    }
}
