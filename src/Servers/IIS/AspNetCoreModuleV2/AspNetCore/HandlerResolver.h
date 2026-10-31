// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include <string>
#include "ShimOptions.h"
#include "HostFxrResolutionResult.h"
#include "ApplicationFactory.h"
#include "RedirectionOutput.h"
#include "HostFxr.h"

class HandlerResolver
{
public:
    HandlerResolver(HMODULE hModule, const IHttpServer &pServer);
    HRESULT GetApplicationFactory(const IHttpApplication &pApplication, const std::filesystem::path& shadowCopyPath, std::unique_ptr<ApplicationFactory>& pApplicationFactory, const ShimOptions& options, ErrorContext& errorContext);
    void ResetHostingModel();
    APP_HOSTING_MODEL GetHostingModel();
    bool GetDisallowRotationOnConfigChange();
    std::chrono::milliseconds GetShutdownDelay() const;

private:
    HRESULT LoadRequestHandlerAssembly(const IHttpApplication &pApplication, const std::filesystem::path& shadowCopyPath, const ShimOptions& pConfiguration, std::unique_ptr<ApplicationFactory>& pApplicationFactory, ErrorContext& errorContext);
    HRESULT FindNativeAssemblyFromGlobalLocation(const ShimOptions& pConfiguration, PCWSTR libraryName, std::wstring& handlerDllPath);
    HRESULT FindNativeAssemblyFromHostfxr(
        const HostFxrResolutionResult& hostfxrOptions,
        PCWSTR libraryName,
        std::wstring& handlerDllPath,
        const IHttpApplication &pApplication,
        const ShimOptions& pConfiguration,
        const std::shared_ptr<StringStreamRedirectionOutput>& stringRedirectionOutput,
        ErrorContext& errorContext);

    HMODULE m_hModule;
    const IHttpServer &m_pServer;

    SRWLOCK      m_requestHandlerLoadLock {};
    std::wstring m_loadedApplicationId;
    APP_HOSTING_MODEL m_loadedApplicationHostingModel;
    HostFxr m_hHostFxrDll;
    bool m_disallowRotationOnConfigChange;
    std::chrono::milliseconds m_shutdownDelay;

    static const PCWSTR          s_pwzAspnetcoreInProcessRequestHandlerName;
    static const PCWSTR          s_pwzAspnetcoreOutOfProcessRequestHandlerName;
    static const DWORD           s_initialGetNativeSearchDirectoriesBufferSize = MAX_PATH * 4;
};

