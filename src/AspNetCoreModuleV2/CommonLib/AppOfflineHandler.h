// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "application.h"
#include "requesthandler.h"

class AppOfflineHandler: public REQUEST_HANDLER
{
public:
    AppOfflineHandler(IHttpContext* pContext, const std::string appOfflineContent)
        : m_pContext(pContext),
          m_strAppOfflineContent(appOfflineContent)
    {
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override;

private:
    IHttpContext* m_pContext;
    std::string m_strAppOfflineContent;
};
