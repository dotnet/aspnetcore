// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"
#include <IPHlpApi.h>
#include <share.h>

extern BOOL g_fNsiApiNotSupported;

#define STARTUP_TIME_LIMIT_INCREMENT_IN_MILLISECONDS 5000

HRESULT
SERVER_PROCESS::Initialize(
    PROCESS_MANAGER    *pProcessManager,
    STRU               *pszProcessExePath,
    STRU               *pszArguments,
    DWORD               dwStartupTimeLimitInMS,
    DWORD               dwShtudownTimeLimitInMS,
    MULTISZ            *pszEnvironment,
    BOOL                fStdoutLogEnabled,
    STRU               *pstruStdoutLogFile
)
{
    HRESULT                                 hr = S_OK;
    JOBOBJECT_EXTENDED_LIMIT_INFORMATION    jobInfo = { 0 };

    m_pProcessManager = pProcessManager;
    m_dwStartupTimeLimitInMS = dwStartupTimeLimitInMS;
    m_dwShutdownTimeLimitInMS = dwShtudownTimeLimitInMS;
    m_fStdoutLogEnabled = fStdoutLogEnabled;

    hr = m_ProcessPath.Copy(*pszProcessExePath);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = m_struLogFile.Copy(*pstruStdoutLogFile);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = m_Arguments.Copy(*pszArguments);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = m_Environment.Copy(*pszEnvironment);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (m_hJobObject == NULL)
    {
        m_hJobObject = CreateJobObject(NULL,   // LPSECURITY_ATTRIBUTES
            NULL); // LPCTSTR lpName
#pragma warning( disable : 4312)
		// 0xdeadbeef is used by Antares
        if (m_hJobObject == NULL || m_hJobObject == (HANDLE)0xdeadbeef)
        {
            m_hJobObject = NULL;
            // ignore job object creation error.
        }
#pragma warning( error : 4312) 
        if (m_hJobObject != NULL)
        {
            jobInfo.BasicLimitInformation.LimitFlags =
                JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            if (!SetInformationJobObject(m_hJobObject,
                JobObjectExtendedLimitInformation,
                &jobInfo,
                sizeof jobInfo))
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto Finished;
            }
        }
    }

Finished:
    return hr;
}


