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
                    builder.set_scheme(utility::conversions::to_string_t("wss"));
                }
                else
                {
                    builder.set_scheme(utility::conversions::to_string_t("ws"));
                }
            }

            return builder;
        }

        web::uri_builder build_uri(const std::string& base_url, const std::string& command, const std::string& query_string)
        {
            web::uri_builder builder(utility::conversions::to_string_t(base_url));
            builder.append_path(utility::conversions::to_string_t(command));
            return builder.append_query(utility::conversions::to_string_t(query_string));
        }

        web::uri_builder build_uri(const std::string& base_url, const std::string& command)
        {
            web::uri_builder builder(utility::conversions::to_string_t(base_url));
            return builder.append_path(utility::conversions::to_string_t(command));
        }

        std::string build_negotiate(const std::string& base_url)
        {
            return utility::conversions::to_utf8string(build_uri(base_url, "negotiate").to_string());
        }

        std::string build_connect(const std::string& base_url, transport_type transport, const std::string& query_string)
        {
            auto builder = build_uri(base_url, "", query_string);
            return utility::conversions::to_utf8string(convert_to_websocket_url(builder, transport).to_string());
        }

        std::string build_start(const std::string& base_url, const std::string &query_string)
        {
            return utility::conversions::to_utf8string(build_uri(base_url, "", query_string).to_string());
        }
    }
}
