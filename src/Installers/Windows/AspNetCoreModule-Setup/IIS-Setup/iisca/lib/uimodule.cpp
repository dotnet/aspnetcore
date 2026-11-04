// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

class ADMINISTRATION_CONFIG_PATH_MAPPER : public IAppHostPathMapper
{

public:

    ADMINISTRATION_CONFIG_PATH_MAPPER(
        VOID
    ) : _cRefs( 1 )
    {
    }

    ~ADMINISTRATION_CONFIG_PATH_MAPPER(
        VOID
    )
    {
    }

    HRESULT
    Initialize()
    {
        // TODO this could be more reliable.
        HRESULT hr = NOERROR;
        DWORD cch;

        cch = ExpandEnvironmentStringsW(
                L"%windir%\\system32\\inetsrv\\config\\administration.config",
                _strMappedPath.QueryStr(),
                _strMappedPath.QuerySizeCCH()
                );
        if( !cch )
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto exit;
        }

        if( cch > _strMappedPath.QuerySizeCCH() )
        {
            hr = _strMappedPath.Resize( cch );
            if( FAILED(hr) )
            {
                goto exit;
            }

            cch = ExpandEnvironmentStringsW(
                    L"%windir%\\system32\\inetsrv\\config\\administration.config",
                    _strMappedPath.QueryStr(),
                    _strMappedPath.QuerySizeCCH()
                    );
            if( !cch ||
                cch > _strMappedPath.QuerySizeCCH() )
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto exit;
            }
        }

        _strMappedPath.SyncWithBuffer();

    exit:

       return hr;

    }

    ULONG
    STDMETHODCALLTYPE
    AddRef(
        VOID
    )
    {
        return InterlockedIncrement( &_cRefs );
    }

    ULONG
    STDMETHODCALLTYPE
    Release(
        VOID
    )
    {
        ULONG                   ulRet = 0;

        ulRet = InterlockedDecrement( &_cRefs );

        if ( ulRet == 0 )
        {
            delete this;
        }
        return ulRet;
    }

    HRESULT
    STDMETHODCALLTYPE
    QueryInterface(
        REFIID                  riid,
        void **                 ppObject
    )
    {
        if ( riid == __uuidof(IUnknown) ||
             riid == __uuidof(IAppHostPathMapper) )
        {
            AddRef();
            *ppObject = (IAppHostPathMapper*) this;
            return S_OK;
        }
        else
        {
            *ppObject = NULL;
            return E_NOINTERFACE;
        }
    }

    HRESULT
    STDMETHODCALLTYPE
    MapPath(
        BSTR                    bstrConfigPath,
        BSTR                    bstrMappedPhysicalPath,
        BSTR *                  pbstrNewPhysicalPath
        )
    {
        BSTR bstrNewPhysicalPath = NULL;

        if ( wcscmp( bstrConfigPath, L"MACHINE/WEBROOT" ) == 0 )
        {
            bstrNewPhysicalPath = SysAllocString( _strMappedPath.QueryStr() );
        }
        else
        {
            bstrNewPhysicalPath = SysAllocString( bstrMappedPhysicalPath );
        }

        if ( bstrNewPhysicalPath == NULL )
        {
            return HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
        }

        *pbstrNewPhysicalPath = bstrNewPhysicalPath;
        return S_OK;
    }

private:

    LONG                        _cRefs;
    STRU                        _strMappedPath;
};

