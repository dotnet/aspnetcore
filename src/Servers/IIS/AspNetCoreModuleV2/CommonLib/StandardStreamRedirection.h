// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "RedirectionOutput.h"
#include "StdWrapper.h"
#include "ModuleHelpers.h"

class StandardStreamRedirection : NonCopyable
{
    // Default timeout for the redirection thread to exit before it is forcefully terminated
    // This can be overridden with ASPNETCORE_OUTPUT_REDIRECTION_TERMINATION_TIMEOUT_MS
    static constexpr int PIPE_OUTPUT_THREAD_TIMEOUT_MS_DEFAULT = 2000;

    // Maximum allowed timeout value
    static constexpr int PIPE_OUTPUT_THREAD_TIMEOUT_MS_MAX = 1800000; // 30 minutes

    // Size of the buffer used to read from the pipe
    static constexpr int PIPE_READ_SIZE = 4096;

public:
    StandardStreamRedirection(RedirectionOutput& output, bool commandLineLaunch);

    ~StandardStreamRedirection() noexcept(false);

private:

    void Start();
    void Stop();

    void
    TryStartRedirection()
    {
        try
        {
            Start();
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
        }
    }

    void
    TryStopRedirection()
    {
        try
        {
            Stop();
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
        }
    }

    // Thread functions
    void ReadStdErrHandleInternal();
    static void ReadStdErrHandle(LPVOID pContext);

    HANDLE                          m_hErrReadPipe;
    HANDLE                          m_hErrWritePipe;
    HANDLE                          m_hErrThread;

    bool m_disposed;
    bool m_commandLineLaunch;
    SRWLOCK m_srwLock{};
    std::unique_ptr<StdWrapper> stdoutWrapper;
    std::unique_ptr<StdWrapper> stderrWrapper;
    RedirectionOutput& m_output;
    int m_terminationTimeoutMs = PIPE_OUTPUT_THREAD_TIMEOUT_MS_DEFAULT;
};
