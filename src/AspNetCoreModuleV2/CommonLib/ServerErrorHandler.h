// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "requesthandler.h"
#include "file_utility.h"
#include "Environment.h"

class ServerErrorHandler : public REQUEST_HANDLER
{
public:

    ServerErrorHandler(IHttpContext &pContext, USHORT statusCode, USHORT subStatusCode, std::string statusText, HRESULT hr, HINSTANCE moduleInstance, bool disableStartupPage, int page) noexcept
        : REQUEST_HANDLER(pContext),
          m_pContext(pContext),
          m_HR(hr),
          m_disableStartupPage(disableStartupPage),
          m_page(page),
          m_moduleInstance(moduleInstance),
          m_statusCode(statusCode),
          m_subStatusCode(subStatusCode),
          m_statusText(std::move(statusText))
    {
    }

    REQUEST_NOTIFICATION_STATUS ExecuteRequestHandler() override
    {
        static std::string s_html500Page = GetHtml(m_moduleInstance, m_page);

        WriteStaticResponse(m_pContext, s_html500Page, m_HR, m_disableStartupPage);

        return RQ_NOTIFICATION_FINISH_REQUEST;
    }

private:
    void WriteStaticResponse(IHttpContext& pContext, std::string &page, HRESULT hr, bool disableStartupErrorPage) const
    {
        if (disableStartupErrorPage)
        {
            pContext.GetResponse()->SetStatus(m_statusCode, m_statusText.c_str(), m_subStatusCode, E_FAIL);
            return;
        }

        HTTP_DATA_CHUNK dataChunk = {};
        IHttpResponse* pResponse = pContext.GetResponse();
        pResponse->SetStatus(m_statusCode, m_statusText.c_str(), m_subStatusCode, hr, nullptr, true);
        pResponse->SetHeader("Content-Type",
            "text/html",
            (USHORT)strlen("text/html"),
            FALSE
        );
        dataChunk.DataChunkType = HttpDataChunkFromMemory;

        dataChunk.FromMemory.pBuffer = page.data();
        dataChunk.FromMemory.BufferLength = static_cast<ULONG>(page.size());
        pResponse->WriteEntityChunkByReference(&dataChunk);
    }

    static
    std::string
    GetHtml(HMODULE module, int page)
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

            return format(data, additionalHtml.c_str());
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
            return "";
        }
    }

    IHttpContext &m_pContext;
    HRESULT m_HR;
    bool m_disableStartupPage;
    int m_page;
    HINSTANCE m_moduleInstance;
    USHORT m_statusCode;
    USHORT m_subStatusCode;
    std::string m_statusText;
};
