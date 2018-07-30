// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "IOutputManager.h"

class PipeOutputManager : public IOutputManager
{
    #define PIPE_OUTPUT_THREAD_TIMEOUT 2000
    #define MAX_PIPE_READ_SIZE 4096
public:
    PipeOutputManager();
    ~PipeOutputManager();

    virtual HRESULT Start() override;
    virtual HRESULT Stop() override;
    virtual bool GetStdOutContent(STRA* struStdOutput) override;

    // Thread functions
    VOID ReadStdErrHandleInternal(VOID);

    static
    VOID ReadStdErrHandle(LPVOID pContext);

private:
    HANDLE                          m_hErrReadPipe;
    HANDLE                          m_hErrWritePipe;
    STRU                            m_struLogFilePath;
    HANDLE                          m_hErrThread;
    CHAR                            m_pzFileContents[MAX_PIPE_READ_SIZE] = { 0 };
    DWORD                           m_dwStdErrReadTotal;
    SRWLOCK                         m_srwLock;
    int                             m_fdPreviousStdOut;
    int                             m_fdPreviousStdErr;
    BOOL                            m_disposed;
};

