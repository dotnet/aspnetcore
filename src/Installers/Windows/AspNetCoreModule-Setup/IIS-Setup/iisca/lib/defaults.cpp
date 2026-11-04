// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"


HRESULT
SetElementMetadata(
    IAppHostElement *   pElement,
    CONST WCHAR *       szMetaType,
    CONST VARIANT *     pVarValue
    )
{
    HRESULT hr = NOERROR;

    BSTR bstrMetaType = SysAllocString( szMetaType );
    if( !bstrMetaType )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pElement->SetMetadata( bstrMetaType,
                                *pVarValue );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString( bstrMetaType );

    return hr;
}

HRESULT
ProcessAttributes(
    IAppHostElement *   pElement,
    IXmlReader *        pReader
    )
{
    HRESULT hr = NOERROR;

    VARIANT varPropValue;
    VariantInit( &varPropValue );

    CONST WCHAR * pszName;
    CONST WCHAR * pszValue;

    for( hr = pReader->MoveToFirstAttribute();
         ;
         hr = pReader->MoveToNextAttribute() )
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        if (FAILED(hr))
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pReader->GetLocalName( &pszName, NULL );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = pReader->GetValue( &pszValue, NULL );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = VariantAssign( &varPropValue, pszValue );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        hr = SetElementProperty( pElement, pszName, &varPropValue );

        if( HRESULT_FROM_WIN32( ERROR_INVALID_INDEX ) == hr )
        {
            //
            // Possibly this is a metadata attribute not a property
            //

            hr = SetElementMetadata( pElement, pszName, &varPropValue );
        }

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        VariantClear( &varPropValue );
    }

exit:

    VariantClear( &varPropValue );

    return hr;
}

