// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"
#include <IPHlpApi.h>

HTTP_MODULE_ID      g_pModuleId = NULL;
IHttpServer *       g_pHttpServer = NULL;
BOOL                g_fAsyncDisconnectAvailable = FALSE;
BOOL                g_fWinHttpNonBlockingCallbackAvailable = FALSE;
PCWSTR              g_pszModuleName = NULL;
HINSTANCE           g_hModule;
HINSTANCE           g_hWinHttpModule;
BOOL                g_fWebSocketSupported = FALSE;
DWORD               g_dwTlsIndex = TLS_OUT_OF_INDEXES;
BOOL                g_fEnableReferenceCountTracing = FALSE;
DWORD               g_dwAspNetCoreDebugFlags = 0;
BOOL                g_fNsiApiNotSupported = FALSE;
DWORD               g_dwActiveServerProcesses = 0;
DWORD               g_OptionalWinHttpFlags = 0; //specify additional WinHTTP options when using WinHttpOpenRequest API.
DWORD               g_dwDebugFlags = 0;
PCSTR               g_szDebugLabel = "ASPNET_CORE_MODULE";

#ifdef DEBUG
STRA                g_strLogs[ASPNETCORE_DEBUG_STRU_ARRAY_SIZE];
DWORD               g_dwLogCounter = 0;
#endif // DEBUG


BOOL WINAPI DllMain(
    HMODULE hModule,
    DWORD   dwReason,
    LPVOID
    )
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        g_hModule = hModule;
        DisableThreadLibraryCalls(hModule);
        break;
    default:
        break;
    }

    return TRUE;
}

VOID
LoadGlobalConfiguration(
VOID
)
{
    HKEY hKey;

    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE,
        L"SOFTWARE\\Microsoft\\IIS Extensions\\IIS AspNetCore Module\\Parameters",
        0,
        KEY_READ,
        &hKey) == NO_ERROR)
    {
        DWORD dwType;
        DWORD dwData;
        DWORD cbData;

        cbData = sizeof(dwData);
        if ((RegQueryValueEx(hKey,
            L"OptionalWinHttpFlags",
            NULL,
            &dwType,
            (LPBYTE)&dwData,
            &cbData) == NO_ERROR) &&
            (dwType == REG_DWORD))
        {
            g_OptionalWinHttpFlags = dwData;
        }

        cbData = sizeof(dwData);
        if ((RegQueryValueEx(hKey,
            L"EnableReferenceCountTracing",
            NULL,
            &dwType,
            (LPBYTE)&dwData,
            &cbData) == NO_ERROR) &&
            (dwType == REG_DWORD) && (dwData == 1 || dwData == 0))
        {
            g_fEnableReferenceCountTracing = !!dwData;
        }

        cbData = sizeof(dwData);
        if ((RegQueryValueEx(hKey,
            L"DebugFlags",
            NULL,
            &dwType,
            (LPBYTE)&dwData,
            &cbData) == NO_ERROR) &&
            (dwType == REG_DWORD))
        {
            g_dwAspNetCoreDebugFlags = dwData;
        }

        RegCloseKey(hKey);
    }

    DWORD dwSize = 0;
    DWORD dwResult = GetExtendedTcpTable(NULL,
        &dwSize,
        FALSE,
        AF_INET,
        TCP_TABLE_OWNER_PID_LISTENER,
        0);
    if (dwResult != NO_ERROR && dwResult != ERROR_INSUFFICIENT_BUFFER)
    {
        g_fNsiApiNotSupported = TRUE;
    }
}

HRESULT
__stdcall
RegisterModule(
DWORD                           dwServerVersion,
IHttpModuleRegistrationInfo *   pModuleInfo,
IHttpServer *                   pHttpServer
)
/*++

Routine description:

Function called by IIS immediately after loading the module, used to let
IIS know what notifications the module is interested in

Arguments:

dwServerVersion - IIS version the module is being loaded on
pModuleInfo - info regarding this module
pHttpServer - callback functions which can be used by the module at
any point

Return value:

HRESULT

--*/
{
    HRESULT                 hr = S_OK;
    CProxyModuleFactory *   pFactory = NULL;

#ifdef DEBUG
    CREATE_DEBUG_PRINT_OBJECT("Asp.Net Core Module");
    g_dwDebugFlags = DEBUG_FLAGS_ANY;
#endif // DEBUG

    LoadGlobalConfiguration();

    //
    // 7.0 is 0,7
    //
    if (dwServerVersion > MAKELONG(0, 7))
    {
        g_fAsyncDisconnectAvailable = TRUE;
    }

    //
    // 8.0 is 0,8
    //
    if (dwServerVersion >= MAKELONG(0, 8))
    {
        // IISOOB:36641 Enable back WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS for Win8.
        // g_fWinHttpNonBlockingCallbackAvailable = TRUE;
        g_fWebSocketSupported = TRUE;
    }

    hr = WINHTTP_HELPER::StaticInitialize();
    if (FAILED(hr))
    {
        if (hr == HRESULT_FROM_WIN32(ERROR_PROC_NOT_FOUND))
        {
            g_fWebSocketSupported = FALSE;
        }
        else
        {
            goto Finished;
        }
    }

    g_pModuleId = pModuleInfo->GetId();
    g_pszModuleName = pModuleInfo->GetName();
    g_pHttpServer = pHttpServer;

#ifdef DEBUG
    for (int i = 0; i < ASPNETCORE_DEBUG_STRU_ARRAY_SIZE; i++)
    {
        g_strLogs[i].Resize(ASPNETCORE_DEBUG_STRU_BUFFER_SIZE + 1);
    }
#endif // DEBUG
    //
    // WinHTTP does not create enough threads, ask it to create more.
    // Starting in Windows 7, this setting is ignored because WinHTTP
    // uses a thread pool.
    //
    SYSTEM_INFO si;
    GetSystemInfo(&si);
    DWORD dwThreadCount = (si.dwNumberOfProcessors * 3 + 1) / 2;
    WinHttpSetOption(NULL,
        WINHTTP_OPTION_WORKER_THREAD_COUNT,
        &dwThreadCount,
        sizeof(dwThreadCount));

    //
    // Create the factory before any static initialization.
    // The CProxyModuleFactory::Terminate method will clean any
    // static object initialized.
    //
    pFactory = new CProxyModuleFactory;
    if (pFactory == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = pModuleInfo->SetRequestNotifications(
        pFactory,
        RQ_EXECUTE_REQUEST_HANDLER,
        0);
    if (FAILED(hr))
    {
        goto Finished;
    }

    pFactory = NULL;	
    g_pResponseHeaderHash = new RESPONSE_HEADER_HASH;
    if (g_pResponseHeaderHash == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = g_pResponseHeaderHash->Initialize();
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = ALLOC_CACHE_HANDLER::StaticInitialize();
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = FORWARDING_HANDLER::StaticInitialize(g_fEnableReferenceCountTracing);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = WEBSOCKET_HANDLER::StaticInitialize(g_fEnableReferenceCountTracing);
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:

    if (pFactory != NULL)
    {
        pFactory->Terminate();
        pFactory = NULL;
    }

    return hr;
}

