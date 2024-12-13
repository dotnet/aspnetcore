// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "proxymodule.h"

#include "applicationmanager.h"
#include "applicationinfo.h"
#include "exceptions.h"
#include "DisconnectHandler.h"
#include "SRWExclusiveLock.h"

extern BOOL         g_fInShutdown;

__override

ASPNET_CORE_PROXY_MODULE_FACTORY::ASPNET_CORE_PROXY_MODULE_FACTORY(HTTP_MODULE_ID moduleId, std::shared_ptr<APPLICATION_MANAGER> applicationManager) noexcept
    : m_pApplicationManager(std::move(applicationManager)),
      m_moduleId(moduleId)
{
}

HRESULT
ASPNET_CORE_PROXY_MODULE_FACTORY::GetHttpModule(
    CHttpModule **      ppModule,
    IModuleAllocator *  pAllocator
)
{

    #pragma warning( push )
    #pragma warning ( disable : 26409 ) // Disable "Avoid using new"
    *ppModule = new (pAllocator) ASPNET_CORE_PROXY_MODULE(m_moduleId, m_pApplicationManager);
    #pragma warning( push )
    if (*ppModule == nullptr)
    {
        return E_OUTOFMEMORY;
    }
    return S_OK;
}

__override
VOID
ASPNET_CORE_PROXY_MODULE_FACTORY::Terminate(
) noexcept
/*++

Routine description:

    Function called by IIS for global (non-request-specific) notifications

Arguments:

    None.

Return value:

    None

--*/
{
    delete this;
}

ASPNET_CORE_PROXY_MODULE::ASPNET_CORE_PROXY_MODULE(HTTP_MODULE_ID moduleId, std::shared_ptr<APPLICATION_MANAGER> applicationManager) noexcept
    : m_pApplicationManager(std::move(applicationManager)),
      m_pApplicationInfo(nullptr),
      m_pHandler(nullptr),
      m_moduleId(moduleId),
      m_pDisconnectHandler(nullptr)
{
    InitializeSRWLock(&m_requestLock);
}

ASPNET_CORE_PROXY_MODULE::~ASPNET_CORE_PROXY_MODULE()
{
    RemoveDisconnectHandler();
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

    TraceContextScope traceScope(pHttpContext->GetTraceContext());
    // We don't want OnAsyncCompletion to complete request before OnExecuteRequestHandler exits
    auto lock = SRWExclusiveLock(m_requestLock);

    try
    {
        if (g_fInShutdown)
        {
            LOG_WARN(L"Received a request during shutdown. Will return a 503 response.");
            FINISHED(HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS));
        }

        FINISHED_IF_FAILED(m_pApplicationManager->GetOrCreateApplicationInfo(
            *pHttpContext,
            m_pApplicationInfo));

        FINISHED_IF_FAILED(hr = m_pApplicationInfo->CreateHandler(*pHttpContext, m_pHandler));

        if (m_pHandler == nullptr)
        {
            FINISHED(HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS));
        }

        SetupDisconnectHandler(pHttpContext);

        retVal = m_pHandler->OnExecuteRequestHandler();
    }
    catch (...)
    {
        hr = OBSERVE_CAUGHT_EXCEPTION();
    }

Finished:
    if (FAILED(LOG_IF_FAILED(hr)))
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

    return HandleNotificationStatus(retVal);
}

__override
REQUEST_NOTIFICATION_STATUS
ASPNET_CORE_PROXY_MODULE::OnAsyncCompletion(
    IHttpContext * pHttpContext,
    DWORD,
    BOOL,
    IHttpEventProvider *,
    IHttpCompletionInfo *   pCompletionInfo
)
{
    TraceContextScope traceScope(pHttpContext->GetTraceContext());
    // We don't want OnAsyncCompletion to complete request before OnExecuteRequestHandler exits
    auto lock = SRWExclusiveLock(m_requestLock);

    try
    {
        return HandleNotificationStatus(m_pHandler->OnAsyncCompletion(
            pCompletionInfo->GetCompletionBytes(),
            pCompletionInfo->GetCompletionStatus()));
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        return HandleNotificationStatus(RQ_NOTIFICATION_FINISH_REQUEST);
    }
}

REQUEST_NOTIFICATION_STATUS ASPNET_CORE_PROXY_MODULE::HandleNotificationStatus(REQUEST_NOTIFICATION_STATUS status) noexcept
{
    if (status != RQ_NOTIFICATION_PENDING)
    {
        RemoveDisconnectHandler();
    }

    return status;
}

void ASPNET_CORE_PROXY_MODULE::SetupDisconnectHandler(IHttpContext * pHttpContext)
{
    auto* connection = pHttpContext
        ->GetConnection();

    // connection might be null in when applicationInitialization is running
    if (connection == nullptr)
    {
        return;
    }

    auto* moduleContainer = connection->GetModuleContextContainer();

    #pragma warning( push )
    #pragma warning ( disable : 26466 ) // Disable "Don't use static_cast downcasts". We build without RTTI support so dynamic_cast is not available
    auto* pDisconnectHandler = static_cast<DisconnectHandler*>(moduleContainer->GetConnectionModuleContext(m_moduleId));
    #pragma warning( push )

    if (pDisconnectHandler == nullptr)
    {
        auto newHandler = std::make_unique<DisconnectHandler>(pHttpContext->GetConnection());
        pDisconnectHandler = newHandler.get();
        // ModuleContextContainer takes ownership of disconnectHandler
        // we are trusting that it would not release it before deleting the context
        LOG_IF_FAILED(moduleContainer->SetConnectionModuleContext(static_cast<IHttpConnectionStoredContext*>(newHandler.release()), m_moduleId));
    }

    // make code analysis happy
    if (pDisconnectHandler != nullptr)
    {
        pDisconnectHandler->SetHandler(::ReferenceRequestHandler(m_pHandler.get()));
        m_pDisconnectHandler = pDisconnectHandler;
    }
}

void ASPNET_CORE_PROXY_MODULE::RemoveDisconnectHandler() noexcept
{
    auto* handler = m_pDisconnectHandler;
    m_pDisconnectHandler = nullptr;

    if (handler != nullptr)
    {
        handler->RemoveHandler();
    }
}
