// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.h"

DWORD
Base64Encode(
    __in_bcount(cbDecodedBufferSize)    VOID *  pDecodedBuffer,
    IN      DWORD       cbDecodedBufferSize,
    __out_ecount_opt(cchEncodedStringSize) PWSTR    pszEncodedString,
    IN      DWORD       cchEncodedStringSize,
    __out_opt DWORD *   pcchEncoded
    )
/*++

Routine Description:

    Decode a base64-encoded string.

Arguments:

    pDecodedBuffer (IN) - buffer to encode.
    cbDecodedBufferSize (IN) - size of buffer to encode.
    cchEncodedStringSize (IN) - size of the buffer for the encoded string.
    pszEncodedString (OUT) = the encoded string.
    pcchEncoded (OUT) - size in characters of the encoded string.

Return Values:

    0 - success.
    E_OUTOFMEMORY

--*/
{
    static WCHAR rgchEncodeTable[64] = {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
    };

    DWORD   ib{0};
    DWORD   ich{0};
    DWORD   cchEncoded{0};
    BYTE    b0{0}, b1{0}, b2{0};
    BYTE *  pbDecodedBuffer = static_cast<BYTE*>(pDecodedBuffer);

    // Calculate encoded string size.
    cchEncoded = 1 + (cbDecodedBufferSize + 2) / 3 * 4;

    if (nullptr != pcchEncoded) {
        *pcchEncoded = cchEncoded;
    }

    if (cchEncodedStringSize == 0 && pszEncodedString == nullptr) {
        return ERROR_SUCCESS;
    }
    else if (pszEncodedString == nullptr)
    {
        return ERROR_INVALID_PARAMETER;
    }

    *pszEncodedString = 0;

    if (cchEncodedStringSize < cchEncoded) {
        // Given buffer is too small to hold encoded string.
        return ERROR_INSUFFICIENT_BUFFER;
    }

    // Encode data byte triplets into four-byte clusters.
    ib = ich = 0;
    while (ib < cbDecodedBufferSize) {
        b0 = pbDecodedBuffer[ib++];
        b1 = 0;
        b2 = 0;
        if (ib < cbDecodedBufferSize)
        {
            b1 = pbDecodedBuffer[ib++];
        }
        if (ib < cbDecodedBufferSize)
        {
            b2 = pbDecodedBuffer[ib++];
        }

        //
        // The checks below for buffer overflow seems redundant to me.
        // But it's the only way I can find to keep OACR quiet so it
        // will have to do.
        //

        pszEncodedString[ich++] = rgchEncodeTable[b0 >> 2];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }

        pszEncodedString[ich++] = rgchEncodeTable[((b0 << 4) & 0x30) | ((b1 >> 4) & 0x0f)];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }

        pszEncodedString[ich++] = rgchEncodeTable[((b1 << 2) & 0x3c) | ((b2 >> 6) & 0x03)];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }

        pszEncodedString[ich++] = rgchEncodeTable[b2 & 0x3f];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }
    }

    // Pad the last cluster as necessary to indicate the number of data bytes
    // it represents.
    switch (cbDecodedBufferSize % 3) {
      case 0:
        break;
      case 1:
        pszEncodedString[ich - 2] = '=';
        __fallthrough;
      case 2:
        pszEncodedString[ich - 1] = '=';
        break;
    }

    // Null-terminate the encoded string.
    pszEncodedString[ich++] = '\0';

    DBG_ASSERT(ich == cchEncoded);

    return ERROR_SUCCESS;
}


DWORD
Base64Encode(
    __in_bcount(cbDecodedBufferSize)    VOID *  pDecodedBuffer,
    IN      DWORD       cbDecodedBufferSize,
    __out_ecount_opt(cchEncodedStringSize) PSTR     pszEncodedString,
    IN      DWORD       cchEncodedStringSize,
    __out_opt DWORD *   pcchEncoded
    )
/*++

Routine Description:

    Decode a base64-encoded string.

Arguments:

    pDecodedBuffer (IN) - buffer to encode.
    cbDecodedBufferSize (IN) - size of buffer to encode.
    cchEncodedStringSize (IN) - size of the buffer for the encoded string.
    pszEncodedString (OUT) = the encoded string.
    pcchEncoded (OUT) - size in characters of the encoded string.

Return Values:

    0 - success.
    E_OUTOFMEMORY

--*/
{
    static CHAR rgchEncodeTable[64] = {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
    };

    DWORD   ib{0};
    DWORD   ich{0};
    DWORD   cchEncoded{0};
    BYTE    b0{0}, b1{0}, b2{0};
    BYTE *  pbDecodedBuffer = (BYTE *) pDecodedBuffer;

    // Calculate encoded string size.
    cchEncoded = 1 + (cbDecodedBufferSize + 2) / 3 * 4;

    if (nullptr != pcchEncoded) {
        *pcchEncoded = cchEncoded;
    }

    if (cchEncodedStringSize == 0 && pszEncodedString == nullptr) {
        return ERROR_SUCCESS;
    }
    else if (pszEncodedString == nullptr)
    {
        return ERROR_INVALID_PARAMETER;
    }

    *pszEncodedString = 0;

    if (cchEncodedStringSize < cchEncoded) {
        // Given buffer is too small to hold encoded string.
        return ERROR_INSUFFICIENT_BUFFER;
    }

    // Encode data byte triplets into four-byte clusters.
    ib = ich = 0;
    while (ib < cbDecodedBufferSize) {
        b0 = pbDecodedBuffer[ib++];
        b1 = 0;
        b2 = 0;
        if (ib < cbDecodedBufferSize)
        {
            b1 = pbDecodedBuffer[ib++];
        }
        if (ib < cbDecodedBufferSize)
        {
            b2 = pbDecodedBuffer[ib++];
        }

        //
        // The checks below for buffer overflow seems redundant to me.
        // But it's the only way I can find to keep OACR quiet so it
        // will have to do.
        //

        pszEncodedString[ich++] = rgchEncodeTable[b0 >> 2];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }

        pszEncodedString[ich++] = rgchEncodeTable[((b0 << 4) & 0x30) | ((b1 >> 4) & 0x0f)];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }

        pszEncodedString[ich++] = rgchEncodeTable[((b1 << 2) & 0x3c) | ((b2 >> 6) & 0x03)];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }

        pszEncodedString[ich++] = rgchEncodeTable[b2 & 0x3f];
        if ( ich >= cchEncodedStringSize )
        {
            DBG_ASSERT( FALSE );
            return ERROR_BUFFER_OVERFLOW;
        }
    }

    // Pad the last cluster as necessary to indicate the number of data bytes
    // it represents.
    switch (cbDecodedBufferSize % 3) {
      case 0:
        break;
      case 1:
        pszEncodedString[ich - 2] = '=';
        __fallthrough;
      case 2:
        pszEncodedString[ich - 1] = '=';
        break;
    }

    // Null-terminate the encoded string.
    pszEncodedString[ich++] = '\0';

    DBG_ASSERT(ich == cchEncoded);

    return ERROR_SUCCESS;
}
