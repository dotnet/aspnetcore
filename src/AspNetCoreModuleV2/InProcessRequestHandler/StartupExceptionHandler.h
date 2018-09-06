// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "requesthandler.h"

class StartupExceptionApplication;

class StartupExceptionHandler : public REQUEST_HANDLER
{
public:
    StartupExceptionHandler(IHttpContext* pContext, BOOL disableLogs)
        :
        m_pContext(pContext),
        m_disableLogs(disableLogs)
    {
    }
    ~StartupExceptionHandler()
    {
        
    }
    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override;

private:
    IHttpContext * m_pContext;
    BOOL        m_disableLogs;
    
    static
    std::string s_html500Page;
};

