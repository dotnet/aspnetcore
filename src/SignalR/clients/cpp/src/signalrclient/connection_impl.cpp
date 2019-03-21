// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include <thread>
#include <algorithm>
#include "constants.h"
#include "connection_impl.h"
#include "negotiate.h"
#include "url_builder.h"
#include "trace_log_writer.h"
#include "make_unique.h"
#include "signalrclient/signalr_exception.h"
#include "default_http_client.h"

namespace signalr
{
    // unnamed namespace makes it invisble outside this translation unit
    namespace
    {
        // this is a workaround for a compiler bug where mutable lambdas won't sometimes compile
        static void log(const logger& logger, trace_level level, const std::string& entry);
    }

    std::shared_ptr<connection_impl> connection_impl::create(const std::string& url, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer)
    {
        return connection_impl::create(url, trace_level, log_writer, nullptr, std::make_unique<transport_factory>());
    }

    std::shared_ptr<connection_impl> connection_impl::create(const std::string& url, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer,
        std::unique_ptr<http_client> http_client, std::unique_ptr<transport_factory> transport_factory)
    {
        return std::shared_ptr<connection_impl>(new connection_impl(url, trace_level,
            log_writer ? log_writer : std::make_shared<trace_log_writer>(), std::move(http_client), std::move(transport_factory)));
    }

    connection_impl::connection_impl(const std::string& url, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer,
        std::unique_ptr<http_client> http_client, std::unique_ptr<transport_factory> transport_factory)
        : m_base_url(url), m_connection_state(connection_state::disconnected), m_logger(log_writer, trace_level), m_transport(nullptr),
        m_transport_factory(std::move(transport_factory)), m_message_received([](const std::string&) noexcept {}), m_disconnected([]() noexcept {})
    {
        if (http_client != nullptr)
        {
            m_http_client = std::move(http_client);
        }
        else
        {
            m_http_client = std::unique_ptr<class http_client>(new default_http_client());
        }
    }

    connection_impl::~connection_impl()
    {
        try
        {
            // Signaling the event is safe here. We are in the dtor so noone is using this instance. There might be some
            // outstanding threads that hold on to the connection via a weak pointer but they won't be able to acquire
            // the instance since it is being destroyed. Note that the event may actually be in non-signaled state here.
            m_start_completed_event.set();
            shutdown().get();
        }
        catch (const pplx::task_canceled&)
        {
            // because we are in the dtor and the `connection_imp` is ref counted we should not get the `task_canceled`
            // exception because it would indicate that some other thread/task still holds reference to this instance
            // so how come we are in the dtor?
            _ASSERTE(false);
            return;
        }
        catch (...) // must not throw from destructors
        { }

        m_transport = nullptr;
        change_state(connection_state::disconnected);
    }

    void connection_impl::start(std::function<void(std::exception_ptr)> callback) noexcept
    {
        {
            std::lock_guard<std::mutex> lock(m_stop_lock);
            if (!change_state(connection_state::disconnected, connection_state::connecting))
            {
                callback(std::make_exception_ptr(signalr_exception("cannot start a connection that is not in the disconnected state")));
                return;
            }

            // there should not be any active transport at this point
            _ASSERTE(!m_transport);

            m_disconnect_cts = pplx::cancellation_token_source();
            m_start_completed_event.reset();
            m_connection_id = "";
        }

        start_negotiate(m_base_url, 0)
            .then([callback](pplx::task<void> prev_task)
        {
            try
            {
                prev_task.get();
                callback(nullptr);
            }
            catch (...)
            {
                callback(std::current_exception());
            }
        });
    }

