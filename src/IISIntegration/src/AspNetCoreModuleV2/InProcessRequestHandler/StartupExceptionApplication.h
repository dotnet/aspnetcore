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
        HINSTANCE moduleInstance,
        BOOL disableLogs,
        HRESULT hr)
        : m_disableLogs(disableLogs),
        m_HR(hr),
        m_moduleInstance(moduleInstance),
        InProcessApplicationBase(pServer, pApplication)
    {
    }

    ~StartupExceptionApplication() = default;

    HRESULT CreateHandler(IHttpContext *pHttpContext, IREQUEST_HANDLER ** pRequestHandler)
    {
        *pRequestHandler = new ServerErrorHandler(*pHttpContext, 500, 30, "Internal Server Error", m_HR, m_moduleInstance, m_disableLogs, IN_PROCESS_RH_STATIC_HTML);
        return S_OK;
    }

    std::string&
        GetStaticHtml500Content()
    {
        return html500Page;
    }

private:
    std::string html500Page;
    BOOL m_disableLogs;
    HRESULT m_HR;
    HINSTANCE m_moduleInstance;
};

