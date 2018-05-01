#pragma once

class OUT_OF_PROCESS_APPLICATION : public APPLICATION
{

public:
    OUT_OF_PROCESS_APPLICATION(ASPNETCORE_CONFIG  *pConfig);

    __override
    ~OUT_OF_PROCESS_APPLICATION() override;

    HRESULT
    Initialize();

    HRESULT
    GetProcess(
        _Out_   SERVER_PROCESS       **ppServerProcess
    );

    __override
    VOID
    ShutDown()
    override;

    __override
    VOID
    Recycle()
    override;

    __override
    HRESULT
    CreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _In_  HTTP_MODULE_ID     *pModuleId,
        _Out_ IREQUEST_HANDLER   **pRequestHandler)
    override;

    ASPNETCORE_CONFIG*
    QueryConfig()
    const;

private:

    PROCESS_MANAGER * m_pProcessManager;
    SRWLOCK           rwlock;

    ASPNETCORE_CONFIG*              m_pConfig;
};
