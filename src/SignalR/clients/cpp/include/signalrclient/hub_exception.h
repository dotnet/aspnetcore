// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <stdexcept>
#include "cpprest/details/basic_types.h"
#include "cpprest/json.h"
#include "cpprest/asyncrt_utils.h"
#include "signalr_exception.h"

namespace signalr
{
    class hub_exception : public signalr_exception
    {
    public:
        hub_exception(const utility::string_t &what)
            : signalr_exception(what)
        {}
    };
}
