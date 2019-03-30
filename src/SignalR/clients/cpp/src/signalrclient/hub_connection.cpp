// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient/hub_connection.h"
#include "hub_connection_impl.h"
#include "signalrclient/signalr_exception.h"

namespace signalr
{
    hub_connection::hub_connection(const std::string& url,
        trace_level trace_level, std::shared_ptr<log_writer> log_writer)
        : m_pImpl(hub_connection_impl::create(url, trace_level, std::move(log_writer)))
    {}

    // Do NOT remove this destructor. Letting the compiler generate and inline the default dtor may lead to
    // undefinded behavior since we are using an incomplete type. More details here:  http://herbsutter.com/gotw/_100/
    hub_connection::~hub_connection() = default;

    void hub_connection::start(std::function<void(std::exception_ptr)> callback) noexcept
    {
        if (!m_pImpl)
        {
            callback(std::make_exception_ptr(signalr_exception("start() cannot be called on destructed hub_connection instance")));
        }

        m_pImpl->start(callback);
    }

    void hub_connection::stop(std::function<void(std::exception_ptr)> callback) noexcept
    {
        if (!m_pImpl)
        {
            callback(std::make_exception_ptr(signalr_exception("stop() cannot be called on destructed hub_connection instance")));
        }

        m_pImpl->stop(callback);
    }

    void hub_connection::on(const std::string& event_name, const method_invoked_handler& handler)
    {
        if (!m_pImpl)
        {
            throw signalr_exception("on() cannot be called on destructed hub_connection instance");
        }

        return m_pImpl->on(event_name, handler);
    }

    void hub_connection::invoke(const std::string& method_name, const web::json::value& arguments, std::function<void(const web::json::value&, std::exception_ptr)> callback) noexcept
    {
        if (!m_pImpl)
        {
            callback(web::json::value(), std::make_exception_ptr(signalr_exception("invoke() cannot be called on destructed hub_connection instance")));
            return;
        }

        return m_pImpl->invoke(method_name, arguments, callback);
    }

    void hub_connection::send(const std::string& method_name, const web::json::value& arguments, std::function<void(std::exception_ptr)> callback) noexcept
    {
        if (!m_pImpl)
        {
            callback(std::make_exception_ptr(signalr_exception("send() cannot be called on destructed hub_connection instance")));
            return;
        }

        m_pImpl->send(method_name, arguments, callback);
    }

    connection_state hub_connection::get_connection_state() const
    {
        if (!m_pImpl)
        {
            throw signalr_exception("get_connection_state() cannot be called on destructed hub_connection instance");
        }

        return m_pImpl->get_connection_state();
    }

    std::string hub_connection::get_connection_id() const
    {
        if (!m_pImpl)
        {
            throw signalr_exception("get_connection_id() cannot be called on destructed hub_connection instance");
        }

        return m_pImpl->get_connection_id();
    }

    void hub_connection::set_disconnected(const std::function<void()>& disconnected_callback)
    {
        if (!m_pImpl)
        {
            throw signalr_exception("set_disconnected() cannot be called on destructed hub_connection instance");
        }

        m_pImpl->set_disconnected(disconnected_callback);
    }

    void hub_connection::set_client_config(const signalr_client_config& config)
    {
        if (!m_pImpl)
        {
            throw signalr_exception("set_client_config() cannot be called on destructed hub_connection instance");
        }

        m_pImpl->set_client_config(config);
    }
}
