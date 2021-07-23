// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

DECLARE_DEBUG_PRINT_OBJECT2("tests", ASPNETCORE_DEBUG_FLAG_INFO | ASPNETCORE_DEBUG_FLAG_CONSOLE);

int wmain(int argc, wchar_t* argv[])
{
    ::testing::InitGoogleTest(&argc, argv);
    RUN_ALL_TESTS();
}
