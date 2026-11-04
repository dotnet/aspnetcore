// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define ONE_MINUTE_IN_MILLISECONDS 60000
class SERVER_PROCESS;

class PROCESS_MANAGER
{
public:

    virtual 
    ~PROCESS_MANAGER();

    VOID
    ReferenceProcessManager() const
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceProcessManager() const
    {
        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    HRESULT 
    GetProcess(
        _In_    REQUESTHANDLER_CONFIG      *pConfig,
        _In_    BOOL                        fWebsocketEnabled,
        _Out_   SERVER_PROCESS            **ppServerProcess
    );

    HANDLE
    QueryNULHandle()
    {
        return m_hNULHandle;
    }

    HRESULT
    Initialize(
        VOID
    );

    VOID
    SendShutdownSignal()
    {
        AcquireSRWLockExclusive( &m_srwLock );

        for(DWORD i = 0; i < m_dwProcessesPerApplication; ++i )
        {
            if( m_ppServerProcessList != nullptr && 
                m_ppServerProcessList[i] != nullptr )
            {
                m_ppServerProcessList[i]->SendSignal();
                m_ppServerProcessList[i]->DereferenceServerProcess();
                m_ppServerProcessList[i] = nullptr;
            }
        }

        ReleaseSRWLockExclusive( &m_srwLock );
    }

    VOID 
    ShutdownProcess(
        SERVER_PROCESS* pServerProcess
    )
    {
        AcquireSRWLockExclusive( &m_srwLock );

        ShutdownProcessNoLock( pServerProcess );

        ReleaseSRWLockExclusive( &m_srwLock );
    }

    VOID 
    ShutdownAllProcesses(
    )
    {
        AcquireSRWLockExclusive( &m_srwLock );

        ShutdownAllProcessesNoLock();

        ReleaseSRWLockExclusive( &m_srwLock );
    }

    VOID
    Shutdown(
    )
    {
        if (InterlockedCompareExchange(&m_lStopping, 1L, 0L) == 0L)
        {
            ShutdownAllProcesses();
        }
    }

    VOID 
    IncrementRapidFailCount(
        VOID
    )
    {
        InterlockedIncrement(&m_cRapidFailCount);
    }

    PROCESS_MANAGER() : 
        m_ppServerProcessList(nullptr),
        m_hNULHandle(nullptr),
        m_cRapidFailCount( 0 ),
        m_dwProcessesPerApplication( 1 ),
        m_dwRouteToProcessIndex( 0 ),
        m_fServerProcessListReady(FALSE),
        m_lStopping(0),
        m_cRefs( 1 )
    {
        m_ppServerProcessList = nullptr;
        m_fServerProcessListReady = FALSE;
        InitializeSRWLock( &m_srwLock );
    }

private:

    BOOL 
    RapidFailsPerMinuteExceeded(
        LONG dwRapidFailsPerMinute
    )
    {
        DWORD dwCurrentTickCount = GetTickCount();

        if( (dwCurrentTickCount - m_dwRapidFailTickStart)
             >= ONE_MINUTE_IN_MILLISECONDS )
        {
            //
            // reset counters every minute.
            //

            InterlockedExchange(&m_cRapidFailCount, 0);
            m_dwRapidFailTickStart = dwCurrentTickCount;
        }

        return m_cRapidFailCount > dwRapidFailsPerMinute;
    }

    VOID 
    ShutdownProcessNoLock(
        SERVER_PROCESS* pServerProcess
    )
    {
        for(DWORD i = 0; i < m_dwProcessesPerApplication; ++i )
        {
            if( m_ppServerProcessList != nullptr &&
                m_ppServerProcessList[i] != nullptr &&
                m_ppServerProcessList[i]->GetPort() == pServerProcess->GetPort() )
            {
                // shutdown pServerProcess if not already shutdown.
                m_ppServerProcessList[i]->StopProcess();
                m_ppServerProcessList[i]->DereferenceServerProcess();
                m_ppServerProcessList[i] = nullptr;
            }
        }
    }

    VOID 
    ShutdownAllProcessesNoLock(
        VOID
    )
    {
        for(DWORD i = 0; i < m_dwProcessesPerApplication; ++i )
        {
            if( m_ppServerProcessList != nullptr &&
                m_ppServerProcessList[i] != nullptr)
            {
                // shutdown pServerProcess if not already shutdown.
                m_ppServerProcessList[i]->SendSignal();
                m_ppServerProcessList[i]->DereferenceServerProcess();
                m_ppServerProcessList[i] = nullptr;
            }
        }
    }

    volatile LONG                     m_cRapidFailCount;
    DWORD                             m_dwRapidFailTickStart;
    DWORD                             m_dwProcessesPerApplication;
    volatile DWORD                    m_dwRouteToProcessIndex;

    SRWLOCK                           m_srwLock;
    SERVER_PROCESS                  **m_ppServerProcessList;

    //
    // m_hNULHandle is used to redirect stdout/stderr to NUL.
    // If Createprocess is called to launch a batch file for example,
    // it tries to write to the console buffer by default. It fails to 
    // start if the console buffer is owned by the parent process i.e 
    // in our case w3wp.exe. So we have to redirect the stdout/stderr
    // of the child process to NUL or to a file (anything other than
    // the console buffer of the parent process).
    //

    HANDLE                            m_hNULHandle;
    mutable LONG                      m_cRefs;

    volatile static BOOL              sm_fWSAStartupDone;
    volatile BOOL                     m_fServerProcessListReady;
    volatile LONG                     m_lStopping;
};
