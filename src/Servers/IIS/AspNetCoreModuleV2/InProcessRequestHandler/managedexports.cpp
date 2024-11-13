// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "inprocessapplication.h"
#include "inprocesshandler.h"
#include "requesthandler_config.h"
#include "EventLog.h"

extern bool g_fInProcessApplicationCreated;
extern std::string g_errorPageContent;
extern IHttpServer* g_pHttpServer;

//
// Add support for certain HTTP/2.0 features like trailing headers
// and GOAWAY or RST_STREAM frames.
//

class __declspec(uuid("1a2acc57-cae2-4f28-b4ab-00c8f96b12ec"))
    IHttpResponse4 : public IHttpResponse3
{
public:
    virtual
        HRESULT
        DeleteTrailer(
            _In_ PCSTR  pszHeaderName
        ) = 0;

    virtual
        PCSTR
        GetTrailer(
            _In_  PCSTR    pszHeaderName,
            _Out_ USHORT* pcchHeaderValue = nullptr
        ) const = 0;

    virtual
        VOID
        ResetStream(
            _In_ ULONG errorCode
        ) = 0;

    virtual
        VOID
        SetNeedGoAway(
            VOID
        ) = 0;

    virtual
        HRESULT
        SetTrailer(
            _In_ PCSTR  pszHeaderName,
            _In_ PCSTR  pszHeaderValue,
            _In_ USHORT cchHeaderValue,
            _In_ BOOL fReplace
        ) = 0;
};

//
// Initialization export
//
EXTERN_C __declspec(dllexport)
HRESULT
register_callbacks(
    _In_ IN_PROCESS_APPLICATION* pInProcessApplication,
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ PFN_DISCONNECT_HANDLER disconnect_handler,
    _In_ PFN_ASYNC_COMPLETION_HANDLER async_completion_handler,
    _In_ PFN_REQUESTS_DRAINED_HANDLER requestsDrainedHandler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    if (pInProcessApplication == nullptr)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->SetCallbackHandles(
        request_handler,
        shutdown_handler,
        disconnect_handler,
        async_completion_handler,
        requestsDrainedHandler,
        pvRequstHandlerContext,
        pvShutdownHandlerContext
    );

    return S_OK;
}

EXTERN_C __declspec(dllexport)
HTTP_REQUEST*
http_get_raw_request(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    return pInProcessHandler->QueryHttpContext()->GetRequest()->GetRawHttpRequest();
}

EXTERN_C __declspec(dllexport)
HTTP_RESPONSE*
http_get_raw_response(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    return pInProcessHandler->QueryHttpContext()->GetResponse()->GetRawHttpResponse();
}

