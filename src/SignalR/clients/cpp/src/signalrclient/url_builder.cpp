// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "cpprest/http_client.h"
#include "signalrclient/transport_type.h"

namespace signalr
{
    namespace url_builder
    {
        web::uri_builder &convert_to_websocket_url(web::uri_builder &builder, transport_type transport)
        {
            if (transport == transport_type::websockets)
            {
                if (builder.scheme() == _XPLATSTR("https"))
                {
                    builder.set_scheme(utility::string_t(_XPLATSTR("wss")));
                }
                else
                {
                    builder.set_scheme(utility::string_t(_XPLATSTR("ws")));
                }
            }

            return builder;
        }

        web::uri_builder build_uri(const web::uri& base_url, const utility::string_t& command, const utility::string_t& query_string)
        {
            web::uri_builder builder(base_url);
            builder.append_path(command);
            return builder.append_query(query_string);
        }

        web::uri_builder build_uri(const web::uri& base_url, const utility::string_t& command)
        {
            web::uri_builder builder(base_url);
            return builder.append_path(command);
        }

        utility::string_t build_negotiate(const utility::string_t& base_url)
        {
            return build_uri(base_url, _XPLATSTR("negotiate")).to_string();
        }

        utility::string_t build_connect(const utility::string_t& base_url, transport_type transport, const utility::string_t& query_string)
        {
            auto builder = build_uri(base_url, _XPLATSTR(""), query_string);
            return convert_to_websocket_url(builder, transport).to_string();
        }

        utility::string_t build_start(const utility::string_t& base_url, const utility::string_t &query_string)
        {
            return build_uri(base_url, _XPLATSTR(""), query_string).to_string();
        }
    }
}
