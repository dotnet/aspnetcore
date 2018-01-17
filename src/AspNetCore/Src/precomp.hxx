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

//#include <ntassert.h>
#include <Shlobj.h>
#include <httpserv.h>

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

#define NTDDI_VERSION 0x06010000
#define WINVER 0x0601
#define _WIN32_WINNT 0x0601

#include <httptrace.h>
#include <winhttp.h>

#include <cstdlib>
#include <vector>
#include <wchar.h>

//
// Option available starting Windows 8.
// 111 is the value in SDK on May 15, 2012.
//
#ifndef WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS
#define WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS 111
#endif

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
    switch(ch)
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

#include <hashfn.h>
#include <hashtable.h>
#include "stringa.h"
#include "stringu.h"
#include "dbgutil.h"
#include "ahutil.h"
#include "multisz.h"
#include "multisza.h"
#include "base64.h"
#include <listentry.h>
#include <datetime.h>
#include <reftrace.h>
#include <acache.h>
#include <time.h>

#include "..\..\CommonLib\environmentvariablehash.h"
#include "..\..\CommonLib\aspnetcoreconfig.h"
#include "..\..\CommonLib\hostfxr_utility.h"
#include "..\..\CommonLib\application.h"
#include "..\..\CommonLib\utility.h"
#include "..\..\CommonLib\debugutil.h"
#include "..\..\CommonLib\requesthandler.h"
#include "..\..\CommonLib\resources.h"
#include "..\..\CommonLib\aspnetcore_msg.h"
//#include "aspnetcore_event.h"
#include "appoffline.h"
#include "filewatcher.h"
#include "applicationinfo.h"
#include "applicationmanager.h"
#include "globalmodule.h"
#include "proxymodule.h"
#include "applicationinfo.h"


FORCEINLINE
DWORD
WIN32_FROM_HRESULT(
    HRESULT hr
)
{
    if ((FAILED(hr)) &&
        (HRESULT_FACILITY(hr) == FACILITY_WIN32))
    {
        return HRESULT_CODE(hr);
    }
    return hr;
}

FORCEINLINE
HRESULT
HRESULT_FROM_GETLASTERROR()
{
    return  ( GetLastError() != NO_ERROR ) 
           ? HRESULT_FROM_WIN32( GetLastError() )
           : E_FAIL;
}

extern PVOID        g_pModuleId;
extern BOOL         g_fAspnetcoreRHAssemblyLoaded;
extern BOOL         g_fAspnetcoreRHLoadedError;
extern BOOL         g_fEnableReferenceCountTracing;
extern DWORD        g_dwActiveServerProcesses;
extern HINSTANCE    g_hModule;
extern HMODULE      g_hAspnetCoreRH;
extern SRWLOCK      g_srwLock;
extern PCWSTR       g_pwzAspnetcoreRequestHandlerName;
extern HANDLE       g_hEventLog;
extern PFN_ASPNETCORE_CREATE_APPLICATION      g_pfnAspNetCoreCreateApplication;
extern PFN_ASPNETCORE_CREATE_REQUEST_HANDLER  g_pfnAspNetCoreCreateRequestHandler;
#pragma warning( error : 4091)
