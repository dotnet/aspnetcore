// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

/*++

Abstract:

    Main Handler for websocket requests.

    Initiates websocket connection to backend.
    Uses WinHttp API's for backend connections,
    and IIS Websocket API's for sending/receiving
    websocket traffic.

    Transfers data between the two IO endpoints.

-----------------
Read Loop Design
-----------------
When a read IO completes successfully on any endpoints, Asp.Net Core Module doesn't
immediately issue the next read. The next read is initiated only after
the read data is sent to the other endpoint. As soon as this send completes,
we initiate the next IO. It should be noted that the send complete merely
indicates the API completion from HTTP, and not necessarily over the network.

This prevents the need for data buffering at the Asp.Net Core Module level.

--*/

#include "websockethandler.h"
#include "exceptions.h"

SRWLOCK WEBSOCKET_HANDLER::sm_RequestsListLock;

LIST_ENTRY WEBSOCKET_HANDLER::sm_RequestsListHead;

TRACE_LOG * WEBSOCKET_HANDLER::sm_pTraceLog;

WEBSOCKET_HANDLER::WEBSOCKET_HANDLER() :
    _pHttpContext(nullptr),
    _pWebSocketContext(nullptr),
    _hWebSocketRequest(nullptr),
    _pHandler(nullptr),
    _dwOutstandingIo(0),
    _fCleanupInProgress(FALSE),
    _fIndicateCompletionToIis(FALSE),
    _fHandleClosed(FALSE),
    _fReceivedCloseMsg(FALSE)
{
    LOG_TRACE(L"WEBSOCKET_HANDLER::WEBSOCKET_HANDLER");

    InitializeCriticalSectionAndSpinCount(&_RequestLock, 1000);
    InsertRequest();
}

VOID
WEBSOCKET_HANDLER::Terminate(
    VOID
    )
{
    LOG_TRACE(L"WEBSOCKET_HANDLER::Terminate");
    if (!_fHandleClosed)
    {
    RemoveRequest();
    _fCleanupInProgress = TRUE;

    if (_pHttpContext != nullptr)
    {
        _pHttpContext->CancelIo();
        _pHttpContext = nullptr;
    }
    if (_hWebSocketRequest)
    {
        WinHttpCloseHandle(_hWebSocketRequest);
        _hWebSocketRequest = nullptr;
    }

    _pWebSocketContext = nullptr;
    DeleteCriticalSection(&_RequestLock);

    delete this;
    }
}

//static
HRESULT
WEBSOCKET_HANDLER::StaticInitialize(
    BOOL   fEnableReferenceCountTracing
    )
/*++

    Routine Description:

    Initialize structures required for idle connection cleanup.

--*/
{
    if (!g_fWebSocketStaticInitialize)
    {
        return S_OK;
    }

    if (fEnableReferenceCountTracing)
    {
        //
        // If tracing is enabled, keep track of all websocket requests
        // for debugging purposes.
        //
        InitializeListHead (&sm_RequestsListHead);
        sm_pTraceLog = CreateRefTraceLog( 10000, 0 );
    }

    InitializeSRWLock(&sm_RequestsListLock);

    return S_OK;
}

//static
VOID
WEBSOCKET_HANDLER::StaticTerminate(
    VOID
    )
{
    if (!g_fWebSocketStaticInitialize)
    {
        return;
    }

    if (sm_pTraceLog)
    {
        DestroyRefTraceLog(sm_pTraceLog);
        sm_pTraceLog = nullptr;
    }
}

VOID
WEBSOCKET_HANDLER::InsertRequest(
    VOID
    )
{
    if (g_fEnableReferenceCountTracing)
    {
        AcquireSRWLockExclusive(&sm_RequestsListLock);
        InsertTailList(&sm_RequestsListHead, &_listEntry);
        ReleaseSRWLockExclusive( &sm_RequestsListLock);
    }
}

//static
VOID
WEBSOCKET_HANDLER::RemoveRequest(
    VOID
    )
{
    if (g_fEnableReferenceCountTracing)
    {
        AcquireSRWLockExclusive(&sm_RequestsListLock);
        RemoveEntryList(&_listEntry);
        ReleaseSRWLockExclusive( &sm_RequestsListLock);
    }
}

