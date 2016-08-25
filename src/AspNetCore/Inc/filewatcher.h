// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define FILE_WATCHER_SHUTDOWN_KEY           (ULONG_PTR)(-1)
#ifndef CONTAINING_RECORD
//
// Calculate the address of the base of the structure given its type, and an
// address of a field within the structure.
//

#define CONTAINING_RECORD(address, type, field) \
    ((type *)((PCHAR)(address)-(ULONG_PTR)(&((type *)0)->field)))

#endif // !CONTAINING_RECORD
#define FILE_NOTIFY_VALID_MASK          0x00000fff
#define FILE_WATCHER_ENTRY_SIGNATURE       ((DWORD) 'FWES')
#define FILE_WATCHER_ENTRY_SIGNATURE_FREE  ((DWORD) 'sewf')

class APPLICATION;

class FILE_WATCHER{
public:

    FILE_WATCHER();

    ~FILE_WATCHER();

    HRESULT Create();

    HANDLE
    QueryCompletionPort(
        VOID
    ) const
    {
        return m_hCompletionPort;
    }

    static
    DWORD
    WINAPI ChangeNotificationThread(LPVOID);

    static
    void
    WINAPI FileWatcherCompletionRoutine
    (
        DWORD                   dwCompletionStatus,
        DWORD                   cbCompletion,
        OVERLAPPED *            pOverlapped
    );

private:
    HANDLE               m_hCompletionPort;
    HANDLE               m_hChangeNotificationThread;
    CRITICAL_SECTION     m_csSyncRoot;
};

class FILE_WATCHER_ENTRY
{
public:
    FILE_WATCHER_ENTRY(FILE_WATCHER *   pFileMonitor);
    virtual ~FILE_WATCHER_ENTRY();

    OVERLAPPED    _overlapped;

    HRESULT
    Create(
        _In_ PCWSTR                  pszDirectoryToMonitor,
        _In_ PCWSTR                  pszFileNameToMonitor,
        _In_ APPLICATION*            pApplication,
        _In_ HANDLE                  hImpersonationToken
        );

    HRESULT Monitor();

    VOID StopMonitor();

    HRESULT
    HandleChangeCompletion(
        _In_ DWORD          dwCompletionStatus,
        _In_ DWORD          cbCompletion
        );

private:
    DWORD                   _dwSignature;
    BUFFER                  _buffDirectoryChanges;
    HANDLE                  _hImpersonationToken;
    HANDLE                  _hDirectory;
    FILE_WATCHER*           _pFileMonitor;
    APPLICATION*            _pApplication;
    STRU                    _strFileName;
    STRU                    _strDirectoryName;
    LONG                    _lStopMonitorCalled;
    SRWLOCK                 _srwLock;
};
