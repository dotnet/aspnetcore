// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "StdWrapper.h"
#include "stringu.h"
#include "BaseOutputManager.h"

class PipeOutputManager : public BaseOutputManager
{
    // Timeout to be used if a thread never exits
    #define PIPE_OUTPUT_THREAD_TIMEOUT 2000

    // Max event log message is ~32KB, limit pipe size just below that.
    #define MAX_PIPE_READ_SIZE 30000
public:
    PipeOutputManager();
    PipeOutputManager(bool fEnableNativeLogging);
    ~PipeOutputManager();

    void Start() override;
    void Stop() override;
    std::wstring GetStdOutContent() override;

    // Thread functions
    void ReadStdErrHandleInternal();

    static void ReadStdErrHandle(LPVOID pContext);

private:

    HANDLE                          m_hErrReadPipe;
    HANDLE                          m_hErrWritePipe;
    HANDLE                          m_hErrThread;
    CHAR                            m_pipeContents[MAX_PIPE_READ_SIZE] = { 0 };
    DWORD                           m_numBytesReadTotal;
};
