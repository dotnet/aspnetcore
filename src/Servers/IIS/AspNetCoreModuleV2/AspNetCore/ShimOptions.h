// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "ConfigurationSource.h"
#include "exceptions.h"

enum APP_HOSTING_MODEL
{
    HOSTING_UNKNOWN = 0,
    HOSTING_IN_PROCESS,
    HOSTING_OUT_PROCESS
};

class ShimOptions: NonCopyable
{
public:
    const std::wstring&
    QueryProcessPath() const noexcept
    {
        return m_strProcessPath;
    }

    const std::wstring&
    QueryArguments() const noexcept
    {
        return m_strArguments;
    }

    APP_HOSTING_MODEL
    QueryHostingModel() const noexcept
    {
        return m_hostingModel;
    }

    const std::wstring&
    QueryHandlerVersion() const noexcept
    {
        return m_strHandlerVersion;
    }

    BOOL
    QueryStdoutLogEnabled() const noexcept
    {
        return m_fStdoutLogEnabled;
    }

    const std::wstring&
    QueryStdoutLogFile() const noexcept
    {
        return m_struStdoutLogFile;
    }

    bool
    QueryDisableStartupPage() const noexcept
    {
        return m_fDisableStartupPage;
    }

    bool
    QueryShowDetailedErrors() const noexcept
    {
        return m_fShowDetailedErrors;
    }

    bool
    QueryShadowCopyEnabled() const noexcept
    {
        return m_fEnableShadowCopying;
    }

    bool
    QueryCleanShadowCopyDirectory() const noexcept
    {
        return m_fCleanShadowCopyDirectory;
    }

    const std::wstring&
    QueryShadowCopyDirectory() const noexcept
    {
        return m_strShadowCopyingDirectory;
    }

    bool
    QueryDisallowRotationOnConfigChange() const noexcept
    {
        return m_fDisallowRotationOnConfigChange;
    }

    std::chrono::milliseconds
    QueryShutdownDelay() const noexcept
    {
        return m_fShutdownDelay;
    }

    ShimOptions(const ConfigurationSource &configurationSource);

private:
    std::wstring                   m_strArguments;
    std::wstring                   m_strProcessPath;
    APP_HOSTING_MODEL              m_hostingModel;
    std::wstring                   m_strHandlerVersion;
    std::wstring                   m_struStdoutLogFile;
    bool                           m_fStdoutLogEnabled;
    bool                           m_fDisableStartupPage;
    bool                           m_fShowDetailedErrors;
    bool                           m_fEnableShadowCopying;
    bool                           m_fCleanShadowCopyDirectory;
    bool                           m_fDisallowRotationOnConfigChange;
    std::wstring                   m_strShadowCopyingDirectory;
    std::chrono::milliseconds      m_fShutdownDelay;

    void SetShutdownDelay(const std::wstring& shutdownDelay);
};
