// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _MULTISZ_H_
#define _MULTISZ_H_

#include "stringu.h"
#include "ntassert.h"
#include <CodeAnalysis/Warnings.h>

/*++
  class MULTISZ:

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
class MULTISZ : public BUFFER
{
public:

    MULTISZ()
      : BUFFER   (),
        m_cchLen ( 0),
        m_cStrings(0)
    { Reset(); }

    // creates a stack version of the MULTISZ object - uses passed in stack buffer
    //  MULTISZ does not free this pbInit on its own.
    MULTISZ( __in_bcount(cbInit) WCHAR * pbInit, DWORD cbInit)
        : BUFFER( (BYTE *) pbInit, cbInit),
          m_cchLen (0),
          m_cStrings(0)
    {}

    MULTISZ( const WCHAR * pchInit )
        : BUFFER   (),
          m_cchLen ( 0),
          m_cStrings(0)
    { AuxInit(pchInit); }

    MULTISZ( const MULTISZ & str )
        : BUFFER   (),
          m_cchLen ( 0),
          m_cStrings(0)
    { AuxInit( str.QueryStr()); }

    BOOL Append( const WCHAR  * pchInit ) {
      return ((pchInit != nullptr) ? (AuxAppend( pchInit,
                                              static_cast<DWORD>(wcslen(pchInit)) * sizeof(WCHAR)
                                              )) :
              TRUE);
    }

    BOOL Append( STRU & str )
      { return AuxAppend( str.QueryStr(),
                          (str.QueryCCH()) * sizeof(WCHAR)); }

    // Resets the internal string to be NULL string. Buffer remains cached.
    VOID Reset()
    { DBG_ASSERT( QueryPtr() != NULL);
      QueryStr()[0] = L'\0';
      QueryStr()[1] = L'\0';
      m_cchLen = 2;
      m_cStrings = 0;
    }

    BOOL Copy( const WCHAR  * pchInit, IN DWORD cbLen ) {
      if ( QueryPtr() ) { Reset(); }
      return ( (pchInit != nullptr) ?
               AuxAppend( pchInit, cbLen, FALSE ):
               TRUE);
    }

    BOOL Copy( const MULTISZ   & str )
    { return ( Copy(str.QueryStr(), str.QueryCB())); }

    //
    //  Returns the number of bytes in the string including the terminating
    //  NULLs
    //
    UINT QueryCB() const
        { return ( m_cchLen * sizeof(WCHAR)); }

    //
    //  Returns # of characters in the string including the terminating NULLs
    //
    UINT QueryCCH() const { return (m_cchLen); }

    //
    // Makes a copy of the stored string in given buffer
    //
    BOOL CopyToBuffer( __out_ecount_opt(*lpcch) WCHAR * lpszBuffer,  LPDWORD lpcch) const;

    //
    //  Return the string buffer
    //
    WCHAR * QueryStr() const { return ((WCHAR *) QueryPtr()); }


    //
    // Calculate total character length of a MULTI_SZ, including the
    // terminating NULLs.
    //

    static DWORD CalcLength( const WCHAR * str,
                                    LPDWORD pcStrings = NULL );

    //
    // Used for scanning a multisz.
    //

    const WCHAR * First() const
        { return *QueryStr() == L'\0' ? nullptr : QueryStr(); }

    const WCHAR * Next( const WCHAR * Current ) const
        { Current += wcslen( Current ) + 1;
          return *Current == L'\0' ? nullptr : Current; }

    BOOL
    Equals(
        MULTISZ* pmszRhs
    ) const;

private:

    DWORD m_cchLen;
    DWORD m_cStrings;
    VOID AuxInit( const WCHAR * pInit );
    BOOL AuxAppend( const WCHAR * pInit,
                           UINT cbStr, BOOL fAddSlop = TRUE );

};

#endif // !_MULTISZ_H_
