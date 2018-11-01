// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

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
            hr = HRESULT_FROM_GETLASTERROR();
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

    if( m_ppServerProcessList != NULL )
    {
        for( DWORD i = 0; i < m_dwProcessesPerApplication; ++i )
        {
            if( m_ppServerProcessList[i] != NULL )
            {
                m_ppServerProcessList[i]->DereferenceServerProcess();
                m_ppServerProcessList[i] = NULL;
            }
        }

        delete[] m_ppServerProcessList;
        m_ppServerProcessList = NULL;
    }

    if( m_hNULHandle != NULL )
    {
        CloseHandle( m_hNULHandle );
        m_hNULHandle = NULL;
    }

    if( sm_fWSAStartupDone )
    {
        WSACleanup();
        sm_fWSAStartupDone = FALSE;
    }

    ReleaseSRWLockExclusive(&m_srwLock);
}

HRESULT 
PROCESS_MANAGER::GetProcess(
    _In_    IHttpContext           *context,
    _In_    ASPNETCORE_CONFIG      *pConfig,
    _Out_   SERVER_PROCESS        **ppServerProcess
)
{
    HRESULT          hr = S_OK;
    BOOL             fSharedLock = FALSE;
    BOOL             fExclusiveLock = FALSE;
    PCWSTR           apsz[1];
    STACK_STRU(      strEventMsg, 256 );
    DWORD            dwProcessIndex = 0;
    SERVER_PROCESS **ppSelectedServerProcess = NULL;

    if (!m_fServerProcessListReady)
    {
        AcquireSRWLockExclusive( &m_srwLock );
        fExclusiveLock  = TRUE;

        if (!m_fServerProcessListReady)
        {
            m_dwProcessesPerApplication = pConfig->QueryProcessesPerApplication();
            m_ppServerProcessList = new SERVER_PROCESS*[m_dwProcessesPerApplication];
            if(m_ppServerProcessList == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }

            for(DWORD i=0;i<m_dwProcessesPerApplication;++i)
            {
                m_ppServerProcessList[i] = NULL;
            }
        }
        m_fServerProcessListReady = TRUE;
        ReleaseSRWLockExclusive( &m_srwLock );
        fExclusiveLock = FALSE;
    }

    AcquireSRWLockShared( &m_srwLock );
    fSharedLock = TRUE;

    //
    // round robin through to the next available process.
    //

    dwProcessIndex = (DWORD) InterlockedIncrement64( (LONGLONG*) &m_dwRouteToProcessIndex );
    dwProcessIndex = dwProcessIndex % m_dwProcessesPerApplication;
    ppSelectedServerProcess = &m_ppServerProcessList[dwProcessIndex];

    if( *ppSelectedServerProcess != NULL && 
        m_ppServerProcessList[dwProcessIndex]->IsReady() )
    {
        m_ppServerProcessList[dwProcessIndex]->ReferenceServerProcess();
        *ppServerProcess = m_ppServerProcessList[dwProcessIndex];
        goto Finished;
    }

    ReleaseSRWLockShared( &m_srwLock );
    fSharedLock = FALSE;
    // should make the lock per process so that we can start processes simultaneously ?

    if(m_ppServerProcessList[dwProcessIndex] == NULL || !m_ppServerProcessList[dwProcessIndex]->IsReady())
    {
        AcquireSRWLockExclusive( &m_srwLock );
        fExclusiveLock  = TRUE;

        if( m_ppServerProcessList[dwProcessIndex] != NULL )
        {
            if( !m_ppServerProcessList[dwProcessIndex]->IsReady() )
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
                m_ppServerProcessList[dwProcessIndex]->ReferenceServerProcess();
                *ppServerProcess = m_ppServerProcessList[dwProcessIndex];
                goto Finished;
            }
        }

        if( RapidFailsPerMinuteExceeded(pConfig->QueryRapidFailsPerMinute()) )
        {
            //
            // rapid fails per minute exceeded, do not create new process.
            //

            if( SUCCEEDED( strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED_MSG,
                pConfig->QueryRapidFailsPerMinute() ) ) )
            {
                apsz[0] = strEventMsg.QueryStr();

                //
                // not checking return code because if ReportEvent
                // fails, we cannot do anything.
                //
                if (FORWARDING_HANDLER::QueryEventLog() != NULL)
                {
                    ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                        EVENTLOG_INFORMATION_TYPE,
                        0,
                        ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED,
                        NULL,
                        1,
                        0,
                        apsz,
                        NULL);
                }
            }

            hr = HRESULT_FROM_WIN32(ERROR_SERVER_DISABLED);
            goto Finished;
        }

        if( m_ppServerProcessList[dwProcessIndex] == NULL )
        {
            m_ppServerProcessList[dwProcessIndex] = new SERVER_PROCESS();
            if( m_ppServerProcessList[dwProcessIndex] == NULL )
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }

            hr = m_ppServerProcessList[dwProcessIndex]->Initialize(
                                    this,
                                    pConfig->QueryProcessPath(),
                                    pConfig->QueryArguments(),
                                    pConfig->QueryStartupTimeLimitInMS(),
                                    pConfig->QueryShutdownTimeLimitInMS(),
                                    pConfig->QueryWindowsAuthEnabled(),
                                    pConfig->QueryBasicAuthEnabled(),
                                    pConfig->QueryAnonymousAuthEnabled(),
                                    pConfig->QueryEnvironmentVariables(),
                                    pConfig->QueryStdoutLogEnabled(),
                                    pConfig->QueryStdoutLogFile()
                                    );
            if( FAILED( hr ) )
            {
                goto Finished;
            }

            hr = m_ppServerProcessList[dwProcessIndex]->StartProcess(context);
            if( FAILED( hr ) )
            {
                goto Finished;
            }
        }

        if( !m_ppServerProcessList[dwProcessIndex]->IsReady() )
        { 
            hr = HRESULT_FROM_WIN32( ERROR_CREATE_FAILED );
            goto Finished;
        }

        m_ppServerProcessList[dwProcessIndex]->ReferenceServerProcess();
        *ppServerProcess = m_ppServerProcessList[dwProcessIndex];
    }

Finished:

    if( FAILED(hr) )
    {
        if(m_ppServerProcessList[dwProcessIndex] != NULL )
        {
            m_ppServerProcessList[dwProcessIndex]->DereferenceServerProcess();
            m_ppServerProcessList[dwProcessIndex] = NULL;
        }
    }

    if( fSharedLock )
    {
        ReleaseSRWLockShared( &m_srwLock );
        fSharedLock = FALSE;
    }

    if( fExclusiveLock )
    {
        ReleaseSRWLockExclusive( &m_srwLock );
        fExclusiveLock = FALSE;
    }

    return hr;
}