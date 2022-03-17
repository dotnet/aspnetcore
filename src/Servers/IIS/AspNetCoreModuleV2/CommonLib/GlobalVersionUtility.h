// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "fx_ver.h"

using namespace aspnet;

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

