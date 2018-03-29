// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <functional>
#include "websocket_client.h"

using namespace signalr;

class test_websocket_client : public websocket_client
{
public:
    test_websocket_client();

    pplx::task<void> connect(const web::uri &url) override;

    pplx::task<void> send(const utility::string_t& msg) override;

    pplx::task<std::string> receive() override;

    pplx::task<void> close() override;

    void set_connect_function(std::function<pplx::task<void>(const web::uri &url)> connect_function);

    void set_send_function(std::function<pplx::task<void>(const utility::string_t& msg)> send_function);

    void set_receive_function(std::function<pplx::task<std::string>()> receive_function);

    void set_close_function(std::function<pplx::task<void>()> close_function);

private:
    std::function<pplx::task<void>(const web::uri &url)> m_connect_function;

    std::function<pplx::task<void>(const utility::string_t&)> m_send_function;

    std::function<pplx::task<std::string>()> m_receive_function;

    std::function<pplx::task<void>()> m_close_function;
};