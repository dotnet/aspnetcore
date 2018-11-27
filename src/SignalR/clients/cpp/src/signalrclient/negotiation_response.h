// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/details/basic_types.h"

namespace signalr
{
    struct negotiation_response
    {
        utility::string_t connection_id;
        web::json::value availableTransports;
    };
}