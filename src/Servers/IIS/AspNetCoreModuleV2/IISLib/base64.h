// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _BASE64_H_
#define _BASE64_H_
#include <ahadmin.h>
#include <intsafe.h>

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

