// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_utils.h"
#include "trace_log_writer.h"
#include "test_websocket_client.h"
#include "websocket_transport.h"
#include "memory_log_writer.h"
#include <future>

using namespace signalr;

TEST(websocket_transport_connect, connect_connects_and_starts_receive_loop)
{
    auto connect_called = false;
    auto receive_called = std::make_shared<event>();
    auto client = std::make_shared<test_websocket_client>();

    client->set_connect_function([&connect_called](const std::string&, std::function<void(std::exception_ptr)> callback)
    {
        connect_called = true;
        callback(nullptr);
    });

    client->set_receive_function([receive_called](std::function<void(std::string, std::exception_ptr)> callback)
    {
        receive_called->set();
        callback("", nullptr);
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level::info));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org/connect?param=42", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });

    mre.get();

    ASSERT_TRUE(connect_called);
    ASSERT_FALSE(receive_called->wait(5000));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);
    ASSERT_EQ("[info        ] [websocket transport] connecting to: ws://fakeuri.org/connect?param=42\n", entry);
}

TEST(websocket_transport_connect, connect_propagates_exceptions)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_connect_function([](const std::string&, std::function<void(std::exception_ptr)> callback)
    {
        callback(std::make_exception_ptr(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed"))));
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    try
    {
        auto mre = manual_reset_event<void>();
        ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
        {
            mre.set(exception);
        });
        mre.get();
        ASSERT_TRUE(false);
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("connecting failed"), utility::conversions::to_string_t(e.what()));
    }
}

TEST(websocket_transport_connect, connect_logs_exceptions)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_connect_function([](const std::string&, std::function<void(std::exception_ptr)> callback)
    {
        callback(std::make_exception_ptr(web::websockets::client::websocket_exception(_XPLATSTR("connecting failed"))));
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());
    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level::errors));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    try
    {
        mre.get();
    }
    catch (...) {}

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_FALSE(log_entries.empty());

    auto entry = remove_date_from_log_entry(log_entries[0]);

    ASSERT_EQ(
        "[error       ] [websocket transport] exception when connecting to the server: connecting failed\n",
        entry);
}

TEST(websocket_transport_connect, cannot_call_connect_on_already_connected_transport)
{
    auto client = std::make_shared<test_websocket_client>();
    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    try
    {
        ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
        {
            mre.set(exception);
        });
        mre.get();
        ASSERT_TRUE(false);
    }
    catch (const std::exception &e)
    {
        ASSERT_EQ(_XPLATSTR("transport already connected"), utility::conversions::to_string_t(e.what()));
    }
}

TEST(websocket_transport_connect, can_connect_after_disconnecting)
{
    auto client = std::make_shared<test_websocket_client>();
    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();
}

