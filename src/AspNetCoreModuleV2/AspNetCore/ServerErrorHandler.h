// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "requesthandler.h"

class ServerErrorHandler : public REQUEST_HANDLER
{
public:
    ServerErrorHandler(IHttpContext* pContext, HRESULT hr) : m_pContext(pContext), m_HR(hr)
    {
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override
    {
        m_pContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, m_HR);
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }

private:
    IHttpContext * m_pContext;
    HRESULT m_HR;
};
