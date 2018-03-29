// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "_exports.h"
#include <memory>
#include <functional>
#include "pplx/pplxtasks.h"
#include "cpprest/json.h"
#include "connection_state.h"
#include "trace_level.h"
#include "log_writer.h"
#include "signalr_client_config.h"

namespace signalr
{
    class hub_connection_impl;

    class hub_connection
    {
    public:
        typedef std::function<void __cdecl (const web::json::value&)> method_invoked_handler;

        SIGNALRCLIENT_API explicit hub_connection(const utility::string_t& url, const utility::string_t& query_string = _XPLATSTR(""),
            trace_level trace_level = trace_level::all, std::shared_ptr<log_writer> log_writer = nullptr, bool use_default_url = true);

        SIGNALRCLIENT_API ~hub_connection();

        hub_connection(const hub_connection&) = delete;

        hub_connection& operator=(const hub_connection&) = delete;

        SIGNALRCLIENT_API pplx::task<void> __cdecl start();
        SIGNALRCLIENT_API pplx::task<void> __cdecl stop();

        SIGNALRCLIENT_API connection_state __cdecl get_connection_state() const;
        SIGNALRCLIENT_API utility::string_t __cdecl get_connection_id() const;

        SIGNALRCLIENT_API void __cdecl set_reconnecting(const std::function<void __cdecl()>& reconnecting_callback);
        SIGNALRCLIENT_API void __cdecl set_reconnected(const std::function<void __cdecl()>& reconnected_callback);
        SIGNALRCLIENT_API void __cdecl set_disconnected(const std::function<void __cdecl()>& disconnected_callback);

        SIGNALRCLIENT_API void __cdecl set_client_config(const signalr_client_config& config);

        SIGNALRCLIENT_API void __cdecl on(const utility::string_t& event_name, const method_invoked_handler& handler);

        pplx::task<web::json::value> invoke(const utility::string_t& method_name, const web::json::value& arguments = web::json::value::array())
        {
            return invoke_json(method_name, arguments);
        }

        pplx::task<void> send(const utility::string_t& method_name, const web::json::value& arguments = web::json::value::array())
        {
            return invoke_void(method_name, arguments);
        }

    private:
        std::shared_ptr<hub_connection_impl> m_pImpl;

        SIGNALRCLIENT_API pplx::task<web::json::value> __cdecl invoke_json(const utility::string_t& method_name, const web::json::value& arguments);
        SIGNALRCLIENT_API pplx::task<void> __cdecl invoke_void(const utility::string_t& method_name, const web::json::value& arguments);
    };
}