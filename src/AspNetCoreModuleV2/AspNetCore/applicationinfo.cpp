// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include <array>
#include "proxymodule.h"
#include "hostfxr_utility.h"
#include "utility.h"
#include "debugutil.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "GlobalVersionUtility.h"
#include "exceptions.h"
#include "EventLog.h"
#include "HandleWrapper.h"
#include "ServerErrorApplication.h"
#include "AppOfflineApplication.h"

APPLICATION_INFO::~APPLICATION_INFO()
{
    ShutDownApplication();
}

HRESULT
APPLICATION_INFO::Initialize(
    _In_ IHttpApplication         &pApplication,
    HandlerResolver * pHandlerResolver
)
{
    m_handlerResolver = pHandlerResolver;
    RETURN_IF_FAILED(m_struConfigPath.Copy(pApplication.GetAppConfigPath()));
    RETURN_IF_FAILED(m_struInfoKey.Copy(pApplication.GetApplicationId()));
    return S_OK;
}


HRESULT
APPLICATION_INFO::GetOrCreateApplication(
    IHttpContext *pHttpContext,
    std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>& pApplication
)
{
    HRESULT             hr = S_OK;

    SRWExclusiveLock lock(m_applicationLock);

    auto& httpApplication = *pHttpContext->GetApplication();

    if (m_pApplication != nullptr)
    {
        if (m_pApplication->QueryStatus() == RECYCLED)
        {
            LOG_INFO("Application went offline");

            // Call to wait for application to complete stopping
            m_pApplication->Stop(/* fServerInitiated */ false);
            m_pApplication = nullptr;
        }
        else
        {
            // another thread created the application
            FINISHED(S_OK);
        }
    }

    if (AppOfflineApplication::ShouldBeStarted(httpApplication))
    {
        LOG_INFO("Detected app_offline file, creating polling application");
        m_pApplication.reset(new AppOfflineApplication(httpApplication));
    }
    else
    {
        STRU struExeLocation;
        PFN_ASPNETCORE_CREATE_APPLICATION      pfnAspNetCoreCreateApplication;
        FINISHED_IF_FAILED(m_handlerResolver->GetApplicationFactory(httpApplication, struExeLocation, &pfnAspNetCoreCreateApplication));
        std::array<APPLICATION_PARAMETER, 1> parameters {
            {"InProcessExeLocation", struExeLocation.QueryStr()}
        };

        LOG_INFO("Creating handler application");
        IAPPLICATION * newApplication;
        FINISHED_IF_FAILED(pfnAspNetCoreCreateApplication(
            &m_pServer,
            &httpApplication,
            parameters.data(),
            static_cast<DWORD>(parameters.size()),
            &newApplication));

        m_pApplication.reset(newApplication);
    }

Finished:

    if (FAILED(hr))
    {
        // Log the failure and update application info to not try again
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
            httpApplication.GetApplicationId(),
            hr);

        m_pApplication.reset(new ServerErrorApplication(httpApplication, hr));
    }

    if (m_pApplication)
    {
        pApplication = ReferenceApplication(m_pApplication.get());
    }

    return hr;
}

VOID
APPLICATION_INFO::RecycleApplication()
{
    SRWExclusiveLock lock(m_applicationLock);

    if (m_pApplication)
    {
        const auto pApplication = m_pApplication.release();

        HandleWrapper<InvalidHandleTraits> hThread = CreateThread(
            NULL,       // default security attributes
            0,          // default stack size
            (LPTHREAD_START_ROUTINE)DoRecycleApplication,
            pApplication,       // thread function arguments
            0,          // default creation flags
            NULL);      // receive thread identifier
    }
}


DWORD WINAPI
APPLICATION_INFO::DoRecycleApplication(
    LPVOID lpParam)
{
    auto pApplication = std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>(static_cast<IAPPLICATION*>(lpParam));

    if (pApplication)
    {
        // Recycle will call shutdown for out of process
        pApplication->Stop(/*fServerInitiated*/ false);
    }

    return 0;
}


VOID
APPLICATION_INFO::ShutDownApplication()
{
    SRWExclusiveLock lock(m_applicationLock);

    if (m_pApplication)
    {
        m_pApplication ->Stop(/* fServerInitiated */ true);
        m_pApplication = nullptr;
    }
}
