// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include <optional>
#include <vector>
#include <map>

#include "NonCopyable.h"
#include "StringHelpers.h"

#define CS_ASPNETCORE_COLLECTION_ITEM_NAME               L"name"
#define CS_ASPNETCORE_COLLECTION_ITEM_VALUE              L"value"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLES              L"environmentVariables"
#define CS_ASPNETCORE_STDOUT_LOG_FILE                    L"stdoutLogFile"
#define CS_ASPNETCORE_STDOUT_LOG_ENABLED                 L"stdoutLogEnabled"
#define CS_ASPNETCORE_PROCESS_EXE_PATH                   L"processPath"
#define CS_ASPNETCORE_PROCESS_ARGUMENTS                  L"arguments"
#define CS_ASPNETCORE_PROCESS_ARGUMENTS_DEFAULT          L""
#define CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT         L"startupTimeLimit"
#define CS_ASPNETCORE_PROCESS_SHUTDOWN_TIME_LIMIT        L"shutdownTimeLimit"
#define CS_ASPNETCORE_HOSTING_MODEL_OUTOFPROCESS         L"outofprocess"
#define CS_ASPNETCORE_HOSTING_MODEL_INPROCESS            L"inprocess"
#define CS_ASPNETCORE_HOSTING_MODEL                      L"hostingModel"
#define CS_ASPNETCORE_HANDLER_SETTINGS                   L"handlerSettings"
#define CS_ASPNETCORE_HANDLER_SET_CURRENT_DIRECTORY      L"setCurrentDirectory"
#define CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE        L"disableStartUpErrorPage"
#define CS_ENABLED                                       L"enabled"
#define CS_ASPNETCORE_HANDLER_CALL_STARTUP_HOOK          L"callStartupHook"
#define CS_ASPNETCORE_HANDLER_STACK_SIZE                 L"stackSize"
#define CS_ASPNETCORE_DETAILEDERRORS                     L"ASPNETCORE_DETAILEDERRORS"
#define CS_ASPNETCORE_ENVIRONMENT                        L"ASPNETCORE_ENVIRONMENT"
#define CS_DOTNET_ENVIRONMENT                            L"DOTNET_ENVIRONMENT"
#define CS_ANCM_LAUNCHER_PATH                            L"ANCM_LAUNCHER_PATH"
#define CS_ANCM_LAUNCHER_ARGS                            L"ANCM_LAUNCHER_ARGS"

class ConfigurationSection: NonCopyable
{
public:
    ConfigurationSection() = default;
    virtual ~ConfigurationSection() = default;
    virtual std::optional<std::wstring> GetString(const std::wstring& name) const = 0;
    virtual std::optional<bool> GetBool(const std::wstring& name) const = 0;
    virtual std::optional<DWORD> GetLong(const std::wstring& name) const = 0;
    virtual std::optional<DWORD> GetTimespan(const std::wstring& name) const = 0;

    virtual std::optional<std::shared_ptr<ConfigurationSection>> GetSection(const std::wstring& name) const = 0;
    virtual std::vector<std::shared_ptr<ConfigurationSection>> GetCollection() const = 0;

    std::wstring GetRequiredString(const std::wstring& name) const;
    bool GetRequiredBool(const std::wstring& name)  const;
    DWORD GetRequiredLong(const std::wstring& name)  const;
    DWORD GetRequiredTimespan(const std::wstring& name)  const;

    virtual std::vector<std::pair<std::wstring, std::wstring>> GetKeyValuePairs(const std::wstring& name) const;
    virtual std::map<std::wstring, std::wstring, ignore_case_comparer> GetMap(const std::wstring& name) const;

    virtual std::shared_ptr<ConfigurationSection> GetRequiredSection(const std::wstring & name) const;

protected:
    static void ThrowRequiredException(const std::wstring& name);
};

std::optional<std::wstring> find_element(const std::vector<std::pair<std::wstring, std::wstring>>& pairs, const std::wstring& name);
