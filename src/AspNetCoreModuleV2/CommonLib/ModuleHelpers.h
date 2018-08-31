// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "HandleWrapper.h"
#include "exceptions.h"

extern HMODULE g_hModule;

class ModuleHelpers
{
public:
    static
    void IncrementCurrentModuleRefCount(HandleWrapper<ModuleHandleTraits> &handle)
    {
        WCHAR path[MAX_PATH];
        THROW_LAST_ERROR_IF(!GetModuleFileName(g_hModule, path, sizeof(path)));
        THROW_LAST_ERROR_IF(!GetModuleHandleEx(0, path, &handle));
    }
};
