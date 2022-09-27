// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "processmanager.h"
#include "EventLog.h"
#include "exceptions.h"
#include "SRWSharedLock.h"

volatile BOOL               PROCESS_MANAGER::sm_fWSAStartupDone = FALSE;

HRESULT
PROCESS_MANAGER::Initialize(
    VOID
)
{
    WSADATA                              wsaData;
    int                                  result;

    if( !sm_fWSAStartupDone )
    {
        auto lock = SRWExclusiveLock(m_srwLock);

        if( !sm_fWSAStartupDone )
        {
            if( (result = WSAStartup(MAKEWORD(2, 2), &wsaData)) != 0 )
            {
                RETURN_HR(HRESULT_FROM_WIN32( result ));
            }
            sm_fWSAStartupDone = TRUE;
        }
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
        RETURN_LAST_ERROR_IF( m_hNULHandle == INVALID_HANDLE_VALUE );
    }

    return S_OK;
}

PROCESS_MANAGER::~PROCESS_MANAGER()
{
}

HRESULT
PROCESS_MANAGER::GetProcess(
    _In_    REQUESTHANDLER_CONFIG      *pConfig,
    _In_    BOOL                        fWebsocketSupported,
    _Out_   SERVER_PROCESS            **ppServerProcess
)
{
    DWORD            dwProcessIndex = 0;
    std::unique_ptr<SERVER_PROCESS>  pSelectedServerProcess;

    if (InterlockedCompareExchange(&m_lStopping, 1L, 1L) == 1L)
    {
        RETURN_IF_FAILED(E_APPLICATION_EXITING);
    }

    if (!m_fServerProcessListReady)
    {
        auto lock = SRWExclusiveLock(m_srwLock);

        if (!m_fServerProcessListReady)
        {
            m_dwProcessesPerApplication = pConfig->QueryProcessesPerApplication();
            m_ppServerProcessList = new SERVER_PROCESS*[m_dwProcessesPerApplication];

            for (DWORD i = 0; i < m_dwProcessesPerApplication; ++i)
            {
                m_ppServerProcessList[i] = NULL;
            }
        }
        m_fServerProcessListReady = TRUE;
    }

    {
        auto lock = SRWSharedLock(m_srwLock);

        //
        // round robin through to the next available process.
        //
        dwProcessIndex = InterlockedIncrement(&m_dwRouteToProcessIndex);
        dwProcessIndex = dwProcessIndex % m_dwProcessesPerApplication;

        if (m_ppServerProcessList[dwProcessIndex] != NULL &&
            m_ppServerProcessList[dwProcessIndex]->IsReady())
        {
            *ppServerProcess = m_ppServerProcessList[dwProcessIndex];
            return S_OK;
        }
    }

    // should make the lock per process so that we can start processes simultaneously ?
    if (m_ppServerProcessList[dwProcessIndex] == NULL ||
        !m_ppServerProcessList[dwProcessIndex]->IsReady())
    {
        auto lock = SRWExclusiveLock(m_srwLock);

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
                return S_OK;
            }
        }

        if (RapidFailsPerMinuteExceeded(pConfig->QueryRapidFailsPerMinute()))
        {
            //
            // rapid fails per minute exceeded, do not create new process.
            //
            EventLog::Info(
                ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED,
                ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED_MSG,
                pConfig->QueryRapidFailsPerMinute());

            RETURN_HR(HRESULT_FROM_WIN32(ERROR_SERVER_DISABLED));
        }

        if (m_ppServerProcessList[dwProcessIndex] == NULL)
        {

            pSelectedServerProcess = std::make_unique<SERVER_PROCESS>();
            RETURN_IF_FAILED(pSelectedServerProcess->Initialize(
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
                    pConfig->QueryEnableOutOfProcessConsoleRedirection(),
                    fWebsocketSupported,
                    pConfig->QueryStdoutLogFile(),
                    pConfig->QueryApplicationPhysicalPath(),   // physical path
                    pConfig->QueryApplicationPath(),           // app path
                    pConfig->QueryApplicationVirtualPath(),     // App relative virtual path,
                    pConfig->QueryBindings()
            ));
            RETURN_IF_FAILED(pSelectedServerProcess->StartProcess());
        }

        if (!pSelectedServerProcess->IsReady())
        {
            RETURN_HR(HRESULT_FROM_WIN32(ERROR_CREATE_FAILED));
        }

        m_ppServerProcessList[dwProcessIndex] = pSelectedServerProcess.release();
    }
    *ppServerProcess = m_ppServerProcessList[dwProcessIndex];

    return S_OK;
}