HRESULT
SERVER_PROCESS::StartProcess(
    IHttpContext *context
)
{
    HRESULT                 hr = S_OK;
    PROCESS_INFORMATION     processInformation = {0};
    STARTUPINFOW            startupInfo = {0};
    WCHAR                  *pszCommandLine = NULL;
    DWORD                   dwCommandLineLen = 0;
    DWORD                   dwNumDigitsInPort = 0;
    DWORD                   dwNumDigitsInDebugPort = 0;
    LPWSTR                  pszCurrentEnvironment = NULL;
    DWORD                   dwCurrentEnvSize = 0;
    DWORD                   dwCreationFlags = 0;
    BOOL                    fReady = FALSE;
    DWORD                   dwTickCount = 0;
    STACK_STRU(             strAspNetCorePortEnvVar, 32);
    STACK_STRU(             strAspNetCoreDebugPortEnvVar, 32);
    MULTISZ                 mszNewEnvironment;
    MULTISZ                 mszEnvCopy;
    DWORD                   cChildProcess = 0;
    DWORD                   dwTimeDifference = 0;
    STACK_STRU(             strEventMsg, 256);
    BOOL                    fDebugPortEnvSet = FALSE;
    BOOL                    fReplacedEnv = FALSE;
    BOOL                    fPortInUse = FALSE;
    LPCWSTR                 pszRootApplicationPath = NULL;
    BOOL                    fDebuggerAttachedToChildProcess = FALSE;
    STRU                    strFullProcessPath;
    STRU                    struApplicationId;
    RPC_STATUS              rpcStatus;
    UUID                    logUuid;
    PSTR                    pszLogUuid = NULL;
    BOOL                    fRpcStringAllocd = FALSE;
    STRU                    struGuidEnv;
    STRU                    finalCommandLine;
    BOOL                    fDonePrepareCommandLine = FALSE;
    //
    // process id of the process listening on port we randomly generated.
    //
    DWORD                   dwActualProcessId = 0;
    WCHAR*                  pszPath = NULL;
    WCHAR                   pszFullPath[_MAX_PATH];
    LPCWSTR                 apsz[1];
    PCWSTR                  pszAppPath = NULL;

    GetStartupInfoW(&startupInfo);

    //
    // setup stdout and stderr handles to our stdout handle only if
    // the handle is valid.
    //

    SetupStdHandles(context, &startupInfo);

    //
    // generate new guid for each process
    //

    rpcStatus = UuidCreate(&logUuid);
    if (rpcStatus != RPC_S_OK)
    {
        hr = rpcStatus;
        goto Finished;
    }

    rpcStatus = UuidToStringA(&logUuid, (BYTE **)&pszLogUuid);
    if (rpcStatus != RPC_S_OK)
    {
        hr = rpcStatus;
        goto Finished;
    }

    fRpcStringAllocd = true;

    hr = m_straGuid.Copy( pszLogUuid );
    if(FAILED(hr))
    {
        goto Finished;
    }

    //
    // Generate random port that the new process will listen on.
    //

    if(g_fNsiApiNotSupported)
    {
        m_dwPort = GenerateRandomPort();
    }
    else
    {
        DWORD cRetry = 0;
        do
        {
            //
            // ignore dwActualProcessId because here we are 
            // determing whether the randomly generated port is
            // in use by any other process.
            //

            m_dwPort = GenerateRandomPort();
            hr = CheckIfServerIsUp(m_dwPort, &dwActualProcessId, &fPortInUse);
        } while( fPortInUse && ++cRetry < MAX_RETRY );

        if( cRetry > MAX_RETRY )
        {
            hr = HRESULT_FROM_WIN32(ERROR_PORT_NOT_SET);
            goto Finished;
        }
    }

    dwNumDigitsInPort = GetNumberOfDigits( m_dwPort );
    hr = strAspNetCorePortEnvVar.SafeSnwprintf( L"%s=%u", ASPNETCORE_PORT_STR, m_dwPort );
    if( FAILED ( hr ) )
    {
        goto Finished;
    }

    //
    // Generate random debug port that the new process will listen on.
    // this will only be used if ASPNETCORE_DEBUG_PORT placeholder is found
    // in the aspNetCore config.
    //

    if(g_fNsiApiNotSupported)
    {
        while((m_dwDebugPort = GenerateRandomPort()) == m_dwPort);
    }
    else
    {
        DWORD cRetry = 0;
        do
        {
            while((m_dwDebugPort = GenerateRandomPort()) == m_dwPort);
            hr = CheckIfServerIsUp(m_dwDebugPort, &dwActualProcessId, &fPortInUse);
        } while( fPortInUse && ++cRetry < MAX_RETRY );

        if( cRetry > MAX_RETRY )
        {
            hr = HRESULT_FROM_WIN32(ERROR_PORT_NOT_SET);
            goto Finished;
        }
    }

    dwNumDigitsInDebugPort = GetNumberOfDigits( m_dwDebugPort );
    hr = strAspNetCoreDebugPortEnvVar.SafeSnwprintf( L"%s=%u", 
                                                       ASPNETCORE_DEBUG_PORT_STR, 
                                                       m_dwDebugPort );
    if( FAILED ( hr ) )
    {
        goto Finished;
    }

    //
    // Create environment for new process
    //

    struApplicationId.Copy( L"ASPNETCORE_APPL_PATH=" );

    // let's find the app path. IIS does not support nested sites
    // we can seek for the fourth '/' if it exits
    // MACHINE/WEBROOT/APPHOST/<site>/<app>. 
    pszAppPath = context->GetApplication()->GetAppConfigPath();
    DWORD dwCounter = 0;
    DWORD dwPosition = 0;
    while (pszAppPath[dwPosition] != NULL)
    {
        if (pszAppPath[dwPosition] == '/')
        {
            dwCounter++;
            if (dwCounter == 4)
                break;
        }
        dwPosition++;
    }
    if (dwCounter == 4)
    {
        struApplicationId.Append(pszAppPath + dwPosition);
    }
    else
    {
        struApplicationId.Append(L"/");
    }

    mszNewEnvironment.Append( struApplicationId );

    struGuidEnv.Copy( L"ASPNETCORE_TOKEN=" );
    struGuidEnv.AppendA( m_straGuid.QueryStr(), m_straGuid.QueryCCH() );

    mszNewEnvironment.Append( struGuidEnv );

    pszRootApplicationPath = context->GetApplication()->GetApplicationPhysicalPath();

    //
    // generate process command line.
    //
    dwCommandLineLen = (DWORD)wcslen(pszRootApplicationPath) + m_ProcessPath.QueryCCH() + m_Arguments.QueryCCH() + 4;

    pszCommandLine = new WCHAR[ dwCommandLineLen ];
    if( pszCommandLine == NULL )
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    pszPath = m_ProcessPath.QueryStr();

    if ((wcsstr(pszPath, L":") == NULL) && (wcsstr(pszPath, L"%") == NULL))
    {
        // let's check whether it is a relative path
        WCHAR pszRelativePath[_MAX_PATH];

        if (swprintf_s(pszRelativePath,
            _MAX_PATH,
            L"%s\\%s",
            pszRootApplicationPath,
            pszPath) == -1)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            goto Finished;
        }

        if (_wfullpath(pszFullPath,
            pszRelativePath,
            _MAX_PATH) == NULL)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
            goto Finished;
        }

        FILE *file = NULL;
        if ((file = _wfsopen(pszFullPath, L"r", _SH_DENYNO)) != NULL)
        {
            fclose(file);
            pszPath = pszFullPath;
        }
    }

    if (swprintf_s(pszCommandLine,
        dwCommandLineLen,
        L"\"%s\" %s",
        pszPath,
        m_Arguments.QueryStr()) == -1)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        goto Finished;
    }

    //
    // replace %ASPNETCORE_PORT% with port number
    //

    hr = ASPNETCORE_UTILS::ReplacePlaceHolderWithValue( 
                                      pszCommandLine, 
                                      ASPNETCORE_PORT_PLACEHOLDER,
                                      ASPNETCORE_PORT_PLACEHOLDER_CCH,
                                      m_dwPort,
                                      dwNumDigitsInPort,
                                      &fReplacedEnv );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    //
    // append AspNetCorePort to env variables.
    //

    mszNewEnvironment.Append( strAspNetCorePortEnvVar );

    hr = ASPNETCORE_UTILS::ReplacePlaceHolderWithValue( 
                                      pszCommandLine, 
                                      ASPNETCORE_DEBUG_PORT_PLACEHOLDER,
                                      ASPNETCORE_DEBUG_PORT_PLACEHOLDER_CCH,
                                      m_dwDebugPort,
                                      dwNumDigitsInDebugPort,
                                      &fReplacedEnv );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    if(fReplacedEnv)
    {
        //
        // append debug port to environment only if 
        // ASPNETCORE_DEBUG_PORT placeholder is present.
        //

        mszNewEnvironment.Append( strAspNetCoreDebugPortEnvVar );
        fDebugPortEnvSet = TRUE;
    }

    //
    // append the environment variables from web.config/aspNetCore section.
    // append takes in length of string without the last null char
    // this allows user to override current environment variables
    //

    if(!mszEnvCopy.Copy(m_Environment))
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    LPWSTR multisz = mszEnvCopy.QueryStr();

    while( *multisz != '\0' )
    {
        //
        // replace %ASPNETCORE_PORT% placeholder if present.
        //
        
        hr = ASPNETCORE_UTILS::ReplacePlaceHolderWithValue( 
                                          multisz, 
                                          ASPNETCORE_PORT_PLACEHOLDER, 
                                          ASPNETCORE_PORT_PLACEHOLDER_CCH, 
                                          m_dwPort, 
                                          dwNumDigitsInPort,
                                          &fReplacedEnv );
        if( FAILED( hr ) )
        {
            goto Finished;
        }
        
        //
        // replace %ASPNETCORE_DEBUG_PORT% placeholder if present.
        // if this placeholder is present, add this placeholder=value as
        // an environment variable as well.
        //

        hr = ASPNETCORE_UTILS::ReplacePlaceHolderWithValue( 
                            multisz, 
                            ASPNETCORE_DEBUG_PORT_PLACEHOLDER, 
                            ASPNETCORE_DEBUG_PORT_PLACEHOLDER_CCH, 
                            m_dwDebugPort, 
                            dwNumDigitsInDebugPort,
                            &fReplacedEnv );
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        if( fReplacedEnv && !fDebugPortEnvSet )
        {
            mszNewEnvironment.Append( strAspNetCoreDebugPortEnvVar );
        }

        mszNewEnvironment.Append( multisz );
        multisz += wcslen( multisz) + 1;
    }

    //
    // Append the current env.
    // copy takes in number of bytes including the double null terminator
    //
    pszCurrentEnvironment = GetEnvironmentStringsW();
    if (pszCurrentEnvironment == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_ENVIRONMENT);
        goto Finished;
    }

    //
    // determine length of current environment block
    //

    do
    {
        while (*(pszCurrentEnvironment + dwCurrentEnvSize++) != 0);
    } while (*(pszCurrentEnvironment + dwCurrentEnvSize++) != 0);

    DBG_ASSERT(dwCurrentEnvSize > 0);
    //
    // environment block ends with  \0\0, we don't want include the last \0 for appending
    //
    mszNewEnvironment.Append(pszCurrentEnvironment, dwCurrentEnvSize-1 );

    dwCreationFlags = CREATE_NO_WINDOW |
        CREATE_UNICODE_ENVIRONMENT |
        CREATE_SUSPENDED; // |
        //CREATE_NEW_PROCESS_GROUP;


    finalCommandLine.Copy( pszCommandLine );
    fDonePrepareCommandLine = TRUE;

    if (!CreateProcessW(
            NULL,                   // applicationName     
            finalCommandLine.QueryStr(),
            NULL,                   // processAttr
            NULL,                   // threadAttr
            TRUE,                   // inheritHandles
            dwCreationFlags,
            mszNewEnvironment.QueryStr(),
            pszRootApplicationPath, // currentDir
            &startupInfo,
            &processInformation) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        // don't check return code as we already in error report
        strEventMsg.SafeSnwprintf(
            ASPNETCORE_EVENT_PROCESS_START_ERROR_MSG,
            pszAppPath,
            pszRootApplicationPath,
            finalCommandLine.QueryStr(),
            hr,
            0);
        goto Finished;
    }

    m_hProcessHandle = processInformation.hProcess;
    m_dwProcessId = processInformation.dwProcessId;

    if( m_hJobObject != NULL )
    {
        if( !AssignProcessToJobObject( m_hJobObject, 
                                       processInformation.hProcess ) )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            if( hr != HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED) )
            {
                goto Finished;
            }
        }
    }
    
    if( ResumeThread( processInformation.hThread ) == -1 )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto Finished;
    }    

    if( CheckRemoteDebuggerPresent(processInformation.hProcess, &fDebuggerAttachedToChildProcess) == 0 )
    {
        // some error occurred  - assume debugger is not attached;
        fDebuggerAttachedToChildProcess = FALSE;
    }

    //
    // since servers like tomcat would startup even if there was a port 
    // collision, need to make sure the server is up and listening on
    // the port specified.
    //

    dwTickCount = GetTickCount();
    do
    {
        DWORD processStatus;

        if (GetExitCodeProcess(m_hProcessHandle, &processStatus))
        {
            if (processStatus != STILL_ACTIVE)
            {
                hr = E_FAIL;
                strEventMsg.SafeSnwprintf(
                    ASPNETCORE_EVENT_PROCESS_START_ERROR_MSG,
                    pszAppPath,
                    pszRootApplicationPath,
                    finalCommandLine.QueryStr(),
                    hr,
                    processStatus);
                goto Finished;
            }
        }

        if(g_fNsiApiNotSupported)
        {
            hr = CheckIfServerIsUp( m_dwPort, &fReady );
        }
        else
        {
            hr = CheckIfServerIsUp( m_dwPort, &dwActualProcessId, &fReady );
        }

        fDebuggerAttachedToChildProcess = IsDebuggerIsAttached();

        if( !fReady )
        {
            Sleep(250);
        }

        dwTimeDifference = (GetTickCount() - dwTickCount);
    } while( fReady == FALSE && 
             (( dwTimeDifference < m_dwStartupTimeLimitInMS) || fDebuggerAttachedToChildProcess) );

    hr = RegisterProcessWait( &m_hProcessWaitHandle, 
                              m_hProcessHandle );

    if( FAILED( hr ) )
    {
        goto Finished;
    }

    //
    // check if debugger is attached after startupTimeout.
    //
    if( !fDebuggerAttachedToChildProcess && 
        CheckRemoteDebuggerPresent(processInformation.hProcess, &fDebuggerAttachedToChildProcess) == 0 )
    {
        // some error occurred  - assume debugger is not attached;
        fDebuggerAttachedToChildProcess = FALSE;
    }

    hr = GetChildProcessHandles();
    if( FAILED(hr ) )
    {
        goto Finished;
    }

    BOOL processMatch = FALSE;
    if(dwActualProcessId == m_dwProcessId)
    {
        m_dwListeningProcessId = m_dwProcessId;
        processMatch = TRUE;
    }

    for(DWORD i=0; i < m_cChildProcess; ++i)
    {
        if( !processMatch && dwActualProcessId == m_dwChildProcessIds[i])
        {
            m_dwListeningProcessId = m_dwChildProcessIds[i];
            processMatch = TRUE;
        }

        if( m_hChildProcessHandles[i] != NULL )
        {
            if( fDebuggerAttachedToChildProcess == FALSE && CheckRemoteDebuggerPresent(m_hChildProcessHandles[i], &fDebuggerAttachedToChildProcess) == 0 )
            {
                // some error occurred  - assume debugger is not attached;
                fDebuggerAttachedToChildProcess = FALSE;
            }

            hr = RegisterProcessWait( &m_hChildProcessWaitHandles[i], 
                                      m_hChildProcessHandles[i] );
            if( FAILED( hr ) )
            {
                goto Finished;
            }

            cChildProcess ++;
        }
    }

    if(fReady == FALSE)
    {
        //
        // hr is already set by CheckIfServerIsUp
        //

        if( dwTimeDifference >= m_dwStartupTimeLimitInMS )
        {
            hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
            strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_PROCESS_START_NOTREADY_ERROR_MSG,
                pszAppPath,
                pszRootApplicationPath,
                finalCommandLine.QueryStr(),
                m_dwPort,
                hr);
        }

        goto Finished;
    }

    if( !g_fNsiApiNotSupported && !processMatch )
    {
        //
        // process that we created is not listening 
        // on the port we specified.
        //
        fReady = FALSE;
        strEventMsg.SafeSnwprintf(
            ASPNETCORE_EVENT_PROCESS_START_WRONGPORT_ERROR_MSG,
            pszAppPath,
            pszRootApplicationPath,
            finalCommandLine.QueryStr(),
            m_dwPort,
            hr);
        hr = HRESULT_FROM_WIN32(ERROR_CREATE_FAILED);
        goto Finished;
    }

    if( cChildProcess > 0 )
    {
        //
        // final check to make sure child process listening on HTTP is still UP
        // This is needed because, the child process might have crashed/exited between
        // the previous call to checkIfServerIsUp and RegisterProcessWait
        // and we would not know about it.
        // 

        if(g_fNsiApiNotSupported)
        {
            hr = CheckIfServerIsUp( m_dwPort, &fReady );
        }
        else
        {
            hr = CheckIfServerIsUp( m_dwPort, &dwActualProcessId, &fReady );
        }

        if( (FAILED(hr) || fReady == FALSE) )
        {
            strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_PROCESS_START_NOTREADY_ERROR_MSG,
                pszAppPath,
                pszRootApplicationPath,
                finalCommandLine.QueryStr(),
                m_dwPort,
                hr);
            goto Finished;
        }
    }

    //
    // ready to mark the server process ready but before this, 
    // create and initialize the FORWARDER_CONNECTION 
    //
    if(m_pForwarderConnection != NULL)
    {
        m_pForwarderConnection->DereferenceForwarderConnection();
        m_pForwarderConnection = NULL;
    }

    if(m_pForwarderConnection == NULL)
    {
        m_pForwarderConnection = new FORWARDER_CONNECTION();
        if(m_pForwarderConnection == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        hr = m_pForwarderConnection->Initialize( m_dwPort );
        if(FAILED(hr))
        {
            goto Finished;
        }
    }

    if(!g_fNsiApiNotSupported)
    {
        m_hListeningProcessHandle = OpenProcess(SYNCHRONIZE | PROCESS_TERMINATE | PROCESS_DUP_HANDLE, FALSE, m_dwListeningProcessId);
    }

    //
    // mark server process as Ready
    //

    m_fReady = TRUE;

    if (SUCCEEDED(strEventMsg.SafeSnwprintf(
        ASPNETCORE_EVENT_PROCESS_START_SUCCESS_MSG,
		pszAppPath,
        m_dwProcessId,
        m_dwPort)))
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
                ASPNETCORE_EVENT_PROCESS_START_SUCCESS,
                NULL,
                1,
                0,
                apsz,
                NULL);
        }
    }

