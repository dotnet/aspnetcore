// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "inprocessapplication.h"
#include "inprocesshandler.h"
#include "hostfxroptions.h"
#include "requesthandler_config.h"
#include "environmentvariablehelpers.h"
#include "aspnetcore_event.h"

IN_PROCESS_APPLICATION*  IN_PROCESS_APPLICATION::s_Application = NULL;
hostfxr_main_fn IN_PROCESS_APPLICATION::s_fMainCallback = NULL;

IN_PROCESS_APPLICATION::IN_PROCESS_APPLICATION(
    IHttpServer *pHttpServer,
    REQUESTHANDLER_CONFIG *pConfig) :
    m_pHttpServer(pHttpServer),
    m_ProcessExitCode(0),
    m_fBlockCallbacksIntoManaged(FALSE),
    m_fInitialized(FALSE),
    m_fShutdownCalledFromNative(FALSE),
    m_fShutdownCalledFromManaged(FALSE),
    m_srwLock()
{
    // is it guaranteed that we have already checked app offline at this point?
    // If so, I don't think there is much to do here.
    DBG_ASSERT(pHttpServer != NULL);
    DBG_ASSERT(pConfig != NULL);

    InitializeSRWLock(&m_srwLock);
    m_pConfig = pConfig;

    m_status = APPLICATION_STATUS::STARTING;
}

HRESULT
IN_PROCESS_APPLICATION::Initialize(
    PCWSTR pDotnetExeLocation
)
{
    return m_struExeLocation.Copy(pDotnetExeLocation);
}

IN_PROCESS_APPLICATION::~IN_PROCESS_APPLICATION()
{

    if (m_pConfig != NULL)
    {
        delete m_pConfig;
        m_pConfig = NULL;
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

    if (m_pLoggerProvider != NULL)
    {
        delete m_pLoggerProvider;
        m_pLoggerProvider = NULL;
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

    m_pLoggerProvider->NotifyStartupComplete();
    // Can't check the std err handle as it isn't a critical error
    // Initialization complete
    SetEvent(m_pInitalizeEvent);
    m_fInitialized = TRUE;
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

    {
        // Set up stdout redirect

        SRWLockWrapper lock(m_srwLock);

        if (m_pLoggerProvider == NULL)
        {
            hr =  LoggingHelpers::CreateLoggingProvider(
                m_pConfig->QueryStdoutLogEnabled(),
                !GetConsoleWindow(),
                m_pConfig->QueryStdoutLogFile()->QueryStr(),
                m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
                &m_pLoggerProvider);
            if (FAILED(hr))
            {
                goto Finished;
            }

            if (FAILED(hr = m_pLoggerProvider->Start()))
            {
                goto Finished;
            }
        }
      
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
    HMODULE             hModule = nullptr;
    DWORD               hostfxrArgc = 0;
    BSTR               *hostfxrArgv = NULL;
    hostfxr_main_fn     pProc;
    std::unique_ptr<HOSTFXR_OPTIONS>    hostFxrOptions = NULL;

    DBG_ASSERT(m_status == APPLICATION_STATUS::STARTING);

    pProc = s_fMainCallback;
    if (pProc == nullptr)
    {
        // hostfxr should already be loaded by the shim. If not, then we will need
        // to load it ourselves by finding hostfxr again.
        hModule = LoadLibraryW(L"hostfxr.dll");

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

        if (FAILED(hr = HOSTFXR_OPTIONS::Create(
            m_struExeLocation.QueryStr(),
            m_pConfig->QueryProcessPath()->QueryStr(),
            m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
            m_pConfig->QueryArguments()->QueryStr(),
            g_hEventLog,
            hostFxrOptions
            )))
        {
            goto Finished;
        }

        hostfxrArgc = hostFxrOptions->GetArgc();
        hostfxrArgv = hostFxrOptions->GetArgv();

        if (FAILED(hr = SetEnvironementVariablesOnWorkerProcess()))
        {
            goto Finished;
        }
    }

    // There can only ever be a single instance of .NET Core
    // loaded in the process but we need to get config information to boot it up in the
    // first place. This is happening in an execute request handler and everyone waits
    // until this initialization is done.
    // We set a static so that managed code can call back into this instance and
    // set the callbacks
    s_Application = this;

    hr = RunDotnetApplication(hostfxrArgc, hostfxrArgv, pProc);

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
        m_pLoggerProvider->NotifyStartupComplete();

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
    STRA struStdErrOutput;
    if (m_pLoggerProvider->GetStdOutContent(&struStdErrOutput))
    {
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDOUT_MSG,
            m_pConfig->QueryApplicationPath()->QueryStr(),
            m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
            hr,
            struStdErrOutput.QueryStr());
    }
    else
    {
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG,
            m_pConfig->QueryApplicationPath()->QueryStr(),
            m_pConfig->QueryApplicationPhysicalPath()->QueryStr(),
            hr);
    }
}

//
// Calls hostfxr_main with the hostfxr and application as arguments.
//
HRESULT
IN_PROCESS_APPLICATION::RunDotnetApplication(DWORD argc, CONST PCWSTR* argv, hostfxr_main_fn pProc)
{
    HRESULT hr = S_OK;

    __try
    {
        m_ProcessExitCode = pProc(argc, argv);
        if (m_ProcessExitCode != 0)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }
    __except(GetExceptionCode() != 0)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
    }

    return hr;
}

// static

REQUESTHANDLER_CONFIG*
IN_PROCESS_APPLICATION::QueryConfig() const
{
    return m_pConfig;
}

HRESULT
IN_PROCESS_APPLICATION::CreateHandler(
    _In_  IHttpContext       *pHttpContext,
    _Out_ IREQUEST_HANDLER  **pRequestHandler)
{
    HRESULT hr = S_OK;
    IREQUEST_HANDLER* pHandler = NULL;

    pHandler = new IN_PROCESS_HANDLER(pHttpContext, this);

    if (pHandler == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }

    *pRequestHandler = pHandler;
    return hr;
}
