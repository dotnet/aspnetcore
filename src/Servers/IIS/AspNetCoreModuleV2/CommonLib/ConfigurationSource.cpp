// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "ConfigurationSource.h"

#include "StringHelpers.h"
#include "ConfigurationLoadException.h"

std::shared_ptr<ConfigurationSection> ConfigurationSource::GetRequiredSection(const std::wstring& name) const
{
    auto section = GetSection(name);
    if (!section)
    {
        throw ConfigurationLoadException(format(L"Unable to get required configuration section '%s'. Possible reason is web.config authoring error.", name.c_str()));
    }
    return section;
}

