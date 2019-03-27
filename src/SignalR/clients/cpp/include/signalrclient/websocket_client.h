// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "transfer_format.h"

namespace signalr
{
    class websocket_client
    {
    public:
        virtual ~websocket_client() {};

        virtual void start(std::string url, transfer_format format, std::function<void(std::exception_ptr)> callback) = 0;

        virtual void stop(std::function<void(std::exception_ptr)> callback) = 0;

        virtual void send(std::string payload, std::function<void(std::exception_ptr)> callback) = 0;

        virtual void receive(std::function<void(std::string, std::exception_ptr)> callback) = 0;
    };
}
