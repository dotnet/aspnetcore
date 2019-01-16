// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_utils.h"
#include "test_web_request_factory.h"
#include "test_websocket_client.h"
#include "test_transport_factory.h"
#include "connection_impl.h"
#include "signalrclient/trace_level.h"
#include "trace_log_writer.h"
#include "memory_log_writer.h"
#include "cpprest/ws_client.h"
#include "signalrclient/signalr_exception.h"

using namespace signalr;

static std::shared_ptr<connection_impl> create_connection(std::shared_ptr<websocket_client> websocket_client = create_test_websocket_client(),
    std::shared_ptr<log_writer> log_writer = std::make_shared<trace_log_writer>(), trace_level trace_level = trace_level::all)
{
    return connection_impl::create(create_uri(), _XPLATSTR(""), trace_level, log_writer, create_test_web_request_factory(),
        std::make_unique<test_transport_factory>(websocket_client));
}

TEST(connection_impl_connection_state, initial_connection_state_is_disconnected)
{
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::none, std::make_shared<trace_log_writer>());

    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());
}

TEST(connection_impl_start, cannot_start_non_disconnected_exception)
{
    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client);

    connection->start().wait();

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const signalr_exception& e)
    {
        ASSERT_STREQ("cannot start a connection that is not in the disconnected state", e.what());
    }
}

TEST(connection_impl_start, connection_state_is_connecting_when_connection_is_being_started)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_exception<std::string>(std::runtime_error("should not be invoked")); },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[](const web::uri&)
        {
            return pplx::task_from_exception<void>(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed")));
        });

    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    connection->start()
        // this test is not set up to connect successfully so we have to observe exceptions otherwise
        // other tests may fail due to an unobserved exception from the outstanding start task
        .then([](pplx::task<void> start_task)
        {
            try
            {
                start_task.get();
            }
            catch (...)
            { }
        });

    ASSERT_EQ(connection->get_connection_state(), connection_state::connecting);
}

TEST(connection_impl_start, connection_state_is_connected_when_connection_established_succesfully)
{
    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client);
    connection->start().get();
    ASSERT_EQ(connection->get_connection_state(), connection_state::connected);
}

TEST(connection_impl_start, connection_state_is_disconnected_when_connection_cannot_be_established)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri &) -> std::unique_ptr<web_request>
    {
        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)404, _XPLATSTR("Bad request"), _XPLATSTR("")));
    });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::none, std::make_shared<trace_log_writer>(),
        std::move(web_request_factory), std::make_unique<transport_factory>());

    try
    {
        connection->start().get();
    }
    catch (...)
    { }

    ASSERT_EQ(connection->get_connection_state(), connection_state::disconnected);
}

TEST(connection_impl_start, start_logs_exceptions)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri &) -> std::unique_ptr<web_request>
    {
        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)404, _XPLATSTR("Bad request"), _XPLATSTR("")));
    });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::errors, writer,
            std::move(web_request_factory), std::make_unique<transport_factory>());

    try
    {
        connection->start().get();
    }
    catch (...)
    { }

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[error       ] connection could not be started due to: web exception - 404 Bad request\n"), entry);
}

TEST(connection_impl_start, start_propagates_exceptions_from_negotiate)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri &) -> std::unique_ptr<web_request>
    {
        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)404, _XPLATSTR("Bad request"), _XPLATSTR("")));
    });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::none, std::make_shared<trace_log_writer>(),
        std::move(web_request_factory), std::make_unique<transport_factory>());

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("web exception - 404 Bad request"), utility::conversions::to_string_t(e.what()));
    }
}

TEST(connection_impl_start, start_fails_if_transport_connect_throws)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_exception<std::string>(std::runtime_error("should not be invoked")); },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[](const web::uri&)
        {
            return pplx::task_from_exception<void>(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed")));
        });

    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("connecting failed"), utility::conversions::to_string_t(e.what()));
    }

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_TRUE(log_entries.size() > 1);

    auto entry = remove_date_from_log_entry(log_entries[1]);
    ASSERT_EQ(_XPLATSTR("[error       ] transport could not connect due to: connecting failed\n"), entry);
}

TEST(connection_impl_start, start_fails_if_TryWebsockets_false_and_no_fallback_transport)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri &) -> std::unique_ptr<web_request>
    {
        utility::string_t response_body(
            _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : false, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}"));

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto websocket_client = std::make_shared<test_websocket_client>();
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::errors, std::make_shared<trace_log_writer>(),
        std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("websockets not supported on the server and there is no fallback transport"),
            utility::conversions::to_string_t(e.what()));
    }
}

#if defined(_WIN32)   //  https://github.com/aspnet/SignalR-Client-Cpp/issues/131

TEST(connection_impl_start, start_fails_if_transport_fails_when_receiving_messages)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []()
        {
            return pplx::task_from_exception<std::string>(std::runtime_error("receive error"));
        });

    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("receive error"), utility::conversions::to_string_t(e.what()));
    }

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_TRUE(log_entries.size() > 1) << dump_vector(log_entries);

    auto entry = remove_date_from_log_entry(log_entries[1]);
    ASSERT_EQ(_XPLATSTR("[error       ] connection could not be started due to: receive error\n"), entry) << dump_vector(log_entries);
}

#endif

TEST(connection_impl_start, start_fails_if_start_request_fails)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : _XPLATSTR("{ }");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto websocket_client = std::make_shared<test_websocket_client>();
    websocket_client->set_receive_function([]()->pplx::task<std::string>
    {
        return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }"));
    });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::messages, writer,
        std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const signalr_exception &e)
    {
        ASSERT_STREQ("start request failed due to unexpected response from the server: { }", e.what());
    }
}

