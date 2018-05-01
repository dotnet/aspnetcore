#include "..\precomp.hxx"

IN_PROCESS_APPLICATION*  IN_PROCESS_APPLICATION::s_Application = NULL;

IN_PROCESS_APPLICATION::IN_PROCESS_APPLICATION(
    IHttpServer*        pHttpServer,
    ASPNETCORE_CONFIG*  pConfig) :
    m_pHttpServer(pHttpServer),
    m_ProcessExitCode(0),
    m_hLogFileHandle(INVALID_HANDLE_VALUE),
    m_hErrReadPipe(INVALID_HANDLE_VALUE),
    m_hErrWritePipe(INVALID_HANDLE_VALUE),
    m_dwStdErrReadTotal(0),
    m_fDoneStdRedirect(FALSE),
    m_fBlockCallbacksIntoManaged(FALSE),
    m_fInitialized(FALSE),
    m_fShutdownCalledFromNative(FALSE),
    m_fShutdownCalledFromManaged(FALSE),
    m_srwLock(),
    m_pConfig(pConfig)
{
    // is it guaranteed that we have already checked app offline at this point?
    // If so, I don't think there is much to do here.
    DBG_ASSERT(pHttpServer != NULL);
    DBG_ASSERT(pConfig != NULL);
    InitializeSRWLock(&m_srwLock);

    // TODO we can probably initialized as I believe we are the only ones calling recycle.
    m_status = APPLICATION_STATUS::STARTING;
}

IN_PROCESS_APPLICATION::~IN_PROCESS_APPLICATION()
{
    if (m_hLogFileHandle != INVALID_HANDLE_VALUE)
    {
        m_Timer.CancelTimer();
        CloseHandle(m_hLogFileHandle);
        m_hLogFileHandle = INVALID_HANDLE_VALUE;
    }

    m_hThread = NULL;
    s_Application = NULL;
}

//static
DWORD
IN_PROCESS_APPLICATION::DoShutDown(
    LPVOID lpParam
)
{
    IN_PROCESS_APPLICATION* pApplication = static_cast<IN_PROCESS_APPLICATION*>(lpParam);
    DBG_ASSERT(pApplication);
    pApplication->ShutDownInternal();
    return 0;
}

__override
VOID
IN_PROCESS_APPLICATION::ShutDown(
    VOID
)
{
    HRESULT hr = S_OK;
    CHandle  hThread;
    DWORD    dwThreadStatus = 0;
    DWORD    dwTimeout = m_pConfig->QueryShutdownTimeLimitInMS();

    if (IsDebuggerPresent())
    {
        dwTimeout = INFINITE;
    }

    hThread.Attach(CreateThread(
        NULL,       // default security attributes
        0,          // default stack size
        (LPTHREAD_START_ROUTINE)DoShutDown,
        this,       // thread function arguments
        0,          // default creation flags
        NULL));      // receive thread identifier

    if ((HANDLE)hThread == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (WaitForSingleObject(hThread, dwTimeout) != WAIT_OBJECT_0)
    {
        // if the thread is still running, we need kill it first before exit to avoid AV
        if (GetExitCodeThread(m_hThread, &dwThreadStatus) != 0 && dwThreadStatus == STILL_ACTIVE)
        {
            // Calling back into managed at this point is prone to have AVs
            // Calling terminate thread here may be our best solution.
            TerminateThread(hThread, STATUS_CONTROL_C_EXIT);
            hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        }
    }

Finished:

    if (FAILED(hr))
    {
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_WARNING_TYPE,
            ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE,
            ASPNETCORE_EVENT_APP_SHUTDOWN_FAILURE_MSG,
            m_pConfig->QueryConfigPath()->QueryStr());

        //
        // Managed layer may block the shutdown and lead to shutdown timeout
        // Assumption: only one inprocess application is hosted.
        // Call process exit to force shutdown
        //
        exit(hr);
    }
}


