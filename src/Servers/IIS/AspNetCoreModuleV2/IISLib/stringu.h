// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "buffer.h"
#include <CodeAnalysis/Warnings.h>
#include <strsafe.h>

class STRU
{

public:

    STRU(
    );

    STRU(
        __inout_ecount(cchInit) WCHAR* pbInit,
        __in DWORD cchInit
    );

    BOOL
    IsEmpty(
    ) const;

    BOOL
    Equals(
        __in const STRU *  pstrRhs,
        __in BOOL           fIgnoreCase = FALSE
    ) const
    {
        _ASSERTE( pstrRhs != NULL );
        return Equals( pstrRhs->QueryStr(), fIgnoreCase );
    }

    BOOL
    Equals(
        __in const STRU &  strRhs,
        __in BOOL           fIgnoreCase = FALSE
    ) const
    {
        return Equals( strRhs.QueryStr(), fIgnoreCase );
    }

    BOOL
    Equals(
        __in PCWSTR pszRhs,
        __in BOOL   fIgnoreCase = FALSE
    ) const
    {
        _ASSERTE( NULL != pszRhs );
        if ( nullptr == pszRhs )
        {
            return FALSE;
        }

    #if defined( NTDDI_VERSION ) && NTDDI_VERSION >= NTDDI_LONGHORN

        return ( CSTR_EQUAL == CompareStringOrdinal( QueryStr(),
                                                     QueryCCH(),
                                                     pszRhs,
                                                     -1,
                                                     fIgnoreCase ) );
    #else

        if( fIgnoreCase )
        {
            return ( 0 == _wcsicmp( QueryStr(), pszRhs ) );
        }
        return ( 0 == wcscmp( QueryStr(), pszRhs ) );

    #endif
    }


    static
    BOOL
    Equals(
        __in PCWSTR pwszLhs,
        __in PCWSTR pwszRhs,
        __in bool   fIgnoreCase = false
    )
    {
        // Return FALSE if either or both strings are NULL.
        if (!pwszLhs || !pwszRhs) return FALSE;

    //
    // This method performs a ordinal string comparison when OS is Vista or
    // greater and a culture sensitive comparison if not (XP). This is
    // consistent with the existing Equals implementation (see above).
    //
#if defined( NTDDI_VERSION ) && NTDDI_VERSION >= NTDDI_LONGHORN

        return ( CSTR_EQUAL == CompareStringOrdinal( pwszLhs,
                                                     -1,
                                                     pwszRhs,
                                                     -1,
                                                     fIgnoreCase ) );
#else

        if( fIgnoreCase )
        {
            return ( 0 == _wcsicmp( pwszLhs, pwszRhs ) );
        }
        else
        {
            return ( 0 == wcscmp( pwszLhs, pwszRhs ) );
        }

#endif
    }

    VOID
    Trim();

    BOOL
    StartsWith(
        __in const STRU *   pStruPrefix,
        __in bool           fIgnoreCase = FALSE
    ) const
    {
        _ASSERTE( pStruPrefix != NULL );
        return StartsWith( pStruPrefix->QueryStr(), fIgnoreCase );
    }

    BOOL
    StartsWith(
        __in const STRU &   struPrefix,
        __in bool           fIgnoreCase = FALSE
    ) const
    {
        return StartsWith( struPrefix.QueryStr(), fIgnoreCase );
    }

    BOOL
    StartsWith(
        __in PCWSTR         pwszPrefix,
        __in bool           fIgnoreCase = FALSE
    ) const;

    BOOL
    EndsWith(
        __in const STRU *   pStruSuffix,
        __in bool           fIgnoreCase = FALSE
    ) const
    {
        _ASSERTE( pStruSuffix != NULL );
        return EndsWith( pStruSuffix->QueryStr(), fIgnoreCase );
    }

    BOOL
    EndsWith(
        __in const STRU &   struSuffix,
        __in bool           fIgnoreCase = FALSE
    ) const
    {
        return EndsWith( struSuffix.QueryStr(), fIgnoreCase );
    }

    BOOL
    EndsWith(
        __in PCWSTR         pwszSuffix,
        __in bool           fIgnoreCase = FALSE
    ) const;

    INT
    IndexOf(
        __in WCHAR          charValue,
        __in DWORD          dwStartIndex = 0
    ) const;

    INT
    IndexOf(
        __in PCWSTR         pwszValue,
        __in DWORD          dwStartIndex = 0
    ) const;

    INT
    LastIndexOf(
        __in WCHAR          charValue,
        __in DWORD          dwStartIndex = 0
    ) const;

    DWORD
    QueryCB(
        VOID
    ) const;

    DWORD
    QueryCCH(
        VOID
    ) const;

    DWORD
    QuerySizeCCH(
        VOID
    ) const;

