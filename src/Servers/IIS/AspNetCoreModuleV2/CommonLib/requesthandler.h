// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "irequesthandler.h"
#include "ntassert.h"
#include "exceptions.h"

//
// Pure abstract class
//
class REQUEST_HANDLER: public virtual IREQUEST_HANDLER
{
public:
    REQUEST_HANDLER(IHttpContext& pHttpContext) noexcept : m_pHttpContext(pHttpContext)
    {
    }

    virtual
    REQUEST_NOTIFICATION_STATUS
    ExecuteRequestHandler() = 0;

    VOID
    ReferenceRequestHandler() noexcept override
    {
        DBG_ASSERT(m_cRefs != 0);

        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceRequestHandler() noexcept override
    {
        DBG_ASSERT(m_cRefs != 0);

        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }


    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler() final
    {
        TraceContextScope traceScope(m_pHttpContext.GetTraceContext());
        return ExecuteRequestHandler();
    }

    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        DWORD      cbCompletion,
        HRESULT    hrCompletionStatus
    ) final
    {
        TraceContextScope traceScope(m_pHttpContext.GetTraceContext());
        return AsyncCompletion(cbCompletion, hrCompletionStatus);
    };

    virtual
    REQUEST_NOTIFICATION_STATUS AsyncCompletion(DWORD cbCompletion, HRESULT hrCompletionStatus)
    {
        UNREFERENCED_PARAMETER(cbCompletion);
        UNREFERENCED_PARAMETER(hrCompletionStatus);
        // We shouldn't get here in default implementation
        DBG_ASSERT(FALSE);
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }

    #pragma warning( push )
    #pragma warning ( disable : 26440 ) // Disable "Can be marked with noexcept"
    VOID NotifyDisconnect() override
    #pragma warning( pop )
    {
    }

protected:
    IHttpContext& m_pHttpContext;
    mutable LONG  m_cRefs = 1;
};
