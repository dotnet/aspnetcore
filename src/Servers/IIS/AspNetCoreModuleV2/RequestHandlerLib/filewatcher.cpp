// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "filewatcher.h"
#include "debugutil.h"
#include "AppOfflineTrackingApplication.h"
#include "exceptions.h"
#include <EventLog.h>

FILE_WATCHER::FILE_WATCHER() :
    m_hCompletionPort(nullptr),
    m_hChangeNotificationThread(nullptr),
    m_fThreadExit(false),
    m_fShadowCopyEnabled(false),
    m_copied(false)
{
    m_pDoneCopyEvent = CreateEvent(
        nullptr,  // default security attributes
        TRUE,     // manual reset event
        FALSE,    // not set
        nullptr); // name

    // Use of TerminateThread for the file watcher thread was eliminated in favor of an event-based
    // approach. Out of an abundance of caution, we are temporarily adding an environment variable
    // to allow falling back to TerminateThread usage. If all goes well, this will be removed in a
    // future release.
    m_fRudeThreadTermination = false;
    auto enableThreadTerminationValue = Environment::GetEnvironmentVariableValue(L"ASPNETCORE_FILE_WATCHER_THREAD_TERMINATION");
    if (enableThreadTerminationValue.has_value())
    {
        m_fRudeThreadTermination = (enableThreadTerminationValue.value() == L"1");
    }
}

FILE_WATCHER::~FILE_WATCHER()
{
    StopMonitor();
    WaitForWatcherThreadExit();
}

void FILE_WATCHER::WaitForWatcherThreadExit()
{
    if (m_hChangeNotificationThread == nullptr)
    {
        return;
    }

    if (m_fRudeThreadTermination)
    {
        // This is the old behavior, which is now opt-in using an environment variable. Wait for
        // the thread to exit, but if it doesn't exit soon enough, terminate it.
        const int totalWaitTimeMs = 10000;
        const int waitIntervalMs = 50;
        const int iterations = totalWaitTimeMs / waitIntervalMs;
        for (int i = 0; i < iterations && !m_fThreadExit; i++)
        {
            // Check if the thread has exited.
            DWORD result = WaitForSingleObject(m_hChangeNotificationThread, waitIntervalMs);
            if (result == WAIT_OBJECT_0)
            {
                // The thread has exited.
                m_fThreadExit = true;
                break;
            }
        }

        if (!m_fThreadExit)
        {
            LOG_INFO(L"File watcher thread did not exit. Forcing termination.");
            TerminateThread(m_hChangeNotificationThread, 1);
        }
    }
    else
    {
        // Wait for the thread to exit.
        LOG_INFO(L"Waiting for file watcher thread to exit.");
        WaitForSingleObject(m_hChangeNotificationThread, INFINITE);
    }
}