    pplx::task<void> connection_impl::start_negotiate(const std::string& url, int redirect_count)
    {
        if (redirect_count >= MAX_NEGOTIATE_REDIRECTS)
        {
            return pplx::task_from_exception<void>(signalr_exception("Negotiate redirection limit exceeded."));
        }

        pplx::task_completion_event<void> start_tce;

        std::weak_ptr<connection_impl> weak_connection = shared_from_this();

        pplx::task_from_result()
            .then([weak_connection, url]()
        {
            auto connection = weak_connection.lock();
            if (!connection)
            {
                return pplx::task_from_exception<negotiation_response>("connection no longer exists");
            }
            return negotiate::negotiate(*connection->m_http_client, url, connection->m_signalr_client_config);
        }, m_disconnect_cts.get_token())
            .then([weak_connection, start_tce, redirect_count, url](negotiation_response negotiation_response)
        {
            auto connection = weak_connection.lock();
            if (!connection)
            {
                return pplx::task_from_exception<void>("connection no longer exists");
            }

            if (!negotiation_response.error.empty())
            {
                return pplx::task_from_exception<void>(signalr_exception(negotiation_response.error));
            }

            if (!negotiation_response.url.empty())
            {
                if (!negotiation_response.accessToken.empty())
                {
                    auto headers = connection->m_signalr_client_config.get_http_headers();
                    headers[_XPLATSTR("Authorization")] = utility::conversions::to_string_t("Bearer " + negotiation_response.accessToken);
                    connection->m_signalr_client_config.set_http_headers(headers);
                }
                return connection->start_negotiate(negotiation_response.url, redirect_count + 1);
            }

            connection->m_connection_id = std::move(negotiation_response.connectionId);

            // TODO: fallback logic

            bool foundWebsockets = false;
            for (auto availableTransport : negotiation_response.availableTransports)
            {
                if (availableTransport.transport == "WebSockets")
                {
                    foundWebsockets = true;
                    break;
                }
            }

            if (!foundWebsockets)
            {
                return pplx::task_from_exception<void>(signalr_exception("The server does not support WebSockets which is currently the only transport supported by this client."));
            }

            // TODO: use transfer format

            return connection->start_transport(url)
                .then([weak_connection, start_tce](std::shared_ptr<transport> transport)
            {
                auto connection = weak_connection.lock();
                if (!connection)
                {
                    return pplx::task_from_exception<void>("connection no longer exists");
                }
                connection->m_transport = transport;

                if (!connection->change_state(connection_state::connecting, connection_state::connected))
                {
                    connection->m_logger.log(trace_level::errors,
                        std::string("internal error - transition from an unexpected state. expected state: connecting, actual state: ")
                        .append(translate_connection_state(connection->get_connection_state())));

                    _ASSERTE(false);
                }

                return pplx::task_from_result();
            });
        }, m_disconnect_cts.get_token())
            .then([start_tce, weak_connection](pplx::task<void> previous_task)
        {
            auto connection = weak_connection.lock();
            if (!connection)
            {
                return pplx::task_from_exception<void>(_XPLATSTR("connection no longer exists"));
            }
            try
            {
                previous_task.get();
                connection->m_start_completed_event.set();
                start_tce.set();
            }
            catch (const std::exception & e)
            {
                auto task_canceled_exception = dynamic_cast<const pplx::task_canceled*>(&e);
                if (task_canceled_exception)
                {
                    connection->m_logger.log(trace_level::info,
                        "starting the connection has been canceled.");
                }
                else
                {
                    connection->m_logger.log(trace_level::errors,
                        std::string("connection could not be started due to: ")
                        .append(e.what()));
                }

                connection->m_transport = nullptr;
                connection->change_state(connection_state::disconnected);
                connection->m_start_completed_event.set();
                start_tce.set_exception(std::current_exception());
            }

            return pplx::task_from_result();
        });

        return pplx::create_task(start_tce);
    }

    pplx::task<std::shared_ptr<transport>> connection_impl::start_transport(const std::string& url)
    {
        auto connection = shared_from_this();

        pplx::task_completion_event<void> connect_request_tce;

        auto weak_connection = std::weak_ptr<connection_impl>(connection);
        const auto& disconnect_cts = m_disconnect_cts;
        const auto& logger = m_logger;

        auto transport = connection->m_transport_factory->create_transport(
            transport_type::websockets, connection->m_logger, connection->m_signalr_client_config);

        transport->on_receive([disconnect_cts, connect_request_tce, logger, weak_connection](std::string message, std::exception_ptr exception)
            {
                if (exception != nullptr)
                {
                    try
                    {
                        // Rethrowing the exception so we can log it
                        std::rethrow_exception(exception);
                    }
                    catch (const std::exception & e)
                    {
                        // When a connection is stopped we don't wait for its transport to stop. As a result if the same connection
                        // is immediately re-started the old transport can still invoke this callback. To prevent this we capture
                        // the disconnect_cts by value which allows distinguishing if the error is for the running connection
                        // or for the one that was already stopped. If this is the latter we just ignore it.
                        if (disconnect_cts.get_token().is_canceled())
                        {
                            logger.log(trace_level::info,
                                std::string{ "ignoring stray error received after connection was restarted. error: " }
                            .append(e.what()));

                            return;
                        }

                        // no op after connection started successfully
                        connect_request_tce.set_exception(exception);
                    }
                }
                else
                {
                    if (disconnect_cts.get_token().is_canceled())
                    {
                        logger.log(trace_level::info,
                            std::string{ "ignoring stray message received after connection was restarted. message: " }
                        .append(message));
                        return;
                    }

                    auto connection = weak_connection.lock();
                    if (connection)
                    {
                        connection->process_response(message);
                    }
                }
            });

        pplx::create_task([connect_request_tce, disconnect_cts, weak_connection]()
        {
            // TODO? std::this_thread::sleep_for(std::chrono::milliseconds(negotiation_response.transport_connect_timeout));
            std::this_thread::sleep_for(std::chrono::milliseconds(5000));

            // if the disconnect_cts is canceled it means that the connection has been stopped or went out of scope in
            // which case we should not throw due to timeout. Instead we need to set the tce prevent the task that is
            // using this tce from hanging indifinitely. (This will eventually result in throwing the pplx::task_canceled
            // exception to the user since this is what we do in the start() function if disconnect_cts is tripped).
            if (disconnect_cts.get_token().is_canceled())
            {
                connect_request_tce.set();
            }
            else
            {
                connect_request_tce.set_exception(signalr_exception("transport timed out when trying to connect"));
            }
        });

        return connection->send_connect_request(transport, url, connect_request_tce)
            .then([transport](){ return pplx::task_from_result(transport); });
    }

