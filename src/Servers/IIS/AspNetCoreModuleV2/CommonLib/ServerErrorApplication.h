// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "PollingAppOfflineApplication.h"
#include "requesthandler.h"
#include "ServerErrorHandler.h"

class ServerErrorApplication : public PollingAppOfflineApplication
{
public:
    ServerErrorApplication(const IHttpApplication& pApplication, HRESULT hr, bool disableStartupPage, const std::string& responseContent, USHORT status, USHORT substatus, const std::string& statusText)
        : m_HR(hr),
        m_disableStartupPage(disableStartupPage),
        m_responseContent(responseContent),
        m_statusCode(status),
        m_subStatusCode(substatus),
        m_statusText(statusText),
        PollingAppOfflineApplication(pApplication, PollingAppOfflineApplicationMode::StopWhenAdded)
    {
    }

    ~ServerErrorApplication() = default;

    HRESULT CreateHandler(IHttpContext *pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override
    {
        *pRequestHandler = std::make_unique<ServerErrorHandler>(*pHttpContext, m_statusCode, m_subStatusCode, m_statusText, m_HR, m_disableStartupPage, m_responseContent).release();

        return S_OK;
    }

    HRESULT OnAppOfflineFound() noexcept override { return S_OK; }
private:
    HRESULT m_HR;
    bool m_disableStartupPage;
    std::string m_responseContent;
    USHORT m_statusCode;
    USHORT m_subStatusCode;
    std::string m_statusText;
};
