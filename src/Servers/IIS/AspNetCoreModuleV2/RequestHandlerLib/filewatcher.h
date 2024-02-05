// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


#pragma once

#include <Windows.h>
#include <functional>
#include "iapplication.h"
#include "HandleWrapper.h"
#include "Environment.h"
#include <sttimer.h>

#define FILE_WATCHER_SHUTDOWN_KEY           (ULONG_PTR)(-1)
#define FILE_WATCHER_ENTRY_BUFFER_SIZE      4096
#define FILE_NOTIFY_VALID_MASK              0x00000fff

class AppOfflineTrackingApplication;

class FILE_WATCHER{
public:

    FILE_WATCHER();

    ~FILE_WATCHER();

    void WaitForWatcherThreadExit();

    HRESULT Create(
        _In_ PCWSTR                  pszDirectoryToMonitor,
        _In_ PCWSTR                  pszFileNameToMonitor,
        _In_ const std::wstring&            shadowCopyPath,
        _In_ AppOfflineTrackingApplication *pApplication,
        _In_ DWORD                   shutdownTimeout
    );

    static
    DWORD
    WINAPI ChangeNotificationThread(LPVOID);

    static
    DWORD
    WINAPI RunNotificationCallback(LPVOID);

    static
    VOID
    WINAPI TimerCallback(_In_ PTP_CALLBACK_INSTANCE Instance,
        _In_ PVOID Context,
        _In_ PTP_TIMER Timer);

    static DWORD WINAPI CopyAndShutdown(FILE_WATCHER* watcher);

    HRESULT HandleChangeCompletion(DWORD cbCompletion);

    HRESULT Monitor();
    void StopMonitor();

private:
    HandleWrapper<NullHandleTraits>               m_hCompletionPort;
    HandleWrapper<NullHandleTraits>               m_hChangeNotificationThread;
    HandleWrapper<NullHandleTraits>               _hDirectory;
    HandleWrapper<NullHandleTraits>               m_pDoneCopyEvent;
    std::atomic_bool        m_fThreadExit;
    STTIMER                 m_Timer;
    SRWLOCK                 m_copyLock{};
    BOOL                    m_copied;

    BUFFER                  _buffDirectoryChanges;
    STRU                    _strFileName;
    STRU                    _strDirectoryName;
    STRU                    _strFullName;
    LONG                    _lStopMonitorCalled {};
    bool                    m_fShadowCopyEnabled;
    std::wstring            m_shadowCopyPath;
    DWORD                   m_shutdownTimeout;
    OVERLAPPED              _overlapped;
    std::unique_ptr<AppOfflineTrackingApplication, IAPPLICATION_DELETER> _pApplication;
    bool                    m_fRudeThreadTermination;
};
