// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <stdexcept>
#include "signalr_exception.h"

namespace signalr
{
    class hub_exception : public signalr_exception
    {
    public:
        hub_exception(const std::string &what)
            : signalr_exception(what)
        {}
    };
}
