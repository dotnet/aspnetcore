// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

class OUT_OF_PROCESS_APPLICATION : public APPLICATION
{
    enum WEBSOCKET_STATUS
    {
        WEBSOCKET_UNKNOWN = 0,
        WEBSOCKET_NOT_SUPPORTED,
        WEBSOCKET_SUPPORTED,
    };

public:
    OUT_OF_PROCESS_APPLICATION(
        REQUESTHANDLER_CONFIG  *pConfig);

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
        _Out_ IREQUEST_HANDLER   **pRequestHandler)
    override;

    REQUESTHANDLER_CONFIG*
    QueryConfig()
    const;

    BOOL
    QueryWebsocketStatus()
    const;

private:

    VOID SetWebsocketStatus(IHttpContext *pHttpContext);

    PROCESS_MANAGER * m_pProcessManager;
    SRWLOCK           m_srwLock;
    IHttpServer      *m_pHttpServer;

    REQUESTHANDLER_CONFIG*        m_pConfig;
    WEBSOCKET_STATUS              m_fWebSocketSupported;
};
