// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"


HRESULT GetSidStringForAccount(
    const WCHAR * szAccount,
    __inout STRU * pstr
    )
{
    HRESULT hr = NOERROR;
    BOOL success = TRUE;
    DWORD sidSize = 0;
    DWORD domainSize = 0;
    PSID psid = NULL;
    SID_NAME_USE sidKind;
    LPWSTR pszSid = NULL;
    LPWSTR pszDomain = NULL;

    LookupAccountNameW(NULL,
            szAccount,
            NULL,
            &sidSize,
            NULL,
            &domainSize,
            &sidKind);
    
    _ASSERT ( sidSize  > 0 );

    psid = new BYTE[sidSize];
    if( psid == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
        DBGERROR_HR(hr);  
        goto exit;
    }

    pszDomain = new WCHAR[domainSize];
    if( pszDomain == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
        DBGERROR_HR(hr);  
        goto exit;
    }

    success = LookupAccountNameW(NULL,
            szAccount,
            psid,
            &sidSize,
            pszDomain,
            &domainSize,
            &sidKind);
    if( !success )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    success = ConvertSidToStringSidW( psid, &pszSid );
    if( !success )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    _ASSERT(pszSid);

    hr = pstr->Copy(pszSid);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    if (pszSid)
    {
        LocalFree( pszSid );
        pszSid = NULL;
    }

    delete [] psid;
    psid = NULL;
    delete [] pszDomain;
    pszDomain = NULL;

    return hr;
}

