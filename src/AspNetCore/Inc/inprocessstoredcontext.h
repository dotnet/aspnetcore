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
};

