// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_websocket_client.h"
#include "pplx/pplxtasks.h"

test_websocket_client::test_websocket_client()
    : m_connect_function([](const std::string&, std::function<void(std::exception_ptr)> callback) { callback(nullptr); }),
    m_send_function([](const std::string msg, std::function<void(std::exception_ptr)> callback) { callback(nullptr); }),
    m_receive_function([](std::function<void(std::string, std::exception_ptr)> callback) { callback("", nullptr); }),
    m_close_function([](std::function<void(std::exception_ptr)> callback) { callback(nullptr); })
{ }

void test_websocket_client::start(std::string url, transfer_format, std::function<void(std::exception_ptr)> callback)
{
    pplx::create_task([url, callback, this]()
        {
            m_connect_function(url, callback);
        });
}

void test_websocket_client::stop(std::function<void(std::exception_ptr)> callback)
{
    pplx::create_task([callback, this]()
        {
            m_close_function(callback);
        });
}

void test_websocket_client::send(std::string payload, std::function<void(std::exception_ptr)> callback)
{
    pplx::create_task([payload, callback, this]()
        {
            m_send_function(payload, callback);
        });
}

void test_websocket_client::receive(std::function<void(std::string, std::exception_ptr)> callback)
{
    pplx::create_task([callback, this]()
        {
            m_receive_function(callback);
        });
}

void test_websocket_client::set_connect_function(std::function<void(const std::string&, std::function<void(std::exception_ptr)>)> connect_function)
{
    m_connect_function = connect_function;
}

void test_websocket_client::set_send_function(std::function<void(const std::string& msg, std::function<void(std::exception_ptr)>)> send_function)
{
    m_send_function = send_function;
}

void test_websocket_client::set_receive_function(std::function<void(std::function<void(std::string, std::exception_ptr)>)> receive_function)
{
    m_receive_function = receive_function;
}

void test_websocket_client::set_close_function(std::function<void(std::function<void(std::exception_ptr)>)> close_function)
{
    m_close_function = close_function;
}
