#pragma once
#include "ModuleHelpers.h"
#include "hostfxr_utility.h"
#include "exceptions.h"
#include <functional>
#include "EventLog.h"
#include "IOutputManager.h"

typedef INT(*hostfxr_get_native_search_directories_fn) (INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (DWORD argc, CONST PCWSTR argv[]);
typedef void(*corehost_error_writer_fn) (const CHAR* message);
typedef corehost_error_writer_fn(*corehost_set_error_writer_fn) (corehost_error_writer_fn error_writer);

class HostFxrErrorRedirector: NonCopyable
{
public:
    HostFxrErrorRedirector(corehost_set_error_writer_fn setErrorWriterFn, RedirectionOutput& writeFunction):
        m_setErrorWriter(setErrorWriterFn)
    {
        if (m_setErrorWriter)
        {
            m_writeFunction = &writeFunction;
            m_setErrorWriter(HostFxrErrorRedirectorCallback);
        }
    }

    HostFxrErrorRedirector(HostFxrErrorRedirector&& other) noexcept
    {
        std::swap(m_setErrorWriter, other.m_setErrorWriter);
    }

    HostFxrErrorRedirector& operator= (HostFxrErrorRedirector && other) noexcept
    {
        std::swap(m_setErrorWriter, other.m_setErrorWriter);
        return *this;
    }

    ~HostFxrErrorRedirector()
    {
        if (m_setErrorWriter)
        {
            m_setErrorWriter(nullptr);
            m_writeFunction = nullptr;
        }
    }

    static void HostFxrErrorRedirectorCallback(const CHAR* message)
    {
        auto const writeFunction = m_writeFunction;
        if (writeFunction)
        {
            writeFunction->Append(to_wide_string(message, GetConsoleCP()));
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
        corehost_set_error_writer_fn corehost_set_error_writer_fn)
        : m_hostfxr_main_fn(hostfxr_main_fn),
          m_hostfxr_get_native_search_directories_fn(hostfxr_get_native_search_directories_fn),
          m_corehost_set_error_writer_fn(corehost_set_error_writer_fn)
    {
    }

	~HostFxr() = default;

    int Main(DWORD argc, CONST PCWSTR argv[]) const
    {
        return m_hostfxr_main_fn(argc, argv);
    }

    int GetNativeSearchDirectories(INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD * required_buffer_size)
    {
        return m_hostfxr_get_native_search_directories_fn(argc, argv, buffer, buffer_size, required_buffer_size);
    }

    HostFxrErrorRedirector RedirectOutput(RedirectionOutput & writer)
    {
        return HostFxrErrorRedirector(m_corehost_set_error_writer_fn, writer);
    }

    bool SupportsOutputRedirection() const
    {
        return m_corehost_set_error_writer_fn != nullptr;
    }

    static
    HostFxr CreateFromLoadedModule()
    {
        HMODULE hModule;
        THROW_LAST_ERROR_IF_NULL(hModule = GetModuleHandle(L"hostfxr.dll"));

        try
        {
            return HostFxr(
                ModuleHelpers::GetKnownProcAddress<hostfxr_main_fn>(hModule, "hostfxr_main"),
                ModuleHelpers::GetKnownProcAddress<hostfxr_get_native_search_directories_fn>(hModule, "get_native_search_directories"),
                ModuleHelpers::GetKnownProcAddress<corehost_set_error_writer_fn>(hModule, "set_error_writer", true));
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

private:
    hostfxr_main_fn m_hostfxr_main_fn;
    hostfxr_get_native_search_directories_fn m_hostfxr_get_native_search_directories_fn;
    corehost_set_error_writer_fn m_corehost_set_error_writer_fn;
};
