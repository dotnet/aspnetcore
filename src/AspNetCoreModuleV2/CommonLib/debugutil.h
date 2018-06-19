// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "stdafx.h"

#define ASPNETCORE_DEBUG_FLAG_INFO          DEBUG_FLAG_INFO
#define ASPNETCORE_DEBUG_FLAG_WARNING       DEBUG_FLAG_WARN
#define ASPNETCORE_DEBUG_FLAG_ERROR         DEBUG_FLAG_ERROR
#define ASPNETCORE_DEBUG_FLAG_CONSOLE       0x00000008

VOID
DebugInitialize();

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

VOID
WDebugPrintf(
    DWORD   dwFlag,
    LPWSTR  szFormat,
    ...
    );
