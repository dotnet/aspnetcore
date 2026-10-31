// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

HRESULT
RegisterTraceArea(
    IN          CONST WCHAR *   szTraceProviderName,
    IN          CONST WCHAR *   szTraceProviderGuid,
    IN          CONST WCHAR *   szAreaName,
    IN          CONST WCHAR *   szAreaValue
)
{
    HRESULT hr = NOERROR;
    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IAppHostElement>                pTraceProviderSection;
    CComPtr<IAppHostElementCollection>      pTraceProvidersCollection;
    CComPtr<IAppHostElement>                pTraceProvider;
    CComPtr<IAppHostElement>                pAreasElement;
    CComPtr<IAppHostElementCollection>      pAreasCollection;
    CComPtr<IAppHostElement>                pAreaElement;

    BSTR bstrAppHostConfigPath = SysAllocString( L"MACHINE/WEBROOT/APPHOST" );
    BSTR bstrTracingSection = SysAllocString( L"system.webServer/tracing/traceProviderDefinitions" );
    BSTR bstrAreas = SysAllocString( L"areas" );

    NAME_VALUE_PAIR ProviderProperties[] =
    {
        { L"name", CComVariant( szTraceProviderName )},
        { L"guid", CComVariant( szTraceProviderGuid )}
    };

    NAME_VALUE_PAIR AreaProperties[] =
    {
        { L"name", CComVariant( szAreaName )},
        { L"value", CComVariant( szAreaValue )}
    };

    if ( bstrAppHostConfigPath == NULL ||
         bstrTracingSection == NULL ||
         bstrAreas == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = CoCreateInstance( __uuidof( AppHostWritableAdminManager ),
                           NULL,
                           CLSCTX_INPROC_SERVER,
                           __uuidof( IAppHostWritableAdminManager ),
                           (VOID **)&pAdminMgr );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->GetAdminSection( bstrTracingSection,
                                     bstrAppHostConfigPath,
                                     &pTraceProviderSection );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pTraceProviderSection->get_Collection( &pTraceProvidersCollection );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetElementFromCollection( pTraceProvidersCollection,
                                  L"name",
                                  szTraceProviderName,
                                  &pTraceProvider );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Create the trace provider if it doesn't exist.
    //
    if ( hr == S_FALSE )
    {
        hr = AddElementToCollection( pTraceProvidersCollection,
                                     L"add",
                                     ProviderProperties,
                                     _countof( ProviderProperties ),
                                     &pTraceProvider );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    hr = pTraceProvider->GetElementByName( bstrAreas,
                                           &pAreasElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAreasElement->get_Collection( &pAreasCollection );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetElementFromCollection( pAreasCollection,
                                  L"name",
                                  szAreaName,
                                  &pAreaElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Add the trace area if it doesn't exist.
    //
    if ( hr != S_FALSE )
    {
        hr = S_OK;
        goto exit;
    }

    hr = AddElementToCollection( pAreasCollection,
                                 L"add",
                                 AreaProperties,
                                 _countof( AreaProperties ),
                                 NULL );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString( bstrAppHostConfigPath );
    SysFreeString( bstrTracingSection );
    SysFreeString( bstrAreas );
    
    return hr;
}

HRESULT
AddElementToCollection(
    IN IAppHostElementCollection *  pCollection,
    IN CONST WCHAR *                pElementName,
    IN NAME_VALUE_PAIR              rgProperties[],
    IN DWORD                        cProperties,
    OUT IAppHostElement **          ppElement
)
{
    HRESULT                     hr = NOERROR;
    CComPtr<IAppHostElement>    pElement;
    CComPtr<IAppHostProperty>   pProperty;

    BSTR bstrName = NULL;
    BSTR bstrElementName = SysAllocString( pElementName );

    if ( bstrElementName == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pCollection->CreateNewElement( bstrElementName,
                                        &pElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for ( DWORD Index = 0; Index < cProperties; Index ++ )
    {
        bstrName = SysAllocString( rgProperties[Index].Name );
        if ( bstrName == NULL )
        {
            hr = E_OUTOFMEMORY;
            DBGERROR_HR(hr);
            goto exit;
        }
        
        hr = pElement->GetPropertyByName( bstrName,
                                          &pProperty );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( rgProperties[Index].Value.vt == VT_ERROR )
        {
            hr = E_OUTOFMEMORY;
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pProperty->put_Value( rgProperties[Index].Value );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        SysFreeString( bstrName );
        pProperty.Detach()->Release();
    }

    hr = pCollection->AddElement( pElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( ppElement != NULL )
    {
        *ppElement = pElement.Detach();
    }

exit:

    SysFreeString( bstrElementName );
    SysFreeString( bstrName );

    return hr;
}

HRESULT
GetElementFromCollection(
    IN  IAppHostElementCollection * pCollection,
    IN  CONST WCHAR *               szIndex,
    IN  CONST WCHAR *               szExpectedPropertyValue,
    OUT IAppHostElement **          ppElement,
    OUT DWORD *                     pIndex
)
{
    HRESULT hr = NOERROR;
    DWORD   dwElementCount = 0;
    CComPtr<IAppHostElement>  pElement;
    CComPtr<IAppHostProperty> pProperty;
    
    BSTR bstrPropertyName = SysAllocString( szIndex );

    if ( bstrPropertyName == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pCollection->get_Count( &dwElementCount );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for ( DWORD Index = 0; Index < dwElementCount; Index ++ )
    {
        CComVariant value;

        hr = pCollection->get_Item( CComVariant( Index ),
                                    &pElement );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pElement->GetPropertyByName( bstrPropertyName,
                                          &pProperty );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pProperty->get_Value( &value );
        if ( value.vt == VT_ERROR )
        {
            hr = E_OUTOFMEMORY;
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( value.vt != VT_BSTR )
        {
            hr = HRESULT_FROM_WIN32( ERROR_INVALID_DATA );
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( _wcsicmp( value.bstrVal, szExpectedPropertyValue ) == 0 )
        {
            //
            // Element found.
            //
            *ppElement = pElement.Detach();
            if ( pIndex != NULL )
            {
                *pIndex = Index;
            }
            hr = S_OK;
            goto exit;
        }

        pProperty.Detach()->Release();
        pElement.Detach()->Release();
    }

    //
    // Element not found.
    //
    hr = S_FALSE;

exit:

    SysFreeString( bstrPropertyName );

    return hr;
}
