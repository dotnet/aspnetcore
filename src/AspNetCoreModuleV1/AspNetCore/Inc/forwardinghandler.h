// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "forwarderconnection.h"
#include "protocolconfig.h"
#include "serverprocess.h"
#include "application.h"
#include "tracelog.h"
#include "websockethandler.h"

#define ASPNETCORE_DEBUG_STRU_BUFFER_SIZE    100
#define ASPNETCORE_DEBUG_STRU_ARRAY_SIZE     100

enum FORWARDING_REQUEST_STATUS
{
    FORWARDER_START,
    FORWARDER_SENDING_REQUEST,
    FORWARDER_RECEIVING_RESPONSE,
    FORWARDER_RECEIVED_WEBSOCKET_RESPONSE,
    FORWARDER_RESET_CONNECTION,
    FORWARDER_DONE
};

extern HTTP_MODULE_ID   g_pModuleId;
extern IHttpServer *    g_pHttpServer;
extern BOOL             g_fAsyncDisconnectAvailable;
extern PCWSTR           g_pszModuleName;
extern HMODULE          g_hModule;
extern HMODULE          g_hWinHttpModule;
extern DWORD            g_dwTlsIndex;
extern DWORD            g_OptionalWinHttpFlags;

#ifdef DEBUG
extern STRA             g_strLogs[ASPNETCORE_DEBUG_STRU_ARRAY_SIZE];
extern DWORD            g_dwLogCounter;
#endif // DEBUG

enum MULTI_PART_POSITION
{
    MULTI_PART_IN_BOUNDARY,
    MULTI_PART_IN_HEADER,
    MULTI_PART_IN_CHUNK,
    MULTI_PART_IN_CHUNK_END
};

class ASYNC_DISCONNECT_CONTEXT;

#define FORWARDING_HANDLER_SIGNATURE        ((DWORD)'FHLR')
#define FORWARDING_HANDLER_SIGNATURE_FREE   ((DWORD)'fhlr')

class FORWARDING_HANDLER
{
public:
    
    FORWARDING_HANDLER(
        __in IHttpContext * pW3Context
    );

    static void * operator new(size_t size);

    static void operator delete(void * pMemory);

    VOID
    ReferenceForwardingHandler(
        VOID
    ) const;

    VOID
    DereferenceForwardingHandler(
        VOID
    ) const;

    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler();

    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        DWORD                   cbCompletion,
        HRESULT                 hrCompletionStatus
    );

    IHttpTraceContext *
    QueryTraceContext()
    {
        return m_pW3Context->GetTraceContext();
    }
    
    IHttpContext *
    QueryHttpContext(
        VOID
        )
    {
        return m_pW3Context;
    }

    static
    VOID
    CALLBACK
    OnWinHttpCompletion(
        HINTERNET   hRequest,
        DWORD_PTR   dwContext,
        DWORD       dwInternetStatus,
        LPVOID      lpvStatusInformation,
        DWORD       dwStatusInformationLength
    )
    {

        FORWARDING_HANDLER * pThis = static_cast<FORWARDING_HANDLER *>(reinterpret_cast<PVOID>(dwContext));
        if (pThis == NULL)
        {
            //error happened, nothing can be done here
            return;
        }
        DBG_ASSERT(pThis->m_Signature == FORWARDING_HANDLER_SIGNATURE);
        pThis->OnWinHttpCompletionInternal(hRequest,
                                           dwInternetStatus,
                                           lpvStatusInformation,
                                           dwStatusInformationLength);
    }

    static
    HRESULT
    StaticInitialize(
        BOOL fEnableReferenceCountTracing 
    );

    static
    VOID
    StaticTerminate();

    static
    PCWSTR
    QueryErrorFormat()
    {
        return sm_strErrorFormat.QueryStr();
    }

    static
    HANDLE
    QueryEventLog()
    {
        return sm_hEventLog;
    }

    VOID
    TerminateRequest(
        BOOL    fClientInitiated
    );

    static HINTERNET                    sm_hSession;

    HRESULT
    SetStatusAndHeaders(
        PCSTR               pszHeaders,
        DWORD               cchHeaders
    );

    HRESULT
    OnSharedRequestEntity(
        ULONGLONG   ulOffset,
        LPCBYTE     pvBuffer,
        DWORD       cbBuffer
    );

    VOID
    SetStatus(
        FORWARDING_REQUEST_STATUS status
        )
    {
        m_RequestStatus = status;
    }

    virtual
    ~FORWARDING_HANDLER(
        VOID
    );

