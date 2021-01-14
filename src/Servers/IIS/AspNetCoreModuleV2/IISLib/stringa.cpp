// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.h"

STRA::STRA(
    VOID
) : m_cchLen( 0 )
{
    *( QueryStr() ) = '\0';
}

STRA::STRA(
    __inout_ecount(cchInit) CHAR* pbInit,
    __in DWORD cchInit
) : m_Buff( pbInit, cchInit * sizeof( CHAR ) ),
    m_cchLen(0)
/*++
    Description:

        Used by STACK_STRA. Initially populates underlying buffer with pbInit.

        pbInit is not freed.

    Arguments:

        pbInit - initial memory to use
        cchInit - count, in characters, of pbInit

    Returns:

        None.

--*/
{
    _ASSERTE( NULL != pbInit );
    _ASSERTE( cchInit > 0 );
    _ASSERTE( pbInit[0] == '\0' );
}

BOOL
STRA::IsEmpty(
    VOID
) const
{
    return ( m_cchLen == 0 );
}

BOOL
STRA::Equals(
    __in PCSTR  pszRhs,
    __in BOOL   fIgnoreCase /*= FALSE*/
) const
{
    _ASSERTE( NULL != pszRhs );

    if( fIgnoreCase )
    {
        return ( 0 == _stricmp( QueryStr(), pszRhs ) );
    }

    return ( 0 == strcmp( QueryStr(), pszRhs ) );
}

BOOL
STRA::Equals(
    __in const STRA *   pstrRhs,
    __in BOOL           fIgnoreCase /*= FALSE*/
) const
{
    _ASSERTE( NULL != pstrRhs );
    return Equals( pstrRhs->QueryStr(), fIgnoreCase );
}

BOOL
STRA::Equals(
    __in const STRA &  strRhs,
    __in BOOL           fIgnoreCase /*= FALSE*/
) const
{
    return Equals( strRhs.QueryStr(), fIgnoreCase );
}

DWORD
STRA::QueryCB(
    VOID
) const
//
// Returns the number of bytes in the string excluding the terminating NULL
//
{
    return m_cchLen * sizeof( CHAR );
}

DWORD
STRA::QueryCCH(
    VOID
) const
//
//  Returns the number of characters in the string excluding the terminating NULL
//
{
    return m_cchLen;
}

DWORD
STRA::QuerySizeCCH(
    VOID
) const
//
// Returns size of the underlying storage buffer, in characters
//
{
    return m_Buff.QuerySize() / sizeof( CHAR );
}

DWORD
STRA::QuerySize(
    VOID
) const
//
//  Returns the size of the storage buffer in bytes
//
{
    return m_Buff.QuerySize();
}

__nullterminated
__bcount(this->m_cchLen)
CHAR *
STRA::QueryStr(
    VOID
) const
//
//  Return the string buffer
//
{
    return m_Buff.QueryPtr();
}

VOID
STRA::Reset(
    VOID
)
//
// Resets the internal string to be NULL string. Buffer remains cached.
//
{
    _ASSERTE( QueryStr() != NULL );
    *(QueryStr()) = '\0';
    m_cchLen = 0;
}

