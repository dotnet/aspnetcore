// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "ConfigurationSource.h"
#include "WebConfigConfigurationSource.h"

class BindingInformation;

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

    const std::vector<BindingInformation>&
    QueryBindings() const
    {
        return m_bindingInformation;
    }

    InProcessOptions(const ConfigurationSource &configurationSource, IHttpSite* pSite);

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
    std::vector<BindingInformation> m_bindingInformation;

protected:
    InProcessOptions() = default;
};

class BindingInformation
{
public:
    BindingInformation(std::wstring protocol, std::wstring host, std::wstring port)
    {
        m_protocol = protocol;
        m_host = host;
        m_port = port;
    }

    std::wstring& QueryProtocol()
    {
        return m_protocol;
    }

    std::wstring& QueryPort()
    {
        return m_port;
    }

    std::wstring& QueryHost()
    {
        return m_host;
    }

    static
    std::vector<BindingInformation>
    Load(const ConfigurationSource &configurationSource, const IHttpSite& pSite)
    {
        std::vector<BindingInformation> items;

        const std::wstring runningSiteName = pSite.GetSiteName();

        auto const siteSection = configurationSource.GetRequiredSection(CS_SITE_SECTION);
        auto sites = siteSection->GetCollection();
        for (const auto& site: sites)
        {
            auto siteName = site->GetRequiredString(L"name");
            if (equals_ignore_case(runningSiteName, siteName))
            {
                auto bindings = site->GetRequiredSection(L"bindings")->GetCollection();
                for (const auto& binding : bindings)
                {
                    const auto information = binding->GetRequiredString(L"bindingInformation");
                    const auto firstColon = information.find(L':');
                    const auto lastColon = information.find_last_of(L':');

                    auto host = information.substr(lastColon, information.length() - lastColon);
                    if (host.length() == 0)
                    {
                        host = L"*";
                    }

                    items.emplace_back(
                        binding->GetRequiredString(L"protocol"),
                        host,
                        information.substr(firstColon, lastColon - firstColon)
                        );
                }
            }
        }

        return items;
    }
private:
    std::wstring                   m_protocol;
    std::wstring                   m_port;
    std::wstring                   m_host;
};
