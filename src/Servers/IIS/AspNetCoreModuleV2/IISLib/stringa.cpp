// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.h"

STRA::STRA(
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
    _ASSERTE( nullptr != pbInit );
    _ASSERTE( cchInit > 0 );
    _ASSERTE( pbInit[0] == '\0' );
}

BOOL
STRA::IsEmpty() const
{
    return ( m_cchLen == 0 );
}

BOOL
STRA::Equals(
    __in PCSTR  pszRhs,
    __in BOOL   fIgnoreCase /*= FALSE*/
) const
{
    _ASSERTE( nullptr != pszRhs );

    if( fIgnoreCase )
    {
        return ( 0 == _stricmp( QueryStr(), pszRhs ) );
    }

    return ( 0 == strcmp( QueryStr(), pszRhs ) );
}

BOOL
STRA::Equals(
    __in const STRA* pstrRhs,
    __in BOOL           fIgnoreCase /*= FALSE*/
) const
{
    _ASSERTE(nullptr != pstrRhs);
    return Equals(pstrRhs->QueryStr(), fIgnoreCase);
}

DWORD
STRA::QueryCB() const
//
// Returns the number of bytes in the string excluding the terminating NULL
//
{
    return m_cchLen * sizeof( CHAR );
}

DWORD
STRA::QueryCCH() const
//
//  Returns the number of characters in the string excluding the terminating NULL
//
{
    return m_cchLen;
}

DWORD
STRA::QuerySizeCCH(
) const
//
// Returns size of the underlying storage buffer, in characters
//
{
    return m_Buff.QuerySize() / sizeof( CHAR );
}