private:

    //
    // Begin OnMapRequestHandler phases.
    //

    HRESULT
    CreateWinHttpRequest(
        __in const IHttpRequest *       pRequest,
        __in const PROTOCOL_CONFIG *    pProtocol,
        __in HINTERNET                  hConnect,
        __inout STRU *                  pstrUrl,
        ASPNETCORE_CONFIG*              pAspNetCoreConfig,
        SERVER_PROCESS*                 pServerProcess
    );

    //
    // End OnMapRequestHandler phases.
    //

    VOID
    RemoveRequest();

    HRESULT
    GetHeaders(
        const PROTOCOL_CONFIG * pProtocol,
        PCWSTR *                ppszHeaders,
        DWORD *                 pcchHeaders,
        ASPNETCORE_CONFIG*      pAspNetCoreConfig,
        SERVER_PROCESS*         pServerProcess
    );

    HRESULT
    DoReverseRewrite(
        __in IHttpResponse *pResponse
    );

    BYTE *
    GetNewResponseBuffer(
        DWORD   dwBufferSize
    );

    VOID
    FreeResponseBuffers();

    VOID
    OnWinHttpCompletionInternal(
        HINTERNET   hRequest,
        DWORD       dwInternetStatus,
        LPVOID      lpvStatusInformation,
        DWORD       dwStatusInformationLength
    );

    HRESULT
    OnWinHttpCompletionSendRequestOrWriteComplete(
        HINTERNET                   hRequest,
        DWORD                       dwInternetStatus,
        __out BOOL *                pfClientError,
        __out BOOL *                pfAnotherCompletionExpected
    );

    HRESULT
    OnWinHttpCompletionStatusHeadersAvailable(
        HINTERNET                   hRequest,
        __out BOOL *                pfAnotherCompletionExpected
    );

    HRESULT
    OnWinHttpCompletionStatusDataAvailable(
        HINTERNET                   hRequest,
        DWORD                       dwBytes,
        __out BOOL *                pfAnotherCompletionExpected
    );

    HRESULT
    OnWinHttpCompletionStatusReadComplete(
        __in IHttpResponse *        pResponse,
        DWORD                       dwStatusInformationLength,
        __out BOOL *                pfAnotherCompletionExpected
    );

    HRESULT
    OnSendingRequest(
        DWORD                       cbCompletion,
        HRESULT                     hrCompletionStatus,
        __out BOOL *                pfClientError
    );

    HRESULT
    OnReceivingResponse();

    HRESULT
    OnWebSocketWinHttpSendComplete(
        HINTERNET   hRequest,
        LPVOID      pvStatus,
        DWORD       hrCompletion,
        DWORD       cbCompletion,
        BOOL *      pfAnotherCompletionExpected
    );

    HRESULT
    OnWebSocketWinHttpReceiveComplete(
        HINTERNET   hRequest,
        LPVOID      pvStatus,
        DWORD       hrCompletion,
        DWORD       cbCompletion,
        BOOL *      pfAnotherCompletionExpected
    );

    HRESULT
    OnWebSocketIisSendComplete(
        DWORD hrCompletion,
        DWORD cbCompletion
    );

    HRESULT
    OnWebSocketIisReceiveComplete(
        DWORD hrCompletion,
        DWORD cbCompletion
    );

    HRESULT
    DoIisWebSocketReceive(
        VOID
    );

    VOID
    TerminateWebsocket(
        VOID
    );

    DWORD                               m_Signature;
    mutable LONG                        m_cRefs;

    IHttpContext *                      m_pW3Context;

    //
    // WinHTTP request handle is protected using a read-write lock.
    //
    SRWLOCK                             m_RequestLock;
    HINTERNET                           m_hRequest;

    APP_OFFLINE_HTM                    *m_pAppOfflineHtm;
    APPLICATION                        *m_pApplication;

    BOOL                                m_fResponseHeadersReceivedAndSet;
    volatile  BOOL                      m_fClientDisconnected;
    //
    // A safety guard flag indicating no more IIS PostCompletion is allowed
    //
    volatile  BOOL                      m_fFinishRequest;
    //
    // A safety guard flag to prevent from unexpect callback which may signal IIS pipeline
    // more than once with non-pending status
    //
    volatile  BOOL                      m_fDoneAsyncCompletion;
    volatile  BOOL                      m_fHasError;
    //
    // WinHttp may hit AV under race if handle got closed more than once simultaneously 
    // Use two bool variables to guard
    //
    volatile  BOOL                      m_fHttpHandleInClose;
    volatile  BOOL                      m_fWebSocketHandleInClose;
    //
    // Record the number of winhttp handles in use
    // release IIS pipeline only after all handles got closed
    //
    volatile  LONG                      m_dwHandlers;

    BOOL                                m_fDoReverseRewriteHeaders;
    BOOL                                m_fServerResetConn;
    DWORD                               m_msStartTime;
    DWORD                               m_BytesToReceive;
    DWORD                               m_BytesToSend;

    BYTE *                              m_pEntityBuffer;
    DWORD                               m_cchLastSend;

    static const SIZE_T                 INLINE_ENTITY_BUFFERS = 8;
    DWORD                               m_cEntityBuffers;
    BUFFER_T<BYTE*,INLINE_ENTITY_BUFFERS> m_buffEntityBuffers;

    DWORD                               m_cBytesBuffered;
    DWORD                               m_cMinBufferLimit;

    PCSTR                               m_pszOriginalHostHeader;

    volatile FORWARDING_REQUEST_STATUS  m_RequestStatus;

    ASYNC_DISCONNECT_CONTEXT *          m_pDisconnect;

    PCWSTR                              m_pszHeaders;
    DWORD                               m_cchHeaders;

    BOOL                                m_fWebSocketEnabled;

    STRU                                m_strFullUri;

    ULONGLONG                           m_cContentLength;

    WEBSOCKET_HANDLER *                 m_pWebSocket;

    static PROTOCOL_CONFIG              sm_ProtocolConfig;

    static STRU                         sm_strErrorFormat;

    static HANDLE                       sm_hEventLog;

    static ALLOC_CACHE_HANDLER *        sm_pAlloc;

    //
    // Reference cout tracing for debugging purposes.
    //
    static TRACE_LOG *                  sm_pTraceLog;
};

