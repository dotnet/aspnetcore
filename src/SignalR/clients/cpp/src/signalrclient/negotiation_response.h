// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/details/basic_types.h"

namespace signalr
{
    struct available_transport
    {
        utility::string_t transport;
        std::vector<utility::string_t> transfer_formats;
    };

    struct negotiation_response
    {
        utility::string_t connectionId;
        std::vector<available_transport> availableTransports;
        utility::string_t url;
        utility::string_t accessToken;
        utility::string_t error;
    };
}
