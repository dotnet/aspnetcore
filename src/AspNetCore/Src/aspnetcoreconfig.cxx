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

    if( ppAspNetCoreConfig == NULL)
    {
        hr = E_INVALIDARG;
        goto Finished;
    }

    *ppAspNetCoreConfig = NULL;

    // potential bug if user sepcific config at virtual dir level
    pAspNetCoreConfig = (ASPNETCORE_CONFIG*) 
        pHttpApplication->GetModuleContextContainer()->GetModuleContext(g_pModuleId);

    if( pAspNetCoreConfig != NULL )
    {
        *ppAspNetCoreConfig = pAspNetCoreConfig;
        pAspNetCoreConfig = NULL;
        goto Finished;
    }

    pAspNetCoreConfig = new ASPNETCORE_CONFIG;
    if( pAspNetCoreConfig == NULL )
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pAspNetCoreConfig->Populate( pHttpContext );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pHttpApplication->GetModuleContextContainer()->
            SetModuleContext( pAspNetCoreConfig, g_pModuleId );
    if( FAILED( hr ) )
    {
        if( hr == HRESULT_FROM_WIN32( ERROR_ALREADY_ASSIGNED ) )
        {
            delete pAspNetCoreConfig;

            pAspNetCoreConfig = (ASPNETCORE_CONFIG*) pHttpApplication->
                                GetModuleContextContainer()->
                                GetModuleContext( g_pModuleId );

            _ASSERT( pAspNetCoreConfig != NULL );

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

    if( pAspNetCoreConfig != NULL )
    {
        delete pAspNetCoreConfig;
        pAspNetCoreConfig = NULL;
    }

    return hr;
}

VOID ReverseMultisz( MULTISZ     * pmszInput,
                     LPCWSTR       pszStr,
                     MULTISZ     * pmszOutput )
{
    if(pszStr == NULL) return;

    ReverseMultisz( pmszInput, pmszInput->Next( pszStr ), pmszOutput );

    pmszOutput->Append( pszStr );
}

HRESULT
ASPNETCORE_CONFIG::Populate(
    IHttpContext   *pHttpContext
)
{
    HRESULT                         hr = S_OK;
    STACK_STRU (                    strSiteConfigPath, 256); 
    STRU                            strEnvName;
    STRU                            strEnvValue;
    STRU                            strFullEnvVar;
    IAppHostAdminManager           *pAdminManager = NULL;
    IAppHostElement                *pAspNetCoreElement = NULL;
    IAppHostElement                *pEnvVarList = NULL;
    IAppHostElementCollection      *pEnvVarCollection = NULL;
    IAppHostElement                *pEnvVar = NULL;
    //IAppHostElement                *pRecycleOnFileChangeFileList = NULL;
    //IAppHostElementCollection      *pRecycleOnFileChangeFileCollection = NULL;
    //IAppHostElement                *pRecycleOnFileChangeFile = NULL;
    ULONGLONG                       ullRawTimeSpan = 0;
    ENUM_INDEX                      index;
    STRU                            strExpandedEnvValue;
    MULTISZ                         mszEnvironment;
    MULTISZ                         mszEnvironmentListReverse;
    MULTISZ                         mszEnvNames;
    LPWSTR                          pszEnvName;
    LPCWSTR                         pcszEnvName;
    LPCWSTR                         pszEnvString;
    STRU                            strFilePath;

    pAdminManager = g_pHttpServer->GetAdminManager();

    hr = strSiteConfigPath.Copy( pHttpContext->GetApplication()->GetAppConfigPath() );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pAdminManager->GetAdminSection( CS_ASPNETCORE_SECTION,
                                         strSiteConfigPath.QueryStr(),
                                         &pAspNetCoreElement );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = GetElementStringProperty( pAspNetCoreElement, 
                                   CS_ASPNETCORE_PROCESS_EXE_PATH, 
                                   &m_struProcessPath );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = GetElementStringProperty( pAspNetCoreElement, 
                                   CS_ASPNETCORE_PROCESS_ARGUMENTS, 
                                   &m_struArguments );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = GetElementDWORDProperty( pAspNetCoreElement,
                                  CS_ASPNETCORE_RAPID_FAILS_PER_MINUTE,
                                  &m_dwRapidFailsPerMinute );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    //
    // rapidFailsPerMinute cannot be greater than 100.
    //

    if(m_dwRapidFailsPerMinute > MAX_RAPID_FAILS_PER_MINUTE)
    {
        m_dwRapidFailsPerMinute = MAX_RAPID_FAILS_PER_MINUTE;
    }

    hr = GetElementDWORDProperty( pAspNetCoreElement,
                                  CS_ASPNETCORE_PROCESSES_PER_APPLICATION,
                                  &m_dwProcessesPerApplication );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = GetElementDWORDProperty( 
            pAspNetCoreElement,
            CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT,
            &m_dwStartupTimeLimitInMS 
            );
    if( FAILED( hr ) )
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

    hr = GetElementBoolProperty( pAspNetCoreElement,
                                 CS_ASPNETCORE_FORWARD_WINDOWS_AUTH_TOKEN,
                                 &m_fForwardWindowsAuthToken );
    if( FAILED( hr ) )
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
    if( FAILED( hr ) )
    {
        goto Finished;
    }
    
    m_dwRequestTimeoutInMS = (DWORD)TIMESPAN_IN_MILLISECONDS(ullRawTimeSpan);

    hr = GetElementBoolProperty( pAspNetCoreElement,
                                 CS_ASPNETCORE_STDOUT_LOG_ENABLED,
                                 &m_fStdoutLogEnabled );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = GetElementStringProperty( pAspNetCoreElement, 
                                   CS_ASPNETCORE_STDOUT_LOG_FILE, 
                                   &m_struStdoutLogFile );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = GetElementChildByName( pAspNetCoreElement,
                                CS_ASPNETCORE_ENVIRONMENT_VARIABLES,
                                &pEnvVarList );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pEnvVarList->get_Collection( &pEnvVarCollection );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    for( hr = FindFirstElement( pEnvVarCollection, &index, &pEnvVar ) ;
         SUCCEEDED( hr ) ;
         hr = FindNextElement( pEnvVarCollection, &index, &pEnvVar ) )
    {
        if( hr == S_FALSE )
        {
            hr = S_OK;
            break;
        }

        hr = GetElementStringProperty( pEnvVar, 
                CS_ASPNETCORE_ENVIRONMENT_VARIABLE_NAME, 
                &strEnvName);
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        hr = GetElementStringProperty( pEnvVar, 
                CS_ASPNETCORE_ENVIRONMENT_VARIABLE_VALUE, 
                &strEnvValue);
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        hr = strFullEnvVar.Append(strEnvName);
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        hr = strFullEnvVar.Append(L"=");
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        pszEnvName = strFullEnvVar.QueryStr();
        while( pszEnvName != NULL && *pszEnvName != '\0')
        {
            *pszEnvName = towupper( *pszEnvName );
            pszEnvName++;
        }

        if( !mszEnvNames.FindString( strFullEnvVar ) )
        {
            if( !mszEnvNames.Append( strFullEnvVar ) )
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }
        }

        hr = STRU::ExpandEnvironmentVariables( strEnvValue.QueryStr(), &strExpandedEnvValue );
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        hr = strFullEnvVar.Append(strExpandedEnvValue);
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        if( !mszEnvironment.Append(strFullEnvVar) )
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        strExpandedEnvValue.Reset();
        strFullEnvVar.Reset();

        pEnvVar->Release();
        pEnvVar = NULL;
    }

    // basically the following logic is to select

    ReverseMultisz( &mszEnvironment, 
                    mszEnvironment.First(), 
                    &mszEnvironmentListReverse );

    pcszEnvName = mszEnvNames.First();
    while(pcszEnvName != NULL)
    {
        pszEnvString = mszEnvironmentListReverse.First();
        while( pszEnvString != NULL )
        {
            if(wcsstr(pszEnvString, pcszEnvName) != NULL)
            {
                if(!m_mszEnvironment.Append(pszEnvString))
                {
                    hr = E_OUTOFMEMORY;
                    goto Finished;
                }
                break;
            }
            pszEnvString = mszEnvironmentListReverse.Next(pszEnvString);
        }
        pcszEnvName = mszEnvNames.Next(pcszEnvName);
    }

    //
    // let's disable this feature for now
    //
    // get all files listed in recycleOnFileChange
    /*
    hr = GetElementChildByName( pAspNetCoreElement,
                                CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE,
                                &pRecycleOnFileChangeFileList );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pRecycleOnFileChangeFileList->get_Collection( &pRecycleOnFileChangeFileCollection );
    if( FAILED( hr ) )
    {
        goto Finished;
    }

    for( hr = FindFirstElement( pRecycleOnFileChangeFileCollection, &index, &pRecycleOnFileChangeFile ) ;
         SUCCEEDED( hr ) ;
         hr = FindNextElement( pRecycleOnFileChangeFileCollection, &index, &pRecycleOnFileChangeFile ) )
    {
        if( hr == S_FALSE )
        {
            hr = S_OK;
            break;
        }

        hr = GetElementStringProperty( pRecycleOnFileChangeFile, 
                CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE_FILE_PATH, 
                &strFilePath);
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        if(!m_mszRecycleOnFileChangeFiles.Append( strFilePath ))
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        strFilePath.Reset();
        pRecycleOnFileChangeFile->Release();
        pRecycleOnFileChangeFile = NULL;
    }
    */

Finished:

    if( pAspNetCoreElement != NULL )
    {
        pAspNetCoreElement->Release();
        pAspNetCoreElement = NULL;
    }

    if( pEnvVarList != NULL )
    {
        pEnvVarList->Release();
        pEnvVarList = NULL;
    }

    if( pEnvVar != NULL )
    {
        pEnvVar->Release();
        pEnvVar = NULL;
    }

    if( pEnvVarCollection != NULL )
    {
        pEnvVarCollection->Release();
        pEnvVarCollection = NULL;
    }

 /*   if( pRecycleOnFileChangeFileCollection != NULL )
    {
        pRecycleOnFileChangeFileCollection->Release();
        pRecycleOnFileChangeFileCollection = NULL;
    }

    if( pRecycleOnFileChangeFileList != NULL )
    {
        pRecycleOnFileChangeFileList->Release();
        pRecycleOnFileChangeFileList = NULL;
    }

    if( pRecycleOnFileChangeFile != NULL )
    {
        pRecycleOnFileChangeFile->Release();
        pRecycleOnFileChangeFile = NULL;
    }*/

    return hr;
}