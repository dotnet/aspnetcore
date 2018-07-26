// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


#pragma once

#include <Windows.h>
#include <functional>

#define FILE_WATCHER_SHUTDOWN_KEY           (ULONG_PTR)(-1)
#define FILE_WATCHER_ENTRY_BUFFER_SIZE      4096
#define FILE_NOTIFY_VALID_MASK              0x00000fff
#define FILE_WATCHER_ENTRY_SIGNATURE       ((DWORD) 'FWES')
#define FILE_WATCHER_ENTRY_SIGNATURE_FREE  ((DWORD) 'sewf')

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
    volatile   BOOL      m_fThreadExit;
};

class FILE_WATCHER_ENTRY
{
public:
    FILE_WATCHER_ENTRY(FILE_WATCHER *   pFileMonitor);

    OVERLAPPED    _overlapped;

    HRESULT
    Create(
        _In_ PCWSTR                  pszDirectoryToMonitor,
        _In_ PCWSTR                  pszFileNameToMonitor,
        _In_ std::function<void()>   pCallback,
        _In_ HANDLE                  hImpersonationToken
        );

    VOID
    ReferenceFileWatcherEntry() const
    {
        InterlockedIncrement(&_cRefs);
    }

    VOID
    DereferenceFileWatcherEntry() const
    {
        if (InterlockedDecrement(&_cRefs) == 0)
        {
            delete this;
        }
    }

    BOOL
    QueryIsValid() const
    {
        return _fIsValid;
    }

    VOID
    MarkEntryInValid()
    {
        _fIsValid = FALSE;
    }

    HRESULT Monitor();

    VOID StopMonitor();

    HRESULT
    HandleChangeCompletion(
        _In_ DWORD          dwCompletionStatus,
        _In_ DWORD          cbCompletion
        );

private:
    virtual ~FILE_WATCHER_ENTRY();

    DWORD                   _dwSignature;
    BUFFER                  _buffDirectoryChanges;
    HANDLE                  _hImpersonationToken;
    HANDLE                  _hDirectory;
    FILE_WATCHER*           _pFileMonitor;
    STRU                    _strFileName;
    STRU                    _strDirectoryName;
    STRU                    _strFullName;
    LONG                    _lStopMonitorCalled;
    mutable LONG            _cRefs;
    BOOL                    _fIsValid;
    SRWLOCK                 _srwLock;
    std::function<void()>   _pCallback;
};


struct FILE_WATCHER_ENTRY_DELETER
{
    void operator()(FILE_WATCHER_ENTRY* entry) const
    {
        entry->DereferenceFileWatcherEntry();
    }
};
