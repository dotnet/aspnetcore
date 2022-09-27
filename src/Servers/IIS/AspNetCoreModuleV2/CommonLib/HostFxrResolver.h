// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <Windows.h>
#include <vector>
#include <filesystem>
#include <optional>
#include <string>

#include "ErrorContext.h"

#define READ_BUFFER_SIZE 4096

class HostFxrResolver
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
        std::vector<std::wstring>       &arguments,
        ErrorContext&                   errorContext
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
    bool
    IsDotnetExecutable(
        const std::filesystem::path & dotnetPath
    );

    static
    bool
    TryGetHostFxrPath(
        std::filesystem::path& hostFxrDllPath,
        const std::filesystem::path& dotnetRoot,
        const std::filesystem::path& applicationPath
    );

    static
    std::filesystem::path
    GetAbsolutePathToDotnetFromHostfxr(
        const std::filesystem::path& hostfxrPath
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

