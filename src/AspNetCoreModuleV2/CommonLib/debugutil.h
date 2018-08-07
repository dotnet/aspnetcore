// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include "stringu.h"
#include <Windows.h>
#include "dbgutil.h"

#define ASPNETCORE_DEBUG_FLAG_INFO          DEBUG_FLAG_INFO
#define ASPNETCORE_DEBUG_FLAG_WARNING       DEBUG_FLAG_WARN
#define ASPNETCORE_DEBUG_FLAG_ERROR         DEBUG_FLAG_ERROR
#define ASPNETCORE_DEBUG_FLAG_CONSOLE       0x00000008
#define ASPNETCORE_DEBUG_FLAG_FILE          0x00000010

#define LOG_INFO(...) DebugPrint(ASPNETCORE_DEBUG_FLAG_INFO, __VA_ARGS__)
#define LOG_INFOF(...) DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO, __VA_ARGS__)

#define LOG_WARN(...) DebugPrint(ASPNETCORE_DEBUG_FLAG_WARNING, __VA_ARGS__)
#define LOG_WARNF(...) DebugPrintf(ASPNETCORE_DEBUG_FLAG_WARNING, __VA_ARGS__)

#define LOG_ERROR(...) DebugPrint(ASPNETCORE_DEBUG_FLAG_ERROR, __VA_ARGS__)
#define LOG_ERRORF(...) DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, __VA_ARGS__)

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
