// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

HRESULT
GetChildSectionGroup(
    IN      IAppHostSectionGroup *      pParentSectionGroup,
    IN      CONST WCHAR *               szChildGroupName,
    OUT     IAppHostSectionGroup **     ppChildSectionGroup
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostSectionGroup>       pChildSectionGroup;

    VARIANT varChildGroupName;
    VariantInit( &varChildGroupName );

    *ppChildSectionGroup = NULL;

    hr = VariantAssign( &varChildGroupName,
                        szChildGroupName );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pParentSectionGroup->get_Item( varChildGroupName,
                                        &pChildSectionGroup );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    *ppChildSectionGroup = pChildSectionGroup.Detach();

exit:

    VariantClear( &varChildGroupName );

    return hr;
}

HRESULT
GetRootSectionGroup(
    IN      IAppHostAdminManager *      pAdminMgr,
    IN      CONST WCHAR *               szConfigPath,
    OUT     IAppHostSectionGroup **     ppRootSectionGroup
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostSectionGroup>           pRootSectionGroup;
    CComPtr<IAppHostConfigManager>          pConfigMgr;
    CComPtr<IAppHostConfigFile>             pConfigFile;

    BSTR bstrConfigPath = NULL;

    *ppRootSectionGroup = NULL;

    bstrConfigPath = SysAllocString( szConfigPath );

    if( !bstrConfigPath )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->get_ConfigManager( &pConfigMgr );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pConfigMgr->GetConfigFile( bstrConfigPath,
                                    &pConfigFile );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pConfigFile->get_RootSectionGroup( &pRootSectionGroup );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    *ppRootSectionGroup = pRootSectionGroup.Detach();

exit:

    SysFreeString( bstrConfigPath );

    return hr;
}

HRESULT 
InitializeAdminManager(
    IN      CONST BOOL                     isSectionInAdminSchema,    
    IN      IAppHostWritableAdminManager * pAdminMgr,
    OUT     CONST WCHAR **                 pszCommitPath
)
{
    HRESULT hr = NOERROR;
    CONST WCHAR * szAdminCommitPath = L"MACHINE/WEBROOT";
    CONST WCHAR * szAppHostCommitPath = L"MACHINE/WEBROOT/APPHOST";

    CONST WCHAR * szCommitPath = NULL;

    *pszCommitPath = NULL;

    if(isSectionInAdminSchema)
    {
        szCommitPath = szAdminCommitPath;
        hr = InitAdminMgrForAdminConfig( pAdminMgr,
                                        szCommitPath );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }
    else
    {
        szCommitPath = szAppHostCommitPath;
    }

    *pszCommitPath = szCommitPath;

exit:
    return hr;
}

