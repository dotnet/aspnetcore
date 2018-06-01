// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

PipeOutputManager::PipeOutputManager() :
    m_dwStdErrReadTotal(0),
    m_hErrReadPipe(INVALID_HANDLE_VALUE),
    m_hErrWritePipe(INVALID_HANDLE_VALUE),
    m_hErrThread(INVALID_HANDLE_VALUE),
    m_fDisposed(FALSE)
{
}

PipeOutputManager::~PipeOutputManager()
{
    StopOutputRedirection();
}

VOID
PipeOutputManager::StopOutputRedirection()
{
    DWORD    dwThreadStatus = 0;
    STRA     straStdOutput;

    if (m_fDisposed)
    {
        return;
    }
    m_fDisposed = true;

    fflush(stdout);
    fflush(stderr);

    if (m_hErrWritePipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrWritePipe);
        m_hErrWritePipe = INVALID_HANDLE_VALUE;
    }

    if (m_hErrThread != NULL &&
        GetExitCodeThread(m_hErrThread, &dwThreadStatus) != 0 &&
        dwThreadStatus == STILL_ACTIVE)
    {
        // wait for gracefullshut down, i.e., the exit of the background thread or timeout
        if (WaitForSingleObject(m_hErrThread, PIPE_OUTPUT_THREAD_TIMEOUT) != WAIT_OBJECT_0)
        {
            // if the thread is still running, we need kill it first before exit to avoid AV
            if (GetExitCodeThread(m_hErrThread, &dwThreadStatus) != 0 && dwThreadStatus == STILL_ACTIVE)
            {
                TerminateThread(m_hErrThread, STATUS_CONTROL_C_EXIT);
            }
        }
    }

    CloseHandle(m_hErrThread);
    m_hErrThread = NULL;

    if (m_hErrReadPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrReadPipe);
        m_hErrReadPipe = INVALID_HANDLE_VALUE;
    }

    // Restore the original stdout and stderr handles of the process,
    // as the application has either finished startup or has exited.
    if (_dup2(m_fdStdOut, _fileno(stdout)) == -1)
    {
        return;
    }

    if (_dup2(m_fdStdErr, _fileno(stderr)) == -1)
    {
        return;
    }

    if (GetStdOutContent(&straStdOutput))
    {
        printf(straStdOutput.QueryStr());
        // Need to flush contents.
        _flushall();
    }
}

HRESULT PipeOutputManager::Start()
{
    HRESULT hr = S_OK;
    SECURITY_ATTRIBUTES     saAttr = { 0 };
    HANDLE                  hStdErrReadPipe;
    HANDLE                  hStdErrWritePipe;

    m_fdStdOut = _dup(_fileno(stdout));
    if (m_fdStdOut == -1)
    {
        hr = E_HANDLE;
        goto Finished;
    }
    m_fdStdErr = _dup(_fileno(stderr));
    if (m_fdStdErr == -1)
    {
        hr = E_HANDLE;
        goto Finished;
    }

    if (!CreatePipe(&hStdErrReadPipe, &hStdErrWritePipe, &saAttr, 0 /*nSize*/))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    // TODO this still doesn't redirect calls in native, like wprintf
    if (!SetStdHandle(STD_ERROR_HANDLE, hStdErrWritePipe))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (!SetStdHandle(STD_OUTPUT_HANDLE, hStdErrWritePipe))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    m_hErrReadPipe = hStdErrReadPipe;
    m_hErrWritePipe = hStdErrWritePipe;

    // Read the stderr handle on a separate thread until we get 4096 bytes.
    m_hErrThread = CreateThread(
        NULL,       // default security attributes
        0,          // default stack size
        (LPTHREAD_START_ROUTINE)ReadStdErrHandle,
        this,       // thread function arguments
        0,          // default creation flags
        NULL);      // receive thread identifier

    if (m_hErrThread == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

Finished:
    return hr;
}

VOID
PipeOutputManager::ReadStdErrHandle(
    LPVOID pContext
)
{
    PipeOutputManager *pLoggingProvider = (PipeOutputManager*)pContext;
    DBG_ASSERT(pLoggingProvider != NULL);
    pLoggingProvider->ReadStdErrHandleInternal();
}

bool PipeOutputManager::GetStdOutContent(STRA* struStdOutput)
{
    bool fLogged = false;
    if (m_dwStdErrReadTotal > 0)
    {
        if (SUCCEEDED(struStdOutput->Copy(m_pzFileContents, m_dwStdErrReadTotal)))
        {
            fLogged = TRUE;
        }
    }
    return fLogged;
}

VOID
PipeOutputManager::ReadStdErrHandleInternal(
    VOID
)
{
    DWORD dwNumBytesRead = 0;
    while (true)
    {
        if (ReadFile(m_hErrReadPipe, &m_pzFileContents[m_dwStdErrReadTotal], MAX_PIPE_READ_SIZE - m_dwStdErrReadTotal, &dwNumBytesRead, NULL))
        {
            m_dwStdErrReadTotal += dwNumBytesRead;
            if (m_dwStdErrReadTotal >= MAX_PIPE_READ_SIZE)
            {
                break;
            }
        }
        else if (GetLastError() == ERROR_BROKEN_PIPE)
        {
            break;
        }
    }
}

VOID
PipeOutputManager::NotifyStartupComplete()
{
    StopOutputRedirection();
}
