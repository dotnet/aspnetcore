// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <precomp.h>
#include <Wbemidl.h>

HRESULT
GetServiceCurrentState(
    __in LPCWSTR            pszServiceName,
    __out SERVICE_STATUS *  pServiceStatus
)
{
    HRESULT         hr = S_OK;
    SC_HANDLE       hServiceControlManager = NULL;
    SC_HANDLE       hService = NULL;

    hServiceControlManager = OpenSCManager( NULL, // Local machine
                                            NULL,
                                            STANDARD_RIGHTS_READ );
    if ( hServiceControlManager == NULL )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto Finished;
    }

    hService = OpenService( hServiceControlManager,
                            pszServiceName,
                            SERVICE_QUERY_STATUS );
    if ( hService == NULL )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto Finished;
    }

    if ( !QueryServiceStatus( hService,
                              pServiceStatus ) )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto Finished;
    }

Finished:

    if ( hService != NULL )
    {
        CloseServiceHandle( hService );
        hService = NULL;
    }

    if ( hServiceControlManager != NULL )
    {
        CloseServiceHandle( hService );
        hService = NULL;
    }

    return hr;
}

BOOL
IsServiceRunning(
    const SERVICE_STATUS & ServiceStatus
)
{
    switch( ServiceStatus.dwCurrentState )
    {
    case SERVICE_RUNNING:
    case SERVICE_START_PENDING:
    case SERVICE_CONTINUE_PENDING:
        return TRUE;
    default:
        return FALSE;
    }
}

HRESULT
IsQfeInstalled(
    __in LPCWSTR pszQfeName,
    __out BOOL * pfIsInstalled
)
{
    HRESULT hr = S_OK;
    CComPtr< IWbemLocator > pLocator;
    CComPtr< IWbemServices > pService;
    CComPtr< IEnumWbemClassObject > pEnumerator;
    ULONG Count = 0;
    CComPtr< IWbemClassObject > pProcessor;
    CComBSTR bstrNamespace;
    CComBSTR bstrQueryLanguage;
    CComBSTR bstrQuery;

    if ( FAILED( hr = bstrNamespace.Append( L"root\\CIMV2", 10 ) ) ||
         FAILED( hr = bstrQueryLanguage.Append( L"WQL", 3 ) ) ||
         FAILED( hr = bstrQuery.Append( L"SELECT HotFixID FROM Win32_QuickFixEngineering WHERE HotFixID='" ) ) ||
         FAILED( hr = bstrQuery.Append( pszQfeName ) ) ||
         FAILED( hr = bstrQuery.Append( L"'", 1 ) ) )
    {
        goto Finished;
    }

    hr = CoCreateInstance( __uuidof(WbemAdministrativeLocator),
                           NULL, // pUnkOuter
                           CLSCTX_INPROC_SERVER,
                           __uuidof(IWbemLocator),
                           reinterpret_cast< void** >( &pLocator ) );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pLocator->ConnectServer( bstrNamespace,
                                  NULL, // strUser
                                  NULL, // strPassword
                                  NULL, // strLocale
                                  WBEM_FLAG_CONNECT_USE_MAX_WAIT,
                                  NULL, // strAuthority
                                  NULL, // pCtx
                                  &pService );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    //
    // Set the proxy so that impersonation of the client occurs.
    //
    hr = CoSetProxyBlanket( pService,
                            RPC_C_AUTHN_DEFAULT,
                            RPC_C_AUTHZ_NONE,
                            NULL,
                            RPC_C_AUTHN_LEVEL_CONNECT,
                            RPC_C_IMP_LEVEL_IMPERSONATE,
                            NULL,
                            EOAC_NONE);
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pService->ExecQuery( bstrQueryLanguage,
                              bstrQuery,
                              WBEM_FLAG_FORWARD_ONLY,
                              NULL,
                              &pEnumerator );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = pEnumerator->Next( WBEM_INFINITE,
                            1L,
                            &pProcessor,
                            &Count );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    *pfIsInstalled = Count > 0;

Finished:

    return hr;
}

UINT
WINAPI
CheckForServicesRunningCA(
    MSIHANDLE hInstall
)
{
    HRESULT         hr = S_OK;
    BOOL            fIsServiceRunning = FALSE;
    SERVICE_STATUS  ServiceStatus;
    LPCWSTR         rgServiceNames[] = { L"WAS", L"WMSVC" };

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    //
    // Check if any pService is running.
    //
    for( DWORD Index = 0; Index < _countof( rgServiceNames ); Index ++ )
    {
        hr = GetServiceCurrentState( rgServiceNames[Index],
                                     &ServiceStatus );
        if ( hr == HRESULT_FROM_WIN32( ERROR_SERVICE_DOES_NOT_EXIST ) )
        {
            hr = S_OK;
        }
        else if ( FAILED( hr ) )
        {
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR,
                        L"Failed to query the state of the service '%s' hr=0x%x",
                        rgServiceNames[Index],
                        hr );
            DBGERROR_HR(hr);
            goto Finished;
        }
        else
        {
            fIsServiceRunning = IsServiceRunning( ServiceStatus );
            if ( fIsServiceRunning )
            {
                break;
            }
        }
    }

    if ( fIsServiceRunning )
    {
        BOOL fQfeInstalled = FALSE;

        hr = IsQfeInstalled( L"KB954438",
                             &fQfeInstalled );
        if ( FAILED( hr ) )
        {
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR,
                        L"Failed to query the hotfix 'KB949172' information hr=0x%x",
                        hr );
            DBGERROR_HR(hr);
            goto Finished;
        }

        if ( fQfeInstalled )
        {
            //
            // hotfix is already installed.
            //
            goto Finished;
        }

        IISLogClose();
        return LogMsiCustomActionError( hInstall, 30003 );
    }

Finished:

    IISLogClose();

    // TODO Wire up when Rollback CA's are wired up
    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_SUCCESS;
}
