// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        m_fAppOfflineProcessed(false)
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

private:
    HRESULT
    StartMonitoringAppOflineImpl();

    std::wstring                                 m_applicationPath;
    std::unique_ptr<FILE_WATCHER>                m_fileWatcher;
    std::atomic_bool                             m_fAppOfflineProcessed;
};
