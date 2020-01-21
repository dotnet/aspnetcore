// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "inprocessapplication.h"
#include "inprocesshandler.h"
#include "HostFxrResolutionResult.h"
#include "requesthandler_config.h"
#include "environmentvariablehelpers.h"
#include "exceptions.h"
#include "LoggingHelpers.h"
#include "resources.h"
#include "EventLog.h"
#include "ModuleHelpers.h"
#include "Environment.h"
#include "HostFxr.h"

IN_PROCESS_APPLICATION*  IN_PROCESS_APPLICATION::s_Application = NULL;

IN_PROCESS_APPLICATION::IN_PROCESS_APPLICATION(
    IHttpServer& pHttpServer,
    IHttpApplication& pApplication,
    std::unique_ptr<InProcessOptions> pConfig,
    APPLICATION_PARAMETER* pParameters,
    DWORD                  nParameters) :
    InProcessApplicationBase(pHttpServer, pApplication),
    m_Initialized(false),
    m_blockManagedCallbacks(true),
    m_waitForShutdown(true),
    m_pConfig(std::move(pConfig)),
    m_requestCount(0)
{
    DBG_ASSERT(m_pConfig);

    const auto knownLocation = FindParameter<PCWSTR>(s_exeLocationParameterName, pParameters, nParameters);
    if (knownLocation != nullptr)
    {
        m_dotnetExeKnownLocation = knownLocation;
    }

    m_stringRedirectionOutput = std::make_shared<StringStreamRedirectionOutput>();
}

IN_PROCESS_APPLICATION::~IN_PROCESS_APPLICATION()
{
    s_Application = nullptr;
}

VOID
IN_PROCESS_APPLICATION::StopInternal(bool fServerInitiated)
{
    StopClr();
    InProcessApplicationBase::StopInternal(fServerInitiated);
}

VOID
IN_PROCESS_APPLICATION::StopClr()
{
    // This has the state lock around it.
    LOG_INFO(L"Stopping CLR");

    if (!m_blockManagedCallbacks)
    {
        // We cannot call into managed if the dll is detaching from the process.
        // Calling into managed code when the dll is detaching is strictly a bad idea,
        // and usually results in an AV saying "The string binding is invalid"
        const auto shutdownHandler = m_ShutdownHandler;

        if (!g_fProcessDetach && shutdownHandler != nullptr)
        {
            shutdownHandler(m_ShutdownHandlerContext);
        }

        SRWSharedLock dataLock(m_dataLock);

        auto requestCount = m_requestCount.load();

        if (requestCount == 0)
        {
            CallRequestsDrained();
        }
    }

    // Signal shutdown
    if (m_pShutdownEvent != nullptr)
    {
        LOG_IF_FAILED(SetEvent(m_pShutdownEvent));
    }

    if (m_workerThread.joinable())
    {
        // Worker thread would wait for clr to finish and log error if required
        m_workerThread.join();
    }

    s_Application = nullptr;
}

VOID
IN_PROCESS_APPLICATION::SetCallbackHandles(
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ PFN_DISCONNECT_HANDLER disconnect_callback,
    _In_ PFN_ASYNC_COMPLETION_HANDLER async_completion_handler,
    _In_ PFN_REQUESTS_DRAINED_HANDLER requestsDrainedHandler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    LOG_INFO(L"In-process callbacks set");

    m_RequestHandler = request_handler;
    m_RequestHandlerContext = pvRequstHandlerContext;
    m_DisconnectHandler = disconnect_callback;
    m_ShutdownHandler = shutdown_handler;
    m_ShutdownHandlerContext = pvShutdownHandlerContext;
    m_AsyncCompletionHandler = async_completion_handler;
    m_RequestsDrainedHandler = requestsDrainedHandler;

    m_blockManagedCallbacks = false;
    m_Initialized = true;

    // Can't check the std err handle as it isn't a critical error
    // Initialization complete
    EventLog::Info(
        ASPNETCORE_EVENT_INPROCESS_START_SUCCESS,
        ASPNETCORE_EVENT_INPROCESS_START_SUCCESS_MSG,
        QueryApplicationPhysicalPath().c_str());

    SetEvent(m_pInitializeEvent);
}

