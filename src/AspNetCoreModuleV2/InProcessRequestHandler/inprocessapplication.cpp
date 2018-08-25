// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "inprocessapplication.h"
#include "inprocesshandler.h"
#include "hostfxroptions.h"
#include "requesthandler_config.h"
#include "environmentvariablehelpers.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "LoggingHelpers.h"
#include "resources.h"
#include "EventLog.h"

const LPCSTR IN_PROCESS_APPLICATION::s_exeLocationParameterName = "InProcessExeLocation";

IN_PROCESS_APPLICATION*  IN_PROCESS_APPLICATION::s_Application = NULL;

IN_PROCESS_APPLICATION::IN_PROCESS_APPLICATION(
    IHttpServer& pHttpServer,
    IHttpApplication& pApplication,
    std::unique_ptr<InProcessOptions> pConfig,
    APPLICATION_PARAMETER *pParameters,
    DWORD                  nParameters) :
    InProcessApplicationBase(pHttpServer, pApplication),
    m_ProcessExitCode(0),
    m_fBlockCallbacksIntoManaged(FALSE),
    m_fShutdownCalledFromNative(FALSE),
    m_fShutdownCalledFromManaged(FALSE),
    m_pConfig(std::move(pConfig))
{
    DBG_ASSERT(m_pConfig);

    for (DWORD i = 0; i < nParameters; i++)
    {
        if (_stricmp(pParameters[i].pzName, s_exeLocationParameterName) == 0)
        {
            m_struExeLocation.Copy(reinterpret_cast<PCWSTR>(pParameters[i].pValue));
        }
    }

    m_status = MANAGED_APPLICATION_STATUS::STARTING;
}

IN_PROCESS_APPLICATION::~IN_PROCESS_APPLICATION()
{
    s_Application = NULL;
}

//static
DWORD WINAPI
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
IN_PROCESS_APPLICATION::StopInternal(bool fServerInitiated)
{
    UNREFERENCED_PARAMETER(fServerInitiated);
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
        EventLog::Warn(
            ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE,
            ASPNETCORE_EVENT_APP_SHUTDOWN_FAILURE_MSG,
            QueryConfigPath().c_str());
    }
	else
    {
        EventLog::Info(
            ASPNETCORE_EVENT_APP_SHUTDOWN_SUCCESSFUL,
            ASPNETCORE_EVENT_APP_SHUTDOWN_SUCCESSFUL_MSG,
            QueryConfigPath().c_str());
    }

    InProcessApplicationBase::StopInternal(fServerInitiated);
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
        m_status == MANAGED_APPLICATION_STATUS::STARTING ||
        m_status == MANAGED_APPLICATION_STATUS::FAIL)
    {
        return;
    }

    {
        if (m_fShutdownCalledFromNative ||
            m_status == MANAGED_APPLICATION_STATUS::STARTING ||
            m_status == MANAGED_APPLICATION_STATUS::FAIL)
        {
            return;
        }

        // We need to keep track of when both managed and native initiate shutdown
        // to avoid AVs. If shutdown has already been initiated in managed, we don't want to call into
        // managed. We still need to wait on main exiting no matter what. m_fShutdownCalledFromNative
        // is used for detecting redundant calls and blocking more requests to OnExecuteRequestHandler.
        m_fShutdownCalledFromNative = TRUE;
        m_status = MANAGED_APPLICATION_STATUS::SHUTDOWN;

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

    s_Application = NULL;
}

