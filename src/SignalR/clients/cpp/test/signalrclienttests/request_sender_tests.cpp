//// Copyright (c) .NET Foundation. All rights reserved.
//// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
#include "stdafx.h"
//#include "cpprest/details/basic_types.h"
//#include "signalrclient/web_exception.h"
//#include "request_sender.h"
//#include "web_request_stub.h"
//#include "test_web_request_factory.h"
//#include "signalrclient/signalr_exception.h"
//
//using namespace signalr;
//
//TEST(request_sender_negotiate, request_created_with_correct_url)
//{
//    std::string requested_url;
//    auto request_factory = test_web_request_factory([&requested_url](const std::string& url) -> std::unique_ptr<web_request>
//    {
//        std::string response_body(
//            "{ \"connectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", "
//            "\"availableTransports\" : [] }");
//
//        requested_url = url;
//        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, "OK", response_body));
//    });
//
//    request_sender::negotiate(request_factory, "http://fake/signalr").get();
//
//    ASSERT_EQ("http://fake/signalr/negotiate", requested_url);
//}
//
//TEST(request_sender_negotiate, negotiation_request_sent_and_response_serialized)
//{
//    auto request_factory = test_web_request_factory([](const std::string&) -> std::unique_ptr<web_request>
//    {
//        std::string response_body(
//            "{\"connectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", "
//            "\"availableTransports\" : [ { \"transport\": \"WebSockets\", \"transferFormats\": [ \"Text\", \"Binary\" ] },"
//            "{ \"transport\": \"ServerSentEvents\", \"transferFormats\": [ \"Text\" ] } ] }");
//
//        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, "OK", response_body));
//    });
//
//    auto response = request_sender::negotiate(request_factory, "http://fake/signalr").get();
//
//    ASSERT_EQ("f7707523-307d-4cba-9abf-3eef701241e8", response.connectionId);
//    ASSERT_EQ(2u, response.availableTransports.size());
//    ASSERT_EQ(2u, response.availableTransports[0].transfer_formats.size());
//    ASSERT_EQ("Text", response.availableTransports[0].transfer_formats[0]);
//    ASSERT_EQ("Binary", response.availableTransports[0].transfer_formats[1]);
//    ASSERT_EQ(1u, response.availableTransports[1].transfer_formats.size());
//    ASSERT_EQ("Text", response.availableTransports[1].transfer_formats[0]);
//}
