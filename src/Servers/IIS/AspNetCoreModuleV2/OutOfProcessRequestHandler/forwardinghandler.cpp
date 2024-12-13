// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "forwardinghandler.h"
#include "url_utility.h"
#include "exceptions.h"
#include "ServerErrorApplication.h"
#include "ServerErrorHandler.h"
#include "resource.h"
#include "file_utility.h"

// Just to be aware of the FORWARDING_HANDLER object size.
C_ASSERT(sizeof(FORWARDING_HANDLER) <= 632);

#define DEF_MAX_FORWARDS        32
#define HEX_TO_ASCII(c) ((CHAR)(((c) < 10) ? ((c) + '0') : ((c) + 'a' - 10)))
#define BUFFER_SIZE         (8192UL)
#define ENTITY_BUFFER_SIZE  (6 + BUFFER_SIZE + 2)

#define FORWARDING_HANDLER_SIGNATURE        ((DWORD)'FHLR')
#define FORWARDING_HANDLER_SIGNATURE_FREE   ((DWORD)'fhlr')

ALLOC_CACHE_HANDLER *       FORWARDING_HANDLER::sm_pAlloc = nullptr;
TRACE_LOG *                 FORWARDING_HANDLER::sm_pTraceLog = nullptr;
PROTOCOL_CONFIG             FORWARDING_HANDLER::sm_ProtocolConfig;
RESPONSE_HEADER_HASH *      FORWARDING_HANDLER::sm_pResponseHeaderHash = nullptr;

FORWARDING_HANDLER::FORWARDING_HANDLER(
    _In_ IHttpContext                  *pW3Context,
    _In_ std::unique_ptr<OUT_OF_PROCESS_APPLICATION, IAPPLICATION_DELETER> pApplication
) : REQUEST_HANDLER(*pW3Context),
    m_Signature(FORWARDING_HANDLER_SIGNATURE),
    m_RequestStatus(FORWARDER_START),
    m_fClientDisconnected(FALSE),
    m_fResponseHeadersReceivedAndSet(FALSE),
    m_fDoReverseRewriteHeaders(FALSE),
    m_fFinishRequest(FALSE),
    m_fHasError(FALSE),
    m_pszHeaders(nullptr),
    m_cchHeaders(0),
    m_BytesToReceive(0),
    m_BytesToSend(0),
    m_fWebSocketEnabled(FALSE),
    m_pWebSocket(nullptr),
    m_dwHandlers (1), // default http handler
    m_fDoneAsyncCompletion(FALSE),
    m_fHttpHandleInClose(FALSE),
    m_fWebSocketHandleInClose(FALSE),
    m_fServerResetConn(FALSE),
    m_cRefs(1),
    m_pW3Context(pW3Context),
    m_pApplication(std::move(pApplication)),
    m_fReactToDisconnect(FALSE)
{
    LOG_TRACE(L"FORWARDING_HANDLER::FORWARDING_HANDLER");

    m_fWebSocketSupported = m_pApplication->QueryWebsocketStatus();
    m_fForwardResponseConnectionHeader = m_pApplication->QueryConfig()->QueryForwardResponseConnectionHeader()->Equals(L"true", /* ignoreCase */ 1);
    InitializeSRWLock(&m_RequestLock);
}

FORWARDING_HANDLER::~FORWARDING_HANDLER(
)
{
    //
    // Destructor has started.
    //
    m_Signature = FORWARDING_HANDLER_SIGNATURE_FREE;

    LOG_TRACE(L"FORWARDING_HANDLER::~FORWARDING_HANDLER");

    //
    // RemoveRequest() should already have been called and m_pDisconnect
    // has been freed or m_pDisconnect was never initialized.
    //
    // Disconnect notification cleanup would happen first, before
    // the FORWARDING_HANDLER instance got removed from m_pSharedhandler list.
    // The m_pServer cleanup would happen afterwards, since there may be a
    // call pending from SHARED_HANDLER to  FORWARDING_HANDLER::SetStatusAndHeaders()
    //
    DBG_ASSERT(!m_fReactToDisconnect);

    RemoveRequest();

    FreeResponseBuffers();

    if (m_pWebSocket)
    {
        m_pWebSocket->Terminate();
        m_pWebSocket = nullptr;
    }
}

__override
REQUEST_NOTIFICATION_STATUS
FORWARDING_HANDLER::ExecuteRequestHandler()
{
    REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;
    HRESULT                     hr = S_OK;
    BOOL                        fRequestLocked = FALSE;
    BOOL                        fFailedToStartKestrel = FALSE;
    BOOL                        fSecure = FALSE;
    HINTERNET                   hConnect = nullptr;
    IHttpRequest               *pRequest = m_pW3Context->GetRequest();
    IHttpResponse              *pResponse = m_pW3Context->GetResponse();
    IHttpConnection            *pClientConnection = nullptr;
    PROTOCOL_CONFIG            *pProtocol = &sm_ProtocolConfig;
    SERVER_PROCESS             *pServerProcess = nullptr;

    USHORT                      cchHostName = 0;

    STACK_STRU(strDestination, 32);
    STACK_STRU(strUrl, 2048);
    STACK_STRU(struEscapedUrl, 2048);

    //
    // Take a reference so that object does not go away as a result of
    // async completion.
    //
    ReferenceRequestHandler();

    // override Protocol related config from aspNetCore config
    pProtocol->OverrideConfig(m_pApplication->QueryConfig());

    // check connection
    pClientConnection = m_pW3Context->GetConnection();
    if (pClientConnection == nullptr ||
        !pClientConnection->IsConnected())
    {
        FAILURE(HRESULT_FROM_WIN32(WSAECONNRESET));
    }

    if (m_pApplication == nullptr)
    {
        FAILURE(E_INVALIDARG);
    }

    hr = m_pApplication->GetProcess(&pServerProcess);
    if (FAILED_LOG(hr))
    {
        fFailedToStartKestrel = TRUE;
        FAILURE(hr);
    }

    if (pServerProcess == nullptr)
    {
        fFailedToStartKestrel = TRUE;
        FAILURE(HRESULT_FROM_WIN32(ERROR_CREATE_FAILED));
    }

    if (pServerProcess->QueryWinHttpConnection() == nullptr)
    {
        FAILURE(HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE));
    }

    hConnect = pServerProcess->QueryWinHttpConnection()->QueryHandle();

    m_pszOriginalHostHeader = pRequest->GetHeader(HttpHeaderHost, &cchHostName);
    //
    // parse original url
    //
    FAILURE_IF_FAILED(URL_UTILITY::SplitUrl(pRequest->GetRawHttpRequest()->CookedUrl.pFullUrl,
        &fSecure,
        &strDestination,
        &strUrl));

    FAILURE_IF_FAILED(URL_UTILITY::EscapeAbsPath(pRequest, &struEscapedUrl));

    m_fDoReverseRewriteHeaders = pProtocol->QueryReverseRewriteHeaders();

    m_cMinBufferLimit = pProtocol->QueryMinResponseBuffer();

    //
    // Mark request as websocket if upgrade header is present.
    //
    if (m_fWebSocketSupported)
    {
        USHORT cchHeader = 0;
        PCSTR pszWebSocketHeader = pRequest->GetHeader("Upgrade", &cchHeader);
        if (cchHeader == 9 && _stricmp(pszWebSocketHeader, "websocket") == 0)
        {
            m_fWebSocketEnabled = TRUE;

            // WinHttp does not support any extensions being returned by the server, so we remove the request header to avoid the server
            // responding with any accepted extensions.
            pRequest->DeleteHeader("Sec-WebSocket-Extensions");
        }
    }

    FAILURE_IF_FAILED(CreateWinHttpRequest(pRequest,
        pProtocol,
        hConnect,
        &struEscapedUrl,
        pServerProcess));

    m_fReactToDisconnect = TRUE;

    // require lock as client disconnect callback may happen
    AcquireSRWLockShared(&m_RequestLock);
    fRequestLocked = TRUE;

    //
    // Remember the handler being processed in the current thread
    // before staring a WinHTTP operation.
    //
    DBG_ASSERT(fRequestLocked);
    DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == nullptr);
    TlsSetValue(g_dwTlsIndex, this);
    DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);

    if (m_hRequest == nullptr)
    {
        FAILURE(HRESULT_FROM_WIN32(WSAECONNRESET));
    }

    //
    // Begins normal request handling. Send request to server.
    //
    m_RequestStatus = FORWARDER_SENDING_REQUEST;

    //
    // Calculate the bytes to receive from the content length.
    //
    DWORD cbContentLength = 0;
    PCSTR pszContentLength = pRequest->GetHeader(HttpHeaderContentLength);
    if (pszContentLength != nullptr)
    {
        cbContentLength = m_BytesToReceive = atol(pszContentLength);
        if (m_BytesToReceive == INFINITE)
        {
            FAILURE(HRESULT_FROM_WIN32(WSAECONNRESET));
        }
    }
    else if (pRequest->GetHeader(HttpHeaderTransferEncoding) != nullptr)
    {
        m_BytesToReceive = INFINITE;
    }

    if (m_fWebSocketEnabled)
    {
        //
        // Set the upgrade flag for a websocket request.
        //
        if (!WinHttpSetOption(m_hRequest,
            WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET,
            nullptr,
            0))
        {
            FINISHED(HRESULT_FROM_WIN32(GetLastError()));
        }
    }

    m_cchLastSend = m_cchHeaders;

    //FREB log
    if (ANCMEvents::ANCM_REQUEST_FORWARD_START::IsEnabled(m_pW3Context->GetTraceContext()))
    {
        ANCMEvents::ANCM_REQUEST_FORWARD_START::RaiseEvent(
            m_pW3Context->GetTraceContext(),
            nullptr);
    }

    if (!WinHttpSendRequest(m_hRequest,
        m_pszHeaders,
        m_cchHeaders,
        nullptr,
        0,
        cbContentLength,
        reinterpret_cast<DWORD_PTR>(this)))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());

        LOG_TRACE(L"FORWARDING_HANDLER::OnExecuteRequestHandler, Send request failed");

        // FREB log
        if (ANCMEvents::ANCM_REQUEST_FORWARD_FAIL::IsEnabled(m_pW3Context->GetTraceContext()))
        {
            ANCMEvents::ANCM_REQUEST_FORWARD_FAIL::RaiseEvent(
                m_pW3Context->GetTraceContext(),
                nullptr,
                hr);
        }

        FAILURE_IF_FAILED(hr);
    }

    //
    // Async WinHTTP operation is in progress. Release this thread meanwhile,
    // OnWinHttpCompletion method should resume the work by posting an IIS completion.
    //
    retVal = RQ_NOTIFICATION_PENDING;
    goto Finished;

