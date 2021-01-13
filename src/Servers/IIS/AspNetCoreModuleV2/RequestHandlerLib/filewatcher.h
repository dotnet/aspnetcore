// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


#pragma once

#include <Windows.h>
#include <functional>
#include "iapplication.h"
#include "HandleWrapper.h"

#define FILE_WATCHER_SHUTDOWN_KEY           (ULONG_PTR)(-1)
#define FILE_WATCHER_ENTRY_BUFFER_SIZE      4096
#define FILE_NOTIFY_VALID_MASK              0x00000fff

class AppOfflineTrackingApplication;

class FILE_WATCHER{
public:

    FILE_WATCHER();

    ~FILE_WATCHER();

    HRESULT Create(
        _In_ PCWSTR                  pszDirectoryToMonitor,
        _In_ PCWSTR                  pszFileNameToMonitor,
        _In_ AppOfflineTrackingApplication *pApplication
    );

    static
    DWORD
    WINAPI ChangeNotificationThread(LPVOID);

    static
    DWORD
    WINAPI RunNotificationCallback(LPVOID);

    HRESULT HandleChangeCompletion(DWORD cbCompletion);

    HRESULT Monitor();
    void StopMonitor();

private:
    HandleWrapper<NullHandleTraits>               m_hCompletionPort;
    HandleWrapper<NullHandleTraits>               m_hChangeNotificationThread;
    HandleWrapper<NullHandleTraits>               _hDirectory;
    volatile   BOOL      m_fThreadExit;

    BUFFER                  _buffDirectoryChanges;
    STRU                    _strFileName;
    STRU                    _strDirectoryName;
    STRU                    _strFullName;
    LONG                    _lStopMonitorCalled {};
    OVERLAPPED              _overlapped;
    std::unique_ptr<AppOfflineTrackingApplication, IAPPLICATION_DELETER> _pApplication;
};
