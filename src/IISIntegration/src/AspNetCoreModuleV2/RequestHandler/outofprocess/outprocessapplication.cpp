#include "..\precomp.hxx"

OUT_OF_PROCESS_APPLICATION::OUT_OF_PROCESS_APPLICATION(
    IHttpServer*        pHttpServer,
    ASPNETCORE_CONFIG*  pConfig) :
    APPLICATION(pHttpServer, pConfig)
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

