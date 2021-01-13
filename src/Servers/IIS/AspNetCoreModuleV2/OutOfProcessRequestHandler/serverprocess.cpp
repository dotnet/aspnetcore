// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "serverprocess.h"

#include <IPHlpApi.h>
#include "EventLog.h"
#include "file_utility.h"
#include "exceptions.h"
//#include <share.h>

//extern BOOL g_fNsiApiNotSupported;

#define STARTUP_TIME_LIMIT_INCREMENT_IN_MILLISECONDS 5000

HRESULT
SERVER_PROCESS::Initialize(
    PROCESS_MANAGER      *pProcessManager,
    STRU                 *pszProcessExePath,
    STRU                 *pszArguments,
    DWORD                 dwStartupTimeLimitInMS,
    DWORD                 dwShtudownTimeLimitInMS,
    BOOL                  fWindowsAuthEnabled,
    BOOL                  fBasicAuthEnabled,
    BOOL                  fAnonymousAuthEnabled,
    std::map<std::wstring, std::wstring, ignore_case_comparer>& pEnvironmentVariables,
    BOOL                  fStdoutLogEnabled,
    BOOL                  fEnableOutOfProcessConsoleRedirection,
    BOOL                  fWebSocketSupported,
    STRU                  *pstruStdoutLogFile,
    STRU                  *pszAppPhysicalPath,
    STRU                  *pszAppPath,
    STRU                  *pszAppVirtualPath,
    STRU                  *pszHttpsPort
)
{
    HRESULT hr = S_OK;

    m_pProcessManager = pProcessManager;
    m_dwStartupTimeLimitInMS = dwStartupTimeLimitInMS;
    m_dwShutdownTimeLimitInMS = dwShtudownTimeLimitInMS;
    m_fStdoutLogEnabled = fStdoutLogEnabled;
    m_fWebSocketSupported = fWebSocketSupported;
    m_fWindowsAuthEnabled = fWindowsAuthEnabled;
    m_fBasicAuthEnabled = fBasicAuthEnabled;
    m_fAnonymousAuthEnabled = fAnonymousAuthEnabled;
    m_fEnableOutOfProcessConsoleRedirection = fEnableOutOfProcessConsoleRedirection;
    m_pProcessManager->ReferenceProcessManager();
    m_fDebuggerAttached = FALSE;

    if (FAILED_LOG(hr = m_ProcessPath.Copy(*pszProcessExePath)) ||
        FAILED_LOG(hr = m_struLogFile.Copy(*pstruStdoutLogFile))||
        FAILED_LOG(hr = m_struPhysicalPath.Copy(*pszAppPhysicalPath))||
        FAILED_LOG(hr = m_struAppFullPath.Copy(*pszAppPath))||
        FAILED_LOG(hr = m_struAppVirtualPath.Copy(*pszAppVirtualPath))||
        FAILED_LOG(hr = m_Arguments.Copy(*pszArguments)) ||
        FAILED_LOG(hr = m_struHttpsPort.Copy(*pszHttpsPort)) ||
        FAILED_LOG(hr = SetupJobObject()))
    {
        goto Finished;
    }

    m_pEnvironmentVarTable = pEnvironmentVariables;

Finished:
    return hr;
}

HRESULT
SERVER_PROCESS::SetupJobObject(VOID)
{
    HRESULT                                 hr = S_OK;
    JOBOBJECT_EXTENDED_LIMIT_INFORMATION    jobInfo = { 0 };

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
            }
        }
    }

    return hr;
}

HRESULT
SERVER_PROCESS::GetRandomPort
(
    DWORD* pdwPickedPort,
    DWORD  dwExcludedPort = 0
)
{
    HRESULT hr = S_OK;
    BOOL    fPortInUse = FALSE;
    DWORD   dwActualProcessId = 0;

    std::uniform_int_distribution<> dist(MIN_PORT, MAX_PORT);

    if (g_fNsiApiNotSupported)
    {
        //
        // the default value for optional parameter dwExcludedPort is 0 which is reserved
        // a random number between MIN_PORT and MAX_PORT
        //
        while ((*pdwPickedPort = dist(m_randomGenerator)) == dwExcludedPort);
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
            while ((*pdwPickedPort = dist(m_randomGenerator)) == dwExcludedPort);
            hr = CheckIfServerIsUp(*pdwPickedPort, &dwActualProcessId, &fPortInUse);
        } while (fPortInUse && ++cRetry < MAX_RETRY);

        if (cRetry >= MAX_RETRY)
        {
            hr = HRESULT_FROM_WIN32(ERROR_PORT_NOT_SET);
        }
    }

    return hr;
}