TEST(connection_impl_start, start_fails_if_connect_request_times_out)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 0.1, \"LongPollDelay\" : 0.0}")
            : _XPLATSTR("{ }");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto websocket_client = std::make_shared<test_websocket_client>();
    websocket_client->set_receive_function([]()->pplx::task<std::string>
    {
        return pplx::task_from_result(std::string("{}"));
    });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::messages, writer,
        std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const signalr_exception &e)
    {
        ASSERT_STREQ("transport timed out when trying to connect", e.what());
    }
}

TEST(connection_impl_start, start_fails_if_protocol_versions_not_compatible)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.2\", \"TransportConnectTimeout\" : 0.1, \"LongPollDelay\" : 0.0}")
            : _XPLATSTR("{ }");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto websocket_client = std::make_shared<test_websocket_client>();
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::all, std::make_shared<trace_log_writer>(),
        std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const signalr_exception &e)
    {
        ASSERT_STREQ("incompatible protocol version. client protocol version: 1.4, server protocol version: 1.2", e.what());
    }
}

TEST(connection_impl_process_response, process_response_logs_messages)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::messages);

    connection->start().get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[message     ] processing message: {\"C\":\"x\", \"S\":1, \"M\":[] }\n"), entry);
}

TEST(connection_impl_send, message_sent)
{
    utility::string_t actual_message;

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }")); },
        /* send function */ [&actual_message](const utility::string_t& message)
    {
        actual_message = message;
        return pplx::task_from_result();
    });

    auto connection = create_connection(websocket_client);

    const utility::string_t message{ _XPLATSTR("Test message") };

    connection->start()
        .then([connection, message]()
        {
            return connection->send(message);
        }).get();

    ASSERT_EQ(message, actual_message);
}

TEST(connection_impl_send, send_throws_if_connection_not_connected)
{
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::none, std::make_shared<trace_log_writer>());

    try
    {
        connection->send(_XPLATSTR("whatever")).get();
        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const signalr_exception &e)
    {
        ASSERT_STREQ("cannot send data when the connection is not in the connected state. current connection state: disconnected", e.what());
    }
}

TEST(connection_impl_send, exceptions_from_send_logged_and_propagated)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }")); },
        /* send function */ [](const utility::string_t&){ return pplx::task_from_exception<void>(std::runtime_error("error")); });

    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    try
    {
        connection->start()
            .then([connection]()
        {
            return connection->send(_XPLATSTR("Test message"));
        }).get();

        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const std::runtime_error &e)
    {
        ASSERT_STREQ("error", e.what());
    }

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[error       ] error sending data: error\n"), entry);
}

TEST(connection_impl_set_message_received, callback_invoked_when_message_received)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]()
        mutable {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{ \"C\":\"x\", \"G\":\"gr0\", \"M\":[]}",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"Test\"] }",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"release\"] }",
            "{}"
        };

        call_number = std::min(call_number + 1, 4);

        return pplx::task_from_result(responses[call_number]);
    });

    auto connection = create_connection(websocket_client);

    auto message = std::make_shared<utility::string_t>();

    auto message_received_event = std::make_shared<event>();
    connection->set_message_received_string([message, message_received_event](const utility::string_t &m){
        if (m == _XPLATSTR("Test"))
        {
            *message = m;
        }

        if (m == _XPLATSTR("release"))
        {
            message_received_event->set();
        }
    });

    connection->start().get();

    ASSERT_FALSE(message_received_event->wait(5000));

    ASSERT_EQ(_XPLATSTR("Test"), *message);
}

TEST(connection_impl_set_message_received, exception_from_callback_caught_and_logged)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]()
        mutable {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"throw\"] }",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"release\"] }",
            "{}"
        };

        call_number = std::min(call_number + 1, 3);

        return pplx::task_from_result(responses[call_number]);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    auto message_received_event = std::make_shared<event>();
    connection->set_message_received_string([message_received_event](const utility::string_t &m){
        if (m == _XPLATSTR("throw"))
        {
            throw std::runtime_error("oops");
        }

        if (m == _XPLATSTR("release"))
        {
            message_received_event->set();
        }
    });

    connection->start().get();

    ASSERT_FALSE(message_received_event->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[error       ] message_received callback threw an exception: oops\n"), entry);
}

TEST(connection_impl_set_message_received, non_std_exception_from_callback_caught_and_logged)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]()
        mutable {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"throw\"] }",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"release\"] }",
            "{}"
        };

        call_number = std::min(call_number + 1, 3);

        return pplx::task_from_result(responses[call_number]);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    auto message_received_event = std::make_shared<event>();
    connection->set_message_received_string([message_received_event](const utility::string_t &m)
    {
        if (m == _XPLATSTR("throw"))
        {
            throw 42;
        }

        if (m == _XPLATSTR("release"))
        {
            message_received_event->set();
        }
    });

    connection->start().get();

    ASSERT_FALSE(message_received_event->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[error       ] message_received callback threw an unknown exception\n"), entry);
}

