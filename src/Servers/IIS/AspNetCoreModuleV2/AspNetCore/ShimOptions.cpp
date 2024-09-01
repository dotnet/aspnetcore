// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "ShimOptions.h"

#include "StringHelpers.h"
#include "ConfigurationLoadException.h"
#include "Environment.h"

#define CS_ASPNETCORE_HANDLER_VERSION                    L"handlerVersion"
#define CS_ASPNETCORE_SHADOW_COPY                        L"enableShadowCopy"
#define CS_ASPNETCORE_SHADOW_COPY_DIRECTORY              L"shadowCopyDirectory"
#define CS_ASPNETCORE_CLEAN_SHADOW_DIRECTORY_CONTENT     L"cleanShadowCopyDirectory"
#define CS_ASPNETCORE_DISALLOW_ROTATE_CONFIG             L"disallowRotationOnConfigChange"
#define CS_ASPNETCORE_SHUTDOWN_DELAY                     L"shutdownDelay"
#define CS_ASPNETCORE_SHUTDOWN_DELAY_ENV                 L"ANCM_shutdownDelay"

ShimOptions::ShimOptions(const ConfigurationSource &configurationSource) :
        m_hostingModel(HOSTING_UNKNOWN),
        m_fStdoutLogEnabled(false)
{
    auto const section = configurationSource.GetRequiredSection(CS_ASPNETCORE_SECTION);
    auto hostingModel = section->GetString(CS_ASPNETCORE_HOSTING_MODEL).value_or(L"");

    if (hostingModel.empty() || equals_ignore_case(hostingModel, CS_ASPNETCORE_HOSTING_MODEL_OUTOFPROCESS))
    {
        m_hostingModel = HOSTING_OUT_PROCESS;
    }
    else if (equals_ignore_case(hostingModel, CS_ASPNETCORE_HOSTING_MODEL_INPROCESS))
    {
        m_hostingModel = HOSTING_IN_PROCESS;
    }
    else
    {
        throw ConfigurationLoadException(format(
            L"Unknown hosting model '%s'. Please specify either hostingModel=\"inprocess\" "
            "or hostingModel=\"outofprocess\" in the web.config file.", hostingModel.c_str()));
    }

    const auto handlerSettings = section->GetKeyValuePairs(CS_ASPNETCORE_HANDLER_SETTINGS);

    if (m_hostingModel == HOSTING_OUT_PROCESS)
    {
        m_strHandlerVersion = find_element(handlerSettings, CS_ASPNETCORE_HANDLER_VERSION).value_or(std::wstring());
    }

    auto enableShadowCopyElement = find_element(handlerSettings, CS_ASPNETCORE_SHADOW_COPY).value_or(std::wstring());
    m_fEnableShadowCopying = equals_ignore_case(L"true", enableShadowCopyElement);

    auto cleanShadowCopyDirectory = find_element(handlerSettings, CS_ASPNETCORE_CLEAN_SHADOW_DIRECTORY_CONTENT).value_or(std::wstring());
    m_fCleanShadowCopyDirectory = equals_ignore_case(L"true", cleanShadowCopyDirectory);

    m_strShadowCopyingDirectory = find_element(handlerSettings, CS_ASPNETCORE_SHADOW_COPY_DIRECTORY)
        .value_or(m_fEnableShadowCopying ? L"ShadowCopyDirectory" : std::wstring());

    auto disallowRotationOnConfigChange = find_element(handlerSettings, CS_ASPNETCORE_DISALLOW_ROTATE_CONFIG).value_or(std::wstring());
    m_fDisallowRotationOnConfigChange = equals_ignore_case(L"true", disallowRotationOnConfigChange);

    m_strProcessPath = section->GetRequiredString(CS_ASPNETCORE_PROCESS_EXE_PATH);
    m_strArguments = section->GetString(CS_ASPNETCORE_PROCESS_ARGUMENTS).value_or(CS_ASPNETCORE_PROCESS_ARGUMENTS_DEFAULT);
    m_fStdoutLogEnabled = section->GetRequiredBool(CS_ASPNETCORE_STDOUT_LOG_ENABLED);
    m_struStdoutLogFile = section->GetRequiredString(CS_ASPNETCORE_STDOUT_LOG_FILE);
    m_fDisableStartupPage = section->GetRequiredBool(CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE);

    auto environmentVariables = section->GetMap(CS_ASPNETCORE_ENVIRONMENT_VARIABLES);

    // Indexing into environment variable map will add a default entry if none is present
    // This is okay here as we throw away the map shortly after.
    // Process set environment variables are prioritized over web config variables.
    const auto detailedErrors = Environment::GetEnvironmentVariableValue(CS_ASPNETCORE_DETAILEDERRORS)
        .value_or(environmentVariables[CS_ASPNETCORE_DETAILEDERRORS]);
    const auto aspnetCoreEnvironment = Environment::GetEnvironmentVariableValue(CS_ASPNETCORE_ENVIRONMENT)
        .value_or(environmentVariables[CS_ASPNETCORE_ENVIRONMENT]);
    const auto dotnetEnvironment = Environment::GetEnvironmentVariableValue(CS_DOTNET_ENVIRONMENT)
        .value_or(environmentVariables[CS_DOTNET_ENVIRONMENT]);
    // We prefer the environment variables for LAUNCHER_PATH and LAUNCHER_ARGS
    m_strProcessPath = Environment::GetEnvironmentVariableValue(CS_ANCM_LAUNCHER_PATH)
        .value_or(m_strProcessPath);
    m_strArguments = Environment::GetEnvironmentVariableValue(CS_ANCM_LAUNCHER_ARGS)
        .value_or(m_strArguments);

    auto detailedErrorsEnabled = equals_ignore_case(L"1", detailedErrors) || equals_ignore_case(L"true", detailedErrors);
    auto aspnetCoreEnvironmentEnabled = equals_ignore_case(L"Development", aspnetCoreEnvironment);
    auto dotnetEnvironmentEnabled = equals_ignore_case(L"Development", dotnetEnvironment);

    m_fShowDetailedErrors = detailedErrorsEnabled || aspnetCoreEnvironmentEnabled || dotnetEnvironmentEnabled;

    // Specifies how long to delay (in milliseconds) after IIS tells us to stop before starting the application shutdown.
    // See StartShutdown in globalmodule to see how it's used.
    auto shutdownDelay = find_element(handlerSettings, CS_ASPNETCORE_SHUTDOWN_DELAY).value_or(std::wstring());
    if (shutdownDelay.empty())
    {
        // Fallback to environment variable if process specific config wasn't set
        shutdownDelay = Environment::GetEnvironmentVariableValue(CS_ASPNETCORE_SHUTDOWN_DELAY_ENV)
            .value_or(environmentVariables[CS_ASPNETCORE_SHUTDOWN_DELAY_ENV]);
        if (shutdownDelay.empty())
        {
            // Default if neither process specific config or environment variable aren't set
            m_fShutdownDelay = std::chrono::seconds(1);
        }
        else
        {
            SetShutdownDelay(shutdownDelay);
        }
    }
    else
    {
        SetShutdownDelay(shutdownDelay);
    }
}

void ShimOptions::SetShutdownDelay(const std::wstring& shutdownDelay)
{
    auto millsecondsValue = std::stoi(shutdownDelay);
    if (millsecondsValue < 0)
    {
        throw ConfigurationLoadException(format(
            L"'shutdownDelay' in web.config or '%s' environment variable is less than 0.", CS_ASPNETCORE_SHUTDOWN_DELAY_ENV));
    }
    m_fShutdownDelay = std::chrono::milliseconds(millsecondsValue);
}
