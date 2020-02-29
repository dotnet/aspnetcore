// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#pragma warning( disable : 4091)

//
// System related headers
//
#define _WINSOCKAPI_

#define NTDDI_VERSION 0x06010000
#define WINVER 0x0601
#define _WIN32_WINNT 0x0601

#include <windows.h>
#include <atlbase.h>
#include <pdh.h>
#include <vector>
#include <Shlobj.h>
#include <httpserv.h>
#include <winhttp.h>
#include <httptrace.h>
#include <cstdlib>
#include <reftrace.h>
#include <wchar.h>
#include <io.h>
#include <stdio.h>
// This should remove our issue of compiling for win7 without header files.
// We  force the Windows 8 version check logic in iiswebsocket.h to succeed even though we're compiling for Windows 7.
// Then, we set the version defines back to Windows 7 to for the remainder of the compilation.
#undef NTDDI_VERSION
#undef WINVER
#undef _WIN32_WINNT
#define NTDDI_VERSION 0x06020000
#define WINVER 0x0602
#define _WIN32_WINNT 0x0602
#include <iiswebsocket.h>
#undef NTDDI_VERSION
#undef WINVER
#undef _WIN32_WINNT

#include <acache.h>
#include <exceptions.h>
#include <SRWExclusiveLock.h>

#define NTDDI_VERSION 0x06010000
#define WINVER 0x0601
#define _WIN32_WINNT 0x0601

#ifdef max
#undef max
template<typename T> inline T max(T a, T b)
{
    return a > b ? a : b;
}
#endif

#ifdef min
#undef min
template<typename T> inline T min(T a, T b)
{
    return a < b ? a : b;
}
#endif


inline bool IsSpace(char ch)
{
    switch (ch)
    {
    case 32: // ' '
    case 9:  // '\t'
    case 10: // '\n'
    case 13: // '\r'
    case 11: // '\v'
    case 12: // '\f'
        return true;
    default:
        return false;
    }
}

extern BOOL       g_fAsyncDisconnectAvailable;
extern BOOL       g_fEnableReferenceCountTracing;
extern BOOL       g_fProcessDetach;
extern SRWLOCK    g_srwLockRH;
extern HANDLE     g_hEventLog;