TEST(connection_impl_set_message_received, error_logged_for_malformed_payload)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]()
        mutable {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{ 42",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"release\"] }",
            "{}"
        };

        call_number = std::min(call_number + 1, 3);

        return pplx::task_from_result(responses[call_number]);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    auto message_received_event = std::make_shared<event>();
    connection->set_message_received_string([message_received_event](const utility::string_t&)
    {
        // this is called only once because we have just one response with a message
        message_received_event->set();
    });

    connection->start().get();

    ASSERT_FALSE(message_received_event->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[error       ] error occured when parsing response: * Line 1, Column 4 Syntax error: Malformed object literal. response: { 42\n"), entry);
}

TEST(connection_impl_set_message_received, unexpected_responses_logged)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]()
        mutable {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "42",
            "{ \"C\":\"d-486F0DF9-BAO,5|BAV,1|BAW,0\", \"M\" : [\"release\"] }",
            "{}"
        };

        call_number = std::min(call_number + 1, 3);

        return pplx::task_from_result(responses[call_number]);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::info);

    auto message_received_event = std::make_shared<event>();
    connection->set_message_received_string([message_received_event](const utility::string_t&)
    {
        // this is called only once because we have just one response with a message
        message_received_event->set();
    });

    connection->start().get();

    ASSERT_FALSE(message_received_event->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_TRUE(log_entries.size() >= 1);

    auto entry = remove_date_from_log_entry(log_entries[1]);
    ASSERT_EQ(_XPLATSTR("[info        ] unexpected response received from the server: 42\n"), entry);
}

void can_be_set_only_in_disconnected_state(std::function<void(connection_impl *)> callback, const char* expected_exception_message)
{
    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client);

    connection->start().get();

    try
    {
        callback(connection.get());
        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const signalr_exception &e)
    {
        ASSERT_STREQ(expected_exception_message, e.what());
    }
}

TEST(connection_impl_set_configuration, set_message_received_string_callback_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) { connection->set_message_received_string([](const utility::string_t&){}); },
        "cannot set the callback when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_set_configuration, set_message_received_json_callback_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) { connection->set_message_received_json([](const web::json::value&){}); },
        "cannot set the callback when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_set_configuration, set_reconnecting_callback_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) { connection->set_reconnecting([](){}); },
        "cannot set the reconnecting callback when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_set_configuration, set_reconnected_callback_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) { connection->set_reconnected([](){}); },
        "cannot set the reconnected callback when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_set_configuration, set_disconnected_callback_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) { connection->set_disconnected([](){}); },
        "cannot set the disconnected callback when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_set_configuration, set_reconnect_delay_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) { connection->set_reconnect_delay(100); },
        "cannot set reconnect delay when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_stop, stopping_disconnected_connection_is_no_op)
{
    std::shared_ptr<log_writer> writer{ std::make_shared<memory_log_writer>() };
    auto connection = connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::all, writer);
    connection->stop().get();

    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(2U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[info        ] stopping connection\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[info        ] acquired lock in shutdown()\n"), remove_date_from_log_entry(log_entries[1]));
}

TEST(connection_impl_stop, stopping_disconnecting_connection_returns_cancelled_task)
{
    event close_event;
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked")); },
        /* connect function */ [&close_event](const web::uri&) { return pplx::task_from_result(); },
        /* close function */ [&close_event]()
        {
            return pplx::create_task([&close_event]()
            {
                close_event.wait();
            });
        });

    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

    connection->start().get();
    auto stop_task = connection->stop();

    try
    {
        connection->stop().get();
        ASSERT_FALSE(true); // exception expected but not thrown
    }
    catch (const pplx::task_canceled&)
    { }

    close_event.set();
    stop_task.get();

    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(4U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> disconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[3]));
}

TEST(connection_impl_stop, can_start_and_stop_connection)
{
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(4U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> disconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[3]));
}

TEST(connection_impl_stop, can_start_and_stop_connection_multiple_times)
{
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    {
        auto websocket_client = create_test_websocket_client(
            /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
        auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

        connection->start()
            .then([connection]()
        {
            return connection->stop();
        })
        .then([connection]()
        {
            return connection->start();
        }).get();
    }

    auto memory_writer = std::dynamic_pointer_cast<memory_log_writer>(writer);

    // The connection_impl will be destroyed when the last reference to shared_ptr holding is released. This can happen
    // on a different thread in which case the dtor will be invoked on a different thread so we need to wait for this
    // to happen and if it does not the test will fail
    for (int wait_time_ms = 5; wait_time_ms < 100 && memory_writer->get_log_entries().size() < 8; wait_time_ms <<= 1)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = memory_writer->get_log_entries();
    ASSERT_EQ(8U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> disconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[3]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[4]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[5]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> disconnecting\n"), remove_date_from_log_entry(log_entries[6]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[7]));
}

TEST(connection_impl_stop, dtor_stops_the_connection)
{
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    {
        auto websocket_client = create_test_websocket_client(
            /* receive function */ []() 
            {
                std::this_thread::sleep_for(std::chrono::milliseconds(1));
                return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }"));
            });
        auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

        connection->start().get();
    }

    auto memory_writer = std::dynamic_pointer_cast<memory_log_writer>(writer);

    // The connection_impl will be destroyed when the last reference to shared_ptr holding is released. This can happen
    // on a different thread in which case the dtor will be invoked on a different thread so we need to wait for this
    // to happen and if it does not the test will fail
    for (int wait_time_ms = 5; wait_time_ms < 100 && memory_writer->get_log_entries().size() < 4; wait_time_ms <<= 1)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = memory_writer->get_log_entries();
    ASSERT_EQ(4U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> disconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[3]));
}

