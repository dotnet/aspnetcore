// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <optional>
#include "HandleWrapper.h"

class RegistryKey
{
public:
    static
    std::optional<DWORD> TryGetDWORD(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName, DWORD flags = 0);

    static
    std::optional<std::wstring> TryGetString(HKEY section, const std::wstring& subSectionName, const std::wstring& valueName);

private:
    static
    bool
    CheckReturnValue(int errorCode);
};