EXTERN_C __declspec(dllexport)
HRESULT
http_get_server_variable(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PCSTR pszVariableName,
    _Out_ BSTR* pwszReturn
)
{
    PCWSTR pszVariableValue = nullptr;
    DWORD cbLength = 0;

    *pwszReturn = nullptr;

    HRESULT hr = pInProcessHandler
        ->QueryHttpContext()
        ->GetServerVariable(pszVariableName, &pszVariableValue, &cbLength);

    if (FAILED(hr) || cbLength == 0)
    {
        goto Finished;
    }

    *pwszReturn = SysAllocString(pszVariableValue);

    if (*pwszReturn == nullptr)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
Finished:
    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_set_server_variable(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PCSTR pszVariableName,
    _In_ PCWSTR pszVariableValue
)
{
    return pInProcessHandler
        ->QueryHttpContext()
        ->SetServerVariable(pszVariableName, pszVariableValue);
}

EXTERN_C __declspec(dllexport)
HRESULT
http_set_response_status_code(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ USHORT statusCode,
    _In_ PCSTR pszReason
)
{
    return pInProcessHandler->QueryHttpContext()->GetResponse()->SetStatus(statusCode, pszReason, 0, 0, nullptr,
        true); // fTrySkipCustomErrors
}

EXTERN_C __declspec(dllexport)
HRESULT
http_post_completion(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    DWORD cbBytes
)
{
    return pInProcessHandler->QueryHttpContext()->PostCompletion(cbBytes);
}

EXTERN_C __declspec(dllexport)
HRESULT
http_set_completion_status(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ REQUEST_NOTIFICATION_STATUS requestNotificationStatus
)
{
    HRESULT hr = S_OK;

    pInProcessHandler->IndicateManagedRequestComplete();
    pInProcessHandler->SetAsyncCompletionStatus(requestNotificationStatus);
    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_set_managed_context(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PVOID pvManagedContext
)
{
    // todo: should we consider changing the signature
    HRESULT hr = S_OK;
    pInProcessHandler->SetManagedHttpContext(pvManagedContext);

    return hr;
}

EXTERN_C __declspec(dllexport)
VOID
http_indicate_completion(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ REQUEST_NOTIFICATION_STATUS notificationStatus
)
{
    pInProcessHandler->QueryHttpContext()->IndicateCompletion(notificationStatus);
}

EXTERN_C __declspec(dllexport)
VOID
http_get_completion_info(
    _In_ IHttpCompletionInfo2* info,
    _Out_ DWORD* cbBytes,
    _Out_ HRESULT* hr
)
{
    *cbBytes = info->GetCompletionBytes();
    *hr = info->GetCompletionStatus();
}

//
// the signature should be changed. application's based address should be passed in
//

struct IISConfigurationData
{
    IN_PROCESS_APPLICATION* pInProcessApplication;
    BSTR pwzFullApplicationPath;
    BSTR pwzVirtualApplicationPath;
    BOOL fWindowsAuthEnabled;
    BOOL fBasicAuthEnabled;
    BOOL fAnonymousAuthEnable;
    BSTR pwzBindings;
    DWORD maxRequestBodySize;
};

EXTERN_C __declspec(dllexport)
HRESULT
http_get_application_properties(
    _In_ IISConfigurationData* pIISConfigurationData
)
{
    auto pInProcessApplication = IN_PROCESS_APPLICATION::GetInstance();
    if (pInProcessApplication == nullptr)
    {
        return E_FAIL;
    }

    const auto& pConfiguration = pInProcessApplication->QueryConfig();

    pIISConfigurationData->pInProcessApplication = pInProcessApplication;
    pIISConfigurationData->pwzFullApplicationPath = SysAllocString(pInProcessApplication->QueryApplicationPhysicalPath().c_str());
    pIISConfigurationData->pwzVirtualApplicationPath = SysAllocString(pInProcessApplication->QueryApplicationVirtualPath().c_str());
    pIISConfigurationData->fWindowsAuthEnabled = pConfiguration.QueryWindowsAuthEnabled();
    pIISConfigurationData->fBasicAuthEnabled = pConfiguration.QueryBasicAuthEnabled();
    pIISConfigurationData->fAnonymousAuthEnable = pConfiguration.QueryAnonymousAuthEnabled();

    auto const serverAddresses = BindingInformation::Format(pConfiguration.QueryBindings(), pInProcessApplication->QueryApplicationVirtualPath());
    pIISConfigurationData->pwzBindings = SysAllocString(serverAddresses.c_str());
    pIISConfigurationData->maxRequestBodySize = pInProcessApplication->QueryConfig().QueryMaxRequestBodySizeLimit();
    return S_OK;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_read_request_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _Out_ CHAR* pvBuffer,
    _In_ DWORD dwCbBuffer,
    _Out_ DWORD* pdwBytesReceived,
    _Out_ BOOL* pfCompletionPending
)
{
    HRESULT hr = S_OK;
    *pvBuffer = 0;

    if (pInProcessHandler == nullptr)
    {
        return E_FAIL;
    }
    if (dwCbBuffer == 0)
    {
        return E_FAIL;
    }
    IHttpRequest* pHttpRequest = (IHttpRequest*)pInProcessHandler->QueryHttpContext()->GetRequest();

    // Check if there is anything to read
    if (pHttpRequest->GetRemainingEntityBytes() > 0)
    {
        BOOL fAsync = TRUE;
        hr = pHttpRequest->ReadEntityBody(
            pvBuffer,
            dwCbBuffer,
            fAsync,
            pdwBytesReceived,
            pfCompletionPending);
    }
    else
    {
        *pdwBytesReceived = 0;
        *pfCompletionPending = FALSE;
    }

    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_write_response_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ HTTP_DATA_CHUNK* pDataChunks,
    _In_ DWORD dwChunks,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse* pHttpResponse = (IHttpResponse*)pInProcessHandler->QueryHttpContext()->GetResponse();
    BOOL fAsync = TRUE;
    BOOL fMoreData = TRUE;
    DWORD dwBytesSent = 0;

    HRESULT hr = pHttpResponse->WriteEntityChunks(
        pDataChunks,
        dwChunks,
        fAsync,
        fMoreData,
        &dwBytesSent,
        pfCompletionExpected);

    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_flush_response_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ BOOL fMoreData,
    _Out_ BOOL* pfCompletionExpected
)
{
    IHttpResponse* pHttpResponse = (IHttpResponse*)pInProcessHandler->QueryHttpContext()->GetResponse();

    BOOL fAsync = TRUE;
    DWORD dwBytesSent = 0;

    HRESULT hr = pHttpResponse->Flush(
        fAsync,
        fMoreData,
        &dwBytesSent,
        pfCompletionExpected);
    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_websockets_read_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ CHAR* pvBuffer,
    _In_ DWORD cbBuffer,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ DWORD* pDwBytesReceived,
    _In_ BOOL* pfCompletionPending
)
{
    IHttpRequest3* pHttpRequest = (IHttpRequest3*)pInProcessHandler->QueryHttpContext()->GetRequest();

    BOOL fAsync = TRUE;

    HRESULT hr = pHttpRequest->ReadEntityBody(
        pvBuffer,
        cbBuffer,
        fAsync,
        pfnCompletionCallback,
        pvCompletionContext,
        pDwBytesReceived,
        pfCompletionPending);

    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_websockets_write_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ HTTP_DATA_CHUNK* pDataChunks,
    _In_ DWORD dwChunks,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse2* pHttpResponse = (IHttpResponse2*)pInProcessHandler->QueryHttpContext()->GetResponse();

    BOOL fAsync = TRUE;
    BOOL fMoreData = TRUE;
    DWORD dwBytesSent;

    HRESULT hr = pHttpResponse->WriteEntityChunks(
        pDataChunks,
        dwChunks,
        fAsync,
        fMoreData,
        pfnCompletionCallback,
        pvCompletionContext,
        &dwBytesSent,
        pfCompletionExpected);

    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_websockets_flush_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse2* pHttpResponse = (IHttpResponse2*)pInProcessHandler->QueryHttpContext()->GetResponse();

    BOOL fAsync = TRUE;
    BOOL fMoreData = TRUE;
    DWORD dwBytesSent;

    HRESULT hr = pHttpResponse->Flush(
        fAsync,
        fMoreData,
        pfnCompletionCallback,
        pvCompletionContext,
        &dwBytesSent,
        pfCompletionExpected);
    return hr;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_enable_websockets(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    ((IHttpContext3*)pInProcessHandler->QueryHttpContext())->EnableFullDuplex();
    ((IHttpResponse2*)pInProcessHandler->QueryHttpContext()->GetResponse())->DisableBuffering();

    return S_OK;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_cancel_io(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    return pInProcessHandler->QueryHttpContext()->CancelIo();
}

EXTERN_C __declspec(dllexport)
HRESULT
http_disable_buffering(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    pInProcessHandler->QueryHttpContext()->GetResponse()->DisableBuffering();

    return S_OK;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_close_connection(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    pInProcessHandler->QueryHttpContext()->GetResponse()->ResetConnection();
    return S_OK;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_response_set_unknown_header(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PCSTR pszHeaderName,
    _In_ PCSTR pszHeaderValue,
    _In_ USHORT usHeaderValueLength,
    _In_ BOOL  fReplace
)
{
    return pInProcessHandler->QueryHttpContext()->GetResponse()->SetHeader(pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace);
}

EXTERN_C __declspec(dllexport)
HRESULT
http_response_set_known_header(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ HTTP_HEADER_ID dwHeaderId,
    _In_ PCSTR pszHeaderValue,
    _In_ USHORT usHeaderValueLength,
    _In_ BOOL  fReplace
)
{
    return pInProcessHandler->QueryHttpContext()->GetResponse()->SetHeader(dwHeaderId, pszHeaderValue, usHeaderValueLength, fReplace);
}

EXTERN_C __declspec(dllexport)
HRESULT
http_get_authentication_information(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _Out_ BSTR* pstrAuthType,
    _Out_ VOID** pvToken
)
{
    *pstrAuthType = SysAllocString(pInProcessHandler->QueryHttpContext()->GetUser()->GetAuthenticationType());
    *pvToken = pInProcessHandler->QueryHttpContext()->GetUser()->GetPrimaryToken();

    return S_OK;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_stop_calls_into_managed(_In_ IN_PROCESS_APPLICATION* pInProcessApplication)
{
    if (pInProcessApplication == nullptr)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->StopCallsIntoManaged();
    return S_OK;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_stop_incoming_requests(_In_ IN_PROCESS_APPLICATION* pInProcessApplication)
{
    if (pInProcessApplication == nullptr)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->StopIncomingRequests();
    return S_OK;
}

EXTERN_C __declspec(dllexport)
VOID
set_main_handler(_In_ hostfxr_main_fn main)
{
    // Allow inprocess application to be recreated as we reuse the same CLR
    g_fInProcessApplicationCreated = false;
    IN_PROCESS_APPLICATION::SetMainCallback(main);
}

EXTERN_C __declspec(dllexport)
VOID
http_set_startup_error_page_content(_In_ byte* errorPageContent, int length)
{
    g_errorPageContent.resize(length);
    memcpy(&g_errorPageContent[0], errorPageContent, length);
}

EXTERN_C __declspec(dllexport)
HRESULT
http_has_response4(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _Out_ BOOL* supportsTrailers
)
{
    IHttpResponse4* pHttpResponse;

    HRESULT hr = HttpGetExtendedInterface(g_pHttpServer, pInProcessHandler->QueryHttpContext()->GetResponse(), &pHttpResponse);
    *supportsTrailers = SUCCEEDED(hr);

    return 0;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_response_set_trailer(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PCSTR pszHeaderName,
    _In_ PCSTR pszHeaderValue,
    _In_ USHORT usHeaderValueLength,
    _In_ BOOL fReplace)
{
    // always unknown
    IHttpResponse4* pHttpResponse = (IHttpResponse4*)pInProcessHandler->QueryHttpContext()->GetResponse();
    return pHttpResponse->SetTrailer(pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace);
}

EXTERN_C __declspec(dllexport)
VOID
http_reset_stream(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    ULONG errorCode
)
{
    IHttpResponse4* pHttpResponse = (IHttpResponse4*)pInProcessHandler->QueryHttpContext()->GetResponse();
    pHttpResponse->ResetStream(errorCode);
}

EXTERN_C __declspec(dllexport)
HRESULT
http_response_set_need_goaway(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
    )
{
    IHttpResponse4* pHttpResponse = (IHttpResponse4*)pInProcessHandler->QueryHttpContext()->GetResponse();
    pHttpResponse->SetNeedGoAway();
    return 0;
}

EXTERN_C __declspec(dllexport)
HRESULT
http_query_request_property(
    _In_ HTTP_OPAQUE_ID requestId,
    _In_ HTTP_REQUEST_PROPERTY propertyId,
    _In_reads_bytes_opt_(qualifierSize) PVOID pQualifier,
    _In_ ULONG qualifierSize,
    _Out_writes_bytes_to_opt_(outputBufferSize, *pcbBytesReturned) PVOID pOutput,
    _In_ ULONG outputBufferSize,
    _Out_opt_ PULONG pcbBytesReturned,
    _In_ LPOVERLAPPED pOverlapped
)
{
    IHttpServer3* httpServer3;
    HRESULT hr = HttpGetExtendedInterface<IHttpServer, IHttpServer3>(g_pHttpServer, g_pHttpServer, &httpServer3);
    if (FAILED(hr))
    {
        return hr;
    }

    return httpServer3->QueryRequestProperty(
        requestId,
        propertyId,
        pQualifier,
        qualifierSize,
        pOutput,
        outputBufferSize,
        pcbBytesReturned,
        pOverlapped);
}
// End of export
