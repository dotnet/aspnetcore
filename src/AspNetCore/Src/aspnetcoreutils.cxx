// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

//
// ReplacePlaceHolderWithValue replaces a placeholder found in 
// pszStr with dwValue.
// If replace is successful, pfReplaced is TRUE else FALSE.
//

HRESULT 
ASPNETCORE_UTILS::ReplacePlaceHolderWithValue(
    _Inout_ LPWSTR  pszStr, 
    _In_    LPWSTR  pszPlaceholder, 
    _In_    DWORD   cchPlaceholder,
    _In_    DWORD   dwValue,
    _In_    DWORD   dwNumDigitsInValue,
    _Out_   BOOL   *pfReplaced
)
{
    HRESULT     hr = S_OK;
    LPWSTR      pszPortPlaceHolder = NULL;

    DBG_ASSERT( pszStr != NULL );
    DBG_ASSERT( pszPlaceholder != NULL );
    DBG_ASSERT( pfReplaced != NULL );

    *pfReplaced = FALSE;

    if((pszPortPlaceHolder = wcsstr(pszStr, pszPlaceholder)) != NULL)
    {
        if( swprintf_s( pszPortPlaceHolder, 
                        cchPlaceholder,
                        L"%u", 
                        dwValue ) == -1 )
        {
            hr = HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER );
            goto Finished;
        }

        if( wmemcpy_s( pszPortPlaceHolder + dwNumDigitsInValue,
                       cchPlaceholder,
                       L"                                    ",
                       cchPlaceholder - dwNumDigitsInValue ) != 0 )
        {
            hr = HRESULT_FROM_WIN32( EINVAL );
            goto Finished;
        }

        *pfReplaced = TRUE;
    }
Finished:
    return hr;
}