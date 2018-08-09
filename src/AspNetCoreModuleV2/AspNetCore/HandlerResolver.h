// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "aspnetcore_shim_config.h"
#include "hostfxroptions.h"
#include <memory>
#include "iapplication.h"
#include <string>
#include "HandleWrapper.h"

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_APPLICATION)(
    _In_  IHttpServer           *pServer,
    _In_  IHttpApplication      *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION         **pApplication
    );

class HandlerResolver
{
public:
    HandlerResolver(HMODULE hModule, IHttpServer &pServer);
    HRESULT GetApplicationFactory(IHttpApplication &pApplication, STRU& location, PFN_ASPNETCORE_CREATE_APPLICATION *pfnCreateApplication);

private:
    HRESULT LoadRequestHandlerAssembly(STRU& location, ASPNETCORE_SHIM_CONFIG * pConfiguration);
    HRESULT FindNativeAssemblyFromGlobalLocation(ASPNETCORE_SHIM_CONFIG * pConfiguration, PCWSTR libraryName, STRU* location);
    HRESULT FindNativeAssemblyFromHostfxr(HOSTFXR_OPTIONS* hostfxrOptions, PCWSTR libraryName, STRU* location);

    HMODULE m_hModule;
    IHttpServer &m_pServer;

    SRWLOCK      m_requestHandlerLoadLock {};
    // S_FALSE - not loaded, S_OK - loaded, everything else - error
    HRESULT      m_fAspnetcoreRHLoadResult;
    std::wstring m_loadedApplicationId;
    APP_HOSTING_MODEL m_loadedApplicationHostingModel;
    HandleWrapper<ModuleHandleTraits> m_hRequestHandlerDll;
    HandleWrapper<ModuleHandleTraits> m_hHostFxrDll;

    PFN_ASPNETCORE_CREATE_APPLICATION      m_pfnAspNetCoreCreateApplication;

    static const PCWSTR          s_pwzAspnetcoreInProcessRequestHandlerName;
    static const PCWSTR          s_pwzAspnetcoreOutOfProcessRequestHandlerName;
};

