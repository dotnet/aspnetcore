// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "PollingAppOfflineApplication.h"
#include "requesthandler.h"
#include "ServerErrorHandler.h"

class ServerErrorApplication : public PollingAppOfflineApplication
{
public:
    ServerErrorApplication(const IHttpApplication& pApplication, HRESULT hr, HINSTANCE moduleInstance)
        : ServerErrorApplication(pApplication, hr, moduleInstance, true /* disableStartupPage*/, 0 /* page */)
    {
    }

    ServerErrorApplication(const IHttpApplication& pApplication, HRESULT hr, HINSTANCE moduleInstance, bool disableStartupPage, int page)
        : m_HR(hr),
        m_disableStartupPage(disableStartupPage),
        m_page(page),
        m_moduleInstance(moduleInstance),
        PollingAppOfflineApplication(pApplication, PollingAppOfflineApplicationMode::StopWhenAdded)
    {
    }

    ~ServerErrorApplication() = default;

    HRESULT CreateHandler(IHttpContext *pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override
    {
        auto handler = std::make_unique<ServerErrorHandler>(*pHttpContext, 500ui16, 0ui16, "Internal Server Error", m_HR, m_moduleInstance, m_disableStartupPage, m_page);
        *pRequestHandler = handler.release();
        return S_OK;
    }

    HRESULT OnAppOfflineFound() noexcept override { return S_OK; }
private:
    HRESULT m_HR;
    bool m_disableStartupPage;
    int m_page;
    HINSTANCE m_moduleInstance;
};
