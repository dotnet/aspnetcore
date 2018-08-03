// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationmanager.h"

#include "proxymodule.h"
#include "utility.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "EventLog.h"

extern BOOL         g_fInShutdown;

// The application manager is a singleton across ANCM.
APPLICATION_MANAGER* APPLICATION_MANAGER::sm_pApplicationManager = NULL;

//
// Retrieves the application info from the application manager
// Will create the application info if it isn't initalized
//
HRESULT
APPLICATION_MANAGER::GetOrCreateApplicationInfo(
    _In_ IHttpContext*         pHttpContext,
    _Out_ APPLICATION_INFO **  ppApplicationInfo
)
{
    HRESULT                 hr = S_OK;
    APPLICATION_INFO       *pApplicationInfo = NULL;
    BOOL                    fMixedHostingModelError = FALSE;
    BOOL                    fDuplicatedInProcessApp = FALSE;
    PCWSTR                  pszApplicationId = NULL;
    APP_HOSTING_MODEL       hostingModel = HOSTING_UNKNOWN;

    STACK_STRU ( strEventMsg, 256 );

    DBG_ASSERT(pHttpContext);
    DBG_ASSERT(ppApplicationInfo);

    *ppApplicationInfo = NULL;
    IHttpApplication &pApplication = *pHttpContext->GetApplication();

    // The configuration path is unique for each application and is used for the
    // key in the applicationInfoHash.
    pszApplicationId = pApplication.GetApplicationId();

    {
        // When accessing the m_pApplicationInfoHash, we need to acquire the application manager
        // lock to avoid races on setting state.
        SRWSharedLock readLock(m_srwLock);
        if (!m_fDebugInitialize)
        {
            DebugInitializeFromConfig(m_pHttpServer, pApplication);
            m_fDebugInitialize = TRUE;
        }

        if (g_fInShutdown)
        {
            FINISHED(HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS));
        }

        m_pApplicationInfoHash->FindKey(pszApplicationId, ppApplicationInfo);

        // It's important to release read lock here so exclusive lock
        // can be reacquired later as SRW lock doesn't allow upgrades
    }

    if (*ppApplicationInfo == NULL)
    {
        // Take exclusive lock before creating the application
        SRWExclusiveLock writeLock(m_srwLock);

        // Check if other thread created the application

        m_pApplicationInfoHash->FindKey(pszApplicationId, ppApplicationInfo);
        if (*ppApplicationInfo != NULL)
        {
            FINISHED(S_OK);
        }

        pApplicationInfo = new APPLICATION_INFO(m_pHttpServer);

        FINISHED_IF_FAILED(pApplicationInfo->Initialize(pApplication));

        hostingModel = pApplicationInfo->QueryConfig()->QueryHostingModel();

        if (m_pApplicationInfoHash->Count() == 0)
        {
            m_hostingModel = hostingModel;
            pApplicationInfo->MarkValid();
        }
        else
        {
            if (hostingModel == HOSTING_OUT_PROCESS &&  hostingModel == m_hostingModel)
            {
                pApplicationInfo->MarkValid();
            }
            else
            {
                if (hostingModel != m_hostingModel)
                {
                    fMixedHostingModelError = TRUE;
                }
                else
                {
                    fDuplicatedInProcessApp = TRUE;
                }
            }
        }

        FINISHED_IF_FAILED(m_pApplicationInfoHash->InsertRecord(pApplicationInfo));

        *ppApplicationInfo = pApplicationInfo;
        pApplicationInfo = NULL;
    }
Finished:

    // log the error
    if (fDuplicatedInProcessApp)
    {
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP,
            ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG,
            pszApplicationId);

    }
    else if (fMixedHostingModelError)
    {
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR,
            ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG,
            pszApplicationId,
            hostingModel);
    }

    if (pApplicationInfo != NULL)
    {
        pApplicationInfo->DereferenceApplicationInfo();
        pApplicationInfo = NULL;
    }

    return hr;
}


