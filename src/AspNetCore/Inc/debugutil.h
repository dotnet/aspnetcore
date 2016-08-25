// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define ASPNETCORE_DEBUG_FLAG_INFO          0x00000001
#define ASPNETCORE_DEBUG_FLAG_WARNING       0x00000002
#define ASPNETCORE_DEBUG_FLAG_ERROR         0x00000004

extern DWORD g_dwAspNetCoreDebugFlags;

static
BOOL
IfDebug(
    DWORD   dwFlag
    )
{
    return ( dwFlag & g_dwAspNetCoreDebugFlags );
}

static
VOID
DebugPrint(
    DWORD   dwFlag,
    LPCSTR  szString
    )
{
    STACK_STRA (strOutput, 256);
    HRESULT  hr = S_OK;

    if ( IfDebug( dwFlag ) )
    {
        hr = strOutput.SafeSnprintf( 
            "[aspnetcore.dll] %s\r\n",
            szString );

        if (FAILED (hr))
        {
            goto Finished;
        }

        OutputDebugStringA( strOutput.QueryStr() );
    }

Finished:

    return;
}

static
VOID
DebugPrintf(
DWORD   dwFlag,
LPCSTR  szFormat,
...
)
{
    STACK_STRA (strCooked,256);

    va_list  args;
    HRESULT hr = S_OK;

    if ( IfDebug( dwFlag ) )
    {
        va_start( args, szFormat );

        hr = strCooked.SafeVsnprintf(szFormat, args );

        va_end( args );

        if (FAILED (hr))
        {
            goto Finished;
        }

        DebugPrint( dwFlag, strCooked.QueryStr() );
    }

Finished:
    return;
}

