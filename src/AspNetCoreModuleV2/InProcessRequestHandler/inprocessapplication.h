// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"
#include "InProcessApplicationBase.h"
#include "inprocesshandler.h"
#include "requesthandler_config.h"
#include "IOutputManager.h"

typedef REQUEST_NOTIFICATION_STATUS(WINAPI * PFN_REQUEST_HANDLER) (IN_PROCESS_HANDLER* pInProcessHandler, void* pvRequestHandlerContext);
typedef BOOL(WINAPI * PFN_SHUTDOWN_HANDLER) (void* pvShutdownHandlerContext);
typedef REQUEST_NOTIFICATION_STATUS(WINAPI * PFN_MANAGED_CONTEXT_HANDLER)(void *pvManagedHttpContext, HRESULT hrCompletionStatus, DWORD cbCompletion);

class IN_PROCESS_APPLICATION : public InProcessApplicationBase
{
public:
    IN_PROCESS_APPLICATION(
        IHttpServer* pHttpServer,
        std::unique_ptr<REQUESTHANDLER_CONFIG> pConfig);

    ~IN_PROCESS_APPLICATION();

    __override
    VOID
    ShutDown();

    VOID
    SetCallbackHandles(
        _In_ PFN_REQUEST_HANDLER request_callback,
        _In_ PFN_SHUTDOWN_HANDLER shutdown_callback,
        _In_ PFN_MANAGED_CONTEXT_HANDLER managed_context_callback,
        _In_ VOID* pvRequstHandlerContext,
        _In_ VOID* pvShutdownHandlerContext
    );

    __override
    HRESULT
    CreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _Out_ IREQUEST_HANDLER   **pRequestHandler)
    override;

    VOID
    SetParameter(
        _In_ LPCWSTR           pzName,
        _In_ LPCWSTR           pzValue)
    override
    {
        const auto exeLocationParameterName = L"InProcessExeLocation";
        if (_wcsicmp(pzName, exeLocationParameterName) == 0)
        {
            m_struExeLocation.Copy(pzValue);
        }
    }

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

    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        DWORD                   cbCompletion,
        HRESULT                 hrCompletionStatus,
        IN_PROCESS_HANDLER*     pInProcessHandler
    );

    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequest
    (
        IHttpContext* pHttpContext,
        IN_PROCESS_HANDLER* pInProcessHandler
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

    REQUESTHANDLER_CONFIG*
    QueryConfig() const;

    PCWSTR
    QueryExeLocation()
    {
        return m_struExeLocation.QueryStr();
    }

private:
    IHttpServer* const      m_pHttpServer;

    // Thread executing the .NET Core process
    HANDLE                          m_hThread;

    // The request handler callback from managed code
    PFN_REQUEST_HANDLER             m_RequestHandler;
    VOID*                           m_RequestHandlerContext;

    // The shutdown handler callback from managed code
    PFN_SHUTDOWN_HANDLER            m_ShutdownHandler;
    VOID*                           m_ShutdownHandlerContext;

    PFN_MANAGED_CONTEXT_HANDLER     m_AsyncCompletionHandler;

    // The event that gets triggered when managed initialization is complete
    HANDLE                          m_pInitalizeEvent;

    STRU                            m_struExeLocation;

    // The exit code of the .NET Core process
    INT                             m_ProcessExitCode;

    BOOL                            m_fIsWebSocketsConnection;
    volatile BOOL                   m_fBlockCallbacksIntoManaged;
    volatile BOOL                   m_fShutdownCalledFromNative;
    volatile BOOL                   m_fShutdownCalledFromManaged;
    BOOL                            m_fRecycleCalled;
    BOOL                            m_fInitialized;


    static IN_PROCESS_APPLICATION*  s_Application;

    IOutputManager*                 m_pLoggerProvider;
    std::unique_ptr<REQUESTHANDLER_CONFIG>          m_pConfig;



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
    DWORD
    DoShutDown(
        LPVOID lpParam
    );

    VOID
    ShutDownInternal(
        VOID
    );
};
