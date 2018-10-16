// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef __NORMALIZE_URL__H__
#define __NORMALIZE_URL__H__

HRESULT
NormalizeUrl(
    __inout LPSTR    pszUrl
);


HRESULT
NormalizeUrlW(
    __inout LPWSTR    pszUrl
);



HRESULT
UlCleanAndCopyUrl(
    __in                    LPSTR       pSource,
    IN                      ULONG       SourceLength,
    OUT                     PULONG      pBytesCopied,
    __inout                 PWSTR       pDestination,
    __deref_opt_out_opt     PWSTR *     ppQueryString OPTIONAL
);

HRESULT
UlInitializeParsing(
    VOID
);

HRESULT
InitializeNormalizeUrl(
    VOID
);


#endif