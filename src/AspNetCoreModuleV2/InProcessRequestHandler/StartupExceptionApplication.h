// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "InProcessApplicationBase.h"
#include "StartupExceptionHandler.h"

class StartupExceptionApplication : public InProcessApplicationBase
{
public:
    StartupExceptionApplication(
        IHttpServer& pServer,
        IHttpApplication& pApplication,
        BOOL disableLogs)
        : m_disableLogs(disableLogs),
        InProcessApplicationBase(pServer, pApplication)
    {
    }

    ~StartupExceptionApplication() = default;

    HRESULT CreateHandler(IHttpContext * pHttpContext, IREQUEST_HANDLER ** pRequestHandler) override;

    std::string&
        GetStaticHtml500Content()
    {
        return html500Page;
    }

private:
    std::string html500Page;
    BOOL m_disableLogs;
};

