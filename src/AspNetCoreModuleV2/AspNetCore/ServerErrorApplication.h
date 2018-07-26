// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "PollingAppOfflineApplication.h"
#include "requesthandler.h"
#include "ServerErrorHandler.h"

class ServerErrorApplication : public PollingAppOfflineApplication
{
public:
    ServerErrorApplication(IHttpApplication& pApplication, HRESULT hr)
        : m_HR(hr),
        PollingAppOfflineApplication(pApplication, PollingAppOfflineApplicationMode::StopWhenAdded)
    {
        m_status = APPLICATION_STATUS::RUNNING;
    }

    ~ServerErrorApplication() = default;

    HRESULT CreateHandler(IHttpContext * pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override
    {
        *pRequestHandler = new ServerErrorHandler(pHttpContext, m_HR);
        return S_OK;
    }

    HRESULT OnAppOfflineFound() override { return S_OK; }
private:
    HRESULT m_HR;
};

