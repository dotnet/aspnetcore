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
private:

    // Thread functions
    void ReadStdErrHandleInternal();
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
