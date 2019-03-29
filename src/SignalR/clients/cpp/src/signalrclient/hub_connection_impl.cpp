// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "hub_connection_impl.h"
#include "signalrclient/hub_exception.h"
#include "trace_log_writer.h"
#include "make_unique.h"
#include "signalrclient/signalr_exception.h"

using namespace web;

namespace signalr
{
    // unnamed namespace makes it invisble outside this translation unit
    namespace
    {
        static std::function<void(const json::value&)> create_hub_invocation_callback(const logger& logger,
            const std::function<void(const json::value&)>& set_result,
            const std::function<void(const std::exception_ptr e)>& set_exception);
    }

    std::shared_ptr<hub_connection_impl> hub_connection_impl::create(const std::string& url, trace_level trace_level,
        const std::shared_ptr<log_writer>& log_writer)
    {
        return hub_connection_impl::create(url, trace_level, log_writer,
            nullptr, std::make_unique<transport_factory>());
    }

    std::shared_ptr<hub_connection_impl> hub_connection_impl::create(const std::string& url, trace_level trace_level,
        const std::shared_ptr<log_writer>& log_writer, std::unique_ptr<http_client> http_client,
        std::unique_ptr<transport_factory> transport_factory)
    {
        auto connection = std::shared_ptr<hub_connection_impl>(new hub_connection_impl(url, trace_level,
            log_writer ? log_writer : std::make_shared<trace_log_writer>(), std::move(http_client), std::move(transport_factory)));

        connection->initialize();

        return connection;
    }

    hub_connection_impl::hub_connection_impl(const std::string& url, trace_level trace_level,
        const std::shared_ptr<log_writer>& log_writer, std::unique_ptr<http_client> http_client,
        std::unique_ptr<transport_factory> transport_factory)
        : m_connection(connection_impl::create(url, trace_level, log_writer,
        std::move(http_client), std::move(transport_factory))), m_logger(log_writer, trace_level),
        m_callback_manager(json::value::parse(_XPLATSTR("{ \"error\" : \"connection went out of scope before invocation result was received\"}"))),
        m_disconnected([]() noexcept {}), m_handshakeReceived(false)
    { }

    void hub_connection_impl::initialize()
    {
        // weak_ptr prevents a circular dependency leading to memory leak and other problems
        std::weak_ptr<hub_connection_impl> weak_hub_connection = shared_from_this();

        m_connection->set_message_received([weak_hub_connection](const std::string& message)
        {
            auto connection = weak_hub_connection.lock();
            if (connection)
            {
                connection->process_message(message);
            }
        });

        m_connection->set_disconnected([weak_hub_connection]()
        {
            auto connection = weak_hub_connection.lock();
            if (connection)
            {
                connection->m_handshakeTask.set_exception(signalr_exception("connection closed while handshake was in progress."));
                connection->m_disconnected();
            }
        });
    }

    void hub_connection_impl::on(const std::string& event_name, const std::function<void(const json::value &)>& handler)
    {
        if (event_name.length() == 0)
        {
            throw std::invalid_argument("event_name cannot be empty");
        }

        auto weak_connection = std::weak_ptr<hub_connection_impl>(shared_from_this());
        auto connection = weak_connection.lock();
        if (connection && connection->get_connection_state() != connection_state::disconnected)
        {
            throw signalr_exception("can't register a handler if the connection is in a disconnected state");
        }

        if (m_subscriptions.find(event_name) != m_subscriptions.end())
        {
            throw signalr_exception(
                "an action for this event has already been registered. event name: " + event_name);
        }

        m_subscriptions.insert(std::pair<std::string, std::function<void(const json::value &)>> {event_name, handler});
    }

