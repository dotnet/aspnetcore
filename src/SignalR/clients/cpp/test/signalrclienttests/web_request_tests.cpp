// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "cpprest/http_listener.h"
#include "web_request.h"

using namespace web;
using namespace signalr;

TEST(web_request_get_response, sends_request_receives_response)
{
    web::uri url(_XPLATSTR("http://localhost:56000/web_request_test"));
    auto request_received = false;
    utility::string_t user_agent_string;

    http::experimental::listener::http_listener listener(url);
    listener.support(http::methods::GET, [&request_received, &user_agent_string](http::http_request request)
    {
        request_received = true;
        user_agent_string = request.headers()[_XPLATSTR("User-Agent")];
        request.reply(http::status_codes::OK, _XPLATSTR("response"));
    });

    listener.open()
        .then([&url]()
        {
            web_request request(url);
            request.set_method(http::methods::GET);
            request.set_user_agent(_XPLATSTR("007"));
            request.get_response()
                .then([](web_response response)
                {
                    ASSERT_EQ((unsigned short)200, response.status_code);
                    ASSERT_EQ(_XPLATSTR("OK"), response.reason_phrase);
                    ASSERT_EQ(_XPLATSTR("response"), response.body.get());
                }).wait();
        }).wait();

    listener.close();

    ASSERT_TRUE(request_received);
    ASSERT_EQ(_XPLATSTR("007"), user_agent_string);
}