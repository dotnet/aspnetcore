// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.h"

HRESULT
SetElementProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    IN          CONST VARIANT *     varPropValue
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostProperty>   pPropElement;

    BSTR bstrPropName = SysAllocString( szPropName );

    if( !bstrPropName )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pElement->GetPropertyByName( bstrPropName,
                                      &pPropElement );
    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pPropElement->put_Value( *varPropValue );
    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

exit:

    if( bstrPropName )
    {
        SysFreeString( bstrPropName );
        bstrPropName = NULL;
    }

    return hr;
}

HRESULT
SetElementStringProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    IN          CONST WCHAR *       szPropValue
    )
{
    VARIANT varPropValue;
    VariantInit(&varPropValue);

    HRESULT hr = VariantAssign(&varPropValue, szPropValue);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = SetElementProperty(pElement, szPropName, &varPropValue);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    VariantClear(&varPropValue);
    return hr;
}

HRESULT
GetElementStringProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    OUT         BSTR *              pbstrPropValue
    )
{
    HRESULT hr = S_OK;
    BSTR bstrPropName = SysAllocString( szPropName );
    IAppHostProperty* pProperty = NULL;

    *pbstrPropValue = NULL;

    if (!bstrPropName)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pElement->GetPropertyByName( bstrPropName, &pProperty );
    if (FAILED(hr))
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pProperty->get_StringValue( pbstrPropValue );
    if (FAILED(hr))
    {
        DBGERROR_HR( hr );
        goto exit;
    }

exit:

    if (pProperty)
    {
        pProperty->Release();
    }

    if (bstrPropName)
    {
        SysFreeString( bstrPropName );
    }

    return hr;
}


HRESULT
GetElementStringProperty(
    IN          IAppHostElement *   pElement,
    IN          CONST WCHAR *       szPropName,
    OUT         STRU *              pstrPropValue
    )
{
    HRESULT hr = S_OK;
    BSTR bstrPropName = SysAllocString( szPropName );
    IAppHostProperty* pProperty = NULL;
    BSTR bstrPropValue = NULL;

    if (!bstrPropName)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pElement->GetPropertyByName( bstrPropName, &pProperty );
    if (FAILED(hr))
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pProperty->get_StringValue( &bstrPropValue );
    if (FAILED(hr))
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pstrPropValue->Copy(bstrPropValue);
    if (FAILED(hr))
    {
        DBGERROR_HR( hr );
        goto exit;
    }

exit:

    if (pProperty)
    {
        pProperty->Release();
    }

    if (bstrPropValue)
    {
        SysFreeString( bstrPropValue );
    }

    if (bstrPropName)
    {
        SysFreeString( bstrPropName );
    }

    return hr;
}

HRESULT
GetElementChildByName(
    IN IAppHostElement *    pElement,
    IN LPCWSTR              pszElementName,
    OUT IAppHostElement **  ppChildElement
)
{
    BSTR bstrElementName = SysAllocString(pszElementName);
    if (bstrElementName == NULL)
    {
        return E_OUTOFMEMORY;
    }
    HRESULT hr = pElement->GetElementByName(bstrElementName,
                                            ppChildElement);
    SysFreeString(bstrElementName);
    return hr;
}

HRESULT
GetElementBoolProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT bool *           pBool
)
{
    BOOL fValue;
    HRESULT hr = GetElementBoolProperty(pElement,
                                        pszPropertyName,
                                        &fValue);
    if (SUCCEEDED(hr))
    {
        *pBool = !!fValue;
    }
    return hr;
}