HRESULT
IN_PROCESS_APPLICATION::LoadManagedApplication(ErrorContext& errorContext)
{
    THROW_LAST_ERROR_IF_NULL(m_pInitializeEvent = CreateEvent(
        nullptr,  // default security attributes
        TRUE,     // manual reset event
        FALSE,    // not set
        nullptr)); // name

    THROW_LAST_ERROR_IF_NULL(m_pShutdownEvent = CreateEvent(
        nullptr,  // default security attributes
        TRUE,     // manual reset event
        FALSE,    // not set
        nullptr)); // name

    LOG_INFO(L"Waiting for initialization");

    m_workerThread = std::thread([](std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> application)
    {
        LOG_INFO(L"Starting in-process worker thread");
        application->ExecuteApplication();
        LOG_INFO(L"Stopping in-process worker thread");
    }, ::ReferenceApplication(this));

    const HANDLE waitHandles[2] = { m_pInitializeEvent, m_workerThread.native_handle() };

    // Wait for shutdown request
    const auto waitResult = WaitForMultipleObjects(2, waitHandles, FALSE, m_pConfig->QueryStartupTimeLimitInMS());

    THROW_LAST_ERROR_IF(waitResult == WAIT_FAILED);

    if (waitResult == WAIT_TIMEOUT)
    {
        // If server wasn't initialized in time shut application down without waiting for CLR thread to exit
        errorContext.statusCode = 500;
        errorContext.subStatusCode = 37;
        errorContext.generalErrorType = "ASP.NET Core app failed to start within startup time limit";
        errorContext.errorReason = format("ASP.NET Core app failed to start after %d milliseconds", m_pConfig->QueryStartupTimeLimitInMS());

        m_waitForShutdown = false;
        StopClr();
        throw InvalidOperationException(format(L"Managed server didn't initialize after %u ms.", m_pConfig->QueryStartupTimeLimitInMS()));
    }

    // WAIT_OBJECT_0 + 1 is the worker thead handle
    if (waitResult == WAIT_OBJECT_0 + 1)
    {
        // Worker thread exited stop
        StopClr();
        throw InvalidOperationException(format(L"CLR worker thread exited prematurely"));
    }

    THROW_IF_FAILED(StartMonitoringAppOffline());

    return S_OK;
}

