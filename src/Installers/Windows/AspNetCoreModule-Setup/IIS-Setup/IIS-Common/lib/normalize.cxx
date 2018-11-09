// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"
#include "normalize.h"
#include "stringa.h"

BOOL    g_fEnableNonUTF8;
BOOL    g_fEnableDBCS;
BOOL    g_fIsSystemDBCS;
static BOOL    g_fFavorDBCS;

#ifndef STACK_STRA
#define STACK_STRA(name, size)  CHAR __ach##name[size]; \
                                STRA name(__ach##name, sizeof(__ach##name) / sizeof(CHAR))
#endif

HRESULT
InitializeNormalizeUrl(
    VOID
)
{
    HKEY    hKey;
    DWORD   dwType;
    DWORD   dwData;
    DWORD   cbData;
    WORD    wPrimaryLangID;

    //
    // Read the registry settings on how to handle URLs
    //
    
    g_fEnableNonUTF8 = TRUE;
    g_fEnableDBCS = FALSE;
    g_fFavorDBCS = FALSE;
    
    if ( RegOpenKeyEx( HKEY_LOCAL_MACHINE,
                       L"System\\CurrentControlSet\\Services\\http\\Parameters",
                       0,
                       KEY_READ,
                       &hKey ) == ERROR_SUCCESS )
    {
        cbData = sizeof( dwData );
        if ( RegQueryValueEx( hKey,
                              L"EnableNonUTF8",
                              NULL,
                              &dwType,
                              (LPBYTE) &dwData,
                              &cbData ) == ERROR_SUCCESS &&
             dwType == REG_DWORD )
        {
            g_fEnableNonUTF8 = !!dwData;
        }
        
        if ( g_fEnableNonUTF8 )
        {
            cbData = sizeof( dwData );
        
            if ( RegQueryValueEx( hKey,
                                  L"EnableDBCS",
                                  NULL,
                                  &dwType,
                                  (LPBYTE) &dwData,
                                  &cbData ) == ERROR_SUCCESS &&
                 dwType == REG_DWORD )
            {
                g_fEnableDBCS = !!dwData;
            }
        }
        else
        {
            g_fEnableDBCS = FALSE;
        }
        
        if ( g_fEnableDBCS )
        {
            cbData = sizeof( dwData );
        
            if ( RegQueryValueEx( hKey,
                                  L"FavorDBCS",
                                  NULL,
                                  &dwType,
                                  (LPBYTE) &dwData,
                                  &cbData ) == ERROR_SUCCESS &&
                 dwType == REG_DWORD )
            {
                g_fFavorDBCS = !!dwData;
            }
        }
        else
        {
            g_fFavorDBCS = FALSE;
        }
        
        RegCloseKey( hKey );
    }


    wPrimaryLangID = PRIMARYLANGID( GetSystemDefaultLangID() );

    g_fIsSystemDBCS = ( wPrimaryLangID == LANG_JAPANESE ||
                        wPrimaryLangID == LANG_CHINESE  ||
                        wPrimaryLangID == LANG_KOREAN );

    return NO_ERROR;
}

//
//  Private constants.
//

#define ACTION_NOTHING              0x00000000
#define ACTION_EMIT_CH              0x00010000
#define ACTION_EMIT_DOT_CH          0x00020000
#define ACTION_EMIT_DOT_DOT_CH      0x00030000
#define ACTION_BACKUP               0x00040000
#define ACTION_MASK                 0xFFFF0000


//
//  Private globals.
//

INT p_StateTable[16] =
    {
        // state 0
        0 ,             // other
        0 ,             // "."
        4 ,             // EOS
        1 ,             // "\"

        //  state 1
        0 ,              // other
        2 ,             // "."
        4 ,             // EOS
        1 ,             // "\"

        // state 2
        0 ,             // other
        3 ,             // "."
        4 ,             // EOS
        1 ,             // "\"

        // state 3
        0 ,             // other
        0 ,             // "."
        4 ,             // EOS
        1               // "\"
    };



INT p_ActionTable[16] =
    {
        // state 0
            ACTION_EMIT_CH,             // other
            ACTION_EMIT_CH,             // "."
            ACTION_EMIT_CH,             // EOS
            ACTION_EMIT_CH,             // "\"

        // state 1
            ACTION_EMIT_CH,             // other
            ACTION_NOTHING,             // "."
            ACTION_EMIT_CH,             // EOS
            ACTION_NOTHING,             // "\"

        // state 2
            ACTION_EMIT_DOT_CH,         // other
            ACTION_NOTHING,             // "."
            ACTION_EMIT_CH,             // EOS
            ACTION_NOTHING,             // "\"

        // state 3
            ACTION_EMIT_DOT_DOT_CH,     // other
            ACTION_EMIT_DOT_DOT_CH,     // "."
            ACTION_BACKUP,              // EOS
            ACTION_BACKUP               // "\"
    };

// since max states = 4, we calculat the index by multiplying with 4.
# define IndexFromState( st)   ( (st) * 4)


// the following table provides the index for various ISA Latin1 characters
//  in the incoming URL.
// It assumes that the URL is ISO Latin1 == ASCII
INT  p_rgIndexForChar[] = {

    2,   // null char
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 1 thru 10
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 11 thru 20
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 21 thru 30
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 31 thru 40
    0, 0, 0, 0, 0, 1, 3, 0, 0, 0,  // 41 thru 50  46 = '.' 47 = '/'
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 51 thru 60
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 61 thru 70
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 71 thru 80
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 81 thru 90
    0, 3, 0, 0, 0, 0, 0, 0, 0, 0,  // 91 thru 100  92 = '\\'
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 101 thru 110
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  // 111 thru 120
    0, 0, 0, 0, 0, 0, 0, 0         // 121 thru 128
};

#define IS_UTF8_TRAILBYTE(ch)      (((ch) & 0xc0) == 0x80)


/*******************************************************************

    NAME:       IsUTF8URL

    ENTRY:      pszPath - The path to sanitize.

    HISTORY:
        atsusk     06-Jan-1998 Created.

********************************************************************/

BOOL IsUTF8URL(__in LPSTR pszPath)
{
    CHAR    ch;

    if ( g_fFavorDBCS )
    {
        return ( MultiByteToWideChar( CP_ACP,
                                      MB_ERR_INVALID_CHARS,
                                      pszPath,
                                      -1,
                                      NULL,
                                      0) == 0);
    }

    while (ch = *pszPath++) {

        if (ch & 0x80) {
            wchar_t wch;
            int     iLen;
            BOOL    bDefault = FALSE;
            char    chTrail1;
            char    chTrail2;

            chTrail1 = *pszPath++;
            if (chTrail1) {
                chTrail2 = *pszPath;
            } else {
                chTrail2 = 0;
            }

            if ( ((ch & 0xF0) == 0xE0) &&
              IS_UTF8_TRAILBYTE(chTrail1) &&
              IS_UTF8_TRAILBYTE(chTrail2) ) {

                // handle three byte case
                // 1110xxxx 10xxxxxx 10xxxxxx
                wch = (wchar_t) (((ch & 0x0f) << 12) |
                                ((chTrail1 & 0x3f) << 6) |
                                (chTrail2 & 0x3f));
                pszPath++;

            } else
            if ( ((ch & 0xE0) == 0xC0) &&
              IS_UTF8_TRAILBYTE(chTrail1) ) {

                // handle two byte case
                // 110xxxxx 10xxxxxx

                wch = (wchar_t) (((ch & 0x1f) << 6) | (chTrail1 & 0x3f));

            } else
                return FALSE;

            iLen = WideCharToMultiByte( CP_ACP,
                                        WC_NO_BEST_FIT_CHARS,
                                        &wch,
                                        1,
                                        NULL,
                                        0,
                                        NULL,
                                        &bDefault );

            if (bDefault == TRUE || iLen == 0 || iLen > 2)
                return FALSE;
        }
    }

    return TRUE;
}   // IsUTF8URL()


/*******************************************************************

    NAME:       CanonURL

    SYNOPSIS:   Sanitizes a path by removing bogus path elements.

                As expected, "/./" entries are simply removed, and
                "/../" entries are removed along with the previous
                path element.

                To maintain compatibility with URL path semantics
                 additional transformations are required. All backward
                 slashes "\\" are converted to forward slashes. Any
                 repeated forward slashes (such as "///") are mapped to
                 single backslashes.

                A state table (see the p_StateTable global at the
                beginning of this file) is used to perform most of
                the transformations.  The table's rows are indexed
                by current state, and the columns are indexed by
                the current character's "class" (either slash, dot,
                NULL, or other).  Each entry in the table consists
                of the new state tagged with an action to perform.
                See the ACTION_* constants for the valid action
                codes.

    ENTRY:      pszPath - The path to sanitize.
                fIsDBCSLocale - Indicates the server is in a
                    locale that uses DBCS.

    HISTORY:
        KeithMo     07-Sep-1994 Created.
        MuraliK     28-Apr-1995 Adopted this for symbolic paths

********************************************************************/
INT
CanonURL(
    __inout LPSTR pszPath,
    BOOL   fIsDBCSLocale
    )
{
    UCHAR * pszSrc;
    UCHAR * pszDest;
    DWORD ch;
    INT   index;
    BOOL  fDBCS = FALSE;
    DWORD cchMultiByte = 0;

    DBG_ASSERT( pszPath != NULL );

    //
    // Always look for UTF8 except when DBCS characters are detected
    //
    BOOL fScanForUTF8 = IsUTF8URL(pszPath);

    // If fScanForUTF8 is true, this URL is UTF8. don't recognize DBCS.
    if (fIsDBCSLocale && fScanForUTF8) {
        fIsDBCSLocale = FALSE;
    }

    //
    //  Start our scan at the first character
    //

    pszSrc = pszDest = (UCHAR *) pszPath;

    //
    //  State 0 is the initial state.
    //
    index = 0; // State = 0

    //
    //  Loop until we enter state 4 (the final, accepting state).
    //

    do {

        //
        //  Grab the next character from the path and compute its
        //  next state.  While we're at it, map any forward
        //  slashes to backward slashes.
        //

        index = IndexFromState( p_StateTable[index]); // 4 = # states
        ch = (DWORD ) *pszSrc++;

        //
        //  If this is a DBCS trailing byte - skip it
        //

        if ( !fIsDBCSLocale )
        {
            index += (( ch >= 0x80) ? 0 : p_rgIndexForChar[ch]);
        }
        else
        {
            if ( fDBCS )
            {
                //
                // If this is a 0 terminator, we need to set next
                // state accordingly
                //

                if ( ch == 0 )
                {
                    index += p_rgIndexForChar[ ch ];
                }

                //
                // fDBCS == TRUE means this byte was a trail byte.
                // index is implicitly set to zero.
                //
                fDBCS = FALSE;
            }
            else
            {
                index += (( ch >= 0x80) ? 0 : p_rgIndexForChar[ch]);

                if ( IsDBCSLeadByte( (UCHAR)ch ) )
                {
                    //
                    // This is a lead byte, so the next is a trail.
                    //
                    fDBCS = TRUE;
                }
            }
        }

        //
        //  Interesting UTF8 characters always have the top bit set
        //

        if ( (ch & 0x80) && fScanForUTF8 )
        {
            wchar_t wch;
            UCHAR mbstr[2];

            //
            //  This is a UTF8 character, convert it here.
            //  index is implicitly set to zero.
            //
            if ( cchMultiByte < 2 )
            {
                char chTrail1;
                char chTrail2;

                chTrail1 = *pszSrc;
                if (chTrail1) {
                    chTrail2 = *(pszSrc+1);
                } else {
                    chTrail2 = 0;
                }
                wch = 0;

                if ((ch & 0xf0) == 0xe0)
                {
                    // handle three byte case
                    // 1110xxxx 10xxxxxx 10xxxxxx

                    wch = (wchar_t) (((ch & 0x0f) << 12) |
                                     ((chTrail1 & 0x3f) << 6) |
                                     (chTrail2 & 0x3f));

                    cchMultiByte = WideCharToMultiByte( CP_ACP,
                                                        WC_NO_BEST_FIT_CHARS,
                                                        &wch,
                                                        1,
                                                        (LPSTR) mbstr,
                                                        2,
                                                        NULL,
                                                        NULL );

                    ch = mbstr[0];
                    pszSrc += (3 - cchMultiByte);

                    // WinSE 12843: Security Fix, Index should be updated for this character
                    index += (( ch >= 0x80) ? 0 : p_rgIndexForChar[ch]);

                } else if ((ch & 0xe0) == 0xc0)
                {
                    // handle two byte case
                    // 110xxxxx 10xxxxxx

                    wch = (wchar_t) (((ch & 0x1f) << 6) | (chTrail1 & 0x3f));

                    cchMultiByte = WideCharToMultiByte( CP_ACP,
                                                        WC_NO_BEST_FIT_CHARS,
                                                        &wch,
                                                        1,
                                                        (LPSTR) mbstr,
                                                        2,
                                                        NULL,
                                                        NULL );

                    ch = mbstr[0];
                    pszSrc += (2 - cchMultiByte);

                    // WinSE 12843: Security Fix, Index should be updated for this character
                    index += (( ch >= 0x80) ? 0 : p_rgIndexForChar[ch]);
                }

            } else {
                //
                // get ready to emit 2nd byte of converted character
                //
                ch = mbstr[1];
                cchMultiByte = 0;
            }
        }


        //
        //  Perform the action associated with the state.
        //

        switch( p_ActionTable[index] )
        {
        case ACTION_EMIT_DOT_DOT_CH :
            *pszDest++ = '.';
            /* fall through */

        case ACTION_EMIT_DOT_CH :
            *pszDest++ = '.';
            /* fall through */

        case ACTION_EMIT_CH :
            *pszDest++ = (CHAR ) ch;
            /* fall through */

        case ACTION_NOTHING :
            break;

        case ACTION_BACKUP :
            if( (pszDest > ( (UCHAR *) pszPath + 1 ) ) && (*pszPath == '/'))
            {
                pszDest--;
                DBG_ASSERT( *pszDest == '/' );

                *pszDest = '\0';
                pszDest = (UCHAR *) strrchr( pszPath, '/') + 1;
            }

            *pszDest = '\0';
            break;

        default :
            DBG_ASSERT( !"Invalid action code in state table!" );
            index = IndexFromState(0) + 2;    // move to invalid state
            DBG_ASSERT( p_StateTable[index] == 4);
            *pszDest++ = '\0';
            break;
        }

    } while( p_StateTable[index] != 4 );

    //
    // point to terminating nul
    // only do the check if we aren't about to go outside of the number
    // of elements in the table.
    //
    if ( ( index < ( sizeof(p_ActionTable) / sizeof(p_ActionTable[0]) ) )
         && p_ActionTable[index] == ACTION_EMIT_CH ) 
    {
        pszDest--;
    }

    DBG_ASSERT(*pszDest == '\0' && pszDest > (UCHAR*) pszPath);

    return (INT)DIFF(pszDest - (UCHAR*)pszPath);
}   // CanonURL()



HRESULT
NormalizeUrl(
    __inout LPSTR   pszStart
    )
/*++

Routine Description:

    Normalize URL

Arguments:

    strUrl - URL to be updated to a canonical URI

Return value:

    TRUE if no error, otherwise FALSE

--*/
{
    CHAR * pchParams;
    LPSTR   pszSlash;
    LPSTR   pszURL;
    LPSTR   pszValue;
    STACK_STRA( strChgUrl, MAX_PATH );
    HRESULT hr;
    DWORD   cchInput;

    if ( pszStart == NULL )
    {
        return HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
    }

    cchInput = (DWORD)strlen( pszStart );

    if ( *pszStart != '/' )
    {
        //
        // assume HTTP URL, skip protocol & host name by
        // searching for 1st '/' following "//"
        //
        // We handle this information as a "Host:" header.
        // It will be overwritten by the real header if it is
        // present.
        //
        // We do not check for a match in this case.
        //

        if ( (pszSlash = strchr( pszStart, '/' )) && pszSlash[1] == '/' )
        {
            pszSlash += 2;
            if ( pszURL = strchr( pszSlash, '/' ) )
            {
                //
                // update pointer to URL to point to the 1st slash
                // following host name
                //

                pszValue = pszURL;
            }
            else
            {
                //
                // if no single slash following host name
                // consider the URL to be empty.
                //

                pszValue = pszSlash + strlen( pszSlash );
            }

            memmove( pszStart, pszValue, strlen(pszValue)+1 );
        }

        //
        // if no double slash, this is not a fully qualified URL
        // and we leave it alone.
        //
    }

    //
    //  Check for a question mark which indicates this URL contains some
    //  parameters and break the two apart if found
    //

    if ( (pchParams = strchr( pszStart, '?' )) )
    {
        *pchParams = '\0';
    }

    //
    // Unescape wants a STR ( sigh )
    //

    hr = strChgUrl.Copy( (CHAR*)pszStart );

    if ( FAILED( hr ) )
    {
        return hr;
    }

    strChgUrl.Unescape();

    hr = StringCchCopyNA( pszStart, cchInput + 1, strChgUrl.QueryStr(), cchInput );
    if ( FAILED( hr ) )
    {
        return hr;
    }

    //
    //  Canonicalize the URL
    //

    CanonURL( pszStart, g_fIsSystemDBCS );

    return NO_ERROR;
}




HRESULT
NormalizeUrlOld(
    __inout LPSTR    pszUrl
)
/*++

Routine Description:

    NormalizeUrl wrapper (used by ISAPI filter and extension support functions)

Parameters:

    pszUrl           - On entry, the URL to be normalized
                       On return, the normalized URL
                       (size of normalized URL is always <= not normalized URL)
   
Return Value:
    
    HRESULT

--*/
{
    HRESULT hr = NO_ERROR;
    
    if ( pszUrl )
    {
        STACK_BUFFER( buffUrlOutput, MAX_PATH );
        STACK_STRA(   strUrlA, MAX_PATH );
        LPWSTR        szQueryString;
        DWORD         cchData;
        DWORD         cbOutput;

        cchData = (DWORD)strlen( pszUrl );

        //
        // Prepare the Output string
        //
        
        if ( !buffUrlOutput.Resize( ( cchData + 1 ) *sizeof( WCHAR ) ) )  
        {
            return HRESULT_FROM_WIN32( GetLastError() );
        }

        //
        // Normalize it
        //

        hr = UlCleanAndCopyUrl(
            pszUrl,
            cchData,
            &cbOutput,
            (WCHAR *) buffUrlOutput.QueryPtr(),
            &szQueryString
            );

        if ( FAILED( hr ) )
        {
            return hr;
        }

        //
        // Terminate the string at the query so that the
        // query string doesn't appear in the output.  IIS 5
        // truncated in this way.
        //

        if ( szQueryString != NULL )
        {
            ((WCHAR *) buffUrlOutput.QueryPtr())[ cbOutput - wcslen( szQueryString )] = 0;
        }

        //
        // Write the normalized URL over the input data
        //

        hr = strUrlA.CopyW( (WCHAR *) buffUrlOutput.QueryPtr() );

        if ( FAILED( hr ) )
        {
            return hr;
        }

        //
        // Normalized string will never be longer than the original one
        //
        
        DBG_ASSERT( strUrlA.QueryCCH() <= cchData );

        hr = StringCchCopyA( pszUrl, cchData + 1, strUrlA.QueryStr() );
        if ( FAILED( hr ) )
        {
            return hr;
        }

        hr = NO_ERROR;
    }
    else
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
    }
    return hr;
}


