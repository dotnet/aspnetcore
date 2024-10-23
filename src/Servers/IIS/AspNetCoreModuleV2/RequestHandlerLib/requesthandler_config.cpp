// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "requesthandler_config.h"
#include "debugutil.h"
#include "environmentvariablehash.h"
#include "exceptions.h"
#include "config_utility.h"
#include "Environment.h"

REQUESTHANDLER_CONFIG::~REQUESTHANDLER_CONFIG()
{
    if (m_ppStrArguments != nullptr)
    {
        delete[] m_ppStrArguments;
        m_ppStrArguments = nullptr;
    }
}

HRESULT
REQUESTHANDLER_CONFIG::CreateRequestHandlerConfig(
    _In_  IHttpServer             *pHttpServer,
    _In_  IHttpSite               *pSite,
    _In_  IHttpApplication        *pHttpApplication,
    _Out_ REQUESTHANDLER_CONFIG  **ppAspNetCoreConfig
)
{
    HRESULT                 hr = S_OK;
    REQUESTHANDLER_CONFIG  *pRequestHandlerConfig = nullptr;
    STRU                    struHostFxrDllLocation;
    STRU                    struExeLocation;

    try
    {
        if (ppAspNetCoreConfig == nullptr)
        {
            hr = E_INVALIDARG;
            goto Finished;
        }

        *ppAspNetCoreConfig = nullptr;

        pRequestHandlerConfig = new REQUESTHANDLER_CONFIG;

        hr = pRequestHandlerConfig->Populate(pHttpServer, pSite, pHttpApplication);
        if (FAILED(hr))
        {
            goto Finished;
        }

        // set appliction info here instead of inside Populate()
        // as the destructor will delete the backend process
        hr = pRequestHandlerConfig->QueryApplicationPath()->Copy(pHttpApplication->GetApplicationId());
        if (FAILED(hr))
        {
            goto Finished;
        }

        *ppAspNetCoreConfig = pRequestHandlerConfig;
        pRequestHandlerConfig = nullptr;
    }
    catch (std::bad_alloc&)
    {
        hr = E_OUTOFMEMORY;
    }

Finished:

    if (pRequestHandlerConfig != nullptr)
    {
        delete pRequestHandlerConfig;
        pRequestHandlerConfig = nullptr;
    }

    return hr;
}

