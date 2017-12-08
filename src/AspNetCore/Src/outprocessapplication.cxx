// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "precomp.hxx"

OUT_OF_PROCESS_APPLICATION::OUT_OF_PROCESS_APPLICATION()
    : m_pProcessManager(NULL)
{
}

OUT_OF_PROCESS_APPLICATION::~OUT_OF_PROCESS_APPLICATION()
{
    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->ShutdownAllProcesses();
        m_pProcessManager->DereferenceProcessManager();
        m_pProcessManager = NULL;
    }
}


//
// Initialize is guarded by a lock inside APPLICATION_MANAGER::GetApplication
// It ensures only one application will be initialized and singleton
// Error will happen if you call Initialized outside APPLICATION_MANAGER::GetApplication
//
__override
HRESULT
OUT_OF_PROCESS_APPLICATION::Initialize(
    _In_ APPLICATION_MANAGER* pApplicationManager,
    _In_ ASPNETCORE_CONFIG*   pConfiguration
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(pApplicationManager != NULL);
    DBG_ASSERT(pConfiguration != NULL);

    m_pApplicationManager = pApplicationManager;
    m_pConfiguration = pConfiguration;

    hr = m_applicationKey.Initialize(pConfiguration->QueryApplicationPath()->QueryStr());
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

__override
VOID 
OUT_OF_PROCESS_APPLICATION::OnAppOfflineHandleChange()
{
    //
    // Sending signal to backend process for shutdown
    //
    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->SendShutdownSignal();
    }
}

__override
REQUEST_NOTIFICATION_STATUS
OUT_OF_PROCESS_APPLICATION::ExecuteRequest(
    _In_ IHttpContext* pHttpContext
)
{
    //
    // TODO:
    // Ideally we should wrap the fowaring logic inside FORWARDING_HANDLER inside this function
    // To achieve better abstraction. It is too risky to do it now
    //
    return RQ_NOTIFICATION_FINISH_REQUEST;
}