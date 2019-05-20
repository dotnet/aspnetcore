// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "PollingAppOfflineApplication.h"
#include "requesthandler.h"
#include "ServerErrorHandler.h"

static std::string
GetHtml(HMODULE module, int page, USHORT statusCode, USHORT subStatusCode, std::string error)
{
    try
    {
        HRSRC rc = nullptr;
        HGLOBAL rcData = nullptr;
        std::string data;
        const char* pTempData = nullptr;

        THROW_LAST_ERROR_IF_NULL(rc = FindResource(module, MAKEINTRESOURCE(page), RT_HTML));
        THROW_LAST_ERROR_IF_NULL(rcData = LoadResource(module, rc));
        auto const size = SizeofResource(module, rc);
        THROW_LAST_ERROR_IF(size == 0);
        THROW_LAST_ERROR_IF_NULL(pTempData = static_cast<const char*>(LockResource(rcData)));
        data = std::string(pTempData, size);

        auto additionalErrorLink = Environment::GetEnvironmentVariableValue(L"ANCM_ADDITIONAL_ERROR_PAGE_LINK");
        std::string additionalHtml;

        if (additionalErrorLink.has_value())
        {
            additionalHtml = format("<a href=\"%S\"> <cite> %S </cite></a> and ", additionalErrorLink->c_str(), additionalErrorLink->c_str());
        }

        return format(data, statusCode, subStatusCode, error.c_str(), additionalHtml.c_str());
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        return "";
    }
}

static std::string GetHtml(HMODULE module, int page, USHORT statusCode, USHORT subStatusCode)
{
    return GetHtml(module, page, statusCode, subStatusCode, std::string());
}

class ServerErrorApplication : public PollingAppOfflineApplication
{
public:
    ServerErrorApplication(const IHttpApplication& pApplication, HRESULT hr, bool disableStartupPage, std::string responseContent, USHORT status, USHORT substatus, std::string statusText)
        : m_HR(hr),
        m_disableStartupPage(disableStartupPage),
        m_responseContent(responseContent),
        m_statusCode(status),
        m_subStatusCode(substatus),
        m_statusText(statusText),
        PollingAppOfflineApplication(pApplication, PollingAppOfflineApplicationMode::StopWhenAdded)
    {
        // switch here
        //options.QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS ? GetHtml(g_hServerModule, IN_PROCESS_SHIM_STATIC_HTML, error) : GetHtml(g_hServerModule, OUT_OF_PROCESS_SHIM_STATIC_HTML)
    }

    ~ServerErrorApplication() = default;

    HRESULT CreateHandler(IHttpContext *pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override
    {
        // TODO consider adding enum here to make it easier to switch between modes
        // Everything should just accept a byte array
        // enum may still be helpful for deciding which handler
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