HRESULT
GetElementBoolProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT BOOL *           pBool
)
{
    HRESULT hr = S_OK;
    IAppHostProperty * pProperty = NULL;
    VARIANT            varValue;

    VariantInit( &varValue );

    BSTR bstrPropertyName = SysAllocString(pszPropertyName);
    if ( bstrPropertyName == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    // Now ask for the property and if it succeeds it is returned directly back.
    hr = pElement->GetPropertyByName( bstrPropertyName, &pProperty );
    if ( FAILED ( hr ) )
    {
       goto exit;
    }

    // Now let's get the property and then extract it from the Variant.
    hr = pProperty->get_Value( &varValue );
    if ( FAILED ( hr ) )
    {
         goto exit;
    }

    hr = VariantChangeType( &varValue, &varValue, 0, VT_BOOL );
    if ( FAILED ( hr ) )
    {
         goto exit;
    }

    // extract the value
    *pBool = ( V_BOOL( &varValue ) == VARIANT_TRUE );

exit:

    VariantClear( &varValue );

    if ( bstrPropertyName != NULL )
    {
        SysFreeString( bstrPropertyName );
        bstrPropertyName = NULL;
    }

    if ( pProperty != NULL )
    {
        pProperty->Release();
        pProperty = NULL;
    }

    return hr;

}

HRESULT
GetElementDWORDProperty(
    IN  IAppHostElement * pSitesCollectionEntry,
    IN  LPCWSTR           pwszName,
    OUT DWORD *           pdwValue
)
{
    HRESULT            hr = S_OK;
    IAppHostProperty * pProperty = NULL;
    VARIANT            varValue;

    VariantInit( &varValue );

    BSTR bstrName = SysAllocString(pwszName);
    if ( bstrName == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto error;
    }

    hr = pSitesCollectionEntry->GetPropertyByName( bstrName,
                                                   &pProperty );
    if ( FAILED ( hr ) )
    {
        goto error;
    }

    hr = pProperty->get_Value( &varValue );
    if ( FAILED ( hr ) )
    {
         goto error;
    }

    hr = VariantChangeType( &varValue, &varValue, 0, VT_UI4 );
    if ( FAILED ( hr ) )
    {
         goto error;
    }

    // extract the value
    *pdwValue = varValue.ulVal;

error:

    VariantClear( &varValue );

    if ( pProperty != NULL )
    {
        pProperty->Release();
        pProperty = NULL;
    }

    if ( bstrName != NULL )
    {
        SysFreeString( bstrName );
        bstrName = NULL;
    }

    return hr;
}

HRESULT
GetElementLONGLONGProperty(
    IN  IAppHostElement * pSitesCollectionEntry,
    IN  LPCWSTR           pwszName,
    OUT LONGLONG *        pllValue
)
{
    HRESULT            hr = S_OK;
    IAppHostProperty * pProperty = NULL;
    VARIANT            varValue;

    VariantInit( &varValue );

    BSTR bstrName = SysAllocString(pwszName);
    if ( bstrName == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto error;
    }

    hr = pSitesCollectionEntry->GetPropertyByName( bstrName,
                                                   &pProperty );
    if ( FAILED ( hr ) )
    {
        goto error;
    }

    hr = pProperty->get_Value( &varValue );
    if ( FAILED ( hr ) )
    {
         goto error;
    }

    hr = VariantChangeType( &varValue, &varValue, 0, VT_I8 );
    if ( FAILED ( hr ) )
    {
         goto error;
    }

    // extract the value
    *pllValue = varValue.ulVal;

error:

    VariantClear( &varValue );

    if ( pProperty != NULL )
    {
        pProperty->Release();
        pProperty = NULL;
    }

    if ( bstrName != NULL )
    {
        SysFreeString( bstrName );
        bstrName = NULL;
    }

    return hr;
}

HRESULT
GetElementRawTimeSpanProperty(
    IN IAppHostElement * pElement,
    IN LPCWSTR           pszPropertyName,
    OUT ULONGLONG *      pulonglong
)
{
    HRESULT hr = S_OK;
    IAppHostProperty * pProperty = NULL;
    VARIANT            varValue;

    VariantInit( &varValue );

    BSTR bstrPropertyName = SysAllocString(pszPropertyName);
    if ( bstrPropertyName == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
        goto Finished;
    }

    // Now ask for the property and if it succeeds it is returned directly back
    hr = pElement->GetPropertyByName( bstrPropertyName, &pProperty );
    if ( FAILED ( hr ) )
    {
       goto Finished;
    }

    // Now let's get the property and then extract it from the Variant.
    hr = pProperty->get_Value( &varValue );
    if ( FAILED ( hr ) )
    {
         goto Finished;
    }

    hr = VariantChangeType( &varValue, &varValue, 0, VT_UI8 );
    if ( FAILED ( hr ) )
    {
         goto Finished;
    }

    // extract the value
    *pulonglong = varValue.ullVal;


Finished:

    VariantClear( &varValue );

    if ( bstrPropertyName != NULL )
    {
        SysFreeString( bstrPropertyName );
        bstrPropertyName = NULL;
    }

    if ( pProperty != NULL )
    {
        pProperty->Release();
        pProperty = NULL;
    }

    return hr;

} // end of Config_GetRawTimeSpanProperty

HRESULT
DeleteElementFromCollection(
    IAppHostElementCollection           *pCollection,
    CONST WCHAR *                       szKeyName,
    CONST WCHAR *                       szKeyValue,
    ULONG                               BehaviorFlags,
    BOOL *                              pfDeleted
    )
{
    HRESULT hr = NOERROR;
    ULONG index;

    VARIANT varIndex;
    VariantInit( &varIndex );

    *pfDeleted = FALSE;

    hr = FindElementInCollection(
             pCollection,
             szKeyName,
             szKeyValue,
             BehaviorFlags,
             &index
             );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if (hr == S_FALSE)
    {
        //
        // Not found.
        //

        goto exit;
    }

    varIndex.vt = VT_UI4;
    varIndex.ulVal = index;

    hr = pCollection->DeleteElement( varIndex );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    *pfDeleted = TRUE;

exit:

    return hr;
}

HRESULT
DeleteAllElementsFromCollection(
    IAppHostElementCollection           *pCollection,
    CONST WCHAR *                       szKeyName,
    CONST WCHAR *                       szKeyValue,
    ULONG                               BehaviorFlags,
    UINT *                              pNumDeleted
    )
{
    HRESULT hr = S_OK;
    UINT numDeleted = 0;
    BOOL fDeleted = TRUE;

    while (fDeleted)
    {
        hr = DeleteElementFromCollection(
                 pCollection,
                 szKeyName,
                 szKeyValue,
                 BehaviorFlags,
                 &fDeleted
                 );

        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            break;
        }

        if (fDeleted)
        {
            numDeleted++;
        }
    }

    *pNumDeleted = numDeleted;
    return hr;
}

