// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

ASPNETCORE_CONFIG::~ASPNETCORE_CONFIG()
{
    //
    // the destructor will be called once IIS decides to recycle the module context (i.e., application)
    //
    if (!m_struApplication.IsEmpty())
    {
        APPLICATION_MANAGER::GetInstance()->RecycleApplication(m_struApplication.QueryStr());
    }
    if(m_pEnvironmentVariables != NULL)
    {
        m_pEnvironmentVariables->Clear();
        delete m_pEnvironmentVariables;
        m_pEnvironmentVariables = NULL;
    }
}

HRESULT
ASPNETCORE_CONFIG::GetConfig(
    _In_  IHttpContext            *pHttpContext,
    _Out_ ASPNETCORE_CONFIG     **ppAspNetCoreConfig
)
{
    HRESULT                 hr = S_OK;
    IHttpApplication       *pHttpApplication = pHttpContext->GetApplication();
    ASPNETCORE_CONFIG      *pAspNetCoreConfig = NULL;

    if (ppAspNetCoreConfig == NULL)
    {
        hr = E_INVALIDARG;
        goto Finished;
    }

    *ppAspNetCoreConfig = NULL;

    // potential bug if user sepcific config at virtual dir level
    pAspNetCoreConfig = (ASPNETCORE_CONFIG*)
        pHttpApplication->GetModuleContextContainer()->GetModuleContext(g_pModuleId);

    if (pAspNetCoreConfig != NULL)
    {
        *ppAspNetCoreConfig = pAspNetCoreConfig;
        pAspNetCoreConfig = NULL;
        goto Finished;
    }

    pAspNetCoreConfig = new ASPNETCORE_CONFIG;
    if (pAspNetCoreConfig == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pAspNetCoreConfig->Populate(pHttpContext);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = pHttpApplication->GetModuleContextContainer()->
        SetModuleContext(pAspNetCoreConfig, g_pModuleId);
    if (FAILED(hr))
    {
        if (hr == HRESULT_FROM_WIN32(ERROR_ALREADY_ASSIGNED))
        {
            delete pAspNetCoreConfig;

            pAspNetCoreConfig = (ASPNETCORE_CONFIG*)pHttpApplication->
                                 GetModuleContextContainer()->
                                 GetModuleContext(g_pModuleId);

            _ASSERT(pAspNetCoreConfig != NULL);

            hr = S_OK;
        }
        else
        {
            goto Finished;
        }
    }
    else
    {
        // set appliction info here instead of inside Populate()
        // as the destructor will delete the backend process 
        hr = pAspNetCoreConfig->QueryApplicationPath()->Copy(pHttpApplication->GetApplicationId());
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    *ppAspNetCoreConfig = pAspNetCoreConfig;
    pAspNetCoreConfig = NULL;

Finished:

    if (pAspNetCoreConfig != NULL)
    {
        delete pAspNetCoreConfig;
        pAspNetCoreConfig = NULL;
    }

    return hr;
}

HRESULT
ASPNETCORE_CONFIG::Populate(
    IHttpContext   *pHttpContext
)
{
    HRESULT                         hr = S_OK;
    STACK_STRU(strSiteConfigPath, 256);
    STRU                            strEnvName;
    STRU                            strEnvValue;
    STRU                            strExpandedEnvValue;
    IAppHostAdminManager           *pAdminManager = NULL;
    IAppHostElement                *pAspNetCoreElement = NULL;
    IAppHostElement                *pWindowsAuthenticationElement = NULL;
    IAppHostElement                *pBasicAuthenticationElement = NULL;
    IAppHostElement                *pAnonymousAuthenticationElement = NULL;
    IAppHostElement                *pEnvVarList = NULL;
    IAppHostElement                *pEnvVar = NULL;
    IAppHostElementCollection      *pEnvVarCollection = NULL;
    ULONGLONG                       ullRawTimeSpan = 0;
    ENUM_INDEX                      index;
    ENVIRONMENT_VAR_ENTRY*          pEntry = NULL;

    m_pEnvironmentVariables = new ENVIRONMENT_VAR_HASH();
    if (m_pEnvironmentVariables == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    if (FAILED(hr = m_pEnvironmentVariables->Initialize(37 /*prime*/)))
    {
        delete m_pEnvironmentVariables;
        m_pEnvironmentVariables = NULL;
        goto Finished;
    }

    pAdminManager = g_pHttpServer->GetAdminManager();

    hr = strSiteConfigPath.Copy(pHttpContext->GetApplication()->GetAppConfigPath());
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = pAdminManager->GetAdminSection(CS_WINDOWS_AUTHENTICATION_SECTION,
        strSiteConfigPath.QueryStr(),
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
            CS_AUTHENTICATION_ENABLED,
            &m_fWindowsAuthEnabled);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    hr = pAdminManager->GetAdminSection(CS_BASIC_AUTHENTICATION_SECTION,
        strSiteConfigPath.QueryStr(),
        &pBasicAuthenticationElement);
    if (FAILED(hr))
    {
        m_fBasicAuthEnabled = FALSE;
    }
    else
    {
        hr = GetElementBoolProperty(pBasicAuthenticationElement,
            CS_AUTHENTICATION_ENABLED,
            &m_fBasicAuthEnabled);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    hr = pAdminManager->GetAdminSection(CS_ANONYMOUS_AUTHENTICATION_SECTION,
        strSiteConfigPath.QueryStr(),
        &pAnonymousAuthenticationElement);
    if (FAILED(hr))
    {
        m_fAnonymousAuthEnabled = FALSE;
    }
    else
    {
        hr = GetElementBoolProperty(pAnonymousAuthenticationElement,
            CS_AUTHENTICATION_ENABLED,
            &m_fAnonymousAuthEnabled);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    hr = pAdminManager->GetAdminSection(CS_ASPNETCORE_SECTION,
        strSiteConfigPath.QueryStr(),
        &pAspNetCoreElement);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_PROCESS_EXE_PATH,
        &m_struProcessPath);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_PROCESS_ARGUMENTS,
        &m_struArguments);
    if (FAILED(hr))
    {
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

    hr = GetElementChildByName(pAspNetCoreElement,
        CS_ASPNETCORE_ENVIRONMENT_VARIABLES,
        &pEnvVarList);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = pEnvVarList->get_Collection(&pEnvVarCollection);
    if (FAILED(hr))
    {
        goto Finished;
    }

    for (hr = FindFirstElement(pEnvVarCollection, &index, &pEnvVar);
        SUCCEEDED(hr);
        hr = FindNextElement(pEnvVarCollection, &index, &pEnvVar))
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        if (FAILED(hr = GetElementStringProperty(pEnvVar,
            CS_ASPNETCORE_ENVIRONMENT_VARIABLE_NAME,
            &strEnvName)) ||
            FAILED(hr = GetElementStringProperty(pEnvVar,
                CS_ASPNETCORE_ENVIRONMENT_VARIABLE_VALUE,
                &strEnvValue)) ||
            FAILED(hr = strEnvName.Append(L"=")) ||
            FAILED(hr = STRU::ExpandEnvironmentVariables(strEnvValue.QueryStr(), &strExpandedEnvValue)))
        {
            goto Finished;
        }

        pEntry = new ENVIRONMENT_VAR_ENTRY();
        if (pEntry == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        if (FAILED(hr = pEntry->Initialize(strEnvName.QueryStr(), strExpandedEnvValue.QueryStr())) ||
            FAILED(hr = m_pEnvironmentVariables->InsertRecord(pEntry)))
        {
            goto Finished;
        }
        strEnvName.Reset();
        strEnvValue.Reset();
        strExpandedEnvValue.Reset();
        pEnvVar->Release();
        pEnvVar = NULL;
        pEntry->Dereference();
        pEntry = NULL;
    }

Finished:

    if (pAspNetCoreElement != NULL)
    {
        pAspNetCoreElement->Release();
        pAspNetCoreElement = NULL;
    }

    if (pEnvVarList != NULL)
    {
        pEnvVarList->Release();
        pEnvVarList = NULL;
    }

    if (pEnvVar != NULL)
    {
        pEnvVar->Release();
        pEnvVar = NULL;
    }

    if (pEnvVarCollection != NULL)
    {
        pEnvVarCollection->Release();
        pEnvVarCollection = NULL;
    }

    if (pEntry != NULL)
    {
        pEntry->Dereference();
        pEntry = NULL;
    }

    return hr;
}