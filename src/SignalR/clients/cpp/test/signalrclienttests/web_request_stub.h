// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "web_request.h"
#include "web_response.h"

using namespace signalr;

struct web_request_stub : public web_request
{
    unsigned short m_status_code;
    utility::string_t m_reason_phrase;
    utility::string_t m_response_body;
    utility::string_t m_method;
    utility::string_t m_user_agent_string;
    signalr_client_config m_signalr_client_config;
    std::function<void(web_request_stub&)> on_get_response = [](web_request_stub&){};

    web_request_stub(unsigned short status_code, const utility::string_t& reason_phrase, const utility::string_t& response_body = _XPLATSTR(""));

    virtual void set_method(const utility::string_t &method) override;
    virtual void set_user_agent(const utility::string_t &user_agent_string) override;
    virtual void set_client_config(const signalr_client_config& client_config) override;

    virtual pplx::task<web_response> get_response() override;
};
