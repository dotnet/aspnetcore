// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include <windows.h>
#include <httpserv.h>

#define CS_ASPNETCORE_SECTION                            L"system.webServer/aspNetCore"
#define CS_ASPNETCORE_PROCESS_EXE_PATH                   L"processPath"
#define CS_ASPNETCORE_PROCESS_ARGUMENTS                  L"arguments"
#define CS_ASPNETCORE_HOSTING_MODEL                      L"hostingModel"
#define CS_ASPNETCORE_STDOUT_LOG_ENABLED                 L"stdoutLogEnabled"
#define CS_ASPNETCORE_STDOUT_LOG_FILE                    L"stdoutLogFile"

enum APP_HOSTING_MODEL
{
    HOSTING_UNKNOWN = 0,
    HOSTING_IN_PROCESS,
    HOSTING_OUT_PROCESS
};

class ASPNETCORE_SHIM_CONFIG
{
public:
    virtual
    ~ASPNETCORE_SHIM_CONFIG() = default;

    HRESULT
    Populate(
        IHttpServer         *pHttpServer,
        IHttpApplication    *pHttpApplication
    );

    std::wstring&
    QueryProcessPath()
    {
        return m_strProcessPath;
    }

    std::wstring&
    QueryArguments()
    {
        return m_strArguments;
    }

    APP_HOSTING_MODEL
    QueryHostingModel()
    {
        return m_hostingModel;
    }

    std::wstring&
    QueryHandlerVersion()
    {
        return m_strHandlerVersion;
    }

    BOOL
    QueryStdoutLogEnabled()
    {
        return m_fStdoutLogEnabled;
    }

    STRU*
    QueryStdoutLogFile()
    {
        return &m_struStdoutLogFile;
    }

    ASPNETCORE_SHIM_CONFIG() :
        m_hostingModel(HOSTING_UNKNOWN)
    {
    }

private:

    std::wstring                   m_strArguments;
    std::wstring                   m_strProcessPath;
    APP_HOSTING_MODEL              m_hostingModel;
    std::wstring                   m_strHandlerVersion;
    BOOL                   m_fStdoutLogEnabled;
    STRU                   m_struStdoutLogFile;
};
