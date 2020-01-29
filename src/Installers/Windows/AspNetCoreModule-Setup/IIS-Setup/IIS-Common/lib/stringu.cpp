// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

STRU::STRU(
    VOID
) : m_cchLen( 0 )
{
    *(QueryStr()) = L'\0';
}

STRU::STRU(
    __inout_ecount(cchInit) WCHAR* pbInit,
    __in DWORD cchInit
) : m_Buff( pbInit, cchInit * sizeof( WCHAR ) ),
    m_cchLen( 0 )
/*++
    Description:

        Used by STACK_STRU. Initially populates underlying buffer with pbInit.

        pbInit is not freed.

    Arguments:

        pbInit - initial memory to use
        cchInit - count, in characters, of pbInit

    Returns:

        None.

--*/
{
    _ASSERTE( cchInit <= (MAXDWORD / sizeof( WCHAR )) );
    _ASSERTE( NULL != pbInit );
    _ASSERTE(cchInit > 0 );
    _ASSERTE(pbInit[0] == L'\0');
}

BOOL
STRU::IsEmpty(
    VOID
) const
{
    return ( m_cchLen == 0 );
}

DWORD
STRU::QueryCB(
    VOID
) const
//
// Returns the number of bytes in the string excluding the terminating NULL
//
{
    return m_cchLen * sizeof( WCHAR );
}

DWORD
STRU::QueryCCH(
    VOID
) const
//
//  Returns the number of characters in the string excluding the terminating NULL
//
{
    return m_cchLen;
}

DWORD
STRU::QuerySizeCCH(
    VOID
) const
//
// Returns size of the underlying storage buffer, in characters
//
{
    return m_Buff.QuerySize() / sizeof( WCHAR );
}

__nullterminated
__ecount(this->m_cchLen)
WCHAR*
STRU::QueryStr(
    VOID
) const
//
//  Return the string buffer
//
{
    return m_Buff.QueryPtr();
}

VOID
STRU::Reset(
    VOID
)
//
// Resets the internal string to be NULL string. Buffer remains cached.
//
{
    _ASSERTE( QueryStr() != NULL );
    *(QueryStr()) = L'\0';
    m_cchLen = 0;
}

HRESULT
STRU::Resize(
    DWORD cchSize
)
{
    SIZE_T cbSize = cchSize * sizeof( WCHAR );
    if ( cbSize > MAXDWORD )
    {
        return HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
    }
    if( !m_Buff.Resize( cbSize ) )
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

HRESULT
STRU::SyncWithBuffer(
    VOID
)
//
// Recalculate the length of the string, etc. because we've modified
// the buffer directly.
//
{
    HRESULT hr;
    size_t size;
    hr = StringCchLengthW( QueryStr(),
                           QuerySizeCCH(),
                           &size );
    if ( SUCCEEDED( hr ) )
    {
        m_cchLen = static_cast<DWORD>(size);
    }
    return hr;
}

HRESULT
STRU::Copy(
    __in PCWSTR pszCopy
)
{
    HRESULT hr;
    size_t  cbStr;

    hr = StringCchLengthW( pszCopy,
                          STRSAFE_MAX_CCH,
                          &cbStr );
    if ( FAILED( hr ) )
    {
        return hr;
    }

    _ASSERTE( cbStr <= MAXDWORD );
    return Copy( pszCopy,
                 cbStr );
}

HRESULT
STRU::Copy(
    __in_ecount(cchLen)
    PCWSTR  pszCopy,
    SIZE_T  cchLen
)
//
// Copy the contents of another string to this one
//
{
    return AuxAppend( pszCopy,
                      cchLen * sizeof(WCHAR),
                      0);
}

HRESULT
STRU::Copy(
    __in const STRU * pstrRhs
)
{
    _ASSERTE( NULL != pstrRhs );
    return Copy( pstrRhs->QueryStr(), pstrRhs->QueryCCH() );
}

HRESULT
STRU::Copy(
    __in const STRU & str
)
{
    return Copy( str.QueryStr(), str.QueryCCH() );
}

HRESULT
STRU::CopyAndExpandEnvironmentStrings(
    __in PCWSTR     pszSource
)
{
    HRESULT hr = S_OK;
    DWORD   cchDestReqBuff = 0;

    Reset();

    cchDestReqBuff = ExpandEnvironmentStringsW( pszSource,
                                                QueryStr(),
                                                QuerySizeCCH() );
    if ( cchDestReqBuff == 0 )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto Finished;
    }
    else if ( cchDestReqBuff > QuerySizeCCH() )
    {
        hr = Resize( cchDestReqBuff );
        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        cchDestReqBuff = ExpandEnvironmentStringsW( pszSource,
                                                    QueryStr(),
                                                    QuerySizeCCH() );

        if ( cchDestReqBuff == 0 || cchDestReqBuff > QuerySizeCCH() )
        {
            _ASSERTE( FALSE );
            hr = HRESULT_FROM_WIN32( GetLastError() );
            goto Finished;
        }
    }

    hr = SyncWithBuffer();
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

Finished:

    return hr;

}

