// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

//
// BUGBUG: Turn off optimization on ia64 builds due to a compiler bug
//
#if (defined(_M_IA64) && (_MSC_FULL_VER == 13009286))
#pragma optimize("",off)
#endif

#define LF                  0x0A
#define SP                  0x20
#define HT                  0x09

#define HTTP_CHAR           0x001
#define HTTP_UPCASE         0x002
#define HTTP_LOCASE         0x004
#define HTTP_ALPHA          (HTTP_UPCASE | HTTP_LOCASE)
#define HTTP_DIGIT          0x008
#define HTTP_CTL            0x010
#define HTTP_LWS            0x020
#define HTTP_HEX            0x040
#define HTTP_SEPERATOR      0x080
#define HTTP_TOKEN          0x100

#define URL_LEGAL           0x200
#define URL_TOKEN           (HTTP_ALPHA | HTTP_DIGIT | URL_LEGAL)

#define IS_HTTP_UPCASE(c)       (HttpChars[(UCHAR)(c)] & HTTP_UPCASE)
#define IS_HTTP_LOCASE(c)       (HttpChars[(UCHAR)(c)] & HTTP_UPCASE)
#define IS_HTTP_ALPHA(c)        (HttpChars[(UCHAR)(c)] & HTTP_ALPHA)
#define IS_HTTP_DIGIT(c)        (HttpChars[(UCHAR)(c)] & HTTP_DIGIT)
#define IS_HTTP_HEX(c)          (HttpChars[(UCHAR)(c)] & HTTP_HEX)
#define IS_HTTP_CTL(c)          (HttpChars[(UCHAR)(c)] & HTTP_CTL)
#define IS_HTTP_LWS(c)          (HttpChars[(UCHAR)(c)] & HTTP_LWS)
#define IS_HTTP_SEPERATOR(c)    (HttpChars[(UCHAR)(c)] & HTTP_SEPERATOR)
#define IS_HTTP_TOKEN(c)        (HttpChars[(UCHAR)(c)] & HTTP_TOKEN)
#define IS_URL_TOKEN(c)         (HttpChars[(UCHAR)(c)] & URL_TOKEN)

// Some stuff not defined in VS SDK
#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#endif

// Copied from ntstatus.h, otherwise we will have double definition for
// number of macros that are both in ntstatus.h and winnt.h
#ifndef STATUS_SUCCESS
#define STATUS_SUCCESS                          ((NTSTATUS)0x00000000L) // ntsubauth
#endif
#ifndef STATUS_OBJECT_PATH_SYNTAX_BAD
#define STATUS_OBJECT_PATH_SYNTAX_BAD    ((NTSTATUS)0xC000003BL)
#endif
#ifndef STATUS_OBJECT_PATH_INVALID
#define STATUS_OBJECT_PATH_INVALID       ((NTSTATUS)0xC0000039L)
#endif
//
//  Constant Declarations for UTF8 Encoding
//

#define ASCII                 0x007f

#define UTF8_2_MAX            0x07ff  // max UTF8 2-byte sequence (32 * 64 =2048)
#define UTF8_1ST_OF_2         0xc0    // 110x xxxx
#define UTF8_1ST_OF_3         0xe0    // 1110 xxxx
#define UTF8_1ST_OF_4         0xf0    // 1111 xxxx
#define UTF8_TRAIL            0x80    // 10xx xxxx

#define HIGHER_6_BIT(u)       ((u) >> 12)
#define MIDDLE_6_BIT(u)       (((u) & 0x0fc0) >> 6)
#define LOWER_6_BIT(u)        ((u) & 0x003f)

#define BIT7(a)               ((a) & 0x80)
#define BIT6(a)               ((a) & 0x40)

#define HIGH_SURROGATE_START  0xd800
#define HIGH_SURROGATE_END    0xdbff
#define LOW_SURROGATE_START   0xdc00
#define LOW_SURROGATE_END     0xdfff

#define EMIT_CHAR(ch)                                   \
    do {                                                \
        pDest[0] = (ch);                                \
        pDest += 1;                                     \
        BytesCopied += 2;                               \
    } while (0)

typedef enum _URL_TYPE
{
    UrlTypeUtf8,
    UrlTypeAnsi,
    UrlTypeDbcs
} URL_TYPE;

typedef enum _URL_PART
{
    Scheme,
    HostName,
    AbsPath,
    QueryString

} URL_PART;

#define IS_UTF8_TRAILBYTE(ch)      (((ch) & 0xc0) == 0x80)

//
// These are copied from RTL NLS routines.
//

#define DBCS_TABLE_SIZE 256
extern PUSHORT NlsLeadByteInfo;

