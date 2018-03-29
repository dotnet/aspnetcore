// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/ws_client.h"
#include "signalrclient/signalr_client_config.h"
#include "websocket_client.h"

namespace signalr
{
    class default_websocket_client : public websocket_client
    {
    public:
        explicit default_websocket_client(const signalr_client_config& signalr_client_config = signalr_client_config{});

        pplx::task<void> connect(const web::uri &url) override;

        pplx::task<void> send(const utility::string_t &message) override;

        pplx::task<std::string> receive() override;

        pplx::task<void> close() override;

    private:
        web::websockets::client::websocket_client m_underlying_client;
    };
}