// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _STBUFF_H
#define _STBUFF_H

#include <stdio.h>
#include <windows.h>

#define STB_INLINE_SIZE  64
#define STB_MAX_ALLOC    16*1024

#define TO_UPPER(ch) (isupper(ch) ? ch : toupper(ch))

class STBUFF
{
public:

    STBUFF(
        BOOL    fZeroInit = TRUE
        ) : _pData( _Inline ),
            _cbData( 0 ),
            _cbBuffer( STB_INLINE_SIZE ),
            _cbMaxAlloc( STB_MAX_ALLOC )
    {
        if ( fZeroInit )
        {
            ZeroInit();
        }

        _Inline[STB_INLINE_SIZE] = 0;
        _Inline[STB_INLINE_SIZE+1] = 0;
    }

    virtual
    ~STBUFF()
    {
        Reset( TRUE );
    }

    VOID
    Reset(
        BOOL    fFreeAlloc = FALSE
        )
    {
        //
        // If we are supposed to free any heap
        // allocations, do so now.
        //

        if ( fFreeAlloc &&
             _pData != _Inline )
        {
            LocalFree( _pData );
            _pData = _Inline;
            _cbBuffer = STB_INLINE_SIZE;
        }

        //
        // Reset the data size
        //

        _cbData = 0;
    }

    HRESULT
    Resize(
        DWORD   cbSize
        )
    {
        HRESULT hr = S_OK;
        BYTE  * pTemp;
        
        //
        // If the buffer is large enough, just return
        //

        if ( cbSize < _cbBuffer )
        {
            goto Finished;
        }

        //
        // If the requested size exceeds our maximum
        // allocation, then fail this call.
        //

        if ( cbSize > _cbMaxAlloc )
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        //
        // Adjust the allocation size so that we allocate
        // in chunks of MINITOOLS_INLINE_SIZE.  Don't
        // exeed _cbMaxAlloc.
        //

        if ( cbSize % STB_INLINE_SIZE )
        {
            cbSize = ( cbSize / STB_INLINE_SIZE + 1 ) *
                       STB_INLINE_SIZE;
        }

        if ( cbSize > _cbMaxAlloc )
        {
            cbSize = _cbMaxAlloc;
        }

        //
        // Allocate the new storage and copy any existing
        // data into it.
        //
        // Allocate two extra bytes so that we can guarantee
        // NULL termination when someone queries the data
        // pointer as a string.
        //
        
        pTemp = (BYTE*)LocalAlloc( LPTR, cbSize + 2 );

        if ( !pTemp )
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        if ( _cbData )
        {
            CopyMemory( pTemp, _pData, _cbData );
        }

        //
        // If the original buffer is not the inline
        // storage, then free it.
        //

        if ( _pData != _Inline )
        {
            LocalFree( _pData );
        }

        //
        // Save the new storage pointer
        //

        _pData = pTemp;
        _cbBuffer = cbSize;

        //
        // Set the extra two bytes as 0
        //

        _pData[_cbBuffer] = 0;
        _pData[_cbBuffer+1] = 0;

Finished:
        return hr;
    }

    HRESULT
    AppendData(
        VOID *  pData,
        DWORD   cbData,
        DWORD   Offset = 0xffffffff
        )
    {
        DWORD   cbNeeded;
        HRESULT hr = S_OK;

        //
        // Resize the buffer if necessary
        //

        cbNeeded = Offset + cbData;

        hr = Resize( cbNeeded );

        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        //
        // Copy the new data
        //

        if ( cbData )
        {
            CopyMemory( _pData + Offset, pData, cbData );
        }

        _cbData = cbNeeded;

        //
        // NULL terminate the data
        //

        GuaranteeNullTermination();

Finished:

        return hr;
    }


    HRESULT
    AppendData(
        LPCSTR szData,
        DWORD  cchData = 0xffffffff,
        DWORD  Offset = 0xffffffff
        )
    {
        //
        // If cchData is 0xffffffff, then calculate size
        //

        if ( cchData == 0xffffffff )
        {
            cchData = (DWORD)strlen( szData );
        }

        //
        // If offset is 0xffffffff, then assume
        // that we are appending to the end of the
        // string.
        //

        if ( Offset == 0xffffffff )
        {
            Offset = _cbData;
        }

        return AppendData( (VOID*)szData,
                           cchData,
                           Offset );
    }

