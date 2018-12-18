// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "RegistryKey.h"

std::optional<DWORD> RegistryKey::TryGetDWORD(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName)
{
    DWORD dwData = 0;
    DWORD cbData = sizeof(dwData);
    if (RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_DWORD, nullptr,
                    reinterpret_cast<LPBYTE>(&dwData), &cbData) == NO_ERROR)
    {
        return dwData;
    }

    return std::nullopt;
}

std::optional<std::wstring> RegistryKey::TryGetString(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName)
{
    DWORD cbData;

    if (!RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_SZ, nullptr, nullptr, &cbData) != ERROR_SUCCESS)
    {
        return std::nullopt;
    }

    std::wstring data;
    data.resize(cbData);

    if (!RegGetValue(section, subSectionName.c_str(), valueName.c_str(), RRF_RT_REG_SZ, nullptr, &data[0], &cbData) == NO_ERROR)
    {
        return std::nullopt;
    }

    data.resize(cbData - 1);

    return data;
}

