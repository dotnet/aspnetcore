#include "..\precomp.hxx"

typedef DWORD(*hostfxr_main_fn) (CONST DWORD argc, CONST WCHAR* argv[]);

IN_PROCESS_APPLICATION*  IN_PROCESS_APPLICATION::s_Application = NULL;

IN_PROCESS_APPLICATION::IN_PROCESS_APPLICATION(
    IHttpServer*        pHttpServer, 
    ASPNETCORE_CONFIG*  pConfig) :
    APPLICATION(pHttpServer, pConfig),
    m_ProcessExitCode(0),
    m_fManagedAppLoaded(FALSE),
    m_fLoadManagedAppError(FALSE),
    m_fInitialized(FALSE),
    m_fRecycleProcessCalled(FALSE),
    m_hLogFileHandle(INVALID_HANDLE_VALUE),
    m_fDoneStdRedirect(FALSE)
{
    // is it guaranteed that we have already checked app offline at this point?
    // If so, I don't think there is much to do here.
    DBG_ASSERT(pHttpServer != NULL);
    DBG_ASSERT(pConfig != NULL);
    InitializeSRWLock(&m_srwLock);

    // TODO we can probably initialized as I believe we are the only ones calling recycle.
    m_fInitialized = TRUE;
    m_status = APPLICATION_STATUS::RUNNING;
}

IN_PROCESS_APPLICATION::~IN_PROCESS_APPLICATION()
{
    Recycle();
}

__override
VOID
IN_PROCESS_APPLICATION::ShutDown()
{
    //todo
}