HRESULT
STRA::Resize(
    __in DWORD cchSize
)
{
    if( !m_Buff.Resize( cchSize * sizeof( CHAR ) ) )
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

HRESULT
STRA::SyncWithBuffer(
    VOID
)
//
// Recalculate the length of the string, etc. because we've modified
// the buffer directly.
//
{
    HRESULT hr;
    size_t size;
    hr = StringCchLengthA( QueryStr(),
                           QuerySizeCCH(),
                           &size );
    if ( SUCCEEDED( hr ) )
    {
        m_cchLen = static_cast<DWORD>(size);
    }
    return hr;
}

HRESULT
STRA::Copy(
    __in PCSTR   pszCopy
)
{
    HRESULT     hr;
    size_t      cbLen;
    hr = StringCbLengthA( pszCopy,
                          STRSAFE_MAX_CCH,
                          &cbLen );
    if ( FAILED( hr ) )
    {
        return hr;
    }
    return Copy( pszCopy, cbLen );
}


HRESULT
STRA::Copy(
    __in_ecount(cchLen)
    PCSTR           pszCopy,
    __in SIZE_T     cbLen
)
//
// Copy the contents of another string to this one
//
{
    _ASSERTE( cbLen <= MAXDWORD );

    return AuxAppend(
        pszCopy,
        static_cast<DWORD>(cbLen),
        0
    );
}

HRESULT
STRA::Copy(
    __in const STRA * pstrRhs
)
{
    _ASSERTE( pstrRhs != NULL );
    return Copy( pstrRhs->QueryStr(), pstrRhs->QueryCCH() );
}

HRESULT
STRA::Copy(
    __in const STRA & strRhs
)
{
    return Copy( strRhs.QueryStr(), strRhs.QueryCCH() );
}

HRESULT
STRA::CopyW(
    __in PCWSTR  pszCopyW
)
{
    HRESULT     hr;
    size_t      cchLen;
    hr = StringCchLengthW( pszCopyW,
                           STRSAFE_MAX_CCH,
                           &cchLen );
    if ( FAILED( hr ) )
    {
        return hr;
    }
    return CopyW( pszCopyW, cchLen );
}

HRESULT
STRA::CopyWTruncate(
    __in PCWSTR pszCopyWTruncate
)
{
    HRESULT     hr;
    size_t      cchLen;
    hr = StringCchLengthW( pszCopyWTruncate,
                           STRSAFE_MAX_CCH,
                           &cchLen );
    if ( FAILED( hr ) )
    {
        return hr;
    }
    return CopyWTruncate( pszCopyWTruncate, cchLen );
}

HRESULT
STRA::CopyWTruncate(
    __in_ecount(cchLen)
    PCWSTR          pszCopyWTruncate,
    __in SIZE_T     cchLen
)
//
// The "Truncate" methods do not do proper conversion. They do a (CHAR) caste
//
{
    _ASSERTE( cchLen <= MAXDWORD );

    return AuxAppendWTruncate(
        pszCopyWTruncate,
        static_cast<DWORD>(cchLen),
        0
    );
}

HRESULT
STRA::Append(
    __in PCSTR pszAppend
)
{
    HRESULT     hr;
    size_t      cbLen;
    hr = StringCbLengthA( pszAppend,
                          STRSAFE_MAX_CCH,
                          &cbLen );
    if ( FAILED( hr ) )
    {
        return hr;
    }
    return Append( pszAppend, cbLen );
}

HRESULT
STRA::Append(
    __in_ecount(cchLen)
    PCSTR       pszAppend,
    __in SIZE_T cbLen
)
{
    _ASSERTE( cbLen <= MAXDWORD );
    if ( cbLen == 0 )
    {
        return S_OK;
    }
    return AuxAppend(
        pszAppend,
        static_cast<DWORD>(cbLen),
        QueryCB()
    );
}

HRESULT
STRA::Append(
    __in const STRA * pstrRhs
)
{
    _ASSERTE( pstrRhs != NULL );
    return Append( pstrRhs->QueryStr(), pstrRhs->QueryCCH() );
}

HRESULT
STRA::Append(
    __in const STRA & strRhs
)
{
    return Append( strRhs.QueryStr(), strRhs.QueryCCH() );
}

HRESULT
STRA::AppendWTruncate(
    __in PCWSTR pszAppendWTruncate
)
{
    HRESULT     hr;
    size_t      cchLen;
    hr = StringCchLengthW( pszAppendWTruncate,
                           STRSAFE_MAX_CCH,
                           &cchLen );
    if ( FAILED( hr ) )
    {
        return hr;
    }
    return AppendWTruncate( pszAppendWTruncate, cchLen );
}

HRESULT
STRA::AppendWTruncate(
    __in_ecount(cchLen)
    PCWSTR          pszAppendWTruncate,
    __in SIZE_T     cchLen
)
//
// The "Truncate" methods do not do proper conversion. They do a (CHAR) caste
//
{
    _ASSERTE( cchLen <= MAXDWORD );
    if ( cchLen == 0 )
    {
        return S_OK;
    }
    return AuxAppendWTruncate(
        pszAppendWTruncate,
        static_cast<DWORD>(cchLen),
        QueryCB()
    );
}

HRESULT
STRA::CopyToBuffer(
    __out_bcount(*pcb) CHAR*    pszBuffer,
    __inout DWORD *             pcb
) const
//
// Makes a copy of the stored string into the given buffer
//
{
    _ASSERTE( NULL != pszBuffer );
    _ASSERTE( NULL != pcb );

    HRESULT hr          = S_OK;
    DWORD   cbNeeded    = QueryCB() + sizeof( CHAR );

    if( *pcb < cbNeeded )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER );
        goto Finished;
    }

    memcpy( pszBuffer, QueryStr(), cbNeeded );

Finished:

    *pcb = cbNeeded;

    return hr;
}

HRESULT
STRA::SetLen(
    __in DWORD cchLen
)
/*++
 *
Routine Description:

    Set the length of the string and null terminate, if there
    is sufficient buffer already allocated. Will not reallocate.

Arguments:

    cchLen - The number of characters in the new string.

Return Value:

    HRESULT

--*/
{
    if( cchLen >= QuerySizeCCH() )
    {
        return HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
    }

    *( QueryStr() + cchLen ) = '\0';
    m_cchLen = cchLen;

    return S_OK;
}


