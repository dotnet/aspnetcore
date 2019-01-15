// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include "stringu.h"
#include <Windows.h>
#include "dbgutil.h"

#define ASPNETCORE_DEBUG_FLAG_TRACE         DEBUG_FLAG_TRACE
#define ASPNETCORE_DEBUG_FLAG_INFO          DEBUG_FLAG_INFO
#define ASPNETCORE_DEBUG_FLAG_WARNING       DEBUG_FLAG_WARN
#define ASPNETCORE_DEBUG_FLAG_ERROR         DEBUG_FLAG_ERROR
#define ASPNETCORE_DEBUG_FLAG_CONSOLE       0x00000010
#define ASPNETCORE_DEBUG_FLAG_FILE          0x00000020
#define ASPNETCORE_DEBUG_FLAG_EVENTLOG      0x00000040

#define LOG_TRACE(...) DebugPrintW(ASPNETCORE_DEBUG_FLAG_TRACE, __VA_ARGS__)
#define LOG_TRACEF(...) DebugPrintfW(ASPNETCORE_DEBUG_FLAG_TRACE, __VA_ARGS__)

#define LOG_INFO(...) DebugPrintW(ASPNETCORE_DEBUG_FLAG_INFO, __VA_ARGS__)
#define LOG_INFOF(...) DebugPrintfW(ASPNETCORE_DEBUG_FLAG_INFO, __VA_ARGS__)

#define LOG_WARN(...) DebugPrintW(ASPNETCORE_DEBUG_FLAG_WARNING, __VA_ARGS__)
#define LOG_WARNF(...) DebugPrintfW(ASPNETCORE_DEBUG_FLAG_WARNING, __VA_ARGS__)

#define LOG_ERROR(...) DebugPrintW(ASPNETCORE_DEBUG_FLAG_ERROR, __VA_ARGS__)
#define LOG_ERRORF(...) DebugPrintfW(ASPNETCORE_DEBUG_FLAG_ERROR, __VA_ARGS__)

VOID
DebugInitialize(HMODULE hModule);

HRESULT
DebugInitializeFromConfig(IHttpServer& pHttpServer, IHttpApplication& pHttpApplication);

VOID
DebugStop();

BOOL
IsEnabled(
    DWORD   dwFlag
    );

VOID
DebugPrintW(
    DWORD   dwFlag,
    LPCWSTR  szString
    );

VOID
DebugPrintfW(
    DWORD   dwFlag,
    LPCWSTR  szFormat,
    ...
    );


VOID
DebugPrint(
    DWORD   dwFlag,
    LPCSTR  szString
    );

VOID
DebugPrintf(
    DWORD   dwFlag,
    LPCSTR  szFormat,
    ...
    );

std::wstring
GetProcessIdString();

std::wstring
GetVersionInfoString();

std::wstring
GetModuleName();
