// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "web_response.h"
#include "signalrclient/signalr_client_config.h"

namespace signalr
{
    class web_request
    {
    public:
        explicit web_request(const web::uri &url);

        virtual void set_method(const utility::string_t &method);
        virtual void set_user_agent(const utility::string_t &user_agent_string);
        virtual void set_client_config(const signalr_client_config& signalr_client_config);

        virtual pplx::task<web_response> get_response();

        web_request& operator=(const web_request&) = delete;

        virtual ~web_request();

    private:
        const web::uri m_url;
        web::http::http_request m_request;
        utility::string_t m_user_agent_string;
        signalr_client_config m_signalr_client_config;
    };
}