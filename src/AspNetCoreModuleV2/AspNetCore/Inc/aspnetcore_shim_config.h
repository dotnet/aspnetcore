// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"
#include <map>

#define CS_ASPNETCORE_SECTION                            L"system.webServer/aspNetCore"
#define CS_ASPNETCORE_PROCESS_EXE_PATH                   L"processPath"
#define CS_ASPNETCORE_PROCESS_ARGUMENTS                  L"arguments"
#define CS_ASPNETCORE_HOSTING_MODEL                      L"hostingModel"

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
    ~ASPNETCORE_SHIM_CONFIG();

    HRESULT
    Populate(
        IHttpServer         *pHttpServer,
        IHttpApplication    *pHttpContext
    );

    STRU*
    QueryApplicationPhysicalPath(
        VOID
    )
    {
        return &m_struApplicationPhysicalPath;
    }

    STRU*
    QueryApplicationPath(
        VOID
    )
    {
        return &m_struApplication;
    }

    STRU*
    QueryConfigPath(
        VOID
    )
    {
        return &m_struConfigPath;
    }

    STRU*
    QueryProcessPath(
        VOID
    )
    {
        return &m_struProcessPath;
    }

    STRU*
    QueryArguments(
        VOID
    )
    {
        return &m_struArguments;
    }

    APP_HOSTING_MODEL
    QueryHostingModel(
        VOID
    )
    {
        return m_hostingModel;
    }

    STRU*
    QueryHandlerVersion(
        VOID
    )
    {
        return &m_struHandlerVersion;
    }

    ASPNETCORE_SHIM_CONFIG() :
        m_hostingModel(HOSTING_UNKNOWN)
    {
    }

private:

    STRU                   m_struArguments;
    STRU                   m_struProcessPath;
    STRU                   m_struApplication;
    STRU                   m_struApplicationPhysicalPath;
    STRU                   m_struConfigPath;
    APP_HOSTING_MODEL      m_hostingModel;
    STRU                   m_struHostFxrLocation;
    STRU                   m_struHandlerVersion;
};

