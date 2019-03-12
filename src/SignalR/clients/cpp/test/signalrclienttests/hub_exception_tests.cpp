// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient/hub_exception.h"

using namespace signalr;

TEST(hub_exception_initialization, hub_exception_initialized_correctly)
{
    auto e = hub_exception{ "error" };

    ASSERT_STREQ("error", e.what());
}
