// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#pragma warning( disable : 4091)

//
// System related headers
//
#define WIN32_LEAN_AND_MEAN

#define NTDDI_VERSION 0x06010000
#define WINVER 0x0601
#define _WIN32_WINNT 0x0601

#include <Windows.h>
#include <atlbase.h>
#include <httpserv.h>

#include "stringu.h"
#include "stringa.h"

#include "ntassert.h"
#include "dbgutil.h"

#include "aspnetcore_msg.h"
#include "resources.h"

#include <memory>

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
extern BOOL         g_fInShutdown;
extern BOOL         g_fEnableReferenceCountTracing;
extern DWORD        g_dwActiveServerProcesses;
extern HINSTANCE    g_hModule;
extern HMODULE      g_hAspnetCoreRH;
extern SRWLOCK      g_srwLock;
extern PCWSTR       g_pwzAspnetcoreRequestHandlerName;
extern HANDLE       g_hEventLog;

#pragma warning( error : 4091)