VOID
WEBSOCKET_HANDLER::IncrementOutstandingIo(
    VOID
    )
{
    LONG dwOutstandingIo = InterlockedIncrement(&_dwOutstandingIo);
    if (sm_pTraceLog)
    {
        WriteRefTraceLog(sm_pTraceLog, dwOutstandingIo, this);
    }
}

VOID
WEBSOCKET_HANDLER::DecrementOutstandingIo(
    VOID
    )
/*++
    Routine Description:
    Decrements outstanding IO count.

    This indicates completion to IIS if all outstanding IO
    has been completed, and a Cleanup was triggered for this
    connection (denoted by _fIndicateCompletionToIis).

--*/
{
    LONG dwOutstandingIo = InterlockedDecrement (&_dwOutstandingIo);

    if (sm_pTraceLog)
    {
        WriteRefTraceLog(sm_pTraceLog, dwOutstandingIo, this);
    }

    if (dwOutstandingIo == 0 && _fIndicateCompletionToIis)
    {
        IndicateCompletionToIIS();
    }
}

VOID
WEBSOCKET_HANDLER::IndicateCompletionToIIS(
    VOID
    )
/*++
    Routine Description:
    Indicates completion to IIS.

    This returns a Pending Status, so that forwarding handler has a chance
    to do book keeping when request is finally done.

--*/
{
    LOG_TRACEF(L"WEBSOCKET_HANDLER::IndicateCompletionToIIS called %d", _dwOutstandingIo);

    //
    // close Websocket handle. This will triger a WinHttp callback
    // on handle close, then let IIS pipeline continue.
    // Make sure no pending IO as there is no IIS websocket cancelation,
    // any unexpected callback will lead to AV. Revisit it once CanelOutGoingIO works
    //
    if (_hWebSocketRequest != nullptr && _dwOutstandingIo == 0)
    {
        LOG_TRACE(L"WEBSOCKET_HANDLER::IndicateCompletionToIIS");

        _pHandler->SetStatus(FORWARDER_DONE);
        _fHandleClosed = TRUE;
        WinHttpCloseHandle(_hWebSocketRequest);
        _hWebSocketRequest = nullptr;
    }
}

