// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "default_websocket_client.h"

namespace signalr
{
    namespace
    {
        static web::websockets::client::websocket_client_config create_client_config(const signalr_client_config& signalr_client_config) noexcept
        {
            auto websocket_client_config = signalr_client_config.get_websocket_client_config();
            websocket_client_config.headers() = signalr_client_config.get_http_headers();

            return websocket_client_config;
        }
    }

    default_websocket_client::default_websocket_client(const signalr_client_config& signalr_client_config) noexcept
        : m_underlying_client(create_client_config(signalr_client_config))
    { }

    void default_websocket_client::start(std::string url, transfer_format, std::function<void(std::exception_ptr)> callback)
    {
        m_underlying_client.connect(utility::conversions::to_string_t(url))
            .then([callback](pplx::task<void> task)
            {
                try
                {
                    task.get();
                    callback(nullptr);
                }
                catch (...)
                {
                    callback(std::current_exception());
                }
            });
    }

    void default_websocket_client::stop(std::function<void(std::exception_ptr)> callback)
    {
        m_underlying_client.close()
            .then([callback](pplx::task<void> task)
            {
                try
                {
                    callback(nullptr);
                }
                catch (...)
                {
                    callback(std::current_exception());
                }
            });
    }

    void default_websocket_client::send(std::string payload, std::function<void(std::exception_ptr)> callback)
    {
        web::websockets::client::websocket_outgoing_message msg;
        msg.set_utf8_message(payload);
        m_underlying_client.send(msg)
            .then([callback](pplx::task<void> task)
            {
                try
                {
                    task.get();
                    callback(nullptr);
                }
                catch (...)
                {
                    callback(std::current_exception());
                }
            });
    }

    void default_websocket_client::receive(std::function<void(std::string, std::exception_ptr)> callback)
    {
        m_underlying_client.receive()
            .then([callback](pplx::task<web::websockets::client::websocket_incoming_message> task)
            {
                try
                {
                    auto response = task.get();
                    auto msg = response.extract_string().get();
                    callback(msg, nullptr);
                }
                catch (...)
                {
                    callback("", std::current_exception());
                }
            });
    }
}
