// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "filewatcher.h"

FILE_WATCHER::FILE_WATCHER() :
    m_hCompletionPort(NULL),
    m_hChangeNotificationThread(NULL),
    m_fThreadExit(FALSE)
{
}

FILE_WATCHER::~FILE_WATCHER()
{
    if (m_hChangeNotificationThread != NULL)
    {
        DWORD dwRetryCounter = 20;      // totally wait for 1s
        DWORD dwExitCode = STILL_ACTIVE;

        // signal the file watch thread to exit
        PostQueuedCompletionStatus(m_hCompletionPort, 0, FILE_WATCHER_SHUTDOWN_KEY, NULL);
        while (!m_fThreadExit && dwRetryCounter > 0)
        {
            if (GetExitCodeThread(m_hChangeNotificationThread, &dwExitCode))
            {
                if (dwExitCode == STILL_ACTIVE)
                {
                    // the file watcher thread will set m_fThreadExit before exit
                    WaitForSingleObject(m_hChangeNotificationThread, 50);
                }
            }
            else
            {
                // fail to get thread status
                // call terminitethread
                TerminateThread(m_hChangeNotificationThread, 1);
                m_fThreadExit = TRUE;
            }
            dwRetryCounter--;
        }

        if (!m_fThreadExit)
        {
            TerminateThread(m_hChangeNotificationThread, 1);
        }

        CloseHandle(m_hChangeNotificationThread);
        m_hChangeNotificationThread = NULL;
    }

    if (NULL != m_hCompletionPort)
    {
        CloseHandle(m_hCompletionPort);
        m_hCompletionPort = NULL;
    }
}

HRESULT
FILE_WATCHER::Create(
    VOID
)
{
    HRESULT                 hr = S_OK;

    m_hCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE,
        NULL,
        0,
        0);

    if (m_hCompletionPort == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    m_hChangeNotificationThread = CreateThread(NULL,
        0,
        (LPTHREAD_START_ROUTINE)ChangeNotificationThread,
        this,
        0,
        NULL);

    if (m_hChangeNotificationThread == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());

        CloseHandle(m_hCompletionPort);
        m_hCompletionPort = NULL;

        goto Finished;
    }

Finished:
    return hr;
}

DWORD
WINAPI
FILE_WATCHER::ChangeNotificationThread(
    LPVOID  pvArg
)
/*++

Routine Description:

IO completion thread

Arguments:

None

Return Value:

Win32 error

--*/
{
    FILE_WATCHER *       pFileMonitor;
    BOOL                 fSuccess = FALSE;
    DWORD                cbCompletion = 0;
    OVERLAPPED *         pOverlapped = NULL;
    DWORD                dwErrorStatus;
    ULONG_PTR            completionKey;

    pFileMonitor = (FILE_WATCHER*)pvArg;
    DBG_ASSERT(pFileMonitor != NULL);

    while (TRUE)
    {
        fSuccess = GetQueuedCompletionStatus(
            pFileMonitor->m_hCompletionPort,
            &cbCompletion,
            &completionKey,
            &pOverlapped,
            INFINITE);

        DBG_ASSERT(fSuccess);
        dwErrorStatus = fSuccess ? ERROR_SUCCESS : GetLastError();

        if (completionKey == FILE_WATCHER_SHUTDOWN_KEY)
        {
            break;
        }

        DBG_ASSERT(pOverlapped != NULL);
        if (pOverlapped != NULL)
        {
            FileWatcherCompletionRoutine(
                dwErrorStatus,
                cbCompletion,
                pOverlapped);
        }
        pOverlapped = NULL;
        cbCompletion = 0;
    }

    pFileMonitor->m_fThreadExit = TRUE;
    ExitThread(0);
}

