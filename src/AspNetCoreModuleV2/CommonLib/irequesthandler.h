// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <httpserv.h>

//
// Pure abstract class
//
class IREQUEST_HANDLER
{
public:

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
    ~IREQUEST_HANDLER(
        VOID
    ) = 0 { }

    virtual
    VOID
    ReferenceRequestHandler(
        VOID
    ) = 0;

    virtual
    VOID
    DereferenceRequestHandler(
        VOID
    ) = 0;
};
