// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "constants.h"
#include "cpprest/http_client.h"
#include "signalrclient/transport_type.h"

namespace signalr
{
    namespace url_builder
    {
        utility::string_t get_transport_name(transport_type transport)
        {
            _ASSERTE(transport == transport_type::websockets || transport == transport_type::long_polling);

            return transport == transport_type::websockets
                ? _XPLATSTR("webSockets")
                : _XPLATSTR("longPolling");
        }

        void append_transport(web::uri_builder &builder, transport_type transport)
        {
            if (transport > static_cast<transport_type>(-1))
            {
                builder.append_query(_XPLATSTR("transport"), get_transport_name(transport));
            }
        }

        void append_connection_token(web::uri_builder &builder, const utility::string_t &connection_token)
        {
            if (connection_token.length() > 0)
            {
                builder.append_query(_XPLATSTR("connectionToken"), connection_token, /* do_encoding */ true);
            }
        }

        void append_connection_data(web::uri_builder& builder, const utility::string_t& connection_data)
        {
            if (connection_data.length() > 0)
            {
                builder.append_query(_XPLATSTR("connectionData"), connection_data, /* do_encoding */ true);
            }
        }

        void append_message_id(web::uri_builder& builder, const utility::string_t& message_id)
        {
            if (message_id.length() > 0)
            {
                builder.append_query(_XPLATSTR("messageId"), message_id, /* do_encoding */ true);
            }
        }

        void append_groups_token(web::uri_builder& builder, const utility::string_t& groups_token)
        {
            if (groups_token.length() > 0)
            {
                builder.append_query(_XPLATSTR("groupsToken"), groups_token, /* do_encoding */ true);
            }
        }

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

        web::uri_builder build_uri(const web::uri& base_url, const utility::string_t& command, transport_type transport,
            const utility::string_t& connection_data, const utility::string_t& query_string,
            const utility::string_t& last_message_id = _XPLATSTR(""), const utility::string_t& groups_token = _XPLATSTR(""))
        {
            _ASSERTE(command == _XPLATSTR("reconnect") || (last_message_id.length() == 0 && groups_token.length() == 0));

            web::uri_builder builder(base_url);
            builder.append_path(command);
            append_transport(builder, transport);
            builder.append_query(_XPLATSTR("clientProtocol"), PROTOCOL);
            //append_connection_token(builder, connection_token);
            append_connection_data(builder, connection_data);
            append_message_id(builder, last_message_id);
            append_groups_token(builder, groups_token);
            return builder.append_query(query_string);
        }

        web::uri_builder build_uri(const web::uri& base_url, const utility::string_t& command, const utility::string_t& query_string)
        {
            web::uri_builder builder(base_url);
            builder.append_path(command);
            return builder.append_query(query_string);
        }

        web::uri build_negotiate(const web::uri& base_url, const utility::string_t& query_string)
        {
            return build_uri(base_url, _XPLATSTR("negotiate"), query_string).to_uri();
        }

        web::uri build_connect(const web::uri& base_url, transport_type transport, const utility::string_t& query_string)
        {
            auto builder = build_uri(base_url, _XPLATSTR(""), query_string);
            return convert_to_websocket_url(builder, transport).to_uri();
            //auto builder = build_uri(base_url, _XPLATSTR("connect"), transport, connection_data, query_string);
            //return convert_to_websocket_url(builder, transport).to_uri();
        }

        web::uri build_reconnect(const web::uri& base_url, transport_type transport, const utility::string_t& last_message_id, const utility::string_t& groups_token,
            const utility::string_t& query_string)
        {
            auto builder = build_uri(base_url, _XPLATSTR("reconnect"), transport, query_string, last_message_id, groups_token);
            return convert_to_websocket_url(builder, transport).to_uri();
        }

        web::uri build_start(const web::uri &base_url, const utility::string_t &query_string)
        {
            return build_uri(base_url, _XPLATSTR(""), query_string).to_uri();
        }

        web::uri build_abort(const web::uri &base_url, transport_type transport,
            const utility::string_t& connection_data, const utility::string_t &query_string)
        {
            return build_uri(base_url, _XPLATSTR("abort"), transport, connection_data, query_string).to_uri();
        }
    }
}