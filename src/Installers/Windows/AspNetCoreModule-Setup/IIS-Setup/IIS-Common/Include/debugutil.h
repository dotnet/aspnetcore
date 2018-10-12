// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#define DEBUG_FLAG_INFO     0x00000001
#define DEBUG_FLAG_WARN     0x00000002
#define DEBUG_FLAG_ERROR    0x00000004

//
// Predefined error level values. These are backwards from the
// windows definitions.
//

#define DEBUG_FLAGS_INFO    (DEBUG_FLAG_ERROR | DEBUG_FLAG_WARN | DEBUG_FLAG_INFO)
#define DEBUG_FLAGS_WARN    (DEBUG_FLAG_ERROR | DEBUG_FLAG_WARN)
#define DEBUG_FLAGS_ERROR   (DEBUG_FLAG_ERROR)
#define DEBUG_FLAGS_ANY     (DEBUG_FLAG_INFO | DEBUG_FLAG_WARN | DEBUG_FLAG_ERROR)

#define DEBUG_FLAGS_REGISTRY_LOCATION_A   "DebugFlags"

extern DWORD g_dwDebugFlags; 

static
BOOL
IfDebug(
    DWORD   dwFlag
    )
{
    return ( dwFlag & g_dwDebugFlags );
}

static
VOID
DebugPrint(
    DWORD   dwFlag,
    LPCSTR  szString
    )
{
    STBUFF strOutput;
    HRESULT hr;

    if ( IfDebug( dwFlag ) )
    {
        hr = strOutput.Printf( "[dipmodule.dll] %s\r\n",
                                      szString );

        if ( FAILED( hr ) )
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
    STBUFF strCooked;
    STBUFF strOutput;
    va_list  args;
    HRESULT  hr;

    if ( IfDebug( dwFlag ) )
    {
        va_start( args, szFormat );

        hr = strCooked.Vsprintf( (LPSTR)szFormat, args );

        va_end( args );

        if ( FAILED( hr ) )
        {
            goto Finished;
        }

        DebugPrint( dwFlag, strCooked.QueryStr() );
    }

Finished:

    return;
}

static void ReadDebugFlagFromRegistryKey(const char* pszRegKey, IN DWORD dwDefault)
{
    HKEY hkey = NULL;
    g_dwDebugFlags = dwDefault;
    DWORD dwType;
    DWORD dwBuffer;
    DWORD  cbBuffer = sizeof(dwBuffer);
    
    DWORD dwError = RegOpenKeyExA(HKEY_LOCAL_MACHINE,
                                  pszRegKey,
                                  0,
                                  KEY_READ,
                                  &hkey);
    if ( dwError == NO_ERROR && hkey != NULL) 
    {
        dwError = RegQueryValueExA( hkey,
                               DEBUG_FLAGS_REGISTRY_LOCATION_A,
                               NULL,
                               &dwType,
                               (LPBYTE)&dwBuffer,
                               &cbBuffer );
        if( ( dwError == NO_ERROR ) && ( dwType == REG_DWORD ) )
        {
            g_dwDebugFlags = dwBuffer;
        }
        RegCloseKey( hkey);
        hkey = NULL;
    }
}