//
// If the application's configuration was changed,
// append the configuration path to the config change context.
//
BOOL
APPLICATION_MANAGER::FindConfigChangedApplication(
    _In_ APPLICATION_INFO *     pEntry,
    _In_ PVOID                  pvContext)
{
    DBG_ASSERT(pEntry);
    DBG_ASSERT(pvContext);

    // Config Change context contains the original config path that changed
    // and a multiStr containing
    CONFIG_CHANGE_CONTEXT* pContext = static_cast<CONFIG_CHANGE_CONTEXT*>(pvContext);
    STRU* pstruConfigPath = pEntry->QueryConfig()->QueryConfigPath();

    // check if the application path contains our app/subapp by seeing if the config path
    // starts with the notification path.
    BOOL fChanged = pstruConfigPath->StartsWith(pContext->pstrPath, true);
    if (fChanged)
    {
        DWORD dwLen = (DWORD)wcslen(pContext->pstrPath);
        WCHAR wChar = pstruConfigPath->QueryStr()[dwLen];

        // We need to check that the last character of the config path
        // is either a null terminator or a slash.
        // This checks the case where the config path was
        // MACHINE/WEBROOT/site and your site path is MACHINE/WEBROOT/siteTest
        if (wChar != L'\0' && wChar != L'/')
        {
            // not current app or sub app
            fChanged = FALSE;
        }
        else
        {
            pContext->MultiSz.Append(pEntry->QueryApplicationInfoKey());
        }
    }
    return fChanged;
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
    HRESULT                 hr = S_OK;
    DWORD                   dwPreviousCounter = 0;
    APPLICATION_INFO_HASH*  table = NULL;
    CONFIG_CHANGE_CONTEXT   context;

    if (g_fInShutdown)
    {
        // We are already shutting down, ignore this event as a global configuration change event
        // can occur after global stop listening for some reason.
        return hr;
    }

    {
        SRWExclusiveLock lock(m_srwLock);
        if (g_fInShutdown)
        {
            return hr;
        }

        // Make a shallow copy of existing hashtable as we may need to remove nodes
        // This will be used for finding differences in which applications are affected by a config change.
        table = new APPLICATION_INFO_HASH();

        // few application expected, small bucket size for hash table
        if (FAILED(hr = table->Initialize(17 /*prime*/)))
        {
            goto Finished;
        }

        context.pstrPath = pszApplicationId;

        // Keep track of the preview count of applications to know whether there are applications to delete
        dwPreviousCounter = m_pApplicationInfoHash->Count();

        // We don't want to hold the application manager lock for long time as it will block all incoming requests
        // Don't call application shutdown inside the lock
        m_pApplicationInfoHash->Apply(APPLICATION_INFO_HASH::ReferenceCopyToTable, static_cast<PVOID>(table));
        DBG_ASSERT(dwPreviousCounter == table->Count());

        // Removed the applications which are impacted by the configurtion change
        m_pApplicationInfoHash->DeleteIf(FindConfigChangedApplication, (PVOID)&context);

        if (m_pApplicationInfoHash->Count() == 0 && m_hostingModel == HOSTING_OUT_PROCESS)
        {
            // reuse current process
            m_hostingModel = HOSTING_UNKNOWN;
        }
    }

    // If we receive a request at this point.
    // OutOfProcess: we will create a new application with new configuration
    // InProcess: the request would have to be rejected, as we are about to call g_HttpServer->RecycleProcess
    // on the worker proocess
    if (!context.MultiSz.IsEmpty())
    {
        PCWSTR path = context.MultiSz.First();
        // Iterate through each of the paths that were shut down,
        // calling RecycleApplication on each of them.
        while (path != NULL)
        {
            APPLICATION_INFO* pRecord;

            // Application got recycled. Log an event
            STACK_STRU(strEventMsg, 256);
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_RECYCLE_CONFIGURATION_MSG,
                path)))
            {
                UTILITY::LogEvent(g_hEventLog,
                    EVENTLOG_INFORMATION_TYPE,
                    ASPNETCORE_EVENT_RECYCLE_CONFIGURATION,
                    strEventMsg.QueryStr());
            }

            table->FindKey(path, &pRecord);
            DBG_ASSERT(pRecord != NULL);

            // RecycleApplication is called on a separate thread.
            pRecord->RecycleApplication();
            pRecord->DereferenceApplicationInfo();
            path = context.MultiSz.Next(path);
        }
    }

Finished:
    if (table != NULL)
    {
        table->Clear();
        delete table;
    }

    if (FAILED(hr))
    {
        // Failed to recycle an application. Log an event
        STACK_STRU(strEventMsg, 256);
        if  (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_RECYCLE_FAILURE_CONFIGURATION_MSG,
                pszApplicationId)))
        {
            UTILITY::LogEvent(g_hEventLog,
                EVENTLOG_ERROR_TYPE,
                ASPNETCORE_EVENT_RECYCLE_APP_FAILURE,
                strEventMsg.QueryStr());
        }
        // Need to recycle the process as we cannot recycle the application
        if (!g_fRecycleProcessCalled)
        {
            g_fRecycleProcessCalled = TRUE;
            m_pHttpServer.RecycleProcess(L"AspNetCore Recycle Process on Demand Due Application Recycle Error");
        }
    }

    return hr;
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
    if (m_pApplicationInfoHash != NULL)
    {
        DBG_ASSERT(m_pApplicationInfoHash);

        // During shutdown we lock until we delete the application
        SRWExclusiveLock lock(m_srwLock);

        // Call shutdown on each application in the application manager
        m_pApplicationInfoHash->Apply(ShutdownApplication, NULL);
        m_pApplicationInfoHash->Clear();
        delete m_pApplicationInfoHash;
        m_pApplicationInfoHash = NULL;
    }
}

//
// Calls shutdown on each application. ApplicationManager's lock is held for duration of
// each shutdown call, guaranteeing another application cannot be created.
//
// static
VOID
APPLICATION_MANAGER::ShutdownApplication(
    _In_ APPLICATION_INFO *     pEntry,
    _In_ PVOID                  pvContext
)
{
    UNREFERENCED_PARAMETER(pvContext);
    DBG_ASSERT(pEntry != NULL);

    pEntry->ShutDownApplication();
}