void
IN_PROCESS_APPLICATION::ExecuteApplication()
{
    try
    {
        std::unique_ptr<HostFxrResolutionResult> hostFxrResolutionResult;

        auto context = std::make_shared<ExecuteClrContext>();

        ErrorContext errorContext; // unused

        if (s_fMainCallback == nullptr)
        {
            THROW_IF_FAILED(HostFxrResolutionResult::Create(
                m_dotnetExeKnownLocation,
                m_pConfig->QueryProcessPath(),
                QueryApplicationPhysicalPath(),
                m_pConfig->QueryArguments(),
                errorContext,
                hostFxrResolutionResult
                ));

            hostFxrResolutionResult->GetArguments(context->m_argc, context->m_argv);
            THROW_IF_FAILED(SetEnvironmentVariablesOnWorkerProcess());
            context->m_hostFxr.Load(hostFxrResolutionResult->GetHostFxrLocation());
        }
        else
        {
            context->m_hostFxr.SetMain(s_fMainCallback);
        }

        // There can only ever be a single instance of .NET Core
        // loaded in the process but we need to get config information to boot it up in the
        // first place. This is happening in an execute request handler and everyone waits
        // until this initialization is done.
        // We set a static so that managed code can call back into this instance and
        // set the callbacks
        s_Application = this;

        if (m_pConfig->QuerySetCurrentDirectory())
        {
            auto dllDirectory = Environment::GetDllDirectoryValue();
            auto currentDirectory = Environment::GetCurrentDirectoryValue();

            LOG_INFOF(L"Initial Dll directory: '%s', current directory: '%s'", dllDirectory.c_str(), currentDirectory.c_str());

            // If DllDirectory wasn't set change it to previous current directory value
            if (dllDirectory.empty())
            {
                LOG_LAST_ERROR_IF(!SetDllDirectory(currentDirectory.c_str()));
                LOG_INFOF(L"Setting dll directory to %s", currentDirectory.c_str());
            }

            LOG_LAST_ERROR_IF(!SetCurrentDirectory(this->QueryApplicationPhysicalPath().c_str()));

            LOG_INFOF(L"Setting current directory to %s", this->QueryApplicationPhysicalPath().c_str());
        }

        auto startupReturnCode = context->m_hostFxr.InitializeForApp(context->m_argc, context->m_argv.get(), m_dotnetExeKnownLocation);
        if (startupReturnCode != 0)
        {
            throw InvalidOperationException(format(L"Error occurred when initializing in-process application, Return code: 0x%x", startupReturnCode));
        }

        if (m_pConfig->QueryCallStartupHook())
        {
            PWSTR startupHookValue = NULL;
            // Will get property not found if the environment variable isn't set.
            context->m_hostFxr.GetRuntimePropertyValue(DOTNETCORE_STARTUP_HOOK, &startupHookValue);

            if (startupHookValue == NULL)
            {
                RETURN_IF_NOT_ZERO(context->m_hostFxr.SetRuntimePropertyValue(DOTNETCORE_STARTUP_HOOK, ASPNETCORE_STARTUP_ASSEMBLY));
            }
            else
            {
                std::wstring startupHook(startupHookValue);
                startupHook.append(L";").append(ASPNETCORE_STARTUP_ASSEMBLY);
                RETURN_IF_NOT_ZERO(context->m_hostFxr.SetRuntimePropertyValue(DOTNETCORE_STARTUP_HOOK, startupHook.c_str()));
            }
        }

        RETURN_IF_NOT_ZERO(context->m_hostFxr.SetRuntimePropertyValue(DOTNETCORE_USE_ENTRYPOINT_FILTER, L"1"));
        RETURN_IF_NOT_ZERO(context->m_hostFxr.SetRuntimePropertyValue(DOTNETCORE_STACK_SIZE, m_pConfig->QueryStackSize().c_str()));

        bool clrThreadExited;
        {
            auto redirectionOutput = LoggingHelpers::CreateOutputs(
                    m_pConfig->QueryStdoutLogEnabled(),
                    m_pConfig->QueryStdoutLogFile(),
                    QueryApplicationPhysicalPath(),
                    m_stringRedirectionOutput
                );

            StandardStreamRedirection redirection(*redirectionOutput.get(), m_pHttpServer.IsCommandLineLaunch());

            context->m_redirectionOutput = redirectionOutput.get();

            //Start CLR thread
            m_clrThread = std::thread(ClrThreadEntryPoint, context);

            // Wait for thread exit or shutdown event
            const HANDLE waitHandles[2] = { m_pShutdownEvent, m_clrThread.native_handle() };

            // Wait for shutdown request
            const auto waitResult = WaitForMultipleObjects(2, waitHandles, FALSE, INFINITE);

            // Disconnect output
            context->m_redirectionOutput = nullptr;

            THROW_LAST_ERROR_IF(waitResult == WAIT_FAILED);

            LOG_INFOF(L"Starting shutdown sequence %d", waitResult);

            clrThreadExited = waitResult == (WAIT_OBJECT_0 + 1);
            // shutdown was signaled
            // only wait for shutdown in case of successful startup
            if (m_waitForShutdown)
            {
                const auto clrWaitResult = WaitForSingleObject(m_clrThread.native_handle(), m_pConfig->QueryShutdownTimeLimitInMS());
                THROW_LAST_ERROR_IF(clrWaitResult == WAIT_FAILED);

                clrThreadExited = clrWaitResult != WAIT_TIMEOUT;
            }
            LOG_INFOF(L"Clr thread wait ended: clrThreadExited: %d", clrThreadExited);
        }

        // At this point CLR thread either finished or timed out, abandon it.
        m_clrThread.detach();

        if (m_fStopCalled)
        {
            if (clrThreadExited)
            {
                EventLog::Info(
                    ASPNETCORE_EVENT_APP_SHUTDOWN_SUCCESSFUL,
                    ASPNETCORE_EVENT_APP_SHUTDOWN_SUCCESSFUL_MSG,
                    QueryConfigPath().c_str());
            }
            else
            {
                EventLog::Warn(
                    ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE,
                    ASPNETCORE_EVENT_APP_SHUTDOWN_FAILURE_MSG,
                    QueryConfigPath().c_str());
            }
        }
        else
        {
            if (clrThreadExited)
            {
                UnexpectedThreadExit(*context);
                // If the inprocess server was initialized, we need to cause recycle to be called on the worker process.
                // in case when it was not initialized we need to keep server running to serve 502 page
                if (m_Initialized)
                {
                    QueueStop();
                }
            }
        }
    }
    catch (InvalidOperationException& ex)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE,
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE_MSG,
            QueryApplicationId().c_str(),
            QueryApplicationPhysicalPath().c_str(),
            ex.as_wstring().c_str());

        OBSERVE_CAUGHT_EXCEPTION();
    }
    catch (std::runtime_error& ex)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE,
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE_MSG,
            QueryApplicationId().c_str(),
            QueryApplicationPhysicalPath().c_str(),
            GetUnexpectedExceptionMessage(ex).c_str());

        OBSERVE_CAUGHT_EXCEPTION();
    }
}

