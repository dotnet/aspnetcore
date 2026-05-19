// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "PollingAppOfflineApplication.h"

#include <filesystem>
#include "SRWExclusiveLock.h"
#include "HandleWrapper.h"
#include "exceptions.h"

HRESULT PollingAppOfflineApplication::TryCreateHandler(_In_ IHttpContext* pHttpContext, _Outptr_result_maybenull_ IREQUEST_HANDLER** pRequestHandler)
{
    CheckAppOffline();

    return LOG_IF_FAILED(APPLICATION::TryCreateHandler(pHttpContext, pRequestHandler));
}

void
PollingAppOfflineApplication::CheckAppOffline()
{
    if (m_fStopCalled)
    {
        return;
    }

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
            m_fAppOfflineFound = FileExists(m_appOfflineLocation);
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


std::filesystem::path PollingAppOfflineApplication::GetAppOfflineLocation(const IHttpApplication& pApplication)
{
    return std::filesystem::path(pApplication.GetApplicationPhysicalPath()) / "app_offline.htm";
}

bool PollingAppOfflineApplication::FileExists(const std::filesystem::path& path) noexcept
{
    std::error_code ec;
    return is_regular_file(path, ec) || ec.value() == ERROR_SHARING_VIOLATION;
}
