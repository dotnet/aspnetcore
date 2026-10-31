// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include <atlcomcli.h>
#include "ConfigurationSection.h"
#include "ConfigurationSource.h"

class WebConfigConfigurationSource: public ConfigurationSource
{
public:
    WebConfigConfigurationSource(IAppHostAdminManager *pAdminManager, const IHttpApplication &pHttpApplication) noexcept
        : m_manager(pAdminManager),
          m_application(pHttpApplication)
    {
    }

    std::shared_ptr<ConfigurationSection> GetSection(const std::wstring& name) const override;

private:
    CComPtr<IAppHostAdminManager> m_manager;
    const IHttpApplication &m_application;
};