#define LeadByteTable (*(PUSHORT *)NlsLeadByteInfo)
#define IS_LEAD_BYTE(c) (IsDBCSLeadByte(c))

ULONG   HttpChars[256];
USHORT  FastPopChars[256];
USHORT  DummyPopChars[256];
WCHAR   FastUpcaseChars[256];
BOOL    g_UlEnableNonUTF8;
BOOL    g_UlEnableDBCS;
BOOL    g_UlFavorDBCS;

// RtlNtStatusToDosError is nowhere defined, should be used through
// native code "reflection"
typedef ULONG (RTL_NT_STATUS_TO_DOS_ERROR_PROC)(NTSTATUS);
static RTL_NT_STATUS_TO_DOS_ERROR_PROC* rtlNtStatusToDosErrorProc = NULL;
static HMODULE lib = NULL;

HRESULT
WIN32_FROM_NTSTATUS(
    __in NTSTATUS status,
    __out DWORD * pResult
    )
{
    HRESULT hr = S_OK;
    if (pResult == NULL)
    {
        hr = E_INVALIDARG;
        goto Finished;
    }
    if (lib == NULL)
    {
        lib = GetModuleHandle(L"Ntdll.dll");
        if (lib == NULL)
        {
            hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
            goto Finished;
        }
    }
    if (rtlNtStatusToDosErrorProc == NULL)
    {
        rtlNtStatusToDosErrorProc =
            (RTL_NT_STATUS_TO_DOS_ERROR_PROC*)GetProcAddress(
                lib, "RtlNtStatusToDosError");
        if (rtlNtStatusToDosErrorProc == NULL)
        {
            hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
            goto Finished;
        }
    }
    *pResult = rtlNtStatusToDosErrorProc(status);

Finished:
    return hr;
}

NTSTATUS
Unescape(
    IN  PUCHAR pChar,
    OUT PUCHAR pOutChar
    )

{
    UCHAR Result, Digit;

    if (pChar[0] != '%' ||
        SAFEIsXDigit(pChar[1]) == FALSE ||
        SAFEIsXDigit(pChar[2]) == FALSE)
    {
        return STATUS_OBJECT_PATH_SYNTAX_BAD;
    }

    //
    // HexToChar() inlined
    //

    // uppercase #1
    //
    if (SAFEIsAlpha(pChar[1]))
        Digit = (UCHAR) toupper(pChar[1]);
    else
        Digit = pChar[1];

    Result = ((Digit >= 'A') ? (Digit - 'A' + 0xA) : (Digit - '0')) << 4;

    // uppercase #2
    //
    if (SAFEIsAlpha(pChar[2]))
        Digit = (UCHAR) toupper(pChar[2]);
    else
        Digit = pChar[2];

    Result |= (Digit >= 'A') ? (Digit - 'A' + 0xA) : (Digit - '0');

    *pOutChar = Result;

    return STATUS_SUCCESS;

}   // Unescape

//
// PopChar is used only if the string is not UTF-8, or UrlPart != QueryString,
// or the current character is '%' or its high bit is set.  In all other cases,
// the FastPopChars table is used for fast conversion.
//