HRESULT
SERVER_PROCESS::SetupListenPort(
    ENVIRONMENT_VAR_HASH    *pEnvironmentVarTable,
    BOOL*                    pfCriticalError
)
{
    HRESULT hr = S_OK;
    ENVIRONMENT_VAR_ENTRY *pEntry = NULL;
    *pfCriticalError = FALSE;

    pEnvironmentVarTable->FindKey(ASPNETCORE_PORT_ENV_STR, &pEntry);
    if (pEntry != NULL)
    {
        if (pEntry->QueryValue() != NULL && pEntry->QueryValue()[0] != L'\0')
        {
            m_dwPort = (DWORD)_wtoi(pEntry->QueryValue());
            if (m_dwPort >MAX_PORT || m_dwPort < MIN_PORT)
            {
                hr = E_INVALIDARG;
                *pfCriticalError = TRUE;
                goto Finished;
                // need add log for this one
            }
            hr = m_struPort.Copy(pEntry->QueryValue());
            goto Finished;
        }
        else
        {
            //
            // user set the env variable but did not give value, let's set it up
            //
            pEnvironmentVarTable->DeleteKey(ASPNETCORE_PORT_ENV_STR);
            pEntry->Dereference();
            pEntry = NULL;
        }
    }

    WCHAR buffer[15];
    if (FAILED_LOG(hr = GetRandomPort(&m_dwPort)))
    {
        goto Finished;
    }

    if (swprintf_s(buffer, 15, L"%d", m_dwPort) <= 0)
    {
        hr = E_INVALIDARG;
        goto Finished;
    }

    pEntry = new ENVIRONMENT_VAR_ENTRY();
    if (pEntry == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    if (FAILED_LOG(hr = pEntry->Initialize(ASPNETCORE_PORT_ENV_STR, buffer)) ||
        FAILED_LOG(hr = pEnvironmentVarTable->InsertRecord(pEntry)) ||
        FAILED_LOG(hr = m_struPort.Copy(buffer)))
    {
        goto Finished;
    }

Finished:
    if (pEntry != NULL)
    {
        pEntry->Dereference();
        pEntry = NULL;
    }

    if (FAILED_LOG(hr))
    {
        EventLog::Error(
            ASPNETCORE_EVENT_PROCESS_START_SUCCESS,
            ASPNETCORE_EVENT_PROCESS_START_PORTSETUP_ERROR_MSG,
            m_struAppFullPath.QueryStr(),
            m_struPhysicalPath.QueryStr(),
            m_dwPort,
            MIN_PORT,
            MAX_PORT,
            hr);
    }

    return hr;
}

HRESULT
SERVER_PROCESS::SetupAppPath(
    ENVIRONMENT_VAR_HASH*    pEnvironmentVarTable
)
{
    HRESULT      hr = S_OK;
    ENVIRONMENT_VAR_ENTRY*  pEntry = NULL;

    pEnvironmentVarTable->FindKey(ASPNETCORE_APP_PATH_ENV_STR, &pEntry);
    if (pEntry != NULL)
    {
        // user should not set this environment variable in configuration
        pEnvironmentVarTable->DeleteKey(ASPNETCORE_APP_PATH_ENV_STR);
        pEntry->Dereference();
        pEntry = NULL;
    }

    pEntry = new ENVIRONMENT_VAR_ENTRY();
    if (pEntry == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    if (FAILED_LOG(hr = pEntry->Initialize(ASPNETCORE_APP_PATH_ENV_STR, m_struAppVirtualPath.QueryStr())) ||
        FAILED_LOG(hr = pEnvironmentVarTable->InsertRecord(pEntry)))
    {
        goto Finished;
    }

Finished:
    if (pEntry != NULL)
    {
        pEntry->Dereference();
        pEntry = NULL;
    }
    return hr;
}

HRESULT
SERVER_PROCESS::SetupAppToken(
    ENVIRONMENT_VAR_HASH    *pEnvironmentVarTable
)
{
    HRESULT     hr = S_OK;
    UUID        logUuid;
    PSTR        pszLogUuid = NULL;
    BOOL        fRpcStringAllocd = FALSE;
    RPC_STATUS  rpcStatus;
    STRU        strAppToken;
    ENVIRONMENT_VAR_ENTRY*  pEntry = NULL;

    pEnvironmentVarTable->FindKey(ASPNETCORE_APP_TOKEN_ENV_STR, &pEntry);
    if (pEntry != NULL)
    {
        // user sets the environment variable
        m_straGuid.Reset();
        hr = m_straGuid.CopyW(pEntry->QueryValue());
        pEntry->Dereference();
        pEntry = NULL;
        goto Finished;
    }
    else
    {
        if (m_straGuid.IsEmpty())
        {
            // the GUID has not been set yet
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

            fRpcStringAllocd = TRUE;

            if (FAILED_LOG(hr = m_straGuid.Copy(pszLogUuid)))
            {
                goto Finished;
            }
        }

        pEntry = new ENVIRONMENT_VAR_ENTRY();
        if (pEntry == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        if (FAILED_LOG(strAppToken.CopyA(m_straGuid.QueryStr())) ||
            FAILED_LOG(hr = pEntry->Initialize(ASPNETCORE_APP_TOKEN_ENV_STR, strAppToken.QueryStr())) ||
            FAILED_LOG(hr = pEnvironmentVarTable->InsertRecord(pEntry)))
        {
            goto Finished;
        }
    }

Finished:

    if (fRpcStringAllocd)
    {
        RpcStringFreeA((BYTE **)&pszLogUuid);
        pszLogUuid = NULL;
    }
    if (pEntry != NULL)
    {
        pEntry->Dereference();
        pEntry = NULL;
    }
    return hr;
}

HRESULT
SERVER_PROCESS::OutputEnvironmentVariables
(
    MULTISZ*                pmszOutput,
    ENVIRONMENT_VAR_HASH*   pEnvironmentVarTable
)
{
    HRESULT    hr = S_OK;
    LPWSTR     pszEnvironmentVariables = NULL;
    LPWSTR     pszCurrentVariable = NULL;
    LPWSTR     pszNextVariable = NULL;
    LPWSTR     pszEqualChar = NULL;
    STRU       strEnvVar;
    ENVIRONMENT_VAR_ENTRY* pEntry = NULL;

    DBG_ASSERT(pmszOutput);
    DBG_ASSERT(pEnvironmentVarTable); // We added some startup variables
    DBG_ASSERT(pEnvironmentVarTable->Count() >0);

    // cleanup, as we may in retry logic
    pmszOutput->Reset();

    pszEnvironmentVariables = GetEnvironmentStringsW();
    if (pszEnvironmentVariables == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_ENVIRONMENT);
        goto Finished;
    }
    pszCurrentVariable = pszEnvironmentVariables;
    while (*pszCurrentVariable != L'\0')
    {
        pszNextVariable = pszCurrentVariable + wcslen(pszCurrentVariable) + 1;
        pszEqualChar = wcschr(pszCurrentVariable, L'=');
        if (pszEqualChar != NULL)
        {
            if (FAILED_LOG(hr = strEnvVar.Copy(pszCurrentVariable, (DWORD)(pszEqualChar - pszCurrentVariable) + 1)))
            {
                goto Finished;
            }
            pEnvironmentVarTable->FindKey(strEnvVar.QueryStr(), &pEntry);
            if (pEntry != NULL)
            {
                // same env variable is defined in configuration, use it
                if (FAILED_LOG(hr = strEnvVar.Append(pEntry->QueryValue())))
                {
                    goto Finished;
                }
                pmszOutput->Append(strEnvVar);  //should we check the returned bool
                // remove the record from hash table as we already output it
                pEntry->Dereference();
                pEnvironmentVarTable->DeleteKey(pEntry->QueryName());
                strEnvVar.Reset();
                pEntry = NULL;
            }
            else
            {
                pmszOutput->Append(pszCurrentVariable);
            }
        }
        else
        {
            // env varaible is not well formated
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_ENVIRONMENT);
            goto Finished;
        }
        // move to next env variable
        pszCurrentVariable = pszNextVariable;
    }
    // append the remaining env variable in hash table
    pEnvironmentVarTable->Apply(ENVIRONMENT_VAR_HELPERS::CopyToMultiSz, pmszOutput);

Finished:
    if (pszEnvironmentVariables != NULL)
    {
        FreeEnvironmentStringsW(pszEnvironmentVariables);
        pszEnvironmentVariables = NULL;
    }
    return hr;
}

HRESULT
SERVER_PROCESS::SetupCommandLine(
    STRU*      pstrCommandLine
)
{
    HRESULT    hr = S_OK;
    LPWSTR     pszPath = NULL;
    LPWSTR     pszFullPath = NULL;
    STRU       strRelativePath;
    DWORD      dwBufferSize = 0;
    FILE       *file = NULL;

    DBG_ASSERT(pstrCommandLine);

    if (!m_struCommandLine.IsEmpty() &&
        pstrCommandLine == (&m_struCommandLine))
    {
        // already set up the commandline string, skip
        goto Finished;
    }

    pszPath = m_ProcessPath.QueryStr();

    if ((wcsstr(pszPath, L":") == NULL) && (wcsstr(pszPath, L"%") == NULL))
    {
        // let's check whether it is a relative path
        if (FAILED_LOG(hr = strRelativePath.Copy(m_struPhysicalPath.QueryStr())) ||
            FAILED_LOG(hr = strRelativePath.Append(L"\\")) ||
            FAILED_LOG(hr = strRelativePath.Append(pszPath)))
        {
            goto Finished;
        }

        dwBufferSize = strRelativePath.QueryCCH() + 1;
        pszFullPath = new WCHAR[dwBufferSize];
        if (pszFullPath == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        if (_wfullpath(pszFullPath,
            strRelativePath.QueryStr(),
            dwBufferSize) == NULL)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
            goto Finished;
        }

        if ((file = _wfsopen(pszFullPath, L"r", _SH_DENYNO)) != NULL)
        {
            fclose(file);
            pszPath = pszFullPath;
        }
    }
    if (FAILED_LOG(hr = pstrCommandLine->Copy(pszPath)) ||
        FAILED_LOG(hr = pstrCommandLine->Append(L" ")) ||
        FAILED_LOG(hr = pstrCommandLine->Append(m_Arguments.QueryStr())))
    {
        goto Finished;
    }

Finished:
    if (pszFullPath != NULL)
    {
        delete pszFullPath;
    }
    return hr;
}

HRESULT
SERVER_PROCESS::PostStartCheck(
    VOID
)
{
    HRESULT hr = S_OK;

    BOOL    fReady = FALSE;
    BOOL    fProcessMatch = FALSE;
    BOOL    fDebuggerAttached = FALSE;
    DWORD   dwTickCount = 0;
    DWORD   dwTimeDifference = 0;
    DWORD   dwActualProcessId = 0;
    INT     iChildProcessIndex = -1;
    STACK_STRU(strEventMsg, 256);

    if (CheckRemoteDebuggerPresent(m_hProcessHandle, &fDebuggerAttached) == 0)
    {
        // some error occurred  - assume debugger is not attached;
        fDebuggerAttached = FALSE;
    }

    dwTickCount = GetTickCount();

    do
    {
        DWORD processStatus = 0;
        if (GetExitCodeProcess(m_hProcessHandle, &processStatus))
        {
            // make sure the process is still running
            if (processStatus != STILL_ACTIVE)
            {
                // double check
                if (GetExitCodeProcess(m_hProcessHandle, &processStatus) && processStatus != STILL_ACTIVE)
                {
                    hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
                    goto Finished;
                }
            }
        }
        //
        // dwActualProcessId will be set only when NsiAPI(GetExtendedTcpTable) is supported
        //
        hr = CheckIfServerIsUp(m_dwPort, &dwActualProcessId, &fReady);
        fDebuggerAttached = IsDebuggerIsAttached();

        if (!fReady)
        {
            Sleep(250);
        }

        dwTimeDifference = (GetTickCount() - dwTickCount);
    } while (fReady == FALSE &&
        ((dwTimeDifference < m_dwStartupTimeLimitInMS) || fDebuggerAttached));

    if (!fReady)
    {
        hr = E_APPLICATION_ACTIVATION_TIMED_OUT;
        goto Finished;
    }

    // register call back with the created process
    if (FAILED_LOG(hr = RegisterProcessWait(&m_hProcessWaitHandle, m_hProcessHandle)))
    {
        goto Finished;
    }

    //
    // check if debugger is attached after startupTimeout.
    //
    if (!fDebuggerAttached &&
        CheckRemoteDebuggerPresent(m_hProcessHandle, &fDebuggerAttached) == 0)
    {
        // some error occurred  - assume debugger is not attached;
        fDebuggerAttached = FALSE;
    }

    if (!g_fNsiApiNotSupported)
    {
        //
        // NsiAPI(GetExtendedTcpTable) is supported. we should check whether processIds matche
        //
        if (dwActualProcessId == m_dwProcessId)
        {
            m_dwListeningProcessId = m_dwProcessId;
            fProcessMatch = TRUE;
        }

        if (!fProcessMatch)
        {
            // could be the scenario that backend creates child process
            if (FAILED_LOG(hr = GetChildProcessHandles()))
            {
                goto Finished;
            }

            for (DWORD i = 0; i < m_cChildProcess; ++i)
            {
                // a child process listen on the assigned port
                if (dwActualProcessId == m_dwChildProcessIds[i])
                {
                    m_dwListeningProcessId = m_dwChildProcessIds[i];
                    fProcessMatch = TRUE;

                    if (m_hChildProcessHandles[i] != NULL)
                    {
                        if (fDebuggerAttached == FALSE &&
                            CheckRemoteDebuggerPresent(m_hChildProcessHandles[i], &fDebuggerAttached) == 0)
                        {
                            // some error occurred  - assume debugger is not attached;
                            fDebuggerAttached = FALSE;
                        }

                        if (FAILED_LOG(hr = RegisterProcessWait(&m_hChildProcessWaitHandles[i],
                            m_hChildProcessHandles[i])))
                        {
                            goto Finished;
                        }
                        iChildProcessIndex = i;
                    }
                    break;
                }
            }
        }

        if(!fProcessMatch)
        {
            //
            // process that we created is not listening
            // on the port we specified.
            //
            fReady = FALSE;
            hr = HRESULT_FROM_WIN32(ERROR_CREATE_FAILED);
            strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_PROCESS_START_WRONGPORT_ERROR_MSG,
                m_struAppFullPath.QueryStr(),
                m_struPhysicalPath.QueryStr(),
                m_struCommandLine.QueryStr(),
                m_dwPort,
                hr);
            goto Finished;
        }
    }

    if (!fReady)
    {
        //
        // hr is already set by CheckIfServerIsUp
        //
        if (dwTimeDifference >= m_dwStartupTimeLimitInMS)
        {
            hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
            strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_PROCESS_START_NOTREADY_ERROR_MSG,
                m_struAppFullPath.QueryStr(),
                m_struPhysicalPath.QueryStr(),
                m_struCommandLine.QueryStr(),
                m_dwPort,
                hr);
        }
        goto Finished;
    }

    if (iChildProcessIndex >= 0)
    {
        //
        // final check to make sure child process listening on HTTP is still UP
        // This is needed because, the child process might have crashed/exited between
        // the previous call to checkIfServerIsUp and RegisterProcessWait
        // and we would not know about it.
        //

        hr = CheckIfServerIsUp(m_dwPort, &dwActualProcessId, &fReady);

        if ((FAILED_LOG(hr) || fReady == FALSE))
        {
            strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_PROCESS_START_NOTREADY_ERROR_MSG,
                m_struAppFullPath.QueryStr(),
                m_struPhysicalPath.QueryStr(),
                m_struCommandLine.QueryStr(),
                m_dwPort,
                hr);
            goto Finished;
        }
    }

    //
    // ready to mark the server process ready but before this,
    // create and initialize the FORWARDER_CONNECTION
    //
    if (m_pForwarderConnection == NULL)
    {
        m_pForwarderConnection = new FORWARDER_CONNECTION();
        if (m_pForwarderConnection == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        hr = m_pForwarderConnection->Initialize(m_dwPort);
        if (FAILED_LOG(hr))
        {
            goto Finished;
        }
    }

    if (!g_fNsiApiNotSupported)
    {
        m_hListeningProcessHandle = OpenProcess(SYNCHRONIZE | PROCESS_TERMINATE | PROCESS_DUP_HANDLE,
                                                FALSE,
                                                m_dwListeningProcessId);
    }

    //
    // mark server process as Ready
    //
    m_fReady = TRUE;

Finished:
    m_fDebuggerAttached = fDebuggerAttached;

    if (FAILED_LOG(hr))
    {
        if (m_pForwarderConnection != NULL)
        {
            m_pForwarderConnection->DereferenceForwarderConnection();
            m_pForwarderConnection = NULL;
        }

        if (!strEventMsg.IsEmpty())
        {
            EventLog::Warn(
                ASPNETCORE_EVENT_PROCESS_START_ERROR,
                strEventMsg.QueryStr());
        }
    }
    return hr;
}

