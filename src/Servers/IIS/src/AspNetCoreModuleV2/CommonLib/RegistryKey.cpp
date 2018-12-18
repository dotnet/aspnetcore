// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "RegistryKey.h"
#include "exceptions.h"

std::optional<DWORD> RegistryKey::TryGetDWORD(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName, DWORD flags)
{
    DWORD dwData = 0;
    DWORD cbData = sizeof(dwData);
    if (LOG_LAST_ERROR_IF(RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_DWORD | flags, nullptr, reinterpret_cast<LPBYTE>(&dwData), &cbData) != NO_ERROR))
    {
        return std::nullopt;
    }

    return dwData;
}

std::optional<std::wstring> RegistryKey::TryGetString(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName, DWORD flags)
{
    DWORD cbData;

    if (LOG_LAST_ERROR_IF(RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_SZ | flags, nullptr, nullptr, &cbData) != NO_ERROR))
    {
        return std::nullopt;
    }

    std::wstring data;
    data.resize(cbData);

    if (LOG_LAST_ERROR_IF(RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_SZ | flags, nullptr, &data[0], &cbData) != NO_ERROR))
    {
        return std::nullopt;
    }

    data.resize(cbData - 1);

    return data;
}

