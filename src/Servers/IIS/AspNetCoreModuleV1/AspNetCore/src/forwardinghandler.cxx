// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"
#include <dbgutil.h>

// Just to be aware of the FORWARDING_HANDLER object size.
C_ASSERT(sizeof(FORWARDING_HANDLER) <= 632);

#define DEF_MAX_FORWARDS        32
#define HEX_TO_ASCII(c) ((CHAR)(((c) < 10) ? ((c) + '0') : ((c) + 'a' - 10)))

#define BUFFER_SIZE         (8192UL)
#define ENTITY_BUFFER_SIZE  (6 + BUFFER_SIZE + 2)
#define STR_ANCM_CHILDREQUEST       "ANCM_WasCreateProcessFailure"

HINTERNET                   FORWARDING_HANDLER::sm_hSession = NULL;
STRU                        FORWARDING_HANDLER::sm_strErrorFormat;
HANDLE                      FORWARDING_HANDLER::sm_hEventLog = NULL;
ALLOC_CACHE_HANDLER *       FORWARDING_HANDLER::sm_pAlloc = NULL;
TRACE_LOG *                 FORWARDING_HANDLER::sm_pTraceLog = NULL;
PROTOCOL_CONFIG             FORWARDING_HANDLER::sm_ProtocolConfig;

FORWARDING_HANDLER::FORWARDING_HANDLER(
    __in IHttpContext * pW3Context
) : m_Signature(FORWARDING_HANDLER_SIGNATURE),
m_cRefs(1),
m_dwHandlers(1),
m_pW3Context(pW3Context),
m_hRequest(NULL),
m_fResponseHeadersReceivedAndSet(FALSE),
m_fDoReverseRewriteHeaders(FALSE),
m_msStartTime(0),
m_BytesToReceive(0),
m_BytesToSend(0),
m_pEntityBuffer(NULL),
m_cchLastSend(0),
m_cEntityBuffers(0),
m_cBytesBuffered(0),
m_cMinBufferLimit(0),
m_pszOriginalHostHeader(NULL),
m_RequestStatus(FORWARDER_START),
m_pDisconnect(NULL),
m_pszHeaders(NULL),
m_cchHeaders(0),
m_fWebSocketEnabled(FALSE),
m_cContentLength(0),
m_pWebSocket(NULL),
m_pApplication(NULL),
m_pAppOfflineHtm(NULL),
m_fFinishRequest(FALSE),
m_fClientDisconnected(FALSE),
m_fHasError(FALSE),
m_fServerResetConn(FALSE),
m_fDoneAsyncCompletion(FALSE),
m_fHttpHandleInClose(FALSE),
m_fWebSocketHandleInClose(FALSE)
{
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
        "FORWARDING_HANDLER --%p\n", this);

    InitializeSRWLock(&m_RequestLock);
}

FORWARDING_HANDLER::~FORWARDING_HANDLER(
    VOID
)
{
    //
    // Destructor has started.
    //
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
        "~FORWARDING_HANDLER --%p\n", this);

    m_Signature = FORWARDING_HANDLER_SIGNATURE_FREE;

    //
    // Disconnect notification cleanup would happen first, before
    // the FORWARDING_HANDLER instance got removed from m_pSharedhandler list.
    // The m_pServer cleanup would happen afterwards, since there may be a 
    // call pending from SHARED_HANDLER to  FORWARDING_HANDLER::SetStatusAndHeaders()
    // 
    DBG_ASSERT(m_pDisconnect == NULL);

    FreeResponseBuffers();

    if (m_hRequest != NULL)
    {
        // m_hRequest should have already been closed and set to NULL
        // if not, we cannot close it as it may callback and cause AV
        // let's do our best job here
        WinHttpSetStatusCallback(m_hRequest,
                                 NULL, // Callback Function
                                 WINHTTP_CALLBACK_FLAG_ALL_NOTIFICATIONS,
                                 NULL);
        WinHttpCloseHandle(m_hRequest);
        m_hRequest = NULL;
    }

    if(m_pApplication != NULL)
    {
        m_pApplication->DereferenceApplication();
        m_pApplication = NULL;
    }

    if(m_pAppOfflineHtm != NULL)
    {
        m_pAppOfflineHtm->DereferenceAppOfflineHtm();
        m_pAppOfflineHtm = NULL;
    }

    m_pW3Context = NULL;
}

// static
void * FORWARDING_HANDLER::operator new(size_t)
{
    DBG_ASSERT(sm_pAlloc != NULL);
    if (sm_pAlloc == NULL)
    {
        return NULL;
    }
    return sm_pAlloc->Alloc();
}

// static
void FORWARDING_HANDLER::operator delete(void * pMemory)
{
    DBG_ASSERT(sm_pAlloc != NULL);
    if (sm_pAlloc != NULL)
    {
        sm_pAlloc->Free(pMemory);
    }
}

VOID
FORWARDING_HANDLER::ReferenceForwardingHandler(
    VOID
) const
{
    LONG cRefs = InterlockedIncrement(&m_cRefs);
    if (sm_pTraceLog != NULL)
    {
        WriteRefTraceLog(sm_pTraceLog,
            cRefs,
            this);
    }
}

VOID
FORWARDING_HANDLER::DereferenceForwardingHandler(
    VOID
) const
{
    DBG_ASSERT(m_cRefs != 0);

    LONG cRefs = InterlockedDecrement(&m_cRefs);
    if (cRefs == 0)
    {
        delete this;
    }

    if (sm_pTraceLog != NULL)
    {
        WriteRefTraceLog(sm_pTraceLog,
            cRefs,
            this);
    }
}

HRESULT
FORWARDING_HANDLER::SetStatusAndHeaders(
    PCSTR           pszHeaders,
    DWORD
)
{
    HRESULT         hr;
    IHttpResponse * pResponse = m_pW3Context->GetResponse();
    IHttpRequest *  pRequest = m_pW3Context->GetRequest();
    STACK_STRA(strHeaderName, 128);
    STACK_STRA(strHeaderValue, 2048);
    DWORD           index = 0;
    PSTR            pchNewline;
    PCSTR           pchEndofHeaderValue;
    BOOL            fServerHeaderPresent = FALSE;

    _ASSERT(pszHeaders != NULL);

    //
    // The first line is the status line
    //
    PSTR pchStatus = const_cast<PSTR>(strchr(pszHeaders, ' '));
    if (pchStatus == NULL)
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
    if (pchStatus == NULL)
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
    if (pchNewline == NULL)
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
        {}

        //
        // Copy the status description
        //
        if (FAILED(hr = strHeaderValue.Copy(
            pchStatus,
            (DWORD)(pchEndofHeaderValue - pchStatus) + 1)) ||
            FAILED(hr = pResponse->SetStatus(uStatus,
                strHeaderValue.QueryStr(),
                0,
                S_OK,
                NULL,
                TRUE)))
        {
            return hr;
        }
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

        if (pchNewline == NULL)
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
            (pchColon != NULL) && (pchColon < pchNewline));
        if ((pchColon == NULL) || (pchColon >= pchNewline))
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
        {}

        pchEndofHeaderName++;

        //
        // Copy the header name
        //
        if (FAILED(hr = strHeaderName.Copy(
            pszHeaders + index,
            (DWORD)(pchEndofHeaderName - pszHeaders) - index)))
        {
            return hr;
        }

        //
        // Skip over the ':' and any trailing spaces
        //
        for (index = static_cast<DWORD>(pchColon - pszHeaders) + 1;
            pszHeaders[index] == ' ';
            index++)
        {}


        //
        // Skip over any spaces before the '\n'
        //
        for (pchEndofHeaderValue = pchNewline - 1;
            (pchEndofHeaderValue >= pszHeaders + index) &&
            ((*pchEndofHeaderValue == ' ') ||
            (*pchEndofHeaderValue == '\r'));
            pchEndofHeaderValue--)
        {}

        pchEndofHeaderValue++;

        //
        // Copy the header value
        //
        if (pchEndofHeaderValue == pszHeaders + index)
        {
            strHeaderValue.Reset();
        }
        else if (FAILED(hr = strHeaderValue.Copy(
            pszHeaders + index,
            (DWORD)(pchEndofHeaderValue - pszHeaders) - index)))
        {
            return hr;
        }

        //
        // Do not pass the transfer-encoding:chunked, Connection, Date or
        // Server headers along
        //
        DWORD headerIndex = g_pResponseHeaderHash->GetIndex(strHeaderName.QueryStr());
        if (headerIndex == UNKNOWN_INDEX)
        {
            hr = pResponse->SetHeader(strHeaderName.QueryStr(),
                strHeaderValue.QueryStr(),
                static_cast<USHORT>(strHeaderValue.QueryCCH()),
                FALSE); // fReplace
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
            case HttpHeaderConnection:
            case HttpHeaderDate:
                continue;

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

            hr = pResponse->SetHeader(static_cast<HTTP_HEADER_ID>(headerIndex),
                strHeaderValue.QueryStr(),
                static_cast<USHORT>(strHeaderValue.QueryCCH()),
                TRUE); // fReplace
        }
        if (FAILED(hr))
        {
            return hr;
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
        hr = DoReverseRewrite(pResponse);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    m_fResponseHeadersReceivedAndSet = TRUE;

    return S_OK;
}

