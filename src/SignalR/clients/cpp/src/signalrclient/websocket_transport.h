// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/ws_client.h"
#include "url_builder.h"
#include "transport.h"
#include "logger.h"
#include "default_websocket_client.h"
#include "connection_impl.h"

namespace signalr
{
    class websocket_transport : public transport, public std::enable_shared_from_this<websocket_transport>
    {
    public:
        static std::shared_ptr<transport> create(const std::function<std::shared_ptr<websocket_client>()>& websocket_client_factory,
            const logger& logger);

        ~websocket_transport();

        websocket_transport(const websocket_transport&) = delete;

        websocket_transport& operator=(const websocket_transport&) = delete;

        transport_type get_transport_type() const noexcept override;

        void start(const std::string& url, /*format,*/ std::function<void(std::exception_ptr)> callback) override;
        void stop(/*format,*/ std::function<void(std::exception_ptr)> callback) override;
        void on_close(std::function<void(std::exception_ptr)> callback) override;

        void send(std::string payload, std::function<void(std::exception_ptr)> callback) override;

    private:
        websocket_transport(const std::function<std::shared_ptr<websocket_client>()>& websocket_client_factory,
            const logger& logger);

        std::function<std::shared_ptr<websocket_client>()> m_websocket_client_factory;
        std::shared_ptr<websocket_client> m_websocket_client;
        std::mutex m_websocket_client_lock;
        std::mutex m_start_stop_lock;

        pplx::cancellation_token_source m_receive_loop_cts;

        void receive_loop(pplx::cancellation_token_source cts);

        std::shared_ptr<websocket_client> safe_get_websocket_client();
    };
}