HRESULT
InitAdminMgrForAdminConfig(
    IN IAppHostWritableAdminManager *        pAdminMgr,
    IN CONST WCHAR *                         szCommitPath
    )
{
    HRESULT hr = NOERROR;

    VARIANT varPathMapper;
    VariantInit( &varPathMapper );

    BSTR bstrPathMapperName = SysAllocString( L"pathMapper" );
    BSTR bstrCommitPath = SysAllocString( szCommitPath );

    ADMINISTRATION_CONFIG_PATH_MAPPER * pPathMapper = NULL;

    if( !bstrPathMapperName || !bstrCommitPath)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->put_CommitPath( bstrCommitPath );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    pPathMapper = new ADMINISTRATION_CONFIG_PATH_MAPPER();
    if( !pPathMapper )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pPathMapper->Initialize();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    varPathMapper.vt = VT_UNKNOWN;
    varPathMapper.punkVal = pPathMapper;
    pPathMapper = NULL;

    hr = pAdminMgr->SetMetadata( bstrPathMapperName, varPathMapper );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString( bstrPathMapperName );
    SysFreeString( bstrCommitPath );

    VariantClear( &varPathMapper );

    if( pPathMapper )
    {
        pPathMapper->Release();
        pPathMapper = NULL;
    }

    return hr;
}


