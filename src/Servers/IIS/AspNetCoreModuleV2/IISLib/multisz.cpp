// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


#pragma warning (disable : 4267)

#include "precomp.h"
#include "multisz.h"

#define MAXULONG 4294967295

//
//  When appending data, this is the extra amount we request to avoid
//  reallocations
//
#define STR_SLOP        128


DWORD
MULTISZ::CalcLength( const WCHAR * str,
                     LPDWORD pcStrings )
{
    DWORD count = 0;
    DWORD total = 1;

    while( *str ) {
        DWORD len = ::wcslen(str) + 1;
        total += len;
        str += len;
        count++;
    }

    if( pcStrings != nullptr ) {
        *pcStrings = count;
    }

    return total;

}   // MULTISZ::CalcLength


VOID
MULTISZ::AuxInit( const WCHAR * pInit )
{
    if ( pInit )
    {
        DWORD cStrings;
        int cbCopy = CalcLength( pInit, &cStrings ) * sizeof(WCHAR);
        BOOL fRet = Resize(cbCopy);

        if ( fRet ) {
            CopyMemory( QueryPtr(), pInit, cbCopy );
            m_cchLen = (cbCopy)/sizeof(WCHAR);
            m_cStrings = cStrings;
        } else {
//            BUFFER::SetValid( FALSE);
        }

    } else {

        Reset();

    }

} // MULTISZ::AuxInit()


/*******************************************************************

    NAME:       MULTISZ::AuxAppend

    SYNOPSIS:   Appends the string onto the multisz.

    ENTRY:      Object to append
********************************************************************/

BOOL MULTISZ::AuxAppend( const WCHAR * pStr, UINT cbStr, BOOL fAddSlop )
{
    DBG_ASSERT( pStr != nullptr );

    UINT cbThis = QueryCB();

    DBG_ASSERT( cbThis >= 2 );

    if( cbThis == 4 ) {

        //
        // It's empty, so start at the beginning.
        //

        cbThis = 0;

    } else {

        //
        // It's not empty, so back up over the final terminating NULL.
        //

        cbThis -= sizeof(WCHAR);

    }

    //
    //  Only resize when we have to.  When we do resize, we tack on
    //  some extra space to avoid extra reallocations.
    //
    //  Note: QuerySize returns the requested size of the string buffer,
    //        *not* the strlen of the buffer
    //

    //AcIncrement( CacMultiszAppend);

    //
    // Check for the arithmetic overflow
    //
    // ( 2 * sizeof( WCHAR ) ) is for the double terminator
    //
    ULONGLONG cb64Required = (ULONGLONG)cbThis + cbStr + 2 * sizeof(WCHAR);
    if ( cb64Required > MAXULONG )
    {
        SetLastError( ERROR_ARITHMETIC_OVERFLOW );
        return FALSE;
    }
    if ( QuerySize() < (DWORD) cb64Required )
    {
        ULONGLONG cb64AllocSize = cb64Required + (fAddSlop ? STR_SLOP : 0 );
        //
        // Check for the arithmetic overflow
        //
        if ( cb64AllocSize > MAXULONG )
        {
            SetLastError( ERROR_ARITHMETIC_OVERFLOW );
            return FALSE;
        }
        if ( !Resize( (DWORD) cb64AllocSize ) )
            return FALSE;
    }

    // copy the exact string and tack on the double terminator
    memcpy( (BYTE *) QueryPtr() + cbThis,
            pStr,
            cbStr);
    *(WCHAR *)((BYTE *)QueryPtr() + cbThis + cbStr) = L'\0';
    *(WCHAR *)((BYTE *)QueryPtr() + cbThis + cbStr + sizeof(WCHAR) ) = L'\0';

    m_cchLen = CalcLength( (const WCHAR *)QueryPtr(), &m_cStrings );
    return TRUE;

} // MULTISZ::AuxAppend()


#if 0

