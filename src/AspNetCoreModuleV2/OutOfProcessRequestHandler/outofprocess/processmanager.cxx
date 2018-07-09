// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "..\precomp.hxx"

volatile BOOL               PROCESS_MANAGER::sm_fWSAStartupDone = FALSE;

HRESULT
PROCESS_MANAGER::Initialize(
    VOID
)
{
    HRESULT                              hr       = S_OK;
    WSADATA                              wsaData;
    int                                  result;
    BOOL                                 fLocked = FALSE;

    if( !sm_fWSAStartupDone )
    {
        AcquireSRWLockExclusive( &m_srwLock );
        fLocked = TRUE;

        if( !sm_fWSAStartupDone )
        {
            if( (result = WSAStartup(MAKEWORD(2, 2), &wsaData)) != 0 )
            {
                hr = HRESULT_FROM_WIN32( result );
                goto Finished;
            }
            sm_fWSAStartupDone = TRUE;
        }

        ReleaseSRWLockExclusive( &m_srwLock );
        fLocked = FALSE;
    }

    m_dwRapidFailTickStart = GetTickCount();

    if( m_hNULHandle == NULL )
    {
        SECURITY_ATTRIBUTES saAttr;
        saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
        saAttr.bInheritHandle = TRUE;
        saAttr.lpSecurityDescriptor = NULL;

        m_hNULHandle = CreateFileW( L"NUL",
                                    FILE_WRITE_DATA,
                                    FILE_SHARE_READ,
                                    &saAttr,
                                    CREATE_ALWAYS,
                                    FILE_ATTRIBUTE_NORMAL,
                                    NULL );
        if( m_hNULHandle == INVALID_HANDLE_VALUE )
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
    }

Finished:

    if(fLocked)
    {
        ReleaseSRWLockExclusive( &m_srwLock );
    }

    return hr;
}

PROCESS_MANAGER::~PROCESS_MANAGER()
{
    AcquireSRWLockExclusive(&m_srwLock);

    //if( m_ppServerProcessList != NULL )
    //{
    //    for( DWORD i = 0; i < m_dwProcessesPerApplication; ++i )
    //    {
    //        if( m_ppServerProcessList[i] != NULL )
    //        {
    //            m_ppServerProcessList[i]->DereferenceServerProcess();
    //            m_ppServerProcessList[i] = NULL;
    //        }
    //    }

    //    delete[] m_ppServerProcessList;
    //    m_ppServerProcessList = NULL;
    //}

    //if( m_hNULHandle != NULL )
    //{
    //    CloseHandle( m_hNULHandle );
    //    m_hNULHandle = NULL;
    //}

    //if( sm_fWSAStartupDone )
    //{
    //    WSACleanup();
    //    sm_fWSAStartupDone = FALSE;
    //}

    ReleaseSRWLockExclusive(&m_srwLock);
}

