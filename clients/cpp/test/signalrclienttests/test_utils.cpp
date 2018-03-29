// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_utils.h"
#include "test_websocket_client.h"
#include "test_web_request_factory.h"

using namespace signalr;

utility::string_t remove_date_from_log_entry(const utility::string_t &log_entry)
{
    // dates are ISO 8601 (e.g. `2014-11-13T06:05:29.452066Z`)
    auto date_end_index = log_entry.find_first_of(_XPLATSTR("Z")) + 1;

    // date is followed by a whitespace hence +1
    return log_entry.substr(date_end_index + 1);
}

std::shared_ptr<websocket_client> create_test_websocket_client(std::function<pplx::task<std::string>()> receive_function,
    std::function<pplx::task<void>(const utility::string_t &msg)> send_function,
    std::function<pplx::task<void>(const web::uri &url)> connect_function,
    std::function<pplx::task<void>()> close_function)
{
    auto websocket_client = std::make_shared<test_websocket_client>();
    websocket_client->set_receive_function(receive_function);
    websocket_client->set_send_function(send_function);
    websocket_client->set_connect_function(connect_function);
    websocket_client->set_close_function(close_function);

    return websocket_client;
}

std::unique_ptr<web_request_factory> create_test_web_request_factory()
{
    return std::make_unique<test_web_request_factory>([](const web::uri& url)
    {
        auto response_body =
            url.path() == _XPLATSTR("/negotiate") || url.path() == _XPLATSTR("/signalr/negotiate")
            ? _XPLATSTR("{\"Url\":\"/signalr\", \"ConnectionToken\" : \"A==\", \"ConnectionId\" : \"f7707523-307d-4cba-9abf-3eef701241e8\", ")
            _XPLATSTR("\"KeepAliveTimeout\" : 20.0, \"DisconnectTimeout\" : 10.0, \"ConnectionTimeout\" : 110.0, \"TryWebSockets\" : true, ")
            _XPLATSTR("\"ProtocolVersion\" : \"1.4\", \"TransportConnectTimeout\" : 5.0, \"LongPollDelay\" : 0.0}")
            : url.path() == _XPLATSTR("/start") || url.path() == _XPLATSTR("/signalr/start")
                ? _XPLATSTR("{\"Response\":\"started\" }")
                : _XPLATSTR("");

        return std::unique_ptr<web_request>(new web_request_stub((unsigned short)200, _XPLATSTR("OK"), response_body));
    });
}

utility::string_t create_uri()
{
    auto unit_test = ::testing::UnitTest::GetInstance();

    // unit test will be null if this function is not called in a test
    _ASSERTE(unit_test);

    return utility::string_t(_XPLATSTR("http://"))
        .append(utility::conversions::to_string_t(unit_test->current_test_info()->name()));
}

std::vector<utility::string_t> filter_vector(const std::vector<utility::string_t>& source, const utility::string_t& string)
{
    std::vector<utility::string_t> filtered_entries;
    std::copy_if(source.begin(), source.end(), std::back_inserter(filtered_entries), [&string](const utility::string_t &e)
    {
        return e.find(string) != utility::string_t::npos;
    });
    return filtered_entries;
}

utility::string_t dump_vector(const std::vector<utility::string_t>& source)
{
    utility::stringstream_t ss;
    ss << _XPLATSTR("Number of entries: ") << source.size() << std::endl;
    for (const auto& e : source)
    {
        ss << e;
        if (e.back() != _XPLATSTR('\n'))
        {
            ss << std::endl;
        }
    }

    return ss.str();
}