    void hub_connection_impl::start(std::function<void(std::exception_ptr)> callback) noexcept
    {
        if (m_connection->get_connection_state() != connection_state::disconnected)
        {
            callback(std::make_exception_ptr(signalr_exception(
                "the connection can only be started if it is in the disconnected state")));
            return;
        }

        m_connection->set_client_config(m_signalr_client_config);
        m_handshakeTask = pplx::task_completion_event<void>();
        m_handshakeReceived = false;
        std::weak_ptr<hub_connection_impl> weak_connection = shared_from_this();
        m_connection->start([weak_connection, callback](std::exception_ptr start_exception)
            {
                auto connection = weak_connection.lock();
                if (!connection)
                {
                    // The connection has been destructed
                    callback(std::make_exception_ptr(signalr_exception("the hub connection has been deconstructed")));
                    return;
                }

                if (start_exception)
                {
                    connection->m_connection->stop([start_exception, callback, weak_connection](std::exception_ptr)
                    {
                        try
                        {
                            auto connection = weak_connection.lock();
                            if (!connection)
                            {
                                callback(std::make_exception_ptr(signalr_exception("the hub connection has been deconstructed")));
                                return;
                            }
                            pplx::task<void>(connection->m_handshakeTask).get();
                        }
                        catch (...) {}

                        callback(start_exception);
                    });
                    return;
                }

                // TODO: Generate this later when we have the protocol abstraction
                auto handshake_request = "{\"protocol\":\"json\",\"version\":1}\x1e";
                connection->m_connection->send(handshake_request, [weak_connection, callback](std::exception_ptr exception)
                {
                    auto connection = weak_connection.lock();
                    if (!connection)
                    {
                        // The connection has been destructed
                        callback(std::make_exception_ptr(signalr_exception("the hub connection has been deconstructed")));
                        return;
                    }

                    if (exception)
                    {
                        callback(exception);
                        return;
                    }

                    try
                    {
                        pplx::task<void>(connection->m_handshakeTask).get();
                        callback(nullptr);
                    }
                    catch (...)
                    {
                        auto handshake_exception = std::current_exception();
                        connection->m_connection->stop([callback, handshake_exception](std::exception_ptr)
                        {
                            callback(handshake_exception);
                        });
                    }
                });
            });
    }

