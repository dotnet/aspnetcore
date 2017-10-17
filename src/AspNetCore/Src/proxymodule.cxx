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
    HRESULT hr = S_OK;
    ASPNETCORE_CONFIG     *pConfig = NULL;
    APPLICATION_MANAGER   *pApplicationManager = NULL;
    APPLICATION           *pApplication = NULL;
    hr = ASPNETCORE_CONFIG::GetConfig(pHttpContext, &pConfig);
    if (FAILED(hr))
    {
        goto Failed;
    }

    pApplicationManager = APPLICATION_MANAGER::GetInstance();
    if (pApplicationManager == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failed;
    }

    hr = pApplicationManager->GetApplication(
                    pHttpContext,
                    pConfig,
                    &pApplication);
    if (FAILED(hr))
    {
        goto Failed;
    }

    m_pHandler = new FORWARDING_HANDLER(pHttpContext, pApplication);

    if (m_pHandler == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failed;
    }

    return m_pHandler->OnExecuteRequestHandler();

Failed:
    pHttpContext->GetResponse()->SetStatus(500, "Internal Server Error", 0, hr);
    return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
}

__override
REQUEST_NOTIFICATION_STATUS
CProxyModule::OnAsyncCompletion(
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