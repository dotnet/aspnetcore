// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "Environment.h"

#include <Windows.h>
#include "exceptions.h"

std::wstring
Environment::ExpandEnvironmentVariables(const std::wstring & str)
{
    DWORD requestedSize = ExpandEnvironmentStringsW(str.c_str(), nullptr, 0);
    if (requestedSize == 0)
    {
        throw std::system_error(GetLastError(), std::system_category(), "ExpandEnvironmentVariables");
    }

    std::wstring expandedStr;
    do
    {
        expandedStr.resize(requestedSize);
        requestedSize = ExpandEnvironmentStringsW(str.c_str(), expandedStr.data(), requestedSize);
        if (requestedSize == 0)
        {
            throw std::system_error(GetLastError(), std::system_category(), "ExpandEnvironmentVariables");
        }
    } while (expandedStr.size() != requestedSize);

    // trim null character as ExpandEnvironmentStringsW returns size including null character
    expandedStr.resize(requestedSize - 1);

    return expandedStr;
}

std::optional<std::wstring>
Environment::GetEnvironmentVariableValue(const std::wstring & str)
{
    DWORD requestedSize = GetEnvironmentVariableW(str.c_str(), nullptr, 0);
    if (requestedSize == 0)
    {
        if (GetLastError() == ERROR_ENVVAR_NOT_FOUND)
        {
            return std::nullopt;
        }

        throw std::system_error(GetLastError(), std::system_category(), "GetEnvironmentVariableW");
    }
    else if (requestedSize == 1)
    {
        // String just contains a nullcharacter, return nothing
        // GetEnvironmentVariableW has inconsistent behavior when returning size for an empty
        // environment variable.
        return std::nullopt;
    }

    std::wstring expandedStr;
    do
    {
        expandedStr.resize(requestedSize);
        requestedSize = GetEnvironmentVariableW(str.c_str(), expandedStr.data(), requestedSize);
        if (requestedSize == 0)
        {
            if (GetLastError() == ERROR_ENVVAR_NOT_FOUND)
            {
                return std::nullopt;
            }
            throw std::system_error(GetLastError(), std::system_category(), "ExpandEnvironmentStringsW");
        }
    } while (expandedStr.size() != requestedSize + 1);

    expandedStr.resize(requestedSize);

    return expandedStr;
}

std::wstring Environment::GetCurrentDirectoryValue()
{
    DWORD requestedSize = GetCurrentDirectory(0, nullptr);
    if (requestedSize == 0)
    {
        throw std::system_error(GetLastError(), std::system_category(), "GetCurrentDirectory");
    }

    std::wstring expandedStr;
    do
    {
        expandedStr.resize(requestedSize);
        requestedSize = GetCurrentDirectory(requestedSize, expandedStr.data());
        if (requestedSize == 0)
        {
            throw std::system_error(GetLastError(), std::system_category(), "GetCurrentDirectory");
        }
    } while (expandedStr.size() != requestedSize + 1);

    expandedStr.resize(requestedSize);

    return expandedStr;
}

std::wstring Environment::GetDllDirectoryValue()
{
    // GetDllDirectory can return 0 in both the success case and the failure case, and it only sets last error when it fails.
    // This requires you to set the last error to ERROR_SUCCESS before calling it in order to detect failure.
    SetLastError(ERROR_SUCCESS);

    DWORD requestedSize = GetDllDirectory(0, nullptr);
    if (requestedSize == 0)
    {
        if (GetLastError() != ERROR_SUCCESS)
        {
            throw std::system_error(GetLastError(), std::system_category(), "GetDllDirectory");
        }
        else
        {
            return L"";
        }
    }

    std::wstring expandedStr;
    do
    {
        expandedStr.resize(requestedSize);
        requestedSize = GetDllDirectory(requestedSize, expandedStr.data());
        // 0 might be returned if GetDllDirectory is empty
        if (requestedSize == 0 && GetLastError() != ERROR_SUCCESS)
        {
            throw std::system_error(GetLastError(), std::system_category(), "GetDllDirectory");
        }
    } while (expandedStr.size() != requestedSize + 1);

    expandedStr.resize(requestedSize);

    return expandedStr;
}

