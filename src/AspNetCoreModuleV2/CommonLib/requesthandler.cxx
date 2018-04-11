// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

REQUEST_HANDLER::REQUEST_HANDLER(
    _In_ IHttpContext *pW3Context,
    _In_ HTTP_MODULE_ID  *pModuleId,
    _In_ APPLICATION  *pApplication)
    : m_cRefs(1)
{
    m_pW3Context = pW3Context;
    m_pApplication = pApplication;
    m_pModuleId = *pModuleId;
}


REQUEST_HANDLER::~REQUEST_HANDLER()
{
}

VOID
REQUEST_HANDLER::ReferenceRequestHandler(
    VOID
) const
{
    InterlockedIncrement(&m_cRefs);
}


VOID
REQUEST_HANDLER::DereferenceRequestHandler(
    VOID
) const
{
    DBG_ASSERT(m_cRefs != 0);

    LONG cRefs = 0;
    if ((cRefs = InterlockedDecrement(&m_cRefs)) == 0)
    {
        delete this;
    }

}
