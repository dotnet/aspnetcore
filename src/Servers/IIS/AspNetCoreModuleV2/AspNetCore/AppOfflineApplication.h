// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "application.h"
#include "requesthandler.h"
#include "PollingAppOfflineApplication.h"

class AppOfflineApplication: public PollingAppOfflineApplication
{
public:
    AppOfflineApplication(const IHttpApplication& pApplication)
        : PollingAppOfflineApplication(pApplication, StopWhenRemoved)
    {
        CheckAppOffline();
    }

    HRESULT CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler) override;

    HRESULT OnAppOfflineFound() override;

    static bool ShouldBeStarted(const IHttpApplication& pApplication);

private:
    std::string m_strAppOfflineContent;
};

