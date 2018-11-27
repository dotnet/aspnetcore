// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include <vector>
#include "test_utils.h"

#if defined(_WIN32)
int wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
    get_url(argc, argv);

    ::testing::InitGoogleTest(&argc, argv);
    RUN_ALL_TESTS();
    return 0;
}