// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include <string>
#include "ShimOptions.h"
#include "hostfxroptions.h"
#include "HandleWrapper.h"
#include "ApplicationFactory.h"

class HandlerResolver
{
public:
    HandlerResolver(HMODULE hModule, IHttpServer &pServer);
    HRESULT GetApplicationFactory(IHttpApplication &pApplication, std::unique_ptr<ApplicationFactory>& pApplicationFactory);
    void ResetHostingModel();

private:
    HRESULT LoadRequestHandlerAssembly(IHttpApplication &pApplication, ShimOptions& pConfiguration, std::unique_ptr<ApplicationFactory>& pApplicationFactory);
    HRESULT FindNativeAssemblyFromGlobalLocation(ShimOptions& pConfiguration, PCWSTR libraryName, std::wstring& handlerDllPath);
    HRESULT FindNativeAssemblyFromHostfxr(HOSTFXR_OPTIONS& hostfxrOptions, PCWSTR libraryName, std::wstring& handlerDllPath);

    HMODULE m_hModule;
    IHttpServer &m_pServer;

    SRWLOCK      m_requestHandlerLoadLock {};
    std::wstring m_loadedApplicationId;
    APP_HOSTING_MODEL m_loadedApplicationHostingModel;
    HandleWrapper<ModuleHandleTraits> m_hHostFxrDll;

    static const PCWSTR          s_pwzAspnetcoreInProcessRequestHandlerName;
    static const PCWSTR          s_pwzAspnetcoreOutOfProcessRequestHandlerName;
    static const DWORD           s_initialGetNativeSearchDirectoriesBufferSize = MAX_PATH * 4;
};

