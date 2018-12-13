// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "ConfigurationSource.h"
#include "StringHelpers.h"
#include "WebConfigConfigurationSource.h"

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
                    const auto firstColon = information.find(L':') + 1;
                    const auto lastColon = information.find_last_of(L':');

                    std::wstring host;
                    // Check that : is not the last character
                    if (lastColon != information.length() + 1)
                    {
                        auto const afterLastColon = lastColon + 1;
                        host = information.substr(afterLastColon, information.length() - afterLastColon);
                    }
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

    static
    std::wstring Format(const std::vector<BindingInformation> bindings)
    {
        std::wstring result;

        for (auto binding : bindings)
        {
            result += binding.QueryProtocol() + L"://" + binding.QueryHost() + L":" + binding.QueryPort() + L";";
        }

        return result;
    }

private:
    std::wstring                   m_protocol;
    std::wstring                   m_port;
    std::wstring                   m_host;
};
