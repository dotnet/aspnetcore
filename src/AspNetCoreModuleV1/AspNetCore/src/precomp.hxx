#pragma once

/*++

Copyright (c) 2013 Microsoft Corporation

Module Name:

    precomp.hxx

Abstract:

    Precompiled header.

--*/

//
// System related headers
//

#define _WIN32_WINNT 0x0600
#define _WINSOCKAPI_
#include <windows.h>
#include <atlbase.h>
#include <pdh.h>
#include <Shlobj.h>

#include <ntassert.h>

#include <httpserv.h>
#include <iiswebsocket.h>
#include <httptrace.h>

#include <winhttp.h>

//
// Option available starting Windows 8.
// 111 is the value in SDK on May 15, 2012.
//
#ifndef WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS
#define WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS 111
#endif

#define ASPNETCORE_EVENT_PROVIDER L"IIS AspNetCore Module"
#define ASPNETCORE_IISEXPRESS_EVENT_PROVIDER L"IIS Express AspNetCore Module"

#define TIMESPAN_IN_MILLISECONDS(x)  ((x)/((LONGLONG)(10000)))
#define TIMESPAN_IN_SECONDS(x)       ((TIMESPAN_IN_MILLISECONDS(x))/((LONGLONG)(1000)))
#define TIMESPAN_IN_MINUTES(x)       ((TIMESPAN_IN_SECONDS(x))/((LONGLONG)(60)))

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
#include <stringa.h>
#include <stringu.h>
#include <treehash.h>

#include <dbgutil.h>
#include "ahutil.h"
#include "multisz.hxx"
#include "multisza.hxx"
#include "sttimer.h"
#include <listentry.h>
#include <base64.hxx>
#include <datetime.h>
#include <reftrace.h>
#include <acache.h>
#include <time.h>

#include "filewatcher.h"
#include "environmentvariablehash.h"
#include "aspnetcore_msg.h"
#include "aspnetcoreconfig.h"
#include "serverprocess.h"
#include "processmanager.h"
#include "application.h"
#include "applicationmanager.h"
#include "resource.h"
#include "path.h"
#include "debugutil.h"
#include "protocolconfig.h"
#include "responseheaderhash.h"
#include "forwarderconnection.h"
#include "winhttphelper.h"
#include "websockethandler.h"
#include "forwardinghandler.h"
#include "proxymodule.h"

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

extern BOOL     g_fAsyncDisconnectAvailable;
extern PVOID    g_pModuleId;
extern BOOL     g_fWebSocketSupported;
extern BOOL     g_fEnableReferenceCountTracing;
extern DWORD    g_dwActiveServerProcesses;