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
        pplx::task<std::string> get(web_request_factory& request_factory, const std::string& url,
            const signalr_client_config& signalr_client_config)
        {
            auto request = request_factory.create_web_request(url);
            request->set_method(utility::conversions::to_utf8string(web::http::methods::GET));

            request->set_user_agent(USER_AGENT);
            request->set_client_config(signalr_client_config);

            return request->get_response().then([](web_response response)
            {
                if (response.status_code != 200)
                {
                    std::stringstream oss;
                    oss << "web exception - " << response.status_code << " " << response.reason_phrase;
                    throw web_exception(oss.str(), response.status_code);
                }

                return response.body;
            });
        }

        pplx::task<std::string> post(web_request_factory& request_factory, const std::string& url,
            const signalr_client_config& signalr_client_config)
        {
            auto request = request_factory.create_web_request(url);
            request->set_method(utility::conversions::to_utf8string(web::http::methods::POST));

            request->set_user_agent(USER_AGENT);
            request->set_client_config(signalr_client_config);

            return request->get_response().then([](web_response response)
            {
                if (response.status_code != 200)
                {
                    std::stringstream oss;
                    oss << "web exception - " << response.status_code << " " << response.reason_phrase;
                    throw web_exception(oss.str(), response.status_code);
                }

                return response.body;
            });
        }
    }
}
