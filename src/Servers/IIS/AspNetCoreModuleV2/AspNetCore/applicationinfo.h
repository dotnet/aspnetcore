// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "HostFxrResolutionResult.h"
#include "iapplication.h"
#include "SRWSharedLock.h"
#include "HandlerResolver.h"

constexpr auto API_BUFFER_TOO_SMALL = 0x80008098;

extern BOOL     g_fRecycleProcessCalled;

class APPLICATION_INFO: NonCopyable
{
public:

    APPLICATION_INFO(
        IHttpServer &pServer,
        IHttpApplication    &pApplication,
        HandlerResolver     &pHandlerResolver
    ) :
        m_pServer(pServer),
        m_handlerResolver(pHandlerResolver),
        m_strConfigPath(pApplication.GetAppConfigPath()),
        m_strInfoKey(pApplication.GetApplicationId())
    {
        InitializeSRWLock(&m_applicationLock);
    }

    ~APPLICATION_INFO() = default;

    const std::wstring&
    QueryApplicationInfoKey() noexcept
    {
        return m_strInfoKey;
    }

    const std::wstring&
    QueryConfigPath() noexcept
    {
        return m_strConfigPath;
    }

    VOID
    ShutDownApplication(bool fServerInitiated);

    HRESULT
    CreateHandler(
        IHttpContext& pHttpContext,
        std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>& pHandler);

    bool ConfigurationPathApplies(const std::wstring& path)
    {
        // We need to check that the last character of the config path
        // is either a null terminator or a slash.
        // This checks the case where the config path was
        // MACHINE/WEBROOT/site and your site path is MACHINE/WEBROOT/siteTest
        auto const changed = m_strConfigPath._Starts_with(path);
        if (changed)
        {
            const auto lastChar = m_strConfigPath[m_strConfigPath.length()];
            return lastChar == L'\0' || lastChar == L'/';
        }
        return false;
    }

private:

    HRESULT
    TryCreateHandler(
        IHttpContext& pHttpContext,
        std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>& pHandler) const;

    HRESULT
    CreateApplication(IHttpContext& pHttpContext);

    HRESULT
    TryCreateApplication(IHttpContext& pHttpContext, const ShimOptions& options, ErrorContext& error);

    std::filesystem::path
    HandleShadowCopy(const ShimOptions& options, IHttpContext& pHttpContext);

    IHttpServer            &m_pServer;
    HandlerResolver        &m_handlerResolver;

    std::wstring            m_strConfigPath;
    std::wstring            m_strInfoKey;
    SRWLOCK                 m_applicationLock {};

    std::unique_ptr<ApplicationFactory> m_pApplicationFactory;
    std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER> m_pApplication;
};

