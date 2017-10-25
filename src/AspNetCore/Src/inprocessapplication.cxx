// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "precomp.hxx"
#include <algorithm>

typedef DWORD(*hostfxr_main_fn) (CONST DWORD argc, CONST WCHAR* argv[]);

IN_PROCESS_APPLICATION*  IN_PROCESS_APPLICATION::s_Application = NULL;

IN_PROCESS_APPLICATION::IN_PROCESS_APPLICATION() :
    m_ProcessExitCode ( 0 ),
    m_fManagedAppLoaded ( FALSE ),
    m_fLoadManagedAppError ( FALSE ),
    m_fInitialized ( FALSE )
{
}

IN_PROCESS_APPLICATION::~IN_PROCESS_APPLICATION()
{
    Recycle();
}

REQUEST_NOTIFICATION_STATUS
IN_PROCESS_APPLICATION::OnAsyncCompletion(
    IHttpContext*           pHttpContext,
    DWORD                   cbCompletion,
    HRESULT                 hrCompletionStatus
)
{
    HRESULT hr;
    IN_PROCESS_STORED_CONTEXT* pInProcessStoredContext = NULL;
    REQUEST_NOTIFICATION_STATUS dwRequestNotificationStatus = RQ_NOTIFICATION_CONTINUE;

    hr = IN_PROCESS_STORED_CONTEXT::GetInProcessStoredContext(pHttpContext, &pInProcessStoredContext);
    if (FAILED(hr))
    {
        // Finish the request as we couldn't get the callback
        pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 19, hr);
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else if (pInProcessStoredContext->QueryIsManagedRequestComplete())
    {
        // means PostCompletion has been called and this is the associated callback.
        dwRequestNotificationStatus = pInProcessStoredContext->QueryAsyncCompletionStatus();
        // TODO cleanup whatever disconnect listener there is
        return dwRequestNotificationStatus;
    }
    else
    {
        // Call the managed handler for async completion.
        return m_AsyncCompletionHandler(pInProcessStoredContext->QueryManagedHttpContext(), hrCompletionStatus, cbCompletion);
    }
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
    m_RequstHandlerContext = pvRequstHandlerContext;
    m_ShutdownHandler = shutdown_handler;
    m_ShutdownHandlerContext = pvShutdownHandlerContext;
    m_AsyncCompletionHandler = async_completion_handler;

    // Initialization complete
    SetEvent(m_pInitalizeEvent);
}

//
// Initialize is guarded by a lock inside APPLICATION_MANAGER::GetApplication
// It ensures only one application will be initialized and singleton
// Error wuill happen if you call Initialized outside APPLICATION_MANAGER::GetApplication
//
__override
HRESULT
IN_PROCESS_APPLICATION::Initialize(
    _In_ APPLICATION_MANAGER* pApplicationManager,
    _In_ ASPNETCORE_CONFIG*   pConfiguration
)
{
    HRESULT hr = S_OK;
    DBG_ASSERT(pApplicationManager != NULL);
    DBG_ASSERT(pConfiguration != NULL);

    m_pConfiguration = pConfiguration;
    m_pApplicationManager = pApplicationManager;
    hr = m_applicationKey.Initialize(pConfiguration->QueryApplicationPath()->QueryStr());
    if (FAILED(hr))
    {
        goto Finished;
    }

    // check app_offline
    UpdateAppOfflineFileHandle();

    if (m_pFileWatcherEntry == NULL)
    {
        m_pFileWatcherEntry = new FILE_WATCHER_ENTRY(m_pApplicationManager->GetFileWatcher());
        if (m_pFileWatcherEntry == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }
    }

    m_pInitalizeEvent = CreateEvent(
                            NULL,   // default security attributes
                            TRUE,   // manual reset event
                            FALSE,  // not set
                            NULL);  // name
    if (m_pInitalizeEvent == NULL)
    {
       hr =  HRESULT_FROM_WIN32(GetLastError());
       goto Finished;
    }
    m_fInitialized = TRUE;

Finished:
    return hr;
}

HRESULT
IN_PROCESS_APPLICATION::LoadManagedApplication()
{
    HRESULT    hr = S_OK;
    DWORD      dwTimeout;
    DWORD      dwResult;
    BOOL       fLocked = FALSE;
    PCWSTR     apsz[1];
    STACK_STRU(strEventMsg, 256);

    if (m_fManagedAppLoaded || m_fLoadManagedAppError)
    {
        // Core CLR has already been loaded.
        // Cannot load more than once even there was a failure
        goto Finished;
    }

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

    // If the debugger is attached, never timeout
    if (IsDebuggerPresent())
    {
        dwTimeout = INFINITE;
    }
    else
    {
        dwTimeout = m_pConfiguration->QueryStartupTimeLimitInMS();
    }

    const HANDLE pHandles[2]{ m_hThread, m_pInitalizeEvent };

    // Wait on either the thread to complete or the event to be set
    dwResult = WaitForMultipleObjects(2, pHandles, FALSE, dwTimeout);

    // It all timed out
    if (dwResult == WAIT_TIMEOUT)
    {
        // do we need kill the backend thread
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

        if (SUCCEEDED(strEventMsg.SafeSnwprintf(
            ASPNETCORE_EVENT_LOAD_CLR_FALIURE_MSG,
            m_pConfiguration->QueryApplicationPath()->QueryStr(),
            m_pConfiguration->QueryApplicationFullPath()->QueryStr(),
            hr)))
        {
            apsz[0] = strEventMsg.QueryStr();

            //
            // not checking return code because if ReportEvent
            // fails, we cannot do anything.
            //
            if (FORWARDING_HANDLER::QueryEventLog() != NULL)
            {
                ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                    EVENTLOG_ERROR_TYPE,
                    0,
                    ASPNETCORE_EVENT_LOAD_CLR_FALIURE,
                    NULL,
                    1,
                    0,
                    apsz,
                    NULL);
            }
        }
    }
    return hr;
}

