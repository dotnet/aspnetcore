// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#define CS_ROOTWEB_CONFIG                                L"MACHINE/WEBROOT/APPHOST/"
#define CS_ROOTWEB_CONFIG_LEN                            _countof(CS_ROOTWEB_CONFIG)-1
#define CS_ASPNETCORE_SECTION                            L"system.webServer/aspNetCore"
#define CS_WINDOWS_AUTHENTICATION_SECTION                L"system.webServer/security/authentication/windowsAuthentication"
#define CS_BASIC_AUTHENTICATION_SECTION                  L"system.webServer/security/authentication/basicAuthentication"
#define CS_ANONYMOUS_AUTHENTICATION_SECTION              L"system.webServer/security/authentication/anonymousAuthentication"
#define CS_AUTHENTICATION_ENABLED                        L"enabled"
#define CS_ASPNETCORE_PROCESS_EXE_PATH                   L"processPath"
#define CS_ASPNETCORE_PROCESS_ARGUMENTS                  L"arguments"
#define CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT         L"startupTimeLimit"
#define CS_ASPNETCORE_PROCESS_SHUTDOWN_TIME_LIMIT        L"shutdownTimeLimit"
#define CS_ASPNETCORE_WINHTTP_REQUEST_TIMEOUT            L"requestTimeout"
#define CS_ASPNETCORE_RAPID_FAILS_PER_MINUTE             L"rapidFailsPerMinute"
#define CS_ASPNETCORE_STDOUT_LOG_ENABLED                 L"stdoutLogEnabled"
#define CS_ASPNETCORE_STDOUT_LOG_FILE                    L"stdoutLogFile"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLES              L"environmentVariables"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLE               L"environmentVariable"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLE_NAME          L"name"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLE_VALUE         L"value"
#define CS_ASPNETCORE_PROCESSES_PER_APPLICATION          L"processesPerApplication"
#define CS_ASPNETCORE_FORWARD_WINDOWS_AUTH_TOKEN         L"forwardWindowsAuthToken"
#define CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE        L"disableStartUpErrorPage"
#define CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE             L"recycleOnFileChange"
#define CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE_FILE        L"file"
#define CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE_FILE_PATH   L"path"
#define CS_ASPNETCORE_HOSTING_MODEL                      L"hostingModel"

#define MAX_RAPID_FAILS_PER_MINUTE 100
#define MILLISECONDS_IN_ONE_SECOND 1000
#define MIN_PORT                   1025
#define MAX_PORT                   48000

#define TIMESPAN_IN_MILLISECONDS(x)  ((x)/((LONGLONG)(10000)))
#define TIMESPAN_IN_SECONDS(x)       ((TIMESPAN_IN_MILLISECONDS(x))/((LONGLONG)(1000)))
#define TIMESPAN_IN_MINUTES(x)       ((TIMESPAN_IN_SECONDS(x))/((LONGLONG)(60)))

//#define HEX_TO_ASCII(c) ((CHAR)(((c) < 10) ? ((c) + '0') : ((c) + 'a' - 10)))

#include "stdafx.h"

enum APP_HOSTING_MODEL
{
    HOSTING_UNKNOWN = 0,
    HOSTING_IN_PROCESS,
    HOSTING_OUT_PROCESS
};

class ASPNETCORE_CONFIG : IHttpStoredContext
{
public:

    virtual
    ~ASPNETCORE_CONFIG();

    VOID
    CleanupStoredContext()
    {
        DereferenceConfiguration();
    }

    static
    HRESULT
    GetConfig(
        _In_  IHttpServer             *pHttpServer,
        _In_  HTTP_MODULE_ID           pModuleId,
        _In_  IHttpContext            *pHttpContext,
        _Out_ ASPNETCORE_CONFIG       **ppAspNetCoreConfig
    );

    ENVIRONMENT_VAR_HASH*
    QueryEnvironmentVariables(
        VOID
    )
    {
        return m_pEnvironmentVariables;
    }

    DWORD
    QueryRapidFailsPerMinute(
        VOID
    )
    {
        return m_dwRapidFailsPerMinute;
    }

    DWORD
    QueryStartupTimeLimitInMS(
        VOID
    )
    {
        return m_dwStartupTimeLimitInMS;
    }

    DWORD
    QueryShutdownTimeLimitInMS(
        VOID
    )
    {
        return m_dwShutdownTimeLimitInMS;
    }

    DWORD
    QueryProcessesPerApplication(
        VOID
    )
    {
        return m_dwProcessesPerApplication;
    }

    DWORD
    QueryRequestTimeoutInMS(
        VOID
    )
    {
        return m_dwRequestTimeoutInMS;
    }