HRESULT
STRU::CopyA(
    __in PCSTR  pszCopyA
)
{
    HRESULT hr;
    size_t  cbStr;

    hr = StringCbLengthA( pszCopyA,
                          STRSAFE_MAX_CCH,
                          &cbStr );
    if ( FAILED( hr ) )
    {
        return hr;
    }

    _ASSERTE( cbStr <= MAXDWORD );
    return CopyA( pszCopyA,
                  cbStr );
}

HRESULT
STRU::CopyA(
    __in_bcount(cchLen)
    PCSTR   pszCopyA,
    SIZE_T  cchLen,
    UINT    CodePage /*= CP_UTF8*/
)
{
    return AuxAppendA(
        pszCopyA,
        cchLen,
        0,
        CodePage
    );
}

HRESULT
STRU::Append(
    __in PCWSTR  pszAppend
)
{
    HRESULT hr;
    size_t  cbStr;

    hr = StringCchLengthW( pszAppend,
                           STRSAFE_MAX_CCH,
                           &cbStr );
    if ( FAILED( hr ) )
    {
        return hr;
    }

    _ASSERTE( cbStr <= MAXDWORD );
    return Append( pszAppend,
                   cbStr );
}

HRESULT
STRU::Append(
    __in_ecount(cchLen)
    PCWSTR  pszAppend,
    SIZE_T  cchLen
)
//
// Append something to the end of the string
//
{
    if ( cchLen == 0 )
    {
        return S_OK;
    }
    return AuxAppend( pszAppend,
                      cchLen * sizeof(WCHAR),
                      QueryCB() );
}

HRESULT
STRU::Append(
    __in const STRU * pstrRhs
)
{
    _ASSERTE( NULL != pstrRhs );
    return Append( pstrRhs->QueryStr(), pstrRhs->QueryCCH() );
}

HRESULT
STRU::Append(
    __in const STRU & strRhs
)
{
    return Append( strRhs.QueryStr(), strRhs.QueryCCH() );
}

HRESULT
STRU::AppendA(
    __in PCSTR  pszAppendA
)
{
    HRESULT hr;
    size_t  cbStr;

    hr = StringCbLengthA( pszAppendA,
                          STRSAFE_MAX_CCH,
                          &cbStr );
    if ( FAILED( hr ) )
    {
        return hr;
    }

    _ASSERTE( cbStr <= MAXDWORD );
    return AppendA( pszAppendA,
                    cbStr );
}

HRESULT
STRU::AppendA(
    __in_bcount(cchLen)
    PCSTR   pszAppendA,
    SIZE_T  cchLen,
    UINT    CodePage /*= CP_UTF8*/
)
{
    if ( cchLen == 0 )
    {
        return S_OK;
    }
    return AuxAppendA(
        pszAppendA,
        cchLen,
        QueryCB(),
        CodePage
    );
}

HRESULT
STRU::CopyToBuffer(
    __out_bcount(*pcb) WCHAR*   pszBuffer,
    PDWORD                      pcb
) const
//
// Makes a copy of the stored string into the given buffer
//
{
    _ASSERTE( NULL != pszBuffer );
    _ASSERTE( NULL != pcb );

    HRESULT hr          = S_OK;
    DWORD   cbNeeded    = QueryCB() + sizeof( WCHAR );

    if( *pcb < cbNeeded )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER );
        goto Finished;
    }

    //
    // BUGBUG: StringCchCopy?
    //
    memcpy( pszBuffer, QueryStr(), cbNeeded );

