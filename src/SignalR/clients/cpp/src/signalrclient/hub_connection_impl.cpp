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

        static utility::string_t adapt_url(const utility::string_t& url, bool use_default_url);
    }

    std::shared_ptr<hub_connection_impl> hub_connection_impl::create(const utility::string_t& url, const utility::string_t& query_string,
        trace_level trace_level, const std::shared_ptr<log_writer>& log_writer, bool use_default_url)
    {
        return hub_connection_impl::create(url, query_string, trace_level, log_writer, use_default_url,
            std::make_unique<web_request_factory>(), std::make_unique<transport_factory>());
    }

    std::shared_ptr<hub_connection_impl> hub_connection_impl::create(const utility::string_t& url, const utility::string_t& query_string,
        trace_level trace_level, const std::shared_ptr<log_writer>& log_writer, bool use_default_url,
        std::unique_ptr<web_request_factory> web_request_factory, std::unique_ptr<transport_factory> transport_factory)
    {
        auto connection = std::shared_ptr<hub_connection_impl>(new hub_connection_impl(url, query_string, trace_level,
            log_writer ? log_writer : std::make_shared<trace_log_writer>(), use_default_url,
            std::move(web_request_factory), std::move(transport_factory)));

        connection->initialize();

        return connection;
    }

    hub_connection_impl::hub_connection_impl(const utility::string_t& url, const utility::string_t& query_string, trace_level trace_level,
        const std::shared_ptr<log_writer>& log_writer, bool use_default_url, std::unique_ptr<web_request_factory> web_request_factory,
        std::unique_ptr<transport_factory> transport_factory)
        : m_connection(connection_impl::create(adapt_url(url, use_default_url), query_string, trace_level, log_writer,
        std::move(web_request_factory), std::move(transport_factory))),m_logger(log_writer, trace_level),
        m_callback_manager(json::value::parse(_XPLATSTR("{ \"E\" : \"connection went out of scope before invocation result was received\"}")))
    { }

    void hub_connection_impl::initialize()
    {
        auto this_hub_connection = shared_from_this();

        // weak_ptr prevents a circular dependency leading to memory leak and other problems
        auto weak_hub_connection = std::weak_ptr<hub_connection_impl>(this_hub_connection);

        m_connection->set_message_received_json([weak_hub_connection](const web::json::value& message)
        {
            auto connection = weak_hub_connection.lock();
            if (connection)
            {
                connection->process_message(message);
            }
        });

        set_reconnecting([](){});
    }

    void hub_connection_impl::on(const utility::string_t& event_name, const std::function<void(const json::value &)>& handler)
    {
        if (event_name.length() == 0)
        {
            throw std::invalid_argument("event_name cannot be empty");
        }

        auto weak_connection = std::weak_ptr<hub_connection_impl>(shared_from_this());
        auto connection = weak_connection.lock();
        if (connection && connection->get_connection_state() != connection_state::disconnected)
        {
            throw signalr_exception(_XPLATSTR("can't register a handler if the connection is in a disconnected state"));
        }

        if (m_subscriptions.find(event_name) != m_subscriptions.end())
        {
            throw signalr_exception(
                _XPLATSTR("an action for this event has already been registered. event name: ") + event_name);
        }

        m_subscriptions.insert(std::pair<utility::string_t, std::function<void(const json::value &)>> {event_name, handler});
    }

    pplx::task<void> hub_connection_impl::start()
    {
        return m_connection->start();
    }

    pplx::task<void> hub_connection_impl::stop()
    {
        m_callback_manager.clear(json::value::parse(_XPLATSTR("{ \"E\" : \"connection was stopped before invocation result was received\"}")));
        return m_connection->stop();
    }

    void hub_connection_impl::process_message(const web::json::value& message)
    {
        auto type = message.at(_XPLATSTR("type")).as_integer();
        if (type == 3)
        {
            if (invoke_callback(message))
            {
                return;
            }
        }
        else if (type == 1)
        {
            auto method = message.at(_XPLATSTR("target")).as_string();
            auto event = m_subscriptions.find(method);
            if (event != m_subscriptions.end())
            {
                event->second(message.at(_XPLATSTR("arguments")));
            }
            return;
        }

        m_logger.log(trace_level::info, utility::string_t(_XPLATSTR("non-hub message received and will be discarded. message: "))
            .append(message.serialize()));
    }

    bool hub_connection_impl::invoke_callback(const web::json::value& message)
    {
        auto id = message.at(_XPLATSTR("invocationId")).as_string();
        if (!m_callback_manager.invoke_callback(id, message, true))
        {
            m_logger.log(trace_level::info, utility::string_t(_XPLATSTR("no callback found for id: ")).append(id));
            return false;
        }

        return true;
    }

    pplx::task<json::value> hub_connection_impl::invoke_json(const utility::string_t& method_name, const json::value& arguments)
    {
        _ASSERTE(arguments.is_array());

        pplx::task_completion_event<json::value> tce;

        const auto callback_id = m_callback_manager.register_callback(
            create_hub_invocation_callback(m_logger, [tce](const json::value& result) { tce.set(result); },
                [tce](const std::exception_ptr e) { tce.set_exception(e); }));

        invoke_hub_method(method_name, arguments, callback_id, nullptr,
            [tce](const std::exception_ptr e){tce.set_exception(e); });

        return pplx::create_task(tce);
    }

    pplx::task<void> hub_connection_impl::invoke_void(const utility::string_t& method_name, const json::value& arguments)
    {
        _ASSERTE(arguments.is_array());

        pplx::task_completion_event<void> tce;

        invoke_hub_method(method_name, arguments, _XPLATSTR(""),
            [tce]() { tce.set(); },
            [tce](const std::exception_ptr e){ tce.set_exception(e); });

        return pplx::create_task(tce);
    }

    void hub_connection_impl::invoke_hub_method(const utility::string_t& method_name, const json::value& arguments,
        const utility::string_t& callback_id, std::function<void()> set_completion, std::function<void(const std::exception_ptr)> set_exception)
    {
        json::value request;
        request[_XPLATSTR("type")] = json::value::value(1);
        if (!callback_id.empty())
        {
            request[_XPLATSTR("invocationId")] = json::value::string(callback_id);
        }
        request[_XPLATSTR("target")] = json::value::string(method_name);
        request[_XPLATSTR("arguments")] = arguments;

        auto this_hub_connection = shared_from_this();

        // weak_ptr prevents a circular dependency leading to memory leak and other problems
        auto weak_hub_connection = std::weak_ptr<hub_connection_impl>(this_hub_connection);

        m_connection->send(request.serialize() + _XPLATSTR('\x1e'))
            .then([set_completion, set_exception, weak_hub_connection, callback_id](pplx::task<void> send_task)
            {
                try
                {
                    send_task.get();
                    if (callback_id.empty())
                    {
                        // complete nonBlocking call
                        set_completion();
                    }
                }
                catch (const std::exception&)
                {
                    set_exception(std::current_exception());
                    auto hub_connection = weak_hub_connection.lock();
                    if (hub_connection)
                    {
                        hub_connection->m_callback_manager.remove_callback(callback_id);
                    }
                }
            });
    }

    connection_state hub_connection_impl::get_connection_state() const
    {
        return m_connection->get_connection_state();
    }

    utility::string_t hub_connection_impl::get_connection_id() const
    {
        return m_connection->get_connection_id();
    }

    void hub_connection_impl::set_client_config(const signalr_client_config& config)
    {
        m_connection->set_client_config(config);
    }

    void hub_connection_impl::set_reconnecting(const std::function<void()>& reconnecting)
    {
        // weak_ptr prevents a circular dependency leading to memory leak and other problems
        auto weak_hub_connection = std::weak_ptr<hub_connection_impl>(shared_from_this());

        m_connection->set_reconnecting([weak_hub_connection, reconnecting]()
        {
            auto hub_connection = weak_hub_connection.lock();
            if (hub_connection)
            {
                hub_connection->m_callback_manager.clear(
                    json::value::parse(_XPLATSTR("{ \"E\" : \"connection has been lost\"}")));
            }

            reconnecting();
        });
    }

    void hub_connection_impl::set_reconnected(const std::function<void()>& reconnected)
    {
        m_connection->set_reconnected(reconnected);
    }

    void hub_connection_impl::set_disconnected(const std::function<void()>& disconnected)
    {
        m_connection->set_disconnected(disconnected);
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
                            signalr_exception(message.at(_XPLATSTR("error")).serialize())));
                }

                set_result(json::value::null());
            };
        }

        static utility::string_t adapt_url(const utility::string_t& url, bool use_default_url)
        {
            if (use_default_url)
            {
                auto new_url = url;
                if (new_url.back() != _XPLATSTR('/'))
                {
                    new_url.append(_XPLATSTR("/"));
                }

                return new_url;
            }

            return url;
        }
    }
}