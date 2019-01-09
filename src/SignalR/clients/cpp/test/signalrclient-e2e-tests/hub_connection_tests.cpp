// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include <string>
#include "cpprest/details/basic_types.h"
#include "cpprest/json.h"
#include "connection.h"
#include "hub_connection.h"
#include "signalr_exception.h"

extern utility::string_t url;

TEST(hub_connection_tests, connection_status_start_stop_start)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
    auto weak_hub_conn = std::weak_ptr<signalr::hub_connection>(hub_conn);

    hub_conn->start().get();
    ASSERT_EQ(hub_conn->get_connection_state(), signalr::connection_state::connected);

    hub_conn->stop().get();
    ASSERT_EQ(hub_conn->get_connection_state(), signalr::connection_state::disconnected);

    hub_conn->start().get();
    ASSERT_EQ(hub_conn->get_connection_state(), signalr::connection_state::connected);
}

TEST(hub_connection_tests, send_message)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url + U("custom"), U(""), signalr::trace_level::all, nullptr);
    auto message = std::make_shared<utility::string_t>();
    auto received_event = std::make_shared<signalr::event>();

    hub_conn->on(U("sendString"), [message, received_event](const web::json::value& arguments)
    {
        *message = arguments.serialize();
        received_event->set();
    });

    hub_conn->start().then([&hub_conn]()
    {
        web::json::value obj{};
        obj[0] = web::json::value(U("test"));

        return hub_conn->send(U("invokeWithString"), obj);

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    ASSERT_EQ(*message, U("[\"Send: test\"]"));
}

TEST(hub_connection_tests, send_message_return)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);

    auto test = hub_conn->start().then([&hub_conn]()
    {
        web::json::value obj{};
        obj[0] = web::json::value(U("test"));

        return hub_conn->invoke(U("returnString"), obj);

    }).get();

    ASSERT_EQ(test.serialize(), U("\"test\""));
}

TEST(hub_connection_tests, send_message_after_connection_restart)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
    auto message = std::make_shared<utility::string_t>();
    auto received_event = std::make_shared<signalr::event>();

    hub_conn->on(U("sendString"), [message, received_event](const web::json::value& arguments)
    {
        *message = arguments.serialize();
        received_event->set();
    });

    hub_conn->start().get();

    hub_conn->stop().get();

    hub_conn->start().then([&hub_conn]()
    {
        web::json::value obj{};
        obj[0] = web::json::value(U("test"));

        return hub_conn->send(U("invokeWithString"), obj);

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    ASSERT_EQ(*message, U("[\"Send: test\"]"));
}

TEST(hub_connection_tests, send_message_empty_param)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
    auto message = std::make_shared<utility::string_t>();
    auto received_event = std::make_shared<signalr::event>();

    hub_conn->on(U("sendString"), [message, received_event](const web::json::value& arguments)
    {
        *message = arguments.serialize();
        received_event->set();
    });

    hub_conn->start().then([&hub_conn]()
    {
        return hub_conn->invoke(U("invokeWithEmptyParam"));

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    ASSERT_EQ(*message, U("[\"Send\"]"));
}

TEST(hub_connection_tests, send_message_primitive_params)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
    auto message = std::make_shared<utility::string_t>();
    auto received_event = std::make_shared<signalr::event>();

    hub_conn->on(U("sendPrimitiveParams"), [message, received_event](const web::json::value& arguments)
    {
        *message = arguments.serialize();
        received_event->set();
    });

    hub_conn->start().then([&hub_conn]()
    {
        web::json::value obj{};
        obj[0] = web::json::value(5);
        obj[1] = web::json::value(21.05);
        obj[2] = web::json::value(8.999999999);
        obj[3] = web::json::value(true);
        obj[4] = web::json::value('a');
        return hub_conn->send(U("invokeWithPrimitiveParams"), obj);

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    web::json::value obj{};
    obj[0] = web::json::value(6);
    obj[1] = web::json::value(22.05);
    obj[2] = web::json::value(9.999999999);
    obj[3] = web::json::value(true);
    obj[4] = web::json::value::string(U("a"));

    ASSERT_EQ(*message, obj.serialize());
}

TEST(hub_connection_tests, send_message_complex_type)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
    auto message = std::make_shared<utility::string_t>();
    auto received_event = std::make_shared<signalr::event>();

    hub_conn->on(U("sendComplexType"), [message, received_event](const web::json::value& arguments)
    {
        *message = arguments.serialize();
        received_event->set();
    });

    hub_conn->start().then([&hub_conn]()
    {
        web::json::value obj{};
        web::json::value person;
        web::json::value address;
        address[U("street")] = web::json::value::string(U("main st"));
        address[U("zip")] = web::json::value::string(U("98052"));
        person[U("address")] = address;
        person[U("name")] = web::json::value::string(U("test"));
        person[U("age")] = web::json::value::number(15);
        obj[0] = person;

        return hub_conn->send(U("invokeWithComplexType"), obj);

    }).get();

    ASSERT_FALSE(received_event->wait(2000));

    ASSERT_EQ(*message, U("[{\"Address\":{\"Street\":\"main st\",\"Zip\":\"98052\"},\"Age\":15,\"Name\":\"test\"}]"));
}

TEST(hub_connection_tests, send_message_complex_type_return)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);

    auto test = hub_conn->start().then([&hub_conn]()
    {
        web::json::value obj{};
        web::json::value person;
        web::json::value address;
        address[U("street")] = web::json::value::string(U("main st"));
        address[U("zip")] = web::json::value::string(U("98052"));
        person[U("address")] = address;
        person[U("name")] = web::json::value::string(U("test"));
        person[U("age")] = web::json::value::number(15);
        obj[0] = person;

        return hub_conn->invoke(U("returnComplexType"), obj);

    }).get();

    ASSERT_EQ(test.serialize(), U("{\"Address\":{\"Street\":\"main st\",\"Zip\":\"98052\"},\"Age\":15,\"Name\":\"test\"}"));
}

