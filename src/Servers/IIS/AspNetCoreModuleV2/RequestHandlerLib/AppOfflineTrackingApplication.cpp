// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "AppOfflineTrackingApplication.h"
#include "EventLog.h"
#include "exceptions.h"

HRESULT AppOfflineTrackingApplication::StartMonitoringAppOffline()
{
    LOG_INFOF(L"Starting app_offline monitoring in application '%ls'", m_applicationPath.c_str());
    HRESULT hr = StartMonitoringAppOflineImpl();

    if (FAILED_LOG(hr))
    {
        EventLog::Warn(
            ASPNETCORE_EVENT_MONITOR_APPOFFLINE_ERROR,
            ASPNETCORE_EVENT_MONITOR_APPOFFLINE_ERROR_MSG,
            m_applicationPath.c_str(),
            hr);
    }

    return hr;
}

void AppOfflineTrackingApplication::StopInternal(bool fServerInitiated)
{
    if (m_fileWatcher)
    {
        m_fileWatcher->StopMonitor();
        m_fileWatcher = nullptr;
    }

    APPLICATION::StopInternal(fServerInitiated);
}

HRESULT AppOfflineTrackingApplication::StartMonitoringAppOflineImpl()
{
    if (m_fileWatcher)
    {
        RETURN_HR(E_UNEXPECTED);
    }

    m_fileWatcher = std::make_unique<FILE_WATCHER>();
    RETURN_IF_FAILED(m_fileWatcher->Create(m_applicationPath.c_str(),
        L"app_offline.htm",
        m_shadowCopyDirectory,
        this,
        m_shutdownTimeout));

    return S_OK;
}

void AppOfflineTrackingApplication::OnAppOffline()
{
    if (m_fAppOfflineProcessed.exchange(true))
    {
        return;
    }

    if (m_detectedAppOffline)
    {
        LOG_INFOF(L"Received app_offline notification in application '%ls'", m_applicationPath.c_str());
        EventLog::Info(
            ASPNETCORE_EVENT_RECYCLE_APPOFFLINE,
            ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_MSG,
            m_applicationPath.c_str());
    }
    else
    {
        LOG_INFOF(L"Received file change notification in application '%ls'", m_applicationPath.c_str());
        EventLog::Info(
            ASPNETCORE_EVENT_RECYCLE_APPOFFLINE,
            ASPNETCORE_EVENT_RECYCLE_FILECHANGE_MSG,
            m_applicationPath.c_str());
    }

    Stop(/*fServerInitiated*/ false);
}