BOOL
FindCompareCaseSensitive(
    CONST WCHAR * szLookupValue,
    CONST WCHAR * szKeyValue
    )
{
    return !wcscmp(szLookupValue, szKeyValue);
}

BOOL
FindCompareCaseInsensitive(
    CONST WCHAR * szLookupValue,
    CONST WCHAR * szKeyValue
    )
{
    return !_wcsicmp(szLookupValue, szKeyValue);
}

typedef
BOOL
(*PFN_FIND_COMPARE_PROC)(
    CONST WCHAR *szLookupValue,
    CONST WCHAR *szKeyValue
    );

HRESULT
FindElementInCollection(
    IAppHostElementCollection           *pCollection,
    CONST WCHAR *                       szKeyName,
    CONST WCHAR *                       szKeyValue,
    ULONG                               BehaviorFlags,
    OUT   ULONG *                       pIndex
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement>        pElement;
    CComPtr<IAppHostProperty>       pKeyProperty;

    VARIANT varIndex;
    VariantInit( &varIndex );

    VARIANT varKeyValue;
    VariantInit( &varKeyValue );

    DWORD   count;
    DWORD   i;

    BSTR bstrKeyName = NULL;
    PFN_FIND_COMPARE_PROC compareProc;

    compareProc = (BehaviorFlags & FIND_ELEMENT_CASE_INSENSITIVE)
                      ? &FindCompareCaseInsensitive
                      : &FindCompareCaseSensitive;

    bstrKeyName = SysAllocString( szKeyName );
    if( !bstrKeyName )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pCollection->get_Count( &count );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for( i = 0; i < count; i++ )
    {
        varIndex.vt = VT_UI4;
        varIndex.ulVal = i;

        hr = pCollection->get_Item( varIndex,
                                    &pElement );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto tryNext;
        }

        hr = pElement->GetPropertyByName( bstrKeyName,
                                          &pKeyProperty );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto tryNext;
        }

        hr = pKeyProperty->get_Value( &varKeyValue );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto tryNext;
        }

        if ((compareProc)(szKeyValue, varKeyValue.bstrVal))
        {
            *pIndex = i;
            break;
        }

