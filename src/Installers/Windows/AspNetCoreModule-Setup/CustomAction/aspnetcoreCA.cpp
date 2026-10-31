// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <precomp.h>
#include <MsiQuery.h>
#include <msxml6.h>

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

#define _HR_RET(hr)                                             __pragma(warning(push)) \
    __pragma(warning(disable:26498)) /*disable constexpr warning */ \
    const HRESULT __hrRet = hr; \
    __pragma(warning(pop))

#define _GOTO_FINISHED()                                        __pragma(warning(push)) \
    __pragma(warning(disable:26438)) /*disable avoid goto warning*/ \
    goto Finished \
    __pragma(warning(pop))

#define RETURN_IF_FAILED(hrr)                                 do { _HR_RET(hrr); if (FAILED(__hrRet)) { hr = __hrRet; IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Exiting hr=0x%x", hr); return hr; }} while (0, 0)

// Modifies the configSections to include the aspNetCore section
UINT
WINAPI
AddConfigSection(
	IN MSIHANDLE handle
)
{
    HRESULT hr;
    CComPtr<IXMLDOMDocument2> pXMLDoc;
    VARIANT_BOOL variantResult;
    IXMLDOMNode* webServerNode;
    IXMLDOMNode* aspNetCoreNode;
    IXMLDOMNode* tempNode;
    IXMLDOMElement* element;
    STRU customActionData;

	CComBSTR selectLanguage = SysAllocString(L"SelectionLanguage");
	CComBSTR xPath = SysAllocString(L"XPath");
	CComBSTR webServerPath = SysAllocString(L"//configuration/configSections/sectionGroup[@name=\"system.webServer\"]");
	CComBSTR aspNetCorePath = SysAllocString(L"//configuration/configSections/sectionGroup[@name=\"system.webServer\"]/section[@name=\"aspNetCore\"]");
	CComBSTR section = SysAllocString(L"section");
	CComBSTR name = SysAllocString(L"name");
	CComBSTR aspNetCore = SysAllocString(L"aspNetCore");
	CComBSTR overrideMode = SysAllocString(L"overrideModeDefault");
	CComBSTR allow = SysAllocString(L"Allow");

	RETURN_IF_FAILED(CoInitialize(NULL));

	hr = MsiUtilGetProperty(handle, TEXT("CustomActionData"), &customActionData);

	RETURN_IF_FAILED(hr = pXMLDoc.CoCreateInstance(__uuidof(DOMDocument60)));

	RETURN_IF_FAILED(hr = pXMLDoc->put_async(false));

	RETURN_IF_FAILED(hr = pXMLDoc->load(CComVariant(customActionData.QueryStr()), &variantResult));

	if (variantResult == VARIANT_FALSE)
	{
		return ERROR_SUCCESS;
	}

	RETURN_IF_FAILED(hr = pXMLDoc->setProperty(selectLanguage, CComVariant(xPath)));

	RETURN_IF_FAILED(hr = pXMLDoc->selectSingleNode(webServerPath, &webServerNode));

	RETURN_IF_FAILED(hr = pXMLDoc->selectSingleNode(aspNetCorePath, &aspNetCoreNode));

	if (aspNetCoreNode == NULL)
	{
		RETURN_IF_FAILED(hr = pXMLDoc->createElement(section, &element));

		RETURN_IF_FAILED(hr = element->setAttribute(name, CComVariant(aspNetCore)));

		RETURN_IF_FAILED(hr = element->setAttribute(overrideMode, CComVariant(allow)));

		RETURN_IF_FAILED(hr = webServerNode->appendChild(element, &tempNode));

		RETURN_IF_FAILED(hr = pXMLDoc->save(CComVariant(customActionData.QueryStr())));
	}

	return ERROR_SUCCESS;
}

// Modifies the configSections to remove the aspNetCore section
UINT
WINAPI
RemoveConfigSection(
    IN MSIHANDLE handle
)
{
    HRESULT hr;
    CComPtr<IXMLDOMDocument2> pXMLDoc;
    VARIANT_BOOL variantResult;
    IXMLDOMNode* webServerNode;
    IXMLDOMNode* aspNetCoreNode;
    IXMLDOMNode* tempNode;
    STRU customActionData;

    CComBSTR selectLanguage = SysAllocString(L"SelectionLanguage");
    CComBSTR xPath = SysAllocString(L"XPath");
    CComBSTR webServerPath = SysAllocString(L"//configuration/configSections/sectionGroup[@name=\"system.webServer\"]");
    CComBSTR aspNetCorePath = SysAllocString(L"//configuration/configSections/sectionGroup[@name=\"system.webServer\"]/section[@name=\"aspNetCore\"]");
    CComBSTR section = SysAllocString(L"section");
    CComBSTR name = SysAllocString(L"name");
    CComBSTR aspNetCore = SysAllocString(L"aspNetCore");
    CComBSTR overrideMode = SysAllocString(L"overrideModeDefault");
    CComBSTR allow = SysAllocString(L"Allow");

    RETURN_IF_FAILED(CoInitialize(NULL));

    hr = MsiUtilGetProperty(handle, TEXT("CustomActionData"), &customActionData);

    RETURN_IF_FAILED(hr = pXMLDoc.CoCreateInstance(__uuidof(DOMDocument60)));

    RETURN_IF_FAILED(hr = pXMLDoc->put_async(false));

    RETURN_IF_FAILED(hr = pXMLDoc->load(CComVariant(customActionData.QueryStr()), &variantResult));

    if (variantResult == VARIANT_FALSE)
    {
        return ERROR_SUCCESS;
    }

    RETURN_IF_FAILED(hr = pXMLDoc->setProperty(selectLanguage, CComVariant(xPath)));

    RETURN_IF_FAILED(hr = pXMLDoc->selectSingleNode(webServerPath, &webServerNode));

    RETURN_IF_FAILED(hr = pXMLDoc->selectSingleNode(aspNetCorePath, &aspNetCoreNode));

    if (aspNetCoreNode != NULL)
    {
        RETURN_IF_FAILED(webServerNode->removeChild(aspNetCoreNode, &tempNode));

        RETURN_IF_FAILED(hr = pXMLDoc->save(CComVariant(customActionData.QueryStr())));
    }

    return ERROR_SUCCESS;
}

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

