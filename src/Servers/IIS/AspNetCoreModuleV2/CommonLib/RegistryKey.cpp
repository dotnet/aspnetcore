// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "RegistryKey.h"
#include "exceptions.h"

std::optional<DWORD> RegistryKey::TryGetDWORD(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName, DWORD flags)
{
    DWORD dwData = 0;
    DWORD cbData = sizeof(dwData);
    if (!CheckReturnValue(RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_DWORD | flags, nullptr, reinterpret_cast<LPBYTE>(&dwData), &cbData)))
    {
        return std::nullopt;
    }

    return dwData;
}

std::optional<std::wstring> RegistryKey::TryGetString(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName)
{
    DWORD cbData{};

    if (!CheckReturnValue(RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_SZ, nullptr, nullptr, &cbData)))
    {
        return std::nullopt;
    }

    std::wstring data;
    data.resize(cbData / sizeof(wchar_t));

    if (!CheckReturnValue(RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_SZ, nullptr, data.data(), &cbData)))
    {
        return std::nullopt;
    }

    data.resize(cbData / sizeof(wchar_t) - 1);

    return data;
}

bool RegistryKey::CheckReturnValue(int errorCode)
{
    if (errorCode == NO_ERROR)
    {
        return true;
    }
    // NotFound result is expected, don't spam logs with failures
    if (errorCode != ERROR_FILE_NOT_FOUND)
    {
        LOG_IF_FAILED(HRESULT_FROM_WIN32(errorCode));
    }

    return false;
}

