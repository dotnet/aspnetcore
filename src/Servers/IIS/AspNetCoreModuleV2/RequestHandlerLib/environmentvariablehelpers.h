// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include <map>
#include "Environment.h"

class ENVIRONMENT_VAR_HELPERS
{

public:
    static
    VOID
    CopyToMultiSz(
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        STRU     strTemp;
        MULTISZ   *pMultiSz = static_cast<MULTISZ *>(pvData);
        DBG_ASSERT(pMultiSz);
        DBG_ASSERT(pEntry);
        strTemp.Copy(pEntry->QueryName());
        strTemp.Append(pEntry->QueryValue());
        pMultiSz->Append(strTemp.QueryStr());
    }

    static
    std::map<std::wstring, std::wstring, ignore_case_comparer>
    InitEnvironmentVariablesTable
    (
        _In_ const std::map<std::wstring, std::wstring, ignore_case_comparer>& pInEnvironmentVarTable,
        _In_ BOOL                           fWindowsAuthEnabled,
        _In_ BOOL                           fBasicAuthEnabled,
        _In_ BOOL                           fAnonymousAuthEnabled,
        _In_ BOOL                           fAddHostingStartup,
        _In_ PCWSTR                         pApplicationPhysicalPath,
        _In_ PCWSTR                         pHttpsPort
    )
    {
        std::map<std::wstring, std::wstring, ignore_case_comparer> environmentVariables = pInEnvironmentVarTable;

        environmentVariables.insert_or_assign(ASPNETCORE_IIS_PHYSICAL_PATH_ENV_STR, pApplicationPhysicalPath);
        if (pHttpsPort)
        {
            environmentVariables.try_emplace(ASPNETCORE_ANCM_HTTPS_PORT_ENV_STR, pHttpsPort);
        }

        std::wstring strIisAuthEnvValue;
        if (fWindowsAuthEnabled)
        {
            strIisAuthEnvValue.append(ASPNETCORE_IIS_AUTH_WINDOWS);
        }
        if (fBasicAuthEnabled)
        {
            strIisAuthEnvValue.append(ASPNETCORE_IIS_AUTH_BASIC);
        }
        if (fAnonymousAuthEnabled)
        {
            strIisAuthEnvValue.append(ASPNETCORE_IIS_AUTH_ANONYMOUS);
        }
        if (strIisAuthEnvValue.empty())
        {
            strIisAuthEnvValue.append(ASPNETCORE_IIS_AUTH_NONE);
        }

        environmentVariables.insert_or_assign(ASPNETCORE_IIS_AUTH_ENV_STR, strIisAuthEnvValue);

        if (fAddHostingStartup && environmentVariables.count(HOSTING_STARTUP_ASSEMBLIES_ENV_STR) == 0)
        {
            auto hostingStartupValues = Environment::GetEnvironmentVariableValue(HOSTING_STARTUP_ASSEMBLIES_ENV_STR).value_or(L"");

            if (hostingStartupValues.find(HOSTING_STARTUP_ASSEMBLIES_ENV_STR) == std::wstring::npos)
            {
                hostingStartupValues += std::wstring(L";") + HOSTING_STARTUP_ASSEMBLIES_VALUE;
            }

            environmentVariables.insert_or_assign(HOSTING_STARTUP_ASSEMBLIES_ENV_STR, hostingStartupValues);
        }

        auto preferEnvironmentVariablesSetting = Environment::GetEnvironmentVariableValue(ANCM_PREFER_ENVIRONMENT_VARIABLES_ENV_STR).value_or(L"false");
        auto preferEnvironmentVariables = equals_ignore_case(L"1", preferEnvironmentVariablesSetting) || equals_ignore_case(L"true", preferEnvironmentVariablesSetting);

        for (auto& environmentVariable : environmentVariables)
        {
            if (preferEnvironmentVariables)
            {
                auto env = Environment::GetEnvironmentVariableValue(environmentVariable.first);
                if (env.has_value())
                {
                    environmentVariable.second = env.value();
                }
                else
                {
                    environmentVariable.second = Environment::ExpandEnvironmentVariables(environmentVariable.second);
                }
            }
            else
            {
                environmentVariable.second = Environment::ExpandEnvironmentVariables(environmentVariable.second);
            }
        }

        return environmentVariables;
    }

    static
    std::map<std::wstring, std::wstring, ignore_case_comparer>
    AddWebsocketEnabledToEnvironmentVariables
    (
        _Inout_ const std::map<std::wstring, std::wstring, ignore_case_comparer>& pInEnvironmentVarTable,
        _In_ BOOL                           fWebsocketsEnabled
    )
    {
        std::map<std::wstring, std::wstring, ignore_case_comparer> environmentVariables = pInEnvironmentVarTable;
        environmentVariables.insert_or_assign(ASPNETCORE_IIS_WEBSOCKETS_SUPPORTED_ENV_STR, fWebsocketsEnabled ? L"true" : L"false");
        return environmentVariables;
    }
};