Failure:
    m_RequestStatus = FORWARDER_DONE;

    //disable client disconnect callback
    RemoveRequest();

    pResponse->DisableKernelCache();
    pResponse->GetRawHttpResponse()->EntityChunkCount = 0;
    if (hr == HRESULT_FROM_WIN32(WSAECONNRESET))
    {
        pResponse->SetStatus(400, "Bad Request", 0, hr);
    }
    else if (hr == E_APPLICATION_EXITING)
    {
        pResponse->SetStatus(503, "Service Unavailable", 0, S_OK, nullptr, TRUE);
    }
    else if (fFailedToStartKestrel && !m_pApplication->QueryConfig()->QueryDisableStartUpErrorPage())
    {
        static std::string htmlResponse = FILE_UTILITY::GetHtml(g_hOutOfProcessRHModule,
            ANCM_ERROR_PAGE,
            502,
            5,
            "ANCM Out-Of-Process Startup Failure",
            "<ul><li> The application process failed to start </li><li> The application process started but then stopped </li><li> The application process started but failed to listen on the configured port </li></ul>");

        ServerErrorHandler handler(*m_pW3Context,
            502,
            5,
            "Bad Gateway",
            hr,
            m_pApplication->QueryConfig()->QueryDisableStartUpErrorPage(),
            htmlResponse);

        handler.ExecuteRequestHandler();
    }
    else
    {
        //
        // default error behavior
        //
        pResponse->SetStatus(502, "Bad Gateway", 3, hr);
    }
    //
    // Finish the request on failure.
    //
    retVal = RQ_NOTIFICATION_FINISH_REQUEST;

Finished:
    if (fRequestLocked)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        TlsSetValue(g_dwTlsIndex, nullptr);
        ReleaseSRWLockShared(&m_RequestLock);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == nullptr);
    }

    DereferenceRequestHandler();
    //
    // Do not use this object after dereferencing it, it may be gone.
    //

    return retVal;
}

__override
REQUEST_NOTIFICATION_STATUS
FORWARDING_HANDLER::AsyncCompletion(
    DWORD           cbCompletion,
    HRESULT         hrCompletionStatus
)
/*++

Routine Description:

Handle the completion from IIS and continue the execution
of this request based on the current state.

Arguments:

cbCompletion - Number of bytes associated with this completion
dwCompletionStatus - the win32 status associated with this completion

Return Value:

REQUEST_NOTIFICATION_STATUS

--*/
{
    HRESULT                     hr = S_OK;
    REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_PENDING;
    BOOL                        fLocked = FALSE;
    BOOL                        fClientError = FALSE;
    BOOL                        fClosed = FALSE;
    BOOL                        fWebSocketUpgraded = FALSE;

    DBG_ASSERT(m_pW3Context != nullptr);
    __analysis_assume(m_pW3Context != nullptr);

    //
    // Take a reference so that object does not go away as a result of
    // async completion.
    //
    ReferenceRequestHandler();

    if (sm_pTraceLog != nullptr)
    {
        WriteRefTraceLogEx(sm_pTraceLog,
            m_cRefs,
            this,
            "FORWARDING_HANDLER::OnAsyncCompletion Enter",
            reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(cbCompletion)),
            reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(hrCompletionStatus)));
    }

    if (TlsGetValue(g_dwTlsIndex) != this)
    {
        //
        // Acquire exclusive lock as WinHTTP callback may happen on different thread
        // We don't want two threads signal IIS pipeline simultaneously
        //
        AcquireLockExclusive();
        fLocked = TRUE;
    }

    if (m_fClientDisconnected && (m_RequestStatus != FORWARDER_DONE))
    {
        FAILURE(ERROR_CONNECTION_ABORTED);
    }

    if (m_RequestStatus == FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
    {
        LOG_TRACE(L"FORWARDING_HANDLER::OnAsyncCompletion, Send completed for 101 response");

        //
        // This should be the write completion of the 101 response.
        //
        m_pWebSocket = new WEBSOCKET_HANDLER();
        if (m_pWebSocket == nullptr)
        {
            FAILURE(E_OUTOFMEMORY);
        }

        hr = m_pWebSocket->ProcessRequest(this, m_pW3Context, m_hRequest, &fWebSocketUpgraded);
        if (fWebSocketUpgraded)
        {
            // WinHttp WebSocket handle has been created, bump the counter so that remember to close it
            // and prevent from premature postcomplation and unexpected callback from winhttp
            InterlockedIncrement(&m_dwHandlers);
        }

        // This failure could happen when client disconnect happens or backend server fails
        // after websocket upgrade
        FAILURE_IF_FAILED(hr);

        //
        // WebSocket upgrade is successful. Close the WinHttpRequest Handle
        //
        m_fHttpHandleInClose = TRUE;
        fClosed = WinHttpCloseHandle(m_hRequest);
        DBG_ASSERT(fClosed);
        m_hRequest = nullptr;

        if (!fClosed)
        {
            FAILURE(HRESULT_FROM_WIN32(GetLastError()));
        }
        retVal = RQ_NOTIFICATION_PENDING;
        goto Finished;
    }

    //
    // Begins normal completion handling. There is already an exclusive acquired lock
    // for protecting the WinHTTP request handle from being closed.
    //
    switch (m_RequestStatus)
    {
    case FORWARDER_RECEIVING_RESPONSE:

        //
        // This is a completion of a write (send) to http.sys, abort in case of
        // failure, if there is more data available from WinHTTP, read it
        // or else ask if there is more.
        //
        if (FAILED_LOG(hrCompletionStatus))
        {
            hr = hrCompletionStatus;
            fClientError = TRUE;
            FAILURE(hr);
        }

        FAILURE_IF_FAILED(OnReceivingResponse());
        break;

    case FORWARDER_SENDING_REQUEST:

        hr = OnSendingRequest(cbCompletion,
            hrCompletionStatus,
            &fClientError);
        FAILURE_IF_FAILED(hr);
        break;

    default:
        DBG_ASSERT(m_RequestStatus == FORWARDER_DONE);
        if (m_hRequest == nullptr && m_pWebSocket == nullptr)
        {
            // Request must have been done
            if (!m_fFinishRequest)
            {
                goto Failure;
            }

            if (m_fHasError)
            {
                retVal = RQ_NOTIFICATION_FINISH_REQUEST;
            }
            else
            {
                retVal = RQ_NOTIFICATION_CONTINUE;
            }
        }
        goto Finished;
    }

    //
    // Either OnReceivingResponse or OnSendingRequest initiated an
    // async WinHTTP operation, release this thread meanwhile,
    // OnWinHttpCompletion method should resume the work by posting an IIS completion.
    //
    retVal = RQ_NOTIFICATION_PENDING;
    goto Finished;

Failure:

    //
    // Reset status for consistency.
    //
    m_RequestStatus = FORWARDER_DONE;
    if (!m_fHasError)
    {
        m_fHasError = TRUE;

        //
        // Do the right thing based on where the error originated from.
        //
        IHttpResponse *pResponse = m_pW3Context->GetResponse();
        pResponse->DisableKernelCache();
        pResponse->GetRawHttpResponse()->EntityChunkCount = 0;

        if (fClientError || m_fClientDisconnected)
        {
            if (!m_fResponseHeadersReceivedAndSet)
            {
                pResponse->SetStatus(400, "Bad Request", 0, HRESULT_FROM_WIN32(WSAECONNRESET));
            }
            else
            {
                //
                // Response headers from origin server were
                // already received and set for the current response.
                // Honor the response status.
                //
            }
        }
        else
        {
            STACK_STRU(strDescription, 128);

            pResponse->SetStatus(502, "Bad Gateway", 3, hr);

            if (hr > HRESULT_FROM_WIN32(WINHTTP_ERROR_BASE) &&
                hr <= HRESULT_FROM_WIN32(WINHTTP_ERROR_LAST))
            {
#pragma prefast (suppress : __WARNING_FUNCTION_NEEDS_REVIEW, "Function and parameters reviewed.")
                FormatMessage(
                    FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_HMODULE,
                    g_hWinHttpModule,
                    HRESULT_CODE(hr),
                    0,
                    strDescription.QueryStr(),
                    strDescription.QuerySizeCCH(),
                    nullptr);
            }
            else
            {
                LoadString(g_hAspNetCoreModule,
                    IDS_SERVER_ERROR,
                    strDescription.QueryStr(),
                    strDescription.QuerySizeCCH());
            }
            (VOID)strDescription.SyncWithBuffer();

            if (strDescription.QueryCCH() != 0)
            {
                pResponse->SetErrorDescription(
                    strDescription.QueryStr(),
                    strDescription.QueryCCH(),
                    FALSE);
            }

            if (hr == HRESULT_FROM_WIN32(ERROR_WINHTTP_INVALID_SERVER_RESPONSE))
            {
                if (!m_fServerResetConn)
                {
                    RemoveRequest();
                    pResponse->ResetConnection();
                    m_fServerResetConn = TRUE;
                }
            }
        }
    }

    if (m_pWebSocket != nullptr && !m_fWebSocketHandleInClose)
    {
        m_fWebSocketHandleInClose = TRUE;
        m_pWebSocket->TerminateRequest();
    }

    if (m_hRequest != nullptr && !m_fHttpHandleInClose)
    {
        m_fHttpHandleInClose = TRUE;
        WinHttpCloseHandle(m_hRequest);
        m_hRequest = nullptr;
    }

Finished:

    if (retVal != RQ_NOTIFICATION_PENDING)
    {

        DBG_ASSERT(m_dwHandlers == 0);
        RemoveRequest();

        // This is just a safety guard to prevent from returning non pending status no more once
        // which should never happen
        if (!m_fDoneAsyncCompletion)
        {
            m_fDoneAsyncCompletion = TRUE;
        }
        else
        {
            retVal = RQ_NOTIFICATION_PENDING;
        }
    }

    if (fLocked)
    {
        ReleaseLockExclusive();
    }

    DereferenceRequestHandler();
    //
    // Do not use this object after dereferencing it, it may be gone.
    //
    LOG_TRACEF(L"FORWARDING_HANDLER::OnAsyncCompletion Done %d", retVal);
    return retVal;
}

