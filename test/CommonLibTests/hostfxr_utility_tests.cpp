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
    PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

    HOSTFXR_UTILITY::ParseHostfxrArguments(
        L"exec \"test.dll\"", // args
        exeStr,  // exe path
        L"invalid",  // physical path to application
        bstrArray); // args array.

    EXPECT_EQ(3, bstrArray.size());
    ASSERT_STREQ(exeStr, bstrArray[0].c_str());
    ASSERT_STREQ(L"exec", bstrArray[1].c_str());
    ASSERT_STREQ(L"test.dll", bstrArray[2].c_str());
}

TEST(ParseHostFxrArguments, NoExecProvided)
{
    std::vector<std::wstring> bstrArray;
    PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

    HOSTFXR_UTILITY::ParseHostfxrArguments(
        L"test.dll", // args
        exeStr,  // exe path
        L"ignored",  // physical path to application
        bstrArray); // args array.

    EXPECT_EQ(DWORD(2), bstrArray.size());
    ASSERT_STREQ(exeStr, bstrArray[0].c_str());
    ASSERT_STREQ(L"test.dll", bstrArray[1].c_str());
}

TEST(ParseHostFxrArguments, ConvertDllToAbsolutePath)
{
    std::vector<std::wstring> bstrArray;
    PCWSTR exeStr = L"C:/Program Files/dotnet.exe";
    // we need to use existing dll so let's use ntdll that we know exists everywhere
    auto system32 = Environment::ExpandEnvironmentVariables(L"%WINDIR%\\System32");
    HOSTFXR_UTILITY::ParseHostfxrArguments(
        L"exec \"ntdll.dll\"", // args
        exeStr,  // exe path
        system32,  // physical path to application
        bstrArray, // args array.
        true); // expandDllPaths

    EXPECT_EQ(DWORD(3), bstrArray.size());
    ASSERT_STREQ(exeStr, bstrArray[0].c_str());
    ASSERT_STREQ(L"exec", bstrArray[1].c_str());
    ASSERT_STREQ((system32 + L"\\ntdll.dll").c_str(), bstrArray[2].c_str());
}

TEST(ParseHostFxrArguments, ProvideNoArgs_InvalidArgs)
{
    std::vector<std::wstring> bstrArray;
    PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

    ASSERT_THROW(HOSTFXR_UTILITY::ParseHostfxrArguments(
        L"", // args
        exeStr,  // exe path
        L"ignored",  // physical path to application
        bstrArray), // args array.
        HOSTFXR_UTILITY::StartupParametersResolutionException);
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
        HOSTFXR_UTILITY::StartupParametersResolutionException);
}
