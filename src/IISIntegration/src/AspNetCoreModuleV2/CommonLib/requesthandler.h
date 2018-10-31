// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include "application.h"

//
// Abstract class
//
class REQUEST_HANDLER
{
public:
    REQUEST_HANDLER(
        _In_ IHttpContext *pW3Context,
        _In_ HTTP_MODULE_ID  *pModuleId,
        _In_ APPLICATION  *pApplication
    );

    virtual
    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler() = 0;

    virtual
    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        DWORD      cbCompletion,
        HRESULT    hrCompletionStatus
    ) = 0;

    virtual
    VOID
    TerminateRequest(
        bool    fClientInitiated
    ) = 0;

    virtual
    ~REQUEST_HANDLER(
        VOID
    );

    VOID
    ReferenceRequestHandler(
        VOID
    ) const;

    virtual
    VOID
    DereferenceRequestHandler(
        VOID
    ) const;

protected:
    mutable LONG    m_cRefs;
    IHttpContext*   m_pW3Context;
    APPLICATION*    m_pApplication;
    HTTP_MODULE_ID   m_pModuleId;
};