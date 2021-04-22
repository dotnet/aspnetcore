// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma warning (disable : 4267)
#include "precomp.h"
#include "multisza.h"

//
//  Private Definitions
//

#define MAXULONG 4294967295
#define ISWHITE( ch )       ((ch) == L' ' || (ch) == L'\t' || (ch) == L'\r')

//
//  When appending data, this is the extra amount we request to avoid
//  reallocations
//
#define STR_SLOP        128


DWORD
MULTISZA::CalcLength( const CHAR * str,
                     LPDWORD pcStrings )
{
    DWORD count = 0;
    DWORD total = 1;

    while( *str ) {
        DWORD len = ::strlen(str) + 1;
        total += len;
        str += len;
        count++;
    }

    if( pcStrings != NULL ) {
        *pcStrings = count;
    }

    return total;

}   // MULTISZA::CalcLength


BOOL
MULTISZA::FindString( const CHAR * str ) const
{
    //
    // Sanity check.
    //

    DBG_ASSERT( QueryStr() != NULL );
    DBG_ASSERT( str != NULL );
    DBG_ASSERT( *str != '\0' );

    //
    // Scan it.
    //

    CHAR* multisz = QueryStr();

    while( *multisz != '\0' ) {

        if( !::strcmp( multisz, str ) ) {

            return TRUE;

        }

        multisz += ::strlen( multisz ) + 1;

    }

    return FALSE;

}   // MULTISZA::FindString


BOOL
MULTISZA::FindStringNoCase( const CHAR * str ) const
{
    //
    // Sanity check.
    //

    DBG_ASSERT( QueryStr() != NULL );
    DBG_ASSERT( str != NULL );
    DBG_ASSERT( *str != '\0' );

    //
    // Scan it.
    //

    CHAR* multisz = QueryStr();

    while( *multisz != '\0' ) {

        if( !_stricmp( multisz, str ) ) {

            return TRUE;

        }

        multisz += strlen( multisz ) + 1;

    }

    return FALSE;

}   // MULTISZA::FindStringNoCase


VOID
MULTISZA::AuxInit( const CHAR * pInit )
{
    if ( pInit )
    {
        DWORD cStrings;
        int cbCopy = CalcLength( pInit, &cStrings ) * sizeof(CHAR);
        BOOL fRet = Resize(cbCopy);

        if ( fRet ) {
            CopyMemory( QueryPtr(), pInit, cbCopy );
            m_cchLen = (cbCopy)/sizeof(CHAR);
            m_cStrings = cStrings;
        } else {
//            BUFFER::SetValid( FALSE);
        }

    } else {

        Reset();

    }

} // MULTISZA::AuxInit()


/*******************************************************************

    NAME:       MULTISZA::AuxAppend

    SYNOPSIS:   Appends the string onto the MULTISZA.

    ENTRY:      Object to append
********************************************************************/

BOOL MULTISZA::AuxAppend( const CHAR * pStr, UINT cbStr, BOOL fAddSlop )
{
    DBG_ASSERT( pStr != NULL );

    UINT cbThis = QueryCB();

    if( cbThis == 2 ) {

        //
        // It's empty, so start at the beginning.
        //

        cbThis = 0;

    } else {

        //
        // It's not empty, so back up over the final terminating NULL.
        //

        cbThis -= sizeof(CHAR);

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
    // ( 2 * sizeof( CHAR ) ) is for the double terminator
    //
    ULONGLONG cb64Required = static_cast<ULONGLONG>(cbThis) + cbStr + 2 * sizeof(CHAR);
    if ( cb64Required > MAXULONG )
    {
        SetLastError( ERROR_ARITHMETIC_OVERFLOW );
        return FALSE;
    }
    if ( QuerySize() < static_cast<DWORD>(cb64Required) )
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
        if ( !Resize( static_cast<DWORD>(cb64AllocSize) ) )
            return FALSE;
    }

    // copy the exact string and tack on the double terminator
    memcpy( static_cast<BYTE*>(QueryPtr()) + cbThis,
            pStr,
            cbStr);
    *reinterpret_cast<CHAR*>(static_cast<BYTE*>(QueryPtr()) + cbThis + cbStr) = L'\0';
    *reinterpret_cast<CHAR*>(static_cast<BYTE*>(QueryPtr()) + cbThis + cbStr + sizeof(CHAR)) = L'\0';

    m_cchLen = CalcLength( reinterpret_cast<const CHAR*>(QueryPtr()), &m_cStrings );
    return TRUE;

} // MULTISZA::AuxAppend()

