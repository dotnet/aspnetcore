// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include <string>
#include "ShimOptions.h"
#include "hostfxroptions.h"
#include "HandleWrapper.h"
#include "ApplicationFactory.h"
#include "BaseOutputManager.h"

class HandlerResolver
{
public:
    HandlerResolver(HMODULE hModule, const IHttpServer &pServer);
    HRESULT GetApplicationFactory(const IHttpApplication &pApplication, std::unique_ptr<ApplicationFactory>& pApplicationFactory, const ShimOptions& options);
    void ResetHostingModel();

private:
    HRESULT LoadRequestHandlerAssembly(const IHttpApplication &pApplication, const ShimOptions& pConfiguration, std::unique_ptr<ApplicationFactory>& pApplicationFactory);
    HRESULT FindNativeAssemblyFromGlobalLocation(const ShimOptions& pConfiguration, PCWSTR libraryName, std::wstring& handlerDllPath);
    HRESULT FindNativeAssemblyFromHostfxr(const HOSTFXR_OPTIONS& hostfxrOptions, PCWSTR libraryName, std::wstring& handlerDllPath, BaseOutputManager* outputManager);

    HMODULE m_hModule;
    const IHttpServer &m_pServer;

    SRWLOCK      m_requestHandlerLoadLock {};
    std::wstring m_loadedApplicationId;
    APP_HOSTING_MODEL m_loadedApplicationHostingModel;
    HandleWrapper<ModuleHandleTraits> m_hHostFxrDll;

    static const PCWSTR          s_pwzAspnetcoreInProcessRequestHandlerName;
    static const PCWSTR          s_pwzAspnetcoreOutOfProcessRequestHandlerName;
    static const DWORD           s_initialGetNativeSearchDirectoriesBufferSize = MAX_PATH * 4;
};

