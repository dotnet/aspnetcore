// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

DECLARE_DEBUG_PRINT_OBJECT2("tests", ASPNETCORE_DEBUG_FLAG_INFO | ASPNETCORE_DEBUG_FLAG_CONSOLE);

int wmain(int argc, wchar_t* argv[])
{
    ::testing::InitGoogleTest(&argc, argv);
    RUN_ALL_TESTS();
}
