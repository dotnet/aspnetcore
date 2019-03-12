// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_websocket_client.h"

test_websocket_client::test_websocket_client()
    : m_connect_function([](const std::string&){ return pplx::task_from_result(); }),
    m_send_function ([](const std::string msg){ return pplx::task_from_result(); }),
    m_receive_function([](){ return pplx::task_from_result<std::string>(""); }),
    m_close_function([](){ return pplx::task_from_result(); })

{ }

pplx::task<void> test_websocket_client::connect(const std::string& url)
{
    return m_connect_function(url);
}

pplx::task<void> test_websocket_client::send(const std::string &msg)
{
    return m_send_function(msg);
}

pplx::task<std::string> test_websocket_client::receive()
{
    return pplx::create_task([this]() { return m_receive_function(); });
}

pplx::task<void> test_websocket_client::close()
{
    return m_close_function();
}

void test_websocket_client::set_connect_function(std::function<pplx::task<void>(const std::string& url)> connect_function)
{
    m_connect_function = connect_function;
}

void test_websocket_client::set_send_function(std::function<pplx::task<void>(const std::string& msg)> send_function)
{
    m_send_function = send_function;
}

void test_websocket_client::set_receive_function(std::function<pplx::task<std::string>()> receive_function)
{
    m_receive_function = receive_function;
}

void test_websocket_client::set_close_function(std::function<pplx::task<void>()> close_function)
{
    m_close_function = close_function;
}
