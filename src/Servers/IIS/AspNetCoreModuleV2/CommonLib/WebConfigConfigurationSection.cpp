// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "WebConfigConfigurationSection.h"

#include "exceptions.h"
#include "ahutil.h"

std::optional<std::wstring> WebConfigConfigurationSection::GetString(const std::wstring& name) const
{
    CComBSTR result;
    if (FAILED_LOG(GetElementStringProperty(m_element, name.c_str(), &result.m_str)))
    {
        return std::nullopt;
    }

    return std::make_optional(std::wstring(result));
}

std::optional<bool> WebConfigConfigurationSection::GetBool(const std::wstring& name) const
{
    bool result;
    if (FAILED_LOG(GetElementBoolProperty(m_element, name.c_str(), &result)))
    {
        return std::nullopt;
    }

    return std::make_optional(result);
}

std::optional<DWORD> WebConfigConfigurationSection::GetLong(const std::wstring& name) const
{
    DWORD result;
    if (FAILED_LOG(GetElementDWORDProperty(m_element, name.c_str(), &result)))
    {
        return std::nullopt;
    }

    return std::make_optional(result);
}

std::optional<DWORD> WebConfigConfigurationSection::GetTimespan(const std::wstring& name) const
{
    ULONGLONG result;
    if (FAILED_LOG(GetElementRawTimeSpanProperty(m_element, name.c_str(), &result)))
    {
        return std::nullopt;
    }

    return std::make_optional(static_cast<DWORD>(result / 10000ull));
}

std::optional<std::shared_ptr<ConfigurationSection>> WebConfigConfigurationSection::GetSection(const std::wstring& name) const
{
    CComPtr<IAppHostElement> element = nullptr;

    if (FAILED_LOG(GetElementChildByName(m_element, name.c_str(), &element)))
    {
        return std::nullopt;
    }

    return std::make_optional(std::make_shared<WebConfigConfigurationSection>(element.Detach()));
}

std::vector<std::shared_ptr<ConfigurationSection>> WebConfigConfigurationSection::GetCollection() const
{
    std::vector<std::shared_ptr<ConfigurationSection>> elements;
    HRESULT findElementResult = S_OK;
    CComPtr<IAppHostElementCollection> elementCollection = nullptr;
    CComPtr<IAppHostElement>           collectionEntry = nullptr;
    ENUM_INDEX                         index{};

    THROW_IF_FAILED(m_element->get_Collection(&elementCollection));
    THROW_IF_FAILED(findElementResult = FindFirstElement(elementCollection, &index, &collectionEntry));

    while (findElementResult != S_FALSE)
    {

        elements.emplace_back(std::make_shared<WebConfigConfigurationSection>(collectionEntry.Detach()));

        collectionEntry.Release();

        THROW_IF_FAILED(findElementResult = FindNextElement(elementCollection, &index, &collectionEntry));
    }

    return elements;
}
