// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

#include <array>
#include "fakeclasses.h"
#include "HostFxrResolver.h"

using ::testing::_;
using ::testing::NiceMock;

// Externals defined in inprocess
namespace InprocessTests
{

    TEST(Dotnet_EXE_Path_Tests, EndWith_dotnet)
    {
        std::filesystem::path hostFxrDllPath;
        std::vector<std::wstring> arguments;
        ErrorContext errorContext;
        auto currentPath = std::filesystem::current_path();
        auto appPath= currentPath /= L"Fake";
        auto processPath = L"hello-dotnet";
        auto args = L"-a --tag t -x";
        std::filesystem::path knownDotnetLocation=L"C:/Program Files/dotnet";
        // expected no exception should be thrown
        HostFxrResolver::GetHostFxrParameters(
            processPath,
            appPath,
            args,
            hostFxrDllPath,
            knownDotnetLocation,
            arguments,
            errorContext);

        ASSERT_TRUE(ends_with(arguments[0], L"\\Fake\\hello-dotnet.exe", true));
        ASSERT_STREQ(arguments[1].c_str(), L"-a");
        ASSERT_STREQ(arguments[2].c_str(), L"--tag");
        ASSERT_STREQ(arguments[3].c_str(), L"t");
        ASSERT_STREQ(arguments[4].c_str(), L"-x");
    }
}
