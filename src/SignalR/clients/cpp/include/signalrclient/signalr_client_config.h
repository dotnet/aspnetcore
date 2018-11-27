// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/http_client.h"
#include "cpprest/ws_client.h"
#include "_exports.h"

namespace signalr
{
    class signalr_client_config
    {
    public:
        SIGNALRCLIENT_API void __cdecl set_proxy(const web::web_proxy &proxy);
        // Please note that setting credentials does not work in all cases.
        // For example, Basic Authentication fails under Win32.
        // As a workaround, you can set the required authorization headers directly
        // using signalr_client_config::set_http_headers
        SIGNALRCLIENT_API void __cdecl set_credentials(const web::credentials &credentials);

        SIGNALRCLIENT_API web::http::client::http_client_config __cdecl get_http_client_config() const;
        SIGNALRCLIENT_API void __cdecl set_http_client_config(const web::http::client::http_client_config& http_client_config);

        SIGNALRCLIENT_API web::websockets::client::websocket_client_config __cdecl get_websocket_client_config() const;
        SIGNALRCLIENT_API void __cdecl set_websocket_client_config(const web::websockets::client::websocket_client_config& websocket_client_config);

        SIGNALRCLIENT_API web::http::http_headers __cdecl get_http_headers() const;
        SIGNALRCLIENT_API void __cdecl set_http_headers(const web::http::http_headers& http_headers);

    private:
        web::http::client::http_client_config m_http_client_config;
        web::websockets::client::websocket_client_config m_websocket_client_config;
        web::http::http_headers m_http_headers;
    };
}