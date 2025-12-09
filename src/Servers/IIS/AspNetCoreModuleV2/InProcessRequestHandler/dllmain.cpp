// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// dllmain.cpp : Defines the entry point for the DLL application.

#include <VersionHelpers.h>

#include "inprocessapplication.h"
#include "inprocesshandler.h"
#include "requesthandler_config.h"
#include "debugutil.h"
#include "resources.h"
#include "exceptions.h"
#include "ShuttingDownApplication.h"
#include "InProcessOptions.h"
#include "EventLog.h"
#include "WebConfigConfigurationSource.h"
#include "ConfigurationLoadException.h"
#include "StartupExceptionApplication.h"
#include "file_utility.h"

DECLARE_DEBUG_PRINT_OBJECT("aspnetcorev2_inprocess.dll");

BOOL                g_fGlobalInitialize = FALSE;
BOOL                g_fProcessDetach = FALSE;
SRWLOCK             g_srwLockRH;
IHttpServer *       g_pHttpServer = nullptr;
HINSTANCE           g_hWinHttpModule;
HINSTANCE           g_hAspNetCoreModule;
HANDLE              g_hEventLog = nullptr;
bool                g_fInProcessApplicationCreated = false;
std::string         g_errorPageContent;
HINSTANCE           g_hServerModule;

HRESULT
InitializeGlobalConfiguration(
    IHttpServer * pServer,
    IHttpApplication* pHttpApplication
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
                g_hEventLog = RegisterEventSource(nullptr, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
            }
            else
            {
                g_hEventLog = RegisterEventSource(nullptr, ASPNETCORE_EVENT_PROVIDER);
            }

            DebugInitializeFromConfig(*pServer, *pHttpApplication);

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
    g_hServerModule = hModule;
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        InitializeSRWLock(&g_srwLockRH);
        DebugInitialize(hModule);
        break;
    case DLL_PROCESS_DETACH:
        g_fProcessDetach = TRUE;
        IN_PROCESS_HANDLER::StaticTerminate();
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
    _In_  IHttpServer           *pServer,
    _In_  IHttpApplication      *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION          **ppApplication
)
{
    TraceContextScope traceScope(FindParameter<IHttpTraceContext*>("TraceContext", pParameters, nParameters));
    const auto pSite = FindParameter<IHttpSite*>("Site", pParameters, nParameters);

    try
    {
        HRESULT hr = S_OK;
        RETURN_IF_FAILED(InitializeGlobalConfiguration(pServer, pHttpApplication));

        // In process application was already created so another call to CreateApplication
        // means that server is shutting does and request arrived in the meantime
        if (g_fInProcessApplicationCreated)
        {
            *ppApplication = new ShuttingDownApplication(*pServer, *pHttpApplication);
            return S_OK;
        }

        // never create two inprocess applications in one process
        g_fInProcessApplicationCreated = true;

        std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> inProcessApplication;

        ErrorContext errorContext;
        errorContext.statusCode = 500;
        errorContext.subStatusCode = 30;
        errorContext.generalErrorType = "ASP.NET Core app failed to start";
        errorContext.errorReason = "<ul><li>The app failed to start</li><li>The app started but then stopped</li><li>The app started but threw an exception during startup</li></ul>";

        if (!FAILED_LOG(hr = IN_PROCESS_APPLICATION::Start(*pServer, pSite, *pHttpApplication, pParameters, nParameters, inProcessApplication, errorContext)))
        {
            *ppApplication = inProcessApplication.release();
        }
        else
        {
            std::unique_ptr<InProcessOptions> options;
            THROW_IF_FAILED(InProcessOptions::Create(*pServer, pSite, *pHttpApplication, options));
            // Set the currently running application to a fake application that returns startup exceptions.
            auto content = !g_errorPageContent.empty() ?
                g_errorPageContent :
                FILE_UTILITY::GetHtml(g_hServerModule,
                    ANCM_ERROR_PAGE,
                    errorContext.statusCode,
                    errorContext.subStatusCode,
                    errorContext.generalErrorType,
                    errorContext.errorReason);

            auto pErrorApplication = std::make_unique<StartupExceptionApplication>(*pServer,
                *pHttpApplication,
                options->QueryDisableStartUpErrorPage(),
                hr,
                content,
                errorContext.statusCode,
                errorContext.subStatusCode,
                "Internal Server Error");

            RETURN_IF_FAILED(pErrorApplication->StartMonitoringAppOffline());
            *ppApplication = pErrorApplication.release();
        }
        return S_OK;
    }
    CATCH_RETURN();
}