    pplx::task<void> connection_impl::send_connect_request(const std::shared_ptr<transport>& transport, const std::string& url, const pplx::task_completion_event<void>& connect_request_tce)
    {
        auto logger = m_logger;
        auto query_string = "id=" + m_connection_id;
        auto connect_url = url_builder::build_connect(url, transport->get_transport_type(), query_string);

        transport->start(connect_url, transfer_format::text, [transport, connect_request_tce, logger](std::exception_ptr exception)
            mutable {
                try
                {
                    if (exception != nullptr)
                    {
                        std::rethrow_exception(exception);
                    }
                    connect_request_tce.set();
                }
                catch (const std::exception& e)
                {
                    logger.log(
                        trace_level::errors,
                        std::string("transport could not connect due to: ")
                            .append(e.what()));

                    connect_request_tce.set_exception(std::current_exception());
                }
            });

        return pplx::create_task(connect_request_tce);
    }

    void connection_impl::process_response(const std::string& response)
    {
        m_logger.log(trace_level::messages,
            std::string("processing message: ").append(response));

        invoke_message_received(response);
    }

    void connection_impl::invoke_message_received(const std::string& message)
    {
        try
        {
            m_message_received(message);
        }
        catch (const std::exception &e)
        {
            m_logger.log(
                trace_level::errors,
                std::string("message_received callback threw an exception: ")
                .append(e.what()));
        }
        catch (...)
        {
            m_logger.log(trace_level::errors, "message_received callback threw an unknown exception");
        }
    }

    void connection_impl::send(const std::string& data, std::function<void(std::exception_ptr)> callback) noexcept
    {
        // To prevent an (unlikely) condition where the transport is nulled out after we checked the connection_state
        // and before sending data we store the pointer in the local variable. In this case `send()` will throw but
        // we won't crash.
        auto transport = m_transport;

        const auto connection_state = get_connection_state();
        if (connection_state != signalr::connection_state::connected || !transport)
        {
            callback(std::make_exception_ptr(signalr_exception(
                std::string("cannot send data when the connection is not in the connected state. current connection state: ")
                    .append(translate_connection_state(connection_state)))));
            return;
        }

        auto logger = m_logger;

        logger.log(trace_level::info, std::string("sending data: ").append(data));

        transport->send(data, [logger, callback](std::exception_ptr exception)
            mutable {
                try
                {
                    if (exception != nullptr)
                    {
                        std::rethrow_exception(exception);
                    }
                    callback(nullptr);
                }
                catch (const std::exception &e)
                {
                    logger.log(
                        trace_level::errors,
                        std::string("error sending data: ")
                        .append(e.what()));

                    callback(exception);
                }
            });
    }

    void connection_impl::stop(std::function<void(std::exception_ptr)> callback) noexcept
    {
        m_logger.log(trace_level::info, "stopping connection");

        auto connection = shared_from_this();
        shutdown()
            .then([connection, callback](pplx::task<void> prev_task)
            {
                try
                {
                    prev_task.get();
                }
                catch (...)
                {
                    callback(std::current_exception());
                    return;
                }

                {
                    // the lock prevents a race where the user calls `stop` on a disconnected connection and calls `start`
                    // on a different thread at the same time. In this case we must not null out the transport if we are
                    // not in the `disconnecting` state to not affect the 'start' invocation.
                    std::lock_guard<std::mutex> lock(connection->m_stop_lock);
                    if (connection->change_state(connection_state::disconnecting, connection_state::disconnected))
                    {
                        // we do let the exception through (especially the task_canceled exception)
                        connection->m_transport = nullptr;
                    }
                }

                try
                {
                    connection->m_disconnected();
                }
                catch (const std::exception &e)
                {
                    connection->m_logger.log(
                        trace_level::errors,
                        std::string("disconnected callback threw an exception: ")
                        .append(e.what()));
                }
                catch (...)
                {
                    connection->m_logger.log(
                        trace_level::errors,
                        std::string("disconnected callback threw an unknown exception"));
                }

                callback(nullptr);
            });
    }

