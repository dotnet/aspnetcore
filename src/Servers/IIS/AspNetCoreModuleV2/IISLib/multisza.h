// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _MULTISZA_H_
#define _MULTISZA_H_

#include <Windows.h>
#include "stringa.h"


/*++
  class MULTISZ:

  Intention:
    A light-weight multi-string class supporting encapsulated string class.

    This object is derived from BUFFER class.
    It maintains following state:

     m_fValid  - whether this object is valid -
        used only by MULTISZ() init functions
        * NYI: I need to kill this someday *
     m_cchLen - string length cached when we update the string.
     m_cStrings - number of strings.

  Member Functions:
    There are two categories of functions:
      1) Safe Functions - which do integrity checking of state
      2) UnSafe Functions - which do not do integrity checking, but
                     enable writing to the data stream freely.
             (someday this will be enabled as Safe versions without
               problem for users)

--*/
class MULTISZA : public BUFFER
{
public:

    MULTISZA()
      : BUFFER   (),
        m_cchLen ( 0),
        m_cStrings(0)
    { Reset(); }

    // creates a stack version of the MULTISZA object - uses passed in stack buffer
    //  MULTISZA does not free this pbInit on its own.
    MULTISZA( __in_bcount(cbInit) CHAR * pbInit, DWORD cbInit)
        : BUFFER( (BYTE *) pbInit, cbInit),
          m_cchLen (0),
          m_cStrings(0)
    {}

    MULTISZA( const CHAR * pchInit )
        : BUFFER   (),
          m_cchLen ( 0),
          m_cStrings(0)
    { AuxInit(pchInit); }

    MULTISZA( const MULTISZA & str )
        : BUFFER   (),
          m_cchLen ( 0),
          m_cStrings(0)
    { AuxInit( str.QueryStr()); }

//    BOOL IsValid(VOID) const { return ( BUFFER::IsValid()) ; }
    //
    //  Checks and returns TRUE if this string has no valid data else FALSE
    //
    BOOL IsEmpty( VOID) const      { return ( *QueryStr() == L'\0'); }

    BOOL Append( const CHAR  * pchInit ) {
      return ((pchInit != NULL) ? (AuxAppend( pchInit,
                                              (DWORD) (::strlen(pchInit)) * sizeof(CHAR)
                                              )) :
              TRUE);
    }


    BOOL Append( const CHAR  * pchInit, DWORD cchLen ) {
      return ((pchInit != NULL) ? (AuxAppend( pchInit,
                                              cchLen * sizeof(CHAR))) :
              TRUE);
    }

    BOOL Append( STRA & str )
      { return AuxAppend( str.QueryStr(),
                          (str.QueryCCH()) * sizeof(CHAR)); }

    // Resets the internal string to be NULL string. Buffer remains cached.
    VOID Reset()
    { DBG_ASSERT( QueryPtr() != NULL);
      QueryStr()[0] = L'\0';
      QueryStr()[1] = L'\0';
      m_cchLen = 2;
      m_cStrings = 0;
    }

    BOOL Copy( const CHAR  * pchInit, IN DWORD cbLen ) {
      if ( QueryPtr() ) { Reset(); }
      return ( (pchInit != NULL) ?
               AuxAppend( pchInit, cbLen, FALSE ):
               TRUE);
    }

    BOOL Copy( const MULTISZA   & str )
    { return ( Copy(str.QueryStr(), str.QueryCB())); }

    //
    //  Returns the number of bytes in the string including the terminating
    //  NULLs
    //
    UINT QueryCB() const
        { return ( m_cchLen * sizeof(CHAR)); }

    //
    //  Returns # of characters in the string including the terminating NULLs
    //
    UINT QueryCCH() const { return (m_cchLen); }

    //
    //  Returns # of strings in the MULTISZA.
    //

    DWORD QueryStringCount() const { return m_cStrings; }

    //
    // Makes a copy of the stored string in given buffer
    //
    BOOL CopyToBuffer( __out_ecount_opt(*lpcch) CHAR * lpszBuffer,  LPDWORD lpcch) const;

    //
    //  Return the string buffer
    //
    CHAR * QueryStrA() const { return ( QueryStr()); }
    CHAR * QueryStr() const { return reinterpret_cast<CHAR*>(QueryPtr()); }

    //
    //  Makes a clone of the current string in the string pointer passed in.
    //
    BOOL
      Clone( OUT MULTISZA * pstrClone) const
        {
          return ((pstrClone == NULL) ?
                  (SetLastError(ERROR_INVALID_PARAMETER), FALSE) :
                  (pstrClone->Copy( *this))
                  );
        } // MULTISZA::Clone()

    //
    //  Recalculates the length of *this because we've modified the buffers
    //  directly
    //

    VOID RecalcLen()
        { m_cchLen = MULTISZA::CalcLength( QueryStr(), &m_cStrings ); }

    //
    // Calculate total character length of a MULTI_SZ, including the
    // terminating NULLs.
    //

    static DWORD CalcLength( const CHAR * str,
                                    LPDWORD pcStrings = NULL );

    //
    // Determine if the MULTISZA contains a specific string.
    //

    BOOL FindString( const CHAR * str ) const;

    BOOL FindString( STRA & str ) const
    { return FindString( str.QueryStr() ); }

    //
    // Determine if the MULTISZA contains a specific string - case-insensitive
    //

    BOOL FindStringNoCase( const CHAR * str ) const;

    BOOL FindStringNoCase( STRA & str ) const
    { return FindStringNoCase( str.QueryStr() ); }

    //
    // Used for scanning a MULTISZA.
    //

    const CHAR * First( VOID ) const
        { return *QueryStr() == L'\0' ? NULL : QueryStr(); }

    const CHAR * Next( const CHAR * Current ) const
        { Current += ::strlen( Current ) + 1;
          return *Current == L'\0' ? NULL : Current; }

    BOOL
    Equals(
        MULTISZA* pmszRhs
    ) const;

private:

    DWORD m_cchLen;
    DWORD m_cStrings;
    VOID AuxInit( const CHAR * pInit );
    BOOL AuxAppend( const CHAR * pInit,
                           UINT cbStr, BOOL fAddSlop = TRUE );

};

//
//  Quick macro for declaring a MULTISZA that will use stack memory of <size>
//  bytes.  If the buffer overflows then a heap buffer will be allocated
//

#define STACK_MULTISZA( name, size )     CHAR __ach##name[size]; \
                                    MULTISZA name( __ach##name, sizeof( __ach##name ))

HRESULT
SplitCommaDelimitedString(
    PCSTR                       pszList,
    BOOL                        fTrimEntries,
    BOOL                        fRemoveEmptyEntries,
    MULTISZA *                  pmszList
);

#endif // !_MULTISZA_HXX_