// static
HRESULT
FORWARDING_HANDLER::StaticInitialize(
    BOOL fEnableReferenceCountTracing
)
/*++

Routine Description:

Global initialization routine for FORWARDING_HANDLERs

Arguments:

fEnableReferenceCountTracing  - True if ref count tracing should be use.

Return Value:

HRESULT

--*/
{
    HRESULT                         hr = S_OK;

    FINISHED_IF_NULL_ALLOC(sm_pAlloc = new ALLOC_CACHE_HANDLER);
    FINISHED_IF_FAILED(sm_pAlloc->Initialize(sizeof(FORWARDING_HANDLER), 64)); // nThreshold

    FINISHED_IF_NULL_ALLOC(sm_pResponseHeaderHash = new RESPONSE_HEADER_HASH);

    FINISHED_IF_FAILED(sm_pResponseHeaderHash->Initialize());

    // Initialize PROTOCOL_CONFIG
    FINISHED_IF_FAILED(sm_ProtocolConfig.Initialize());

    if (fEnableReferenceCountTracing)
    {
        sm_pTraceLog = CreateRefTraceLog(10000, 0);
    }

Finished:
    if (FAILED_LOG(hr))
    {
        StaticTerminate();
    }
    return hr;
}

//static
VOID
FORWARDING_HANDLER::StaticTerminate()
{
    if (sm_pResponseHeaderHash != nullptr)
    {
        sm_pResponseHeaderHash->Clear();
        delete sm_pResponseHeaderHash;
        sm_pResponseHeaderHash = nullptr;
    }

    if (sm_pTraceLog != nullptr)
    {
        DestroyRefTraceLog(sm_pTraceLog);
        sm_pTraceLog = nullptr;
    }

    if (sm_pAlloc != nullptr)
    {
        delete sm_pAlloc;
        sm_pAlloc = nullptr;
    }
}

// static
void * FORWARDING_HANDLER::operator new(size_t)
{
    DBG_ASSERT(sm_pAlloc != nullptr);
    if (sm_pAlloc == nullptr)
    {
        return nullptr;
    }
    return sm_pAlloc->Alloc();
}

// static
void FORWARDING_HANDLER::operator delete(void * pMemory)
{
    DBG_ASSERT(sm_pAlloc != nullptr);
    if (sm_pAlloc != nullptr)
    {
        sm_pAlloc->Free(pMemory);
    }
}

HRESULT
FORWARDING_HANDLER::GetHeaders(
    _In_ const PROTOCOL_CONFIG *    pProtocol,
    _In_    BOOL                    fForwardWindowsAuthToken,
    _In_    SERVER_PROCESS*         pServerProcess,
    _Out_   PCWSTR *                ppszHeaders,
    _Inout_ DWORD *                 pcchHeaders
)
{
    PCSTR pszCurrentHeader = nullptr;
    PCSTR ppHeadersToBeRemoved = nullptr;
    PCSTR pszFinalHeader = nullptr;
    USHORT cchCurrentHeader = 0;
    DWORD cchFinalHeader = 0;
    BOOL  fSecure = FALSE;  // dummy. Used in SplitUrl. Value will not be used
                            // as ANCM always use http protocol to communicate with backend
    STRU  struDestination;
    STRU  struUrl;
    STACK_STRA(strTemp, 64);
    HTTP_REQUEST_HEADERS *pHeaders = nullptr;
    IHttpRequest *pRequest = m_pW3Context->GetRequest();
    MULTISZA mszMsAspNetCoreHeaders;

    //
    // We historically set the host section in request url to the new host header
    // this is wrong but Kestrel has dependency on it.
    // should change it in the future
    //
    if (!pProtocol->QueryPreserveHostHeader())
    {
        RETURN_IF_FAILED(URL_UTILITY::SplitUrl(pRequest->GetRawHttpRequest()->CookedUrl.pFullUrl,
            &fSecure,
            &struDestination,
            &struUrl));

        RETURN_IF_FAILED(strTemp.CopyW(struDestination.QueryStr()));
        RETURN_IF_FAILED(pRequest->SetHeader(HttpHeaderHost,
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE)); // fReplace
    }
    //
    // Strip all headers starting with MS-ASPNETCORE.
    // These headers are generated by the asp.net core module and
    // passed to the process it creates.
    //

    pHeaders = &m_pW3Context->GetRequest()->GetRawHttpRequest()->Headers;
    for (DWORD i = 0; i<pHeaders->UnknownHeaderCount; i++)
    {
        if (_strnicmp(pHeaders->pUnknownHeaders[i].pName, "MS-ASPNETCORE", 13) == 0)
        {
            mszMsAspNetCoreHeaders.Append(pHeaders->pUnknownHeaders[i].pName, (DWORD)pHeaders->pUnknownHeaders[i].NameLength);
        }
    }

    ppHeadersToBeRemoved = mszMsAspNetCoreHeaders.First();

    //
    // iterate the list of headers to be removed and delete them from the request.
    //

    while (ppHeadersToBeRemoved != nullptr)
    {
        m_pW3Context->GetRequest()->DeleteHeader(ppHeadersToBeRemoved);
        ppHeadersToBeRemoved = mszMsAspNetCoreHeaders.Next(ppHeadersToBeRemoved);
    }

    if (pServerProcess->QueryGuid() != nullptr)
    {
        RETURN_IF_FAILED(m_pW3Context->GetRequest()->SetHeader("MS-ASPNETCORE-TOKEN",
            pServerProcess->QueryGuid(),
            (USHORT)strlen(pServerProcess->QueryGuid()),
            TRUE));
    }

    if (fForwardWindowsAuthToken &&
        (_wcsicmp(m_pW3Context->GetUser()->GetAuthenticationType(), L"negotiate") == 0 ||
            _wcsicmp(m_pW3Context->GetUser()->GetAuthenticationType(), L"ntlm") == 0))
    {
        if (m_pW3Context->GetUser()->GetPrimaryToken() != nullptr &&
            m_pW3Context->GetUser()->GetPrimaryToken() != INVALID_HANDLE_VALUE)
        {
            HANDLE hTargetTokenHandle = nullptr;
            RETURN_IF_FAILED(pServerProcess->SetWindowsAuthToken(m_pW3Context->GetUser()->GetPrimaryToken(),
                &hTargetTokenHandle));

            //
            // set request header with target token value
            //
            CHAR pszHandleStr[16] = { 0 };
            if (_ui64toa_s((UINT64)hTargetTokenHandle, pszHandleStr, 16, 16) != 0)
            {
                RETURN_HR(HRESULT_FROM_WIN32(ERROR_INVALID_DATA));
            }

            RETURN_IF_FAILED(m_pW3Context->GetRequest()->SetHeader("MS-ASPNETCORE-WINAUTHTOKEN",
                pszHandleStr,
                (USHORT)strlen(pszHandleStr),
                TRUE));
        }
    }

    if (!pProtocol->QueryXForwardedForName()->IsEmpty())
    {
        strTemp.Reset();

        pszCurrentHeader = pRequest->GetHeader(pProtocol->QueryXForwardedForName()->QueryStr(), &cchCurrentHeader);
        if (pszCurrentHeader != nullptr)
        {
            RETURN_IF_FAILED(strTemp.Copy(pszCurrentHeader, cchCurrentHeader));
            RETURN_IF_FAILED(strTemp.Append(", ", 2));
        }

        RETURN_IF_FAILED(m_pW3Context->GetServerVariable("REMOTE_ADDR",
            &pszFinalHeader,
            &cchFinalHeader));

        if (pRequest->GetRawHttpRequest()->Address.pRemoteAddress->sa_family == AF_INET6)
        {
            RETURN_IF_FAILED(strTemp.Append("[", 1));
            RETURN_IF_FAILED(strTemp.Append(pszFinalHeader, cchFinalHeader));
            RETURN_IF_FAILED(strTemp.Append("]", 1));
        }
        else
        {
            RETURN_IF_FAILED(strTemp.Append(pszFinalHeader, cchFinalHeader));
        }

        if (pProtocol->QueryIncludePortInXForwardedFor())
        {
            RETURN_IF_FAILED(m_pW3Context->GetServerVariable("REMOTE_PORT",
                &pszFinalHeader,
                &cchFinalHeader));

            RETURN_IF_FAILED(strTemp.Append(":", 1));
            RETURN_IF_FAILED(strTemp.Append(pszFinalHeader, cchFinalHeader));
        }

        RETURN_IF_FAILED(pRequest->SetHeader(pProtocol->QueryXForwardedForName()->QueryStr(),
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE)); // fReplace
    }

    if (!pProtocol->QuerySslHeaderName()->IsEmpty())
    {
        const HTTP_SSL_INFO *pSslInfo = pRequest->GetRawHttpRequest()->pSslInfo;
        LPSTR pszScheme = "http";
        if (pSslInfo != nullptr)
        {
            pszScheme = "https";
        }

        strTemp.Reset();

        pszCurrentHeader = pRequest->GetHeader(pProtocol->QuerySslHeaderName()->QueryStr(), &cchCurrentHeader);
        if (pszCurrentHeader != nullptr)
        {
            RETURN_IF_FAILED(strTemp.Copy(pszCurrentHeader, cchCurrentHeader));
            RETURN_IF_FAILED(strTemp.Append(", ", 2));
        }

        RETURN_IF_FAILED(strTemp.Append(pszScheme));

        RETURN_IF_FAILED(pRequest->SetHeader(pProtocol->QuerySslHeaderName()->QueryStr(),
            strTemp.QueryStr(),
            (USHORT)strTemp.QueryCCH(),
            TRUE));
    }

    if (!pProtocol->QueryClientCertName()->IsEmpty())
    {
        if (pRequest->GetRawHttpRequest()->pSslInfo == nullptr ||
            pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo == nullptr)
        {
            pRequest->DeleteHeader(pProtocol->QueryClientCertName()->QueryStr());
        }
        else
        {
            // Resize the buffer large enough to hold the encoded certificate info
            RETURN_IF_FAILED(strTemp.Resize(
                1 + (pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo->CertEncodedSize + 2) / 3 * 4));

            Base64Encode(
                pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo->pCertEncoded,
                pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo->CertEncodedSize,
                strTemp.QueryStr(),
                strTemp.QuerySize(),
                nullptr);
            strTemp.SyncWithBuffer();

            RETURN_IF_FAILED(pRequest->SetHeader(
                pProtocol->QueryClientCertName()->QueryStr(),
                strTemp.QueryStr(),
                static_cast<USHORT>(strTemp.QueryCCH()),
                TRUE)); // fReplace
        }
    }

    //
    // Remove the connection header
    //
    if (!m_fWebSocketEnabled)
    {
        pRequest->DeleteHeader(HttpHeaderConnection);
    }

    //
    // Get all the headers to send to the client
    //
    RETURN_IF_FAILED(m_pW3Context->GetServerVariable("ALL_RAW",
        ppszHeaders,
        pcchHeaders));

    return S_OK;
}

