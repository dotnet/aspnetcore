// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient/connection.h"
#include "signalrclient/transport_type.h"
#include "connection_impl.h"

namespace signalr
{
    connection::connection(const std::string& url, trace_level trace_level, std::shared_ptr<log_writer> log_writer)
        : m_pImpl(connection_impl::create(url, trace_level, std::move(log_writer)))
    {}

    // Do NOT remove this destructor. Letting the compiler generate and inline the default dtor may lead to
    // undefinded behavior since we are using an incomplete type. More details here:  http://herbsutter.com/gotw/_100/
    connection::~connection() = default;

    void connection::start(std::function<void(std::exception_ptr)> callback) noexcept
    {
        m_pImpl->start(callback);
    }

    void connection::send(const std::string& data, std::function<void(std::exception_ptr)> callback) noexcept
    {
        m_pImpl->send(data, callback);
    }

    void connection::set_message_received(const message_received_handler& message_received_callback)
    {
        m_pImpl->set_message_received(message_received_callback);
    }

    void connection::set_disconnected(const std::function<void()>& disconnected_callback)
    {
        m_pImpl->set_disconnected(disconnected_callback);
    }

    void connection::set_client_config(const signalr_client_config& config)
    {
        m_pImpl->set_client_config(config);
    }

    void connection::stop(std::function<void(std::exception_ptr)> callback) noexcept
    {
        m_pImpl->stop(callback);
    }

    connection_state connection::get_connection_state() const noexcept
    {
        return m_pImpl->get_connection_state();
    }

    std::string connection::get_connection_id() const
    {
        return m_pImpl->get_connection_id();
    }
}
