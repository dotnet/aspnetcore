// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "outprocessapplication.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"

OUT_OF_PROCESS_APPLICATION::OUT_OF_PROCESS_APPLICATION(
    IHttpApplication& pApplication,
    std::unique_ptr<REQUESTHANDLER_CONFIG> pConfig) :
    AppOfflineTrackingApplication(pApplication),
    m_fWebSocketSupported(WEBSOCKET_STATUS::WEBSOCKET_UNKNOWN),
    m_pConfig(std::move(pConfig))
{
    m_pProcessManager = NULL;
}

OUT_OF_PROCESS_APPLICATION::~OUT_OF_PROCESS_APPLICATION()
{
    SRWExclusiveLock lock(m_stopLock);
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
    if (m_pProcessManager == NULL)
    {
        m_pProcessManager = new PROCESS_MANAGER();
        RETURN_IF_FAILED(m_pProcessManager->Initialize());
    }
    return S_OK;
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
OUT_OF_PROCESS_APPLICATION::StopInternal(bool fServerInitiated)
{
    AppOfflineTrackingApplication::StopInternal(fServerInitiated);

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
    IREQUEST_HANDLER* pHandler = NULL;

    //add websocket check here
    if (m_fWebSocketSupported == WEBSOCKET_STATUS::WEBSOCKET_UNKNOWN)
    {
        SetWebsocketStatus(pHttpContext);
    }

    pHandler = new FORWARDING_HANDLER(pHttpContext, ::ReferenceApplication(this));
    *pRequestHandler = pHandler;
    return S_OK;
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
    HRESULT hr = pHttpContext->GetServerVariable("WEBSOCKET_VERSION", &pszTempWebsocketValue, &cbLength);
    if (SUCCEEDED(hr))
    {
        m_fWebSocketSupported = WEBSOCKET_STATUS::WEBSOCKET_SUPPORTED;
    }
    else
    {
        m_fWebSocketSupported = WEBSOCKET_STATUS::WEBSOCKET_NOT_SUPPORTED;
        if (hr != HRESULT_FROM_WIN32(ERROR_INVALID_INDEX))
        {
            LOG_IF_FAILED(hr);
        }
    }
}

BOOL
OUT_OF_PROCESS_APPLICATION::QueryWebsocketStatus() const
{
    return m_fWebSocketSupported == WEBSOCKET_STATUS::WEBSOCKET_SUPPORTED;
}
