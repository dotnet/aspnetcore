// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "HostFxr.h"

#include "ModuleHelpers.h"
#include "EventLog.h"

HostFxr HostFxr::CreateFromLoadedModule()
{
    HMODULE hModule;
    THROW_LAST_ERROR_IF_NULL(hModule = GetModuleHandle(L"hostfxr.dll"));

    try
    {
        return HostFxr(
            ModuleHelpers::GetKnownProcAddress<hostfxr_main_fn>(hModule, "hostfxr_main"),
            ModuleHelpers::GetKnownProcAddress<hostfxr_get_native_search_directories_fn>(hModule, "hostfxr_get_native_search_directories"),
            ModuleHelpers::GetKnownProcAddress<corehost_set_error_writer_fn>(hModule, "hostfxr_set_error_writer", true));
    }
    catch (...)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_GENERAL_ERROR,
            ASPNETCORE_EVENT_HOSTFXR_DLL_INVALID_VERSION_MSG,
            ModuleHelpers::GetModuleFileNameValue(hModule).c_str()
        );

        throw;
    }
}
