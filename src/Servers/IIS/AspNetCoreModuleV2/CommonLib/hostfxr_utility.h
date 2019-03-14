// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <Windows.h>
#include <vector>
#include <filesystem>
#include <optional>
#include <string>

typedef INT(*hostfxr_get_native_search_directories_fn) (CONST INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (CONST DWORD argc, CONST PCWSTR argv[]);

#define READ_BUFFER_SIZE 4096

class HOSTFXR_UTILITY
{
public:

    static
    void
    GetHostFxrParameters(
        const std::filesystem::path     &processPath,
        const std::filesystem::path     &applicationPhysicalPath,
        const std::wstring              &applicationArguments,
        std::filesystem::path           &hostFxrDllPath,
        std::filesystem::path           &dotnetExePath,
        std::vector<std::wstring>       &arguments
    );

    static
    void
    AppendArguments(
        const std::wstring          &arugments,
        const std::filesystem::path &applicationPhysicalPath,
        std::vector<std::wstring>   &arguments,
        bool                        expandDllPaths = false
    );

    static
    std::optional<std::filesystem::path>
    GetAbsolutePathToDotnetFromProgramFiles();
private:

    static
    BOOL
    IsDotnetExecutable(
        const std::filesystem::path & dotnetPath
    );

    static
    VOID
    FindDotNetFolders(
        const std::filesystem::path& path,
        std::vector<std::wstring> & pvFolders
    );

    static
    std::wstring
    FindHighestDotNetVersion(
        std::vector<std::wstring> & vFolders
    );

    static
    std::filesystem::path
    GetAbsolutePathToHostFxr(
        const std::filesystem::path & dotnetPath
    );


    static
    std::optional<std::filesystem::path>
    InvokeWhereToFindDotnet();

    static
    std::filesystem::path
    GetAbsolutePathToDotnet(
        const std::filesystem::path & applicationPath,
        const std::filesystem::path & requestedPath
    );

    struct LocalFreeDeleter
    {
         void operator ()(_In_ LPWSTR* ptr) const
         {
             LocalFree(ptr);
         }
    };
};

