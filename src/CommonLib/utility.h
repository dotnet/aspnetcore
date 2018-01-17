// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class UTILITY
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

    static
    HRESULT
    EnsureDirectoryPathExist(
        _In_  LPCWSTR pszPath
    );

    static
    BOOL
    DirectoryExists(
        _In_ STRU *pstrPath
    );

    static
    VOID
    FindDotNetFolders(
        _In_ PCWSTR pszPath,
        _Out_ std::vector<std::wstring> *pvFolders
    );

    static
    HRESULT
    FindHighestDotNetVersion(
        _In_ std::vector<std::wstring> vFolders,
        _Out_ STRU *pstrResult
    );

    static
    BOOL
    CheckIfFileExists(
        PCWSTR pszFilePath
    );

    static
    VOID
    LogEvent(
        _In_ HANDLE  hEventLog,
        _In_ WORD    dwEventInfoType,
        _In_ DWORD   dwEventId,
        _In_ LPCWSTR pstrMsg
   );

private:

    UTILITY() {}
    ~UTILITY() {}
};
