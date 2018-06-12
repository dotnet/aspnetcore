#include "..\precomp.hxx"

OUT_OF_PROCESS_APPLICATION::OUT_OF_PROCESS_APPLICATION(
    REQUESTHANDLER_CONFIG  *pConfig) :
    m_fWebSocketSupported(WEBSOCKET_STATUS::WEBSOCKET_UNKNOWN),
    m_pConfig(pConfig)
{
    m_status = APPLICATION_STATUS::RUNNING;
    m_pProcessManager = NULL;
    InitializeSRWLock(&m_srwLock);
}

OUT_OF_PROCESS_APPLICATION::~OUT_OF_PROCESS_APPLICATION()
{
    if (m_pProcessManager != NULL)
    {
        m_pProcessManager->ShutdownAllProcesses();
        m_pProcessManager->DereferenceProcessManager();
        m_pProcessManager = NULL;
    }

    if (m_pConfig != NULL)
    {
        delete m_pConfig;
        m_pConfig = NULL;
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
    return m_pProcessManager->GetProcess(m_pConfig, QueryWebsocketStatus(), ppServerProcess);
}

REQUESTHANDLER_CONFIG*
OUT_OF_PROCESS_APPLICATION::QueryConfig() const
{
    return m_pConfig;
}

__override
VOID
OUT_OF_PROCESS_APPLICATION::ShutDown()
{
    AcquireSRWLockExclusive(&m_srwLock);
    {
        if (m_pProcessManager != NULL)
        {
            m_pProcessManager->ShutdownAllProcesses();
            m_pProcessManager->DereferenceProcessManager();
            m_pProcessManager = NULL;
        }
    }
    ReleaseSRWLockExclusive(&m_srwLock);
}

__override
VOID
OUT_OF_PROCESS_APPLICATION::Recycle()
{
    ShutDown();
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