HRESULT
FORWARDING_HANDLER::DoReverseRewrite(
    __in IHttpResponse *pResponse
)
{
    DBG_ASSERT(pResponse == m_pW3Context->GetResponse());
    BOOL fSecure = (m_pW3Context->GetRequest()->GetRawHttpRequest()->pSslInfo != NULL);
    STRA strTemp;
    PCSTR pszHeader;
    PCSTR pszStartHost;
    PCSTR pszEndHost;
    HTTP_RESPONSE_HEADERS *pHeaders;
    HRESULT hr;

    //
    // Content-Location and Location are easy, one known header in
    // http[s]://host/url format
    //
    pszHeader = pResponse->GetHeader(HttpHeaderContentLocation);
    if (pszHeader != NULL)
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

        if (FAILED(hr = strTemp.Copy(fSecure ? "https://" : "http://")) ||
            FAILED(hr = strTemp.Append(m_pszOriginalHostHeader)))
        {
            return hr;
        }
        if (pszEndHost != NULL &&
            FAILED(hr = strTemp.Append(pszEndHost)))
        {
            return hr;
        }
        if (FAILED(hr = pResponse->SetHeader(HttpHeaderContentLocation,
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE)))
        {
            return hr;
        }
    }

Location:

    pszHeader = pResponse->GetHeader(HttpHeaderLocation);
    if (pszHeader != NULL)
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

        if (FAILED(hr = strTemp.Copy(fSecure ? "https://" : "http://")) ||
            FAILED(hr = strTemp.Append(m_pszOriginalHostHeader)))
        {
            return hr;
        }
        if (pszEndHost != NULL &&
            FAILED(hr = strTemp.Append(pszEndHost)))
        {
            return hr;
        }
        if (FAILED(hr = pResponse->SetHeader(HttpHeaderLocation,
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE)))
        {
            return hr;
        }
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
        while (pszStartHost != NULL)
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

            if (FAILED(hr = strTemp.Copy(pszHeader, static_cast<DWORD>(pszStartHost - pszHeader))) ||
                FAILED(hr = strTemp.Append(m_pszOriginalHostHeader)) ||
                FAILED(hr = strTemp.Append(pszEndHost)))
            {
                return hr;
            }

            pszHeader = (PCSTR)m_pW3Context->AllocateRequestMemory(strTemp.QueryCCH() + 1);
            if (pszHeader == NULL)
            {
                return E_OUTOFMEMORY;
            }
            StringCchCopyA(const_cast<PSTR>(pszHeader), strTemp.QueryCCH() + 1, strTemp.QueryStr());
            pHeaders->pUnknownHeaders[i].pRawValue = pszHeader;
            pHeaders->pUnknownHeaders[i].RawValueLength = static_cast<USHORT>(strTemp.QueryCCH());

            break;
        }
    }

    return S_OK;
}

