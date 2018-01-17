// dllmain.cpp : Defines the entry point for the DLL application.
#include "precomp.hxx"
#include <IPHlpApi.h>
#include <VersionHelpers.h>

BOOL                g_fNsiApiNotSupported = FALSE;
BOOL                g_fWebSocketSupported = FALSE;
BOOL                g_fEnableReferenceCountTracing = FALSE;
BOOL                g_fGlobalInitialize = FALSE;
BOOL                g_fOutOfProcessInitialize = FALSE;
BOOL                g_fOutOfProcessInitializeError = FALSE;
BOOL                g_fWinHttpNonBlockingCallbackAvailable = FALSE;
DWORD               g_OptionalWinHttpFlags = 0;
DWORD               g_dwAspNetCoreDebugFlags = 0;
DWORD               g_dwDebugFlags = 0;
DWORD               g_dwTlsIndex = TLS_OUT_OF_INDEXES;
SRWLOCK             g_srwLockRH;
HINTERNET           g_hWinhttpSession = NULL;
IHttpServer *       g_pHttpServer = NULL;
HINSTANCE           g_hWinHttpModule;
HANDLE              g_hEventLog = NULL;


VOID
InitializeGlobalConfiguration(
    IHttpServer * pServer
)
{
    HKEY hKey;
    BOOL fLocked = FALSE;
    DWORD dwSize = 0;
    DWORD dwResult = 0;

    if (!g_fGlobalInitialize)
    {
        AcquireSRWLockExclusive(&g_srwLockRH);
        fLocked = TRUE;

        if (g_fGlobalInitialize)
        {
            // Done by another thread
            goto Finished;
        }

        g_pHttpServer = pServer;
        if (pServer->IsCommandLineLaunch())
        {
            g_hEventLog = RegisterEventSource(NULL, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
        }
        else
        {
            g_hEventLog = RegisterEventSource(NULL, ASPNETCORE_EVENT_PROVIDER);
        }

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

        dwResult = GetExtendedTcpTable(NULL,
            &dwSize,
            FALSE,
            AF_INET,
            TCP_TABLE_OWNER_PID_LISTENER,
            0);
        if (dwResult != NO_ERROR && dwResult != ERROR_INSUFFICIENT_BUFFER)
        {
            g_fNsiApiNotSupported = TRUE;
        }

        // WebSocket is supported on Win8 and above only
        // todo: test on win7
        g_fWebSocketSupported = IsWindows8OrGreater();

        g_fGlobalInitialize = TRUE;
    }
Finished:
    if (fLocked)
    {
        ReleaseSRWLockExclusive(&g_srwLockRH);
    }
}

//
// Global initialization routine for OutOfProcess
//
HRESULT
EnsureOutOfProcessInitializtion()
{

    DBG_ASSERT(pServer);

    HRESULT hr = S_OK;
    BOOL    fLocked = FALSE;

    if (g_fOutOfProcessInitializeError)
    {
        hr = E_NOT_VALID_STATE;
        goto Finished;
    }

    if (!g_fOutOfProcessInitialize)
    {
        AcquireSRWLockExclusive(&g_srwLockRH);
        fLocked = TRUE;
        if (g_fOutOfProcessInitializeError)
        {
            hr = E_NOT_VALID_STATE;
            goto Finished;
        }

        if (g_fOutOfProcessInitialize)
        {
            // Done by another thread
            goto Finished;
        }

        g_hWinHttpModule = GetModuleHandle(TEXT("winhttp.dll"));

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

        g_hWinhttpSession = WinHttpOpen(L"",
            WINHTTP_ACCESS_TYPE_NO_PROXY,
            WINHTTP_NO_PROXY_NAME,
            WINHTTP_NO_PROXY_BYPASS,
            WINHTTP_FLAG_ASYNC);
        if (g_hWinhttpSession == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        //
        // Don't set non-blocking callbacks WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS, 
        // as we will call WinHttpQueryDataAvailable to get response on the same thread
        // that we received callback from Winhttp on completing sending/forwarding the request
        // 

        //
        // Setup the callback function
        //
        if (WinHttpSetStatusCallback(g_hWinhttpSession,
            FORWARDING_HANDLER::OnWinHttpCompletion,
            (WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
                WINHTTP_CALLBACK_STATUS_SENDING_REQUEST),
            NULL) == WINHTTP_INVALID_STATUS_CALLBACK)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        //
        // Make sure we see the redirects (rather than winhttp doing it
        // automatically)
        //
        DWORD dwRedirectOption = WINHTTP_OPTION_REDIRECT_POLICY_NEVER;
        if (!WinHttpSetOption(g_hWinhttpSession,
            WINHTTP_OPTION_REDIRECT_POLICY,
            &dwRedirectOption,
            sizeof(dwRedirectOption)))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        g_dwTlsIndex = TlsAlloc();
        if (g_dwTlsIndex == TLS_OUT_OF_INDEXES)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
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
    }

Finished:
    if (FAILED(hr))
    {
        g_fOutOfProcessInitializeError = TRUE;
    }
    if (fLocked)
    {
        ReleaseSRWLockExclusive(&g_srwLockRH);
    }
    return hr;
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        InitializeSRWLock(&g_srwLockRH);
        break;
    default:
        break;
    }
    return TRUE;
}

HRESULT
__stdcall
CreateApplication(
    _In_  IHttpServer        *pServer,
    _In_  ASPNETCORE_CONFIG  *pConfig,
    _Out_ APPLICATION       **ppApplication
)
{
    HRESULT      hr = S_OK;
    APPLICATION *pApplication = NULL;

    // Initialze some global variables here
    InitializeGlobalConfiguration(pServer);

    if (pConfig->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
    {
        pApplication = new IN_PROCESS_APPLICATION(pServer, pConfig);
        if (pApplication == NULL)
        {
            hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
            goto Finished;
        }
    }
    else if (pConfig->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_OUT_PROCESS)
    {
        hr = EnsureOutOfProcessInitializtion();
        if (FAILED(hr))
        {
            goto Finished;
        }

        pApplication = new OUT_OF_PROCESS_APPLICATION(pServer, pConfig);
        if (pApplication == NULL)
        {
            hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
            goto Finished;
        }

        hr = ((OUT_OF_PROCESS_APPLICATION*)pApplication)->Initialize();
        if (FAILED(hr))
        {
            delete pApplication;
            pApplication = NULL;
            goto Finished;
        }
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        goto Finished;
    }

    *ppApplication = pApplication;

Finished:
    return hr;
}

HRESULT
__stdcall
CreateRequestHandler(
    _In_  IHttpContext       *pHttpContext,
    _In_  HTTP_MODULE_ID     *pModuleId,
    _In_  APPLICATION        *pApplication,
    _Out_ REQUEST_HANDLER   **pRequestHandler
)
{
    HRESULT hr = S_OK;
    REQUEST_HANDLER* pHandler = NULL;
    ASPNETCORE_CONFIG* pConfig = pApplication->QueryConfig();
    DBG_ASSERT(pConfig);

    if (pConfig->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
    {
        pHandler = new IN_PROCESS_HANDLER(pHttpContext, pModuleId, pApplication);
    }
    else if (pConfig->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_OUT_PROCESS)
    {
        pHandler = new FORWARDING_HANDLER(pHttpContext, pModuleId, pApplication);
    }
    else
    {
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    if (pHandler == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }
    else
    {
        *pRequestHandler = pHandler;
    }
    return hr;
}
