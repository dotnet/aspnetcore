#include "..\precomp.hxx"

OUT_OF_PROCESS_APPLICATION::OUT_OF_PROCESS_APPLICATION(
    ASPNETCORE_CONFIG*  pConfig) :
    m_pConfig(pConfig)
{
    m_status = APPLICATION_STATUS::RUNNING;
    m_pProcessManager = NULL;
    InitializeSRWLock(&rwlock);
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
    return m_pProcessManager->GetProcess(m_pConfig, ppServerProcess);
}

ASPNETCORE_CONFIG*
OUT_OF_PROCESS_APPLICATION::QueryConfig() const
{
    return m_pConfig;
}

__override
VOID
OUT_OF_PROCESS_APPLICATION::ShutDown()
{
    AcquireSRWLockExclusive(&rwlock);
    {
        if (m_pProcessManager != NULL)
        {
            m_pProcessManager->ShutdownAllProcesses();
            m_pProcessManager->DereferenceProcessManager();
            m_pProcessManager = NULL;
        }
    }
    ReleaseSRWLockExclusive(&rwlock);
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
    _In_  HTTP_MODULE_ID     *pModuleId,
    _Out_ IREQUEST_HANDLER   **pRequestHandler)
{
    HRESULT hr = S_OK;
    IREQUEST_HANDLER* pHandler = NULL;
    pHandler = new FORWARDING_HANDLER(pHttpContext, pModuleId, this);

    if (pHandler == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
    }

    *pRequestHandler = pHandler;
    return hr;
}