HRESULT
FORWARDING_HANDLER::GetHeaders(
    const PROTOCOL_CONFIG * pProtocol,
    PCWSTR *                ppszHeaders,
    DWORD *                 pcchHeaders,
    ASPNETCORE_CONFIG*      pAspNetCoreConfig,
    SERVER_PROCESS*         pServerProcess
)
{

    HRESULT hr = S_OK;
    PCSTR pszCurrentHeader;
    PCSTR ppHeadersToBeRemoved;
    PCSTR pszFinalHeader;
    USHORT cchCurrentHeader;
    DWORD cchFinalHeader;
    BOOL  fSecure = FALSE;  // dummy. Used in SplitUrl. Value will not be used 
                            // as ANCM always use http protocol to communicate with backend  
    STRU  struDestination;
    STRU  struUrl;
    STACK_STRA(strTemp, 64);
    HTTP_REQUEST_HEADERS *pHeaders;
    IHttpRequest *pRequest = m_pW3Context->GetRequest();
    MULTISZA mszMsAspNetCoreHeaders;

    //
    // We historically set the host section in request url to the new host header
    // this is wrong but Kestrel has dependency on it.
    // should change it in the future
    //
    if (!pProtocol->QueryPreserveHostHeader())
    {
        if (FAILED(hr = PATH::SplitUrl(pRequest->GetRawHttpRequest()->CookedUrl.pFullUrl,
            &fSecure,
            &struDestination,
            &struUrl)) ||
            FAILED(hr = strTemp.CopyW(struDestination.QueryStr())) ||
            FAILED(hr = pRequest->SetHeader(HttpHeaderHost,
                strTemp.QueryStr(),
                static_cast<USHORT>(strTemp.QueryCCH()),
                TRUE))) // fReplace
        {
            return hr;
        }
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

    while (ppHeadersToBeRemoved != NULL)
    {
        m_pW3Context->GetRequest()->DeleteHeader(ppHeadersToBeRemoved);
        ppHeadersToBeRemoved = mszMsAspNetCoreHeaders.Next(ppHeadersToBeRemoved);
    }

    if (pServerProcess->QueryGuid() != NULL)
    {
        hr = m_pW3Context->GetRequest()->SetHeader("MS-ASPNETCORE-TOKEN",
            pServerProcess->QueryGuid(),
            (USHORT)strlen(pServerProcess->QueryGuid()),
            TRUE);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    if (pAspNetCoreConfig->QueryForwardWindowsAuthToken() &&
        (_wcsicmp(m_pW3Context->GetUser()->GetAuthenticationType(), L"negotiate") == 0 ||
            _wcsicmp(m_pW3Context->GetUser()->GetAuthenticationType(), L"ntlm") == 0))
    {
        if (m_pW3Context->GetUser()->GetPrimaryToken() != NULL &&
            m_pW3Context->GetUser()->GetPrimaryToken() != INVALID_HANDLE_VALUE)
        {
            HANDLE hTargetTokenHandle = NULL;
            hr = pServerProcess->SetWindowsAuthToken(m_pW3Context->GetUser()->GetPrimaryToken(),
                &hTargetTokenHandle);
            if (FAILED(hr))
            {
                return hr;
            }

            //
            // set request header with target token value
            //
            CHAR pszHandleStr[16] = { 0 };
            if (_ui64toa_s((UINT64)hTargetTokenHandle, pszHandleStr, 16, 16) != 0)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                return hr;
            }

            hr = m_pW3Context->GetRequest()->SetHeader("MS-ASPNETCORE-WINAUTHTOKEN",
                pszHandleStr,
                (USHORT)strlen(pszHandleStr),
                TRUE);
            if (FAILED(hr))
            {
                return hr;
            }
        }
    }

    if (!pProtocol->QueryXForwardedForName()->IsEmpty())
    {
        strTemp.Reset();

        pszCurrentHeader = pRequest->GetHeader(pProtocol->QueryXForwardedForName()->QueryStr(), &cchCurrentHeader);
        if (pszCurrentHeader != NULL)
        {
            if (FAILED(hr = strTemp.Copy(pszCurrentHeader, cchCurrentHeader)) ||
                FAILED(hr = strTemp.Append(", ", 2)))
            {
                return hr;
            }
        }

        if (FAILED(hr = m_pW3Context->GetServerVariable("REMOTE_ADDR",
            &pszFinalHeader,
            &cchFinalHeader)))
        {
            return hr;
        }

        if (pRequest->GetRawHttpRequest()->Address.pRemoteAddress->sa_family == AF_INET6)
        {
            if (FAILED(hr = strTemp.Append("[", 1)) ||
                FAILED(hr = strTemp.Append(pszFinalHeader, cchFinalHeader)) ||
                FAILED(hr = strTemp.Append("]", 1)))
            {
                return hr;
            }
        }
        else
        {
            if (FAILED(hr = strTemp.Append(pszFinalHeader, cchFinalHeader)))
            {
                return hr;
            }
        }

        if (pProtocol->QueryIncludePortInXForwardedFor())
        {
            if (FAILED(hr = m_pW3Context->GetServerVariable("REMOTE_PORT",
                &pszFinalHeader,
                &cchFinalHeader)))
            {
                return hr;
            }

            if (FAILED(hr = strTemp.Append(":", 1)) ||
                FAILED(hr = strTemp.Append(pszFinalHeader, cchFinalHeader)))
            {
                return hr;
            }
        }

        if (FAILED(hr = pRequest->SetHeader(pProtocol->QueryXForwardedForName()->QueryStr(),
            strTemp.QueryStr(),
            static_cast<USHORT>(strTemp.QueryCCH()),
            TRUE))) // fReplace
        {
            return hr;
        }
    }

    if (!pProtocol->QuerySslHeaderName()->IsEmpty())
    {
        const HTTP_SSL_INFO *pSslInfo = pRequest->GetRawHttpRequest()->pSslInfo;
        LPSTR pszScheme = "http";
        if (pSslInfo != NULL)
        {
            pszScheme = "https";
        }

        strTemp.Reset();

        pszCurrentHeader = pRequest->GetHeader(pProtocol->QuerySslHeaderName()->QueryStr(), &cchCurrentHeader);
        if (pszCurrentHeader != NULL)
        {
            if (FAILED(hr = strTemp.Copy(pszCurrentHeader, cchCurrentHeader)) ||
                FAILED(hr = strTemp.Append(", ", 2)))
            {
                return hr;
            }
        }

        if (FAILED(hr = strTemp.Append(pszScheme)))
        {
            return hr;
        }

        if (FAILED(pRequest->SetHeader(pProtocol->QuerySslHeaderName()->QueryStr(),
            strTemp.QueryStr(),
            (USHORT)strTemp.QueryCCH(),
            TRUE)))
        {
            return hr;
        }
    }

    if (!pProtocol->QueryClientCertName()->IsEmpty())
    {
        if (pRequest->GetRawHttpRequest()->pSslInfo == NULL ||
            pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo == NULL)
        {
            pRequest->DeleteHeader(pProtocol->QueryClientCertName()->QueryStr());
        }
        else
        {
            // Resize the buffer large enough to hold the encoded certificate info
            if (FAILED(hr = strTemp.Resize(
                1 + (pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo->CertEncodedSize + 2) / 3 * 4)))
            {
                return hr;
            }

            Base64Encode(
                pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo->pCertEncoded,
                pRequest->GetRawHttpRequest()->pSslInfo->pClientCertInfo->CertEncodedSize,
                strTemp.QueryStr(),
                strTemp.QuerySize(),
                NULL);
            strTemp.SyncWithBuffer();

            if (FAILED(hr = pRequest->SetHeader(
                pProtocol->QueryClientCertName()->QueryStr(),
                strTemp.QueryStr(),
                static_cast<USHORT>(strTemp.QueryCCH()),
                TRUE))) // fReplace
            {
                return hr;
            }
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
    hr = m_pW3Context->GetServerVariable("ALL_RAW",
        ppszHeaders,
        pcchHeaders);
    if (FAILED(hr))
    {
        return hr;
    }

    return S_OK;
}

HRESULT
FORWARDING_HANDLER::CreateWinHttpRequest(
    __in const IHttpRequest *       pRequest,
    __in const PROTOCOL_CONFIG *    pProtocol,
    __in HINTERNET                  hConnect,
    __inout STRU *                  pstrUrl,
    ASPNETCORE_CONFIG*              pAspNetCoreConfig,
    SERVER_PROCESS*                 pServerProcess
)
{
    HRESULT         hr = S_OK;
    PCWSTR          pszVersion = NULL;
    PCSTR           pszVerb;
    DWORD           dwTimeout = INFINITE;
    STACK_STRU(strVerb, 32);

    //
    // Create the request handle for this request (leave some fields blank,
    // we will fill them when sending the request)
    //
    pszVerb = pRequest->GetHttpMethod();
    if (FAILED(hr = strVerb.CopyA(pszVerb)))
    {
        goto Finished;
    }

    //pszVersion = pProtocol->QueryVersion();
    if (pszVersion == NULL)
    {
        DWORD cchUnused;
        hr = m_pW3Context->GetServerVariable(
            "HTTP_VERSION",
            &pszVersion,
            &cchUnused);
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    m_hRequest = WinHttpOpenRequest(hConnect,
        strVerb.QueryStr(),
        pstrUrl->QueryStr(),
        pszVersion,
        WINHTTP_NO_REFERER,
        WINHTTP_DEFAULT_ACCEPT_TYPES,
        WINHTTP_FLAG_ESCAPE_DISABLE_QUERY
        | g_OptionalWinHttpFlags);
    if (m_hRequest == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (!pServerProcess->IsDebuggerAttached())
    {
        dwTimeout = pProtocol->QueryTimeout();
    }

    if (!WinHttpSetTimeouts(m_hRequest,
                            dwTimeout, //resolve timeout
                            dwTimeout, // connect timeout
                            dwTimeout, // send timeout
                            dwTimeout)) // receive timeout
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    DWORD dwResponseBufferLimit = pProtocol->QueryResponseBufferLimit();
    if (!WinHttpSetOption(m_hRequest,
        WINHTTP_OPTION_MAX_RESPONSE_DRAIN_SIZE,
        &dwResponseBufferLimit,
        sizeof(dwResponseBufferLimit)))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    DWORD dwMaxHeaderSize = pProtocol->QueryMaxResponseHeaderSize();
    if (!WinHttpSetOption(m_hRequest,
        WINHTTP_OPTION_MAX_RESPONSE_HEADER_SIZE,
        &dwMaxHeaderSize,
        sizeof(dwMaxHeaderSize)))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    DWORD dwOption = WINHTTP_DISABLE_COOKIES;

    dwOption |= WINHTTP_DISABLE_AUTHENTICATION;

    if (!pProtocol->QueryDoKeepAlive())
    {
        dwOption |= WINHTTP_DISABLE_KEEP_ALIVE;
    }
    if (!WinHttpSetOption(m_hRequest,
        WINHTTP_OPTION_DISABLE_FEATURE,
        &dwOption,
        sizeof(dwOption)))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (WinHttpSetStatusCallback(m_hRequest,
        FORWARDING_HANDLER::OnWinHttpCompletion,
        (WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
            WINHTTP_CALLBACK_FLAG_HANDLES |
            WINHTTP_CALLBACK_STATUS_SENDING_REQUEST),
        NULL) == WINHTTP_INVALID_STATUS_CALLBACK)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    hr = GetHeaders(pProtocol,
        &m_pszHeaders,
        &m_cchHeaders,
        pAspNetCoreConfig,
        pServerProcess);
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:

    return hr;
}

REQUEST_NOTIFICATION_STATUS
FORWARDING_HANDLER::OnExecuteRequestHandler(
    VOID
)
{
    REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;
    HRESULT                     hr = S_OK;
    ASPNETCORE_CONFIG          *pAspNetCoreConfig = NULL;
    FORWARDER_CONNECTION       *pConnection = NULL;
    STACK_STRU(strDestination, 32);
    STACK_STRU(strUrl, 2048);
    STACK_STRU(struEscapedUrl, 2048);
    STACK_STRU(strDescription, 128);
    HINTERNET                   hConnect = NULL;
    IHttpRequest               *pRequest = m_pW3Context->GetRequest();
    IHttpResponse              *pResponse = m_pW3Context->GetResponse();
    PROTOCOL_CONFIG            *pProtocol = &sm_ProtocolConfig;
    APPLICATION_MANAGER        *pApplicationManager = NULL;
    SERVER_PROCESS             *pServerProcess = NULL;
    USHORT                      cchHostName = 0;
    BOOL                        fSecure = FALSE;
    BOOL                        fProcessStartFailure = FALSE;
    HTTP_DATA_CHUNK            *pDataChunk = NULL;
    IHttpConnection            *pClientConnection = NULL;

    DBG_ASSERT(m_RequestStatus == FORWARDER_START);

    //
    // Take a reference so that object does not go away as a result of
    // async completion.
    //
    ReferenceForwardingHandler();

    m_pszOriginalHostHeader = pRequest->GetHeader(HttpHeaderHost, &cchHostName);

    // read per site aspNetCore configuration.
    hr = ASPNETCORE_CONFIG::GetConfig(m_pW3Context, &pAspNetCoreConfig);
    if (FAILED(hr))
    {
        // configuration error.
        goto Failure;
    }

    // override Protocol related config from aspNetCore config
    pProtocol->OverrideConfig(pAspNetCoreConfig);

    //
    // parse original url
    //
    if (FAILED(hr = PATH::SplitUrl(pRequest->GetRawHttpRequest()->CookedUrl.pFullUrl,
        &fSecure,
        &strDestination,
        &strUrl)))
    {
        goto Failure;
    }

    if (FAILED(hr = PATH::EscapeAbsPath(pRequest, &struEscapedUrl)))
    {
        goto Failure;
    }

    m_fDoReverseRewriteHeaders = pProtocol->QueryReverseRewriteHeaders();

    m_cMinBufferLimit = pProtocol->QueryMinResponseBuffer();

    pClientConnection = m_pW3Context->GetConnection();
    if (pClientConnection == NULL ||
        !pClientConnection->IsConnected())
    {
        hr = HRESULT_FROM_WIN32(WSAECONNRESET);
        goto Failure;
    }

    //
    // Find the application that is supposed to service this request.
    //
    pApplicationManager = APPLICATION_MANAGER::GetInstance();
    if (pApplicationManager == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }

    hr = pApplicationManager->GetApplication(m_pW3Context,
        &m_pApplication);
    if (FAILED(hr))
    {
        goto Failure;
    }

    m_pAppOfflineHtm = m_pApplication->QueryAppOfflineHtm();
    if (m_pAppOfflineHtm != NULL)
    {
        m_pAppOfflineHtm->ReferenceAppOfflineHtm();
    }

    if (m_pApplication->AppOfflineFound() && m_pAppOfflineHtm != NULL)
    {
        HTTP_DATA_CHUNK DataChunk;

        DataChunk.DataChunkType = HttpDataChunkFromMemory;
        DataChunk.FromMemory.pBuffer = (PVOID)m_pAppOfflineHtm->m_Contents.QueryStr();
        DataChunk.FromMemory.BufferLength = m_pAppOfflineHtm->m_Contents.QueryCB();

        pResponse->SetStatus(503, "Service Unavailable", 0, hr, NULL, TRUE);
        pResponse->SetHeader("Content-Type",
            "text/html",
            (USHORT)strlen("text/html"),
            FALSE
        ); // no need to check return hresult

        pResponse->WriteEntityChunkByReference(&DataChunk);
        goto Finished;
    }

    hr = m_pApplication->GetProcess(m_pW3Context,
        pAspNetCoreConfig,
        &pServerProcess);
    if (FAILED(hr))
    {
        fProcessStartFailure = TRUE;
        goto Failure;
    }

    if (pServerProcess == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_CREATE_FAILED);
        goto Failure;
    }

    if (pServerProcess->QueryWinHttpConnection() == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE);
        goto Failure;
    }

    hConnect = pServerProcess->QueryWinHttpConnection()->QueryHandle();

    //
    // Mark request as websocket if upgrade header is present.
    //

    if (g_fWebSocketSupported)
    {
        USHORT cchHeader = 0;
        PCSTR pszWebSocketHeader = pRequest->GetHeader("Upgrade", &cchHeader);

        if (cchHeader == 9 && _stricmp(pszWebSocketHeader, "websocket") == 0)
        {
            m_fWebSocketEnabled = TRUE;
        }
    }

    hr = CreateWinHttpRequest(pRequest,
        pProtocol,
        hConnect,
        &struEscapedUrl,
        pAspNetCoreConfig,
        pServerProcess);

    if (FAILED(hr))
    {
        goto Failure;
    }

    //
    // Register for connection disconnect notification with http.sys.
    //
    if (g_fAsyncDisconnectAvailable)
    {
        m_pDisconnect = static_cast<ASYNC_DISCONNECT_CONTEXT *>(
            pClientConnection->GetModuleContextContainer()->
                GetConnectionModuleContext(g_pModuleId));
        if (m_pDisconnect == NULL)
        {
            m_pDisconnect = new ASYNC_DISCONNECT_CONTEXT;
            if (m_pDisconnect == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Failure;
            }

            hr = pClientConnection->GetModuleContextContainer()->
                    SetConnectionModuleContext(m_pDisconnect,
                                               g_pModuleId);
            DBG_ASSERT(hr != HRESULT_FROM_WIN32(ERROR_ALREADY_ASSIGNED));
            if (FAILED(hr))
            {
                m_pDisconnect->CleanupStoredContext();
                m_pDisconnect = NULL;
                goto Failure;
            }
        }

        //
        // Issue: There is a window of opportunity to miss on the disconnect
        // notification if it happens before the SetHandler() call is made.
        // It is suboptimal for performance, but should functionally be OK.
        //

        m_pDisconnect->SetHandler(this);
    }

    if (m_hRequest == NULL)
    {
        hr = HRESULT_FROM_WIN32(WSAECONNRESET);
        goto Failure;
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
    if (pszContentLength != NULL)
    {
        cbContentLength = m_BytesToReceive = atol(pszContentLength);
        if (m_BytesToReceive == INFINITE)
        {
            hr = HRESULT_FROM_WIN32(WSAECONNRESET);
            goto Failure;
        }
    }
    else if (pRequest->GetHeader(HttpHeaderTransferEncoding) != NULL)
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
            NULL,
            0))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
    }

    m_cchLastSend = m_cchHeaders;

    if (!WinHttpSendRequest(m_hRequest,
        m_pszHeaders,
        m_cchHeaders,
        NULL,
        0,
        cbContentLength,
        reinterpret_cast<DWORD_PTR>(static_cast<PVOID>(this))))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
            "FORWARDING_HANDLER::OnExecuteRequestHandler, Send request failed");
        goto Failure;
    }

    //
    // Async WinHTTP operation is in progress. Release this thread meanwhile,
    // OnWinHttpCompletion method should resume the work by posting an IIS completion.
    //
    retVal = RQ_NOTIFICATION_PENDING;
    goto Finished;