HRESULT
REQUESTHANDLER_CONFIG::Populate(
    IHttpServer    *pHttpServer,
    IHttpSite      *pSite,
    IHttpApplication   *pHttpApplication
)
{
    STACK_STRU(strHostingModel, 300);
    HRESULT                         hr = S_OK;
    STRU                            strEnvName;
    STRU                            strEnvValue;
    STRU                            strExpandedEnvValue;
    STRU                            strApplicationFullPath;
    IAppHostAdminManager           *pAdminManager = nullptr;
    IAppHostElement                *pAspNetCoreElement = nullptr;
    IAppHostElement                *pWindowsAuthenticationElement = nullptr;
    IAppHostElement                *pBasicAuthenticationElement = nullptr;
    IAppHostElement                *pAnonymousAuthenticationElement = nullptr;
    ULONGLONG                       ullRawTimeSpan = 0;
    DWORD                           dwCounter = 0;
    DWORD                           dwPosition = 0;
    WCHAR*                          pszPath = nullptr;
    BSTR                            bstrWindowAuthSection = nullptr;
    BSTR                            bstrBasicAuthSection = nullptr;
    BSTR                            bstrAnonymousAuthSection = nullptr;
    BSTR                            bstrAspNetCoreSection = nullptr;
    std::optional<std::wstring> launcherPathEnv;
    std::optional<std::wstring> launcherArgsEnv;

    pAdminManager = pHttpServer->GetAdminManager();
    try
    {
        WebConfigConfigurationSource source(pAdminManager, *pHttpApplication);
        if (pSite != nullptr)
        {
            m_struHttpsPort.Copy(BindingInformation::GetHttpsPort(BindingInformation::Load(source, *pSite)).c_str());
        }

        m_pEnvironmentVariables = source.GetSection(CS_ASPNETCORE_SECTION)->GetMap(CS_ASPNETCORE_ENVIRONMENT_VARIABLES);
    }
    catch (...)
    {
        FINISHED_IF_FAILED(OBSERVE_CAUGHT_EXCEPTION());
    }

    hr = m_struConfigPath.Copy(pHttpApplication->GetAppConfigPath());
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = m_struApplicationPhysicalPath.Copy(pHttpApplication->GetApplicationPhysicalPath());
    if (FAILED(hr))
    {
        goto Finished;
    }

    pszPath = m_struConfigPath.QueryStr();
    while (pszPath[dwPosition] != NULL)
    {
        if (pszPath[dwPosition] == '/')
        {
            dwCounter++;
            if (dwCounter == 4)
                break;
        }
        dwPosition++;
    }

    if (dwCounter == 4)
    {
        hr = m_struApplicationVirtualPath.Copy(pszPath + dwPosition);
    }
    else
    {
        hr = m_struApplicationVirtualPath.Copy(L"/");
    }

    // Will setup the application virtual path.
    if (FAILED(hr))
    {
        goto Finished;
    }

    bstrWindowAuthSection = SysAllocString(CS_WINDOWS_AUTHENTICATION_SECTION);
    if (bstrWindowAuthSection == nullptr)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    hr = pAdminManager->GetAdminSection(bstrWindowAuthSection,
        m_struConfigPath.QueryStr(),
        &pWindowsAuthenticationElement);
    if (FAILED(hr))
    {
        // assume the corresponding authen was not enabled
        // as the section may get deleted by user in some HWC case
        // ToDo: log a warning to event log
        m_fWindowsAuthEnabled = FALSE;
    }
    else
    {
        hr = GetElementBoolProperty(pWindowsAuthenticationElement,
            CS_ENABLED,
            &m_fWindowsAuthEnabled);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    bstrBasicAuthSection = SysAllocString(CS_BASIC_AUTHENTICATION_SECTION);
    if (bstrBasicAuthSection == nullptr)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pAdminManager->GetAdminSection(bstrBasicAuthSection,
        m_struConfigPath.QueryStr(),
        &pBasicAuthenticationElement);
    if (FAILED(hr))
    {
        m_fBasicAuthEnabled = FALSE;
    }
    else
    {
        hr = GetElementBoolProperty(pBasicAuthenticationElement,
            CS_ENABLED,
            &m_fBasicAuthEnabled);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    bstrAnonymousAuthSection = SysAllocString(CS_ANONYMOUS_AUTHENTICATION_SECTION);
    if (bstrAnonymousAuthSection == nullptr)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pAdminManager->GetAdminSection(bstrAnonymousAuthSection,
        m_struConfigPath.QueryStr(),
        &pAnonymousAuthenticationElement);
    if (FAILED(hr))
    {
        m_fAnonymousAuthEnabled = FALSE;
    }
    else
    {
        hr = GetElementBoolProperty(pAnonymousAuthenticationElement,
            CS_ENABLED,
            &m_fAnonymousAuthEnabled);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    bstrAspNetCoreSection = SysAllocString(CS_ASPNETCORE_SECTION);
    if (bstrAspNetCoreSection == nullptr)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    hr = pAdminManager->GetAdminSection(bstrAspNetCoreSection,
        m_struConfigPath.QueryStr(),
        &pAspNetCoreElement);
    if (FAILED(hr))
    {
        goto Finished;
    }

    // We prefer the environment variables for LAUNCHER_PATH and LAUNCHER_ARGS
    try
    {
        launcherPathEnv = Environment::GetEnvironmentVariableValue(CS_ANCM_LAUNCHER_PATH);
        launcherArgsEnv = Environment::GetEnvironmentVariableValue(CS_ANCM_LAUNCHER_ARGS);
    }
    catch(...)
    {
        FINISHED_IF_FAILED(E_FAIL);
    }

    if (launcherPathEnv.has_value())
    {
        hr = m_struProcessPath.Copy(launcherPathEnv.value().c_str());
        FINISHED_IF_FAILED(hr);
    }
    else
    {
        hr = GetElementStringProperty(pAspNetCoreElement,
            CS_ASPNETCORE_PROCESS_EXE_PATH,
            &m_struProcessPath);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    if (launcherArgsEnv.has_value())
    {
        hr = m_struArguments.Copy(launcherArgsEnv.value().c_str());
        FINISHED_IF_FAILED(hr);
    }
    else
    {
        hr = GetElementStringProperty(pAspNetCoreElement,
            CS_ASPNETCORE_PROCESS_ARGUMENTS,
            &m_struArguments);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    hr = GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_HOSTING_MODEL,
        &strHostingModel);
    if (FAILED(hr))
    {
        // Swallow this error for backward compatability
        // Use default behavior for empty string
        hr = S_OK;
    }

    if (strHostingModel.IsEmpty() || strHostingModel.Equals(L"outofprocess", TRUE))
    {
        m_hostingModel = HOSTING_OUT_PROCESS;
    }
    else if (strHostingModel.Equals(L"inprocess", TRUE))
    {
        m_hostingModel = HOSTING_IN_PROCESS;
    }
    else
    {
        // block unknown hosting value
        hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        goto Finished;
    }

    hr = GetElementDWORDProperty(pAspNetCoreElement,
        CS_ASPNETCORE_RAPID_FAILS_PER_MINUTE,
        &m_dwRapidFailsPerMinute);
    if (FAILED(hr))
    {
        goto Finished;
    }

    //
    // rapidFailsPerMinute cannot be greater than 100.
    //
    if (m_dwRapidFailsPerMinute > MAX_RAPID_FAILS_PER_MINUTE)
    {
        m_dwRapidFailsPerMinute = MAX_RAPID_FAILS_PER_MINUTE;
    }

    hr = GetElementDWORDProperty(pAspNetCoreElement,
        CS_ASPNETCORE_PROCESSES_PER_APPLICATION,
        &m_dwProcessesPerApplication);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = GetElementDWORDProperty(
        pAspNetCoreElement,
        CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT,
        &m_dwStartupTimeLimitInMS
    );
    if (FAILED(hr))
    {
        goto Finished;
    }

    m_dwStartupTimeLimitInMS *= MILLISECONDS_IN_ONE_SECOND;

    hr = GetElementDWORDProperty(
        pAspNetCoreElement,
        CS_ASPNETCORE_PROCESS_SHUTDOWN_TIME_LIMIT,
        &m_dwShutdownTimeLimitInMS
    );
    if (FAILED(hr))
    {
        goto Finished;
    }
    m_dwShutdownTimeLimitInMS *= MILLISECONDS_IN_ONE_SECOND;

    hr = GetElementBoolProperty(pAspNetCoreElement,
        CS_ASPNETCORE_FORWARD_WINDOWS_AUTH_TOKEN,
        &m_fForwardWindowsAuthToken);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = GetElementBoolProperty(pAspNetCoreElement,
        CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE,
        &m_fDisableStartUpErrorPage);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = GetElementRawTimeSpanProperty(
        pAspNetCoreElement,
        CS_ASPNETCORE_WINHTTP_REQUEST_TIMEOUT,
        &ullRawTimeSpan
    );
    if (FAILED(hr))
    {
        goto Finished;
    }

    m_dwRequestTimeoutInMS = (DWORD)TIMESPAN_IN_MILLISECONDS(ullRawTimeSpan);

    hr = GetElementBoolProperty(pAspNetCoreElement,
        CS_ASPNETCORE_STDOUT_LOG_ENABLED,
        &m_fStdoutLogEnabled);
    if (FAILED(hr))
    {
        goto Finished;
    }
    hr = GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_STDOUT_LOG_FILE,
        &m_struStdoutLogFile);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = ConfigUtility::FindEnableOutOfProcessConsoleRedirection(pAspNetCoreElement, m_fEnableOutOfProcessConsoleRedirection);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = ConfigUtility::FindForwardResponseConnectionHeader(pAspNetCoreElement, m_struForwardResponseConnectionHeader);
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:

    if (pAspNetCoreElement != nullptr)
    {
        pAspNetCoreElement->Release();
        pAspNetCoreElement = nullptr;
    }

    if (pWindowsAuthenticationElement != nullptr)
    {
        pWindowsAuthenticationElement->Release();
        pWindowsAuthenticationElement = nullptr;
    }

    if (pAnonymousAuthenticationElement != nullptr)
    {
        pAnonymousAuthenticationElement->Release();
        pAnonymousAuthenticationElement = nullptr;
    }

    if (pBasicAuthenticationElement != nullptr)
    {
        pBasicAuthenticationElement->Release();
        pBasicAuthenticationElement = nullptr;
    }

    return hr;
}
