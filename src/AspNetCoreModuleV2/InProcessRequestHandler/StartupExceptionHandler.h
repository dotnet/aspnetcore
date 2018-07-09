// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "requesthandler.h"

class StartupExceptionApplication;

class StartupExceptionHandler : public REQUEST_HANDLER
{
public:
    StartupExceptionHandler(IHttpContext* pContext, BOOL disableLogs, StartupExceptionApplication* pApplication)
        :
        m_pContext(pContext),
        m_disableLogs(disableLogs),
        m_pApplication(pApplication)
    {
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override;

private:
    IHttpContext * m_pContext;
    BOOL        m_disableLogs;
    StartupExceptionApplication* m_pApplication;
};