    HRESULT
    AppendData(
        LPCWSTR szData,
        DWORD   cchData = 0xffffffff,
        DWORD   cchOffset = 0xffffffff
        )
    {
        DWORD cbData;
        DWORD cbOffset;
        //
        // If cchData is 0xffffffff, then calculate size
        //

        if ( cchData == 0xffffffff )
        {
            cchData = (DWORD)wcslen( szData );
        }

        cbData = cchData * sizeof(WCHAR);

        //
        // If offset is 0xffffffff, then assume
        // that we are appending to the end of the
        // string.
        //

        if ( cchOffset == 0xffffffff )
        {
            cchOffset = _cbData;
        }

        cbOffset = cchOffset * sizeof(WCHAR);

        return AppendData( (VOID*)szData,
                           cbData,
                           cbOffset );
    }

    HRESULT
    AppendData(
        STBUFF    *pbuff
        )
    {
        return AppendData( pbuff->QueryPtr(),
                           pbuff->QueryDataSize() );
    }

    HRESULT
    SetData(
        VOID * pData,
        DWORD  cbData
        )
    {
        //
        // Set data is just an append to offset zero
        //

        return AppendData( pData,
                           cbData,
                           0 );
    }

    HRESULT
    SetData(
        LPCSTR pData,
        DWORD  cchData = 0xffffffff
        )
    {
        //
        // If cbData is 0xffffffff, then assume that
        // pData is a NULL terminated string.
        //

        if ( cchData == 0xffffffff )
        {
            cchData = (DWORD)strlen( (LPSTR)pData );
        }

        return SetData( (VOID*)pData, cchData );
    }

    HRESULT
    SetData(
        LPCWSTR pData,
        DWORD   cchData = 0xffffffff
        )
    {
        //
        // If cbData is 0xffffffff, then assume that
        // pData is a NULL terminated string.
        //

        if ( cchData == 0xffffffff )
        {
            cchData = (DWORD)wcslen( (LPWSTR)pData );
        }

        return SetData( (VOID*)pData, cchData * sizeof(WCHAR) );
    }


    HRESULT
    SetData(
        STBUFF    *pbuff
        )
    {
        return AppendData( pbuff->QueryPtr(),
                           pbuff->QueryDataSize(),
                           0 );
    }

    HRESULT
    AnsiToUnicode(
        LPCSTR   szString,
        UINT     codepage = CP_UTF8
        )
    {
        DWORD   cchString = (DWORD)strlen( szString );
        HRESULT hr = S_OK;

        hr = Resize( cchString * sizeof(WCHAR) );

        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        if ( !MultiByteToWideChar( codepage,
                                   MB_ERR_INVALID_CHARS,
                                   szString,
                                   cchString,
                                   (LPWSTR)_pData,
                                   cchString ) )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            goto Finished;
        }

        _cbData = cchString * sizeof(WCHAR);

Finished:

        return hr;
    }

    HRESULT
    UnicodeToAnsi(
        LPCWSTR  szStringW,
        UINT     codepage = CP_UTF8
        )
    {
        DWORD   cchString = (DWORD)wcslen( szStringW );
        HRESULT hr = S_OK;

        hr = Resize( cchString );

        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        if ( !WideCharToMultiByte( codepage,
                                   0,
                                   szStringW,
                                   cchString,
                                   (LPSTR)_pData,
                                   cchString,
                                   NULL,
                                   NULL ) )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            goto Finished;
        }

        _cbData = cchString;

