// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "applicationinfo.h"
#include "irequesthandler.h"
#include "applicationmanager.h"

extern HTTP_MODULE_ID   g_pModuleId;

class ASPNET_CORE_PROXY_MODULE : NonCopyable, public CHttpModule
{
 public:

     ASPNET_CORE_PROXY_MODULE(std::shared_ptr<APPLICATION_MANAGER> applicationManager) noexcept;

    ~ASPNET_CORE_PROXY_MODULE() = default;

    void * operator new(size_t size, IModuleAllocator * pPlacement)
    {
        return pPlacement->AllocateMemory(static_cast<DWORD>(size));
    }

    VOID
    operator delete(
        void *
    )
    {}

    __override
    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler(
        IHttpContext *          pHttpContext,
        IHttpEventProvider *    pProvider
    ) override;

    __override
    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        IHttpContext *          pHttpContext,
        DWORD                   dwNotification,
        BOOL                    fPostNotification,
        IHttpEventProvider *    pProvider,
        IHttpCompletionInfo *   pCompletionInfo
    ) override;

 private:
    std::shared_ptr<APPLICATION_MANAGER> m_pApplicationManager;
    std::shared_ptr<APPLICATION_INFO> m_pApplicationInfo;
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> m_pHandler;
};

class ASPNET_CORE_PROXY_MODULE_FACTORY : NonCopyable, public IHttpModuleFactory
{
 public:
    ASPNET_CORE_PROXY_MODULE_FACTORY(std::shared_ptr<APPLICATION_MANAGER> applicationManager) noexcept;
    virtual ~ASPNET_CORE_PROXY_MODULE_FACTORY() = default;

    HRESULT
    GetHttpModule(
        CHttpModule **      ppModule,
        IModuleAllocator *  pAllocator
    ) override;

    VOID
    Terminate() noexcept override;
    
 private:
    std::shared_ptr<APPLICATION_MANAGER> m_pApplicationManager;
};