__inline
NTSTATUS
PopChar(
    IN URL_TYPE UrlType,
    IN URL_PART UrlPart,
    __in LPSTR pChar,
    __out WCHAR * pUnicodeChar,
    __out WCHAR * pUnicodeChar2,
    OUT PULONG pCharToSkip
    )
{
    NTSTATUS Status;
    WCHAR   UnicodeChar = L'\0';
    WCHAR   UnicodeChar2 = L'\0';
    UCHAR   Char;
    UCHAR   Trail1;
    UCHAR   Trail2;
    UCHAR   Trail3;
    ULONG   CharToSkip;

    //
    // validate it as a valid url character
    //

    if (UrlPart != QueryString)
    {
        if (IS_URL_TOKEN((UCHAR)pChar[0]) == FALSE)
        {
            Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
            goto end;
        }
    }
    else
    {
        //
        // Allow anything but linefeed in the query string.
        //

        if (pChar[0] == LF)
        {
            Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
            goto end;
        }

        UnicodeChar = (USHORT) (UCHAR)pChar[0];
        CharToSkip = 1;

        // skip all the decoding stuff
        goto slash;
    }

    //
    // need to unescape ?
    //
    // can't decode the query string.  that would be lossy decodeing
    // as '=' and '&' characters might be encoded, but have meaning
    // to the usermode parser.
    //

    if (pChar[0] == '%')
    {
        Status = Unescape((PUCHAR)pChar, &Char);
        if (NT_SUCCESS(Status) == FALSE)
            goto end;
        CharToSkip = 3;
    }
    else
    {
        Char = pChar[0];
        CharToSkip = 1;
    }

    if (UrlType == UrlTypeUtf8)
    {
        //
        // convert to unicode, checking for utf8 .
        //
        // 3 byte runs are the largest we can have.  16 bits in UCS-2 =
        // 3 bytes of (4+4,2+6,2+6) where it's code + char.
        // for a total of 6+6+4 char bits = 16 bits.
        //

        //
        // NOTE: we'll only bother to decode utf if it was escaped
        // thus the (CharToSkip == 3)
        //
        if ((CharToSkip == 3) && ((Char & 0xf8) == 0xf0))
        {
            // 4 byte run
            //

            // Unescape the next 3 trail bytes
            //

            Status = Unescape((PUCHAR)pChar+CharToSkip, &Trail1);
            if (NT_SUCCESS(Status) == FALSE)
                goto end;

            CharToSkip += 3; // %xx

            Status = Unescape((PUCHAR)pChar+CharToSkip, &Trail2);
            if (NT_SUCCESS(Status) == FALSE)
                goto end;

            CharToSkip += 3; // %xx

            Status = Unescape((PUCHAR)pChar+CharToSkip, &Trail3);
            if (NT_SUCCESS(Status) == FALSE)
                goto end;

            CharToSkip += 3; // %xx

            if (IS_UTF8_TRAILBYTE(Trail1) == FALSE ||
                IS_UTF8_TRAILBYTE(Trail2) == FALSE ||
                IS_UTF8_TRAILBYTE(Trail3) == FALSE)
            {
                // bad utf!
                //
                Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
                goto end;
            }

            // handle four byte case - convert to utf-16
            // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx

            UnicodeChar = ((USHORT) ((Char & 0x07) << 8) |
                                     ((Trail1 & 0x3f) << 2) |
                                     ((Trail2 & 0x30) >> 4)) + 0xD7C0;
            UnicodeChar2 = ((USHORT) ((Trail2 & 0x0f) << 6) |
                                      (Trail3 & 0x3f)) | 0xDC00;
        }
        else if ((CharToSkip == 3) && ((Char & 0xf0) == 0xe0))
        {
            // 3 byte run
            //

            // Unescape the next 2 trail bytes
            //

            Status = Unescape((PUCHAR)pChar+CharToSkip, &Trail1);
            if (NT_SUCCESS(Status) == FALSE)
                goto end;

            CharToSkip += 3; // %xx

            Status = Unescape((PUCHAR)pChar+CharToSkip, &Trail2);
            if (NT_SUCCESS(Status) == FALSE)
                goto end;

            CharToSkip += 3; // %xx

            if (IS_UTF8_TRAILBYTE(Trail1) == FALSE ||
                IS_UTF8_TRAILBYTE(Trail2) == FALSE)
            {
                // bad utf!
                //
                Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
                goto end;
            }

            // handle three byte case
            // 1110xxxx 10xxxxxx 10xxxxxx

            UnicodeChar = (USHORT) (((Char & 0x0f) << 12) |
                                    ((Trail1 & 0x3f) << 6) |
                                    (Trail2 & 0x3f));

        }
        else if ((CharToSkip == 3) && ((Char & 0xe0) == 0xc0))
        {
            // 2 byte run
            //

            // Unescape the next 1 trail byte
            //

            Status = Unescape((PUCHAR)pChar+CharToSkip, &Trail1);
            if (NT_SUCCESS(Status) == FALSE)
                goto end;

            CharToSkip += 3; // %xx

            if (IS_UTF8_TRAILBYTE(Trail1) == FALSE)
            {
                // bad utf!
                //
                Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
                goto end;
            }

            // handle two byte case
            // 110xxxxx 10xxxxxx

            UnicodeChar = (USHORT) (((Char & 0x1f) << 6) |
                                    (Trail1 & 0x3f));

        }

        // now this can either be unescaped high-bit (bad)
        // or escaped high-bit.  (also bad)
        //
        // thus not checking CharToSkip
        //

        else if ((Char & 0x80) == 0x80)
        {
            // high bit set !  bad utf!
            //
            Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
            goto end;

        }
        //
        // Normal character (again either escaped or unescaped)
        //
        else
        {
            //
            // Simple conversion to unicode, it's 7-bit ascii.
            //

            UnicodeChar = (USHORT)Char;
        }

    }
    else // UrlType != UrlTypeUtf8
    {
        UCHAR AnsiChar[2];
        ULONG AnsiCharSize;

        //
        // Convert ANSI character to Unicode.
        // If the UrlType is UrlTypeDbcs, then we may have
        // a DBCS lead/trail pair.
        //

        if (UrlType == UrlTypeDbcs && IS_LEAD_BYTE(Char))
        {
            UCHAR SecondByte;
        
            //
            // This is a double-byte character.
            //

            SecondByte = *(pChar+CharToSkip);

            AnsiCharSize = 2;
            AnsiChar[0] = Char;

            if (SecondByte == '%')
            {
                Status = Unescape((PUCHAR)pChar+CharToSkip, &AnsiChar[1]);
                if (!NT_SUCCESS(Status))
                {
                    goto end;
                }

                CharToSkip += 3; // %xx
            }
            else
            {
                AnsiChar[1] = SecondByte;
                CharToSkip += 1;
            }

        }
        else
        {
            //
            // This is a single-byte character.
            //

            AnsiCharSize = 1;
            AnsiChar[0] = Char;

        }
        /*
        Status = RtlMultiByteToUnicodeN(
                        &UnicodeChar,
                        sizeof(WCHAR),
                        NULL,
                        (PCHAR) &AnsiChar[0],
                        AnsiCharSize
                        );
        */
        Status = MultiByteToWideChar(
            CP_ACP,
            0,
            (PCHAR) &AnsiChar[0],
            AnsiCharSize,
            &UnicodeChar,
            sizeof(WCHAR)
            );

        if (!NT_SUCCESS(Status))
        {
            goto end;
        }
    }


slash:
    //
    // turn backslashes into forward slashes
    //

    if (UrlPart != QueryString && UnicodeChar == L'\\')
    {
        UnicodeChar = L'/';
    }
    else if (UnicodeChar == UNICODE_NULL)
    {
        //
        // we pop'd a NULL.  bad!
        //
        Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
        goto end;
    }

    *pCharToSkip  = CharToSkip;
    *pUnicodeChar = UnicodeChar;
    *pUnicodeChar2 = UnicodeChar2;

    Status = STATUS_SUCCESS;

end:
    return Status;

}   // PopChar


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
// Private globals
//

