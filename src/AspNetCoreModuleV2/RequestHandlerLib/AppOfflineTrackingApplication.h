// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <Windows.h>
#include "application.h"
#include "filewatcher.h"

class AppOfflineTrackingApplication: public APPLICATION
{
public:
    AppOfflineTrackingApplication(const IHttpApplication& application)
        : m_applicationPath(application.GetApplicationPhysicalPath()),
        m_fileWatcher(nullptr),
        m_fileWatcherEntry(nullptr)
    {
    }

    ~AppOfflineTrackingApplication() override = default;

    HRESULT
    StartMonitoringAppOffline();
    
    virtual
    VOID
    OnAppOffline();

private:
    HRESULT
    StartMonitoringAppOflineImpl();

    std::wstring                                 m_applicationPath;
    std::unique_ptr<FILE_WATCHER>                m_fileWatcher;
    std::unique_ptr<FILE_WATCHER_ENTRY, FILE_WATCHER_ENTRY_DELETER>  m_fileWatcherEntry;
};
