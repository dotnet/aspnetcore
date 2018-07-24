// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once
#include <filesystem>
#include "application.h"
#include "requesthandler.h"

class PollingAppOfflineApplication: public APPLICATION
{
public:
    PollingAppOfflineApplication(IHttpApplication& pApplication)
        :
        m_ulLastCheckTime(0),
        m_appOfflineLocation(GetAppOfflineLocation(pApplication)),
        m_fAppOfflineFound(false)
    {
        InitializeSRWLock(&m_statusLock);
    }

    HRESULT CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler) override;
    
    APPLICATION_STATUS QueryStatus() override;
    bool AppOfflineExists();
    HRESULT LoadAppOfflineContent();
    static bool ShouldBeStarted(IHttpApplication& pApplication);
    void ShutDown() override;
    void Recycle() override;

private:
    static const int c_appOfflineRefreshIntervalMS = 200;
    static std::experimental::filesystem::path GetAppOfflineLocation(IHttpApplication& pApplication);
    std::string m_strAppOfflineContent;
    ULONGLONG m_ulLastCheckTime;
    std::experimental::filesystem::path m_appOfflineLocation;
    bool m_fAppOfflineFound;
    SRWLOCK m_statusLock {};
};


class PollingAppOfflineHandler: public REQUEST_HANDLER
{
public:
    PollingAppOfflineHandler(IHttpContext* pContext, const std::string appOfflineContent)
        : m_pContext(pContext),
          m_strAppOfflineContent(appOfflineContent)
    {    
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override;

private:
    IHttpContext* m_pContext;
    std::string m_strAppOfflineContent;
};


