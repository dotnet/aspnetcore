// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/http_client.h"
#include "signalrclient/transport_type.h"

namespace signalr
{
    namespace url_builder
    {
        std::string build_negotiate(const std::string& base_url);
        std::string build_connect(const std::string& base_url, transport_type transport, const std::string& query_string);
        std::string build_start(const std::string& base_url, const std::string& query_string);
    }
}