    void hub_connection_impl::stop(std::function<void(std::exception_ptr)> callback) noexcept
    {
        m_callback_manager.clear(json::value::parse(_XPLATSTR("{ \"error\" : \"connection was stopped before invocation result was received\"}")));
        m_connection->stop(callback);
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

    void hub_connection_impl::process_message(const std::string& response)
    {
        try
        {
            auto pos = response.find('\x1e');
            std::size_t lastPos = 0;
            while (pos != std::string::npos)
            {
                auto message = response.substr(lastPos, pos - lastPos);
                const auto result = web::json::value::parse(utility::conversions::to_string_t(message));

                if (!result.is_object())
                {
                    m_logger.log(trace_level::info, std::string("unexpected response received from the server: ")
                        .append(message));

                    return;
                }

                if (!m_handshakeReceived)
                {
                    if (result.has_field(_XPLATSTR("error")))
                    {
                        auto error = utility::conversions::to_utf8string(result.at(_XPLATSTR("error")).as_string());
                        m_logger.log(trace_level::errors, std::string("handshake error: ")
                            .append(error));
                        m_handshakeTask.set_exception(signalr_exception(std::string("Received an error during handshake: ").append(error)));
                        return;
                    }
                    else
                    {
                        if (result.has_field(_XPLATSTR("type")))
                        {
                            m_handshakeTask.set_exception(signalr_exception(std::string("Received unexpected message while waiting for the handshake response.")));
                        }
                        m_handshakeReceived = true;
                        m_handshakeTask.set();
                        return;
                    }
                }

                auto messageType = result.at(_XPLATSTR("type"));
                switch (messageType.as_integer())
                {
                case MessageType::Invocation:
                {
                    auto method = utility::conversions::to_utf8string(result.at(_XPLATSTR("target")).as_string());
                    auto event = m_subscriptions.find(method);
                    if (event != m_subscriptions.end())
                    {
                        event->second(result.at(_XPLATSTR("arguments")));
                    }
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
                        // TODO: error
                    }
                    invoke_callback(result);
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
            m_logger.log(trace_level::errors, std::string("error occured when parsing response: ")
                .append(e.what())
                .append(". response: ")
                .append(response));
        }
    }

    bool hub_connection_impl::invoke_callback(const web::json::value& message)
    {
        auto id = utility::conversions::to_utf8string(message.at(_XPLATSTR("invocationId")).as_string());
        if (!m_callback_manager.invoke_callback(id, message, true))
        {
            m_logger.log(trace_level::info, std::string("no callback found for id: ").append(id));
            return false;
        }

        return true;
    }

    void hub_connection_impl::invoke(const std::string& method_name, const json::value& arguments, std::function<void(const web::json::value&, std::exception_ptr)> callback) noexcept
    {
        _ASSERTE(arguments.is_array());

        const auto callback_id = m_callback_manager.register_callback(
            create_hub_invocation_callback(m_logger, [callback](const json::value& result) { callback(result, nullptr); },
                [callback](const std::exception_ptr e) { callback(json::value(), e); }));

        invoke_hub_method(method_name, arguments, callback_id, nullptr,
            [callback](const std::exception_ptr e){ callback(json::value(), e); });
    }

    void hub_connection_impl::send(const std::string& method_name, const json::value& arguments, std::function<void(std::exception_ptr)> callback) noexcept
    {
        _ASSERTE(arguments.is_array());

        invoke_hub_method(method_name, arguments, "",
            [callback]() { callback(nullptr); },
            [callback](const std::exception_ptr e){ callback(e); });
    }

    void hub_connection_impl::invoke_hub_method(const std::string& method_name, const json::value& arguments,
        const std::string& callback_id, std::function<void()> set_completion, std::function<void(const std::exception_ptr)> set_exception) noexcept
    {
        json::value request;
        request[_XPLATSTR("type")] = json::value(1);
        if (!callback_id.empty())
        {
            request[_XPLATSTR("invocationId")] = json::value::string(utility::conversions::to_string_t(callback_id));
        }
        request[_XPLATSTR("target")] = json::value::string(utility::conversions::to_string_t(method_name));
        request[_XPLATSTR("arguments")] = arguments;

        // weak_ptr prevents a circular dependency leading to memory leak and other problems
        auto weak_hub_connection = std::weak_ptr<hub_connection_impl>(shared_from_this());

        m_connection->send(utility::conversions::to_utf8string(request.serialize() + _XPLATSTR('\x1e')), [set_completion, set_exception, weak_hub_connection, callback_id](std::exception_ptr exception)
        {
            if (exception)
            {
                set_exception(exception);
                auto hub_connection = weak_hub_connection.lock();
                if (hub_connection)
                {
                    hub_connection->m_callback_manager.remove_callback(callback_id);
                }
            }
            else
            {
                if (callback_id.empty())
                {
                    // complete nonBlocking call
                    set_completion();
                }
            }
        });
    }

    connection_state hub_connection_impl::get_connection_state() const noexcept
    {
        return m_connection->get_connection_state();
    }

    std::string hub_connection_impl::get_connection_id() const
    {
        return m_connection->get_connection_id();
    }

    void hub_connection_impl::set_client_config(const signalr_client_config& config)
    {
        m_signalr_client_config = config;
        m_connection->set_client_config(config);
    }

    void hub_connection_impl::set_disconnected(const std::function<void()>& disconnected)
    {
        m_disconnected = disconnected;
    }

    // unnamed namespace makes it invisble outside this translation unit
    namespace
    {
        static std::function<void(const json::value&)> create_hub_invocation_callback(const logger& logger,
            const std::function<void(const json::value&)>& set_result,
            const std::function<void(const std::exception_ptr)>& set_exception)
        {
            return [logger, set_result, set_exception](const json::value& message)
            {
                if (message.has_field(_XPLATSTR("result")))
                {
                    set_result(message.at(_XPLATSTR("result")));
                }
                else if (message.has_field(_XPLATSTR("error")))
                {
                    set_exception(
                        std::make_exception_ptr(
                            hub_exception(utility::conversions::to_utf8string(message.at(_XPLATSTR("error")).serialize()))));
                }
                else
                {
                    set_result(json::value::value());
                }
            };
        }
    }
}
