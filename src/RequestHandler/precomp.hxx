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

#define NTDDI_VERSION 0x06010000
#define WINVER 0x0601
#define _WIN32_WINNT 0x0601

#include "..\IISLib\acache.h"
#include "..\IISLib\multisz.h"
#include "..\IISLib\multisza.h"
#include "..\IISLib\base64.h"
#include "..\IISLib\listentry.h"
#include "..\CommonLib\fx_ver.h"
#include "..\CommonLib\debugutil.h"
#include "..\CommonLib\requesthandler.h"
#include "..\CommonLib\aspnetcoreconfig.h"
#include "..\CommonLib\utility.h"
#include "..\CommonLib\application.h"
#include "..\CommonLib\resources.h"
#include "aspnetcore_event.h"
#include "aspnetcore_msg.h"
#include "disconnectcontext.h"
#include "sttimer.h"
#include ".\inprocess\InProcessHandler.h"
#include ".\inprocess\inprocessapplication.h"
#include ".\outofprocess\responseheaderhash.h"
#include ".\outofprocess\protocolconfig.h"
#include ".\outofprocess\forwarderconnection.h"
#include ".\outofprocess\serverprocess.h"
#include ".\outofprocess\processmanager.h"
#include ".\outofprocess\websockethandler.h"
#include ".\outofprocess\forwardinghandler.h"
#include ".\outofprocess\outprocessapplication.h"
#include ".\outofprocess\winhttphelper.h"

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
extern BOOL       g_fWinHttpNonBlockingCallbackAvailable;
extern BOOL       g_fWebSocketSupported;
extern BOOL       g_fNsiApiNotSupported;
extern BOOL       g_fEnableReferenceCountTracing;
extern DWORD      g_dwActiveServerProcesses;
extern DWORD      g_OptionalWinHttpFlags;
extern SRWLOCK    g_srwLockRH;
extern HINTERNET  g_hWinhttpSession;
extern DWORD      g_dwTlsIndex;
extern HANDLE     g_hEventLog;
