// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"
#include "cpprest/details/basic_types.h"

namespace signalr
{
    struct web_response
    {
        unsigned short status_code;
        utility::string_t reason_phrase;
        pplx::task<utility::string_t> body;
    };
}