HRESULT
SERVER_PROCESS::StartProcess(
    VOID
)
{
    HRESULT                 hr = S_OK;
    PROCESS_INFORMATION     processInformation = {0};
    STARTUPINFOW            startupInfo = {0};
    DWORD                   dwRetryCount = 2; // should we allow customer to config it
    DWORD                   dwCreationFlags = 0;
    MULTISZ                 mszNewEnvironment;
    ENVIRONMENT_VAR_HASH    *pHashTable = NULL;
    PWSTR                   pStrStage = NULL;
    BOOL                    fCriticalError = FALSE;
    std::map<std::wstring, std::wstring, ignore_case_comparer> variables;

    GetStartupInfoW(&startupInfo);

    //
    // setup stdout and stderr handles to our stdout handle only if
    // the handle is valid.
    //
    SetupStdHandles(&startupInfo);

    while (dwRetryCount > 0)
    {
        m_dwPort = 0;
        dwRetryCount--;
        //
        // generate process command line.
        //
        if (FAILED_LOG(hr = SetupCommandLine(&m_struCommandLine)))
        {
            pStrStage = L"SetupCommandLine";
            goto Failure;
        }

        try
        {
            variables = ENVIRONMENT_VAR_HELPERS::InitEnvironmentVariablesTable(
                m_pEnvironmentVarTable,
                m_fWindowsAuthEnabled,
                m_fBasicAuthEnabled,
                m_fAnonymousAuthEnabled,
                true, // fAddHostingStartup
                m_struAppFullPath.QueryStr(),
                m_struHttpsPort.QueryStr());

            variables = ENVIRONMENT_VAR_HELPERS::AddWebsocketEnabledToEnvironmentVariables(variables, m_fWebSocketSupported);
        }
        CATCH_RETURN();


        pHashTable = new ENVIRONMENT_VAR_HASH();
        RETURN_IF_FAILED(pHashTable->Initialize(37 /*prime*/));
        // Copy environment variables to old style hash table
        for (auto & variable : variables)
        {
            auto pNewEntry = std::unique_ptr<ENVIRONMENT_VAR_ENTRY, ENVIRONMENT_VAR_ENTRY_DELETER>(new ENVIRONMENT_VAR_ENTRY());
            RETURN_IF_FAILED(pNewEntry->Initialize((variable.first + L"=").c_str(), variable.second.c_str()));
            RETURN_IF_FAILED(pHashTable->InsertRecord(pNewEntry.get()));
        }

        //
        // setup the the port that the backend process will listen on
        //
        if (FAILED_LOG(hr = SetupListenPort(pHashTable, &fCriticalError)))
        {
            pStrStage = L"SetupListenPort";
            goto Failure;
        }

        //
        // get app path
        //
        if (FAILED_LOG(hr = SetupAppPath(pHashTable)))
        {
            pStrStage = L"SetupAppPath";
            goto Failure;
        }

        //
        // generate new guid for each process
        //
        if (FAILED_LOG(hr = SetupAppToken(pHashTable)))
        {
            pStrStage = L"SetupAppToken";
            goto Failure;
        }

        //
        // setup environment variables for new process
        //
        if (FAILED_LOG(hr = OutputEnvironmentVariables(&mszNewEnvironment, pHashTable)))
        {
            pStrStage = L"OutputEnvironmentVariables";
            goto Failure;
        }

        dwCreationFlags = CREATE_NO_WINDOW |
            CREATE_UNICODE_ENVIRONMENT |
            CREATE_SUSPENDED |
            CREATE_NEW_PROCESS_GROUP;

        if (!CreateProcessW(
            NULL,                   // applicationName
            m_struCommandLine.QueryStr(),
            NULL,                   // processAttr
            NULL,                   // threadAttr
            TRUE,                   // inheritHandles
            dwCreationFlags,
            mszNewEnvironment.QueryStr(),
            m_struPhysicalPath.QueryStr(), // currentDir
            &startupInfo,
            &processInformation))
        {
            pStrStage = L"CreateProcessW";
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Failure;
        }

        m_hProcessHandle = processInformation.hProcess;
        m_dwProcessId = processInformation.dwProcessId;

        if (FAILED_LOG(hr = SetupJobObject()))
        {
            pStrStage = L"SetupJobObject";
            goto Failure;
        }

        if (m_hJobObject != NULL)
        {
            if (!AssignProcessToJobObject(m_hJobObject, m_hProcessHandle))
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                if (hr != HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED))
                {
                    pStrStage = L"AssignProcessToJobObject";
                    goto Failure;
                }
            }
        }

        if (ResumeThread(processInformation.hThread) == -1)
        {
            pStrStage = L"ResumeThread";
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Failure;
        }

        //
        // need to make sure the server is up and listening on the port specified.
        //
        if (FAILED_LOG(hr = PostStartCheck()))
        {
            pStrStage = L"PostStartCheck";
            goto Failure;
        }

        // Backend process starts successfully. Set retry counter to 0
        dwRetryCount = 0;

        EventLog::Info(
            ASPNETCORE_EVENT_PROCESS_START_SUCCESS,
            ASPNETCORE_EVENT_PROCESS_START_SUCCESS_MSG,
            m_struAppFullPath.QueryStr(),
            m_dwProcessId,
            m_dwListeningProcessId,
            m_dwPort);

        goto Finished;

    Failure:
        if (fCriticalError)
        {
            // Critical error, no retry need to avoid wasting resource and polluting log
            dwRetryCount = 0;
        }

        EventLog::Warn(
            ASPNETCORE_EVENT_PROCESS_START_ERROR,
            ASPNETCORE_EVENT_PROCESS_START_ERROR_MSG,
            m_struAppFullPath.QueryStr(),
            m_struPhysicalPath.QueryStr(),
            m_struCommandLine.QueryStr(),
            pStrStage,
            hr,
            m_dwPort,
            dwRetryCount);

        if (processInformation.hThread != NULL)
        {
            CloseHandle(processInformation.hThread);
            processInformation.hThread = NULL;
        }

        if (pHashTable != NULL)
        {
            pHashTable->Clear();
            delete pHashTable;
            pHashTable = NULL;
        }

        CleanUp();
    }

