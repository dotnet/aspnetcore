// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "trace_log_writer.h"
#include "test_utils.h"
#include "memory_log_writer.h"
#include "test_transport_factory.h"
#include "hub_connection_impl.h"
#include "signalrclient/signalr_exception.h"
#include "signalrclient/hub_connection.h"

using namespace signalr;

TEST(on, event_name_must_not_be_empty_string)
{
    hub_connection hub_connection{ _XPLATSTR("") };
    try
    {
        hub_connection.on(_XPLATSTR(""), [](const json::value&){});

        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const std::invalid_argument& e)
    {
        ASSERT_STREQ("event_name cannot be empty", e.what());
    }
}

TEST(on, cannot_register_multiple_handlers_for_event)
{
    hub_connection hub_connection{ _XPLATSTR("") };
    hub_connection.on(_XPLATSTR("ping"), [](const json::value&){});

    try
    {
        hub_connection.on(_XPLATSTR("ping"), [](const json::value&){});
        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const signalr_exception& e)
    {
        ASSERT_STREQ("an action for this event has already been registered. event name: ping", e.what());
    }
}

TEST(on, cannot_register_handler_if_connection_not_in_disconnected_state)
{
    try
    {
        auto websocket_client = create_test_websocket_client(
            /* receive function */ []() { return pplx::task_from_result(std::string("{ }\x1e")); });
        auto hub_connection = hub_connection_impl::create(_XPLATSTR("http://fakeuri"), _XPLATSTR(""), trace_level::all,
            std::make_shared<trace_log_writer>(), create_test_web_request_factory(),
            std::make_unique<test_transport_factory>(websocket_client));

        hub_connection->start().get();

        hub_connection->on(_XPLATSTR("myfunc"), [](const web::json::value&){});

        ASSERT_TRUE(false); // exception expected but not thrown
    }
    catch (const signalr_exception& e)
    {
        ASSERT_STREQ("can't register a handler if the connection is in a disconnected state", e.what());
    }
}

TEST(invoke_json, invoke_throws_when_the_underlying_connection_is_not_valid)
{
    hub_connection hub_connection{ _XPLATSTR("") };

    try
    {
        hub_connection.invoke(_XPLATSTR("method")).get();
        ASSERT_TRUE(true); // exception expected but not thrown
    }
    catch (const signalr_exception& e)
    {
        ASSERT_STREQ("cannot send data when the connection is not in the connected state. current connection state: disconnected", e.what());
    }
}

TEST(invoke_void, send_throws_when_the_underlying_connection_is_not_valid)
{
    hub_connection hub_connection{ _XPLATSTR("") };

    try
    {
        hub_connection.send(_XPLATSTR("method")).get();
        ASSERT_TRUE(true); // exception expected but not thrown
    }
    catch (const signalr_exception& e)
    {
        ASSERT_STREQ("cannot send data when the connection is not in the connected state. current connection state: disconnected", e.what());
    }
}
