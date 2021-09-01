// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    PollingAppOfflineApplication(const IHttpApplication& pApplication, PollingAppOfflineApplicationMode mode)
        : APPLICATION(pApplication),
        m_ulLastCheckTime(0),
        m_appOfflineLocation(GetAppOfflineLocation(pApplication)),
        m_fAppOfflineFound(false),
        m_mode(mode)
    {
        InitializeSRWLock(&m_statusLock);
    }

    HRESULT
    TryCreateHandler(
        _In_ IHttpContext       *pHttpContext,
        _Outptr_result_maybenull_ IREQUEST_HANDLER  **pRequestHandler) override;

    void CheckAppOffline();
    virtual HRESULT OnAppOfflineFound() = 0;
    void StopInternal(bool fServerInitiated) override { UNREFERENCED_PARAMETER(fServerInitiated); }

protected:
    std::filesystem::path m_appOfflineLocation;
    static std::filesystem::path GetAppOfflineLocation(const IHttpApplication& pApplication);
    static bool FileExists(const std::filesystem::path& path) noexcept;
private:
    static const int c_appOfflineRefreshIntervalMS = 200;
    std::string m_strAppOfflineContent;
    ULONGLONG m_ulLastCheckTime;
    bool m_fAppOfflineFound;
    SRWLOCK m_statusLock {};
    PollingAppOfflineApplicationMode m_mode;
};
