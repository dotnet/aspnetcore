// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "transport.h"
#include "connection_impl.h"

namespace signalr
{
    transport::transport(const logger& logger)
        : m_logger(logger)
    {}

    // Do NOT remove this destructor. Letting the compiler generate and inline the default dtor may lead to
    // undefinded behavior since we are using an incomplete type. More details here:  http://herbsutter.com/gotw/_100/
    transport::~transport()
    { }

    void transport::on_receive(std::function<void(std::string, std::exception_ptr)> callback)
    {
        m_process_response_callback = callback;;
    }

    void transport::process_response(std::string message)
    {
        m_process_response_callback(message, nullptr);
    }

    void transport::process_response(std::exception_ptr exception)
    {
        m_process_response_callback("", exception);
    }
}