VOID
WINAPI
FILE_WATCHER::FileWatcherCompletionRoutine(
    DWORD                   dwCompletionStatus,
    DWORD                   cbCompletion,
    OVERLAPPED *            pOverlapped
)
/*++

Routine Description:

Called when ReadDirectoryChangesW() completes

Arguments:

dwCompletionStatus - Error of completion
cbCompletion - Bytes of completion
pOverlapped - State of completion

Return Value:

None

--*/
{
    FILE_WATCHER_ENTRY *     pMonitorEntry;
    pMonitorEntry = CONTAINING_RECORD(pOverlapped, FILE_WATCHER_ENTRY, _overlapped);

    DBG_ASSERT(pMonitorEntry != NULL);

    pMonitorEntry->HandleChangeCompletion(dwCompletionStatus, cbCompletion);

    if (pMonitorEntry->QueryIsValid())
    {
        //
        // Continue monitoring
        //
        pMonitorEntry->Monitor();
    }
    //
    // Deference the counter not matter whether the monitor is valid
    // Valid: Monitor increases the counter, need to reduce one
    // InValid: Reduce the counter to free the entry
    //
    pMonitorEntry->DereferenceFileWatcherEntry();

}


FILE_WATCHER_ENTRY::FILE_WATCHER_ENTRY(FILE_WATCHER *   pFileMonitor) :
    _pFileMonitor(pFileMonitor),
    _hDirectory(INVALID_HANDLE_VALUE),
    _hImpersonationToken(NULL),
    _pCallback(),
    _lStopMonitorCalled(0),
    _cRefs(1),
    _fIsValid(TRUE)
{
    _dwSignature = FILE_WATCHER_ENTRY_SIGNATURE;
    InitializeSRWLock(&_srwLock);
}

FILE_WATCHER_ENTRY::~FILE_WATCHER_ENTRY()
{
    StopMonitor();

    _dwSignature = FILE_WATCHER_ENTRY_SIGNATURE_FREE;

    if (_hDirectory != INVALID_HANDLE_VALUE)
    {
        CloseHandle(_hDirectory);
        _hDirectory = INVALID_HANDLE_VALUE;
    }

    if (_hImpersonationToken != NULL)
    {
        CloseHandle(_hImpersonationToken);
        _hImpersonationToken = NULL;
    }
}

#pragma warning(disable:4100)

HRESULT
FILE_WATCHER_ENTRY::HandleChangeCompletion(
    _In_ DWORD          dwCompletionStatus,
    _In_ DWORD          cbCompletion
)
/*++

Routine Description:

Handle change notification (see if any of associated config files
need to be flushed)

Arguments:

dwCompletionStatus - Completion status
cbCompletion - Bytes of completion

Return Value:

HRESULT

--*/
{
    HRESULT                     hr = S_OK;
    FILE_NOTIFY_INFORMATION *   pNotificationInfo;
    BOOL                        fFileChanged = FALSE;

    AcquireSRWLockExclusive(&_srwLock);
    if (!_fIsValid)
    {
        goto Finished;
    }

    // When directory handle is closed then HandleChangeCompletion
    // happens with cbCompletion = 0 and dwCompletionStatus = 0
    // From documentation it is not clear if that combination
    // of return values is specific to closing handles or whether
    // it could also mean an error condition. Hence we will maintain
    // explicit flag that will help us determine if entry
    // is being shutdown (StopMonitor() called)
    //
    if (_lStopMonitorCalled)
    {
        goto Finished;
    }

    //
    // There could be a FCN overflow
    // Let assume the file got changed instead of checking files
    // Othersie we have to cache the file info
    //
    if (cbCompletion == 0)
    {
        fFileChanged = TRUE;
    }
    else
    {
        pNotificationInfo = (FILE_NOTIFY_INFORMATION*)_buffDirectoryChanges.QueryPtr();
        DBG_ASSERT(pNotificationInfo != NULL);

        while (pNotificationInfo != NULL)
        {
            //
            // check whether the monitored file got changed
            //
            if (_wcsnicmp(pNotificationInfo->FileName,
                _strFileName.QueryStr(),
                pNotificationInfo->FileNameLength / sizeof(WCHAR)) == 0)
            {
                fFileChanged = TRUE;
                break;
            }
            //
            // Advance to next notification
            //
            if (pNotificationInfo->NextEntryOffset == 0)
            {
                pNotificationInfo = NULL;
            }
            else
            {
                pNotificationInfo = (FILE_NOTIFY_INFORMATION*)
                    ((PBYTE)pNotificationInfo +
                        pNotificationInfo->NextEntryOffset);
            }
        }
    }

Finished:
    ReleaseSRWLockExclusive(&_srwLock);

    if (fFileChanged)
    {
        //
        // so far we only monitoring app_offline
        //
        _pCallback();
    }
    return hr;
}