VOID
IN_PROCESS_APPLICATION::ShutDownInternal()
{
    DWORD    dwThreadStatus = 0;
    DWORD    dwTimeout = m_pConfig->QueryShutdownTimeLimitInMS();
    HANDLE   handle = NULL;
    WIN32_FIND_DATA fileData;

    if (IsDebuggerPresent())
    {
        dwTimeout = INFINITE;
    }

    if (m_fShutdownCalledFromNative ||
        m_status == APPLICATION_STATUS::STARTING ||
        m_status == APPLICATION_STATUS::FAIL)
    {
        return;
    }

    {
        SRWLockWrapper lock(m_srwLock);

        if (m_fShutdownCalledFromNative ||
            m_status == APPLICATION_STATUS::STARTING ||
            m_status == APPLICATION_STATUS::FAIL)
        {
            return;
        }

        // We need to keep track of when both managed and native initiate shutdown
        // to avoid AVs. If shutdown has already been initiated in managed, we don't want to call into
        // managed. We still need to wait on main exiting no matter what. m_fShutdownCalledFromNative
        // is used for detecting redundant calls and blocking more requests to OnExecuteRequestHandler.
        m_fShutdownCalledFromNative = TRUE;
        m_status = APPLICATION_STATUS::SHUTDOWN;

        if (!m_fShutdownCalledFromManaged)
        {
            // We cannot call into managed if the dll is detaching from the process.
            // Calling into managed code when the dll is detaching is strictly a bad idea,
            // and usually results in an AV saying "The string binding is invalid"
            if (!g_fProcessDetach)
            {
                m_ShutdownHandler(m_ShutdownHandlerContext);
                m_ShutdownHandler = NULL;
            }
        }

        // Release the lock before we wait on the thread to exit.
    }

    if (!m_fShutdownCalledFromManaged)
    {
        if (m_hThread != NULL &&
            GetExitCodeThread(m_hThread, &dwThreadStatus) != 0 &&
            dwThreadStatus == STILL_ACTIVE)
        {
            // wait for graceful shutdown, i.e., the exit of the background thread or timeout
            if (WaitForSingleObject(m_hThread, dwTimeout) != WAIT_OBJECT_0)
            {
                // if the thread is still running, we need kill it first before exit to avoid AV
                if (GetExitCodeThread(m_hThread, &dwThreadStatus) != 0 && dwThreadStatus == STILL_ACTIVE)
                {
                    // Calling back into managed at this point is prone to have AVs
                    // Calling terminate thread here may be our best solution.
                    TerminateThread(m_hThread, STATUS_CONTROL_C_EXIT);
                }
            }
        }
    }

    CloseHandle(m_hThread);
    m_hThread = NULL;
    s_Application = NULL;

    CloseStdErrHandles();

    if (m_pStdFile != NULL)
    {
        fflush(stdout);
        fflush(stderr);
        fclose(m_pStdFile);
    }

    if (m_hLogFileHandle != INVALID_HANDLE_VALUE)
    {
        m_Timer.CancelTimer();
        CloseHandle(m_hLogFileHandle);
        m_hLogFileHandle = INVALID_HANDLE_VALUE;
    }

    // delete empty log file
    handle = FindFirstFile(m_struLogFilePath.QueryStr(), &fileData);
    if (handle != INVALID_HANDLE_VALUE &&
        fileData.nFileSizeHigh == 0 &&
        fileData.nFileSizeLow == 0) // skip check of nFileSizeHigh
    {
        FindClose(handle);
        // no need to check whether the deletion succeeds
        // as nothing can be done
        DeleteFile(m_struLogFilePath.QueryStr());
    }
}

__override
VOID
IN_PROCESS_APPLICATION::Recycle(
    VOID
)
{
    // We need to guarantee that recycle is only called once, as calling pHttpServer->RecycleProcess
    // multiple times can lead to AVs.
    if (m_fRecycleCalled)
    {
        return;
    }

    {
        SRWLockWrapper lock(m_srwLock);

        if (m_fRecycleCalled)
        {
            return;
        }

        m_fRecycleCalled = true;
    }

    if (!m_pHttpServer->IsCommandLineLaunch())
    {
        // IIS scenario.
        // notify IIS first so that new request will be routed to new worker process
        m_pHttpServer->RecycleProcess(L"AspNetCore InProcess Recycle Process on Demand");
    }
    else
    {
        // IISExpress scenario
        // Shutdown the managed application and call exit to terminate current process
        ShutDown();
        exit(0);
    }
}