Failure:

    //
    // Reset status for consistency.
    //
    m_RequestStatus = FORWARDER_DONE;
    m_fHasError = TRUE;
    pResponse->DisableKernelCache();
    pResponse->GetRawHttpResponse()->EntityChunkCount = 0;
    //
    // Finish the request on failure.
    //
    retVal = RQ_NOTIFICATION_FINISH_REQUEST;

    if (hr == HRESULT_FROM_WIN32(WSAECONNRESET))
    {
        pResponse->SetStatus(400, "Bad Request", 0, hr);
        goto Finished;
    }
    else if (fProcessStartFailure && !pAspNetCoreConfig->QueryDisableStartUpErrorPage())
    {
        pResponse->SetStatus(502, "Bad Gateway", 5, hr, NULL, TRUE);
        pResponse->SetHeader("Content-Type",
            "text/html",
            (USHORT)strlen("text/html"),
            FALSE
        );

        if (SUCCEEDED(pApplicationManager->Get502ErrorPage(&pDataChunk)))
        {
            pResponse->WriteEntityChunkByReference(pDataChunk);
            goto Finished;
        }
    }

    //
    // default error behavior
    //
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
            NULL);
    }
    else
    {
        LoadString(g_hModule,
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

Finished:

    if (pConnection != NULL)
    {
        pConnection->DereferenceForwarderConnection();
        pConnection = NULL;
    }

    if (pServerProcess != NULL)
    {
        pServerProcess->DereferenceServerProcess();
        pServerProcess = NULL;
    }

    if (retVal != RQ_NOTIFICATION_PENDING)
    {
        RemoveRequest();
    }

    DereferenceForwardingHandler();
    //
    // Do not use this object after dereferencing it, it may be gone.
    //

    return retVal;
}

VOID
FORWARDING_HANDLER::RemoveRequest()
{
    ASYNC_DISCONNECT_CONTEXT *          pDisconnect;
    pDisconnect = (ASYNC_DISCONNECT_CONTEXT *) InterlockedExchangePointer((PVOID*)&m_pDisconnect, NULL);
    if (pDisconnect != NULL)
    {
        pDisconnect->ResetHandler();
        pDisconnect = NULL;
    }
}

REQUEST_NOTIFICATION_STATUS
FORWARDING_HANDLER::OnAsyncCompletion(
    DWORD                   cbCompletion,
    HRESULT                 hrCompletionStatus
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
    BOOL                        fWebsocketUpgraded = FALSE;

#ifdef DEBUG
    FORWARDING_REQUEST_STATUS   dwLocalStatus = m_RequestStatus;
#endif // DEBUG

    if (sm_pTraceLog != NULL)
    {
        WriteRefTraceLogEx(sm_pTraceLog,
            m_cRefs,
            this,
            "FORWARDING_HANDLER::OnAsyncCompletion Enter",
            reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(cbCompletion)),
            reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(hrCompletionStatus)));
    }

    DBG_ASSERT(m_pW3Context != NULL);
    __analysis_assume(m_pW3Context != NULL);

    //
    // Take a reference so that object does not go away as a result of
    // async completion.
    //
    ReferenceForwardingHandler();
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
        "FORWARDING_HANDLER::OnAsyncCompletion");

    //
    // OnAsyncCompletion can be called on a Winhttp io completion thread.
    // Hence we need to check the TLS before we acquire the shared lock.
    //
    if (TlsGetValue(g_dwTlsIndex) != this)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == NULL);
        AcquireSRWLockExclusive(&m_RequestLock);
        TlsSetValue(g_dwTlsIndex, this);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);

        fLocked = TRUE;
    }

    if (m_fClientDisconnected && (m_RequestStatus != FORWARDER_DONE) )
    {
        hr = ERROR_CONNECTION_ABORTED;
        goto Failure;
    }

    if (m_RequestStatus == FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
    {
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
            "FORWARDING_HANDLER::OnAsyncCompletion, Send completed for 101 response");
        //
        // This should be the write completion of the 101 response.
        //
        m_pWebSocket = new WEBSOCKET_HANDLER();
        if (m_pWebSocket == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Failure;
        }

        hr = m_pWebSocket->ProcessRequest(this, m_pW3Context, m_hRequest, &fWebsocketUpgraded);
        if (fWebsocketUpgraded)
        {
            // WinHttp WebSocket handle has been created, bump the counter so that remember to close it
            // and prevent from premature postcomplation and unexpected callback from winhttp
            InterlockedIncrement(&m_dwHandlers);
        }

        if (FAILED(hr))
        {
            // This failure could happen when client disconnect happens or backend server fails 
            // after websocket upgrade
            goto Failure;
        }

        //
        // WebSocket upgrade is successful. Close the WinHttpRequest Handle
        //
        m_fHttpHandleInClose = TRUE;
        fClosed = WinHttpCloseHandle(m_hRequest);
        DBG_ASSERT(fClosed);
        m_hRequest = NULL;

        if (!fClosed)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Failure;
        }
        retVal = RQ_NOTIFICATION_PENDING;
        goto Finished;
    }

    //
    // Begins normal completion handling. There is already a shared acquired
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
        if (FAILED(hrCompletionStatus))
        {
            hr = hrCompletionStatus;
            fClientError = TRUE;
            goto Failure;
        }

        hr = OnReceivingResponse();
        if (FAILED(hr))
        {
            goto Failure;
        }
        break;

    case FORWARDER_SENDING_REQUEST:

        hr = OnSendingRequest(cbCompletion,
            hrCompletionStatus,
            &fClientError);
        if (FAILED(hr))
        {
            goto Failure;
        }
        break;

    default:
        DBG_ASSERT(m_RequestStatus == FORWARDER_DONE);
        if (m_hRequest == NULL && m_pWebSocket == NULL)
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
                    NULL);
            }
            else
            {
                LoadString(g_hModule,
                    IDS_SERVER_ERROR,
                    strDescription.QueryStr(),
                    strDescription.QuerySizeCCH());
            }
            (VOID)strDescription.SyncWithBuffer();