//
// this table says what to do based on the current state and the current
// character
//
ULONG  pActionTable[16] =
{
    //
    // state 0 = fresh, seen nothing exciting yet
    //
    ACTION_EMIT_CH,         // other = emit it                      state = 0
    ACTION_EMIT_CH,         // "."   = emit it                      state = 0
    ACTION_NOTHING,         // EOS   = normal finish                state = 4
    ACTION_EMIT_CH,         // "/"   = we saw the "/", emit it      state = 1

    //
    // state 1 = we saw a "/" !
    //
    ACTION_EMIT_CH,         // other = emit it,                     state = 0
    ACTION_NOTHING,         // "."   = eat it,                      state = 2
    ACTION_NOTHING,         // EOS   = normal finish                state = 4
    ACTION_NOTHING,         // "/"   = extra slash, eat it,         state = 1

    //
    // state 2 = we saw a "/" and ate a "." !
    //
    ACTION_EMIT_DOT_CH,     // other = emit the dot we ate.         state = 0
    ACTION_NOTHING,         // "."   = eat it, a ..                 state = 3
    ACTION_NOTHING,         // EOS   = normal finish                state = 4
    ACTION_NOTHING,         // "/"   = we ate a "/./", swallow it   state = 1

    //
    // state 3 = we saw a "/" and ate a ".." !
    //
    ACTION_EMIT_DOT_DOT_CH, // other = emit the "..".               state = 0
    ACTION_EMIT_DOT_DOT_CH, // "."   = 3 dots, emit the ".."        state = 0
    ACTION_BACKUP,          // EOS   = we have a "/..\0", backup!   state = 4
    ACTION_BACKUP           // "/"   = we have a "/../", backup!    state = 1
};

//
// this table says which newstate to be in given the current state and the
// character we saw
//
ULONG  pNextStateTable[16] =
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

//
// this says how to index into pNextStateTable given our current state.
//
// since max states = 4, we calculate the index by multiplying with 4.
//
#define IndexFromState( st)   ( (st) * 4)


