// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include <string>
#include "cpprest/details/basic_types.h"
#include "cpprest/json.h"
#include "connection.h"
#include "hub_connection.h"

extern std::string url;

TEST(connection_tests, connection_status_start_stop)
{
    auto conn = std::make_shared<signalr::connection>(url + "raw-connection");

    conn->start().get();
    ASSERT_EQ(conn->get_connection_state(), signalr::connection_state::connected);

    conn->stop().get();
    ASSERT_EQ(conn->get_connection_state(), signalr::connection_state::disconnected);

    conn->start().get();
    ASSERT_EQ(conn->get_connection_state(), signalr::connection_state::connected);
}

TEST(connection_tests, send_message)
{
    auto conn = std::make_shared<signalr::connection>(url + "raw-connection");
    auto message = std::make_shared<std::string>();
    auto received_event = std::make_shared<signalr::event>();

    conn->set_message_received([message, received_event](const std::string& payload)
    {
        *message = payload;
        received_event->set();
    });

    conn->start().then([conn]()
    {
        web::json::value obj;
        obj[U("type")] = web::json::value::number(0);
        obj[U("value")] = web::json::value::string(U("test"));
        return conn->send(utility::conversions::to_utf8string(obj.serialize()));

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    ASSERT_EQ(*message, "{\"data\":\"test\",\"type\":0}");
}

TEST(connection_tests, send_message_after_connection_restart)
{
    auto conn = std::make_shared<signalr::connection>(url + "raw-connection");
    auto message = std::make_shared<std::string>();
    auto received_event = std::make_shared<signalr::event>();

    conn->set_message_received([message, received_event](const std::string& payload)
    {
        *message = payload;
        received_event->set();
    });

    conn->start().get();

    conn->stop().get();

    conn->start().then([conn]()
    {
        web::json::value obj;
        obj[U("type")] = web::json::value::number(0);
        obj[U("value")] = web::json::value::string(U("test"));
        return conn->send(utility::conversions::to_utf8string(obj.serialize()));

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    ASSERT_EQ(*message, "{\"data\":\"test\",\"type\":0}");
}

TEST(connection_tests, connection_id_start_stop)
{
    auto conn = std::make_shared<signalr::connection>(url + "raw-connection");

    ASSERT_EQ("", conn->get_connection_id());

    conn->start().get();
    auto connection_id = conn->get_connection_id();
    ASSERT_NE(connection_id, "");

    conn->stop().get();
    ASSERT_EQ(conn->get_connection_id(), connection_id);

    conn->start().get();
    ASSERT_NE(conn->get_connection_id(), "");
    ASSERT_NE(conn->get_connection_id(), connection_id);
}
