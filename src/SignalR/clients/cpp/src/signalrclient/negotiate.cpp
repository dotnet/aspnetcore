// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "negotiate.h"
#include "url_builder.h"
#include "signalrclient/signalr_exception.h"

namespace signalr
{
    namespace negotiate
    {
        pplx::task<negotiation_response> negotiate(http_client& client, const std::string& base_url,
            const signalr_client_config& config)
        {
            auto negotiate_url = url_builder::build_negotiate(base_url);

            pplx::task_completion_event<negotiation_response> tce;

            // TODO: signalr_client_config
            http_request request;
            request.method = http_method::POST;

            for (auto& header : config.get_http_headers())
            {
                request.headers.insert(std::make_pair(utility::conversions::to_utf8string(header.first), utility::conversions::to_utf8string(header.second)));
            }

            client.send(negotiate_url, request, [tce](http_response http_response, std::exception_ptr exception)
            {
                if (exception != nullptr)
                {
                    tce.set_exception(exception);
                    return;
                }

                if (http_response.status_code != 200)
                {
                    tce.set_exception(signalr_exception("negotiate failed with status code " + std::to_string(http_response.status_code)));
                    return;
                }

                try
                {
                    auto negotiation_response_json = web::json::value::parse(utility::conversions::to_string_t(http_response.content));

                    negotiation_response response;

                    if (negotiation_response_json.has_field(_XPLATSTR("error")))
                    {
                        response.error = utility::conversions::to_utf8string(negotiation_response_json[_XPLATSTR("error")].as_string());
                        tce.set(std::move(response));
                        return;
                    }

                    if (negotiation_response_json.has_field(_XPLATSTR("connectionId")))
                    {
                        response.connectionId = utility::conversions::to_utf8string(negotiation_response_json[_XPLATSTR("connectionId")].as_string());
                    }

                    if (negotiation_response_json.has_field(_XPLATSTR("availableTransports")))
                    {
                        for (auto transportData : negotiation_response_json[_XPLATSTR("availableTransports")].as_array())
                        {
                            available_transport transport;
                            transport.transport = utility::conversions::to_utf8string(transportData[_XPLATSTR("transport")].as_string());

                            for (auto format : transportData[_XPLATSTR("transferFormats")].as_array())
                            {
                                transport.transfer_formats.push_back(utility::conversions::to_utf8string(format.as_string()));
                            }

                            response.availableTransports.push_back(transport);
                        }
                    }

                    if (negotiation_response_json.has_field(_XPLATSTR("url")))
                    {
                        response.url = utility::conversions::to_utf8string(negotiation_response_json[_XPLATSTR("url")].as_string());

                        if (negotiation_response_json.has_field(_XPLATSTR("accessToken")))
                        {
                            response.accessToken = utility::conversions::to_utf8string(negotiation_response_json[_XPLATSTR("accessToken")].as_string());
                        }
                    }

                    if (negotiation_response_json.has_field(_XPLATSTR("ProtocolVersion")))
                    {
                        tce.set_exception(signalr_exception("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details."));
                    }

                    tce.set(std::move(response));
                }
                catch (...)
                {
                    tce.set_exception(std::current_exception());
                }
            });

            return pplx::create_task(tce);
        }
    }
}
