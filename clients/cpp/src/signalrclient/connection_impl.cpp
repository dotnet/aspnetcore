// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include <thread>
#include <algorithm>
#include "cpprest/asyncrt_utils.h"
#include "constants.h"
#include "connection_impl.h"
#include "request_sender.h"
#include "url_builder.h"
#include "trace_log_writer.h"
#include "make_unique.h"
#include "signalrclient/signalr_exception.h"

namespace signalr
{
    // unnamed namespace makes it invisble outside this translation unit
    namespace
    {
        // this is a workaround for a compiler bug where mutable lambdas won't sometimes compile
        static void log(const logger& logger, trace_level level, const utility::string_t& entry);
    }

    std::shared_ptr<connection_impl> connection_impl::create(const utility::string_t& url, const utility::string_t& query_string,
        trace_level trace_level, const std::shared_ptr<log_writer>& log_writer)
    {
        return connection_impl::create(url, query_string, trace_level, log_writer, std::make_unique<web_request_factory>(), std::make_unique<transport_factory>());
    }

    std::shared_ptr<connection_impl> connection_impl::create(const utility::string_t& url, const utility::string_t& query_string, trace_level trace_level,
        const std::shared_ptr<log_writer>& log_writer, std::unique_ptr<web_request_factory> web_request_factory, std::unique_ptr<transport_factory> transport_factory)
    {
        return std::shared_ptr<connection_impl>(new connection_impl(url, query_string, trace_level,
            log_writer ? log_writer : std::make_shared<trace_log_writer>(), std::move(web_request_factory), std::move(transport_factory)));
    }

    connection_impl::connection_impl(const utility::string_t& url, const utility::string_t& query_string, trace_level trace_level, const std::shared_ptr<log_writer>& log_writer,
        std::unique_ptr<web_request_factory> web_request_factory, std::unique_ptr<transport_factory> transport_factory)
        : m_base_url(url), m_query_string(query_string), m_connection_state(connection_state::disconnected), m_reconnect_delay(2000),
        m_logger(log_writer, trace_level), m_transport(nullptr), m_web_request_factory(std::move(web_request_factory)),
        m_transport_factory(std::move(transport_factory)), m_message_received([](const web::json::value&){}),
        m_reconnecting([](){}), m_reconnected([](){}), m_disconnected([](){}), m_handshakeReceived(false)
    { }

