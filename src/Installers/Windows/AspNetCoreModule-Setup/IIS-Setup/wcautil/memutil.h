// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
//-------------------------------------------------------------------------------------------------
// <summary>
//    Header for memory helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseMem(p) if (p) { MemFree(p); }
#define ReleaseNullMem(p) if (p) { MemFree(p); p = NULL; }


HRESULT DAPI MemInitialize();
void DAPI MemUninitialize();

LPVOID DAPI MemAlloc(
    __in SIZE_T cbSize,
    __in BOOL fZero
    );
LPVOID DAPI MemReAlloc(
    __in LPVOID pv,
    __in SIZE_T cbSize,
    __in BOOL fZero
    );

HRESULT DAPI MemFree(
    __in LPVOID pv
    );
SIZE_T DAPI MemSize(
    __in LPVOID pv
    );

#ifdef __cplusplus
}
#endif

