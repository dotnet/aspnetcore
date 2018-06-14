// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "proxymodule.h"

#include "applicationmanager.h"
#include "applicationinfo.h"
#include "acache.h"
#include "hostfxroptions.h"


__override
HRESULT
ASPNET_CORE_PROXY_MODULE_FACTORY::GetHttpModule(
    CHttpModule **      ppModule,
    IModuleAllocator *  pAllocator
)
{
    ASPNET_CORE_PROXY_MODULE *pModule = new (pAllocator) ASPNET_CORE_PROXY_MODULE();
    if (pModule == NULL)
    {
        return E_OUTOFMEMORY;
    }
    *ppModule = pModule;
    return S_OK;
}

__override
VOID
ASPNET_CORE_PROXY_MODULE_FACTORY::Terminate(
    VOID
)
/*++

Routine description:

    Function called by IIS for global (non-request-specific) notifications

Arguments:

    None.

Return value:

    None

--*/
{
    /* FORWARDING_HANDLER::StaticTerminate();

    WEBSOCKET_HANDLER::StaticTerminate();*/

    ALLOC_CACHE_HANDLER::StaticTerminate();
    delete this;
}

ASPNET_CORE_PROXY_MODULE::ASPNET_CORE_PROXY_MODULE(
) : m_pApplicationInfo(NULL), m_pHandler(NULL)
{
}

ASPNET_CORE_PROXY_MODULE::~ASPNET_CORE_PROXY_MODULE()
{
    if (m_pApplicationInfo != NULL)
    {
        m_pApplicationInfo->DereferenceApplicationInfo();
        m_pApplicationInfo = NULL;
    }

    if (m_pHandler != NULL)
    {
        m_pHandler->DereferenceRequestHandler();
        m_pHandler = NULL;
    }
}

__override
REQUEST_NOTIFICATION_STATUS
ASPNET_CORE_PROXY_MODULE::OnExecuteRequestHandler(
    IHttpContext *          pHttpContext,
    IHttpEventProvider *
)
{
    HRESULT hr = S_OK;
    APPLICATION_MANAGER   *pApplicationManager = NULL;
    REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;
    IAPPLICATION* pApplication = NULL;
    STRU struExeLocation;

    if (g_fInShutdown)
    {
        hr = HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS);
        goto Finished;
    }

    pApplicationManager = APPLICATION_MANAGER::GetInstance();

    hr = pApplicationManager->GetOrCreateApplicationInfo(
        g_pHttpServer,
        pHttpContext,
        &m_pApplicationInfo);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (!m_pApplicationInfo->QueryAllowStart())
    {
        // Application cannot be started due to wrong hosting mode
        // the error should already been logged to window event log for the first request
        hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
        goto Finished;
    }

    // app_offline check to avoid loading aspnetcorerh.dll unnecessarily
    if (m_pApplicationInfo->AppOfflineFound())
    {
        // servicing app_offline
        HTTP_DATA_CHUNK   DataChunk;
        IHttpResponse    *pResponse = NULL;
        APP_OFFLINE_HTM  *pAppOfflineHtm = NULL;

        pResponse = pHttpContext->GetResponse();
        pAppOfflineHtm = m_pApplicationInfo->QueryAppOfflineHtm();
        DBG_ASSERT(pAppOfflineHtm);
        DBG_ASSERT(pResponse);

        // Ignore failure hresults as nothing we can do
        // Set fTrySkipCustomErrors to true as we want client see the offline content
        pResponse->SetStatus(503, "Service Unavailable", 0, hr, NULL, TRUE);
        pResponse->SetHeader("Content-Type",
                             "text/html",
                             (USHORT)strlen("text/html"),
                             FALSE
        );

        DataChunk.DataChunkType = HttpDataChunkFromMemory;
        DataChunk.FromMemory.pBuffer = (PVOID)pAppOfflineHtm->m_Contents.QueryStr();
        DataChunk.FromMemory.BufferLength = pAppOfflineHtm->m_Contents.QueryCB();
        pResponse->WriteEntityChunkByReference(&DataChunk);

        retVal = RQ_NOTIFICATION_FINISH_REQUEST;
        goto Finished;
    }

    // make sure assmebly is loaded and application is created
    hr = m_pApplicationInfo->EnsureApplicationCreated(pHttpContext);
    if (FAILED(hr))
    {
        goto Finished;
    }

    m_pApplicationInfo->ExtractApplication(&pApplication);

    // make sure application is in running state
    // cannot recreate the application as we cannot reload clr for inprocess
    if (pApplication != NULL &&
        pApplication->QueryStatus() != APPLICATION_STATUS::RUNNING &&
        pApplication->QueryStatus() != APPLICATION_STATUS::STARTING)
    {
        hr = HRESULT_FROM_WIN32(ERROR_SERVER_DISABLED);
        goto Finished;
    }

    // Create RequestHandler and process the request
    hr = pApplication->CreateHandler(pHttpContext,
                                     &m_pHandler);

    if (FAILED(hr))
    {
        goto Finished;
    }

    retVal = m_pHandler->OnExecuteRequestHandler();

Finished:
    if (FAILED(hr))
    {
        retVal = RQ_NOTIFICATION_FINISH_REQUEST;
        if (hr == HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS))
        {
            pHttpContext->GetResponse()->SetStatus(503, "Service Unavailable", 0, hr);
        }
        else
        {
            pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, hr);
        }
    }

    if (pApplication != NULL)
    {
        pApplication->DereferenceApplication();
    }
    return retVal;
}

__override
REQUEST_NOTIFICATION_STATUS
ASPNET_CORE_PROXY_MODULE::OnAsyncCompletion(
    IHttpContext *,
    DWORD,
    BOOL,
    IHttpEventProvider *,
    IHttpCompletionInfo *   pCompletionInfo
)
{
    return m_pHandler->OnAsyncCompletion(
        pCompletionInfo->GetCompletionBytes(),
        pCompletionInfo->GetCompletionStatus());
}
