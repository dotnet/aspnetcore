// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "websocket_transport.h"
#include "logger.h"
#include "signalrclient/signalr_exception.h"

namespace signalr
{
    std::shared_ptr<transport> websocket_transport::create(const std::function<std::shared_ptr<websocket_client>()>& websocket_client_factory,
        const logger& logger, const std::function<void(const utility::string_t &)>& process_response_callback,
        std::function<void(const std::exception&)> error_callback)
    {
        return std::shared_ptr<transport>(
            new websocket_transport(websocket_client_factory, logger, process_response_callback, error_callback));
    }

    websocket_transport::websocket_transport(const std::function<std::shared_ptr<websocket_client>()>& websocket_client_factory,
        const logger& logger, const std::function<void(const utility::string_t &)>& process_response_callback,
        std::function<void(const std::exception&)> error_callback)
        : transport(logger, process_response_callback, error_callback), m_websocket_client_factory(websocket_client_factory)
    {
        // we use this cts to check if the receive loop is running so it should be
        // initially cancelled to indicate that the receive loop is not running
        m_receive_loop_cts.cancel();
    }

    websocket_transport::~websocket_transport()
    {
        try
        {
            disconnect().get();
        }
        catch (...) // must not throw from the destructor
        {}
    }

    transport_type websocket_transport::get_transport_type() const
    {
        return transport_type::websockets;
    }

    pplx::task<void> websocket_transport::connect(const web::uri &url)
    {
        _ASSERTE(url.scheme() == _XPLATSTR("ws") || url.scheme() == _XPLATSTR("wss"));

        {
            std::lock_guard<std::mutex> stop_lock(m_start_stop_lock);

            if (!m_receive_loop_cts.get_token().is_canceled())
            {
                throw signalr_exception(_XPLATSTR("transport already connected"));
            }

            m_logger.log(trace_level::info,
                utility::string_t(_XPLATSTR("[websocket transport] connecting to: "))
                .append(url.to_string()));

            auto websocket_client = m_websocket_client_factory();

            {
                std::lock_guard<std::mutex> client_lock(m_websocket_client_lock);
                m_websocket_client = websocket_client;
            }

            pplx::cancellation_token_source receive_loop_cts;
            pplx::task_completion_event<void> connect_tce;

            auto transport = shared_from_this();

            websocket_client->connect(url)
                .then([transport, connect_tce, receive_loop_cts](pplx::task<void> connect_task)
                {
                    try
                    {
                        connect_task.get();
                        transport->receive_loop(receive_loop_cts);
                        connect_tce.set();
                    }
                    catch (const std::exception &e)
                    {
                        transport->m_logger.log(
                            trace_level::errors,
                            utility::string_t(_XPLATSTR("[websocket transport] exception when connecting to the server: "))
                            .append(utility::conversions::to_string_t(e.what())));

                        receive_loop_cts.cancel();
                        connect_tce.set_exception(std::current_exception());
                    }
                });

            m_receive_loop_cts = receive_loop_cts;

            return pplx::create_task(connect_tce);
        }
    }

    pplx::task<void> websocket_transport::send(const utility::string_t &data)
    {
        // send will return a faulted task if client has disconnected
        return safe_get_websocket_client()->send(data);
    }

    pplx::task<void> websocket_transport::disconnect()
    {
        std::shared_ptr<websocket_client> websocket_client = nullptr;

        {
            std::lock_guard<std::mutex> lock(m_start_stop_lock);

            if (m_receive_loop_cts.get_token().is_canceled())
            {
                return pplx::task_from_result();
            }

            m_receive_loop_cts.cancel();

            websocket_client = safe_get_websocket_client();
        }

        auto logger = m_logger;

        return websocket_client->close()
            .then([logger](pplx::task<void> close_task)
            mutable {
                try
                {
                    close_task.get();
                }
                catch (const std::exception &e)
                {
                    logger.log(
                        trace_level::errors,
                        utility::string_t(_XPLATSTR("[websocket transport] exception when closing websocket: "))
                        .append(utility::conversions::to_string_t(e.what())));
                }
            });
    }

    // Note that the connection assumes that the error callback won't be fired when the result is being processed. This
    // may no longer be true when we replace the `receive_loop` with "on_message_received" and "on_close" events if they
    // can be fired on different threads in which case we will have to lock before setting groups token and message id.
    void websocket_transport::receive_loop(pplx::cancellation_token_source cts)
    {
        auto this_transport = shared_from_this();
        auto logger = this_transport->m_logger;

        // Passing the `std::weak_ptr<websocket_transport>` prevents from a memory leak where we would capture the shared_ptr to
        // the transport in the continuation lambda and as a result as long as the loop runs the ref count would never get to
        // zero. Now we capture the weak pointer and get the shared pointer only when the continuation runs so the ref count is
        // incremented when the shared pointer is acquired and then decremented when it goes out of scope of the continuation.
        auto weak_transport = std::weak_ptr<websocket_transport>(this_transport);

        auto websocket_client = this_transport->safe_get_websocket_client();

        websocket_client->receive()
            // There are two cases when we exit the loop. The first case is implicit - we pass the cancellation_token
            // to `then` (note this is after the lambda body) and if the token is cancelled the continuation will not
            // run at all. The second - explicit - case happens if the token gets cancelled after the continuation has
            // been started in which case we just stop the loop by not scheduling another receive task.
            .then([weak_transport, cts](std::string message)
            {
                auto transport = weak_transport.lock();
                if (transport)
                {
                    transport->process_response(utility::conversions::to_string_t(message));

                    if (!cts.get_token().is_canceled())
                    {
                        transport->receive_loop(cts);
                    }
                }
            }, cts.get_token())
            // this continuation is used to observe exceptions from the previous tasks. It will run always - even if one of
            // the previous continuations throws or was not scheduled due to the cancellation token being set to cancelled
            .then([weak_transport, logger, websocket_client, cts](pplx::task<void> task)
            mutable {
                try
                {
                    task.get();
                }
                catch (const pplx::task_canceled&)
                {
                    cts.cancel();

                    logger.log(trace_level::info,
                        utility::string_t(_XPLATSTR("[websocket transport] receive task canceled.")));
                }
                catch (const std::exception& e)
                {
                    cts.cancel();

                    logger.log(
                        trace_level::errors,
                        utility::string_t(_XPLATSTR("[websocket transport] error receiving response from websocket: "))
                        .append(utility::conversions::to_string_t(e.what())));

                    websocket_client->close()
                        .then([](pplx::task<void> task)
                    {
                        try { task.get(); }
                        catch (...) {}
                    });

                    auto transport = weak_transport.lock();
                    if (transport)
                    {
                        transport->error(e);
                    }
                }
                catch (...)
                {
                    cts.cancel();

                    logger.log(
                        trace_level::errors,
                        utility::string_t(_XPLATSTR("[websocket transport] unknown error occurred when receiving response from websocket")));

                    websocket_client->close()
                        .then([](pplx::task<void> task)
                    {
                        try { task.get(); }
                        catch (...) {}
                    });

                    auto transport = weak_transport.lock();
                    if (transport)
                    {
                        transport->error(signalr_exception(_XPLATSTR("unknown error")));
                    }
                }
            });
    }

    std::shared_ptr<websocket_client> websocket_transport::safe_get_websocket_client()
    {
        {
            std::lock_guard<std::mutex> lock(m_websocket_client_lock);
            auto websocket_client = m_websocket_client;

            return websocket_client;
        }
    }
}