Finished:
    if ( FAILED(hr) )
    {
        if (strEventMsg.IsEmpty())
        {
            if (!fDonePrepareCommandLine)
                strEventMsg.SafeSnwprintf(
                pszAppPath,
                ASPNETCORE_EVENT_PROCESS_START_INTERNAL_ERROR_MSG,
                hr);
            else
                strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_PROCESS_START_POSTCREATE_ERROR_MSG,
                pszAppPath,
                pszRootApplicationPath,
                finalCommandLine.QueryStr(),
                hr);
        }

        apsz[0] = strEventMsg.QueryStr();

        // not checking return code because if ReportEvent
        // fails, we cannot do anything.
        //
        if (FORWARDING_HANDLER::QueryEventLog() != NULL)
        {
            ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                EVENTLOG_ERROR_TYPE,
                0,
                ASPNETCORE_EVENT_PROCESS_START_ERROR,
                NULL,
                1,
                0,
                apsz,
                NULL);
        }
    }

    if ( fRpcStringAllocd )
    {
        RpcStringFreeA((BYTE **)&pszLogUuid);
        pszLogUuid = NULL;
    }

    if( processInformation.hThread != NULL )
    {
        CloseHandle(processInformation.hThread);
        processInformation.hThread = NULL;
    }

    if(pszCurrentEnvironment != NULL )
    {
        FreeEnvironmentStringsW( pszCurrentEnvironment );
        pszCurrentEnvironment = NULL;
    }

    if( pszCommandLine != NULL )
    {
        delete[] pszCommandLine;
        pszCommandLine = NULL;
    }

    if( FAILED( hr ) || m_fReady == FALSE)
    {
        if(m_hStdoutHandle != NULL)
        {
            if( m_hStdoutHandle != INVALID_HANDLE_VALUE )
            {
                CloseHandle( m_hStdoutHandle );
            }
            m_hStdoutHandle = NULL;
        }

        if( m_fStdoutLogEnabled )
        {
            m_Timer.CancelTimer();
        }

        if(m_hListeningProcessHandle != NULL)
        {
            if( m_hListeningProcessHandle != INVALID_HANDLE_VALUE )
            {
                CloseHandle( m_hListeningProcessHandle );
            }
            m_hListeningProcessHandle = NULL;
        }

        if( m_hProcessWaitHandle != NULL )
        {
            UnregisterWait( m_hProcessWaitHandle );
            m_hProcessWaitHandle = NULL;
        }

        for(DWORD i=0;i<m_cChildProcess;++i)
        {
            if( m_hChildProcessWaitHandles[i] != NULL )
            {
                UnregisterWait( m_hChildProcessWaitHandles[i] );
                m_hChildProcessWaitHandles[i] = NULL;
            }
        }

        StopProcess();

        StopAllProcessesInJobObject();
    }

    return hr;
}