#ifdef DEBUG
            // adding more info to error descrption to help us diagnose the issue
            STACK_STRA(strMoreData, 128);
            sprintf_s(strMoreData.QueryStr(), 100, "OnAsyncCompletion --%p--%p--%d--%d--%d--%d\n", this, m_pW3Context, GetCurrentThreadId(), (UINT)dwLocalStatus, m_fServerResetConn, m_fClientDisconnected);
            strMoreData.SyncWithBuffer();
            strDescription.AppendA(strMoreData.QueryStr());
#endif // DEBUG

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

    if (m_pWebSocket != NULL && !m_fWebSocketHandleInClose)
    {
        m_fWebSocketHandleInClose = TRUE;
        m_pWebSocket->TerminateRequest();
    }

    if (m_hRequest != NULL && !m_fHttpHandleInClose)
    {
        m_fHttpHandleInClose = TRUE;
        WinHttpCloseHandle(m_hRequest);
        m_hRequest = NULL;
    }

Finished:

    if (fLocked)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        TlsSetValue(g_dwTlsIndex, NULL);
        ReleaseSRWLockExclusive(&m_RequestLock);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == NULL);
    }

    if (retVal != RQ_NOTIFICATION_PENDING)
    {
#ifdef DEBUG
        DWORD counter = InterlockedIncrement(&g_dwLogCounter) % ASPNETCORE_DEBUG_STRU_ARRAY_SIZE;
        g_strLogs[counter].Reset();
        sprintf_s(g_strLogs[counter].QueryStr(), ASPNETCORE_DEBUG_STRU_BUFFER_SIZE,
            "OnAyncCompletion--%p--%d--%d--%d--%d\n", this, GetCurrentThreadId(), (UINT)dwLocalStatus, retVal, m_fDoneAsyncCompletion);
        g_strLogs[counter].SyncWithBuffer();
#endif // DEBUG

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

    DereferenceForwardingHandler();
    //
    // Do not use this object after dereferencing it, it may be gone.
    //
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
        "FORWARDING_HANDLER::OnAsyncCompletion Done %d", retVal);
    return retVal;
}

HRESULT
FORWARDING_HANDLER::OnSendingRequest(
    DWORD                       cbCompletion,
    HRESULT                     hrCompletionStatus,
    __out BOOL *                pfClientError
)
{
    HRESULT hr = S_OK;

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

                               //
                               // WinHttpWriteData can operate asynchronously.
                               //
                               // Take reference so that object does not go away as a result of
                               // async completion.
                               //
            if (!WinHttpWriteData(m_hRequest,
                "0\r\n\r\n",
                5,
                NULL))
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto Failure;
            }
        }
        else
        {
            m_RequestStatus = FORWARDER_RECEIVING_RESPONSE;

            //
            // WinHttpReceiveResponse can operate asynchronously.
            //
            // Take reference so that object does not go away as a result of
            // async completion.
            //
            if (!WinHttpReceiveResponse(m_hRequest, NULL))
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto Failure;
            }
        }
    }
    else if (SUCCEEDED(hrCompletionStatus))
    {
        DWORD cbOffset;

        if (m_BytesToReceive != INFINITE)
        {
            m_BytesToReceive -= cbCompletion;
            cbOffset = 6;
        }
        else
        {
            //
            // For chunk-encoded requests, need to re-chunk the
            // entity body
            //
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

        //
        // WinHttpWriteData can operate asynchronously.
        //
        // Take reference so that object does not go away as a result of
        // async completion.
        //
        if (!WinHttpWriteData(m_hRequest,
            m_pEntityBuffer + cbOffset,
            cbCompletion,
            NULL))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Failure;
        }
    }
    else
    {
        hr = hrCompletionStatus;
        *pfClientError = TRUE;
        goto Failure;
    }

