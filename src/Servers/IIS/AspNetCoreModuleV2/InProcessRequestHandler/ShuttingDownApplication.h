// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "InProcessApplicationBase.h"

class ShuttingDownHandler : public REQUEST_HANDLER
{
public:
    ShuttingDownHandler(IHttpContext* pContext)
        : REQUEST_HANDLER(*pContext),
          m_pContext(pContext)
    {
    }

    REQUEST_NOTIFICATION_STATUS ExecuteRequestHandler() override
    {
        return ServerShutdownMessage(m_pContext);
    }

    static REQUEST_NOTIFICATION_STATUS ServerShutdownMessage(IHttpContext * pContext)
    {
        pContext->GetResponse()->SetStatus(503, "Server has been shutdown", 0, HRESULT_FROM_WIN32(ERROR_SHUTDOWN_IN_PROGRESS));
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
private:
    IHttpContext * m_pContext;
};

class ShuttingDownApplication : public InProcessApplicationBase
{
public:
    ShuttingDownApplication(IHttpServer& pHttpServer, IHttpApplication& pHttpApplication)
        : InProcessApplicationBase(pHttpServer, pHttpApplication)
    {
    }

    ~ShuttingDownApplication() = default;

    HRESULT CreateHandler(IHttpContext * pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override
    {
        *pRequestHandler = new ShuttingDownHandler(pHttpContext);
        return S_OK;
    }
};