HRESULT
STRA::SafeSnprintf(
    __in __format_string
    PCSTR       pszFormatString,
    ...
)
/*++

Routine Description:

    Writes to a STRA, growing it as needed. It arbitrarily caps growth at 64k chars.

Arguments:

    pszFormatString    - printf format
    ...                - printf args

Return Value:

    HRESULT

--*/
{
    HRESULT     hr          = S_OK;
    va_list     argsList;
    va_start(   argsList, pszFormatString );

    hr = SafeVsnprintf(pszFormatString, argsList);

    va_end( argsList );
    return hr;
}

HRESULT
STRA::SafeVsnprintf(
    __in __format_string
    PCSTR       pszFormatString,
    va_list     argsList
)
/*++

Routine Description:

    Writes to a STRA, growing it as needed. It arbitrarily caps growth at 64k chars.

Arguments:

    pszFormatString    - printf format
    argsList           - printf va_list

Return Value:

    HRESULT

--*/
{
    HRESULT     hr          = S_OK;
    int         cchOutput;
    int         cchNeeded;

    //
    // Format the incoming message using vsnprintf()
    // so that the overflows are captured
    //
    cchOutput = _vsnprintf_s(
            QueryStr(),
            QuerySizeCCH(),
            QuerySizeCCH() - 1,
            pszFormatString,
            argsList
        );

    if( cchOutput == -1 )
    {
        //
        // Couldn't fit this in the original STRU size.
        //
        cchNeeded = _vscprintf( pszFormatString, argsList );
        if( cchNeeded > 64 * 1024 )
        {
            //
            // If we're trying to produce a string > 64k chars, then
            // there is probably a problem
            //
            hr = HRESULT_FROM_WIN32( ERROR_INVALID_DATA );
            goto Finished;
        }

        //
        // _vscprintf doesn't include terminating null character
        //
        cchNeeded++;

        hr = Resize( cchNeeded );
        if( FAILED( hr ) )
        {
            goto Finished;
        }

        cchOutput = _vsnprintf_s(
            QueryStr(),
            QuerySizeCCH(),
            QuerySizeCCH() - 1,
            pszFormatString,
            argsList
        );
        if( -1 == cchOutput )
        {
            //
            // This should never happen, cause we should already have correctly sized memory
            //
            _ASSERTE( FALSE );

            hr = HRESULT_FROM_WIN32( ERROR_INVALID_DATA );
            goto Finished;
        }
    }

    //
    // always null terminate at the last WCHAR
    //
    QueryStr()[ QuerySizeCCH() - 1 ] = L'\0';

    //
    // we directly touched the buffer - therefore:
    //
    hr = SyncWithBuffer();
    if( FAILED( hr ) )
    {
        goto Finished;
    }

Finished:

    if( FAILED( hr ) )
    {
        Reset();
    }

    return hr;
}

bool
FShouldEscapeUtf8(
    BYTE ch
    )
{
    if ( ( ch >= 128 ) )
    {
        return true;
    }

    return false;
}

bool
FShouldEscapeUrl(
    BYTE ch
    )
{
    if ( ( ch >= 128   ||
           ch <= 32    ||
           ch == '<'   ||
           ch == '>'   ||
           ch == '%'   ||
           ch == '?'   ||
           ch == '#' ) &&
         !( ch == '\n' || ch == '\r' ) )
    {
        return true;
    }

    return false;
}

HRESULT
STRA::Escape(
    VOID
)
/*++

Routine Description:

    Escapes a STRA

Arguments:

    None

Return Value:

    None

--*/
{
    return EscapeInternal( FShouldEscapeUrl );
}

HRESULT
STRA::EscapeUtf8(
    VOID
)
/*++

Routine Description:

    Escapes the high-bit chars in a STRA.  LWS, CR, LF & controls are untouched.

Arguments:

    None

Return Value:

    None

--*/
{
    return EscapeInternal( FShouldEscapeUtf8 );
}


