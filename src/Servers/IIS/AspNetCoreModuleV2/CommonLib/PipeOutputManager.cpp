// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "PipeOutputManager.h"

#include "stdafx.h"
#include "Exceptions.h"
#include "SRWExclusiveLock.h"
#include "StdWrapper.h"
#include "ntassert.h"
#include "StringHelpers.h"

#define LOG_IF_DUPFAIL(err) do { if (err == -1) { LOG_IF_FAILED(HRESULT_FROM_WIN32(_doserrno)); } } while (0, 0);
#define LOG_IF_ERRNO(err) do { if (err != 0) { LOG_IF_FAILED(HRESULT_FROM_WIN32(_doserrno)); } } while (0, 0);

PipeOutputManager::PipeOutputManager()
    : PipeOutputManager( /* fEnableNativeLogging */ true)
{
}

PipeOutputManager::PipeOutputManager(bool fEnableNativeLogging) :
    BaseOutputManager(fEnableNativeLogging),
    m_hErrReadPipe(INVALID_HANDLE_VALUE),
    m_hErrWritePipe(INVALID_HANDLE_VALUE),
    m_hErrThread(nullptr),
    m_numBytesReadTotal(0)
{
}

PipeOutputManager::~PipeOutputManager()
{
    PipeOutputManager::Stop();
}

// Start redirecting stdout and stderr into a pipe
// Continuously read the pipe on a background thread
// until Stop is called.
void PipeOutputManager::Start()
{
    SECURITY_ATTRIBUTES     saAttr = { 0 };
    HANDLE                  hStdErrReadPipe;
    HANDLE                  hStdErrWritePipe;

    // To make Console.* functions work, allocate a console
    // in the current process.
    if (!AllocConsole())
    {
        // ERROR_ACCESS_DENIED means there is a console already present.
        if (GetLastError() != ERROR_ACCESS_DENIED)
        {
            THROW_LAST_ERROR();
        }
    }

    THROW_LAST_ERROR_IF(!CreatePipe(&hStdErrReadPipe, &hStdErrWritePipe, &saAttr, 0 /*nSize*/));

    m_hErrReadPipe = hStdErrReadPipe;
    m_hErrWritePipe = hStdErrWritePipe;

    stdoutWrapper = std::make_unique<StdWrapper>(stdout, STD_OUTPUT_HANDLE, hStdErrWritePipe, m_enableNativeRedirection);
    stderrWrapper = std::make_unique<StdWrapper>(stderr, STD_ERROR_HANDLE, hStdErrWritePipe, m_enableNativeRedirection);

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

    THROW_LAST_ERROR_IF_NULL(m_hErrThread);
}

// Stop redirecting stdout and stderr into a pipe
// This closes the background thread reading from the pipe
// and prints any output that was captured in the pipe.
// If more than 30Kb was written to the pipe, that output will
// be thrown away.
void PipeOutputManager::Stop()
{
    DWORD    dwThreadStatus = 0;

    if (m_disposed)
    {
        return;
    }

    SRWExclusiveLock lock(m_srwLock);

    if (m_disposed)
    {
        return;
    }

    m_disposed = true;

    // Both pipe wrappers duplicate the pipe writer handle
    // meaning we are fine to close the handle too.
    if (m_hErrWritePipe != INVALID_HANDLE_VALUE)
    {
        // Flush the pipe writer before closing to capture all output
        THROW_LAST_ERROR_IF(!FlushFileBuffers(m_hErrWritePipe));
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
    m_stdOutContent = to_wide_string(std::string(m_pipeContents, m_numBytesReadTotal), GetConsoleOutputCP());

    if (!m_stdOutContent.empty())
    {
        // printf will fail in in full IIS
        if (wprintf(m_stdOutContent.c_str()) != -1)
        {
            // Need to flush contents for the new stdout and stderr
            _flushall();
        }
    }
}

std::wstring PipeOutputManager::GetStdOutContent()
{
    return m_stdOutContent;
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
            &m_pipeContents[m_numBytesReadTotal],
            MAX_PIPE_READ_SIZE - m_numBytesReadTotal,
            &dwNumBytesRead,
            nullptr))
        {
            m_numBytesReadTotal += dwNumBytesRead;
            if (m_numBytesReadTotal >= MAX_PIPE_READ_SIZE)
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
