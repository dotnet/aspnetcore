// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "cpprest/details/basic_types.h"
#include "signalrclient/web_exception.h"
#include "request_sender.h"
#include "web_request_stub.h"
#include "test_web_request_factory.h"
#include "signalrclient/signalr_exception.h"

using namespace signalr;

TEST(request_sender_negotiate, request_created_with_correct_url)
{
    web::uri requested_url;
    auto request_factory = test_web_request_factory([&requested_url](const web::uri &url) -> std::unique_ptr<web_request>
    {
        utility::string_t response_body(
            _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}"));

        requested_url = url;
        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    request_sender::negotiate(request_factory, web::uri{ _XPLATSTR("http://fake/signalr") }, _XPLATSTR("")).get();

    ASSERT_EQ(web::uri(_XPLATSTR("http://fake/signalr/negotiate?clientProtocol=1.4&connectionData=data")), requested_url);
}

TEST(request_sender_negotiate, negotiation_request_sent_and_response_serialized)
{
    auto request_factory = test_web_request_factory([](const web::uri&) -> std::unique_ptr<web_request>
    {
        utility::string_t response_body(
            _XPLATSTR("{\"Invocation\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 30.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.5, \"LongPollDelay\" : 0.0}"));

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });

    auto response = request_sender::negotiate(request_factory, web::uri{ _XPLATSTR("http://fake/signalr") }, _XPLATSTR("")).get();

    ASSERT_EQ(_XPLATSTR("f7707523-307d-4cba-9abf-3eef701241e8"), response.connection_id);
    // TODO: response.availableTransports
}