HRESULT
STRA::EscapeInternal(
    PFN_F_SHOULD_ESCAPE pfnFShouldEscape
)
/*++

Routine Description:

    Escapes a STRA according to the predicate function passed in

Arguments:

    None

Return Value:

    None

--*/
{
    LPCSTR  pch     = QueryStr();
    __analysis_assume( pch != NULL );
    int     i      = 0;
    BYTE    ch;
    HRESULT hr      = S_OK;
    BOOL    fRet    = FALSE;
    SIZE_T  NewSize = 0;

    // Set to true if any % escaping occurs
    BOOL fEscapingDone = FALSE;

    //
    // If there are any characters that need to be escaped we copy the entire string
    // character by character into straTemp, escaping as we go, then at the end
    // copy all of straTemp over. Don't modify InlineBuffer directly.
    //
    CHAR InlineBuffer[512];
    InlineBuffer[0] = '\0';
    STRA straTemp(InlineBuffer, sizeof(InlineBuffer)/sizeof(*InlineBuffer));

    _ASSERTE( pch );

    while (ch = pch[i])
    {
        //
        //  Escape characters that are in the non-printable range
        //  but ignore CR and LF
        //

        if ( pfnFShouldEscape( ch ) )
        {
            if (FALSE == fEscapingDone)
            {
                // first character in the string that needed escaping
                fEscapingDone = TRUE;

                // guess that the size needs to be larger than
                // what we used to have times two
                NewSize = QueryCCH() * 2;
                if ( NewSize > MAXDWORD )
                {
                    hr = HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
                    return hr;
                }

                hr = straTemp.Resize( static_cast<DWORD>(NewSize) );

                if (FAILED(hr))
                {
                    return hr;
                }

                // Copy all of the previous buffer into buffTemp, only if it is not the first character:

                if ( i > 0)
                {
                    hr = straTemp.Copy(QueryStr(),
                                       i * sizeof(CHAR));
                    if (FAILED(hr))
                    {
                        return hr;
                    }
                }
            }

            // resize the temporary (if needed) with the slop of the entire buffer length
            // this fixes constant reallocation if the entire string needs to be escaped
            NewSize = QueryCCH() + 2 * sizeof(CHAR) + 1 * sizeof(CHAR);
            if ( NewSize > MAXDWORD )
            {
                hr = HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
                return hr;
            }

            fRet = straTemp.m_Buff.Resize( NewSize );
            if ( !fRet )
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                return hr;
            }

            //
            //  Create the string to append for the current character
            //

            CHAR chHex[3];
            chHex[0] = '%';

            //
            //  Convert the low then the high character to hex
            //

            UINT nLowDigit = (UINT)(ch % 16);
            chHex[2] = TODIGIT( nLowDigit );

            ch /= 16;

            UINT nHighDigit = (UINT)(ch % 16);

            chHex[1] = TODIGIT( nHighDigit );

            //
            // Actually append the converted character to the end of the temporary
            //
            hr = straTemp.Append(chHex, 3);
            if (FAILED(hr))
            {
                return hr;
            }
        }
        else
        {
            // if no escaping done, no need to copy
            if (fEscapingDone)
            {
                // if ANY escaping done, copy current character into new buffer
                straTemp.Append(&pch[i], 1);
            }
        }

        // inspect the next character in the string
        i++;
    }

    if (fEscapingDone)
    {
        // the escaped string is now in straTemp
        hr = Copy(straTemp);
    }

    return hr;

} // EscapeInternal()

VOID
STRA::Unescape(
    VOID
)
/*++

Routine Description:

    Unescapes a STRA

    Supported escape sequences are:
      %uxxxx unescapes Unicode character xxxx into system codepage
      %xx    unescapes character xx
      %      without following hex digits is ignored

Arguments:

    None

Return Value:

    None

--*/
{
    CHAR   *pScan;
    CHAR   *pDest;
    CHAR   *pNextScan;
    WCHAR   wch;
    DWORD   dwLen;
    BOOL    fChanged = FALSE;

    //
    // Now take care of any escape characters
    //
    pDest = pScan = strchr(QueryStr(), '%');

    while (pScan)
    {
        if ((pScan[1] == 'u' || pScan[1] == 'U') &&
            SAFEIsXDigit(pScan[2]) &&
            SAFEIsXDigit(pScan[3]) &&
            SAFEIsXDigit(pScan[4]) &&
            SAFEIsXDigit(pScan[5]))
        {
            wch = TOHEX(pScan[2]) * 4096 + TOHEX(pScan[3]) * 256
                + TOHEX(pScan[4]) * 16 + TOHEX(pScan[5]);

            dwLen = WideCharToMultiByte(CP_ACP,
                                        WC_NO_BEST_FIT_CHARS,
                                        &wch,
                                        1,
                                        (LPSTR) pDest,
                                        6,
                                        NULL,
                                        NULL);

            pDest += dwLen;
            pScan += 6;
            fChanged = TRUE;
        }
        else if (SAFEIsXDigit(pScan[1]) && SAFEIsXDigit(pScan[2]))
        {
            *pDest = TOHEX(pScan[1]) * 16 + TOHEX(pScan[2]);

            pDest ++;
            pScan += 3;
            fChanged = TRUE;
        }
        else   // Not an escaped char, just a '%'
        {
            if (fChanged)
            {
                *pDest = *pScan;
            }

            pDest++;
            pScan++;
        }

        //
        // Copy all the information between this and the next escaped char
        //
        pNextScan = strchr(pScan, '%');

        if (fChanged)   // pScan!=pDest, so we have to copy the char's
        {
            if (!pNextScan)   // That was the last '%' in the string
            {
                memmove(pDest,
                        pScan,
                        QueryCCH() - DIFF(pScan - QueryStr()) + 1);
            }
            else
            {
                // There is another '%', move intermediate chars
                if ((dwLen = (DWORD)DIFF(pNextScan - pScan)) != 0)
                {
                    memmove(pDest,
                            pScan,
                            dwLen);
                    pDest += dwLen;
                }
            }
        }

        pScan = pNextScan;
    }

    if (fChanged)
    {
        m_cchLen = (DWORD)strlen(QueryStr());  // for safety recalc the length
    }

    return;
}