REQUEST_NOTIFICATION_STATUS
IN_PROCESS_APPLICATION::OnAsyncCompletion(
    DWORD           cbCompletion,
    HRESULT         hrCompletionStatus,
    IN_PROCESS_HANDLER* pInProcessHandler
)
{
    REQUEST_NOTIFICATION_STATUS dwRequestNotificationStatus = RQ_NOTIFICATION_CONTINUE;

    ReferenceApplication();

    if (pInProcessHandler->QueryIsManagedRequestComplete())
    {
        // means PostCompletion has been called and this is the associated callback.
        dwRequestNotificationStatus = pInProcessHandler->QueryAsyncCompletionStatus();
    }
    else if (m_fBlockCallbacksIntoManaged)
    {
        // this can potentially happen in ungraceful shutdown.
        // Or something really wrong happening with async completions
        // At this point, managed is in a shutting down state and we cannot send a request to it.
        pInProcessHandler->QueryHttpContext()->GetResponse()->SetStatus(503,
            "Server has been shutdown",
            0,
            (ULONG)HRESULT_FROM_WIN32(ERROR_SHUTDOWN_IN_PROGRESS));
        dwRequestNotificationStatus = RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else
    {
        // Call the managed handler for async completion.
        dwRequestNotificationStatus = m_AsyncCompletionHandler(pInProcessHandler->QueryManagedHttpContext(), hrCompletionStatus, cbCompletion);
    }

    DereferenceApplication();

    return dwRequestNotificationStatus;
}

REQUEST_NOTIFICATION_STATUS
IN_PROCESS_APPLICATION::OnExecuteRequest(
    _In_ IHttpContext* pHttpContext,
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    REQUEST_NOTIFICATION_STATUS dwRequestNotificationStatus = RQ_NOTIFICATION_CONTINUE;
    PFN_REQUEST_HANDLER pRequestHandler = NULL;

    ReferenceApplication();
    pRequestHandler = m_RequestHandler;

    if (pRequestHandler == NULL)
    {
        //
        // return error as the application did not register callback
        //
        if (ANCMEvents::ANCM_EXECUTE_REQUEST_FAIL::IsEnabled(pHttpContext->GetTraceContext()))
        {
            ANCMEvents::ANCM_EXECUTE_REQUEST_FAIL::RaiseEvent(pHttpContext->GetTraceContext(),
                NULL,
                (ULONG)E_APPLICATION_ACTIVATION_EXEC_FAILURE);
        }

        pHttpContext->GetResponse()->SetStatus(500,
            "Internal Server Error",
            0,
            (ULONG)E_APPLICATION_ACTIVATION_EXEC_FAILURE);

        dwRequestNotificationStatus = RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else if (m_status != APPLICATION_STATUS::RUNNING || m_fBlockCallbacksIntoManaged)
    {
        pHttpContext->GetResponse()->SetStatus(503,
            "Server is currently shutting down.",
            0,
            (ULONG)HRESULT_FROM_WIN32(ERROR_SHUTDOWN_IN_PROGRESS));
        dwRequestNotificationStatus = RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else
    {
        dwRequestNotificationStatus = pRequestHandler(pInProcessHandler, m_RequestHandlerContext);
    }

    DereferenceApplication();

    return dwRequestNotificationStatus;
}

VOID
IN_PROCESS_APPLICATION::SetCallbackHandles(
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ PFN_MANAGED_CONTEXT_HANDLER async_completion_handler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    m_RequestHandler = request_handler;
    m_RequestHandlerContext = pvRequstHandlerContext;
    m_ShutdownHandler = shutdown_handler;
    m_ShutdownHandlerContext = pvShutdownHandlerContext;
    m_AsyncCompletionHandler = async_completion_handler;

    CloseStdErrHandles();
    // Can't check the std err handle as it isn't a critical error
    SetStdHandle(STD_ERROR_HANDLE, INVALID_HANDLE_VALUE);
    // Initialization complete
    SetEvent(m_pInitalizeEvent);
    m_fInitialized = TRUE;
}

VOID
IN_PROCESS_APPLICATION::SetStdOut(
    VOID
)
{
    HRESULT                 hr = S_OK;
    STRU                    struPath;
    SYSTEMTIME              systemTime;
    SECURITY_ATTRIBUTES     saAttr = { 0 };
    HANDLE                  hStdErrReadPipe;
    HANDLE                  hStdErrWritePipe;

    if (!m_fDoneStdRedirect)
    {
        // Have not set stdout yet, redirect stdout to log file
        SRWLockWrapper lock(m_srwLock);
        if (!m_fDoneStdRedirect)
        {
            saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
            saAttr.bInheritHandle = TRUE;
            saAttr.lpSecurityDescriptor = NULL;

            //
            // best effort
            // no need to capture the error code as nothing we can do here
            // in case mamanged layer exits abnormally, may not be able to capture the log content as it is buffered.
            //
            if (!GetConsoleWindow())
            {
                // Full IIS scenario.

                //
                // SetStdHandle works as w3wp does not have Console
                // Current process does not have a console
                //
                if (m_pConfig->QueryStdoutLogEnabled())
                {
                    hr = UTILITY::ConvertPathToFullPath(
                        m_pConfig->QueryStdoutLogFile()->QueryStr(),
                        m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
                        &struPath);
                    if (FAILED(hr))
                    {
                        goto Finished;
                    }

                    hr = UTILITY::EnsureDirectoryPathExist(struPath.QueryStr());
                    if (FAILED(hr))
                    {
                        goto Finished;
                    }

                    GetSystemTime(&systemTime);
                    hr = m_struLogFilePath.SafeSnwprintf(L"%s_%d%02d%02d%02d%02d%02d_%d.log",
                        struPath.QueryStr(),
                        systemTime.wYear,
                        systemTime.wMonth,
                        systemTime.wDay,
                        systemTime.wHour,
                        systemTime.wMinute,
                        systemTime.wSecond,
                        GetCurrentProcessId());
                    if (FAILED(hr))
                    {
                        goto Finished;
                    }

                    m_hLogFileHandle = CreateFileW(m_struLogFilePath.QueryStr(),
                        FILE_READ_DATA | FILE_WRITE_DATA,
                        FILE_SHARE_READ,
                        &saAttr,
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);

                    if (m_hLogFileHandle == INVALID_HANDLE_VALUE)
                    {
                        hr = HRESULT_FROM_WIN32(GetLastError());
                        goto Finished;
                    }

                    if (!SetStdHandle(STD_OUTPUT_HANDLE, m_hLogFileHandle))
                    {
                        hr = HRESULT_FROM_WIN32(GetLastError());
                        goto Finished;
                    }

                    if (!SetStdHandle(STD_ERROR_HANDLE, m_hLogFileHandle))
                    {
                        hr = HRESULT_FROM_WIN32(GetLastError());
                        goto Finished;
                    }

                    // not work
                    // AllocConsole()  does not help
                    // *stdout = *m_pStdFile;
                    // *stderr = *m_pStdFile;
                    // _dup2(_fileno(m_pStdFile), _fileno(stdout));
                    // _dup2(_fileno(m_pStdFile), _fileno(stderr));
                    // this one cannot capture the process start failure
                    // _wfreopen_s(&m_pStdFile, struLogFileName.QueryStr(), L"w", stdout);

                    // Periodically flush the log content to file
                    m_Timer.InitializeTimer(STTIMER::TimerCallback, &m_struLogFilePath, 3000, 3000);
                }
                else
                {
                    //
                    // CreatePipe for outputting stderr to the windows event log.
                    // Ignore failures
                    //
                    if (!CreatePipe(&hStdErrReadPipe, &hStdErrWritePipe, &saAttr, 0 /*nSize*/))
                    {
                        goto Finished;
                    }

                    if (!SetStdHandle(STD_ERROR_HANDLE, hStdErrWritePipe))
                    {
                        goto Finished;
                    }

                    m_hErrReadPipe = hStdErrReadPipe;
                    m_hErrWritePipe = hStdErrWritePipe;

                    // Read the stderr handle on a separate thread until we get 4096 bytes.
                    m_hErrThread = CreateThread(
                        NULL,       // default security attributes
                        0,          // default stack size
                        (LPTHREAD_START_ROUTINE)ReadStdErrHandle,
                        this,       // thread function arguments
                        0,          // default creation flags
                        NULL);      // receive thread identifier

                    if (m_hErrThread == NULL)
                    {
                        hr = HRESULT_FROM_WIN32(GetLastError());
                        goto Finished;
                    }
                }
            }
            else
            {
                // The process has console, e.g., IIS Express scenario

                if (_wfopen_s(&m_pStdFile, m_struLogFilePath.QueryStr(), L"w") == 0)
                {
                    // known issue: error info may not be capture when process crashes during buffering
                    // even we disabled FILE buffering
                    setvbuf(m_pStdFile, NULL, _IONBF, 0);
                    _dup2(_fileno(m_pStdFile), _fileno(stdout));
                    _dup2(_fileno(m_pStdFile), _fileno(stderr));
                }
                // These don't work for console scenario
                // close and AllocConsole does not help
                //_wfreopen_s(&m_pStdFile, struLogFileName.QueryStr(), L"w", stdout);
                // SetStdHandle(STD_ERROR_HANDLE, m_hLogFileHandle);
                // SetStdHandle(STD_OUTPUT_HANDLE, m_hLogFileHandle);
                //*stdout = *m_pStdFile;
                //*stderr = *m_pStdFile;
            }
        }
    }

Finished:
    m_fDoneStdRedirect = TRUE;
    if (FAILED(hr) && m_pConfig->QueryStdoutLogEnabled())
    {
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_WARNING_TYPE,
            ASPNETCORE_EVENT_CONFIG_ERROR,
            ASPNETCORE_EVENT_INVALID_STDOUT_LOG_FILE_MSG,
            m_struLogFilePath.QueryStr(),
            hr);
    }
}

VOID
IN_PROCESS_APPLICATION::ReadStdErrHandle(
    LPVOID pContext
)
{
    IN_PROCESS_APPLICATION *pApplication = (IN_PROCESS_APPLICATION*)pContext;
    DBG_ASSERT(pApplication != NULL);
    pApplication->ReadStdErrHandleInternal();
}

VOID
IN_PROCESS_APPLICATION::ReadStdErrHandleInternal(
    VOID
)
{
    DWORD dwNumBytesRead = 0;
    while (true)
    {
        if (ReadFile(m_hErrReadPipe, &m_pzFileContents[m_dwStdErrReadTotal], 4096 - m_dwStdErrReadTotal, &dwNumBytesRead, NULL))
        {
            m_dwStdErrReadTotal += dwNumBytesRead;
            if (m_dwStdErrReadTotal >= 4096)
            {
                break;
            }
        }
        else if (GetLastError() == ERROR_BROKEN_PIPE)
        {
            break;
        }
    }
}

VOID
IN_PROCESS_APPLICATION::CloseStdErrHandles
(
    VOID
)
{
    DWORD    dwThreadStatus = 0;
    DWORD    dwTimeout = m_pConfig->QueryShutdownTimeLimitInMS();
    // Close Handles for stderr as we only care about capturing startup errors
    if (m_hErrWritePipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrWritePipe);
        m_hErrWritePipe = INVALID_HANDLE_VALUE;
    }

    if (m_hErrThread != NULL &&
        GetExitCodeThread(m_hErrThread, &dwThreadStatus) != 0 &&
        dwThreadStatus == STILL_ACTIVE)
    {
        // wait for gracefullshut down, i.e., the exit of the background thread or timeout
        if (WaitForSingleObject(m_hErrThread, dwTimeout) != WAIT_OBJECT_0)
        {
            // if the thread is still running, we need kill it first before exit to avoid AV
            if (GetExitCodeThread(m_hErrThread, &dwThreadStatus) != 0 && dwThreadStatus == STILL_ACTIVE)
            {
                TerminateThread(m_hErrThread, STATUS_CONTROL_C_EXIT);
            }
        }
    }

    CloseHandle(m_hErrThread);
    m_hErrThread = NULL;

    if (m_hErrReadPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hErrReadPipe);
        m_hErrReadPipe = INVALID_HANDLE_VALUE;
    }
}

