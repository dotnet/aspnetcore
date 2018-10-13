// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _BASE64_HXX_
#define _BASE64_HXX_

DWORD
Base64Encode(
    __in_bcount( cbDecodedBufferSize ) VOID *   pDecodedBuffer,
    IN      DWORD                               cbDecodedBufferSize,
    __out_ecount_opt( cchEncodedStringSize ) PWSTR  pszEncodedString,
    IN      DWORD                               cchEncodedStringSize,
    __out_opt DWORD *                           pcchEncoded
    );

DWORD
Base64Decode(
    __in    PCWSTR                              pszEncodedString,
    __out_opt   VOID *                              pDecodeBuffer,
    __in    DWORD                               cbDecodeBufferSize,
    __out_opt DWORD *                           pcbDecoded
    );

DWORD
Base64Encode(
    __in_bcount( cbDecodedBufferSize ) VOID *   pDecodedBuffer,
    IN      DWORD                               cbDecodedBufferSize,
    __out_ecount_opt( cchEncodedStringSize ) PSTR   pszEncodedString,
    IN      DWORD                               cchEncodedStringSize,
    __out_opt DWORD *                           pcchEncoded
    );

DWORD
Base64Decode(
    __in    PCSTR                               pszEncodedString,
    __out_opt   VOID *                              pDecodeBuffer,
    __in    DWORD                               cbDecodeBufferSize,
    __out_opt DWORD *                           pcbDecoded
    );

#endif // _BASE64_HXX_

