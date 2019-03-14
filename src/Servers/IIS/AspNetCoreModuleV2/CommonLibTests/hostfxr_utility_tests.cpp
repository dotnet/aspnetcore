// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include <filesystem>
#include <vector>
#include <string>
#include "hostfxr_utility.h"
#include "Environment.h"

TEST(ParseHostFxrArguments, BasicHostFxrArguments)
{
    std::vector<std::wstring> bstrArray;

    HOSTFXR_UTILITY::AppendArguments(
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

    HOSTFXR_UTILITY::AppendArguments(
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
    HOSTFXR_UTILITY::AppendArguments(
        L"exec \"ntdll.dll\"", // args
        system32,  // physical path to application
        bstrArray, // args array.
        true); // expandDllPaths

    EXPECT_EQ(2, bstrArray.size());
    ASSERT_STREQ(L"exec", bstrArray[0].c_str());
    ASSERT_STREQ((system32 + L"\\ntdll.dll").c_str(), bstrArray[1].c_str());
}

TEST(ParseHostFxrArguments, ProvideNoArgs_InvalidArgs)
{
    std::vector<std::wstring> bstrArray;
    std::filesystem::path struHostFxrDllLocation;
    std::filesystem::path struExeLocation;

    EXPECT_THROW(HOSTFXR_UTILITY::GetHostFxrParameters(
        L"dotnet", // processPath
        L"some\\path",  // application physical path, ignored.
        L"",  //arguments
        struHostFxrDllLocation,
        struExeLocation,
        bstrArray), // args array.
        InvalidOperationException);
}

TEST(GetAbsolutePathToDotnetFromProgramFiles, BackupWorks)
{
    STRU struAbsolutePathToDotnet;
    BOOL fDotnetInProgramFiles;
    BOOL is64Bit;
    BOOL fIsWow64 = FALSE;
    SYSTEM_INFO systemInfo;
    IsWow64Process(GetCurrentProcess(), &fIsWow64);
    if (fIsWow64)
    {
        is64Bit = FALSE;
    }
    else
    {
        GetNativeSystemInfo(&systemInfo);
        is64Bit = systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64;
    }

    if (is64Bit)
    {
        fDotnetInProgramFiles = std::filesystem::is_regular_file(L"C:/Program Files/dotnet/dotnet.exe");
    }
    else
    {
        fDotnetInProgramFiles = std::filesystem::is_regular_file(L"C:/Program Files (x86)/dotnet/dotnet.exe");
    }

    auto dotnetPath = HOSTFXR_UTILITY::GetAbsolutePathToDotnetFromProgramFiles();
    if (fDotnetInProgramFiles)
    {
        EXPECT_TRUE(dotnetPath.has_value());
    }
    else
    {
        EXPECT_FALSE(dotnetPath.has_value());
    }
}

TEST(GetHostFxrArguments, InvalidParams)
{
    std::vector<std::wstring> bstrArray;
    std::filesystem::path struHostFxrDllLocation;
    std::filesystem::path struExeLocation;

    EXPECT_THROW(HOSTFXR_UTILITY::GetHostFxrParameters(
        L"bogus", // processPath
        L"",  // application physical path, ignored.
        L"ignored",  //arguments
        struHostFxrDllLocation,
        struExeLocation,
        bstrArray), // args array.
        InvalidOperationException);
}
