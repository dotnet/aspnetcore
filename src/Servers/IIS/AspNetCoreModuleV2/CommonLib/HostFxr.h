// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "HostFxrResolver.h"
#include "exceptions.h"
#include <functional>
#include "RedirectionOutput.h"

typedef INT(*hostfxr_get_native_search_directories_fn) (INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (DWORD argc, CONST PCWSTR argv[]);
typedef void(*corehost_error_writer_fn) (const WCHAR* message);
typedef corehost_error_writer_fn(*corehost_set_error_writer_fn) (corehost_error_writer_fn error_writer);

class HostFxrErrorRedirector: NonCopyable
{
public:
    HostFxrErrorRedirector(corehost_set_error_writer_fn setErrorWriterFn, RedirectionOutput& writeFunction) noexcept :
        m_setErrorWriter(setErrorWriterFn)
    {
        if (m_setErrorWriter)
        {
            m_writeFunction = &writeFunction;
            m_setErrorWriter(HostFxrErrorRedirectorCallback);
        }
    }

    ~HostFxrErrorRedirector()
    {
        if (m_setErrorWriter)
        {
            m_setErrorWriter(nullptr);
            m_writeFunction = nullptr;
        }
    }

    static void HostFxrErrorRedirectorCallback(const WCHAR* message)
    {
        auto const writeFunction = m_writeFunction;
        if (writeFunction)
        {
            writeFunction->Append(std::wstring(message) + L"\r\n");
        }
    }

private:
    corehost_set_error_writer_fn m_setErrorWriter;
    static inline thread_local RedirectionOutput* m_writeFunction;
};

class HostFxr
{
public:
    HostFxr(
        hostfxr_main_fn hostfxr_main_fn,
        hostfxr_get_native_search_directories_fn hostfxr_get_native_search_directories_fn,
        corehost_set_error_writer_fn corehost_set_error_writer_fn) noexcept
        : m_hostfxr_main_fn(hostfxr_main_fn),
          m_hostfxr_get_native_search_directories_fn(hostfxr_get_native_search_directories_fn),
          m_corehost_set_error_writer_fn(corehost_set_error_writer_fn)
    {
    }

	~HostFxr() = default;

    int Main(DWORD argc, CONST PCWSTR* argv) const noexcept(false)
    {
        return m_hostfxr_main_fn(argc, argv);
    }

    int GetNativeSearchDirectories(INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD * required_buffer_size) noexcept
    {
        return m_hostfxr_get_native_search_directories_fn(argc, argv, buffer, buffer_size, required_buffer_size);
    }

    HostFxrErrorRedirector RedirectOutput(RedirectionOutput & writer) noexcept
    {
        return HostFxrErrorRedirector(m_corehost_set_error_writer_fn, writer);
    }

    bool SupportsOutputRedirection() const noexcept
    {
        return m_corehost_set_error_writer_fn != nullptr;
    }

    static
    HostFxr CreateFromLoadedModule();

private:
    hostfxr_main_fn m_hostfxr_main_fn;
    hostfxr_get_native_search_directories_fn m_hostfxr_get_native_search_directories_fn;
    corehost_set_error_writer_fn m_corehost_set_error_writer_fn;
};
