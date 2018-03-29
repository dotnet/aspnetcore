// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/http_client.h"
#include "signalrclient/transport_type.h"

namespace signalr
{
    namespace url_builder
    {
        web::uri build_negotiate(const web::uri& base_url, const utility::string_t& query_string);
        web::uri build_connect(const web::uri& base_url, transport_type transport, const utility::string_t& query_string);
        web::uri build_reconnect(const web::uri& base_url, transport_type transport, const utility::string_t& last_message_id,
            const utility::string_t& groups_token, const utility::string_t& query_string);
        web::uri build_start(const web::uri& base_url, const utility::string_t& query_string);
        web::uri build_abort(const web::uri &base_url, transport_type transport,
            const utility::string_t& connection_data, const utility::string_t& query_string);
    }
}
