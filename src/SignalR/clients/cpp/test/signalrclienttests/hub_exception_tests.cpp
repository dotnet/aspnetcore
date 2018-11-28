// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient/hub_exception.h"

using namespace signalr;

TEST(hub_exception_initialization, hub_exception_initialized_correctly)
{
    auto error_data = web::json::value::parse(_XPLATSTR("{\"SomeData\" : 42 }"));

    auto e = hub_exception{ _XPLATSTR("error"), error_data };

    ASSERT_STREQ("error", e.what());
    ASSERT_EQ(error_data.serialize(), e.error_data().serialize());
}