HRESULT
STRA::CopyWToUTF8Unescaped(
    __in LPCWSTR cpchStr
)
{
    return STRA::CopyWToUTF8Unescaped(cpchStr, (DWORD) wcslen(cpchStr));
}

HRESULT
STRA::CopyWToUTF8Unescaped(
    __in_ecount(cch)
    LPCWSTR         cpchStr,
    __in DWORD      cch
)
{
    HRESULT hr = S_OK;
    int iRet;

    if (cch == 0)
    {
        Reset();
        return S_OK;
    }

    iRet = ConvertUnicodeToUTF8(cpchStr,
                                &m_Buff,
                                cch);
    if (-1 == iRet)
    {
        // could not convert
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    m_cchLen = iRet;

    _ASSERTE(strlen(m_Buff.QueryPtr()) == m_cchLen);
Finished:
    return hr;
}

HRESULT
STRA::CopyWToUTF8Escaped(
    __in LPCWSTR cpchStr
)
{
    return STRA::CopyWToUTF8Escaped(cpchStr, (DWORD) wcslen(cpchStr));
}

HRESULT
STRA::CopyWToUTF8Escaped(
    __in_ecount(cch)
    LPCWSTR         cpchStr,
    __in DWORD      cch
)
{
    HRESULT hr = S_OK;

    hr = CopyWToUTF8Unescaped(cpchStr, cch);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = Escape();
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = S_OK;
Finished:
    return hr;
}

HRESULT
STRA::AuxAppend(
    __in_ecount(cbLen)
    LPCSTR          pStr,
    __in DWORD      cbLen,
    __in DWORD      cbOffset
)
{
    _ASSERTE( NULL != pStr );
    _ASSERTE( cbOffset <= QueryCB() );

    ULONGLONG cb64NewSize = (ULONGLONG)cbOffset + cbLen + sizeof( CHAR );
    if( cb64NewSize > MAXDWORD )
    {
        return HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
    }

    if( m_Buff.QuerySize() < cb64NewSize )
    {
        if( !m_Buff.Resize( static_cast<SIZE_T>(cb64NewSize) ) )
        {
            return E_OUTOFMEMORY;
        }
    }

    memcpy( reinterpret_cast<BYTE*>(m_Buff.QueryPtr()) + cbOffset, pStr, cbLen );

    m_cchLen = cbLen + cbOffset;

    *( QueryStr() + m_cchLen ) = '\0';

    return S_OK;
}

HRESULT
STRA::AuxAppendW(
    __in_ecount(cchAppendW)
    PCWSTR          pszAppendW,
    __in DWORD      cchAppendW,
    __in DWORD      cbOffset,
    __in UINT       CodePage,
    __in BOOL       fFailIfNoTranslation,
    __in DWORD      dwFlags
)
{
    HRESULT hr          = S_OK;
    DWORD   cbAvailable = 0;
    DWORD   cbRet       = 0;

    //
    // There are only two expect places to append
    //
    _ASSERTE( 0 == cbOffset || QueryCB() == cbOffset );

    if ( cchAppendW == 0 )
    {
        goto Finished;
    }

    //
    // start by assuming 1 char to 1 char will be enough space
    //
    if( !m_Buff.Resize( cbOffset + cchAppendW + sizeof( CHAR ) ) )
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    cbAvailable = m_Buff.QuerySize() - cbOffset;

    cbRet = WideCharToMultiByte(
        CodePage,
        dwFlags,
        pszAppendW,
        cchAppendW,
        QueryStr() + cbOffset,
        cbAvailable,
        NULL,
        NULL
    );
    if( 0 != cbRet )
    {
        if(!m_Buff.Resize(cbOffset + cbRet + 1))
        {
            hr = E_OUTOFMEMORY;
        }

        //
        // not zero --> success, so we're done
        //
        goto Finished;
    }

    //
    // We only know how to handle ERROR_INSUFFICIENT_BUFFER
    //
    hr = HRESULT_FROM_WIN32( GetLastError() );
    if( hr != HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER ) )
    {
        goto Finished;
    }

    //
    // Reset HResult because we need to get the number of bytes needed
    //
    hr = S_OK;
    cbRet = WideCharToMultiByte(
        CodePage,
        dwFlags,
        pszAppendW,
        cchAppendW,
        NULL,
        0,
        NULL,
        NULL
    );
    if( 0 == cbRet )
    {
        //
        // no idea how we could ever reach here
        //
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto Finished;
    }

    if( !m_Buff.Resize( cbOffset + cbRet + 1) )
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    cbAvailable = m_Buff.QuerySize() - cbOffset;

    cbRet = WideCharToMultiByte(
        CodePage,
        dwFlags,
        pszAppendW,
        cchAppendW,
        QueryStr() + cbOffset,
        cbAvailable,
        NULL,
        NULL
    );
    if( 0 == cbRet )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto Finished;
    }

