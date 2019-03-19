// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "negotiate.h"
#include "test_http_client.h"

using namespace signalr;

TEST(negotiate, request_created_with_correct_url)
{
    std::string requested_url;
    auto http_client = test_http_client([&requested_url](const std::string& url, http_request request)
    {
        std::string response_body(
            "{ \"connectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", "
            "\"availableTransports\" : [] }");

        requested_url = url;
        return http_response(200, response_body);
    });

    negotiate::negotiate(http_client, "http://fake/signalr").get();

    ASSERT_EQ("http://fake/signalr/negotiate", requested_url);
}

TEST(negotiate, negotiation_request_sent_and_response_serialized)
{
    auto request_factory = test_http_client([](const std::string&, http_request request)
    {
        std::string response_body(
            "{\"connectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", "
            "\"availableTransports\" : [ { \"transport\": \"WebSockets\", \"transferFormats\": [ \"Text\", \"Binary\" ] },"
            "{ \"transport\": \"ServerSentEvents\", \"transferFormats\": [ \"Text\" ] } ] }");

        return http_response(200, response_body);
    });

    auto response = negotiate::negotiate(request_factory, "http://fake/signalr").get();

    ASSERT_EQ("f7707523-307d-4cba-9abf-3eef701241e8", response.connectionId);
    ASSERT_EQ(2u, response.availableTransports.size());
    ASSERT_EQ(2u, response.availableTransports[0].transfer_formats.size());
    ASSERT_EQ("Text", response.availableTransports[0].transfer_formats[0]);
    ASSERT_EQ("Binary", response.availableTransports[0].transfer_formats[1]);
    ASSERT_EQ(1u, response.availableTransports[1].transfer_formats.size());
    ASSERT_EQ("Text", response.availableTransports[1].transfer_formats[0]);
}

TEST(negotiate, negotiation_response_with_redirect)
{
    auto request_factory = test_http_client([](const std::string&, http_request request)
    {
        std::string response_body(
            "{\"url\" : \"http://redirect\", "
            "\"accessToken\" : \"secret\" }");

        return http_response(200, response_body);
    });

    auto response = negotiate::negotiate(request_factory, "http://fake/signalr").get();

    ASSERT_EQ("http://redirect", response.url);
    ASSERT_EQ("secret", response.accessToken);
}
