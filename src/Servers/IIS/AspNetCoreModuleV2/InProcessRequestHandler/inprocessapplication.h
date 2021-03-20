// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <thread>
#include "InProcessApplicationBase.h"
#include "InProcessOptions.h"
#include "HostFxr.h"

class IN_PROCESS_HANDLER;
typedef REQUEST_NOTIFICATION_STATUS(WINAPI * PFN_REQUEST_HANDLER) (IN_PROCESS_HANDLER* pInProcessHandler, void* pvRequestHandlerContext);
typedef VOID(WINAPI * PFN_DISCONNECT_HANDLER) (void *pvManagedHttpContext);
typedef BOOL(WINAPI * PFN_SHUTDOWN_HANDLER) (void* pvShutdownHandlerContext);
typedef REQUEST_NOTIFICATION_STATUS(WINAPI * PFN_ASYNC_COMPLETION_HANDLER)(void *pvManagedHttpContext, HRESULT hrCompletionStatus, DWORD cbCompletion);
typedef void(WINAPI * PFN_REQUESTS_DRAINED_HANDLER) (void* pvShutdownHandlerContext);

class IN_PROCESS_APPLICATION : public InProcessApplicationBase
{
public:
    IN_PROCESS_APPLICATION(
        IHttpServer& pHttpServer,
        IHttpApplication& pApplication,
        std::unique_ptr<InProcessOptions> pConfig,
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
        _In_ PFN_DISCONNECT_HANDLER disconnect_callback,
        _In_ PFN_ASYNC_COMPLETION_HANDLER managed_context_callback,
        _In_ PFN_REQUESTS_DRAINED_HANDLER requestsDrainedHandler,
        _In_ VOID* pvRequstHandlerContext,
        _In_ VOID* pvShutdownHandlerContext
    );

    __override
    HRESULT
    CreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _Out_ IREQUEST_HANDLER   **pRequestHandler)
    override;

    void HandleRequestCompletion();

    // Executes the .NET Core process
    void
    ExecuteApplication();

    HRESULT
    LoadManagedApplication(ErrorContext& errorContext);

    void
    QueueStop();

    void
    StopIncomingRequests()
    {
        QueueStop();

        LOG_INFOF(L"Waiting for %d requests to drain", m_requestCount.load());
    }

    void
    StopCallsIntoManaged()
    {
        m_blockManagedCallbacks = true;
    }

    static
    VOID SetMainCallback(hostfxr_main_fn mainCallback)
    {
        s_fMainCallback = mainCallback;
    }

    static
    IN_PROCESS_APPLICATION*
    GetInstance()
    {
        return s_Application;
    }

    const std::wstring&
    QueryExeLocation() const
    {
        return m_dotnetExeKnownLocation;
    }

    const InProcessOptions&
    QueryConfig() const
    {
        return *m_pConfig;
    }

    bool
    QueryBlockCallbacksIntoManaged() const
    {
        return m_blockManagedCallbacks;
    }

    static
    HRESULT Start(
        IHttpServer& pServer,
        IHttpSite* pSite,
        IHttpApplication& pHttpApplication,
        APPLICATION_PARAMETER* pParameters,
        DWORD nParameters,
        std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER>& application,
        ErrorContext& errorContext);

private:
    struct ExecuteClrContext: std::enable_shared_from_this<ExecuteClrContext>
    {
        ExecuteClrContext():
            m_argc(0),
            m_hostFxr(nullptr, nullptr, nullptr),
            m_exitCode(0),
            m_exceptionCode(0)
        {
        }

        ~ExecuteClrContext()
        {
            m_redirectionOutput = nullptr;
        }

        DWORD m_argc;
        std::unique_ptr<PCWSTR[]> m_argv;

        HostFxr m_hostFxr;
        RedirectionOutput* m_redirectionOutput;
        int m_exitCode;
        int m_exceptionCode;
    };

    // Thread executing the .NET Core process this might be abandoned in timeout cases
    std::thread                     m_clrThread;
    // Thread tracking the CLR thread, this one is always joined on shutdown
    std::thread                     m_workerThread;
    // Thread for cleaning up existing shadow copy folders
    std::thread                     m_folderCleanupThread;
    // The event that gets triggered when managed initialization is complete
    HandleWrapper<NullHandleTraits> m_pInitializeEvent;
    // The event that gets triggered when worker thread should exit
    HandleWrapper<NullHandleTraits> m_pShutdownEvent;

    // The request handler callback from managed code
    PFN_REQUEST_HANDLER             m_RequestHandler;
    VOID*                           m_RequestHandlerContext;

    // The shutdown handler callback from managed code
    PFN_SHUTDOWN_HANDLER            m_ShutdownHandler;
    VOID*                           m_ShutdownHandlerContext;

    PFN_ASYNC_COMPLETION_HANDLER    m_AsyncCompletionHandler;
    PFN_DISCONNECT_HANDLER          m_DisconnectHandler;
    std::atomic<PFN_REQUESTS_DRAINED_HANDLER>    m_RequestsDrainedHandler;

    std::wstring                    m_dotnetExeKnownLocation;

    std::atomic_bool                m_blockManagedCallbacks;
    bool                            m_Initialized;
    bool                            m_waitForShutdown;

    std::atomic<int>                m_requestCount;

    std::unique_ptr<InProcessOptions> m_pConfig;

    static IN_PROCESS_APPLICATION*  s_Application;

    std::shared_ptr<StringStreamRedirectionOutput> m_stringRedirectionOutput;

    inline static const LPCSTR      s_exeLocationParameterName = "InProcessExeLocation";
    inline static const LPCSTR      s_shadowCopyDirectoryName = "ShadowCopyDirectory";

    VOID
    UnexpectedThreadExit(const ExecuteClrContext& context) const;

    HRESULT
    SetEnvironmentVariablesOnWorkerProcess();

    void
    StopClr();

    void
    CallRequestsDrained();

    static
    void
    ClrThreadEntryPoint(const std::shared_ptr<ExecuteClrContext> &context);

    static
    void
    ExecuteClr(const std::shared_ptr<ExecuteClrContext> &context);

    // Allows to override call to hostfxr_main with custom callback
    // used in testing
    inline static hostfxr_main_fn  s_fMainCallback = nullptr;
};
