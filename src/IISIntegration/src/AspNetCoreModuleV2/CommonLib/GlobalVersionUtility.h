// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "fx_ver.h"

class GlobalVersionUtility
{
public:

    static
        std::wstring
        GetGlobalRequestHandlerPath(PCWSTR pwzAspNetCoreFolderPath, PCWSTR pwzHandlerVersion, PCWSTR pwzHandlerName);

    static
        std::wstring
        FindHighestGlobalVersion(PCWSTR pwzAspNetCoreFolderPath);

    static
        std::wstring
        RemoveFileNameFromFolderPath(std::wstring fileName);

    static
        std::vector<fx_ver_t>
        GetRequestHandlerVersions(PCWSTR pwzAspNetCoreFolderPath);

    static
        std::wstring
        GetModuleName(HMODULE hModuleName);
};

