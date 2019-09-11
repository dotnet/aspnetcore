// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "requesthandler.h"
#include "resource.h"
#include "file_utility.h"


class StartupExceptionHandler : public REQUEST_HANDLER
{
public:

    StartupExceptionHandler(IHttpContext& pContext, BOOL disableLogs, HRESULT hr)
        : m_pContext(pContext),
        m_disableLogs(disableLogs),
        m_HR(hr)
    {
    }

    ~StartupExceptionHandler()
    {
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler()
    {
        static std::string s_html500Page = FILE_UTILITY::GetHtml(g_hModule, IN_PROCESS_SHIM_STATIC_HTML);

        WriteStaticResponse(m_pContext, s_html500Page, m_HR, m_disableLogs);

        return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
    }

private:
    IHttpContext& m_pContext;
    BOOL        m_disableLogs;
    HRESULT     m_HR;
};

