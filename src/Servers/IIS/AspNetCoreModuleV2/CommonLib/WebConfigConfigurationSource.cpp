// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "WebConfigConfigurationSource.h"

#include "exceptions.h"
#include "WebConfigConfigurationSection.h"

std::shared_ptr<ConfigurationSection> WebConfigConfigurationSource::GetSection(const std::wstring& name) const
{
    const CComBSTR bstrAspNetCoreSection = name.c_str();
    const CComBSTR applicationConfigPath = m_application.GetAppConfigPath();

    IAppHostElement* sectionElement;
    if (FAILED_LOG(m_manager->GetAdminSection(bstrAspNetCoreSection, applicationConfigPath, &sectionElement)))
    {
        return nullptr;
    }
    return std::make_unique<WebConfigConfigurationSection>(sectionElement);
}
