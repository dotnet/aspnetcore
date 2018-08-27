// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "PipeOutputManager.h"
#include "exceptions.h"
#include "SRWExclusiveLock.h"
#include "StdWrapper.h"
#include "ntassert.h"

#define LOG_IF_DUPFAIL(err) do { if (err == -1) { LOG_IF_FAILED(HRESULT_FROM_WIN32(_doserrno)); } } while (0, 0);
#define LOG_IF_ERRNO(err) do { if (err != 0) { LOG_IF_FAILED(HRESULT_FROM_WIN32(_doserrno)); } } while (0, 0);

PipeOutputManager::PipeOutputManager()
    : PipeOutputManager( /* fEnableNativeLogging */ true)
{
}

PipeOutputManager::PipeOutputManager(bool fEnableNativeLogging) :
    m_hErrReadPipe(INVALID_HANDLE_VALUE),
    m_hErrWritePipe(INVALID_HANDLE_VALUE),
    m_hErrThread(nullptr),
    m_dwStdErrReadTotal(0),
    m_disposed(FALSE),
    m_fEnableNativeRedirection(fEnableNativeLogging),
    stdoutWrapper(nullptr),
    stderrWrapper(nullptr)
{
    InitializeSRWLock(&m_srwLock);
}

PipeOutputManager::~PipeOutputManager()
{
    PipeOutputManager::Stop();
}

// Start redirecting stdout and stderr into a pipe
// Continuously read the pipe on a background thread
// until Stop is called.
HRESULT PipeOutputManager::Start()
{
    SECURITY_ATTRIBUTES     saAttr = { 0 };
    HANDLE                  hStdErrReadPipe;
    HANDLE                  hStdErrWritePipe;

    RETURN_LAST_ERROR_IF(!CreatePipe(&hStdErrReadPipe, &hStdErrWritePipe, &saAttr, 0 /*nSize*/));

    m_hErrReadPipe = hStdErrReadPipe;
    m_hErrWritePipe = hStdErrWritePipe;

    stdoutWrapper = std::make_unique<StdWrapper>(stdout, STD_OUTPUT_HANDLE, hStdErrWritePipe, m_fEnableNativeRedirection);
    stderrWrapper = std::make_unique<StdWrapper>(stderr, STD_ERROR_HANDLE, hStdErrWritePipe, m_fEnableNativeRedirection);

    LOG_IF_FAILED(stdoutWrapper->StartRedirection());
    LOG_IF_FAILED(stderrWrapper->StartRedirection());

    // Read the stderr handle on a separate thread until we get 30Kb.
    m_hErrThread = CreateThread(
        nullptr,       // default security attributes
        0,          // default stack size
        reinterpret_cast<LPTHREAD_START_ROUTINE>(ReadStdErrHandle),
        this,       // thread function arguments
        0,          // default creation flags
        nullptr);      // receive thread identifier

    RETURN_LAST_ERROR_IF_NULL(m_hErrThread);

    return S_OK;
}