Finished:
    if (FAILED_LOG(hr) || m_fReady == FALSE)
    {
        if (m_hStdErrWritePipe != NULL)
        {
            if (m_hStdErrWritePipe != INVALID_HANDLE_VALUE)
            {
                CloseHandle(m_hStdErrWritePipe);
            }

            m_hStdErrWritePipe = NULL;
        }

        if (m_hStdoutHandle != NULL)
        {
            if (m_hStdoutHandle != INVALID_HANDLE_VALUE)
            {
                CloseHandle(m_hStdoutHandle);
            }

            m_hStdoutHandle = NULL;
        }

        if (m_fStdoutLogEnabled)
        {
            m_Timer.CancelTimer();
        }

        EventLog::Error(
            ASPNETCORE_EVENT_PROCESS_START_FAILURE,
            ASPNETCORE_EVENT_PROCESS_START_FAILURE_MSG,
            m_struAppFullPath.QueryStr(),
            m_struPhysicalPath.QueryStr(),
            m_struCommandLine.QueryStr(),
            m_dwPort,
            m_output.str().c_str());
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

    if (m_hListeningProcessHandle != NULL && m_hListeningProcessHandle != INVALID_HANDLE_VALUE)
    {
        if (!DuplicateHandle( GetCurrentProcess(),
                             hToken,
                             m_hListeningProcessHandle,
                             pTargetTokenHandle,
                             0,
                             FALSE,
                             DUPLICATE_SAME_ACCESS ))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
    }

Finished:

    return hr;
}

