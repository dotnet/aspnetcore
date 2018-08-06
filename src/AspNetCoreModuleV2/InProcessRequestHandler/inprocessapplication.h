// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "InProcessApplicationBase.h"
#include "requesthandler_config.h"
#include "IOutputManager.h"

class IN_PROCESS_HANDLER;
typedef REQUEST_NOTIFICATION_STATUS(WINAPI * PFN_REQUEST_HANDLER) (IN_PROCESS_HANDLER* pInProcessHandler, void* pvRequestHandlerContext);
typedef BOOL(WINAPI * PFN_SHUTDOWN_HANDLER) (void* pvShutdownHandlerContext);
typedef REQUEST_NOTIFICATION_STATUS(WINAPI * PFN_ASYNC_COMPLETION_HANDLER)(void *pvManagedHttpContext, HRESULT hrCompletionStatus, DWORD cbCompletion);

class IN_PROCESS_APPLICATION : public InProcessApplicationBase
{
public:
    IN_PROCESS_APPLICATION(
        IHttpServer& pHttpServer,
        IHttpApplication& pApplication,
        std::unique_ptr<REQUESTHANDLER_CONFIG> pConfig,
        APPLICATION_PARAMETER *pParameters,
        DWORD                  nParameters);

    ~IN_PROCESS_APPLICATION();

    __override
    VOID
    StopInternal(bool fServerInitiated) override;

    VOID
    SetCallbackHandles(
        _In_ PFN_REQUEST_HANDLER request_callback,
        _In_ PFN_SHUTDOWN_HANDLER shutdown_callback,
        _In_ PFN_ASYNC_COMPLETION_HANDLER managed_context_callback,
        _In_ VOID* pvRequstHandlerContext,
        _In_ VOID* pvShutdownHandlerContext
    );

    __override
    HRESULT
    CreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _Out_ IREQUEST_HANDLER   **pRequestHandler)
    override;

    // Executes the .NET Core process
    HRESULT
    ExecuteApplication(
        VOID
    );

    HRESULT
    LoadManagedApplication(
        VOID
    );

    VOID
    LogErrorsOnMainExit(
        HRESULT hr
    );

    VOID
    StopCallsIntoManaged(
        VOID
    )
    {
        m_fBlockCallbacksIntoManaged = TRUE;
    }

    VOID
    StopIncomingRequests(
        VOID
    )
    {
        m_fShutdownCalledFromManaged = TRUE;
    }

    static
    VOID SetMainCallback(hostfxr_main_fn mainCallback)
    {
        s_fMainCallback = mainCallback;
    }

    static
    IN_PROCESS_APPLICATION*
    GetInstance(
        VOID
    )
    {
        return s_Application;
    }

    PCWSTR
    QueryExeLocation()
    {
        return m_struExeLocation.QueryStr();
    }

    REQUESTHANDLER_CONFIG*
    QueryConfig()
    {
        return m_pConfig.get();
    }

    bool
    QueryBlockCallbacksIntoManaged() const
    {
        return m_fBlockCallbacksIntoManaged;
    }

private:

    enum MANAGED_APPLICATION_STATUS
    {
        UNKNOWN = 0,
        STARTING,
        RUNNING_MANAGED,
        SHUTDOWN,
        FAIL
    };

    // Thread executing the .NET Core process
    HANDLE                          m_hThread;

    // The request handler callback from managed code
    PFN_REQUEST_HANDLER             m_RequestHandler;
    VOID*                           m_RequestHandlerContext;

    // The shutdown handler callback from managed code
    PFN_SHUTDOWN_HANDLER            m_ShutdownHandler;
    VOID*                           m_ShutdownHandlerContext;

    PFN_ASYNC_COMPLETION_HANDLER    m_AsyncCompletionHandler;

    // The event that gets triggered when managed initialization is complete
    HANDLE                          m_pInitalizeEvent;

    STRU                            m_struExeLocation;

    // The exit code of the .NET Core process
    INT                             m_ProcessExitCode;

    volatile BOOL                   m_fBlockCallbacksIntoManaged;
    volatile BOOL                   m_fShutdownCalledFromNative;
    volatile BOOL                   m_fShutdownCalledFromManaged;
    BOOL                            m_fInitialized;
    MANAGED_APPLICATION_STATUS      m_status;
    std::unique_ptr<REQUESTHANDLER_CONFIG> m_pConfig;

    static IN_PROCESS_APPLICATION*  s_Application;

    IOutputManager*                 m_pLoggerProvider;

    static const LPCSTR             s_exeLocationParameterName;

    static
    VOID
    ExecuteAspNetCoreProcess(
        _In_ LPVOID pContext
    );

    HRESULT
    SetEnvironementVariablesOnWorkerProcess(
        VOID
    );

    HRESULT
    RunDotnetApplication(
        DWORD argc,
        CONST PCWSTR* argv,
        hostfxr_main_fn pProc
    );

    static
    DWORD WINAPI
    DoShutDown(
        LPVOID lpParam
    );

    VOID
    ShutDownInternal(
        VOID
    );
};