// Stop redirecting stdout and stderr into a pipe
// This closes the background thread reading from the pipe
// and prints any output that was captured in the pipe.
// If more than 30Kb was written to the pipe, that output will
// be thrown away.
HRESULT PipeOutputManager::Stop()
{
    DWORD    dwThreadStatus = 0;
    STRA     straStdOutput;

    if (m_disposed)
    {
        return S_OK;
    }

    SRWExclusiveLock lock(m_srwLock);

    if (m_disposed)
    {
        return S_OK;
    }

    m_disposed = true;

    // Both pipe wrappers duplicate the pipe writer handle
    // meaning we are fine to close the handle too.
    if (m_hErrWritePipe != INVALID_HANDLE_VALUE)
    {
        // Flush the pipe writer before closing to capture all output
        RETURN_LAST_ERROR_IF(!FlushFileBuffers(m_hErrWritePipe));
        CloseHandle(m_hErrWritePipe);
        m_hErrWritePipe = INVALID_HANDLE_VALUE;
    }

    // Tell each pipe wrapper to stop redirecting output and restore the original values
    if (stdoutWrapper != nullptr)
    {
        LOG_IF_FAILED(stdoutWrapper->StopRedirection());
    }

    if (stderrWrapper != nullptr)
    {
        LOG_IF_FAILED(stderrWrapper->StopRedirection());
    }

    // Forces ReadFile to cancel, causing the read loop to complete.
    // Don't check return value as IO may or may not be completed already.
    if (m_hErrThread != nullptr)
    {
        CancelSynchronousIo(m_hErrThread);
    }

    // GetExitCodeThread returns 0 on failure; thread status code is invalid.
    if (m_hErrThread != nullptr &&
        !LOG_LAST_ERROR_IF(GetExitCodeThread(m_hErrThread, &dwThreadStatus) == 0) &&
        dwThreadStatus == STILL_ACTIVE)
    {
        // Wait for graceful shutdown, i.e., the exit of the background thread or timeout
        if (WaitForSingleObject(m_hErrThread, PIPE_OUTPUT_THREAD_TIMEOUT) != WAIT_OBJECT_0)
        {
            // If the thread is still running, we need kill it first before exit to avoid AV
            if (!LOG_LAST_ERROR_IF(GetExitCodeThread(m_hErrThread, &dwThreadStatus) == 0) &&
                dwThreadStatus == STILL_ACTIVE)
            {
                LOG_WARN(L"Thread reading stdout/err hit timeout, forcibly closing thread.");
                TerminateThread(m_hErrThread, STATUS_CONTROL_C_EXIT);
            }
        }
    }

    if (m_hErrThread != nullptr)
    {
        CloseHandle(m_hErrThread);
        m_hErrThread = nullptr;
    }

    if (m_hErrReadPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrReadPipe);
        m_hErrReadPipe = INVALID_HANDLE_VALUE;
    }

    // If we captured any output, relog it to the original stdout
    // Useful for the IIS Express scenario as it is running with stdout and stderr
    if (GetStdOutContent(&straStdOutput))
    {
        // printf will fail in in full IIS
        if (printf(straStdOutput.QueryStr()) != -1)
        {
            // Need to flush contents for the new stdout and stderr
            _flushall();
        }
    }

    return S_OK;
}

bool PipeOutputManager::GetStdOutContent(STRA* straStdOutput)
{
    bool fLogged = false;

    // TODO consider returning the file contents rather than copying.
    if (m_dwStdErrReadTotal > 0)
    {
        if (SUCCEEDED(straStdOutput->Copy(m_pzFileContents, m_dwStdErrReadTotal)))
        {
            fLogged = TRUE;
        }
    }

    return fLogged;
}

void
PipeOutputManager::ReadStdErrHandle(
    LPVOID pContext
)
{
    auto pLoggingProvider = static_cast<PipeOutputManager*>(pContext);
    DBG_ASSERT(pLoggingProvider != NULL);
    pLoggingProvider->ReadStdErrHandleInternal();
}

void
PipeOutputManager::ReadStdErrHandleInternal()
{
    // If ReadFile ever returns false, exit the thread
    DWORD dwNumBytesRead = 0;
    while (true)
    {
        // Fill a maximum of MAX_PIPE_READ_SIZE into a buffer.
        if (ReadFile(m_hErrReadPipe,
            &m_pzFileContents[m_dwStdErrReadTotal],
            MAX_PIPE_READ_SIZE - m_dwStdErrReadTotal,
            &dwNumBytesRead,
            nullptr))
        {
            m_dwStdErrReadTotal += dwNumBytesRead;
            if (m_dwStdErrReadTotal >= MAX_PIPE_READ_SIZE)
            {
                break;
            }
        }
        else
        {
            return;
        }
    }

    // Using std::string as a wrapper around new char[] so we don't need to call delete
    // Also don't allocate on stack as stack size is 128KB by default.
    std::string tempBuffer; 
    tempBuffer.resize(MAX_PIPE_READ_SIZE);

    // After reading the maximum amount of data, keep reading in a loop until Stop is called on the output manager.
    while (true)
    {
        if (!ReadFile(m_hErrReadPipe, tempBuffer.data(), MAX_PIPE_READ_SIZE, &dwNumBytesRead, nullptr))
        {
            return;
        }
    }
}