    connection_impl::~connection_impl()
    {
        try
        {
            // Signaling the event is safe here. We are in the dtor so noone is using this instance. There might be some
            // outstanding threads that hold on to the connection via a weak pointer but they won't be able to acquire
            // the instance since it is being destroyed. Note that the event may actually be in non-signaled state here.
            // This for instance happens when the connection goes out of scope while a reconnect is in progress. In this
            // case the reconnect logic will not be able to acquire the connection instance from the weak_pointer to
            // signal the event so this dtor would hang indefinitely. Using a shared_ptr to the connection in reconnect
            // is not a good idea since it would prevent from invoking this dtor until the connection is reconnected or
            // reconnection fails even if the instance actually went out of scope.
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

    pplx::task<void> connection_impl::start()
    {
        {
            std::lock_guard<std::mutex> lock(m_stop_lock);
            if (!change_state(connection_state::disconnected, connection_state::connecting))
            {
                return pplx::task_from_exception<void>(
                    signalr_exception(_XPLATSTR("cannot start a connection that is not in the disconnected state")));
            }

            // there should not be any active transport at this point
            _ASSERTE(!m_transport);

            m_disconnect_cts = pplx::cancellation_token_source();
            m_start_completed_event.reset();
            m_message_id = m_groups_token = m_connection_id = _XPLATSTR("");
        }

        pplx::task_completion_event<void> start_tce;

        auto connection = shared_from_this();

        pplx::task_from_result()
            .then([connection]()
            {
                return request_sender::negotiate(*connection->m_web_request_factory, connection->m_base_url,
                    connection->m_query_string, connection->m_signalr_client_config);
            }, m_disconnect_cts.get_token())
            .then([connection](negotiation_response negotiation_response)
            {
                connection->m_connection_id = negotiation_response.connection_id;

                // TODO: check available transports

                return connection->start_transport(negotiation_response)
                    .then([connection, negotiation_response](std::shared_ptr<transport> transport)
                    {
                        connection->m_transport = transport;
                    });
            }, m_disconnect_cts.get_token())
            .then([start_tce, connection](pplx::task<void> previous_task)
            {
                try
                {
                    previous_task.get();
                    if (!connection->change_state(connection_state::connecting, connection_state::connected))
                    {
                        connection->m_logger.log(trace_level::errors,
                            utility::string_t(_XPLATSTR("internal error - transition from an unexpected state. expected state: connecting, actual state: "))
                            .append(translate_connection_state(connection->get_connection_state())));

                        _ASSERTE(false);
                    }

                    connection->m_start_completed_event.set();
                    start_tce.set();
                }
                catch (const std::exception &e)
                {
                    auto task_canceled_exception = dynamic_cast<const pplx::task_canceled *>(&e);
                    if (task_canceled_exception)
                    {
                        connection->m_logger.log(trace_level::info,
                            _XPLATSTR("starting the connection has been cancelled."));
                    }
                    else
                    {
                        connection->m_logger.log(trace_level::errors,
                            utility::string_t(_XPLATSTR("connection could not be started due to: "))
                            .append(utility::conversions::to_string_t(e.what())));
                    }

                    connection->m_transport = nullptr;
                    connection->change_state(connection_state::disconnected);
                    connection->m_start_completed_event.set();
                    start_tce.set_exception(std::current_exception());
                }
            });

        return pplx::create_task(start_tce);
    }

    pplx::task<std::shared_ptr<transport>> connection_impl::start_transport(negotiation_response negotiation_response)
    {
        auto connection = shared_from_this();

        pplx::task_completion_event<void> connect_request_tce;

        auto weak_connection = std::weak_ptr<connection_impl>(connection);
        auto& disconnect_cts = m_disconnect_cts;
        auto& logger = m_logger;

        auto process_response_callback =
            [weak_connection, connect_request_tce, disconnect_cts, logger](const utility::string_t& response) mutable
            {
                // When a connection is stopped we don't wait for its transport to stop. As a result if the same connection
                // is immediately re-started the old transport can still invoke this callback. To prevent this we capture
                // the disconnect_cts by value which allows distinguishing if the message is for the running connection
                // or for the one that was already stopped. If this is the latter we just ignore it.
                if (disconnect_cts.get_token().is_canceled())
                {
                    logger.log(trace_level::info,
                        utility::string_t(_XPLATSTR("ignoring stray message received after connection was restarted. message: "))
                        .append(response));
                    return;
                }

                auto connection = weak_connection.lock();
                if (connection)
                {
                    connection->process_response(response, connect_request_tce);
                }
            };


        auto error_callback =
            [weak_connection, connect_request_tce, disconnect_cts, logger](const std::exception &e) mutable
            {
                // When a connection is stopped we don't wait for its transport to stop. As a result if the same connection
                // is immediately re-started the old transport can still invoke this callback. To prevent this we capture
                // the disconnect_cts by value which allows distinguishing if the error is for the running connection
                // or for the one that was already stopped. If this is the latter we just ignore it.
                if (disconnect_cts.get_token().is_canceled())
                {
                    logger.log(trace_level::info,
                        utility::string_t(_XPLATSTR("ignoring stray error received after connection was restarted. error: "))
                        .append(utility::conversions::to_string_t(e.what())));

                    return;
                }

                // no op after connection started successfully
                connect_request_tce.set_exception(e);

                auto connection = weak_connection.lock();
                if (connection)
                {
                    connection->reconnect();
                }
            };

        auto transport = connection->m_transport_factory->create_transport(
            transport_type::websockets, connection->m_logger, connection->m_signalr_client_config,
            process_response_callback, error_callback);

        pplx::create_task([negotiation_response, connect_request_tce, disconnect_cts, weak_connection]()
        {
            //std::this_thread::sleep_for(std::chrono::milliseconds(negotiation_response.transport_connect_timeout));
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
                connect_request_tce.set_exception(signalr_exception(_XPLATSTR("transport timed out when trying to connect")));
            }
        });

        return connection->send_connect_request(transport, connect_request_tce)
            .then([transport](){ return pplx::task_from_result(transport); });
    }

    pplx::task<void> connection_impl::send_connect_request(const std::shared_ptr<transport>& transport, const pplx::task_completion_event<void>& connect_request_tce)
    {
        auto logger = m_logger;
        auto connect_url = url_builder::build_connect(m_base_url, transport->get_transport_type(), m_query_string);

        transport->connect(connect_url)
            .then([transport, connect_request_tce, logger](pplx::task<void> connect_task)
            mutable {
                try
                {
                    connect_task.get();
                    transport->send(_XPLATSTR("{\"protocol\":\"json\",\"version\":1}\x1e")).get();
                }
                catch (const std::exception& e)
                {
                    logger.log(
                        trace_level::errors,
                        utility::string_t(_XPLATSTR("transport could not connect due to: "))
                            .append(utility::conversions::to_string_t(e.what())));

                    connect_request_tce.set_exception(std::current_exception());
                }
            });

        return pplx::create_task(connect_request_tce);
    }

    enum MessageType
    {
        Invocation = 1,
        StreamItem,
        Completion,
        StreamInvocation,
        CancelInvocation,
        Ping,
        Close,
    };

    void connection_impl::process_response(const utility::string_t& response, const pplx::task_completion_event<void>& connect_request_tce)
    {
        m_logger.log(trace_level::messages,
            utility::string_t(_XPLATSTR("processing message: ")).append(response));

        try
        {
            auto pos = response.find('\x1e');
            std::size_t lastPos = 0;
            while (pos != utility::string_t::npos)
            {
                auto message = response.substr(lastPos, pos - lastPos);
                const auto result = web::json::value::parse(message);

                if (!result.is_object())
                {
                    m_logger.log(trace_level::info, utility::string_t(_XPLATSTR("unexpected response received from the server: "))
                        .append(message));

                    return;
                }

                if (!m_handshakeReceived)
                {
                    if (result.has_field(_XPLATSTR("error")))
                    {
                        auto error = result.at(_XPLATSTR("error")).as_string();
                        m_logger.log(trace_level::errors, utility::string_t(_XPLATSTR("handshake error: "))
                            .append(error));
                        connect_request_tce.set_exception(signalr_exception(utility::string_t(_XPLATSTR("Received an error during handshake: ")).append(error)));
                        return;
                    }
                    else
                    {
                        if (result.size() != 0)
                        {
                            connect_request_tce.set_exception(signalr_exception(utility::string_t(_XPLATSTR("Received unexpected message while waiting for the handshake response."))));
                        }
                        m_handshakeReceived = true;
                        connect_request_tce.set();
                    }
                }

                auto messageType = result.at(_XPLATSTR("type"));
                switch (messageType.as_integer())
                {
                case MessageType::Invocation:
                {
                    invoke_message_received(result);
                    break;
                }
                case MessageType::StreamInvocation:
                    // Sent to server only, should not be received by client
                    throw std::runtime_error("Received unexpected message type 'StreamInvocation'.");
                case MessageType::StreamItem:
                    // TODO
                    break;
                case MessageType::Completion:
                {
                    if (result.has_field(_XPLATSTR("error")) && result.has_field(_XPLATSTR("result")))
                    {
                        //error
                    }
                    invoke_message_received(result);
                    break;
                }
                case MessageType::CancelInvocation:
                    // Sent to server only, should not be received by client
                    throw std::runtime_error("Received unexpected message type 'CancelInvocation'.");
                case MessageType::Ping:
                    // TODO
                    break;
                case MessageType::Close:
                    // TODO
                    break;
                }

                lastPos = pos + 1;
                pos = response.find('\x1e', lastPos);
            }
        }
        catch (const std::exception &e)
        {
            m_logger.log(trace_level::errors, utility::string_t(_XPLATSTR("error occured when parsing response: "))
                .append(utility::conversions::to_string_t(e.what()))
                .append(_XPLATSTR(". response: "))
                .append(response));
        }
    }

    void connection_impl::invoke_message_received(const web::json::value& message)
    {
        try
        {
            m_message_received(message);
        }
        catch (const std::exception &e)
        {
            m_logger.log(
                trace_level::errors,
                utility::string_t(_XPLATSTR("message_received callback threw an exception: "))
                .append(utility::conversions::to_string_t(e.what())));
        }
        catch (...)
        {
            m_logger.log(trace_level::errors, _XPLATSTR("message_received callback threw an unknown exception"));
        }
    }

    pplx::task<void> connection_impl::send(const utility::string_t& data)
    {
        // To prevent an (unlikely) condition where the transport is nulled out after we checked the connection_state
        // and before sending data we store the pointer in the local variable. In this case `send()` will throw but
        // we won't crash.
        auto transport = m_transport;

        auto connection_state = get_connection_state();
        if (connection_state != signalr::connection_state::connected || !transport)
        {
            return pplx::task_from_exception<void>(signalr_exception(
                utility::string_t{_XPLATSTR("cannot send data when the connection is not in the connected state. current connection state: " })
                    .append(translate_connection_state(connection_state))));
        }

        auto logger = m_logger;

        logger.log(trace_level::info, utility::string_t(_XPLATSTR("sending data: ")).append(data));

        return transport->send(data)
            .then([logger](pplx::task<void> send_task)
            mutable {
                try
                {
                    send_task.get();
                }
                catch (const std::exception &e)
                {
                    logger.log(
                        trace_level::errors,
                        utility::string_t(_XPLATSTR("error sending data: "))
                        .append(utility::conversions::to_string_t(e.what())));

                    throw;
                }
            });
    }

    pplx::task<void> connection_impl::stop()
    {
        m_logger.log(trace_level::info, _XPLATSTR("stopping connection"));

        auto connection = shared_from_this();
        return shutdown()
            .then([connection]()
            {
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
                        utility::string_t(_XPLATSTR("disconnected callback threw an exception: "))
                        .append(utility::conversions::to_string_t(e.what())));
                }
                catch (...)
                {
                    connection->m_logger.log(
                        trace_level::errors,
                        utility::string_t(_XPLATSTR("disconnected callback threw an unknown exception")));
                }
            });
    }

    // This function is called from the dtor so you must not use `shared_from_this` here (it will throw).
    pplx::task<void> connection_impl::shutdown()
    {
        m_handshakeReceived = false;
        {
            std::lock_guard<std::mutex> lock(m_stop_lock);
            m_logger.log(trace_level::info, _XPLATSTR("acquired lock in shutdown()"));

            auto current_state = get_connection_state();
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
                return pplx::create_task([](){}, cts.get_token());
            }

            // we request a cancellation of the ongoing start or reconnect request (if any) and wait until it is cancelled
            m_disconnect_cts.cancel();

            while (m_start_completed_event.wait(60000) != 0)
            {
                m_logger.log(trace_level::errors,
                    _XPLATSTR("internal error - stopping the connection is still waiting for the start operation to finish which should have already finished or timed out"));
            }

            // at this point we are either in the connected, reconnecting or disconnected state. If we are in the disconnected state
            // we must break because the transport has already been nulled out.
            if (m_connection_state == connection_state::disconnected)
            {
                return pplx::task_from_result();
            }

            _ASSERTE(m_connection_state == connection_state::connected || m_connection_state == connection_state::reconnecting);

            change_state(connection_state::disconnecting);
        }

        return m_transport->disconnect();
    }

    void connection_impl::reconnect()
    {
        m_logger.log(trace_level::info, _XPLATSTR("connection lost - trying to re-establish connection"));

        pplx::cancellation_token_source disconnect_cts;

        {
            std::lock_guard<std::mutex> lock(m_stop_lock);
            m_logger.log(trace_level::info, _XPLATSTR("acquired lock before invoking reconnecting callback"));

            // reconnect might be called when starting the connection has not finished yet so wait until it is done
            // before actually trying to reconnect
            while (m_start_completed_event.wait(60000) != 0)
            {
                m_logger.log(trace_level::errors,
                    _XPLATSTR("internal error - reconnect is still waiting for the start operation to finish which should have already finished or timed out"));
            }

            // exit if starting the connection has not completed successfully or there is an ongoing stop request
            if (!change_state(connection_state::connected, connection_state::reconnecting))
            {
                m_logger.log(trace_level::info,
                    _XPLATSTR("reconnecting cancelled - connection is not in the connected state"));

                return;
            }

            disconnect_cts = m_disconnect_cts;
        }

        try
        {
            m_logger.log(trace_level::info, _XPLATSTR("invoking reconnecting callback"));
            m_reconnecting();
            m_logger.log(trace_level::info, _XPLATSTR("reconnecting callback returned without error"));
        }
        catch (const std::exception &e)
        {
            m_logger.log(
                trace_level::errors,
                utility::string_t(_XPLATSTR("reconnecting callback threw an exception: "))
                .append(utility::conversions::to_string_t(e.what())));
        }
        catch (...)
        {
            m_logger.log(
                trace_level::errors,
                utility::string_t(_XPLATSTR("reconnecting callback threw an unknown exception")));
        }

        {
            std::lock_guard<std::mutex> lock(m_stop_lock);

            m_logger.log(trace_level::info, _XPLATSTR("acquired lock before starting reconnect logic"));

            // This is to prevent a case where a connection was stopped (and possibly restarted and got into a reconnecting
            // state) after we changed the state to reconnecting in the original reconnecting request. In this case we have
            // the original cts which would have been cancelled by the stop request and we can use it to stop the original
            // reconnecting request
            if (disconnect_cts.get_token().is_canceled())
            {
                m_logger.log(trace_level::info,
                    _XPLATSTR("reconnecting canceled - connection was stopped and restarted after reconnecting started"));

                return;
            }

            // we set the connection to the reconnecting before we invoked the reconnecting callback. If the connection
            // state changed from the reconnecting state the user might have stopped/restarted the connection in the
            // reconnecting callback or there might have started stopping the connection on the main thread and we should
            // not try to continue the reconnect
            if (m_connection_state != connection_state::reconnecting)
            {
                m_logger.log(trace_level::info,
                    _XPLATSTR("reconnecting canceled - connection is no longer in the reconnecting state"));

                return;
            }

            // re-using the start completed event is safe because you cannot start the connection if it is not in the
            // disconnected state. It also make it easier to handle stopping the connection when it is reconnecting.
            m_start_completed_event.reset();
        }

        auto reconnect_url = url_builder::build_reconnect(m_base_url, m_transport->get_transport_type(),
            m_message_id, m_groups_token, m_query_string);

        auto weak_connection = std::weak_ptr<connection_impl>(shared_from_this());

        // this is non-blocking
        try_reconnect(reconnect_url, utility::datetime::utc_now().to_interval(), m_reconnect_window, m_reconnect_delay, disconnect_cts)
            .then([weak_connection](pplx::task<bool> reconnect_task)
            {
                // try reconnect does not throw
                auto reconnected = reconnect_task.get();

                auto connection = weak_connection.lock();
                if (!connection)
                {
                    // connection instance went away - nothing to be done
                    return pplx::task_from_result();
                }

                if (reconnected)
                {
                    if (!connection->change_state(connection_state::reconnecting, connection_state::connected))
                    {
                        connection->m_logger.log(trace_level::errors,
                            utility::string_t(_XPLATSTR("internal error - transition from an unexpected state. expected state: reconnecting, actual state: "))
                            .append(translate_connection_state(connection->get_connection_state())));

                        _ASSERTE(false);
                    }

                    // we must set the event before calling into the user code to prevent a deadlock that would happen
                    // if the user called stop() from the handler
                    connection->m_start_completed_event.set();

                    try
                    {
                        connection->m_logger.log(trace_level::info, _XPLATSTR("invoking reconnected callback"));
                        connection->m_reconnected();
                        connection->m_logger.log(trace_level::info, _XPLATSTR("reconnected callback returned without error"));
                    }
                    catch (const std::exception &e)
                    {
                        connection->m_logger.log(
                            trace_level::errors,
                            utility::string_t(_XPLATSTR("reconnected callback threw an exception: "))
                            .append(utility::conversions::to_string_t(e.what())));
                    }
                    catch (...)
                    {
                        connection->m_logger.log(
                            trace_level::errors, _XPLATSTR("reconnected callback threw an unknown exception"));
                    }

                    return pplx::task_from_result();
                }

                connection->m_start_completed_event.set();

                return connection->stop();
            });
    }

    // the assumption is that this function won't throw
    pplx::task<bool> connection_impl::try_reconnect(const web::uri& reconnect_url, const utility::datetime::interval_type reconnect_start_time,
        int reconnect_window /*milliseconds*/, int reconnect_delay /*milliseconds*/, pplx::cancellation_token_source disconnect_cts)
    {
        if (disconnect_cts.get_token().is_canceled())
        {
            log(m_logger, trace_level::info, utility::string_t(_XPLATSTR("reconnecting cancelled - connection is being stopped. line: "))
                .append(utility::conversions::to_string_t(std::to_string(__LINE__))));
            return pplx::task_from_result<bool>(false);
        }

        auto weak_connection = std::weak_ptr<connection_impl>(shared_from_this());
        auto& logger = m_logger;

        return m_transport->connect(reconnect_url)
            .then([weak_connection, reconnect_url, reconnect_start_time, reconnect_window, reconnect_delay, logger, disconnect_cts]
            (pplx::task<void> reconnect_task)
        {
            try
            {
                log(logger, trace_level::info, _XPLATSTR("reconnect attempt starting"));
                reconnect_task.get();
                log(logger, trace_level::info, _XPLATSTR("reconnect attempt completed successfully"));

                return pplx::task_from_result<bool>(true);
            }
            catch (const std::exception& e)
            {
                log(logger, trace_level::info, utility::string_t(_XPLATSTR("reconnect attempt failed due to: "))
                    .append(utility::conversions::to_string_t(e.what())));
            }

            if (disconnect_cts.get_token().is_canceled())
            {
                log(logger, trace_level::info, utility::string_t(_XPLATSTR("reconnecting cancelled - connection is being stopped. line: "))
                    .append(utility::conversions::to_string_t(std::to_string(__LINE__))));
                return pplx::task_from_result<bool>(false);
            }

            auto reconnect_window_end = reconnect_start_time + utility::datetime::from_milliseconds(reconnect_window);
            if (utility::datetime::utc_now().to_interval() + utility::datetime::from_milliseconds(reconnect_delay) > reconnect_window_end)
            {
                utility::ostringstream_t oss;
                oss << _XPLATSTR("connection could not be re-established within the configured timeout of ")
                    << reconnect_window << _XPLATSTR(" milliseconds");
                log(logger, trace_level::info, oss.str());

                return pplx::task_from_result<bool>(false);
            }

            std::this_thread::sleep_for(std::chrono::milliseconds(reconnect_delay));

            if (disconnect_cts.get_token().is_canceled())
            {
                log(logger, trace_level::info, utility::string_t(_XPLATSTR("reconnecting cancelled - connection is being stopped. line: "))
                    .append(utility::conversions::to_string_t(std::to_string(__LINE__))));

                return pplx::task_from_result<bool>(false);
            }

            auto connection = weak_connection.lock();
            if (connection)
            {
                return connection->try_reconnect(reconnect_url, reconnect_start_time, reconnect_window, reconnect_delay, disconnect_cts);
            }

            log(logger, trace_level::info, _XPLATSTR("reconnecting cancelled - connection no longer valid."));
            return pplx::task_from_result<bool>(false);
        });
    }

    connection_state connection_impl::get_connection_state() const
    {
        return m_connection_state.load();
    }

    utility::string_t connection_impl::get_connection_id() const
    {
        if (m_connection_state.load() == connection_state::connecting)
        {
            return _XPLATSTR("");
        }

        return m_connection_id;
    }

    void connection_impl::set_message_received_string(const std::function<void(const utility::string_t&)>& message_received)
    {
        set_message_received_json([message_received](const web::json::value& payload)
        {
            message_received(payload.is_string() ? payload.as_string() : payload.serialize());
        });
    }

    void connection_impl::set_message_received_json(const std::function<void(const web::json::value&)>& message_received)
    {
        ensure_disconnected(_XPLATSTR("cannot set the callback when the connection is not in the disconnected state. "));
        m_message_received = message_received;
    }

    void connection_impl::set_connection_data(const utility::string_t& connection_data)
    {
        _ASSERTE(get_connection_state() == connection_state::disconnected);

        m_connection_data = connection_data;
    }

    void connection_impl::set_client_config(const signalr_client_config& config)
    {
        ensure_disconnected(_XPLATSTR("cannot set client config when the connection is not in the disconnected state. "));
        m_signalr_client_config = config;
    }

    void connection_impl::set_reconnecting(const std::function<void()>& reconnecting)
    {
        ensure_disconnected(_XPLATSTR("cannot set the reconnecting callback when the connection is not in the disconnected state. "));
        m_reconnecting = reconnecting;
    }

    void connection_impl::set_reconnected(const std::function<void()>& reconnected)
    {
        ensure_disconnected(_XPLATSTR("cannot set the reconnected callback when the connection is not in the disconnected state. "));
        m_reconnected = reconnected;
    }

    void connection_impl::set_disconnected(const std::function<void()>& disconnected)
    {
        ensure_disconnected(_XPLATSTR("cannot set the disconnected callback when the connection is not in the disconnected state. "));
        m_disconnected = disconnected;
    }

    void connection_impl::set_reconnect_delay(const int reconnect_delay)
    {
        ensure_disconnected(_XPLATSTR("cannot set reconnect delay when the connection is not in the disconnected state. "));
        m_reconnect_delay = reconnect_delay;
    }

    void connection_impl::ensure_disconnected(const utility::string_t& error_message)
    {
        auto state = get_connection_state();
        if (state != connection_state::disconnected)
        {
            throw signalr_exception(
                error_message + _XPLATSTR("current connection state: ") + translate_connection_state(state));
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
            .append(_XPLATSTR(" -> "))
            .append(translate_connection_state(new_state)));

        // Words of wisdom (if we decide to add a state_changed callback and invoke it from here):
        // "Be extra careful when you add this callback, because this is sometimes being called with the m_stop_lock.
        // This could lead to interesting problems.For example, you could run into a segfault if the connection is
        // stopped while / after transitioning into the connecting state."
    }

    utility::string_t connection_impl::translate_connection_state(connection_state state)
    {
        switch (state)
        {
        case connection_state::connecting:
            return _XPLATSTR("connecting");
        case connection_state::connected:
            return _XPLATSTR("connected");
        case connection_state::reconnecting:
            return _XPLATSTR("reconnecting");
        case connection_state::disconnecting:
            return _XPLATSTR("disconnecting");
        case connection_state::disconnected:
            return _XPLATSTR("disconnected");
        default:
            _ASSERTE(false);
            return _XPLATSTR("(unknown)");
        }
    }

    namespace
    {
        // this is a workaround for the VS2013 compiler bug where mutable lambdas won't compile sometimes
        static void log(const logger& logger, trace_level level, const utility::string_t& entry)
        {
            const_cast<signalr::logger &>(logger).log(level, entry);
        }
    }
}
