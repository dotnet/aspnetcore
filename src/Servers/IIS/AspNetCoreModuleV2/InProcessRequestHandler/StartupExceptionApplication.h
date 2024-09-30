// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "InProcessApplicationBase.h"
#include "ServerErrorHandler.h"
#include "resource.h"

class StartupExceptionApplication : public InProcessApplicationBase
{
public:
    StartupExceptionApplication(
        IHttpServer& pServer,
        IHttpApplication& pApplication,
        BOOL disableLogs,
        HRESULT hr,
        const std::string& errorPageContent,
        USHORT statusCode,
        USHORT subStatusCode,
        const std::string& statusText)
        : m_disableLogs(disableLogs),
        m_HR(hr),
        m_error(errorPageContent),
        m_statusCode(statusCode),
        m_subStatusCode(subStatusCode),
        m_statusText(statusText),
        InProcessApplicationBase(pServer, pApplication)
    {
    }

    ~StartupExceptionApplication() = default;

    HRESULT CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler)
    {
        *pRequestHandler = new ServerErrorHandler(*pHttpContext, m_statusCode, m_subStatusCode, m_statusText, m_HR, m_disableLogs, m_error);

        return S_OK;
    }

private:
    std::string m_error;
    BOOL m_disableLogs;
    HRESULT m_HR;
    USHORT m_statusCode;
    USHORT m_subStatusCode;
    std::string m_statusText;
};

