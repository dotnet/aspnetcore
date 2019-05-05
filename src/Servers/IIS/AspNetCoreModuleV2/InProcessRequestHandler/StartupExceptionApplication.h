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
        HRESULT hr,
        std::vector<byte>&& errorPageContent
        )
        : m_disableLogs(disableLogs),
        m_HR(hr),
        m_moduleInstance(moduleInstance),
        m_errorPageContent(std::move(errorPageContent)),
        InProcessApplicationBase(pServer, pApplication)
    {
    }

    ~StartupExceptionApplication() = default;

    HRESULT CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler)
    {
        *pRequestHandler = new ServerErrorHandler(*pHttpContext, 500, 30, "Internal Server Error", m_HR, m_moduleInstance, m_disableLogs, IN_PROCESS_RH_STATIC_HTML, m_errorPageContent);

        return S_OK;
    }

private:
    std::vector<byte> m_errorPageContent;
    BOOL m_disableLogs;
    HRESULT m_HR;
    HINSTANCE m_moduleInstance;
};