HRESULT
FILE_WATCHER::Create(
    _In_ PCWSTR                  pszDirectoryToMonitor,
    _In_ PCWSTR                  pszFileNameToMonitor,
    _In_ const std::wstring&     shadowCopyPath,
    _In_ AppOfflineTrackingApplication* pApplication,
    _In_ DWORD                   shutdownTimeout
)
{
    m_shadowCopyPath = shadowCopyPath;
    m_fShadowCopyEnabled = !shadowCopyPath.empty();
    m_shutdownTimeout = shutdownTimeout;

    RETURN_LAST_ERROR_IF_NULL(m_hCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, nullptr, 0, 0));

    RETURN_LAST_ERROR_IF_NULL(m_hChangeNotificationThread = CreateThread(nullptr,
        0,
        (LPTHREAD_START_ROUTINE)ChangeNotificationThread,
        this,
        0,
        NULL));

    if (pszDirectoryToMonitor == nullptr ||
        pszFileNameToMonitor == nullptr ||
        pApplication == nullptr)
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
        nullptr,
        OPEN_EXISTING,
        FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OVERLAPPED,
        NULL);

    RETURN_LAST_ERROR_IF(_hDirectory == INVALID_HANDLE_VALUE);

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
    FILE_WATCHER* pFileMonitor = (FILE_WATCHER*)pvArg;
    
    LOG_INFO(L"Starting file watcher thread");
    DBG_ASSERT(pFileMonitor != nullptr);

    while (true)
    {

        DWORD       cbCompletion = 0;
        OVERLAPPED* pOverlapped = nullptr;
        ULONG_PTR   completionKey;

        BOOL success = GetQueuedCompletionStatus(
            pFileMonitor->m_hCompletionPort,
            &cbCompletion,
            &completionKey,
            &pOverlapped,
            INFINITE);

        DBG_ASSERT(success);
        (void)success;

        if (completionKey == FILE_WATCHER_SHUTDOWN_KEY)
        {
            break;
        }

        DBG_ASSERT(pOverlapped != nullptr);
        if (pOverlapped != nullptr)
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
    }

    pFileMonitor->m_fThreadExit = true;

    if (pFileMonitor->m_fShadowCopyEnabled)
    {
        // Cancel the timer to avoid it calling copy.
        pFileMonitor->m_Timer.CancelTimer();
        FILE_WATCHER::CopyAndShutdown(pFileMonitor);
    }

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
    BOOL                        fAppOfflineChanged = FALSE;
    BOOL                        fDllChanged = FALSE;

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
    // Otherwise we have to cache the file info
    //
    if (cbCompletion == 0)
    {
        fAppOfflineChanged = TRUE;
    }
    else
    {
        auto pNotificationInfo = (FILE_NOTIFY_INFORMATION*)_buffDirectoryChanges.QueryPtr();
        DBG_ASSERT(pNotificationInfo != nullptr);

        while (pNotificationInfo != nullptr)
        {
            //
            // check whether the monitored file got changed
            //
            if (_wcsnicmp(pNotificationInfo->FileName,
                _strFileName.QueryStr(),
                pNotificationInfo->FileNameLength / sizeof(WCHAR)) == 0)
            {
                fAppOfflineChanged = TRUE;
                auto app = _pApplication.get();
                app->m_detectedAppOffline = true;
                break;
            }

            //
            // Look for changes to dlls when shadow copying is enabled.
            //

            if (m_fShadowCopyEnabled)
            {
                std::wstring notification(pNotificationInfo->FileName, pNotificationInfo->FileNameLength / sizeof(WCHAR));
                std::filesystem::path notificationPath(notification);
                if (notificationPath.extension().compare(L".dll") == 0)
                {
                    fDllChanged = TRUE;
                }
            }

            //
            // Advance to next notification
            //
            if (pNotificationInfo->NextEntryOffset == 0)
            {
                pNotificationInfo = nullptr;
            }
            else
            {
                pNotificationInfo = (FILE_NOTIFY_INFORMATION*)
                    ((PBYTE)pNotificationInfo +
                        pNotificationInfo->NextEntryOffset);
            }
        }
    }

    if (fAppOfflineChanged && !_lStopMonitorCalled)
    {
        // Reference application before
        _pApplication->ReferenceApplication();
        RETURN_LAST_ERROR_IF(!QueueUserWorkItem(RunNotificationCallback, _pApplication.get(), WT_EXECUTEDEFAULT));
    }

    if (fDllChanged && m_fShadowCopyEnabled && !_lStopMonitorCalled)
    {
        // Reset timer for dll checks
        LOG_INFO(L"Detected dll change, resetting timer callback which will eventually trigger shutdown.");
        m_Timer.CancelTimer();
        m_Timer.InitializeTimer(FILE_WATCHER::TimerCallback, this, 5000, INFINITE);
    }

    return S_OK;
}


VOID
CALLBACK
FILE_WATCHER::TimerCallback(
    _In_ PTP_CALLBACK_INSTANCE Instance,
    _In_ PVOID Context,
    _In_ PTP_TIMER Timer
)
{
    UNREFERENCED_PARAMETER(Instance);
    UNREFERENCED_PARAMETER(Timer);
    CopyAndShutdown((FILE_WATCHER*)Context);
}

