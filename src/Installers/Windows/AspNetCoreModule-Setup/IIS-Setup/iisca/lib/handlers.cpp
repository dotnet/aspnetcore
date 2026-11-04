// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

//
// Local function declarations
//

//
// Public functions
//

HRESULT
RegisterHandler(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          ULONG                   Index,
    IN          CONST WCHAR *           szName,
    IN          CONST WCHAR *           szPath,
    IN          CONST WCHAR *           szVerbs,
    IN OPTIONAL CONST WCHAR *           szType,
    IN OPTIONAL CONST WCHAR *           szModules,
    IN OPTIONAL CONST WCHAR *           szScriptProcessor,
    IN OPTIONAL CONST WCHAR *           szResourceType,
    IN OPTIONAL CONST WCHAR *           szRequiredAccess,
    IN OPTIONAL CONST WCHAR *           szPreCondition
    )
{
    HRESULT hr;
    CComPtr<IAppHostElementCollection> pHandlersCollection;
    CComPtr<IAppHostElement> pNewElement;
    UINT numDeleted;
    BSTR bstrAdd;

    bstrAdd = SysAllocString(L"add");

    if (bstrAdd == NULL)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = GetHandlersCollection(
            pAdminMgr,
            szConfigPath,
            &pHandlersCollection
            );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Just in case... delete if it's already there.
    //

    hr = DeleteAllElementsFromCollection(
             pHandlersCollection,
             L"name",
             szName,
             FIND_ELEMENT_CASE_SENSITIVE,
             &numDeleted
             );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if (Index == HANDLER_INDEX_BEFORE_STATICFILE)
    {
                //
                // If the StaticFile handler is installed (and it probably is)
                // then install just before it.
                //

                hr = FindHandlerByName(
                                pAdminMgr,
                                szConfigPath,
                                HANDLER_STATICFILE_NAME,
                                &Index
                                );

                if (FAILED(hr))
                {
                        DBGERROR_HR(hr);
                        goto exit;
                }

                if (hr == S_FALSE)
                {
                        //
                        // Not found.  Install at end of list.
                        //

                        Index = HANDLER_INDEX_LAST;
                }
        }

    //
    // If the caller wants this to be the last handler, then
    // chase down the current entry count.
    //

    if (Index == HANDLER_INDEX_LAST)
    {
        hr = pHandlersCollection->get_Count(&Index);

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    hr = pHandlersCollection->CreateNewElement(bstrAdd, &pNewElement);

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Required properties.
    //

    hr = SetElementStringProperty(
             pNewElement,
             L"name",
             szName
             );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = SetElementStringProperty(
             pNewElement,
             L"path",
             szPath
             );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = SetElementStringProperty(
             pNewElement,
             L"verb",
             szVerbs
             );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Optional properties.
    //

    if (szType != NULL && szType[0] != L'\0')
    {
        hr = SetElementStringProperty(
                 pNewElement,
                 L"type",
                 szType
                 );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if (szModules != NULL && szModules[0] != L'\0')
    {
        hr = SetElementStringProperty(
                 pNewElement,
                 L"modules",
                 szModules
                 );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if (szScriptProcessor != NULL && szScriptProcessor[0] != L'\0')
    {
        hr = SetElementStringProperty(
                 pNewElement,
                 L"scriptProcessor",
                 szScriptProcessor
                 );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if (szResourceType != NULL && szResourceType[0] != L'\0')
    {
        hr = SetElementStringProperty(
                 pNewElement,
                 L"resourceType",
                 szResourceType
                 );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if (szRequiredAccess != NULL && szRequiredAccess[0] != L'\0')
    {
        hr = SetElementStringProperty(
                 pNewElement,
                 L"requireAccess",
                 szRequiredAccess
                 );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    if (szPreCondition != NULL && szPreCondition[0] != L'\0')
    {
        hr = SetElementStringProperty(
                 pNewElement,
                 L"preCondition",
                 szPreCondition
                 );

        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
    }

    hr = pHandlersCollection->AddElement(pNewElement, Index);

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString(bstrAdd);
    return hr;
}

HRESULT
UnRegisterHandler(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szName
    )
{
    HRESULT hr;
    CComPtr<IAppHostElementCollection> pHandlersCollection;
    CComPtr<IAppHostConfigLocationCollection> pLocationCollection;
    CComPtr<IAppHostConfigLocation> pLocation;
    CComPtr<IAppHostElement> pElement;
    ENUM_INDEX locationIndex;
    BOOL found;
    UINT numDeleted;

    //
    // Enum the <location> tags, look for any <handler> sections,
    // and if found, remove the specified handler from each.
    //

    hr = GetLocationCollection(
            pAdminMgr,
            szConfigPath,
            &pLocationCollection
            );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    for (hr = FindFirstLocation(pLocationCollection, &locationIndex, &pLocation) ;
         SUCCEEDED(hr) ;
         hr = FindNextLocation(pLocationCollection, &locationIndex, &pLocation))
    {
        if (hr == S_FALSE)
        {
            hr = S_OK;
            break;
        }

        hr = GetSectionFromLocation(
                 pLocation,
                 L"system.webServer/handlers",
                 &pElement,
                 &found
                 );

        if (SUCCEEDED(hr))
        {
            if (found)
            {
                hr = pElement->get_Collection(&pHandlersCollection);

                if (SUCCEEDED(hr))
                {
                    hr = DeleteAllElementsFromCollection(
                             pHandlersCollection,
                             L"name",
                             szName,
                             FIND_ELEMENT_CASE_SENSITIVE,
                             &numDeleted
                             );

                    if( FAILED(hr) )
                    {
                        DBGERROR_HR(hr);
                    }

                    pHandlersCollection.Release();
                }
                else
                {
                    DBGERROR_HR(hr);
                }

                pElement.Release();
            }
        }
        else
        {
            DBGERROR_HR(hr);
        }

        pLocation.Release();
    }

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}

HRESULT
FindHandlerByName(
    IN          IAppHostAdminManager *  pAdminMgr,
    IN          CONST WCHAR *           szConfigPath,
    IN          CONST WCHAR *           szName,
    OUT         ULONG *                 pIndex
    )
{
    HRESULT hr;
    CComPtr<IAppHostElementCollection> pHandlersCollection;

    *pIndex = 0;

    hr = GetHandlersCollection(
             pAdminMgr,
             szConfigPath,
             &pHandlersCollection
             );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = FindElementInCollection(
             pHandlersCollection,
             L"name",
             szName,
             FIND_ELEMENT_CASE_SENSITIVE,
             pIndex
             );

    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}

HRESULT
GetHandlersCollection(
    IN      IAppHostAdminManager *              pAdminMgr,
    IN      CONST WCHAR *                       szConfigPath,
    OUT     IAppHostElementCollection **        pHandlersCollection
    )
{
    HRESULT hr;
    CComPtr<IAppHostElement> pHandlersElement;
    BSTR bstrConfigPath;
    BSTR bstrHandlersSectionName;

    bstrConfigPath = SysAllocString(szConfigPath);
    bstrHandlersSectionName = SysAllocString(L"system.webServer/handlers");
    *pHandlersCollection = NULL;

    if (bstrConfigPath == NULL || bstrHandlersSectionName == NULL)
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Chase down the handlers collection.
    //

    hr = pAdminMgr->GetAdminSection( bstrHandlersSectionName,
                                     bstrConfigPath,
                                     &pHandlersElement );

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pHandlersElement->get_Collection(pHandlersCollection);

    if (FAILED(hr))
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    SysFreeString(bstrHandlersSectionName);
    SysFreeString(bstrConfigPath);
    return hr;
}

