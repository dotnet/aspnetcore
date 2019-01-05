// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "forwardinghandler.h"

class CProxyModule : public CHttpModule
{
public:

    CProxyModule();

    ~CProxyModule();

    void * operator new(size_t size, IModuleAllocator * pPlacement)
    {
        return pPlacement->AllocateMemory(static_cast<DWORD>(size));
    }

    VOID
        operator delete(
            void *
            )
    {
    }

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

    FORWARDING_HANDLER * m_pHandler;
};

class CProxyModuleFactory : public IHttpModuleFactory
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