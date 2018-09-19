// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "proxymodule.h"

#include "applicationmanager.h"
#include "applicationinfo.h"
#include "exceptions.h"
#include "DisconnectHandler.h"

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
    VOID
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
}

ASPNET_CORE_PROXY_MODULE::~ASPNET_CORE_PROXY_MODULE()
{
    if (m_pDisconnectHandler != nullptr)
    {
        m_pDisconnectHandler->SetHandler(nullptr);
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
    REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;

    try
    {
        if (g_fInShutdown)
        {
            FINISHED(HRESULT_FROM_WIN32(ERROR_SERVER_SHUTDOWN_IN_PROGRESS));
        }

        auto moduleContainer = pHttpContext
            ->GetConnection()
            ->GetModuleContextContainer();

        #pragma warning( push )
        #pragma warning ( disable : 26466 ) // Disable "Don't use static_cast downcasts". We build without RTTI support so dynamic_cast is not available
        m_pDisconnectHandler = static_cast<DisconnectHandler*>(moduleContainer->GetConnectionModuleContext(m_moduleId));
        #pragma warning( push )

        if (m_pDisconnectHandler == nullptr)
        {
            auto disconnectHandler = std::make_unique<DisconnectHandler>();
            m_pDisconnectHandler = disconnectHandler.get();
            // ModuleContextContainer takes ownership of disconnectHandler
            // we are trusting that it would not release it before deleting the context
            FINISHED_IF_FAILED(moduleContainer->SetConnectionModuleContext(static_cast<IHttpConnectionStoredContext*>(disconnectHandler.release()), m_moduleId));
        }

        m_pDisconnectHandler->SetHandler(this);

        FINISHED_IF_FAILED(m_pApplicationManager->GetOrCreateApplicationInfo(
            *pHttpContext,
            m_pApplicationInfo));

        FINISHED_IF_FAILED(m_pApplicationInfo->CreateHandler(*pHttpContext, m_pHandler));

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

void ASPNET_CORE_PROXY_MODULE::NotifyDisconnect() const
{
    m_pHandler->NotifyDisconnect();
}