HRESULT
FORWARDING_HANDLER::CreateWinHttpRequest(
    _In_ const IHttpRequest *       pRequest,
    _In_ const PROTOCOL_CONFIG *    pProtocol,
    _In_ HINTERNET                  hConnect,
    _Inout_ STRU *                  pstrUrl,
    _In_ SERVER_PROCESS*            pServerProcess
)
{
    HRESULT         hr = S_OK;
    PCWSTR          pszVersion = nullptr;
    PCSTR           pszVerb = nullptr;
    DWORD           dwTimeout = INFINITE;
    STACK_STRU(strVerb, 32);

    //
    // Create the request handle for this request (leave some fields blank,
    // we will fill them when sending the request)
    //
    pszVerb = pRequest->GetHttpMethod();
    FINISHED_IF_FAILED(strVerb.CopyA(pszVerb));

    //pszVersion = pProtocol->QueryVersion();
    if (pszVersion == nullptr)
    {
        DWORD cchUnused;
        FINISHED_IF_FAILED(m_pW3Context->GetServerVariable(
            "HTTP_VERSION",
            &pszVersion,
            &cchUnused));
    }

#pragma warning(push)
#pragma warning(disable: 26477) // NULL usage via Windows header
    m_hRequest = WinHttpOpenRequest(hConnect,
        strVerb.QueryStr(),
        pstrUrl->QueryStr(),
        pszVersion,
        WINHTTP_NO_REFERER,
        WINHTTP_DEFAULT_ACCEPT_TYPES,
        WINHTTP_FLAG_ESCAPE_DISABLE_QUERY
        | g_OptionalWinHttpFlags);
#pragma warning(pop)

    FINISHED_LAST_ERROR_IF_NULL (m_hRequest);

    if (!pServerProcess->IsDebuggerAttached())
    {
        dwTimeout = pProtocol->QueryTimeout();
    }

    FINISHED_LAST_ERROR_IF(!WinHttpSetTimeouts(m_hRequest,
                            dwTimeout, //resolve timeout
                            dwTimeout, // connect timeout
                            dwTimeout, // send timeout
                            dwTimeout)); // receive timeout

    DWORD dwResponseBufferLimit = pProtocol->QueryResponseBufferLimit();
    FINISHED_LAST_ERROR_IF(!WinHttpSetOption(m_hRequest,
        WINHTTP_OPTION_MAX_RESPONSE_DRAIN_SIZE,
        &dwResponseBufferLimit,
        sizeof(dwResponseBufferLimit)));

    DWORD dwMaxHeaderSize = pProtocol->QueryMaxResponseHeaderSize();
    FINISHED_LAST_ERROR_IF(!WinHttpSetOption(m_hRequest,
        WINHTTP_OPTION_MAX_RESPONSE_HEADER_SIZE,
        &dwMaxHeaderSize,
        sizeof(dwMaxHeaderSize)));

    DWORD dwOption = WINHTTP_DISABLE_COOKIES;

    dwOption |= WINHTTP_DISABLE_AUTHENTICATION;

    if (!pProtocol->QueryDoKeepAlive())
    {
        dwOption |= WINHTTP_DISABLE_KEEP_ALIVE;
    }

    FINISHED_LAST_ERROR_IF(!WinHttpSetOption(m_hRequest,
        WINHTTP_OPTION_DISABLE_FEATURE,
        &dwOption,
        sizeof(dwOption)));

    FINISHED_LAST_ERROR_IF(WinHttpSetStatusCallback(m_hRequest,
        FORWARDING_HANDLER::OnWinHttpCompletion,
        (WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
            WINHTTP_CALLBACK_FLAG_HANDLES |
            WINHTTP_CALLBACK_STATUS_SENDING_REQUEST),
        0) == WINHTTP_INVALID_STATUS_CALLBACK);

    FINISHED_IF_FAILED(GetHeaders(pProtocol,
                    m_pApplication->QueryConfig()->QueryForwardWindowsAuthToken(),
                    pServerProcess,
                   &m_pszHeaders,
                   &m_cchHeaders));
Finished:

    return hr;
}

