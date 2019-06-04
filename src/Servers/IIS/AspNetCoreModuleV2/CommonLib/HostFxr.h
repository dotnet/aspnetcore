// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "HostFxrResolver.h"
#include "exceptions.h"
#include "RedirectionOutput.h"

typedef INT(*hostfxr_get_native_search_directories_fn) (INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (DWORD argc, CONST PCWSTR argv[]);
typedef void(*corehost_error_writer_fn) (const WCHAR* message);
typedef corehost_error_writer_fn(*corehost_set_error_writer_fn) (corehost_error_writer_fn error_writer);

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
          m_corehost_set_error_writer_fn(corehost_set_error_writer_fn)
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

private:
    HandleWrapper<ModuleHandleTraits> m_hHostFxrDll;
    hostfxr_main_fn m_hostfxr_main_fn;
    hostfxr_get_native_search_directories_fn m_hostfxr_get_native_search_directories_fn;
    corehost_set_error_writer_fn m_corehost_set_error_writer_fn;
};