TEST(connection_impl_stop, stop_cancels_ongoing_start_request)
{
    auto disconnect_completed_event = std::make_shared<event>();

    auto websocket_client = create_test_websocket_client(
        /* receive function */ [disconnect_completed_event]()
        {
            disconnect_completed_event->wait();
            return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }"));
        });

    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};
    auto connection = create_connection(std::make_shared<test_websocket_client>(), writer, trace_level::all);

    auto start_task = connection->start();
    connection->stop().get();
    disconnect_completed_event->set();

    start_task.then([](pplx::task<void> t)
    {
        try
        {
            t.get();
            ASSERT_TRUE(false); // exception expected but not thrown
        }
        catch (const pplx::task_canceled &)
        { }
    }).get();

    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(5U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[info        ] stopping connection\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[info        ] acquired lock in shutdown()\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[info        ] starting the connection has been cancelled.\n"), remove_date_from_log_entry(log_entries[3]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> disconnected\n"), remove_date_from_log_entry(log_entries[4]));
}

TEST(connection_impl_stop, ongoing_start_request_cancelled_if_connection_stopped_before_init_message_received)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"DisconnectTimeout\" : 0.5, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 0.1, \"LongPollDelay\" : 0.0}")
            : _XPLATSTR("");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto websocket_client = create_test_websocket_client(/*receive function*/ []()
    {
        return pplx::task_from_result<std::string>("{}");
    });

    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};
    auto connection = connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::all, writer,
        std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    auto start_task = connection->start();
    connection->stop().get();

    start_task.then([](pplx::task<void> t)
    {
        try
        {
            t.get();
            ASSERT_TRUE(false); // exception expected but not thrown
        }
        catch (const pplx::task_canceled &)
        { }
    }).get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(5U, log_entries.size()) << dump_vector(log_entries);
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[info        ] stopping connection\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[info        ] acquired lock in shutdown()\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[info        ] starting the connection has been cancelled.\n"), remove_date_from_log_entry(log_entries[3]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> disconnected\n"), remove_date_from_log_entry(log_entries[4]));
}

TEST(connection_impl_stop, stop_ignores_exceptions_from_abort_requests)
{
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : url.path() == _XPLATSTR("/start")
                ? _XPLATSTR("{\"Response\":\"started\" }")
                : _XPLATSTR("");

        return url.path() == _XPLATSTR("/abort")
            ? std::unique_ptr<web_request>(new web_request_stub((unsigned short)503, _XPLATSTR("Bad request"), response_body))
            : std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::state_changes,
        writer, std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(4U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> disconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[3]));
}

TEST(connection_impl_stop, stop_invokes_disconnected_callback)
{
    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client);

    auto disconnected_invoked = false;
    connection->set_disconnected([&disconnected_invoked](){ disconnected_invoked = true; });

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    ASSERT_TRUE(disconnected_invoked);
}

TEST(connection_impl_stop, std_exception_for_disconnected_callback_caught_and_logged)
{
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    connection->set_disconnected([](){ throw std::runtime_error("exception from disconnected"); });

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(1U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[error       ] disconnected callback threw an exception: exception from disconnected\n"), remove_date_from_log_entry(log_entries[0]));
}

TEST(connection_impl_stop, exception_for_disconnected_callback_caught_and_logged)
{
    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};

    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    connection->set_disconnected([](){ throw 42; });

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(1U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[error       ] disconnected callback threw an unknown exception\n"), remove_date_from_log_entry(log_entries[0]));
}

//TEST(connection_impl_config, custom_headers_set_in_requests)
//{
//    auto writer = std::shared_ptr<log_writer>{std::make_shared<memory_log_writer>()};
//
//    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
//    {
//        auto response_body =
//            url.path() == _XPLATSTR("/negotiate")
//            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
//            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
//            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
//            : url.path() == _XPLATSTR("/start")
//            ? _XPLATSTR("{\"Response\":\"started\" }")
//            : _XPLATSTR("");
//
//        auto request = new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body);
//        request->on_get_response = [](web_request_stub& request)
//        {
//            auto http_headers = request.m_signalr_client_config.get_http_headers();
//            ASSERT_EQ(1, http_headers.size());
//            ASSERT_EQ(_XPLATSTR("42"), http_headers[_XPLATSTR("Answer")]);
//        };
//
//        return std::unique_ptr<web_request>(request);
//    });
//
//    auto websocket_client = create_test_websocket_client(
//        /* receive function */ []() { return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
//
//    auto connection =
//        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::state_changes,
//        writer, std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));
//
//    signalr::signalr_client_config signalr_client_config{};
//    auto http_headers = signalr_client_config.get_http_headers();
//    http_headers[_XPLATSTR("Answer")] = _XPLATSTR("42");
//    signalr_client_config.set_http_headers(http_headers);
//    connection->set_client_config(signalr_client_config);
//
//    connection->start()
//        .then([connection]()
//    {
//        return connection->stop();
//    }).get();
//
//    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());
//}

TEST(connection_impl_set_config, config_can_be_set_only_in_disconnected_state)
{
    can_be_set_only_in_disconnected_state(
        [](connection_impl* connection) 
        {
            signalr::signalr_client_config signalr_client_config;
            connection->set_client_config(signalr_client_config);
        },"cannot set client config when the connection is not in the disconnected state. current connection state: connected");
}

TEST(connection_impl_change_state, change_state_logs)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto websocket_client = create_test_websocket_client(
        /* receive function */ []() { return pplx::task_from_result(std::string("{\"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

    connection->start().wait();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), entry);
}

TEST(connection_impl_reconnect, can_reconnect)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = std::min(call_number + 1, 3);

            return call_number == 2
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        });

    auto connection = create_connection(websocket_client);
    connection->set_reconnect_delay(100);
    auto reconnected_event = std::make_shared<event>();
    connection->set_reconnected([reconnected_event](){ reconnected_event->set(); });
    connection->start();

    ASSERT_FALSE(reconnected_event->wait(5000));
    ASSERT_EQ(connection_state::connected, connection->get_connection_state());
}

