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
    }

    ~ServerErrorApplication() = default;

    HRESULT CreateHandler(IHttpContext *pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override
    {
        auto handler = std::make_unique<ServerErrorHandler>(*pHttpContext, m_HR);
        *pRequestHandler = handler.release();
        return S_OK;
    }

    HRESULT OnAppOfflineFound() noexcept override { return S_OK; }
private:
    HRESULT m_HR;
};

