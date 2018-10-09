// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "irequesthandler.h"
#include "ntassert.h"

//
// Pure abstract class
//
class REQUEST_HANDLER: public virtual IREQUEST_HANDLER
{

public:
    VOID
    ReferenceRequestHandler() noexcept override
    {
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

    REQUEST_NOTIFICATION_STATUS OnAsyncCompletion(DWORD cbCompletion, HRESULT hrCompletionStatus) override
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

private:
    mutable LONG                    m_cRefs = 1;
};