TEST(connection_impl_reconnect, successful_reconnect_state_changes)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = std::min(call_number + 1, 3);

            return call_number == 2
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);
    connection->set_reconnect_delay(100);
    auto reconnected_event = std::make_shared<event>();
    connection->set_reconnected([reconnected_event](){ reconnected_event->set(); });
    connection->start();

    ASSERT_FALSE(reconnected_event->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(4U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> connected\n"), remove_date_from_log_entry(log_entries[3]));
}

TEST(connection_impl_reconnect, connection_stopped_if_reconnecting_failed)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"DisconnectTimeout\" : 0.5, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : url.path() == _XPLATSTR("/start")
                ? _XPLATSTR("{\"Response\":\"started\" }")
                : _XPLATSTR("");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    int call_number = -1;
    int reconnect_invocations = 0;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = std::min(call_number + 1, 3);

            return call_number == 2
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[&reconnect_invocations](const web::uri& url)
        {
            if (url.path() == _XPLATSTR("/reconnect"))
            {
                reconnect_invocations++;
                return pplx::task_from_exception<void>(std::runtime_error("reconnect rejected"));
            }

            return pplx::task_from_result();
        });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::state_changes,
        writer, std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    auto disconnected_event = std::make_shared<event>();
    connection->set_disconnected([disconnected_event](){ disconnected_event->set(); });
    connection->set_reconnect_delay(100);
    connection->start();

    ASSERT_FALSE(disconnected_event->wait(5000));
    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());
    ASSERT_GE(reconnect_invocations, 2);

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(5U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> disconnecting\n"), remove_date_from_log_entry(log_entries[3]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[4]));
}

