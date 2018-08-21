// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <windows.h>
#include <httpserv.h>

#include "stringu.h"

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

    STRU*
    QueryProcessPath()
    {
        return &m_struProcessPath;
    }

    STRU*
    QueryArguments()
    {
        return &m_struArguments;
    }

    APP_HOSTING_MODEL
    QueryHostingModel()
    {
        return m_hostingModel;
    }

    STRU*
    QueryHandlerVersion()
    {
        return &m_struHandlerVersion;
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

    STRU                   m_struArguments;
    STRU                   m_struProcessPath;
    APP_HOSTING_MODEL      m_hostingModel;
    STRU                   m_struHandlerVersion;
    BOOL                   m_fStdoutLogEnabled;
    STRU                   m_struStdoutLogFile;
};
