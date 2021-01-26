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

void HostFxr::Load(HMODULE moduleHandle)
{
    // A hostfxr may already be loaded here if we tried to start with an
    // invalid configuration. Release hostfxr before loading it again.
    if (m_hHostFxrDll != nullptr)
    {
        m_hHostFxrDll.release();
    }

    m_hHostFxrDll = moduleHandle;

    try
    {
        m_hostfxr_get_native_search_directories_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_get_native_search_directories_fn>(moduleHandle, "hostfxr_get_native_search_directories");
        m_corehost_set_error_writer_fn = ModuleHelpers::GetKnownProcAddress<corehost_set_error_writer_fn>(moduleHandle, "hostfxr_set_error_writer", /* optional */ true);
        m_hostfxr_initialize_for_dotnet_commandline_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_initialize_for_dotnet_runtime_fn>(moduleHandle, "hostfxr_initialize_for_dotnet_command_line", /* optional */ true);
        m_hostfxr_set_runtime_property_value_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_set_runtime_property_value_fn>(moduleHandle, "hostfxr_set_runtime_property_value", /* optional */ true);
        m_hostfxr_get_runtime_property_value_fn = ModuleHelpers::GetKnownProcAddress<hostfxr_get_runtime_property_value_fn>(moduleHandle, "hostfxr_get_runtime_property_value", /* optional */ true);
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
    // Make sure to always load hostfxr via an absolute path.
    // If the process fails to start for whatever reason, a mismatched hostfxr
    // may be already loaded in the process.
    try
    {
        HMODULE hModule;
        LOG_INFOF(L"Loading hostfxr from location %s", location.c_str());
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

int HostFxr::InitializeForApp(int argc, PCWSTR* argv, const std::wstring& dotnetExe) const noexcept
{
    if (m_hostfxr_initialize_for_dotnet_commandline_fn == nullptr || m_hostfxr_main_fn != nullptr)
    {
        return 0;
    }

    hostfxr_initialize_parameters params;
    params.size = sizeof(hostfxr_initialize_parameters);
    params.host_path = L"";

    // Transformation occurs here rather than hostfxr arguments as hostfxr_get_native_directories still needs
    // exe as the first argument.
    if (!dotnetExe.empty())
    {
        // Portable application
        // argv[0] = dotnet.exe
        // argv[1] = app.dll
        // argv[2] = rest of the args

        std::filesystem::path dotnetExePath(dotnetExe);
        auto dotnetFolderPath = dotnetExePath.parent_path();
        params.dotnet_root = dotnetFolderPath.c_str();

        RETURN_INT_IF_NOT_ZERO(m_hostfxr_initialize_for_dotnet_commandline_fn(argc - 1, &argv[1], &params, &m_host_context_handle));
    }
    else
    {
        // Standalone application
        // argv[0] = app.exe
        // argv[1] = rest of the args
        params.dotnet_root = L"";
        std::filesystem::path applicationPath(argv[0]); 
        applicationPath.replace_extension(".dll");
        argv[0] = applicationPath.c_str();

        RETURN_INT_IF_NOT_ZERO(m_hostfxr_initialize_for_dotnet_commandline_fn(argc, argv, &params, &m_host_context_handle));
    }

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

int HostFxr::GetRuntimePropertyValue(PCWSTR name, PWSTR* value) const noexcept
{
    if (m_host_context_handle != nullptr && m_hostfxr_get_runtime_property_value_fn != nullptr)
    {
        return m_hostfxr_get_runtime_property_value_fn(m_host_context_handle, name, value);
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
