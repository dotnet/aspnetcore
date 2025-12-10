// dllmain.cpp : Defines the entry point for the DLL application.

#include <IPHlpApi.h>
#include <VersionHelpers.h>
#include "exceptions.h"

DECLARE_DEBUG_PRINT_OBJECT("aspnetcorev2_outofprocess.dll");

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
HINTERNET           g_hWinhttpSession = nullptr;
IHttpServer *       g_pHttpServer = nullptr;
HINSTANCE           g_hWinHttpModule;
HINSTANCE           g_hOutOfProcessRHModule;
HINSTANCE           g_hAspNetCoreModule;
HANDLE              g_hEventLog = nullptr;

VOID
InitializeGlobalConfiguration(
    IHttpServer * pServer
)
{
    HKEY hKey{};
    BOOL fLocked = FALSE;

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
            g_hEventLog = RegisterEventSource(nullptr, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
        }
        else
        {
            g_hEventLog = RegisterEventSource(nullptr, ASPNETCORE_EVENT_PROVIDER);
        }

        if (RegOpenKeyEx(HKEY_LOCAL_MACHINE,
            L"SOFTWARE\\Microsoft\\IIS Extensions\\IIS AspNetCore Module V2\\Parameters",
            0,
            KEY_READ,
            &hKey) == NO_ERROR)
        {
            DWORD dwType = 0;
            DWORD dwData = 0;
            DWORD cbData = 0;

            cbData = sizeof(dwData);
            if ((RegQueryValueEx(hKey,
                L"OptionalWinHttpFlags",
                nullptr,
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
                nullptr,
                &dwType,
                (LPBYTE)&dwData,
                &cbData) == NO_ERROR) &&
                (dwType == REG_DWORD) && (dwData == 1 || dwData == 0))
            {
                g_fEnableReferenceCountTracing = !!dwData;
            }
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
EnsureOutOfProcessInitializtion(IHttpApplication *pHttpApplication)
{

    DBG_ASSERT(g_pHttpServer);

    HRESULT hr = S_OK;

    if (g_fOutOfProcessInitializeError)
    {
        FINISHED(E_NOT_VALID_STATE);
    }

    if (g_fOutOfProcessInitialize)
    {
        FINISHED(S_OK);
    }

    {
        auto lock = SRWExclusiveLock(g_srwLockRH);

        if (g_fOutOfProcessInitializeError)
        {
            FINISHED(E_NOT_VALID_STATE);
        }

        if (g_fOutOfProcessInitialize)
        {
            // Done by another thread
            FINISHED(S_OK);
        }

        g_fOutOfProcessInitialize = TRUE;

        g_hWinHttpModule = GetModuleHandle(TEXT("winhttp.dll"));

        g_hAspNetCoreModule = GetModuleHandle(TEXT("aspnetcorev2.dll"));

        hr = WINHTTP_HELPER::StaticInitialize();
        if (FAILED_LOG(hr))
        {
            if (hr == HRESULT_FROM_WIN32(ERROR_PROC_NOT_FOUND))
            {
                g_fWebSocketStaticInitialize = FALSE;
            }
            else
            {
                FINISHED(hr);
            }
        }

#pragma warning(push)
#pragma warning(disable: 26477) // NULL usage via Windows header
        g_hWinhttpSession = WinHttpOpen(L"",
            WINHTTP_ACCESS_TYPE_NO_PROXY,
            WINHTTP_NO_PROXY_NAME,
            WINHTTP_NO_PROXY_BYPASS,
            WINHTTP_FLAG_ASYNC);
#pragma warning(pop)

        FINISHED_LAST_ERROR_IF(g_hWinhttpSession == nullptr);

        //
        // Don't set non-blocking callbacks WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS,
        // as we will call WinHttpQueryDataAvailable to get response on the same thread
        // that we received callback from Winhttp on completing sending/forwarding the request
        //

        //
        // Setup the callback function
        //
        FINISHED_LAST_ERROR_IF(WinHttpSetStatusCallback(g_hWinhttpSession,
            FORWARDING_HANDLER::OnWinHttpCompletion,
            (WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
                WINHTTP_CALLBACK_STATUS_SENDING_REQUEST),
            0) == WINHTTP_INVALID_STATUS_CALLBACK);

        //
        // Make sure we see the redirects (rather than winhttp doing it
        // automatically)
        //
        DWORD dwRedirectOption = WINHTTP_OPTION_REDIRECT_POLICY_NEVER;
        FINISHED_LAST_ERROR_IF(!WinHttpSetOption(g_hWinhttpSession,
            WINHTTP_OPTION_REDIRECT_POLICY,
            &dwRedirectOption,
            sizeof(dwRedirectOption)));

        g_dwTlsIndex = TlsAlloc();
        FINISHED_LAST_ERROR_IF(g_dwTlsIndex == TLS_OUT_OF_INDEXES);
        FINISHED_IF_FAILED(ALLOC_CACHE_HANDLER::StaticInitialize());
        FINISHED_IF_FAILED(FORWARDING_HANDLER::StaticInitialize(g_fEnableReferenceCountTracing));
        FINISHED_IF_FAILED(WEBSOCKET_HANDLER::StaticInitialize(g_fEnableReferenceCountTracing));

        DebugInitializeFromConfig(*g_pHttpServer, *pHttpApplication);
    }
Finished:
    if (FAILED(hr))
    {
        g_fOutOfProcessInitializeError = TRUE;
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
        g_hOutOfProcessRHModule = hModule;
        DisableThreadLibraryCalls(hModule);
        InitializeSRWLock(&g_srwLockRH);
        DebugInitialize(hModule);
        break;
    case DLL_PROCESS_DETACH:
        g_fProcessDetach = TRUE;
        FORWARDING_HANDLER::StaticTerminate();
        ALLOC_CACHE_HANDLER::StaticTerminate();
        DebugStop();
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
    _In_  IHttpApplication   *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION      **ppApplication
)
{
    TraceContextScope traceScope(FindParameter<IHttpTraceContext*>("TraceContext", pParameters, nParameters));
    auto pSite = FindParameter<IHttpSite*>("Site", pParameters, nParameters);

    InitializeGlobalConfiguration(pServer);

    REQUESTHANDLER_CONFIG *pConfig = nullptr;
    RETURN_IF_FAILED(REQUESTHANDLER_CONFIG::CreateRequestHandlerConfig(pServer, pSite, pHttpApplication, &pConfig));
    std::unique_ptr<REQUESTHANDLER_CONFIG> pRequestHandlerConfig(pConfig);

    RETURN_IF_FAILED(EnsureOutOfProcessInitializtion(pHttpApplication));

    std::unique_ptr<OUT_OF_PROCESS_APPLICATION> pApplication = std::make_unique<OUT_OF_PROCESS_APPLICATION>(*pHttpApplication, std::move(pRequestHandlerConfig));

    RETURN_IF_FAILED(pApplication->Initialize());
    RETURN_IF_FAILED(pApplication->StartMonitoringAppOffline());

    *ppApplication = pApplication.release();
    return S_OK;
}
