// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

//
// Local function declarations
//

HRESULT
AddModuleToGlobalModules(
    IN IAppHostWritableAdminManager *   pAdminMgr,
    IN          CONST WCHAR *           szName,
    IN          CONST WCHAR *           szImage,
    IN OPTIONAL CONST WCHAR *           szPreCondition
    );

HRESULT
AddModuleToRootModules(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szName,
    IN          CONST WCHAR *           szPreCondition,
    IN OPTIONAL CONST WCHAR *           szType
    );

HRESULT
DeleteModuleFromRootModules(
    IAppHostAdminManager *      pAdminMgr,
    CONST WCHAR *               szName,
    BOOL *                      pfDeleted
    );

//
// Public functions
//

HRESULT
InstallModule(
    IN          CONST WCHAR *   szName,
    IN          CONST WCHAR *   szImage,
    IN OPTIONAL CONST WCHAR *   szPreCondition,
    IN OPTIONAL CONST WCHAR *   szType
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;

    hr = CoCreateInstance( __uuidof( AppHostWritableAdminManager ),
                           NULL,
                           CLSCTX_INPROC_SERVER,
                           __uuidof( IAppHostWritableAdminManager ),
                           (VOID **)&pAdminMgr );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    // If the type is present, this is a .Net module and
    // should not be added to the globalModules
    if ( ( szType == NULL ) || ( *szType == L'\0' ) )
    {
        STRU strFinalImage;
        WCHAR ProgFiles[64];
        WCHAR SysRoot[64];
        WCHAR SysDrive[16];
        PCWSTR szSubstFrom = NULL;
        PCWSTR szSubstTo = NULL;

        if (GetEnvironmentVariable(L"ProgramFiles",
                                   ProgFiles,
                                   _countof(ProgFiles)) > _countof(ProgFiles) ||
            GetEnvironmentVariable(L"SystemRoot",
                                   SysRoot,
                                   _countof(SysRoot)) > _countof(SysRoot) ||
            GetEnvironmentVariable(L"SystemDrive",
                                   SysDrive,
                                   _countof(SysDrive)) > _countof(SysDrive))
        {
            hr = E_UNEXPECTED;
            DBGERROR_HR(hr);
            goto exit;
        }

        if (_wcsnicmp(szImage, ProgFiles, wcslen(ProgFiles)) == 0)
        {
            szSubstFrom = ProgFiles;
            szSubstTo = L"%ProgramFiles%";
        }
        else if (_wcsnicmp(szImage, SysRoot, wcslen(SysRoot)) == 0)
        {
            szSubstFrom = SysRoot;
            szSubstTo = L"%SystemRoot%";
        }
        else if (_wcsnicmp(szImage, SysDrive, wcslen(SysDrive)) == 0)
        {
            szSubstFrom = SysDrive;
            szSubstTo = L"%SystemDrive%";
        }

        if (szSubstFrom != NULL)
        {
            if (FAILED(hr = strFinalImage.Copy(szSubstTo)) ||
                FAILED(hr = strFinalImage.Append(szImage + wcslen(szSubstFrom))))
            {
                goto exit;
            }
            szImage = strFinalImage.QueryStr();
        }

        hr = AddModuleToGlobalModules( pAdminMgr,
                                       szName,
                                       szImage,
                                       szPreCondition );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    hr = AddModuleToRootModules( pAdminMgr,
                                 szName,
                                 szPreCondition,
                                 szType );
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

    return hr;
}

HRESULT
UnInstallModule(
    IN          CONST WCHAR *   szName,
    IN OPTIONAL CONST WCHAR *   szType
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IAppHostElement>                pGlobalModulesSection;
    CComPtr<IAppHostElementCollection>      pGlobalModulesCollection;

    BSTR bstrAppHostConfigPath = SysAllocString( L"MACHINE/WEBROOT/APPHOST" );
    BSTR bstrGlobalModules = SysAllocString( L"system.webServer/globalModules" );

    BOOL fChanged = FALSE;
    BOOL fDeleted = FALSE;
    UINT numDeleted;

    if ( !bstrAppHostConfigPath ||
         !bstrGlobalModules )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = CoCreateInstance( __uuidof( AppHostWritableAdminManager ),
                           NULL,
                           CLSCTX_INPROC_SERVER,
                           __uuidof( IAppHostWritableAdminManager ),
                           (VOID **)&pAdminMgr );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Remove from root modules
    //

    hr = DeleteModuleFromRootModules( pAdminMgr,
                                      szName,
                                      &fDeleted );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( !fDeleted )
    {
        DBGWARN(( DBG_CONTEXT,
                  "Expected to find %S in root modules collection\n" ,
                  szName ));
    }

    fChanged = fDeleted;

    if ( ( szType == NULL ) || ( *szType == L'\0' ) )
    {
        //
        // Remove from globalModules
        //

        hr = pAdminMgr->GetAdminSection( bstrGlobalModules,
                                         bstrAppHostConfigPath,
                                         &pGlobalModulesSection );

        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pGlobalModulesSection->get_Collection( &pGlobalModulesCollection );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }


        hr = DeleteAllElementsFromCollection( pGlobalModulesCollection,
                                              L"name",
                                              szName,
                                              FIND_ELEMENT_CASE_SENSITIVE,
                                              &numDeleted );

        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( numDeleted == 0 )
        {
            DBGWARN(( DBG_CONTEXT,
                      "Expected to find %S in globalModules list.\n",
                      szName ));
        }
        else
        {
            fChanged = TRUE;
        }
    }

    if ( fChanged )
    {
        hr = pAdminMgr->CommitChanges();
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

exit:

    SysFreeString( bstrAppHostConfigPath );
    SysFreeString( bstrGlobalModules );

    return hr;
}

//
// Local functions
//

HRESULT
AddModuleToGlobalModules(
    IN IAppHostWritableAdminManager *   pAdminMgr,
    IN          CONST WCHAR *           szName,
    IN          CONST WCHAR *           szImage,
    IN OPTIONAL CONST WCHAR *           szPreCondition
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement>                pGlobalModulesSection;
    CComPtr<IAppHostElementCollection>      pGlobalModulesCollection;
    CComPtr<IAppHostElement>                pNewGlobalModuleElement;

    VARIANT varPropValue;
    VariantInit( &varPropValue );

    BSTR bstrAppHostConfigPath = SysAllocString( L"MACHINE/WEBROOT/APPHOST" );
    BSTR bstrGlobalModules = SysAllocString( L"system.webServer/globalModules" );
    BSTR bstrAdd = SysAllocString( L"add" );

    if ( !bstrAppHostConfigPath ||
        !bstrGlobalModules ||
        !bstrAdd )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    //
    // Get the global modules collection
    //

    hr = pAdminMgr->GetAdminSection( bstrGlobalModules,
                                     bstrAppHostConfigPath,
                                     &pGlobalModulesSection );

    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pGlobalModulesSection->get_Collection( &pGlobalModulesCollection );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    //
    // Create a new module element
    //

    hr = pGlobalModulesCollection->CreateNewElement( bstrAdd,
                                                     &pNewGlobalModuleElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = VariantAssign( &varPropValue, szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = SetElementProperty( pNewGlobalModuleElement,
                             L"name",
                             &varPropValue );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = VariantAssign( &varPropValue, szImage );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = SetElementProperty( pNewGlobalModuleElement,
                             L"image",
                             &varPropValue );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    if ( szPreCondition && *szPreCondition )
    {
        hr = VariantAssign( &varPropValue, szPreCondition );
        if ( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }

        hr = SetElementProperty( pNewGlobalModuleElement,
                                 L"preCondition",
                                 &varPropValue );
        if ( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }
    }

    //
    // Add the new element
    //

    hr = pGlobalModulesCollection->AddElement( pNewGlobalModuleElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

exit:

    VariantClear( &varPropValue );

    SysFreeString( bstrAppHostConfigPath );
    SysFreeString( bstrGlobalModules );
    SysFreeString( bstrAdd );

    return hr;
}

HRESULT
AddModuleToRootModules(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szName,
    IN          CONST WCHAR *           szPreCondition,
    IN OPTIONAL CONST WCHAR *           szType
    )
{
    HRESULT hr = NOERROR;

    BOOL found = FALSE;

    CComPtr<IAppHostConfigLocation>     pLocation;
    CComPtr<IAppHostElement>            pModulesSection;
    CComPtr<IAppHostElementCollection>  pModuleCollection;
    CComPtr<IAppHostElement>            pNewModuleElement;

    VARIANT varPropValue;
    VariantInit( &varPropValue );

    BSTR bstrAdd = SysAllocString( L"add" );

    if ( !bstrAdd )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetLocationFromFile( pAdminMgr,
                              L"MACHINE/WEBROOT/APPHOST",
                              L"",
                              &pLocation,
                              &found );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( !found )
    {
        hr = HRESULT_FROM_WIN32( ERROR_PATH_NOT_FOUND );
        DBGERROR(( DBG_CONTEXT,
                   "Failed to find root location path\n" ));
        goto exit;
    }

    hr = GetSectionFromLocation( pLocation,
                                 L"system.webServer/modules",
                                 &pModulesSection,
                                 &found );

    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( !found )
    {
        hr = HRESULT_FROM_WIN32( ERROR_PATH_NOT_FOUND );
        DBGERROR(( DBG_CONTEXT,
                   "Failed to find modules section\n" ));
        goto exit;
    }

    //
    // Create a new module element
    //

    hr = pModulesSection->get_Collection( &pModuleCollection );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pModuleCollection->CreateNewElement( bstrAdd,
                                              &pNewModuleElement );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = VariantAssign( &varPropValue, szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = SetElementProperty( pNewModuleElement,
                             L"name",
                             &varPropValue );
    if ( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    if ( szPreCondition && *szPreCondition )
    {
        hr = VariantAssign( &varPropValue, szPreCondition );
        if ( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }

        hr = SetElementProperty( pNewModuleElement,
                                 L"preCondition",
                                 &varPropValue );
        if ( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }
    }

    if ( szType && *szType )
    {
        hr = VariantAssign( &varPropValue, szType );
        if ( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }

        hr = SetElementProperty( pNewModuleElement,
                                 L"type",
                                 &varPropValue );
        if ( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }
    }

    hr = pModuleCollection->AddElement( pNewModuleElement,
                                        -1 );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString( bstrAdd );
    VariantClear( &varPropValue );

    return hr;
}

HRESULT
DeleteModuleFromRootModules(
    IAppHostAdminManager *      pAdminMgr,
    CONST WCHAR *               szName,
    BOOL *                      pfDeleted
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostConfigLocation>     pLocation;
    CComPtr<IAppHostElement>            pModulesSection;
    CComPtr<IAppHostElementCollection>  pModulesCollection;

    UINT numDeleted;
    BOOL found = FALSE;

    *pfDeleted = FALSE;

    hr = GetLocationFromFile( pAdminMgr,
                              L"MACHINE/WEBROOT/APPHOST",
                              L"",
                              &pLocation,
                              &found );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( !found )
    {
        DBGWARN(( DBG_CONTEXT,
                  "Failed to find root location path\n" ));
        goto exit;
    }

    hr = GetSectionFromLocation( pLocation,
                                 L"system.webServer/modules",
                                 &pModulesSection,
                                 &found );

    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( !found )
    {
        DBGWARN(( DBG_CONTEXT,
                  "Failed to find modules section in root\n" ));
        goto exit;
    }

    hr = pModulesSection->get_Collection( &pModulesCollection );

    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = DeleteAllElementsFromCollection( pModulesCollection,
                                          L"name",
                                          szName,
                                          FIND_ELEMENT_CASE_SENSITIVE,
                                          &numDeleted );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( numDeleted == 0 )
    {
        DBGWARN(( DBG_CONTEXT,
                  "Failed to find %S in root modules\n",
                  szName ));
    }
    else
    {
        *pfDeleted = TRUE;
    }

exit:

    return hr;
}

