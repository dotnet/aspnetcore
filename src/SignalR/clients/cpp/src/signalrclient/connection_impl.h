// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <atomic>
#include <mutex>
#include "cpprest/http_client.h"
#include "signalrclient/trace_level.h"
#include "signalrclient/connection_state.h"
#include "signalrclient/signalr_client_config.h"
#include "web_request_factory.h"
#include "transport_factory.h"
#include "logger.h"
#include "negotiation_response.h"
#include "event.h"

namespace signalr
{
    // Note:
    // Factory methods and private constructors prevent from using this class incorrectly. Because this class
    // derives from `std::enable_shared_from_this` the instance has to be owned by a `std::shared_ptr` whenever
    // a member method calls `std::shared_from_this()` otherwise the behavior is undefined. Therefore constructors
    // are private to disallow creating instances directly and factory methods return `std::shared_ptr<connection_impl>`.
    class connection_impl : public std::enable_shared_from_this<connection_impl>
    {
    public:
        static std::shared_ptr<connection_impl> create(const utility::string_t& url, const utility::string_t& query_string,
            trace_level trace_level, const std::shared_ptr<log_writer>& log_writer);

        static std::shared_ptr<connection_impl> create(const utility::string_t& url, const utility::string_t& query_string, trace_level trace_level,
            const std::shared_ptr<log_writer>& log_writer, std::unique_ptr<web_request_factory> web_request_factory, std::unique_ptr<transport_factory> transport_factory);

        connection_impl(const connection_impl&) = delete;

        connection_impl& operator=(const connection_impl&) = delete;

        ~connection_impl();

        pplx::task<void> start();
        pplx::task<void> send(const utility::string_t &data);
        pplx::task<void> stop();

        connection_state get_connection_state() const;
        utility::string_t get_connection_id() const;

        void set_message_received_string(const std::function<void(const utility::string_t&)>& message_received);
        void set_message_received_json(const std::function<void(const web::json::value&)>& message_received);
        void set_reconnecting(const std::function<void()>& reconnecting);
        void set_reconnected(const std::function<void()>& reconnected);
        void set_disconnected(const std::function<void()>& disconnected);
        void set_client_config(const signalr_client_config& config);
        void set_reconnect_delay(const int reconnect_delay /*milliseconds*/);

        void set_connection_data(const utility::string_t& connection_data);

    private:
        web::uri m_base_url;
        utility::string_t m_query_string;
        std::atomic<connection_state> m_connection_state;
        logger m_logger;
        std::shared_ptr<transport> m_transport;
        std::unique_ptr<web_request_factory> m_web_request_factory;
        std::unique_ptr<transport_factory> m_transport_factory;

        std::function<void(const web::json::value&)> m_message_received;
        std::function<void()> m_reconnecting;
        std::function<void()> m_reconnected;
        std::function<void()> m_disconnected;
        signalr_client_config m_signalr_client_config;

        pplx::cancellation_token_source m_disconnect_cts;
        std::mutex m_stop_lock;
        event m_start_completed_event;
        utility::string_t m_connection_id;
        utility::string_t m_connection_data;
        int m_reconnect_window; // in milliseconds
        int m_reconnect_delay; // in milliseconds
        utility::string_t m_message_id;
        utility::string_t m_groups_token;
        bool m_handshakeReceived;

        connection_impl(const utility::string_t& url, const utility::string_t& query_string, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer,
            std::unique_ptr<web_request_factory> web_request_factory, std::unique_ptr<transport_factory> transport_factory);

        pplx::task<std::shared_ptr<transport>> start_transport(negotiation_response negotiation_response);
        pplx::task<void> send_connect_request(const std::shared_ptr<transport>& transport,
            const pplx::task_completion_event<void>& connect_request_tce);

        void process_response(const utility::string_t& response, const pplx::task_completion_event<void>& connect_request_tce);

        pplx::task<void> shutdown();
        void reconnect();
        pplx::task<bool> try_reconnect(const web::uri& reconnect_url, const utility::datetime::interval_type reconnect_start_time,
            int reconnect_window, int reconnect_delay, pplx::cancellation_token_source disconnect_cts);

        bool change_state(connection_state old_state, connection_state new_state);
        connection_state change_state(connection_state new_state);
        void handle_connection_state_change(connection_state old_state, connection_state new_state);
        void invoke_message_received(const web::json::value& message);

        static utility::string_t translate_connection_state(connection_state state);
        void ensure_disconnected(const utility::string_t& error_message);
    };
}
