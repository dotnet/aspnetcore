// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "InProcessOptions.h"

InProcessOptions::InProcessOptions(const ConfigurationSource &configurationSource) :
    m_fStdoutLogEnabled(false),
    m_fWindowsAuthEnabled(false),
    m_fBasicAuthEnabled(false),
    m_fAnonymousAuthEnabled(false),
    m_dwStartupTimeLimitInMS(INFINITE),
    m_dwShutdownTimeLimitInMS(INFINITE)
{
    auto const aspNetCoreSection = configurationSource.GetRequiredSection(CS_ASPNETCORE_SECTION);
    m_strArguments = aspNetCoreSection->GetString(CS_ASPNETCORE_PROCESS_ARGUMENTS).value_or(CS_ASPNETCORE_PROCESS_ARGUMENTS_DEFAULT);
    m_strProcessPath = aspNetCoreSection->GetRequiredString(CS_ASPNETCORE_PROCESS_EXE_PATH);
    m_fStdoutLogEnabled = aspNetCoreSection->GetRequiredBool(CS_ASPNETCORE_STDOUT_LOG_ENABLED);
    m_struStdoutLogFile = aspNetCoreSection->GetRequiredString(CS_ASPNETCORE_STDOUT_LOG_FILE);
    m_fDisableStartUpErrorPage = aspNetCoreSection->GetRequiredBool(CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE);
    m_environmentVariables = aspNetCoreSection->GetKeyValuePairs(CS_ASPNETCORE_ENVIRONMENT_VARIABLES);

    const auto basicAuthSection = configurationSource.GetSection(CS_BASIC_AUTHENTICATION_SECTION);
    m_fBasicAuthEnabled = basicAuthSection && basicAuthSection->GetBool(CS_ENABLED).value_or(false);

    const auto windowsAuthSection = configurationSource.GetSection(CS_WINDOWS_AUTHENTICATION_SECTION);
    m_fWindowsAuthEnabled = windowsAuthSection && windowsAuthSection->GetBool(CS_ENABLED).value_or(false);

    const auto anonAuthSection = configurationSource.GetSection(CS_ANONYMOUS_AUTHENTICATION_SECTION);
    m_fAnonymousAuthEnabled = anonAuthSection && anonAuthSection->GetBool(CS_ENABLED).value_or(false);
}
