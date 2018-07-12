// dllmain.cpp : Defines the entry point for the DLL application.

#include <IPHlpApi.h>
#include <VersionHelpers.h>

DECLARE_DEBUG_PRINT_OBJECT("aspnetcorev2_outofprocess.dll");

BOOL                g_fNsiApiNotSupported = FALSE;
BOOL                g_fWebSocketStaticInitialize = FALSE;
BOOL                g_fEnableReferenceCountTracing = FALSE;
BOOL                g_fGlobalInitialize = FALSE;
BOOL                g_fOutOfProcessInitialize = FALSE;
BOOL                g_fOutOfProcessInitializeError = FALSE;
BOOL                g_fWinHttpNonBlockingCallbackAvailable = FALSE;
BOOL                g_fProcessDetach = FALSE;
DWORD               g_OptionalWinHttpFlags = 0;
DWORD               g_dwTlsIndex = TLS_OUT_OF_INDEXES;
SRWLOCK             g_srwLockRH;
HINTERNET           g_hWinhttpSession = NULL;
IHttpServer *       g_pHttpServer = NULL;
HINSTANCE           g_hWinHttpModule;
HINSTANCE           g_hAspNetCoreModule;
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
        }

        DebugInitialize();

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

        g_fWebSocketStaticInitialize = IsWindows8OrGreater();

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

    DBG_ASSERT(g_pHttpServer);

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

        g_hAspNetCoreModule = GetModuleHandle(TEXT("aspnetcorev2.dll"));

        hr = WINHTTP_HELPER::StaticInitialize();
        if (FAILED(hr))
        {
            if (hr == HRESULT_FROM_WIN32(ERROR_PROC_NOT_FOUND))
            {
                g_fWebSocketStaticInitialize = FALSE;
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
    case DLL_PROCESS_DETACH:
        g_fProcessDetach = TRUE;
        DebugStop();
    default:
        break;
    }
    return TRUE;
}

HRESULT
__stdcall
CreateApplication(
    _In_  IHttpServer        *pServer,
    _In_  IHttpApplication   *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION      **ppApplication
)
{
    UNREFERENCED_PARAMETER(pParameters);
    UNREFERENCED_PARAMETER(nParameters);
    
    InitializeGlobalConfiguration(pServer);

    REQUESTHANDLER_CONFIG *pConfig = nullptr;
    RETURN_IF_FAILED(REQUESTHANDLER_CONFIG::CreateRequestHandlerConfig(pServer, pHttpApplication, &pConfig));
    std::unique_ptr<REQUESTHANDLER_CONFIG> pRequestHandlerConfig(pConfig);

    RETURN_IF_FAILED(EnsureOutOfProcessInitializtion());

    std::unique_ptr<OUT_OF_PROCESS_APPLICATION> pApplication = std::make_unique<OUT_OF_PROCESS_APPLICATION>(*pHttpApplication, std::move(pRequestHandlerConfig));

    RETURN_IF_FAILED(pApplication->Initialize());
    RETURN_IF_FAILED(pApplication->StartMonitoringAppOffline());

    *ppApplication = pApplication.release();
    return S_OK;
}