Finished:

        return hr;
    }

    HRESULT
    ExpandEnvironmentStrings(
        VOID
        )
    {
        STBUFF    Temp;
        DWORD     cbNeeded;
        HRESULT   hr = S_OK;

        cbNeeded = ::ExpandEnvironmentStringsA( QueryStr(),
                                                Temp.QueryStr(),
                                                Temp.QueryBufferSize() );

        if ( cbNeeded > Temp.QueryBufferSize() )
        {
            hr = Temp.Resize ( cbNeeded );
            if ( FAILED (hr) )
            {
                goto Finished;
            }

            cbNeeded = ::ExpandEnvironmentStringsA( QueryStr(),
                                                    Temp.QueryStr(),
                                                    Temp.QueryBufferSize() );

        }

        Temp.CalcDataSize();

        hr = SetData( &Temp );
        if ( FAILED (hr) )
        {
            goto Finished;
        }

    Finished:

        return hr;
    }

    HRESULT
    Vsprintf(
        LPCSTR  szFormat,
        va_list args
        )
    {
        DWORD   cchWritten;
        HRESULT hr = S_OK;

        DWORD   cbNeeded = _vscprintf( szFormat, args );

        hr = Resize( cbNeeded + 1 );

        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        cchWritten = _vsnprintf_s( QueryStr(),
                                   QueryBufferSize(),
                                   QueryBufferSize(),
                                   szFormat,
                                   args );

        _cbData = cchWritten;

Finished:

        return hr;
    }

    HRESULT
    Vsprintf(
        LPCWSTR  szFormat,
        va_list  args
        )
    {
        DWORD   cchWritten;
        HRESULT hr = S_OK;

        DWORD   cbNeeded = _vscwprintf( szFormat, args ) * sizeof(WCHAR);

        hr = Resize( cbNeeded + 1 );

        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        cchWritten = _vsnwprintf_s( QueryStrW(),
                                    QueryBufferSize() / sizeof(WCHAR),
                                    _TRUNCATE,
                                    szFormat,
                                    args );

        _cbData = cchWritten * sizeof(WCHAR);

Finished:

        return hr;
    }

    HRESULT
    Printf(
        LPCSTR   szFormat,
        ...
        )
    {
        HRESULT hr;

        //
        // Let Vsprintf do the work
        //

        va_list args;

        va_start( args, szFormat );

        hr = Vsprintf( szFormat,
                       args );

        va_end( args );

        return hr;
    }

    HRESULT
    Printf(
        LPCWSTR   szFormat,
        ...
        )
    {
        HRESULT hr;

        //
        // Let Vsprintf do the work
        //

        va_list args;

        va_start( args, szFormat );

        hr = Vsprintf( szFormat,
                       args );

        va_end( args );

        return hr;
    }

    VOID *
    QueryPtr()
    {
        return (VOID*)_pData;
    }

    LPSTR
    QueryStr()
    {
        GuaranteeNullTermination();

        return (LPSTR)_pData;
    }

    LPWSTR
    QueryStrW()
    {
        GuaranteeNullTermination();

        return (LPWSTR)_pData;
    }

    DWORD
    QueryDataSize()
    {
        return _cbData;
    }

    HRESULT
    SetDataSize(
        DWORD   cbData
        )
    {
        HRESULT hr = S_OK;

        if ( cbData > _cbBuffer )
        {
            hr = HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER );
            goto Finished;
        }

        _cbData = cbData;

    Finished:
        return hr;
    }

    VOID
    CalcDataSize()
    {
        _cbData = (DWORD)strlen( (LPSTR)_pData );
    }

    VOID
    CalcDataSizeW()
    {
        _cbData = (DWORD)wcslen( (LPWSTR)_pData );
    }

    DWORD
    QueryBufferSize()
    {
        return _cbBuffer;
    }

    DWORD
    QueryMaxAlloc()
    {
        return _cbMaxAlloc;
    }

    VOID
    SetMaxAlloc(
        DWORD   cbMaxAlloc
        )
    {
        _cbMaxAlloc = cbMaxAlloc;
    }

    VOID
    ZeroInit(
        VOID
        )
    {
        FillMemory( _Inline, STB_INLINE_SIZE, 0x00 );

        if ( _pData != _Inline )
        {
            FillMemory( _pData, _cbBuffer, 0x00 );
        }
    }

    HRESULT
    Escape(
        BOOL    fAllowDoubleEscaping = FALSE
        )
    {
        STBUFF      Temp;
        DWORD       dwNumEscapes = 0;
        CHAR        szHex[3] = {0};
        BYTE *      pRead;
        BYTE *      pWrite;
        HRESULT     hr = S_OK;

        //
        // Walk through the string once.  If there
        // are no escapes, then we can just return.
        //

        GuaranteeNullTermination();

        pRead = (BYTE*)_pData;

        while ( *pRead != '\0' )
        {
            if ( ( fAllowDoubleEscaping ||
                   !IsEscapeSequence( (CHAR*)pRead ) ) &&
                 ShouldEscape( *pRead ) )
            {
                dwNumEscapes++;
            }

            pRead++;
        }

        if ( dwNumEscapes == 0 )
        {
            goto Finished;
        }

        //
        // Make sure that our cooked string buffer is big enough, so
        // we can manipulate its pointer directly.
        //

        hr = Temp.Resize( _cbData + dwNumEscapes * 2 );
        if ( FAILED (hr) )
        {
            goto Finished;
        }

        pRead = (BYTE*)_pData;
        pWrite = (BYTE*)Temp.QueryStr();

        while ( *pRead != '\0' )
        {
            if ( ( fAllowDoubleEscaping ||
                   !IsEscapeSequence( (CHAR*)pRead ) ) &&
                 ShouldEscape( *pRead ) )
            {
                _snprintf_s( szHex, 3, 2, "%02x", *pRead );

                *pWrite = '%';
                *(pWrite+1) = szHex[0];
                *(pWrite+2) = szHex[1];

                pRead++;
                pWrite += 3;

                continue;
            }

            *pWrite = *pRead;

            pRead++;
            pWrite++;
        }

        *pWrite = '\0';

        Temp.CalcDataSize();

        hr = SetData( Temp.QueryStr() );
        if ( FAILED (hr) )
        {
            goto Finished;
        }

    Finished:

        return hr;
    }

    VOID
    Unescape(
        VOID
        )
    {
        CHAR *  pRead;
        CHAR *  pWrite;
        CHAR    szHex[3] = {0};
        BYTE    c;

        pRead = (CHAR*)_pData;
        pWrite = pRead;

        while ( *pRead )
        {
            if ( IsEscapeSequence( pRead ) )
            {
                szHex[0] = *(pRead+1);
                szHex[1] = *(pRead+2);

                c = (BYTE)strtoul( szHex, NULL, 16 );

                *pWrite = c;

                pRead += 3;
                pWrite++;

                continue;
            }

            *pWrite = *pRead;

            pRead++;
            pWrite++;
        }

        *pWrite = '\0';

        CalcDataSize();

        return;
    }

    VOID
    MoveToFront(
        DWORD   cbOffset
        )
    {
        if ( cbOffset >= _cbData )
        {
            Reset();

            return;
        }

        MoveMemory( _pData, _pData + cbOffset, _cbData - cbOffset );

        _cbData -= cbOffset;
    }

    BOOL
    IsWildcardMatch(
        LPCSTR szExpr
        )
    {
        LPCSTR pExpr = szExpr;
        LPCSTR pString = QueryStr();
        LPCSTR pSubMatch;
        DWORD  cchSubMatch;

        if ( !pExpr )
        {
            SetLastError( ERROR_INVALID_PARAMETER );
            return FALSE;
        }

        while ( *pExpr )
        {
            switch ( *pExpr )
            {
            case '*':

                //
                // Eat '*' characters
                //

                while ( *pExpr == '*' )
                {
                    pExpr++;
                }

                //
                // Find the next wildcard
                //

                pSubMatch = strchr( pExpr, '*' );

                cchSubMatch = (DWORD)(pSubMatch ?
                                      pSubMatch - pExpr :
                                      strlen( pExpr ));

                if ( cchSubMatch == 0 )
                {
                    //
                    // No submatch.  The rest of
                    // pString automatically matches
                    //

                    return TRUE;
                }

                //
                // Ensure that the current submatch exists
                //

                while ( _strnicmp( pString, pExpr, cchSubMatch ) )
                {
                    pString++;

                    if ( *pString == '\0' )
                    {
                        //
                        // Not found
                        //

                        return FALSE;
                    }
                }

                pExpr += cchSubMatch;
                pString += cchSubMatch;

                break;

            default:

                if ( TO_UPPER( *pExpr ) != TO_UPPER( *pString ) )
                {
                    return FALSE;
                }

                pExpr++;
                pString++;
            }
        }

        if ( *pString != '\0' )
        {
            return FALSE;
        }

        return TRUE;
    }