HRESULT
PROCESS_MANAGER::GetProcess(
    _In_    REQUESTHANDLER_CONFIG      *pConfig,
    _In_    BOOL                        fWebsocketSupported,
    _Out_   SERVER_PROCESS            **ppServerProcess
)
{
    HRESULT          hr = S_OK;
    BOOL             fSharedLock = FALSE;
    BOOL             fExclusiveLock = FALSE;
    DWORD            dwProcessIndex = 0;
    SERVER_PROCESS  *pSelectedServerProcess = NULL;

    if (InterlockedCompareExchange(&m_lStopping, 1L, 1L) == 1L)
    {
        return hr = E_APPLICATION_EXITING;
    }

    if (!m_fServerProcessListReady)
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fExclusiveLock = TRUE;

        if (!m_fServerProcessListReady)
        {
            m_dwProcessesPerApplication = pConfig->QueryProcessesPerApplication();
            m_ppServerProcessList = new SERVER_PROCESS*[m_dwProcessesPerApplication];
            if (m_ppServerProcessList == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }

            for (DWORD i = 0; i < m_dwProcessesPerApplication; ++i)
            {
                m_ppServerProcessList[i] = NULL;
            }
        }
        m_fServerProcessListReady = TRUE;
        ReleaseSRWLockExclusive(&m_srwLock);
        fExclusiveLock = FALSE;
    }

    AcquireSRWLockShared(&m_srwLock);
    fSharedLock = TRUE;

    //
    // round robin through to the next available process.
    //
    dwProcessIndex = (DWORD)InterlockedIncrement64((LONGLONG*)&m_dwRouteToProcessIndex);
    dwProcessIndex = dwProcessIndex % m_dwProcessesPerApplication;

    if (m_ppServerProcessList[dwProcessIndex] != NULL &&
        m_ppServerProcessList[dwProcessIndex]->IsReady())
    {
        *ppServerProcess = m_ppServerProcessList[dwProcessIndex];
        goto Finished;
    }

    ReleaseSRWLockShared(&m_srwLock);
    fSharedLock = FALSE;

    // should make the lock per process so that we can start processes simultaneously ?
    if (m_ppServerProcessList[dwProcessIndex] == NULL ||
        !m_ppServerProcessList[dwProcessIndex]->IsReady())
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fExclusiveLock = TRUE;

        if (m_ppServerProcessList[dwProcessIndex] != NULL)
        {
            if (!m_ppServerProcessList[dwProcessIndex]->IsReady())
            {
                //
                // terminate existing process that is not ready
                // before creating new one.
                //
                ShutdownProcessNoLock( m_ppServerProcessList[dwProcessIndex] );
            }
            else
            {
                // server is already up and ready to serve requests.
                //m_ppServerProcessList[dwProcessIndex]->ReferenceServerProcess();
                *ppServerProcess = m_ppServerProcessList[dwProcessIndex];
                goto Finished;
            }
        }

        if (RapidFailsPerMinuteExceeded(pConfig->QueryRapidFailsPerMinute()))
        {
            //
            // rapid fails per minute exceeded, do not create new process.
            //
            UTILITY::LogEventF(g_hEventLog,
                EVENTLOG_INFORMATION_TYPE,
                ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED,
                ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED_MSG,
                pConfig->QueryRapidFailsPerMinute());

            hr = HRESULT_FROM_WIN32(ERROR_SERVER_DISABLED);
            goto Finished;
        }

        if (m_ppServerProcessList[dwProcessIndex] == NULL)
        {

            pSelectedServerProcess = new SERVER_PROCESS();
            if (pSelectedServerProcess == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }


            hr = pSelectedServerProcess->Initialize(
                    this,                                   //ProcessManager
                    pConfig->QueryProcessPath(),            //
                    pConfig->QueryArguments(),              //
                    pConfig->QueryStartupTimeLimitInMS(),
                    pConfig->QueryShutdownTimeLimitInMS(),
                    pConfig->QueryWindowsAuthEnabled(),
                    pConfig->QueryBasicAuthEnabled(),
                    pConfig->QueryAnonymousAuthEnabled(),
                    pConfig->QueryEnvironmentVariables(),
                    pConfig->QueryStdoutLogEnabled(),
                    fWebsocketSupported,
                    pConfig->QueryStdoutLogFile(),
                    pConfig->QueryApplicationPhysicalPath(),   // physical path
                    pConfig->QueryApplicationPath(),           // app path
                    pConfig->QueryApplicationVirtualPath()     // App relative virtual path
            );
            if (FAILED(hr))
            {
                goto Finished;
            }

            hr = pSelectedServerProcess->StartProcess();
            if (FAILED(hr))
            {
                goto Finished;
            }
        }

        if (!pSelectedServerProcess->IsReady())
        {
            hr = HRESULT_FROM_WIN32(ERROR_CREATE_FAILED);
            goto Finished;
        }

        m_ppServerProcessList[dwProcessIndex] = pSelectedServerProcess;
        pSelectedServerProcess = NULL;

    }
    *ppServerProcess = m_ppServerProcessList[dwProcessIndex];

Finished:

    if (fSharedLock)
    {
        ReleaseSRWLockShared(&m_srwLock);
        fSharedLock = FALSE;
    }

    if (fExclusiveLock)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
        fExclusiveLock = FALSE;
    }

    if (pSelectedServerProcess != NULL)
    {
        delete pSelectedServerProcess;
        pSelectedServerProcess = NULL;
    }

    return hr;
}
