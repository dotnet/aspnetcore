// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

APPLICATION_MANAGER* APPLICATION_MANAGER::sm_pApplicationManager = NULL;

HRESULT
APPLICATION_MANAGER::GetApplicationInfo(
    _In_ IHttpServer*          pServer,
    _In_ ASPNETCORE_CONFIG*    pConfig,
    _Out_ APPLICATION_INFO **  ppApplicationInfo
)
{
    HRESULT                hr = S_OK;
    APPLICATION_INFO      *pApplicationInfo = NULL;
    APPLICATION_INFO_KEY   key;
    BOOL                   fExclusiveLock = FALSE;
    BOOL                   fMixedHostingModelError = FALSE;
    BOOL                   fDuplicatedInProcessApp = FALSE;
    PCWSTR                 pszApplicationId = NULL;
    STACK_STRU ( strEventMsg, 256 );

    *ppApplicationInfo = NULL;

    DBG_ASSERT(pServer != NULL);
    DBG_ASSERT(pConfig != NULL);

    pszApplicationId = pConfig->QueryConfigPath()->QueryStr(); 

    hr = key.Initialize(pszApplicationId);
    if (FAILED(hr))
    {
        goto Finished;
    }

    AcquireSRWLockShared(&m_srwLock);
    if (m_fInShutdown)
    {
        ReleaseSRWLockShared(&m_srwLock);
        hr = HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS);
        goto Finished;
    }
    m_pApplicationInfoHash->FindKey(&key, ppApplicationInfo);
    ReleaseSRWLockShared(&m_srwLock);

    if (*ppApplicationInfo == NULL)
    {
        switch (pConfig->QueryHostingModel())
        {
        case HOSTING_IN_PROCESS:
            if (m_pApplicationInfoHash->Count() > 0)
            {
                // Only one inprocess app is allowed per IIS worker process
                fDuplicatedInProcessApp = TRUE;
                hr = HRESULT_FROM_WIN32(ERROR_APP_INIT_FAILURE);
                goto Finished;
            }
            break;

        case HOSTING_OUT_PROCESS:
            break;

        default:
            hr = E_UNEXPECTED;
            goto Finished;
        }
        pApplicationInfo = new APPLICATION_INFO(pServer);
        if (pApplicationInfo == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        AcquireSRWLockExclusive(&m_srwLock);
        fExclusiveLock = TRUE;
        if (m_fInShutdown)
        {
            // Already in shuting down. No need to create the application
            hr = HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS);
            goto Finished;
        }
        m_pApplicationInfoHash->FindKey(&key, ppApplicationInfo);

        if (*ppApplicationInfo != NULL)
        {
            // someone else created the application
            delete pApplicationInfo;
            pApplicationInfo = NULL;
            goto Finished;
        }

        // hosting model check. We do not allow mixed scenario for now
        // could be changed in the future
        if (m_hostingModel != HOSTING_UNKNOWN)
        {
            if (m_hostingModel != pConfig->QueryHostingModel())
            {
                // hosting model does not match, error out
                fMixedHostingModelError = TRUE;
                hr = HRESULT_FROM_WIN32(ERROR_APP_INIT_FAILURE);
                goto Finished;
            }
        }

        hr = pApplicationInfo->Initialize(pConfig, m_pFileWatcher);
        if (FAILED(hr))
        {
            goto Finished;
        }

        hr = m_pApplicationInfoHash->InsertRecord( pApplicationInfo );
        if (FAILED(hr))
        {
            goto Finished;
        }

        //
        // first application will decide which hosting model allowed by this process
        //
        if (m_hostingModel == HOSTING_UNKNOWN)
        {
            m_hostingModel = pConfig->QueryHostingModel();
        }

        *ppApplicationInfo = pApplicationInfo;
        ReleaseSRWLockExclusive(&m_srwLock);
        fExclusiveLock = FALSE;

        pApplicationInfo->StartMonitoringAppOffline();
        pApplicationInfo = NULL;
    }

Finished:

    if (fExclusiveLock)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
    }

    if (pApplicationInfo != NULL)
    {
        pApplicationInfo->DereferenceApplicationInfo();
        pApplicationInfo = NULL;
    }

    if (FAILED(hr))
    {
        if (fDuplicatedInProcessApp)
        {
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG,
                pszApplicationId)))
            {
                UTILITY::LogEvent(g_hEventLog,
                    EVENTLOG_ERROR_TYPE,
                    ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP,
                    strEventMsg.QueryStr());
            }
        }
        else if (fMixedHostingModelError)
        {
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG,
                pszApplicationId,
                pConfig->QueryHostingModel())))
            {
                UTILITY::LogEvent(g_hEventLog,
                    EVENTLOG_ERROR_TYPE,
                    ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR,
                    strEventMsg.QueryStr());
            }
        }
        else
        {
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
                pszApplicationId,
                hr)))
            {
                UTILITY::LogEvent(g_hEventLog,
                    EVENTLOG_ERROR_TYPE,
                    ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
                    strEventMsg.QueryStr());
            }
        }
    }

    return hr;
}

HRESULT
APPLICATION_MANAGER::RecycleApplication(
    _In_ LPCWSTR pszApplicationId
)
{
    HRESULT          hr = S_OK;
    APPLICATION_INFO_KEY  key;

    hr = key.Initialize(pszApplicationId);
    if (FAILED(hr))
    {
        goto Finished;
    }
    AcquireSRWLockExclusive(&m_srwLock);
    m_pApplicationInfoHash->DeleteKey(&key);

    if (m_pApplicationInfoHash->Count() == 0)
    {
        m_hostingModel = HOSTING_UNKNOWN;
    }

    if (g_fAspnetcoreRHLoadedError)
    {
        // We had assembly loading failure
        // this error blocked the start of all applications
        // Let's recycle the worker process if user redeployed any application
        g_pHttpServer->RecycleProcess(L"AspNetCore Recycle Process on Demand due to assembly loading failure");
    }

    ReleaseSRWLockExclusive(&m_srwLock);

Finished:

    return hr;
}

VOID
APPLICATION_MANAGER::ShutDown()
{
    m_fInShutdown = TRUE;
    if (m_pApplicationInfoHash != NULL)
    {
        AcquireSRWLockExclusive(&m_srwLock);

        // clean up the hash table so that the application will be informed on shutdown
        m_pApplicationInfoHash->Clear();

        ReleaseSRWLockExclusive(&m_srwLock);
    }

    // stop filewatcher monitoring thread
    if (m_pFileWatcher != NULL)
    {
        delete  m_pFileWatcher;
        m_pFileWatcher = NULL;
    }
}