    __nullterminated
    __ecount(this->m_cchLen)
    WCHAR*
    QueryStr(
    ) const;

    VOID
    Reset(
    );

    HRESULT
    Resize(
        DWORD cchSize
    );

    HRESULT
    SyncWithBuffer(
    );

    template<size_t size>
    HRESULT
    Copy(
        __in PCWSTR const (&rgpszStrings)[size]
    )
    //
    // Copies an array of strings declared as stack array. For example:
    //
    // LPCWSTR rgExample[] { L"one", L"two" };
    // hr = str.Copy( rgExample );
    //
    {
        Reset();

        return AuxAppend( rgpszStrings, _countof( rgpszStrings ) );
    }

    HRESULT
    Copy(
        __in PCWSTR pszCopy
    );

    HRESULT
    Copy(
        __in_ecount(cchLen)
        PCWSTR  pszCopy,
        SIZE_T  cchLen
    );

    HRESULT
    Copy(
        __in const STRU * pstrRhs
    );

    HRESULT
    Copy(
        __in const STRU & str
    );

    HRESULT
    CopyAndExpandEnvironmentStrings(
        __in PCWSTR     pszSource
    );

    HRESULT
    CopyA(
        __in PCSTR  pszCopyA
    );

    HRESULT
    CopyA(
        __in_bcount(cchLen)
        PCSTR   pszCopyA,
        SIZE_T  cchLen,
        UINT    CodePage = CP_UTF8
    );

    template<size_t size>
    HRESULT
    Append(
        __in PCWSTR const (&rgpszStrings)[size]
    )
    //
    // Appends an array of strings declared as stack array. For example:
    //
    // LPCWSTR rgExample[] { L"one", L"two" };
    // hr = str.Append( rgExample );
    //
    {
        return AuxAppend( rgpszStrings, _countof( rgpszStrings ) );
    }

    HRESULT
    Append(
        __in PCWSTR  pszAppend
    );

    HRESULT
    Append(
        __in_ecount(cchLen)
        PCWSTR  pszAppend,
        SIZE_T  cchLen
    );

    HRESULT
    Append(
        __in const STRU * pstrRhs
    );

    HRESULT
    Append(
        __in const STRU & strRhs
    );

    HRESULT
    AppendA(
        __in PCSTR  pszAppendA
    );

    HRESULT
    AppendA(
        __in_bcount(cchLen)
        PCSTR   pszAppendA,
        SIZE_T  cchLen,
        UINT    CodePage = CP_UTF8
    );

    HRESULT
    CopyToBuffer(
        __out_bcount(*pcb) WCHAR*   pszBuffer,
        PDWORD                      pcb
    ) const;

    HRESULT
    SetLen(
        __in DWORD cchLen
    );

    HRESULT
    SafeSnwprintf(
        __in PCWSTR pwszFormatString,
        ...
    );

    HRESULT
    SafeVsnwprintf(
        __in PCWSTR pwszFormatString,
        va_list     argsList
    );

private:

    //
    // Avoid C++ errors. This object should never go through a copy
    // constructor, unintended cast or assignment.
    //
    STRU( const STRU & );
    STRU & operator = ( const STRU & );

    HRESULT
    AuxAppend(
        __in_ecount(cNumStrings)
        PCWSTR const    rgpszStrings[],
        SIZE_T          cNumStrings
    );

    HRESULT
    AuxAppend(
        __in_bcount(cbStr)
        const WCHAR*    pStr,
        SIZE_T          cbStr,
        DWORD           cbOffset
    );

    HRESULT
    AuxAppendA(
        __in_bcount(cbStr)
        const CHAR*     pStr,
        SIZE_T          cbStr,
        DWORD           cbOffset,
        UINT            CodePage
    );

    //
    // Buffer with an inline buffer of 1,
    // enough to hold null-terminating character.
    //
    BUFFER_T<WCHAR,1>   m_Buff;
    DWORD               m_cchLen;
};

//
// Helps to initialize an external buffer before
// constructing the STRU object.
//
template<DWORD size>
WCHAR* InitHelper(__out WCHAR (&psz)[size])
{
    psz[0] = L'\0';
    return psz;
}

//
// Heap operation reduction macros
//
#define STACK_STRU(name, size)  WCHAR __ach##name[size];\
                                STRU name(InitHelper(__ach##name), sizeof(__ach##name)/sizeof(*__ach##name))

#define INLINE_STRU(name, size) WCHAR  __ach##name[size];\
                                STRU  name;

#define INLINE_STRU_INIT(name) name(InitHelper(__ach##name), sizeof(__ach##name)/sizeof(*__ach##name))


HRESULT
MakePathCanonicalizationProof(
    IN PCWSTR               pszName,
    OUT STRU *              pstrPath
);