UINT 
WINAPI 
ScheduleHttpListenerCA(
    IN MSIHANDLE hInstall,
    IN const WCHAR * pszCAName,
    IIS_HTTP_LISTENER_CA_TYPE caType
    )
{
    HRESULT hr = S_OK;
    UINT status = ERROR_SUCCESS;
    BOOL scheduleDefferedCA = FALSE;

    PMSIHANDLE hDatabase;
    PMSIHANDLE hView;
    PMSIHANDLE hRecord;

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISHttpListener`.`Name`, "
            L"`IISHttpListener`.`Component_`, "
            L"`IISHttpListener`.`Account`, "
            L"`IISHttpListener`.`Prefix` "
        L"FROM `IISHttpListener`";

    enum { CA_HTTP_NAME = 1, 
           CA_HTTP_COMPONENT,
           CA_HTTP_ACCOUNT,
           CA_HTTP_PREFIX};

    CA_DATA_WRITER cadata;

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        STACK_STRU( strName, 128 );
        STACK_STRU( strComponent, 128 );
        STACK_STRU( strAccount, 128 );
        STACK_STRU( strPrefix, 128 );
        INSTALLSTATE installStateCurrent;
        INSTALLSTATE installStateAction;
        BOOL scheduleThisComponent = FALSE;

        hr = MsiUtilRecordGetString( hRecord,
                                     CA_HTTP_NAME,
                                     &strName );
        if ( FAILED( hr ) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = MsiUtilRecordGetString( hRecord,
                                     CA_HTTP_COMPONENT,
                                     &strComponent );
        if ( FAILED( hr ) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }        
        
        status = MsiGetComponentStateW( hInstall,
                                        strComponent.QueryStr(),
                                        &installStateCurrent,
                                        &installStateAction );
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

        if (MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {
            if (caType == IIS_HTTP_LISTENER_CA_INSTALL)
            {
                scheduleThisComponent = TRUE;
                scheduleDefferedCA = TRUE;
            }
        }
        else if (MsiUtilIsUnInstalling( installStateCurrent, installStateAction))
        {
            if (caType == IIS_HTTP_LISTENER_CA_UNINSTALL)
            {
                scheduleThisComponent = TRUE;
                scheduleDefferedCA = TRUE;
            }

        }

        if (scheduleThisComponent)
        {
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HTTP_ACCOUNT,
                                         &strAccount );
            if ( FAILED( hr ) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HTTP_PREFIX,
                                         &strPrefix );
            if ( FAILED( hr ) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata.Write( strName.QueryStr(), strName.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilFormatString( hInstall, &strAccount);
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata.Write( strAccount.QueryStr(), strAccount.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilFormatString( hInstall, &strPrefix);
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata.Write( strPrefix.QueryStr(), strPrefix.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
        }
    }

    if ( scheduleDefferedCA )
    {
        hr = MsiUtilScheduleDeferredAction( hInstall,
            pszCAName,
            cadata.QueryData() );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

exit:
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
}

UINT
__stdcall
ExecuteHttpListenerCA(
    IN MSIHANDLE hInstall,
    IIS_HTTP_LISTENER_CA_TYPE caType
    )
{
    HRESULT hr = NOERROR;
    ULONG status = NO_ERROR;
    BOOL bHttpInitialized = FALSE;
    CA_DATA_READER cadata;
    WCHAR * szName = NULL;
    WCHAR * szAccount = NULL;
    WCHAR * szPrefix = NULL;
    HTTPAPI_VERSION httpVersion1 = HTTPAPI_VERSION_1;

    hr = cadata.LoadDeferredCAData( hInstall );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    status = HttpInitialize( httpVersion1, HTTP_INITIALIZE_CONFIG, NULL );
    if ( status != NO_ERROR )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error calling HttpInitialize, hr=0x%x", hr);
        goto exit;
    }
    bHttpInitialized = TRUE;

    while ( SUCCEEDED(hr = cadata.Read( &szName)) )
    {
        const WCHAR * securityDescriptorFormat = L"D:(A;;GX;;;%s)";
        HTTP_SERVICE_CONFIG_URLACL_SET inputConfigInfo;
        STACK_STRU( strSid, 128 );
        STACK_STRU( strSecurityString, 128 );

        hr = cadata.Read( &szAccount);
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = cadata.Read( &szPrefix);
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = GetSidStringForAccount(szAccount, &strSid);
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = strSecurityString.SafeSnwprintf(securityDescriptorFormat, strSid.QueryStr());
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        inputConfigInfo.KeyDesc.pUrlPrefix = szPrefix;
        inputConfigInfo.ParamDesc.pStringSecurityDescriptor = strSecurityString.QueryStr();
        
        status = HttpDeleteServiceConfiguration(NULL,
            HttpServiceConfigUrlAclInfo,
            &inputConfigInfo,
            sizeof(inputConfigInfo),
            NULL);

        if (status == E_INVALIDARG)
        {
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error calling Http API. Please make sure that the URL and Account information specified is correct.");
        }
        
        if ( (status != NO_ERROR) && (status != ERROR_FILE_NOT_FOUND) )
        {
            hr = HRESULT_FROM_WIN32( status );
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error calling HttpDeleteServiceConfiguration for account '%s', prefix '%s', securityDescriptor '%s', hr=0x%x", 
                szAccount,
                szPrefix,
                strSecurityString.QueryStr(),
                hr);
            goto exit;
        }

        if ( caType == IIS_HTTP_LISTENER_CA_INSTALL)
        {
            status = HttpSetServiceConfiguration(NULL,
                HttpServiceConfigUrlAclInfo,
                &inputConfigInfo,
                sizeof(inputConfigInfo),
                NULL);
            if (status == E_INVALIDARG)
            {
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error calling Http API. Please make sure that the URL and Account information specified is correct.");
            }
            if ( status != NO_ERROR )
            {
                hr = HRESULT_FROM_WIN32( status );
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error calling HttpSetServiceConfiguration for account '%s', prefix '%s', securityDescriptor '%s', hr=0x%x", 
                    szAccount,
                    szPrefix,
                    strSecurityString.QueryStr(),
                    hr);
                goto exit;
            }
        }
    }
    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:
    if (bHttpInitialized)
    {
        HttpTerminate ( HTTP_INITIALIZE_CONFIG , NULL);
    }

    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
}

    //for(inputConfigInfo.dwToken = 0; /* condition inside */ ; inputConfigInfo.dwToken++)
    //{
    //    HTTP_SERVICE_CONFIG_URLACL_SET * pOutputConfigInfo = NULL;
    //    ULONG outputConfigInfoSize = 0;

    //    while (status == ERROR_INSUFFICIENT_BUFFER)
    //    {
    //        delete pOutputConfigInfo;
    //        pOutputConfigInfo = NULL;
    //        pOutputConfigInfo = new BYTE[outputConfigInfoSize];
    //        if (pOutputConfigInfo == NULL)
    //        {
    //            hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
    //            DBGERROR_HR(hr);  
    //            goto exit;
    //        }

    //        status = HttpQueryServiceConfiguration ( NULL,
    //            HttpServiceConfigUrlAclInfo,
    //            &inputConfigInfo,
    //            sizeof(inputConfigInfo),
    //            pOutputConfigInfo,
    //            outputConfigInfoSize,
    //            &outputConfigInfoSize,
    //            NULL);
    //    } //end while

    //     _ASSERT (status != ERROR_INSUFFICIENT_BUFFER)
    //    
    //    if (status == NO_ERROR)
    //    {
    //        //lookup output info

    //    }
    //    else if (status == ERROR_NO_MORE_ITEMS)
    //    {
    //        //we're done
    //        break;
    //    }
    //    else
    //    {
    //        hr = HRESULT_FROM_WIN32( status );
    //        DBGERROR_HR(hr);
    //        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error calling HttpQueryServiceConfiguration, hr=0x%x", hr);
    //        goto exit;
    //    }
    //} //end for

UINT 
WINAPI 
ScheduleInstallHttpListenerCA(
    IN MSIHANDLE hInstall
    )
{
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));
    UINT retVal = ScheduleHttpListenerCA(hInstall, L"ExecuteInstallHttpListener", IIS_HTTP_LISTENER_CA_INSTALL);
    IISLogClose();
    return retVal;
}

UINT 
WINAPI 
ScheduleUnInstallHttpListenerCA(
    IN MSIHANDLE hInstall
    )
{
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));
    UINT retVal = ScheduleHttpListenerCA(hInstall, L"ExecuteUnInstallHttpListener", IIS_HTTP_LISTENER_CA_UNINSTALL);
    IISLogClose();
    return retVal;
}
UINT
__stdcall
ExecuteInstallHttpListenerCA(
    IN MSIHANDLE hInstall
    )
{
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));
    UINT retVal = ExecuteHttpListenerCA(hInstall, IIS_HTTP_LISTENER_CA_INSTALL);
    IISLogClose();
    return retVal;
}

UINT
__stdcall
ExecuteUnInstallHttpListenerCA(
    IN      MSIHANDLE   hInstall
    )
{
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));
    UINT retVal = ExecuteHttpListenerCA(hInstall, IIS_HTTP_LISTENER_CA_UNINSTALL); 
    IISLogClose();
    return retVal;
}
