// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

extern IHttpServer *    g_pHttpServer;
class FORWARDING_HANDLER;

class WEBSOCKET_HANDLER
{
public:
    WEBSOCKET_HANDLER();

    static
    HRESULT
    StaticInitialize(
        BOOL fEnableReferenceTraceLogging
        );

    static
    VOID
    StaticTerminate(
        VOID
        );

    VOID
    Terminate(
        VOID
        );

    VOID
    TerminateRequest(
        VOID
        )
    {
        Cleanup(ServerStateUnavailable);
    }

    HRESULT
    ProcessRequest(
        FORWARDING_HANDLER *pHandler,
        IHttpContext * pHttpContext,
        HINTERNET      hRequest,
        BOOL*          pfHandleCreated
        );

    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        VOID
        );

    HRESULT
    OnWinHttpSendComplete(
        WINHTTP_WEB_SOCKET_STATUS * pCompletionStatus
    );

    HRESULT
    OnWinHttpShutdownComplete(
        VOID
    );

    HRESULT
    OnWinHttpReceiveComplete(
        WINHTTP_WEB_SOCKET_STATUS * pCompletionStatus
    );

    HRESULT
    OnWinHttpIoError(
        WINHTTP_WEB_SOCKET_ASYNC_RESULT *pCompletionStatus
    );


private:
    enum CleanupReason
    {
        CleanupReasonUnknown = 0,
        IdleTimeout = 1,
        ConnectFailed = 2,
        ClientDisconnect = 3,
        ServerDisconnect = 4,
        ServerStateUnavailable = 5
    };

    virtual
    ~WEBSOCKET_HANDLER()
    {
    }

    WEBSOCKET_HANDLER(const WEBSOCKET_HANDLER &);
    void operator=(const WEBSOCKET_HANDLER &);

    VOID
    InsertRequest(
        VOID
        );

    VOID
    RemoveRequest(
        VOID
        );

    static
    VOID
    WINAPI
    OnReadIoCompletion(
        HRESULT     hrError,
        VOID *      pvCompletionContext,
        DWORD       cbIO,
        BOOL        fUTF8Encoded,
        BOOL        fFinalFragment,
        BOOL        fClose
        );

    static
    VOID
    WINAPI
    OnWriteIoCompletion(
        HRESULT     hrError,
        VOID *      pvCompletionContext,
        DWORD       cbIO,
        BOOL        fUTF8Encoded,
        BOOL        fFinalFragment,
        BOOL        fClose
    );

    VOID
    Cleanup(
        CleanupReason  reason
        );

    HRESULT
    DoIisWebSocketReceive(
        VOID
    );

    HRESULT
    DoWinHttpWebSocketReceive(
        VOID
    );

    HRESULT
    DoIisWebSocketSend(
        DWORD cbData,
        WINHTTP_WEB_SOCKET_BUFFER_TYPE  eBufferType
    );

    HRESULT
    DoWinHttpWebSocketSend(
        DWORD cbData,
        WINHTTP_WEB_SOCKET_BUFFER_TYPE  eBufferType
    );

    HRESULT
    OnIisSendComplete(
        HRESULT     hrError,
        DWORD       cbIO
    );

    HRESULT
    OnIisReceiveComplete(
        HRESULT     hrError,
        DWORD       cbIO,
        BOOL        fUTF8Encoded,
        BOOL        fFinalFragment,
        BOOL        fClose
    );

    VOID
    IncrementOutstandingIo(
        VOID
    );

    VOID
    DecrementOutstandingIo(
        VOID
    );

    VOID
    IndicateCompletionToIIS(
        VOID
    );

private:
    static const
    DWORD               RECEIVE_BUFFER_SIZE = 4*1024;

    LIST_ENTRY          _listEntry;

    IHttpContext3 *     _pHttpContext;

    IWebSocketContext * _pWebSocketContext;

    FORWARDING_HANDLER *_pHandler;

    HINTERNET           _hWebSocketRequest;

    BYTE                _WinHttpReceiveBuffer[RECEIVE_BUFFER_SIZE];

    BYTE                _IisReceiveBuffer[RECEIVE_BUFFER_SIZE];

    CRITICAL_SECTION    _RequestLock;

    LONG                _dwOutstandingIo;

    volatile
    BOOL                _fCleanupInProgress;

    volatile
    BOOL                _fIndicateCompletionToIis;

    volatile
    BOOL                _fHandleClosed;

    volatile
    BOOL                _fReceivedCloseMsg;

    static
    LIST_ENTRY          sm_RequestsListHead;

    static
    SRWLOCK             sm_RequestsListLock;

    static
    TRACE_LOG *         sm_pTraceLog;
};