Finished:

    if( SUCCEEDED( hr ) && 0 != cbRet )
    {
        m_cchLen = cbRet + cbOffset;
    }

    //
    // ensure we're still NULL terminated in the right spot
    // (regardless of success or failure)
    //
    QueryStr()[m_cchLen] = '\0';

    return hr;
}

HRESULT
STRA::AuxAppendWTruncate(
    __in_ecount(cchAppendW)
    __in PCWSTR     pszAppendW,
    __in DWORD      cchAppendW,
    __in DWORD      cbOffset
)
//
// Cheesey WCHAR --> CHAR conversion
//
{
    HRESULT hr = S_OK;
    CHAR*   pszBuffer;

    _ASSERTE( NULL != pszAppendW );
    _ASSERTE( 0 == cbOffset || cbOffset == QueryCB() );

    if( !pszAppendW )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Finished;
    }

    ULONGLONG cbNeeded = (ULONGLONG)cbOffset + cchAppendW + sizeof( CHAR );
    if( cbNeeded > MAXDWORD )
    {
        hr = HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
        goto Finished;
    }

    if( !m_Buff.Resize( static_cast<SIZE_T>(cbNeeded) ) )
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    //
    // Copy/convert the UNICODE string over (by making two bytes into one)
    //
    pszBuffer = QueryStr() + cbOffset;
    for( DWORD i = 0; i < cchAppendW; i++ )
    {
        pszBuffer[i] = static_cast<CHAR>(pszAppendW[i]);
    }

    m_cchLen = cchAppendW + cbOffset;
    *( QueryStr() + m_cchLen ) = '\0';

Finished:

    return hr;
}

// static
int
STRA::ConvertUnicodeToCodePage(
    __in_ecount(dwStringLen)
    LPCWSTR                     pszSrcUnicodeString,
    __inout BUFFER_T<CHAR,1> *  pbufDstAnsiString,
    __in DWORD                  dwStringLen,
    __in UINT                   uCodePage
)
{
    _ASSERTE(NULL != pszSrcUnicodeString);
    _ASSERTE(NULL != pbufDstAnsiString);

    BOOL bTemp;
    int iStrLen = 0;
    DWORD dwFlags;

    if (uCodePage == CP_ACP)
    {
        dwFlags = WC_NO_BEST_FIT_CHARS;
    }
    else
    {
        dwFlags = 0;
    }

    iStrLen = WideCharToMultiByte(uCodePage,
                                  dwFlags,
                                  pszSrcUnicodeString,
                                  dwStringLen,
                                  (LPSTR)pbufDstAnsiString->QueryPtr(),
                                  (int)pbufDstAnsiString->QuerySize(),
                                  NULL,
                                  NULL);
    if ((iStrLen == 0) && (GetLastError() == ERROR_INSUFFICIENT_BUFFER)) {
        iStrLen = WideCharToMultiByte(uCodePage,
                                      dwFlags,
                                      pszSrcUnicodeString,
                                      dwStringLen,
                                      NULL,
                                      0,
                                      NULL,
                                      NULL);
        if (iStrLen != 0) {
            // add one just for the extra NULL
            bTemp = pbufDstAnsiString->Resize(iStrLen + 1);
            if (!bTemp)
            {
                iStrLen = 0;
            }
            else
            {
                iStrLen = WideCharToMultiByte(uCodePage,
                                              dwFlags,
                                              pszSrcUnicodeString,
                                              dwStringLen,
                                              (LPSTR)pbufDstAnsiString->QueryPtr(),
                                              (int)pbufDstAnsiString->QuerySize(),
                                              NULL,
                                              NULL);
            }

        }
    }

    if (0 != iStrLen &&
        pbufDstAnsiString->Resize(iStrLen + 1))
    {
        // insert a terminating NULL into buffer for the dwStringLen+1 in the case that the dwStringLen+1 was not a NULL.
        ((CHAR*)pbufDstAnsiString->QueryPtr())[iStrLen] = '\0';
    }
    else
    {
        iStrLen = -1;
    }

    return iStrLen;
}

