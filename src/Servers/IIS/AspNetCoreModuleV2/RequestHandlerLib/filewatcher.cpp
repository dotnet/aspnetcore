// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "filewatcher.h"
#include "debugutil.h"
#include "AppOfflineTrackingApplication.h"
#include "exceptions.h"

FILE_WATCHER::FILE_WATCHER() :
    m_hCompletionPort(NULL),
    m_hChangeNotificationThread(NULL),
    m_fThreadExit(FALSE)
{
}

FILE_WATCHER::~FILE_WATCHER()
{
    StopMonitor();

    if (m_hChangeNotificationThread != NULL)
    {
        DWORD dwRetryCounter = 20;      // totally wait for 1s
        DWORD dwExitCode = STILL_ACTIVE;

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
    }
}

HRESULT
FILE_WATCHER::Create(
    _In_ PCWSTR                  pszDirectoryToMonitor,
    _In_ PCWSTR                  pszFileNameToMonitor,
    _In_ AppOfflineTrackingApplication *pApplication
)
{

    RETURN_LAST_ERROR_IF_NULL(m_hCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0));

    RETURN_LAST_ERROR_IF_NULL(m_hChangeNotificationThread = CreateThread(NULL,
        0,
        (LPTHREAD_START_ROUTINE)ChangeNotificationThread,
        this,
        0,
        NULL));

    if (pszDirectoryToMonitor == NULL ||
        pszFileNameToMonitor == NULL ||
        pApplication == NULL)
    {
        DBG_ASSERT(FALSE);
        return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
    }

    _pApplication = ReferenceApplication(pApplication);

    RETURN_IF_FAILED(_strFileName.Copy(pszFileNameToMonitor));
    RETURN_IF_FAILED(_strDirectoryName.Copy(pszDirectoryToMonitor));
    RETURN_IF_FAILED(_strFullName.Append(_strDirectoryName));
    RETURN_IF_FAILED(_strFullName.Append(_strFileName));

    //
    // Resize change buffer to something "reasonable"
    //
    RETURN_LAST_ERROR_IF(!_buffDirectoryChanges.Resize(FILE_WATCHER_ENTRY_BUFFER_SIZE));

    _hDirectory = CreateFileW(
        _strDirectoryName.QueryStr(),
        FILE_LIST_DIRECTORY,
        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OVERLAPPED,
        NULL);

    RETURN_LAST_ERROR_IF_NULL(_hDirectory);

    RETURN_LAST_ERROR_IF_NULL(CreateIoCompletionPort(
        _hDirectory,
        m_hCompletionPort,
        NULL,
        0));

    RETURN_IF_FAILED(Monitor());

    return S_OK;
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

    LOG_INFO(L"Starting file watcher thread");
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
            pFileMonitor->HandleChangeCompletion(cbCompletion);

            if (!pFileMonitor->_lStopMonitorCalled)
            {
                //
                // Continue monitoring
                //
                pFileMonitor->Monitor();
            }
        }
        pOverlapped = NULL;
        cbCompletion = 0;
    }

    pFileMonitor->m_fThreadExit = TRUE;

    LOG_INFO(L"Stopping file watcher thread");
    ExitThread(0);
}


HRESULT
FILE_WATCHER::HandleChangeCompletion(
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
        return S_OK;
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
        auto pNotificationInfo = (FILE_NOTIFY_INFORMATION*)_buffDirectoryChanges.QueryPtr();
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

    if (fFileChanged && !_lStopMonitorCalled)
    {
        // Reference application before
        _pApplication->ReferenceApplication();
        RETURN_LAST_ERROR_IF(!QueueUserWorkItem(RunNotificationCallback, _pApplication.get(), WT_EXECUTEDEFAULT));
    }

    return S_OK;
}

DWORD
WINAPI
FILE_WATCHER::RunNotificationCallback(
    LPVOID  pvArg
)
{
    // Recapture application instance into unique_ptr
    auto pApplication = std::unique_ptr<AppOfflineTrackingApplication, IAPPLICATION_DELETER>(static_cast<AppOfflineTrackingApplication*>(pvArg));
    DBG_ASSERT(pFileMonitor != NULL);
    pApplication->OnAppOffline();

    return 0;
}

HRESULT
FILE_WATCHER::Monitor(VOID)
{
    HRESULT hr = S_OK;
    DWORD   cbRead;

    ZeroMemory(&_overlapped, sizeof(_overlapped));

    RETURN_LAST_ERROR_IF(!ReadDirectoryChangesW(_hDirectory,
        _buffDirectoryChanges.QueryPtr(),
        _buffDirectoryChanges.QuerySize(),
        FALSE,        // Watching sub dirs. Set to False now as only monitoring app_offline
        FILE_NOTIFY_VALID_MASK & ~FILE_NOTIFY_CHANGE_LAST_ACCESS,
        &cbRead,
        &_overlapped,
        NULL));

    // Check if file exist because ReadDirectoryChangesW would not fire events for existing files
    if (GetFileAttributes(_strFullName.QueryStr()) != INVALID_FILE_ATTRIBUTES)
    {
        PostQueuedCompletionStatus(m_hCompletionPort, 0, 0, &_overlapped);
    }

    return hr;
}

VOID
FILE_WATCHER::StopMonitor()
{
    //
    // Flag that monitoring is being stopped so that
    // we know that HandleChangeCompletion() call
    // can be ignored
    //
    InterlockedExchange(&_lStopMonitorCalled, 1);
    // signal the file watch thread to exit
    PostQueuedCompletionStatus(m_hCompletionPort, 0, FILE_WATCHER_SHUTDOWN_KEY, NULL);
    // Release application reference
    _pApplication.reset(nullptr);
}