HRESULT
ProcessSection(
    IAppHostWritableAdminManager *  pAdminMgr,
    CONST WCHAR *           szSectionName,
    IXmlReader *            pReader,
    IAppHostElement *       pParent,
    BOOL                    fClearSection
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostElement> pChildElement;
    CComPtr<IAppHostElement> pParentElement = pParent;

    CONST WCHAR * pwszLocalName;

    BSTR bstrName = NULL;
    BSTR bstrSectionName = NULL;
    BSTR bstrRoot = NULL;
    BSTR bstrAddElementName = NULL;

    BOOL isEmpty = FALSE;

    bstrSectionName = SysAllocString( szSectionName );
    bstrRoot = SysAllocString( L"MACHINE/WEBROOT/APPHOST" );

    if( !bstrSectionName || !bstrRoot )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    XmlNodeType nodeType;

    for (;;)
    {
        hr = pReader->Read(&nodeType);

        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        if (FAILED(hr))
        {
            break;
        }

        if( nodeType == XmlNodeType_Element )
        {
            hr = pReader->GetLocalName( &pwszLocalName, NULL );
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            SysFreeString( bstrName );
            bstrName = NULL;

            bstrName = SysAllocString( pwszLocalName );
            if( !bstrName )
            {
                hr = E_OUTOFMEMORY;
                DBGERROR_HR(hr);
                goto exit;
            }

            isEmpty = pReader->IsEmptyElement();

            if( !pParent )
            {
                //
                // This is the entry point to resetting the section. Begin by
                // clearing.
                //

                hr = pAdminMgr->GetAdminSection( bstrSectionName,
                                                 bstrRoot,
                                                 &pChildElement );
                if( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }

                if (fClearSection)
                {
                    hr = pChildElement->Clear();
                    if( FAILED(hr) )
                    {
                        DBGERROR_HR(hr);
                        goto exit;
                    }

                    // Need to commit change after clear
                    
                    hr = pAdminMgr->CommitChanges();
                    if( FAILED(hr) )
                    {
                        DBGERROR_HR(hr);
                        goto exit;
                    }

                    // Need to reload after commit

                    pChildElement.Release();

                    hr = pAdminMgr->GetAdminSection( bstrSectionName,
                                                     bstrRoot,
                                                     &pChildElement );
                    if( FAILED(hr) )
                    {
                        DBGERROR_HR(hr);
                        goto exit;
                    }
                }

            }
            else
            {
                // Test the parent of this node for a collection

                CComPtr<IAppHostElementCollection> pCollection;
                hr = pParentElement->get_Collection( &pCollection );

                if ( SUCCEEDED ( hr ) && ( pCollection != NULL ) )
                {
                    // Get the schema for the collection
                    CComPtr<IAppHostCollectionSchema> pCollectionSchema;
                    hr = pCollection->get_Schema( &pCollectionSchema );
                    if( FAILED(hr) )
                    {
                        DBGERROR_HR(hr);
                        goto exit;
                    }

                    SysFreeString( bstrAddElementName );
                    bstrAddElementName = NULL;

                    // Parent element has a collection.  Get the addElement token.
                    pCollectionSchema->get_AddElementNames(&bstrAddElementName);

                    if( 0 == wcscmp( pwszLocalName, bstrAddElementName ) )
                    {
                        hr = pCollection->CreateNewElement( bstrName, &pChildElement );
                        if( FAILED(hr) )
                        {
                            DBGERROR_HR(hr);
                            goto exit;
                        }

                        hr = ProcessAttributes( pChildElement, pReader );
                        if( FAILED(hr) )
                        {
                            DBGERROR_HR(hr);
                            goto exit;
                        }

                        hr = pCollection->AddElement( pChildElement );
                        if( FAILED(hr) )
                        {
                            DBGERROR_HR(hr);
                            goto exit;
                        }

                        pCollection.Release();
                    }
                    else
                    {
                        // Parent has collection, but this isn't an 'addElement'
                        hr = pParentElement->GetElementByName( bstrName,
                                                               &pChildElement );
                        if( FAILED(hr) )
                        {
                            DBGERROR(( DBG_CONTEXT, "Failed to get child element %S, %08x\n", bstrName, hr ));
                            goto exit;
                        }
                    }

                    pCollectionSchema.Release();
                }
                else
                {
                    // Parent is not a collection.
                    hr = pParentElement->GetElementByName( bstrName,
                                                           &pChildElement );
                    if( FAILED(hr) )
                    {
                        DBGERROR(( DBG_CONTEXT, "Failed to get child element %S, %08x\n", bstrName, hr ));
                        goto exit;
                    }
                }
            }

            hr = ProcessAttributes( pChildElement, pReader );
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            if( !isEmpty )
            {
                hr = ProcessSection( pAdminMgr,
                                     szSectionName,
                                     pReader,
                                     pChildElement,
                                     fClearSection);
                if( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
            }

            pChildElement.Release();
        }
        else if( nodeType == XmlNodeType_EndElement )
        {
            break;
        }
    }

exit:

    SysFreeString( bstrAddElementName );
    SysFreeString( bstrName );
    SysFreeString( bstrSectionName );
    SysFreeString( bstrRoot );

    return hr;
}

HRESULT
ResetConfigSection(
    IN      IAppHostWritableAdminManager *  pAdminMgr,
    IN      CONST WCHAR *                   szSectionName,
    IN OUT  IStream *                       pStreamDefaults
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IXmlReader>                     pReader;


    hr = CreateXmlReader(__uuidof(IXmlReader), (void**) &pReader, NULL);
    if( FAILED(hr) )
    {
        DBGERROR(( DBG_CONTEXT, "Error creating xml reader, error is %08.8lx", hr));
        goto exit;
    }

    hr = pReader->SetProperty( XmlReaderProperty_DtdProcessing,
                               DtdProcessing_Prohibit );
    if( FAILED(hr) )
    {
        DBGERROR(( DBG_CONTEXT, "Error setting XmlReaderProperty_DtdProcessing, %08x", hr));
        goto exit;
    }

    hr = pReader->SetInput( pStreamDefaults );
    if( FAILED(hr) )
    {
        DBGERROR(( DBG_CONTEXT, "Error setting input for reader, %08x", hr));
        goto exit;
    }

    hr = ProcessSection( pAdminMgr,
                         szSectionName,
                         pReader,
                         NULL,
                         TRUE);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}

HRESULT
CreateStreamFromTextResource(
    IN      HINSTANCE           hInstance,
    IN      CONST WCHAR *       szResourceName,
    OUT     IStream **          ppStream
    )
{
    HRESULT hr = NOERROR;

    HRSRC       hResDefaults = NULL;
    HGLOBAL     hGlobDefaults = NULL;
    LPCVOID     pDefaults = NULL;
    DWORD       cbDefaults;
    HGLOBAL     hDefaultsData = NULL;
    LPVOID      pDefaultsData = NULL;

    hResDefaults = FindResourceW( g_hinst,
                                  szResourceName,
                                  L"TEXT" );
    if( !hResDefaults )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    hGlobDefaults = LoadResource( hInstance, hResDefaults );
    if( !hGlobDefaults )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    pDefaults = LockResource( hGlobDefaults );
    if( !pDefaults )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    cbDefaults = SizeofResource( g_hinst, hResDefaults );
    if( !cbDefaults )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    hDefaultsData = GlobalAlloc( GMEM_MOVEABLE, cbDefaults );
    if( !hDefaultsData )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    pDefaultsData = GlobalLock( hDefaultsData );
    if( !pDefaultsData )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    #pragma prefast( suppress:26010, "cbDefaults is allocated size for pDefaultsData" )
    CopyMemory( pDefaultsData, pDefaults, cbDefaults );

    GlobalUnlock( hDefaultsData );

    hr = CreateStreamOnHGlobal( hDefaultsData, TRUE, ppStream );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    hDefaultsData = NULL;

exit:

    if( hDefaultsData )
    {
        GlobalFree( hDefaultsData );
    }

    return hr;
}


HRESULT
ResetConfigSectionFromResource(
    IN      CONST WCHAR *       szResourceName,
    IN      CONST WCHAR *       szSectionName
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IStream>                        pStream;

    hr = CreateStreamFromTextResource( g_hinst,
                                       szResourceName,
                                       &pStream );
    if( FAILED(hr) )
    {
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

    hr = ResetConfigSection( pAdminMgr,
                             szSectionName,
                             pStream );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Save
    //

    hr = pAdminMgr->CommitChanges();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return ( hr );
}

HRESULT
ResetConfigSectionFromFile(
    IN      CONST WCHAR *       szFileName,
    IN      CONST WCHAR *       szSectionName
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IStream>                        pStream;

    hr = SHCreateStreamOnFileEx( szFileName,
                                 STGM_READ,
                                 FILE_ATTRIBUTE_NORMAL,
                                 FALSE,
                                 NULL,
                                 &pStream);
    if( FAILED(hr) )
    {
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

    hr = ResetConfigSection( pAdminMgr,
                             szSectionName,
                             pStream );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Save
    //

    hr = pAdminMgr->CommitChanges();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return ( hr );
}

HRESULT
AppendConfigSectionFromFile(
    IN      CONST WCHAR *       szFileName,
    IN      CONST WCHAR *       szSectionName
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
    CComPtr<IStream>                        pStream;

    hr = SHCreateStreamOnFileEx( szFileName,
                                 STGM_READ,
                                 FILE_ATTRIBUTE_NORMAL,
                                 FALSE,
                                 NULL,
                                 &pStream);
    if( FAILED(hr) )
    {
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

    hr = AppendConfigSection( pAdminMgr,
                             szSectionName,
                             pStream );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Save
    //

    hr = pAdminMgr->CommitChanges();
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return ( hr );
}


HRESULT
AppendConfigSection(
    IN      IAppHostWritableAdminManager *  pAdminMgr,
    IN      CONST WCHAR *                   szSectionName,
    IN OUT  IStream *                       pStreamDefaults
    )
{
    HRESULT hr = NOERROR;

    CComPtr<IXmlReader>                     pReader;


    hr = CreateXmlReader(__uuidof(IXmlReader), (void**) &pReader, NULL);
    if( FAILED(hr) )
    {
        DBGERROR(( DBG_CONTEXT, "Error creating xml reader, error is %08.8lx", hr));
        goto exit;
    }

    hr = pReader->SetProperty( XmlReaderProperty_DtdProcessing,
                               DtdProcessing_Prohibit );
    if( FAILED(hr) )
    {
        DBGERROR(( DBG_CONTEXT, "Error setting XmlReaderProperty_DtdProcessing, %08x", hr));
        goto exit;
    }

    hr = pReader->SetInput( pStreamDefaults );
    if( FAILED(hr) )
    {
        DBGERROR(( DBG_CONTEXT, "Error setting input for reader, %08x", hr));
        goto exit;
    }

    hr = ProcessSection( pAdminMgr,
                         szSectionName,
                         pReader,
                         NULL,
                         FALSE);
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}
