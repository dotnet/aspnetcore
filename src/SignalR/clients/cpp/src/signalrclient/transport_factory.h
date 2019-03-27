// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "signalrclient/signalr_client_config.h"
#include "signalrclient/transport_type.h"
#include "transport.h"

namespace signalr
{
    class transport_factory
    {
    public:
        virtual std::shared_ptr<transport> create_transport(transport_type transport_type, const logger& logger,
            const signalr_client_config& signalr_client_config);

        virtual ~transport_factory();
    };
}
