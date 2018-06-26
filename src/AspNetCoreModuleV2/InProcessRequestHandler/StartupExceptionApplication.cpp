// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "StartupExceptionApplication.h"

VOID StartupExceptionApplication::ShutDown()
{
    exit(0);
}

HRESULT StartupExceptionApplication::CreateHandler(IHttpContext *pContext, IREQUEST_HANDLER ** pRequestHandler)
{
    *pRequestHandler = new StartupExceptionHandler(pContext, m_disableLogs, this);
    return S_OK;
}
