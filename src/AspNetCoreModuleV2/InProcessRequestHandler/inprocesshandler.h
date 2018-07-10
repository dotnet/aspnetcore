// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"
#include "requesthandler.h"
#include <memory>
#include "iapplication.h"
#include "inprocessapplication.h"

class IN_PROCESS_APPLICATION;

class IN_PROCESS_HANDLER : public REQUEST_HANDLER
{
public:
    IN_PROCESS_HANDLER(
        _In_ std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> pApplication,
        _In_ IHttpContext   *pW3Context,
        _In_ PFN_REQUEST_HANDLER pRequestHandler,
        _In_ void * pRequestHandlerContext,
        _In_ PFN_ASYNC_COMPLETION_HANDLER pAsyncCompletion);

    ~IN_PROCESS_HANDLER() override = default;

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
    
    IHttpContext*
    QueryHttpContext(
        VOID
    ) const
    {
        return m_pW3Context;
    }

    VOID
    SetManagedHttpContext(
        PVOID pManagedHttpContext
    );

    VOID
    IndicateManagedRequestComplete(
        VOID
    );

    VOID
    SetAsyncCompletionStatus(
        REQUEST_NOTIFICATION_STATUS requestNotificationStatus
    );

    static void * operator new(size_t size);

    static void operator delete(void * pMemory);

    static
    HRESULT
    StaticInitialize(VOID);

private:
    REQUEST_NOTIFICATION_STATUS
    ServerShutdownMessage() const;

    PVOID m_pManagedHttpContext;
    BOOL m_fManagedRequestComplete;
    REQUEST_NOTIFICATION_STATUS m_requestNotificationStatus;
    IHttpContext*               m_pW3Context;
    std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> m_pApplication;
    PFN_REQUEST_HANDLER         m_pRequestHandler;
    void*                       m_pRequestHandlerContext;
    PFN_ASYNC_COMPLETION_HANDLER m_pAsyncCompletionHandler;

    static ALLOC_CACHE_HANDLER *   sm_pAlloc;
};
