#include "precomp.hxx"
#include "fx_ver.h"
#include <algorithm>

typedef DWORD(*hostfxr_main_fn) (CONST DWORD argc, CONST WCHAR* argv[]);

// Initialization export

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
VOID
register_callbacks(
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    ASPNETCORE_APPLICATION::GetInstance()->SetCallbackHandles(
        request_handler,
        shutdown_handler,
        pvRequstHandlerContext,
        pvShutdownHandlerContext
    );
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HTTP_REQUEST*
http_get_raw_request(
    _In_ IHttpContext* pHttpContext
)
{
    return pHttpContext->GetRequest()->GetRawHttpRequest();
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HTTP_RESPONSE*
http_get_raw_response(
    _In_ IHttpContext* pHttpContext
)
{
    return pHttpContext->GetResponse()->GetRawHttpResponse();
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT VOID http_set_response_status_code(
    _In_ IHttpContext* pHttpContext,
    _In_ USHORT statusCode,
    _In_ PCSTR pszReason
)
{
    pHttpContext->GetResponse()->SetStatus(statusCode, pszReason);
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_post_completion(
    _In_ IHttpContext* pHttpContext
)
{
    return pHttpContext->PostCompletion(0);
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
VOID
http_indicate_completion(
    _In_ IHttpContext* pHttpContext,
    _In_ REQUEST_NOTIFICATION_STATUS notificationStatus
)
{
    pHttpContext->IndicateCompletion(notificationStatus);
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

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
BSTR // TODO probably should make this a wide string
http_get_application_full_path()
{
    return SysAllocString(ASPNETCORE_APPLICATION::GetInstance()->GetConfig()->QueryApplicationFullPath()->QueryStr());
}

EXTERN_C __MIDL_DECLSPEC_DLLEXPORT
HRESULT
http_read_request_bytes(
    _In_ IHttpContext* pHttpContext,
    _In_ CHAR* pvBuffer,
    _In_ DWORD cbBuffer,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ DWORD* pDwBytesReceived,
    _In_ BOOL* pfCompletionPending
)
{
    IHttpRequest3 *pHttpRequest = (IHttpRequest3*)pHttpContext->GetRequest();

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
http_write_response_bytes(
    _In_ IHttpContext* pHttpContext,
    _In_ HTTP_DATA_CHUNK* pDataChunks,
    _In_ DWORD nChunks,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse2 *pHttpResponse = (IHttpResponse2*)pHttpContext->GetResponse();

    BOOL fAsync = TRUE;
    BOOL fMoreData = TRUE;
    DWORD dwBytesSent;

    HRESULT hr = pHttpResponse->WriteEntityChunks(
        pDataChunks,
        nChunks,
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
http_flush_response_bytes(
    _In_ IHttpContext* pHttpContext,
    _In_ PFN_ASYNC_COMPLETION pfnCompletionCallback,
    _In_ VOID* pvCompletionContext,
    _In_ BOOL* pfCompletionExpected
)
{
    IHttpResponse2 *pHttpResponse = (IHttpResponse2*)pHttpContext->GetResponse();

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

// Thread execution callback
static
VOID
ExecuteAspNetCoreProcess(
    _In_ LPVOID pContext
)
{
    HRESULT hr;
    ASPNETCORE_APPLICATION *pApplication = (ASPNETCORE_APPLICATION*)pContext;

    hr = pApplication->ExecuteApplication();
    if (hr != S_OK)
    {
        // TODO log error
    }
}

ASPNETCORE_APPLICATION*
ASPNETCORE_APPLICATION::s_Application = NULL;

VOID
ASPNETCORE_APPLICATION::SetCallbackHandles(
    _In_ PFN_REQUEST_HANDLER request_handler,
    _In_ PFN_SHUTDOWN_HANDLER shutdown_handler,
    _In_ VOID* pvRequstHandlerContext,
    _In_ VOID* pvShutdownHandlerContext
)
{
    m_RequestHandler = request_handler;
    m_RequstHandlerContext = pvRequstHandlerContext;
    m_ShutdownHandler = shutdown_handler;
    m_ShutdownHandlerContext = pvShutdownHandlerContext;

    // Initialization complete
    SetEvent(m_pInitalizeEvent);
}

HRESULT
ASPNETCORE_APPLICATION::Initialize(
    _In_ ASPNETCORE_CONFIG * pConfig
)
{
    HRESULT hr = S_OK;

    DWORD dwTimeout;
    DWORD dwResult;
    DBG_ASSERT(pConfig != NULL);

    m_pConfiguration = pConfig;

    m_pInitalizeEvent = CreateEvent(
        NULL,   // default security attributes
        TRUE,   // manual reset event
        FALSE,  // not set
        NULL);  // name

    if (m_pInitalizeEvent == NULL)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    m_hThread = CreateThread(
        NULL,       // default security attributes
        0,          // default stack size
        (LPTHREAD_START_ROUTINE)ExecuteAspNetCoreProcess,
        this,       // thread function arguments
        0,          // default creation flags
        NULL);      // receive thread identifier

    if (m_hThread == NULL)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // If the debugger is attached, never timeout
    if (IsDebuggerPresent())
    {
        dwTimeout = INFINITE;
    }
    else
    {
        dwTimeout = pConfig->QueryStartupTimeLimitInMS();
    }

    const HANDLE pHandles[2]{ m_hThread, m_pInitalizeEvent };

    // Wait on either the thread to complete or the event to be set
    dwResult = WaitForMultipleObjects(2, pHandles, FALSE, dwTimeout);

    // It all timed out
    if (dwResult == WAIT_TIMEOUT)
    {
        return HRESULT_FROM_WIN32(dwResult);
    }
    else if (dwResult == WAIT_FAILED)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    dwResult = WaitForSingleObject(m_hThread, 0);

    // The thread ended it means that something failed
    if (dwResult == WAIT_OBJECT_0)
    {
        return HRESULT_FROM_WIN32(dwResult);
    }
    else if (dwResult == WAIT_FAILED)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    return S_OK;
}

HRESULT
ASPNETCORE_APPLICATION::ExecuteApplication(
    VOID
)
{
    HRESULT     hr = S_OK;

    STRU                        strFullPath;
    STRU                        strDotnetExeLocation;
    STRU                        strHostFxrSearchExpression;
    STRU                        strDotnetFolderLocation;
    STRU                        strHighestDotnetVersion;
    STRU                        strApplicationFullPath;
    PWSTR                       strDelimeterContext = NULL;
    PCWSTR                      pszDotnetExeLocation = NULL;
    PCWSTR                      pszDotnetExeString(L"dotnet.exe");
    DWORD                       dwCopyLength;
    HMODULE                     hModule;
    PCWSTR                      argv[2];
    hostfxr_main_fn             pProc;
    std::vector<std::wstring>   vVersionFolders;

    // Get the System PATH value.
    if (!GetEnv(L"PATH", &strFullPath))
    {
        goto Failed;
    }

    // Split on ';', checking to see if dotnet.exe exists in any folders.
    pszDotnetExeLocation = wcstok_s(strFullPath.QueryStr(), L";", &strDelimeterContext);

    while (pszDotnetExeLocation != NULL)
    {
        dwCopyLength = wcsnlen_s(pszDotnetExeLocation, 260);
        if (dwCopyLength == 0)
        {
            continue;
        }

        // We store both the exe and folder locations as we eventually need to check inside of host\\fxr
        // which doesn't need the dotnet.exe portion of the string
        // TODO consider reducing allocations.
        strDotnetExeLocation.Reset();
        strDotnetFolderLocation.Reset();
        hr = strDotnetExeLocation.Copy(pszDotnetExeLocation, dwCopyLength);
        if (FAILED(hr))
        {
            goto Failed;
        }

        hr = strDotnetFolderLocation.Copy(pszDotnetExeLocation, dwCopyLength);
        if (FAILED(hr))
        {
            goto Failed;
        }

        if (dwCopyLength > 0 && pszDotnetExeLocation[dwCopyLength - 1] != L'\\')
        {
            hr = strDotnetExeLocation.Append(L"\\");
            if (FAILED(hr))
            {
                goto Failed;
            }
        }

        hr = strDotnetExeLocation.Append(pszDotnetExeString);
        if (FAILED(hr))
        {
            goto Failed;
        }

        if (PathFileExists(strDotnetExeLocation.QueryStr()))
        {
            // means we found the folder with a dotnet.exe inside of it.
            break;
        }
        pszDotnetExeLocation = wcstok_s(NULL, L";", &strDelimeterContext);
    }

    hr = strDotnetFolderLocation.Append(L"\\host\\fxr");
    if (FAILED(hr))
    {
        goto Failed;
    }

    if (!DirectoryExists(&strDotnetFolderLocation))
    {
        goto Failed;
    }

    // Find all folders under host\\fxr\\ for version numbers.
    hr = strHostFxrSearchExpression.Copy(strDotnetFolderLocation);
    if (FAILED(hr))
    {
        goto Failed;
    }

    hr = strHostFxrSearchExpression.Append(L"\\*");
    if (FAILED(hr))
    {
        goto Failed;
    }

    // As we use the logic from core-setup, we are opting to use std here.
    // TODO remove all uses of std?
    FindDotNetFolders(&strHostFxrSearchExpression, &vVersionFolders);

    if (vVersionFolders.size() == 0)
    {
        goto Failed;
    }

    hr = FindHighestDotNetVersion(vVersionFolders, &strHighestDotnetVersion);
    if (FAILED(hr))
    {
        goto Failed;
    }
    hr = strDotnetFolderLocation.Append(L"\\");
    if (FAILED(hr))
    {
        goto Failed;
    }

    hr = strDotnetFolderLocation.Append(strHighestDotnetVersion.QueryStr());
    if (FAILED(hr))
    {
        goto Failed;

    }

    hr = strDotnetFolderLocation.Append(L"\\hostfxr.dll");
    if (FAILED(hr))
    {
        goto Failed;
    }

    hModule = LoadLibraryW(strDotnetFolderLocation.QueryStr());

    if (hModule == NULL)
    {
        // .NET Core not installed (we can log a more detailed error message here)
        goto Failed;
    }

    // Get the entry point for main
    pProc = (hostfxr_main_fn)GetProcAddress(hModule, "hostfxr_main");
    if (pProc == NULL) {
        goto Failed;
    }

    // The first argument is mostly ignored
    hr = strDotnetExeLocation.Append(pszDotnetExeString);
    if (FAILED(hr))
    {
        goto Failed;
    }

    argv[0] = strDotnetExeLocation.QueryStr();
    PATH::ConvertPathToFullPath(m_pConfiguration->QueryArguments()->QueryStr(), m_pConfiguration->QueryApplicationFullPath()->QueryStr(), &strApplicationFullPath);
    argv[1] = strApplicationFullPath.QueryStr();

    // There can only ever be a single instance of .NET Core
    // loaded in the process but we need to get config information to boot it up in the
    // first place. This is happening in an execute request handler and everyone waits
    // until this initialization is done.

    // We set a static so that managed code can call back into this instance and
    // set the callbacks
    s_Application = this;

    m_ProcessExitCode = pProc(2, argv);
    if (m_ProcessExitCode != 0)
    {
        // TODO error
    }

    return hr;
Failed:
    // TODO log any errors
    return hr;
}

BOOL
ASPNETCORE_APPLICATION::GetEnv(
    _In_ PCWSTR pszEnvironmentVariable,
    _Out_ STRU *pstrResult
)
{
    DWORD dwLength;
    PWSTR pszBuffer= NULL;
    BOOL fSucceeded = FALSE;

    if (pszEnvironmentVariable == NULL)
    {
        goto Finished;
    }
    pstrResult->Reset();
    dwLength = GetEnvironmentVariableW(pszEnvironmentVariable, NULL, 0);

    if (dwLength == 0)
    {
        goto Finished;
    }

    pszBuffer = new WCHAR[dwLength];
    if (GetEnvironmentVariableW(pszEnvironmentVariable, pszBuffer, dwLength) == 0)
    {
        goto Finished;
    }

    pstrResult->Copy(pszBuffer);

    fSucceeded = TRUE;

Finished:
    if (pszBuffer != NULL) {
        delete[] pszBuffer;
    }
    return fSucceeded;
}

VOID
ASPNETCORE_APPLICATION::FindDotNetFolders(
    _In_ STRU *pstrPath,
    _Out_ std::vector<std::wstring> *pvFolders
)
{
    HANDLE handle = NULL;
    WIN32_FIND_DATAW data = { 0 };

    handle = FindFirstFileExW(pstrPath->QueryStr(), FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    if (handle == INVALID_HANDLE_VALUE)
    {
        return;
    }

    do
    {
        std::wstring folder(data.cFileName);
        pvFolders->push_back(folder);
    } while (FindNextFileW(handle, &data));

    FindClose(handle);
}

HRESULT
ASPNETCORE_APPLICATION::FindHighestDotNetVersion(
    _In_ std::vector<std::wstring> vFolders,
    _Out_ STRU *pstrResult
)
{
    HRESULT hr = S_OK;
    fx_ver_t max_ver(-1, -1, -1);
    for (const auto& dir : vFolders)
    {
        fx_ver_t fx_ver(-1, -1, -1);
        if (fx_ver_t::parse(dir, &fx_ver, false))
        {
            max_ver = std::max(max_ver, fx_ver);
        }
    }

    hr = pstrResult->Copy(max_ver.as_str().c_str());

    // we check FAILED(hr) outside of function
    return hr;
}

BOOL
ASPNETCORE_APPLICATION::DirectoryExists(
    _In_ STRU *pstrPath
)
{
    WIN32_FILE_ATTRIBUTE_DATA data;

    if (pstrPath->IsEmpty())
    {
        return false;
    }

    return GetFileAttributesExW(pstrPath->QueryStr(), GetFileExInfoStandard, &data);
}

REQUEST_NOTIFICATION_STATUS
ASPNETCORE_APPLICATION::ExecuteRequest(
    _In_ IHttpContext* pHttpContext
)
{
    if (m_RequestHandler != NULL)
    {
        return m_RequestHandler(pHttpContext, m_RequstHandlerContext);
    }

    pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    return RQ_NOTIFICATION_FINISH_REQUEST;
}


VOID
ASPNETCORE_APPLICATION::Shutdown(
    VOID
)
{
    // First call into the managed server and shutdown
    BOOL result = m_ShutdownHandler(m_ShutdownHandlerContext);
    s_Application = NULL;
    delete this;
}