Finished:

    *pcb = cbNeeded;

    return hr;
}

HRESULT
STRU::CopyToBufferA(
    __out_bcount(*pcb) CHAR* pszBuffer,
    __inout PDWORD           pcb
) const
{
    HRESULT hr = S_OK;
    STACK_STRA( straBuffer, 256 );
    hr = straBuffer.CopyW( QueryStr() );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }
    hr = straBuffer.CopyToBuffer( pszBuffer, pcb );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }
Finished:
    return hr;
}

HRESULT
STRU::SetLen(
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

    *( QueryStr() + cchLen ) = L'\0';
    m_cchLen = cchLen;

    return S_OK;
}

HRESULT
STRU::SafeSnwprintf(
    __in PCWSTR pwszFormatString,
    ...
)
/*++

Routine Description:

    Writes to a STRU, growing it as needed. It arbitrarily caps growth at 64k chars.

Arguments:

    pwszFormatString    - printf format
    ...                 - printf args

Return Value:

    HRESULT

--*/
{
    HRESULT     hr = S_OK;
    va_list     argsList;
    va_start(   argsList, pwszFormatString );

    hr = SafeVsnwprintf(pwszFormatString, argsList);

    va_end( argsList );
    return hr;
}

HRESULT
STRU::SafeVsnwprintf(
    __in PCWSTR pwszFormatString,
    va_list     argsList
)
/*++

Routine Description:

    Writes to a STRU, growing it as needed. It arbitrarily caps growth at 64k chars.

Arguments:

    pwszFormatString    - printf format
    argsList            - printf va_list

Return Value:

    HRESULT

--*/
{
    HRESULT     hr = S_OK;
    int         cchOutput;
    int         cchNeeded;

    //
    // Format the incoming message using vsnprintf()
    // so that the overflows are captured
    //
    cchOutput = _vsnwprintf_s(
            QueryStr(),
            QuerySizeCCH(),
            QuerySizeCCH() - 1,
            pwszFormatString,
            argsList
        );

    if( cchOutput == -1 )
    {
        //
        // Couldn't fit this in the original STRU size.
        //
        cchNeeded = _vscwprintf( pwszFormatString, argsList );
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

        cchOutput = _vsnwprintf_s(
            QueryStr(),
            QuerySizeCCH(),
            QuerySizeCCH() - 1,
            pwszFormatString,
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
    if ( FAILED( hr ) )
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

HRESULT
STRU::AuxAppend(
    __in_ecount(cNumStrings)
    PCWSTR const    rgpszStrings[],
    SIZE_T          cNumStrings
)
/*++

Routine Description:

    Appends an array of strings of length cNumStrings

Arguments:

    rgStrings   - The array of strings to be appened
    cNumStrings - The count of String

Return Value:

    HRESULT

--*/
{
    HRESULT         hr = S_OK;
    size_t          cbStringsTotal = sizeof( WCHAR ); // Account for null-terminator

    //
    //  Compute total size of the string.
    //  Resize internal buffer
    //  Copy each array element one by one to backing buffer
    //  Update backing buffer string length
    //
    for ( SIZE_T i = 0; i < cNumStrings; i++ )
    {
        _ASSERTE( rgpszStrings[ i ] != NULL );
        if ( NULL == rgpszStrings[ i ] )
        {
            return E_INVALIDARG;
        }

        size_t      cbString = 0;

        hr = StringCbLengthW( rgpszStrings[ i ],
                             STRSAFE_MAX_CCH * sizeof( WCHAR ),
                             &cbString );
        if ( FAILED( hr ) )
        {
            return hr;
        }

        cbStringsTotal += cbString;

        if ( cbStringsTotal > MAXDWORD )
        {
            return HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
        }
    }

    size_t cbBufSizeRequired = QueryCB() + cbStringsTotal;
    if ( cbBufSizeRequired > MAXDWORD )
    {
        return HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
    }

    if( m_Buff.QuerySize() < cbBufSizeRequired )
    {
        if( !m_Buff.Resize( cbBufSizeRequired ) )
        {
            return E_OUTOFMEMORY;
        }
    }

    STRSAFE_LPWSTR pszStringEnd = QueryStr() + QueryCCH();
    size_t cchRemaining = QuerySizeCCH() - QueryCCH();
    for ( SIZE_T i = 0; i < cNumStrings; i++ )
    {
        hr = StringCchCopyExW( pszStringEnd,        //  pszDest
                               cchRemaining,        //  cchDest
                               rgpszStrings[ i ],   //  pszSrc
                               &pszStringEnd,       //  ppszDestEnd
                               &cchRemaining,       //  pcchRemaining
                               0 );                 //  dwFlags
        if ( FAILED( hr ) )
        {
            _ASSERTE( FALSE );
            HRESULT hr2 = SyncWithBuffer();
            if ( FAILED( hr2 ) )
            {
                return hr2;
            }
            return hr;
        }
    }

    m_cchLen = static_cast< DWORD >( cbBufSizeRequired ) / sizeof( WCHAR ) - 1;

    return S_OK;
}

HRESULT
STRU::AuxAppend(
    __in_bcount(cbStr)
    const WCHAR*    pStr,
    SIZE_T          cbStr,
    DWORD           cbOffset
)
/*++

Routine Description:

    Appends to the string starting at the (byte) offset cbOffset.

Arguments:

    pStr     - A unicode string to be appended
    cbStr    - Length, in bytes, of pStr
    cbOffset - Offset, in bytes, at which to begin the append

Return Value:

    HRESULT

--*/
{
    _ASSERTE( NULL != pStr );
    _ASSERTE( 0 == cbStr % sizeof( WCHAR ) );
    _ASSERTE( cbOffset <= QueryCB() );
    _ASSERTE( 0 == cbOffset % sizeof( WCHAR ) );

    ULONGLONG cb64NewSize = (ULONGLONG)cbOffset + cbStr + sizeof( WCHAR );
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

    memcpy( reinterpret_cast<BYTE*>(m_Buff.QueryPtr()) + cbOffset, pStr, cbStr );

    m_cchLen = (static_cast<DWORD>(cbStr) + cbOffset) / sizeof(WCHAR);

    *( QueryStr() + m_cchLen ) = L'\0';

    return S_OK;
}

HRESULT
STRU::AuxAppendA(
    __in_bcount(cbStr)
    const CHAR*     pStr,
    SIZE_T          cbStr,
    DWORD           cbOffset,
    UINT            CodePage
)
/*++

Routine Description:

    Convert and append an ANSI string to the string starting at
    the (byte) offset cbOffset

Arguments:

    pStr     - An ANSI string to be appended
    cbStr    - Length, in bytes, of pStr
    cbOffset - Offset, in bytes, at which to begin the append
    CodePage - code page to use for conversion

Return Value:

    HRESULT

--*/
{
    WCHAR*  pszBuffer;
    DWORD   cchBuffer;
    DWORD   cchCharsCopied = 0;

    _ASSERTE( NULL != pStr );
    _ASSERTE( cbOffset <= QueryCB() );
    _ASSERTE( 0 == cbOffset % sizeof( WCHAR ) );

    if ( NULL == pStr )
    {
        return E_INVALIDARG;
    }

    if( 0 == cbStr )
    {
        return S_OK;
    }

    //
    //  Only resize when we have to.  When we do resize, we tack on
    //  some extra space to avoid extra reallocations.
    //
    if( m_Buff.QuerySize() < (ULONGLONG)cbOffset + (cbStr * sizeof( WCHAR )) + sizeof(WCHAR) )
    {
        ULONGLONG cb64NewSize = (ULONGLONG)( cbOffset + cbStr * sizeof(WCHAR) + sizeof( WCHAR ) );

        //
        // Check for the arithmetic overflow
        //
        if( cb64NewSize > MAXDWORD )
        {
            return HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
        }

        if( !m_Buff.Resize( static_cast<SIZE_T>(cb64NewSize) ) )
        {
            return E_OUTOFMEMORY;
        }
    }

    pszBuffer = reinterpret_cast<WCHAR*>(reinterpret_cast<BYTE*>(m_Buff.QueryPtr()) + cbOffset);
    cchBuffer = ( m_Buff.QuerySize() - cbOffset - sizeof( WCHAR ) ) / sizeof( WCHAR );

    cchCharsCopied = MultiByteToWideChar(
        CodePage,
        MB_ERR_INVALID_CHARS,
        pStr,
        static_cast<int>(cbStr),
        pszBuffer,
        cchBuffer
    );
    if( 0 == cchCharsCopied )
    {
        return HRESULT_FROM_WIN32( GetLastError() );
    }

    //
    // set the new length
    //
    m_cchLen = cchCharsCopied + cbOffset/sizeof(WCHAR);

    //
    // Must be less than, cause still need to add NULL
    //
    _ASSERTE( m_cchLen < QuerySizeCCH() );

    //
    // append NULL character
    //
    *(QueryStr() + m_cchLen) = L'\0';

    return S_OK;
}


/*++

Routine Description:

    Removes leading and trailing whitespace

--*/

VOID
STRU::Trim()
{
    PWSTR               pwszString              = QueryStr();
    DWORD               cchNewLength            = m_cchLen;
    DWORD               cchLeadingWhitespace    = 0;
    DWORD               cchTempLength           = 0;

    for (LONG ixString = m_cchLen - 1; ixString >= 0; ixString--)
    {
        if (iswspace(pwszString[ixString]) != 0)
        {
            pwszString[ixString] = L'\0';
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
        if (iswspace(pwszString[ixString]) != 0)
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
        memmove(pwszString, pwszString + cchLeadingWhitespace, cchNewLength * sizeof(WCHAR));
        pwszString[cchNewLength] = L'\0';
    }

    SyncWithBuffer();
}

/*++

Routine Description:

    Compares the string to the provided prefix to check for equality

Arguments:

    pwszPrefix  - wide char string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if prefix string matches with internal string, FALSE otherwise

--*/

BOOL
STRU::StartsWith(
    __in PCWSTR         pwszPrefix,
    __in bool           fIgnoreCase) const
{
    HRESULT hr          = S_OK;
    BOOL    fMatch      = FALSE;
    size_t  cchPrefix   = 0;

    if (pwszPrefix == NULL)
    {
        goto Finished;
    }

    hr = StringCchLengthW( pwszPrefix,
                           STRSAFE_MAX_CCH,
                           &cchPrefix );
    if (FAILED(hr))
    {
        goto Finished;
    }

    _ASSERTE( cchPrefix <= MAXLONG );

    if (cchPrefix > m_cchLen)
    {
        goto Finished;
    }

    #if defined( NTDDI_VERSION ) && NTDDI_VERSION >= NTDDI_LONGHORN

        fMatch = ( CSTR_EQUAL == CompareStringOrdinal( QueryStr(),
                                                       (int)cchPrefix,
                                                       pwszPrefix,
                                                       (int)cchPrefix,
                                                       fIgnoreCase ) );
    #else

        if( fIgnoreCase )
        {
            fMatch = ( 0 == _wcsnicmp( QueryStr(), pwszPrefix, cchPrefix ) );
        }
        else
        {
            fMatch = ( 0 == wcsncmp( QueryStr(), pwszPrefix, cchPrefix ) );
        }

    #endif

Finished:

    return fMatch;
}

/*++

Routine Description:

    Compares the string to the provided suffix to check for equality

Arguments:

    pwszSuffix  - wide char string to compare with
    fIgnoreCase - indicates whether the string comparison should be case-sensitive

Return Value:

    TRUE if suffix string matches with internal string, FALSE otherwise

--*/


BOOL
STRU::EndsWith(
    __in PCWSTR         pwszSuffix,
    __in bool           fIgnoreCase) const
{
    HRESULT     hr          = S_OK;
    PWSTR       pwszString  = QueryStr();
    BOOL        fMatch      = FALSE;
    size_t      cchSuffix   = 0;
    ptrdiff_t   ixOffset    = 0;

    if (pwszSuffix == NULL)
    {
        goto Finished;
    }

    hr = StringCchLengthW( pwszSuffix,
                           STRSAFE_MAX_CCH,
                           &cchSuffix );
    if (FAILED(hr))
    {
        goto Finished;
    }

    _ASSERTE( cchSuffix <= MAXLONG );

    if (cchSuffix > m_cchLen)
    {
        goto Finished;
    }

    ixOffset = m_cchLen - cchSuffix;
    _ASSERTE(ixOffset >= 0 && ixOffset <= MAXDWORD);

    #if defined( NTDDI_VERSION ) && NTDDI_VERSION >= NTDDI_LONGHORN

        fMatch = ( CSTR_EQUAL == CompareStringOrdinal( pwszString + ixOffset,
                                                       (int)cchSuffix,
                                                       pwszSuffix,
                                                       (int)cchSuffix,
                                                       fIgnoreCase ) );
    #else

        if( fIgnoreCase )
        {
            fMatch = ( 0 == _wcsnicmp( pwszString + ixOffset, pwszSuffix, cchSuffix ) );
        }
        else
        {
            fMatch = ( 0 == wcsncmp( pwszString + ixOffset, pwszSuffix, cchSuffix ) );
        }

    #endif

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
STRU::IndexOf(
    __in WCHAR          charValue,
    __in DWORD          dwStartIndex
    ) const
{
    INT nIndex = -1;

    // Make sure that there are no buffer overruns.
    if( dwStartIndex >= QueryCCH() )
    {
        goto Finished;
    }

    const WCHAR* pwChar = wcschr( QueryStr() + dwStartIndex, charValue );

    // Determine the index if found
    if( pwChar )
    {
        // nIndex will be set to -1 on failure.
        (VOID)SizeTToInt( pwChar - QueryStr(), &nIndex );
    }

Finished:

    return nIndex;
}


/*++

Routine Description:

    Searches the string for the first occurrence of the specified substring.

Arguments:

    pwszValue       - substring to find
    dwStartIndex    - initial index.

Return Value:

    The index for the first character occurrence in the string.

    -1 if not found.

--*/
INT
STRU::IndexOf(
    __in PCWSTR         pwszValue,
    __in DWORD          dwStartIndex
    ) const
{
    HRESULT hr = S_OK;
    INT nIndex = -1;
    SIZE_T cchValue = 0;

    // Validate input parameters
    if( dwStartIndex >= QueryCCH() || !pwszValue )
    {
        goto Finished;
    }

    const WCHAR* pwChar = wcsstr( QueryStr() + dwStartIndex, pwszValue );

    // Determine the index if found
    if( pwChar )
    {
        // nIndex will be set to -1 on failure.
        (VOID)SizeTToInt( pwChar - QueryStr(), &nIndex );
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
STRU::LastIndexOf(
    __in WCHAR          charValue,
    __in DWORD          dwStartIndex
    ) const
{
    INT nIndex = -1;

    // Make sure that there are no buffer overruns.
    if( dwStartIndex >= QueryCCH() )
    {
        goto Finished;
    }

    const WCHAR* pwChar = wcsrchr( QueryStr() + dwStartIndex, charValue );

    // Determine the index if found
    if( pwChar )
    {
        // nIndex will be set to -1 on failure.
        (VOID)SizeTToInt( pwChar - QueryStr(), &nIndex );
    }

Finished:

    return nIndex;
}

//static
HRESULT
STRU::ExpandEnvironmentVariables(
    __in  PCWSTR                  pszString,
    __out STRU *                  pstrExpandedString
    )
/*++

Routine Description:

    Expand the environment variables in a string

Arguments:

    pszString - String with environment variables to expand
    pstrExpandedString - Receives expanded string on success

Return Value:

    HRESULT

--*/
{
    HRESULT                 hr              = S_OK;
    DWORD                   cchNewSize      = 0;

    if ( pszString == NULL ||
         pstrExpandedString == NULL )
    {
        DBG_ASSERT( FALSE );
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Exit;
    }

    cchNewSize = ExpandEnvironmentStrings( pszString,
                                           pstrExpandedString->QueryStr(),
                                           pstrExpandedString->QuerySizeCCH() );
    if ( cchNewSize == 0 )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto Exit;
    }

    if ( cchNewSize > pstrExpandedString->QuerySizeCCH() )
    {
        hr = pstrExpandedString->Resize(
            ( cchNewSize + 1 ) * sizeof( WCHAR )
            );
        if ( FAILED( hr ) )
        {
            goto Exit;
        }

        cchNewSize = ExpandEnvironmentStrings(
            pszString,
            pstrExpandedString->QueryStr(),
            pstrExpandedString->QuerySizeCCH()
            );

        if ( cchNewSize == 0 ||
             cchNewSize > pstrExpandedString->QuerySizeCCH() )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            goto Exit;
        }
    }

    pstrExpandedString->SyncWithBuffer();

    hr = S_OK;

Exit:

    return hr;
}
