// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "Environment.h"

#include <Windows.h>

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

    std::wstring expandedStr;
    do
    {
        expandedStr.resize(requestedSize);
        requestedSize = GetEnvironmentVariableW(str.c_str(), expandedStr.data(), requestedSize);
        if (requestedSize == 0)
        {
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