// static
HRESULT
STRA::ConvertUnicodeToMultiByte(
    __in_ecount(dwStringLen)
    LPCWSTR                     pszSrcUnicodeString,
    __in BUFFER_T<CHAR,1> *     pbufDstAnsiString,
    __in DWORD                  dwStringLen
)
{
    return ConvertUnicodeToCodePage( pszSrcUnicodeString,
                                      pbufDstAnsiString,
                                      dwStringLen,
                                      CP_ACP );
}

// static
HRESULT
STRA::ConvertUnicodeToUTF8(
    __in_ecount(dwStringLen)
    LPCWSTR                     pszSrcUnicodeString,
    __in BUFFER_T<CHAR,1> *     pbufDstAnsiString,
    __in DWORD                  dwStringLen
)
{
    return ConvertUnicodeToCodePage( pszSrcUnicodeString,
                                      pbufDstAnsiString,
                                      dwStringLen,
                                      CP_UTF8 );
}

/*++

Routine Description:

    Removes leading and trailing whitespace

--*/

VOID
STRA::Trim()
{
    PSTR    pszString               = QueryStr();
    DWORD   cchNewLength            = m_cchLen;
    DWORD   cchLeadingWhitespace    = 0;
    DWORD   cchTempLength           = 0;

    for (LONG ixString = m_cchLen - 1; ixString >= 0; ixString--)
    {
        if (isspace((unsigned char) pszString[ixString]) != 0)
        {
            pszString[ixString] = '\0';
            cchNewLength--;
        }
        else
        {
            break;
        }
    }

    cchTempLength = cchNewLength;
    for (DWORD ixString = 0; ixString < cchTempLength; ixString++)
    {
        if (isspace((unsigned char) pszString[ixString]) != 0)
        {
            cchLeadingWhitespace++;
            cchNewLength--;
        }
        else
        {
            break;
        }
    }

    if (cchNewLength == 0)
    {

        Reset();
    }
    else if (cchLeadingWhitespace > 0)
    {
        memmove(pszString, pszString + cchLeadingWhitespace, cchNewLength * sizeof(CHAR));
        pszString[cchNewLength] = '\0';
    }

    SyncWithBuffer();
}

/*++

Routine Description:

    Compares the string to the provided prefix to check for equality

Arguments:

    pStraPrefix - string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if prefix string matches with internal string, FALSE otherwise

--*/
BOOL
STRA::StartsWith(
    __in const STRA *   pStraPrefix,
    __in bool           fIgnoreCase) const
{
    _ASSERTE( pStraPrefix != NULL );
    return StartsWith(pStraPrefix->QueryStr(), fIgnoreCase);
}

/*++

Routine Description:

    Compares the string to the provided prefix to check for equality

Arguments:

    straPrefix  - string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if prefix string matches with internal string, FALSE otherwise

--*/
BOOL
STRA::StartsWith(
    __in const STRA &   straPrefix,
    __in bool           fIgnoreCase) const
{
    return StartsWith(straPrefix.QueryStr(), fIgnoreCase);
}

/*++

Routine Description:

    Compares the string to the provided prefix to check for equality

Arguments:

    pszPrefix   - string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if prefix string matches with internal string, FALSE otherwise

--*/
BOOL
STRA::StartsWith(
    __in PCSTR          pszPrefix,
    __in bool           fIgnoreCase) const
{
    HRESULT hr          = S_OK;
    BOOL    fMatch      = FALSE;
    size_t  cchPrefix   = 0;

    if (pszPrefix == NULL)
    {
        goto Finished;
    }

    hr = StringCchLengthA( pszPrefix,
                           STRSAFE_MAX_CCH,
                           &cchPrefix );
    if (FAILED(hr))
    {
        goto Finished;
    }

    _ASSERTE( cchPrefix <= MAXDWORD );

    if (cchPrefix > m_cchLen)
    {
        goto Finished;
    }

    if( fIgnoreCase )
    {
        fMatch = ( 0 == _strnicmp( QueryStr(), pszPrefix, cchPrefix ) );
    }
    else
    {
        fMatch = ( 0 == strncmp( QueryStr(), pszPrefix, cchPrefix ) );
    }


Finished:

    return fMatch;
}