VOID
IN_PROCESS_APPLICATION::Recycle(
    VOID
)
{
    if (m_fInitialized)
    {
        DWORD    dwThreadStatus = 0;
        DWORD    dwTimeout = m_pConfiguration->QueryShutdownTimeLimitInMS();

        AcquireSRWLockExclusive(&m_srwLock);

        if (!g_pHttpServer->IsCommandLineLaunch() &&
            !g_fRecycleProcessCalled &&
            (g_pHttpServer->GetAdminManager() != NULL))
        {
            // IIS scenario.
            // notify IIS first so that new request will be routed to new worker process
            g_pHttpServer->RecycleProcess(L"AspNetCore Recycle Process on Demand");
        }

        g_fRecycleProcessCalled = TRUE;

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
        if (g_pHttpServer && g_pHttpServer->IsCommandLineLaunch())
        {
            // IISExpress scenario
            // Can only call exit to terminate current process
            exit(0);
        }
    }
}

VOID
IN_PROCESS_APPLICATION::OnAppOfflineHandleChange()
{
    // only recycle the worker process after managed app was loaded
    // app_offline scenario managed application has not been loaded yet
    if (m_fManagedAppLoaded || m_fLoadManagedAppError)
    {
        Recycle();

    }
}

REQUEST_NOTIFICATION_STATUS
IN_PROCESS_APPLICATION::ExecuteRequest(
    _In_ IHttpContext* pHttpContext
)
{
    if (m_RequestHandler != NULL)
    {
        return m_RequestHandler(pHttpContext, m_RequstHandlerContext);
    }

    //
    // return error as the application did not register callback
    //
    if (ANCMEvents::ANCM_EXECUTE_REQUEST_FAIL::IsEnabled(pHttpContext->GetTraceContext()))
    {
        ANCMEvents::ANCM_EXECUTE_REQUEST_FAIL::RaiseEvent(pHttpContext->GetTraceContext(),
            NULL,
            E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    }
    pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    return RQ_NOTIFICATION_FINISH_REQUEST;
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
        dwCopyLength = wcsnlen_s(pszDotnetExeLocation, 260);
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
    hr = strDotnetExeLocation.Append(pszDotnetExeString);
    if (FAILED(hr))
    {
        goto Finished;
    }

    argv[0] = strDotnetExeLocation.QueryStr();
    PATH::ConvertPathToFullPath(m_pConfiguration->QueryArguments()->QueryStr(),
                                m_pConfiguration->QueryApplicationFullPath()->QueryStr(),
                                &strApplicationFullPath);
    argv[1] = strApplicationFullPath.QueryStr();

    // There can only ever be a single instance of .NET Core
    // loaded in the process but we need to get config information to boot it up in the
    // first place. This is happening in an execute request handler and everyone waits
    // until this initialization is done.

    // We set a static so that managed code can call back into this instance and
    // set the callbacks
    s_Application = this;

    m_ProcessExitCode = pProc(2, argv);
    if (m_ProcessExitCode != 0)
    {
        
    }

Finished:
    //
    // this method is called by the background thread and should never exit unless shutdown
    //
    if (!g_fRecycleProcessCalled)
    {
        STRU                    strEventMsg;
        LPCWSTR                 apsz[1];
        if (SUCCEEDED(strEventMsg.SafeSnwprintf(
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG,
            m_pConfiguration->QueryApplicationPath()->QueryStr(),
            m_pConfiguration->QueryApplicationFullPath()->QueryStr(),
            m_ProcessExitCode
        )))
        {
            apsz[0] = strEventMsg.QueryStr();

            //
            // not checking return code because if ReportEvent
            // fails, we cannot do anything.
            //
            if (FORWARDING_HANDLER::QueryEventLog() != NULL)
            {
                ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                    EVENTLOG_ERROR_TYPE,
                    0,
                    ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
                    NULL,
                    1,
                    0,
                    apsz,
                    NULL);
            }
            // error. the thread exits after application started
            // Question: should we shutdown current worker process or keep the application in failure state?
            // for now, we reccylce to keep the same behavior as that of out-of-process
            if (m_fManagedAppLoaded)
            {
                Recycle();
            }
        }
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
            max_ver = std::max(max_ver, fx_ver);
        }
    }

    hr = pstrResult->Copy(max_ver.as_str().c_str());

    // we check FAILED(hr) outside of function
    return hr;
}