HRESULT
RegisterUIModule(
    IN          CONST WCHAR *   szModuleName,
    IN          CONST WCHAR *   szModuleTypeInfo,
    IN OPTIONAL CONST WCHAR *   szRegisterInModulesSection,
    IN OPTIONAL CONST WCHAR *   szPrependToList
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IAppHostElement>                pProvidersSection;
    CComPtr<IAppHostElementCollection>      pProvidersCollection;
    CComPtr<IAppHostElement>                pNewModuleElement;
    CComPtr<IAppHostElement>                pModulesSection;
    CComPtr<IAppHostElementCollection>      pModulesCollection;
    CComPtr<IAppHostElement>                pModulesCollectionElement;

    VARIANT varValue;
    VariantInit( &varValue );
    DWORD dwIndex;
    BOOL fAddElement = FALSE;
    INT cIndex;

    BSTR bstrCommitPath = SysAllocString( L"MACHINE/WEBROOT" );
    BSTR bstrModuleProvidersName = SysAllocString( L"moduleProviders" );
    BSTR bstrAdd = SysAllocString( L"add" );
    BSTR bstrModulesSectionName = SysAllocString( L"modules" );

    if( !bstrCommitPath ||
        !bstrModuleProvidersName ||
        !bstrAdd ||
        !bstrModulesSectionName )
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

    hr = InitAdminMgrForAdminConfig( pAdminMgr,
                                    bstrCommitPath );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->GetAdminSection( bstrModuleProvidersName,
                                     bstrCommitPath,
                                     &pProvidersSection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pProvidersSection->get_Collection( &pProvidersCollection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = FindElementInCollection(
            pProvidersCollection,
            L"name",
            szModuleName,
            FIND_ELEMENT_CASE_SENSITIVE,
            &dwIndex);
    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if (hr == S_OK)
    {
        VARIANT vtIndex;
        vtIndex.vt = VT_UI4;
        vtIndex.ulVal = dwIndex;

        hr = pProvidersCollection->get_Item(
                vtIndex,
                &pNewModuleElement);
        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }
    else
    {
        hr = pProvidersCollection->CreateNewElement( bstrAdd,
                                                     &pNewModuleElement );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        fAddElement = TRUE;
    }

    hr = VariantAssign( &varValue, szModuleName );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = SetElementProperty( pNewModuleElement,
                             L"name",
                             &varValue );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = VariantAssign( &varValue, szModuleTypeInfo );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = SetElementProperty( pNewModuleElement,
                             L"type",
                             &varValue );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if (fAddElement)
    {
        cIndex = -1;
        if (szPrependToList && *szPrependToList)
        {
            cIndex = 0;
        }

        hr = pProvidersCollection->AddElement( pNewModuleElement,
                                               cIndex );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if( szRegisterInModulesSection && *szRegisterInModulesSection )
    {
        // register global <modules> section
        hr = pAdminMgr->GetAdminSection( bstrModulesSectionName,
                                         bstrCommitPath,
                                         &pModulesSection );

        if( FAILED(hr) )
        {
             DBGERROR_HR(hr);
             goto exit;
        }

        hr = pModulesSection->get_Collection( &pModulesCollection );

        if( FAILED(hr) )
        {
             DBGERROR_HR(hr);
             goto exit;
        }

        hr = pModulesCollection->CreateNewElement( bstrAdd,
                                                   &pModulesCollectionElement );
        if( FAILED(hr) )
        {
             DBGERROR_HR(hr);
             goto exit;
        }

        hr = VariantAssign( &varValue, szModuleName );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = SetElementProperty( pModulesCollectionElement,
                                 L"name",
                                 &varValue );
        if( FAILED(hr) )
        {
             DBGERROR_HR(hr);
             goto exit;
        }

        cIndex = -1;
        if (szPrependToList && *szPrependToList)
        {
            cIndex = 0;
        }

        hr = pModulesCollection->AddElement( pModulesCollectionElement,
                                             cIndex );
        if (hr == HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
        {
            hr = S_OK;
        }
        if( FAILED(hr) )
        {
             DBGERROR_HR(hr);
             goto exit;
        }
    }

    hr = pAdminMgr->CommitChanges();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString( bstrCommitPath );
    SysFreeString( bstrModuleProvidersName );
    SysFreeString( bstrAdd );
    SysFreeString( bstrModulesSectionName );

    VariantClear( &varValue );

    return hr;
}

HRESULT
UnRegisterUIModule(
    IN          CONST WCHAR *   szModuleName,
    IN          CONST WCHAR *   szModuleTypeInfo
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IAppHostElement>                pProvidersSection;
    CComPtr<IAppHostElementCollection>      pProvidersCollection;
    CComPtr<IAppHostElement>                pProviderElement;
    CComPtr<IAppHostElement>                pModulesSection;
    CComPtr<IAppHostElementCollection>      pModulesCollection;

    BSTR bstrCommitPath = SysAllocString( L"MACHINE/WEBROOT" );
    BSTR bstrModuleProvidersName = SysAllocString( L"moduleProviders" );
    BSTR bstrModulesSectionName = SysAllocString( L"modules" );
    BSTR bstrType = NULL;
    DWORD dwIndex;

    if( !bstrCommitPath ||
        !bstrModuleProvidersName ||
        !bstrModulesSectionName )
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

    hr = InitAdminMgrForAdminConfig( pAdminMgr, bstrCommitPath );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->GetAdminSection( bstrModuleProvidersName,
                                     bstrCommitPath,
                                     &pProvidersSection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pProvidersSection->get_Collection( &pProvidersCollection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    BOOL fProvidersDeleted = FALSE;

    hr = FindElementInCollection(
            pProvidersCollection,
            L"name",
            szModuleName,
            FIND_ELEMENT_CASE_SENSITIVE,
            &dwIndex);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    if (hr == S_OK)
    {
        VARIANT vtIndex;
        vtIndex.vt = VT_UI4;
        vtIndex.ulVal = dwIndex;

        hr = pProvidersCollection->get_Item(
                vtIndex,
                &pProviderElement);
        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = GetElementStringProperty(
                pProviderElement,
                L"type",
                &bstrType);
        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if (wcscmp(bstrType, szModuleTypeInfo) == 0)
        {
            hr = pProvidersCollection->DeleteElement(vtIndex);
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            fProvidersDeleted = TRUE;
        }
        else
        {
            goto exit;
        }
    }

    // now remove from global <modules> section if present
    hr = pAdminMgr->GetAdminSection( bstrModulesSectionName,
                                     bstrCommitPath,
                                     &pModulesSection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pModulesSection->get_Collection( &pModulesCollection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    BOOL fModulesDeleted = FALSE;
    hr = DeleteElementFromCollection( pModulesCollection,
                                      L"name",
                                      szModuleName,
                                      FIND_ELEMENT_CASE_SENSITIVE,
                                      &fModulesDeleted );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if( fProvidersDeleted || fModulesDeleted )
    {
        hr = pAdminMgr->CommitChanges();
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

exit:

    SysFreeString( bstrCommitPath );
    SysFreeString( bstrModuleProvidersName );
    SysFreeString( bstrModulesSectionName );
    SysFreeString( bstrType );

    return hr;
}

