// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "AppOfflineTrackingApplication.h"
#include "EventLog.h"
#include "exceptions.h"

extern HANDLE g_hEventLog;

HRESULT AppOfflineTrackingApplication::StartMonitoringAppOffline()
{
    LOG_INFOF("Starting app_offline monitoring in application %S", m_applicationPath.c_str());
    HRESULT hr = StartMonitoringAppOflineImpl();

    if (FAILED_LOG(hr))
    {
        UTILITY::LogEventF(g_hEventLog,
                           EVENTLOG_WARNING_TYPE,
                           ASPNETCORE_EVENT_MONITOR_APPOFFLINE_ERROR,
                           ASPNETCORE_EVENT_MONITOR_APPOFFLINE_ERROR_MSG,
                           m_applicationPath.c_str(),
                           hr);
    }

    return hr;
}

void AppOfflineTrackingApplication::Stop(bool fServerInitiated)
{
    APPLICATION::Stop(fServerInitiated);

    m_status = APPLICATION_STATUS::RECYCLED;

    if (m_fileWatcher)
    {
        m_fileWatcher->StopMonitor();
        m_fileWatcher = nullptr;
    }
}

HRESULT AppOfflineTrackingApplication::StartMonitoringAppOflineImpl()
{
    if (m_fileWatcher)
    {
        RETURN_IF_FAILED(E_UNEXPECTED);
    }

    m_fileWatcher = std::make_unique<FILE_WATCHER>();
    RETURN_IF_FAILED(m_fileWatcher->Create(m_applicationPath.c_str(),
        L"app_offline.htm",
        this));

    return S_OK;
}

void AppOfflineTrackingApplication::OnAppOffline()
{
    if (m_fAppOfflineProcessed.exchange(true))
    {
        return;
    }

    LOG_INFOF("Received app_offline notification in application %S", m_applicationPath.c_str());
    UTILITY::LogEventF(g_hEventLog,
                       EVENTLOG_INFORMATION_TYPE,
                       ASPNETCORE_EVENT_RECYCLE_APPOFFLINE,
                       ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_MSG,
                       m_applicationPath.c_str());

    Stop(/*fServerInitiated*/ false);
}
