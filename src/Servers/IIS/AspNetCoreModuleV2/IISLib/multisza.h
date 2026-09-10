// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _MULTISZA_H_
#define _MULTISZA_H_

#include <Windows.h>
#include "stringa.h"


/*++
  class MULTISZA:

  Intention:
    A light-weight multi-string class supporting encapsulated string class.

    This object is derived from BUFFER class.
    It maintains following state:

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

    BOOL Append( const CHAR  * pchInit, DWORD cchLen ) {
      return ((pchInit != nullptr) ? (AuxAppend( pchInit,
                                              cchLen * sizeof(CHAR))) :
              TRUE);
    }

    // Resets the internal string to be NULL string. Buffer remains cached.
    VOID Reset()
    { DBG_ASSERT( QueryPtr() != nullptr );
      QueryStr()[0] = L'\0';
      QueryStr()[1] = L'\0';
      m_cchLen = 2;
      m_cStrings = 0;
    }

    BOOL Copy( const CHAR  * pchInit, IN DWORD cbLen ) {
      if ( QueryPtr() ) { Reset(); }
      return ( (pchInit != nullptr) ?
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
    //  Return the string buffer
    //
    CHAR * QueryStr() const { return reinterpret_cast<CHAR*>(QueryPtr()); }

    //
    //  Makes a clone of the current string in the string pointer passed in.
    //
    BOOL
      Clone( OUT MULTISZA * pstrClone) const
        {
          return ((pstrClone == nullptr) ?
                  (SetLastError(ERROR_INVALID_PARAMETER), FALSE) :
                  (pstrClone->Copy( *this))
                  );
        } // MULTISZA::Clone()


    //
    // Calculate total character length of a MULTI_SZ, including the
    // terminating NULLs.
    //

    static DWORD CalcLength( const CHAR * str,
                                    LPDWORD pcStrings = nullptr );

    //
    // Used for scanning a MULTISZA.
    //

    const CHAR * First( VOID ) const
        { return *QueryStr() == L'\0' ? nullptr : QueryStr(); }

    const CHAR * Next( const CHAR * Current ) const
        { Current += ::strlen( Current ) + 1;
          return *Current == L'\0' ? nullptr : Current; }

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

#endif // !_MULTISZA_H_