tryNext:

        pElement.Release();
        pKeyProperty.Release();

        VariantClear( &varKeyValue );
    }

    if (i >= count)
    {
        hr = S_FALSE;
    }

exit:

    SysFreeString( bstrKeyName );
    VariantClear( &varKeyValue );

    return hr;
}

HRESULT
VariantAssign(
    IN OUT      VARIANT *       pv,
    IN          CONST WCHAR *   sz
    )
{
    if( !pv || !sz )
    {
        return E_INVALIDARG;
    }

    HRESULT hr = NOERROR;

    BSTR bstr = SysAllocString( sz );
    if( !bstr )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = VariantClear( pv );
    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    pv->vt = VT_BSTR;
    pv->bstrVal = bstr;
    bstr = NULL;

exit:

    SysFreeString( bstr );

    return hr;
}

HRESULT
GetLocationFromFile(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szLocationPath,
    OUT     IAppHostConfigLocation **   ppLocation,
    OUT     BOOL *                      pFound
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostConfigLocationCollection>   pLocationCollection;
    CComPtr<IAppHostConfigLocation>             pLocation;

    BSTR bstrLocationPath = NULL;

    *ppLocation = NULL;
    *pFound = FALSE;

    hr = GetLocationCollection( pAdminMgr,
                                szConfigPath,
                                &pLocationCollection );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    DWORD count;
    DWORD i;
    VARIANT varIndex;
    VariantInit( &varIndex );

    hr = pLocationCollection->get_Count( &count );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for( i = 0; i < count; i++ )
    {
        varIndex.vt = VT_UI4;
        varIndex.ulVal = i;

        hr = pLocationCollection->get_Item( varIndex,
                                            &pLocation );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pLocation->get_Path( &bstrLocationPath );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if( 0 == wcscmp ( szLocationPath, bstrLocationPath ) )
        {
            *pFound = TRUE;
            *ppLocation = pLocation.Detach();
            break;
        }


        pLocation.Release();

        SysFreeString( bstrLocationPath );
        bstrLocationPath = NULL;
    }

exit:

    SysFreeString( bstrLocationPath );

    return hr;
}

HRESULT
GetSectionFromLocation(
    IN      IAppHostConfigLocation *            pLocation,
    IN      CONST WCHAR *                       szSectionName,
    OUT     IAppHostElement **                  ppSectionElement,
    OUT     BOOL *                              pFound
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement>    pSectionElement;

    DWORD count;
    DWORD i;

    VARIANT varIndex;
    VariantInit( &varIndex );

    BSTR bstrSectionName = NULL;

    *pFound = FALSE;
    *ppSectionElement = NULL;

    hr = pLocation->get_Count( &count );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for( i = 0; i < count; i++ )
    {
        varIndex.vt = VT_UI4;
        varIndex.ulVal = i;


        hr = pLocation->get_Item( varIndex,
                                  &pSectionElement );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pSectionElement->get_Name( &bstrSectionName );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if( 0 == wcscmp ( szSectionName, bstrSectionName ) )
        {
            *pFound = TRUE;
            *ppSectionElement = pSectionElement.Detach();
            break;
        }

        pSectionElement.Release();

        SysFreeString( bstrSectionName );
        bstrSectionName = NULL;
    }

exit:

    SysFreeString( bstrSectionName );

    return hr;
}


