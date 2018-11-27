// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_utils.h"
#include "trace_log_writer.h"
#include "test_websocket_client.h"
#include "websocket_transport.h"
#include "memory_log_writer.h"

using namespace signalr;

TEST(websocket_transport_connect, connect_connects_and_starts_receive_loop)
{
    auto connect_called = false;
    auto receive_called = std::make_shared<bool>(false);
    auto client = std::make_shared<test_websocket_client>();

    client->set_connect_function([&connect_called](const web::uri &) -> pplx::task<void>
    {
        connect_called = true;
        return pplx::task_from_result();
    });

    client->set_receive_function([receive_called]()->pplx::task<std::string>
    {
        *receive_called = true;
        return pplx::task_from_result(std::string(""));
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level::info),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org/connect?param=42")).get();

    ASSERT_TRUE(connect_called);
    ASSERT_TRUE(*receive_called);

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ(_XPLATSTR("[info        ] [websocket transport] connecting to: ws://fakeuri.org/connect?param=42\n"), entry);
}

TEST(websocket_transport_connect, connect_propagates_exceptions)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_connect_function([](const web::uri &)->pplx::task<void>
    {
        return pplx::task_from_exception<void>(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed")));
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    try
    {
        ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("connecting failed"), utility::conversions::to_string_t(e.what()));
    }
}

TEST(websocket_transport_connect, connect_logs_exceptions)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_connect_function([](const web::uri &) -> pplx::task<void>
    {
        return pplx::task_from_exception<void>(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed")));
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level::errors),
        [](const utility::string_t&){}, [](const std::exception&){});

    try
    {
        ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).wait();
    }
    catch (...)
    { }

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);

    ASSERT_EQ(
        _XPLATSTR("[error       ] [websocket transport] exception when connecting to the server: connecting failed\n"),
        entry);
}

TEST(websocket_transport_connect, cannot_call_connect_on_already_connected_transport)
{
    auto client = std::make_shared<test_websocket_client>();
    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).wait();

    try
    {
        ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).wait();
        ASSERT_TRUE(false); // exception not thrown
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("transport already connected"), utility::conversions::to_string_t(e.what()));
    }
}

TEST(websocket_transport_connect, can_connect_after_disconnecting)
{
    auto client = std::make_shared<test_websocket_client>();
    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();
    ws_transport->disconnect().get();
    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();
    // shouldn't throw or crash
}

