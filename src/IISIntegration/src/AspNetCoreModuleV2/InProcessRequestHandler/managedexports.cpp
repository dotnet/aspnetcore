// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "inprocessapplication.h"
#include "inprocesshandler.h"
#include "requesthandler_config.h"

extern bool g_fInProcessApplicationCreated;

//
// Initialization export
//
EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
register_callbacks(
    _In_ IN_PROCESS_APPLICATION* pInProcessApplication,
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ PFN_DISCONNECT_HANDLER disconnect_handler,
    _In_ PFN_ASYNC_COMPLETION_HANDLER async_completion_handler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    if (pInProcessApplication == NULL)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->SetCallbackHandles(
        request_handler,
        shutdown_handler,
        disconnect_handler,
        async_completion_handler,
        pvRequstHandlerContext,
        pvShutdownHandlerContext
    );

    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HTTP_REQUEST*
http_get_raw_request(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    return pInProcessHandler->QueryHttpContext()->GetRequest()->GetRawHttpRequest();
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HTTP_RESPONSE*
http_get_raw_response(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    return pInProcessHandler->QueryHttpContext()->GetResponse()->GetRawHttpResponse();
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_get_server_variable(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PCSTR pszVariableName,
    _Out_ BSTR* pwszReturn
)
{
    PCWSTR pszVariableValue;
    DWORD cbLength;

    HRESULT hr = pInProcessHandler
        ->QueryHttpContext()
        ->GetServerVariable(pszVariableName, &pszVariableValue, &cbLength);

    if (FAILED(hr) || cbLength == 0)
    {
        goto Finished;
    }

    *pwszReturn = SysAllocString(pszVariableValue);

    if (*pwszReturn == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
Finished:
    return hr;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_post_completion(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    DWORD cbBytes
)
{
    return pInProcessHandler->QueryHttpContext()->PostCompletion(cbBytes);
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
VOID
http_indicate_completion(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ REQUEST_NOTIFICATION_STATUS notificationStatus
)
{
    pInProcessHandler->QueryHttpContext()->IndicateCompletion(notificationStatus);
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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
};

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_get_application_properties(
    _In_ IISConfigurationData* pIISCofigurationData
)
{
    auto pInProcessApplication = IN_PROCESS_APPLICATION::GetInstance();
    if (pInProcessApplication == NULL)
    {
        return E_FAIL;
    }

    const auto& pConfiguration = pInProcessApplication->QueryConfig();

    pIISCofigurationData->pInProcessApplication = pInProcessApplication;
    pIISCofigurationData->pwzFullApplicationPath = SysAllocString(pInProcessApplication->QueryApplicationPhysicalPath().c_str());
    pIISCofigurationData->pwzVirtualApplicationPath = SysAllocString(pInProcessApplication->QueryApplicationVirtualPath().c_str());
    pIISCofigurationData->fWindowsAuthEnabled = pConfiguration.QueryWindowsAuthEnabled();
    pIISCofigurationData->fBasicAuthEnabled = pConfiguration.QueryBasicAuthEnabled();
    pIISCofigurationData->fAnonymousAuthEnable = pConfiguration.QueryAnonymousAuthEnabled();

    auto const serverAddresses = BindingInformation::Format(pConfiguration.QueryBindings(), pInProcessApplication->QueryApplicationVirtualPath());
    pIISCofigurationData->pwzBindings = SysAllocString(serverAddresses.c_str());
    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

    if (pInProcessHandler == NULL)
    {
        return E_FAIL;
    }
    if (dwCbBuffer == 0)
    {
        return E_FAIL;
    }
    IHttpRequest *pHttpRequest = (IHttpRequest*)pInProcessHandler->QueryHttpContext()->GetRequest();

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

        if (hr == HRESULT_FROM_WIN32(ERROR_HANDLE_EOF))
        {
            // We reached the end of the data
            hr = S_OK;
        }
    }
    else
    {
        *pdwBytesReceived = 0;
        *pfCompletionPending = FALSE;
    }

    return hr;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_write_response_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ HTTP_DATA_CHUNK* pDataChunks,
    _In_ DWORD dwChunks,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse *pHttpResponse = (IHttpResponse*)pInProcessHandler->QueryHttpContext()->GetResponse();
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_flush_response_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ BOOL fMoreData,
    _Out_ BOOL* pfCompletionExpected
)
{
    IHttpResponse *pHttpResponse = (IHttpResponse*)pInProcessHandler->QueryHttpContext()->GetResponse();

    BOOL fAsync = TRUE;
    DWORD dwBytesSent = 0;

    HRESULT hr = pHttpResponse->Flush(
        fAsync,
        fMoreData,
        &dwBytesSent,
        pfCompletionExpected);
    return hr;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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
    IHttpRequest3 *pHttpRequest = (IHttpRequest3*)pInProcessHandler->QueryHttpContext()->GetRequest();

    BOOL fAsync = TRUE;

    HRESULT hr = pHttpRequest->ReadEntityBody(
        pvBuffer,
        cbBuffer,
        fAsync,
        pfnCompletionCallback,
        pvCompletionContext,
        pDwBytesReceived,
        pfCompletionPending);

    if (hr == HRESULT_FROM_WIN32(ERROR_HANDLE_EOF))
    {
        // We reached the end of the data
        hr = S_OK;
    }

    return hr;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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
    IHttpResponse2 *pHttpResponse = (IHttpResponse2*)pInProcessHandler->QueryHttpContext()->GetResponse();

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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_websockets_flush_bytes(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse2 *pHttpResponse = (IHttpResponse2*)pInProcessHandler->QueryHttpContext()->GetResponse();

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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_enable_websockets(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    ((IHttpContext3*)pInProcessHandler->QueryHttpContext())->EnableFullDuplex();
    ((IHttpResponse2*)pInProcessHandler->QueryHttpContext()->GetResponse())->DisableBuffering();

    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_cancel_io(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    return pInProcessHandler->QueryHttpContext()->CancelIo();
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_disable_buffering(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    pInProcessHandler->QueryHttpContext()->GetResponse()->DisableBuffering();

    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_close_connection(
    _In_ IN_PROCESS_HANDLER* pInProcessHandler
)
{
    pInProcessHandler->QueryHttpContext()->GetResponse()->ResetConnection();
    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_stop_calls_into_managed(_In_ IN_PROCESS_APPLICATION* pInProcessApplication)
{
    if (pInProcessApplication == NULL)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->StopCallsIntoManaged();
    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_stop_incoming_requests(_In_ IN_PROCESS_APPLICATION* pInProcessApplication)
{
    if (pInProcessApplication == NULL)
    {
        return E_INVALIDARG;
    }

    pInProcessApplication->StopIncomingRequests();
    return S_OK;
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
VOID
set_main_handler(_In_ hostfxr_main_fn main)
{
    // Allow inprocess application to be recreated as we reuse the same CLR
    g_fInProcessApplicationCreated = false;
    IN_PROCESS_APPLICATION::SetMainCallback(main);
}

// End of export