#pragma warning( error : 4100 )

HRESULT
FILE_WATCHER_ENTRY::Monitor(VOID)
{
    HRESULT hr = S_OK;
    DWORD   cbRead;

    AcquireSRWLockExclusive(&_srwLock);
    ReferenceFileWatcherEntry();
    ZeroMemory(&_overlapped, sizeof(_overlapped));

    if (!ReadDirectoryChangesW(_hDirectory,
        _buffDirectoryChanges.QueryPtr(),
        _buffDirectoryChanges.QuerySize(),
        FALSE,        // Watching sub dirs. Set to False now as only monitoring app_offline
        FILE_NOTIFY_VALID_MASK & ~FILE_NOTIFY_CHANGE_LAST_ACCESS,
        &cbRead,
        &_overlapped,
        NULL))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        DereferenceFileWatcherEntry();
    }

    ReleaseSRWLockExclusive(&_srwLock);
    return hr;
}

VOID
FILE_WATCHER_ENTRY::StopMonitor(VOID)
{
    //
    // Flag that monitoring is being stopped so that
    // we know that HandleChangeCompletion() call
    // can be ignored
    //
    InterlockedExchange(&_lStopMonitorCalled, 1);

    if (_hDirectory != INVALID_HANDLE_VALUE)
    {
        AcquireSRWLockExclusive(&_srwLock);
        if (_hDirectory != INVALID_HANDLE_VALUE)
        {
            CloseHandle(_hDirectory);
            _hDirectory = INVALID_HANDLE_VALUE;
            DereferenceFileWatcherEntry();
        }
        ReleaseSRWLockExclusive(&_srwLock);
    }
}

HRESULT
FILE_WATCHER_ENTRY::Create(
    _In_ PCWSTR                  pszDirectoryToMonitor,
    _In_ PCWSTR                  pszFileNameToMonitor,
    _In_ std::function<void()>   pCallback,
    _In_ HANDLE                  hImpersonationToken
)
{
    HRESULT             hr = S_OK;
    BOOL                fRet = FALSE;

    if (pszDirectoryToMonitor == NULL ||
        pszFileNameToMonitor == NULL ||
        pCallback == NULL)
    {
        DBG_ASSERT(FALSE);
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
        goto Finished;
    }

    _pCallback = pCallback;

    if (FAILED(hr = _strFileName.Copy(pszFileNameToMonitor)))
    {
        goto Finished;
    }

    if (FAILED(hr = _strDirectoryName.Copy(pszDirectoryToMonitor)))
    {
        goto Finished;
    }

    //
    // Resize change buffer to something "reasonable"
    //
    if (!_buffDirectoryChanges.Resize(FILE_WATCHER_ENTRY_BUFFER_SIZE))
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
        goto Finished;
    }

    if (hImpersonationToken != NULL)
    {
        fRet = DuplicateHandle(GetCurrentProcess(),
            hImpersonationToken,
            GetCurrentProcess(),
            &_hImpersonationToken,
            0,
            FALSE,
            DUPLICATE_SAME_ACCESS);

        if (!fRet)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
    }
    else
    {
        if (_hImpersonationToken != NULL)
        {
            CloseHandle(_hImpersonationToken);
            _hImpersonationToken = NULL;
        }
    }

    _hDirectory = CreateFileW(
        _strDirectoryName.QueryStr(),
        FILE_LIST_DIRECTORY,
        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OVERLAPPED,
        NULL);

    if (_hDirectory == INVALID_HANDLE_VALUE)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (CreateIoCompletionPort(
        _hDirectory,
        _pFileMonitor->QueryCompletionPort(),
        NULL,
        0) == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    //
    // Start monitoring
    //
    hr = Monitor();

Finished:

    return hr;
}