/***************************************************************************++

Routine Description:

    This function can be told to clean up UTF-8, ANSI, or DBCS URLs.

    Unescape
    Convert backslash to forward slash
    Remove double slashes (empty directiories names) - e.g. // or \\
    Handle /./
    Handle /../
    Convert to unicode
    computes the case insensitive hash

Arguments:


Return Value:

    NTSTATUS - Completion status.


--***************************************************************************/
NTSTATUS
UlpCleanAndCopyUrlByType(
    IN                          URL_TYPE    UrlType,
    IN                          URL_PART    UrlPart,
    __inout                     PWSTR       pDestination,
    __in_ecount(SourceLength)   LPSTR       pSource,
    IN                          ULONG       SourceLength,
    OUT                         PULONG      pBytesCopied,
    __deref_opt_out_opt         PWSTR *     ppQueryString
    )
{
    NTSTATUS Status;
    PWSTR   pDest;
    PUCHAR  pChar;
    ULONG   CharToSkip;
    ULONG   BytesCopied;
    PWSTR   pQueryString;
    ULONG   StateIndex;
    WCHAR   UnicodeChar;
    WCHAR   UnicodeChar2 = L'\0';
    BOOLEAN MakeCanonical;
    PUSHORT pFastPopChar;

    pDest = pDestination;
    pQueryString = NULL;
    BytesCopied = 0;

    pChar = (PUCHAR)pSource;
    CharToSkip = 0;

    StateIndex = 0;

    MakeCanonical = (UrlPart == AbsPath) ? TRUE : FALSE;

    if (UrlType == UrlTypeUtf8 && UrlPart != QueryString)
    {
        pFastPopChar = FastPopChars;
    }
    else
    {
        pFastPopChar = DummyPopChars;
    }

    while (SourceLength > 0)
    {
        //
        // advance !  it's at the top of the loop to enable ANSI_NULL to
        // come through ONCE
        //

        pChar += CharToSkip;
        SourceLength -= CharToSkip;

        //
        // well?  have we hit the end?
        //

        if (SourceLength == 0)
        {
            UnicodeChar = UNICODE_NULL;
            UnicodeChar2 = UNICODE_NULL;
        }
        else
        {
            //
            // Nope.  Peek briefly to see if we hit the query string
            //

            if (UrlPart == AbsPath && pChar[0] == '?')
            {
                DBG_ASSERT(pQueryString == NULL);

                //
                // remember it's location
                //

                pQueryString = pDest;

                //
                // let it fall through ONCE to the canonical
                // in order to handle a trailing "/.." like
                // "http://hostname:80/a/b/..?v=1&v2"
                //

                UnicodeChar = L'?';
                UnicodeChar2 = UNICODE_NULL;
                CharToSkip = 1;

                //
                // now we are cleaning the query string
                //

                UrlPart = QueryString;

                //
                // cannot use fast path for PopChar anymore
                //

                pFastPopChar = DummyPopChars;
            }
            else
            {
                USHORT NextUnicodeChar = pFastPopChar[*pChar];

                //
                // Grab the next character. Try to be fast for the
                // normal character case. Otherwise call PopChar.
                //

                if (NextUnicodeChar == 0)
                {
                    Status = PopChar(
                                    UrlType,
                                    UrlPart,
                                    (LPSTR)pChar,
                                    &UnicodeChar,
                                    &UnicodeChar2,
                                    &CharToSkip
                                    );

                    if (NT_SUCCESS(Status) == FALSE)
                        goto end;
                }
                else
                {
#if DBG
                    Status = PopChar(
                                    UrlType,
                                    UrlPart,
                                    (LPSTR)pChar,
                                    &UnicodeChar,
                                    &UnicodeChar2,
                                    &CharToSkip
                                    );

                    DBG_ASSERT(NT_SUCCESS(Status));
                    DBG_ASSERT(UnicodeChar == NextUnicodeChar);
                    DBG_ASSERT(CharToSkip == 1);
#endif
                    UnicodeChar = NextUnicodeChar;
                    UnicodeChar2 = UNICODE_NULL;
                    CharToSkip = 1;
                }
            }
        }

        if (MakeCanonical)
        {
            //
            // now use the state machine to make it canonical .
            //

            //
            // from the old value of StateIndex, figure out our new base StateIndex
            //
            StateIndex = IndexFromState(pNextStateTable[StateIndex]);

            //
            // did we just hit the query string?  this will only happen once
            // that we take this branch after hitting it, as we stop
            // processing after hitting it.
            //

            if (UrlPart == QueryString)
            {
                //
                // treat this just like we hit a NULL, EOS.
                //

                StateIndex += 2;
            }
            else
            {
                //
                // otherwise based the new state off of the char we
                // just popped.
                //

                switch (UnicodeChar)
                {
                case UNICODE_NULL:      StateIndex += 2;    break;
                case L'.':              StateIndex += 1;    break;
                case L'/':              StateIndex += 3;    break;
                default:                StateIndex += 0;    break;
                }
            }

        }
        else
        {
            StateIndex = (UnicodeChar == UNICODE_NULL) ? 2 : 0;
        }

        //
        //  Perform the action associated with the state.
        //

        switch (pActionTable[StateIndex])
        {
        case ACTION_EMIT_DOT_DOT_CH:

            EMIT_CHAR(L'.');

            // fall through

        case ACTION_EMIT_DOT_CH:

            EMIT_CHAR(L'.');

            // fall through

        case ACTION_EMIT_CH:

            EMIT_CHAR(UnicodeChar);
            if (UnicodeChar2 != UNICODE_NULL)
            {
                EMIT_CHAR(UnicodeChar2);
            }

            // fall through

        case ACTION_NOTHING:
            break;

        case ACTION_BACKUP:

            //
            // pDest currently points 1 past the last '/'.  backup over it and
            // find the preceding '/', set pDest to 1 past that one.
            //

            //
            // backup to the '/'
            //

            pDest       -= 1;
            BytesCopied -= 2;

            DBG_ASSERT(pDest[0] == L'/');

            //
            // are we at the start of the string?  that's bad, can't go back!
            //

            if (pDest == pDestination)
            {
                DBG_ASSERT(BytesCopied == 0);
                Status = STATUS_OBJECT_PATH_INVALID;
                goto end;
            }

            //
            // back up over the '/'
            //

            pDest       -= 1;
            BytesCopied -= 2;

            DBG_ASSERT(pDest > pDestination);

            //
            // now find the previous slash
            //

            while (pDest > pDestination && pDest[0] != L'/')
            {
                pDest       -= 1;
                BytesCopied -= 2;
            }

            //
            // we already have a slash, so don't have to store 1.
            //

            DBG_ASSERT(pDest[0] == L'/');

            //
            // simply skip it, as if we had emitted it just now
            //

            pDest       += 1;
            BytesCopied += 2;

            break;

        default:
            DBG_ASSERT(!"http!UlpCleanAndCopyUrl: Invalid action code in state table!");
            Status = STATUS_OBJECT_PATH_SYNTAX_BAD;
            goto end;
        }

        //
        // Just hit the query string ?
        //

        if (MakeCanonical && UrlPart == QueryString)
        {
            //
            // Stop canonical processing
            //

            MakeCanonical = FALSE;

            //
            // Need to emit the '?', it wasn't emitted above
            //

            DBG_ASSERT(pActionTable[StateIndex] != ACTION_EMIT_CH);

            EMIT_CHAR(L'?');

        }

    }

    //
    // terminate the string, it hasn't been done in the loop
    //

    DBG_ASSERT((pDest-1)[0] != UNICODE_NULL);

    pDest[0] = UNICODE_NULL;
    *pBytesCopied = BytesCopied;

    if (ppQueryString != NULL)
    {
        *ppQueryString = pQueryString;
    }

    Status = STATUS_SUCCESS;


end:
    return Status;

}   // UlpCleanAndCopyUrlByType


