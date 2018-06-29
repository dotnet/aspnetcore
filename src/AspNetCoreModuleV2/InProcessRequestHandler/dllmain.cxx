// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// dllmain.cpp : Defines the entry point for the DLL application.

#include "precomp.hxx"
#include <IPHlpApi.h>
#include <VersionHelpers.h>

#include "inprocessapplication.h"
#include "StartupExceptionApplication.h"
#include "inprocesshandler.h"
#include "requesthandler_config.h"
#include "debugutil.h"
#include "resources.h"
#include "exceptions.h"

DECLARE_DEBUG_PRINT_OBJECT("aspnetcorev2_inprocess.dll");

BOOL                g_fGlobalInitialize = FALSE;
BOOL                g_fProcessDetach = FALSE;
SRWLOCK             g_srwLockRH;
IHttpServer *       g_pHttpServer = NULL;
HINSTANCE           g_hWinHttpModule;
HINSTANCE           g_hAspNetCoreModule;
HANDLE              g_hEventLog = NULL;

HRESULT
InitializeGlobalConfiguration(
    IHttpServer * pServer
)
{
    if (!g_fGlobalInitialize)
    {
        SRWExclusiveLock lock(g_srwLockRH);

        if (!g_fGlobalInitialize)
        {
            g_pHttpServer = pServer;
            RETURN_IF_FAILED(ALLOC_CACHE_HANDLER::StaticInitialize());
            RETURN_IF_FAILED(IN_PROCESS_HANDLER::StaticInitialize());

            if (pServer->IsCommandLineLaunch())
            {
                g_hEventLog = RegisterEventSource(NULL, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
            }
            else
            {
                g_hEventLog = RegisterEventSource(NULL, ASPNETCORE_EVENT_PROVIDER);
            }

            DebugInitialize();
            g_fGlobalInitialize = TRUE;
        }
    }

    return S_OK;
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
    _In_  IHttpServer           *pServer,
    _In_  IHttpApplication      *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION          **ppApplication
)
{
    REQUESTHANDLER_CONFIG  *pConfig = NULL;

    try
    {
        // Initialze some global variables here
        RETURN_IF_FAILED(InitializeGlobalConfiguration(pServer));
        RETURN_IF_FAILED(REQUESTHANDLER_CONFIG::CreateRequestHandlerConfig(pServer, pHttpApplication, &pConfig));

        auto config = std::unique_ptr<REQUESTHANDLER_CONFIG>(pConfig);

        const bool disableStartupPage = pConfig->QueryDisableStartUpErrorPage();

        auto pApplication = std::make_unique<IN_PROCESS_APPLICATION>(pServer, std::move(config), pParameters, nParameters);

        if (FAILED_LOG(pApplication->LoadManagedApplication()))
        {
            // Set the currently running application to a fake application that returns startup exceptions.
            *ppApplication = new StartupExceptionApplication(pServer, disableStartupPage);
        }
        else
        {
            *ppApplication = pApplication.release();
        }
    }
    CATCH_RETURN();

    return S_OK;
}