// Will be called by the inprocesshandler
HRESULT
IN_PROCESS_APPLICATION::LoadManagedApplication
(
    VOID
)
{
    HRESULT    hr = S_OK;
    DWORD      dwTimeout;
    DWORD      dwResult;

    ReferenceApplication();

    if (m_status != APPLICATION_STATUS::STARTING)
    {
        // Core CLR has already been loaded.
        // Cannot load more than once even there was a failure
        if (m_status == APPLICATION_STATUS::FAIL)
        {
            hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
        }
        else if (m_status == APPLICATION_STATUS::SHUTDOWN)
        {
            hr = HRESULT_FROM_WIN32(ERROR_SHUTDOWN_IS_SCHEDULED);
        }

        goto Finished;
    }

    // Set up stdout redirect
    SetStdOut();

    {
        SRWLockWrapper lock(m_srwLock);
        if (m_status != APPLICATION_STATUS::STARTING)
        {
            if (m_status == APPLICATION_STATUS::FAIL)
            {
                hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
            }
            else if (m_status == APPLICATION_STATUS::SHUTDOWN)
            {
                hr = HRESULT_FROM_WIN32(ERROR_SHUTDOWN_IS_SCHEDULED);
            }

            goto Finished;
        }
        m_hThread = CreateThread(
            NULL,       // default security attributes
            0,          // default stack size
            (LPTHREAD_START_ROUTINE)ExecuteAspNetCoreProcess,
            this,       // thread function arguments
            0,          // default creation flags
            NULL);      // receive thread identifier

        if (m_hThread == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        m_pInitalizeEvent = CreateEvent(
            NULL,   // default security attributes
            TRUE,   // manual reset event
            FALSE,  // not set
            NULL);  // name

        if (m_pInitalizeEvent == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }

        // If the debugger is attached, never timeout
        if (IsDebuggerPresent())
        {
            dwTimeout = INFINITE;
        }
        else
        {
            dwTimeout = m_pConfig->QueryStartupTimeLimitInMS();
        }

        const HANDLE pHandles[2]{ m_hThread, m_pInitalizeEvent };

        // Wait on either the thread to complete or the event to be set
        dwResult = WaitForMultipleObjects(2, pHandles, FALSE, dwTimeout);

        // It all timed out
        if (dwResult == WAIT_TIMEOUT)
        {
            // kill the backend thread as loading dotnet timedout
            TerminateThread(m_hThread, 0);
            hr = HRESULT_FROM_WIN32(dwResult);
            goto Finished;
        }
        else if (dwResult == WAIT_FAILED)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        // The thread ended it means that something failed
        if (dwResult == WAIT_OBJECT_0)
        {
            hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
            goto Finished;
        }

        m_status = APPLICATION_STATUS::RUNNING;
    }
Finished:

    if (FAILED(hr))
    {
        m_status = APPLICATION_STATUS::FAIL;

        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_LOAD_CLR_FALIURE,
            ASPNETCORE_EVENT_LOAD_CLR_FALIURE_MSG,
            m_pConfig->QueryApplicationPath()->QueryStr(),
            m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
            hr);
    }
    DereferenceApplication();

    return hr;
}

