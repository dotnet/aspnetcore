// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "applicationinfo.h"
#include "irequesthandler.h"

extern HTTP_MODULE_ID   g_pModuleId;
extern IHttpServer     *g_pHttpServer;
extern HMODULE          g_hAspnetCoreRH;

class ASPNET_CORE_PROXY_MODULE : public CHttpModule
{
 public:

     ASPNET_CORE_PROXY_MODULE();

    ~ASPNET_CORE_PROXY_MODULE();

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
    );

    __override
    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        IHttpContext *          pHttpContext,
        DWORD                   dwNotification,
        BOOL                    fPostNotification,
        IHttpEventProvider *    pProvider,
        IHttpCompletionInfo *   pCompletionInfo
    );

 private:

    APPLICATION_INFO *m_pApplicationInfo;
    IAPPLICATION      *m_pApplication;
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>  m_pHandler;
};

class ASPNET_CORE_PROXY_MODULE_FACTORY : public IHttpModuleFactory
{
 public:
    HRESULT
    GetHttpModule(
        CHttpModule **      ppModule,
        IModuleAllocator *  pAllocator
    );

    VOID
    Terminate();
};
