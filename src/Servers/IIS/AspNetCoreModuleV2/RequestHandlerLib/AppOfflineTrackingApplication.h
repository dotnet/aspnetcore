// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <Windows.h>
#include "application.h"
#include "filewatcher.h"
#include <atomic>

class AppOfflineTrackingApplication: public APPLICATION
{
public:
    AppOfflineTrackingApplication(const IHttpApplication& application)
        : APPLICATION(application),
        m_applicationPath(application.GetApplicationPhysicalPath()),
        m_fileWatcher(nullptr),
        m_fAppOfflineProcessed(false),
        m_shutdownTimeout(120000), // default to 2 minutes
        m_detectedAppOffline(false)
    {
    }

    ~AppOfflineTrackingApplication() override
    {
        if (m_fileWatcher)
        {
            m_fileWatcher->StopMonitor();
        }
    };

    HRESULT
    StartMonitoringAppOffline();

    VOID
    StopInternal(bool fServerInitiated) override;

    virtual
    VOID
    OnAppOffline();

    // TODO protected
    bool                                         m_detectedAppOffline;
    std::wstring                                 m_shadowCopyDirectory;
    DWORD                                        m_shutdownTimeout;
private:
    HRESULT
    StartMonitoringAppOflineImpl();

    std::wstring                                 m_applicationPath;
    std::unique_ptr<FILE_WATCHER>                m_fileWatcher;
    std::atomic_bool                             m_fAppOfflineProcessed;
};
