// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "buffer.h"
#include <strsafe.h>

class STRA
{

public:

    STRA(
    );

    STRA(
        __inout_ecount(cchInit) CHAR* pbInit,
        __in DWORD cchInit
    );

    BOOL
    IsEmpty(
    ) const;

    BOOL
    Equals(
        __in PCSTR  pszRhs,
        __in BOOL   fIgnoreCase = FALSE
    ) const;

    BOOL
    Equals(
        __in const STRA *   pstrRhs,
        __in BOOL           fIgnoreCase = FALSE
    ) const;

    DWORD
    QueryCB(
    ) const;

    DWORD
    QueryCCH(
    ) const;

    DWORD
    QuerySizeCCH(
    ) const;

    DWORD
    QuerySize(
    ) const;

    __nullterminated
    __bcount(this->m_cchLen)
    CHAR *
    QueryStr(
    ) const;

    VOID EnsureNullTerminated();

    VOID
    Reset(
    );

    HRESULT
    Resize(
        __in DWORD cchSize
    );

    HRESULT
    SyncWithBuffer(
    );

    HRESULT
    Copy(
        __in PCSTR   pszCopy
    );

    HRESULT
    Copy(
        __in_ecount(cbLen)
        PCSTR           pszCopy,
        __in SIZE_T     cbLen
    );

    HRESULT
    Copy(
        __in const STRA * pstrRhs
    );

    HRESULT
    Copy(
        __in const STRA & strRhs
    );

    HRESULT
    CopyW(
        __in PCWSTR  pszCopyW
    );

    HRESULT
    CopyW(
        __in_ecount(cchLen)
        PCWSTR          pszCopyW,
        __in SIZE_T     cchLen,
        __in UINT       CodePage = CP_UTF8,
        __in BOOL       fFailIfNoTranslation = FALSE
    )
    {
        _ASSERTE( cchLen <= MAXDWORD );

        return AuxAppendW(
            pszCopyW,
            static_cast<DWORD>(cchLen),
            0,
            CodePage,
            fFailIfNoTranslation
        );
    }

    HRESULT
    Append(
        __in PCSTR pszAppend
    );

    HRESULT
    Append(
        __in_ecount(cbLen)
        PCSTR       pszAppend,
        __in SIZE_T cbLen
    );

    HRESULT
    Append(
        __in const STRA & strRhs
    );

    HRESULT
    AppendW(
        __in PCWSTR  pszAppendW
    )
    {
        size_t      cchLen;
        HRESULT hr = StringCchLengthW(pszAppendW,
                                      STRSAFE_MAX_CCH,
                                      &cchLen);
        if ( FAILED( hr ) )
        {
            return hr;
        }
        return AppendW( pszAppendW, cchLen );
    }

    HRESULT
    AppendW(
        __in_ecount(cchLen)
        PCWSTR          pszAppendW,
        __in SIZE_T     cchLen,
        __in UINT       CodePage = CP_UTF8,
        __in BOOL       fFailIfNoTranslation = FALSE
    )
    {
        _ASSERTE( cchLen <= MAXDWORD );
        if ( cchLen == 0 )
        {
            return S_OK;
        }
        return AuxAppendW(
            pszAppendW,
            static_cast<DWORD>(cchLen),
            QueryCB(),
            CodePage,
            fFailIfNoTranslation
        );
    }

    HRESULT
    CopyToBuffer(
        __out_bcount(*pcb) CHAR*    pszBuffer,
        __inout DWORD *             pcb
    ) const;

    HRESULT
    SafeVsnprintf(
        __in __format_string
        PCSTR       pszFormatString,
        va_list     argsList
    );

    HRESULT
    CopyWToUTF8Unescaped(
        __in LPCWSTR cpchStr
    );

    HRESULT
    CopyWToUTF8Unescaped(
        __in_ecount(cch)
        LPCWSTR         cpchStr,
        __in DWORD      cch
    );

private:

    //
    // Avoid C++ errors. This object should never go through a copy
    // constructor, unintended cast or assignment.
    //
    STRA( const STRA &);
    STRA & operator = (const STRA &);

    HRESULT
    AuxAppend(
        __in_ecount(cbLen)
        LPCSTR          pStr,
        __in DWORD      cbLen,
        __in DWORD      cbOffset
    );

    HRESULT
    AuxAppendW(
        __in_ecount(cchAppendW)
        PCWSTR          pszAppendW,
        __in DWORD      cchAppendW,
        __in DWORD      cbOffset,
        __in UINT       CodePage,
        __in BOOL       fFailIfNoTranslation
    )
    {
        DWORD dwFlags = 0;

        if( CP_ACP == CodePage )
        {
            dwFlags = WC_NO_BEST_FIT_CHARS;
        }
        else if( fFailIfNoTranslation && CodePage == CP_UTF8 )
        {
            //
            // WC_ERR_INVALID_CHARS is only supported in Longhorn or greater.
            //
#if defined( NTDDI_VERSION ) && NTDDI_VERSION >= NTDDI_LONGHORN
            dwFlags |= WC_ERR_INVALID_CHARS;
#else
            UNREFERENCED_PARAMETER(fFailIfNoTranslation);
#endif
        }

        return AuxAppendW( pszAppendW,
                            cchAppendW,
                            cbOffset,
                            CodePage,
                            fFailIfNoTranslation,
                            dwFlags );
    }

    HRESULT
    AuxAppendW(
        __in_ecount(cchAppendW)
        PCWSTR          pszAppendW,
        __in DWORD      cchAppendW,
        __in DWORD      cbOffset,
        __in UINT       CodePage,
        __in BOOL       fFailIfNoTranslation,
        __in DWORD      dwFlags
    );

    HRESULT
    AuxAppendWTruncate(
        __in_ecount(cchAppendW)
        __in PCWSTR     pszAppendW,
        __in DWORD      cchAppendW,
        __in DWORD      cbOffset
    );

    static
    int
    ConvertUnicodeToCodePage(
        __in_ecount(dwStringLen)
        LPCWSTR                     pszSrcUnicodeString,
        __inout BUFFER_T<CHAR,1> *  pbufDstAnsiString,
        __in DWORD                  dwStringLen,
        __in UINT                   uCodePage
    );

    static
    HRESULT
    ConvertUnicodeToUTF8(
        __in_ecount(dwStringLen)
        LPCWSTR                     pszSrcUnicodeString,
        __in BUFFER_T<CHAR,1> *     pbufDstAnsiString,
        __in DWORD                  dwStringLen
    );

    typedef bool (* PFN_F_SHOULD_ESCAPE)(BYTE ch);

    //
    // Buffer with an inline buffer of 1,
    // enough to hold null-terminating character.
    //
    BUFFER_T<CHAR,1>    m_Buff;
    DWORD               m_cchLen;
};

template<DWORD size>
CHAR* InitHelper(__out CHAR (&psz)[size])
{
    psz[0] = '\0';
    return psz;
}

//
// Heap operation reduction macros
//
#define STACK_STRA(name, size)  CHAR __ach##name[size];\
                                STRA  name(InitHelper(__ach##name), sizeof(__ach##name))
