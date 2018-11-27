// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient/web_exception.h"
#include "web_request_factory.h"
#include "constants.h"

namespace signalr
{
    namespace http_sender
    {
        pplx::task<utility::string_t> get(web_request_factory& request_factory, const web::uri& url,
            const signalr_client_config& signalr_client_config)
        {
            auto request = request_factory.create_web_request(url);
            request->set_method(web::http::methods::GET);

            request->set_user_agent(USER_AGENT);
            request->set_client_config(signalr_client_config);

            return request->get_response().then([](web_response response)
            {
                if (response.status_code != 200)
                {
                    utility::ostringstream_t oss;
                    oss << _XPLATSTR("web exception - ") << response.status_code << _XPLATSTR(" ") << response.reason_phrase;
                    throw web_exception(oss.str(), response.status_code);
                }

                return response.body;
            });
        }

        pplx::task<utility::string_t> post(web_request_factory& request_factory, const web::uri& url,
            const signalr_client_config& signalr_client_config)
        {
            auto request = request_factory.create_web_request(url);
            request->set_method(web::http::methods::POST);

            request->set_user_agent(USER_AGENT);
            request->set_client_config(signalr_client_config);

            return request->get_response().then([](web_response response)
            {
                if (response.status_code != 200)
                {
                    utility::ostringstream_t oss;
                    oss << _XPLATSTR("web exception - ") << response.status_code << _XPLATSTR(" ") << response.reason_phrase;
                    throw web_exception(oss.str(), response.status_code);
                }

                return response.body;
            });
        }
    }
}