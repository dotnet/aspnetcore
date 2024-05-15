// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"
#include "applicationmanager.h"
#include "proxymodule.h"
#include "globalmodule.h"
#include "acache.h"
#include "debugutil.h"
#include "resources.h"
#include "exceptions.h"
#include "EventLog.h"
#include "RegistryKey.h"

DECLARE_DEBUG_PRINT_OBJECT("aspnetcorev2.dll");

HANDLE              g_hEventLog = nullptr;
BOOL                g_fRecycleProcessCalled = FALSE;
BOOL                g_fInShutdown = FALSE;
BOOL                g_fInAppOfflineShutdown = FALSE;
HINSTANCE           g_hServerModule;
DWORD               g_dwIISServerVersion;

VOID
StaticCleanup()
{
    if (g_hEventLog != nullptr)
    {
        DeregisterEventSource(g_hEventLog);
        g_hEventLog = nullptr;
    }

    DebugStop();
    ALLOC_CACHE_HANDLER::StaticTerminate();
}

BOOL WINAPI DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
    )
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:

        ALLOC_CACHE_HANDLER::StaticInitialize();
        g_hServerModule = hModule;
        DisableThreadLibraryCalls(hModule);
        DebugInitialize(hModule);
        break;
    case DLL_PROCESS_DETACH:

        // IIS can cause dll detach to occur before we receive global notifications
        // For example, when we switch the bitness of the worker process,
        // this is a bug in IIS. To try to avoid AVs, we will set a global flag
        g_fInShutdown = TRUE;
        StaticCleanup();
    default:
        break;
    }

    return TRUE;
}

HRESULT
__stdcall
RegisterModule(
DWORD                           dwServerVersion,
IHttpModuleRegistrationInfo *   pModuleInfo,
IHttpServer *                   pHttpServer
) try
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
    g_dwIISServerVersion = dwServerVersion;

    if (pHttpServer->IsCommandLineLaunch())
    {
        g_hEventLog = RegisterEventSource(nullptr, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
    }
    else
    {
        g_hEventLog = RegisterEventSource(nullptr, ASPNETCORE_EVENT_PROVIDER);
    }

    auto fDisableModule = RegistryKey::TryGetDWORD(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\IIS Extensions\\IIS AspNetCore Module V2\\Parameters", L"DisableANCM");

    if (fDisableModule.has_value() && fDisableModule.value() != 0)
    {
        EventLog::Warn(ASPNETCORE_EVENT_MODULE_DISABLED, ASPNETCORE_EVENT_MODULE_DISABLED_MSG);
        // this will return 500 error to client
        // as we did not register the module
        return S_OK;
    }

    //
    // Create the factory before any static initialization.
    // The ASPNET_CORE_PROXY_MODULE_FACTORY::Terminate method will clean any
    // static object initialized.
    //

    auto applicationManager = std::make_shared<APPLICATION_MANAGER>(g_hServerModule, *pHttpServer);
    auto moduleFactory = std::make_unique<ASPNET_CORE_PROXY_MODULE_FACTORY>(pModuleInfo->GetId(), applicationManager);

    RETURN_IF_FAILED(pModuleInfo->SetRequestNotifications(
                                  moduleFactory.release(),
                                  RQ_EXECUTE_REQUEST_HANDLER,
                                  0));

    auto pGlobalModule = std::make_unique<ASPNET_CORE_GLOBAL_MODULE>(std::move(applicationManager));

    RETURN_IF_FAILED(pModuleInfo->SetGlobalNotifications(
        pGlobalModule.release(),
        GL_CONFIGURATION_CHANGE | // Configuration change triggers IIS application stop
        GL_STOP_LISTENING | // worker process will stop listening for http requests
        GL_APPLICATION_STOP)); // app pool recycle or stop

    return S_OK;
}
CATCH_RETURN()
