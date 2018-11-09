// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
//-------------------------------------------------------------------------------------------------
// <summary>
//    Header for string helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseStr(pwz) if (pwz) { StrFree(pwz); }
#define ReleaseNullStr(pwz) if (pwz) { StrFree(pwz); pwz = NULL; }
#define ReleaseBSTR(bstr) if (bstr) { ::SysFreeString(bstr); }
#define ReleaseNullBSTR(bstr) if (bstr) { ::SysFreeString(bstr); bstr = NULL; }

HRESULT DAPI StrAlloc(
    __inout LPWSTR* ppwz,
    __in DWORD_PTR cch
    );
HRESULT DAPI StrAnsiAlloc(
    __inout LPSTR* ppz,
    __in DWORD_PTR cch
    );
HRESULT DAPI StrAllocString(
    __inout LPWSTR* ppwz,
    __in LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    );
HRESULT DAPI StrAnsiAllocString(
    __inout LPSTR* ppsz,
    __in LPCWSTR wzSource,
    __in DWORD_PTR cchSource,
    __in UINT uiCodepage
    );
HRESULT DAPI StrAllocStringAnsi(
    __inout LPWSTR* ppwz,
    __in LPCSTR szSource,
    __in DWORD_PTR cchSource,
    __in UINT uiCodepage
    );
HRESULT DAPI StrAllocPrefix(
    __inout LPWSTR* ppwz,
    __in LPCWSTR wzPrefix,
    __in DWORD_PTR cchPrefix
    );
HRESULT DAPI StrAllocConcat(
    __inout LPWSTR* ppwz,
    __in LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    );
HRESULT __cdecl StrAllocFormatted(
    __inout LPWSTR* ppwz,
    __in LPCWSTR wzFormat,
    ...
    );
HRESULT __cdecl StrAnsiAllocFormatted(
    __inout LPSTR* ppsz,
    __in LPCSTR szFormat,
    ...
    );
HRESULT DAPI StrAllocFormattedArgs(
    __inout LPWSTR* ppwz,
    __in LPCWSTR wzFormat,
    __in va_list args
    );
HRESULT DAPI StrAnsiAllocFormattedArgs(
    __inout LPSTR* ppsz,
    __in LPCSTR szFormat,
    __in va_list args
    );

HRESULT DAPI StrMaxLength(
    __in LPVOID p,
    __out DWORD_PTR* pcch
    );
HRESULT DAPI StrSize(
    __in LPVOID p,
    __out DWORD_PTR* pcb
    );

HRESULT DAPI StrFree(
    __in LPVOID p
    );

HRESULT DAPI StrCurrentTime(
    __inout LPWSTR* ppwz,
    __in BOOL fGMT
    );
HRESULT DAPI StrCurrentDateTime(
    __inout LPWSTR* ppwz,
    __in BOOL fGMT
    );

HRESULT DAPI StrHexEncode(
    __in_ecount(cbSource) const BYTE* pbSource,
    __in DWORD_PTR cbSource,
    __out_ecount(cchDest) LPWSTR wzDest,
    __in DWORD_PTR cchDest
    );
HRESULT DAPI StrHexDecode(
    __in LPCWSTR wzSource,
    __out_bcount(cbDest) BYTE* pbDest,
    __in DWORD_PTR cbDest
    );

HRESULT DAPI StrAllocBase85Encode(
    __in_bcount(cbSource) const BYTE* pbSource,
    __in DWORD_PTR cbSource,
    __inout LPWSTR* pwzDest
    );
HRESULT DAPI StrAllocBase85Decode(
    __in LPCWSTR wzSource,
    __out BYTE** hbDest,
    __out DWORD_PTR* pcbDest
    );

HRESULT DAPI MultiSzLen(
    __in LPCWSTR pwzMultiSz,
    __out DWORD_PTR* pcch
    );
HRESULT DAPI MultiSzPrepend(
    __inout LPWSTR* ppwzMultiSz,
    __inout_opt DWORD_PTR *pcchMultiSz,
    __in LPCWSTR pwzInsert
    );
HRESULT DAPI MultiSzFindSubstring(
    __in LPCWSTR pwzMultiSz,
    __in LPCWSTR pwzSubstring,
    __out_opt DWORD_PTR* pdwIndex,
    __out_opt LPCWSTR* ppwzFoundIn
    );
HRESULT DAPI MultiSzFindString(
    __in LPCWSTR pwzMultiSz,
    __in LPCWSTR pwzString,
    __out DWORD_PTR* pdwIndex,
    __out LPCWSTR* ppwzFound
    );
HRESULT DAPI MultiSzRemoveString(
    __inout LPWSTR* ppwzMultiSz,
    __in DWORD_PTR dwIndex
    );
HRESULT DAPI MultiSzInsertString(
    __inout LPWSTR* ppwzMultiSz,
    __inout_opt DWORD_PTR *pcchMultiSz,
    __in DWORD_PTR dwIndex,
    __in LPCWSTR pwzInsert
    );
HRESULT DAPI MultiSzReplaceString(
    __inout LPWSTR* ppwzMultiSz,
    __in DWORD_PTR dwIndex,
    __in LPCWSTR pwzString
    );

LPCWSTR wcsistr(
    IN LPCWSTR wzString,
    IN LPCWSTR wzCharSet
    );

#ifdef __cplusplus
}
#endif
