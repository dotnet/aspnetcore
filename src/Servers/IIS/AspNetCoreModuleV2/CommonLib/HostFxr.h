// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "HostFxrResolver.h"
#include "exceptions.h"
#include "RedirectionOutput.h"

struct hostfxr_initialize_parameters
{
    size_t size;
    const PCWSTR host_path;
    const PCWSTR dotnet_root;
};

typedef INT(*hostfxr_get_native_search_directories_fn) (INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (DWORD argc, CONST PCWSTR argv[]);
typedef void(*corehost_error_writer_fn) (const WCHAR* message);
typedef corehost_error_writer_fn(*corehost_set_error_writer_fn) (corehost_error_writer_fn error_writer);
typedef int(*hostfxr_initialize_for_app_fn)(int argc, PCWSTR* argv, PCWSTR app_path, hostfxr_initialize_parameters* parameters, void* const* host_context_handle);
typedef int(*hostfxr_set_runtime_property_value_fn)(void* host_context_handle, PCWSTR name, PCWSTR value);
typedef int(*hostfxr_run_app_fn)(void* host_context_handle);
typedef int(*hostfxr_close)(void* hostfxr_context_handle);

struct ErrorContext
{
    // TODO consider adding HRESULT here
    std::string detailedErrorContent;
    USHORT statusCode;
    USHORT subStatusCode;
    std::string generalErrorType;
    std::string errorReason;
};

class HostFxrErrorRedirector: NonCopyable
{
public:
    HostFxrErrorRedirector(corehost_set_error_writer_fn setErrorWriterFn, RedirectionOutput* writeFunction) noexcept;

    ~HostFxrErrorRedirector();

    static void HostFxrErrorRedirectorCallback(const WCHAR* message);

private:
    corehost_set_error_writer_fn m_setErrorWriter;
    static inline thread_local RedirectionOutput* m_writeFunction;
};

class HostFxr: NonCopyable
{
public:
    HostFxr() : HostFxr(nullptr, nullptr, nullptr)
    {
    }

    HostFxr(
        hostfxr_main_fn hostfxr_main_fn,
        hostfxr_get_native_search_directories_fn hostfxr_get_native_search_directories_fn,
        corehost_set_error_writer_fn corehost_set_error_writer_fn) noexcept
        : m_hostfxr_main_fn(hostfxr_main_fn),
          m_hostfxr_get_native_search_directories_fn(hostfxr_get_native_search_directories_fn),
          m_corehost_set_error_writer_fn(corehost_set_error_writer_fn),
          m_host_context_handle(nullptr)
    {
    }

    void Load();
    void Load(HMODULE moduleHandle);
    void Load(const std::wstring& location);

    ~HostFxr() = default;

    void SetMain(hostfxr_main_fn hostfxr_main_fn);

    int Main(DWORD argc, CONST PCWSTR* argv) const noexcept(false);

    int GetNativeSearchDirectories(INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size) const noexcept;

    HostFxrErrorRedirector RedirectOutput(RedirectionOutput* writer) const noexcept;
    int SetRuntimePropertyValue(PCWSTR name, PCWSTR value) const noexcept;
    int InitializeForApp(DWORD argc, PCWSTR* argv, PCWSTR app_path, hostfxr_initialize_parameters* parameters) const noexcept; // todo const this

private:
    HandleWrapper<ModuleHandleTraits> m_hHostFxrDll;
    hostfxr_main_fn m_hostfxr_main_fn;
    hostfxr_get_native_search_directories_fn m_hostfxr_get_native_search_directories_fn;
    hostfxr_initialize_for_app_fn m_hostfxr_initialize_for_app_fn;
    hostfxr_set_runtime_property_value_fn m_hostfxr_set_runtime_property_value_fn;
    hostfxr_run_app_fn m_hostfxr_run_app_fn;
    corehost_set_error_writer_fn m_corehost_set_error_writer_fn;
    hostfxr_close m_hostfxr_close_fn;
    void* m_host_context_handle;
};
