// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "debugutil.h"

#include <string>
#include "dbgutil.h"
#include "stringu.h"
#include "stringa.h"
#include "dbgutil.h"
#include "Environment.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "atlbase.h"
#include "config_utility.h"

inline HANDLE g_hStandardOutput = INVALID_HANDLE_VALUE;
inline HANDLE g_logFile = INVALID_HANDLE_VALUE;
inline SRWLOCK g_logFileLock;

VOID
DebugInitialize()
{
    g_hStandardOutput = GetStdHandle(STD_OUTPUT_HANDLE);

    HKEY hKey;
    InitializeSRWLock(&g_logFileLock);

    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE,
            L"SOFTWARE\\Microsoft\\IIS Extensions\\IIS AspNetCore Module V2\\Parameters",
            0,
            KEY_READ,
            &hKey) == NO_ERROR)
    {
        DWORD dwType;
        DWORD dwData;
        DWORD cbData;

        cbData = sizeof(dwData);
        if ((RegQueryValueEx(hKey,
            L"DebugFlags",
            NULL,
            &dwType,
            (LPBYTE)&dwData,
            &cbData) == NO_ERROR) &&
            (dwType == REG_DWORD))
        {
            DEBUG_FLAGS_VAR = dwData;
        }

        RegCloseKey(hKey);
    }

    try
    {
        SetDebugFlags(Environment::GetEnvironmentVariableValue(L"ASPNETCORE_MODULE_DEBUG").value_or(L"0"));
    }
    catch (...)
    {
        // ignore
    }

    try
    {
        const auto debugOutputFile = Environment::GetEnvironmentVariableValue(L"ASPNETCORE_MODULE_DEBUG_FILE");

        CreateDebugLogFile(debugOutputFile.value_or(L""));
    }
    catch (...)
    {
        // ignore
    }

    if (IsDebuggerPresent())
    {
        DEBUG_FLAGS_VAR |= DEBUG_FLAGS_INFO;
    }
}

HRESULT
DebugInitializeFromConfig(IHttpServer& pHttpServer, IHttpApplication& pHttpApplication)
{
    CComPtr<IAppHostElement>        pAspNetCoreElement;

    const CComBSTR bstrAspNetCoreSection = L"system.webServer/aspNetCore";
    CComBSTR bstrConfigPath = pHttpApplication.GetAppConfigPath();

    RETURN_IF_FAILED(pHttpServer.GetAdminManager()->GetAdminSection(bstrAspNetCoreSection,
        bstrConfigPath,
        &pAspNetCoreElement));

    STRU debugFile;
    RETURN_IF_FAILED(ConfigUtility::FindDebugFile(pAspNetCoreElement, debugFile));

    STRU debugValue;
    RETURN_IF_FAILED(ConfigUtility::FindDebugLevel(pAspNetCoreElement, debugValue));

    SetDebugFlags(debugValue.QueryStr());

    CreateDebugLogFile(debugFile.QueryStr());

    return S_OK;
}

void SetDebugFlags(const std::wstring &debugValue)
{
    try
    {
        if (!debugValue.empty())
        {
            const auto value = std::stoi(debugValue.c_str());

            if (value >= 1) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_ERROR;
            if (value >= 2) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_WARNING;
            if (value >= 3) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_INFO;
            if (value >= 4) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_CONSOLE;
        }
    }
    catch (...)
    {
        // ignore
    }
}

void CreateDebugLogFile(const std::wstring &debugOutputFile)
{
    try
    {
        if (!debugOutputFile.empty())
        {
            if (g_logFile != INVALID_HANDLE_VALUE)
            {
                WLOG_INFOF(L"Switching debug log files to %s", debugOutputFile.c_str());
                CloseHandle(g_logFile);
                DEBUG_FLAGS_VAR &= ~ASPNETCORE_DEBUG_FLAG_FILE;

            }
            g_logFile = CreateFileW(debugOutputFile.c_str(),
                (GENERIC_READ | GENERIC_WRITE),
                (FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE),
                nullptr,
                OPEN_ALWAYS,
                FILE_ATTRIBUTE_NORMAL,
                nullptr
            );
            if (g_logFile != INVALID_HANDLE_VALUE)
            {
                DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_FILE;
            }
        }
    }
    catch (...)
    {
        // ignore
    }
}

VOID
DebugStop()
{
    if (IsEnabled(ASPNETCORE_DEBUG_FLAG_FILE))
    {
        CloseHandle(g_logFile);
    }
}

BOOL
IsEnabled(
    DWORD   dwFlag
    )
{
    return ( dwFlag & DEBUG_FLAGS_VAR );
}

VOID
DebugPrint(
    DWORD   dwFlag,
    const LPCSTR  szString
    )
{
    STACK_STRA (strOutput, 256);
    HRESULT  hr = S_OK;

    if ( IsEnabled( dwFlag ) )
    {
        hr = strOutput.SafeSnprintf(
            "[%s] %s\r\n",
            DEBUG_LABEL_VAR, szString );

        if (FAILED (hr))
        {
            return;
        }

        OutputDebugStringA( strOutput.QueryStr() );
        DWORD nBytesWritten = 0;

        if (IsEnabled(ASPNETCORE_DEBUG_FLAG_CONSOLE))
        {
            WriteFile(g_hStandardOutput, strOutput.QueryStr(), strOutput.QueryCB(), &nBytesWritten, nullptr);
        }

        if (IsEnabled(ASPNETCORE_DEBUG_FLAG_FILE))
        {
            SRWExclusiveLock lock(g_logFileLock);

            SetFilePointer(g_logFile, 0, nullptr, FILE_END);
            WriteFile(g_logFile, strOutput.QueryStr(), strOutput.QueryCB(), &nBytesWritten, nullptr);
            FlushFileBuffers(g_logFile);
        }
    }
}

VOID
DebugPrintf(
    DWORD   dwFlag,
    const LPCSTR  szFormat,
    ...
    )
{
    STACK_STRA (strCooked,256);

    va_list  args;
    HRESULT hr = S_OK;

    if ( IsEnabled( dwFlag ) )
    {
        va_start( args, szFormat );

        hr = strCooked.SafeVsnprintf(szFormat, args );

        va_end( args );

        if (FAILED (hr))
        {
            return;
        }

        DebugPrint( dwFlag, strCooked.QueryStr() );
    }
}

VOID
WDebugPrintf(
    DWORD   dwFlag,
    LPCWSTR   szFormat,
    ...
    )
{
    va_list  args;
    HRESULT hr = S_OK;

    if ( IsEnabled( dwFlag ) )
    {
        STACK_STRU (formatted,256);

        va_start( args, szFormat );

        hr = formatted.SafeVsnwprintf(szFormat, args );

        va_end( args );

        if (FAILED (hr))
        {
            return;
        }

        STACK_STRA (converted, 256);
        if (FAILED ( converted.CopyW(formatted.QueryStr(), formatted.QueryCCH()) ))
        {
            return;
        }

        DebugPrint( dwFlag, converted.QueryStr() );
    }
}
