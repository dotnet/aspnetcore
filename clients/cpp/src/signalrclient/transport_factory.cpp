// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "transport_factory.h"
#include "websocket_transport.h"

namespace signalr
{
    std::shared_ptr<transport> transport_factory::create_transport(transport_type transport_type, const logger& logger,
        const signalr_client_config& signalr_client_config,
        std::function<void(const utility::string_t&)> process_response_callback,
        std::function<void(const std::exception&)> error_callback)
    {
        if (transport_type == signalr::transport_type::websockets)
        {
            return websocket_transport::create(
                [signalr_client_config](){ return std::make_shared<default_websocket_client>(signalr_client_config); },
                logger, process_response_callback, error_callback);
        }

        throw std::runtime_error("not implemented");
    }

    transport_factory::~transport_factory()
    { }
}