TEST(websocket_transport_send, send_creates_and_sends_websocket_messages)
{
    bool send_called = false;

    auto client = std::make_shared<test_websocket_client>();

    client->set_send_function([&send_called](const utility::string_t&) -> pplx::task<void>
    {
        send_called = true;
        return pplx::task_from_result();
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://url"))
        .then([ws_transport](){ return ws_transport->send(_XPLATSTR("ABC")); })
        .wait();

    ASSERT_TRUE(send_called);
}

TEST(websocket_transport_disconnect, disconnect_closes_websocket)
{
    bool close_called = false;

    auto client = std::make_shared<test_websocket_client>();

    client->set_close_function([&close_called]() -> pplx::task<void>
    {
        close_called = true;
        return pplx::task_from_result();
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://url"))
        .then([ws_transport]()
        {
            return ws_transport->disconnect();
        }).get();

    ASSERT_TRUE(close_called);
}

TEST(websocket_transport_disconnect, disconnect_does_not_throw)
{
    auto client = std::make_shared<test_websocket_client>();

    bool close_called = false;
    client->set_close_function([&close_called]() -> pplx::task<void>
    {
        close_called = true;
        return pplx::task_from_exception<void>(std::exception());
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://url"))
        .then([ws_transport]()
    {
        return ws_transport->disconnect();
    }).get();

    ASSERT_TRUE(close_called);
}

TEST(websocket_transport_disconnect, disconnect_logs_exceptions)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_close_function([]()->pplx::task<void>
    {
        return pplx::task_from_exception<void>(web::websockets::client::websocket_exception(_XPLATSTR("connection closing failed")));
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level::errors),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://url"))
        .then([ws_transport]()
        {
            return ws_transport->disconnect();
        }).get();

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_FALSE(log_entries.empty());

    // disconnect cancels the receive loop by setting the cancellation token source to cancelled which results in writing
    // to the log. Exceptions from close are also logged but this happens on a different thread. As a result the order
    // of messages in the log is not deterministic and therefore we just use the "contains" idiom to find the message.
    ASSERT_NE(std::find_if(log_entries.begin(), log_entries.end(), [](const utility::string_t& entry)
        {
            return remove_date_from_log_entry(entry) ==
                _XPLATSTR("[error       ] [websocket transport] exception when closing websocket: connection closing failed\n");
        }),
        log_entries.end());
}

TEST(websocket_transport_disconnect, receive_not_called_after_disconnect)
{
    auto client = std::make_shared<test_websocket_client>();

    pplx::task_completion_event<std::string> receive_task_tce;

    // receive_task_tce is captured by reference since we assign it a new value after the first disconnect. This is
    // safe here because we are blocking on disconnect and therefore we won't get into a state were we would be using
    // an invalid reference because the tce went out of scope and was destroyed.
    client->set_close_function([&receive_task_tce]()
    {
        // unblock receive
        receive_task_tce.set(std::string(""));
        return pplx::task_from_result();
    });

    int num_called = 0;

    client->set_receive_function([&receive_task_tce, &num_called]() -> pplx::task<std::string>
    {
        num_called++;
        return pplx::create_task(receive_task_tce);
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();
    ws_transport->disconnect().get();

    receive_task_tce = pplx::task_completion_event<std::string>();
    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();
    ws_transport->disconnect().get();

    ASSERT_EQ(2, num_called);
}

TEST(websocket_transport_disconnect, disconnect_is_no_op_if_transport_not_started)
{
    auto client = std::make_shared<test_websocket_client>();

    auto close_called = false;

    client->set_close_function([&close_called]()
    {
        close_called = true;
        return pplx::task_from_result();
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->disconnect().get();

    ASSERT_FALSE(close_called);
}

TEST(websocket_transport_disconnect, exceptions_from_outstanding_receive_task_observed_after_websocket_transport_disconnected)
{
    auto client = std::make_shared<test_websocket_client>();

    auto receive_event = std::make_shared<event>();
    client->set_receive_function([receive_event]()
    {
        return pplx::create_task([receive_event]()
        {
            receive_event->wait();
            return pplx::task_from_exception<std::string>(std::runtime_error("exception from receive"));
        });
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();
    ws_transport->disconnect().get();

    // at this point the cancellation token that closes the receive loop is set to cancelled so
    // we can unblock the the receive task which throws an exception that should be observed otwherwise the test will crash
    receive_event->set();
}

template<typename T>
void receive_loop_logs_exception_runner(const T& e, const utility::string_t& expected_message, trace_level trace_level);

TEST(websocket_transport_receive_loop, receive_loop_logs_websocket_exceptions)
{
    receive_loop_logs_exception_runner(
        web::websockets::client::websocket_exception(_XPLATSTR("receive failed")),
        _XPLATSTR("[error       ] [websocket transport] error receiving response from websocket: receive failed\n"),
        trace_level::errors);
}

TEST(websocket_transport_receive_loop, receive_loop_logs_if_receive_task_cancelled)
{
    receive_loop_logs_exception_runner(
        pplx::task_canceled("cancelled"),
        _XPLATSTR("[info        ] [websocket transport] receive task cancelled.\n"),
        trace_level::info);
}

TEST(websocket_transport_receive_loop, receive_loop_logs_std_exception)
{
    receive_loop_logs_exception_runner(
        std::runtime_error("exception"),
        _XPLATSTR("[error       ] [websocket transport] error receiving response from websocket: exception\n"),
        trace_level::errors);
}

template<typename T>
void receive_loop_logs_exception_runner(const T& e, const utility::string_t& expected_message, trace_level trace_level)
{
    event receive_event;
    auto client = std::make_shared<test_websocket_client>();

    client->set_receive_function([&receive_event, &e]()->pplx::task<std::string>
    {
        receive_event.set();
        return pplx::task_from_exception<std::string>(e);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level),
        [](const utility::string_t&){}, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://url"))
        .then([&receive_event]()
    {
        receive_event.wait();
    }).get();

    // this is race'y but there is nothing we can block on
    std::this_thread::sleep_for(std::chrono::milliseconds(10));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_NE(std::find_if(log_entries.begin(), log_entries.end(),
        [&expected_message](utility::string_t entry) { return remove_date_from_log_entry(entry) == expected_message; }),
        log_entries.end());
}

TEST(websocket_transport_receive_loop, process_response_callback_called_when_message_received)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_receive_function([]() -> pplx::task<std::string>
    {
        return pplx::task_from_result(std::string("msg"));
    });

    auto process_response_event = std::make_shared<event>();
    auto msg = std::make_shared<utility::string_t>();

    auto process_response = [msg, process_response_event](const utility::string_t& message)
    {
        *msg = message;
        process_response_event->set();
    };

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        process_response, [](const std::exception&){});

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();

    process_response_event->wait(1000);

    ASSERT_EQ(utility::string_t(_XPLATSTR("msg")), *msg);
}

TEST(websocket_transport_receive_loop, error_callback_called_when_exception_thrown)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_receive_function([]()
    {
        return pplx::task_from_exception<std::string>(std::runtime_error("error"));
    });

    auto close_invoked = std::make_shared<bool>(false);
    client->set_close_function([close_invoked]()
    {
        *close_invoked = true;
        return pplx::task_from_result();
    });

    auto error_event = std::make_shared<event>();
    auto exception_msg = std::make_shared<std::string>();

    auto error_callback = [exception_msg, error_event](const std::exception& e)
    {
        *exception_msg = e.what();
        error_event->set();
    };

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, error_callback);

    ws_transport->connect(_XPLATSTR("ws://fakeuri.org")).get();

    error_event->wait(1000);

    ASSERT_STREQ("error", exception_msg->c_str());
    ASSERT_TRUE(*close_invoked);
}

TEST(websocket_transport_get_transport_type, get_transport_type_returns_websockets)
{
    auto ws_transport = websocket_transport::create(
        [](){ return std::make_shared<default_websocket_client>(); },
        logger(std::make_shared<trace_log_writer>(), trace_level::none),
        [](const utility::string_t&){}, [](const std::exception&){});

    ASSERT_EQ(transport_type::websockets, ws_transport->get_transport_type());
}