void IN_PROCESS_APPLICATION::QueueStop()
{
    if (m_fStopCalled)
    {
        return;
    }

    LOG_INFO(L"Queueing in-process stop thread");

    std::thread stoppingThread([](std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> application)
    {
        LOG_INFO(L"Starting in-process stop thread");
        application->Stop(false);
        LOG_INFO(L"Stopping in-process stop thread");
    }, ::ReferenceApplication(this));

    stoppingThread.detach();
}

HRESULT IN_PROCESS_APPLICATION::Start(
    IHttpServer& pServer,
    IHttpSite* pSite,
    IHttpApplication& pHttpApplication,
    APPLICATION_PARAMETER* pParameters,
    DWORD nParameters,
    std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER>& application,
    ErrorContext& errorContext)
{
    try
    {
        std::unique_ptr<InProcessOptions> options;
        THROW_IF_FAILED(InProcessOptions::Create(pServer, pSite, pHttpApplication, options));
        application = std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER>(
            new IN_PROCESS_APPLICATION(pServer, pHttpApplication, std::move(options), pParameters, nParameters));
        THROW_IF_FAILED(application->LoadManagedApplication(errorContext));
        return S_OK;
    }
    catch (InvalidOperationException& ex)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE,
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE_MSG,
            pHttpApplication.GetApplicationId(),
            pHttpApplication.GetApplicationPhysicalPath(),
            ex.as_wstring().c_str());

        RETURN_CAUGHT_EXCEPTION();
    }
    catch (std::runtime_error& ex)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE,
            ASPNETCORE_EVENT_LOAD_CLR_FAILURE_MSG,
            pHttpApplication.GetApplicationId(),
            pHttpApplication.GetApplicationPhysicalPath(),
            GetUnexpectedExceptionMessage(ex).c_str());

        RETURN_CAUGHT_EXCEPTION();
    }
    CATCH_RETURN();
}

// Required because __try and objects with destructors can not be mixed
void
IN_PROCESS_APPLICATION::ExecuteClr(const std::shared_ptr<ExecuteClrContext>& context)
{
    __try
    {
        auto const exitCode = context->m_hostFxr.Main(context->m_argc, context->m_argv.get());

        LOG_INFOF(L"Managed application exited with code %d", exitCode);

        context->m_exitCode = exitCode;
        context->m_hostFxr.Close();
    }
    __except(GetExceptionCode() != 0)
    {
        LOG_INFOF(L"Managed threw an exception %d", GetExceptionCode());

        context->m_exceptionCode = GetExceptionCode();
    }
}

//
// Calls hostfxr_main with the hostfxr and application as arguments.
// This method should not access IN_PROCESS_APPLICATION instance as it may be already freed
// in case of startup timeout
//
VOID
IN_PROCESS_APPLICATION::ClrThreadEntryPoint(const std::shared_ptr<ExecuteClrContext> &context)
{
    HandleWrapper<ModuleHandleTraits> moduleHandle;

    // Keep aspnetcorev2_inprocess.dll loaded while this thread is running
    // this is required because thread might be abandoned
    ModuleHelpers::IncrementCurrentModuleRefCount(moduleHandle);

    // Nested block is required here because FreeLibraryAndExitThread would prevent destructors from running
    // so we need to do in in a nested scope
    {
        // We use forwarder here instead of context->m_errorWriter itself to be able to
        // disconnect listener before CLR exits
        ForwardingRedirectionOutput redirectionForwarder(&context->m_redirectionOutput);
        const auto redirect = context->m_hostFxr.RedirectOutput(&redirectionForwarder);

        ExecuteClr(context);
    }
    FreeLibraryAndExitThread(moduleHandle.release(), 0);
}