VOID
IN_PROCESS_APPLICATION::SetCallbackHandles(
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ PFN_ASYNC_COMPLETION_HANDLER async_completion_handler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    m_RequestHandler = request_handler;
    m_RequestHandlerContext = pvRequstHandlerContext;
    m_ShutdownHandler = shutdown_handler;
    m_ShutdownHandlerContext = pvShutdownHandlerContext;
    m_AsyncCompletionHandler = async_completion_handler;

    // Can't check the std err handle as it isn't a critical error
    // Initialization complete
    EventLog::Info(
        ASPNETCORE_EVENT_INPROCESS_START_SUCCESS,
        ASPNETCORE_EVENT_INPROCESS_START_SUCCESS_MSG,
        QueryApplicationPhysicalPath().c_str());
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

    if (m_status != MANAGED_APPLICATION_STATUS::STARTING)
    {
        // Core CLR has already been loaded.
        // Cannot load more than once even there was a failure
        if (m_status == MANAGED_APPLICATION_STATUS::FAIL)
        {
            hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
        }
        else if (m_status == MANAGED_APPLICATION_STATUS::SHUTDOWN)
        {
            hr = HRESULT_FROM_WIN32(ERROR_SHUTDOWN_IS_SCHEDULED);
        }

        goto Finished;
    }

    {
        SRWExclusiveLock lock(m_stateLock);

        if (m_status != MANAGED_APPLICATION_STATUS::STARTING)
        {
            if (m_status == MANAGED_APPLICATION_STATUS::FAIL)
            {
                hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
            }
            else if (m_status == MANAGED_APPLICATION_STATUS::SHUTDOWN)
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

        m_status = MANAGED_APPLICATION_STATUS::RUNNING_MANAGED;
    }
Finished:

    if (FAILED(hr))
    {
        m_status = MANAGED_APPLICATION_STATUS::FAIL;

        EventLog::Error(
            ASPNETCORE_EVENT_LOAD_CLR_FALIURE,
            ASPNETCORE_EVENT_LOAD_CLR_FALIURE_MSG,
            QueryApplicationId().c_str(),
            QueryApplicationPhysicalPath().c_str(),
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
    auto variables = m_pConfig->QueryEnvironmentVariables();
    auto inputTable = std::unique_ptr<ENVIRONMENT_VAR_HASH, ENVIRONMENT_VAR_HASH_DELETER>(new ENVIRONMENT_VAR_HASH());
    RETURN_IF_FAILED(inputTable->Initialize(37 /*prime*/));
    // Copy environment variables to old style hash table
    for (auto & variable : variables)
    {
        auto pNewEntry = std::unique_ptr<ENVIRONMENT_VAR_ENTRY, ENVIRONMENT_VAR_ENTRY_DELETER>(new ENVIRONMENT_VAR_ENTRY());
        RETURN_IF_FAILED(pNewEntry->Initialize((variable.first + L"=").c_str(), variable.second.c_str()));
        RETURN_IF_FAILED(inputTable->InsertRecord(pNewEntry.get()));
    }

    ENVIRONMENT_VAR_HASH* pHashTable = NULL;
    std::unique_ptr<ENVIRONMENT_VAR_HASH, ENVIRONMENT_VAR_HASH_DELETER> table;
    RETURN_IF_FAILED(ENVIRONMENT_VAR_HELPERS::InitEnvironmentVariablesTable(
        inputTable.get(),
        m_pConfig->QueryWindowsAuthEnabled(),
        m_pConfig->QueryBasicAuthEnabled(),
        m_pConfig->QueryAnonymousAuthEnabled(),
        &pHashTable));

    table.reset(pHashTable);

    HRESULT hr = S_OK;
    table->Apply(ENVIRONMENT_VAR_HELPERS::AppendEnvironmentVariables, &hr);
    RETURN_IF_FAILED(hr);

    table->Apply(ENVIRONMENT_VAR_HELPERS::SetEnvironmentVariables, &hr);
    RETURN_IF_FAILED(hr);

    return S_OK;
}

HRESULT
IN_PROCESS_APPLICATION::ExecuteApplication(
    VOID
)
{
    HRESULT             hr;
    HMODULE             hModule = nullptr;
    hostfxr_main_fn     pProc;
    std::unique_ptr<HOSTFXR_OPTIONS>    hostFxrOptions = NULL;
    DWORD                       hostfxrArgc = 0;
    std::unique_ptr<PCWSTR[]>   hostfxrArgv;
    DBG_ASSERT(m_status == MANAGED_APPLICATION_STATUS::STARTING);

    pProc = s_fMainCallback;
    if (pProc == nullptr)
    {
        // hostfxr should already be loaded by the shim. If not, then we will need
        // to load it ourselves by finding hostfxr again.
        hModule = LoadLibraryW(L"hostfxr.dll");

        if (hModule == NULL)
        {
            // .NET Core not installed (we can log a more detailed error message here)
            hr = LOG_IF_FAILED(ERROR_BAD_ENVIRONMENT);
            goto Finished;
        }

        // Get the entry point for main
        pProc = (hostfxr_main_fn)GetProcAddress(hModule, "hostfxr_main");
        if (pProc == NULL)
        {
            hr = LOG_IF_FAILED(ERROR_BAD_ENVIRONMENT);
            goto Finished;
        }

        FINISHED_IF_FAILED(hr = HOSTFXR_OPTIONS::Create(
            m_struExeLocation.QueryStr(),
            m_pConfig->QueryProcessPath().c_str(),
            QueryApplicationPhysicalPath().c_str(),
            m_pConfig->QueryArguments().c_str(),
            hostFxrOptions
            ));
        hostFxrOptions->GetArguments(hostfxrArgc, hostfxrArgv);
        FINISHED_IF_FAILED(SetEnvironementVariablesOnWorkerProcess());
    }

    LOG_INFO(L"Starting managed application");

    if (m_pLoggerProvider == NULL)
    {
        FINISHED_IF_FAILED(hr = LoggingHelpers::CreateLoggingProvider(
            m_pConfig->QueryStdoutLogEnabled(),
            !m_pHttpServer.IsCommandLineLaunch(),
            m_pConfig->QueryStdoutLogFile().c_str(),
            QueryApplicationPhysicalPath().c_str(),
            m_pLoggerProvider));

        LOG_IF_FAILED(m_pLoggerProvider->Start());
    }

    // There can only ever be a single instance of .NET Core
    // loaded in the process but we need to get config information to boot it up in the
    // first place. This is happening in an execute request handler and everyone waits
    // until this initialization is done.
    // We set a static so that managed code can call back into this instance and
    // set the callbacks
    s_Application = this;

    hr = RunDotnetApplication(hostfxrArgc, hostfxrArgv.get(), pProc);

Finished:

    //
    // this method is called by the background thread and should never exit unless shutdown
    // If main returned and shutdown was not called in managed, we want to block native from calling into
    // managed. To do this, we can say that shutdown was called from managed.
    // Don't bother locking here as there will always be a race between receiving a native shutdown
    // notification and unexpected managed exit.
    //
    m_status = MANAGED_APPLICATION_STATUS::SHUTDOWN;
    m_fShutdownCalledFromManaged = TRUE;

    m_pLoggerProvider->Stop();

    if (!m_fShutdownCalledFromNative)
    {
        LogErrorsOnMainExit(hr);
        if (m_fInitialized)
        {
            //
            // If the inprocess server was initialized, we need to cause recycle to be called on the worker process.
            //
            Stop(/*fServerInitiated*/ false);
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
    STRA straStdErrOutput;
    STRU struStdMsg;
    if (m_pLoggerProvider->GetStdOutContent(&straStdErrOutput))
    {
        if (SUCCEEDED(struStdMsg.CopyA(straStdErrOutput.QueryStr()))) {
            EventLog::Error(
                ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDOUT,
                ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDOUT_MSG,
                QueryApplicationId().c_str(),
                QueryApplicationPhysicalPath().c_str(),
                hr,
                struStdMsg.QueryStr());
        }
    }
    else
    {
        EventLog::Error(
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG,
            QueryApplicationId().c_str(),
            QueryApplicationPhysicalPath().c_str(),
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

        LOG_INFOF(L"Managed application exited with code %d", m_ProcessExitCode);
    }
    __except(GetExceptionCode() != 0)
    {

        LOG_INFOF(L"Managed threw an exception %d", GetExceptionCode());
        hr = HRESULT_FROM_WIN32(GetLastError());
    }

    return hr;
}

HRESULT
IN_PROCESS_APPLICATION::CreateHandler(
    _In_  IHttpContext       *pHttpContext,
    _Out_ IREQUEST_HANDLER  **pRequestHandler)
{
    HRESULT hr = S_OK;
    IREQUEST_HANDLER* pHandler = NULL;

    pHandler = new IN_PROCESS_HANDLER(::ReferenceApplication(this), pHttpContext, m_RequestHandler, m_RequestHandlerContext, m_AsyncCompletionHandler);

    if (pHandler == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }

    *pRequestHandler = pHandler;
    return hr;
}
