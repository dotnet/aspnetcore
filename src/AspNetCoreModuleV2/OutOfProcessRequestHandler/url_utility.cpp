// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "url_utility.h"

#include <Shlwapi.h>
#include "debugutil.h"

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
    HRESULT hr;

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
        return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    if (*pszDestinationUrl == L'\0')
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    //
    // Find the 3rd slash corresponding to the url
    //
    LPCWSTR pszSlash = wcschr(pszDestinationUrl, L'/');
    if (pszSlash == NULL)
    {
        if (FAILED(hr = pstrUrl->Copy(L"/", 1)) ||
            FAILED(hr = pstrDestination->Copy(pszDestinationUrl)))
        {
            return hr;
        }
    }
    else
    {
        if (FAILED(hr = pstrUrl->Copy(pszSlash)) ||
            FAILED(hr = pstrDestination->Copy(pszDestinationUrl,
                            (DWORD)(pszSlash - pszDestinationUrl))))
        {
            return hr;
        }
    }

    return S_OK;
}

// Change a hexadecimal digit to its numerical equivalent
#define TOHEX( ch )                                     \
    ((ch) > L'9' ?                                      \
        (ch) >= L'a' ?                                  \
            (ch) - L'a' + 10 :                          \
            (ch) - L'A' + 10                            \
        : (ch) - L'0')

HRESULT
URL_UTILITY::EscapeAbsPath(
    IHttpRequest * pRequest,
    STRU * strEscapedUrl
)
{
    HRESULT hr = S_OK;
    STRU    strAbsPath;
    LPCWSTR pszAbsPath = NULL;
    LPCWSTR pszFindStr = NULL;

    hr = strAbsPath.Copy( pRequest->GetRawHttpRequest()->CookedUrl.pAbsPath,
        pRequest->GetRawHttpRequest()->CookedUrl.AbsPathLength / sizeof(WCHAR) );
    if(FAILED(hr))
    {
        goto Finished;
    }

    pszAbsPath = strAbsPath.QueryStr();
    pszFindStr = wcschr(pszAbsPath, L'?');

    while(pszFindStr != NULL)
    {
        strEscapedUrl->Append( pszAbsPath, pszFindStr - pszAbsPath);
        strEscapedUrl->Append(L"%3F");
        pszAbsPath = pszFindStr + 1;
        pszFindStr = wcschr(pszAbsPath, L'?');
    }

    strEscapedUrl->Append(pszAbsPath);
    strEscapedUrl->Append(pRequest->GetRawHttpRequest()->CookedUrl.pQueryString,
                          pRequest->GetRawHttpRequest()->CookedUrl.QueryStringLength / sizeof(WCHAR));

Finished:
    return hr;
}

