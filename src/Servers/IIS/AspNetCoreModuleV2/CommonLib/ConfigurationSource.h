// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include <string>
#include "NonCopyable.h"
#include "ConfigurationSection.h"

constexpr auto CS_ASPNETCORE_SECTION = L"system.webServer/aspNetCore";
constexpr auto CS_WINDOWS_AUTHENTICATION_SECTION = L"system.webServer/security/authentication/windowsAuthentication";
constexpr auto CS_BASIC_AUTHENTICATION_SECTION = L"system.webServer/security/authentication/basicAuthentication";
constexpr auto CS_ANONYMOUS_AUTHENTICATION_SECTION = L"system.webServer/security/authentication/anonymousAuthentication";
constexpr auto CS_MAX_REQUEST_BODY_SIZE_SECTION = L"system.webServer/security/requestFiltering";

class ConfigurationSource: NonCopyable
{
public:
    ConfigurationSource() = default;
    virtual ~ConfigurationSource() = default;
    [[nodiscard]] virtual std::shared_ptr<ConfigurationSection> GetSection(const std::wstring& name) const = 0;
    [[nodiscard]] std::shared_ptr<ConfigurationSection> GetRequiredSection(const std::wstring& name) const;
};