VOID
FORWARDING_HANDLER::OnWinHttpCompletion(
    HINTERNET   hRequest,
    DWORD_PTR   dwContext,
    DWORD       dwInternetStatus,
    LPVOID      lpvStatusInformation,
    DWORD       dwStatusInformationLength
)
{
    FORWARDING_HANDLER * pThis = static_cast<FORWARDING_HANDLER *>(reinterpret_cast<PVOID>(dwContext));
    if (pThis == nullptr)
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

VOID
FORWARDING_HANDLER::OnWinHttpCompletionInternal(
    _In_ HINTERNET   hRequest,
    _In_ DWORD       dwInternetStatus,
    _In_ LPVOID      lpvStatusInformation,
    _In_ DWORD       dwStatusInformationLength
)
/*++

Routine Description:

Completion call associated with a WinHTTP operation

Arguments:

hRequest - The winhttp request handle associated with this completion
dwInternetStatus - enum specifying what the completion is for
lpvStatusInformation - completion specific information
dwStatusInformationLength - length of the above information

Return Value:

None

--*/
{
    HRESULT hr = S_OK;
    BOOL fExclusiveLocked = FALSE;
    BOOL fSharedLocked = FALSE;
    BOOL fClientError = FALSE;
    BOOL fAnotherCompletionExpected = FALSE;
    BOOL fDoPostCompletion = FALSE;
    BOOL fHandleClosing = (dwInternetStatus == WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING);
    DWORD dwHandlers = 1; // default for http handler


    DBG_ASSERT(m_pW3Context != nullptr);
    __analysis_assume(m_pW3Context != nullptr);
    IHttpResponse * pResponse = m_pW3Context->GetResponse();

    // Reference the request handler to prevent it from being released prematurely
    ReferenceRequestHandler();

    UNREFERENCED_PARAMETER(dwStatusInformationLength);

    if (sm_pTraceLog != nullptr)
    {
        WriteRefTraceLogEx(sm_pTraceLog,
            m_cRefs,
            this,
            "FORWARDING_HANDLER::OnWinHttpCompletionInternal Enter",
            reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(dwInternetStatus)),
            nullptr);
    }

    //FREB log
    if (ANCMEvents::ANCM_WINHTTP_CALLBACK::IsEnabled(m_pW3Context->GetTraceContext()))
    {
        ANCMEvents::ANCM_WINHTTP_CALLBACK::RaiseEvent(
            m_pW3Context->GetTraceContext(),
            nullptr,
            dwInternetStatus);
    }

    LOG_TRACEF(L"FORWARDING_HANDLER::OnWinHttpCompletionInternal %x -- %d --%p\n", dwInternetStatus, GetCurrentThreadId(), m_pW3Context);

    //
    // Exclusive lock on the winhttp handle to protect from a client disconnect/
    // server stop closing the handle while we are using it.
    //
    // WinHttp can call async completion on the same thread/stack, so
    // we have to account for that and not try to take the lock again,
    // otherwise, we could end up in a deadlock.
    //

    if (TlsGetValue(g_dwTlsIndex) != this)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == nullptr);
        if (m_RequestStatus != FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
        {
            // Websocket has already been guarded by critical section
            // Only require exclusive lock for non-websocket scenario which has duplex channel
            // Otherwise, there will be a deadlock
            AcquireLockExclusive();
            fExclusiveLocked = TRUE;
        }
        else
        {
            AcquireSRWLockShared(&m_RequestLock);
            TlsSetValue(g_dwTlsIndex, this);
            fSharedLocked = TRUE;
            DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        }
    }

    if (fHandleClosing)
    {
        dwHandlers = InterlockedDecrement(&m_dwHandlers);
    }

    if (m_fFinishRequest)
    {
        // Request was done by another thread, skip
        goto Finished;
    }


    if (m_fClientDisconnected && (m_RequestStatus != FORWARDER_DONE))
    {
        FAILURE(ERROR_CONNECTION_ABORTED);
    }

    //
    // In case of websocket, http request handle (m_hRequest) will be closed immediately after upgrading success
    // This close will trigger a callback with WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING
    // As m_RequestStatus is FORWARDER_RECEIVED_WEBSOCKET_RESPONSE, this callback will be skipped.
    // When WebSocket handle (m_pWebsocket) gets closed, another winhttp handle close callback will be triggered
    // This callback will be captured and then notify IIS pipeline to continue
    // This ensures no request leaks
    //
    if (m_RequestStatus == FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
    {
        fAnotherCompletionExpected = TRUE;
        if (m_pWebSocket == nullptr)
        {
            goto Finished;
        }

        switch (dwInternetStatus)
        {
        case WINHTTP_CALLBACK_STATUS_SHUTDOWN_COMPLETE:
            m_pWebSocket->OnWinHttpShutdownComplete();
            break;

        case WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE:
            m_pWebSocket->OnWinHttpSendComplete(
                (WINHTTP_WEB_SOCKET_STATUS*)lpvStatusInformation
            );
            break;

        case WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
            m_pWebSocket->OnWinHttpReceiveComplete(
                (WINHTTP_WEB_SOCKET_STATUS*)lpvStatusInformation
            );
            break;

        case WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
            m_pWebSocket->OnWinHttpIoError(
                (WINHTTP_WEB_SOCKET_ASYNC_RESULT*)lpvStatusInformation
            );
            break;
        }
        goto Finished;
    }

    switch (dwInternetStatus)
    {
    case WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE:
    case WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE:
        hr = LOG_IF_FAILED(OnWinHttpCompletionSendRequestOrWriteComplete(hRequest,
            dwInternetStatus,
            &fClientError,
            &fAnotherCompletionExpected));
        break;

    case WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE:
        hr = LOG_IF_FAILED(OnWinHttpCompletionStatusHeadersAvailable(hRequest,
            &fAnotherCompletionExpected));
        break;

    case WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE:
        hr = LOG_IF_FAILED(OnWinHttpCompletionStatusDataAvailable(hRequest,
            *reinterpret_cast<const DWORD *>(lpvStatusInformation), // dwBytes
            &fAnotherCompletionExpected));
        break;

    case WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
        hr = LOG_IF_FAILED(OnWinHttpCompletionStatusReadComplete(pResponse,
            dwStatusInformationLength,
            &fAnotherCompletionExpected));
        break;

    case WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
        hr = LOG_IF_FAILED(HRESULT_FROM_WIN32(static_cast<const WINHTTP_ASYNC_RESULT *>(lpvStatusInformation)->dwError));
        break;

    case WINHTTP_CALLBACK_STATUS_SENDING_REQUEST:
        //
        // This is a notification, not a completion.  This notifiation happens
        // during the Send Request operation.
        //
        fAnotherCompletionExpected = TRUE;
        break;

    case WINHTTP_CALLBACK_STATUS_REQUEST_SENT:
        //
        // Need to ignore this event.  We get it as a side-effect of registering
        // for WINHTTP_CALLBACK_STATUS_SENDING_REQUEST (which we actually need).
        //
        hr = S_OK;
        fAnotherCompletionExpected = TRUE;
        break;

    case WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING:
        if (ANCMEvents::ANCM_REQUEST_FORWARD_END::IsEnabled(m_pW3Context->GetTraceContext()))
        {
            ANCMEvents::ANCM_REQUEST_FORWARD_END::RaiseEvent(
                m_pW3Context->GetTraceContext(),
                nullptr);
        }
        if (m_RequestStatus != FORWARDER_DONE)
        {
            hr = LOG_IF_FAILED(ERROR_CONNECTION_ABORTED);
            fClientError = m_fClientDisconnected;
        }
        m_hRequest = nullptr;
        fAnotherCompletionExpected = FALSE;
        break;

    case WINHTTP_CALLBACK_STATUS_CONNECTION_CLOSED:
        hr = LOG_IF_FAILED(ERROR_CONNECTION_ABORTED);
        break;

    default:
        //
        // E_UNEXPECTED is rarely used, if seen means that this condition may been occurred.
        //
        DBG_ASSERT(FALSE);
        hr = LOG_IF_FAILED(E_UNEXPECTED);
        if (sm_pTraceLog != nullptr)
        {
            WriteRefTraceLogEx(sm_pTraceLog,
                m_cRefs,
                this,
                "FORWARDING_HANDLER::OnWinHttpCompletionInternal Unexpected WinHTTP Status",
                reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(dwInternetStatus)),
                nullptr);
        }
        break;
    }

    //
    // Handle failure code for switch statement above.
    //
    if (FAILED_LOG(hr))
    {
        goto Failure;
    }

    //
    // WinHTTP completion handled successfully.
    //
    goto Finished;

Failure:

    if (!m_fHasError)
    {
        m_RequestStatus = FORWARDER_DONE;
        m_fHasError = TRUE;

        pResponse->DisableKernelCache();
        pResponse->GetRawHttpResponse()->EntityChunkCount = 0;

        if (hr == HRESULT_FROM_WIN32(ERROR_WINHTTP_INVALID_SERVER_RESPONSE))
        {
            m_fResetConnection = TRUE;
        }

        if (fClientError || m_fClientDisconnected)
        {
            if (!m_fResponseHeadersReceivedAndSet)
            {
                pResponse->SetStatus(400, "Bad Request", 0, HRESULT_FROM_WIN32(WSAECONNRESET));
            }
            else
            {
                //
                // Response headers from origin server were
                // already received and set for the current response.
                // Honor the response status.
                //
            }
        }
        else
        {
            STACK_STRU(strDescription, 128);

            pResponse->SetStatus(502, "Bad Gateway", 3, hr);

            if (!(hr > HRESULT_FROM_WIN32(WINHTTP_ERROR_BASE) &&
                hr <= HRESULT_FROM_WIN32(WINHTTP_ERROR_LAST)) ||
#pragma prefast (suppress : __WARNING_FUNCTION_NEEDS_REVIEW, "Function and parameters reviewed.")
                FormatMessage(
                    FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_HMODULE,
                    g_hWinHttpModule,
                    HRESULT_CODE(hr),
                    0,
                    strDescription.QueryStr(),
                    strDescription.QuerySizeCCH(),
                    nullptr) == 0)
            {
                LoadString(g_hAspNetCoreModule,
                    IDS_SERVER_ERROR,
                    strDescription.QueryStr(),
                    strDescription.QuerySizeCCH());
            }

            strDescription.SyncWithBuffer();
            if (strDescription.QueryCCH() != 0)
            {
                pResponse->SetErrorDescription(
                    strDescription.QueryStr(),
                    strDescription.QueryCCH(),
                    FALSE);
            }
        }
    }

    // FREB log
    if (ANCMEvents::ANCM_REQUEST_FORWARD_FAIL::IsEnabled(m_pW3Context->GetTraceContext()))
    {
        ANCMEvents::ANCM_REQUEST_FORWARD_FAIL::RaiseEvent(
            m_pW3Context->GetTraceContext(),
            nullptr,
            hr);
    }

Finished:
    //
    // Since we use TLS to guard WinHttp operation, call PostCompletion instead of
    // IndicateCompletion to allow cleaning up the TLS before thread reuse.
    // Never post after the request has been finished for whatever reason
    //
    // Only postCompletion after all WinHttp handles (http and websocket) got closed,
    // i.e., received WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING callback for both handles
    // So that no further WinHttp callback will be called
    // Never post completion again after that
    // Otherwise, there will be a AV as the request already passed IIS pipeline
    //
    if (fHandleClosing && dwHandlers == 0)
    {
        //
        // Happy path
        //
        // Marked the request is finished, no more PostCompletion is allowed
        RemoveRequest();
        m_fFinishRequest = TRUE;
        fDoPostCompletion = TRUE;
        if (m_pWebSocket != nullptr)
        {
            m_pWebSocket->Terminate();
            m_pWebSocket = nullptr;
        }
    }
    else if (m_RequestStatus == FORWARDER_DONE)
    {
        //
        // Error path
        //
        RemoveRequest();
        if (m_hRequest != nullptr && !m_fHttpHandleInClose)
        {
            m_fHttpHandleInClose = TRUE;
            WinHttpCloseHandle(m_hRequest);
            m_hRequest = nullptr;
        }

        if (m_pWebSocket != nullptr && !m_fWebSocketHandleInClose)
        {
            m_fWebSocketHandleInClose = TRUE;
            m_pWebSocket->TerminateRequest();
        }

        if (fHandleClosing)
        {
            fDoPostCompletion = dwHandlers == 0;
            m_fFinishRequest = fDoPostCompletion;
        }
    }
    else if (!fAnotherCompletionExpected)
    {
        //
        // Regular async IO operation
        //
        fDoPostCompletion = !m_fFinishRequest;
    }

    //
    // No code should access IIS m_pW3Context after posting the completion.
    //
    if (fDoPostCompletion)
    {
        m_pW3Context->PostCompletion(0);
    }

    if (fExclusiveLocked)
    {
        ReleaseLockExclusive();
    }
    else if (fSharedLocked)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        TlsSetValue(g_dwTlsIndex, nullptr);
        ReleaseSRWLockShared(&m_RequestLock);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == nullptr);
    }

    DereferenceRequestHandler();

}