// static
VOID
IN_PROCESS_APPLICATION::ExecuteAspNetCoreProcess(
    _In_ LPVOID pContext
)
{
    HRESULT hr = S_OK;
    IN_PROCESS_APPLICATION *pApplication = (IN_PROCESS_APPLICATION*)pContext;
    DBG_ASSERT(pApplication != NULL);
    hr = pApplication->ExecuteApplication();
    //
    // no need to log the error here as if error happened, the thread will exit
    // the error will ba catched by caller LoadManagedApplication which will log an error
    //

}

HRESULT
IN_PROCESS_APPLICATION::SetEnvironementVariablesOnWorkerProcess(
    VOID
)
{
    HRESULT hr = S_OK;
    ENVIRONMENT_VAR_HASH* pHashTable = NULL;
    if (FAILED(hr = ENVIRONMENT_VAR_HELPERS::InitEnvironmentVariablesTable(
        m_pConfig->QueryEnvironmentVariables(),
        m_pConfig->QueryWindowsAuthEnabled(),
        m_pConfig->QueryBasicAuthEnabled(),
        m_pConfig->QueryAnonymousAuthEnabled(),
        &pHashTable)))
    {
        goto Finished;
    }

    pHashTable->Apply(ENVIRONMENT_VAR_HELPERS::AppendEnvironmentVariables, &hr);
    if (FAILED(hr))
    {
        goto Finished;
    }
    pHashTable->Apply(ENVIRONMENT_VAR_HELPERS::SetEnvironmentVariables, &hr);
    if (FAILED(hr))
    {
        goto Finished;
    }
Finished:
    return hr;
}

