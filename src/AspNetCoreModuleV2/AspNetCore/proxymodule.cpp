// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "proxymodule.h"

#include "applicationmanager.h"
#include "applicationinfo.h"
#include "acache.h"
#include "exceptions.h"

extern BOOL         g_fInShutdown;

__override
HRESULT
ASPNET_CORE_PROXY_MODULE_FACTORY::GetHttpModule(
    CHttpModule **      ppModule,
    IModuleAllocator *  pAllocator
)
{
    try
    {
        *ppModule = THROW_IF_NULL_ALLOC(new (pAllocator) ASPNET_CORE_PROXY_MODULE());;
        return S_OK;
    }
    CATCH_RETURN();
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
    ALLOC_CACHE_HANDLER::StaticTerminate();
    delete this;
}

ASPNET_CORE_PROXY_MODULE::ASPNET_CORE_PROXY_MODULE(
) : m_pApplicationInfo(nullptr), m_pHandler(nullptr)
{
}

__override
REQUEST_NOTIFICATION_STATUS
ASPNET_CORE_PROXY_MODULE::OnExecuteRequestHandler(
    IHttpContext *          pHttpContext,
    IHttpEventProvider *
)
{
    HRESULT hr = S_OK;
    REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;
    try
    {

        if (g_fInShutdown)
        {
            FINISHED(HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS));
        }

        auto pApplicationManager = APPLICATION_MANAGER::GetInstance();

        FINISHED_IF_FAILED(pApplicationManager->GetOrCreateApplicationInfo(
            *pHttpContext,
            m_pApplicationInfo));

        std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER> pApplication;
        FINISHED_IF_FAILED(m_pApplicationInfo->GetOrCreateApplication(pHttpContext, pApplication));

        IREQUEST_HANDLER* pHandler;
        // Create RequestHandler and process the request
        FINISHED_IF_FAILED(pApplication->CreateHandler(pHttpContext, &pHandler));
        m_pHandler.reset(pHandler);

        retVal = m_pHandler->OnExecuteRequestHandler();
    }
    catch (...)
    {
        hr = OBSERVE_CAUGHT_EXCEPTION();
    }

Finished:
    if (LOG_IF_FAILED(hr))
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
    try
    {
        return m_pHandler->OnAsyncCompletion(
            pCompletionInfo->GetCompletionBytes(),
            pCompletionInfo->GetCompletionStatus());
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
}
