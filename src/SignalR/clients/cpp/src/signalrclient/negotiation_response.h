// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

namespace signalr
{
    struct available_transport
    {
        std::string transport;
        std::vector<std::string> transfer_formats;
    };

    struct negotiation_response
    {
        std::string connectionId;
        std::vector<available_transport> availableTransports;
        std::string url;
        std::string accessToken;
        std::string error;
    };
}