    // This function is called from the dtor so you must not use `shared_from_this` here (it will throw).
    pplx::task<void> connection_impl::shutdown()
    {
        {
            std::lock_guard<std::mutex> lock(m_stop_lock);
            m_logger.log(trace_level::info, "acquired lock in shutdown()");

            const auto current_state = get_connection_state();
            if (current_state == connection_state::disconnected)
            {
                return pplx::task_from_result();
            }

            if (current_state == connection_state::disconnecting)
            {
                // canceled task will be returned if `stop` was called while another `stop` was already in progress.
                // This is to prevent from resetting the `m_transport` in the upstream callers because doing so might
                // affect the other invocation which is using it.
                auto cts = pplx::cancellation_token_source();
                cts.cancel();
                return pplx::create_task([]() noexcept {}, cts.get_token());
            }

            // we request a cancellation of the ongoing start (if any) and wait until it is canceled
            m_disconnect_cts.cancel();

            while (m_start_completed_event.wait(60000) != 0)
            {
                m_logger.log(trace_level::errors,
                    "internal error - stopping the connection is still waiting for the start operation to finish which should have already finished or timed out");
            }

            // at this point we are either in the connected or disconnected state. If we are in the disconnected state
            // we must break because the transport has already been nulled out.
            if (m_connection_state == connection_state::disconnected)
            {
                return pplx::task_from_result();
            }

            _ASSERTE(m_connection_state == connection_state::connected);

            change_state(connection_state::disconnecting);
        }

        pplx::task_completion_event<void> tce;
        m_transport->stop([tce](std::exception_ptr exception)
            {
                if (exception != nullptr)
                {
                    tce.set_exception(exception);
                }
                else
                {
                    tce.set();
                }
            });

        return pplx::create_task(tce);
    }

    connection_state connection_impl::get_connection_state() const noexcept
    {
        return m_connection_state.load();
    }

    std::string connection_impl::get_connection_id() const noexcept
    {
        if (m_connection_state.load() == connection_state::connecting)
        {
            return "";
        }

        return m_connection_id;
    }

    void connection_impl::set_message_received(const std::function<void(const std::string&)>& message_received)
    {
        ensure_disconnected("cannot set the callback when the connection is not in the disconnected state. ");
        m_message_received = message_received;
    }

    void connection_impl::set_client_config(const signalr_client_config& config)
    {
        ensure_disconnected("cannot set client config when the connection is not in the disconnected state. ");
        m_signalr_client_config = config;
    }

    void connection_impl::set_disconnected(const std::function<void()>& disconnected)
    {
        ensure_disconnected("cannot set the disconnected callback when the connection is not in the disconnected state. ");
        m_disconnected = disconnected;
    }

    void connection_impl::ensure_disconnected(const std::string& error_message) const
    {
        const auto state = get_connection_state();
        if (state != connection_state::disconnected)
        {
            throw signalr_exception(
                error_message + "current connection state: " + translate_connection_state(state));
        }
    }

    bool connection_impl::change_state(connection_state old_state, connection_state new_state)
    {
        if (m_connection_state.compare_exchange_strong(old_state, new_state, std::memory_order_seq_cst))
        {
            handle_connection_state_change(old_state, new_state);
            return true;
        }

        return false;
    }

    connection_state connection_impl::change_state(connection_state new_state)
    {
        auto old_state = m_connection_state.exchange(new_state);
        if (old_state != new_state)
        {
            handle_connection_state_change(old_state, new_state);
        }

        return old_state;
    }

    void connection_impl::handle_connection_state_change(connection_state old_state, connection_state new_state)
    {
        m_logger.log(
            trace_level::state_changes,
            translate_connection_state(old_state)
            .append(" -> ")
            .append(translate_connection_state(new_state)));

        // Words of wisdom (if we decide to add a state_changed callback and invoke it from here):
        // "Be extra careful when you add this callback, because this is sometimes being called with the m_stop_lock.
        // This could lead to interesting problems.For example, you could run into a segfault if the connection is
        // stopped while / after transitioning into the connecting state."
    }

    std::string connection_impl::translate_connection_state(connection_state state)
    {
        switch (state)
        {
        case connection_state::connecting:
            return "connecting";
        case connection_state::connected:
            return "connected";
        case connection_state::disconnecting:
            return "disconnecting";
        case connection_state::disconnected:
            return "disconnected";
        default:
            _ASSERTE(false);
            return "(unknown)";
        }
    }
}
