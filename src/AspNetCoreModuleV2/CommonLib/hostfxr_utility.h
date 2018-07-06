// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <Windows.h>
#include <vector>
#include <experimental/filesystem>
#include <optional>
#include "stringu.h"

typedef INT(*hostfxr_get_native_search_directories_fn) (CONST INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (CONST DWORD argc, CONST PCWSTR argv[]);

#define READ_BUFFER_SIZE 4096

class HOSTFXR_UTILITY
{
public:

    static
    HRESULT
    GetStandaloneHostfxrParameters(
        PCWSTR              pwzExeAbsolutePath, // includes .exe file extension.
        PCWSTR				pcwzApplicationPhysicalPath,
        PCWSTR              pcwzArguments,
        HANDLE              hEventLog,
        _Inout_ STRU*		pStruHostFxrDllLocation,
        _Out_ DWORD*		pdwArgCount,
        _Out_ BSTR**		ppwzArgv
    );

    static
    HRESULT
    ParseHostfxrArguments(
        PCWSTR              pwzArgumentsFromConfig,
        PCWSTR              pwzExePath,
        PCWSTR              pcwzApplicationPhysicalPath,
        HANDLE              hEventLog,
        _Out_ DWORD*        pdwArgCount,
        _Out_ BSTR**        pbstrArgv
    );

    static
    BOOL
    IsDotnetExecutable(
        _In_ const std::experimental::filesystem::path & dotnetPath
    );

    static
    HRESULT
    GetHostFxrParameters(
        _In_ HANDLE         hEventLog,
        _In_ PCWSTR         pcwzProcessPath,
        _In_ PCWSTR         pcwzApplicationPhysicalPath,
        _In_ PCWSTR         pcwzArguments,
        _Inout_ STRU       *pStruHostFxrDllLocation,
        _Inout_ STRU       *struExeAbsolutePath,
        _Out_ DWORD        *pdwArgCount,
        _Out_ BSTR        **ppwzArgv
    );

    static
    VOID
    FindDotNetFolders(
        _In_ PCWSTR pszPath,
        _Out_ std::vector<std::wstring> & pvFolders
    );

    static
    std::wstring
    FindHighestDotNetVersion(
        _In_ std::vector<std::wstring> & vFolders
    );

    static
    std::optional<std::experimental::filesystem::path>
    GetAbsolutePathToHostFxr(
        _In_ const std::experimental::filesystem::path & dotnetPath,
        _In_ HANDLE hEventLog
    );

    static
    std::optional<std::experimental::filesystem::path>
    GetAbsolutePathToDotnetFromProgramFiles();

    static
    std::optional<std::experimental::filesystem::path>
    InvokeWhereToFindDotnet();

    static
    std::optional<std::experimental::filesystem::path>
    GetAbsolutePathToDotnet(
        _In_ const std::experimental::filesystem::path & applicationPath,
        _In_ const std::experimental::filesystem::path & requestedPath
    );
};

