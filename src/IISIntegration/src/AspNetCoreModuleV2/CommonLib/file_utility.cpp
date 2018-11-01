// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "file_utility.h"

#include <Shlwapi.h>
#include "debugutil.h"
#include "exceptions.h"

HRESULT
FILE_UTILITY::IsPathUnc(
    __in  LPCWSTR       pszPath,
    __out BOOL *        pfIsUnc
)
{
    HRESULT hr = S_OK;
    STRU strTempPath;

    if ( pszPath == NULL || pfIsUnc == NULL )
    {
        hr = E_INVALIDARG;
        goto Finished;
    }

    hr = MakePathCanonicalizationProof( (LPWSTR) pszPath, &strTempPath );
    if ( FAILED(hr) )
    {
        goto Finished;
    }

    //
    // MakePathCanonicalizationProof will map \\?\UNC, \\.\UNC and \\ to \\?\UNC
    //
    (*pfIsUnc) = ( _wcsnicmp( strTempPath.QueryStr(), L"\\\\?\\UNC\\", 8 /* sizeof \\?\UNC\ */) == 0 );

Finished:

    return hr;
}

HRESULT
FILE_UTILITY::ConvertPathToFullPath(
    _In_  LPCWSTR   pszPath,
    _In_  LPCWSTR   pszRootPath,
    _Out_ STRU*     pStruFullPath
)
{
    HRESULT hr = S_OK;
    STRU strFileFullPath;
    LPWSTR pszFullPath = NULL;

    // if relative path, prefix with root path and then convert to absolute path.
    if ( PathIsRelative(pszPath) )
    {
        hr = strFileFullPath.Copy(pszRootPath);
        if(FAILED(hr))
        {
            goto Finished;
        }

        if(!strFileFullPath.EndsWith(L"\\"))
        {
            hr = strFileFullPath.Append(L"\\");
            if(FAILED(hr))
            {
                goto Finished;
            }
        }
    }

    hr = strFileFullPath.Append( pszPath );
    if (FAILED(hr))
    {
        goto Finished;
    }

    pszFullPath = new WCHAR[ strFileFullPath.QueryCCH() + 1];

    if(_wfullpath( pszFullPath,
                   strFileFullPath.QueryStr(),
                   strFileFullPath.QueryCCH() + 1 ) == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Finished;
    }

    // convert to canonical path
    hr = MakePathCanonicalizationProof( pszFullPath, pStruFullPath );
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:

    if ( pszFullPath != NULL )
    {
        delete[] pszFullPath;
        pszFullPath = NULL;
    }

    return hr;
}

HRESULT
FILE_UTILITY::EnsureDirectoryPathExist(
    _In_  LPCWSTR pszPath
)
{
    HRESULT hr = S_OK;
    STRU    struPath;
    DWORD   dwPosition = 0;
    BOOL    fDone = FALSE;
    BOOL    fUnc = FALSE;

    struPath.Copy(pszPath);
    hr = IsPathUnc(pszPath, &fUnc);
    if (FAILED(hr))
    {
        goto Finished;
    }
    if (fUnc)
    {
        // "\\?\UNC\"
        dwPosition = 8;
    }
    else if (struPath.IndexOf(L'?', 0) != -1)
    {
        // sceanrio "\\?\"
        dwPosition = 4;
    }
    while (!fDone)
    {
        dwPosition = struPath.IndexOf(L'\\', dwPosition + 1);
        if (dwPosition == -1)
        {
            // not found '/'
            fDone = TRUE;
            goto Finished;
        }
        else if (dwPosition ==0)
        {
            hr = ERROR_INTERNAL_ERROR;
            goto Finished;
        }
        else if (struPath.QueryStr()[dwPosition-1] == L':')
        {
            //  skip volume case
            continue;
        }
        else
        {
            struPath.QueryStr()[dwPosition] = L'\0';
        }

        if (!CreateDirectory(struPath.QueryStr(), NULL) &&
            ERROR_ALREADY_EXISTS != GetLastError())
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            fDone = TRUE;
            goto Finished;
        }
        struPath.QueryStr()[dwPosition] = L'\\';
    }

Finished:
    return hr;
}

