// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "outprocessapplication.h"

#include "SRWExclusiveLock.h"

OUT_OF_PROCESS_APPLICATION::OUT_OF_PROCESS_APPLICATION(
    IHttpApplication& pApplication,
    std::unique_ptr<REQUESTHANDLER_CONFIG> pConfig) :
    AppOfflineTrackingApplication(pApplication),
    m_fWebSocketSupported(WEBSOCKET_STATUS::WEBSOCKET_UNKNOWN),
    m_pConfig(std::move(pConfig))
{
    m_status = APPLICATION_STATUS::RUNNING;
    m_pProcessManager = NULL;
}

OUT_OF_PROCESS_APPLICATION::~OUT_OF_PROCESS_APPLICATION()
{
    SRWExclusiveLock lock(m_stateLock);
    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->Shutdown();
        m_pProcessManager->DereferenceProcessManager();
        m_pProcessManager = NULL;
    }
}

HRESULT
OUT_OF_PROCESS_APPLICATION::Initialize(
)
{
    HRESULT hr = S_OK;
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

Finished:
    return hr;
}

HRESULT
OUT_OF_PROCESS_APPLICATION::GetProcess(
    _Out_   SERVER_PROCESS       **ppServerProcess
)
{
    return m_pProcessManager->GetProcess(m_pConfig.get(), QueryWebsocketStatus(), ppServerProcess);
}

__override
VOID
OUT_OF_PROCESS_APPLICATION::Stop(bool fServerInitiated)
{   
    SRWExclusiveLock lock(m_stateLock);

    if (m_fStopCalled)
    {
        return;
    }

    AppOfflineTrackingApplication::Stop(fServerInitiated);

    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->Shutdown();
    }
}

HRESULT
OUT_OF_PROCESS_APPLICATION::CreateHandler(
    _In_  IHttpContext       *pHttpContext,
    _Out_ IREQUEST_HANDLER  **pRequestHandler)
{
    HRESULT hr = S_OK;
    IREQUEST_HANDLER* pHandler = NULL;

    //add websocket check here
    if (m_fWebSocketSupported == WEBSOCKET_STATUS::WEBSOCKET_UNKNOWN)
    {
        SetWebsocketStatus(pHttpContext);
    }

    pHandler = new FORWARDING_HANDLER(pHttpContext, this);

    if (pHandler == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }

    *pRequestHandler = pHandler;
    return hr;
}

VOID
OUT_OF_PROCESS_APPLICATION::SetWebsocketStatus(
    IHttpContext* pHttpContext
)
{
    // Even though the applicationhost.config file contains the websocket element,
    // the websocket module may still not be enabled.
    PCWSTR pszTempWebsocketValue;
    DWORD cbLength;
    HRESULT hr;

    hr = pHttpContext->GetServerVariable("WEBSOCKET_VERSION", &pszTempWebsocketValue, &cbLength);
    if (FAILED(hr))
    {
        m_fWebSocketSupported = WEBSOCKET_STATUS::WEBSOCKET_NOT_SUPPORTED;
    }
    else
    {
        m_fWebSocketSupported = WEBSOCKET_STATUS::WEBSOCKET_SUPPORTED;
    }
}

BOOL
OUT_OF_PROCESS_APPLICATION::QueryWebsocketStatus() const
{
    return m_fWebSocketSupported == WEBSOCKET_STATUS::WEBSOCKET_SUPPORTED;
}
