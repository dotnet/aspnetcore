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
APPLICATION_INFO::GetOrCreateApplication(
    IHttpContext& pHttpContext,
    std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>& pApplication
)
{
    HRESULT             hr = S_OK;

    SRWExclusiveLock lock(m_applicationLock);

    auto& httpApplication = *pHttpContext.GetApplication();

    if (m_pApplication != nullptr)
    {
        if (m_pApplication->QueryStatus() == RECYCLED)
        {
            LOG_INFO(L"Application went offline");

            // Call to wait for application to complete stopping
            m_pApplication->Stop(/* fServerInitiated */ false);
            m_pApplication = nullptr;
            m_pApplicationFactory = nullptr;
        }
        else
        {
            // another thread created the application
            FINISHED(S_OK);
        }
    }

    if (AppOfflineApplication::ShouldBeStarted(httpApplication))
    {
        LOG_INFO(L"Detected app_offline file, creating polling application");
        #pragma warning( push )
        #pragma warning ( disable : 26409 ) // Disable "Avoid using new", using custom deleter here
        m_pApplication.reset(new AppOfflineApplication(httpApplication));
        #pragma warning( pop )
    }
    else
    {
        FINISHED_IF_FAILED(m_handlerResolver.GetApplicationFactory(httpApplication, m_pApplicationFactory));

        LOG_INFO(L"Creating handler application");
        IAPPLICATION * newApplication;
        FINISHED_IF_FAILED(m_pApplicationFactory->Execute(
            &m_pServer,
            &httpApplication,
            &newApplication));

        m_pApplication.reset(newApplication);
    }

Finished:

    if (FAILED(hr))
    {
        // Log the failure and update application info to not try again
        EventLog::Error(
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
            httpApplication.GetApplicationId(),
            hr);
        
        #pragma warning( push )
        #pragma warning ( disable : 26409 ) // Disable "Avoid using new", using custom deleter here
        m_pApplication.reset(new ServerErrorApplication(httpApplication, hr));
        #pragma warning( pop )
    }

    if (m_pApplication)
    {
        pApplication = ReferenceApplication(m_pApplication.get());
    }

    return hr;
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
