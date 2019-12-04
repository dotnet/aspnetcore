// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "ConfigurationSection.h"

#include "StringHelpers.h"
#include "ConfigurationLoadException.h"

std::wstring ConfigurationSection::GetRequiredString(const std::wstring& name)  const
{
    auto result = GetString(name);
    if (!result.has_value() || result.value().empty())
    {
        ThrowRequiredException(name);
    }
    return result.value();
}

bool ConfigurationSection::GetRequiredBool(const std::wstring& name) const
{
    auto result = GetBool(name);
    if (!result.has_value())
    {
        ThrowRequiredException(name);
    }
    return result.value();
}

DWORD ConfigurationSection::GetRequiredLong(const std::wstring& name) const
{
    auto result = GetLong(name);
    if (!result.has_value())
    {
        ThrowRequiredException(name);
    }
    return result.value();
}

DWORD ConfigurationSection::GetRequiredTimespan(const std::wstring& name) const
{
    auto result = GetTimespan(name);
    if (!result.has_value())
    {
        ThrowRequiredException(name);
    }
    return result.value();
}

void ConfigurationSection::ThrowRequiredException(const std::wstring& name)
{
    throw ConfigurationLoadException(format(L"Attribute '%s' is required.", name.c_str()));
}

std::optional<std::wstring> find_element(const std::vector<std::pair<std::wstring, std::wstring>>& pairs, const std::wstring& name)
{
    const auto iter = std::find_if(
        pairs.begin(),
        pairs.end(),
        [&](const std::pair<std::wstring, std::wstring>& pair) { return equals_ignore_case(pair.first, name); });

    if (iter == pairs.end())
    {
        return std::nullopt;
    }

    return std::make_optional(iter->second);
}
