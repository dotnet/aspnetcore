// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

typedef void(*request_handler_cb) (int error, IHttpContext* pHttpContext, void* pvCompletionContext);
typedef REQUEST_NOTIFICATION_STATUS(*PFN_REQUEST_HANDLER) (IHttpContext* pHttpContext, void* pvRequstHandlerContext);
typedef BOOL(*PFN_SHUTDOWN_HANDLER) (void* pvShutdownHandlerContext);

class ASPNETCORE_APPLICATION
{
public:

    ASPNETCORE_APPLICATION():
        m_pConfiguration(NULL),
        m_RequestHandler(NULL)
    {
    }

    ~ASPNETCORE_APPLICATION()
    {
        if (m_hThread != NULL)
        {
            CloseHandle(m_hThread);
            m_hThread = NULL;
        }

        if (m_pInitalizeEvent != NULL)
        {
            CloseHandle(m_pInitalizeEvent);
            m_pInitalizeEvent = NULL;
        }
    }

    HRESULT
    Initialize(
        _In_ ASPNETCORE_CONFIG* pConfig
    );

    REQUEST_NOTIFICATION_STATUS
    ExecuteRequest(
        _In_ IHttpContext* pHttpContext
    );

    VOID
    Shutdown(
        VOID
    );

    VOID
    SetCallbackHandles(
        _In_ PFN_REQUEST_HANDLER request_callback,
        _In_ PFN_SHUTDOWN_HANDLER shutdown_callback,
        _In_ VOID* pvRequstHandlerContext,
        _In_ VOID* pvShutdownHandlerContext
    );

    // Executes the .NET Core process
    HRESULT
    ExecuteApplication(
        VOID
    );

    ASPNETCORE_CONFIG*
    GetConfig(
        VOID
    )
    {
		return m_pConfiguration;
	}

    static
    ASPNETCORE_APPLICATION*
    GetInstance(
        VOID
    )
    {
        return s_Application;
    }

private:
    // Thread executing the .NET Core process
    HANDLE                          m_hThread;

    // Configuration for this application
    ASPNETCORE_CONFIG*              m_pConfiguration;

    // The request handler callback from managed code
    PFN_REQUEST_HANDLER             m_RequestHandler;
    VOID*                           m_RequstHandlerContext;

    // The shutdown handler callback from managed code
    PFN_SHUTDOWN_HANDLER            m_ShutdownHandler;
    VOID*                           m_ShutdownHandlerContext;

    // The event that gets triggered when managed initialization is complete
    HANDLE                          m_pInitalizeEvent;

    // The exit code of the .NET Core process
    INT                             m_ProcessExitCode;

    static ASPNETCORE_APPLICATION*  s_Application;

    static VOID
        FindDotNetFolders(
            _In_ STRU *pstrPath,
            _Out_ std::vector<std::wstring> *pvFolders
        );

    static HRESULT
        FindHighestDotNetVersion(
            _In_ std::vector<std::wstring> vFolders,
            _Out_ STRU *pstrResult
        );

    static BOOL
        DirectoryExists(
            _In_ STRU *pstrPath
        );

    static BOOL
        GetEnv(
            _In_ PCWSTR pszEnvironmentVariable,
            _Out_ STRU *pstrResult
        );
};