Failure:

    return hr;
}

HRESULT
FORWARDING_HANDLER::OnReceivingResponse(
)
{
    HRESULT hr = S_OK;

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
        // WinHttpQueryDataAvailable can operate asynchronously.
        //
        if (!WinHttpQueryDataAvailable(m_hRequest, NULL))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Failure;
        }
    }
    else
    {
        //
        // Buffering enabled.
        //

        if (m_pEntityBuffer == NULL)
        {
            m_pEntityBuffer = GetNewResponseBuffer(min(m_BytesToSend, BUFFER_SIZE));
            if (m_pEntityBuffer == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Failure;
            }
        }

        //
        // WinHttpReadData can operate asynchronously.
        //
        if (!WinHttpReadData(m_hRequest,
            m_pEntityBuffer,
            min(m_BytesToSend, BUFFER_SIZE),
            NULL))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Failure;
        }
    }

Failure:

    return hr;
}

VOID
FORWARDING_HANDLER::OnWinHttpCompletionInternal(
    HINTERNET   hRequest,
    DWORD       dwInternetStatus,
    LPVOID      lpvStatusInformation,
    DWORD       dwStatusInformationLength
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
    BOOL fExclusiveLock = FALSE;
    BOOL fSharedLock = FALSE;
    BOOL fDoPostCompletion = FALSE;
    BOOL fClientError = FALSE;
    BOOL fAnotherCompletionExpected = FALSE;
    DWORD dwHandlers = 1; // defaullt for http handler

    DBG_ASSERT(m_pW3Context != NULL);
    __analysis_assume(m_pW3Context != NULL);
    IHttpResponse * pResponse = m_pW3Context->GetResponse();
    BOOL fEndRequest = FALSE;

    // Reference the request handler to prevent it from being released prematurely
    ReferenceForwardingHandler();

    UNREFERENCED_PARAMETER(dwStatusInformationLength);

    if (sm_pTraceLog != NULL)
    {
        WriteRefTraceLogEx(sm_pTraceLog,
            m_cRefs,
            this,
            "FORWARDING_HANDLER::OnWinHttpCompletionInternal Enter",
            reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(dwInternetStatus)),
            NULL);
    }

    DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
        "FORWARDING_HANDLER::OnWinHttpCompletionInternal %x --%p",
        dwInternetStatus, this);

    //
    // Exclusive lock on the winhttp handle to protect from a client disconnect/
    // server stop closing the handle while we are using it.
    //
    // WinHttp can call async completion on the same thread/stack, so
    // we have to account for that and not try to take the lock again,
    // otherwise, we could end up in a deadlock.
    //

    fEndRequest = (dwInternetStatus == WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING);

    if (TlsGetValue(g_dwTlsIndex) != this)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == NULL);
        if (m_RequestStatus != FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
        {
            // Webscoket has already been guarded by critical section
            // Only require exclisive lock for non-websocket scenario which has duplex channel
            // Otherwise, there will be a deadlock
            AcquireSRWLockExclusive(&m_RequestLock);
            TlsSetValue(g_dwTlsIndex, this);
            fExclusiveLock = TRUE;
            DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        }
        else
        {
            AcquireSRWLockShared(&m_RequestLock);
            TlsSetValue(g_dwTlsIndex, this);
            fSharedLock = TRUE;
            DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        }
    }

    if (fEndRequest)
    {
        dwHandlers = InterlockedDecrement(&m_dwHandlers);
    }

    if (m_fFinishRequest)
    {
        // Request was done by another thread, skip
        goto Finished;
    }

    if(m_fClientDisconnected && (m_RequestStatus != FORWARDER_DONE))
    {
        hr = ERROR_CONNECTION_ABORTED;
        goto Failure;
    }

    if (m_RequestStatus == FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
    {
        fAnotherCompletionExpected = TRUE;

        if(m_pWebSocket == NULL)
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
        hr = OnWinHttpCompletionSendRequestOrWriteComplete(hRequest,
            dwInternetStatus,
            &fClientError,
            &fAnotherCompletionExpected);
        break;

    case WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE:
        hr = OnWinHttpCompletionStatusHeadersAvailable(hRequest,
            &fAnotherCompletionExpected);
        break;

    case WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE:
        hr = OnWinHttpCompletionStatusDataAvailable(hRequest,
            *reinterpret_cast<const DWORD *>(lpvStatusInformation), // dwBytes
            &fAnotherCompletionExpected);
        break;

    case WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
        hr = OnWinHttpCompletionStatusReadComplete(pResponse,
            dwStatusInformationLength,
            &fAnotherCompletionExpected);
        break;

    case WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
        hr = HRESULT_FROM_WIN32(static_cast<const WINHTTP_ASYNC_RESULT *>(lpvStatusInformation)->dwError);
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

        fAnotherCompletionExpected = FALSE;

        if (m_RequestStatus != FORWARDER_DONE)
        {
            hr = ERROR_CONNECTION_ABORTED;
            fClientError = m_fClientDisconnected;
            goto Failure;
        }
        break;

    case WINHTTP_CALLBACK_STATUS_CONNECTION_CLOSED:
        hr = ERROR_CONNECTION_ABORTED;
        break;

    default:
        //
        // E_UNEXPECTED is rarely used, if seen means that this condition may been occurred.
        //
        DBG_ASSERT(FALSE);
        hr = E_UNEXPECTED;
        if (sm_pTraceLog != NULL)
        {
            WriteRefTraceLogEx(sm_pTraceLog,
                m_cRefs,
                this,
                "FORWARDING_HANDLER::OnWinHttpCompletionInternal Unexpected WinHTTP Status",
                reinterpret_cast<PVOID>(static_cast<DWORD_PTR>(dwInternetStatus)),
                NULL);
        }
        break;
    }

    //
    // Handle failure code for switch statement above.
    //
    if (FAILED(hr))
    {
        goto Failure;
    }

    //
    // WinHTTP completion handled successfully.
    //
    goto Finished;

Failure:
    m_RequestStatus = FORWARDER_DONE;
    if(!m_fHasError)
    {
        m_fHasError = TRUE;
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
                    NULL);
            }
            else
            {
                LoadString(g_hModule,
                    IDS_SERVER_ERROR,
                    strDescription.QueryStr(),
                    strDescription.QuerySizeCCH());
            }
            strDescription.SyncWithBuffer();

#ifdef DEBUG
            // addingmore data to error description to help us diaganose the issue
            STACK_STRA(strMoreData, 128);
            sprintf_s(strMoreData.QueryStr(), 100, "OnWinHttpCompletionInternal --%p--%d--%d--%d\n", this, GetCurrentThreadId(), dwInternetStatus, m_fServerResetConn);
            strMoreData.SyncWithBuffer();
            strDescription.AppendA(strMoreData.QueryStr());
#endif // DEBUG

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