HRESULT
GetAdminElement(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName,
    OUT     IAppHostElement **          pElement
)
{
    HRESULT hr = S_OK;

    BSTR bstrConfigPath = SysAllocString(szConfigPath);
    BSTR bstrElementName = SysAllocString(szElementName);

    if (bstrConfigPath == NULL || bstrElementName == NULL)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->GetAdminSection( bstrElementName,
                                     bstrConfigPath,
                                     pElement );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    if ( bstrElementName != NULL )
    {
        SysFreeString(bstrElementName);
        bstrElementName = NULL;
    }
    if ( bstrConfigPath != NULL )
    {
        SysFreeString(bstrConfigPath);
        bstrConfigPath = NULL;
    }

    return hr;
}


HRESULT
ClearAdminElement(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName
    )
{
    CComPtr<IAppHostElement> pElement;

    HRESULT hr = GetAdminElement(
        pAdminMgr,
        szConfigPath,
        szElementName,
        &pElement
    );

    if (FAILED(hr))
    {
        if (hr == HRESULT_FROM_WIN32(ERROR_NOT_FOUND))
        {
            hr = S_OK;
        }
        else
        {
            DBGERROR_HR(hr);
        }

        goto exit;
    }

    hr = pElement->Clear();

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}


HRESULT
ClearElementFromAllSites(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName
    )
{
    CComPtr<IAppHostElementCollection> pSitesCollection;
    CComPtr<IAppHostElement> pSiteElement;
    CComPtr<IAppHostChildElementCollection> pChildCollection;
    ENUM_INDEX index;
    BOOL found;

    //
    // Enumerate the sites, remove the specified elements.
    //

    HRESULT hr = GetSitesCollection(
        pAdminMgr,
        szConfigPath,
        &pSitesCollection
    );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for (hr = FindFirstElement(pSitesCollection, &index, &pSiteElement) ;
         SUCCEEDED(hr) ;
         hr = FindNextElement(pSitesCollection, &index, &pSiteElement))
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        hr = pSiteElement->get_ChildElements(&pChildCollection);

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if (pChildCollection)
        {
            hr = ClearChildElementsByName(
                    pChildCollection,
                    szElementName,
                    &found
                    );

            if (FAILED(hr))
            {
                DBGERROR_HR(hr);
                goto exit;
            }
        }

        pSiteElement.Release();
    }

exit:

    return hr;

}


HRESULT
ClearElementFromAllLocations(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    IN      CONST WCHAR *               szElementName
    )
{
    CComPtr<IAppHostConfigLocationCollection> pLocationCollection;
    CComPtr<IAppHostConfigLocation> pLocation;
    CComPtr<IAppHostChildElementCollection> pChildCollection;
    ENUM_INDEX index;

    //
    // Enum the <location> tags, remove the specified elements.
    //

    HRESULT hr = GetLocationCollection(
        pAdminMgr,
        szConfigPath,
        &pLocationCollection
    );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for (hr = FindFirstLocation(pLocationCollection, &index, &pLocation) ;
         SUCCEEDED(hr) ;
         hr = FindNextLocation(pLocationCollection, &index, &pLocation))
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        hr = ClearLocationElements(pLocation, szElementName);

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        pLocation.Release();
    }

exit:

    return hr;

}

HRESULT
ClearLocationElements(
    IN      IAppHostConfigLocation *    pLocation,
    IN      CONST WCHAR *               szElementName
    )
{
    HRESULT hr;
    CComPtr<IAppHostElement> pElement;
    ENUM_INDEX index;
    BOOL matched;

    for (hr = FindFirstLocationElement(pLocation, &index, &pElement) ;
         SUCCEEDED(hr) ;
         hr = FindNextLocationElement(pLocation, &index, &pElement))
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        hr = CompareElementName(pElement, szElementName, &matched);

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if (matched)
        {
            pElement->Clear();
        }

        pElement.Release();
    }

exit:

    return hr;
}

HRESULT
CompareElementName(
    IN      IAppHostElement *           pElement,
    IN      CONST WCHAR *               szNameToMatch,
    OUT     BOOL *                      pMatched
    )
{
    BSTR bstrElementName = NULL;

    *pMatched = FALSE;  // until proven otherwise

    HRESULT hr = pElement->get_Name(&bstrElementName);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if( 0 == wcscmp ( szNameToMatch, bstrElementName ) )
    {
        *pMatched = TRUE;
    }

exit:

    SysFreeString(bstrElementName);
    return hr;
}