/*++

Routine Description:

    Compares the string to the provided suffix to check for equality

Arguments:

    pStraSuffix - string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if suffix string matches with internal string, FALSE otherwise

--*/
BOOL
STRA::EndsWith(
    __in const STRA *   pStraSuffix,
    __in bool           fIgnoreCase) const
{
    _ASSERTE( pStraSuffix != NULL );
    return EndsWith(pStraSuffix->QueryStr(), fIgnoreCase);
}


/*++

Routine Description:

    Compares the string to the provided suffix to check for equality

Arguments:

    straSuffix  - string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if suffix string matches with internal string, FALSE otherwise

--*/
BOOL
STRA::EndsWith(
    __in const STRA &   straSuffix,
    __in bool           fIgnoreCase) const
{
    return EndsWith(straSuffix.QueryStr(), fIgnoreCase);
}


/*++

Routine Description:

    Compares the string to the provided suffix to check for equality

Arguments:

    pszSuffix   - string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if suffix string matches with internal string, FALSE otherwise

--*/
BOOL
STRA::EndsWith(
    __in PCSTR          pszSuffix,
    __in bool           fIgnoreCase) const
{
    HRESULT   hr          = S_OK;
    PSTR      pszString   = QueryStr();
    BOOL      fMatch      = FALSE;
    size_t    cchSuffix   = 0;
    ptrdiff_t ixOffset    = 0;

    if (pszSuffix == NULL)
    {
        goto Finished;
    }

    hr = StringCchLengthA( pszSuffix,
                           STRSAFE_MAX_CCH,
                           &cchSuffix );
    if (FAILED(hr))
    {
        goto Finished;
    }

    _ASSERTE( cchSuffix <= MAXDWORD );

    if (cchSuffix > m_cchLen)
    {
        goto Finished;
    }

    ixOffset = m_cchLen - cchSuffix;
    _ASSERTE(ixOffset >= 0 && ixOffset <= MAXDWORD);

    if( fIgnoreCase )
    {
        fMatch = ( 0 == _strnicmp( pszString + ixOffset, pszSuffix, cchSuffix ) );
    }
    else
    {
        fMatch = ( 0 == strncmp( pszString + ixOffset, pszSuffix, cchSuffix ) );
    }

Finished:

    return fMatch;
}


/*++

Routine Description:

    Searches the string for the first occurrence of the specified character.

Arguments:

    charValue       - character to find
    dwStartIndex    - the initial index.

Return Value:

    The index for the first character occurrence in the string.

    -1 if not found.

--*/
INT
STRA::IndexOf(
    __in CHAR           charValue,
    __in DWORD          dwStartIndex
    ) const
{
    INT nIndex = -1;

    // Make sure that there are no buffer overruns.
    if( dwStartIndex >= QueryCCH() )
    {
        goto Finished;
    }

    const CHAR* pChar = strchr( QueryStr() + dwStartIndex, charValue );

    // Determine the index if found
    if( pChar )
    {
        // nIndex will be set to -1 on failure.
        (VOID)SizeTToInt( pChar - QueryStr(), &nIndex );
    }

Finished:

    return nIndex;
}


/*++

Routine Description:

    Searches the string for the first occurrence of the specified substring.

Arguments:

    pszValue        - substring to find
    dwStartIndex    - initial index.

Return Value:

    The index for the first character occurrence in the string.

    -1 if not found.

--*/
INT
STRA::IndexOf(
    __in PCSTR          pszValue,
    __in DWORD          dwStartIndex
    ) const
{
    HRESULT hr = S_OK;
    INT nIndex = -1;
    SIZE_T cchValue = 0;

    // Validate input parameters
    if( dwStartIndex >= QueryCCH() || !pszValue )
    {
        goto Finished;
    }

    const CHAR* pChar = strstr( QueryStr() + dwStartIndex, pszValue );

    // Determine the index if found
    if( pChar )
    {
        // nIndex will be set to -1 on failure.
        (VOID)SizeTToInt( pChar - QueryStr(), &nIndex );
    }

Finished:

    return nIndex;
}


/*++

Routine Description:

    Searches the string for the last occurrence of the specified character.

Arguments:

    charValue       - character to find
    dwStartIndex    - initial index.

Return Value:

    The index for the last character occurrence in the string.

    -1 if not found.

--*/
INT
STRA::LastIndexOf(
    __in CHAR           charValue,
    __in DWORD          dwStartIndex
    ) const
{
    INT nIndex = -1;

    // Make sure that there are no buffer overruns.
    if( dwStartIndex >= QueryCCH() )
    {
        goto Finished;
    }

    const CHAR* pChar = strrchr( QueryStr() + dwStartIndex, charValue );

    // Determine the index if found
    if( pChar )
    {
        // nIndex will be set to -1 on failure.
        (VOID)SizeTToInt( pChar - QueryStr(), &nIndex );
    }

Finished:

    return nIndex;
}