HRESULT
SERVER_PROCESS::SetWindowsAuthToken(
    HANDLE hToken,
    LPHANDLE pTargetTokenHandle
)
{
    HRESULT hr = S_OK;

    if( m_hListeningProcessHandle != NULL && m_hListeningProcessHandle != INVALID_HANDLE_VALUE )
    {
        if(!DuplicateHandle( GetCurrentProcess(),
                             hToken,
                             m_hListeningProcessHandle,
                             pTargetTokenHandle,
                             0,
                             FALSE,
                             DUPLICATE_SAME_ACCESS ))
        {
            hr = HRESULT_FROM_GETLASTERROR();
            goto Finished;
        }
    }

Finished:

    return hr;
}

HRESULT
SERVER_PROCESS::SetupStdHandles(
    IHttpContext *context,
    LPSTARTUPINFOW pStartupInfo
)
{
    SECURITY_ATTRIBUTES     saAttr = {0};
    HRESULT                 hr = S_OK;
    SYSTEMTIME              systemTime;
    STRU                    struLogFileName;
    BOOL                    fStdoutLoggingFailed = FALSE;
    STRU                    strEventMsg;
    LPCWSTR                 apsz[1];
    STRU                    struAbsLogFilePath;

    DBG_ASSERT(pStartupInfo);

    if( m_fStdoutLogEnabled )
    {
        saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
        saAttr.bInheritHandle = TRUE; 
        saAttr.lpSecurityDescriptor = NULL;

        if(m_hStdoutHandle != NULL)
        {
            if(!CloseHandle( m_hStdoutHandle ))
            {
                hr = HRESULT_FROM_GETLASTERROR();
                goto Finished;
            }

            m_hStdoutHandle = NULL;
        }

        hr = PATH::ConvertPathToFullPath( m_struLogFile.QueryStr(), 
                                          context->GetApplication()->GetApplicationPhysicalPath(),
                                          &struAbsLogFilePath );
        if(FAILED(hr))
        {
            goto Finished;
        }

        GetSystemTime(&systemTime);
        hr = struLogFileName.SafeSnwprintf( L"%s_%d_%d%d%d%d%d%d.log", 
                                            struAbsLogFilePath.QueryStr(), 
                                            GetCurrentProcessId(),
                                            systemTime.wYear,
                                            systemTime.wMonth,
                                            systemTime.wDay,
                                            systemTime.wHour,
                                            systemTime.wMinute,
                                            systemTime.wSecond );
        if(FAILED(hr))
        {
            goto Finished;
        }

        m_hStdoutHandle = CreateFileW( struLogFileName.QueryStr(),
                                       FILE_WRITE_DATA,
                                       FILE_SHARE_READ,
                                       &saAttr,
                                       CREATE_ALWAYS,
                                       FILE_ATTRIBUTE_NORMAL,
                                       NULL );
        if( m_hStdoutHandle == INVALID_HANDLE_VALUE )
        {
            fStdoutLoggingFailed = TRUE;
            m_hStdoutHandle = NULL;

            if( SUCCEEDED( strEventMsg.SafeSnwprintf( 
                           ASPNETCORE_EVENT_INVALID_STDOUT_LOG_FILE_MSG,
                           struLogFileName.QueryStr(), 
                           HRESULT_FROM_GETLASTERROR() ) ) )
            {
                apsz[0] = strEventMsg.QueryStr();

                //
                // not checking return code because if ReportEvent
                // fails, we cannot do anything.
                //
                if (FORWARDING_HANDLER::QueryEventLog() != NULL)
                {
                    ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                        EVENTLOG_WARNING_TYPE,
                        0,
                        ASPNETCORE_EVENT_CONFIG_ERROR,
                        NULL,
                        1,
                        0,
                        apsz,
                        NULL);
                }
            }
        }

        if( !fStdoutLoggingFailed )
        {
            pStartupInfo->dwFlags = STARTF_USESTDHANDLES;
            pStartupInfo->hStdInput = INVALID_HANDLE_VALUE;
            pStartupInfo->hStdError = m_hStdoutHandle;
            pStartupInfo->hStdOutput = m_hStdoutHandle;

            m_struFullLogFile.Copy( struLogFileName );

            // start timer to open and close handles regularly.
            m_Timer.InitializeTimer(SERVER_PROCESS::TimerCallback, this, 3000, 3000);
        }
    }

    if( (!m_fStdoutLogEnabled || fStdoutLoggingFailed) && 
        m_pProcessManager->QueryNULHandle() != NULL &&
        m_pProcessManager->QueryNULHandle() != INVALID_HANDLE_VALUE )
    {
        pStartupInfo->dwFlags = STARTF_USESTDHANDLES;
        pStartupInfo->hStdInput = INVALID_HANDLE_VALUE;
        pStartupInfo->hStdError = m_pProcessManager->QueryNULHandle();
        pStartupInfo->hStdOutput = m_pProcessManager->QueryNULHandle();
    }