TEST(hub_connection_tests, connection_id_start_stop_start)
{
    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
    auto weak_hub_conn = std::weak_ptr<signalr::hub_connection>(hub_conn);

    utility::string_t connection_id;

    ASSERT_EQ(U(""), hub_conn->get_connection_id());

    hub_conn->start().get();
    connection_id = hub_conn->get_connection_id();
    ASSERT_NE(connection_id, U(""));

    hub_conn->stop().get();
    ASSERT_EQ(hub_conn->get_connection_id(), connection_id);

    hub_conn->start().get();
    ASSERT_NE(hub_conn->get_connection_id(), U(""));
    ASSERT_NE(hub_conn->get_connection_id(), connection_id);

    connection_id = hub_conn->get_connection_id();
}

//TEST(hub_connection_tests, mirror_header)
//{
//    auto hub_conn = std::make_shared<signalr::hub_connection>(url);
//
//    signalr::signalr_client_config signalr_client_config{};
//    auto headers = signalr_client_config.get_http_headers();
//    headers[U("x-mirror")] = U("MirrorThis");
//    signalr_client_config.set_http_headers(headers);
//    hub_conn->set_client_config(signalr_client_config);
//
//    {
//        auto mirrored_header_value = hub_conn->start().then([&hub_conn]()
//        {
//            return hub_conn->invoke(U("mirrorHeader"));
//        }).get();
//        ASSERT_EQ(U("MirrorThis"), mirrored_header_value.as_string());
//    }
//
//    hub_conn->stop().wait();
//
//    headers[U("x-mirror")] = U("MirrorThat");
//    signalr_client_config.set_http_headers(headers);
//    hub_conn->set_client_config(signalr_client_config);
//
//    {
//        auto mirrored_header_value = hub_conn->start().then([&hub_conn]()
//        {
//            return hub_conn->invoke(U("mirrorHeader"));
//        }).get();
//        ASSERT_EQ(U("MirrorThat"), mirrored_header_value.as_string());
//    }
//}
