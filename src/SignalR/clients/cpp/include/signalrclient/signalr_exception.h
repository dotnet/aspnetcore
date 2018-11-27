// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <stdexcept>
#include "cpprest/details/basic_types.h"
#include "cpprest/asyncrt_utils.h"

namespace signalr
{
    class signalr_exception : public std::runtime_error
    {
    public:
        explicit signalr_exception(const utility::string_t &what)
            : runtime_error(utility::conversions::to_utf8string(what))
        {}
    };
}