bool Environment::IsRunning64BitProcess()
{
    // Check the bitness of the currently running process
    // matches the dotnet.exe found.
    BOOL fIsWow64Process = false;
    THROW_LAST_ERROR_IF(!IsWow64Process(GetCurrentProcess(), &fIsWow64Process));

    if (fIsWow64Process)
    {
        // 32 bit mode
        return false;
    }

    // Check the SystemInfo to see if we are currently 32 or 64 bit.
    SYSTEM_INFO systemInfo;
    GetNativeSystemInfo(&systemInfo);
    return systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64;
}

HRESULT Environment::CopyToDirectory(const std::wstring& source, const std::filesystem::path& destination, bool cleanDest, const std::filesystem::path& directoryToIgnore, int& copiedFileCount)
{
    if (cleanDest && std::filesystem::exists(destination))
    {
        std::filesystem::remove_all(destination);
    }

    Environment::CopyToDirectoryInner(source, destination, directoryToIgnore, copiedFileCount);
    return S_OK;
}

void Environment::CopyToDirectoryInner(const std::filesystem::path& source, const std::filesystem::path& destination, const std::filesystem::path& directoryToIgnore, int& copiedFileCount)
{
    auto destinationDirEntry = std::filesystem::directory_entry(destination);
    if (!destinationDirEntry.exists())
    {
        CreateDirectory(destination.wstring().c_str(), nullptr);
    }

    for (auto& path : std::filesystem::directory_iterator(source))
    {
        if (path.is_regular_file())
        {
            auto sourceFile = path.path().filename();
            auto destinationPath = (destination / sourceFile);

            if (std::filesystem::directory_entry(destinationPath).exists())
            {
                auto sourceFileTime = std::filesystem::last_write_time(path);
                auto destinationFileTime = std::filesystem::last_write_time(destinationPath);
                if (sourceFileTime <= destinationFileTime) // file write time is the same
                {
                    continue;
                }
            }

            copiedFileCount++;
            CopyFile(path.path().wstring().c_str(), destinationPath.wstring().c_str(), FALSE);
        }
        else if (path.is_directory())
        {
            auto sourceInnerDirectory = path.path();

            if (sourceInnerDirectory.wstring().rfind(directoryToIgnore, 0) != 0)
            {
                CopyToDirectoryInner(path.path(), destination / path.path().filename(), directoryToIgnore, copiedFileCount);
            }
        }
    }
}

bool Environment::CheckUpToDate(const std::wstring& source, const std::filesystem::path& destination, const std::wstring& extension, const std::filesystem::path& directoryToIgnore)
{
    for (auto& path : std::filesystem::directory_iterator(source))
    {
        if (path.is_regular_file()
            && path.path().has_extension()
            && path.path().filename().extension().wstring() == extension)
        {
            auto sourceFile = path.path().filename();
            auto destinationPath = (destination / sourceFile);

            if (std::filesystem::directory_entry(destinationPath).exists())
            {
                auto originalFileTime = std::filesystem::last_write_time(path);
                auto destFileTime = std::filesystem::last_write_time(destinationPath);
                if (originalFileTime > destFileTime) // file write time is the same
                {
                    return false;
                }
            }

            CopyFile(path.path().wstring().c_str(), destinationPath.wstring().c_str(), FALSE);
        }
        else if (path.is_directory())
        {
            auto sourceInnerDirectory = std::filesystem::directory_entry(path);
            if (sourceInnerDirectory.path() != directoryToIgnore)
            {
                CheckUpToDate(/* source */ path.path(), /* destination */ destination / path.path().filename(), extension, directoryToIgnore);
            }
        }
    }
    return true;
}