/***************************************************************************++

Routine Description:


    Unescape
    Convert backslash to forward slash
    Remove double slashes (empty directiories names) - e.g. // or \\
    Handle /./
    Handle /../
    Convert to unicode

Arguments:

Return Value:

    HRESULT 


--***************************************************************************/
HRESULT
UlCleanAndCopyUrl(
    __in                    LPSTR       pSource,
    IN                      ULONG       SourceLength,
    OUT                     PULONG      pBytesCopied,
    __inout                 PWSTR       pDestination,
    __deref_opt_out_opt     PWSTR *     ppQueryString OPTIONAL
    )
{
    NTSTATUS Status;
    URL_TYPE AnsiUrlType = g_UlEnableDBCS ? UrlTypeDbcs : UrlTypeAnsi;

    if (!g_UlEnableNonUTF8)
    {
        //
        // Only accept UTF-8 URLs.
        //

        Status = UlpCleanAndCopyUrlByType(
                        UrlTypeUtf8,
                        AbsPath,
                        pDestination,
                        pSource,
                        SourceLength,
                        pBytesCopied,
                        ppQueryString
                        );

    }
    else if (!g_UlFavorDBCS)
    {
        //
        // The URL may be either UTF-8 or ANSI. First
        // try UTF-8, and if that fails go for ANSI.
        //

        Status = UlpCleanAndCopyUrlByType(
                        UrlTypeUtf8,
                        AbsPath,
                        pDestination,
                        pSource,
                        SourceLength,
                        pBytesCopied,
                        ppQueryString
                        );

        if (!NT_SUCCESS(Status))
        {
            Status = UlpCleanAndCopyUrlByType(
                            AnsiUrlType,
                            AbsPath,
                            pDestination,
                            pSource,
                            SourceLength,
                            pBytesCopied,
                            ppQueryString
                            );

        }

    }
    else
    {
        //
        // The URL may be either ANSI or UTF-8. First
        // try the ANSI interpretation, and if that fails
        // go for UTF-8.
        //
        Status = UlpCleanAndCopyUrlByType(
                        AnsiUrlType,
                        AbsPath,
                        pDestination,
                        pSource,
                        SourceLength,
                        pBytesCopied,
                        ppQueryString
                        );

        if (!NT_SUCCESS(Status))
        {
            Status = UlpCleanAndCopyUrlByType(
                            UrlTypeUtf8,
                            AbsPath,
                            pDestination,
                            pSource,
                            SourceLength,
                            pBytesCopied,
                            ppQueryString
                            );

        }
    }
    
    //
    // Convert NTSTATUS to HRESULT
    //
    
    if ( Status == STATUS_SUCCESS )            
    {
        return S_OK;
    }
    else
    {
        DWORD dwErr = 0;
        if (SUCCEEDED(WIN32_FROM_NTSTATUS( Status, &dwErr )))
        {
            return HRESULT_FROM_WIN32( dwErr );
        }
        else
        {
            return Status;
        }
    }
}