HRESULT
RegisterSectionSchema(
    IN           CONST BOOL     isSectionInAdminSchema,
    IN           CONST WCHAR *  szSectionName,
    IN           CONST WCHAR *  szOverrideModeDefault,
    IN  OPTIONAL CONST WCHAR *  szAllowDefinition,
    IN  OPTIONAL CONST WCHAR *  szType
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IAppHostSectionGroup>           pRootSectionGroup;
    CComPtr<IAppHostSectionGroup>           pParentSectionGroup;
    CComPtr<IAppHostSectionGroup>           pChildSectionGroup;
    CComPtr<IAppHostSectionDefinitionCollection> pSections;
    CComPtr<IAppHostSectionDefinition>      pNewSection;


    BSTR bstrSectionGroupName = NULL;
    BSTR bstrSectionName = NULL;
    BSTR bstrOverrideModeDefault = NULL;
    BSTR bstrAllowDefinition = NULL;
    BSTR bstrType = NULL;

    CONST WCHAR * szCommitPath = NULL;

    WCHAR * szSectionNameCopy = _wcsdup( szSectionName );
    if( !szSectionNameCopy )
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }
    szSectionName = NULL;

    //
    // szSectionNameCopy is the full name of the section. The last
    // segment szShortName will be registered as the name of the
    // section and the other segments are section groups
    //
    // eg. "system.webServer/foo/bar/mysection"
    //

    WCHAR * szShortName = wcsrchr( szSectionNameCopy, L'/' );
    if( szShortName == NULL )
    {
        szShortName = szSectionNameCopy;
    }
    else
    {
        *szShortName++ = L'\0';
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
    
    hr = InitializeAdminManager( isSectionInAdminSchema,
                                 pAdminMgr,
                                 &szCommitPath);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetRootSectionGroup( pAdminMgr,
                              szCommitPath,
                              &pRootSectionGroup);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // For each section group referenced in szSectionNameCopy retrieve or
    // create it.
    //

    WCHAR * pszGroupName = szSectionNameCopy;
    WCHAR * pszNext = NULL;

    pParentSectionGroup = pRootSectionGroup;
    
    if (szShortName == szSectionNameCopy)
    {
        goto SkipAddingGroups;
    }

    while( pszGroupName )
    {
        pszNext = wcschr( pszGroupName, L'/' );
        if( pszNext )
        {
            *pszNext++ = 0;
        }

        hr = GetChildSectionGroup( pParentSectionGroup,
                                   pszGroupName,
                                   &pChildSectionGroup );

        if( hr == HRESULT_FROM_WIN32( ERROR_INVALID_INDEX ) )
        {
            //
            // Create the group if it does not exist
            //
            bstrSectionGroupName = SysAllocString( pszGroupName );
            if( !bstrSectionGroupName )
            {
                hr = E_OUTOFMEMORY;
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = pParentSectionGroup->AddSectionGroup( bstrSectionGroupName,
                                                       &pChildSectionGroup );
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
        }
        else if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        pParentSectionGroup = pChildSectionGroup;
        pChildSectionGroup.Release();

        SysFreeString( bstrSectionGroupName );
        bstrSectionGroupName = NULL;

        pszGroupName = pszNext;
    }

 SkipAddingGroups:
    //
    // Add the new section
    //

    hr = pParentSectionGroup->get_Sections( &pSections );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    bstrSectionName = SysAllocString( szShortName );
    if( !bstrSectionName )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pSections->AddSection( bstrSectionName,
                                &pNewSection );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    bstrOverrideModeDefault = SysAllocString( szOverrideModeDefault );
    if( !bstrOverrideModeDefault )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pNewSection->put_OverrideModeDefault( bstrOverrideModeDefault );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if( szAllowDefinition && *szAllowDefinition )
    {
        bstrAllowDefinition = SysAllocString( szAllowDefinition );
        if( !bstrAllowDefinition )
        {
            hr = E_OUTOFMEMORY;
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pNewSection->put_AllowDefinition( bstrAllowDefinition );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if( szType && *szType )
    {
        bstrType = SysAllocString( szType );
        if( !bstrType )
        {
            hr = E_OUTOFMEMORY;
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pNewSection->put_Type( bstrType );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    //
    // Persist changes
    //

    hr = pAdminMgr->CommitChanges();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString( bstrSectionGroupName );
    SysFreeString( bstrSectionName );
    SysFreeString( bstrOverrideModeDefault );
    SysFreeString( bstrType );
    SysFreeString( bstrAllowDefinition );
    
    free( szSectionNameCopy );

    return hr;
}

HRESULT
RemoveSectionDefinition(
    IN      IAppHostSectionGroup *              pParentSection,
    __in_z  WCHAR *                             szSectionPath,
    __in_z  WCHAR *                             szSectionName
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostSectionGroup>                   pChildSectionGroup;
    CComPtr<IAppHostSectionDefinitionCollection>    pSections;

    WCHAR * pszNextPath = NULL;

    VARIANT varIndex;
    VariantInit( &varIndex );

    //
    // If there are no more path segments, remove the section
    //

    if( !szSectionPath )
    {
        hr = pParentSection->get_Sections( &pSections );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = VariantAssign( &varIndex, szSectionName );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pSections->DeleteSection( varIndex );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        // End recursion
        goto exit;
    }

    //
    // We have more path segments, so move to the next segment
    // and call RemoveSectionDefinition recursively
    //

    pszNextPath = wcschr( szSectionPath, L'/' );
    if( pszNextPath )
    {
        *pszNextPath++ = 0;
    }

    hr = GetChildSectionGroup( pParentSection,
                               szSectionPath,
                               &pChildSectionGroup );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = RemoveSectionDefinition( pChildSectionGroup,
                                  pszNextPath,
                                  szSectionName );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // The section has been removed, check to see if the
    // child section group is empty and clean up if it is.
    //

    ULONG   childCount = 0;
    hr = pChildSectionGroup->get_Count( &childCount );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if( childCount == 0 )
    {
        hr = pChildSectionGroup->get_Sections( &pSections );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pSections->get_Count( &childCount );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        if( childCount == 0 )
        {
            hr = VariantAssign( &varIndex, szSectionPath );
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = pParentSection->DeleteSectionGroup( varIndex );
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

        }
    }

exit:

    VariantClear( &varIndex );

    return hr;
}

HRESULT
RemoveSectionData(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szSectionName,
    IN      CONST WCHAR *                       szConfigPath
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement>                    pSectionElement;
    CComPtr<IAppHostConfigManager>              pConfigMgr;
    CComPtr<IAppHostConfigFile>                 pConfigFile;
    CComPtr<IAppHostConfigLocationCollection>   pLocations;
    CComPtr<IAppHostConfigLocation>             pLocation;

    BSTR bstrSectionName = SysAllocString( szSectionName );
    BSTR bstrPath = SysAllocString( szConfigPath );

    VARIANT varSectionName;
    VariantInit( &varSectionName );

    if( !bstrSectionName ||
        !bstrPath )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = VariantAssign( &varSectionName, szSectionName );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->GetAdminSection( bstrSectionName,
                                     bstrPath,
                                     &pSectionElement );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pSectionElement->Clear();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Go through the location tags and delete the section
    //

    hr = pAdminMgr->get_ConfigManager( &pConfigMgr );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pConfigMgr->GetConfigFile( bstrPath,
                                    &pConfigFile );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pConfigFile->get_Locations( &pLocations );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    DWORD       count;
    VARIANT     varIndex;
    VariantInit( &varIndex );

    hr = pLocations->get_Count( &count );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for( DWORD i = 0; i < count; i++ )
    {
        varIndex.vt = VT_UI4;
        varIndex.ulVal = i;

        hr = pLocations->get_Item( varIndex, &pLocation );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pLocation->DeleteConfigSection( varSectionName );
        if( HRESULT_FROM_WIN32( ERROR_FILE_NOT_FOUND ) == hr )
        {
            hr = S_OK;
        }
        else if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        pLocation.Release();
    }

exit:

    SysFreeString( bstrSectionName );
    SysFreeString( bstrPath );

    return hr;
}

HRESULT
UnRegisterSectionSchema(
    IN  CONST BOOL      isSectionInAdminSchema,
    IN  CONST WCHAR *   szSectionName
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>       pAdminMgr;
    CComPtr<IAppHostSectionGroup>               pRootSectionGroup;

    CONST WCHAR * szCommitPath = NULL;

    WCHAR * szSectionNameCopy = _wcsdup( szSectionName );
    if( !szSectionNameCopy )
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }

    //
    // szSectionNameCopy is the full name of the section. The last
    // segment szShortName will be registered as the name of the
    // section and the other segments are section groups
    //
    // eg. "system.webServer/foo/bar/mysection"
    //

    WCHAR * szShortName = wcsrchr( szSectionNameCopy, L'/' );
    if( szShortName == NULL )
    {
        szShortName = szSectionNameCopy;
    }
    else
    {
        *szShortName++ = 0;
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

    hr = InitializeAdminManager( isSectionInAdminSchema,
                                 pAdminMgr,
                                 &szCommitPath);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = RemoveSectionData( pAdminMgr,
                            szSectionName,
                            szCommitPath);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetRootSectionGroup( pAdminMgr,
                              szCommitPath,
                              &pRootSectionGroup );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = RemoveSectionDefinition( pRootSectionGroup,
                                  (szShortName == szSectionNameCopy) ? NULL : szSectionNameCopy,
                                  szShortName );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pAdminMgr->CommitChanges();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    free( szSectionNameCopy );

    return hr;
}

