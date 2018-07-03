// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "PipeOutputManager.h"
#include "exceptions.h"
#include "SRWExclusiveLock.h"

#define LOG_IF_DUPFAIL(err) do { if (err == -1) { LOG_IF_FAILED(HRESULT_FROM_WIN32(_doserrno)); } } while (0, 0);
#define LOG_IF_ERRNO(err) do { if (err != 0) { LOG_IF_FAILED(HRESULT_FROM_WIN32(_doserrno)); } } while (0, 0);

PipeOutputManager::PipeOutputManager() :
    m_dwStdErrReadTotal(0),
    m_hErrReadPipe(INVALID_HANDLE_VALUE),
    m_hErrWritePipe(INVALID_HANDLE_VALUE),
    m_hErrThread(NULL),
    m_fDisposed(FALSE)
{
    InitializeSRWLock(&m_srwLock);
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
    SRWExclusiveLock lock(m_srwLock);

    if (m_fDisposed)
    {
        return;
    }
    m_fDisposed = true;

    fflush(stdout);
    fflush(stderr);

    // Restore the original stdout and stderr handles of the process,
    // as the application has either finished startup or has exited.

    // If stdout/stderr were not set, we need to set it to NUL:
    // such that other calls to Console.WriteLine don't use an invalid handle
    FILE *stream;

    if (m_fdPreviousStdOut >= 0)
    {
        LOG_IF_DUPFAIL(_dup2(m_fdPreviousStdOut, _fileno(stdout)));
    }
    else
    {
        LOG_IF_ERRNO(freopen_s(&stream, "NUL:", "w", stdout));
    }

    if (m_fdPreviousStdErr >= 0)
    {
        LOG_IF_DUPFAIL(_dup2(m_fdPreviousStdErr, _fileno(stderr)));
    }
    else
    {
        LOG_IF_ERRNO(freopen_s(&stream, "NUL:", "w", stderr));
    }

    if (m_hErrWritePipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrWritePipe);
        m_hErrWritePipe = INVALID_HANDLE_VALUE;
    }

    // GetExitCodeThread returns 0 on failure; thread status code is invalid.
    if (m_hErrThread != NULL &&
        !LOG_LAST_ERROR_IF(GetExitCodeThread(m_hErrThread, &dwThreadStatus) == 0) &&
        dwThreadStatus == STILL_ACTIVE)
    {
        // wait for graceful shutdown, i.e., the exit of the background thread or timeout
        if (WaitForSingleObject(m_hErrThread, PIPE_OUTPUT_THREAD_TIMEOUT) != WAIT_OBJECT_0)
        {
            // if the thread is still running, we need kill it first before exit to avoid AV
            if (!LOG_LAST_ERROR_IF(GetExitCodeThread(m_hErrThread, &dwThreadStatus) == 0) &&
                dwThreadStatus == STILL_ACTIVE)
            {
                LOG_WARN("Thread reading stdout/err hit timeout, forcibly closing thread.");
                TerminateThread(m_hErrThread, STATUS_CONTROL_C_EXIT);
            }
        }
    }

    if (m_hErrThread != NULL)
    {
        CloseHandle(m_hErrThread);
        m_hErrThread = NULL;
    }


    if (m_hErrReadPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrReadPipe);
        m_hErrReadPipe = INVALID_HANDLE_VALUE;
    }

    if (GetStdOutContent(&straStdOutput))
    {
        printf(straStdOutput.QueryStr());
        // Need to flush contents for the new stdout and stderr
        _flushall();
    }
}

HRESULT PipeOutputManager::Start()
{
    SECURITY_ATTRIBUTES     saAttr = { 0 };
    HANDLE                  hStdErrReadPipe;
    HANDLE                  hStdErrWritePipe;

    m_fdPreviousStdOut = _dup(_fileno(stdout));
    LOG_IF_DUPFAIL(m_fdPreviousStdOut);

    m_fdPreviousStdErr = _dup(_fileno(stderr));
    LOG_IF_DUPFAIL(m_fdPreviousStdErr);

    RETURN_LAST_ERROR_IF(!CreatePipe(&hStdErrReadPipe, &hStdErrWritePipe, &saAttr, 0 /*nSize*/));

    // TODO this still doesn't redirect calls in native, like wprintf
    RETURN_LAST_ERROR_IF(!SetStdHandle(STD_ERROR_HANDLE, hStdErrWritePipe));

    RETURN_LAST_ERROR_IF(!SetStdHandle(STD_OUTPUT_HANDLE, hStdErrWritePipe));

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

    RETURN_LAST_ERROR_IF_NULL(m_hErrThread);

    return S_OK;
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