class ASYNC_DISCONNECT_CONTEXT : public IHttpConnectionStoredContext
{
 public:
    ASYNC_DISCONNECT_CONTEXT()
    {
        m_pHandler = NULL;
    }

    VOID
    CleanupStoredContext()
    {
        DBG_ASSERT(m_pHandler == NULL);
        delete this;
    }

    VOID
    NotifyDisconnect()
    {
        FORWARDING_HANDLER *pInitialValue = (FORWARDING_HANDLER*)
            InterlockedExchangePointer((PVOID*) &m_pHandler, NULL);

        if (pInitialValue != NULL)
        {
            pInitialValue->TerminateRequest(TRUE);
            pInitialValue->DereferenceForwardingHandler();
        }
    }

    VOID
    SetHandler(
        FORWARDING_HANDLER *pHandler
    )
    {
        //
        // Take a reference on the forwarding handler.
        // This reference will be released on either of two conditions:
        //
        // 1. When the request processing ends, in which case a ResetHandler()
        // is called.
        // 
        // 2. When a disconnect notification arrives.
        //
        // We need to make sure that only one of them ends up dereferencing
        // the object.
        //

        DBG_ASSERT (pHandler != NULL);
        DBG_ASSERT (m_pHandler == NULL);

        pHandler->ReferenceForwardingHandler();
        InterlockedExchangePointer((PVOID*)&m_pHandler, pHandler);
    }

    VOID
    ResetHandler(
        VOID
    )
    {
        FORWARDING_HANDLER *pInitialValue = (FORWARDING_HANDLER*) 
            InterlockedExchangePointer( (PVOID*)&m_pHandler, NULL);

        if (pInitialValue != NULL)
        {
            pInitialValue->DereferenceForwardingHandler();
        }
    }

 private:
    ~ASYNC_DISCONNECT_CONTEXT()
    {}

    FORWARDING_HANDLER *     m_pHandler;
};