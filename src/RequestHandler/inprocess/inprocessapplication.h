// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

typedef void(*request_handler_cb) (int error, IHttpContext* pHttpContext, void* pvCompletionContext);
typedef REQUEST_NOTIFICATION_STATUS(*PFN_REQUEST_HANDLER) (IN_PROCESS_HANDLER* pInProcessHandler, void* pvRequestHandlerContext);
typedef BOOL(*PFN_SHUTDOWN_HANDLER) (void* pvShutdownHandlerContext);
typedef REQUEST_NOTIFICATION_STATUS(*PFN_MANAGED_CONTEXT_HANDLER)(void *pvManagedHttpContext, HRESULT hrCompletionStatus, DWORD cbCompletion);
typedef DWORD(*hostfxr_main_fn) (CONST DWORD argc, CONST WCHAR* argv[]);

class IN_PROCESS_APPLICATION : public APPLICATION
{
public:
    IN_PROCESS_APPLICATION(IHttpServer* pHttpServer, ASPNETCORE_CONFIG  *pConfig);

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

    VOID
    Recycle(
        VOID
    );

    // Executes the .NET Core process
    HRESULT
    ExecuteApplication(
        VOID
    );

    HRESULT
    LoadManagedApplication(
        VOID
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

    static 
    INT
    FilterException(unsigned int code, struct _EXCEPTION_POINTERS *ep);

    HRESULT
    RunDotnetApplication(
        PCWSTR* argv,
        hostfxr_main_fn pProc
    );

    static
    IN_PROCESS_APPLICATION*
    GetInstance(
        VOID
    )
    {
        return s_Application;
    }

private:
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

    // The std log file handle
    HANDLE                          m_hLogFileHandle;
    STRU                            m_struLogFilePath;

    // The exit code of the .NET Core process
    INT                             m_ProcessExitCode;

    BOOL                            m_fManagedAppLoaded;
    BOOL                            m_fLoadManagedAppError;
    BOOL                            m_fInitialized;
    BOOL                            m_fIsWebSocketsConnection;
    BOOL                            m_fDoneStdRedirect;
    BOOL                            m_fRecycleProcessCalled;

    FILE*                           m_pStdFile;
    STTIMER                         m_Timer;
    SRWLOCK                         m_srwLock;

    static IN_PROCESS_APPLICATION*   s_Application;

    VOID
    SetStdOut(
        VOID
    );

    static
    VOID
    FindDotNetFolders(
        _In_ PCWSTR pszPath,
        _Out_ std::vector<std::wstring> *pvFolders
    );

    static
    HRESULT
    FindHighestDotNetVersion(
        _In_ std::vector<std::wstring> vFolders,
        _Out_ STRU *pstrResult
    );

    static
    BOOL
    DirectoryExists(
        _In_ STRU *pstrPath  //todo: this does not need to be stru, can be PCWSTR
    );

    static BOOL
    GetEnv(
        _In_ PCWSTR pszEnvironmentVariable,
        _Out_ STRU *pstrResult
    );

    static
    VOID
    ExecuteAspNetCoreProcess(
        _In_ LPVOID pContext
    );
};