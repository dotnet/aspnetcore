// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "transport_factory.h"
#include "websocket_client.h"

using namespace signalr;

class test_transport_factory : public transport_factory
{
public:
    test_transport_factory(const std::shared_ptr<websocket_client>& websocket_client);

    std::shared_ptr<transport> create_transport(transport_type transport_type, const logger& logger,
        const signalr_client_config& signalr_client_config,
        std::function<void(const utility::string_t&)> process_message_callback,
        std::function<void(const std::exception&)> error_callback) override;

private:
    std::shared_ptr<websocket_client> m_websocket_client;
};
