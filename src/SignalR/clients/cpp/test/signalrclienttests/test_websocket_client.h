// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <functional>
#include "signalrclient/websocket_client.h"

using namespace signalr;

class test_websocket_client : public websocket_client
{
public:
    test_websocket_client();

    void start(std::string url, transfer_format format, std::function<void(std::exception_ptr)> callback) override;

    void stop(std::function<void(std::exception_ptr)> callback) override;

    void send(std::string payload, std::function<void(std::exception_ptr)> callback) override;

    void receive(std::function<void(std::string, std::exception_ptr)> callback) override;

    void set_connect_function(std::function<void(const std::string&, std::function<void(std::exception_ptr)>)> connect_function);

    void set_send_function(std::function<void(const std::string& msg, std::function<void(std::exception_ptr)>)> send_function);

    void set_receive_function(std::function<void(std::function<void(std::string, std::exception_ptr)>)> receive_function);

    void set_close_function(std::function<void(std::function<void(std::exception_ptr)>)> close_function);

private:
    std::function<void(const std::string&, std::function<void(std::exception_ptr)>)> m_connect_function;

    std::function<void(const std::string&, std::function<void(std::exception_ptr)>)> m_send_function;

    std::function<void(std::function<void(std::string, std::exception_ptr)>)> m_receive_function;

    std::function<void(std::function<void(std::exception_ptr)>)> m_close_function;
};
