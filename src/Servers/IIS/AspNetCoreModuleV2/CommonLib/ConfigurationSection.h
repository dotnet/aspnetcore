// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include <optional>
#include <vector>
#include <map>

#include "NonCopyable.h"
#include "StringHelpers.h"

constexpr auto CS_ASPNETCORE_COLLECTION_ITEM_NAME = L"name";
constexpr auto CS_ASPNETCORE_COLLECTION_ITEM_VALUE = L"value";
constexpr auto CS_ASPNETCORE_ENVIRONMENT_VARIABLES = L"environmentVariables";
constexpr auto CS_ASPNETCORE_STDOUT_LOG_FILE = L"stdoutLogFile";
constexpr auto CS_ASPNETCORE_STDOUT_LOG_ENABLED = L"stdoutLogEnabled";
constexpr auto CS_ASPNETCORE_PROCESS_EXE_PATH = L"processPath";
constexpr auto CS_ASPNETCORE_PROCESS_ARGUMENTS = L"arguments";
constexpr auto CS_ASPNETCORE_PROCESS_ARGUMENTS_DEFAULT = L"";
constexpr auto CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT = L"startupTimeLimit";
constexpr auto CS_ASPNETCORE_PROCESS_SHUTDOWN_TIME_LIMIT = L"shutdownTimeLimit";
constexpr auto CS_ASPNETCORE_HOSTING_MODEL_OUTOFPROCESS = L"outofprocess";
constexpr auto CS_ASPNETCORE_HOSTING_MODEL_INPROCESS = L"inprocess";
constexpr auto CS_ASPNETCORE_HOSTING_MODEL = L"hostingModel";
constexpr auto CS_ASPNETCORE_HANDLER_SETTINGS = L"handlerSettings";
constexpr auto CS_ASPNETCORE_HANDLER_SET_CURRENT_DIRECTORY = L"setCurrentDirectory";
constexpr auto CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE = L"disableStartUpErrorPage";
constexpr auto CS_ENABLED = L"enabled";
constexpr auto CS_ASPNETCORE_HANDLER_CALL_STARTUP_HOOK = L"callStartupHook";
constexpr auto CS_ASPNETCORE_HANDLER_STACK_SIZE = L"stackSize";
constexpr auto CS_ASPNETCORE_SUPPRESS_RECYCLE_ON_STARTUP_TIMEOUT = L"suppressRecycleOnStartupTimeout";
constexpr auto CS_ASPNETCORE_DETAILEDERRORS = L"ASPNETCORE_DETAILEDERRORS";
constexpr auto CS_ASPNETCORE_ENVIRONMENT = L"ASPNETCORE_ENVIRONMENT";
constexpr auto CS_DOTNET_ENVIRONMENT = L"DOTNET_ENVIRONMENT";
constexpr auto CS_ANCM_LAUNCHER_PATH = L"ANCM_LAUNCHER_PATH";
constexpr auto CS_ANCM_LAUNCHER_ARGS = L"ANCM_LAUNCHER_ARGS";

class ConfigurationSection: NonCopyable
{
public:
    ConfigurationSection() = default;
    virtual ~ConfigurationSection() = default;
    [[nodiscard]] virtual std::optional<std::wstring> GetString(const std::wstring& name) const = 0;
    [[nodiscard]] virtual std::optional<bool> GetBool(const std::wstring& name) const = 0;
    [[nodiscard]] virtual std::optional<DWORD> GetLong(const std::wstring& name) const = 0;
    [[nodiscard]] virtual std::optional<DWORD> GetTimespan(const std::wstring& name) const = 0;

    [[nodiscard]] virtual std::optional<std::shared_ptr<ConfigurationSection>> GetSection(const std::wstring& name) const = 0;
    [[nodiscard]] virtual std::vector<std::shared_ptr<ConfigurationSection>> GetCollection() const = 0;

    [[nodiscard]] std::wstring GetRequiredString(const std::wstring& name) const;
    [[nodiscard]] bool GetRequiredBool(const std::wstring& name)  const;
    [[nodiscard]] DWORD GetRequiredLong(const std::wstring& name)  const;
    [[nodiscard]] DWORD GetRequiredTimespan(const std::wstring& name)  const;

    [[nodiscard]] virtual std::vector<std::pair<std::wstring, std::wstring>> GetKeyValuePairs(const std::wstring& name) const;
    [[nodiscard]] virtual std::map<std::wstring, std::wstring, ignore_case_comparer> GetMap(const std::wstring& name) const;

    [[nodiscard]] virtual std::shared_ptr<ConfigurationSection> GetRequiredSection(const std::wstring & name) const;

protected:
    static void ThrowRequiredException(const std::wstring& name);
};

std::optional<std::wstring> find_element(const std::vector<std::pair<std::wstring, std::wstring>>& pairs, const std::wstring& name);
