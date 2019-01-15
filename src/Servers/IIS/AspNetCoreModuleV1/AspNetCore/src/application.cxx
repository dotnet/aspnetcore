// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

APPLICATION::~APPLICATION()
{
    if (m_pAppOfflineHtm != NULL)
    {
        m_pAppOfflineHtm->DereferenceAppOfflineHtm();
        m_pAppOfflineHtm = NULL;
    }

    if (m_pFileWatcherEntry != NULL)
    {
        // Mark the entry as invalid,
        // StopMonitor will close the file handle and trigger a FCN
        // the entry will delete itself when processing this FCN 
        m_pFileWatcherEntry->MarkEntryInValid();
        m_pFileWatcherEntry->StopMonitor();
        m_pFileWatcherEntry = NULL;
    }

    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->ShutdownAllProcesses();
        m_pProcessManager->DereferenceProcessManager();
        m_pProcessManager = NULL;
    }
}

HRESULT
APPLICATION::Initialize(
    _In_ APPLICATION_MANAGER* pApplicationManager,
    _In_ LPCWSTR  pszApplication,
    _In_ LPCWSTR  pszPhysicalPath
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(pszPhysicalPath != NULL);
    DBG_ASSERT(pApplicationManager != NULL);
    DBG_ASSERT(pszPhysicalPath != NULL);
    m_strAppPhysicalPath.Copy(pszPhysicalPath);

    m_pApplicationManager = pApplicationManager;

    hr = m_applicationKey.Initialize(pszApplication);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (m_pProcessManager == NULL)
    {
        m_pProcessManager = new PROCESS_MANAGER;
        if (m_pProcessManager == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        hr = m_pProcessManager->Initialize();
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    if (m_pFileWatcherEntry == NULL)
    {
        m_pFileWatcherEntry = new FILE_WATCHER_ENTRY(pApplicationManager->GetFileWatcher());
        if (m_pFileWatcherEntry == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }
    }

    UpdateAppOfflineFileHandle();

Finished:

    if (FAILED(hr))
    {
        if (m_pFileWatcherEntry != NULL)
        {
            m_pFileWatcherEntry->DereferenceFileWatcherEntry();
            m_pFileWatcherEntry = NULL;
        }

        if (m_pProcessManager != NULL)
        {
            m_pProcessManager->DereferenceProcessManager();
            m_pProcessManager = NULL;
        }
    }

    return hr;
}

HRESULT
APPLICATION::StartMonitoringAppOffline()
{
    HRESULT hr = S_OK;

    hr = m_pFileWatcherEntry->Create(m_strAppPhysicalPath.QueryStr(), L"app_offline.htm", this, NULL);

    return hr;
}

VOID
APPLICATION::UpdateAppOfflineFileHandle()
{
    STRU strFilePath;
    PATH::ConvertPathToFullPath(L".\\app_offline.htm", m_strAppPhysicalPath.QueryStr(), &strFilePath);
    APP_OFFLINE_HTM *pOldAppOfflineHtm = NULL;
    APP_OFFLINE_HTM *pNewAppOfflineHtm = NULL;

    if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(strFilePath.QueryStr()) && GetLastError() == ERROR_FILE_NOT_FOUND)
    {
        m_fAppOfflineFound = FALSE;
    }
    else
    {
        m_fAppOfflineFound = TRUE;
        
        //
        // send shutdown signal
        //

        // The reason why we send the shutdown signal before loading the new app_offline file is because we want to make some delay 
        // before reading the appoffline.htm so that the file change can be done on time.
        if (m_pProcessManager != NULL)
        {
            m_pProcessManager->SendShutdownSignal();
        }

        pNewAppOfflineHtm = new APP_OFFLINE_HTM(strFilePath.QueryStr());

        if ( pNewAppOfflineHtm != NULL )
        {
            if (pNewAppOfflineHtm->Load())
            {
                //
                // loaded the new app_offline.htm
                //
                pOldAppOfflineHtm = (APP_OFFLINE_HTM *)InterlockedExchangePointer((VOID**)&m_pAppOfflineHtm, pNewAppOfflineHtm);

                if (pOldAppOfflineHtm != NULL)
                {
                    pOldAppOfflineHtm->DereferenceAppOfflineHtm();
                    pOldAppOfflineHtm = NULL;
                }
            }
            else
            {
                // ignored the new app_offline file because the file does not exist.
                pNewAppOfflineHtm->DereferenceAppOfflineHtm(); 
                pNewAppOfflineHtm = NULL;
            }
        }
    }
}