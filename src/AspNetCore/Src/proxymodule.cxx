// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

__override
HRESULT
CProxyModuleFactory::GetHttpModule(
    CHttpModule **      ppModule,
    IModuleAllocator *  pAllocator
)
{
    CProxyModule *pModule = new (pAllocator) CProxyModule();
    if (pModule == NULL)
    {
        return E_OUTOFMEMORY;
    }

    *ppModule = pModule;
    return S_OK;
}

__override
VOID
CProxyModuleFactory::Terminate(
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
    FORWARDING_HANDLER::StaticTerminate();

    WEBSOCKET_HANDLER::StaticTerminate();

    if (g_pResponseHeaderHash != NULL)
    {
        g_pResponseHeaderHash->Clear();
        delete g_pResponseHeaderHash;
        g_pResponseHeaderHash = NULL;
    }

    ALLOC_CACHE_HANDLER::StaticTerminate();

    delete this;
}

CProxyModule::CProxyModule(
) : m_pHandler(NULL)
{
}

CProxyModule::~CProxyModule()
{
    if (m_pHandler != NULL)
    {
        m_pHandler->DereferenceForwardingHandler();
        m_pHandler = NULL;
    }
}

__override
REQUEST_NOTIFICATION_STATUS
CProxyModule::OnExecuteRequestHandler(
    IHttpContext *          pHttpContext,
    IHttpEventProvider *
)
{
    HRESULT hr;
    APPLICATION_MANAGER* pApplicationManager;
    APPLICATION* pApplication;
    ASPNETCORE_CONFIG* config;
    ASPNETCORE_APPLICATION* pAspNetCoreApplication;
    ASPNETCORE_CONFIG::GetConfig(pHttpContext, &config);

    if (config->QueryIsOutOfProcess())// case insensitive
    {
        m_pHandler = new FORWARDING_HANDLER(pHttpContext);
        if (m_pHandler == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Failed;
        }

        return m_pHandler->OnExecuteRequestHandler();
    }
    else if (config->QueryIsInProcess())
    {
        pApplicationManager = APPLICATION_MANAGER::GetInstance();
        if (pApplicationManager == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Failed;
        }

        hr = pApplicationManager->GetApplication(pHttpContext,
            &pApplication);
        if (FAILED(hr))
        {
            goto Failed;
        }

        hr = pApplication->GetAspNetCoreApplication(config, pHttpContext, &pAspNetCoreApplication);
        if (FAILED(hr))
        {
            goto Failed;
        }

        // Allow reading and writing to simultaneously
        ((IHttpContext3*)pHttpContext)->EnableFullDuplex();

        // Disable response buffering by default, we'll do a write behind buffering in managed code
        ((IHttpResponse2*)pHttpContext->GetResponse())->DisableBuffering();

        // TODO: Optimize sync completions
        return pAspNetCoreApplication->ExecuteRequest(pHttpContext);
    }
Failed:
    pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, hr);
    return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
}

__override
REQUEST_NOTIFICATION_STATUS
CProxyModule::OnAsyncCompletion(
    IHttpContext * pHttpContext,
    DWORD                   dwNotification,
    BOOL                    fPostNotification,
    IHttpEventProvider *,
    IHttpCompletionInfo *   pCompletionInfo
)
{
    // TODO store whether we are inproc or outofproc so we don't need to check the config everytime?
    ASPNETCORE_CONFIG* config;
    ASPNETCORE_CONFIG::GetConfig(pHttpContext, &config);

    if (config->QueryIsOutOfProcess())
    {
        return m_pHandler->OnAsyncCompletion(
            pCompletionInfo->GetCompletionBytes(),
            pCompletionInfo->GetCompletionStatus());
    }
    else if (config->QueryIsInProcess())
    {
        return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_CONTINUE;
    }

    pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
}