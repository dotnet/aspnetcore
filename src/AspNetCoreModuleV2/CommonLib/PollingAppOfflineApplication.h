// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once
#include <filesystem>
#include "application.h"

enum PollingAppOfflineApplicationMode
{
    StopWhenAdded,
    StopWhenRemoved
};

class PollingAppOfflineApplication: public APPLICATION
{
public:
    PollingAppOfflineApplication(IHttpApplication& pApplication, PollingAppOfflineApplicationMode mode)
        :
        m_ulLastCheckTime(0),
        m_appOfflineLocation(GetAppOfflineLocation(pApplication)),
        m_fAppOfflineFound(false),
        m_mode(mode)
    {
        InitializeSRWLock(&m_statusLock);
    }

    APPLICATION_STATUS QueryStatus() override;
    void CheckAppOffline();
    virtual HRESULT OnAppOfflineFound() = 0;
    void StopInternal(bool fServerInitiated) override { UNREFERENCED_PARAMETER(fServerInitiated); }

protected:
    std::experimental::filesystem::path m_appOfflineLocation;
    static std::experimental::filesystem::path GetAppOfflineLocation(IHttpApplication& pApplication);

private:
    static const int c_appOfflineRefreshIntervalMS = 200;
    std::string m_strAppOfflineContent;
    ULONGLONG m_ulLastCheckTime;
    bool m_fAppOfflineFound;
    SRWLOCK m_statusLock {};
    PollingAppOfflineApplicationMode m_mode;
};