HRESULT
FORWARDING_HANDLER::OnWinHttpCompletionSendRequestOrWriteComplete(
    HINTERNET                   hRequest,
    DWORD,
    __out BOOL *                pfClientError,
    __out BOOL *                pfAnotherCompletionExpected
)
{
    HRESULT hr = S_OK;
    IHttpRequest *      pRequest = m_pW3Context->GetRequest();

    *pfClientError = FALSE;

    //
    // completion for sending the initial request or request entity to
    // winhttp, get more request entity if available, else start receiving
    // the response
    //
    if (m_BytesToReceive > 0)
    {
        if (m_pEntityBuffer == nullptr)
        {
            FINISHED_IF_NULL_ALLOC(m_pEntityBuffer = GetNewResponseBuffer(
                ENTITY_BUFFER_SIZE));
        }

        if (sm_pTraceLog != nullptr)
        {
            WriteRefTraceLogEx(sm_pTraceLog,
                m_cRefs,
                this,
                "Calling ReadEntityBody",
                nullptr,
                nullptr);
        }
        hr = pRequest->ReadEntityBody(
            m_pEntityBuffer + 6,
            min(m_BytesToReceive, BUFFER_SIZE),
            TRUE,       // fAsync
            nullptr,       // pcbBytesReceived
            nullptr);      // pfCompletionPending
        if (hr == HRESULT_FROM_WIN32(ERROR_HANDLE_EOF))
        {
            DBG_ASSERT(m_BytesToReceive == 0 ||
                m_BytesToReceive == INFINITE);

            //
            // ERROR_HANDLE_EOF is not an error.
            //
            hr = S_OK;

            if (m_BytesToReceive == INFINITE)
            {
                m_BytesToReceive = 0;
                m_cchLastSend = 5;

                //
                // WinHttpWriteData can operate asynchronously.
                //
                // Take reference so that object does not go away as a result of
                // async completion.
                //
                //ReferenceForwardingHandler();
                if (!WinHttpWriteData(m_hRequest,
                    "0\r\n\r\n",
                    5,
                    nullptr))
                {
                    FINISHED(HRESULT_FROM_WIN32(GetLastError()));
                    //DereferenceForwardingHandler();
                    goto Finished;
                }
                *pfAnotherCompletionExpected = TRUE;

                goto Finished;
            }
        }
        else if (FAILED_LOG(hr))
        {
            *pfClientError = TRUE;
            goto Finished;
        }
        else
        {
            //
            // ReadEntityBody will post a completion to IIS.
            //
            *pfAnotherCompletionExpected = TRUE;

            goto Finished;
        }
    }

    m_RequestStatus = FORWARDER_RECEIVING_RESPONSE;

    FINISHED_LAST_ERROR_IF(!WinHttpReceiveResponse(hRequest, nullptr));
    *pfAnotherCompletionExpected = TRUE;

Finished:

    return hr;
}

HRESULT
FORWARDING_HANDLER::OnWinHttpCompletionStatusHeadersAvailable(
    HINTERNET                   hRequest,
    __out BOOL *                pfAnotherCompletionExpected
)
{
    HRESULT       hr = S_OK;
    STACK_BUFFER(bufHeaderBuffer, 2048);
    STACK_STRA(strHeaders, 2048);
    DWORD         dwHeaderSize = bufHeaderBuffer.QuerySize();

    *pfAnotherCompletionExpected = FALSE;

    //
    // Headers are available, read the status line and headers and pass
    // them on to the client
    //
    // WinHttpQueryHeaders operates synchronously,
    // no need for taking reference.
    //
#pragma warning(push)
#pragma warning(disable: 26477) // NULL usage via Windows header
    dwHeaderSize = bufHeaderBuffer.QuerySize();
    if (!WinHttpQueryHeaders(hRequest,
        WINHTTP_QUERY_RAW_HEADERS_CRLF,
        WINHTTP_HEADER_NAME_BY_INDEX,
        bufHeaderBuffer.QueryPtr(),
        &dwHeaderSize,
        WINHTTP_NO_HEADER_INDEX))
    {
        if (!bufHeaderBuffer.Resize(dwHeaderSize))
        {
            FINISHED(E_OUTOFMEMORY);
        }

        //
        // WinHttpQueryHeaders operates synchronously,
        // no need for taking reference.
        //
        FINISHED_LAST_ERROR_IF(!WinHttpQueryHeaders(hRequest,
            WINHTTP_QUERY_RAW_HEADERS_CRLF,
            WINHTTP_HEADER_NAME_BY_INDEX,
            bufHeaderBuffer.QueryPtr(),
            &dwHeaderSize,
            WINHTTP_NO_HEADER_INDEX));
    }
#pragma warning(pop)

    FINISHED_IF_FAILED(strHeaders.CopyW(reinterpret_cast<PWSTR>(bufHeaderBuffer.QueryPtr())));

    // Issue: The reason we add trailing \r\n is to eliminate issues that have been observed
    // in some configurations where status and headers would not have final \r\n nor \r\n\r\n
    // (last header was null terminated).That caused crash within header parsing code that expected valid
    // format. Parsing code was fized to return ERROR_INVALID_PARAMETER, but we still should make
    // Example of a status+header string that was causing problems (note the missing \r\n at the end)
    // HTTP/1.1 302 Moved Permanently\r\n....\r\nLocation:http://site\0
    //

    if (!strHeaders.IsEmpty() && strHeaders.QueryStr()[strHeaders.QueryCCH() - 1] != '\n')
    {
        FINISHED_IF_FAILED(strHeaders.Append("\r\n"));
    }

    FINISHED_IF_FAILED(SetStatusAndHeaders(
        strHeaders.QueryStr(),
        strHeaders.QueryCCH()));

    FreeResponseBuffers();

    //
    // If the request was websocket, and response was 101,
    // trigger a flush, so that IIS's websocket module
    // can get a chance to initialize and complete the handshake.
    //

    if (m_fWebSocketEnabled)
    {
        m_RequestStatus = FORWARDER_RECEIVED_WEBSOCKET_RESPONSE;

        hr = m_pW3Context->GetResponse()->Flush(
            TRUE,
            TRUE,
            nullptr,
            nullptr);

        if (FAILED_LOG(hr))
        {
            *pfAnotherCompletionExpected = FALSE;
        }
        else
        {
            *pfAnotherCompletionExpected = TRUE;
        }
    }

Finished:

    return hr;
}

HRESULT
FORWARDING_HANDLER::OnWinHttpCompletionStatusDataAvailable(
    HINTERNET                   hRequest,
    DWORD                       dwBytes,
    _Out_ BOOL *                pfAnotherCompletionExpected
)
{
    HRESULT hr = S_OK;

    *pfAnotherCompletionExpected = FALSE;

    //
    // Response data is available from winhttp, read it
    //
    if (dwBytes == 0)
    {
        if (m_cContentLength != 0)
        {
            FINISHED(HRESULT_FROM_WIN32(ERROR_WINHTTP_INVALID_SERVER_RESPONSE));
        }

        m_RequestStatus = FORWARDER_DONE;

        goto Finished;
    }

    m_BytesToSend = dwBytes;
    if (m_cContentLength != 0)
    {
        m_cContentLength -= dwBytes;
    }

    m_pEntityBuffer = GetNewResponseBuffer(
        min(m_BytesToSend, BUFFER_SIZE));
    FINISHED_IF_NULL_ALLOC(m_pEntityBuffer);

    //
    // WinHttpReadData can operate asynchronously.
    //
    // Take reference so that object does not go away as a result of
    // async completion.
    //
    //ReferenceForwardingHandler();
    FINISHED_LAST_ERROR_IF(!WinHttpReadData(hRequest,
        m_pEntityBuffer,
        min(m_BytesToSend, BUFFER_SIZE),
        nullptr));

    *pfAnotherCompletionExpected = TRUE;

Finished:

    return hr;
}

HRESULT
FORWARDING_HANDLER::OnWinHttpCompletionStatusReadComplete(
    __in IHttpResponse *        pResponse,
    DWORD                       dwStatusInformationLength,
    __out BOOL *                pfAnotherCompletionExpected
)
{
    HRESULT hr = S_OK;

    *pfAnotherCompletionExpected = FALSE;

    //
    // Response data has been read from winhttp, send it to the client
    //
    m_BytesToSend -= dwStatusInformationLength;

    if (m_cMinBufferLimit >= BUFFER_SIZE / 2)
    {
        if (m_cContentLength != 0)
        {
            m_cContentLength -= dwStatusInformationLength;
        }

        //
        // If we were not using WinHttpQueryDataAvailable and winhttp
        // did not fill our buffer, we must have reached the end of the
        // response
        //
        if (dwStatusInformationLength == 0 ||
            m_BytesToSend != 0)
        {
            if (m_cContentLength != 0)
            {
                FINISHED(HRESULT_FROM_WIN32(ERROR_WINHTTP_INVALID_SERVER_RESPONSE));
            }

            m_RequestStatus = FORWARDER_DONE;
        }
    }
    else
    {
        DBG_ASSERT(dwStatusInformationLength != 0);
    }

    if (dwStatusInformationLength == 0)
    {
        goto Finished;
    }
    else
    {
        m_cBytesBuffered += dwStatusInformationLength;

        HTTP_DATA_CHUNK Chunk;
        Chunk.DataChunkType = HttpDataChunkFromMemory;
        Chunk.FromMemory.pBuffer = m_pEntityBuffer;
        Chunk.FromMemory.BufferLength = dwStatusInformationLength;
        FINISHED_IF_FAILED(pResponse->WriteEntityChunkByReference(&Chunk));
    }

    if (m_cBytesBuffered >= m_cMinBufferLimit)
    {
        //
        // Always post a completion to resume the WinHTTP data pump.
        //
        FINISHED_IF_FAILED(pResponse->Flush(TRUE,     // fAsync
            TRUE,     // fMoreData
            nullptr));    // pcbSent

        *pfAnotherCompletionExpected = TRUE;
    }
    else
    {
        *pfAnotherCompletionExpected = FALSE;
    }

Finished:

    return hr;
}

