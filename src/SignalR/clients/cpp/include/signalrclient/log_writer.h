// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/details/basic_types.h"

namespace signalr
{
    class log_writer
    {
    public:
        // NOTE: the caller does not enforce thread safety of this call
        virtual void __cdecl write(const utility::string_t &entry) = 0;
    };
}