DWORD WINAPI FILE_WATCHER::CopyAndShutdown(FILE_WATCHER* watcher)
{
    // Only copy and shutdown once
    SRWExclusiveLock lock(watcher->m_copyLock);
    if (watcher->m_copied)
    {
        return 0;
    }

    watcher->m_copied = true;

    LOG_INFO(L"Starting copy on shutdown in file watcher, creating directory.");

    auto directoryNameInt = 0;
    auto currentShadowCopyDirectory = std::filesystem::path(watcher->m_shadowCopyPath);
    auto parentDirectory = currentShadowCopyDirectory.parent_path();
    try
    {
        directoryNameInt = std::stoi(currentShadowCopyDirectory.filename().string());
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        return 0;
    }

    // Add one to the directory we want to copy to.
    directoryNameInt++;
    auto destination = parentDirectory / std::to_wstring(directoryNameInt);

    LOG_INFOF(L"Copying new shadow copy directory to %ls.", destination.wstring().c_str());
    int copiedFileCount = 0;

    // Copy contents before shutdown
    try
    {
        Environment::CopyToDirectory(watcher->_strDirectoryName.QueryStr(), destination, false, parentDirectory, copiedFileCount);
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        return 0;
    }

    LOG_INFOF(L"Finished copy on shutdown to %ls. %d files copied.", destination.wstring().c_str(), copiedFileCount);

    SetEvent(watcher->m_pDoneCopyEvent);

    // reference application before callback (same thing we do with app_offline).
    watcher->_pApplication->ReferenceApplication();
    QueueUserWorkItem(RunNotificationCallback, watcher->_pApplication.get(), WT_EXECUTEDEFAULT);

    return 0;
}

DWORD
WINAPI
FILE_WATCHER::RunNotificationCallback(
    LPVOID  pvArg
)
{
    // Recapture application instance into unique_ptr
    auto pApplication = std::unique_ptr<AppOfflineTrackingApplication, IAPPLICATION_DELETER>(static_cast<AppOfflineTrackingApplication*>(pvArg));
    pApplication->OnAppOffline();

    return 0;
}

HRESULT
FILE_WATCHER::Monitor(VOID)
{
    DWORD   cbRead;
    ZeroMemory(&_overlapped, sizeof(_overlapped));

    RETURN_LAST_ERROR_IF(!ReadDirectoryChangesW(_hDirectory,
        _buffDirectoryChanges.QueryPtr(),
        _buffDirectoryChanges.QuerySize(),
        FALSE,        // Watching sub dirs. Set to False now as only monitoring app_offline
        FILE_NOTIFY_VALID_MASK & ~FILE_NOTIFY_CHANGE_LAST_ACCESS,
        &cbRead,
        &_overlapped,
        nullptr));

    // Check if file exist because ReadDirectoryChangesW would not fire events for existing files
    if (GetFileAttributes(_strFullName.QueryStr()) != INVALID_FILE_ATTRIBUTES)
    {
        PostQueuedCompletionStatus(m_hCompletionPort, 0, 0, &_overlapped);
    }

    return S_OK;
}

VOID
FILE_WATCHER::StopMonitor()
{
    //
    // Flag that monitoring is being stopped so that
    // we know that HandleChangeCompletion() call
    // can be ignored
    //
    if (InterlockedExchange(&_lStopMonitorCalled, 1) == 1)
    {
        return;
    }

    LOG_INFO(L"Stopping file watching.");

    // Signal the file watcher thread to exit
    PostQueuedCompletionStatus(m_hCompletionPort, 0, FILE_WATCHER_SHUTDOWN_KEY, NULL);
    WaitForWatcherThreadExit();

    if (m_fShadowCopyEnabled)
    {
        // If we are shadow copying, wait for the copying to finish.
        WaitForSingleObject(m_pDoneCopyEvent, m_shutdownTimeout);
    }

    // Release application reference
    _pApplication.reset(nullptr);
}
