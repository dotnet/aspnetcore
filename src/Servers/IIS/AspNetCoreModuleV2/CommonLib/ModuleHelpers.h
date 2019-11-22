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

#pragma warning( push )
#pragma warning ( disable : 26485 ) // Calling WinAPI causes expected array to pointer decay

        THROW_LAST_ERROR_IF(!GetModuleFileName(g_hModule, path, MAX_PATH));
        THROW_LAST_ERROR_IF(!GetModuleHandleEx(0, path, &handle));

#pragma warning( pop )
    }

    template<typename Func>
    static
    Func GetKnownProcAddress(HMODULE hModule, LPCSTR lpProcName, bool optional = false) {

#pragma warning( push )
#pragma warning ( disable : 26490 ) // Disable Don't use reinterpret_cast
        auto proc = reinterpret_cast<Func>(GetProcAddress(hModule, lpProcName));
#pragma warning( pop )

        THROW_LAST_ERROR_IF (!optional && !proc);
        return proc;
    }

    static
    std::wstring
    GetModuleFileNameValue(HMODULE hModule)
    {
        // this method is used for logging purposes for modules known to be under MAX_PATH
        WCHAR path[MAX_PATH];

#pragma warning( push )
#pragma warning ( disable : 26485 ) // Calling WinAPI causes expected array to pointer decay

        THROW_LAST_ERROR_IF(!GetModuleFileName(hModule, path, MAX_PATH));

        return path;
#pragma warning( pop )
    }
};