HRESULT
SERVER_PROCESS::SetupStdHandles(
    LPSTARTUPINFOW pStartupInfo
)
{
    HRESULT                 hr = S_OK;
    SYSTEMTIME              systemTime;
    SECURITY_ATTRIBUTES     saAttr = { 0 };

    STRU                    struPath;

    DBG_ASSERT(pStartupInfo);

    saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
    saAttr.bInheritHandle = TRUE;
    saAttr.lpSecurityDescriptor = NULL;

    if (!m_fEnableOutOfProcessConsoleRedirection)
    {
        pStartupInfo->dwFlags = STARTF_USESTDHANDLES;
        pStartupInfo->hStdInput = INVALID_HANDLE_VALUE;
        pStartupInfo->hStdError = INVALID_HANDLE_VALUE;
        pStartupInfo->hStdOutput = INVALID_HANDLE_VALUE;
        return hr;
    }

    if (!m_fStdoutLogEnabled)
    {
        CreatePipe(&m_hStdoutHandle, &m_hStdErrWritePipe, &saAttr, 0 /*nSize*/);

        // Read the stderr handle on a separate thread until we get 30Kb.
        m_hReadThread = CreateThread(
            nullptr,       // default security attributes
            0,          // default stack size
            reinterpret_cast<LPTHREAD_START_ROUTINE>(ReadStdErrHandle),
            this,       // thread function arguments
            0,          // default creation flags
            nullptr);      // receive thread identifier

        pStartupInfo->dwFlags = STARTF_USESTDHANDLES;
        pStartupInfo->hStdInput = INVALID_HANDLE_VALUE;
        pStartupInfo->hStdError = m_hStdErrWritePipe;
        pStartupInfo->hStdOutput = m_hStdErrWritePipe;
        return hr;
    }

    hr = FILE_UTILITY::ConvertPathToFullPath(
                 m_struLogFile.QueryStr(),
                 m_struPhysicalPath.QueryStr(),
                 &struPath);
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

    GetSystemTime(&systemTime);
    hr = m_struFullLogFile.SafeSnwprintf(L"%s_%d%02d%02d%02d%02d%02d_%d.log",
        struPath.QueryStr(),
        systemTime.wYear,
        systemTime.wMonth,
        systemTime.wDay,
        systemTime.wHour,
        systemTime.wMinute,
        systemTime.wSecond,
        GetCurrentProcessId());
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

    hr = FILE_UTILITY::EnsureDirectoryPathExist(struPath.QueryStr());
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

    m_hStdoutHandle = CreateFileW(m_struFullLogFile.QueryStr(),
        FILE_WRITE_DATA,
        FILE_SHARE_READ,
        &saAttr,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if (m_hStdoutHandle == INVALID_HANDLE_VALUE)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    pStartupInfo->dwFlags = STARTF_USESTDHANDLES;
    pStartupInfo->hStdInput = INVALID_HANDLE_VALUE;
    pStartupInfo->hStdError = m_hStdoutHandle;
    pStartupInfo->hStdOutput = m_hStdoutHandle;
    // start timer to open and close handles regularly.
    m_Timer.InitializeTimer(STTIMER::TimerCallback, &m_struFullLogFile, 3000, 3000);

Finished:
    if (FAILED_LOG(hr))
    {
        pStartupInfo->dwFlags = STARTF_USESTDHANDLES;
        pStartupInfo->hStdInput = INVALID_HANDLE_VALUE;
        pStartupInfo->hStdError = INVALID_HANDLE_VALUE;
        pStartupInfo->hStdOutput = INVALID_HANDLE_VALUE;

        if (m_fStdoutLogEnabled)
        {
            // Log the error
            EventLog::Warn(
                ASPNETCORE_EVENT_CONFIG_ERROR,
                ASPNETCORE_EVENT_INVALID_STDOUT_LOG_FILE_MSG,
                m_struFullLogFile.IsEmpty()? m_struLogFile.QueryStr() : m_struFullLogFile.QueryStr(),
                hr);
        }
        // The log file was not created yet in case of failure. No need to clean it
        m_struFullLogFile.Reset();
    }
    return hr;
}


void
SERVER_PROCESS::ReadStdErrHandle(
    LPVOID pContext
)
{
    auto pLoggingProvider = static_cast<SERVER_PROCESS*>(pContext);
    DBG_ASSERT(pLoggingProvider != NULL);
    pLoggingProvider->ReadStdErrHandleInternal();
}

void
SERVER_PROCESS::ReadStdErrHandleInternal()
{
    const size_t bufferSize = 4096;
    size_t charactersLeft = 30000;
    std::string tempBuffer;

    tempBuffer.resize(bufferSize);

    DWORD dwNumBytesRead = 0;
    while (charactersLeft > 0)
    {
        if (ReadFile(m_hStdoutHandle,
            tempBuffer.data(),
            bufferSize,
            &dwNumBytesRead,
            nullptr))
        {
            auto text = to_wide_string(tempBuffer, dwNumBytesRead, GetConsoleOutputCP());
            auto const writeSize = min(charactersLeft, text.size());
            m_output.write(text.c_str(), writeSize);
            charactersLeft -= writeSize;
        }
        else
        {
            return;
        }
    }

    // Continue reading from console out until the program ends or the handle is invalid.
    // Otherwise, the program may hang as nothing is reading stdout.
    while (ReadFile(m_hStdoutHandle,
        tempBuffer.data(),
        bufferSize,
        &dwNumBytesRead,
        nullptr))
    {
    }
}

HRESULT
SERVER_PROCESS::CheckIfServerIsUp(
    _In_  DWORD       dwPort,
    _Out_ DWORD     * pdwProcessId,
    _Out_ BOOL      * pfReady
)
{
    HRESULT                 hr = S_OK;
    DWORD                   dwResult = ERROR_INSUFFICIENT_BUFFER;
    MIB_TCPTABLE_OWNER_PID *pTCPInfo = NULL;
    MIB_TCPROW_OWNER_PID   *pOwner = NULL;
    DWORD                   dwSize = 1000; // Initial size for pTCPInfo buffer
    int                     iResult = 0;
    SOCKADDR_IN             sockAddr;
    SOCKET                  socketCheck = INVALID_SOCKET;

    DBG_ASSERT(pfReady);
    DBG_ASSERT(pdwProcessId);

    *pfReady = FALSE;
    //
    // it's OK for us to return processID 0 in case we cannot detect the real one
    //
    *pdwProcessId = 0;

    if (!g_fNsiApiNotSupported)
    {
        while (dwResult == ERROR_INSUFFICIENT_BUFFER)
        {
            // Increase the buffer size with additional space, MIB_TCPROW 20 bytes
            // New entries may be added by other processes before calling GetExtendedTcpTable
            dwSize += 200;

            if (pTCPInfo != NULL)
            {
                HeapFree(GetProcessHeap(), 0, pTCPInfo);
            }

            pTCPInfo = (MIB_TCPTABLE_OWNER_PID*)HeapAlloc(GetProcessHeap(), 0, dwSize);
            if (pTCPInfo == NULL)
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

            if (dwResult != NO_ERROR && dwResult != ERROR_INSUFFICIENT_BUFFER)
            {
                hr = HRESULT_FROM_WIN32(dwResult);
                goto Finished;
            }
        }

        // iterate pTcpInfo struct to find PID/PORT entry
        for (DWORD dwLoop = 0; dwLoop < pTCPInfo->dwNumEntries; dwLoop++)
        {
            pOwner = &pTCPInfo->table[dwLoop];
            if (ntohs((USHORT)pOwner->dwLocalPort) == dwPort)
            {
                *pdwProcessId = pOwner->dwOwningPid;
                *pfReady = TRUE;
                break;
            }
        }
    }
    else
    {
        //
        // We have to open socket to ping the service
        //
        socketCheck = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

        if (socketCheck == INVALID_SOCKET)
        {
            hr = HRESULT_FROM_WIN32(WSAGetLastError());
            goto Finished;
        }

        sockAddr.sin_family = AF_INET;
        if (!inet_pton(AF_INET, LOCALHOST, &(sockAddr.sin_addr)))
        {
            hr = HRESULT_FROM_WIN32(WSAGetLastError());
            goto Finished;
        }

        //sockAddr.sin_addr.s_addr = inet_addr( LOCALHOST );
        sockAddr.sin_port = htons((u_short)dwPort);

        //
        // Connect to server.
        // if connection fails, socket is not closed, we reuse the same socket
        // while retrying
        //
        iResult = connect(socketCheck, (SOCKADDR *)&sockAddr, sizeof(sockAddr));
        if (iResult == SOCKET_ERROR)
        {
            hr = HRESULT_FROM_WIN32(WSAGetLastError());
            if (hr == HRESULT_FROM_WIN32(WSAECONNREFUSED))
            {
                // WSAECONNREFUSED means no application listen on the given port.
                // This is not a failure. Reset the hresult to S_OK and return fReady to false
                hr = S_OK;
            }
            goto Finished;
        }
        *pfReady = TRUE;
    }

Finished:

    if (socketCheck != INVALID_SOCKET)
    {
        iResult = closesocket(socketCheck);
        if (iResult == SOCKET_ERROR)
        {
            hr = HRESULT_FROM_WIN32(WSAGetLastError());
        }
        socketCheck = INVALID_SOCKET;
    }

    if (pTCPInfo != NULL)
    {
        HeapFree(GetProcessHeap(), 0, pTCPInfo);
        pTCPInfo = NULL;
    }

    return hr;
}

// send signal to the process to let it gracefully shutdown
// if the process cannot shutdown within given time, terminate it
VOID
SERVER_PROCESS::SendSignal(
    VOID
)
{
    HRESULT hr      = S_OK;
    HANDLE  hThread = NULL;

    ReferenceServerProcess();

    m_hShutdownHandle = OpenProcess(SYNCHRONIZE | PROCESS_TERMINATE, FALSE, m_dwProcessId);

    if (m_hShutdownHandle == NULL)
    {
        // since we cannot open the process. let's terminate the process
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    hThread = CreateThread(
        NULL,       // default security attributes
        0,          // default stack size
        (LPTHREAD_START_ROUTINE)SendShutDownSignal,
        this,       // thread function arguments
        0,          // default creation flags
        NULL);      // receive thread identifier

    if (hThread == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    //
    // Reset the shutdown timeout if debugger is attached.
    // Do it only for the case that debugger is attached during process creation
    // as IsDebuggerIsAttached call is too heavy
    //
    if (WaitForSingleObject(m_hShutdownHandle, m_fDebuggerAttached ? INFINITE : m_dwShutdownTimeLimitInMS) != WAIT_OBJECT_0)
    {
        hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        goto Finished;
    }
    // thread should already exit
    CloseHandle(hThread);
    hThread = NULL;

Finished:
    if (hThread != NULL)
    {
        // if the send shutdown message thread is still running, terminate it
        DWORD dwThreadStatus = 0;
        if (GetExitCodeThread(hThread, &dwThreadStatus)!= 0 && dwThreadStatus == STILL_ACTIVE)
        {
            TerminateThread(hThread, STATUS_CONTROL_C_EXIT);
        }
        CloseHandle(hThread);
        hThread = NULL;
    }

    if (FAILED_LOG(hr))
    {
        TerminateBackendProcess();
    }

    if (m_hShutdownHandle != NULL && m_hShutdownHandle != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hShutdownHandle);
        m_hShutdownHandle = NULL;
    }

    DereferenceServerProcess();
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

    for (INT i=0; i<MAX_ACTIVE_CHILD_PROCESSES; ++i)
    {
        if (m_hChildProcessHandles[i] != NULL)
        {
            if (m_hChildProcessHandles[i] != INVALID_HANDLE_VALUE)
            {
                TerminateProcess(m_hChildProcessHandles[i], 0);
                CloseHandle(m_hChildProcessHandles[i]);
            }
            m_hChildProcessHandles[i] = NULL;
            m_dwChildProcessIds[i] = 0;
        }
    }

    if (m_hProcessHandle != NULL)
    {
        if (m_hProcessHandle != INVALID_HANDLE_VALUE)
        {
            TerminateProcess(m_hProcessHandle, 0);
            CloseHandle(m_hProcessHandle);
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

        if (processList != NULL)
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
        if (processList == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        RtlZeroMemory(processList, cbNumBytes);

        if (!QueryInformationJobObject(
                m_hJobObject,
                JobObjectBasicProcessIdList,
                processList,
                cbNumBytes,
                NULL))
        {
            dwError = GetLastError();
            if (dwError != ERROR_MORE_DATA)
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }

    } while (dwRetries++ < 5 &&
             processList != NULL &&
             (processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList ||
              processList->NumberOfProcessIdsInList == 0));

    if (dwError == ERROR_MORE_DATA)
    {
        hr = E_OUTOFMEMORY;
        // some error
        goto Finished;
    }

    if (processList == NULL ||
        (processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList ||
        processList->NumberOfProcessIdsInList == 0))
    {
        hr = HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED);
        // some error
        goto Finished;
    }

    if (processList->NumberOfProcessIdsInList > MAX_ACTIVE_CHILD_PROCESSES)
    {
        hr = HRESULT_FROM_WIN32(ERROR_CREATE_FAILED);
        goto Finished;
    }

    for (DWORD i=0; i<processList->NumberOfProcessIdsInList; i++)
    {
        dwPid = (DWORD)processList->ProcessIdList[i];
        if (dwPid != dwWorkerProcessPid)
        {
            HANDLE hProcess = OpenProcess(
                    PROCESS_QUERY_INFORMATION | SYNCHRONIZE | PROCESS_TERMINATE | PROCESS_DUP_HANDLE,
                    FALSE,
                    dwPid);

            BOOL returnValue = CheckRemoteDebuggerPresent(hProcess, &fDebuggerPresent);
            if (hProcess != NULL)
            {
                CloseHandle(hProcess);
                hProcess = NULL;
            }

            if (!returnValue)
            {
                goto Finished;
            }

            if (fDebuggerPresent)
            {
                break;
            }
        }
    }

Finished:

    if (processList != NULL)
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

        if (processList != NULL)
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
        if (processList == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        RtlZeroMemory(processList, cbNumBytes);

        if (!QueryInformationJobObject(
                m_hJobObject,
                JobObjectBasicProcessIdList,
                processList,
                cbNumBytes,
                NULL))
        {
            dwError = GetLastError();
            if (dwError != ERROR_MORE_DATA)
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }

    } while (dwRetries++ < 5 &&
             processList != NULL &&
             (processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0));

    if (dwError == ERROR_MORE_DATA)
    {
        hr = E_OUTOFMEMORY;
        // some error
        goto Finished;
    }

    if (processList == NULL || (processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0))
    {
        hr = HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED);
        // some error
        goto Finished;
    }

    if (processList->NumberOfProcessIdsInList > MAX_ACTIVE_CHILD_PROCESSES)
    {
        hr = HRESULT_FROM_WIN32(ERROR_CREATE_FAILED);
        goto Finished;
    }

    for (DWORD i=0; i<processList->NumberOfProcessIdsInList; i++)
    {
        dwPid = (DWORD)processList->ProcessIdList[i];
        if (dwPid != m_dwProcessId &&
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

    if (processList != NULL)
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
        if (processList != NULL)
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
        if (processList == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        RtlZeroMemory(processList, cbNumBytes);

        if (!QueryInformationJobObject(
                m_hJobObject,
                JobObjectBasicProcessIdList,
                processList,
                cbNumBytes,
                NULL))
        {
            DWORD dwError = GetLastError();
            if (dwError != ERROR_MORE_DATA)
            {
                hr = HRESULT_FROM_WIN32(dwError);
                goto Finished;
            }
        }

    } while (dwRetries++ < 5 &&
             processList != NULL &&
             (processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0));

    if (processList == NULL || (processList->NumberOfAssignedProcesses > processList->NumberOfProcessIdsInList || processList->NumberOfProcessIdsInList == 0))
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
        // some error
        goto Finished;
    }

    for (DWORD i=0; i<processList->NumberOfProcessIdsInList; i++)
    {
        if (dwWorkerProcessPid != (DWORD)processList->ProcessIdList[i])
        {
            hProcess = OpenProcess(PROCESS_TERMINATE,
                                   FALSE,
                                   (DWORD)processList->ProcessIdList[i]);
            if (hProcess != NULL)
            {
                if (!TerminateProcess(hProcess, 1))
                {
                    hr = HRESULT_FROM_WIN32(GetLastError());
                }
                else
                {
                    WaitForSingleObject(hProcess, INFINITE);
                }

                if (hProcess != NULL)
                {
                    CloseHandle(hProcess);
                    hProcess = NULL;
                }
            }
        }
    }

Finished:

    if (processList != NULL)
    {
        HeapFree(GetProcessHeap(), 0, processList);
    }

    return hr;
}

SERVER_PROCESS::SERVER_PROCESS() :
    m_cRefs(1),
    m_hProcessHandle(NULL),
    m_hProcessWaitHandle(NULL),
    m_dwProcessId(0),
    m_cChildProcess(0),
    m_fReady(FALSE),
    m_lStopping(0L),
    m_hStdoutHandle(NULL),
    m_fStdoutLogEnabled(FALSE),
    m_hJobObject(NULL),
    m_pForwarderConnection(NULL),
    m_dwListeningProcessId(0),
    m_hListeningProcessHandle(NULL),
    m_hShutdownHandle(NULL),
    m_hStdErrWritePipe(NULL),
    m_hReadThread(nullptr),
    m_randomGenerator(std::random_device()())
{
    //InterlockedIncrement(&g_dwActiveServerProcesses);

    for (INT i=0; i<MAX_ACTIVE_CHILD_PROCESSES; ++i)
    {
        m_dwChildProcessIds[i] = 0;
        m_hChildProcessHandles[i] = NULL;
        m_hChildProcessWaitHandles[i] = NULL;
    }
}

VOID
SERVER_PROCESS::CleanUp()
{
    if (m_hProcessWaitHandle != NULL)
    {
        UnregisterWait(m_hProcessWaitHandle);
        m_hProcessWaitHandle = NULL;
    }

    for (INT i = 0; i<MAX_ACTIVE_CHILD_PROCESSES; ++i)
    {
        if (m_hChildProcessWaitHandles[i] != NULL)
        {
            UnregisterWait(m_hChildProcessWaitHandles[i]);
            m_hChildProcessWaitHandles[i] = NULL;
        }
    }

    if (m_hProcessHandle != NULL)
    {
        if (m_hProcessHandle != INVALID_HANDLE_VALUE)
        {
            TerminateProcess(m_hProcessHandle, 1);
            CloseHandle(m_hProcessHandle);
        }
        m_hProcessHandle = NULL;
    }

    if (m_hListeningProcessHandle != NULL)
    {
        if (m_hListeningProcessHandle != INVALID_HANDLE_VALUE)
        {
            CloseHandle(m_hListeningProcessHandle);
        }
        m_hListeningProcessHandle = NULL;
    }

    for (INT i = 0; i<MAX_ACTIVE_CHILD_PROCESSES; ++i)
    {
        if (m_hChildProcessHandles[i] != NULL)
        {
            if (m_hChildProcessHandles[i] != INVALID_HANDLE_VALUE)
            {
                TerminateProcess(m_hChildProcessHandles[i], 1);
                CloseHandle(m_hChildProcessHandles[i]);
            }
            m_hChildProcessHandles[i] = NULL;
            m_dwChildProcessIds[i] = 0;
        }
    }

    if (m_hJobObject != NULL)
    {
        if (m_hJobObject != INVALID_HANDLE_VALUE)
        {
            CloseHandle(m_hJobObject);
        }
        m_hJobObject = NULL;
    }

    if (m_pForwarderConnection != NULL)
    {
        m_pForwarderConnection->DereferenceForwarderConnection();
        m_pForwarderConnection = NULL;
    }

}

SERVER_PROCESS::~SERVER_PROCESS()
{
    DWORD    dwThreadStatus = 0;

    CleanUp();

    // no need to free m_pEnvironmentVarTable, as it references to
    // the same hash table hold by configuration.
    // the hashtable memory will be freed once onfiguration got recycled

    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->DereferenceProcessManager();
        m_pProcessManager = NULL;
    }

    if (m_hStdErrWritePipe != NULL)
    {
        if (m_hStdErrWritePipe != INVALID_HANDLE_VALUE)
        {
            FlushFileBuffers(m_hStdErrWritePipe);
            CloseHandle(m_hStdErrWritePipe);
        }

        m_hStdErrWritePipe = NULL;
    }

    // Forces ReadFile to cancel, causing the read loop to complete.
    // Don't check return value as IO may or may not be completed already.
    if (m_hReadThread != nullptr)
    {
        LOG_INFO(L"Canceling standard stream pipe reader.");
        CancelSynchronousIo(m_hReadThread);
    }

    // GetExitCodeThread returns 0 on failure; thread status code is invalid.
    if (m_hReadThread != nullptr &&
        !LOG_LAST_ERROR_IF(!GetExitCodeThread(m_hReadThread, &dwThreadStatus)) &&
        dwThreadStatus == STILL_ACTIVE)
    {
        // Wait for graceful shutdown, i.e., the exit of the background thread or timeout
        if (WaitForSingleObject(m_hReadThread, PIPE_OUTPUT_THREAD_TIMEOUT) != WAIT_OBJECT_0)
        {
            // If the thread is still running, we need kill it first before exit to avoid AV
            if (!LOG_LAST_ERROR_IF(GetExitCodeThread(m_hReadThread, &dwThreadStatus) == 0) &&
                dwThreadStatus == STILL_ACTIVE)
            {
                LOG_WARN(L"Thread reading stdout/err hit timeout, forcibly closing thread.");
                TerminateThread(m_hReadThread, STATUS_CONTROL_C_EXIT);
            }
        }
    }

    if (m_hReadThread != nullptr)
    {
        CloseHandle(m_hReadThread);
        m_hReadThread = nullptr;
    }

    if (m_hStdoutHandle != NULL)
    {
        if (m_hStdoutHandle != INVALID_HANDLE_VALUE)
        {
            CloseHandle(m_hStdoutHandle);
        }
        m_hStdoutHandle = NULL;
    }

    if (m_fStdoutLogEnabled)
    {
        m_Timer.CancelTimer();
    }

    if (!m_fStdoutLogEnabled && !m_struFullLogFile.IsEmpty())
    {
        WIN32_FIND_DATA fileData;
        HANDLE handle = FindFirstFile(m_struFullLogFile.QueryStr(), &fileData);
        if (handle != INVALID_HANDLE_VALUE &&
            fileData.nFileSizeHigh == 0 &&
            fileData.nFileSizeLow == 0)
        {
            FindClose(handle);
            // no need to check whether the deletion succeeds
            // as nothing can be done
            DeleteFile(m_struFullLogFile.QueryStr());
        }
    }
}

//static
VOID
CALLBACK
SERVER_PROCESS::ProcessHandleCallback(
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

    _ASSERT(phWaitHandle != NULL && *phWaitHandle == NULL);

    *phWaitHandle = NULL;

    // wait thread will dereference.
    ReferenceServerProcess();

    status = RegisterWaitForSingleObject(
                phWaitHandle,
                hProcessToWaitOn,
                (WAITORTIMERCALLBACKFUNC)&ProcessHandleCallback,
                this,
                INFINITE,
                WT_EXECUTEONLYONCE | WT_EXECUTEINWAITTHREAD
                );

    if (status < 0)
    {
        hr = HRESULT_FROM_NT(status);
        goto Finished;
    }

Finished:

    if (FAILED_LOG(hr))
    {
        *phWaitHandle = NULL;
        DereferenceServerProcess();
    }

    return hr;
}

VOID
SERVER_PROCESS::HandleProcessExit( VOID )
{
    BOOL        fReady = FALSE;
    DWORD       dwProcessId = 0;

    if (InterlockedCompareExchange(&m_lStopping, 1L, 0L) == 0L)
    {
        CheckIfServerIsUp(m_dwPort, &dwProcessId, &fReady);

        if (!fReady)
        {
            EventLog::Info(
                ASPNETCORE_EVENT_PROCESS_SHUTDOWN,
                ASPNETCORE_EVENT_PROCESS_SHUTDOWN_MSG,
                m_struAppFullPath.QueryStr(),
                m_struPhysicalPath.QueryStr(),
                m_dwProcessId,
                m_dwPort);

            m_pProcessManager->ShutdownProcess(this);
        }

        DereferenceServerProcess();
    }
}

HRESULT
SERVER_PROCESS::SendShutdownHttpMessage( VOID )
{
    HRESULT    hr = S_OK;
    HINTERNET  hSession = NULL;
    HINTERNET  hConnect = NULL;
    HINTERNET  hRequest = NULL;

    STACK_STRU(strHeaders, 256);
    STRU       strAppToken;
    STRU       strUrl;
    DWORD      dwStatusCode = 0;
    DWORD      dwSize = sizeof(dwStatusCode);

    hSession = WinHttpOpen(L"",
        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
        WINHTTP_NO_PROXY_NAME,
        WINHTTP_NO_PROXY_BYPASS,
        0);

    if (hSession == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    hConnect = WinHttpConnect(hSession,
        L"127.0.0.1",
        (USHORT)m_dwPort,
        0);

    if (hConnect == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }
    if (m_struAppVirtualPath.QueryCCH() > 1)
    {
        // app path size is 1 means site root, i.e., "/"
        // we don't want to add duplicated '/' to the request url
        // otherwise the request will fail
        strUrl.Copy(m_struAppVirtualPath);
    }
    strUrl.Append(L"/iisintegration");

    hRequest = WinHttpOpenRequest(hConnect,
        L"POST",
        strUrl.QueryStr(),
        NULL,
        WINHTTP_NO_REFERER,
        NULL,
        0);

    if (hRequest == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    // set timeout
    if (!WinHttpSetTimeouts(hRequest,
        m_dwShutdownTimeLimitInMS,  // dwResolveTimeout
        m_dwShutdownTimeLimitInMS,  // dwConnectTimeout
        m_dwShutdownTimeLimitInMS,  // dwSendTimeout
        m_dwShutdownTimeLimitInMS)) // dwReceiveTimeout
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    // set up the shutdown headers
    if (FAILED_LOG(hr = strHeaders.Append(L"MS-ASPNETCORE-EVENT:shutdown \r\n")) ||
        FAILED_LOG(hr = strAppToken.Append(L"MS-ASPNETCORE-TOKEN:")) ||
        FAILED_LOG(hr = strAppToken.AppendA(m_straGuid.QueryStr())) ||
        FAILED_LOG(hr = strHeaders.Append(strAppToken.QueryStr())))
    {
        goto Finished;
    }

    if (!WinHttpSendRequest(hRequest,
        strHeaders.QueryStr(),  // pwszHeaders
        strHeaders.QueryCCH(),  // dwHeadersLength
        WINHTTP_NO_REQUEST_DATA,
        0,   // dwOptionalLength
        0,   // dwTotalLength
        0))  // dwContext
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (!WinHttpReceiveResponse(hRequest , NULL))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (!WinHttpQueryHeaders(hRequest,
        WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
        WINHTTP_HEADER_NAME_BY_INDEX,
        &dwStatusCode,
        &dwSize,
        WINHTTP_NO_HEADER_INDEX))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (dwStatusCode != 202)
    {
        // not expected http status
        hr = E_FAIL;
    }

    // log
    EventLog::Info(
        ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST,
        ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST_MSG,
        m_dwProcessId,
        dwStatusCode);

Finished:
    if (hRequest)
    {
        WinHttpCloseHandle(hRequest);
        hRequest = NULL;
    }
    if (hConnect)
    {
        WinHttpCloseHandle(hConnect);
        hConnect = NULL;
    }
    if (hSession)
    {
        WinHttpCloseHandle(hSession);
        hSession = NULL;
    }
    return hr;
}

//static
VOID
SERVER_PROCESS::SendShutDownSignal(
    LPVOID lpParam
)
{
    SERVER_PROCESS* pThis = static_cast<SERVER_PROCESS *>(lpParam);
    DBG_ASSERT(pThis);
    pThis->SendShutDownSignalInternal();
}

//
// send shutdown message first, if fail then send
// ctrl-c to the backend process to let it gracefully shutdown
//
VOID
SERVER_PROCESS::SendShutDownSignalInternal(
    VOID
)
{
    ReferenceServerProcess();

    if (FAILED_LOG(SendShutdownHttpMessage()))
    {
        //
        // failed to send shutdown http message
        // try send ctrl signal
        //
        HWND  hCurrentConsole = NULL;
        BOOL  fFreeConsole = FALSE;
        hCurrentConsole = GetConsoleWindow();
        if (hCurrentConsole)
        {
            // free current console first, as we may have one, e.g., hostedwebcore case
            fFreeConsole = FreeConsole();
        }

        if (AttachConsole(m_dwProcessId))
        {
            // As we called CreateProcess with CREATE_NEW_PROCESS_GROUP
            // call ctrl-break instead of ctrl-c as child process ignores ctrl-c
            if (!GenerateConsoleCtrlEvent(CTRL_BREAK_EVENT, m_dwProcessId))
            {
                // failed to send the ctrl signal. terminate the backend process immediately instead of waiting for timeout
                TerminateBackendProcess();
            }
            FreeConsole();

            if (fFreeConsole)
            {
                // IISExpress and hostedwebcore w3wp run as background process
                // have to attach console back to ensure post app_offline scenario still works
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
        }
        else
        {
            // terminate the backend process immediately instead of waiting for timeout
            TerminateBackendProcess();
        }
    }

    DereferenceServerProcess();
}

VOID
SERVER_PROCESS::TerminateBackendProcess(
    VOID
)
{
    if (InterlockedCompareExchange(&m_lStopping, 1L, 0L) == 0L)
    {
        // backend process will be terminated, remove the waitcallback
        if (m_hProcessWaitHandle != NULL)
        {
            UnregisterWait(m_hProcessWaitHandle);

            // as we skipped process exit callback (ProcessHandleCallback),
            // need to dereference the object otherwise memory leak
            DereferenceServerProcess();

            m_hProcessWaitHandle = NULL;
        }

        // cannot gracefully shutdown or timeout, terminate the process
        if (m_hProcessHandle != NULL && m_hProcessHandle != INVALID_HANDLE_VALUE)
        {
            TerminateProcess(m_hProcessHandle, 0);
            m_hProcessHandle = NULL;
        }

        // log a warning for ungraceful shutdown
        EventLog::Warn(
            ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE,
            ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE_MSG,
            m_dwProcessId);
    }
}
