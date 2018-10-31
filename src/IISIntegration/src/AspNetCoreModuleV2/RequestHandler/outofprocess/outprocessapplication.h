#pragma once

class OUT_OF_PROCESS_APPLICATION : public APPLICATION
{

public:
    OUT_OF_PROCESS_APPLICATION(IHttpServer* pHttpServer, ASPNETCORE_CONFIG  *pConfig);

    ~OUT_OF_PROCESS_APPLICATION();

    HRESULT
    Initialize();

    HRESULT
    GetProcess(
        _Out_   SERVER_PROCESS       **ppServerProcess
    );

    __override
    VOID
    ShutDown();

    __override
    VOID
    Recycle();

private:
    PROCESS_MANAGER * m_pProcessManager;
    SRWLOCK           rwlock;
};