HRESULT
IN_PROCESS_APPLICATION::ExecuteApplication(
    VOID
)
{
    HRESULT             hr = S_OK;
    HMODULE             hModule;
    hostfxr_main_fn     pProc;

    DBG_ASSERT(m_status == APPLICATION_STATUS::STARTING);

    hModule = LoadLibraryW(m_pConfig->QueryHostFxrFullPath());

    if (hModule == NULL)
    {
        // .NET Core not installed (we can log a more detailed error message here)
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Get the entry point for main
    pProc = (hostfxr_main_fn)GetProcAddress(hModule, "hostfxr_main");
    if (pProc == NULL)
    {
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    if (FAILED(hr = SetEnvironementVariablesOnWorkerProcess()))
    {
        goto Finished;
    }

    // There can only ever be a single instance of .NET Core
    // loaded in the process but we need to get config information to boot it up in the
    // first place. This is happening in an execute request handler and everyone waits
    // until this initialization is done.
    // We set a static so that managed code can call back into this instance and
    // set the callbacks
    s_Application = this;

    hr = RunDotnetApplication(m_pConfig->QueryHostFxrArgCount(), m_pConfig->QueryHostFxrArguments(), pProc);

Finished:

    //
    // this method is called by the background thread and should never exit unless shutdown
    // If main returned and shutdown was not called in managed, we want to block native from calling into
    // managed. To do this, we can say that shutdown was called from managed.
    // Don't bother locking here as there will always be a race between receiving a native shutdown
    // notification and unexpected managed exit.
    //
    m_status = APPLICATION_STATUS::SHUTDOWN;
    m_fShutdownCalledFromManaged = TRUE;
    FreeLibrary(hModule);

    if (!m_fShutdownCalledFromNative)
    {
        LogErrorsOnMainExit(hr);
        if (m_fInitialized)
        {
            //
            // If the inprocess server was initialized, we need to cause recycle to be called on the worker process.
            //
            Recycle();
        }
    }

    return hr;
}

VOID
IN_PROCESS_APPLICATION::LogErrorsOnMainExit(
    HRESULT hr
)
{
    //
    // Ungraceful shutdown, try to log an error message.
    // This will be a common place for errors as it means the hostfxr_main returned
    // or there was an exception.
    //
    CHAR            pzFileContents[4096] = { 0 };
    DWORD           dwNumBytesRead;
    STRU            struStdErrLog;
    LARGE_INTEGER   li = { 0 };
    BOOL            fLogged = FALSE;
    DWORD           dwFilePointer = 0;

    if (m_pConfig->QueryStdoutLogEnabled())
    {
        // Put stdout/stderr logs into
        if (m_hLogFileHandle != INVALID_HANDLE_VALUE)
        {
            if (GetFileSizeEx(m_hLogFileHandle, &li) && li.LowPart > 0 && li.HighPart == 0)
            {
                if (li.LowPart > 4096)
                {
                    dwFilePointer = SetFilePointer(m_hLogFileHandle, -4096, NULL, FILE_END);
                }
                else
                {
                    dwFilePointer = SetFilePointer(m_hLogFileHandle, 0, NULL, FILE_BEGIN);
                }
                if (dwFilePointer != INVALID_SET_FILE_POINTER)
                {
                    if (ReadFile(m_hLogFileHandle, pzFileContents, 4096, &dwNumBytesRead, NULL))
                    {
                        if (SUCCEEDED(struStdErrLog.CopyA(m_pzFileContents, m_dwStdErrReadTotal)))
                        {
                            UTILITY::LogEventF(g_hEventLog,
                                EVENTLOG_ERROR_TYPE,
                                ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
                                ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDOUT_MSG,
                                m_pConfig->QueryApplicationPath()->QueryStr(),
                                m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
                                hr,
                                struStdErrLog.QueryStr());
                            fLogged = TRUE;

                        }
                    }
                }
            }
        }
    }
    else
    {
        if (m_dwStdErrReadTotal > 0)
        {
            if (SUCCEEDED(struStdErrLog.CopyA(m_pzFileContents, m_dwStdErrReadTotal)))
            {
                UTILITY::LogEventF(g_hEventLog,
                    EVENTLOG_ERROR_TYPE,
                    ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
                    ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDERR_MSG,
                    m_pConfig->QueryApplicationPath()->QueryStr(),
                    m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
                    hr,
                    struStdErrLog.QueryStr());
                fLogged = TRUE;
            }
        }
    }

    if (!fLogged)
    {
        // If we didn't log, log the generic message.
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG,
            m_pConfig->QueryApplicationPath()->QueryStr(),
            m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
            hr);
        fLogged = TRUE;
    }
}

//
// Calls hostfxr_main with the hostfxr and application as arguments.
// Method should be called with only
// Need to have __try / __except in methods that require unwinding.
// Note, this will not
//
HRESULT
IN_PROCESS_APPLICATION::RunDotnetApplication(DWORD argc, CONST PCWSTR* argv, hostfxr_main_fn pProc)
{
    HRESULT hr = S_OK;
    __try
    {
        m_ProcessExitCode = pProc(argc, argv);
    }
    __except (FilterException(GetExceptionCode(), GetExceptionInformation()))
    {
        // TODO Log error message here.
        hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
    }

    return hr;
}

// static
INT
IN_PROCESS_APPLICATION::FilterException(unsigned int, struct _EXCEPTION_POINTERS*)
{
    // We assume that any exception is a failure as the applicaiton didn't start or there was a startup error.
    // TODO, log error based on exception code.
    return EXCEPTION_EXECUTE_HANDLER;
}

ASPNETCORE_CONFIG*
IN_PROCESS_APPLICATION::QueryConfig() const
{
    return m_pConfig;
}

HRESULT
IN_PROCESS_APPLICATION::CreateHandler(
    _In_  IHttpContext       *pHttpContext,
    _In_  HTTP_MODULE_ID     *pModuleId,
    _Out_ IREQUEST_HANDLER   **pRequestHandler)
{
    HRESULT hr = S_OK;
    IREQUEST_HANDLER* pHandler = NULL;
    pHandler = new IN_PROCESS_HANDLER(pHttpContext, pModuleId, this);

    if (pHandler == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }

    *pRequestHandler = pHandler;
    return hr;
}