TEST(connection_impl_reconnect, reconnect_works_if_connection_dropped_during_after_init_and_before_start_successfully_completed)
{
    auto connection_dropped_event = std::make_shared<event>();

    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number, connection_dropped_event]() mutable
    {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{}",
            "{}"
        };

        call_number = std::min(call_number + 1, 2);

        if (call_number == 1)
        {
            connection_dropped_event->set();
            return pplx::task_from_exception<std::string>(std::runtime_error("connection exception"));
        }

        return pplx::task_from_result(responses[call_number]);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);
    connection->set_reconnect_delay(100);
    auto reconnected_event = std::make_shared<event>();
    connection->set_reconnected([reconnected_event](){ reconnected_event->set(); });

    connection->start();

    ASSERT_FALSE(reconnected_event->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(4U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> connected\n"), remove_date_from_log_entry(log_entries[3]));
}

TEST(connection_impl_reconnect, reconnect_cancelled_if_connection_dropped_during_start_and_start_failed)
{
    auto connection_dropped_event = std::make_shared<event>();

    auto web_request_factory = std::make_unique<test_web_request_factory>([&connection_dropped_event](const web::uri& url)
    {
        if (url.path() == _XPLATSTR("/start"))
        {
            connection_dropped_event->wait();
            return std::unique_ptr<web_request>(new web_request_stub((unsigned short)404, _XPLATSTR("Bad request"), _XPLATSTR("")));
        }

        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"DisconnectTimeout\" : 0.5, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : _XPLATSTR("");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number, connection_dropped_event]() mutable
    {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{}",
            "{}"
        };

        call_number = std::min(call_number + 1, 2);

        if (call_number == 1)
        {
            connection_dropped_event->set();
            return pplx::task_from_exception<std::string>(std::runtime_error("connection exception"));
        }

        return pplx::task_from_result(responses[call_number]);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::state_changes | trace_level::info,
        writer, std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    try
    {
        connection->start().get();
        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const std::exception&)
    {
    }

    // Reconnecting happens on its own thread. If the connection is dropped after a successfull /connect but before the
    // entire start sequence completes the reconnect thread is blocked to see if the starts sequence succeded or not.
    // If the start sequence ultimately fails the reconnect logic will not be run - the reconnect thread will exit.
    // However there is no further synchronization between start and reconnect threads so the order in which they will
    // finish is not defined. Note that this does not matter for the user since they don't directly depend on/observe
    // the reconnect in any way. In tests however if the start thread finishes first we can get here while the reconnect
    // thread still has not finished. This would make the test fail so we need to wait until the reconnect thread finishes
    // which will be when it logs a message that it is giving up reconnecting.
    auto memory_writer = std::dynamic_pointer_cast<memory_log_writer>(writer);
    for (int wait_time_ms = 5; wait_time_ms < 100 && memory_writer->get_log_entries().size() < 6; wait_time_ms <<= 1)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = memory_writer->get_log_entries();
    ASSERT_EQ(6U, log_entries.size()) << dump_vector(log_entries);

    auto state_changes = filter_vector(log_entries, _XPLATSTR("[state change]"));
    ASSERT_EQ(2U, state_changes.size()) << dump_vector(log_entries);
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(state_changes[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> disconnected\n"), remove_date_from_log_entry(state_changes[1]));

    auto info_entries = filter_vector(log_entries, _XPLATSTR("[info        ]"));
    ASSERT_EQ(4U, info_entries.size()) << dump_vector(log_entries);
    ASSERT_EQ(_XPLATSTR("[info        ] [websocket transport] connecting to: ws://reconnect_cancelled_if_connection_dropped_during_start_and_start_failed/connect?transport=webSockets&clientProtocol=1.4&connectionToken=A%3D%3D\n"), remove_date_from_log_entry(info_entries[0]));
    ASSERT_EQ(_XPLATSTR("[info        ] connection lost - trying to re-establish connection\n"), remove_date_from_log_entry(info_entries[1]));
    ASSERT_EQ(_XPLATSTR("[info        ] acquired lock before invoking reconnecting callback\n"), remove_date_from_log_entry(info_entries[2]));
    ASSERT_EQ(_XPLATSTR("[info        ] reconnecting cancelled - connection is not in the connected state\n"), remove_date_from_log_entry(info_entries[3]));
}

TEST(connection_impl_reconnect, reconnect_cancelled_when_connection_being_stopped)
{
    std::atomic<bool> connection_started{ false };

    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number, &connection_started]() mutable
    {
        std::string responses[]
        {
            "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
            "{}"
        };

        call_number = std::min(call_number + 1, 1);

        return connection_started
            ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
            : pplx::task_from_result(responses[call_number]);
    },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[](const web::uri& url)
    {
        if (url.path() == _XPLATSTR("/reconnect"))
        {
            return pplx::task_from_exception<void>(std::runtime_error("reconnect rejected"));
        }

        return pplx::task_from_result();
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::all);
    connection->set_reconnect_delay(100);
    event reconnecting_event{};
    connection->set_reconnecting([&reconnecting_event](){ reconnecting_event.set(); });

    connection->start().then([&connection_started](){ connection_started = true; });
    ASSERT_FALSE(reconnecting_event.wait(5000));
    connection->stop().get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    auto state_changes = filter_vector(log_entries, _XPLATSTR("[state change]"));
    ASSERT_EQ(state_changes.size(), 5U);
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(state_changes[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(state_changes[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(state_changes[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> disconnecting\n"), remove_date_from_log_entry(state_changes[3]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(state_changes[4]));

    // there is an iherent race between stop and reconnect to acquire the lock which results in finishing reconnecting
    // in one of two ways and, sometimes, in completing stopping the connection before finishing reconnecting
    for (int wait_time_ms = 5; wait_time_ms < 100; wait_time_ms <<= 1)
    {
        log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
        if ((filter_vector(log_entries, _XPLATSTR("[info        ] reconnecting cancelled - connection is being stopped. line")).size() +
            filter_vector(log_entries, _XPLATSTR("[info        ] reconnecting cancelled - connection was stopped and restarted after reconnecting started")).size()) != 0)
        {
            break;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    ASSERT_EQ(1U,
        filter_vector(log_entries, _XPLATSTR("[info        ] reconnecting cancelled - connection is being stopped. line")).size() +
        filter_vector(log_entries, _XPLATSTR("[info        ] reconnecting cancelled - connection was stopped and restarted after reconnecting started")).size())
            << dump_vector(log_entries);
}

TEST(connection_impl_reconnect, reconnect_cancelled_if_connection_goes_out_of_scope)
{
    std::atomic<bool> connection_started{ false };

    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number, &connection_started]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}"
            };

            call_number = std::min(call_number + 1, 1);

            return connection_started
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[](const web::uri& url)
        {
            if (url.path() == _XPLATSTR("/reconnect"))
            {
                return pplx::task_from_exception<void>(std::runtime_error("reconnect rejected"));
            }

            return pplx::task_from_result();
        });


    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    {
        auto connection = create_connection(websocket_client, writer, trace_level::all);
        connection->set_reconnect_delay(100);
        event reconnecting_event{};
        connection->set_reconnecting([&reconnecting_event](){ reconnecting_event.set(); });

        connection->start().then([&connection_started](){ connection_started = true; });
        ASSERT_FALSE(reconnecting_event.wait(5000));
    }

    // The connection_impl destructor does can be called on a different thread. This is because it is being internally
    // used by tasks as a shared_ptr. As a result the dtor is being called on the thread which released the last reference.
    // Therefore we need to wait block until the dtor has actually completed. Time out would most likely indicate a bug.
    auto memory_writer = std::dynamic_pointer_cast<memory_log_writer>(writer);
    for (int wait_time_ms = 5; wait_time_ms < 10000; wait_time_ms <<= 1)
    {
        if (filter_vector(std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries(),
            _XPLATSTR("[state change] disconnecting -> disconnected")).size() > 0)
        {
            break;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = memory_writer->get_log_entries();
    auto state_changes = filter_vector(log_entries, _XPLATSTR("[state change]"));

    ASSERT_EQ(5U, state_changes.size()) << dump_vector(log_entries);

    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(state_changes[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(state_changes[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(state_changes[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> disconnecting\n"), remove_date_from_log_entry(state_changes[3]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(state_changes[4]));
}

TEST(connection_impl_reconnect, std_exception_for_reconnected_reconnecting_callback_caught_and_logged)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = std::min(call_number + 1, 3);

            return call_number == 2
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::errors);
    connection->set_reconnect_delay(100);
    connection->set_reconnecting([](){ throw std::runtime_error("exception from reconnecting"); });
    auto reconnected_event = std::make_shared<event>();
    connection->set_reconnected([reconnected_event]()
    {
        reconnected_event->set();
        throw std::runtime_error("exception from reconnected");
    });

    connection->start();
    ASSERT_FALSE(reconnected_event->wait(5000));
    ASSERT_EQ(connection_state::connected, connection->get_connection_state());

    auto memory_writer = std::dynamic_pointer_cast<memory_log_writer>(writer);
    for (int wait_time_ms = 5; wait_time_ms < 100 && memory_writer->get_log_entries().size() < 3; wait_time_ms <<= 1)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = memory_writer->get_log_entries();
    ASSERT_EQ(_XPLATSTR("[error       ] reconnecting callback threw an exception: exception from reconnecting\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[error       ] reconnected callback threw an exception: exception from reconnected\n"), remove_date_from_log_entry(log_entries[2]));
}

TEST(connection_impl_reconnect, exception_for_reconnected_reconnecting_callback_caught_and_logged)
{
    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = std::min(call_number + 1, 3);

            return call_number == 2
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection = create_connection(websocket_client, writer, trace_level::errors);
    connection->set_reconnect_delay(100);
    connection->set_reconnecting([](){ throw 42; });
    auto reconnected_event = std::make_shared<event>();
    connection->set_reconnected([reconnected_event]()
    {
        reconnected_event->set();
        throw 42;
    });

    connection->start();
    ASSERT_FALSE(reconnected_event->wait(5000));
    ASSERT_EQ(connection_state::connected, connection->get_connection_state());

    auto memory_writer = std::dynamic_pointer_cast<memory_log_writer>(writer);
    for (int wait_time_ms = 5; wait_time_ms < 100 && memory_writer->get_log_entries().size() < 3; wait_time_ms <<= 1)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = memory_writer->get_log_entries();
    ASSERT_EQ(3U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[error       ] reconnecting callback threw an unknown exception\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[error       ] reconnected callback threw an unknown exception\n"), remove_date_from_log_entry(log_entries[2]));
}

TEST(connection_impl_reconnect, can_stop_connection_from_reconnecting_event)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"DisconnectTimeout\" : 0.5, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : url.path() == _XPLATSTR("/start")
            ? _XPLATSTR("{\"Response\":\"started\" }")
            : _XPLATSTR("");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    int call_number = -1;
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = std::min(call_number + 1, 3);

            return call_number == 2
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[](const web::uri& url)
        {
            if (url.path() == _XPLATSTR("/reconnect"))
            {
                return pplx::task_from_exception<void>(std::runtime_error("reconnect rejected"));
            }

            return pplx::task_from_result();
        });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::state_changes,
        writer, std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    auto stop_event = std::make_shared<event>();
    connection->set_reconnecting([&connection, stop_event]()
    {
        connection->stop()
            .then([stop_event](){ stop_event->set(); });
    });
    connection->set_reconnect_delay(100);
    connection->start();

    ASSERT_FALSE(stop_event->wait(5000));
    ASSERT_EQ(connection_state::disconnected, connection->get_connection_state());

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_EQ(5U, log_entries.size());
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(log_entries[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(log_entries[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(log_entries[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> disconnecting\n"), remove_date_from_log_entry(log_entries[3]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(log_entries[4]));
}


TEST(connection_impl_reconnect, current_reconnect_cancelled_if_another_reconnect_initiated_from_reconnecting_event)
{
    auto web_request_factory = std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"DisconnectTimeout\" : 0.5, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : url.path() == _XPLATSTR("/start")
            ? _XPLATSTR("{\"Response\":\"started\" }")
            : _XPLATSTR("");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    int call_number = -1;
    auto allow_reconnect = std::make_shared<std::atomic<bool>>(false);
    auto websocket_client = create_test_websocket_client(
        /* receive function */ [call_number, allow_reconnect]() mutable
        {
            std::string responses[]
            {
                "{ \"C\":\"x\", \"S\":1, \"M\":[] }",
                "{}",
                "{}",
                "{}"
            };

            call_number = (call_number + 1) % 4;

            return call_number == 2 && !(*allow_reconnect)
                ? pplx::task_from_exception<std::string>(std::runtime_error("connection exception"))
                : pplx::task_from_result(responses[call_number]);
        },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[allow_reconnect](const web::uri& url)
        {
            if (url.path() == _XPLATSTR("/reconnect") && !(*allow_reconnect))
            {
                return pplx::task_from_exception<void>(std::runtime_error("reconnect rejected"));
            }

            return pplx::task_from_result();
        });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::all, writer,
        std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    auto reconnecting_count = 0;
    connection->set_reconnecting([&connection, reconnecting_count, allow_reconnect]() mutable
    {
        if (++reconnecting_count == 1)
        {
            connection->stop().get();
            connection->start().get();
            *allow_reconnect = true;
        }
    });

    event reconnected_event;
    connection->set_reconnected([&reconnected_event]()
    {
        reconnected_event.set();
    });

    connection->set_reconnect_delay(100);
    connection->start();

    ASSERT_FALSE(reconnected_event.wait(5000));
    ASSERT_EQ(connection_state::connected, connection->get_connection_state());

    // There are two racing reconnect attemps happening at the same time. The second one sets the reconnect_event and
    // unblocks the tests so that verification can happen. Sometimes however the second reconnect one finishes before
    // the first and verification fails. We are blocking here until we get the expected message from the first reconnect
    // or timeout. The threads doing reconnects are not observable outside so this is the only way to verify that both
    // reconnect attempts have actually completed.
    for (int wait_time_ms = 5; wait_time_ms < 100; wait_time_ms <<= 1)
    {
        if (filter_vector(std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries(),
            _XPLATSTR("[info        ] reconnecting cancelled - connection was stopped and restarted after reconnecting started")).size() > 0)
        {
            break;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(wait_time_ms));
    }

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_EQ(1U,
        filter_vector(log_entries, _XPLATSTR("[info        ] reconnecting cancelled - connection was stopped and restarted after reconnecting started")).size())
            << dump_vector(log_entries);

    auto state_changes = filter_vector(log_entries, _XPLATSTR("[state change]"));
    ASSERT_EQ(9U, state_changes.size()) << dump_vector(log_entries);
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(state_changes[0]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(state_changes[1]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(state_changes[2]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> disconnecting\n"), remove_date_from_log_entry(state_changes[3]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnecting -> disconnected\n"), remove_date_from_log_entry(state_changes[4]));
    ASSERT_EQ(_XPLATSTR("[state change] disconnected -> connecting\n"), remove_date_from_log_entry(state_changes[5]));
    ASSERT_EQ(_XPLATSTR("[state change] connecting -> connected\n"), remove_date_from_log_entry(state_changes[6]));
    ASSERT_EQ(_XPLATSTR("[state change] connected -> reconnecting\n"), remove_date_from_log_entry(state_changes[7]));
    ASSERT_EQ(_XPLATSTR("[state change] reconnecting -> connected\n"), remove_date_from_log_entry(state_changes[8]));
}

TEST(connection_id, connection_id_is_set_if_start_fails_but_negotiate_request_succeeds)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto websocket_client = create_test_websocket_client(
        /* receive function */ [](){ return pplx::task_from_exception<std::string>(std::runtime_error("should not be invoked")); },
        /* send function */ [](const utility::string_t){ return pplx::task_from_exception<void>(std::runtime_error("should not be invoked"));  },
        /* connect function */[](const web::uri&)
        {
            return pplx::task_from_exception<void>(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed")));
        });

    auto connection = create_connection(websocket_client, writer, trace_level::errors);

    auto start_task = connection->start()
        // this test is not set up to connect successfully so we have to observe exceptions otherwise
        // other tests may fail due to an unobserved exception from the outstanding start task
        .then([](pplx::task<void> start_task)
        {
            try
            {
                start_task.get();
            }
            catch (...)
            {
            }
        });

    ASSERT_EQ(_XPLATSTR(""), connection->get_connection_id());
    start_task.get();
    ASSERT_EQ(_XPLATSTR("f7707523-307d-4cba-9abf-3eef701241e8"), connection->get_connection_id());
}

TEST(connection_id, can_get_connection_id_when_connection_in_connected_state)
{
    auto writer = std::shared_ptr<log_writer>{ std::make_shared<memory_log_writer>() };

    auto websocket_client = create_test_websocket_client(
        /* receive function */ [](){ return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

    utility::string_t connection_id;
    connection->start()
        .then([connection, &connection_id]()
        mutable {
            connection_id = connection->get_connection_id();
            return connection->stop();
        }).get();

    ASSERT_EQ(_XPLATSTR("f7707523-307d-4cba-9abf-3eef701241e8"), connection_id);
}

TEST(connection_id, can_get_connection_id_after_connection_has_stopped)
{
    auto writer = std::shared_ptr<log_writer>{ std::make_shared<memory_log_writer>() };

    auto websocket_client = create_test_websocket_client(
        /* receive function */ [](){ return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });
    auto connection = create_connection(websocket_client, writer, trace_level::state_changes);

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    ASSERT_EQ(_XPLATSTR("f7707523-307d-4cba-9abf-3eef701241e8"), connection->get_connection_id());
}

TEST(connection_id, connection_id_reset_when_starting_connection)
{
    auto fail_http_requests = false;

    auto writer = std::shared_ptr<log_writer>{ std::make_shared<memory_log_writer>() };

    auto websocket_client = create_test_websocket_client(
        /* receive function */ [](){ return pplx::task_from_result(std::string("{ \"C\":\"x\", \"S\":1, \"M\":[] }")); });

    auto web_request_factory = std::make_unique<test_web_request_factory>([&fail_http_requests](const web::uri &url) -> std::unique_ptr<web_request>
    {
        if (!fail_http_requests) {
            auto response_body =
                url.path() == _XPLATSTR("/negotiate") || url.path() == _XPLATSTR("/signalr/negotiate")
                ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
                _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 10.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
                _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
                : url.path() == _XPLATSTR("/start") || url.path() == _XPLATSTR("/signalr/start")
                    ? _XPLATSTR("{\"Response\":\"started\" }")
                    : _XPLATSTR("");

            return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
        }

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)500, _XPLATSTR("Internal Server Error"), _XPLATSTR("")));
    });

    auto connection =
        connection_impl::create(create_uri(), _XPLATSTR(""), trace_level::none, std::make_shared<trace_log_writer>(),
            std::move(web_request_factory), std::make_unique<test_transport_factory>(websocket_client));

    connection->start()
        .then([connection]()
        {
            return connection->stop();
        }).get();

    ASSERT_EQ(_XPLATSTR("f7707523-307d-4cba-9abf-3eef701241e8"), connection->get_connection_id());

    fail_http_requests = true;

    connection->start()
        // this test is not set up to connect successfully so we have to observe exceptions otherwise
        // other tests may fail due to an unobserved exception from the outstanding start task
        .then([](pplx::task<void> start_task)
        {
            try
            {
                start_task.get();
            }
            catch (...)
            {
            }
        }).get();

    ASSERT_EQ(_XPLATSTR(""), connection->get_connection_id());
}
