// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "RedirectionOutput.h"
#include "EventLog.h"
#include "StdWrapper.h"
#include "ModuleHelpers.h"

class PipeOutputManager: NonCopyable
{
    // Timeout to be used if a thread never exits
    #define PIPE_OUTPUT_THREAD_TIMEOUT 2000
    #define PIPE_READ_SIZE 4096

public:
    PipeOutputManager(RedirectionOutput& output);

    ~PipeOutputManager() noexcept(false);

    void Start();
    void Stop();

    void
    TryStartRedirection()
    {
        const auto startLambda = [&]() { this->Start(); };
        TryOperation(startLambda, L"Could not start stdout redirection in %s. Exception message: %s.");
    }

    void
    TryStopRedirection()
    {
        const auto stopLambda = [&]() { this->Stop(); };
        TryOperation(stopLambda, L"Could not stop stdout redirection in %s. Exception message: %s.");
    }
private:

    // Thread functions
    void ReadStdErrHandleInternal();

    template<typename Functor>
    static
    void
    TryOperation(Functor func, std::wstring exceptionMessage)
    {
        try
        {
            func();
        }
        catch (const std::runtime_error& exception)
        {
            EventLog::Warn(ASPNETCORE_EVENT_GENERAL_WARNING, exceptionMessage.c_str(), GetModuleName().c_str(), to_wide_string(exception.what(), GetConsoleOutputCP()).c_str());
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
        }
    }

    static void ReadStdErrHandle(LPVOID pContext);

    HANDLE                          m_hErrReadPipe;
    HANDLE                          m_hErrWritePipe;
    HANDLE                          m_hErrThread;

    bool m_disposed;
    SRWLOCK m_srwLock{};
    std::unique_ptr<StdWrapper> stdoutWrapper;
    std::unique_ptr<StdWrapper> stderrWrapper;
    RedirectionOutput& m_output;
};
