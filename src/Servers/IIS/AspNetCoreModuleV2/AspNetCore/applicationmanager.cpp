// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationmanager.h"

#include "proxymodule.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "EventLog.h"

extern BOOL         g_fInShutdown;
extern BOOL         g_fInAppOfflineShutdown;

//
// Retrieves the application info from the application manager
// Will create the application info if it isn't initialized
//
HRESULT
APPLICATION_MANAGER::GetOrCreateApplicationInfo(
    _In_ IHttpContext& pHttpContext,
    _Out_ std::shared_ptr<APPLICATION_INFO>& ppApplicationInfo
)
{
    auto &pApplication = *pHttpContext.GetApplication();

    // The configuration path is unique for each application and is used for the
    // key in the applicationInfoHash.
    std::wstring pszApplicationId = pApplication.GetApplicationId();

    {
        // When accessing the m_pApplicationInfoHash, we need to acquire the application manager
        // lock to avoid races on setting state.
        SRWSharedLock readLock(m_srwLock);

        if (g_fInShutdown)
        {
            return HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS);
        }

        const auto pair = m_pApplicationInfoHash.find(pszApplicationId);
        if (pair != m_pApplicationInfoHash.end())
        {
            ppApplicationInfo = pair->second;
            return S_OK;
        }

        // It's important to release read lock here so exclusive lock
        // can be reacquired later as SRW lock doesn't allow upgrades
    }

    // Take exclusive lock before creating the application
    SRWExclusiveLock writeLock(m_srwLock);

    if (!m_fDebugInitialize)
    {
        DebugInitializeFromConfig(m_pHttpServer, pApplication);
        m_fDebugInitialize = TRUE;
    }

    // Check if other thread created the application
    const auto pair = m_pApplicationInfoHash.find(pszApplicationId);
    if (pair != m_pApplicationInfoHash.end())
    {
        ppApplicationInfo = pair->second;
        return S_OK;
    }

    ppApplicationInfo = std::make_shared<APPLICATION_INFO>(m_pHttpServer, pApplication, m_handlerResolver);
    m_pApplicationInfoHash.emplace(pszApplicationId, ppApplicationInfo);

    return S_OK;
}

//
// Finds any applications affected by a configuration change and calls Recycle on them
// InProcess:  Triggers g_httpServer->RecycleProcess() and keep the application inside of the manager.
//             This will cause a shutdown event to occur through the global stop listening event.
// OutOfProcess: Removes all applications in the application manager and calls Recycle, which will call Shutdown,
//             on each application.
//
HRESULT
APPLICATION_MANAGER::RecycleApplicationFromManager(
    _In_ const LPCWSTR pszApplicationId
)
{
    try
    {
        std::vector<std::shared_ptr<APPLICATION_INFO>> applicationsToRecycle;

        if (g_fInShutdown)
        {
            // We are already shutting down, ignore this event as a global configuration change event
            // can occur after global stop listening for some reason.
            return S_OK;
        }

        {
            SRWExclusiveLock lock(m_srwLock);
            if (g_fInShutdown)
            {
                return S_OK;
            }
            const std::wstring configurationPath = pszApplicationId;

            auto itr = m_pApplicationInfoHash.begin();
            while (itr != m_pApplicationInfoHash.end())
            {
                if (itr->second->ConfigurationPathApplies(configurationPath))
                {
                    applicationsToRecycle.emplace_back(itr->second);
                    // Delay deleting an in-process app until after shutting the application down to avoid creating
                    // another application info, which would just return app_offline.
                    if (m_handlerResolver.GetHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
                    {
                        ++itr;
                    }
                    else
                    {
                        itr = m_pApplicationInfoHash.erase(itr);
                    }
                }
                else
                {
                    ++itr;

                }
            }

            if (m_handlerResolver.GetHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
            {
                // For detecting app_offline when the app_offline file isn't present.
                // Normally, app_offline state is independent of application
                // (Just checks for app_offline file).
                // For shadow copying, we need some other indication that the app is offline.
                g_fInAppOfflineShutdown = true;
            }

            // All applications were unloaded reset handler resolver validation logic
            if (m_pApplicationInfoHash.empty())
            {
                m_handlerResolver.ResetHostingModel();
            }
        }

        if (!applicationsToRecycle.empty())
        {
            for (auto& application : applicationsToRecycle)
            {
                try
                {
                    if (UseLegacyShutdown())
                    {
                        application->ShutDownApplication(/* fServerInitiated */ false);
                    }
                    else
                    {
                        // Recycle the process to trigger OnGlobalStopListening
                        // which will shutdown the server and stop listening for new requests for this app
                        m_pHttpServer.RecycleProcess(L"AspNetCore InProcess Recycle Process on Demand");
                    }
                }
                catch (...)
                {
                    LOG_ERRORF(L"Failed to recycle application '%ls'", application->QueryApplicationInfoKey().c_str());
                    OBSERVE_CAUGHT_EXCEPTION()

                    // Failed to recycle an application. Log an event
                    EventLog::Error(
                        ASPNETCORE_EVENT_RECYCLE_APP_FAILURE,
                        ASPNETCORE_EVENT_RECYCLE_FAILURE_CONFIGURATION_MSG,
                        pszApplicationId);
                    // Need to recycle the process as we cannot recycle the application
                    if (!g_fRecycleProcessCalled)
                    {
                        g_fRecycleProcessCalled = TRUE;
                        m_pHttpServer.RecycleProcess(L"AspNetCore Recycle Process on Demand Due Application Recycle Error");
                    }
                }
            }
        }

        if (UseLegacyShutdown())
        {
            // Remove apps after calling shutdown on each of them
            // This is exclusive to in-process, as the shutdown of an in-process app recycles
            // the entire worker process.
            if (m_handlerResolver.GetHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
            {
                SRWExclusiveLock lock(m_srwLock);
                const std::wstring configurationPath = pszApplicationId;

                auto itr = m_pApplicationInfoHash.begin();
                while (itr != m_pApplicationInfoHash.end())
                {
                    if (itr->second != nullptr && itr->second->ConfigurationPathApplies(configurationPath)
                        && std::find(applicationsToRecycle.begin(), applicationsToRecycle.end(), itr->second) != applicationsToRecycle.end())
                    {
                        itr = m_pApplicationInfoHash.erase(itr);
                    }
                    else
                    {
                        ++itr;
                    }
                }
            } // Release Exclusive m_srwLock
        }
    }
    CATCH_RETURN()

    return S_OK;
}

//
// Shutsdown all applications in the application hashtable
// Only called by OnGlobalStopListening.
//
VOID
APPLICATION_MANAGER::ShutDown()
{
    // During shutdown we lock until we delete the application
    SRWExclusiveLock lock(m_srwLock);

    // We are guaranteed to only have one outstanding OnGlobalStopListening event at a time
    // However, it is possible to receive multiple OnGlobalStopListening events
    // Protect against this by checking if we already shut down.
    if (g_fInShutdown)
    {
        return;
    }

    g_fInShutdown = TRUE;
    g_fInAppOfflineShutdown = true;
    for (auto & [str, applicationInfo] : m_pApplicationInfoHash)
    {
        applicationInfo->ShutDownApplication(/* fServerInitiated */ true);
        applicationInfo = nullptr;
    }
}