DWORD
STRA::QuerySize(
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
STRA::QueryStr() const
//
//  Return the string buffer
//
{
    return m_Buff.QueryPtr();
}

VOID STRA::EnsureNullTerminated()
{
    // m_cchLen represents the string's length, not the underlying buffer length
    m_Buff.QueryPtr()[m_cchLen] = '\0';
}

VOID
STRA::Reset(
)
//
// Resets the internal string to be NULL string. Buffer remains cached.
//
{
    _ASSERTE( QueryStr() != nullptr );
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
)
//
// Recalculate the length of the string, etc. because we've modified
// the buffer directly.
//
{
    size_t size;
    HRESULT hr = StringCchLengthA(QueryStr(),
                                  QuerySizeCCH(),
                                  &size);
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
    size_t      cbLen;
    HRESULT hr = StringCbLengthA(pszCopy,
                                 STRSAFE_MAX_CCH,
                                 &cbLen);
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
    _ASSERTE( pstrRhs != nullptr );
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
    size_t      cchLen;
    HRESULT hr = StringCchLengthW(pszCopyW,
                                  STRSAFE_MAX_CCH,
                                  &cchLen);
    if ( FAILED( hr ) )
    {
        return hr;
    }
    return CopyW( pszCopyW, cchLen );
}

HRESULT
STRA::Append(
    __in PCSTR pszAppend
)
{
    size_t      cbLen;
    HRESULT hr = StringCbLengthA(pszAppend,
                                 STRSAFE_MAX_CCH,
                                 &cbLen);
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
    __in const STRA & strRhs
)
{
    return Append( strRhs.QueryStr(), strRhs.QueryCCH() );
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
    _ASSERTE( nullptr != pszBuffer );
    _ASSERTE( nullptr != pcb );

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

    //
    // Format the incoming message using vsnprintf()
    // so that the overflows are captured
    //
    int cchOutput = _vsnprintf_s(
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
        int cchNeeded = _vscprintf(pszFormatString, argsList);
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

HRESULT
STRA::CopyWToUTF8Unescaped(
    __in LPCWSTR cpchStr
)
{
    return STRA::CopyWToUTF8Unescaped(cpchStr, static_cast<DWORD>(wcslen(cpchStr)));
}

HRESULT
STRA::CopyWToUTF8Unescaped(
    __in_ecount(cch)
    LPCWSTR         cpchStr,
    __in DWORD      cch
)
{
    HRESULT hr = S_OK;

    if (cch == 0)
    {
        Reset();
        return S_OK;
    }

    int iRet = ConvertUnicodeToUTF8(cpchStr,
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
STRA::AuxAppend(
    __in_ecount(cbLen)
    LPCSTR          pStr,
    __in DWORD      cbLen,
    __in DWORD      cbOffset
)
{
    _ASSERTE( nullptr != pStr );
    _ASSERTE( cbOffset <= QueryCB() );

    ULONGLONG cb64NewSize = static_cast<ULONGLONG>(cbOffset) + cbLen + sizeof( CHAR );
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
    DWORD   cbRet       = 0;
    DWORD cbAvailable   = 0;
    UNREFERENCED_PARAMETER(fFailIfNoTranslation);

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
        nullptr,
        nullptr
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
        nullptr,
        0,
        nullptr,
        nullptr
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
        nullptr,
        nullptr
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
    EnsureNullTerminated();

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
    _ASSERTE( nullptr != pszAppendW );
    _ASSERTE( 0 == cbOffset || cbOffset == QueryCB() );

    if( !pszAppendW )
    {
        return HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
    }

    ULONGLONG cbNeeded = static_cast<ULONGLONG>(cbOffset) + cchAppendW + sizeof( CHAR );
    if( cbNeeded > MAXDWORD )
    {
        return HRESULT_FROM_WIN32( ERROR_ARITHMETIC_OVERFLOW );
    }

    if( !m_Buff.Resize( static_cast<SIZE_T>(cbNeeded) ) )
    {
        return E_OUTOFMEMORY;
    }

    //
    // Copy/convert the UNICODE string over (by making two bytes into one)
    //
    CHAR* pszBuffer = QueryStr() + cbOffset;
    for( DWORD i = 0; i < cchAppendW; i++ )
    {
        pszBuffer[i] = static_cast<CHAR>(pszAppendW[i]);
    }

    m_cchLen = cchAppendW + cbOffset;
    *( QueryStr() + m_cchLen ) = '\0';

    return S_OK;
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
    _ASSERTE(nullptr != pszSrcUnicodeString);
    _ASSERTE(nullptr != pbufDstAnsiString);

    DWORD dwFlags{0};

    if (uCodePage == CP_ACP)
    {
        dwFlags = WC_NO_BEST_FIT_CHARS;
    }
    else
    {
        dwFlags = 0;
    }

    int iStrLen = WideCharToMultiByte(uCodePage,
                                      dwFlags,
                                      pszSrcUnicodeString,
                                      dwStringLen,
                                      static_cast<LPSTR>(pbufDstAnsiString->QueryPtr()),
                                      static_cast<int>(pbufDstAnsiString->QuerySize()),
                                      nullptr,
                                      nullptr);
    if ((iStrLen == 0) && (GetLastError() == ERROR_INSUFFICIENT_BUFFER)) {
        iStrLen = WideCharToMultiByte(uCodePage,
                                      dwFlags,
                                      pszSrcUnicodeString,
                                      dwStringLen,
                                      nullptr,
                                      0,
                                      nullptr,
                                      nullptr);
        if (iStrLen != 0) {
            // add one just for the extra NULL
            BOOL bTemp = pbufDstAnsiString->Resize(iStrLen + 1);
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
                                              static_cast<LPSTR>(pbufDstAnsiString->QueryPtr()),
                                              static_cast<int>(pbufDstAnsiString->QuerySize()),
                                              nullptr,
                                              nullptr);
            }

        }
    }

    if (0 != iStrLen &&
        pbufDstAnsiString->Resize(iStrLen + 1))
    {
        // insert a terminating NULL into buffer for the dwStringLen+1 in the case that the dwStringLen+1 was not a NULL.
        static_cast<CHAR*>(pbufDstAnsiString->QueryPtr())[iStrLen] = '\0';
    }
    else
    {
        iStrLen = -1;
    }

    return iStrLen;
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