HRESULT
ClearChildElementsByName(
    IN      IAppHostChildElementCollection *    pCollection,
    IN      CONST WCHAR *                       szElementName,
    OUT     BOOL *                              pFound
    )
{
    HRESULT hr;
    CComPtr<IAppHostElement> pElement;
    ENUM_INDEX index;
    BOOL matched;

    *pFound = FALSE;

    for (hr = FindFirstChildElement(pCollection, &index, &pElement) ;
         SUCCEEDED(hr) ;
         hr = FindNextChildElement(pCollection, &index, &pElement))
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        hr = CompareElementName(pElement, szElementName, &matched);

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if (matched)
        {
            hr = pElement->Clear();

            if (FAILED(hr))
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            *pFound = TRUE;
        }

        pElement.Release();
    }

exit:

    return hr;
}


HRESULT
GetSitesCollection(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szConfigPath,
    OUT     IAppHostElementCollection **        pSitesCollection
    )
{
    HRESULT hr;
    CComPtr<IAppHostElement> pSitesElement;

    BSTR bstrConfigPath = SysAllocString(szConfigPath);
    BSTR bstrSitesSectionName = SysAllocString(L"system.applicationHost/sites");
    *pSitesCollection = NULL;

    if (bstrConfigPath == NULL || bstrSitesSectionName == NULL)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Chase down the sites collection.
    //

    hr = pAdminMgr->GetAdminSection( bstrSitesSectionName,
                                     bstrConfigPath,
                                     &pSitesElement );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pSitesElement->get_Collection(pSitesCollection);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString(bstrSitesSectionName);
    SysFreeString(bstrConfigPath);
    return hr;
}


HRESULT
GetLocationCollection(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szConfigPath,
    OUT     IAppHostConfigLocationCollection ** pLocationCollection
    )
{
    HRESULT hr;
    CComPtr<IAppHostConfigManager>      pConfigMgr;
    CComPtr<IAppHostConfigFile>         pConfigFile;

    BSTR bstrConfigPath = SysAllocString(szConfigPath);
    *pLocationCollection = NULL;

    if (bstrConfigPath == NULL)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->get_ConfigManager(&pConfigMgr);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pConfigMgr->GetConfigFile(bstrConfigPath, &pConfigFile);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pConfigFile->get_Locations(pLocationCollection);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString(bstrConfigPath);
    return hr;
}


HRESULT
FindFirstElement(
    IN      IAppHostElementCollection *         pCollection,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    )
{
    HRESULT hr = pCollection->get_Count(&pIndex->Count);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        return hr;
    }

    VariantInit(&pIndex->Index);
    pIndex->Index.vt = VT_UI4;
    pIndex->Index.ulVal = 0;

    return FindNextElement(pCollection, pIndex, pElement);
}

HRESULT
FindNextElement(
    IN      IAppHostElementCollection *         pCollection,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    )
{
    *pElement = NULL;

    if (pIndex->Index.ulVal >= pIndex->Count)
    {
        return S_FALSE;
    }

    HRESULT hr = pCollection->get_Item(pIndex->Index, pElement);

    if (SUCCEEDED(hr))
    {
        pIndex->Index.ulVal++;
    }

    return hr;
}

HRESULT
FindFirstChildElement(
    IN      IAppHostChildElementCollection *    pCollection,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    )
{
    HRESULT hr = pCollection->get_Count(&pIndex->Count);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        return hr;
    }

    VariantInit(&pIndex->Index);
    pIndex->Index.vt = VT_UI4;
    pIndex->Index.ulVal = 0;

    return FindNextChildElement(pCollection, pIndex, pElement);
}

