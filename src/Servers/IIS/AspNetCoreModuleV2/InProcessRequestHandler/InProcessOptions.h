// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "ConfigurationSource.h"
#include "WebConfigConfigurationSource.h"

class InProcessOptions: NonCopyable
{
public:
    const std::wstring&
    QueryProcessPath() const
    {
        return m_strProcessPath;
    }

    const std::wstring&
    QueryArguments() const
    {
        return m_strArguments;
    }

    bool
    QueryStdoutLogEnabled() const
    {
        return m_fStdoutLogEnabled;
    }

    const std::wstring&
    QueryStdoutLogFile() const
    {
        return m_struStdoutLogFile;
    }

    bool
    QueryDisableStartUpErrorPage() const
    {
        return m_fDisableStartUpErrorPage;
    }

    bool
    QuerySetCurrentDirectory() const
    {
        return m_fSetCurrentDirectory;
    }

    bool
    QueryWindowsAuthEnabled() const
    {
        return m_fWindowsAuthEnabled;
    }

    bool
    QueryBasicAuthEnabled() const
    {
        return m_fBasicAuthEnabled;
    }

    bool
    QueryAnonymousAuthEnabled() const
    {
        return m_fAnonymousAuthEnabled;
    }

    DWORD
    QueryStartupTimeLimitInMS() const
    {
        if (IsDebuggerPresent())
        {
            return INFINITE;
        }

        return m_dwStartupTimeLimitInMS;
    }

    DWORD
    QueryShutdownTimeLimitInMS() const
    {
        if (IsDebuggerPresent())
        {
            return INFINITE;
        }

        return m_dwShutdownTimeLimitInMS;
    }

    const std::vector<std::pair<std::wstring, std::wstring>>&
    QueryEnvironmentVariables() const
    {
        return m_environmentVariables;
    }

    InProcessOptions(const ConfigurationSource &configurationSource);

    static
    HRESULT InProcessOptions::Create(
        IHttpServer& pServer,
        IHttpApplication& pHttpApplication,
        std::unique_ptr<InProcessOptions>& options);

private:
    std::wstring                   m_strArguments;
    std::wstring                   m_strProcessPath;
    std::wstring                   m_struStdoutLogFile;
    bool                           m_fStdoutLogEnabled;
    bool                           m_fDisableStartUpErrorPage;
    bool                           m_fSetCurrentDirectory;
    bool                           m_fWindowsAuthEnabled;
    bool                           m_fBasicAuthEnabled;
    bool                           m_fAnonymousAuthEnabled;
    DWORD                          m_dwStartupTimeLimitInMS;
    DWORD                          m_dwShutdownTimeLimitInMS;
    std::vector<std::pair<std::wstring, std::wstring>> m_environmentVariables;

protected:
    InProcessOptions() = default;
};