HRESULT
UlInitializeParsing(
    VOID
)
{
    ULONG           i;
    UCHAR           c;
    HKEY            hKey;
    DWORD           dwType;
    DWORD           dwData;
    DWORD           cbData;

    //
    // First read the HTTP registry settings on how to handle URLs
    //
    
    g_UlEnableNonUTF8 = TRUE;
    g_UlEnableDBCS = FALSE;
    g_UlFavorDBCS = FALSE;
    
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
            g_UlEnableNonUTF8 = !!dwData;
        }
        
        cbData = sizeof( dwData );
        
        if ( g_UlEnableNonUTF8 )
        {
            if ( RegQueryValueEx( hKey,
                                  L"EnableDBCS",
                                  NULL,
                                  &dwType,
                                  (LPBYTE) &dwData,
                                  &cbData ) == ERROR_SUCCESS &&
                 dwType == REG_DWORD )
            {
                g_UlEnableDBCS = !!dwData;
            }
        }
        else
        {
            g_UlEnableDBCS = FALSE;
        }
        
        cbData = sizeof( dwData );
        
        if ( g_UlEnableDBCS )
        {
            if ( RegQueryValueEx( hKey,
                                  L"FavorDBCS",
                                  NULL,
                                  &dwType,
                                  (LPBYTE) &dwData,
                                  &cbData ) == ERROR_SUCCESS &&
                 dwType == REG_DWORD )
            {
                g_UlFavorDBCS = !!dwData;
            }
        }
        else
        {
            g_UlFavorDBCS = FALSE;
        }
        
        RegCloseKey( hKey );
    }
    

    // Initialize the HttpChars array appropriately.

    for (i = 0; i < 128; i++)
    {
        HttpChars[i] = HTTP_CHAR;
    }

    for (i = 'A'; i <= 'Z'; i++)
    {
        HttpChars[i] |= HTTP_UPCASE;
    }

    for (i = 'a'; i <= 'z'; i++)
    {
        HttpChars[i] |= HTTP_LOCASE;
    }

    for (i = '0'; i <= '9'; i++)
    {
        HttpChars[i] |= (HTTP_DIGIT | HTTP_HEX);
    }


    for (i = 0; i <= 31; i++)
    {
        HttpChars[i] |= HTTP_CTL;
    }

    HttpChars[127] |= HTTP_CTL;

    HttpChars[SP] |= HTTP_LWS;
    HttpChars[HT] |= HTTP_LWS;


    for (i = 'A'; i <= 'F'; i++)
    {
        HttpChars[i] |= HTTP_HEX;
    }

    for (i = 'a'; i <= 'f'; i++)
    {
        HttpChars[i] |= HTTP_HEX;
    }

    HttpChars['('] |= HTTP_SEPERATOR;
    HttpChars[')'] |= HTTP_SEPERATOR;
    HttpChars['<'] |= HTTP_SEPERATOR;
    HttpChars['>'] |= HTTP_SEPERATOR;
    HttpChars['@'] |= HTTP_SEPERATOR;
    HttpChars[','] |= HTTP_SEPERATOR;
    HttpChars[';'] |= HTTP_SEPERATOR;
    HttpChars[':'] |= HTTP_SEPERATOR;
    HttpChars['\\'] |= HTTP_SEPERATOR;
    HttpChars['"'] |= HTTP_SEPERATOR;
    HttpChars['/'] |= HTTP_SEPERATOR;
    HttpChars['['] |= HTTP_SEPERATOR;
    HttpChars[']'] |= HTTP_SEPERATOR;
    HttpChars['?'] |= HTTP_SEPERATOR;
    HttpChars['='] |= HTTP_SEPERATOR;
    HttpChars['{'] |= HTTP_SEPERATOR;
    HttpChars['}'] |= HTTP_SEPERATOR;
    HttpChars[SP] |= HTTP_SEPERATOR;
    HttpChars[HT] |= HTTP_SEPERATOR;


    //
    // URL "reserved" characters (rfc2396)
    //

    HttpChars[';'] |= URL_LEGAL;
    HttpChars['/'] |= URL_LEGAL;
    HttpChars['\\'] |= URL_LEGAL;
    HttpChars['?'] |= URL_LEGAL;
    HttpChars[':'] |= URL_LEGAL;
    HttpChars['@'] |= URL_LEGAL;
    HttpChars['&'] |= URL_LEGAL;
    HttpChars['='] |= URL_LEGAL;
    HttpChars['+'] |= URL_LEGAL;
    HttpChars['$'] |= URL_LEGAL;
    HttpChars[','] |= URL_LEGAL;


    //
    // URL escape character
    //

    HttpChars['%'] |= URL_LEGAL;

    //
    // URL "mark" characters (rfc2396)
    //

    HttpChars['-'] |= URL_LEGAL;
    HttpChars['_'] |= URL_LEGAL;
    HttpChars['.'] |= URL_LEGAL;
    HttpChars['!'] |= URL_LEGAL;
    HttpChars['~'] |= URL_LEGAL;
    HttpChars['*'] |= URL_LEGAL;
    HttpChars['\''] |= URL_LEGAL;
    HttpChars['('] |= URL_LEGAL;
    HttpChars[')'] |= URL_LEGAL;


    //
    // RFC2396 describes these characters as `unwise' "because gateways and
    // other transport agents are known to sometimes modify such characters,
    // or they are used as delimiters". However, for compatibility with
    // IIS 5.0 and DAV, we must allow these unwise characters in URLs.
    //

    HttpChars['{'] |= URL_LEGAL;
    HttpChars['}'] |= URL_LEGAL;
    HttpChars['|'] |= URL_LEGAL;
    HttpChars['^'] |= URL_LEGAL;
    HttpChars['['] |= URL_LEGAL;
    HttpChars[']'] |= URL_LEGAL;
    HttpChars['`'] |= URL_LEGAL;

    //
    // '#', '%', and '"' are not considered URL_LEGAL, according to the RFC.
    // However, IIS 5.0 allowed them, so we should too.
    //
    
    HttpChars['#'] |= URL_LEGAL;
    HttpChars['%'] |= URL_LEGAL;
    HttpChars['"'] |= URL_LEGAL;

    //
    // In DBCS locales we need to explicitly accept lead bytes which
    // we would normally reject.
    //

    if (0)      // BUGBUG
    {
        for (i = 0; i < DBCS_TABLE_SIZE; i++)
        {
            if (IS_LEAD_BYTE((BYTE)i))
            {
                HttpChars[i] |= URL_LEGAL;
            }
        }
    }

    //
    // These US-ASCII characters are "excluded"; i.e. not URL_LEGAL (see RFC):
    //      '<' | '>' | ' ' (0x20)
    // In addition, control characters (0x00-0x1F and 0x7F) and
    // non US-ASCII characters (0x80-0xFF) are not URL_LEGAL.
    //

    for (i = 0; i < 128; i++)
    {
        if (!IS_HTTP_SEPERATOR(i) && !IS_HTTP_CTL(i))
        {
            HttpChars[i] |= HTTP_TOKEN;
        }
    }


    //
    // Fast path for PopChar
    //

    RtlZeroMemory(FastPopChars, 256 * sizeof(USHORT));
    RtlZeroMemory(DummyPopChars, 256 * sizeof(USHORT));

    for (i = 0; i < 256; i++)
    {
        c = (UCHAR)i;

        if (IS_URL_TOKEN(c) && c != '%' && (c & 0x80) != 0x80)
        {
            FastPopChars[i] = (USHORT)c;
        }
    }

    //
    // Turn backslashes into forward slashes
    //

    FastPopChars['\\'] = L'/';


    //
    // Fast path for UpcaseUnicodeChar
    //

    for (i = 0; i < 256; i++)
    {
        FastUpcaseChars[i] = towupper((WCHAR)i);
    }


    return S_OK;
}