BOOL
MULTISZA::CopyToBuffer( __out_ecount_opt(*lpcch) CHAR * lpszBuffer, LPDWORD lpcch) const
/*++
    Description:
        Copies the string into the CHAR buffer passed in if the buffer
          is sufficient to hold the translated string.
        If the buffer is small, the function returns small and sets *lpcch
          to contain the required number of characters.

    Arguments:
        lpszBuffer      pointer to CHAR buffer which on return contains
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

    if ( lpcch == NULL) {
        SetLastError( ERROR_INVALID_PARAMETER);
        return ( FALSE);
    }

   DWORD cch = QueryCCH();

    if ( *lpcch >= cch) {

        DBG_ASSERT( lpszBuffer);
        memcpy( lpszBuffer, QueryStr(), cch * sizeof(CHAR));
    } else {
        DBG_ASSERT( *lpcch < cch);
        SetLastError( ERROR_INSUFFICIENT_BUFFER);
        fReturn = FALSE;
    }

    *lpcch = cch;

    return ( fReturn);
} // MULTISZA::CopyToBuffer()

BOOL
MULTISZA::Equals(
    MULTISZA* pmszRhs
) const
//
// Compares this to pmszRhs, returns TRUE if equal
//
{
    DBG_ASSERT( NULL != pmszRhs );

    PCSTR pszLhs = First( );
    PCSTR pszRhs = pmszRhs->First( );

    if( m_cStrings != pmszRhs->m_cStrings )
    {
        return FALSE;
    }

    while( NULL != pszLhs )
    {
        DBG_ASSERT( NULL != pszRhs );

        if( 0 != strcmp( pszLhs, pszRhs ) )
        {
            return FALSE;
        }

        pszLhs = Next( pszLhs );
        pszRhs = pmszRhs->Next( pszRhs );
    }

    return TRUE;
}

HRESULT
SplitCommaDelimitedString(
    PCSTR                        pszList,
    BOOL                         fTrimEntries,
    BOOL                         fRemoveEmptyEntries,
    MULTISZA *                   pmszList
)
/*++

Routine Description:

    Split comma delimited string into a MULTISZA. Additional leading empty
    entries after the first are discarded.

Arguments:

    pszList - List to split up
    fTrimEntries - Whether each entry should be trimmed before added to MULTISZA
    fRemoveEmptyEntries - Whether empty entires should be discarded
    pmszList - Filled with MULTISZA list

Return Value:

    HRESULT

--*/
{
    HRESULT                 hr = S_OK;

    if ( pszList == NULL ||
         pmszList == NULL )
    {
        DBG_ASSERT( FALSE );
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Finished;
    }
    
    pmszList->Reset();

    /*
        pszCurrent: start of the current entry which may be the comma that
                    precedes the next entry if the entry is empty

        pszNext: the comma that precedes the next entry. If
                 pszCurrent == pszNext, then the entry is empty

        pszEnd: just past the end of the current entry
    */
    
    for ( PCSTR pszCurrent = pszList,
                 pszNext = strchr( pszCurrent, L',' )
            ;
            ;
          pszCurrent = pszNext + 1,
          pszNext = strchr( pszCurrent, L',' ) )
    {
        PCSTR pszEnd = NULL;

        if ( pszNext != NULL )
        {
            pszEnd = pszNext;
        }
        else
        {
            pszEnd = pszCurrent + strlen( pszCurrent );
        }

        if ( fTrimEntries )
        {
            while ( pszCurrent < pszEnd && ISWHITE( pszCurrent[ 0 ] ) )
            {
                pszCurrent++;
            }

            while ( pszEnd > pszCurrent && ISWHITE( pszEnd[ -1 ] ) )
            {
                pszEnd--;
            }
        }

        if ( pszCurrent != pszEnd || !fRemoveEmptyEntries  )
        {
            if ( !pmszList->Append( pszCurrent, static_cast<DWORD>(pszEnd - pszCurrent) ) )
            {
                hr = HRESULT_FROM_WIN32( GetLastError() );
                goto Finished;
            }
        }
        
        if ( pszNext == NULL )
        {
            break;
        }
    }

Finished:

    return hr;
}
#pragma warning(default:4267)
