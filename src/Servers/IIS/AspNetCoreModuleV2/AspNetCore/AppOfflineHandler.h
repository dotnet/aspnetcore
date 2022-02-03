// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <string>
#include "requesthandler.h"

class AppOfflineHandler: public REQUEST_HANDLER
{
public:
    AppOfflineHandler(IHttpContext& pContext, const std::string& appOfflineContent)
        : REQUEST_HANDLER(pContext),
        m_pContext(pContext),
        m_strAppOfflineContent(appOfflineContent)
    {
    }

    REQUEST_NOTIFICATION_STATUS ExecuteRequestHandler() override;

private:
    IHttpContext& m_pContext;
    std::string m_strAppOfflineContent;
};
