// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "requesthandler.h"
#include "file_utility.h"

class ServerErrorHandler : public REQUEST_HANDLER
{
public:

    ServerErrorHandler(IHttpContext &pContext, HRESULT hr, HINSTANCE moduleInstance, bool disableStartupPage, int page)
        : m_pContext(pContext), m_HR(hr), m_disableStartupPage(disableStartupPage), m_page(page), m_moduleInstance(moduleInstance)
    {
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override
    {
        static std::string s_html500Page = FILE_UTILITY::GetHtml(m_moduleInstance, m_page);

        WriteStaticResponse(m_pContext, s_html500Page, m_HR, m_disableStartupPage);

        return RQ_NOTIFICATION_FINISH_REQUEST;
    }

private:
    IHttpContext &m_pContext;
    HRESULT m_HR;
    bool m_disableStartupPage;
    int m_page;
    HINSTANCE m_moduleInstance;
};