BOOL
MULTISZ::CopyToBuffer( WCHAR * lpszBuffer, LPDWORD lpcch) const
/*++
    Description:
        Copies the string into the WCHAR buffer passed in if the buffer
        is sufficient to hold the translated string.
        If the buffer is small, the function returns small and sets *lpcch
        to contain the required number of characters.

    Arguments:
        lpszBuffer      pointer to WCHAR buffer which on return contains
                        the UNICODE version of string on success.
        lpcch           pointer to DWORD containing the length of the buffer.
                        If *lpcch == 0 then the function returns TRUE with
                        the count of characters required stored in *lpcch.
                        Also in this case lpszBuffer is not affected.
    Returns:
        TRUE on success.
        FALSE on failure.  Use GetLastError() for further details.
--*/
{
   BOOL fReturn = TRUE;

    if ( lpcch == nullptr ) {
        SetLastError( ERROR_INVALID_PARAMETER);
        return ( FALSE);
    }

    if ( *lpcch == 0) {

      //
      //  Inquiring the size of buffer alone
      //
      *lpcch = QueryCCH() + 1;    // add one character for terminating null
    } else {

        //
        // Copy after conversion from ANSI to Unicode
        //
        int  iRet;
        iRet = MultiByteToWideChar( CP_ACP,
                                    MB_PRECOMPOSED | MB_ERR_INVALID_CHARS,
                                    QueryStrA(),  QueryCCH() + 1,
                                    lpszBuffer, (int )*lpcch);

        if ( iRet == 0 || iRet != (int ) *lpcch) {

            //
            // Error in conversion.
            //
            fReturn = FALSE;
        }
    }

    return ( fReturn);
} // MULTISZ::CopyToBuffer()
#endif

BOOL
MULTISZ::CopyToBuffer( __out_ecount_opt(*lpcch) WCHAR * lpszBuffer, LPDWORD lpcch) const
/*++
    Description:
        Copies the string into the WCHAR buffer passed in if the buffer
          is sufficient to hold the translated string.
        If the buffer is small, the function returns small and sets *lpcch
          to contain the required number of characters.

    Arguments:
        lpszBuffer      pointer to WCHAR buffer which on return contains
                        the string on success.
        lpcch           pointer to DWORD containing the length of the buffer.
                        If *lpcch == 0 then the function returns TRUE with
                        the count of characters required stored in lpcch.
                        Also in this case lpszBuffer is not affected.
    Returns:
        TRUE on success.
        FALSE on failure.  Use GetLastError() for further details.
--*/
{
   BOOL fReturn = TRUE;

    if ( lpcch == nullptr ) {
        SetLastError( ERROR_INVALID_PARAMETER);
        return ( FALSE);
    }

    DWORD cch = QueryCCH();

    if ( *lpcch >= cch) {

        DBG_ASSERT( lpszBuffer);
        memcpy( lpszBuffer, QueryStr(), cch * sizeof(WCHAR));
    } else {
        DBG_ASSERT( *lpcch < cch);
        SetLastError( ERROR_INSUFFICIENT_BUFFER);
        fReturn = FALSE;
    }

    *lpcch = cch;

    return ( fReturn);
} // MULTISZ::CopyToBuffer()

BOOL
MULTISZ::Equals(
    MULTISZ* pmszRhs
) const
//
// Compares this to pmszRhs, returns TRUE if equal
//
{
    DBG_ASSERT( nullptr != pmszRhs );

    PCWSTR pszLhs = First( );
    PCWSTR pszRhs = pmszRhs->First( );

    if( m_cStrings != pmszRhs->m_cStrings )
    {
        return FALSE;
    }

    while( nullptr  != pszLhs )
    {
        DBG_ASSERT( nullptr != pszRhs );

        if( 0 != wcscmp( pszLhs, pszRhs ) )
        {
            return FALSE;
        }

        pszLhs = Next( pszLhs );
        pszRhs = pmszRhs->Next( pszRhs );
    }

    return TRUE;
}

#pragma warning(default:4267)
