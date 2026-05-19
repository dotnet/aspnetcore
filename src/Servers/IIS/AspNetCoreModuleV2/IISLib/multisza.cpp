// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma warning (disable : 4267)
#include "precomp.h"
#include "multisza.h"

//
//  Private Definitions
//

#define MAXULONG 4294967295

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

    if( pcStrings != nullptr ) {
        *pcStrings = count;
    }

    return total;

}   // MULTISZA::CalcLength


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
    DBG_ASSERT( pStr != nullptr);

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
MULTISZA::Equals(
    MULTISZA* pmszRhs
) const
//
// Compares this to pmszRhs, returns TRUE if equal
//
{
    DBG_ASSERT(nullptr != pmszRhs );

    PCSTR pszLhs = First( );
    PCSTR pszRhs = pmszRhs->First( );

    if( m_cStrings != pmszRhs->m_cStrings )
    {
        return FALSE;
    }

    while( nullptr != pszLhs )
    {
        DBG_ASSERT( nullptr != pszRhs );

        if( 0 != strcmp( pszLhs, pszRhs ) )
        {
            return FALSE;
        }

        pszLhs = Next( pszLhs );
        pszRhs = pmszRhs->Next( pszRhs );
    }

    return TRUE;
}

#pragma warning(default:4267)