HRESULT
FORWARDING_HANDLER::OnSendingRequest(
    DWORD                       cbCompletion,
    HRESULT                     hrCompletionStatus,
    __out BOOL *                pfClientError
)
{
    *pfClientError = FALSE;

    //
    // This is a completion for a read from http.sys, abort in case
    // of failure, if we read anything write it out over WinHTTP,
    // but we have already reached EOF, now read the response
    //
    if (hrCompletionStatus == HRESULT_FROM_WIN32(ERROR_HANDLE_EOF))
    {
        DBG_ASSERT(m_BytesToReceive == 0 || m_BytesToReceive == INFINITE);
        if (m_BytesToReceive == INFINITE)
        {
            m_BytesToReceive = 0;
            m_cchLastSend = 5; // "0\r\n\r\n"

            RETURN_LAST_ERROR_IF(!WinHttpWriteData(m_hRequest,
                "0\r\n\r\n",
                5,
                nullptr));
        }
        else
        {
            m_RequestStatus = FORWARDER_RECEIVING_RESPONSE;

            RETURN_LAST_ERROR_IF(!WinHttpReceiveResponse(m_hRequest, nullptr));
        }
    }
    else if (SUCCEEDED(hrCompletionStatus))
    {
        DWORD cbOffset = 0;

        if (m_BytesToReceive != INFINITE)
        {
            m_BytesToReceive -= cbCompletion;
            cbOffset = 6;
        }
        else
        {
            //
            // For chunk-encoded requests, need to re-chunk the entity body
            // Add the CRLF just before and after the chunk data
            //
            m_pEntityBuffer[4] = '\r';
            m_pEntityBuffer[5] = '\n';

            m_pEntityBuffer[cbCompletion + 6] = '\r';
            m_pEntityBuffer[cbCompletion + 7] = '\n';

            if (cbCompletion < 0x10)
            {
                cbOffset = 3;
                m_pEntityBuffer[3] = HEX_TO_ASCII(cbCompletion);
                cbCompletion += 5;
            }
            else if (cbCompletion < 0x100)
            {
                cbOffset = 2;
                m_pEntityBuffer[2] = HEX_TO_ASCII(cbCompletion >> 4);
                m_pEntityBuffer[3] = HEX_TO_ASCII(cbCompletion & 0xf);
                cbCompletion += 6;
            }
            else if (cbCompletion < 0x1000)
            {
                cbOffset = 1;
                m_pEntityBuffer[1] = HEX_TO_ASCII(cbCompletion >> 8);
                m_pEntityBuffer[2] = HEX_TO_ASCII((cbCompletion >> 4) & 0xf);
                m_pEntityBuffer[3] = HEX_TO_ASCII(cbCompletion & 0xf);
                cbCompletion += 7;
            }
            else
            {
                DBG_ASSERT(cbCompletion < 0x10000);

                cbOffset = 0;
                m_pEntityBuffer[0] = HEX_TO_ASCII(cbCompletion >> 12);
                m_pEntityBuffer[1] = HEX_TO_ASCII((cbCompletion >> 8) & 0xf);
                m_pEntityBuffer[2] = HEX_TO_ASCII((cbCompletion >> 4) & 0xf);
                m_pEntityBuffer[3] = HEX_TO_ASCII(cbCompletion & 0xf);
                cbCompletion += 8;
            }
        }
        m_cchLastSend = cbCompletion;

        RETURN_LAST_ERROR_IF(!WinHttpWriteData(m_hRequest,
            m_pEntityBuffer + cbOffset,
            cbCompletion,
            nullptr));
    }
    else
    {
        *pfClientError = TRUE;
        RETURN_HR(hrCompletionStatus);
    }

    return S_OK;
}

HRESULT
FORWARDING_HANDLER::OnReceivingResponse(
)
{
    if (m_cBytesBuffered >= m_cMinBufferLimit)
    {
        FreeResponseBuffers();
    }

    if (m_BytesToSend == 0)
    {
        //
        // If response buffering is enabled, try to read large chunks
        // at a time - also treat very small buffering limit as no
        // buffering
        //
        m_BytesToSend = min(m_cMinBufferLimit, BUFFER_SIZE);
        if (m_BytesToSend < BUFFER_SIZE / 2)
        {
            //
            // Disable buffering.
            //
            m_BytesToSend = 0;
        }
    }

    if (m_BytesToSend == 0)
    {
        //
        // No buffering enabled.
        //
        RETURN_LAST_ERROR_IF(!WinHttpQueryDataAvailable(m_hRequest, nullptr));
    }
    else
    {
        //
        // Buffering enabled.
        //
        if (m_pEntityBuffer == nullptr)
        {
            m_pEntityBuffer = GetNewResponseBuffer(min(m_BytesToSend, BUFFER_SIZE));
            if (m_pEntityBuffer == nullptr)
            {
                RETURN_HR(E_OUTOFMEMORY);
            }
        }

        RETURN_LAST_ERROR_IF(!WinHttpReadData(m_hRequest,
            m_pEntityBuffer,
            min(m_BytesToSend, BUFFER_SIZE),
            nullptr));
    }

    return S_OK;
}

BYTE *
FORWARDING_HANDLER::GetNewResponseBuffer(
    DWORD   dwBufferSize
)
{
    DWORD dwNeededSize = (m_cEntityBuffers + 1) * sizeof(BYTE *);
    if (dwNeededSize > m_buffEntityBuffers.QuerySize() &&
        !m_buffEntityBuffers.Resize(
            max(dwNeededSize, m_buffEntityBuffers.QuerySize() * 2)))
    {
        return nullptr;
    }

    BYTE *pBuffer = (BYTE *)HeapAlloc(GetProcessHeap(),
        0, // dwFlags
        dwBufferSize);
    if (pBuffer == nullptr)
    {
        return nullptr;
    }

    m_buffEntityBuffers.QueryPtr()[m_cEntityBuffers] = pBuffer;
    m_cEntityBuffers++;

    return pBuffer;
}

VOID
FORWARDING_HANDLER::FreeResponseBuffers()
{
    BYTE **pBuffers = m_buffEntityBuffers.QueryPtr();
    for (DWORD i = 0; i<m_cEntityBuffers; i++)
    {
        HeapFree(GetProcessHeap(),
            0, // dwFlags
            pBuffers[i]);
    }
    m_cEntityBuffers = 0;
    m_pEntityBuffer = nullptr;
    m_cBytesBuffered = 0;
}

HRESULT
FORWARDING_HANDLER::SetStatusAndHeaders(
    PCSTR           pszHeaders,
    DWORD
)
{
    IHttpResponse * pResponse = m_pW3Context->GetResponse();
    IHttpRequest *  pRequest = m_pW3Context->GetRequest();
    STACK_STRA(strHeaderName, 128);
    STACK_STRA(strHeaderValue, 2048);
    DWORD           index = 0;
    PSTR            pchNewline = nullptr;
    PCSTR           pchEndofHeaderValue = nullptr;
    BOOL            fServerHeaderPresent = FALSE;

    _ASSERT(pszHeaders != nullptr);

    //
    // The first line is the status line
    //
    PSTR pchStatus = const_cast<PSTR>(strchr(pszHeaders, ' '));
    if (pchStatus == nullptr)
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
    }
    while (*pchStatus == ' ')
    {
        pchStatus++;
    }
    USHORT uStatus = static_cast<USHORT>(atoi(pchStatus));

    if (m_fWebSocketEnabled && uStatus != 101)
    {
        //
        // Expected 101 response.
        //

        m_fWebSocketEnabled = FALSE;
    }

    pchStatus = strchr(pchStatus, ' ');
    if (pchStatus == nullptr)
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
    }
    while (*pchStatus == ' ')
    {
        pchStatus++;
    }
    if (*pchStatus == '\r' || *pchStatus == '\n')
    {
        pchStatus--;
    }

    pchNewline = strchr(pchStatus, '\n');
    if (pchNewline == nullptr)
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
    }

    if (uStatus != 200)
    {
        //
        // Skip over any spaces before the '\n'
        //
        for (pchEndofHeaderValue = pchNewline - 1;
            (pchEndofHeaderValue > pchStatus) &&
            ((*pchEndofHeaderValue == ' ') ||
            (*pchEndofHeaderValue == '\r'));
            pchEndofHeaderValue--)
        {
        }

        //
        // Copy the status description
        //
        RETURN_IF_FAILED(strHeaderValue.Copy(
            pchStatus,
            (DWORD)(pchEndofHeaderValue - pchStatus) + 1));
        RETURN_IF_FAILED(pResponse->SetStatus(uStatus,
                strHeaderValue.QueryStr(),
                0,
                S_OK,
                nullptr,
                TRUE));
    }

    for (index = static_cast<DWORD>(pchNewline - pszHeaders) + 1;
        pszHeaders[index] != '\r' && pszHeaders[index] != '\n' && pszHeaders[index] != '\0';
        index = static_cast<DWORD>(pchNewline - pszHeaders) + 1)
    {
        //
        // Find the ':' in Header : Value\r\n
        //
        PCSTR pchColon = strchr(pszHeaders + index, ':');

        //
        // Find the '\n' in Header : Value\r\n
        //
        pchNewline = const_cast<PSTR>(strchr(pszHeaders + index, '\n'));

        if (pchNewline == nullptr)
        {
            return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
        }

        //
        // Take care of header continuation
        //
        while (pchNewline[1] == ' ' ||
            pchNewline[1] == '\t')
        {
            pchNewline = strchr(pchNewline + 1, '\n');
        }

        DBG_ASSERT(
            (pchColon != nullptr) && (pchColon < pchNewline));
        if ((pchColon == nullptr) || (pchColon >= pchNewline))
        {
            return HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER);
        }

        //
        // Skip over any spaces before the ':'
        //
        PCSTR pchEndofHeaderName;
        for (pchEndofHeaderName = pchColon - 1;
            (pchEndofHeaderName >= pszHeaders + index) &&
            (*pchEndofHeaderName == ' ');
            pchEndofHeaderName--)
        {
        }

        pchEndofHeaderName++;

        //
        // Copy the header name
        //
        RETURN_IF_FAILED(strHeaderName.Copy(
            pszHeaders + index,
            (DWORD)(pchEndofHeaderName - pszHeaders) - index));

        //
        // Skip over the ':' and any trailing spaces
        //
        for (index = static_cast<DWORD>(pchColon - pszHeaders) + 1;
            pszHeaders[index] == ' ';
            index++)
        {
        }

        //
        // Skip over any spaces before the '\n'
        //
        for (pchEndofHeaderValue = pchNewline - 1;
            (pchEndofHeaderValue >= pszHeaders + index) &&
            ((*pchEndofHeaderValue == ' ') ||
            (*pchEndofHeaderValue == '\r'));
            pchEndofHeaderValue--)
        {
        }

        pchEndofHeaderValue++;

        //
        // Copy the header value
        //
        if (pchEndofHeaderValue == pszHeaders + index)
        {
            strHeaderValue.Reset();
        }
        else
        {
            RETURN_IF_FAILED(strHeaderValue.Copy(
                pszHeaders + index,
                (DWORD)(pchEndofHeaderValue - pszHeaders) - index));
        }

        //
        // Do not pass the transfer-encoding:chunked, Connection, Date or
        // Server headers along
        //
        DWORD headerIndex = sm_pResponseHeaderHash->GetIndex(strHeaderName.QueryStr());
        if (headerIndex == UNKNOWN_INDEX)
        {
            RETURN_IF_FAILED(pResponse->SetHeader(strHeaderName.QueryStr(),
                strHeaderValue.QueryStr(),
                static_cast<USHORT>(strHeaderValue.QueryCCH()),
                FALSE)); // fReplace
        }
        else
        {
            switch (headerIndex)
            {
            case HttpHeaderTransferEncoding:
                if (!strHeaderValue.Equals("chunked", TRUE))
                {
                    break;
                }
                __fallthrough;
            case HttpHeaderDate:
                continue;
            case HttpHeaderConnection:
                if (!m_fForwardResponseConnectionHeader)
                {
                    continue;
                }
                break;
            case HttpHeaderServer:
                fServerHeaderPresent = TRUE;
                break;

            case HttpHeaderContentLength:
                if (pRequest->GetRawHttpRequest()->Verb != HttpVerbHEAD)
                {
                    m_cContentLength = _atoi64(strHeaderValue.QueryStr());
                }
                break;
            }

            RETURN_IF_FAILED(pResponse->SetHeader(static_cast<HTTP_HEADER_ID>(headerIndex),
                strHeaderValue.QueryStr(),
                static_cast<USHORT>(strHeaderValue.QueryCCH()),
                TRUE)); // fReplace
        }
    }

    //
    // Explicitly remove the Server header if the back-end didn't set one.
    //

    if (!fServerHeaderPresent)
    {
        pResponse->DeleteHeader("Server");
    }

    if (m_fDoReverseRewriteHeaders)
    {
        RETURN_IF_FAILED(DoReverseRewrite(pResponse));
    }

    m_fResponseHeadersReceivedAndSet = TRUE;

    return S_OK;
}