// This is the same function as before, TODO configrm if we need to change anything for configuration.
VOID
IN_PROCESS_APPLICATION::Recycle(
    VOID
)
{
    if (m_fInitialized)
    {
        DWORD    dwThreadStatus = 0;
        DWORD    dwTimeout = m_pConfig->QueryShutdownTimeLimitInMS();
        HANDLE   handle = NULL;
        WIN32_FIND_DATA fileData;

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

        // delete empty log file, if logging is not enabled
        handle = FindFirstFile(m_struLogFilePath.QueryStr(), &fileData);
        if (handle != INVALID_HANDLE_VALUE &&
                fileData.nFileSizeHigh &&
                fileData.nFileSizeLow == 0) // skip check of nFileSizeHigh
        {
            FindClose(handle);
            // no need to check whether the deletion succeeds
            // as nothing can be done
            DeleteFile(m_struLogFilePath.QueryStr());
        }

        AcquireSRWLockExclusive(&m_srwLock);

        if (!m_pHttpServer->IsCommandLineLaunch() &&
            !m_fRecycleProcessCalled &&
            (m_pHttpServer->GetAdminManager() != NULL))
        {
            // IIS scenario.
            // notify IIS first so that new request will be routed to new worker process
            m_pHttpServer->RecycleProcess(L"AspNetCore Recycle Process on Demand");
        }

        m_fRecycleProcessCalled = TRUE;

        // First call into the managed server and shutdown
        if (m_ShutdownHandler != NULL)
        {
            m_ShutdownHandler(m_ShutdownHandlerContext);
            m_ShutdownHandler = NULL;
        }

        if (m_hThread != NULL &&
            GetExitCodeThread(m_hThread, &dwThreadStatus) != 0 &&
            dwThreadStatus == STILL_ACTIVE)
        {
            // wait for gracefullshut down, i.e., the exit of the background thread or timeout
            if (WaitForSingleObject(m_hThread, dwTimeout) != WAIT_OBJECT_0)
            {
                // if the thread is still running, we need kill it first before exit to avoid AV
                if (GetExitCodeThread(m_hThread, &dwThreadStatus) != 0 && dwThreadStatus == STILL_ACTIVE)
                {
                    TerminateThread(m_hThread, STATUS_CONTROL_C_EXIT);
                }
            }
        }

        CloseHandle(m_hThread);
        m_hThread = NULL;
        s_Application = NULL;

        ReleaseSRWLockExclusive(&m_srwLock);
        if (m_pHttpServer && m_pHttpServer->IsCommandLineLaunch())
        {
            // IISExpress scenario
            // Can only call exit to terminate current process
            exit(0);
        }
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

    if (pInProcessHandler->QueryIsManagedRequestComplete())
    {
        // means PostCompletion has been called and this is the associated callback.
        dwRequestNotificationStatus = pInProcessHandler->QueryAsyncCompletionStatus();
        // TODO cleanup whatever disconnect listener there is
        return dwRequestNotificationStatus;
    }
    else
    {
        // Call the managed handler for async completion.
        return m_AsyncCompletionHandler(pInProcessHandler->QueryManagedHttpContext(), hrCompletionStatus, cbCompletion);
    }
}

REQUEST_NOTIFICATION_STATUS
IN_PROCESS_APPLICATION::OnExecuteRequest(
    _In_ IHttpContext* pHttpContext,
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    if (m_RequestHandler != NULL)
    {
        return m_RequestHandler(pInProcessHandler, m_RequestHandlerContext);
    }

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

    return RQ_NOTIFICATION_FINISH_REQUEST;
}

BOOL
IN_PROCESS_APPLICATION::DirectoryExists(
    _In_ STRU *pstrPath
)
{
    WIN32_FILE_ATTRIBUTE_DATA data;

    if (pstrPath->IsEmpty())
    {
        return false;
    }

    return GetFileAttributesExW(pstrPath->QueryStr(), GetFileExInfoStandard, &data);
}

BOOL
IN_PROCESS_APPLICATION::GetEnv(
    _In_ PCWSTR pszEnvironmentVariable,
    _Out_ STRU *pstrResult
)
{
    DWORD dwLength;
    PWSTR pszBuffer = NULL;
    BOOL fSucceeded = FALSE;

    if (pszEnvironmentVariable == NULL)
    {
        goto Finished;
    }
    pstrResult->Reset();
    dwLength = GetEnvironmentVariableW(pszEnvironmentVariable, NULL, 0);

    if (dwLength == 0)
    {
        goto Finished;
    }

    pszBuffer = new WCHAR[dwLength];
    if (GetEnvironmentVariableW(pszEnvironmentVariable, pszBuffer, dwLength) == 0)
    {
        goto Finished;
    }

    pstrResult->Copy(pszBuffer);

    fSucceeded = TRUE;

Finished:
    if (pszBuffer != NULL) {
        delete[] pszBuffer;
    }
    return fSucceeded;
}

VOID
IN_PROCESS_APPLICATION::FindDotNetFolders(
    _In_ PCWSTR pszPath,
    _Out_ std::vector<std::wstring> *pvFolders
)
{
    HANDLE handle = NULL;
    WIN32_FIND_DATAW data = { 0 };

    handle = FindFirstFileExW(pszPath, FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    if (handle == INVALID_HANDLE_VALUE)
    {
        return;
    }

    do
    {
        std::wstring folder(data.cFileName);
        pvFolders->push_back(folder);
    } while (FindNextFileW(handle, &data));

    FindClose(handle);
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

    // Initialization complete
    SetEvent(m_pInitalizeEvent);
}

HRESULT
IN_PROCESS_APPLICATION::FindHighestDotNetVersion(
    _In_ std::vector<std::wstring> vFolders,
    _Out_ STRU *pstrResult
)
{
    HRESULT hr = S_OK;
    fx_ver_t max_ver(-1, -1, -1);
    for (const auto& dir : vFolders)
    {
        fx_ver_t fx_ver(-1, -1, -1);
        if (fx_ver_t::parse(dir, &fx_ver, false))
        {
            // TODO using max instead of std::max works
            max_ver = max(max_ver, fx_ver);
        }
    }

    hr = pstrResult->Copy(max_ver.as_str().c_str());

    // we check FAILED(hr) outside of function
    return hr;
}

VOID
IN_PROCESS_APPLICATION::SetStdOut(
    VOID
)
{
    HRESULT      hr = S_OK;
    BOOL         fLocked = FALSE;
    STRU         struPath;

    SYSTEMTIME              systemTime;
    SECURITY_ATTRIBUTES     saAttr = { 0 };

    if (!m_fDoneStdRedirect)
    {
        // Have not set stdout yet, redirect stdout to log file
        AcquireSRWLockExclusive(&m_srwLock);
        fLocked = TRUE;
        if (!m_fDoneStdRedirect)
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

            saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
            saAttr.bInheritHandle = TRUE;
            saAttr.lpSecurityDescriptor = NULL;

            m_hLogFileHandle = CreateFileW(m_struLogFilePath.QueryStr(),
                FILE_WRITE_DATA,
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

            //
            // best effort
            // no need to capture the error code as nothing we can do here
            // in case mamanged layer exits abnormally, may not be able to capture the log content as it is buffered.
            //
            if (!GetConsoleWindow())
            {
                //
                // SetStdHandle works as w3wp does not have Console
                // Current process does not have a console
                //
                SetStdHandle(STD_ERROR_HANDLE, m_hLogFileHandle);
                if (m_pConfig->QueryStdoutLogEnabled())
                {
                    SetStdHandle(STD_OUTPUT_HANDLE, m_hLogFileHandle);
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
            }
            else
            {
                // The process has console, e.g., IIS Express scenario
                CloseHandle(m_hLogFileHandle);
                m_hLogFileHandle = INVALID_HANDLE_VALUE;

                if (m_pConfig->QueryStdoutLogEnabled())
                {
                    if (_wfopen_s(&m_pStdFile, m_struLogFilePath.QueryStr(), L"w") == 0)
                    {
                        // known issue: error info may not be capture when process crashes during buffering
                        // even we disabled FILE buffering
                        setvbuf(m_pStdFile, NULL, _IONBF, 0);
                        _dup2(_fileno(m_pStdFile), _fileno(stdout));
                        _dup2(_fileno(m_pStdFile), _fileno(stderr));
                    }
                    // not work for console scenario
                    // close and AllocConsole does not help
                    //_wfreopen_s(&m_pStdFile, struLogFileName.QueryStr(), L"w", stdout);
                    // SetStdHandle(STD_ERROR_HANDLE, m_hLogFileHandle);
                    // SetStdHandle(STD_OUTPUT_HANDLE, m_hLogFileHandle);
                    //*stdout = *m_pStdFile;
                    //*stderr = *m_pStdFile;
                }
                else
                {
                    // delete the file as log is disabled
                    WIN32_FIND_DATA fileData;
                    HANDLE handle = FindFirstFile(m_struLogFilePath.QueryStr(), &fileData);
                    if (handle != INVALID_HANDLE_VALUE &&
                        fileData.nFileSizeHigh == 0 &&
                        fileData.nFileSizeLow == 0)
                    {
                        FindClose(handle);
                        // no need to check whether the deletion succeeds
                        // as nothing can be done
                        DeleteFile(m_struLogFilePath.QueryStr());
                    }
                }
            }
        }
    }

Finished:
    m_fDoneStdRedirect = TRUE;
    if (fLocked)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
    }
    if (FAILED(hr) && m_pConfig->QueryStdoutLogEnabled())
    {
        //todo log an warning
        //STRU                    strEventMsg;
        //LPCWSTR                 apsz[1];

        //if (SUCCEEDED(strEventMsg.SafeSnwprintf(
        //    ASPNETCORE_EVENT_INVALID_STDOUT_LOG_FILE_MSG,
        //    m_struLogFilePath.QueryStr(),
        //    HRESULT_FROM_GETLASTERROR())))
        //{
        //    apsz[0] = strEventMsg.QueryStr();
        //    //
        //    // not checking return code because if ReportEvent
        //    // fails, we cannot do anything.
        //    //
        //    if (FORWARDING_HANDLER::QueryEventLog() != NULL)
        //    {
        //        ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
        //            EVENTLOG_WARNING_TYPE,
        //            0,
        //            ASPNETCORE_EVENT_CONFIG_ERROR,
        //            NULL,
        //            1,
        //            0,
        //            apsz,
        //            NULL);
        //    }
        //}
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
    BOOL       fLocked = FALSE;
    //PCWSTR     apsz[1];
    //STACK_STRU(strEventMsg, 256);

    if (m_fManagedAppLoaded || m_fLoadManagedAppError)
    {
        // Core CLR has already been loaded.
        // Cannot load more than once even there was a failure
        goto Finished;
    }

    // Set up stdout redirect
    SetStdOut();

    AcquireSRWLockExclusive(&m_srwLock);
    fLocked = TRUE;
    if (m_fManagedAppLoaded || m_fLoadManagedAppError)
    {
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

    m_fManagedAppLoaded = TRUE;

Finished:
    if (fLocked)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
    }

    if (FAILED(hr))
    {
        // Question: in case of application loading failure, should we allow retry on 
        // following request or block the activation at all
        m_fLoadManagedAppError = FALSE; // m_hThread != NULL ?

        // TODO
        //if (SUCCEEDED(strEventMsg.SafeSnwprintf(
        //    ASPNETCORE_EVENT_LOAD_CLR_FALIURE_MSG,
        //    m_pConfiguration->QueryApplicationPath()->QueryStr(),
        //    m_pConfiguration->QueryApplicationPhysicalPath()->QueryStr(),
        //    hr)))
        //{
        //    apsz[0] = strEventMsg.QueryStr();

        //    //
        //    // not checking return code because if ReportEvent
        //    // fails, we cannot do anything.
        //    //
        //    if (FORWARDING_HANDLER::QueryEventLog() != NULL)
        //    {
        //        ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
        //            EVENTLOG_ERROR_TYPE,
        //            0,
        //            ASPNETCORE_EVENT_LOAD_CLR_FALIURE,
        //            NULL,
        //            1,
        //            0,
        //            apsz,
        //            NULL);
        //    }
        //}
    }
    return hr;
}

// static
VOID
IN_PROCESS_APPLICATION::ExecuteAspNetCoreProcess(
    _In_ LPVOID pContext
)
{

    IN_PROCESS_APPLICATION *pApplication = (IN_PROCESS_APPLICATION*)pContext;
    DBG_ASSERT(pApplication != NULL);
    pApplication->ExecuteApplication();
    //
    // no need to log the error here as if error happened, the thread will exit
    // the error will ba catched by caller LoadManagedApplication which will log an error
    //
}


HRESULT
IN_PROCESS_APPLICATION::ExecuteApplication(
    VOID
)
{
    HRESULT     hr = S_OK;

    STRU                        strFullPath;
    STRU                        strDotnetExeLocation;
    STRU                        strHostFxrSearchExpression;
    STRU                        strDotnetFolderLocation;
    STRU                        strHighestDotnetVersion;
    STRU                        strApplicationFullPath;
    PWSTR                       strDelimeterContext = NULL;
    PCWSTR                      pszDotnetExeLocation = NULL;
    PCWSTR                      pszDotnetExeString(L"dotnet.exe");
    DWORD                       dwCopyLength;
    HMODULE                     hModule;
    PCWSTR                      argv[2];
    hostfxr_main_fn             pProc;
    std::vector<std::wstring>   vVersionFolders;
    bool                        fFound = FALSE;

    // Get the System PATH value.
    if (!GetEnv(L"PATH", &strFullPath))
    {
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Split on ';', checking to see if dotnet.exe exists in any folders.
    pszDotnetExeLocation = wcstok_s(strFullPath.QueryStr(), L";", &strDelimeterContext);

    while (pszDotnetExeLocation != NULL)
    {
        dwCopyLength = (DWORD) wcsnlen_s(pszDotnetExeLocation, 260);
        if (dwCopyLength == 0)
        {
            continue;
        }

        // We store both the exe and folder locations as we eventually need to check inside of host\\fxr
        // which doesn't need the dotnet.exe portion of the string
        // TODO consider reducing allocations.
        strDotnetExeLocation.Reset();
        strDotnetFolderLocation.Reset();
        hr = strDotnetExeLocation.Copy(pszDotnetExeLocation, dwCopyLength);
        if (FAILED(hr))
        {
            goto Finished;
        }

        hr = strDotnetFolderLocation.Copy(pszDotnetExeLocation, dwCopyLength);
        if (FAILED(hr))
        {
            goto Finished;
        }

        if (dwCopyLength > 0 && pszDotnetExeLocation[dwCopyLength - 1] != L'\\')
        {
            hr = strDotnetExeLocation.Append(L"\\");
            if (FAILED(hr))
            {
                goto Finished;
            }
        }

        hr = strDotnetExeLocation.Append(pszDotnetExeString);
        if (FAILED(hr))
        {
            goto Finished;
        }

        if (PathFileExists(strDotnetExeLocation.QueryStr()))
        {
            // means we found the folder with a dotnet.exe inside of it.
            fFound = TRUE;
            break;
        }
        pszDotnetExeLocation = wcstok_s(NULL, L";", &strDelimeterContext);
    }
    if (!fFound)
    {
        // could not find dotnet.exe, error out
        hr = ERROR_BAD_ENVIRONMENT;
    }

    hr = strDotnetFolderLocation.Append(L"\\host\\fxr");
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (!DirectoryExists(&strDotnetFolderLocation))
    {
        // error, not found the folder
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Find all folders under host\\fxr\\ for version numbers.
    hr = strHostFxrSearchExpression.Copy(strDotnetFolderLocation);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = strHostFxrSearchExpression.Append(L"\\*");
    if (FAILED(hr))
    {
        goto Finished;
    }

    // As we use the logic from core-setup, we are opting to use std here.
    // TODO remove all uses of std?
    FindDotNetFolders(strHostFxrSearchExpression.QueryStr(), &vVersionFolders);

    if (vVersionFolders.size() == 0)
    {
        // no core framework was found
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    hr = FindHighestDotNetVersion(vVersionFolders, &strHighestDotnetVersion);
    if (FAILED(hr))
    {
        goto Finished;
    }
    hr = strDotnetFolderLocation.Append(L"\\");
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = strDotnetFolderLocation.Append(strHighestDotnetVersion.QueryStr());
    if (FAILED(hr))
    {
        goto Finished;

    }

    hr = strDotnetFolderLocation.Append(L"\\hostfxr.dll");
    if (FAILED(hr))
    {
        goto Finished;
    }

    hModule = LoadLibraryW(strDotnetFolderLocation.QueryStr());

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
        hr = ERROR_BAD_ENVIRONMENT; // better hrresult?
        goto Finished;
    }

    // The first argument is mostly ignored
    argv[0] = strDotnetExeLocation.QueryStr();
    UTILITY::ConvertPathToFullPath(m_pConfig->QueryArguments()->QueryStr(),
        m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
        &strApplicationFullPath);
    argv[1] = strApplicationFullPath.QueryStr();

    // There can only ever be a single instance of .NET Core
    // loaded in the process but we need to get config information to boot it up in the
    // first place. This is happening in an execute request handler and everyone waits
    // until this initialization is done.

    // We set a static so that managed code can call back into this instance and
    // set the callbacks
    s_Application = this;

    RunDotnetApplication(argv, pProc);

Finished:
    //
    // this method is called by the background thread and should never exit unless shutdown
    //
    if (!m_fRecycleProcessCalled)
    {
        //STRU                    strEventMsg;
        //LPCWSTR                 apsz[1];
        //if (SUCCEEDED(strEventMsg.SafeSnwprintf(
        //    ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG,
        //    m_pConfig->QueryApplicationPath()->QueryStr(),
        //    m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
        //    m_ProcessExitCode
        //)))
        //{
        //    apsz[0] = strEventMsg.QueryStr();

        //    //
        //    // not checking return code because if ReportEvent
        //    // fails, we cannot do anything.
        //    //
        //    if (FORWARDING_HANDLER::QueryEventLog() != NULL)
        //    {
        //        ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
        //            EVENTLOG_ERROR_TYPE,
        //            0,
        //            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
        //            NULL,
        //            1,
        //            0,
        //            apsz,
        //            NULL);
        //    }
        //    // error. the thread exits after application started
        //    // Question: should we shutdown current worker process or keep the application in failure state?
        //    // for now, we reccylce to keep the same behavior as that of out-of-process
        //}
        if (m_fManagedAppLoaded)
        {
            Recycle();
        }
    }
    return hr;
}

//
// Calls hostfxr_main with the hostfxr and application as arguments.
// Method should be called with only 
// Need to have __try / __except in methods that require unwinding.
// 
HRESULT
IN_PROCESS_APPLICATION::RunDotnetApplication(PCWSTR* argv, hostfxr_main_fn pProc)
{
    HRESULT hr = S_OK;
    __try
    {
        m_ProcessExitCode = pProc(2, argv);
    }
    __except (FilterException(GetExceptionCode(), GetExceptionInformation()))
    {
        // TODO Log error message here.
        hr = E_FAIL;
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
