// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "request_sender.h"
#include "http_sender.h"
#include "url_builder.h"
#include "signalrclient/signalr_exception.h"

namespace signalr
{
    namespace request_sender
    {
        pplx::task<negotiation_response> negotiate(web_request_factory& request_factory, const web::uri& base_url,
            const utility::string_t& query_string, const signalr_client_config& signalr_client_config)
        {
            auto negotiate_url = url_builder::build_negotiate(base_url, query_string);

            return http_sender::post(request_factory, negotiate_url, signalr_client_config)
                .then([](utility::string_t body)
            {
                auto negotiation_response_json = web::json::value::parse(body);

                negotiation_response response;

                if (negotiation_response_json.has_field(_XPLATSTR("error")))
                {
                    response.error = negotiation_response_json[_XPLATSTR("error")].as_string();
                    return std::move(response);
                }

                if (negotiation_response_json.has_field(_XPLATSTR("connectionId")))
                {
                    response.connectionId = negotiation_response_json[_XPLATSTR("connectionId")].as_string();
                }

                if (negotiation_response_json.has_field(_XPLATSTR("availableTransports")))
                {
                    for (auto transportData : negotiation_response_json[_XPLATSTR("availableTransports")].as_array())
                    {
                        available_transport transport;
                        transport.transport = transportData[_XPLATSTR("transport")].as_string();

                        for (auto format : transportData[_XPLATSTR("transferFormats")].as_array())
                        {
                            transport.transfer_formats.push_back(format.as_string());
                        }

                        response.availableTransports.push_back(transport);
                    }
                }

                if (negotiation_response_json.has_field(_XPLATSTR("url")))
                {
                    response.url = negotiation_response_json[_XPLATSTR("url")].as_string();

                    if (negotiation_response_json.has_field(_XPLATSTR("accessToken")))
                    {
                        response.accessToken = negotiation_response_json[_XPLATSTR("accessToken")].as_string();
                    }
                }

                return std::move(response);
            });
        }
    }
}
