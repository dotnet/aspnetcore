// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "ConfigurationSection.h"

#include "StringHelpers.h"
#include "ConfigurationLoadException.h"
#include <map>

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

std::vector<std::pair<std::wstring, std::wstring>> ConfigurationSection::GetKeyValuePairs(const std::wstring& name) const
{
    std::vector<std::pair<std::wstring, std::wstring>> pairs;

    for (auto const element : GetRequiredSection(name)->GetCollection())
    {
        pairs.emplace_back(element->GetRequiredString(CS_ASPNETCORE_COLLECTION_ITEM_NAME),
                           element->GetString(CS_ASPNETCORE_COLLECTION_ITEM_VALUE).value_or(L""));
    }
    return pairs;
}

std::map<std::wstring, std::wstring, ignore_case_comparer> ConfigurationSection::GetMap(const std::wstring& name) const
{
    std::map<std::wstring, std::wstring, ignore_case_comparer> pairs;

    for (auto const element : GetRequiredSection(name)->GetCollection())
    {
        pairs.insert_or_assign(element->GetRequiredString(CS_ASPNETCORE_COLLECTION_ITEM_NAME), element->GetString(CS_ASPNETCORE_COLLECTION_ITEM_VALUE).value_or(L""));
    }
    return pairs;
}

std::shared_ptr<ConfigurationSection> ConfigurationSection::GetRequiredSection(const std::wstring& name) const
{
    auto section = GetSection(name);
    if (!section)
    {
        throw ConfigurationLoadException(format(L"Unable to get required configuration section '%s'. Possible reason is web.config authoring error.", name.c_str()));
    }
    return section.value();
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