HRESULT
IN_PROCESS_APPLICATION::SetEnvironmentVariablesOnWorkerProcess()
{
    auto variables = ENVIRONMENT_VAR_HELPERS::InitEnvironmentVariablesTable(
        m_pConfig->QueryEnvironmentVariables(),
        m_pConfig->QueryWindowsAuthEnabled(),
        m_pConfig->QueryBasicAuthEnabled(),
        m_pConfig->QueryAnonymousAuthEnabled(),
        false, // fAddHostingStartup
        QueryApplicationPhysicalPath().c_str(),
        nullptr);

    for (const auto & variable : variables)
    {
        LOG_INFOF(L"Setting environment variable %ls=%ls", variable.first.c_str(), variable.second.c_str());
        SetEnvironmentVariable(variable.first.c_str(), variable.second.c_str());
    }

    return S_OK;
}

VOID
IN_PROCESS_APPLICATION::UnexpectedThreadExit(const ExecuteClrContext& context) const
{
    auto content = m_stringRedirectionOutput->GetOutput();

    if (context.m_exceptionCode != 0)
    {
        if (!content.empty())
        {
            EventLog::Error(
                ASPNETCORE_EVENT_INPROCESS_THREAD_EXCEPTION,
                ASPNETCORE_EVENT_INPROCESS_THREAD_EXCEPTION_STDOUT_MSG,
                QueryApplicationId().c_str(),
                QueryApplicationPhysicalPath().c_str(),
                context.m_exceptionCode,
                content.c_str());
        }
        else
        {
            EventLog::Error(
                ASPNETCORE_EVENT_INPROCESS_THREAD_EXCEPTION,
                ASPNETCORE_EVENT_INPROCESS_THREAD_EXCEPTION_MSG,
                QueryApplicationId().c_str(),
                QueryApplicationPhysicalPath().c_str(),
                context.m_exceptionCode
                );
        }
        return;
    }

    //
    // Ungraceful shutdown, try to log an error message.
    // This will be a common place for errors as it means the hostfxr_main returned
    // or there was an exception.
    //

    if (!content.empty())
    {
        EventLog::Error(
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDOUT,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_STDOUT_MSG,
            QueryApplicationId().c_str(),
            QueryApplicationPhysicalPath().c_str(),
            context.m_exitCode,
            content.c_str());
    }
    else
    {
        EventLog::Error(
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT,
            ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG,
            QueryApplicationId().c_str(),
            QueryApplicationPhysicalPath().c_str(),
            context.m_exitCode);
    }
}

HRESULT
IN_PROCESS_APPLICATION::CreateHandler(
    _In_  IHttpContext       *pHttpContext,
    _Out_ IREQUEST_HANDLER  **pRequestHandler)
{
    try
    {
        SRWSharedLock dataLock(m_dataLock);

        DBG_ASSERT(!m_fStopCalled);
        m_requestCount++;

        LOG_TRACEF(L"Adding request. Total Request Count %d", m_requestCount.load());

        *pRequestHandler = new IN_PROCESS_HANDLER(::ReferenceApplication(this), pHttpContext, m_RequestHandler, m_RequestHandlerContext, m_DisconnectHandler, m_AsyncCompletionHandler);
    }
    CATCH_RETURN();

    return S_OK;
}

void
IN_PROCESS_APPLICATION::HandleRequestCompletion()
{
    SRWSharedLock dataLock(m_dataLock);

    auto requestCount = --m_requestCount;

    LOG_TRACEF(L"Removing Request %d", requestCount);

    if (m_fStopCalled && requestCount == 0 && !m_blockManagedCallbacks)
    {
        CallRequestsDrained();
    }
}

void IN_PROCESS_APPLICATION::CallRequestsDrained()
{
    // Atomic swap these.
    auto handler = m_RequestsDrainedHandler.exchange(nullptr);
    if (handler != nullptr)
    {
        LOG_INFO(L"Drained all requests, notifying managed.");
        handler(m_ShutdownHandlerContext);
    }
}
