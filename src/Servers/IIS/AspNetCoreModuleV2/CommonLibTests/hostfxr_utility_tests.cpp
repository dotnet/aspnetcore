// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include <filesystem>
#include <vector>
#include <string>
#include "HostFxrResolver.h"
#include "Environment.h"

TEST(ParseHostFxrArguments, BasicHostFxrArguments)
{
    std::vector<std::wstring> bstrArray;

    HostFxrResolver::AppendArguments(
        L"exec \"test.dll\"", // args
        L"invalid",  // physical path to application
        bstrArray); // args array.

    EXPECT_EQ(2, bstrArray.size());
    ASSERT_STREQ(L"exec", bstrArray[0].c_str());
    ASSERT_STREQ(L"test.dll", bstrArray[1].c_str());
}

TEST(ParseHostFxrArguments, NoExecProvided)
{
    std::vector<std::wstring> bstrArray;

    HostFxrResolver::AppendArguments(
        L"test.dll", // args
        L"ignored",  // physical path to application
        bstrArray); // args array.

    EXPECT_EQ(1, bstrArray.size());
    ASSERT_STREQ(L"test.dll", bstrArray[0].c_str());
}

TEST(ParseHostFxrArguments, ConvertDllToAbsolutePath)
{
    std::vector<std::wstring> bstrArray;
    // we need to use existing dll so let's use ntdll that we know exists everywhere
    auto system32 = Environment::ExpandEnvironmentVariables(L"%WINDIR%\\System32");
    HostFxrResolver::AppendArguments(
        L"exec \"ntdll.dll\"", // args
        system32,  // physical path to application
        bstrArray, // args array.
        true); // expandDllPaths

    EXPECT_EQ(2, bstrArray.size());
    ASSERT_STREQ(L"exec", bstrArray[0].c_str());
    ASSERT_STREQ((system32 + L"\\ntdll.dll").c_str(), bstrArray[1].c_str());
}
