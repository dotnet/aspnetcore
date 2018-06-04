// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"
#include "requesthandler.h"

class IN_PROCESS_APPLICATION;

class IN_PROCESS_HANDLER : public REQUEST_HANDLER
{
public:
    IN_PROCESS_HANDLER(
        _In_ IHttpContext   *pW3Context,
        _In_ IN_PROCESS_APPLICATION  *pApplication);

    ~IN_PROCESS_HANDLER() override;

    __override
    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler() override;

    __override
    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        DWORD       cbCompletion,
        HRESULT     hrCompletionStatus
    ) override;

    __override
    VOID
    TerminateRequest(
        bool    fClientInitiated
    ) override;

    PVOID
    QueryManagedHttpContext(
        VOID
    );

    VOID
    SetManagedHttpContext(
        PVOID pManagedHttpContext
    );

    IHttpContext*
    QueryHttpContext(
        VOID
    );

    BOOL
    QueryIsManagedRequestComplete(
        VOID
    );

    VOID
    IndicateManagedRequestComplete(
        VOID
    );

    REQUEST_NOTIFICATION_STATUS
    QueryAsyncCompletionStatus(
        VOID
    );

    VOID
    SetAsyncCompletionStatus(
        REQUEST_NOTIFICATION_STATUS requestNotificationStatus
    );

private:
    PVOID m_pManagedHttpContext;
    BOOL m_fManagedRequestComplete;
    REQUEST_NOTIFICATION_STATUS m_requestNotificationStatus;

    IHttpContext*               m_pW3Context;
    IN_PROCESS_APPLICATION*     m_pApplication;
};
