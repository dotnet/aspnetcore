// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class PATH
{
public:

    static
    HRESULT
    SplitUrl(
        PCWSTR pszDestinationUrl,
        BOOL *pfSecure,
        STRU *pstrDestination,
        STRU *pstrUrl
    );

    static
    HRESULT
    UnEscapeUrl(
        PCWSTR      pszUrl,
        DWORD       cchUrl,
        bool        fCopyQuery,
        STRA *      pstrResult
    );

    static
    HRESULT
    UnEscapeUrl(
        PCWSTR      pszUrl,
        DWORD       cchUrl,
        STRU *      pstrResult
    );

    static HRESULT
    EscapeAbsPath(
        IHttpRequest * pRequest,
        STRU * strEscapedUrl
    );

    static
    bool
    IsValidAttributeNameChar(
        WCHAR ch
    );

    static
    bool
    IsValidQueryStringName(
        PCWSTR  pszName
    );

    static
    bool
    IsValidHeaderName(
        PCWSTR  pszName
    );

    static
    bool
    FindInMultiString(
        PCWSTR      pszMultiString,
        PCWSTR      pszStringToFind
    );

    static
    HRESULT
    IsPathUnc(
        __in  LPCWSTR       pszPath, 
        __out BOOL *        pfIsUnc 
    );

    static
    HRESULT
    ConvertPathToFullPath(
        _In_  LPCWSTR   pszPath,
        _In_  LPCWSTR   pszRootPath,
        _Out_ STRU*     pStrFullPath
    );

private:

    PATH() {}
    ~PATH() {}

    static
    CHAR 
    ToHexDigit(
        UINT nDigit
    )
    {
        return static_cast<CHAR>(nDigit > 9 ? nDigit - 10 + 'A' : nDigit + '0');
    }
};