HRESULT
WEBSOCKET_HANDLER::ProcessRequest(
    FORWARDING_HANDLER *pHandler,
    IHttpContext *pHttpContext,
    HINTERNET     hRequest,
    BOOL*         pfHandleCreated
)
/*++

Routine Description:

    Entry point to WebSocket Handler:

    This routine is called after the 101 response was successfully sent to
    the client.
    This routine get's a websocket handle to winhttp,
    websocket handle to IIS's websocket context, and initiates IO
    in these two endpoints.


--*/
{
    HRESULT hr = S_OK;
    //DWORD dwBuffSize = RECEIVE_BUFFER_SIZE;

    *pfHandleCreated = FALSE;
    _pHandler = pHandler;

    EnterCriticalSection(&_RequestLock);
    LOG_TRACEF(L"WEBSOCKET_HANDLER::ProcessRequest");

    //
    // Cache the points to IHttpContext3
    //
    hr = HttpGetExtendedInterface(g_pHttpServer,
            pHttpContext,
            &_pHttpContext);
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

    //
    // Get pointer to IWebSocketContext for IIS websocket IO.
    //

     _pWebSocketContext = (IWebSocketContext *) _pHttpContext->
        GetNamedContextContainer()->GetNamedContext(IIS_WEBSOCKET);
    if ( _pWebSocketContext == nullptr )
    {
        hr = HRESULT_FROM_WIN32( ERROR_FILE_NOT_FOUND );
        goto Finished;
    }

    //
    // Get Handle to Winhttp's websocket context.
    //
    _hWebSocketRequest = WINHTTP_HELPER::sm_pfnWinHttpWebSocketCompleteUpgrade(
        hRequest,
        (DWORD_PTR) pHandler);

    if (_hWebSocketRequest == nullptr)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    *pfHandleCreated = TRUE;

    //
    // Resize the send & receive buffers to be more conservative (and avoid DoS attacks).
    // NOTE: The two WinHTTP options below were added for WinBlue, so we can't
    // rely on their existence.
    //

    //if (!WinHttpSetOption(_hWebSocketRequest,
    //                      WINHTTP_OPTION_WEB_SOCKET_RECEIVE_BUFFER_SIZE,
    //                      &dwBuffSize,
    //                      sizeof(dwBuffSize)))
    //{
    //    DWORD dwRet = GetLastError();
    //    if ( dwRet != ERROR_WINHTTP_INVALID_OPTION )
    //    {
    //        hr = HRESULT_FROM_WIN32(dwRet);
    //        goto Finished;
    //    }
    //}

    //if (!WinHttpSetOption(_hWebSocketRequest,
    //                      WINHTTP_OPTION_WEB_SOCKET_SEND_BUFFER_SIZE,
    //                      &dwBuffSize,
    //                      sizeof(dwBuffSize)))
    //{
    //    DWORD dwRet = GetLastError();
    //    if ( dwRet != ERROR_WINHTTP_INVALID_OPTION )
    //    {
    //        hr = HRESULT_FROM_WIN32(dwRet);
    //        goto Finished;
    //    }
    //}

    //
    // Initiate Read on IIS
    //
    hr = DoIisWebSocketReceive();
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

    //
    // Initiate Read on WinHttp
    //

    hr = DoWinHttpWebSocketReceive();
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

Finished:
    LeaveCriticalSection(&_RequestLock);

    if (FAILED_LOG(hr))
    {
        LOG_ERRORF(L"Process Request Failed with HR=%08x", hr);
    }

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::DoIisWebSocketReceive(
    VOID
)
/*++

Routine Description:

    Initiates a websocket receive on the IIS Websocket Context.


--*/
{
    HRESULT hr = S_OK;
    DWORD   dwBufferSize = RECEIVE_BUFFER_SIZE;
    BOOL    fUtf8Encoded;
    BOOL    fFinalFragment;
    BOOL    fClose;

    LOG_TRACE(L"WEBSOCKET_HANDLER::DoIisWebSocketReceive");

    IncrementOutstandingIo();

    hr = _pWebSocketContext->ReadFragment(
            &_IisReceiveBuffer,
            &dwBufferSize,
            TRUE,
            &fUtf8Encoded,
            &fFinalFragment,
            &fClose,
            OnReadIoCompletion,
            this,
            nullptr);
    if (FAILED_LOG(hr))
    {
        DecrementOutstandingIo();
        LOG_ERRORF(L"WEBSOCKET_HANDLER::DoIisWebSocketSend failed with %08x", hr);
    }

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::DoWinHttpWebSocketReceive(
    VOID
)
/*++

Routine Description:

    Initiates a websocket receive on WinHttp


--*/
{
    HRESULT hr = S_OK;
    DWORD   dwError = NO_ERROR;

    LOG_TRACE(L"WEBSOCKET_HANDLER::DoWinHttpWebSocketReceive");

    IncrementOutstandingIo();

    dwError = WINHTTP_HELPER::sm_pfnWinHttpWebSocketReceive(
                _hWebSocketRequest,
                &_WinHttpReceiveBuffer,
                RECEIVE_BUFFER_SIZE,
                nullptr,
                nullptr);

    if (dwError != NO_ERROR)
    {
        DecrementOutstandingIo();
        hr = HRESULT_FROM_WIN32(dwError);
        LOG_ERRORF(L"WEBSOCKET_HANDLER::DoWinHttpWebSocketReceive failed with %08x", hr);
    }

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::DoIisWebSocketSend(
    DWORD cbData,
    WINHTTP_WEB_SOCKET_BUFFER_TYPE  eBufferType
)
/*++

Routine Description:

    Initiates a websocket send on IIS

--*/
{
    HRESULT hr = S_OK;
    BOOL    fUtf8Encoded = FALSE;
    BOOL    fFinalFragment = FALSE;
    BOOL    fClose = FALSE;

    LOG_TRACEF(L"WEBSOCKET_HANDLER::DoIisWebSocketSend %d", eBufferType);

    if (eBufferType == WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE)
    {
        //
        // Query Close Status from WinHttp
        //

        DWORD dwError = NO_ERROR;
        USHORT uStatus = 0;
        DWORD  dwReceived = 0;
        STACK_STRU(strCloseReason, 128);

        dwError = WINHTTP_HELPER::sm_pfnWinHttpWebSocketQueryCloseStatus(
                    _hWebSocketRequest,
                    &uStatus,
                    &_WinHttpReceiveBuffer,
                    RECEIVE_BUFFER_SIZE,
                    &dwReceived);

        if (dwError != NO_ERROR)
        {
            hr = HRESULT_FROM_WIN32(dwError);
            goto Finished;
        }

        //
        // Convert close reason to WCHAR
        //
        hr = strCloseReason.CopyA((PCSTR)&_WinHttpReceiveBuffer,
            dwReceived);
        if (FAILED_LOG(hr))
        {
            goto Finished;
        }

        IncrementOutstandingIo();
        //
        // Backend end may start close hand shake first
        // Need to indicate no more receive should be called on WinHttp connection
        //
        _fReceivedCloseMsg = TRUE;
        _fIndicateCompletionToIis = TRUE;

        //
        // Send close to IIS.
        //
        hr = _pWebSocketContext->SendConnectionClose(
            TRUE,
            uStatus,
            uStatus == 1005 ? nullptr : strCloseReason.QueryStr(),
            OnWriteIoCompletion,
            this,
            nullptr);
    }
    else
    {
        //
        // Get equivalent flags for IIS API from buffer type.
        //

        WINHTTP_HELPER::GetFlagsFromBufferType(eBufferType,
            &fUtf8Encoded,
            &fFinalFragment,
            &fClose);

        IncrementOutstandingIo();

        //
        // Do the Send.
        //
        hr = _pWebSocketContext->WriteFragment(
                &_WinHttpReceiveBuffer,
                &cbData,
                TRUE,
                fUtf8Encoded,
                fFinalFragment,
                OnWriteIoCompletion,
                this,
                nullptr);
    }

    if (FAILED_LOG(hr))
    {
        DecrementOutstandingIo();
    }

Finished:
    if (FAILED_LOG(hr))
    {
        LOG_ERRORF(L"WEBSOCKET_HANDLER::DoIisWebSocketSend failed with %08x", hr);
    }

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::DoWinHttpWebSocketSend(
    DWORD cbData,
    WINHTTP_WEB_SOCKET_BUFFER_TYPE  eBufferType
)
/*++

Routine Description:

    Initiates a websocket send on WinHttp

--*/
{
    DWORD       dwError = NO_ERROR;
    HRESULT     hr = S_OK;

    LOG_TRACEF(L"WEBSOCKET_HANDLER::DoWinHttpWebSocketSend, %d", eBufferType);

    if (eBufferType == WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE)
    {
        USHORT  uStatus;
        LPCWSTR pszReason;
        STACK_STRA(strCloseReason, 128);

        //
        // Get Close status from IIS.
        //
        hr = _pWebSocketContext->GetCloseStatus(&uStatus,
                &pszReason);

        if (FAILED_LOG(hr))
        {
            goto Finished;
        }

        //
        // Convert status to UTF8
        //
        hr = strCloseReason.CopyWToUTF8Unescaped(pszReason);
        if (FAILED_LOG(hr))
        {
            goto Finished;
        }

        IncrementOutstandingIo();

        //
        // Send Close.
        //
        dwError = WINHTTP_HELPER::sm_pfnWinHttpWebSocketShutdown(
            _hWebSocketRequest,
            uStatus,
            strCloseReason.QueryCCH() == 0 ? nullptr : (PVOID) strCloseReason.QueryStr(),
            strCloseReason.QueryCCH());

        if (dwError == ERROR_IO_PENDING)
        {
            //
            // Call will complete asynchronously, return.
            // ignore error.
            //
            LOG_TRACE(L"WEBSOCKET_HANDLER::DoWinhttpWebSocketSend IO_PENDING");

            dwError = NO_ERROR;
        }
        else
        {
            if (dwError == NO_ERROR)
            {
                //
                // Call completed synchronously.
                //
                LOG_TRACE(L"WEBSOCKET_HANDLER::DoWinhttpWebSocketSend Shutdown successful.");
            }
        }
    }
    else
    {
        IncrementOutstandingIo();

        dwError = WINHTTP_HELPER::sm_pfnWinHttpWebSocketSend(
                        _hWebSocketRequest,
                        eBufferType,
                        cbData == 0 ? nullptr : &_IisReceiveBuffer,
                        cbData
                        );
    }

    if (dwError != NO_ERROR)
    {
        hr = HRESULT_FROM_WIN32(dwError);
        DecrementOutstandingIo();
        goto Finished;
    }

Finished:
    if (FAILED_LOG(hr))
    {
        LOG_ERRORF(L"WEBSOCKET_HANDLER::DoWinHttpWebSocketSend failed with %08x", hr);
    }

    return hr;
}

//static
VOID
WINAPI
WEBSOCKET_HANDLER::OnReadIoCompletion(
    HRESULT     hrError,
    VOID *      pvCompletionContext,
    DWORD       cbIO,
    BOOL        fUTF8Encoded,
    BOOL        fFinalFragment,
    BOOL        fClose
    )
/*++

 Routine Description:

     Completion routine for Read's from IIS pipeline.

--*/
{
    WEBSOCKET_HANDLER *     pHandler = (WEBSOCKET_HANDLER *)
                            pvCompletionContext;

    pHandler->OnIisReceiveComplete(
        hrError,
        cbIO,
        fUTF8Encoded,
        fFinalFragment,
        fClose
        );
}

//static
VOID
WINAPI
WEBSOCKET_HANDLER::OnWriteIoCompletion(
    HRESULT     hrError,
    VOID *      pvCompletionContext,
    DWORD       cbIO,
    BOOL        fUTF8Encoded,
    BOOL        fFinalFragment,
    BOOL        fClose
    )
/*++
 Routine Description:

     Completion routine for Write's from IIS pipeline.

--*/
{
    WEBSOCKET_HANDLER *     pHandler = (WEBSOCKET_HANDLER *)
                            pvCompletionContext;

    UNREFERENCED_PARAMETER(fUTF8Encoded);
    UNREFERENCED_PARAMETER(fFinalFragment);
    UNREFERENCED_PARAMETER(fClose);

    pHandler->OnIisSendComplete(
        hrError,
        cbIO
        );
}


HRESULT
WEBSOCKET_HANDLER::OnWinHttpSendComplete(
    WINHTTP_WEB_SOCKET_STATUS *
    )
/*++

Routine Description:
    Completion callback executed when a send to backend
    server completes.

    If the send was successful, issue the next read
    on the client's endpoint.

++*/
{
    HRESULT                 hr = S_OK;
    BOOL                    fLocked = FALSE;
    CleanupReason           cleanupReason = CleanupReasonUnknown;

    LOG_TRACE(L"WEBSOCKET_HANDLER::OnWinHttpSendComplete");

    if (_fCleanupInProgress)
    {
        goto Finished;
    }

    EnterCriticalSection (&_RequestLock);

    fLocked = TRUE;

    if (_fCleanupInProgress)
    {
        goto Finished;
    }
    //
    // Data was successfully sent to backend.
    // Initiate next receive from IIS.
    //

    hr = DoIisWebSocketReceive();
    if (FAILED_LOG(hr))
    {
        goto Finished;
    }

Finished:
    if (fLocked)
    {
        LeaveCriticalSection(&_RequestLock);
    }

    if (FAILED_LOG(hr))
    {
        Cleanup (cleanupReason);

        LOG_ERRORF(L"WEBSOCKET_HANDLER::OnWinsockSendComplete failed with HR=%08x", hr);
    }

    //
    // The handler object can be gone after this call.
    // do not reference it after this statement.
    //
    DecrementOutstandingIo();

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::OnWinHttpShutdownComplete(
    VOID
    )
{
    LOG_TRACEF(L"WEBSOCKET_HANDLER::OnWinHttpShutdownComplete --%p", _pHandler);

    DecrementOutstandingIo();

    return S_OK;
}

HRESULT
WEBSOCKET_HANDLER::OnWinHttpIoError(
    WINHTTP_WEB_SOCKET_ASYNC_RESULT *pCompletionStatus
)
{
    HRESULT hr = HRESULT_FROM_WIN32(pCompletionStatus->AsyncResult.dwError);

    LOG_ERRORF(L"WEBSOCKET_HANDLER::OnWinHttpIoError HR = %08x, Operation = %d",
        hr, pCompletionStatus->AsyncResult.dwResult);

    Cleanup(ServerDisconnect);
    DecrementOutstandingIo();

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::OnWinHttpReceiveComplete(
    WINHTTP_WEB_SOCKET_STATUS * pCompletionStatus
    )
/*++

Routine Description:

    Completion callback executed when a receive completes
    on the backend server winhttp endpoint.

    Issue send on the Client(IIS) if the receive was
    successful.

    If the receive completed with zero bytes, that
    indicates that the server has disconnected the connection.
    Issue cleanup for the websocket handler.
--*/
{
    HRESULT  hr = S_OK;
    BOOL     fLocked = FALSE;
    CleanupReason cleanupReason = CleanupReasonUnknown;

    LOG_TRACEF(L"WEBSOCKET_HANDLER::OnWinHttpReceiveComplete --%p", _pHandler);

    if (_fCleanupInProgress)
    {
        goto Finished;
    }

    EnterCriticalSection(&_RequestLock);

    fLocked = TRUE;
    if (_fCleanupInProgress)
    {
        goto Finished;
    }
    hr = DoIisWebSocketSend(
            pCompletionStatus->dwBytesTransferred,
            pCompletionStatus->eBufferType
            );

    if (FAILED_LOG(hr))
    {
        cleanupReason = ClientDisconnect;
        goto Finished;
    }

Finished:
    if (fLocked)
    {
        LeaveCriticalSection(&_RequestLock);
    }
    if (FAILED_LOG(hr))
    {
        Cleanup (cleanupReason);

        LOG_ERRORF(L"WEBSOCKET_HANDLER::OnWinsockReceiveComplete failed with HR=%08x", hr);
    }

    //
    // The handler object can be gone after this call.
    // do not reference it after this statement.
    //

    DecrementOutstandingIo();

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::OnIisSendComplete(
    HRESULT  hrCompletion,
    DWORD    cbIo
    )
/*++
Routine Description:

    Completion callback executed when a send
    completes from the client.

    If send was successful,issue read on the
    server endpoint, to continue the readloop.

--*/
{
    HRESULT         hr = S_OK;
    BOOL            fLocked = FALSE;
    CleanupReason   cleanupReason = CleanupReasonUnknown;

    UNREFERENCED_PARAMETER(cbIo);

    LOG_TRACE(L"WEBSOCKET_HANDLER::OnIisSendComplete");

    if (FAILED_LOG(hrCompletion))
    {
        hr = hrCompletion;
        cleanupReason = ClientDisconnect;
        goto Finished;
    }

    if (_fCleanupInProgress)
    {
        goto Finished;
    }
    EnterCriticalSection(&_RequestLock);
    fLocked = TRUE;
    if (_fCleanupInProgress)
    {
        goto Finished;
    }

    //
    // Only call read if no close hand shake was received from backend
    //
    if (!_fReceivedCloseMsg)
    {
        //
        // Write Completed, initiate next read from backend server.
        //
        hr = DoWinHttpWebSocketReceive();
        if (FAILED_LOG(hr))
        {
            cleanupReason = ServerDisconnect;
            goto Finished;
        }
    }

Finished:
    if (fLocked)
    {
        LeaveCriticalSection(&_RequestLock);
    }
    if (FAILED_LOG(hr))
    {
        Cleanup (cleanupReason);

        LOG_ERRORF(L"WEBSOCKET_HANDLER::OnIisSendComplete failed with HR=%08x", hr);
    }

    //
    // The handler object can be gone after this call.
    // do not reference it after this statement.
    //

    DecrementOutstandingIo();

    return hr;
}

HRESULT
WEBSOCKET_HANDLER::OnIisReceiveComplete(
    HRESULT     hrCompletion,
    DWORD       cbIO,
    BOOL        fUTF8Encoded,
    BOOL        fFinalFragment,
    BOOL        fClose
    )
/*++
Routine Description:

    Completion routine executed when a receive completes
    from the client (IIS endpoint).

    If the receive was successful, initiate a send on
    the backend server (winhttp) endpoint.

    If the receive failed, initiate cleanup.

--*/
{
    HRESULT    hr = S_OK;
    BOOL       fLocked = FALSE;
    CleanupReason cleanupReason = CleanupReasonUnknown;
    WINHTTP_WEB_SOCKET_BUFFER_TYPE  BufferType{};

    LOG_TRACE(L"WEBSOCKET_HANDLER::OnIisReceiveComplete");

    if (FAILED_LOG(hrCompletion))
    {
        cleanupReason = ClientDisconnect;
        hr = hrCompletion;
        goto Finished;
    }

    if (_fCleanupInProgress)
    {
        goto Finished;
    }

    EnterCriticalSection(&_RequestLock);

    fLocked = TRUE;
    if (_fCleanupInProgress)
    {
        goto Finished;
    }
    //
    // Get Buffer Type from flags.
    //

    WINHTTP_HELPER::GetBufferTypeFromFlags(fUTF8Encoded,
        fFinalFragment,
        fClose,
        &BufferType);

    //
    // Initiate Send.
    //

    hr =  DoWinHttpWebSocketSend(cbIO, BufferType);
    if (FAILED_LOG(hr))
    {
        cleanupReason = ServerDisconnect;
        goto Finished;
    }

Finished:
    if (fLocked)
    {
        LeaveCriticalSection(&_RequestLock);
    }
    if (FAILED_LOG(hr))
    {
        Cleanup (cleanupReason);

        LOG_ERRORF(L"WEBSOCKET_HANDLER::OnIisReceiveComplete failed with HR=%08x", hr);
    }

    //
    // The handler object can be gone after this call.
    // do not reference it after this statement.
    //

    DecrementOutstandingIo();

    return hr;
}

VOID
WEBSOCKET_HANDLER::Cleanup(
    CleanupReason reason
)
/*++

Routine Description:

    Cleanup function for the websocket handler.

    Initiates cancelIo on the two IO endpoints:
    IIS, WinHttp client.

Arguments:
    CleanupReason
--*/
{
    BOOL    fLocked = FALSE;
    LOG_TRACEF(L"WEBSOCKET_HANDLER::Cleanup Initiated with reason %d", reason);

    if (_fCleanupInProgress)
    {
        goto Finished;
    }

    EnterCriticalSection(&_RequestLock);

    fLocked = TRUE;
    if (_fCleanupInProgress)
    {
        goto Finished;
    }

    _fCleanupInProgress = TRUE;

    _fIndicateCompletionToIis = TRUE;

    //
    // TODO:: Raise FREB event with cleanup reason.
    //
    if (reason == ClientDisconnect || reason == ServerStateUnavailable)
    {
        //
        // Calling shutdown to notify the backend about disconnect
        //
        WINHTTP_HELPER::sm_pfnWinHttpWebSocketShutdown(
            _hWebSocketRequest,
            1011,    // indicate that a server is terminating the connection because it encountered
                     // an unexpected condition that prevent it from fulfilling the request
            nullptr, // Reason
            0);      // length of Reason

    }

    if (reason == ServerDisconnect || reason == ServerStateUnavailable)
    {
        _pHttpContext->CancelIo();

        //
        // CancelIo sometimes may not be able to cancel pending websocket IO.
        // ResetConnection to force IISWebsocket module to release the pipeline.
        //
        _pHttpContext->GetResponse()->ResetConnection();
    }

Finished:
    if (fLocked)
    {
        LeaveCriticalSection(&_RequestLock);
    }
}