Finished:
    //
    // Since we use TLS to guard WinHttp operation, call PostCompletion instead of
    // IndicateCompletion to allow cleaning up the TLS before thread reuse.
    // Never post after the request has been finished for whatever reason
    //
    // Only postCompletion after all WinHttp handle got closed,
    // i.e., received WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING callback for all handles
    // So that no further WinHttp callback will be called
    // Never post completion again after that
    // Otherwise, there will be a AV as the request already passed IIS pipeline
    //
    if (fEndRequest && !m_fFinishRequest && dwHandlers ==0)
    {
        //
        // Happy path
        //
#ifdef DEBUG
        DWORD counter = InterlockedIncrement(&g_dwLogCounter) % ASPNETCORE_DEBUG_STRU_ARRAY_SIZE;
        sprintf_s(g_strLogs[counter].QueryStr(), ASPNETCORE_DEBUG_STRU_BUFFER_SIZE, 
            "PostCompletion 0 --%p--%p--%d\n", this, m_pW3Context, GetCurrentThreadId());
        g_strLogs[counter].SyncWithBuffer();
#endif // DEBUG

        // Marked the request is finished, no more PostCompletion is allowed
        RemoveRequest();
        m_fFinishRequest = TRUE;
        fDoPostCompletion = TRUE;
        if (m_pWebSocket != NULL)
        {
            delete(m_pWebSocket);
            m_pWebSocket = NULL;
        }
    }

    else if (m_RequestStatus == FORWARDER_DONE)
    {
        //
        // Error path
        //
        RemoveRequest();
        if (m_hRequest != NULL && !m_fHttpHandleInClose)
        {
            m_fHttpHandleInClose = TRUE;
            WinHttpCloseHandle(m_hRequest);
            m_hRequest = NULL;
        }

        if (m_pWebSocket != NULL && !m_fWebSocketHandleInClose)
        {
            m_fWebSocketHandleInClose = TRUE;
            m_pWebSocket->TerminateRequest();
        }

        if (fEndRequest)
        {
            fDoPostCompletion = (dwHandlers == 0 && !m_fFinishRequest);
            if (fDoPostCompletion)
            {
#ifdef DEBUG
                DWORD counter = InterlockedIncrement(&g_dwLogCounter) % ASPNETCORE_DEBUG_STRU_ARRAY_SIZE;
                sprintf_s(g_strLogs[counter].QueryStr(), ASPNETCORE_DEBUG_STRU_BUFFER_SIZE,
                    "PostCompletion 1 --%p--%p--%d\n", this, m_pW3Context, GetCurrentThreadId());
                g_strLogs[counter].SyncWithBuffer();
#endif // DEBUG
                // Marked the request is finished, no more PostCompletion is allowed
                m_fFinishRequest = TRUE;
            }
        }
    }
    else if (!fAnotherCompletionExpected)
    {
        //
        // Regular async IO operation
        //
        fDoPostCompletion = !m_fFinishRequest;
#ifdef DEBUG
        if (fDoPostCompletion)
        {
            DWORD counter = InterlockedIncrement(&g_dwLogCounter) % ASPNETCORE_DEBUG_STRU_ARRAY_SIZE;
            sprintf_s(g_strLogs[counter].QueryStr(), ASPNETCORE_DEBUG_STRU_BUFFER_SIZE,
                "PostCompletion 2 --%p--%p--%d\n", this, m_pW3Context, GetCurrentThreadId());
            g_strLogs[counter].SyncWithBuffer();
        }
#endif // DEBUG
    }

    //
    // No code should access IIS m_pW3Context after posting the completion.
    //
    if (fDoPostCompletion)
    {
        m_pW3Context->PostCompletion(0);
    }

    if (fExclusiveLock)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        TlsSetValue(g_dwTlsIndex, NULL);
        ReleaseSRWLockExclusive(&m_RequestLock);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == NULL);
    }
    else if (fSharedLock)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        TlsSetValue(g_dwTlsIndex, NULL);
        ReleaseSRWLockShared(&m_RequestLock);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == NULL);
    }

    DereferenceForwardingHandler();

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

    //
    // completion for sending the initial request or request entity to
    // winhttp, get more request entity if available, else start receiving
    // the response
    //
    if (m_BytesToReceive > 0)
    {
        if (m_pEntityBuffer == NULL)
        {
            m_pEntityBuffer = GetNewResponseBuffer(
                ENTITY_BUFFER_SIZE);
            if (m_pEntityBuffer == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }
        }

        if (sm_pTraceLog != NULL)
        {
            WriteRefTraceLogEx(sm_pTraceLog,
                m_cRefs,
                this,
                "Calling ReadEntityBody",
                NULL,
                NULL);
        }
        hr = pRequest->ReadEntityBody(
            m_pEntityBuffer + 6,
            min(m_BytesToReceive, BUFFER_SIZE),
            TRUE,       // fAsync
            NULL,       // pcbBytesReceived
            NULL);      // pfCompletionPending
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

                if (!WinHttpWriteData(m_hRequest,
                    "0\r\n\r\n",
                    5,
                    NULL))
                {
                    hr = HRESULT_FROM_WIN32(GetLastError());
                    goto Finished;
                }
                *pfAnotherCompletionExpected = TRUE;

                goto Finished;
            }
        }
        else if (FAILED(hr))
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

    if (!WinHttpReceiveResponse(hRequest, NULL))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }
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

    UNREFERENCED_PARAMETER(pfAnotherCompletionExpected);

    //
    // Headers are available, read the status line and headers and pass
    // them on to the client
    //
    // WinHttpQueryHeaders operates synchronously,
    // no need for taking reference.
    //
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
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        //
        // WinHttpQueryHeaders operates synchronously,
        // no need for taking reference.
        //
        if (!WinHttpQueryHeaders(hRequest,
            WINHTTP_QUERY_RAW_HEADERS_CRLF,
            WINHTTP_HEADER_NAME_BY_INDEX,
            bufHeaderBuffer.QueryPtr(),
            &dwHeaderSize,
            WINHTTP_NO_HEADER_INDEX))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
    }

    if (FAILED(hr = strHeaders.CopyW(
        reinterpret_cast<PWSTR>(bufHeaderBuffer.QueryPtr()))))
    {
        goto Finished;
    }

    // Issue: The reason we add trailing \r\n is to eliminate issues that have been observed
    // in some configurations where status and headers would not have final \r\n nor \r\n\r\n
    // (last header was null terminated).That caused crash within header parsing code that expected valid
    // format. Parsing code was fized to return ERROR_INVALID_PARAMETER, but we still should make
    // Example of a status+header string that was causing problems (note the missing \r\n at the end)
    // HTTP/1.1 302 Moved Permanently\r\n....\r\nLocation:http://site\0
    //

    if (!strHeaders.IsEmpty() && strHeaders.QueryStr()[strHeaders.QueryCCH() - 1] != '\n')
    {
        hr = strHeaders.Append("\r\n");
        if (FAILED(hr))
        {
            goto Finished;
        }
    }

    if (FAILED(hr = SetStatusAndHeaders(
        strHeaders.QueryStr(),
        strHeaders.QueryCCH())))
    {
        goto Finished;
    }

    FreeResponseBuffers();

    //
    // If the request was websocket, and response was 101,
    // trigger a flush, so that IIS's websocket module
    // can get a chance to initialize and complete the handshake.
    //

    if (m_fWebSocketEnabled)
    {

        hr = m_pW3Context->GetResponse()->Flush(
            TRUE,
            TRUE,
            NULL,
            NULL);

        if (FAILED(hr))
        {
            *pfAnotherCompletionExpected = FALSE;
        }
        else
        {
            m_RequestStatus = FORWARDER_RECEIVED_WEBSOCKET_RESPONSE;
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
    __out BOOL *                pfAnotherCompletionExpected
)
{
    HRESULT hr = S_OK;

    //
    // Response data is available from winhttp, read it
    //
    if (dwBytes == 0)
    {
        if (m_cContentLength != 0)
        {
            hr = HRESULT_FROM_WIN32(ERROR_WINHTTP_INVALID_SERVER_RESPONSE);
            goto Finished;
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
    if (m_pEntityBuffer == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    if (!WinHttpReadData(hRequest,
        m_pEntityBuffer,
        min(m_BytesToSend, BUFFER_SIZE),
        NULL))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }
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
                hr = HRESULT_FROM_WIN32(ERROR_WINHTTP_INVALID_SERVER_RESPONSE);
                goto Finished;
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
        if (FAILED(hr = pResponse->WriteEntityChunkByReference(&Chunk)))
        {
            goto Finished;
        }
    }

    if (m_cBytesBuffered >= m_cMinBufferLimit)
    {
        //
        // Always post a completion to resume the WinHTTP data pump.
        //
        hr = pResponse->Flush(TRUE,     // fAsync
            TRUE,     // fMoreData
            NULL);    // pcbSent    
        if (FAILED(hr))
        {
            goto Finished;
        }
        *pfAnotherCompletionExpected = TRUE;
    }
    else
    {
        *pfAnotherCompletionExpected = FALSE;
    }

Finished:

    return hr;
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

    sm_pAlloc = new ALLOC_CACHE_HANDLER;
    if (sm_pAlloc == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }

    hr = sm_pAlloc->Initialize(sizeof(FORWARDING_HANDLER),
        128); // nThreshold
    if (FAILED(hr))
    {
        goto Failure;
    }

    //
    // Open the session handle, specify random user-agent that will be
    // overwritten by the client
    //
    sm_hSession = WinHttpOpen(L"",
        WINHTTP_ACCESS_TYPE_NO_PROXY,
        WINHTTP_NO_PROXY_NAME,
        WINHTTP_NO_PROXY_BYPASS,
        WINHTTP_FLAG_ASYNC);
    if (sm_hSession == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    //
    // Don't set non-blocking callbacks WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS, 
    // as we will call WinHttpQueryDataAvailable to get response on the same thread
    // that we received callback from Winhttp on completing sending/forwarding the request
    // 

    //
    // Setup the callback function
    //
    if (WinHttpSetStatusCallback(sm_hSession,
        FORWARDING_HANDLER::OnWinHttpCompletion,
        (WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
            WINHTTP_CALLBACK_STATUS_SENDING_REQUEST),
        NULL) == WINHTTP_INVALID_STATUS_CALLBACK)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    //
    // Make sure we see the redirects (rather than winhttp doing it
    // automatically)
    //
    DWORD dwRedirectOption = WINHTTP_OPTION_REDIRECT_POLICY_NEVER;
    if (!WinHttpSetOption(sm_hSession,
        WINHTTP_OPTION_REDIRECT_POLICY,
        &dwRedirectOption,
        sizeof(dwRedirectOption)))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    // Initialize Application Manager
    APPLICATION_MANAGER *pApplicationManager = APPLICATION_MANAGER::GetInstance();
    if (pApplicationManager == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }

    hr = pApplicationManager->Initialize();
    if (FAILED(hr))
    {
        goto Failure;
    }

    // Initialize PROTOCOL_CONFIG
    sm_ProtocolConfig.Initialize();

    if (FAILED(hr = sm_strErrorFormat.Resize(256)))
    {
        goto Failure;
    }

    if (LoadString(g_hModule,
        IDS_INVALID_PROPERTY,
        sm_strErrorFormat.QueryStr(),
        sm_strErrorFormat.QuerySizeCCH()) == 0)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }
    sm_strErrorFormat.SyncWithBuffer();

    // If RegisterEventSource failed, we cannot do any thing about it
    // No need to check whether the returned handle is valid

    if (g_pHttpServer->IsCommandLineLaunch())
    {
        sm_hEventLog = RegisterEventSource(NULL, ASPNETCORE_IISEXPRESS_EVENT_PROVIDER);
    }
    else
    {
        sm_hEventLog = RegisterEventSource(NULL, ASPNETCORE_EVENT_PROVIDER);
    }

    g_dwTlsIndex = TlsAlloc();
    if (g_dwTlsIndex == TLS_OUT_OF_INDEXES)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    if (fEnableReferenceCountTracing)
    {
        sm_pTraceLog = CreateRefTraceLog(10000, 0);
    }

    return S_OK;

Failure:

    StaticTerminate();

    return hr;
}

// static
VOID
FORWARDING_HANDLER::StaticTerminate(
    VOID
)
/*++

Routine Description:

Global termination routine for FORWARDING_HANDLERs

Arguments:

None

Return Value:

None

--*/
{
    //
    // Delete all the statics
    //

    APPLICATION_MANAGER::Cleanup();

    //
    // wait for all server processes to go away
    // for a max of 10 seconds.
    //

    DWORD tickCount = GetTickCount();

    while (g_dwActiveServerProcesses > 0)
    {
        if ((GetTickCount() - tickCount) > 10000)
        {
            break;
        }
        Sleep(250);
    }

    if (sm_hSession != NULL)
    {
        WinHttpCloseHandle(sm_hSession);
        sm_hSession = NULL;
    }

    if (sm_hEventLog != NULL)
    {
        DeregisterEventSource(sm_hEventLog);
        sm_hEventLog = NULL;
    }

    if (g_dwTlsIndex != TLS_OUT_OF_INDEXES)
    {
        DBG_REQUIRE(TlsFree(g_dwTlsIndex));
        g_dwTlsIndex = TLS_OUT_OF_INDEXES;
    }

    sm_strErrorFormat.Reset();

    if (sm_pTraceLog != NULL)
    {
        DestroyRefTraceLog(sm_pTraceLog);
        sm_pTraceLog = NULL;
    }

    if (sm_pAlloc != NULL)
    {
        delete sm_pAlloc;
        sm_pAlloc = NULL;
    }
}

VOID
CopyMultiSzToOutput(
    IGlobalRSCAQueryProvider *  pProvider,
    PCWSTR                      pszList,
    DWORD *                     pcbData
)
{
    PBYTE pvData;
    DWORD cbData = 0;
    PCWSTR pszListCopy = pszList;
    while (*pszList != L'\0')
    {
        cbData += (static_cast<DWORD>(wcslen(pszList)) + 1) * sizeof(WCHAR);
        pszList += wcslen(pszList) + 1;
    }
    cbData += sizeof(WCHAR);
    if (FAILED(pProvider->GetOutputBuffer(cbData,
        &pvData)))
    {
        return;
    }
    memcpy(pvData,
        pszListCopy,
        cbData);
    *pcbData = cbData;
}

struct AFFINITY_LOOKUP_CONTEXT
{
    DWORD       timeout;
    PCWSTR      pszServer;
    BUFFER *    pHostNames;
    DWORD       cbData;
};

struct CACHE_CONTEXT
{
    PCSTR   pszHostName;
    IGlobalRSCAQueryProvider *pProvider;
    __field_bcount_part(cbBuffer, cbData)
        PBYTE   pvData;
    DWORD   cbData;
    DWORD   cbBuffer;
};

VOID
FORWARDING_HANDLER::TerminateRequest(
    BOOL    fClientInitiated
)
{
    BOOL fAcquiredLock = FALSE;
    if (TlsGetValue(g_dwTlsIndex) != this)
    {
        AcquireSRWLockExclusive(&m_RequestLock);
        TlsSetValue(g_dwTlsIndex, this);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        fAcquiredLock = TRUE;
    }

    // Only set the disconnect flag as the disconnect happens, request most likely is in OnAsyncCompletion
    // If we close the handle here, most likely WinHttp callback happens on the same threads. 
    // We will have two OnAyncCompletion calls and may have race on PostCompletion
    // Need do more investigation
    if (!m_fHttpHandleInClose)
    {
        m_fClientDisconnected = fClientInitiated;
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_INFO,
            "FORWARDING_HANDLER::TerminateRequest");
        RemoveRequest();

        if (m_RequestStatus == FORWARDER_RECEIVED_WEBSOCKET_RESPONSE)
        {
            //
            // websocket client is gone, cannot finish closing handshake gracefully
            // have to terminate the request
            //
            if (m_pWebSocket != NULL)
            {
                m_pWebSocket->TerminateRequest();
            }
        }
    }
    if (fAcquiredLock)
    {
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == this);
        TlsSetValue(g_dwTlsIndex, NULL);
        ReleaseSRWLockExclusive(&m_RequestLock);
        DBG_ASSERT(TlsGetValue(g_dwTlsIndex) == NULL);
    }
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
        return NULL;
    }

    BYTE *pBuffer = (BYTE *)HeapAlloc(GetProcessHeap(),
        0, // dwFlags
        dwBufferSize);
    if (pBuffer == NULL)
    {
        return NULL;
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
    m_pEntityBuffer = NULL;
    m_cBytesBuffered = 0;
}