Finished:

    return hr;
}

VOID
CALLBACK
SERVER_PROCESS::TimerCallback(
    IN PTP_CALLBACK_INSTANCE Instance,
    IN PVOID Context,
    IN PTP_TIMER Timer
)
{
    Instance;
    Timer;
    SERVER_PROCESS*         pServerProcess = (SERVER_PROCESS*) Context;
    HANDLE                  hStdoutHandle = NULL;
    SECURITY_ATTRIBUTES     saAttr = {0};
    HRESULT                 hr = S_OK;

    saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
    saAttr.bInheritHandle = TRUE; 
    saAttr.lpSecurityDescriptor = NULL;

    hStdoutHandle = CreateFileW( pServerProcess->QueryFullLogPath(),
                                 FILE_READ_DATA,
                                 FILE_SHARE_WRITE,
                                 &saAttr,
                                 OPEN_ALWAYS,
                                 FILE_ATTRIBUTE_NORMAL,
                                 NULL );
    if( hStdoutHandle == INVALID_HANDLE_VALUE )
    {
        hr = HRESULT_FROM_GETLASTERROR();
    }

    CloseHandle( hStdoutHandle );
}

HRESULT
SERVER_PROCESS::CheckIfServerIsUp(
    _In_  DWORD      dwPort,
    _Out_ BOOL      *fReady
)
{
    HRESULT         hr = S_OK;
    int             iResult = 0;
    SOCKADDR_IN     sockAddr;
    BOOL            fLocked = FALSE;

    _ASSERT( fReady != NULL );

    *fReady = FALSE;

    EnterCriticalSection( &m_csLock );
    fLocked = TRUE;

    if( m_socket == INVALID_SOCKET || m_socket == NULL)
    {
        m_socket = socket( AF_INET, SOCK_STREAM, IPPROTO_TCP );
        if( m_socket == INVALID_SOCKET )
        {
            hr = HRESULT_FROM_WIN32(WSAGetLastError());
            goto Finished;
        }
    }

    sockAddr.sin_family = AF_INET;
	if( !inet_pton(AF_INET, LOCALHOST, &(sockAddr.sin_addr)))
	{
		hr = HRESULT_FROM_WIN32(WSAGetLastError());
		goto Finished;
	}
    //sockAddr.sin_addr.s_addr = inet_addr( LOCALHOST );
    sockAddr.sin_port = htons( (u_short) dwPort );

    //
    // Connect to server.
    // if connection fails, socket is not closed, we reuse the same socket
    // while retrying
    //

    iResult = connect(m_socket, (SOCKADDR *) &sockAddr, sizeof (sockAddr));
    if (iResult == SOCKET_ERROR) 
    {
        hr = HRESULT_FROM_WIN32( WSAGetLastError() );
        goto Finished;
    }

    //
    // Connected successfully, close socket.
    //

    iResult = closesocket( m_socket );
    if (iResult == SOCKET_ERROR) 
    {
        hr = HRESULT_FROM_WIN32( WSAGetLastError() );
        goto Finished;
    }

    m_socket = NULL;
    *fReady = TRUE;

Finished:

    if( fLocked )
    {
        LeaveCriticalSection( &m_csLock );
        fLocked = FALSE;
    }

    return hr;
}

