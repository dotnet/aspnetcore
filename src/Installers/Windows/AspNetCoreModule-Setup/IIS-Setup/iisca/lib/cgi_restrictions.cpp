// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

//
// Public functions
//

HRESULT
RegisterCgiRestriction(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szPath,
    IN                BOOL              fAllowed,
    IN OPTIONAL CONST WCHAR *           szGroupId,
    IN OPTIONAL CONST WCHAR *           szDescription
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement>                pCgiRestrictionSection;
    CComPtr<IAppHostElementCollection>      pCgiRestrictionCollection;
    CComPtr<IAppHostElement>                pNewCgiRestrictionElement;

    BSTR bstrAppHostConfigPath = SysAllocString(szConfigPath);
    BSTR bstrCgiRestriction = SysAllocString( L"system.webServer/security/isapiCgiRestriction" );
    BSTR bstrAdd = SysAllocString( L"add" );

    if( !bstrAppHostConfigPath ||
        !bstrCgiRestriction ||
        !bstrAdd )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    //
    // Remove any existing web service first,
    // ignore any resulting errors.
    //

    hr = UnRegisterCgiRestriction(
             pAdminMgr,
             szConfigPath,
             szPath,
             FALSE              // do NOT expand environment strings
             );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        // press on regardless
    }

    hr = UnRegisterCgiRestriction(
             pAdminMgr,
             szConfigPath,
             szPath,
             TRUE               // DO expand environment strings
             );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        // press on regardless
    }

    //
    // Get the ISAPI CGI Restriction collection
    //

    hr = pAdminMgr->GetAdminSection( bstrCgiRestriction,
                                     bstrAppHostConfigPath,
                                     &pCgiRestrictionSection );


    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = pCgiRestrictionSection->get_Collection( &pCgiRestrictionCollection );

    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    //
    // Create a new element
    //

    hr = pCgiRestrictionCollection->CreateNewElement( bstrAdd,
                                                      &pNewCgiRestrictionElement );

    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = SetElementStringProperty(
            pNewCgiRestrictionElement,
            L"path",
            szPath
            );

    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    hr = SetElementStringProperty(
            pNewCgiRestrictionElement,
            L"allowed",
            fAllowed ? L"true" : L"false"
            );

    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

    if( szGroupId && *szGroupId )
    {
        hr = SetElementStringProperty(
                pNewCgiRestrictionElement,
                L"groupId",
                szGroupId
                );

        if( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }
    }

    if( szDescription && *szDescription )
    {
        hr = SetElementStringProperty(
                pNewCgiRestrictionElement,
                L"description",
                szDescription
                );

        if( FAILED(hr) )
        {
            DBGERROR_HR( hr );
            goto exit;
        }
    }

    //
    // Add the new element
    //

    hr = pCgiRestrictionCollection->AddElement( pNewCgiRestrictionElement );

    if( FAILED(hr) )
    {
        DBGERROR_HR( hr );
        goto exit;
    }

exit:

    SysFreeString( bstrAppHostConfigPath );
    SysFreeString( bstrCgiRestriction );
    SysFreeString( bstrAdd );

    return hr;
}

HRESULT
UnRegisterCgiRestriction(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szPath,
    IN          BOOL                    fExpandPath
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement>                pCgiRestrictionSection;
    CComPtr<IAppHostElementCollection>      pCgiRestrictionCollection;

    BSTR bstrAppHostConfigPath = SysAllocString(szConfigPath);
    BSTR bstrCgiRestriction = SysAllocString( L"system.webServer/security/isapiCgiRestriction" );

    UINT numDeleted;

    STRU expandedPath;

    if( !bstrAppHostConfigPath ||
        !bstrCgiRestriction )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto exit;
    }

    if (fExpandPath)
    {
        hr = expandedPath.CopyAndExpandEnvironmentStrings(szPath);

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        szPath = expandedPath.QueryStr();
    }

    //
    // Remove from the ISAPI CGI Restriction collection
    //

    hr = pAdminMgr->GetAdminSection( bstrCgiRestriction,
                                     bstrAppHostConfigPath,
                                     &pCgiRestrictionSection );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pCgiRestrictionSection->get_Collection( &pCgiRestrictionCollection );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = DeleteAllElementsFromCollection( pCgiRestrictionCollection,
                                          L"path",
                                          szPath,
                                          FIND_ELEMENT_CASE_INSENSITIVE,
                                          &numDeleted
                                          );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if( numDeleted == 0 )
    {
        DBGWARN(( DBG_CONTEXT,
                  "Expected to find %S in ISAPI CGI Restriction collection\n",
                  szPath ));
    }

exit:

    SysFreeString( bstrAppHostConfigPath );
    SysFreeString( bstrCgiRestriction );

    return hr;
}

