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

            RETURN_IF_FAILED(CreateApplication(*pHttpContext.GetApplication()));

            RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));
        }
    }

    return S_OK;
}

HRESULT
APPLICATION_INFO::CreateApplication(const IHttpApplication& pHttpApplication)
{
    HRESULT hr = S_OK;

    if (AppOfflineApplication::ShouldBeStarted(pHttpApplication))
    {
        LOG_INFO(L"Detected app_offline file, creating polling application");
        #pragma warning( push )
        #pragma warning ( disable : 26409 ) // Disable "Avoid using new", using custom deleter here
        m_pApplication.reset(new AppOfflineApplication(pHttpApplication));
        #pragma warning( pop )
    }
    else
    {
        FINISHED_IF_FAILED(m_handlerResolver.GetApplicationFactory(pHttpApplication, m_pApplicationFactory));

        LOG_INFO(L"Creating handler application");
        IAPPLICATION * newApplication;
        FINISHED_IF_FAILED(m_pApplicationFactory->Execute(
            &m_pServer,
            &pHttpApplication,
            &newApplication));

        m_pApplication.reset(newApplication);
    }

Finished:

    if (m_pApplication == nullptr || FAILED(hr))
    {
        // Log the failure and update application info to not try again
        EventLog::Error(
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
            pHttpApplication.GetApplicationId(),
            hr);
        
        #pragma warning( push )
        #pragma warning ( disable : 26409 ) // Disable "Avoid using new", using custom deleter here
        m_pApplication.reset(new ServerErrorApplication(pHttpApplication, hr));
        #pragma warning( pop )
    }

    return hr;
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
        m_pApplication ->Stop(fServerInitiated);
        m_pApplication = nullptr;
        m_pApplicationFactory = nullptr;
    }
}