HRESULT
FORWARDING_HANDLER::DoReverseRewrite(
    _In_ IHttpResponse *pResponse
)
{
    DBG_ASSERT(pResponse == m_pW3Context->GetResponse());
    BOOL fSecure = (m_pW3Context->GetRequest()->GetRawHttpRequest()->pSslInfo != nullptr);
    STRA strTemp;
    PCSTR pszHeader = nullptr;
    PCSTR pszStartHost = nullptr;
    PCSTR pszEndHost = nullptr;
    HTTP_RESPONSE_HEADERS *pHeaders = nullptr;

    //
    // Content-Location and Location are easy, one known header in
    // http[s]://host/url format
    //
    pszHeader = pResponse->GetHeader(HttpHeaderContentLocation);
    if (pszHeader != nullptr)
    {
        if (_strnicmp(pszHeader, "http://", 7) == 0)
        {
            pszStartHost = pszHeader + 7;
        }
        else if (_strnicmp(pszHeader, "https://", 8) == 0)
        {
            pszStartHost = pszHeader + 8;
        }
        else
        {
            goto Location;
        }

        pszEndHost = strchr(pszStartHost, '/');

        RETURN_IF_FAILED(strTemp.Copy(fSecure ? "https://" : "http://"));
        RETURN_IF_FAILED(strTemp.Append(m_pszOriginalHostHeader));

        if (pszEndHost != nullptr)
        {
            RETURN_IF_FAILED(strTemp.Append(pszEndHost));
        }
        RETURN_IF_FAILED(pResponse->SetHeader(HttpHeaderContentLocation,
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE));
    }

Location:

    pszHeader = pResponse->GetHeader(HttpHeaderLocation);
    if (pszHeader != nullptr)
    {
        if (_strnicmp(pszHeader, "http://", 7) == 0)
        {
            pszStartHost = pszHeader + 7;
        }
        else if (_strnicmp(pszHeader, "https://", 8) == 0)
        {
            pszStartHost = pszHeader + 8;
        }
        else
        {
            goto SetCookie;
        }

        pszEndHost = strchr(pszStartHost, '/');

        RETURN_IF_FAILED(strTemp.Copy(fSecure ? "https://" : "http://"));
        RETURN_IF_FAILED(strTemp.Append(m_pszOriginalHostHeader));

        if (pszEndHost != nullptr)
        {
            RETURN_IF_FAILED(strTemp.Append(pszEndHost));
        }
        RETURN_IF_FAILED(pResponse->SetHeader(HttpHeaderLocation,
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE));
    }

SetCookie:

    //
    // Set-Cookie is different - possibly multiple unknown headers with
    // syntax name=value ; ... ; Domain=.host ; ...
    //
    pHeaders = &pResponse->GetRawHttpResponse()->Headers;
    for (DWORD i = 0; i<pHeaders->UnknownHeaderCount; i++)
    {
        if (_stricmp(pHeaders->pUnknownHeaders[i].pName, "Set-Cookie") != 0)
        {
            continue;
        }

        pszHeader = pHeaders->pUnknownHeaders[i].pRawValue;
        pszStartHost = strchr(pszHeader, ';');
        while (pszStartHost != nullptr)
        {
            pszStartHost++;
            while (IsSpace(*pszStartHost))
            {
                pszStartHost++;
            }

            if (_strnicmp(pszStartHost, "Domain", 6) != 0)
            {
                pszStartHost = strchr(pszStartHost, ';');
                continue;
            }
            pszStartHost += 6;

            while (IsSpace(*pszStartHost))
            {
                pszStartHost++;
            }
            if (*pszStartHost != '=')
            {
                break;
            }
            pszStartHost++;
            while (IsSpace(*pszStartHost))
            {
                pszStartHost++;
            }
            if (*pszStartHost == '.')
            {
                pszStartHost++;
            }
            pszEndHost = pszStartHost;
            while (!IsSpace(*pszEndHost) &&
                *pszEndHost != ';' &&
                *pszEndHost != '\0')
            {
                pszEndHost++;
            }

            RETURN_IF_FAILED(strTemp.Copy(pszHeader, static_cast<DWORD>(pszStartHost - pszHeader)));
            RETURN_IF_FAILED(strTemp.Append(m_pszOriginalHostHeader));
            RETURN_IF_FAILED(strTemp.Append(pszEndHost));

            pszHeader = (PCSTR)m_pW3Context->AllocateRequestMemory(strTemp.QueryCCH() + 1);
            if (pszHeader == nullptr)
            {
                RETURN_HR(E_OUTOFMEMORY);
            }
            StringCchCopyA(const_cast<PSTR>(pszHeader), strTemp.QueryCCH() + 1, strTemp.QueryStr());
            pHeaders->pUnknownHeaders[i].pRawValue = pszHeader;
            pHeaders->pUnknownHeaders[i].RawValueLength = static_cast<USHORT>(strTemp.QueryCCH());

            break;
        }
    }

    return S_OK;
}

VOID
FORWARDING_HANDLER::RemoveRequest(
    VOID
)
{
    m_fReactToDisconnect = FALSE;
}

VOID
FORWARDING_HANDLER::NotifyDisconnect()
{
    if (!m_fReactToDisconnect)
    {
        return;
    }

    BOOL fLocked = FALSE;
    if (TlsGetValue(g_dwTlsIndex) != this)
    {
        //
        // Acquire exclusive lock as WinHTTP callback may happen on different thread
        // We don't want two threads signal IIS pipeline simultaneously
        //
        AcquireLockExclusive();
        fLocked = TRUE;
    }

    // Set tls as close winhttp handle will immediately trigger
    // a winhttp callback on the same thread and we donot want to
    // acquire the lock again

    LOG_TRACEF(L"FORWARDING_HANDLER::TerminateRequest %d --%p\n", GetCurrentThreadId(), m_pW3Context);

    if (!m_fHttpHandleInClose)
    {
        m_fClientDisconnected = true;
    }

    if (fLocked)
    {
        ReleaseLockExclusive();
    }
}

_Acquires_exclusive_lock_(this->m_RequestLock)
VOID
FORWARDING_HANDLER::AcquireLockExclusive()
{
    DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == nullptr);
    AcquireSRWLockExclusive(&m_RequestLock);
    TlsSetValue(g_dwTlsIndex, this);
    DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
}

_Releases_exclusive_lock_(this->m_RequestLock)
VOID
FORWARDING_HANDLER::ReleaseLockExclusive()
{
    DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
    TlsSetValue(g_dwTlsIndex, nullptr);
    ReleaseSRWLockExclusive(&m_RequestLock);
    DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == nullptr);
}
