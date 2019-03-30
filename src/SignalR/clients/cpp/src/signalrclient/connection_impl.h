// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <atomic>
#include <mutex>
#include "signalrclient/http_client.h"
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
        static std::shared_ptr<connection_impl> create(const std::string& url, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer);

        static std::shared_ptr<connection_impl> create(const std::string& url, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer,
            std::unique_ptr<http_client> http_client, std::unique_ptr<transport_factory> transport_factory);

        connection_impl(const connection_impl&) = delete;

        connection_impl& operator=(const connection_impl&) = delete;

        ~connection_impl();

        void start(std::function<void(std::exception_ptr)> callback) noexcept;
        void send(const std::string &data, std::function<void(std::exception_ptr)> callback) noexcept;
        void stop(std::function<void(std::exception_ptr)> callback) noexcept;

        connection_state get_connection_state() const noexcept;
        std::string get_connection_id() const noexcept;

        void set_message_received(const std::function<void(const std::string&)>& message_received);
        void set_disconnected(const std::function<void()>& disconnected);
        void set_client_config(const signalr_client_config& config);

    private:
        std::string m_base_url;
        std::atomic<connection_state> m_connection_state;
        logger m_logger;
        std::shared_ptr<transport> m_transport;
        std::unique_ptr<transport_factory> m_transport_factory;

        std::function<void(const std::string&)> m_message_received;
        std::function<void()> m_disconnected;
        signalr_client_config m_signalr_client_config;

        pplx::cancellation_token_source m_disconnect_cts;
        std::mutex m_stop_lock;
        event m_start_completed_event;
        std::string m_connection_id;
        std::unique_ptr<http_client> m_http_client;

        connection_impl(const std::string& url, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer,
            std::unique_ptr<http_client> http_client, std::unique_ptr<transport_factory> transport_factory);

        pplx::task<std::shared_ptr<transport>> start_transport(const std::string& url);
        pplx::task<void> send_connect_request(const std::shared_ptr<transport>& transport,
            const std::string& url, const pplx::task_completion_event<void>& connect_request_tce);
        pplx::task<void> start_negotiate(const std::string& url, int redirect_count);

        void process_response(const std::string& response);

        pplx::task<void> shutdown();

        bool change_state(connection_state old_state, connection_state new_state);
        connection_state change_state(connection_state new_state);
        void handle_connection_state_change(connection_state old_state, connection_state new_state);
        void invoke_message_received(const std::string& message);

        static std::string translate_connection_state(connection_state state);
        void ensure_disconnected(const std::string& error_message) const;
    };
}
