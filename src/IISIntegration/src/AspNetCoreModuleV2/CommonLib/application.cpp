// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

APPLICATION::APPLICATION(
    _In_ IHttpServer* pHttpServer,
    _In_ ASPNETCORE_CONFIG* pConfig) :
    m_cRefs(1),
    m_pConfig(pConfig),
    m_status(APPLICATION_STATUS::UNKNOWN)
{
    UNREFERENCED_PARAMETER(pHttpServer);
}

APPLICATION::~APPLICATION()
{
}

APPLICATION_STATUS
APPLICATION::QueryStatus()
{
    return m_status;
}

ASPNETCORE_CONFIG*
APPLICATION::QueryConfig()
{
    return m_pConfig;
}

VOID
APPLICATION::ReferenceApplication()
const
{
    InterlockedIncrement(&m_cRefs);
}

VOID
APPLICATION::DereferenceApplication()
const
{
    DBG_ASSERT(m_cRefs != 0);

    LONG cRefs = 0;
    if ((cRefs = InterlockedDecrement(&m_cRefs)) == 0)
    {
        delete this;
    }
}
