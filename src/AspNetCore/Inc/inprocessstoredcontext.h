// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
class IN_PROCESS_STORED_CONTEXT : public IHttpStoredContext
{
public:
    IN_PROCESS_STORED_CONTEXT(
        IHttpContext* pHttpContext,
        PVOID pvManagedContext
    );

    ~IN_PROCESS_STORED_CONTEXT();

    virtual
    VOID
    CleanupStoredContext(
        VOID
    )
    {
        delete this;
    }

    virtual
    VOID
    OnClientDisconnected(
        VOID
    )
    {
    }

    virtual
    VOID
    OnListenerEvicted(
        VOID
    )
    {
    }

    PVOID
    QueryManagedHttpContext(
        VOID
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

    static
    HRESULT
    GetInProcessStoredContext(
        IHttpContext*               pHttpContext,
        IN_PROCESS_STORED_CONTEXT** ppInProcessStoredContext
    );

    static
    HRESULT
    SetInProcessStoredContext(
        IHttpContext*               pHttpContext,
        IN_PROCESS_STORED_CONTEXT* pInProcessStoredContext
    );

private:
    PVOID m_pManagedHttpContext;
    IHttpContext* m_pHttpContext;
    BOOL m_fManagedRequestComplete;
    REQUEST_NOTIFICATION_STATUS m_requestNotificationStatus;
};

