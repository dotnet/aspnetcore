// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <stdexcept>

namespace signalr
{
    class signalr_exception : public std::runtime_error
    {
    public:
        explicit signalr_exception(const std::string &what)
            : runtime_error(what)
        {}
    };
}