private:

    LPBYTE  _pData;
    DWORD   _cbData;
    DWORD   _cbBuffer;
    DWORD   _cbMaxAlloc;
    BYTE    _Inline[STB_INLINE_SIZE+2];

    VOID
    GuaranteeNullTermination()
    {
        _pData[_cbData] = 0;
        _pData[_cbData+1] = 0;
    }

    BOOL
    IsEscapeSequence(
        CHAR *  str
        )
    {
        if ( *str == '%' &&
             isxdigit( *(str+1) ) &&
             isxdigit( *(str+2) ) )
        {
            return TRUE;
        }

        return FALSE;
    }

    BOOL
    ShouldEscape(
        BYTE    c
        )
    {
        //
        // If the character is listed in RFC2396, section
        // 2.4.3 as control, space, delims or unwise, we
        // should escape it.  Also, we should escape characters
        // with the high bit set.
        //

        if ( c <= 0x1f ||
             c == 0x7f )
        {
            //
            // Control character
            //

            goto ReturnTrue;
        }

        if ( c >= 0x80 )
        {
            //
            // High bit set
            //

            goto ReturnTrue;
        }

        switch ( c )
        {

        //
        // space
        //
        case ' ':

        //
        // delims
        //
        case '<':
        case '>':
        case '#':
        case '%':
        case '\"':

        //
        // unwise
        //
        case '{':
        case '}':
        case '|':
        case '\\':
        case '^':
        case '[':
        case ']':
        case '`':

            goto ReturnTrue;
        }

        //
        // If we get here, then the character should not be
        // escaped
        //

        return FALSE;

    ReturnTrue:

        return TRUE;
    }
};

#endif // _STBUFF_H
