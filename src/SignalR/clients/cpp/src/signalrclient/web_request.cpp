// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "cpprest/http_client.h"
#include "web_request.h"

namespace signalr
{
    web_request::web_request(const std::string& url)
        : m_url(url)
    { }

    void web_request::set_method(const std::string &method)
    {
        m_request.set_method(utility::conversions::to_string_t(method));
    }

    void web_request::set_user_agent(const std::string &user_agent_string)
    {
        m_user_agent_string = user_agent_string;
    }

    void web_request::set_client_config(const signalr_client_config& signalr_client_config)
    {
        m_signalr_client_config = signalr_client_config;
    }

    pplx::task<web_response> web_request::get_response()
    {
        web::http::client::http_client client(utility::conversions::to_string_t(m_url), m_signalr_client_config.get_http_client_config());

        m_request.headers() = m_signalr_client_config.get_http_headers();
        if (!m_user_agent_string.empty())
        {
            m_request.headers()[_XPLATSTR("User-Agent")] = utility::conversions::to_string_t(m_user_agent_string);
        }

        return client.request(m_request)
            .then([](web::http::http_response response)
        {
            return web_response
            {
                response.status_code(),
                utility::conversions::to_utf8string(response.reason_phrase()),
                response.extract_string().then([](const utility::string_t& body)
                {
                    return utility::conversions::to_utf8string(body);
                })
            };
        });
    }

    web_request::~web_request() = default;
}
