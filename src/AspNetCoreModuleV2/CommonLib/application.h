// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

enum APPLICATION_STATUS
{
    UNKNOWN = 0,
    STARTING,
    RUNNING,
    SHUTDOWN,
    FAIL
};

class  ASPNETCORE_CONFIG;

class APPLICATION
{
public:
    APPLICATION(
        _In_ IHttpServer* pHttpServer,
        _In_ ASPNETCORE_CONFIG* pConfig);

    virtual
    VOID
    ShutDown() = 0;

    virtual
    VOID
    Recycle() = 0;

    virtual
    ~APPLICATION();

    APPLICATION_STATUS
    QueryStatus();

    ASPNETCORE_CONFIG*
    QueryConfig();

    VOID
    ReferenceApplication()
    const;

    VOID
    DereferenceApplication()
    const;

protected:
    mutable LONG            m_cRefs;
    volatile APPLICATION_STATUS m_status;
    ASPNETCORE_CONFIG*      m_pConfig;
};
