// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "PollingAppOfflineApplication.h"

#include <filesystem>
#include "SRWExclusiveLock.h"
#include "HandleWrapper.h"

APPLICATION_STATUS PollingAppOfflineApplication::QueryStatus()
{
    CheckAppOffline();
    return APPLICATION::QueryStatus();
}

void
PollingAppOfflineApplication::CheckAppOffline()
{
    const auto ulCurrentTime = GetTickCount64();
    //
    // we only care about app offline presented. If not, it means the application has started
    // and is monitoring  the app offline file
    // we cache the file exist check result for 200 ms
    //
    if (ulCurrentTime - m_ulLastCheckTime > c_appOfflineRefreshIntervalMS)
    {
        SRWExclusiveLock lock(m_statusLock);
        if (ulCurrentTime - m_ulLastCheckTime > c_appOfflineRefreshIntervalMS)
        {
            m_fAppOfflineFound = is_regular_file(m_appOfflineLocation);
            if(m_fAppOfflineFound)
            {
                LOG_IF_FAILED(OnAppOfflineFound());
            }
            m_ulLastCheckTime = ulCurrentTime;
        }
    }

    if (m_fAppOfflineFound != (m_mode == StopWhenRemoved))
    {
        Stop(/* fServerInitiated */ false);
    }
}


std::experimental::filesystem::path PollingAppOfflineApplication::GetAppOfflineLocation(IHttpApplication& pApplication)
{
    return std::experimental::filesystem::path(pApplication.GetApplicationPhysicalPath()) / "app_offline.htm";
}