HRESULT
NormalizeUrlW(
    __inout LPWSTR    pszUrl
)
/*++

Routine Description:

    unicode version of NormalizeUrl wrapper (used by ISAPI filter and extension support functions)

Parameters:

    pszUrl           - On entry, the URL to be normalized
                       On return, the normalized URL
                       (size of normalized URL is always <= not normalized URL)
   
Return Value:
    
    HRESULT

--*/
{

    HRESULT hr = NO_ERROR;
    
    if ( pszUrl )
    {
        STACK_BUFFER( buffUrlOutput, MAX_PATH );
        STACK_STRA(   strUrlA, MAX_PATH );
        LPWSTR        szQueryString;
        DWORD         cchData;
        DWORD         cbOutput;

        cchData = (DWORD)wcslen( pszUrl );

        hr = strUrlA.CopyWToUTF8Escaped( pszUrl );

        if ( FAILED( hr ) )
        {
            return hr;
        }

        //
        // Prepare Output string
        //
        
        if ( !buffUrlOutput.Resize( ( cchData + 1 ) *sizeof( WCHAR ) ) )  
        {
            return HRESULT_FROM_WIN32( GetLastError() );
        }

        //
        // Normalize it
        //

        hr = UlCleanAndCopyUrl(
            strUrlA.QueryStr(),
            strUrlA.QueryCB(),
            &cbOutput,
            (WCHAR *) buffUrlOutput.QueryPtr(),
            &szQueryString
            );

        if ( FAILED( hr ) )
        {
            return hr;
        }


        //
        // Terminate the string at the query so that the
        // query string doesn't appear in the output.  IIS 5
        // truncated in this way.
        //

        if ( szQueryString != NULL )
        {
            ((WCHAR *) buffUrlOutput.QueryPtr())[ cbOutput - wcslen( szQueryString )] = 0;
        }

        //
        // normalized string will never be longer than the original one
        //

        DBG_ASSERT( cbOutput <= cchData * sizeof( WCHAR )  );

        //
        // Write the normalized URL over the input data
        //

        hr = StringCchCopyW( pszUrl, cchData+1, (WCHAR *) buffUrlOutput.QueryPtr() );
        if ( FAILED( hr )  )
        {
            return hr;
        }

        hr = NO_ERROR;
    }
    else
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
    }
    return hr;
}
