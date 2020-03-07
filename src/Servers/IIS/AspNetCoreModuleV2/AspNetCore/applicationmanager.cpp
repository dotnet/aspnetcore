// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationmanager.h"

#include "proxymodule.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "EventLog.h"

extern BOOL         g_fInShutdown;

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
    _In_ LPCWSTR pszApplicationId
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
                    itr = m_pApplicationInfoHash.erase(itr);
                }
                else
                {
                    ++itr;
                }
            }

            // All applications were unloaded reset handler resolver validation logic
            if (m_pApplicationInfoHash.empty())
            {
                m_handlerResolver.ResetHostingModel();
            }
        }

        // If we receive a request at this point.
        // OutOfProcess: we will create a new application with new configuration
        // InProcess: the request would have to be rejected, as we are about to call g_HttpServer->RecycleProcess
        // on the worker proocess

        if (!applicationsToRecycle.empty())
        {
            for (auto& application : applicationsToRecycle)
            {
                try
                {
                    application->ShutDownApplication(/* fServerInitiated */ false);
                }
                catch (...)
                {
                    LOG_ERRORF(L"Failed to stop application '%ls'", application->QueryApplicationInfoKey().c_str());
                    OBSERVE_CAUGHT_EXCEPTION();

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
    }
    CATCH_RETURN();

    return S_OK;
}

//
// Shutsdown all applications in the application hashtable
// Only called by OnGlobalStopListening.
//
VOID
APPLICATION_MANAGER::ShutDown()
{
    // We are guaranteed to only have one outstanding OnGlobalStopListening event at a time
    // However, it is possible to receive multiple OnGlobalStopListening events
    // Protect against this by checking if we already shut down.
    g_fInShutdown = TRUE;

    // During shutdown we lock until we delete the application
    SRWExclusiveLock lock(m_srwLock);
    for (auto &pair : m_pApplicationInfoHash)
    {
        pair.second->ShutDownApplication(/* fServerInitiated */ true);
        pair.second = nullptr;
    }
}