HRESULT
FindNextChildElement(
    IN      IAppHostChildElementCollection *    pCollection,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    )
{
    *pElement = NULL;

    if (pIndex->Index.ulVal >= pIndex->Count)
    {
        return S_FALSE;
    }

    HRESULT hr = pCollection->get_Item(pIndex->Index, pElement);

    if (SUCCEEDED(hr))
    {
        pIndex->Index.ulVal++;
    }

    return hr;
}

HRESULT
FindFirstLocation(
    IN      IAppHostConfigLocationCollection *  pCollection,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostConfigLocation **           pLocation
    )
{
    HRESULT hr = pCollection->get_Count(&pIndex->Count);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        return hr;
    }

    VariantInit(&pIndex->Index);
    pIndex->Index.vt = VT_UI4;
    pIndex->Index.ulVal = 0;

    return FindNextLocation(pCollection, pIndex, pLocation);
}

HRESULT
FindNextLocation(
    IN      IAppHostConfigLocationCollection *  pCollection,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostConfigLocation **           pLocation
    )
{
    *pLocation = NULL;

    if (pIndex->Index.ulVal >= pIndex->Count)
    {
        return S_FALSE;
    }

    HRESULT hr = pCollection->get_Item(pIndex->Index, pLocation);

    if (SUCCEEDED(hr))
    {
        pIndex->Index.ulVal++;
    }

    return hr;
}

HRESULT
FindFirstLocationElement(
    IN      IAppHostConfigLocation *            pLocation,
    OUT     ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    )
{
    HRESULT hr = pLocation->get_Count(&pIndex->Count);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        return hr;
    }

    VariantInit(&pIndex->Index);
    pIndex->Index.vt = VT_UI4;
    pIndex->Index.ulVal = 0;

    return FindNextLocationElement(pLocation, pIndex, pElement);
}

HRESULT
FindNextLocationElement(
    IN      IAppHostConfigLocation *            pLocation,
    IN OUT  ENUM_INDEX *                        pIndex,
    OUT     IAppHostElement **                  pElement
    )
{
    *pElement = NULL;

    if (pIndex->Index.ulVal >= pIndex->Count)
    {
        return S_FALSE;
    }

    HRESULT hr = pLocation->get_Item(pIndex->Index, pElement);

    if (SUCCEEDED(hr))
    {
        pIndex->Index.ulVal++;
    }

    return hr;
}

HRESULT
GetSharedConfigEnabled(
    BOOL * pfIsSharedConfig
)
/*++

Routine Description:
   Search the configuration for the shared configuration property.

Arguments:

    pfIsSharedConfig - true if shared configuration is enabled

Return Value:
    HRESULT

--*/
{
    HRESULT                 hr = S_OK;
    IAppHostAdminManager    *pAdminManager = NULL;

    BSTR                    bstrConfigPath = NULL;

    IAppHostElement *       pConfigRedirSection = NULL;


    BSTR bstrSectionName = SysAllocString(L"configurationRedirection");

    if ( bstrSectionName == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    bstrConfigPath = SysAllocString( L"MACHINE/REDIRECTION" );
    if ( bstrConfigPath == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = CoCreateInstance( CLSID_AppHostAdminManager,
                           NULL,
                           CLSCTX_INPROC_SERVER,
                           IID_IAppHostAdminManager,
                           (VOID **)&pAdminManager );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminManager->GetAdminSection( bstrSectionName,
                                         bstrConfigPath,
                                         &pConfigRedirSection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetElementBoolProperty( pConfigRedirSection,
                                 L"enabled",
                                 pfIsSharedConfig );

    if ( FAILED( hr ) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    pConfigRedirSection->Release();
    pConfigRedirSection = NULL;


exit:

    //
    // dump config exception to setup log file (if available)
    //

    if ( pConfigRedirSection != NULL )
    {
        pConfigRedirSection->Release();
    }

    if ( pAdminManager != NULL )
    {
        pAdminManager->Release();
    }

    if ( bstrConfigPath != NULL )
    {
        SysFreeString( bstrConfigPath );
    }

    if ( bstrSectionName != NULL )
    {
        SysFreeString( bstrSectionName );
    }

    return hr;
}
