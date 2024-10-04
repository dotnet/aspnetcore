// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "url_utility.h"

#include <Shlwapi.h>
#include "debugutil.h"
#include "exceptions.h"

// static
HRESULT
URL_UTILITY::SplitUrl(
    PCWSTR pszDestinationUrl,
    BOOL *pfSecure,
    STRU *pstrDestination,
    STRU *pstrUrl
)
/*++

Routine Description:

    Split the URL specified for forwarding into its specific components
    The format of the URL looks like
    http[s]://destination[:port]/path
    when port is omitted, the default port for that specific protocol is used
    when host is omitted, it gets the same value as the destination

Arguments:

    pszDestinationUrl - the url to be split up
    pfSecure - SSL to be used in forwarding?
    pstrDestination - destination
    pDestinationPort - port
    pstrUrl - URL

Return Value:

    HRESULT

--*/
{
    //
    // First determine if the target is secure
    //
    if (_wcsnicmp(pszDestinationUrl, L"http://", 7) == 0)
    {
        *pfSecure = FALSE;
        pszDestinationUrl += 7;
    }
    else if (_wcsnicmp(pszDestinationUrl, L"https://", 8) == 0)
    {
        *pfSecure = TRUE;
        pszDestinationUrl += 8;
    }
    else
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
    }

    if (*pszDestinationUrl == L'\0')
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
    }

    //
    // Find the 3rd slash corresponding to the url
    //
    LPCWSTR pszSlash = wcschr(pszDestinationUrl, L'/');
    if (pszSlash == nullptr)
    {
        RETURN_IF_FAILED(pstrUrl->Copy(L"/", 1));
        RETURN_IF_FAILED(pstrDestination->Copy(pszDestinationUrl));
    }
    else
    {
        RETURN_IF_FAILED(pstrUrl->Copy(pszSlash));
        RETURN_IF_FAILED(pstrDestination->Copy(pszDestinationUrl,
                            (DWORD)(pszSlash - pszDestinationUrl)));
    }

    return S_OK;
}

HRESULT
URL_UTILITY::EscapeAbsPath(
    IHttpRequest * pRequest,
    STRU * strEscapedUrl
)
{
    STRU    strAbsPath;
    LPCWSTR pszAbsPath = nullptr;
    LPCWSTR pszFindStr = nullptr;

    RETURN_IF_FAILED(strAbsPath.Copy( pRequest->GetRawHttpRequest()->CookedUrl.pAbsPath,
        pRequest->GetRawHttpRequest()->CookedUrl.AbsPathLength / sizeof(WCHAR) ));

    pszAbsPath = strAbsPath.QueryStr();
    pszFindStr = wcschr(pszAbsPath, L'?');

    while(pszFindStr != nullptr)
    {
        RETURN_IF_FAILED(strEscapedUrl->Append( pszAbsPath, pszFindStr - pszAbsPath));
        RETURN_IF_FAILED(strEscapedUrl->Append(L"%3F"));
        pszAbsPath = pszFindStr + 1;
        pszFindStr = wcschr(pszAbsPath, L'?');
    }

    RETURN_IF_FAILED(strEscapedUrl->Append(pszAbsPath));
    RETURN_IF_FAILED(strEscapedUrl->Append(pRequest->GetRawHttpRequest()->CookedUrl.pQueryString,
                          pRequest->GetRawHttpRequest()->CookedUrl.QueryStringLength / sizeof(WCHAR)));

    return S_OK;
}