TEST(websocket_transport_send, send_creates_and_sends_websocket_messages)
{
    bool send_called = false;

    auto client = std::make_shared<test_websocket_client>();

    client->set_send_function([&send_called](const std::string&, std::function<void(std::exception_ptr)> callback)
    {
        send_called = true;
        callback(nullptr);
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://url", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->send("ABC", [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ASSERT_TRUE(send_called);
}

TEST(websocket_transport_disconnect, disconnect_closes_websocket)
{
    bool close_called = false;

    auto client = std::make_shared<test_websocket_client>();

    client->set_close_function([&close_called](std::function<void(std::exception_ptr)> callback)
    {
        close_called = true;
        callback(nullptr);
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://url", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ASSERT_TRUE(close_called);
}

TEST(websocket_transport_stop, propogates_exception_from_close)
{
    auto client = std::make_shared<test_websocket_client>();

    bool close_called = false;
    client->set_close_function([&close_called](std::function<void(std::exception_ptr)> callback)
    {
        close_called = true;
        callback(std::make_exception_ptr(std::exception()));
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://url", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    try
    {
        mre.get();
        ASSERT_TRUE(false);
    }
    catch (...) { }

    ASSERT_TRUE(close_called);
}

TEST(websocket_transport_disconnect, disconnect_logs_exceptions)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_close_function([](std::function<void(std::exception_ptr)> callback)
    {
        callback(std::make_exception_ptr(web::websockets::client::websocket_exception(_XPLATSTR("connection closing failed"))));
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level::errors));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://url", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });

    try
    {
        mre.get();
        ASSERT_TRUE(false);
    }
    catch (...) {}

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_FALSE(log_entries.empty());

    // disconnect cancels the receive loop by setting the cancellation token source to cancelled which results in writing
    // to the log. Exceptions from close are also logged but this happens on a different thread. As a result the order
    // of messages in the log is not deterministic and therefore we just use the "contains" idiom to find the message.
    ASSERT_NE(std::find_if(log_entries.begin(), log_entries.end(), [](const std::string& entry)
        {
            return remove_date_from_log_entry(entry) ==
                "[error       ] [websocket transport] exception when closing websocket: connection closing failed\n";
        }),
        log_entries.end());
}

TEST(websocket_transport_disconnect, receive_not_called_after_disconnect)
{
    auto client = std::make_shared<test_websocket_client>();

    pplx::task_completion_event<std::string> receive_task_tce;
    pplx::task_completion_event<void> receive_task_started_tce;

    // receive_task_tce is captured by reference since we assign it a new value after the first disconnect. This is
    // safe here because we are blocking on disconnect and therefore we won't get into a state where we would be using
    // an invalid reference because the tce went out of scope and was destroyed.
    client->set_close_function([&receive_task_tce](std::function<void(std::exception_ptr)> callback)
    {
        // unblock receive
        receive_task_tce.set(std::string(""));
        callback(nullptr);
    });

    int num_called = 0;
    client->set_receive_function([&receive_task_tce, &receive_task_started_tce, &num_called](std::function<void(std::string, std::exception_ptr)> callback)
    {
        num_called++;
        receive_task_started_tce.set();
        pplx::create_task(receive_task_tce)
            .then([callback](pplx::task<std::string> prev)
        {
            prev.get();
            callback("", nullptr);
        });
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr)
    {
        mre.set();
    });
    mre.get();

    pplx::create_task(receive_task_started_tce).get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    receive_task_tce = pplx::task_completion_event<std::string>();
    receive_task_started_tce = pplx::task_completion_event<void>();

    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    pplx::create_task(receive_task_started_tce).get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ASSERT_EQ(2, num_called);
}

TEST(websocket_transport_disconnect, disconnect_is_no_op_if_transport_not_started)
{
    auto client = std::make_shared<test_websocket_client>();

    auto close_called = false;

    client->set_close_function([&close_called](std::function<void(std::exception_ptr)> callback)
    {
        close_called = true;
        callback(nullptr);
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ASSERT_FALSE(close_called);
}

TEST(websocket_transport_disconnect, exceptions_from_outstanding_receive_task_observed_after_websocket_transport_disconnected)
{
    auto client = std::make_shared<test_websocket_client>();

    auto receive_event = std::make_shared<event>();
    client->set_receive_function([receive_event](std::function<void(std::string, std::exception_ptr)> callback)
    {
        pplx::create_task([receive_event, callback]()
        {
            receive_event->wait();
            callback("", std::make_exception_ptr(std::runtime_error("exception from receive")));
        });
    });

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    ws_transport->stop([&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    // at this point the cancellation token that closes the receive loop is set to cancelled so
    // we can unblock the the receive task which throws an exception that should be observed otwherwise the test will crash
    receive_event->set();
}

template<typename T>
void receive_loop_logs_exception_runner(const T& e, const std::string& expected_message, trace_level trace_level);

TEST(websocket_transport_receive_loop, receive_loop_logs_websocket_exceptions)
{
    receive_loop_logs_exception_runner(
        web::websockets::client::websocket_exception(_XPLATSTR("receive failed")),
        "[error       ] [websocket transport] error receiving response from websocket: receive failed\n",
        trace_level::errors);
}

TEST(websocket_transport_receive_loop, receive_loop_logs_if_receive_task_canceled)
{
    receive_loop_logs_exception_runner(
        pplx::task_canceled("canceled"),
        "[error       ] [websocket transport] error receiving response from websocket: canceled\n",
        trace_level::errors);
}

TEST(websocket_transport_receive_loop, receive_loop_logs_std_exception)
{
    receive_loop_logs_exception_runner(
        std::runtime_error("exception"),
        "[error       ] [websocket transport] error receiving response from websocket: exception\n",
        trace_level::errors);
}

template<typename T>
void receive_loop_logs_exception_runner(const T& e, const std::string& expected_message, trace_level trace_level)
{
    event receive_event;
    auto client = std::make_shared<test_websocket_client>();

    client->set_receive_function([&receive_event, &e](std::function<void(std::string, std::exception_ptr)> callback)
    {
        callback("", std::make_exception_ptr(e));
        receive_event.set();
    });

    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(writer, trace_level));

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://url", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    receive_event.wait();

    // this is race'y but there is nothing we can block on
    std::this_thread::sleep_for(std::chrono::milliseconds(10));

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_NE(std::find_if(log_entries.begin(), log_entries.end(),
        [&expected_message](std::string entry) { return remove_date_from_log_entry(entry) == expected_message; }),
        log_entries.end()) << dump_vector(log_entries);
}

TEST(websocket_transport_receive_loop, process_response_callback_called_when_message_received)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_receive_function([](std::function<void(std::string, std::exception_ptr)> callback)
    {
        callback("msg", nullptr);
    });

    auto process_response_event = std::make_shared<event>();
    auto msg = std::make_shared<std::string>();

    auto process_response = [msg, process_response_event](const std::string& message, std::exception_ptr exception)
    {
        ASSERT_FALSE(exception);
        *msg = message;
        process_response_event->set();
    };

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));
    ws_transport->on_receive(process_response);

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    process_response_event->wait(1000);

    ASSERT_EQ(std::string("msg"), *msg);
}

TEST(websocket_transport_receive_loop, error_callback_called_when_exception_thrown)
{
    auto client = std::make_shared<test_websocket_client>();
    client->set_receive_function([](std::function<void(std::string, std::exception_ptr)> callback)
    {
        callback("", std::make_exception_ptr(std::runtime_error("error")));
    });

    auto close_invoked = std::make_shared<bool>(false);
    client->set_close_function([close_invoked](std::function<void(std::exception_ptr)> callback)
    {
        *close_invoked = true;
        callback(nullptr);
    });

    auto error_event = std::make_shared<event>();
    auto exception_msg = std::make_shared<std::string>();

    auto error_callback = [exception_msg, error_event](std::exception_ptr exception)
    {
        try
        {
            std::rethrow_exception(exception);
        }
        catch (const std::exception& e)
        {
            *exception_msg = e.what();
        }
        error_event->set();
    };

    auto ws_transport = websocket_transport::create([&](){ return client; }, logger(std::make_shared<trace_log_writer>(), trace_level::none));
    ws_transport->on_close(error_callback);

    auto mre = manual_reset_event<void>();
    ws_transport->start("ws://fakeuri.org", transfer_format::text, [&mre](std::exception_ptr exception)
    {
        mre.set(exception);
    });
    mre.get();

    error_event->wait(1000);

    ASSERT_STREQ("error", exception_msg->c_str());
    ASSERT_TRUE(*close_invoked);
}

TEST(websocket_transport_get_transport_type, get_transport_type_returns_websockets)
{
    auto ws_transport = websocket_transport::create(
        [](){ return std::make_shared<default_websocket_client>(); },
        logger(std::make_shared<trace_log_writer>(), trace_level::none));

    ASSERT_EQ(transport_type::websockets, ws_transport->get_transport_type());
}