    STRU*
    QueryArguments(
        VOID
    )
    {
        return &m_struArguments;
    }

    STRU*
    QueryApplicationPath(
        VOID
    )
    {
        return &m_struApplication;
    }

    STRU*
    QueryApplicationPhysicalPath(
        VOID
    )
    {
        return &m_struApplicationPhysicalPath;
    }

    STRU*
    QueryApplicationVirtualPath(
        VOID
    )
    {
        return &m_struApplicationVirtualPath;
    }

    STRU*
    QueryProcessPath(
            VOID
        )
    {
        return &m_struProcessPath;
    }

    APP_HOSTING_MODEL
    QueryHostingModel(
        VOID
    )
    {
        return m_hostingModel;
    }

    BOOL
        QueryStdoutLogEnabled()
    {
        return m_fStdoutLogEnabled;
    }

    BOOL
    QueryForwardWindowsAuthToken()
    {
        return m_fForwardWindowsAuthToken;
    }

    BOOL
    QueryWindowsAuthEnabled()
    {
        return m_fWindowsAuthEnabled;
    }

    BOOL
    QueryBasicAuthEnabled()
    {
        return m_fBasicAuthEnabled;
    }

    BOOL
    QueryAnonymousAuthEnabled()
    {
        return m_fAnonymousAuthEnabled;
    }

    BOOL
    QueryDisableStartUpErrorPage()
    {
        return m_fDisableStartUpErrorPage;
    }

    STRU*
    QueryStdoutLogFile()
    {
        return &m_struStdoutLogFile;
    }

    STRU*
    QueryConfigPath()
    {
        return &m_struConfigPath;
    }

    CONST
    PCWSTR*
    QueryHostFxrArguments(
        VOID
    )
    {
        return m_ppStrArguments;
    }

    CONST
    DWORD
    QueryHostFxrArgCount(
        VOID
    )
    {
        return m_dwArgc;
    }

    CONST
    PCWSTR
    QueryHostFxrFullPath(
        VOID
    )
    {
        return m_struHostFxrLocation.QueryStr();
    }

    HRESULT
    SetHostFxrFullPath(
        PCWSTR pStrHostFxrFullPath
    )
    {
        return m_struHostFxrLocation.Copy(pStrHostFxrFullPath);
    }

    VOID
    SetHostFxrArguments(
        DWORD dwArgc,
        PCWSTR* ppStrArguments
    )
    {
        if (m_ppStrArguments != NULL)
        {
            delete[] m_ppStrArguments;
        }

        m_dwArgc = dwArgc;
        m_ppStrArguments = ppStrArguments;
    }

    VOID
    ReferenceConfiguration(
        VOID
    ) const;

    VOID
    DereferenceConfiguration(
        VOID
    ) const;

private:

    //
    // private constructor
    //    
    ASPNETCORE_CONFIG():
        m_fStdoutLogEnabled( FALSE ),
        m_pEnvironmentVariables( NULL ),
        m_cRefs( 1 ),
        m_hostingModel( HOSTING_UNKNOWN ),
        m_ppStrArguments(NULL)
    {
    }

    HRESULT
    Populate(
        IHttpServer  *pHttpServer,
        IHttpContext *pHttpContext
    );

    mutable LONG           m_cRefs;

    DWORD                  m_dwRequestTimeoutInMS;
    DWORD                  m_dwStartupTimeLimitInMS;
    DWORD                  m_dwShutdownTimeLimitInMS;
    DWORD                  m_dwRapidFailsPerMinute;
    DWORD                  m_dwProcessesPerApplication;
    STRU                   m_struArguments;
    STRU                   m_struProcessPath;
    STRU                   m_struStdoutLogFile;
    STRU                   m_struApplication;
    STRU                   m_struApplicationPhysicalPath;
    STRU                   m_struApplicationVirtualPath;
    STRU                   m_struConfigPath;
    BOOL                   m_fStdoutLogEnabled;
    BOOL                   m_fForwardWindowsAuthToken;
    BOOL                   m_fDisableStartUpErrorPage;
    BOOL                   m_fWindowsAuthEnabled;
    BOOL                   m_fBasicAuthEnabled;
    BOOL                   m_fAnonymousAuthEnabled;
    BOOL                   m_fIsStandAloneApplication;
    APP_HOSTING_MODEL      m_hostingModel;
    ENVIRONMENT_VAR_HASH*  m_pEnvironmentVariables;
    STRU                   m_struHostFxrLocation;
    PCWSTR*                m_ppStrArguments;
    DWORD                  m_dwArgc;
};
