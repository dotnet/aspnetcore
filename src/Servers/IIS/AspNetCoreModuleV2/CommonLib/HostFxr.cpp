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

void HostFxr::Load()
{
    HMODULE hModule;
    THROW_LAST_ERROR_IF(!GetModuleHandleEx(0, L"hostfxr.dll", &hModule));
    Load(hModule);
}

void HostFxr::Load(HMODULE moduleHandle)
{
    m_hHostFxrDll = moduleHandle;
    try
    {
        m_hostfxr_get_native_search_directories_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_get_native_search_directories_fn>(moduleHandle, "hostfxr_get_native_search_directories");
        m_corehost_set_error_writer_fn = ModuleHelpers::GetKnownProcAddress<corehost_set_error_writer_fn>(moduleHandle, "hostfxr_set_error_writer", /* optional */ true);
        m_hostfxr_initialize_for_app_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_initialize_for_app_fn>(moduleHandle, "hostfxr_initialize_for_app", /* optional */ true);
        m_hostfxr_set_runtime_property_value_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_set_runtime_property_value_fn>(moduleHandle, "hostfxr_set_runtime_property_value", /* optional */ true);
        m_hostfxr_run_app_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_run_app_fn>(moduleHandle, "hostfxr_run_app", /* optional */ true);
        m_hostfxr_close_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_close_fn>(moduleHandle, "hostfxr_close", /* optional */ true);
    }
    catch (...)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_GENERAL_ERROR,
            ASPNETCORE_EVENT_HOSTFXR_DLL_INVALID_VERSION_MSG,
            ModuleHelpers::GetModuleFileNameValue(moduleHandle).c_str()
        );

        throw;
    }
}

void HostFxr::Load(const std::wstring& location)
{
    try
    {
        HMODULE hModule;
        THROW_LAST_ERROR_IF_NULL(hModule = LoadLibraryW(location.c_str()));
        Load(hModule);
    }
    catch (...)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_GENERAL_ERROR,
            ASPNETCORE_EVENT_HOSTFXR_DLL_UNABLE_TO_LOAD_MSG,
            location.c_str()
        );

        throw;
    }
}

void HostFxr::SetMain(hostfxr_main_fn hostfxr_main_fn)
{
    m_hostfxr_main_fn = hostfxr_main_fn;
}

int HostFxr::Main(DWORD argc, const PCWSTR* argv) const noexcept(false)
{
    if (m_host_context_handle != nullptr && m_hostfxr_run_app_fn != nullptr)
    {
        return m_hostfxr_run_app_fn(m_host_context_handle);
    }
    else
    {
        return m_hostfxr_main_fn(argc, argv);
    }
}

int HostFxr::GetNativeSearchDirectories(INT argc, const PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size) const noexcept
{
    return m_hostfxr_get_native_search_directories_fn(argc, argv, buffer, buffer_size, required_buffer_size);
}

HostFxrErrorRedirector HostFxr::RedirectOutput(RedirectionOutput* writer) const noexcept
{
    return HostFxrErrorRedirector(m_corehost_set_error_writer_fn, writer);
}

int HostFxr::InitializeForApp(int argc, const PCWSTR* argv, const std::wstring dotnetExe, bool callStartupHook) const noexcept
{
    if (m_hostfxr_initialize_for_app_fn == nullptr)
    {
        return 0;
    }

    hostfxr_initialize_parameters params;
    params.size = sizeof(hostfxr_initialize_parameters);
    params.host_path = L"";

    if (!dotnetExe.empty())
    {
        std::filesystem::path dotnetExePath(dotnetExe);

        auto dotnetFolderPath = dotnetExePath.parent_path();
        params.dotnet_root = dotnetFolderPath.c_str();
        RETURN_IF_NOT_ZERO(m_hostfxr_initialize_for_app_fn(argc - 1, &argv[1], nullptr, &params, &m_host_context_handle));

    }
    else
    {
        params.dotnet_root = L"";

        // Initialize_for_app doesn't work with an exe name
        std::filesystem::path applicationPath(argv[0]);
        applicationPath.replace_extension(".dll");

        RETURN_IF_NOT_ZERO(m_hostfxr_initialize_for_app_fn(argc - 1, &argv[1], applicationPath.c_str(), &params, &m_host_context_handle));
    }

    if (callStartupHook)
    {
        RETURN_IF_NOT_ZERO(SetRuntimePropertyValue(DOTNETCORE_STARTUP_HOOK, ASPNETCORE_STARTUP_ASSEMBLY));
    }

    RETURN_IF_NOT_ZERO(SetRuntimePropertyValue(DOTNETCORE_USE_ENTRYPOINT_FILTER, L"1"));

    return 0;
}

int HostFxr::SetRuntimePropertyValue(PCWSTR name, PCWSTR value) const noexcept
{
    if (m_host_context_handle != nullptr && m_hostfxr_set_runtime_property_value_fn != nullptr)
    {
        return m_hostfxr_set_runtime_property_value_fn(m_host_context_handle, name, value);
    }
    return 0;
}

void HostFxr::Close() const noexcept
{
    if (m_host_context_handle != nullptr && m_hostfxr_close_fn != nullptr)
    {
        m_hostfxr_close_fn(m_host_context_handle);
    }
}
