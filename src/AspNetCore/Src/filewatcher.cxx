// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

FILE_WATCHER::FILE_WATCHER() :
    m_hCompletionPort(NULL),
    m_hChangeNotificationThread(NULL)
{
    InitializeCriticalSection(&this->m_csSyncRoot);
}

FILE_WATCHER::~FILE_WATCHER()
{
    DeleteCriticalSection(&this->m_csSyncRoot);
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
        ChangeNotificationThread,
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
    BOOL                 fRet = FALSE;
    DWORD                cbCompletion = 0;
    OVERLAPPED *         pOverlapped = NULL;
    DWORD                dwErrorStatus;
    ULONG_PTR            completionKey;

    pFileMonitor = (FILE_WATCHER*)pvArg;
    while (TRUE)
    {
        fRet = GetQueuedCompletionStatus(
            pFileMonitor->m_hCompletionPort,
            &cbCompletion,
            &completionKey,
            &pOverlapped,
            INFINITE);

        dwErrorStatus = fRet ? ERROR_SUCCESS : GetLastError();

        if (completionKey == FILE_WATCHER_SHUTDOWN_KEY)
        {
            continue;
        }

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
    pMonitorEntry->HandleChangeCompletion(dwCompletionStatus,
        cbCompletion);
}


FILE_WATCHER_ENTRY::FILE_WATCHER_ENTRY(FILE_WATCHER *   pFileMonitor) :
    _pFileMonitor(pFileMonitor),
    _hDirectory(INVALID_HANDLE_VALUE),
    _hImpersonationToken(NULL),
    _pApplication(NULL),
    _lStopMonitorCalled(0)
{
    _dwSignature = FILE_WATCHER_ENTRY_SIGNATURE;
    InitializeSRWLock(&_srwLock);
}

FILE_WATCHER_ENTRY::~FILE_WATCHER_ENTRY()
{
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
        hr = S_OK;
        goto Finished;
    }

    if (cbCompletion == 0)
    {
        //
        // There could be a FCN overflow
        // Let assume the file got changed instead of checking files 
        // Othersie we have to cache the file info 
        // 

        fFileChanged = TRUE;
        hr = HRESULT_FROM_WIN32(dwCompletionStatus);
    }
    else
    {
        pNotificationInfo = (FILE_NOTIFY_INFORMATION*)_buffDirectoryChanges.QueryPtr();
        _ASSERT(pNotificationInfo != NULL);

        while (pNotificationInfo != NULL)
        {
            //
            // check whether the monitored file got changed
            //
            if (wcscmp(pNotificationInfo->FileName, _strFileName.QueryStr()) == 0)
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

        RtlZeroMemory(_buffDirectoryChanges.QueryPtr(), _buffDirectoryChanges.QuerySize());
    }
    //
    //continue monitoring
    //
    StopMonitor();

    if (fFileChanged)
    {
        //
        // so far we only monitoring app_offline
        //
        _pApplication->UpdateAppOfflineFileHandle();
    }

    hr = Monitor();

Finished:
    return hr;
}

HRESULT
FILE_WATCHER_ENTRY::Monitor(VOID)
{
    HRESULT hr = S_OK;
    BOOL    fRet = FALSE;
    DWORD   cbRead;

    AcquireSRWLockExclusive(&_srwLock);

    ZeroMemory(&_overlapped, sizeof(_overlapped));
    if (_hDirectory != INVALID_HANDLE_VALUE)
    {
        CloseHandle(_hDirectory);
        _hDirectory = INVALID_HANDLE_VALUE;
    }

    _hDirectory = CreateFileW(_strDirectoryName.QueryStr(),
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
    // Resize change buffer to something "reasonable"
    //
    fRet = _buffDirectoryChanges.Resize(4096);
    if (!fRet)
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
        goto Finished;
    }

    fRet = ReadDirectoryChangesW(_hDirectory,
        _buffDirectoryChanges.QueryPtr(),
        _buffDirectoryChanges.QuerySize(),
        FALSE,        // watch sub dirs. set to False now as only monitoring app_offline
        FILE_NOTIFY_VALID_MASK & ~FILE_NOTIFY_CHANGE_LAST_ACCESS & ~FILE_NOTIFY_CHANGE_ATTRIBUTES,
        &cbRead,
        &_overlapped,
        NULL);

    if (!fRet)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
    }

    InterlockedExchange(&_lStopMonitorCalled, 0);

Finished:

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

    AcquireSRWLockExclusive(&_srwLock);

    if (_hDirectory != INVALID_HANDLE_VALUE)
    {
        CloseHandle(_hDirectory);
        _hDirectory = INVALID_HANDLE_VALUE;
    }

    ReleaseSRWLockExclusive(&_srwLock);
}

HRESULT
FILE_WATCHER_ENTRY::Create(
    _In_ PCWSTR                  pszDirectoryToMonitor,
    _In_ PCWSTR                  pszFileNameToMonitor,
    _In_ APPLICATION*            pApplication,
    _In_ HANDLE                  hImpersonationToken
)
{
    HRESULT             hr = S_OK;
    BOOL                fRet = FALSE;

    if (pszDirectoryToMonitor == NULL ||
        pszFileNameToMonitor == NULL ||
        pApplication == NULL)
    {
        _ASSERT(FALSE);
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
        goto Finished;
    }

    //
    //remember the application
    //
    _pApplication = pApplication;

    if (FAILED(hr = _strFileName.Copy(pszFileNameToMonitor)))
    {
        goto Finished;
    }

    if (FAILED(hr = _strDirectoryName.Copy(pszDirectoryToMonitor)))
    {
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
    //
    // Start monitoring
    //
    hr = Monitor();

Finished:

    return hr;
}