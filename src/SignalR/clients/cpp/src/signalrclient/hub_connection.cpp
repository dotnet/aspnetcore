// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient/hub_connection.h"
#include "hub_connection_impl.h"
#include "signalrclient/signalr_exception.h"

namespace signalr
{
    hub_connection::hub_connection(const utility::string_t& url, const utility::string_t& query_string,
        trace_level trace_level, std::shared_ptr<log_writer> log_writer, bool use_default_url)
        : m_pImpl(hub_connection_impl::create(url, query_string, trace_level, std::move(log_writer), use_default_url))
    {}

    // Do NOT remove this destructor. Letting the compiler generate and inline the default dtor may lead to
    // undefinded behavior since we are using an incomplete type. More details here:  http://herbsutter.com/gotw/_100/
    hub_connection::~hub_connection() = default;

    pplx::task<void> hub_connection::start()
    {
        return m_pImpl->start();
    }

    pplx::task<void> hub_connection::stop()
    {
        return m_pImpl->stop();
    }

    void hub_connection::on(const utility::string_t& event_name, const method_invoked_handler& handler)
    {
        if (!m_pImpl)
        {
            throw signalr_exception(_XPLATSTR("on() cannot be called on uninitialized hub_connection instance"));
        }

        return m_pImpl->on(event_name, handler);
    }

    pplx::task<web::json::value> hub_connection::invoke_json(const utility::string_t& method_name, const web::json::value& arguments)
    {
        if (!m_pImpl)
        {
            throw signalr_exception(_XPLATSTR("invoke() cannot be called on uninitialized hub_proxy instance"));
        }

        return m_pImpl->invoke_json(method_name, arguments);
    }

    pplx::task<void> hub_connection::invoke_void(const utility::string_t& method_name, const web::json::value& arguments)
    {
        if (!m_pImpl)
        {
            throw signalr_exception(_XPLATSTR("invoke() cannot be called on uninitialized hub_proxy instance"));
        }

        return m_pImpl->invoke_void(method_name, arguments);
    }

    connection_state hub_connection::get_connection_state() const
    {
        return m_pImpl->get_connection_state();
    }

    utility::string_t hub_connection::get_connection_id() const
    {
        return m_pImpl->get_connection_id();
    }

    void hub_connection::set_reconnecting(const std::function<void()>& reconnecting_callback)
    {
        m_pImpl->set_reconnecting(reconnecting_callback);
    }

    void hub_connection::set_reconnected(const std::function<void()>& reconnected_callback)
    {
        m_pImpl->set_reconnected(reconnected_callback);
    }

    void hub_connection::set_disconnected(const std::function<void()>& disconnected_callback)
    {
        m_pImpl->set_disconnected(disconnected_callback);
    }

    void hub_connection::set_client_config(const signalr_client_config& config)
    {
        m_pImpl->set_client_config(config);
    }
}