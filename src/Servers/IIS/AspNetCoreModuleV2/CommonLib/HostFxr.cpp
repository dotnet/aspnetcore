// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "HostFxr.h"

#include "ModuleHelpers.h"
#include "EventLog.h"

HostFxrErrorRedirector::HostFxrErrorRedirector(corehost_set_error_writer_fn setErrorWriterFn, RedirectionOutput* writeFunction) noexcept
    : m_setErrorWriter(setErrorWriterFn)
{
    if (m_setErrorWriter)
    {
        m_writeFunction = writeFunction;
        m_setErrorWriter(HostFxrErrorRedirectorCallback);
    }
}

HostFxrErrorRedirector::~HostFxrErrorRedirector()
{
    if (m_setErrorWriter)
    {
        m_setErrorWriter(nullptr);
        m_writeFunction = nullptr;
    }
}

void HostFxrErrorRedirector::HostFxrErrorRedirectorCallback(const WCHAR* message)
{
    m_writeFunction->Append(std::wstring(message) + L"\r\n");
}

int HostFxr::Main(DWORD argc, const PCWSTR* argv) const noexcept(false)
{
    return m_hostfxr_main_fn(argc, argv);
}

int HostFxr::GetNativeSearchDirectories(INT argc, const PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size) const noexcept
{
    return m_hostfxr_get_native_search_directories_fn(argc, argv, buffer, buffer_size, required_buffer_size);
}

HostFxrErrorRedirector HostFxr::RedirectOutput(RedirectionOutput* writer) const noexcept
{
    return HostFxrErrorRedirector(m_corehost_set_error_writer_fn, writer);
}

HostFxr HostFxr::CreateFromLoadedModule()
{
    HMODULE hModule;
    THROW_LAST_ERROR_IF_NULL(hModule = GetModuleHandle(L"hostfxr.dll"));

    try
    {
        return HostFxr(
            ModuleHelpers::GetKnownProcAddress<hostfxr_main_fn>(hModule, "hostfxr_main"),
            ModuleHelpers::GetKnownProcAddress<hostfxr_get_native_search_directories_fn>(hModule, "hostfxr_get_native_search_directories"),
            ModuleHelpers::GetKnownProcAddress<corehost_set_error_writer_fn>(hModule, "hostfxr_set_error_writer", /* optional */ true));
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