HRESULT
SERVER_PROCESS::CheckIfServerIsUp(
    _In_  DWORD       dwPort,
    _Out_ DWORD     * pdwProcessId,
    _Out_ BOOL      * pfReady
)
{
    HRESULT                 hr = S_OK;
    DWORD                   dwResult = 0;
    MIB_TCPTABLE_OWNER_PID *pTCPInfo = NULL;
    MIB_TCPROW_OWNER_PID   *pOwner = NULL;
    DWORD                   dwSize = 0;

    DBG_ASSERT( pfReady );
    DBG_ASSERT( pdwProcessId );

    *pfReady = FALSE;
    *pdwProcessId = 0;
    
    dwResult = GetExtendedTcpTable(NULL, 
                                   &dwSize, 
                                   FALSE, 
                                   AF_INET, 
                                   TCP_TABLE_OWNER_PID_LISTENER, 
                                   0);
    if (dwResult != NO_ERROR && dwResult != ERROR_INSUFFICIENT_BUFFER)
    {
        hr = HRESULT_FROM_WIN32( dwResult );
        goto Finished;
    }

    pTCPInfo = (MIB_TCPTABLE_OWNER_PID*)HeapAlloc(GetProcessHeap(), 0, dwSize);
    if(pTCPInfo == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    
    dwResult = GetExtendedTcpTable(pTCPInfo, 
                                   &dwSize, 
                                   FALSE, 
                                   AF_INET, 
                                   TCP_TABLE_OWNER_PID_LISTENER, 
                                   0);
    if (dwResult != NO_ERROR)
    {
        hr = HRESULT_FROM_WIN32( dwResult );
        goto Finished;
    }

    // iterate pTcpInfo struct to find PID/PORT entry
    for (DWORD dwLoop = 0; dwLoop < pTCPInfo->dwNumEntries; dwLoop++)
    {
        pOwner = &pTCPInfo->table[dwLoop];
        if( ntohs((USHORT)pOwner->dwLocalPort) == dwPort )
        {
            *pdwProcessId = pOwner->dwOwningPid;
            *pfReady = TRUE;
            break;
        }
    }

Finished:

    if( pTCPInfo != NULL )
    {
        HeapFree( GetProcessHeap(), 0, pTCPInfo );
        pTCPInfo = NULL;
    }

    return hr;
}

// send ctrl-c signnal to the process to let it graceful shutdown
// if the process cannot shutdown within given time, terminate it
// todo: allow user to config this shutdown timeout

VOID
SERVER_PROCESS::SendSignal(
    VOID
)
{
    HANDLE    hProc;
    BOOL      fIsSuccess = FALSE;
    LPCWSTR   apsz[1];
    STACK_STRU(strEventMsg, 256);

    hProc = OpenProcess(SYNCHRONIZE | PROCESS_TERMINATE, FALSE, m_dwProcessId);
    if (hProc != INVALID_HANDLE_VALUE)
    {
        fIsSuccess = GenerateConsoleCtrlEvent(CTRL_C_EVENT, m_dwProcessId);

        if (!fIsSuccess)
        {
            if (AttachConsole(m_dwProcessId))
            {
                fIsSuccess = GenerateConsoleCtrlEvent(CTRL_C_EVENT, m_dwProcessId);
                FreeConsole();
                CloseHandle(m_hProcessHandle);
                m_hProcessHandle = INVALID_HANDLE_VALUE;
            }

            if (!fIsSuccess)
            {
                if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                    ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE_MSG,
                    m_dwProcessId )))
                {
                    apsz[0] = strEventMsg.QueryStr();
                    // log a warning for ungraceful shutdown
                    if (FORWARDING_HANDLER::QueryEventLog() != NULL)
                    {
                        ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                            EVENTLOG_INFORMATION_TYPE,
                            0,
                            ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE,
                            NULL,
                            1,
                            0,
                            apsz,
                            NULL);
                    }
                }
            }
        }

        if (!fIsSuccess || (WaitForSingleObject(hProc, m_dwShutdownTimeLimitInMS) != WAIT_OBJECT_0))
        {
            // cannot gracefule shutdown or timeout
            // terminate the process
            TerminateProcess(m_hProcessHandle, 0);
        }
    }
    if (hProc != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hProc);
        hProc = INVALID_HANDLE_VALUE;
    }
}

//
// StopProcess is only called if process crashes OR if the process
// creation failed and calling this counts towards RapidFailCounts.
//

VOID
SERVER_PROCESS::StopProcess(
    VOID
)
{
    m_fReady = FALSE;

    m_pProcessManager->IncrementRapidFailCount();

    for(INT i=0;i<MAX_ACTIVE_CHILD_PROCESSES; ++i)
    {
        if(m_hChildProcessHandles[i] != NULL)
        {
            if( m_hChildProcessHandles[i] != INVALID_HANDLE_VALUE )
            {
                TerminateProcess( m_hChildProcessHandles[i], 0 );
                CloseHandle( m_hChildProcessHandles[i] );
            }
            m_hChildProcessHandles[i] = NULL;
            m_dwChildProcessIds[i] = 0;
        }
    }

    if( m_hProcessHandle != NULL )
    {
        if( m_hProcessHandle != INVALID_HANDLE_VALUE )
        {
            TerminateProcess( m_hProcessHandle, 0 );
            CloseHandle( m_hProcessHandle );
        }
        m_hProcessHandle = NULL;
    }
}

