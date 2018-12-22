// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include "proxymodule.h"
#include "hostfxr_utility.h"
#include "debugutil.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "EventLog.h"
#include "ServerErrorApplication.h"
#include "AppOfflineApplication.h"
#include "WebConfigConfigurationSource.h"
#include "ConfigurationLoadException.h"
#include "resource.h"

extern HINSTANCE           g_hServerModule;

HRESULT
APPLICATION_INFO::CreateHandler(
    IHttpContext& pHttpContext,
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>& pHandler
)
{
    HRESULT             hr = S_OK;

    {
        SRWSharedLock lock(m_applicationLock);

        RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));

        if (hr == S_OK)
        {
            return S_OK;
        }
    }

    {
        SRWExclusiveLock lock(m_applicationLock);

        // check if other thread created application
        RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));

        // In some cases (adding and removing app_offline quickly) application might start and stop immediately
        // so retry until we get valid handler or error
        while (hr != S_OK)
        {
            // At this point application is either null or shutdown and is returning S_FALSE

            if (m_pApplication != nullptr)
            {
                LOG_INFO(L"Application went offline");

                // Call to wait for application to complete stopping
                m_pApplication->Stop(/* fServerInitiated */ false);
                m_pApplication = nullptr;
                m_pApplicationFactory = nullptr;
            }

            RETURN_IF_FAILED(CreateApplication(pHttpContext));

            RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));
        }
    }

    return S_OK;
}

HRESULT
APPLICATION_INFO::CreateApplication(IHttpContext& pHttpContext)
{
    auto& pHttpApplication = *pHttpContext.GetApplication();
    if (AppOfflineApplication::ShouldBeStarted(pHttpApplication))
    {
        LOG_INFO(L"Detected app_offline file, creating polling application");
        m_pApplication = make_application<AppOfflineApplication>(pHttpApplication);

        return S_OK;
    }
    else
    {
        try
        {
            const WebConfigConfigurationSource configurationSource(m_pServer.GetAdminManager(), pHttpApplication);
            ShimOptions options(configurationSource);

            const auto hr = TryCreateApplication(pHttpContext, options);

            if (FAILED_LOG(hr))
            {
                // Log the failure and update application info to not try again
                EventLog::Error(
                    ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
                    ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
                    pHttpApplication.GetApplicationId(),
                    hr);

                m_pApplication = make_application<ServerErrorApplication>(
                    pHttpApplication,
                    hr,
                    g_hServerModule,
                    options.QueryDisableStartupPage(),
                    options.QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS ? IN_PROCESS_SHIM_STATIC_HTML : OUT_OF_PROCESS_SHIM_STATIC_HTML);
            }
            return S_OK;
        }
        catch (const ConfigurationLoadException &ex)
        {
            EventLog::Error(
                ASPNETCORE_CONFIGURATION_LOAD_ERROR,
                ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
                ex.get_message().c_str());
        }
        catch (...)
        {
            EventLog::Error(
                ASPNETCORE_CONFIGURATION_LOAD_ERROR,
                ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
                L"");
        }

        m_pApplication = make_application<ServerErrorApplication>(
            pHttpApplication,
            E_FAIL,
            g_hServerModule);

        return S_OK;
    }
}

HRESULT
APPLICATION_INFO::TryCreateApplication(IHttpContext& pHttpContext, const ShimOptions& options)
{
    RETURN_IF_FAILED(m_handlerResolver.GetApplicationFactory(*pHttpContext.GetApplication(), m_pApplicationFactory, options));
    LOG_INFO(L"Creating handler application");

    IAPPLICATION * newApplication;
    RETURN_IF_FAILED(m_pApplicationFactory->Execute(
        &m_pServer,
        &pHttpContext,
        &newApplication));

    m_pApplication.reset(newApplication);
    return S_OK;
}

HRESULT
APPLICATION_INFO::TryCreateHandler(
    IHttpContext& pHttpContext,
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>& pHandler)
{
    if (m_pApplication != nullptr)
    {
        IREQUEST_HANDLER * newHandler;
        const auto result = m_pApplication->TryCreateHandler(&pHttpContext, &newHandler);
        RETURN_IF_FAILED(result);

        if (result == S_OK)
        {
            pHandler.reset(newHandler);
            // another thread created the application
            return S_OK;
        }
    }
    return S_FALSE;
}

VOID
APPLICATION_INFO::ShutDownApplication(bool fServerInitiated)
{
    SRWExclusiveLock lock(m_applicationLock);

    if (m_pApplication)
    {
        LOG_INFOF(L"Stopping application '%ls'", QueryApplicationInfoKey().c_str());
        m_pApplication->Stop(fServerInitiated);
        m_pApplication = nullptr;
        m_pApplicationFactory = nullptr;
    }
}
