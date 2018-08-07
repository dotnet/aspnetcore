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
inline HMODULE g_hModule;
inline SRWLOCK g_logFileLock;

HRESULT
PrintDebugHeader()
{
    DWORD  verHandle = 0;
    UINT   size      = 0;
    LPVOID lpBuffer  = NULL;

    WCHAR path[MAX_PATH];
    RETURN_LAST_ERROR_IF(!GetModuleFileName(g_hModule, path, sizeof(path)));

    DWORD verSize = GetFileVersionInfoSize(path, &verHandle);
    RETURN_LAST_ERROR_IF(verSize == 0);

    // Allocate memory to hold data structure returned by GetFileVersionInfo
    std::vector<BYTE> verData(verSize);

    RETURN_LAST_ERROR_IF(!GetFileVersionInfo(path, verHandle, verSize, verData.data()));
    RETURN_LAST_ERROR_IF(!VerQueryValue(verData.data(), L"\\", &lpBuffer, &size));

    auto verInfo = reinterpret_cast<VS_FIXEDFILEINFO *>(lpBuffer);
    // Check result signature
    if (verInfo->dwSignature == VS_FFI_SIGNATURE)
    {
        LPVOID pvProductName = NULL;
        unsigned int iProductNameLen = 0;
        RETURN_LAST_ERROR_IF(!VerQueryValue(verData.data(), _T("\\StringFileInfo\\040904b0\\FileDescription"), &pvProductName, &iProductNameLen));

        // Major, minor are stored in dwFileVersionMS field and patch, build in dwFileVersionLS field as pair of 32 bit numbers
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO, "Initializing logs for %S. File Version: %d.%d.%d.%d. Description: %S",
            path,
            ( verInfo->dwFileVersionMS >> 16 ) & 0xffff,
            ( verInfo->dwFileVersionMS >>  0 ) & 0xffff,
            ( verInfo->dwFileVersionLS >> 16 ) & 0xffff,
            ( verInfo->dwFileVersionLS >>  0 ) & 0xffff,
            pvProductName
        );
    }

    return S_OK;
}

void SetDebugFlags(const std::wstring &debugValue)
{
    try
    {
        if (!debugValue.empty())
        {
            const auto value = std::stoi(debugValue);

            if (value >= 1) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_ERROR;
            if (value >= 2) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_WARNING;
            if (value >= 3) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_INFO;
            if (value >= 4) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_CONSOLE;
            if (value >= 5) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_FILE;

            return;
        }
    }
    catch (...)
    {
        // ignore
    }

    try
    {
        std::wstringstream stringStream(debugValue);
        std::wstring flag;

        while (std::getline(stringStream, flag, L','))
        {
            if (_wcsnicmp(flag.c_str(), L"error", wcslen(L"error")) == 0) DEBUG_FLAGS_VAR |= DEBUG_FLAGS_ERROR;
            if (_wcsnicmp(flag.c_str(), L"warning", wcslen(L"warning")) == 0) DEBUG_FLAGS_VAR |= DEBUG_FLAGS_WARN;
            if (_wcsnicmp(flag.c_str(), L"info", wcslen(L"info")) == 0) DEBUG_FLAGS_VAR |= DEBUG_FLAGS_INFO;
            if (_wcsnicmp(flag.c_str(), L"console", wcslen(L"console")) == 0) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_CONSOLE;
            if (_wcsnicmp(flag.c_str(), L"file", wcslen(L"file")) == 0) DEBUG_FLAGS_VAR |= ASPNETCORE_DEBUG_FLAG_FILE;
        }

        // If file or console is enabled but level is not set, enable all levels
        if (DEBUG_FLAGS_VAR != 0 && (DEBUG_FLAGS_VAR & DEBUG_FLAGS_ANY) == 0)
        {
            DEBUG_FLAGS_VAR |= DEBUG_FLAGS_ANY;
        }
    }
    catch (...)
    {
        // ignore
    }
}

bool CreateDebugLogFile(const std::wstring &debugOutputFile)
{
    try
    {
        if (!debugOutputFile.empty())
        {
            if (g_logFile != INVALID_HANDLE_VALUE)
            {
                LOG_INFOF("Switching debug log files to %S", debugOutputFile.c_str());
            }

            SRWExclusiveLock lock(g_logFileLock);
            if (g_logFile != INVALID_HANDLE_VALUE)
            {
                CloseHandle(g_logFile);
                g_logFile = INVALID_HANDLE_VALUE;
            }
            g_logFile = CreateFileW(debugOutputFile.c_str(),
                (GENERIC_READ | GENERIC_WRITE),
                (FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE),
                nullptr,
                OPEN_ALWAYS,
                FILE_ATTRIBUTE_NORMAL,
                nullptr
            );
            return true;
        }
    }
    catch (...)
    {
        // ignore
    }

    return false;
}

VOID
DebugInitialize(HMODULE hModule)
{
    g_hModule = hModule;
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

    PrintDebugHeader();
}

HRESULT
DebugInitializeFromConfig(IHttpServer& pHttpServer, IHttpApplication& pHttpApplication)
{
    auto oldFlags = DEBUG_FLAGS_VAR;

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

    if (debugFile.QueryCCH() == 0 && IsEnabled(ASPNETCORE_DEBUG_FLAG_FILE))
    {
        debugFile.Append(L".\\aspnetcore-debug.log");
    }

    std::filesystem::path filePath = std::filesystem::path(debugFile.QueryStr());
    if (!filePath.empty() && filePath.is_relative())
    {
        filePath = std::filesystem::path(pHttpApplication.GetApplicationPhysicalPath()) / filePath;
    }

    const auto reopenedFile = CreateDebugLogFile(filePath);

    // Print header if flags changed
    if (oldFlags != DEBUG_FLAGS_VAR || reopenedFile)
    {
        PrintDebugHeader();
    }

    return S_OK;
}

VOID
DebugStop()
{
    if (g_logFile != INVALID_HANDLE_VALUE)
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

        if (g_logFile != INVALID_HANDLE_VALUE)
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
