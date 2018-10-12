// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include <precomp.h>

DECLARE_DEBUG_PRINT_OBJECT( "proxyCA.dll" );

HINSTANCE g_hinst;

BOOL WINAPI
DllMain(
    HINSTANCE   hModule,
    DWORD       dwReason,
    LPVOID      lpReserved
    )
{
    UNREFERENCED_PARAMETER( lpReserved );
    switch( dwReason )
    {
        case DLL_PROCESS_ATTACH:
            CREATE_DEBUG_PRINT_OBJECT;
            DisableThreadLibraryCalls( hModule );
            g_hinst = hModule;
            break;

        case DLL_PROCESS_DETACH:
            break;
    }

    return TRUE;
}


struct COMPRESSION_MIME_TYPE
{
    PCWSTR  pszMimeType;
    BOOL    fEnabled;
};

COMPRESSION_MIME_TYPE gMimeTypes[] =
    { { L"text/event-stream", FALSE} };

UINT
WINAPI
RegisterANCMCompressionCA(
    IN  MSIHANDLE
    )
{
    HRESULT hr = S_OK;
    DWORD                           i;
    VARIANT                         varName;
    IAppHostWritableAdminManager *  pAdminMgr = NULL;
    IAppHostElement *               pHttpCompressionSection = NULL;
    IAppHostElement *               pDynamicCompressionElement = NULL;
    IAppHostElementCollection *     pMimeTypeCollection = NULL;
    IAppHostElement *               pMimeTypeElement = NULL;

    VariantInit(&varName);

    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        goto exit;
    }

    hr = pAdminMgr->GetAdminSection(L"system.webServer/httpCompression",
                                    L"MACHINE/WEBROOT/APPHOST",
                                    &pHttpCompressionSection);
    if (FAILED(hr))
    {
        goto exit;
    }

    hr = pHttpCompressionSection->GetElementByName(L"dynamicTypes",
                                             &pDynamicCompressionElement);
    if (FAILED(hr))
    {
        goto exit;
    }

    hr = pDynamicCompressionElement->get_Collection(&pMimeTypeCollection);
    if (FAILED(hr))
    {
        goto exit;
    }

    hr = pMimeTypeCollection->get_Count(&i);
    if (FAILED(hr) || i == 0)
    {
        // failure or DynamicCmpression is not enabled
        goto exit;
    }

    for (i=0; i<_countof(gMimeTypes); i++)
    {
        hr = pMimeTypeCollection->CreateNewElement(L"add",
                                                   &pMimeTypeElement);
        if (FAILED(hr))
        {
            goto exit;
        }

        hr = VariantAssign(&varName,
                           gMimeTypes[i].pszMimeType);
        if (FAILED(hr))
        {
            goto exit;
        }

        hr = SetElementProperty(pMimeTypeElement,
                                L"mimeType",
                                &varName);
        if (FAILED(hr))
        {
            goto exit;
        }
        VariantClear(&varName);

        varName.vt = VT_BOOL;
        varName.boolVal = gMimeTypes[i].fEnabled ? VARIANT_TRUE : VARIANT_FALSE;

        hr = SetElementProperty(pMimeTypeElement,
                                L"enabled",
                                &varName);
        if (FAILED(hr))
        {
            goto exit;
        }
        VariantClear(&varName);

        hr = pMimeTypeCollection->AddElement(pMimeTypeElement);
        if (FAILED(hr) &&
            hr != HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
        {
            goto exit;
        }

        pMimeTypeElement->Release();
        pMimeTypeElement = NULL;
    }

    hr = pAdminMgr->CommitChanges();

 exit:

    VariantClear(&varName);

    if (pMimeTypeElement != NULL)
    {
        pMimeTypeElement->Release();
        pMimeTypeElement = NULL;
    }

    if (pMimeTypeCollection != NULL)
    {
        pMimeTypeCollection->Release();
        pMimeTypeCollection = NULL;
    }

    if (pDynamicCompressionElement != NULL)
    {
        pDynamicCompressionElement->Release();
        pDynamicCompressionElement = NULL;
    }

    if (pHttpCompressionSection != NULL)
    {
        pHttpCompressionSection->Release();
        pHttpCompressionSection = NULL;
    }

    if (pAdminMgr != NULL)
    {
        pAdminMgr->Release();
        pAdminMgr = NULL;
    }

    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_SUCCESS;
}

