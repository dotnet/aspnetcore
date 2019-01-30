// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "InProcessOptions.h"
#include "InvalidOperationException.h"
#include "EventLog.h"

HRESULT InProcessOptions::Create(
    IHttpServer& pServer,
    IHttpSite* site,
    IHttpApplication& pHttpApplication,
    std::unique_ptr<InProcessOptions>& options)
{
    try
    {
        const WebConfigConfigurationSource configurationSource(pServer.GetAdminManager(), pHttpApplication);
        options = std::make_unique<InProcessOptions>(configurationSource, site);
    }
    catch (InvalidOperationException& ex)
    {
        EventLog::Error(
            ASPNETCORE_CONFIGURATION_LOAD_ERROR,
            ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
            ex.as_wstring().c_str());

        RETURN_CAUGHT_EXCEPTION();
    }
    catch (std::runtime_error& ex)
    {
        EventLog::Error(
            ASPNETCORE_CONFIGURATION_LOAD_ERROR,
            ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
            GetUnexpectedExceptionMessage(ex).c_str());

        RETURN_CAUGHT_EXCEPTION();
    }
    CATCH_RETURN();

    return S_OK;
}

InProcessOptions::InProcessOptions(const ConfigurationSource &configurationSource, IHttpSite* pSite) :
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
    m_environmentVariables = aspNetCoreSection->GetMap(CS_ASPNETCORE_ENVIRONMENT_VARIABLES);

    const auto handlerSettings = aspNetCoreSection->GetKeyValuePairs(CS_ASPNETCORE_HANDLER_SETTINGS);
    m_fSetCurrentDirectory = equals_ignore_case(find_element(handlerSettings, CS_ASPNETCORE_HANDLER_SET_CURRENT_DIRECTORY).value_or(L"true"), L"true");

    m_dwStartupTimeLimitInMS = aspNetCoreSection->GetRequiredLong(CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT) * 1000;
    m_dwShutdownTimeLimitInMS = aspNetCoreSection->GetRequiredLong(CS_ASPNETCORE_PROCESS_SHUTDOWN_TIME_LIMIT) * 1000;

    const auto basicAuthSection = configurationSource.GetSection(CS_BASIC_AUTHENTICATION_SECTION);
    m_fBasicAuthEnabled = basicAuthSection && basicAuthSection->GetBool(CS_ENABLED).value_or(false);

    const auto windowsAuthSection = configurationSource.GetSection(CS_WINDOWS_AUTHENTICATION_SECTION);
    m_fWindowsAuthEnabled = windowsAuthSection && windowsAuthSection->GetBool(CS_ENABLED).value_or(false);

    const auto anonAuthSection = configurationSource.GetSection(CS_ANONYMOUS_AUTHENTICATION_SECTION);
    m_fAnonymousAuthEnabled = anonAuthSection && anonAuthSection->GetBool(CS_ENABLED).value_or(false);

    if (pSite != nullptr)
    {
        m_bindingInformation = BindingInformation::Load(configurationSource, *pSite);
    }
}