BOOL 
SERVER_PROCESS::IsDebuggerIsAttached(
    VOID
)
{
    HRESULT                             hr = S_OK;
    PJOBOBJECT_BASIC_PROCESS_ID_LIST    processList = NULL;
    DWORD                               dwPid = 0;
    DWORD                               dwWorkerProcessPid = 0;
    DWORD                               cbNumBytes = 1024;
    DWORD                               dwRetries = 0;
    DWORD                               dwError = NO_ERROR;
    BOOL                                fDebuggerPresent = FALSE;

    dwWorkerProcessPid = GetCurrentProcessId();

    do
    {
        dwError = NO_ERROR;

        if( processList != NULL )
        {
            HeapFree(GetProcessHeap(), 0, processList);
            processList = NULL;

            // resize
            cbNumBytes = cbNumBytes * 2;
        }

        processList = (PJOBOBJECT_BASIC_PROCESS_ID_LIST) HeapAlloc(
                            GetProcessHeap(), 
                            0, 
                            cbNumBytes
                            );
        if( processList == NULL )
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        RtlZeroMemory( processList, cbNumBytes );

        if( !QueryInformationJobObject( 
                m_hJobObject, 
                JobObjectBasicProcessIdList, 
                processList, 
                cbNumBytes, 
                NULL) )
        {
            dwError = GetLastError();
            if( dwError != ERROR_MORE_DATA )
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }

    } while( dwRetries++ < 5 && 
             processList != NULL && 
             ( processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || 
               processList->NumberOfProcessIdsInList == 0 ) );

    if( dwError == ERROR_MORE_DATA )
    {
        hr = E_OUTOFMEMORY;
        // some error
        goto Finished;
    }

    if( processList == NULL || 
        ( processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || 
        processList->NumberOfProcessIdsInList == 0 ) )
    {
        hr = HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED);
        // some error
        goto Finished;
    }

    if( processList->NumberOfProcessIdsInList > MAX_ACTIVE_CHILD_PROCESSES )
    {
        hr = HRESULT_FROM_WIN32( ERROR_CREATE_FAILED );
        goto Finished;
    }
 
    for( DWORD i=0; i<processList->NumberOfProcessIdsInList; i++ )
    {
        dwPid = (DWORD)processList->ProcessIdList[i];
        if( dwPid != dwWorkerProcessPid )
        {
            HANDLE hProcess = OpenProcess( 
                                            PROCESS_QUERY_INFORMATION | SYNCHRONIZE | PROCESS_TERMINATE | PROCESS_DUP_HANDLE,
                                            FALSE, 
                                            dwPid 
                                        );
            BOOL returnValue = CheckRemoteDebuggerPresent( hProcess, &fDebuggerPresent );
            if( ! returnValue )
            {
                goto Finished;
            }

            if( fDebuggerPresent )
            {
                break;
            }
        }
    }

Finished:

    if( processList != NULL )
    {
        HeapFree(GetProcessHeap(), 0, processList);
    }

    return fDebuggerPresent;
}

HRESULT 
SERVER_PROCESS::GetChildProcessHandles(
    VOID
)
{
    HRESULT                             hr = S_OK;
    PJOBOBJECT_BASIC_PROCESS_ID_LIST    processList = NULL;
    DWORD                               dwPid = 0;
    DWORD                               dwWorkerProcessPid = 0;
    DWORD                               cbNumBytes = 1024;
    DWORD                               dwRetries = 0;
    DWORD                               dwError = NO_ERROR;

    dwWorkerProcessPid = GetCurrentProcessId();

    do
    {
        dwError = NO_ERROR;

        if( processList != NULL )
        {
            HeapFree(GetProcessHeap(), 0, processList);
            processList = NULL;

            // resize
            cbNumBytes = cbNumBytes * 2;
        }

        processList = (PJOBOBJECT_BASIC_PROCESS_ID_LIST) HeapAlloc(
                            GetProcessHeap(), 
                            0, 
                            cbNumBytes
                            );
        if( processList == NULL )
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        RtlZeroMemory( processList, cbNumBytes );

        if( !QueryInformationJobObject( 
                m_hJobObject, 
                JobObjectBasicProcessIdList, 
                processList, 
                cbNumBytes, 
                NULL) )
        {
            dwError = GetLastError();
            if( dwError != ERROR_MORE_DATA )
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }

    } while( dwRetries++ < 5 && 
             processList != NULL && 
             ( processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0 ) );

    if( dwError == ERROR_MORE_DATA )
    {
        hr = E_OUTOFMEMORY;
        // some error
        goto Finished;
    }

    if( processList == NULL || ( processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0 ) )
    {
        hr = HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED);
        // some error
        goto Finished;
    }

    if( processList->NumberOfProcessIdsInList > MAX_ACTIVE_CHILD_PROCESSES )
    {
        hr = HRESULT_FROM_WIN32( ERROR_CREATE_FAILED );
        goto Finished;
    }
 
    for( DWORD i=0; i<processList->NumberOfProcessIdsInList; i++ )
    {
        dwPid = (DWORD)processList->ProcessIdList[i];
        if( dwPid != m_dwProcessId &&
            dwPid != dwWorkerProcessPid )
        {
            m_hChildProcessHandles[m_cChildProcess] = OpenProcess( 
                                            PROCESS_QUERY_INFORMATION | SYNCHRONIZE | PROCESS_TERMINATE | PROCESS_DUP_HANDLE,
                                            FALSE, 
                                            dwPid 
                                        );
            m_dwChildProcessIds[m_cChildProcess] = dwPid;
            m_cChildProcess ++;
        }
    }

Finished:

    if( processList != NULL )
    {
        HeapFree(GetProcessHeap(), 0, processList);
    }

    return hr;
}

HRESULT
SERVER_PROCESS::StopAllProcessesInJobObject(
        VOID
)
{
    HRESULT                             hr = S_OK;
    PJOBOBJECT_BASIC_PROCESS_ID_LIST    processList = NULL;
    HANDLE                              hProcess = NULL;
    DWORD                               dwWorkerProcessPid = 0;
    DWORD                               cbNumBytes = 1024;
    DWORD                               dwRetries = 0;

    dwWorkerProcessPid = GetCurrentProcessId();

    do
    {
        if( processList != NULL )
        {
            HeapFree(GetProcessHeap(), 0, processList);
            processList = NULL;

            // resize
            cbNumBytes = cbNumBytes * 2;
        }

        processList = (PJOBOBJECT_BASIC_PROCESS_ID_LIST) HeapAlloc(
                            GetProcessHeap(), 
                            0, 
                            cbNumBytes
                            );
        if( processList == NULL )
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        RtlZeroMemory( processList, cbNumBytes );

        if( !QueryInformationJobObject( 
                m_hJobObject, 
                JobObjectBasicProcessIdList, 
                processList, 
                cbNumBytes, 
                NULL) )
        {
            DWORD dwError = GetLastError();
            if( dwError != ERROR_MORE_DATA )
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }

    } while( dwRetries++ < 5 && 
             processList != NULL && 
             ( processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0 ) );

    if( processList == NULL || ( processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0 ) )
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
        // some error
        goto Finished;
    }
 
    for( DWORD i=0; i<processList->NumberOfProcessIdsInList; i++ )
    {
        if( dwWorkerProcessPid != (DWORD)processList->ProcessIdList[i] )
        {
            hProcess = OpenProcess( PROCESS_TERMINATE, 
                                    FALSE, 
                                    (DWORD)processList->ProcessIdList[i] );
            if( hProcess != NULL )
            {
                if( !TerminateProcess(hProcess, 1) )
                {
                    hr = HRESULT_FROM_GETLASTERROR();
                }
                else
                {
                    WaitForSingleObject( hProcess, INFINITE );
                }

                if( hProcess != NULL )
                {
                    CloseHandle( hProcess );
                    hProcess = NULL;
                }
            }
        }
    }

