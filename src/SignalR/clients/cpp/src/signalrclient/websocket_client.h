// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"

namespace signalr
{
    class websocket_client
    {
    public:
        virtual pplx::task<void> connect(const std::string& url) = 0;

        virtual pplx::task<void> send(const std::string& message) = 0;

        virtual pplx::task<std::string> receive() = 0;

        virtual pplx::task<void> close() = 0;

        virtual ~websocket_client() {};
    };
}