Finished:

    if( processList != NULL )
    {
        HeapFree(GetProcessHeap(), 0, processList);
    }

    return hr;
}

SERVER_PROCESS::SERVER_PROCESS() : 
    m_cRefs( 1 ),
    m_CancelEvent( NULL ),
    m_hProcessHandle( NULL ),
    m_hProcessWaitHandle( NULL ),
    m_dwProcessId( 0 ),
    m_cChildProcess( 0 ),
    m_socket( INVALID_SOCKET ),
    m_fReady( FALSE ),
    m_fStopping( FALSE ),
    m_hStdoutHandle( NULL ),
    m_fStdoutLogEnabled( FALSE ),
    m_hJobObject( NULL ),
    m_pForwarderConnection( NULL ),
    m_dwListeningProcessId( 0 ),
    m_hListeningProcessHandle( NULL )
{
    InterlockedIncrement(&g_dwActiveServerProcesses);
    srand((unsigned int)time(NULL));
    InitializeCriticalSection( &m_csLock );

    for(INT i=0;i<MAX_ACTIVE_CHILD_PROCESSES; ++i)
    {
        m_dwChildProcessIds[i] = 0;
        m_hChildProcessHandles[i] = NULL;
        m_hChildProcessWaitHandles[i] = NULL;
    }
}

SERVER_PROCESS::~SERVER_PROCESS()
{
    if(m_socket != NULL)
    {
        closesocket( m_socket );
        m_socket = NULL;
    }

    if(m_hProcessWaitHandle != NULL)
    {
        UnregisterWait( m_hProcessWaitHandle );
        m_hProcessWaitHandle = NULL;
    }

    if( m_CancelEvent != NULL )
    {
        SetEvent( m_CancelEvent );
        CloseHandle( m_CancelEvent );
        m_CancelEvent = NULL;
    }

    for(INT i=0;i<MAX_ACTIVE_CHILD_PROCESSES;++i)
    {
        if(m_hChildProcessWaitHandles[i] != NULL)
        {
            UnregisterWait( m_hChildProcessWaitHandles[i] );
            m_hChildProcessWaitHandles[i] = NULL;
        }
    }

    if(m_hProcessHandle != NULL)
    {
        if(m_hProcessHandle != INVALID_HANDLE_VALUE)
        {
            CloseHandle( m_hProcessHandle );
        }
        m_hProcessHandle = NULL;
    }

    if(m_hListeningProcessHandle != NULL)
    {
        if(m_hListeningProcessHandle != INVALID_HANDLE_VALUE)
        {
            CloseHandle( m_hListeningProcessHandle );
        }
        m_hListeningProcessHandle = NULL;
    }

    for(INT i=0;i<MAX_ACTIVE_CHILD_PROCESSES;++i)
    {
        if(m_hChildProcessHandles[i] != NULL)
        {
            if(m_hChildProcessHandles[i] != INVALID_HANDLE_VALUE)
            {
                CloseHandle( m_hChildProcessHandles[i] );
            }
            m_hChildProcessHandles[i] = NULL;
            m_dwChildProcessIds[i] = 0;
        }
    }

    if( m_hStdoutHandle != NULL )
    {
        if(m_hStdoutHandle != INVALID_HANDLE_VALUE)
        {
            CloseHandle( m_hStdoutHandle );
        }
        m_hStdoutHandle = NULL;
    }

    if( m_fStdoutLogEnabled )
    {
        m_Timer.CancelTimer();
    }

    if( m_hJobObject != NULL )
    {
        if(m_hJobObject != INVALID_HANDLE_VALUE)
        {
            CloseHandle( m_hJobObject );
        }
        m_hJobObject = NULL;
    }

    if( m_pProcessManager != NULL )
    {
        m_pProcessManager->DereferenceProcessManager();
        m_pProcessManager = NULL;
    }

    if(m_pForwarderConnection != NULL)
    {
        m_pForwarderConnection->DereferenceForwarderConnection();
        m_pForwarderConnection = NULL;
    }

    DeleteCriticalSection( &m_csLock );

    InterlockedDecrement(&g_dwActiveServerProcesses);
}

VOID 
ProcessHandleCallback(
    _In_ PVOID  pContext,
    _In_ BOOL
)
{
    SERVER_PROCESS *pServerProcess = (SERVER_PROCESS*) pContext;
    pServerProcess->HandleProcessExit();
}

HRESULT
SERVER_PROCESS::RegisterProcessWait(
    PHANDLE                 phWaitHandle,
    HANDLE                  hProcessToWaitOn
)
{
    HRESULT     hr = S_OK;
    NTSTATUS    status = 0;

    _ASSERT( phWaitHandle != NULL && *phWaitHandle == NULL );

    *phWaitHandle = NULL;

    // wait thread will dereference.
    ReferenceServerProcess();

    status = RegisterWaitForSingleObject( 
                phWaitHandle,
                hProcessToWaitOn,
                (WAITORTIMERCALLBACK)&ProcessHandleCallback,
                this,
                INFINITE,
                WT_EXECUTEONLYONCE | WT_EXECUTEINWAITTHREAD 
                );

    if( status < 0 )
    {
        hr = HRESULT_FROM_NT( status );
        goto Finished;
    }

Finished:

    if( FAILED( hr ) ) 
    {
        *phWaitHandle = NULL;
        DereferenceServerProcess();
    }

    return hr;
}

HRESULT
SERVER_PROCESS::HandleProcessExit()
{
    HRESULT     hr = S_OK;
    BOOL        fReady = FALSE;

    CheckIfServerIsUp( m_dwPort, &fReady );

    if( !fReady )
    {
        if( !m_fStopping )
        {
            m_fStopping = TRUE;
            m_pProcessManager->ShutdownProcess( this );
        }
    }

    DereferenceServerProcess();

    return hr;
}